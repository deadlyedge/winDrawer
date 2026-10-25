using winDrawer.Theming;

namespace winDrawer;

/// <summary>
/// Applies the shared native-button colour rules. Flat buttons keep the existing theme colours and
/// hover/press feedback; rounded corners and SunnyUI-only state colours are intentionally dropped.
/// </summary>
internal static class UiButtonTheming
{
    /// <summary>Applies the accent palette: theme colour, readable accent text and the theme border.</summary>
    public static void ApplyAccentPalette(Button button, ThemePalette theme)
    {
        ApplyColorPalette(button, theme.Accent, theme.AccentHover, theme.AccentPressed, theme.AccentText);
        ApplyBorderPalette(button, theme.Border);
    }

    /// <summary>Applies a fixed three-state palette with a flat background.</summary>
    public static void ApplyColorPalette(Button button, Color fill, Color hover, Color pressed, Color textColor)
    {
        button.UseVisualStyleBackColor = false;
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = fill;
        button.ForeColor = textColor;
        button.FlatAppearance.MouseOverBackColor = hover;
        button.FlatAppearance.MouseDownBackColor = pressed;
        button.FlatAppearance.BorderColor = fill;
        button.FlatAppearance.BorderSize = 0;
    }

    /// <summary>Uses one border colour for every button state (accent borders).</summary>
    public static void ApplyBorderPalette(Button button, Color border)
    {
        button.FlatAppearance.BorderColor = border;
        button.FlatAppearance.BorderSize = 1;
    }
}
