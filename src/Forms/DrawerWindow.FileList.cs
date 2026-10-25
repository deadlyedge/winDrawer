using System.Globalization;

namespace winDrawer;

/// <summary>
/// File list orchestration for <see cref="DrawerWindow"/>: metadata refresh, viewport-driven image
/// loading and ownership of the images that are added to the drawer image list.
/// </summary>
public partial class DrawerWindow
{
    private const string PlaceholderImageKey = "__placeholder__";
    private const int ImagePrefetchScreenCount = 1;
    private const int ViewportDebounceIntervalMilliseconds = 80;

    private readonly Dictionary<int, Image> _loadedImagesByIndex = [];
    private readonly HashSet<int> _imageRequestIndices = [];

    // Serializes the background batches. It is intentionally never disposed: an in-flight batch
    // releases it after the form was disposed, and a SemaphoreSlim without a wait handle owns no
    // unmanaged resources.
    private readonly SemaphoreSlim _imageLoadSemaphore = new(1, 1);

    private Image? _placeholderImage;
    private System.Windows.Forms.Timer? _viewportDebounceTimer;
    private CancellationTokenSource? _fileListLoadCancellation;
    private CancellationTokenSource? _imageWorkCancellation;
    private int _fileListLoadGeneration;
    private int _imageRequestGeneration;

    private readonly record struct PendingImageRequest(int Index, DrawerItemInfo Item);

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

    /// <summary>User-requested refresh from the context menu or the F5 shortcut.</summary>
    private Task RefreshDrawerAsync()
    {
        return PopulateFileListAsync();
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
        var scale = Math.Max(1, dpi) / 96f;
        drawerContentsScrollBar.Width = Math.Max(18, (int)Math.Round(18 * scale));
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

        var scrollBarWidth = drawerContentsScrollBar.Width;
        var viewportWidth = Math.Max(1, drawerContentsHostPanel.ClientSize.Width - scrollBarWidth);
        drawerContentsListView.SetBounds(0, 0, drawerContentsHostPanel.ClientSize.Width, drawerContentsHostPanel.ClientSize.Height);
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

        var largeChange = Math.Max(1, viewportSize);
        drawerContentsScrollBar.LargeChange = largeChange;
        drawerContentsScrollBar.Maximum = Math.Max(largeChange, maximumPosition + largeChange - 1);
        drawerContentsScrollBar.Value = Math.Clamp(
            position,
            0,
            drawerContentsScrollBar.Maximum - drawerContentsScrollBar.LargeChange + 1);
        drawerContentsScrollBar.Visible = true;
        PositionDrawerScrollBar();
        drawerContentsScrollBar.BringToFront();
    }

    private static Size GetImageSize(int dpi)
    {
        const int logicalImageSize = 48;
        var scaledSize = Math.Max(1, (int)Math.Round(logicalImageSize * dpi / 96d));
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
        RestartImageWork();

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

        _imageWorkCancellation?.Cancel();
        InvalidateImageRequests();
    }

