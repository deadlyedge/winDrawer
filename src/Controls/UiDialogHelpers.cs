namespace winDrawer;

/// <summary>
/// Helpers shared by the two settings dialogs: the colour-dialog preset palette and the Enter/Space
/// activation rule were each implemented twice.
/// </summary>
internal static class UiDialogHelpers
{
    private static readonly Color[] PresetColors =
    [
        Color.FromArgb(9, 140, 210),
        Color.FromArgb(64, 64, 64),
        Color.White,
        Color.Black,
        Color.Red,
        Color.Green,
        Color.Blue,
        Color.Yellow,
        Color.Magenta,
        Color.Cyan,
        Color.Orange,
        Color.Purple,
        Color.LightGray,
        Color.DarkGray
    ];

    /// <summary>
    /// Builds the custom colour row offered by the colour dialogs: the active colour first, then an
    /// optional second colour (falling back to the default theme colour) and finally the shared preset
    /// palette. <see cref="ColorDialog.CustomColors"/> expects 0x00BBGGRR values.
    /// </summary>
    public static int[] BuildColorDialogCustomColors(Color primary, Color? secondary = null)
    {
        var colors = new List<int>
        {
          ToCustomColor(primary),
          ToCustomColor(secondary ?? AppConstants.Defaults.ThemeColor)
        };
        foreach (var preset in PresetColors)
        {
            colors.Add(ToCustomColor(preset));
        }

        return [.. colors];
    }

    /// <summary>
    /// Shows a system colour dialog configured like the rest of the settings UI and returns the chosen
    /// colour, or <see langword="null"/> when the user cancelled. The dialog is created and disposed per
    /// call, so callers do not keep a long-lived instance alive.
    /// </summary>
    public static Color? ShowColorDialog(IWin32Window owner, Color initialColor, Color? secondaryColor = null)
    {
        using var dialog = new ColorDialog
        {
            Color = initialColor,
            AllowFullOpen = true,
            FullOpen = true,
            AnyColor = true,
            SolidColorOnly = true,
            CustomColors = BuildColorDialogCustomColors(initialColor, secondaryColor)
        };

        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.Color : null;
    }

    /// <summary>
    /// Runs <paramref name="activate"/> when Enter or Space was pressed and consumes the key so that a
    /// display-only control behaves like a button.
    /// </summary>
    public static void HandleActivationKey(KeyEventArgs e, Action activate)
    {
        if (e.KeyCode is not Keys.Enter and not Keys.Space)
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        activate();
    }

    private static int ToCustomColor(Color color) => color.R | (color.G << 8) | (color.B << 16);
}
