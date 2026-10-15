namespace winDrawer;

/// <summary>
/// Encapsulates the system tray icon and associated menu interactions for the main window.
/// </summary>
internal sealed class TrayController(Action toggleAction, Action openSettingsAction, Action exitAction) : IDisposable
{
    private readonly Action _toggleAction = toggleAction ?? throw new ArgumentNullException(nameof(toggleAction));
    private readonly Action _openSettingsAction = openSettingsAction ?? throw new ArgumentNullException(nameof(openSettingsAction));
    private readonly Action _exitAction = exitAction ?? throw new ArgumentNullException(nameof(exitAction));
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _trayMenu;

    public void Initialize(string iconPath, Icon? fallbackIcon, string tooltipText)
    {
        if (_trayIcon != null)
        {
            DrawerLogger.LogDebug("TrayController already initialized; skipping duplicate initialization.");
            return;
        }

        try
        {
            _trayMenu = new ContextMenuStrip();

            var showHideItem = new ToolStripMenuItem("Show/Hide");
            showHideItem.Click += (_, _) =>
            {
                DrawerLogger.LogInformation("Tray show/hide requested.");
                _toggleAction();
            };

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (_, _) =>
            {
                DrawerLogger.LogInformation("Tray settings requested.");
                _openSettingsAction();
            };

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) =>
            {
                DrawerLogger.LogInformation("Tray exit requested.");
                try
                {
                    Dispose();
                }
                catch (Exception disposeEx)
                {
                    DrawerLogger.LogError("Failed to dispose tray resources during exit.", disposeEx);
                }
                _exitAction();
            };

            _trayMenu.Items.AddRange(new ToolStripItem[]
            {
                showHideItem,
                settingsItem,
                new ToolStripSeparator(),
                exitItem
            });

            _trayIcon = new NotifyIcon
            {
                ContextMenuStrip = _trayMenu,
                Text = tooltipText,
                Visible = true
            };

            ApplyIcon(iconPath, fallbackIcon);
            _trayIcon.DoubleClick += (_, _) => _toggleAction();

            DrawerLogger.LogInformation("System tray icon initialized.");
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Failed to initialize system tray icon.", ex);
            Dispose();
        }
    }

    private void ApplyIcon(string iconPath, Icon? fallbackIcon)
    {
        if (_trayIcon == null)
        {
            return;
        }

        if (File.Exists(iconPath))
        {
            try
            {
                _trayIcon.Icon = new Icon(iconPath, AppConstants.Ui.TrayIconSize);
                DrawerLogger.LogInformation($"Tray icon loaded from '{iconPath}'.");
                return;
            }
            catch (Exception ex)
            {
                DrawerLogger.LogWarning($"Failed to load tray icon from '{iconPath}'. Using fallback. {ex}");
            }
        }
        else
        {
            DrawerLogger.LogWarning($"Tray icon asset not found at '{iconPath}'. Using fallback icon.");
        }

        AssignFallbackIcon(fallbackIcon);
    }

    private void AssignFallbackIcon(Icon? fallbackIcon)
    {
        if (_trayIcon == null || fallbackIcon == null)
        {
            return;
        }

        try
        {
            _trayIcon.Icon = (Icon)fallbackIcon.Clone();
            DrawerLogger.LogInformation("Fallback tray icon applied.");
        }
        catch (Exception ex)
        {
            DrawerLogger.LogWarning($"Failed to assign fallback tray icon. {ex}");
        }
    }

    public void Dispose()
    {
        try
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Error disposing tray icon.", ex);
        }

        try
        {
            _trayMenu?.Dispose();
            _trayMenu = null;
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Error disposing tray menu.", ex);
        }
    }
}
