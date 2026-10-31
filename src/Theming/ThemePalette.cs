using System.Globalization;

namespace winDrawer.Theming;

/// <summary>
/// Represents the calculated colors and opacity that should be applied across the application.
/// </summary>
public sealed class ThemePalette
{
    private ThemePalette(
        Color accent,
        Color accentHover,
        Color accentPressed,
        Color accentText,
        Color border,
        Color windowBackground,
        Color contentBackground,
        Color cardBackground,
        Color textPrimary,
        Color contentTextPrimary,
        Color textSecondary,
        double windowOpacity)
    {
        Accent = accent;
        AccentHover = accentHover;
        AccentPressed = accentPressed;
        AccentText = accentText;
        Border = border;
        WindowBackground = windowBackground;
        ContentBackground = contentBackground;
        CardBackground = cardBackground;
        TextPrimary = textPrimary;
        ContentTextPrimary = contentTextPrimary;
        TextSecondary = textSecondary;
        WindowOpacity = windowOpacity;
    }

    public Color Accent { get; }
    public Color AccentHover { get; }
    public Color AccentPressed { get; }
    public Color AccentText { get; }
    public Color Border { get; }
    public Color WindowBackground { get; }
    public Color ContentBackground { get; }
    public Color CardBackground { get; }
    public Color TextPrimary { get; }

    /// <summary>
    /// Text colour for the content surface (<see cref="ContentBackground"/>), which differs slightly
    /// from <see cref="WindowBackground"/> and is therefore classified separately.
    /// </summary>
    public Color ContentTextPrimary { get; }

    public Color TextSecondary { get; }
    public double WindowOpacity { get; }

    public static ThemePalette FromColor(Color color, int opacity)
    {
        return CreatePalette(
            color,
            Math.Clamp(opacity, AppConstants.Theme.OpacityMin, AppConstants.Theme.OpacityMax) / 100d);
    }

    /// <summary>
    /// True when both palettes produce the same main-window drawer-list colours. The list caches its
    /// row colours, so this is used to skip an unnecessary repaint when only unrelated settings
    /// (opacity, window height, ...) changed.
    /// </summary>
    internal bool HasSameDrawerListColors(ThemePalette other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return ContentBackground == other.ContentBackground &&
               ContentTextPrimary == other.ContentTextPrimary &&
               CardBackground == other.CardBackground &&
               Accent == other.Accent &&
               AccentText == other.AccentText;
    }

    public static bool TryParseHexColor(string? value, out Color color)
    {
        color = Color.Empty;
        if (value is not { Length: 7 } || value[0] != '#' ||
            !uint.TryParse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return false;
        }

        color = Color.FromArgb(
            (int)((rgb >> 16) & 0xFF),
            (int)((rgb >> 8) & 0xFF),
            (int)(rgb & 0xFF));
        return true;
    }

    public static string FormatHexColor(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>
    /// Derives the palette with direct RGB blends. Brightness is approximated by blending toward
    /// white or black, and large surfaces are desaturated toward their luminance grey before being
    /// lightened. This keeps the previous visual intent without an HSL round-trip.
    /// </summary>
    private static ThemePalette CreatePalette(Color color, double opacity)
    {
        opacity = Math.Clamp(opacity, 0d, 1d);

        const float accentDarken = 0.18f;
        const float accentHoverLighten = 0.12f;
        const float accentPressedDarken = 0.28f;
        const float windowBackgroundLighten = 0.18f;
        const float contentBackgroundLighten = 0.12f;
        const float cardBackgroundLighten = 0.26f;
        const float backgroundDesaturation = 0.65f;
        const float cardDesaturation = 0.75f;

        var accent = Blend(color, Color.Black, accentDarken);
        var accentHover = Blend(color, Color.White, accentHoverLighten);
        var accentPressed = Blend(color, Color.Black, accentPressedDarken);
        var border = accent;

        // Desaturate first so the lightened panels stay muted instead of becoming vivid.
        var backgroundBase = Desaturate(color, backgroundDesaturation);
        var windowBackground = Blend(backgroundBase, Color.White, windowBackgroundLighten);
        var contentBackground = Blend(backgroundBase, Color.White, contentBackgroundLighten);
        var cardBackground = Blend(
            Desaturate(color, cardDesaturation),
            Color.White,
            cardBackgroundLighten);

        var textPrimary = TextContrast.For(windowBackground);
        var contentTextPrimary = TextContrast.For(contentBackground);
        var textSecondary = Blend(textPrimary, windowBackground, 0.35f);
        var accentText = TextContrast.For(accent);

        return new ThemePalette(
            accent,
            accentHover,
            accentPressed,
            accentText,
            border,
            windowBackground,
            contentBackground,
            cardBackground,
            textPrimary,
            contentTextPrimary,
            textSecondary,
            opacity);
    }

    internal static Color BlendOverlayColor(Color background, Color overlay)
    {
        const double overlayOpacity = 0.7d;
        return Color.FromArgb(
            (int)Math.Round(background.R * (1 - overlayOpacity) + overlay.R * overlayOpacity),
            (int)Math.Round(background.G * (1 - overlayOpacity) + overlay.G * overlayOpacity),
            (int)Math.Round(background.B * (1 - overlayOpacity) + overlay.B * overlayOpacity));
    }

    /// <summary>
    /// Blends a colour toward its perceived luminance grey. Large surfaces use this instead of an
    /// HSL saturation conversion.
    /// </summary>
    private static Color Desaturate(Color color, float amount)
    {
        var grey = (int)Math.Round((color.R * 0.299) + (color.G * 0.587) + (color.B * 0.114));
        return Blend(color, Color.FromArgb(grey, grey, grey), amount);
    }

    /// <summary>
    /// Blends two colours channel by channel: <paramref name="amount"/> 0 keeps
    /// <paramref name="from"/>, 1 returns <paramref name="to"/>. Shared by theme derivation and the
    /// native hover highlight blend.
    /// </summary>
    internal static Color Blend(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        int r = (int)(from.R * (1 - amount) + to.R * amount);
        int g = (int)(from.G * (1 - amount) + to.G * amount);
        int b = (int)(from.B * (1 - amount) + to.B * amount);
        return Color.FromArgb(r, g, b);
    }
}
