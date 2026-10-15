using System.Globalization;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;
using Tomlyn.Syntax;
using winDrawer.Theming;

namespace winDrawer.Configuration;

internal sealed class SettingsRepository : IDisposable
{
    private readonly string _settingsPath;
    private TomlTable _root;

    // Background writer state
    private readonly SemaphoreSlim _writeSignal = new(0);
    private readonly CancellationTokenSource _writerCts = new();
    private readonly Lock _writeLock = new();
    private readonly Task? _writerTask;
    private bool _dirty;
    private bool _writeInProgress;
    private TaskCompletionSource<bool>? _flushTcs;

    public SettingsRepository(string settingsPath)
    {
        _settingsPath = settingsPath;
        _root = LoadOrCreateTable(settingsPath);
        DrawerLogger.LogInformation($"SettingsRepository initialized with path '{settingsPath}'.");

        // Start background writer loop
        _writerTask = Task.Run(() => BackgroundWriterLoop(_writerCts.Token));
    }

    public static string GetDefaultPath()
    {
        return Path.Combine(AppConstants.GetUserConfigurationDirectory(), AppConstants.Files.SettingsFileName);
    }

    public SettingsSnapshot GetSnapshot()
    {
        lock (_writeLock)
        {
            // Return snapshot from current in-memory model
            var snapshot = SettingsSnapshot.FromModel(_root);
            DrawerLogger.LogDebug("Settings snapshot materialized from repository.");
            return snapshot;
        }
    }

    public void Update(SettingsSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        lock (_writeLock)
        {
            var startup = EnsureTable(_root, "startup");
            startup["start_with_windows"] = snapshot.StartWithWindows;
            startup["window_locked"] = snapshot.WindowLocked;
            startup["icon_size"] = snapshot.IconSize;
            startup["window_height"] = Math.Clamp(snapshot.WindowHeight, AppConstants.Defaults.MainWindowHeightMin, AppConstants.Defaults.MainWindowHeightMax);
            startup["window_position"] = new TomlArray { snapshot.WindowPosition.X, snapshot.WindowPosition.Y };
            startup["window_color_hsla"] = new TomlArray { snapshot.ThemeHue, snapshot.ThemeSaturation, snapshot.ThemeLightness, snapshot.ThemeAlpha };

            // Mark dirty and signal background writer
            _dirty = true;
        }

        DrawerLogger.LogInformation("Settings snapshot updated; background write scheduled.");
        _writeSignal.Release();
    }

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

            var rawCount = value switch
            {
                TomlTableArray tableArray => tableArray.Count,
                TomlArray array => array.Count,
                _ => -1
            };
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

            switch (value)
            {
                case TomlTableArray tableArray:
                    tableArray.Clear();
                    foreach (var entry in reordered)
                    {
                        tableArray.Add(entry);
                    }
                    break;
                case TomlArray array:
                    array.Clear();
                    foreach (var entry in reordered)
                    {
                        array.Add(entry);
                    }
                    break;
                default:
                    error = "The drawer configuration format is unsupported.";
                    return false;
            }

