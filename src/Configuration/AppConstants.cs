namespace winDrawer;

/// <summary>
/// Central location for application-wide constants so that hard-coded values are easy to discover and reuse.
/// </summary>
internal static class AppConstants
{
    public static string GetUserConfigurationDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config",
            "winDrawer");
    }

    internal static class Files
    {
        public const string SettingsFileName = "Drawer-settings.toml";
        public const string LogFileName = "Drawer.log";
        public const string AppIconFileName = "drawer.icon.4.ico";
        public const string LogoFontFileName = "Lobster-Regular.ttf";
    }

    internal static class Directories
    {
        public const string Assets = "assets";
    }

    internal static class Logging
    {
        public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// Optional environment variable that overrides the minimum logged level
        /// (<c>Debug</c>, <c>Information</c>, <c>Warning</c>, <c>Error</c> or <c>Off</c>).
        /// </summary>
        public const string LevelEnvironmentVariable = "WINDRAWER_LOG_LEVEL";

        /// <summary>Size the active log file may reach before it is rotated to <c>Drawer.log.1</c>.</summary>
        public const long MaxLogFileBytes = 1024 * 1024;

        /// <summary>Suffix of the rotated (previous) log file.</summary>
        public const string RotatedLogFileSuffix = ".1";
    }

    internal static class Branding
    {
        public const string ApplicationDisplayName = "winDrawer";
    }

    /// <summary>
    /// Global (system-wide) shortcuts. The values are registered against the main-window handle and
    /// the window receives <see cref="GlobalHotKey.WmHotKey"/> for the matching id.
    /// </summary>
    internal static class HotKeys
    {
        /// <summary>Window-message identifier of the show/hide shortcut ('WD').</summary>
        public const int ToggleVisibilityId = 0x5744;

        /// <summary>Human-readable shortcut name used in logs and user messages.</summary>
        public const string ToggleVisibilityDescription = "Win+Shift+D";
    }

    internal static class Defaults
    {
        public static readonly Point WindowPosition = new(100, 100);
        public const int MainWindowHeightMin = 100;
        public const int MainWindowHeightDefault = 200;
        public const int MainWindowHeightMax = 1000;
        public const int MaxDrawerItemsMin = 1;
        public const int MaxDrawerItemsDefault = 200;
        public const int MaxDrawerItemsMax = 10000;
        public const int MainWindowWidth = 200;
        public static readonly Color ThemeColor = Color.FromArgb(0, 5, 51);
        public const int ThemeOpacity = 95;
        public const float LogoFontSize = 14f;
        public const float ButtonFontSize = 10f;
    }

    internal static class Theme
    {
        public const int OpacityMin = 0;
        public const int OpacityMax = 100;
    }

    internal static class Ui
    {
        public static readonly Size WindowIconSize = new(32, 32);
        public static readonly Size TrayIconSize = new(16, 16);

        /// <summary>
        /// Drawer footer metrics in 96-DPI logical pixels. The drawer window uses
        /// <c>AutoScaleMode.None</c>, so these values are scaled explicitly for the current DPI.
        /// </summary>
        internal static class DrawerFooter
        {
            public const int PaddingLeft = 12;
            public const int PaddingRight = 8;
            public const int IconTextGap = 8;
            public const int FolderIconSize = 24;
        }
    }
}
