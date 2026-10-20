namespace winDrawer.Configuration
{
    public sealed class DrawerDefinition(
        string name,
        string path,
        Size? preferredSize,
        Color? overlayColor = null,
        DrawerSortMode sortMode = DrawerSortMode.NewestFirst)
    {
        public string Name { get; } = name;
        public string Path { get; } = path;
        public Size? PreferredSize { get; } = preferredSize;
        public Color? OverlayColor { get; } = overlayColor;

        /// <summary>Display order of this drawer's items; persisted as <c>drawers[].sort_mode</c>.</summary>
        public DrawerSortMode SortMode { get; } = sortMode;

        /// <summary>Returns a copy with another sort mode; the instance itself stays immutable.</summary>
        public DrawerDefinition WithSortMode(DrawerSortMode sortMode)
        {
            return new DrawerDefinition(Name, Path, PreferredSize, OverlayColor, sortMode);
        }

        public override string ToString() => Name;
    }
}
