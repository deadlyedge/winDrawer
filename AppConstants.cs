using System.Drawing;

namespace winDrawer;

/// <summary>
/// Central location for application-wide constants so that hard-coded values are easy to discover and reuse.
/// </summary>
internal static class AppConstants
{
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
        public const string Fonts = "fonts";
    }

    internal static class Logging
    {
        public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    }

    internal static class Branding
    {
        public const string ApplicationDisplayName = "winDrawer";
    }

    internal static class Defaults
    {
        public static readonly Point WindowPosition = new(100, 100);
        public const int IconSizeMin = 32;
        public const int IconSizeMax = 512;
        public const int IconSizeDefault = 96;
        public const int ThemeHue = 194;
        public const int ThemeSaturation = 100;
        public const int ThemeLightness = 41;
        public const int ThemeAlpha = 60;
        public const float LogoFontSize = 14f;
    }

    internal static class Theme
    {
        public const int HueMin = 0;
        public const int HueMax = 360;
        public const int SaturationMin = 0;
        public const int SaturationMax = 100;
        public const int LightnessMin = 0;
        public const int LightnessMax = 100;
        public const int AlphaMin = 0;
        public const int AlphaMax = 100;
    }

    internal static class Ui
    {
        public static readonly Size WindowIconSize = new(32, 32);
        public static readonly Size TrayIconSize = new(16, 16);
    }

    internal static class DrawerUi
    {
        public const float ItemHoverBlendAmount = 0.28f;
        public const int ResizeGripWidth = 30;
        public const int ResizeGripHeight = 44;
        public const int ContentHorizontalPadding = 2;
        public const int ContentVerticalPadding = 8;
    }
}
