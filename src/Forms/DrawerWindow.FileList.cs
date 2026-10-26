namespace winDrawer;

/// <summary>
/// File list orchestration for <see cref="DrawerWindow"/>: metadata refresh, viewport-driven image
/// requests and binding the images produced by <see cref="DrawerItemImageCache"/>.
/// </summary>
public partial class DrawerWindow
{
    private const int ViewportDebounceIntervalMilliseconds = 80;

    private readonly DrawerItemImageCache _imageCache = new();
    private System.Windows.Forms.Timer? _viewportDebounceTimer;
    private CancellationTokenSource? _fileListLoadCancellation;
    private int _fileListLoadGeneration;
    private bool _synchronizingScrollBar;

    private void InitializeFileList()
    {
        drawerImageList.ImageSize = GetImageSize(DeviceDpi);
        drawerContentsListView.LargeImageList = drawerImageList;
        drawerContentsListView.ViewportChanged += DrawerContentsListView_ViewportChanged;
        drawerContentsScrollBar.ValueChanged += DrawerContentsScrollBar_ValueChanged;
        drawerContentsHostPanel.Resize += DrawerContentsHostPanel_Resize;
        DpiChanged += DrawerWindow_DpiChanged;

        _viewportDebounceTimer = new System.Windows.Forms.Timer { Interval = ViewportDebounceIntervalMilliseconds };
        _viewportDebounceTimer.Tick += ViewportDebounceTimer_Tick;

        UpdateDrawerScrollBarDpi(DeviceDpi);
        Disposed += (_, _) => ReleaseFileListResources();
        ShowListStatus("Loading...");
    }

    private void DrawerWindow_DpiChanged(object? sender, DpiChangedEventArgs e)
    {
        UpdateStandardFolderIcon(e.DeviceDpiNew);
        UpdateTopResizeGrip(e.DeviceDpiNew);
        UpdateFooterLayout(e.DeviceDpiNew);
        RefreshFileListForDpi(e.DeviceDpiNew);
    }

    /// <summary>
    /// Recreates the image list and reloads the folder contents after the window moved to a display
    /// with a different DPI scale.
    /// </summary>
    private void RefreshFileListForDpi(int dpi)
    {
        CancelFileListWork();
        ClearFileList();
        drawerImageList.ImageSize = GetImageSize(dpi);
        _ = PopulateFileListAsync();
    }

    private void UpdateDrawerScrollBarDpi(int dpi)
    {
        var scale = DpiScale.Factor(dpi);
        drawerContentsScrollBar.RestFillWidth = Math.Max(2, DpiScale.Pixels(3, scale));
        drawerContentsScrollBar.Width = Math.Max(17, DpiScale.Pixels(18, scale));
        drawerContentsScrollBar.AccessibleName = "Scroll folder contents";
        drawerContentsScrollBar.AccessibleRole = AccessibleRole.ScrollBar;
        PositionDrawerScrollBar();
    }

    private void PositionDrawerScrollBar()
    {
        if (!drawerContentsListView.IsHandleCreated)
        {
            return;
        }

        var scale = DpiScale.Factor(DeviceDpi);
        var nativeScrollBarWidth = Math.Max(17, DpiScale.Pixels(17, scale));
        var scrollBarWidth = drawerContentsScrollBar.Width;
        var viewportWidth = Math.Max(1, drawerContentsHostPanel.ClientSize.Width - scrollBarWidth);
        var listWidth = drawerContentsHostPanel.ClientSize.Width + nativeScrollBarWidth;

        // The list is wider than its host on purpose: the native vertical scrollbar is laid out
        // outside the host's client area and clipped away. The themed scrollbar below covers the
        // right edge, while the ListView keeps native wheel/scrollbar behavior internally.
        drawerContentsListView.SetBounds(0, 0, listWidth, drawerContentsHostPanel.ClientSize.Height);
        drawerContentsScrollBar.SetBounds(viewportWidth, 0, scrollBarWidth, drawerContentsHostPanel.ClientSize.Height);
    }

    private void DrawerContentsListView_ViewportChanged(object? sender, EventArgs e)
    {
        SynchronizeDrawerScrollBar();
        RequestViewportImages();
    }

    private void DrawerContentsHostPanel_Resize(object? sender, EventArgs e)
    {
        PositionDrawerScrollBar();
        SynchronizeDrawerScrollBar();
        RequestViewportImages();
    }

    private void DrawerContentsScrollBar_ValueChanged(object? sender, EventArgs e)
    {
        if (_synchronizingScrollBar)
        {
            return;
        }

        drawerContentsListView.ScrollToVerticalPosition(drawerContentsScrollBar.Value);
    }

