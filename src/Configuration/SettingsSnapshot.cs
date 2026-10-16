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
    bool alwaysOnTop = false)
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
    public IReadOnlyList<DrawerDefinition> Drawers => _drawers;

    public ThemePalette BuildThemePalette()
    {
        return ThemePalette.FromColor(WindowColor, WindowOpacity);
    }

    public Color GetAccentColor()
    {
        return BuildThemePalette().Accent;
    }

    public SettingsSnapshot Clone()
    {
        return new SettingsSnapshot(StartWithWindows, WindowLocked, WindowColor, WindowOpacity, WindowPosition, _drawers, WindowHeight, AlwaysOnTop);
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
        var startup = root.TryGetValue("startup", out var startupObj) && startupObj is TomlTable st
            ? st
            : new TomlTable();
        var drawers = root.TryGetValue("drawers", out var drawersObj) && drawersObj is TomlTableArray drawerArray
            ? ParseDrawers(drawerArray)
            : [];

        var windowHeight = GetInt(startup, "window_height", AppConstants.Defaults.MainWindowHeightMin, AppConstants.Defaults.MainWindowHeightMax, AppConstants.Defaults.MainWindowHeightDefault);
        var windowPositionArray = GetIntArray(startup, "window_position", new int[] { AppConstants.Defaults.WindowPosition.X, AppConstants.Defaults.WindowPosition.Y });
        var windowColor = ThemePalette.TryParseHexColor(GetString(startup, "window_color"), out var parsedColor)
            ? parsedColor
            : AppConstants.Defaults.ThemeColor;

        return new SettingsSnapshot(
            startWithWindows: GetBoolean(startup, "start_with_windows"),
            windowLocked: GetBoolean(startup, "window_locked"),
            windowColor: windowColor,
            windowOpacity: GetInt(startup, "window_opacity", AppConstants.Theme.OpacityMin, AppConstants.Theme.OpacityMax, AppConstants.Defaults.ThemeOpacity),
            windowPosition: new Point(windowPositionArray[0], windowPositionArray[1]),
            drawers: drawers,
            windowHeight: windowHeight,
            alwaysOnTop: GetBoolean(startup, "always_on_top"));
    }

    private static IReadOnlyList<DrawerDefinition> ParseDrawers(TomlTableArray entries)
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
        return ThemePalette.TryParseHexColor(GetString(drawerTable, "overlay_color"), out var color)
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
