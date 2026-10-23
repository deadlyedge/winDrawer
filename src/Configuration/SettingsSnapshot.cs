using Tomlyn.Model;
using winDrawer.Theming;

namespace winDrawer.Configuration;

public sealed class SettingsSnapshot(
    bool startWithWindows,
    bool windowLocked,
    Color windowColor,
    int windowOpacity,
    Point windowPosition,
    IEnumerable<DrawerDefinition>? drawers = null,
    int windowHeight = winDrawer.AppConstants.Defaults.MainWindowHeightDefault,
    bool alwaysOnTop = false,
    bool toggleHotkeyEnabled = true)
{
    private readonly List<DrawerDefinition> _drawers = drawers != null
            ? [.. drawers]
            : [];

    public bool StartWithWindows { get; set; } = startWithWindows;
    public bool WindowLocked { get; set; } = windowLocked;
    public Color WindowColor { get; set; } = Color.FromArgb(windowColor.R, windowColor.G, windowColor.B);
    public int WindowOpacity { get; set; } = Math.Clamp(windowOpacity, 0, 100);
    public Point WindowPosition { get; set; } = windowPosition;
    public int WindowHeight { get; set; } = Math.Clamp(windowHeight, winDrawer.AppConstants.Defaults.MainWindowHeightMin, winDrawer.AppConstants.Defaults.MainWindowHeightMax);
    public bool AlwaysOnTop { get; set; } = alwaysOnTop;
    public bool ToggleHotkeyEnabled { get; set; } = toggleHotkeyEnabled;
    public IReadOnlyList<DrawerDefinition> Drawers => _drawers;

    public ThemePalette BuildThemePalette()
    {
        return ThemePalette.FromColor(WindowColor, WindowOpacity);
    }

    public SettingsSnapshot Clone()
    {
        return new SettingsSnapshot(
            startWithWindows: StartWithWindows,
            windowLocked: WindowLocked,
            windowColor: WindowColor,
            windowOpacity: WindowOpacity,
            windowPosition: WindowPosition,
            drawers: _drawers,
            windowHeight: WindowHeight,
            alwaysOnTop: AlwaysOnTop,
            toggleHotkeyEnabled: ToggleHotkeyEnabled);
    }

    private static int[] GetIntArray(TomlTable table, string key, int[] fallback)
    {
        if (!table.TryGetValue(key, out var value) || value is not TomlArray array || array.Count != fallback.Length)
        {
            return (int[])fallback.Clone();
        }

        var result = new int[fallback.Length];
        for (var index = 0; index < array.Count; index++)
        {
            if (!TryGetInteger(array[index], out result[index]))
            {
                return (int[])fallback.Clone();
            }
        }

        return result;
    }

    internal static SettingsSnapshot FromModel(TomlTable root)
    {
        var startup = root.TryGetValue(SettingsKeys.Startup, out var startupObj) && startupObj is TomlTable st
            ? st
            : [];
        var drawers = root.TryGetValue(SettingsKeys.Drawers, out var drawersObj) && drawersObj is TomlTableArray drawerArray
            ? ParseDrawers(drawerArray)
            : [];

        var windowHeight = GetInt(startup, SettingsKeys.WindowHeight, AppConstants.Defaults.MainWindowHeightMin, AppConstants.Defaults.MainWindowHeightMax, AppConstants.Defaults.MainWindowHeightDefault);
        var windowPositionArray = GetIntArray(startup, SettingsKeys.WindowPosition, [AppConstants.Defaults.WindowPosition.X, AppConstants.Defaults.WindowPosition.Y]);
        var windowColor = ThemePalette.TryParseHexColor(GetString(startup, SettingsKeys.WindowColor), out var parsedColor)
            ? parsedColor
            : AppConstants.Defaults.ThemeColor;

        return new SettingsSnapshot(
            startWithWindows: GetBoolean(startup, SettingsKeys.StartWithWindows),
            windowLocked: GetBoolean(startup, SettingsKeys.WindowLocked),
            windowColor: windowColor,
            windowOpacity: GetInt(startup, SettingsKeys.WindowOpacity, AppConstants.Theme.OpacityMin, AppConstants.Theme.OpacityMax, AppConstants.Defaults.ThemeOpacity),
            windowPosition: new Point(windowPositionArray[0], windowPositionArray[1]),
            drawers: drawers,
            windowHeight: windowHeight,
            alwaysOnTop: GetBoolean(startup, SettingsKeys.AlwaysOnTop),
            toggleHotkeyEnabled: GetBoolean(startup, SettingsKeys.ToggleHotkeyEnabled, fallback: true));
    }

    private static List<DrawerDefinition> ParseDrawers(TomlTableArray entries)
    {
        var results = new List<DrawerDefinition>();

        foreach (var entry in entries)
        {
            if (entry is not TomlTable drawerTable)
            {
                continue;
            }

            var name = GetString(drawerTable, SettingsKeys.DrawerName).Trim();
            var pathValue = GetString(drawerTable, SettingsKeys.DrawerPath).Trim();

            if (string.IsNullOrWhiteSpace(pathValue))
            {
                continue;
            }

            var expandedPath = PathComparison.Normalize(pathValue);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = Path.GetFileName(expandedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = expandedPath;
                }
            }

            Size? preferredSize = null;
            if (drawerTable.TryGetValue(SettingsKeys.DrawerSize, out var sizeObj) && sizeObj is TomlArray sizeArray && sizeArray.Count == 2 &&
                TryGetInteger(sizeArray[0], out var width) && TryGetInteger(sizeArray[1], out var height))
            {
                preferredSize = new Size(Math.Clamp(width, 200, 4000), Math.Clamp(height, 200, 4000));
            }

            results.Add(new DrawerDefinition(
                name,
                expandedPath,
                preferredSize,
                ParseOverlayColor(drawerTable),
                DrawerSortModeText.Parse(GetString(drawerTable, SettingsKeys.DrawerSortMode))));
        }

        return results;
    }

    private static Color? ParseOverlayColor(TomlTable drawerTable)
    {
        return ThemePalette.TryParseHexColor(GetString(drawerTable, SettingsKeys.DrawerOverlayColor), out var color)
            ? Color.FromArgb(179, color.R, color.G, color.B)
            : null;
    }

    private static bool GetBoolean(TomlTable table, string key, bool fallback = false)
    {
        return table.TryGetValue(key, out var value) && value is bool boolean ? boolean : fallback;
    }

    private static int GetInt(TomlTable table, string key, int min, int max, int fallback)
    {
        if (table.TryGetValue(key, out var value))
        {
            switch (value)
            {
                case int i:
                    return Math.Clamp(i, min, max);
                case long l when l >= int.MinValue && l <= int.MaxValue:
                    return Math.Clamp((int)l, min, max);
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

        return value is string text ? text : fallback;
    }

}
