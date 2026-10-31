namespace winDrawer
{
    public partial class MainWindow : Form
    {
        private void InitializeTray()
        {
            var baseDir = AppContext.BaseDirectory;
            var iconPath = Path.Combine(baseDir, AppConstants.Directories.Assets, AppConstants.Files.AppIconFileName);

            DrawerLogger.LogDebug($"Initializing tray icon using '{iconPath}'.");

            _trayController = new TrayController(
                ToggleVisibility,
                () =>
                {
                    EnsureMainWindowAndDrawerShown();
                    OpenSettingsPanel();
                },
                () =>
                {
                    DrawerLogger.LogInformation("Application exit initiated from tray.");
                    ShutdownApplication("tray menu");
                });

            if (!_trayController.Initialize(iconPath, Icon, AppConstants.Branding.ApplicationDisplayName))
            {
                DrawerLogger.LogWarning("System tray initialization failed; showing the main window in the taskbar as a fallback.");
                ShowInTaskbar = true;
            }
        }

        /// <summary>
        /// Single entry point for the tray menu, the tray double-click and the Win+Shift+D shortcut.
        /// Hiding and showing always cover the main window together with the drawer window that is
        /// currently open, because an owned window is <em>not</em> hidden automatically when its
        /// owner is hidden.
        /// </summary>
        private void ToggleVisibility()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ToggleVisibility));
                return;
            }

            if (IsMainWindowShown)
            {
                HideMainWindowAndDrawer();
            }
            else
            {
                ShowMainWindowAndDrawer();
            }
        }

        /// <summary>True while the main window is on screen and not minimized.</summary>
        private bool IsMainWindowShown => Visible && WindowState != FormWindowState.Minimized;

        /// <summary>
        /// Brings the main window and the open drawer window back on screen before another entry
        /// point uses them. The tray's Settings command would otherwise show only the main window
        /// and leave a hidden drawer orphaned behind the settings panel.
        /// </summary>
        private void EnsureMainWindowAndDrawerShown()
        {
            if (IsMainWindowShown)
            {
                Activate();
                return;
            }

            ShowMainWindowAndDrawer();
        }

        private void HideMainWindowAndDrawer()
        {
            DrawerLogger.LogInformation("Hiding the main window and the open drawer window.");

            var drawerWindow = _currentDrawerWindow;
            if (drawerWindow is { IsDisposed: false, Visible: true })
            {
                drawerWindow.Hide();
            }

            // Restore the configured opacity while nothing is on screen, so the next show starts from a
            // known state instead of a half-finished fade.
            _windowFade?.Cancel();
            Hide();
        }

        private void ShowMainWindowAndDrawer()
        {
            DrawerLogger.LogInformation("Showing the main window and the open drawer window.");

            Show();
            WindowState = FormWindowState.Normal;

            // The main window can move while everything is hidden (for example through the settings
            // panel's height preview), so glue the drawer back to it before showing it again.
            PositionDrawerWindowNextToMainWindow();

            var drawerWindow = _currentDrawerWindow;
            if (drawerWindow is { IsDisposed: false })
            {
                // No slide here: the drawer is restored where MainWindow put it, only the fade runs.
                drawerWindow.PrepareAppearanceTransition(0);
                drawerWindow.Show(this);
                drawerWindow.StartAppearanceTransition();
            }

            Activate();
            PlayWindowFadeIn();
        }
    }
}
