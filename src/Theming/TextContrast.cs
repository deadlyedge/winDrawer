namespace winDrawer.Theming;

/// <summary>
/// Single definition of the light/dark text rule: surfaces above
/// <see cref="AppConstants.Theme.TextLuminanceThreshold"/> get dark text, everything else gets light
/// text. The threshold is a build-time constant on purpose; change it in <see cref="AppConstants"/>
/// and rebuild to tune how a theme colour is classified.
/// </summary>
internal static class TextContrast
{
    /// <summary>Returns the text colour that stays readable on <paramref name="background"/>.</summary>
    public static Color For(Color background)
    {
        return RelativeLuminance(background) > AppConstants.Theme.TextLuminanceThreshold
            ? AppConstants.Theme.DarkTextColor
            : AppConstants.Theme.LightTextColor;
    }

    /// <summary>WCAG relative luminance of an sRGB colour, in the 0..1 range.</summary>
    public static double RelativeLuminance(Color color)
    {
        static double LineariseChannel(int component)
        {
            var channel = component / 255d;
            return channel <= 0.03928
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        var r = LineariseChannel(color.R);
        var g = LineariseChannel(color.G);
        var b = LineariseChannel(color.B);

        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }
}
