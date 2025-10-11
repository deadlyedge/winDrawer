using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Tomlyn;
using Tomlyn.Model;

namespace winDrawer.Configuration;

internal sealed class SettingsRepository
{
    private readonly string _settingsPath;
    private TomlTable _root;

    public SettingsRepository(string settingsPath)
    {
        _settingsPath = settingsPath;
        _root = LoadOrCreateTable(settingsPath);
    }

    public static string GetDefaultPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Drawer-settings.toml");
    }

    public SettingsSnapshot GetSnapshot()
    {
        return SettingsSnapshot.FromModel(_root);
    }

    public void Update(SettingsSnapshot snapshot)
    {
        var startup = EnsureTable(_root, "startup");
        startup["start_with_windows"] = snapshot.StartWithWindows;
        startup["window_locked"] = snapshot.WindowLocked;
        startup["icon_size"] = snapshot.IconSize;
        startup["window_position"] = new TomlArray { snapshot.WindowPosition.X, snapshot.WindowPosition.Y };
        startup["window_color_hsla"] = new TomlArray { snapshot.ThemeHue, snapshot.ThemeSaturation, snapshot.ThemeLightness, snapshot.ThemeAlpha };
    }

    public bool UpdateDrawerSize(string drawerPath, Size size)
    {
        if (string.IsNullOrWhiteSpace(drawerPath))
        {
            return false;
        }

        if (!_root.TryGetValue("drawers", out var drawersObj) || drawersObj is not TomlTableArray drawerArray)
        {
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
            return true;
        }

        return false;
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var content = Toml.FromModel(_root);
        File.WriteAllText(_settingsPath, content);
    }

    private static TomlTable LoadOrCreateTable(string path)
    {
        if (!File.Exists(path))
        {
            var table = CreateDefaultTable();
            WriteTable(path, table);
            return table;
        }

        try
        {
            var document = Toml.Parse(File.ReadAllText(path));
            return document.ToModel();
        }
        catch (TomlException ex)
        {
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
                ["icon_size"] = 96,
                ["window_position"] = new TomlArray { 0, 0 },
                ["window_color_hsla"] = new TomlArray { 194, 100, 41, 60 }
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
        int thumbnailWidth,
        int thumbnailHeight,
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
        ThumbnailWidth = thumbnailWidth;
        ThumbnailHeight = thumbnailHeight;
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
    public int ThumbnailWidth { get; set; }
    public int ThumbnailHeight { get; set; }
    public Point WindowPosition { get; set; }
    public int IconSize { get; set; }
    public IReadOnlyList<DrawerDefinition> Drawers => _drawers;

    public Color GetAccentColor()
    {
        var saturation = ThemeSaturation / 100f;
        var lightness = ThemeLightness / 100f;
        var alpha = ThemeAlpha / 100f;
        return ColorFromHsla(ThemeHue, saturation, lightness, alpha);
    }

    public Size GetThumbnailSize()
    {
        var width = Math.Clamp(ThumbnailWidth, 16, 512);
        var height = Math.Clamp(ThumbnailHeight, 16, 512);
        return new Size(width, height);
    }

    public SettingsSnapshot Clone()
    {
        return new SettingsSnapshot(StartWithWindows, WindowLocked, ThemeHue, ThemeSaturation, ThemeLightness, ThemeAlpha, ThumbnailWidth, ThumbnailHeight, WindowPosition, IconSize, _drawers);
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

        var iconSize = GetInt(startup, "icon_size", 32, 512, 96);
        var windowPositionArray = GetIntArray(startup, "window_position", new int[] { 0, 0 });
        var windowColorHsla = GetIntArray(startup, "window_color_hsla", new int[] { 194, 100, 41, 60 });

        return new SettingsSnapshot(
            startWithWindows: GetBoolean(startup, "start_with_windows"),
            windowLocked: GetBoolean(startup, "window_locked"),
            themeHue: windowColorHsla.Length > 0 ? Math.Clamp(windowColorHsla[0], 0, 360) : 194,
            themeSaturation: windowColorHsla.Length > 1 ? Math.Clamp(windowColorHsla[1], 0, 100) : 100,
            themeLightness: windowColorHsla.Length > 2 ? Math.Clamp(windowColorHsla[2], 0, 100) : 41,
            themeAlpha: windowColorHsla.Length > 3 ? Math.Clamp(windowColorHsla[3], 0, 100) : 60,
            thumbnailWidth: iconSize,
            thumbnailHeight: iconSize,
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

    private static Color ColorFromHsla(int hue, float saturation, float lightness, float alpha)
    {
        lightness = Math.Clamp(lightness, 0f, 1f);
        saturation = Math.Clamp(saturation, 0f, 1f);

        float q = lightness < 0.5f
            ? lightness * (1 + saturation)
            : lightness + saturation - lightness * saturation;
        float p = 2 * lightness - q;

        float r = HueToRgb(p, q, hue / 360f + 1f / 3f);
        float g = HueToRgb(p, q, hue / 360f);
        float b = HueToRgb(p, q, hue / 360f - 1f / 3f);

        var a = (int)Math.Clamp(alpha * 255f, 0f, 255f);
        return Color.FromArgb(a, (int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    private static float HueToRgb(float p, float q, float t)
    {
        if (t < 0f)
        {
            t += 1f;
        }
        if (t > 1f)
        {
            t -= 1f;
        }
        if (t < 1f / 6f)
        {
            return p + (q - p) * 6f * t;
        }
        if (t < 1f / 2f)
        {
            return q;
        }
        if (t < 2f / 3f)
        {
            return p + (q - p) * (2f / 3f - t) * 6f;
        }
        return p;
    }
}

public sealed class DrawerDefinition
{
    public DrawerDefinition(string name, string path, Size? preferredSize)
    {
        Name = name;
        Path = path;
        PreferredSize = preferredSize;
    }

    public string Name { get; }
    public string Path { get; }
    public Size? PreferredSize { get; }

    public override string ToString() => Name;
}
