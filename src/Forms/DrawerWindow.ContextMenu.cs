namespace winDrawer;

/// <summary>
/// Drawer list display commands: the right-click menu (refresh plus the three sort modes) and the F5
/// shortcut that replaces the refresh button the drawer used to show at its bottom-right corner.
/// </summary>
public partial class DrawerWindow
{
    private ContextMenuStrip? _drawerContextMenu;
    private ToolStripMenuItem? _sortByNameMenuItem;
    private ToolStripMenuItem? _newestFirstMenuItem;
    private ToolStripMenuItem? _oldestFirstMenuItem;

    /// <summary>Raised after the user picked another sort mode so the host can reload its snapshot.</summary>
    public event EventHandler? SortModeChanged;

    /// <summary>Display order currently used by this drawer.</summary>
    public DrawerSortMode SortMode => _drawer.SortMode;

    private void InitializeDrawerContextMenu()
    {
        var refreshMenuItem = new ToolStripMenuItem("Refresh") { ShortcutKeyDisplayString = "F5" };
        refreshMenuItem.Click += (_, _) => _ = RefreshDrawerAsync();

        _sortByNameMenuItem = CreateSortMenuItem("Sort by name", DrawerSortMode.NameAscending);
        _newestFirstMenuItem = CreateSortMenuItem("Newest first", DrawerSortMode.NewestFirst);
        _oldestFirstMenuItem = CreateSortMenuItem("Oldest first", DrawerSortMode.OldestFirst);

        _drawerContextMenu = new ContextMenuStrip { AccessibleName = "Drawer list commands" };
        _drawerContextMenu.Items.AddRange(
        [
            refreshMenuItem,
            new ToolStripSeparator(),
            _sortByNameMenuItem,
            _newestFirstMenuItem,
            _oldestFirstMenuItem
        ]);
        _drawerContextMenu.Opening += (_, _) => UpdateSortModeChecks();

        UiContextMenuTheming.Apply(_drawerContextMenu, _theme);
        drawerContentsListView.ContextMenuStrip = _drawerContextMenu;
        UpdateSortModeChecks();
    }

    private ToolStripMenuItem CreateSortMenuItem(string text, DrawerSortMode mode)
    {
        var item = new ToolStripMenuItem(text) { CheckOnClick = false };
        item.Click += (_, _) => ApplySortMode(mode);
        return item;
    }

    /// <summary>Marks the active mode; the three sort entries are mutually exclusive.</summary>
    private void UpdateSortModeChecks()
    {
        if (_sortByNameMenuItem is null || _newestFirstMenuItem is null || _oldestFirstMenuItem is null)
        {
            return;
        }

        _sortByNameMenuItem.Checked = _drawer.SortMode == DrawerSortMode.NameAscending;
        _newestFirstMenuItem.Checked = _drawer.SortMode == DrawerSortMode.NewestFirst;
        _oldestFirstMenuItem.Checked = _drawer.SortMode == DrawerSortMode.OldestFirst;
    }

    /// <summary>
    /// Applies and persists a new order, then reloads the list. Picking the active mode again only
    /// refreshes, so it never rewrites the settings file without a change.
    /// </summary>
    private void ApplySortMode(DrawerSortMode mode)
    {
        if (_drawer.SortMode == mode)
        {
            _ = RefreshDrawerAsync();
            return;
        }

        DrawerLogger.LogInformation($"Drawer '{_drawer.Name}' sort mode changed to {mode}.");
        _drawer = _drawer.WithSortMode(mode);
        UpdateSortModeChecks();

        if (!_settingsRepository.UpdateDrawerSortMode(_drawer.Path, mode))
        {
            DrawerLogger.LogWarning($"Failed to persist the sort mode of drawer '{_drawer.Name}'.");
        }

        SortModeChanged?.Invoke(this, EventArgs.Empty);
        _ = RefreshDrawerAsync();
    }

    /// <summary>F5 refreshes the list; the drawer no longer has a refresh button.</summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F5)
        {
            _ = RefreshDrawerAsync();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void DisposeDrawerContextMenu()
    {
        var menu = _drawerContextMenu;
        _drawerContextMenu = null;
        drawerContentsListView.ContextMenuStrip = null;
        menu?.Dispose();
    }
}
