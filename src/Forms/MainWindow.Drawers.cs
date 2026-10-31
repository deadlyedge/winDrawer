using winDrawer.Configuration;

namespace winDrawer
{
    public partial class MainWindow : Form
    {
        private void ShowDrawerSettings(int index)
        {
            if (index < 0 || index >= _currentDrawers.Count)
            {
                return;
            }

            var drawer = _currentDrawers[index];
            using var dialog = new DrawerSettingsDialog(drawer);
            ApplySettingsWindowPlacement(dialog);
            var result = dialog.ShowDialog(this);
            if (result == DialogResult.Cancel)
            {
                return;
            }

            if (result == DialogResult.Yes && dialog.DeleteRequested)
            {
                if (!UiMessages.Confirm(this, "Delete Drawer", $"Remove '{drawer.Name}' from the drawer list? The folder and its files will not be deleted."))
                {
                    return;
                }

                if (!_settingsRepository.DeleteDrawer(drawer.Path, out var deleteError))
                {
                    UiMessages.ShowError(this, "Delete Drawer", deleteError);
                    return;
                }

                CloseDrawerIfOpen(drawer.Path);
                ReloadSettingsFromStorage();
                return;
            }

            if (result != DialogResult.OK)
            {
                return;
            }

            var requestedPath = PathComparison.Normalize(dialog.DrawerPath);
            if (!PathComparison.AreSame(drawer.Path, requestedPath) && !Directory.Exists(requestedPath))
            {
                UiMessages.ShowWarning(this, "Drawer Settings", $"The selected folder does not exist or is inaccessible:{Environment.NewLine}{requestedPath}");
                return;
            }

            var pathChanged = !PathComparison.AreSame(drawer.Path, requestedPath);
            if (pathChanged && _currentDrawers.Any(existing => !PathComparison.AreSame(existing.Path, drawer.Path) && PathComparison.AreSame(existing.Path, requestedPath)))
            {
                UiMessages.ShowWarning(this, "Drawer Settings", "Another drawer already uses the selected folder.");
                return;
            }

            if (!_settingsRepository.UpdateDrawer(drawer.Path, dialog.DrawerName, requestedPath, dialog.OverlayColor, out var updateError))
            {
                UiMessages.ShowWarning(this, "Drawer Settings", updateError);
                return;
            }

            if (pathChanged)
            {
                CloseDrawerIfOpen(drawer.Path);
            }
            else
            {
                _currentDrawerWindow?.ApplyDrawerDefinition(_settingsRepository.GetSnapshot().Drawers[index]);
            }

            ReloadSettingsFromStorage();
        }

        private void CloseDrawerIfOpen(string path)
        {
            if (_currentOpenDrawer == null || !PathComparison.AreSame(_currentOpenDrawer.Path, path))
            {
                return;
            }

            _currentDrawerWindow?.Close();
            _currentDrawerWindow = null;
            _currentOpenDrawer = null;
        }

        /// <summary>
        /// Clears the drawer-list selection after the open drawer closed. The event can arrive from a
        /// different thread, so the UI work is marshalled once instead of duplicating the reset.
        /// </summary>
        private void ClearDrawerListSelection()
        {
            // A drawer switch keeps the clicked row selected until the new window is ready.
            if (_switchingDrawer)
            {
                return;
            }

            if (drawerListBox.InvokeRequired)
            {
                drawerListBox.BeginInvoke(new Action(ClearDrawerListSelection));
                return;
            }

            _suppressSelectionChange = true;
            try
            {
                drawerListBox.SelectedIndex = -1;
            }
            finally
            {
                _suppressSelectionChange = false;
            }
        }

        /// <summary>
        /// Keeps the open drawer window glued to the right edge of the main window. Called when the
        /// main window moves and when it is shown again after being hidden.
        /// </summary>
        private void PositionDrawerWindowNextToMainWindow()
        {
            var drawerWindow = _currentDrawerWindow;
            if (drawerWindow is null || drawerWindow.IsDisposed || drawerWindow.WindowState != FormWindowState.Normal)
            {
                return;
            }

            // A running slide must not fight the authoritative position below.
            drawerWindow.CancelAppearanceTransition();
            drawerWindow.Location = new Point(Right, Top);
        }

