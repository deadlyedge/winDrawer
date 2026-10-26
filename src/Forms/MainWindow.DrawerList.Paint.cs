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
            var background = drawerListBox.FillColor;
            var foreground = drawerListBox.ForeColor;
            if (selected)
            {
                background = drawerListBox.ItemSelectBackColor;
                foreground = drawerListBox.ItemSelectForeColor;
            }
            else if (drawerListBox.HoveredIndex == e.Index)
            {
                background = ThemePalette.Blend(
                    background,
                    drawerListBox.HoverColor,
                    (float)drawerListBox.HoverProgress);
            }

            var item = drawerListBox.Items[e.Index];
            if (item is DrawerListItem coloredDrawerItem)
            {
                background = ThemePalette.BlendOverlayColor(background, coloredDrawerItem.OverlayColor);
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
