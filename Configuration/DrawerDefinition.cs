namespace winDrawer.Configuration
{
    public sealed class DrawerDefinition
    {
        public DrawerDefinition(string name, string path, Size? preferredSize, Color? overlayColor = null)
        {
            Name = name;
            Path = path;
            PreferredSize = preferredSize;
            OverlayColor = overlayColor;
        }

        public string Name { get; }
        public string Path { get; }
        public Size? PreferredSize { get; }
        public Color? OverlayColor { get; }

        public override string ToString() => Name;
    }
}
