namespace winDrawer.Theming;

public sealed class ThemePreviewEventArgs(ThemePalette palette) : EventArgs
{
    public ThemePalette Palette { get; } = palette ?? throw new ArgumentNullException(nameof(palette));
}
