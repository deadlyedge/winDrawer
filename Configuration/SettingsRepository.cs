using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;
using winDrawer;
using winDrawer.Theming;

namespace winDrawer.Configuration;

internal sealed class SettingsRepository : IDisposable
{
    private readonly string _settingsPath;
    private TomlTable _root;

    // Background writer state
    private readonly SemaphoreSlim _writeSignal = new(0);
    private readonly CancellationTokenSource _writerCts = new();
    private readonly object _writeLock = new();
    private Task? _writerTask;
    private bool _dirty;
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
        return Path.Combine(AppContext.BaseDirectory, AppConstants.Files.SettingsFileName);
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
            if (!_root.TryGetValue("drawers", out var drawersObj) || drawersObj is not TomlTableArray drawerArray)
            {
                DrawerLogger.LogWarning($"Drawer collection missing when updating size for '{drawerPath}'.");
                return false;
            }

            foreach (var entry in drawerArray.OfType<TomlTable>())
            {
                var candidatePath = ReadString(entry, "path");
                if (!PathsEqual(candidatePath, drawerPath))
                {
                    continue;
                }

                var sizeTable = entry.TryGetValue("size", out var sizeObj) && sizeObj is TomlTable st
                    ? st
                    : new TomlTable();

                sizeTable["width"] = size.Width;
                sizeTable["height"] = size.Height;
                entry["size"] = sizeTable;

                _dirty = true;
                _writeSignal.Release();
                DrawerLogger.LogInformation($"Queued drawer size update for '{drawerPath}' => {size}.");

                return true;
            }
        }

        DrawerLogger.LogWarning($"Unable to find drawer entry when updating size for '{drawerPath}'.");
        return false;
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
            if (!_root.TryGetValue("drawers", out var drawersObj) || drawersObj is not TomlTableArray drawerArray)
            {
                drawerArray = new TomlTableArray();
                _root["drawers"] = drawerArray;
            }

            // Check for existing entry
            foreach (var entry in drawerArray.OfType<TomlTable>())
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

            drawerArray.Add(entryTable);
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
            if (!_dirty)
            {
                DrawerLogger.LogDebug("Flush skipped because repository is not dirty.");
                return true; // nothing to flush
            }

            // create a TCS for this flush request; background writer will complete it after next write
            _flushTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            // ensure writer is signaled
            _writeSignal.Release();
            tcs = _flushTcs;
        }

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
        var success = completed == tcs.Task && tcs.Task.IsCompletedSuccessfully;
        // clear flush tcs if still set
        lock (_writeLock)
        {
            if (ReferenceEquals(_flushTcs, tcs))
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
            content = Toml.FromModel(_root);
        }

        DrawerLogger.LogInformation("Performing synchronous settings save.");
        File.WriteAllText(_settingsPath, content);
        DrawerLogger.LogInformation("Synchronous settings save completed.");
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
                        shouldWrite = true;
                        tcs = _flushTcs;
                        // clear _flushTcs here to avoid double-complete; we'll complete below after write
                        _flushTcs = null;
                    }
                }

                if (shouldWrite)
                {
                    try
                    {
                        DrawerLogger.LogDebug("Background writer persisting settings to disk.");
                        // capture content under lock to avoid inconsistencies
                        string content;
                        lock (_writeLock)
                        {
                            content = Toml.FromModel(_root);
                        }

                        // write asynchronously
                        var directory = Path.GetDirectoryName(_settingsPath);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        await File.WriteAllTextAsync(_settingsPath, content, token);
                        DrawerLogger.LogInformation("Background settings write completed.");
                        tcs?.TrySetResult(true);
                    }
                    catch (OperationCanceledException)
                    {
                        DrawerLogger.LogInformation("Background writer write canceled.");
                        tcs?.TrySetCanceled();
                        break;
                    }
                    catch (Exception ex)
                    {
                        // swallow IO errors but signal flush as failed
                        DrawerLogger.LogError("Background writer failed to persist settings.", ex);
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
                        content = Toml.FromModel(_root);
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
            _writerCts.Cancel();
            _writeSignal.Release();
            _writerTask?.Wait(500);
        }
        catch { }

        _writerCts.Dispose();
        _writeSignal.Dispose();
        DrawerLogger.LogInformation("SettingsRepository disposed.");
    }

    private static TomlTable LoadOrCreateTable(string path)
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
            var document = Toml.Parse(File.ReadAllText(path));
            DrawerLogger.LogInformation($"Settings file loaded from '{path}'.");
            return document.ToModel();
        }
        catch (TomlException ex)
        {
            DrawerLogger.LogError($"Failed to parse settings file '{path}'.", ex);
            throw new InvalidDataException($"Failed to parse settings file '{path}'.", ex);
        }
    }

    private static void WriteTable(string path, TomlTable table)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        DrawerLogger.LogInformation($"Writing settings table to '{path}'.");
        File.WriteAllText(path, Toml.FromModel(table));
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


public sealed class SettingsSnapshot
{
    private readonly List<DrawerDefinition> _drawers;

    public SettingsSnapshot(
        bool startWithWindows,
        bool windowLocked,
        int themeHue,
        int themeSaturation,
        int themeLightness,
        int themeAlpha,
        Point windowPosition,
        int iconSize,
        IEnumerable<DrawerDefinition>? drawers = null)
    {
        StartWithWindows = startWithWindows;
        WindowLocked = windowLocked;
        ThemeHue = themeHue;
        ThemeSaturation = themeSaturation;
        ThemeLightness = themeLightness;
        ThemeAlpha = themeAlpha;
        // ThumbnailWidth = thumbnailWidth;
        // ThumbnailHeight = thumbnailHeight;
        WindowPosition = windowPosition;
        IconSize = iconSize;
        _drawers = drawers != null
            ? new List<DrawerDefinition>(drawers)
            : new List<DrawerDefinition>();
    }

    public bool StartWithWindows { get; set; }
    public bool WindowLocked { get; set; }
    public int ThemeHue { get; set; }
    public int ThemeSaturation { get; set; }
    public int ThemeLightness { get; set; }
    public int ThemeAlpha { get; set; }
    // public int ThumbnailWidth { get; set; }
    // public int ThumbnailHeight { get; set; }
    public Point WindowPosition { get; set; }
    public int IconSize { get; set; }
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
        return new SettingsSnapshot(StartWithWindows, WindowLocked, ThemeHue, ThemeSaturation, ThemeLightness, ThemeAlpha, WindowPosition, IconSize, _drawers);
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
        var drawers = root.TryGetValue("drawers", out var drawersObj) && drawersObj is TomlTableArray drawerArray
            ? ParseDrawers(drawerArray)
            : Array.Empty<DrawerDefinition>();

        var iconSize = GetInt(startup, "icon_size", AppConstants.Defaults.IconSizeMin, AppConstants.Defaults.IconSizeMax, AppConstants.Defaults.IconSizeDefault);
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
            drawers: drawers);
    }

    private static IReadOnlyList<DrawerDefinition> ParseDrawers(TomlTableArray tableArray)
    {
        var results = new List<DrawerDefinition>();

        foreach (var entry in tableArray)
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
            if (drawerTable.TryGetValue("size", out var sizeObj) && sizeObj is TomlTable sizeTable)
            {
                var width = GetInt(sizeTable, "width", 200, 4000, 800);
                var height = GetInt(sizeTable, "height", 200, 4000, 600);
                preferredSize = new Size(width, height);
            }

            results.Add(new DrawerDefinition(name, expandedPath, preferredSize));
        }

        return results;
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
