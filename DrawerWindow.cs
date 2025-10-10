using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using winDrawer.Configuration;

namespace winDrawer
{
    public partial class DrawerWindow : Form
    {
        private readonly SettingsRepository _settingsRepository;
        private readonly DrawerDefinition _drawer;
        private readonly Size _iconSize;
        private Size _lastPersistedSize;

        internal DrawerWindow(SettingsRepository settingsRepository, DrawerDefinition drawer, Size iconSize)
        {
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));
            _iconSize = NormalizeIconSize(iconSize);

            InitializeComponent();
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            DoubleBuffered = true;
            Text = _drawer.Name;
            fullPathLabel.Text = _drawer.Path;

            drawerImageList.ImageSize = _iconSize;
            drawerContentsListView.LargeImageList = drawerImageList;
            drawerContentsListView.SmallImageList = drawerImageList;

            refreshDrawerButton.Click += (_, _) => PopulateFileList();
            closeDrawerButton.Click += (_, _) => Close();

            drawerHeaderTableLayoutPanel.MouseDown += Header_MouseDown;
            fullPathLabel.MouseDown += Header_MouseDown;

            Size = _drawer.PreferredSize ?? Size;
            _lastPersistedSize = Size;

            PopulateFileList();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_drawer.PreferredSize is { Width: > 0, Height: > 0 })
            {
                Size = _drawer.PreferredSize.Value;
                _lastPersistedSize = Size;
            }
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            PersistWindowSize();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            PersistWindowSize();
            base.OnFormClosed(e);
        }

        private void PopulateFileList()
        {
            drawerContentsListView.BeginUpdate();
            try
            {
                drawerContentsListView.Items.Clear();
                drawerImageList.Images.Clear();
                fullPathLabel.Text = _drawer.Path;

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

        private void PersistWindowSize()
        {
            if (WindowState != FormWindowState.Normal)
            {
                return;
            }

            if (Size == _lastPersistedSize)
            {
                return;
            }

            if (_settingsRepository.UpdateDrawerSize(_drawer.Path, Size))
            {
                _settingsRepository.Save();
                _lastPersistedSize = Size;
            }
        }

        private static Size NormalizeIconSize(Size size)
        {
            var width = Math.Clamp(size.Width, 32, 512);
            var height = Math.Clamp(size.Height, 32, 512);
            return new Size(width, height);
        }

        private void Header_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, WmNclbuttondown, HtCaption, 0);
        }

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WmNclbuttondown = 0xA1;
        private const int HtCaption = 0x2;
    }
}
