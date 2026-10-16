using Sunny.UI;
using winDrawer.Theming;

namespace winDrawer;

/// <summary>
/// Applies the shared button colour rules. The same fill/border assignment block used to be copied
/// into the main window, the drawer window and both settings dialogs, so a new SunnyUI button
/// property had to be added in four places.
/// </summary>
internal static class UiButtonTheming
{
    /// <summary>Applies the accent palette: theme colour, readable accent text and the theme border.</summary>
    public static void ApplyAccentPalette(UIButton button, ThemePalette theme)
    {
        button.FillColor = theme.Accent;
        button.FillHoverColor = theme.AccentHover;
        button.FillPressColor = theme.AccentPressed;
        button.FillSelectedColor = theme.AccentPressed;
        button.ForeColor = theme.AccentText;
        ApplyBorderPalette(button, theme.Border);
    }

    /// <summary>Applies a fixed three-state palette together with its border colours.</summary>
    public static void ApplyColorPalette(UIButton button, Color fill, Color hover, Color pressed, Color textColor)
    {
        button.FillColor = fill;
        button.FillHoverColor = hover;
        button.FillPressColor = pressed;
        button.FillSelectedColor = pressed;
        button.ForeColor = textColor;
        button.RectColor = fill;
        button.RectHoverColor = hover;
        button.RectPressColor = pressed;
        button.RectSelectedColor = pressed;
    }

    /// <summary>Uses one border colour for every button state (accent borders).</summary>
    public static void ApplyBorderPalette(UIButton button, Color border)
    {
        button.RectColor = border;
        button.RectDisableColor = border;
        button.RectHoverColor = border;
        button.RectPressColor = border;
        button.RectSelectedColor = border;
    }
}
