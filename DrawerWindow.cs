using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Sunny.UI;
using System.Diagnostics;
using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer
{
    public partial class DrawerWindow : Form
    {
        private readonly SettingsRepository _settingsRepository;
        private readonly DrawerDefinition _drawer;
        private readonly Size _iconSize;
        private ThemePalette _theme;
        private Size _lastPersistedSize;
        private bool _isResizing;
        private Point _resizeOffset;
        private Size _initialSize;
        private Panel? resizeGrip;

        internal DrawerWindow(SettingsRepository settingsRepository, DrawerDefinition drawer, Size iconSize, ThemePalette theme)
        {
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));
            _iconSize = NormalizeIconSize(iconSize);
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));

            InitializeComponent();
            InitializeWindow();
            ApplyTheme(_theme);
        }

        private void InitializeWindow()
        {
            DoubleBuffered = true;
            Text = _drawer.Name;
            fullPathLabel.Text = _drawer.Path;

            drawerImageList.ImageSize = _iconSize;
            // Ensure FlowLayoutPanel layout behavior at runtime (in case Designer values are missing)
            drawerContentsLayout.AutoScroll = true;
            // drawerContentsLayout.AutoScrollMargin = new Size(16, 16);
            drawerContentsLayout.FlowDirection = FlowDirection.LeftToRight;
            drawerContentsLayout.WrapContents = true;
            drawerContentsLayout.Padding = new Padding(8, 8, 8, 8);
            drawerContentsLayout.StyleCustomMode = true;

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

        public void ApplyTheme(ThemePalette theme)
        {
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));

            Opacity = _theme.WindowOpacity;
            BackColor = _theme.WindowBackground;

            mainTableLayoutPanel.BackColor = _theme.WindowBackground;
            drawerHeaderTableLayoutPanel.BackColor = Color.Transparent;
            drawerButtonTableLayoutPanel.BackColor = Color.Transparent;

            drawerContentsLayout.BackColor = _theme.ContentBackground;
            drawerContentsLayout.FillColor = _theme.ContentBackground;
            drawerContentsLayout.RectColor = Color.Transparent;
            drawerContentsLayout.ForeColor = _theme.TextPrimary;

            fullPathLabel.ForeColor = _theme.TextSecondary;
            fullPathLabel.BackColor = Color.Transparent;

            ApplyThemeToButton(refreshDrawerButton);
            ApplyThemeToButton(closeDrawerButton);
            ApplyThemeToItems();
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
            drawerContentsLayout.SuspendLayout();
            try
            {
                drawerContentsLayout.Clear();
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
                drawerContentsLayout.ResumeLayout(performLayout: true);
                drawerContentsLayout.PerformLayout();
                ApplyThemeToItems();
                drawerContentsLayout.Invalidate();
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

            const int horizontalPadding = 2;
            const int verticalPadding = 4;
            const int iconTextSpacing = 8;
            var panelWidth = _iconSize.Width + horizontalPadding * 2;

            var itemPanel = new UIPanel
            {
                Width = panelWidth,
                BackColor = _theme.CardBackground,
                FillColor = _theme.CardBackground,
                RectColor = Color.Transparent,
                Margin = new Padding(8, 6, 8, 10),
                Padding = new Padding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding),
                Tag = path
            };
            itemPanel.StyleCustomMode = true;
            itemPanel.Cursor = Cursors.Hand;

            var pb = new PictureBox
            {
                Image = img,
                Size = _iconSize,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point((panelWidth - _iconSize.Width) / 2, verticalPadding),
                BackColor = Color.Transparent,
                TabStop = false
            };

            var labelWidth = panelWidth - horizontalPadding * 2;
            var labelHeight = Math.Clamp(_iconSize.Height / 2, 32, 48);
            var lbl = new UILabel
            {
                Text = name,
                ForeColor = _theme.TextPrimary,
                BackColor = Color.Transparent,
                Font = SystemFonts.MessageBoxFont,
                AutoSize = false,
                Size = new Size(labelWidth, labelHeight),
                TextAlign = ContentAlignment.TopCenter,
                UseMnemonic = false,
                AutoEllipsis = true,
                UseCompatibleTextRendering = true
            };
            lbl.StyleCustomMode = true;
            lbl.Location = new Point(horizontalPadding, pb.Bottom + iconTextSpacing);

            itemPanel.Controls.Add(pb);
            itemPanel.Controls.Add(lbl);
            itemPanel.Height = lbl.Bottom + verticalPadding;

            drawerContentsLayout.Add(itemPanel);
        }

        private void ApplyThemeToButton(UIButton button)
        {
            if (button == null)
            {
                return;
            }

            button.FillColor = _theme.Accent;
            button.FillHoverColor = _theme.AccentHover;
            button.FillPressColor = _theme.AccentPressed;
            button.FillSelectedColor = _theme.AccentPressed;
            button.ForeColor = _theme.AccentText;
            button.RectColor = _theme.Border;
            button.RectDisableColor = _theme.Border;
            button.RectHoverColor = _theme.Border;
            button.RectPressColor = _theme.Border;
            button.RectSelectedColor = _theme.Border;
        }

        private void ApplyThemeToItems()
        {
            if (drawerContentsLayout == null)
            {
                return;
            }

            foreach (Control control in drawerContentsLayout.Controls)
            {
                if (control is not UIPanel panel)
                {
                    continue;
                }

                panel.BackColor = _theme.CardBackground;
                panel.FillColor = _theme.CardBackground;
                panel.RectColor = Color.Transparent;
                foreach (Control child in panel.Controls)
                {
                    if (child is UILabel label)
                    {
                        label.ForeColor = _theme.TextPrimary;
                    }
                }
            }
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
