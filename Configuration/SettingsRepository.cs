using System;
using System.Drawing;
using System.Globalization;
using System.IO;
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

        var theme = EnsureTable(_root, "theme");
        theme["h"] = snapshot.ThemeHue;
        theme["s"] = snapshot.ThemeSaturation;
        theme["l"] = snapshot.ThemeLightness;
        theme["a"] = snapshot.ThemeAlpha;
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
                ["window_locked"] = false
            },
            ["drawers"] = new TomlTableArray(),
            ["window_position"] = new TomlTable
            {
                ["x"] = 0,
                ["y"] = 0
            },
            ["thumbnail_size"] = new TomlTable
            {
                ["width"] = 96,
                ["height"] = 96
            },
            ["theme"] = new TomlTable
            {
                ["h"] = 194,
                ["s"] = 100,
                ["l"] = 41,
                ["a"] = 60
            }
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
}

public sealed class SettingsSnapshot
{
    public SettingsSnapshot(bool startWithWindows, bool windowLocked, int themeHue, int themeSaturation, int themeLightness, int themeAlpha)
    {
        StartWithWindows = startWithWindows;
        WindowLocked = windowLocked;
        ThemeHue = themeHue;
        ThemeSaturation = themeSaturation;
        ThemeLightness = themeLightness;
        ThemeAlpha = themeAlpha;
    }

    public bool StartWithWindows { get; set; }
    public bool WindowLocked { get; set; }
    public int ThemeHue { get; set; }
    public int ThemeSaturation { get; set; }
    public int ThemeLightness { get; set; }
    public int ThemeAlpha { get; set; }

    public Color GetAccentColor()
    {
        var saturation = ThemeSaturation / 100f;
        var lightness = ThemeLightness / 100f;
        var alpha = ThemeAlpha / 100f;
        return ColorFromHsla(ThemeHue, saturation, lightness, alpha);
    }

    public SettingsSnapshot Clone()
    {
        return new SettingsSnapshot(StartWithWindows, WindowLocked, ThemeHue, ThemeSaturation, ThemeLightness, ThemeAlpha);
    }

    internal static SettingsSnapshot FromModel(TomlTable root)
    {
        var startup = root.TryGetValue("startup", out var startupObj) && startupObj is TomlTable st
            ? st
            : new TomlTable();
        var theme = root.TryGetValue("theme", out var themeObj) && themeObj is TomlTable th
            ? th
            : new TomlTable();

        return new SettingsSnapshot(
            startWithWindows: GetBoolean(startup, "start_with_windows"),
            windowLocked: GetBoolean(startup, "window_locked"),
            themeHue: GetInt(theme, "h", 0, 360, 194),
            themeSaturation: GetInt(theme, "s", 0, 100, 100),
            themeLightness: GetInt(theme, "l", 0, 100, 41),
            themeAlpha: GetInt(theme, "a", 0, 100, 60));
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
