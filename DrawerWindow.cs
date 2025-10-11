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
        private bool _isResizing;
        private Point _resizeOffset;
        private Size _initialSize;
        private Panel? resizeGrip;

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

            refreshDrawerButton.Click += (_, _) => PopulateFileList();
            closeDrawerButton.Click += (_, _) => Close();

            drawerHeaderTableLayoutPanel.MouseDown += Header_MouseDown;

            resizeGrip = new Panel
            {
                Size = new Size(30, 44),
                BackColor = Color.Transparent,
                Cursor = Cursors.SizeNWSE
            };
            resizeGrip.MouseDown += ResizeGrip_MouseDown;
            resizeGrip.MouseMove += ResizeGrip_MouseMove;
            resizeGrip.MouseUp += ResizeGrip_MouseUp;
            drawerButtonTableLayoutPanel.Controls.Add(resizeGrip, 2, 0);

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
            drawerContentsListView.SuspendLayout();
            try
            {
                drawerContentsListView.Controls.Clear();
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
                drawerContentsListView.ResumeLayout();
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

            var img = drawerImageList.Images[key];

            var itemPanel = new Panel
            {
                Width = _iconSize.Width + 12,
                Height = _iconSize.Height + 28,
                BackColor = Color.FromArgb(56, 56, 56),
                Margin = new Padding(6)
            };

            var pb = new PictureBox
            {
                Image = img,
                Size = _iconSize,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(6, 4)
            };

            var lbl = new Label
            {
                Text = name,
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = itemPanel.Width,
                Height = 20,
                Location = new Point(0, _iconSize.Height + 6)
            };

            itemPanel.Tag = path;
            itemPanel.Controls.Add(pb);
            itemPanel.Controls.Add(lbl);

            drawerContentsListView.Controls.Add(itemPanel);
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

        private void fullPathLabel_Click(object sender, EventArgs e)
        {

        }

        private void ResizeGrip_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _isResizing = true;
            _resizeOffset = e.Location;
            _initialSize = this.Size;
            SuspendLayout();
        }

        private void ResizeGrip_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isResizing)
            {
                return;
            }

            var deltaX = e.X - _resizeOffset.X;
            var deltaY = e.Y - _resizeOffset.Y;
            var newWidth = Math.Max(MinimumSize.Width, _initialSize.Width + deltaX);
            var newHeight = Math.Max(MinimumSize.Height, _initialSize.Height + deltaY);
            this.Size = new Size(newWidth, newHeight);
        }

        private void ResizeGrip_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _isResizing = false;
            ResumeLayout(performLayout: true);
            PersistWindowSize();
        }
    }
}
