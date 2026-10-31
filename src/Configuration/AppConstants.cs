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

        /// <summary>Folder created inside the configuration directory for the first-run demo.</summary>
        public const string DemoDrawer = "demoDrawer";
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

        /// <summary>Project page opened by the settings window link.</summary>
        public const string RepositoryUrl = "https://github.com/deadlyedge/winDrawer";
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

        /// <summary>Frame interval of the transition animations; 0 disables every transition.</summary>
        public const int AnimationFrameMillisecondsMin = 0;
        public const int AnimationFrameMillisecondsDefault = 15;
        public const int AnimationFrameMillisecondsMax = 50;
        /// <summary>
        /// 96-DPI logical width of the main window. It stays narrow on purpose: the drawer list only
        /// needs room for folder names, and the header actions are icon buttons.
        /// </summary>
        public const int MainWindowWidth = 150;
        public static readonly Color ThemeColor = Color.FromArgb(0, 5, 51);
        public const int ThemeOpacity = 95;
        /// <summary>Point size of the branding font in the main-window header.</summary>
        public const float LogoFontSize = 11f;
        public const float ButtonFontSize = 10f;
        public const float CompactButtonFontSize = 9.5f;
    }

    internal static class Theme
    {
        /// <summary>
        /// Lowest allowed window opacity in percent. Below this the window text stops being readable
        /// on most wallpapers, so both the slider range and the parsed setting stop here.
        /// </summary>
        public const int OpacityMin = 40;

        public const int OpacityMax = 100;

        /// <summary>
        /// Relative luminance above which a surface counts as light and gets dark text. The default is
        /// the point where the two text colours (<see cref="DarkTextColor"/> and
        /// <see cref="LightTextColor"/>) have the same contrast ratio, so arbitrary overlay colours get
        /// the more readable option. Raise it to keep light text on more backgrounds; it is
        /// intentionally a build-time constant and not a user setting.
        /// </summary>
        public const double TextLuminanceThreshold = 0.229d;

        /// <summary>Text colour used on light surfaces.</summary>
        public static readonly Color DarkTextColor = Color.FromArgb(48, 48, 48);

        /// <summary>Text colour used on dark surfaces.</summary>
        public static readonly Color LightTextColor = Color.FromArgb(242, 242, 242);
    }

    internal static class Ui
    {
        public static readonly Size WindowIconSize = new(32, 32);
        public static readonly Size TrayIconSize = new(16, 16);

        /// <summary>
        /// Transition timings and offsets in 96-DPI logical units. The orchestration lives in
        /// <c>WindowAnimation.cs</c>; keeping the numbers here makes every transition behave the same.
        /// </summary>
        internal static class Animation
        {
            /// <summary>Fastest frame interval the clock accepts (higher values are slower, fewer frames).</summary>
            public const int MinFrameIntervalMilliseconds = 8;

            /// <summary>Timer resolution requested from Windows while a transition runs.</summary>
            public const int TimerResolutionMilliseconds = 1;

            /// <summary>Duration of short feedback transitions (appearing, scrollbar widening).</summary>
            public const int ShortDurationMilliseconds = 120;

            /// <summary>Duration of the drawer slide and other full transitions.</summary>
            public const int NormalDurationMilliseconds = 160;

            /// <summary>Duration of the drawer-list hover highlight fade.</summary>
            public const int HoverDurationMilliseconds = 160;

            /// <summary>Distance the drawer starts to the left of its final position.</summary>
            public const int DrawerSlideDistanceLogical = 24;

            /// <summary>Widened scrollbar track while the pointer is over it or it is dragged.</summary>
            public const int ScrollBarActiveWidthLogical = 6;

            /// <summary>Opacity factor a fading window starts from, relative to the theme opacity.</summary>
            public const double FadeInStartFactor = 0.4d;
        }

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
