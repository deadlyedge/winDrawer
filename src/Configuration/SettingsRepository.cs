using Tomlyn.Model;
using winDrawer.Theming;

namespace winDrawer.Configuration;

internal sealed partial class SettingsRepository : IDisposable
{
    private const string ObsoleteIconSizeSettingName = "icon_size";

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
    ArgumentNullException.ThrowIfNull(snapshot);

    lock (_writeLock)
        {
            var startup = EnsureTable(_root, SettingsKeys.Startup);
            startup[SettingsKeys.StartWithWindows] = snapshot.StartWithWindows;
            startup[SettingsKeys.WindowLocked] = snapshot.WindowLocked;
            startup[SettingsKeys.AlwaysOnTop] = snapshot.AlwaysOnTop;
            startup[SettingsKeys.ToggleHotkeyEnabled] = snapshot.ToggleHotkeyEnabled;
            startup[SettingsKeys.WindowHeight] = Math.Clamp(snapshot.WindowHeight, AppConstants.Defaults.MainWindowHeightMin, AppConstants.Defaults.MainWindowHeightMax);
            startup[SettingsKeys.MaxDrawerItems] = Math.Clamp(snapshot.MaxDrawerItems, AppConstants.Defaults.MaxDrawerItemsMin, AppConstants.Defaults.MaxDrawerItemsMax);
            startup[SettingsKeys.AnimationFrameMilliseconds] = Math.Clamp(
                snapshot.AnimationFrameMilliseconds,
                AppConstants.Defaults.AnimationFrameMillisecondsMin,
                AppConstants.Defaults.AnimationFrameMillisecondsMax);
            startup[SettingsKeys.WindowPosition] = new TomlArray { snapshot.WindowPosition.X, snapshot.WindowPosition.Y };
            startup[SettingsKeys.WindowColor] = ThemePalette.FormatHexColor(snapshot.WindowColor);
            startup[SettingsKeys.WindowOpacity] = Math.Clamp(snapshot.WindowOpacity, AppConstants.Theme.OpacityMin, AppConstants.Theme.OpacityMax);
            // Mark dirty and signal background writer
            _dirty = true;
        }

        DrawerLogger.LogInformation("Settings snapshot updated; background write scheduled.");
        _writeSignal.Release();
    }

    public SettingsSnapshot GetEmbeddedStartupDefaults()
    {
        var defaults = SettingsSnapshot.FromModel(LoadEmbeddedDefaultTable());
        var current = GetSnapshot();
        return new SettingsSnapshot(
            startWithWindows: defaults.StartWithWindows,
            windowLocked: defaults.WindowLocked,
            windowColor: defaults.WindowColor,
            windowOpacity: defaults.WindowOpacity,
            windowPosition: defaults.WindowPosition,
            drawers: current.Drawers,
            windowHeight: defaults.WindowHeight,
            alwaysOnTop: defaults.AlwaysOnTop,
            toggleHotkeyEnabled: defaults.ToggleHotkeyEnabled,
            maxDrawerItems: defaults.MaxDrawerItems,
            animationFrameMilliseconds: defaults.AnimationFrameMilliseconds);
    }

    public bool ResetStartupToEmbeddedDefaults(TimeSpan timeout, out SettingsSnapshot? resetSnapshot, out string error)
    {
        resetSnapshot = null;
        error = string.Empty;
        if (timeout <= TimeSpan.Zero)
        {
            error = "The settings reset could not be saved because the write timeout is invalid.";
            return false;
        }

        if (!SavePendingChanges(timeout))
        {
            error = "Pending settings could not be saved before resetting startup options.";
            return false;
        }

        lock (_writeLock)
        {
            if (_writeInProgress || _dirty)
            {
                error = "Pending settings changed while preparing the reset. Retry the reset after the current write completes.";
                return false;
            }

            TomlTable defaults;
            try
            {
                defaults = LoadEmbeddedDefaultTable();
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Unable to load embedded startup defaults.", ex);
                error = $"The embedded default settings could not be loaded.{Environment.NewLine}{ex.Message}";
                return false;
            }

            if (!defaults.TryGetValue(SettingsKeys.Startup, out var defaultsValue) || defaultsValue is not TomlTable defaultsStartup)
            {
                error = "The embedded default settings do not contain a valid startup section.";
                return false;
            }

            if (!TryGetStartupTable(_root, out var currentStartup))
            {
                error = "The current startup settings are not a valid TOML table.";
                return false;
            }

            var previousStartup = CloneTable(currentStartup);
            var previousRoot = _root;
            foreach (var key in defaultsStartup.Keys)
            {
                currentStartup[key] = defaultsStartup[key];
            }

            currentStartup.Remove(ObsoleteIconSizeSettingName);
            _root[SettingsKeys.Startup] = currentStartup;
            var snapshot = SettingsSnapshot.FromModel(_root);

            try
            {
                WriteTable(_settingsPath, _root);
                _dirty = false;
                resetSnapshot = snapshot;
                DrawerLogger.LogInformation("Startup settings reset to embedded defaults and saved immediately.");
                return true;
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Failed to persist startup defaults; restoring the previous in-memory settings.", ex);
                previousRoot[SettingsKeys.Startup] = previousStartup;
                _root = previousRoot;
                error = $"The default startup options could not be saved. Existing settings were retained.{Environment.NewLine}{ex.Message}";
                return false;
            }
        }
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

    private static bool TryGetStartupTable(TomlTable root, out TomlTable startup)
    {
        if (!root.TryGetValue(SettingsKeys.Startup, out var value))
        {
            startup = [];
            return true;
        }

        if (value is TomlTable table)
        {
            startup = table;
            return true;
        }

        startup = null!;
        return false;
    }

    private static TomlTable CloneTable(TomlTable source)
    {
        var clone = new TomlTable();
        foreach (var key in source.Keys)
        {
            clone[key] = source[key];
        }

        return clone;
    }

    private static string ReadString(TomlTable table, string key, string fallback = "")
    {
        if (!table.TryGetValue(key, out var value) || value is null)
        {
            return fallback;
        }

        return value is string text ? text : fallback;
    }
}
