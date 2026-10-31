using winDrawer.Theming;

namespace winDrawer
{
    /// <summary>
    /// Owner-drawn rendering for the drawer list. Kept separate from selection and reorder logic so
    /// the paint path can be read without scrolling through drag handling.
    /// </summary>
    public partial class MainWindow : Form
    {
        private sealed record DrawerListItem(string Text, Color OverlayColor)
        {
            public override string ToString() => Text;
        }

        private void DrawerListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= drawerListBox.Items.Count)
            {
                return;
            }

            var bounds = e.Bounds;
            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var item = drawerListBox.Items[e.Index];
            var overlayColor = item is DrawerListItem coloredDrawerItem
                ? coloredDrawerItem.OverlayColor
                : (Color?)null;

            // Compose the row background in the original order: selection or hover first, then the
            // drawer overlay. The overlay therefore stays visible on the active row as well.
            var rowBackground = selected
                ? drawerListBox.ItemSelectBackColor
                : drawerListBox.FillColor;
            if (overlayColor is { } rowOverlay)
            {
                rowBackground = ThemePalette.BlendOverlayColor(rowBackground, rowOverlay);
            }

            // Derive the text colour from the stable (non-hover) background so a hover fade cannot make
            // the text switch between dark and light mid-animation.
            var foreground = selected && overlayColor is null
                ? drawerListBox.ItemSelectForeColor
                : TextContrast.For(rowBackground);

            var background = rowBackground;
            if (!selected && drawerListBox.HoveredIndex == e.Index)
            {
                background = ThemePalette.Blend(
                    drawerListBox.FillColor,
                    drawerListBox.HoverColor,
                    (float)drawerListBox.HoverProgress);
                if (overlayColor is { } hoverOverlay)
                {
                    background = ThemePalette.BlendOverlayColor(background, hoverOverlay);
                }
            }

            using var backgroundBrush = new SolidBrush(background);
            e.Graphics.FillRectangle(backgroundBrush, bounds);

            var text = item is DrawerListItem drawerItem
                ? drawerItem.Text
                : drawerListBox.GetItemText(item);
            bounds.Inflate(-8, 0);
            TextRenderer.DrawText(
                e.Graphics,
                text,
                drawerListBox.Font,
                bounds,
                foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            e.DrawFocusRectangle();
        }
    }
}
