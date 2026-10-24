using System.Collections.Specialized;

namespace winDrawer;

/// <summary>
/// Drag-out support for <see cref="DrawerWindow"/>.
///
/// The drawer only publishes the selected entries as <c>CF_HDROP</c> data; Explorer (or whichever
/// application is the drop target) performs the actual move/copy with its own progress and conflict
/// UI. The application itself never moves, copies or deletes user files.
/// </summary>
public partial class DrawerWindow
{
    private void InitializeDragDrop()
    {
        drawerContentsListView.ItemDrag += DrawerContentsListView_ItemDrag;
    }

    private void DrawerContentsListView_ItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var selectedItems = drawerContentsListView.SelectedItems.Cast<ListViewItem>().ToArray();
        var paths = CollectDragOutPaths(e.Item as ListViewItem, selectedItems);
        if (paths.Count == 0)
        {
            return;
        }

        DrawerLogger.LogInformation($"Starting drag-out of {paths.Count} item(s) from '{_drawer.Path}'.");

        var fileDrop = new StringCollection();
        fileDrop.AddRange(paths.ToArray());
        var dataObject = new DataObject();
        dataObject.SetFileDropList(fileDrop);

        DragDropEffects effect;
        try
        {
            // No Link: allowing it would let the target create shortcuts, which this feature is not
            // about. Copy and Move mirror what Explorer offers for a file-system drag.
            effect = drawerContentsListView.DoDragDrop(
                dataObject,
                DragDropEffects.Copy | DragDropEffects.Move);
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError($"Drag-out from '{_drawer.Path}' failed to start.", ex);
            UiMessages.ShowError(
                this,
                "Drag Failed",
                $"Failed to drag the selected items: {ex.Message}");
            return;
        }

        DrawerLogger.LogInformation($"Drag-out from '{_drawer.Path}' finished with effect {effect}.");

        // A copy leaves this folder untouched; only a move can have removed entries from it.
        if ((effect & DragDropEffects.Move) != 0 && !IsDisposed && !Disposing)
        {
            _ = RefreshDrawerAsync();
        }
    }

    /// <summary>
    /// Returns the paths to publish for a drag. When the dragged entry belongs to a multi-selection
    /// the whole selection is dragged (Explorer behaviour); otherwise only the dragged entry is.
    /// Entries without a file-system path are ignored and duplicate paths are removed.
    /// </summary>
    internal static IReadOnlyList<string> CollectDragOutPaths(
        ListViewItem? draggedItem,
        IReadOnlyList<ListViewItem> selectedItems)
    {
        IReadOnlyList<ListViewItem> source = draggedItem is not null &&
                                            draggedItem.Selected &&
                                            selectedItems.Count > 1
            ? selectedItems
            : draggedItem is null
                ? Array.Empty<ListViewItem>()
                : [draggedItem];

        var paths = new List<string>(source.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in source)
        {
            if (item.Tag is not DrawerItemInfo info ||
                string.IsNullOrWhiteSpace(info.Path) ||
                !seen.Add(info.Path))
            {
                continue;
            }

            paths.Add(info.Path);
        }

        return paths;
    }
}