            _dirty = true;
        }

        SignalSettingsWrite("drawer order update");
        return true;
    }

    private static bool RemoveDrawerEntry(object? collection, TomlTable entry)
    {
        return collection switch
        {
            TomlTableArray tableArray => tableArray.Remove(entry),
            TomlArray array => array.Remove(entry),
            _ => false
        };
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

    /// <summary>
    /// Enqueue a save request: does not block. Background writer will persist the in-memory model.
    /// </summary>
    public void EnqueueSave()
    {
        lock (_writeLock)
        {
            _dirty = true;
        }
        DrawerLogger.LogDebug("Manual save enqueued.");
        _writeSignal.Release();
    }

    /// <summary>
    /// Force a flush: waits up to <paramref name="timeout"/> for pending writes to complete.
    /// </summary>
    public async Task<bool> FlushAsync(TimeSpan timeout)
    {
        DrawerLogger.LogInformation($"Flush requested with timeout {timeout}.");
        TaskCompletionSource<bool> tcs;
        lock (_writeLock)
        {
            if (!_dirty && !_writeInProgress)
            {
                DrawerLogger.LogDebug("Flush skipped because repository is not dirty.");
                return true; // nothing to flush
            }

            if (_flushTcs?.Task.IsCompleted == true)
            {
                _flushTcs = null;
            }

            _flushTcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            tcs = _flushTcs;
            if (_dirty)
            {
                _writeSignal.Release();
            }
        }

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout)).ConfigureAwait(false);
        var success = completed == tcs.Task && tcs.Task.IsCompletedSuccessfully;
        lock (_writeLock)
        {
            if (!success && ReferenceEquals(_flushTcs, tcs))
            {
                _flushTcs = null;
            }
        }

        DrawerLogger.LogInformation($"Flush completed. Success={success}.");
        return success;
    }

    /// <summary>
    /// Synchronous save kept for compatibility; will persist immediately on calling thread.
    /// Prefer EnqueueSave or FlushAsync in production code.
    /// </summary>
    public void Save()
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string content;
        lock (_writeLock)
        {
            content = SerializeSettings(_root);
        }

        DrawerLogger.LogInformation("Performing synchronous settings save.");
        File.WriteAllText(_settingsPath, content);
        DrawerLogger.LogInformation("Synchronous settings save completed.");
    }

    public bool SavePendingChanges(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            return false;
        }

        if (_writerTask == null || _writerTask.IsCompleted)
        {
            string content;
            lock (_writeLock)
            {
                content = SerializeSettings(_root);
                _dirty = false;
            }

            try
            {
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_settingsPath, content);
                return true;
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Failed to synchronously persist pending settings.", ex);
                lock (_writeLock)
                {
                    _dirty = true;
                }

                return false;
            }
        }

        try
        {
            return FlushAsync(timeout).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Failed to flush pending settings.", ex);
            return false;
        }
    }

    private async Task BackgroundWriterLoop(CancellationToken token)
    {
        try
        {
            DrawerLogger.LogInformation("Background settings writer loop started.");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _writeSignal.WaitAsync(token);
                }
                catch (OperationCanceledException)
                {
                    DrawerLogger.LogInformation("Background writer cancellation requested.");
                    break;
                }

                bool shouldWrite = false;
                TaskCompletionSource<bool>? tcs = null;

                lock (_writeLock)
                {
                    if (_dirty)
                    {
                        _dirty = false;
                        _writeInProgress = true;
                        shouldWrite = true;
                    }
                }

                if (shouldWrite)
                {
                    try
                    {
                        DrawerLogger.LogDebug("Background writer persisting settings to disk.");
                        string content;
                        lock (_writeLock)
                        {
                            content = SerializeSettings(_root);
                        }

                        if (token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(token);
                        }

                        lock (_writeLock)
                        {
                            if (_dirty)
                            {
                                _writeInProgress = false;
                                continue;
                            }
                        }

                        // write asynchronously
                        var directory = Path.GetDirectoryName(_settingsPath);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        await File.WriteAllTextAsync(_settingsPath, content, token);
                        DrawerLogger.LogInformation("Background settings write completed.");
                        lock (_writeLock)
                        {
                            _writeInProgress = false;
                            if (!_dirty)
                            {
                                tcs = _flushTcs;
                                _flushTcs = null;
                            }
                        }

                        tcs?.TrySetResult(true);
                    }
                    catch (OperationCanceledException)
                    {
                        DrawerLogger.LogInformation("Background writer write canceled.");
                        lock (_writeLock)
                        {
                            _writeInProgress = false;
                            if (!_dirty)
                            {
                                tcs = _flushTcs;
                                _flushTcs = null;
                            }
                        }

                        tcs?.TrySetCanceled();
                        break;
                    }
                    catch (Exception ex)
                    {
                        DrawerLogger.LogError("Background writer failed to persist settings.", ex);
                        lock (_writeLock)
                        {
                            _writeInProgress = false;
                            _dirty = true;
                            tcs = _flushTcs;
                            _flushTcs = null;
                        }

                        try { tcs?.TrySetException(ex); } catch { }
                    }
                }
            }
        }
        finally
        {
            DrawerLogger.LogInformation("Background settings writer loop stopping.");
            // Attempt one last write if dirty
            bool needFinal = false;
            lock (_writeLock)
            {
                if (_dirty)
                {
                    _dirty = false;
                    needFinal = true;
                }
            }

            if (needFinal)
            {
                try
                {
                    DrawerLogger.LogInformation("Background writer performing final flush.");
                    string content;
                    lock (_writeLock)
                    {
                        content = SerializeSettings(_root);
                    }

                    var directory = Path.GetDirectoryName(_settingsPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(_settingsPath, content);
                    DrawerLogger.LogInformation("Final settings flush completed.");
                }
                catch (Exception ex)
                {
                    DrawerLogger.LogError("Final settings flush failed.", ex);
                }
            }
        }
    }

    public void Dispose()
    {
        DrawerLogger.LogInformation("Disposing SettingsRepository.");
        try
        {
            if (_writerTask is { IsCompleted: false })
            {
                FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }

            _writerCts.Cancel();
            _writeSignal.Release();
            _writerTask?.Wait(500);
        }
        catch { }

        _writerCts.Dispose();
        _writeSignal.Dispose();
        DrawerLogger.LogInformation("SettingsRepository disposed.");
    }

    private TomlTable LoadOrCreateTable(string path)
    {
        if (!File.Exists(path))
        {
            DrawerLogger.LogInformation($"Settings file not found. Creating defaults at '{path}'.");
            var table = CreateDefaultTable();
            WriteTable(path, table);
            return table;
        }

        try
        {
            var settings = TomlSerializer.Deserialize<TomlTable>(File.ReadAllText(path));
            DrawerLogger.LogInformation($"Settings file loaded from '{path}'.");
            return settings ?? new TomlTable();
        }
        catch (TomlException ex)
        {
            DrawerLogger.LogError($"Failed to parse settings file '{path}'.", ex);
            throw new InvalidDataException($"Failed to parse settings file '{path}'.", ex);
        }
    }

    private void WriteTable(string path, TomlTable table)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        DrawerLogger.LogInformation($"Writing settings table to '{path}'.");
        File.WriteAllText(path, SerializeSettings(table));
    }

    private string SerializeSettings(TomlTable root)
    {
        var metadataStore = new TomlMetadataStore();
        var options = new TomlSerializerOptions { MetadataStore = metadataStore };
        var orderedRoot = new TomlTable();
        if (root.TryGetValue("drawers", out var drawers))
        {
            switch (drawers)
            {
                case TomlTableArray tableArray:
                    orderedRoot["drawers"] = CreateDrawerTableArray(tableArray, metadataStore);
                    break;
                case TomlArray array when array.All(entry => entry is TomlTable):
                    var drawerTables = new TomlTableArray();
                    foreach (var drawer in array.Cast<TomlTable>())
                    {
                        drawerTables.Add(CreateDrawerTable(drawer, metadataStore));
                    }

                    orderedRoot["drawers"] = drawerTables;
                    break;
                default:
                    orderedRoot["drawers"] = drawers;
                    break;
            }
        }

        if (root.TryGetValue("startup", out var startup))
        {
            if (startup is TomlTable startupTable)
            {
                var standardStartupTable = new TomlTable();
                foreach (var key in startupTable.Keys)
                {
                    standardStartupTable[key] = startupTable[key];
                }

                AddSectionFormatting(standardStartupTable, metadataStore,
                    "启动与主窗口设置",
                    "配置登录启动、窗口位置、主题颜色和图标大小。");
                orderedRoot["startup"] = standardStartupTable;
            }
            else
            {
                orderedRoot["startup"] = startup;
            }
        }

        foreach (var key in root.Keys)
        {
            if (key is not "drawers" and not "startup")
            {
                orderedRoot[key] = root[key];
            }
        }

        return TomlSerializer.Serialize(orderedRoot, options);
    }

    private TomlTableArray CreateDrawerTableArray(TomlTableArray drawers, TomlMetadataStore metadataStore)
    {
        var result = new TomlTableArray();
        foreach (var drawer in drawers)
        {
            result.Add(CreateDrawerTable(drawer, metadataStore));
        }

        return result;
    }

    private static TomlTable CreateDrawerTable(TomlTable source, TomlMetadataStore metadataStore)
    {
        var drawer = new TomlTable();
        foreach (var key in source.Keys)
        {
            drawer[key] = source[key];
        }

        // AddSectionFormatting(drawer, metadataStore,
        //     "抽屉目录",
        //     "name 为显示名称，path 为目录路径，size 为窗口尺寸；每个抽屉单独配置。");
        return drawer;
    }

    private void AddSectionFormatting(
        TomlTable table,
        TomlMetadataStore metadataStore,
        string title,
        string description)
    {
        var keys = table.Keys.ToList();
        if (keys.Count == 0)
        {
            return;
        }

        var firstKey = keys[0];
        var lastKey = keys[^1];
        var metadata = new TomlPropertiesMetadata();
        metadata.SetProperty(firstKey, new TomlPropertyMetadata
        {
            LeadingTrivia = new List<TomlSyntaxTriviaMetadata>
            {
                new(TokenKind.Comment, $"# {title}"),
                new(TokenKind.NewLine, "\n"),
                new(TokenKind.Comment, $"# {description}"),
                new(TokenKind.NewLine, "\n")
            }
        });
        if (firstKey != lastKey)
        {
            metadata.SetProperty(lastKey, new TomlPropertyMetadata
            {
                TrailingTriviaAfterEndOfLine = new List<TomlSyntaxTriviaMetadata>
                {
                    new(TokenKind.NewLine, "\n"),
                    // new(TokenKind.NewLine, "\n")
                }
            });
        }

        metadataStore.SetProperties(table, metadata);
    }

    private static TomlTable CreateDefaultTable()
    {
        return new TomlTable
        {
            ["startup"] = new TomlTable
            {
                ["start_with_windows"] = false,
                ["window_locked"] = false,
                ["icon_size"] = AppConstants.Defaults.IconSizeDefault,
                ["window_height"] = AppConstants.Defaults.MainWindowHeightDefault,
                ["window_position"] = new TomlArray { AppConstants.Defaults.WindowPosition.X, AppConstants.Defaults.WindowPosition.Y },
                ["window_color_hsla"] = new TomlArray
                {
                    AppConstants.Defaults.ThemeHue,
                    AppConstants.Defaults.ThemeSaturation,
                    AppConstants.Defaults.ThemeLightness,
                    AppConstants.Defaults.ThemeAlpha
                }
            },
            ["drawers"] = new TomlTableArray()
        };
    }

    private static TomlTable EnsureTable(TomlTable root, string key)
    {
        if (root.TryGetValue(key, out var existing) && existing is TomlTable existingTable)
        {
            return existingTable;
        }

        var created = new TomlTable();
        root[key] = created;
        return created;
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

        switch (value)
        {
            case TomlTableArray tableArray:
                drawers.AddRange(tableArray);
                return true;
            case TomlArray array:
                drawers.AddRange(array.OfType<TomlTable>());
                return true;
            default:
                return false;
        }
    }

    private static void AddDrawerEntry(TomlTable root, TomlTable drawer)
    {
        if (root.TryGetValue("drawers", out var value) && value is TomlArray array)
        {
            array.Add(drawer);
            return;
        }

        if (value is TomlTableArray tableArray)
        {
            tableArray.Add(drawer);
            return;
        }

        root["drawers"] = new TomlTableArray { drawer };
    }

    private static string ReadString(TomlTable table, string key, string fallback = "")
    {
        if (!table.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            string s => s,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback
        };
    }



    private static bool PathsEqual(string? first, string? second)
    {
        var expandedFirst = Environment.ExpandEnvironmentVariables(first ?? string.Empty).Trim();
        var expandedSecond = Environment.ExpandEnvironmentVariables(second ?? string.Empty).Trim();

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(expandedFirst, expandedSecond, comparison);
    }
}


