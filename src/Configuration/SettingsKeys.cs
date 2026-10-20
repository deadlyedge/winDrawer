namespace winDrawer.Configuration;

/// <summary>
/// TOML key names of the settings file. They are referenced by the reader, the writer, the
/// serializer and the startup-default reset, so keeping them in one place avoids silent typos that
/// the compiler cannot catch.
/// </summary>
internal static class SettingsKeys
{
    public const string Startup = "startup";
    public const string Drawers = "drawers";
    public const string StartWithWindows = "start_with_windows";
    public const string WindowLocked = "window_locked";
    public const string AlwaysOnTop = "always_on_top";
    public const string ToggleHotkeyEnabled = "toggle_hotkey_enabled";
    public const string WindowHeight = "window_height";
    public const string WindowPosition = "window_position";
    public const string WindowColor = "window_color";
    public const string WindowOpacity = "window_opacity";
    public const string DrawerName = "name";
    public const string DrawerPath = "path";
    public const string DrawerSize = "size";
    public const string DrawerOverlayColor = "overlay_color";
}
