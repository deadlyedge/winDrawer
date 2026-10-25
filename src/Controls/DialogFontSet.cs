namespace winDrawer;

/// <summary>
/// Owns the fonts shared by the settings dialogs and applies the default sizes to their control tree.
/// The dialogs assign <see cref="Title"/> and <see cref="PrimaryButton"/> explicitly to the few
/// controls that differ from the default.
/// </summary>
internal sealed class DialogFontSet : IDisposable
{
    private readonly List<Font> _fonts = [];

    public DialogFontSet()
    {
        Regular = Own(new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0));
        PrimaryButton = Own(BrandingFont.Create(AppConstants.Defaults.ButtonFontSize));
        CompactButton = Own(BrandingFont.Create(AppConstants.Defaults.CompactButtonFontSize));
        Title = Own(BrandingFont.Create(13F));
    }

    /// <summary>Regular UI font used by labels, inputs and group boxes.</summary>
    public Font Regular { get; }

    /// <summary>Font of the dialog's confirm and cancel buttons.</summary>
    public Font PrimaryButton { get; }

    /// <summary>Font of every other button in the dialog.</summary>
    public Font CompactButton { get; }

    /// <summary>Font of the dialog header.</summary>
    public Font Title { get; }

    /// <summary>
    /// Applies <see cref="Regular"/> to every descendant and <see cref="CompactButton"/> to buttons.
    /// </summary>
    public void ApplyTo(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);

        foreach (Control control in root.Controls)
        {
            control.Font = control is Button ? CompactButton : Regular;
            ApplyTo(control);
        }
    }

    public void Dispose()
    {
        foreach (var font in _fonts)
        {
            font.Dispose();
        }
    }

    private Font Own(Font font)
    {
        _fonts.Add(font);
        return font;
    }
}
