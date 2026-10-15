namespace winDrawer.Configuration
{
    public sealed class DrawerDefinition(string name, string path, Size? preferredSize, Color? overlayColor = null)
    {
        public string Name { get; } = name;
        public string Path { get; } = path;
        public Size? PreferredSize { get; } = preferredSize;
        public Color? OverlayColor { get; } = overlayColor;

        public override string ToString() => Name;
    }
}
