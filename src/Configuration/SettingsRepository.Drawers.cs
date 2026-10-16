using Tomlyn.Model;

namespace winDrawer.Configuration;

internal sealed partial class SettingsRepository
{
    public bool UpdateDrawerSize(string drawerPath, Size size)
    {
        if (string.IsNullOrWhiteSpace(drawerPath))
        {
            DrawerLogger.LogWarning("Attempted to persist drawer size with an empty path.");
            return false;
        }

        lock (_writeLock)
        {
            if (!TryGetDrawers(_root, out var drawers))
            {
                DrawerLogger.LogWarning($"Drawer collection missing when updating size for '{drawerPath}'.");
                return false;
            }

            foreach (var entry in drawers)
            {
                var candidatePath = ReadString(entry, "path");
                if (!PathsEqual(candidatePath, drawerPath))
                {
                    continue;
                }

                entry["size"] = new TomlArray { size.Width, size.Height };

                _dirty = true;
                _writeSignal.Release();
                DrawerLogger.LogInformation($"Queued drawer size update for '{drawerPath}' => {size}.");

                return true;
            }
        }

        DrawerLogger.LogWarning($"Unable to find drawer entry when updating size for '{drawerPath}'.");
        return false;
    }

    public bool UpdateDrawer(string originalPath, string name, string newPath, Color? overlayColor, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(originalPath) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(newPath))
        {
            error = "Drawer name and folder path are required.";
            return false;
        }

        var expandedOriginalPath = Environment.ExpandEnvironmentVariables(originalPath).Trim();
        var expandedPath = Environment.ExpandEnvironmentVariables(newPath).Trim();
        if (!PathsEqual(expandedOriginalPath, expandedPath) && !Directory.Exists(expandedPath))
        {
            error = "The selected folder does not exist or is inaccessible.";
            return false;
        }

        var normalizedColor = overlayColor is { } color
            ? Color.FromArgb(179, color.R, color.G, color.B)
            : (Color?)null;

        lock (_writeLock)
        {
            if (!TryGetDrawers(_root, out var drawers))
            {
                error = "The drawer configuration is unavailable.";
                return false;
            }

            var matches = drawers.Where(entry => PathsEqual(ReadString(entry, "path"), originalPath)).ToList();
            if (matches.Count != 1)
            {
                error = matches.Count == 0
                    ? "The drawer could not be found in settings."
                    : "The drawer path is duplicated in settings; edit was cancelled to avoid changing the wrong entry.";
                return false;
            }

            if (drawers.Any(entry => !ReferenceEquals(entry, matches[0]) && PathsEqual(ReadString(entry, "path"), expandedPath)))
            {
                error = "Another drawer already uses the selected folder.";
                return false;
            }

            matches[0]["name"] = name.Trim();
            matches[0]["path"] = expandedPath;
            if (normalizedColor is { } drawerColor)
            {
                matches[0]["overlay_color"] = FormatOverlayColor(drawerColor);
            }
            else
            {
                matches[0].Remove("overlay_color");
            }

            _dirty = true;
        }