        /// <summary>
        /// Stops a drawer appearance transition. Called whenever something else takes over placement:
        /// the user drags the main window, the drawer is hidden or it is restored from hidden state.
        /// </summary>
        private void CancelDrawerAppearanceTransition()
        {
            if (_currentDrawerWindow is { IsDisposed: false } drawerWindow)
            {
                drawerWindow.CancelAppearanceTransition();
            }
        }

        private bool OpenDrawer(DrawerDefinition drawer)
        {
            if (string.IsNullOrWhiteSpace(drawer.Path))
            {
                DrawerLogger.LogWarning($"Requested to open drawer '{drawer.Name}' without a path.");
                return false;
            }

            if (!Directory.Exists(drawer.Path))
            {
                DrawerLogger.LogWarning($"Drawer path not found for '{drawer.Name}': {drawer.Path}");
                UiMessages.ShowWarning(this, "Drawer Missing", $"Folder not found:{Environment.NewLine}{drawer.Path}");
                return false;
            }

            try
            {
                DrawerLogger.LogInformation($"Creating drawer window for '{drawer.Name}' ({drawer.Path}).");
                var drawerWindow = new DrawerWindow(_settingsRepository, drawer, _currentTheme)
                {
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(Right, Top),
                    TopMost = IsAlwaysOnTop
                };
                _currentDrawerWindow = drawerWindow;
                drawerWindow.SortModeChanged += (_, _) => ReloadSettingsFromStorage();
                drawerWindow.FormClosed += (_, _) =>
                {
                    // Clear the active state first, so the reload cannot briefly reselect the closed
                    // drawer. The reload is still needed to pick up the size the window persisted.
                    // During a drawer switch the selection reset is suppressed, so the clicked row
                    // stays selected until the new window is ready.
                    _currentDrawerWindow = null;
                    _currentOpenDrawer = null;
                    ClearDrawerListSelection();
                    ReloadSettingsFromStorage();
                };
                // Prepare before showing so the drawer never flashes at its final position first.
                drawerWindow.PrepareAppearanceTransition(DpiScale.Pixels(
                    AppConstants.Ui.Animation.DrawerSlideDistanceLogical,
                    DeviceDpi));
                drawerWindow.Show(this);
                drawerWindow.StartAppearanceTransition();
                DrawerLogger.LogInformation($"Drawer window displayed for '{drawer.Name}'.");
                return true;
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError($"Failed to open drawer '{drawer.Name}'.", ex);
                UiMessages.ShowError(this, "Drawer Error", $"Failed to open drawer '{drawer.Name}': {ex.Message}");
                return false;
            }
        }

        private void AddDrawerButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the folder you want to link as a drawer.",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                DrawerLogger.LogDebug("Add drawer dialog cancelled.");
                return;
            }

            var selectedPath = dialog.SelectedPath;
            if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
            {
                DrawerLogger.LogWarning($"Invalid drawer folder selected: '{selectedPath}'.");
                UiMessages.ShowWarning(this, "Invalid Folder", $"Invalid folder selected:{Environment.NewLine}{selectedPath}");
                return;
            }

            var name = Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            try
            {
                DrawerLogger.LogInformation($"Attempting to add drawer '{name}' at '{selectedPath}'.");
                var added = _settingsRepository.AddDrawer(selectedPath, name);
                if (!added)
                {
                    DrawerLogger.LogInformation($"Drawer '{selectedPath}' already exists.");
                    UiMessages.ShowInformation(this, "Add Drawer", "The selected folder is already configured as a drawer.");
                    return;
                }

                // Reload settings from storage to refresh the UI
                ReloadSettingsFromStorage();
                DrawerLogger.LogInformation($"Drawer added successfully: '{selectedPath}'.");
                UiMessages.ShowInformation(this, "Add Drawer", $"Drawer added: {selectedPath}");
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError($"Failed to add drawer '{selectedPath}'.", ex);
                UiMessages.ShowError(this, "Add Drawer Error", $"Failed to add drawer: {ex.Message}");
            }
        }
    }
}
