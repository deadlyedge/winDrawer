using System;
using System.Drawing;

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
    public Color TextSecondary { get; }
    public double WindowOpacity { get; }

    public static ThemePalette FromHsla(int hue, int saturation, int lightness, int alpha)
    {
        var clampedHue = Math.Clamp(hue, 0, 360);
        var clampedSaturation = Math.Clamp(saturation, 0, 100) / 100f;
        var clampedLightness = Math.Clamp(lightness, 0, 100) / 100f;
        var clampedAlpha = Math.Clamp(alpha, 0, 100) / 100d;

        const float borderOffset = -0.18f;
        const float hoverOffset = 0.12f;
        const float contentOffset = 0.18f;
        const float cardOffset = 0.26f;
        const float pressedOffset = -0.1f;

        var accentLightness = AdjustLightness(clampedLightness, borderOffset);
        var accentPressedLightness = AdjustLightness(accentLightness, pressedOffset);
        var hoverLightness = AdjustLightness(clampedLightness, hoverOffset);
        var contentLightness = AdjustLightness(clampedLightness, contentOffset);
        var cardLightness = AdjustLightness(clampedLightness, cardOffset);

        var accent = FromHslaInternal(clampedHue, clampedSaturation, accentLightness, 1f);
        var accentHover = FromHslaInternal(clampedHue, clampedSaturation, hoverLightness, 1f);
        var accentPressed = FromHslaInternal(clampedHue, clampedSaturation, accentPressedLightness, 1f);
        var border = Color.FromArgb(255, accent.R, accent.G, accent.B);

        // Reduce saturation for backgrounds to avoid overly vibrant panels.
        var backgroundSaturation = Math.Clamp(clampedSaturation * 0.35f, 0f, 1f);
        var contentSaturation = Math.Clamp(clampedSaturation * 0.25f, 0f, 1f);

        var windowBackground = FromHslaInternal(clampedHue, backgroundSaturation, contentLightness, 1f);
        var contentBackground = FromHslaInternal(clampedHue, backgroundSaturation, hoverLightness, 1f);
        var cardBackground = FromHslaInternal(clampedHue, contentSaturation, cardLightness, 1f);

        var textPrimary = GetReadableTextColor(windowBackground);
        var textSecondary = Blend(textPrimary, windowBackground, 0.35f);
        var accentText = GetReadableTextColor(accent);

        var opacity = Math.Clamp(clampedAlpha, 0.05d, 1d);

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
            textSecondary,
            opacity);
    }

    public static Color FromHsla(int hue, float saturation, float lightness, float alpha)
    {
        return FromHslaInternal(
            Math.Clamp(hue, 0, 360),
            Math.Clamp(saturation, 0f, 1f),
            Math.Clamp(lightness, 0f, 1f),
            Math.Clamp(alpha, 0f, 1f));
    }

    private static float AdjustLightness(float value, float delta)
    {
        return Math.Clamp(value + delta, 0f, 1f);
    }

    private static Color FromHslaInternal(int hue, float saturation, float lightness, float alpha)
    {
        if (Math.Abs(saturation) < 0.0001f)
        {
            var gray = (int)(lightness * 255f);
            return Color.FromArgb((int)(alpha * 255f), gray, gray, gray);
        }

        float q = lightness < 0.5f
            ? lightness * (1 + saturation)
            : lightness + saturation - lightness * saturation;
        float p = 2 * lightness - q;

        float r = HueToRgb(p, q, hue / 360f + 1f / 3f);
        float g = HueToRgb(p, q, hue / 360f);
        float b = HueToRgb(p, q, hue / 360f - 1f / 3f);

        return Color.FromArgb(
            (int)(alpha * 255f),
            (int)(r * 255f),
            (int)(g * 255f),
            (int)(b * 255f));
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

    private static Color GetReadableTextColor(Color background)
    {
        var luminance = GetRelativeLuminance(background);
        return luminance > 0.6
            ? Color.FromArgb(48, 48, 48)
            : Color.FromArgb(242, 242, 242);
    }

    private static double GetRelativeLuminance(Color color)
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

    private static Color Blend(Color foreground, Color background, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        int r = (int)(foreground.R * (1 - amount) + background.R * amount);
        int g = (int)(foreground.G * (1 - amount) + background.G * amount);
        int b = (int)(foreground.B * (1 - amount) + background.B * amount);
        return Color.FromArgb(r, g, b);
    }
}
