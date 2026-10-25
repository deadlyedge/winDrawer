using winDrawer.Theming;

namespace winDrawer;

/// <summary>
/// Applies the shared native-button colour rules. Flat buttons keep the existing theme colours and
/// hover/press feedback; rounded corners and SunnyUI-only state colours are intentionally dropped.
/// </summary>
internal static class UiButtonTheming
{
    /// <summary>Applies the confirm-button palette used by the settings dialogs.</summary>
    public static void ApplyPrimaryPalette(Button button, bool handCursor = false)
    {
        ApplyDialogPalette(button, Color.SeaGreen, Color.MediumSeaGreen, Color.DarkGreen, handCursor);
    }

    /// <summary>Applies the neutral palette used by secondary and tool buttons in the dialogs.</summary>
    public static void ApplySecondaryPalette(Button button, bool handCursor = false)
    {
        ApplyDialogPalette(button, Color.SteelBlue, Color.CornflowerBlue, Color.DarkSlateBlue, handCursor);
    }

    /// <summary>Applies the cancel/abort palette used by the settings dialogs.</summary>
    public static void ApplyDangerPalette(Button button, bool handCursor = false)
    {
        ApplyDialogPalette(button, Color.Firebrick, Color.IndianRed, Color.DarkRed, handCursor);
    }

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

    private static void ApplyDialogPalette(Button button, Color fill, Color hover, Color pressed, bool handCursor)
    {
        ApplyColorPalette(button, fill, hover, pressed, Color.White);
        if (handCursor)
        {
            button.Cursor = Cursors.Hand;
        }
    }
}
