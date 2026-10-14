// using System.Drawing;

namespace winDrawer.Configuration
{
    public sealed class DrawerDefinition
    {
        public DrawerDefinition(string name, string path, Size? preferredSize)
        {
            Name = name;
            Path = path;
            PreferredSize = preferredSize;
        }

        public string Name { get; }
        public string Path { get; }
        public Size? PreferredSize { get; }

        public override string ToString() => Name;
    }
}