public sealed class SettingsSnapshot(
    bool startWithWindows,
    bool windowLocked,
    int themeHue,
    int themeSaturation,
    int themeLightness,
    int themeAlpha,
    Point windowPosition,
    int iconSize,
    IEnumerable<DrawerDefinition>? drawers = null,
    int windowHeight = winDrawer.AppConstants.Defaults.MainWindowHeightDefault)
{
    private readonly List<DrawerDefinition> _drawers = drawers != null
            ? [.. drawers]
            : [];

    public bool StartWithWindows { get; set; } = startWithWindows;
    public bool WindowLocked { get; set; } = windowLocked;
    public int ThemeHue { get; set; } = themeHue;
    public int ThemeSaturation { get; set; } = themeSaturation;
    public int ThemeLightness { get; set; } = themeLightness;
    public int ThemeAlpha { get; set; } = themeAlpha;
    public Point WindowPosition { get; set; } = windowPosition;
    public int IconSize { get; set; } = iconSize;
    public int WindowHeight { get; set; } = Math.Clamp(windowHeight, winDrawer.AppConstants.Defaults.MainWindowHeightMin, winDrawer.AppConstants.Defaults.MainWindowHeightMax);
    public IReadOnlyList<DrawerDefinition> Drawers => _drawers;

    public ThemePalette BuildThemePalette()
    {
        return ThemePalette.FromHsla(ThemeHue, ThemeSaturation, ThemeLightness, ThemeAlpha);
    }

    public Color GetAccentColor()
    {
        return BuildThemePalette().Accent;
    }

    public SettingsSnapshot Clone()
    {
        return new SettingsSnapshot(StartWithWindows, WindowLocked, ThemeHue, ThemeSaturation, ThemeLightness, ThemeAlpha, WindowPosition, IconSize, _drawers, WindowHeight);
    }

    private static int[] GetIntArray(TomlTable table, string key, int[] fallback)
    {
        if (!table.TryGetValue(key, out var value) || value is not TomlArray arr)
        {
            return fallback;
        }

        return arr.Cast<object>().Select(v => Convert.ToInt32(v)).ToArray();
    }

    internal static SettingsSnapshot FromModel(TomlTable root)
    {
        var startup = root.TryGetValue("startup", out var startupObj) && startupObj is TomlTable st
            ? st
            : new TomlTable();
        var drawers = root.TryGetValue("drawers", out var drawersObj)
            ? drawersObj switch
            {
                TomlTableArray drawerArray => ParseDrawers(drawerArray),
                TomlArray drawerArray => ParseDrawers(drawerArray),
                _ => []
            }
            : [];

        var iconSize = GetInt(startup, "icon_size", AppConstants.Defaults.IconSizeMin, AppConstants.Defaults.IconSizeMax, AppConstants.Defaults.IconSizeDefault);
        var windowHeight = GetInt(startup, "window_height", AppConstants.Defaults.MainWindowHeightMin, AppConstants.Defaults.MainWindowHeightMax, AppConstants.Defaults.MainWindowHeightDefault);
        var windowPositionArray = GetIntArray(startup, "window_position", new int[] { AppConstants.Defaults.WindowPosition.X, AppConstants.Defaults.WindowPosition.Y });
        var windowColorHsla = GetIntArray(startup, "window_color_hsla", new int[]
        {
            AppConstants.Defaults.ThemeHue,
            AppConstants.Defaults.ThemeSaturation,
            AppConstants.Defaults.ThemeLightness,
            AppConstants.Defaults.ThemeAlpha
        });

        return new SettingsSnapshot(
            startWithWindows: GetBoolean(startup, "start_with_windows"),
            windowLocked: GetBoolean(startup, "window_locked"),
            themeHue: windowColorHsla.Length > 0 ? Math.Clamp(windowColorHsla[0], AppConstants.Theme.HueMin, AppConstants.Theme.HueMax) : AppConstants.Defaults.ThemeHue,
            themeSaturation: windowColorHsla.Length > 1 ? Math.Clamp(windowColorHsla[1], AppConstants.Theme.SaturationMin, AppConstants.Theme.SaturationMax) : AppConstants.Defaults.ThemeSaturation,
            themeLightness: windowColorHsla.Length > 2 ? Math.Clamp(windowColorHsla[2], AppConstants.Theme.LightnessMin, AppConstants.Theme.LightnessMax) : AppConstants.Defaults.ThemeLightness,
            themeAlpha: windowColorHsla.Length > 3 ? Math.Clamp(windowColorHsla[3], AppConstants.Theme.AlphaMin, AppConstants.Theme.AlphaMax) : AppConstants.Defaults.ThemeAlpha,
            windowPosition: new Point(windowPositionArray[0], windowPositionArray[1]),
            iconSize: iconSize,
            drawers: drawers,
            windowHeight: windowHeight);
    }

    private static IReadOnlyList<DrawerDefinition> ParseDrawers(TomlTableArray tableArray)
    {
        return ParseDrawers(tableArray.Cast<object>());
    }

    private static IReadOnlyList<DrawerDefinition> ParseDrawers(TomlArray array)
    {
        return ParseDrawers(array.Cast<object>());
    }

    private static IReadOnlyList<DrawerDefinition> ParseDrawers(IEnumerable<object> entries)
    {
        var results = new List<DrawerDefinition>();

        foreach (var entry in entries)
        {
            if (entry is not TomlTable drawerTable)
            {
                continue;
            }

            var name = GetString(drawerTable, "name").Trim();
            var pathValue = GetString(drawerTable, "path").Trim();

            if (string.IsNullOrWhiteSpace(pathValue))
            {
                continue;
            }

            var expandedPath = Environment.ExpandEnvironmentVariables(pathValue);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = Path.GetFileName(expandedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = expandedPath;
                }
            }

            Size? preferredSize = null;
            if (drawerTable.TryGetValue("size", out var sizeObj) && sizeObj is TomlArray sizeArray && sizeArray.Count == 2 &&
                TryGetInteger(sizeArray[0], out var width) && TryGetInteger(sizeArray[1], out var height))
            {
                preferredSize = new Size(Math.Clamp(width, 200, 4000), Math.Clamp(height, 200, 4000));
            }

            results.Add(new DrawerDefinition(name, expandedPath, preferredSize, ParseOverlayColor(drawerTable)));
        }

        return results;
    }

    private static Color? ParseOverlayColor(TomlTable drawerTable)
    {
        var value = GetString(drawerTable, "overlay_color").Trim();
        if (value.Length != 7 || value[0] != '#' ||
            !uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            if (value.Length == 9 && value[0] == '#' &&
                uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var oldArgb))
            {
                rgb = oldArgb & 0x00FFFFFF;
            }
            else
            {
                return null;
            }
        }

        return Color.FromArgb(179, (int)((rgb >> 16) & 0xFF), (int)((rgb >> 8) & 0xFF), (int)(rgb & 0xFF));
    }

    private static bool GetBoolean(TomlTable table, string key, bool fallback = false)
    {
        if (table.TryGetValue(key, out var value))
        {
            if (value is bool b)
            {
                return b;
            }

            if (value is string s && bool.TryParse(s, out var result))
            {
                return result;
            }
        }

        return fallback;
    }

    private static int GetInt(TomlTable table, string key, int min, int max, int fallback)
    {
        if (table.TryGetValue(key, out var value))
        {
            switch (value)
            {
                case int i:
                    return Math.Clamp(i, min, max);
                case long l:
                    return Math.Clamp((int)l, min, max);
                case double d:
                    return Math.Clamp((int)Math.Round(d, MidpointRounding.AwayFromZero), min, max);
                case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                    return Math.Clamp(parsed, min, max);
            }
        }

        return Math.Clamp(fallback, min, max);
    }

    private static bool TryGetInteger(object? value, out int result)
    {
        switch (value)
        {
            case int integer:
                result = integer;
                return true;
            case long integer when integer >= int.MinValue && integer <= int.MaxValue:
                result = (int)integer;
                return true;
            default:
                result = default;
                return false;
        }
    }

    private static string GetString(TomlTable table, string key, string fallback = "")
    {
        if (!table.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value switch
        {
            string s => s,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback
        };
    }

}
