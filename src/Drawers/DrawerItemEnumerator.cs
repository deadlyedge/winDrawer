using System.Security;

namespace winDrawer;

internal enum DrawerEnumerationOutcome
{
    Success,
    FolderMissing,
    TooManyItems
}

internal readonly record struct DrawerEnumerationResult(
    DrawerEnumerationOutcome Outcome,
    IReadOnlyList<DrawerItemInfo> Items);

internal static class DrawerItemEnumerator
{
    /// <summary>
    /// Value that the file system APIs return for a path without a valid timestamp; it means the entry
    /// disappeared or its time could not be read.
    /// </summary>
    private static readonly DateTime MissingTimestamp = DateTime.FromFileTimeUtc(0);

    public static DrawerEnumerationResult Enumerate(
        string folderPath,
        DrawerSortMode sortMode,
        int maxItems,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(folderPath))
        {
            return new(DrawerEnumerationOutcome.FolderMissing, []);
        }

        var limit = Math.Max(1, maxItems);
        var items = new List<DrawerItemInfo>();
        foreach (var path in Directory.EnumerateFileSystemEntries(folderPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Stop before reading metadata for the first entry over the limit: the drawer only needs
            // to know that the folder is too large, not how many entries it has in total.
            if (items.Count >= limit)
            {
                return new(DrawerEnumerationOutcome.TooManyItems, []);
            }

            FileAttributes attributes;
            try
            {
                attributes = File.GetAttributes(path);
            }
            catch (Exception ex) when (IsItemAccessException(ex))
            {
                DrawerLogger.LogDebug($"Skipping drawer item that could not be inspected '{path}'. {ex}");
                continue;
            }

            var isDirectory = (attributes & FileAttributes.Directory) != 0;
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name))
            {
                name = path;
            }

            DateTime? lastWriteTimeUtc;
            try
            {
                var lastWriteTime = isDirectory
                    ? Directory.GetLastWriteTimeUtc(path)
                    : File.GetLastWriteTimeUtc(path);

                // An entry that vanished between enumeration and this call reports the 1601 sentinel
                // instead of throwing; keep it in the list but sort it after items with a real time.
                lastWriteTimeUtc = lastWriteTime == MissingTimestamp ? null : lastWriteTime;
            }
            catch (Exception ex) when (IsItemAccessException(ex))
            {
                lastWriteTimeUtc = null;
                DrawerLogger.LogDebug($"Could not read last-write time for drawer item '{path}'. {ex}");
            }

            items.Add(new DrawerItemInfo(path, name, isDirectory, lastWriteTimeUtc));
        }

        cancellationToken.ThrowIfCancellationRequested();
        items.Sort((left, right) => CompareItems(left, right, sortMode));
        cancellationToken.ThrowIfCancellationRequested();
        return new(DrawerEnumerationOutcome.Success, items);
    }

    /// <summary>
    /// Deterministic order for one sort mode. Missing timestamps sort after readable ones in the time
    /// modes, and every mode ends with the full path so the order never depends on enumeration order.
    /// </summary>
    internal static int CompareItems(DrawerItemInfo left, DrawerItemInfo right, DrawerSortMode sortMode)
    {
        if (sortMode != DrawerSortMode.NameAscending)
        {
            if (left.LastWriteTimeUtc is { } leftTime)
            {
                if (right.LastWriteTimeUtc is not { } rightTime)
                {
                    return -1;
                }

                var timeComparison = sortMode == DrawerSortMode.NewestFirst
                    ? rightTime.CompareTo(leftTime)
                    : leftTime.CompareTo(rightTime);
                if (timeComparison != 0)
                {
                    return timeComparison;
                }
            }
            else if (right.LastWriteTimeUtc is not null)
            {
                return 1;
            }
        }

        var nameComparison = StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name);
        return nameComparison != 0
            ? nameComparison
            : StringComparer.Ordinal.Compare(left.Path, right.Path);
    }

    private static bool IsItemAccessException(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or SecurityException or
            ArgumentException or NotSupportedException;
    }
}