    private void ApplyDrawerItems(IReadOnlyList<DrawerItemInfo> items)
    {
        drawerContentsListView.BeginUpdate();
        try
        {
            ClearFileList();
            EnsurePlaceholderImage();
            foreach (var item in items)
            {
                var listViewItem = new ListViewItem(item.Name, PlaceholderImageKey)
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
        InvalidateImageRequests();

        drawerContentsListView.BeginUpdate();
        try
        {
            drawerContentsListView.Items.Clear();
            drawerImageList.Images.Clear();
            DisposeFileListImages();
        }
        finally
        {
            drawerContentsListView.EndUpdate();
        }

        SynchronizeDrawerScrollBar();
    }

    private void ShowListStatus(string text, string? toolTip = null)
    {
        InvalidateImageRequests();

        drawerContentsListView.Items.Clear();
        var statusItem = new ListViewItem(text)
        {
            ToolTipText = toolTip ?? text
        };

        drawerContentsListView.Items.Add(statusItem);
        SynchronizeDrawerScrollBar();
    }

    private void EnsurePlaceholderImage()
    {
        if (_placeholderImage is not null && drawerImageList.Images.ContainsKey(PlaceholderImageKey))
        {
            return;
        }

        _placeholderImage?.Dispose();
        _placeholderImage = new Bitmap(drawerImageList.ImageSize.Width, drawerImageList.ImageSize.Height);
        drawerImageList.Images.Add(PlaceholderImageKey, _placeholderImage);
    }

    /// <summary>
    /// Releases the images owned by the file list. The designer dispose path calls this after the
    /// list view items and the image list were cleared.
    /// </summary>
    private void DisposeFileListImages()
    {
        foreach (var image in _loadedImagesByIndex.Values)
        {
            image.Dispose();
        }

        _loadedImagesByIndex.Clear();
        _imageRequestIndices.Clear();

        var placeholder = _placeholderImage;
        _placeholderImage = null;
        placeholder?.Dispose();
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
    /// Loads the images of the visible items plus a one-screen prefetch margin. Items that left the
    /// prefetch margin release their images again.
    /// </summary>
    private async Task RefreshViewportImagesAsync()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        var requests = CollectPendingImageRequests();
        ReleaseImagesOutsidePrefetchMargin();

        if (requests.Count == 0)
        {
            return;
        }

        var cancellation = _imageWorkCancellation;
        if (cancellation == null || cancellation.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await LoadItemImagesAsync(requests, drawerImageList.ImageSize, _imageRequestGeneration, cancellation);
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError($"Failed to load drawer list images for '{_drawer.Name}'.", ex);
        }
    }

    private List<PendingImageRequest> CollectPendingImageRequests()
    {
        var requests = new List<PendingImageRequest>();
        _imageRequestIndices.Clear();

        if (!drawerContentsListView.TryGetVisibleItemRange(out var firstVisible, out var lastVisible))
        {
            return requests;
        }

        var visibleCount = lastVisible - firstVisible + 1;
        var prefetchMargin = visibleCount * ImagePrefetchScreenCount;
        var firstRequest = Math.Max(0, firstVisible - prefetchMargin);
        var lastRequest = Math.Min(drawerContentsListView.Items.Count - 1, lastVisible + prefetchMargin);

        for (var index = firstRequest; index <= lastRequest; index++)
        {
            _imageRequestIndices.Add(index);
            if (_loadedImagesByIndex.ContainsKey(index))
            {
                continue;
            }

            if (drawerContentsListView.Items[index].Tag is DrawerItemInfo item)
            {
                requests.Add(new PendingImageRequest(index, item));
            }
        }

        return requests;
    }

    private void ReleaseImagesOutsidePrefetchMargin()
    {
        List<int>? indicesToRelease = null;
        foreach (var index in _loadedImagesByIndex.Keys)
        {
            if (!_imageRequestIndices.Contains(index))
            {
                (indicesToRelease ??= []).Add(index);
            }
        }

        if (indicesToRelease == null)
        {
            return;
        }

        foreach (var index in indicesToRelease)
        {
            ReleaseLoadedImage(index);
        }
    }

    private void ReleaseLoadedImage(int index)
    {
        if (!_loadedImagesByIndex.Remove(index, out var image))
        {
            return;
        }

        // Rebind the item to the placeholder before the image list drops the loaded image, so the
        // control never draws an image that is about to be disposed.
        if (index >= 0 && index < drawerContentsListView.Items.Count)
        {
            drawerContentsListView.Items[index].ImageKey = PlaceholderImageKey;
        }

        drawerImageList.Images.RemoveByKey(GetImageKey(index));
        image.Dispose();
    }

    private async Task LoadItemImagesAsync(
        IReadOnlyList<PendingImageRequest> requests,
        Size imageSize,
        int generation,
        CancellationTokenSource cancellation)
    {
        // Only one batch may run at a time. A queued batch re-checks its generation when it gets its
        // turn and exits as soon as a newer viewport replaced it.
        await _imageLoadSemaphore.WaitAsync().ConfigureAwait(true);
        try
        {
            foreach (var request in requests)
            {
                if (!IsImageRequestCurrent(request.Index, generation, cancellation))
                {
                    return;
                }

                Image image;
                try
                {
                    image = await Task.Run(
                        () => DrawerItemImageLoader.Load(request.Item, imageSize, cancellation.Token),
                        cancellation.Token).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    DrawerLogger.LogError($"Failed to load the list image for '{request.Item.Path}'.", ex);
                    continue;
                }

                if (!IsImageRequestCurrent(request.Index, generation, cancellation))
                {
                    image.Dispose();
                    return;
                }

                ApplyLoadedImage(request.Index, image);
            }
        }
        finally
        {
            _imageLoadSemaphore.Release();
        }
    }

    private bool IsImageRequestCurrent(int index, int generation, CancellationTokenSource cancellation)
    {
        return !IsDisposed && !Disposing &&
               generation == _imageRequestGeneration &&
               ReferenceEquals(_imageWorkCancellation, cancellation) &&
               !cancellation.IsCancellationRequested &&
               index >= 0 && index < drawerContentsListView.Items.Count &&
               _imageRequestIndices.Contains(index);
    }

    private void ApplyLoadedImage(int index, Image image)
    {
        var key = GetImageKey(index);
        try
        {
            drawerImageList.Images.Add(key, image);
            _loadedImagesByIndex[index] = image;
        }
        catch
        {
            image.Dispose();
            throw;
        }

        drawerContentsListView.Items[index].ImageKey = key;
    }

    private static string GetImageKey(int index)
    {
        return index.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Discards every pending and in-flight image request without allocating a new cancellation
    /// source. Scroll changes, list resets and DPI changes use this cheap invalidation.
    /// </summary>
    private void InvalidateImageRequests()
    {
        _imageRequestGeneration++;
        _imageRequestIndices.Clear();
    }

    /// <summary>
    /// Starts a new image work session. The previous cancellation source is cancelled but not
    /// disposed, because an in-flight worker may still observe its token.
    /// </summary>
    private void RestartImageWork()
    {
        _imageWorkCancellation?.Cancel();
        _imageWorkCancellation = new CancellationTokenSource();
        InvalidateImageRequests();
    }
}
