// using System;

namespace winDrawer.Theming;

public sealed class ThemePreviewEventArgs : EventArgs
{
    public ThemePreviewEventArgs(ThemePalette palette)
    {
        Palette = palette ?? throw new ArgumentNullException(nameof(palette));
    }

    public ThemePalette Palette { get; }
}