    private void SynchronizeDrawerScrollBar()
    {
        if (IsDisposed || Disposing || !drawerContentsListView.IsHandleCreated)
        {
            return;
        }

        if (!drawerContentsListView.TryGetVerticalScrollInfo(out var maximumPosition, out var position, out var viewportSize) ||
            maximumPosition <= 0)
        {
            drawerContentsScrollBar.Visible = false;
            PositionDrawerScrollBar();
            return;
        }

        _synchronizingScrollBar = true;
        try
        {
            drawerContentsScrollBar.LargeChange = Math.Max(1, viewportSize);
            drawerContentsScrollBar.Maximum = maximumPosition;
            drawerContentsScrollBar.Value = position;
            drawerContentsScrollBar.Visible = true;
            PositionDrawerScrollBar();
            drawerContentsScrollBar.BringToFront();
        }
        finally
        {
            _synchronizingScrollBar = false;
        }
    }

    private static Size GetImageSize(int dpi)
    {
        const int logicalImageSize = 48;
        var scaledSize = DpiScale.Pixels(logicalImageSize, dpi);
        return new Size(scaledSize, scaledSize);
    }

    private async Task PopulateFileListAsync()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        var previousCancellation = _fileListLoadCancellation;
        var cancellation = new CancellationTokenSource();
        _fileListLoadCancellation = cancellation;
        var generation = ++_fileListLoadGeneration;
        previousCancellation?.Cancel();

        // List images always belong to the contents that were visible when they were requested.
        _imageCache.Restart();

        DrawerLogger.LogInformation($"Refreshing drawer contents for '{_drawer.Name}'.");
        ClearFileList();
        fullPathLabel.Text = _drawer.Path;
        ShowListStatus("Loading...");

        try
        {
            var folderPath = _drawer.Path;
            var sortMode = _drawer.SortMode;
            var maxItems = _settingsRepository.GetSnapshot().MaxDrawerItems;
            var result = await Task.Run(() => DrawerItemEnumerator.Enumerate(folderPath, sortMode, maxItems, cancellation.Token));

            if (!IsCurrentFileListLoad(generation, cancellation))
            {
                return;
            }

            if (result.Outcome == DrawerEnumerationOutcome.FolderMissing)
            {
                fullPathLabel.Text = $"{folderPath} (missing)";
                ShowListStatus("Folder not found or inaccessible");
                DrawerLogger.LogWarning($"Drawer path missing for '{_drawer.Name}': {folderPath}");
                return;
            }

            if (result.Outcome == DrawerEnumerationOutcome.TooManyItems)
            {
                ShowListStatus(
                    $"Too many items (more than {maxItems}). Open the folder in File Explorer.",
                    "Use the folder path below to open it in File Explorer.");
                DrawerLogger.LogInformation(
                    $"Drawer '{_drawer.Name}' has more than {maxItems} items; its contents were not displayed.");
                return;
            }

            var items = result.Items;
            ApplyDrawerItems(items);

            var directoryCount = items.Count(item => item.IsDirectory);
            DrawerLogger.LogInformation(
                $"Drawer '{_drawer.Name}' loaded {directoryCount} directories and {items.Count - directoryCount} files.");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            DrawerLogger.LogDebug($"Discarded an outdated load for drawer '{_drawer.Name}'.");
        }
        catch (Exception ex)
        {
            if (!IsCurrentFileListLoad(generation, cancellation))
            {
                return;
            }

            DrawerLogger.LogError($"Failed to populate drawer '{_drawer.Name}'.", ex);
            ClearFileList();
            ShowListStatus("Failed to load", ex.Message);
            UiMessages.ShowError(this, "Drawer Error", $"Failed to load drawer contents: {ex.Message}");
        }
        finally
        {
            if (ReferenceEquals(_fileListLoadCancellation, cancellation))
            {
                _fileListLoadCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private bool IsCurrentFileListLoad(int generation, CancellationTokenSource cancellation)
    {
        return !IsDisposed && !Disposing &&
               generation == _fileListLoadGeneration &&
               ReferenceEquals(_fileListLoadCancellation, cancellation) &&
               !cancellation.IsCancellationRequested;
    }

    /// <summary>
    /// Cancels the pending metadata and image work, for example when the window closes. A window
    /// close that the user cancels must not reach this method.
    /// </summary>
    private void CancelFileListWork()
    {
        _fileListLoadGeneration++;
        var metadataCancellation = _fileListLoadCancellation;
        _fileListLoadCancellation = null;
        metadataCancellation?.Cancel();

        _imageCache.Cancel();
    }

    private void ApplyDrawerItems(IReadOnlyList<DrawerItemInfo> items)
    {
        drawerContentsListView.BeginUpdate();
        try
        {
            ClearFileList();
            _imageCache.EnsurePlaceholder(drawerImageList);
            foreach (var item in items)
            {
                var listViewItem = new ListViewItem(item.Name, DrawerItemImageCache.PlaceholderKey)
                {
                    ToolTipText = item.Path,
                    Tag = item
                };

                drawerContentsListView.Items.Add(listViewItem);
            }

            if (items.Count == 0)
            {
                ShowListStatus("Folder is empty");
            }
        }
        catch
        {
            ClearFileList();
            throw;
        }
        finally
        {
            drawerContentsListView.EndUpdate();
        }

        SynchronizeDrawerScrollBar();

        // The viewport changed while the items were added; load the first screen without a delay.
        RequestViewportImages(immediate: true);
    }

    private void ClearFileList()
    {
        drawerContentsListView.BeginUpdate();
        try
        {
            drawerContentsListView.Items.Clear();
            drawerImageList.Images.Clear();
            _imageCache.Clear();
        }
        finally
        {
            drawerContentsListView.EndUpdate();
        }

        SynchronizeDrawerScrollBar();
    }

    private void ShowListStatus(string text, string? toolTip = null)
    {
        _imageCache.Invalidate();

        drawerContentsListView.Items.Clear();
        var statusItem = new ListViewItem(text)
        {
            ToolTipText = toolTip ?? text
        };

        drawerContentsListView.Items.Add(statusItem);
        SynchronizeDrawerScrollBar();
    }

    private void ReleaseFileListResources()
    {
        CancelFileListWork();

        var timer = _viewportDebounceTimer;
        _viewportDebounceTimer = null;
        if (timer != null)
        {
            timer.Tick -= ViewportDebounceTimer_Tick;
            timer.Dispose();
        }

        _imageCache.Dispose();
    }

    private void RequestViewportImages(bool immediate = false)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        var timer = _viewportDebounceTimer;
        timer?.Stop();

        if (immediate)
        {
            _ = RefreshViewportImagesAsync();
        }

        // The debounced pass also covers layout races: the first pass can run before the list view
        // finished arranging its items or after a DPI change invalidated the image size.
        timer?.Start();
    }

    private void ViewportDebounceTimer_Tick(object? sender, EventArgs e)
    {
        _viewportDebounceTimer?.Stop();
        _ = RefreshViewportImagesAsync();
    }

    /// <summary>
    /// Requests the images of the visible items plus a one-screen prefetch margin. Items that left
    /// the prefetch margin release their images again.
    /// </summary>
    private async Task RefreshViewportImagesAsync()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        var requests = CollectPendingImageRequests();
        foreach (var index in _imageCache.GetIndicesToRelease())
        {
            ReleaseLoadedImage(index);
        }

        if (requests.Count == 0)
        {
            return;
        }

        try
        {
            await _imageCache.LoadAsync(
                requests,
                drawerImageList.ImageSize,
                CanApplyImage,
                ApplyLoadedImage);
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError($"Failed to load drawer list images for '{_drawer.Name}'.", ex);
        }
    }

