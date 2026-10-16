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

        private void OpenDrawer(DrawerDefinition drawer)
        {
            if (string.IsNullOrWhiteSpace(drawer.Path))
            {
                DrawerLogger.LogWarning($"Requested to open drawer '{drawer.Name}' without a path.");
                return;
            }

            if (!Directory.Exists(drawer.Path))
            {
                DrawerLogger.LogWarning($"Drawer path not found for '{drawer.Name}': {drawer.Path}");
                UiMessages.ShowWarning(this, "Drawer Missing", $"Folder not found:{Environment.NewLine}{drawer.Path}");
                return;
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
                drawerWindow.FormClosed += (_, _) =>
                {
                    ReloadSettingsFromStorage();
                    _currentDrawerWindow = null;
                    _currentOpenDrawer = null;
                    // 清空list中的选中状态
                    if (drawerListBox.InvokeRequired)
                    {
                        drawerListBox.BeginInvoke(new Action(() =>
                        {
                            _suppressSelectionChange = true;
                            drawerListBox.SelectedIndex = -1;
                            _suppressSelectionChange = false;
                        }));
                    }
                    else
                    {
                        _suppressSelectionChange = true;
                        drawerListBox.SelectedIndex = -1;
                        _suppressSelectionChange = false;
                    }
                };
                drawerWindow.Show(this);
                DrawerLogger.LogInformation($"Drawer window displayed for '{drawer.Name}'.");
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError($"Failed to open drawer '{drawer.Name}'.", ex);
                UiMessages.ShowError(this, "Drawer Error", $"Failed to open drawer '{drawer.Name}': {ex.Message}");
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
