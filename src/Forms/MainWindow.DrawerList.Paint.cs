using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer
{
    /// <summary>
    /// Owner-drawn rendering for the drawer list. Kept separate from selection and reorder logic so
    /// the paint path can be read without scrolling through drag handling.
    /// </summary>
    public partial class MainWindow : Form
    {
        /// <summary>
        /// One drawer row with its precomputed colours. They depend only on the current theme and the
        /// drawer's overlay colour, so they are calculated when the list is populated or the theme
        /// changes instead of on every paint (a hover only invalidates the affected rows once).
        /// </summary>
        private sealed class DrawerListItem(string text)
        {
            public string Text { get; set; } = text;
            public Color Background { get; set; }
            public Color Foreground { get; set; }
            public Color SelectedBackground { get; set; }
            public Color SelectedForeground { get; set; }
            public Color HoverBackground { get; set; }

            public override string ToString() => Text;
        }

        private DrawerListItem CreateDrawerListItem(DrawerDefinition drawer)
        {
            var item = new DrawerListItem(string.Empty);
            ApplyDrawerItemColors(item, drawer);
            return item;
        }

        private void ApplyDrawerItemColors(DrawerListItem item, DrawerDefinition drawer)
        {
            var displayName = string.IsNullOrWhiteSpace(drawer.Name)
                ? "(unnamed drawer)"
                : drawer.Name;
            item.Text = Directory.Exists(drawer.Path)
                ? displayName
                : $"{displayName} (missing)";

            var background = drawerListBox.FillColor;
            var selectedBackground = drawerListBox.ItemSelectBackColor;
            var hoverBackground = drawerListBox.HoverColor;
            var foreground = drawerListBox.ForeColor;
            var selectedForeground = drawerListBox.ItemSelectForeColor;

            if (drawer.OverlayColor is { } overlay)
            {
                background = ThemePalette.BlendOverlayColor(background, overlay);
                selectedBackground = ThemePalette.BlendOverlayColor(selectedBackground, overlay);
                hoverBackground = ThemePalette.BlendOverlayColor(hoverBackground, overlay);
                foreground = TextContrast.For(background);
                selectedForeground = TextContrast.For(selectedBackground);
            }

            item.Background = background;
            item.Foreground = foreground;
            item.SelectedBackground = selectedBackground;
            item.SelectedForeground = selectedForeground;
            item.HoverBackground = hoverBackground;
        }

        /// <summary>
        /// Updates the cached row colours after the theme changed. Items are updated in place so the
        /// selection is untouched, and the list is repainted once instead of row by row.
        /// </summary>
        private void RefreshDrawerItemColors()
        {
            var changed = false;
            for (var index = 0; index < _currentDrawers.Count && index < drawerListBox.Items.Count; index++)
            {
                if (drawerListBox.Items[index] is DrawerListItem item)
                {
                    ApplyDrawerItemColors(item, _currentDrawers[index]);
                    changed = true;
                }
            }

            if (changed)
            {
                drawerListBox.Invalidate();
            }
        }

        private void DrawerListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= drawerListBox.Items.Count)
            {
                return;
            }

            var bounds = e.Bounds;
            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var hovered = !selected && drawerListBox.HoveredIndex == e.Index;
            var item = drawerListBox.Items[e.Index];
            Color background;
            Color foreground;

            if (item is DrawerListItem drawerItem)
            {
                // Every row colour was precomputed and the hover state switches instantly, so a hover
                // never starts a per-frame repaint of the native list.
                if (selected)
                {
                    background = drawerItem.SelectedBackground;
                    foreground = drawerItem.SelectedForeground;
                }
                else
                {
                    background = hovered ? drawerItem.HoverBackground : drawerItem.Background;
                    foreground = drawerItem.Foreground;
                }
            }
            else if (selected)
            {
                background = drawerListBox.ItemSelectBackColor;
                foreground = drawerListBox.ItemSelectForeColor;
            }
            else
            {
                background = hovered ? drawerListBox.HoverColor : drawerListBox.FillColor;
                foreground = drawerListBox.ForeColor;
            }

            using var backgroundBrush = new SolidBrush(background);
            e.Graphics.FillRectangle(backgroundBrush, bounds);

            var text = item is DrawerListItem drawerListItem
                ? drawerListItem.Text
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
