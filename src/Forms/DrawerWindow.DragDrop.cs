using System.Collections.Specialized;
using System.Runtime.InteropServices;

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
    private IDataObject? _dropData;
    private IReadOnlyList<string> _dropPaths = Array.Empty<string>();
    private bool _dropDestinationAvailable;

    private void InitializeDragDrop()
    {
        drawerContentsListView.ItemDrag += DrawerContentsListView_ItemDrag;

        // Only the list itself accepts drops: the folder link and the close button must never become
        // drop targets by accident.
        drawerContentsListView.AllowDrop = true;
        drawerContentsListView.DragEnter += DrawerContentsListView_DragEnter;
        drawerContentsListView.DragOver += DrawerContentsListView_DragOver;
        drawerContentsListView.DragLeave += DrawerContentsListView_DragLeave;
        drawerContentsListView.DragDrop += DrawerContentsListView_DragDrop;
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
            _ = PopulateFileListAsync();
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

    private void DrawerContentsListView_DragEnter(object? sender, DragEventArgs e)
    {
        _dropDestinationAvailable = Directory.Exists(_drawer.Path);
        _dropData = e.Data;
        _dropPaths = ReadDropPaths(e.Data);
        e.Effect = BuildDropPlan(e).AllowedEffect;
    }

    private void DrawerContentsListView_DragOver(object? sender, DragEventArgs e)
    {
        // DragOver fires for every mouse move: re-read the data only when the drag source swapped it
        // so file-system existence checks are not repeated dozens of times per second.
        if (!ReferenceEquals(e.Data, _dropData))
        {
            _dropData = e.Data;
            _dropPaths = ReadDropPaths(e.Data);
        }

        // Re-plan on every move so Ctrl/Shift pressed during the drag are reflected immediately.
        e.Effect = BuildDropPlan(e).AllowedEffect;
    }

    private void DrawerContentsListView_DragLeave(object? sender, EventArgs e)
    {
        ResetDropState();
    }

    private void DrawerContentsListView_DragDrop(object? sender, DragEventArgs e)
    {
        ResetDropState();

        // Do not trust the cached plan: existence and modifier keys may have changed while dragging.
        var paths = ReadDropPaths(e.Data);
        if (paths.Count == 0)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        if (!Directory.Exists(_drawer.Path))
        {
            e.Effect = DragDropEffects.None;
            DrawerLogger.LogWarning($"Drop rejected because drawer folder '{_drawer.Path}' is unavailable.");
            UiMessages.ShowError(
                this,
                "Drop Failed",
                $"The drawer folder '{_drawer.Path}' is not available.");
            return;
        }

        var plan = DrawerDropPlanner.Plan(paths, _drawer.Path, e.AllowedEffect, Control.ModifierKeys);
        if (plan.Rejection == DrawerDropRejection.DestinationInsideSource)
        {
            e.Effect = DragDropEffects.None;
            DrawerLogger.LogWarning($"Drop rejected because '{_drawer.Path}' lies inside a dropped folder.");
            UiMessages.ShowWarning(
                this,
                "Drop Not Allowed",
                "A folder cannot be moved or copied into itself or into one of its subfolders.");
            return;
        }

        if (!plan.HasWork)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        // The drag source only accepts a single effect back, even when the batch mixes move and copy.
        e.Effect = plan.MovePaths.Count > 0 ? DragDropEffects.Move : DragDropEffects.Copy;
        ExecuteDrop(plan);
    }

    private DrawerDropPlan BuildDropPlan(DragEventArgs e)
    {
        if (_dropPaths.Count == 0)
        {
            return DrawerDropPlan.Reject(DrawerDropRejection.NoSources);
        }

        if (!_dropDestinationAvailable)
        {
            return DrawerDropPlan.Reject(DrawerDropRejection.DestinationUnavailable);
        }

        return DrawerDropPlanner.Plan(_dropPaths, _drawer.Path, e.AllowedEffect, Control.ModifierKeys);
    }

    /// <summary>
    /// Returns the real file-system paths of a <c>CF_HDROP</c> payload. Virtual entries (archive
    /// contents, mail attachments) and expired paths are dropped instead of being converted.
    /// </summary>
    private static IReadOnlyList<string> ReadDropPaths(IDataObject? data)
    {
        if (data is null || !data.GetDataPresent(DataFormats.FileDrop))
        {
            return Array.Empty<string>();
        }

        if (data.GetData(DataFormats.FileDrop) is not string[] dropped)
        {
            return Array.Empty<string>();
        }

        var paths = new List<string>(dropped.Length);
        foreach (var path in dropped)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                continue;
            }

            paths.Add(path);
        }

        return paths;
    }

    private void ResetDropState()
    {
        _dropData = null;
        _dropPaths = Array.Empty<string>();
        _dropDestinationAvailable = false;
    }

    /// <summary>
    /// Hands the plan to Windows. <see cref="ShellFileOperation.Perform"/> shows the Shell's own
    /// progress and conflict dialogs and blocks until it is done, exactly like a drop in Explorer.
    /// </summary>
    private void ExecuteDrop(DrawerDropPlan plan)
    {
        DrawerLogger.LogInformation(
            $"Dropping into '{_drawer.Path}': move={plan.MovePaths.Count}, " +
            $"copy={plan.CopyPaths.Count}, modifiers={Control.ModifierKeys}.");

        ShellFileOperationResult result;
        try
        {
            result = ShellFileOperation.Perform(Handle, _drawer.Path, plan.MovePaths, plan.CopyPaths);
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError($"Shell file operation into '{_drawer.Path}' threw.", ex);
            UiMessages.ShowError(
                this,
                "Drop Failed",
                $"The items could not be moved or copied: {ex.Message}");
            return;
        }

        if (result.Canceled)
        {
            DrawerLogger.LogInformation($"Shell file operation into '{_drawer.Path}' was cancelled.");
        }
        else if (!result.Succeeded)
        {
            DrawerLogger.LogError(
                $"Shell file operation into '{_drawer.Path}' failed with HRESULT 0x{result.HResult:X8}.");
            var message = Marshal.GetExceptionForHR(result.HResult)?.Message
                          ?? $"HRESULT 0x{result.HResult:X8}";
            UiMessages.ShowError(
                this,
                "Drop Failed",
                $"The items could not be moved or copied: {message}");
        }

        // Cancelling can still leave a partially completed batch, so always re-read the folder.
        if (!IsDisposed && !Disposing)
        {
            _ = PopulateFileListAsync();
        }
    }
}
