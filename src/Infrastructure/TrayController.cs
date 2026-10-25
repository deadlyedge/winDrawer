using System.ComponentModel;
using System.Runtime.InteropServices;

namespace winDrawer;

/// <summary>
/// Encapsulates the system tray icon and associated menu interactions for the main window.
/// </summary>
internal sealed class TrayController(Action toggleAction, Action openSettingsAction, Action exitAction) : IDisposable
{
    private readonly Action _toggleAction = toggleAction ?? throw new ArgumentNullException(nameof(toggleAction));
    private readonly Action _openSettingsAction = openSettingsAction ?? throw new ArgumentNullException(nameof(openSettingsAction));
    private readonly Action _exitAction = exitAction ?? throw new ArgumentNullException(nameof(exitAction));
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _trayMenu;
    private Icon? _ownedIcon;
    private Point _trayMenuAnchor;
    private bool _hasTrayMenuTargetLocation;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

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

            // Keep the menu owned by NotifyIcon so Windows does not create a taskbar button for
            // the popup. The internal NotifyIcon path can still choose the wrong monitor with
            // mirrored taskbars or mixed DPI, so Opening/Opened correct the location afterwards.
            _trayMenu.Opening += TrayMenu_Opening;
            _trayMenu.Opened += TrayMenu_Opened;

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

    /// <summary>
    /// Calculates the location for the system-owned menu before it becomes visible. This runs after
    /// NotifyIcon's internal positioning, so the corrected value wins whenever WinForms respects it.
    /// </summary>
    private void TrayMenu_Opening(object? sender, CancelEventArgs e)
    {
        try
        {
            if (_trayMenu is not { } menu)
            {
                _hasTrayMenuTargetLocation = false;
                return;
            }

            var cursor = Cursor.Position;
            var size = menu.GetPreferredSize(Size.Empty);
            if (size.IsEmpty)
            {
                size = menu.Size;
            }

            _trayMenuAnchor = cursor;
            _hasTrayMenuTargetLocation = true;
            menu.Location = CalculateTrayMenuLocation(cursor, size);
        }
        catch (Exception ex)
        {
            _hasTrayMenuTargetLocation = false;
            DrawerLogger.LogWarning($"Failed to prepare the tray menu location. {ex}");
        }
    }

    /// <summary>
    /// Final correction after the menu is visible. WinForms' NotifyIcon path can still override the
    /// location during ShowInTaskbar, so move the real window when it did not end up on the cursor's
    /// working area. The menu remains owned by NotifyIcon, which keeps it out of the taskbar.
    /// </summary>
    private void TrayMenu_Opened(object? sender, EventArgs e)
    {
        if (!_hasTrayMenuTargetLocation || _trayMenu is not { } menu || !menu.Visible)
        {
            return;
        }

        try
        {
            var target = CalculateTrayMenuLocation(_trayMenuAnchor, menu.Size);
            var current = menu.Bounds.Location;
            if (current == target)
            {
                return;
            }

            if (SetWindowPos(
                menu.Handle,
                IntPtr.Zero,
                target.X,
                target.Y,
                0,
                0,
                SwpNoSize | SwpNoZOrder | SwpNoActivate))
            {
                DrawerLogger.LogDebug($"Tray menu repositioned from {current} to {target}.");
                return;
            }

            DrawerLogger.LogWarning(
                $"Failed to reposition the tray menu to {target}; Win32 error {Marshal.GetLastWin32Error()}.");
        }
        catch (Exception ex)
        {
            DrawerLogger.LogWarning($"Failed to reposition the tray menu. {ex}");
        }
    }

    /// <summary>
    /// Keeps the menu inside the working area of the display that contains the cursor. Using the
    /// working area (instead of the full monitor bounds) prevents a bottom taskbar from pushing the
    /// menu onto a neighbouring display.
    /// </summary>
    private static Point CalculateTrayMenuLocation(Point cursor, Size menuSize)
    {
        var screen = Screen.FromPoint(cursor);
        var workingArea = screen.WorkingArea;
        var direction = GetTrayMenuDirection(workingArea, cursor);

        var x = cursor.X;
        var y = cursor.Y;
        switch (direction)
        {
            case ToolStripDropDownDirection.AboveLeft:
                x -= menuSize.Width;
                y -= menuSize.Height;
                break;
            case ToolStripDropDownDirection.AboveRight:
                y -= menuSize.Height;
                break;
            case ToolStripDropDownDirection.BelowLeft:
                x -= menuSize.Width;
                break;
        }

        var maximumX = Math.Max(workingArea.Left, workingArea.Right - menuSize.Width);
        var maximumY = Math.Max(workingArea.Top, workingArea.Bottom - menuSize.Height);
        return new Point(
            Math.Clamp(x, workingArea.Left, maximumX),
            Math.Clamp(y, workingArea.Top, maximumY));
    }

    /// <summary>
    /// Opens the menu away from the taskbar edge the icon belongs to, so the menu never tries to
    /// expand onto a neighbouring display.
    /// </summary>
    private static ToolStripDropDownDirection GetTrayMenuDirection(Rectangle workingArea, Point cursor)
    {
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
