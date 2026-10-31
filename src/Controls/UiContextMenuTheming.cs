using winDrawer.Theming;

namespace winDrawer;

/// <summary>
/// Applies the application colour palette to a <see cref="ContextMenuStrip"/>. WinForms derives menu
/// colours from the renderer's colour table, so the palette is supplied as a table; the renderer also
/// keeps the item text readable on both the panel background and the accent-filled selected state.
/// </summary>
internal static class UiContextMenuTheming
{
    public static void Apply(ContextMenuStrip menu, ThemePalette theme)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(theme);

        menu.RenderMode = ToolStripRenderMode.Professional;
        menu.BackColor = theme.ContentBackground;
        menu.ForeColor = theme.ContentTextPrimary;
        menu.Renderer = new PaletteRenderer(new PaletteColorTable(theme), theme);

        foreach (ToolStripItem item in menu.Items)
        {
            item.BackColor = theme.ContentBackground;
            item.ForeColor = theme.ContentTextPrimary;
        }
    }

    private sealed class PaletteColorTable : ProfessionalColorTable
    {
        private readonly ThemePalette _theme;

        public PaletteColorTable(ThemePalette theme)
        {
            _theme = theme;
        }

        public override Color ToolStripDropDownBackground => _theme.ContentBackground;

        public override Color MenuBorder => _theme.Border;

        public override Color MenuItemBorder => _theme.Border;

        public override Color MenuItemSelected => _theme.Accent;

        public override Color MenuItemSelectedGradientBegin => _theme.Accent;

        public override Color MenuItemSelectedGradientEnd => _theme.Accent;

        public override Color MenuItemPressedGradientBegin => _theme.AccentPressed;

        public override Color MenuItemPressedGradientMiddle => _theme.AccentPressed;

        public override Color MenuItemPressedGradientEnd => _theme.AccentPressed;

        public override Color ImageMarginGradientBegin => _theme.ContentBackground;

        public override Color ImageMarginGradientMiddle => _theme.ContentBackground;

        public override Color ImageMarginGradientEnd => _theme.ContentBackground;

        public override Color SeparatorDark => _theme.Border;

        public override Color SeparatorLight => _theme.Border;
    }

    private sealed class PaletteRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemePalette _theme;

        public PaletteRenderer(PaletteColorTable colorTable, ThemePalette theme)
            : base(colorTable)
        {
            _theme = theme;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected && e.Item.Enabled
                ? TextContrast.For(_theme.Accent)
                : _theme.ContentTextPrimary;
            base.OnRenderItemText(e);
        }

        /// <summary>
        /// The default check mark uses a fixed colour that disappears on the accent fill, so it is drawn
        /// with the same colour the adjacent text uses.
        /// </summary>
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var bounds = e.ImageRectangle;
            if (bounds.Width <= 2 || bounds.Height <= 2)
            {
                return;
            }

            var color = e.Item.Selected ? TextContrast.For(_theme.Accent) : _theme.ContentTextPrimary;
            var left = bounds.Left + 1;
            var middle = bounds.Left + (bounds.Width / 3);
            var right = bounds.Right - 1;
            var top = bounds.Top + 2;
            var bottom = bounds.Bottom - 3;

            using var pen = new Pen(color, 2f);
            e.Graphics.DrawLines(pen,
            [
                new Point(left, (top + bottom) / 2),
                new Point(middle, bottom),
                new Point(right, top)
            ]);
        }
    }
}