    private IReadOnlyList<DrawerItemImageCache.PendingImageRequest> CollectPendingImageRequests()
    {
        if (!drawerContentsListView.TryGetVisibleItemRange(out var firstVisible, out var lastVisible))
        {
            return [];
        }

        return _imageCache.CollectRequests(
            firstVisible,
            lastVisible,
            drawerContentsListView.Items.Count,
            index => drawerContentsListView.Items[index].Tag as DrawerItemInfo);
    }

    private bool CanApplyImage(int index)
    {
        return !IsDisposed && !Disposing &&
               index >= 0 && index < drawerContentsListView.Items.Count &&
               drawerContentsListView.Items[index].Tag is DrawerItemInfo;
    }

    private void ReleaseLoadedImage(int index)
    {
        if (!_imageCache.RemoveImage(index, out var image))
        {
            return;
        }

        // Rebind the item to the placeholder before the image list drops the loaded image, so the
        // control never draws an image that is about to be disposed.
        if (index >= 0 && index < drawerContentsListView.Items.Count)
        {
            drawerContentsListView.Items[index].ImageKey = DrawerItemImageCache.PlaceholderKey;
        }

        drawerImageList.Images.RemoveByKey(DrawerItemImageCache.GetImageKey(index));
        image?.Dispose();
    }

    private void ApplyLoadedImage(int index, Image image)
    {
        var key = DrawerItemImageCache.GetImageKey(index);
        _imageCache.StoreImage(index, image);

        try
        {
            drawerImageList.Images.Add(key, image);
        }
        catch
        {
            _imageCache.RemoveImage(index, out _);
            throw;
        }

        if (index >= 0 && index < drawerContentsListView.Items.Count)
        {
            drawerContentsListView.Items[index].ImageKey = key;
        }
    }
}
