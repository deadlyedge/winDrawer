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

            var showHideItem = new ToolStripMenuItem("Show/Hide window and drawer");
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

            _trayMenu.Items.AddRange(
            [
                showHideItem,
                settingsItem,
                new ToolStripSeparator(),
                exitItem
            ]);

            _trayIcon = new NotifyIcon
            {
                Text = tooltipText
            };

            // Do not assign ContextMenuStrip: NotifyIcon shows it through the internal
            // ContextMenuStrip.ShowInTaskbar path, which can pick the wrong monitor when the
            // taskbar is mirrored across displays or the monitors use different DPIs. Showing the
            // menu ourselves uses the regular working-area-constrained ToolStripDropDown path.
            _trayIcon.MouseUp += TrayIcon_MouseUp;

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

    private void TrayIcon_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        ShowTrayMenu();
    }

    /// <summary>
    /// Shows the tray menu on the display that contains the cursor. The menu is deliberately not
    /// attached to <see cref="NotifyIcon.ContextMenuStrip"/>; see the comment in
    /// <see cref="Initialize"/>.
    /// </summary>
    private void ShowTrayMenu()
    {
        var menu = _trayMenu;
        if (menu == null)
        {
            return;
        }

        var cursor = Cursor.Position;
        try
        {
            var screen = Screen.FromPoint(cursor);
            var workingArea = screen.WorkingArea;
            var anchor = new Point(
                Math.Clamp(cursor.X, workingArea.Left, workingArea.Right - 1),
                Math.Clamp(cursor.Y, workingArea.Top, workingArea.Bottom - 1));

            menu.Show(anchor, GetTrayMenuDirection(screen, cursor));
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Failed to show the tray menu at the cursor position.", ex);
            try
            {
                menu.Show(cursor);
            }
            catch (Exception fallbackException)
            {
                DrawerLogger.LogError("Fallback tray menu display failed.", fallbackException);
            }
        }
    }

    /// <summary>
    /// Opens the menu away from the taskbar edge the icon belongs to, so the menu never tries to
    /// expand onto a neighbouring display.
    /// </summary>
    private static ToolStripDropDownDirection GetTrayMenuDirection(Screen screen, Point cursor)
    {
        var workingArea = screen.WorkingArea;

        if (cursor.Y >= workingArea.Bottom)
        {
            return ToolStripDropDownDirection.AboveLeft;
        }

        if (cursor.Y < workingArea.Top)
        {
            return ToolStripDropDownDirection.BelowLeft;
        }

        if (cursor.X < workingArea.Left)
        {
            return ToolStripDropDownDirection.BelowRight;
        }

        if (cursor.X >= workingArea.Right)
        {
            return ToolStripDropDownDirection.BelowLeft;
        }

        // The tray is normally inside the working area only with auto-hide or a floating taskbar.
        return cursor.Y > workingArea.Top + (workingArea.Height / 2)
            ? ToolStripDropDownDirection.AboveLeft
            : ToolStripDropDownDirection.BelowLeft;
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