        SignalSettingsWrite("drawer update");
        return true;
    }

    public bool DeleteDrawer(string drawerPath, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(drawerPath))
        {
            error = "Drawer path is required.";
            return false;
        }

        lock (_writeLock)
        {
            if (!TryGetDrawers(_root, out var drawers))
            {
                error = "The drawer configuration is unavailable.";
                return false;
            }

            var matches = drawers.Where(entry => PathsEqual(ReadString(entry, "path"), drawerPath)).ToList();
            if (matches.Count != 1)
            {
                error = matches.Count == 0
                    ? "The drawer could not be found in settings."
                    : "The drawer path is duplicated in settings; deletion was cancelled to avoid removing the wrong entry.";
                return false;
            }

            if (!_root.TryGetValue("drawers", out var value) || !RemoveDrawerEntry(value, matches[0]))
            {
                error = "The drawer entry could not be removed from settings.";
                return false;
            }

            _dirty = true;
        }

        SignalSettingsWrite("drawer deletion");
        return true;
    }

    public bool ReorderDrawers(IReadOnlyList<string> orderedPaths, out string error)
    {
        error = string.Empty;
        ArgumentNullException.ThrowIfNull(orderedPaths);

        lock (_writeLock)
        {
            if (!TryGetDrawers(_root, out var drawers) || !_root.TryGetValue("drawers", out var value))
            {
                error = "The drawer configuration is unavailable.";
                return false;
            }

            var rawCount = value is TomlTableArray existingTableArray ? existingTableArray.Count : -1;
            if (rawCount != drawers.Count || orderedPaths.Count != drawers.Count)
            {
                error = $"The drawer list contains invalid entries (raw={rawCount}, valid={drawers.Count}, requested={orderedPaths.Count}); reordering was cancelled to preserve the configuration.";
                return false;
            }

            var byPath = new Dictionary<string, TomlTable>(StringComparer.OrdinalIgnoreCase);
            foreach (var drawer in drawers)
            {
                var path = Environment.ExpandEnvironmentVariables(ReadString(drawer, "path")).Trim();
                if (string.IsNullOrWhiteSpace(path) || !byPath.TryAdd(path, drawer))
                {
                    error = "Drawer paths are missing or duplicated in settings; reordering was cancelled.";
                    return false;
                }
            }

            var reordered = new List<TomlTable>(orderedPaths.Count);
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var requestedPath in orderedPaths)
            {
                var path = Environment.ExpandEnvironmentVariables(requestedPath ?? string.Empty).Trim();
                if (!seenPaths.Add(path) || !byPath.TryGetValue(path, out var entry))
                {
                    error = "The requested order does not match the configured drawers.";
                    return false;
                }

                reordered.Add(entry);
            }

            if (value is not TomlTableArray tableArray)
            {
                error = "The drawer configuration format is unsupported.";
                return false;
            }

            tableArray.Clear();
            foreach (var entry in reordered)
            {
                tableArray.Add(entry);
            }

            _dirty = true;
        }

        SignalSettingsWrite("drawer order update");
        return true;
    }

    private static bool RemoveDrawerEntry(object? collection, TomlTable entry)
    {
        return collection is TomlTableArray tableArray && tableArray.Remove(entry);
    }

    private void SignalSettingsWrite(string reason)
    {
        DrawerLogger.LogInformation($"Queued {reason} for persistence.");
        _writeSignal.Release();
    }

    private static string FormatOverlayColor(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>
    /// Add a new drawer entry if it does not already exist.
    /// Returns true when a new entry was added; false if the path was already present or input invalid.
    /// This method updates the in-memory model and signals the background writer to persist changes.
    /// </summary>
    public bool AddDrawer(string path, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            DrawerLogger.LogWarning("Attempted to add drawer with an empty path.");
            return false;
        }

        var expanded = Environment.ExpandEnvironmentVariables(path).Trim();
        lock (_writeLock)
        {
            var drawers = EnsureDrawersArray(_root);

            // Check for existing entry
            foreach (var entry in drawers)
            {
                var candidatePath = ReadString(entry, "path");
                if (PathsEqual(candidatePath, expanded))
                {
                    DrawerLogger.LogDebug($"Drawer path already tracked: '{expanded}'.");
                    return false;
                }
            }

            var entryTable = new TomlTable();
            var entryName = string.IsNullOrWhiteSpace(name)
                ? Path.GetFileName(expanded.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                : name;

            entryTable["name"] = entryName ?? string.Empty;
            entryTable["path"] = expanded;

            AddDrawerEntry(_root, entryTable);
            DrawerLogger.LogInformation($"Added drawer '{entryName}' => '{expanded}'.");

            _dirty = true;
        }

        DrawerLogger.LogDebug("Signaled background writer after adding drawer.");
        _writeSignal.Release();
        return true;
    }

    private static List<TomlTable> EnsureDrawersArray(TomlTable root)
    {
        if (TryGetDrawers(root, out var drawers))
        {
            return drawers;
        }

        root["drawers"] = new TomlTableArray();
        return [];
    }

    private static bool TryGetDrawers(TomlTable root, out List<TomlTable> drawers)
    {
        drawers = [];
        if (!root.TryGetValue("drawers", out var value))
        {
            return false;
        }

        if (value is not TomlTableArray tableArray)
        {
            return false;
        }

        drawers.AddRange(tableArray);
        return true;
    }

    private static void AddDrawerEntry(TomlTable root, TomlTable drawer)
    {
        if (root.TryGetValue("drawers", out var value) && value is TomlTableArray tableArray)
        {
            tableArray.Add(drawer);
            return;
        }

        root["drawers"] = new TomlTableArray { drawer };
    }
}
