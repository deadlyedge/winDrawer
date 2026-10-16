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
    private Icon? _ownedIcon;

    public bool Initialize(string iconPath, Icon? fallbackIcon, string tooltipText)
    {
        if (_trayIcon != null)
        {
            DrawerLogger.LogDebug("TrayController already initialized; skipping duplicate initialization.");
            return _trayIcon.Visible && _trayIcon.Icon != null;
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
                Text = tooltipText
            };

            ApplyIcon(iconPath, fallbackIcon);
            if (_trayIcon.Icon == null)
            {
                throw new InvalidOperationException("No system tray icon or fallback icon could be loaded.");
            }

            _trayIcon.DoubleClick += (_, _) => _toggleAction();
            _trayIcon.Visible = true;

            DrawerLogger.LogInformation("System tray icon initialized.");
            return _trayIcon.Visible;
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Failed to initialize system tray icon.", ex);
            Dispose();
            return false;
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
                AssignOwnedIcon(new Icon(iconPath, AppConstants.Ui.TrayIconSize));
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
            AssignOwnedIcon((Icon)fallbackIcon.Clone());
            DrawerLogger.LogInformation("Fallback tray icon applied.");
        }
        catch (Exception ex)
        {
            DrawerLogger.LogWarning($"Failed to assign fallback tray icon. {ex}");
        }
    }

    private void AssignOwnedIcon(Icon icon)
    {
        if (_trayIcon == null)
        {
            icon.Dispose();
            return;
        }

        try
        {
            var previousIcon = _ownedIcon;
            _trayIcon.Icon = icon;
            _ownedIcon = icon;
            previousIcon?.Dispose();
        }
        catch
        {
            icon.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        var trayIcon = _trayIcon;
        _trayIcon = null;
        try
        {
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Error hiding tray icon during disposal.", ex);
        }

        try
        {
            trayIcon?.Dispose();
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Error disposing tray icon.", ex);
        }

        try
        {
            _ownedIcon?.Dispose();
            _ownedIcon = null;
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Error disposing the tray icon image.", ex);
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
