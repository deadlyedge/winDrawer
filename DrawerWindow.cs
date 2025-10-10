using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using winDrawer.Configuration;

namespace winDrawer
{
    public partial class DrawerWindow : Form
    {
        private readonly DrawerDefinition? _drawer;

        public DrawerWindow()
        {
            InitializeComponent();
        }

        public DrawerWindow(DrawerDefinition drawer)
            : this()
        {
            _drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));
            Text = drawer.Name;
            fullPathLabel.Text = drawer.Path;
            PopulateFileList();
        }

        private void PopulateFileList()
        {
            if (_drawer == null)
            {
                return;
            }

            fullPathLabel.Text = _drawer.Path;
            drawerContentsListView.BeginUpdate();
            try
            {
                drawerContentsListView.Items.Clear();
                drawerImageList.Images.Clear();

                if (!Directory.Exists(_drawer.Path))
                {
                    fullPathLabel.Text = $"{_drawer.Path} (missing)";
                    return;
                }

                foreach (var directory in Directory.EnumerateDirectories(_drawer.Path)
                             .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
                {
                    AddItem(directory, true);
                }

                foreach (var file in Directory.EnumerateFiles(_drawer.Path)
                             .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
                {
                    AddItem(file, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load drawer contents: {ex.Message}", "Drawer Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                drawerContentsListView.EndUpdate();
            }
        }

        private void AddItem(string path, bool isDirectory)
        {
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name))
            {
                name = path;
            }

            var key = $"{(isDirectory ? "D" : "F")}::{path}";
            if (!drawerImageList.Images.ContainsKey(key))
            {
                using var icon = ShellIcon.GetIcon(path, isDirectory);
                var bitmap = (icon ?? SystemIcons.WinLogo).ToBitmap();
                drawerImageList.Images.Add(key, bitmap);
            }

            var item = new ListViewItem(name)
            {
                ImageKey = key,
                Tag = path
            };

            drawerContentsListView.Items.Add(item);
        }
    }
}
