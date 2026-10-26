using System.Globalization;

namespace winDrawer;

/// <summary>
/// Owns the images that back the drawer list's <see cref="ImageList"/>: request generations,
/// cancellation, the placeholder and disposal. The window decides which indices are visible and
/// applies loaded images to its controls.
/// </summary>
internal sealed class DrawerItemImageCache : IDisposable
{
    public const string PlaceholderKey = "__placeholder__";

    private const int PrefetchScreenCount = 1;

    private readonly Dictionary<int, Image> _loadedImagesByIndex = [];
    private readonly HashSet<int> _requestIndices = [];

    // Serializes the background batches. It is intentionally never disposed: an in-flight batch
    // releases it after the form was disposed, and a SemaphoreSlim without a wait handle owns no
    // unmanaged resources.
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

    private Image? _placeholderImage;
    private CancellationTokenSource? _workCancellation;
    private int _requestGeneration;

    /// <summary>Creates the placeholder on first use and keeps it in the supplied image list.</summary>
    public void EnsurePlaceholder(ImageList imageList)
    {
        ArgumentNullException.ThrowIfNull(imageList);
        if (_placeholderImage is not null && imageList.Images.ContainsKey(PlaceholderKey))
        {
            return;
        }

        _placeholderImage?.Dispose();
        if (imageList.Images.ContainsKey(PlaceholderKey))
        {
            imageList.Images.RemoveByKey(PlaceholderKey);
        }

        _placeholderImage = new Bitmap(imageList.ImageSize.Width, imageList.ImageSize.Height);
        imageList.Images.Add(PlaceholderKey, _placeholderImage);
    }

    /// <summary>
    /// Marks the visible range plus one prefetch screen as requested and returns the items that do
    /// not have an image yet.
    /// </summary>
    public IReadOnlyList<PendingImageRequest> CollectRequests(
        int firstVisible,
        int lastVisible,
        int itemCount,
        Func<int, DrawerItemInfo?> itemAt)
    {
        ArgumentNullException.ThrowIfNull(itemAt);
        _requestIndices.Clear();
        var requests = new List<PendingImageRequest>();

        if (itemCount <= 0 || firstVisible < 0 || lastVisible < firstVisible)
        {
            return requests;
        }

        var visibleCount = lastVisible - firstVisible + 1;
        var prefetchMargin = visibleCount * PrefetchScreenCount;
        var firstRequest = Math.Max(0, firstVisible - prefetchMargin);
        var lastRequest = Math.Min(itemCount - 1, lastVisible + prefetchMargin);

        for (var index = firstRequest; index <= lastRequest; index++)
        {
            _requestIndices.Add(index);
            if (_loadedImagesByIndex.ContainsKey(index))
            {
                continue;
            }

            if (itemAt(index) is { } item)
            {
                requests.Add(new PendingImageRequest(index, item));
            }
        }

        return requests;
    }

    /// <summary>Loaded images that are outside the current prefetch range.</summary>
    public IReadOnlyList<int> GetIndicesToRelease()
    {
        List<int>? indices = null;
        foreach (var index in _loadedImagesByIndex.Keys)
        {
            if (!_requestIndices.Contains(index))
            {
                (indices ??= []).Add(index);
            }
        }

        return indices ?? [];
    }

    public bool RemoveImage(int index, out Image? image)
    {
        return _loadedImagesByIndex.Remove(index, out image);
    }

    public void StoreImage(int index, Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _loadedImagesByIndex[index] = image;
    }

    /// <summary>
    /// Loads the requested images one batch at a time. A batch stops as soon as a newer request
    /// generation, another cancellation source or a released index invalidates it.
    /// </summary>
    public async Task LoadAsync(
        IReadOnlyList<PendingImageRequest> requests,
        Size imageSize,
        Func<int, bool> canApply,
        Action<int, Image> applyImage)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(canApply);
        ArgumentNullException.ThrowIfNull(applyImage);

        var cancellation = _workCancellation;
        if (cancellation is null || cancellation.IsCancellationRequested)
        {
            return;
        }

        var generation = _requestGeneration;
        await _loadSemaphore.WaitAsync().ConfigureAwait(true);
        try
        {
            foreach (var request in requests)
            {
                if (!IsCurrent(request.Index, generation, cancellation, canApply))
                {
                    return;
                }

                // A queued batch can still contain an index that an earlier batch already filled.
                if (_loadedImagesByIndex.ContainsKey(request.Index))
                {
                    continue;
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

                if (!IsCurrent(request.Index, generation, cancellation, canApply))
                {
                    image.Dispose();
                    return;
                }

                try
                {
                    applyImage(request.Index, image);
                }
                catch
                {
                    image.Dispose();
                    throw;
                }
            }
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    public void Invalidate()
    {
        _requestGeneration++;
        _requestIndices.Clear();
    }

    /// <summary>Starts a new work session without disposing the previous cancellation source.</summary>
    public void Restart()
    {
        _workCancellation?.Cancel();
        _workCancellation = new CancellationTokenSource();
        Invalidate();
    }

    public void Cancel()
    {
        _workCancellation?.Cancel();
        Invalidate();
    }

    /// <summary>Disposes every loaded image and the placeholder.</summary>
    public void Clear()
    {
        Invalidate();

        foreach (var image in _loadedImagesByIndex.Values)
        {
            image.Dispose();
        }

        _loadedImagesByIndex.Clear();

        var placeholder = _placeholderImage;
        _placeholderImage = null;
        placeholder?.Dispose();
    }

    public void Dispose()
    {
        _workCancellation?.Cancel();
        Clear();
    }

    public static string GetImageKey(int index)
    {
        return index.ToString(CultureInfo.InvariantCulture);
    }

    private bool IsCurrent(
        int index,
        int generation,
        CancellationTokenSource cancellation,
        Func<int, bool> canApply)
    {
        return generation == _requestGeneration &&
               ReferenceEquals(_workCancellation, cancellation) &&
               !cancellation.IsCancellationRequested &&
               _requestIndices.Contains(index) &&
               canApply(index);
    }

    internal readonly record struct PendingImageRequest(int Index, DrawerItemInfo Item);
}

