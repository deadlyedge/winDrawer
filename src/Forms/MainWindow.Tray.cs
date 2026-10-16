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
                    if (!Visible)
                    {
                        Show();
                    }

                    if (WindowState == FormWindowState.Minimized)
                    {
                        WindowState = FormWindowState.Normal;
                    }

                    Activate();
                    SettingsButton_Click(this, EventArgs.Empty);
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

        private void ToggleVisibility()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ToggleVisibility));
                return;
            }

            if (Visible && WindowState != FormWindowState.Minimized)
            {
                DrawerLogger.LogInformation("Hiding main window from tray request.");
                Hide();
            }
            else
            {
                DrawerLogger.LogInformation("Showing main window from tray request.");
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }
    }
}
