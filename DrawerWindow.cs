using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".bmp",
            ".gif",
            ".ico",
            ".jpeg",
            ".jpg",
            ".jfif",
            ".png",
            ".tif",
            ".tiff",
            ".webp"
        };

        private const float ItemHoverBlendAmount = 0.28f;

        private sealed record DrawerItemInfo(string Path, bool IsDirectory);

        private const int ContentHorizontalPadding = 2;
        private const int ContentVerticalPadding = 8;

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
            drawerContentsLayout.Padding = CreateContentPadding(reserveScrollArea: false);
            drawerContentsLayout.StyleCustomMode = true;
            drawerContentsLayout.SizeChanged += (_, _) => UpdateContentPaddingForScrollBar();
            drawerContentsLayout.ControlAdded += (_, _) => UpdateContentPaddingForScrollBar();
            drawerContentsLayout.ControlRemoved += (_, _) => UpdateContentPaddingForScrollBar();
            drawerContentsLayout.Layout += (_, _) => UpdateContentPaddingForScrollBar();

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
            drawerContentsLayout.ScrollBarBackColor = _theme.ContentBackground;
            drawerContentsLayout.ScrollBarColor = BlendColors(_theme.Accent, _theme.AccentHover, 0.3f);
            // drawerContentsLayout.ScrollBarColor = BlendColors(_theme.ContentBackground, _theme.AccentHover, 0.3f);

            fullPathLabel.ForeColor = _theme.TextSecondary;
            fullPathLabel.BackColor = Color.Transparent;

            ApplyThemeToButton(refreshDrawerButton);
            ApplyThemeToButton(closeDrawerButton);
            ApplyThemeToItems();
            UpdateContentPaddingForScrollBar();
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
                UpdateContentPaddingForScrollBar();
                ApplyThemeToItems();
                drawerContentsLayout.Invalidate();
            }
        }

        private void AddItem(string path, bool isDirectory)
        {
            var layout = drawerContentsLayout ?? throw new InvalidOperationException("Drawer contents layout is not initialized.");
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name))
            {
                name = path;
            }

            var key = $"{(isDirectory ? "D" : "F")}::{path}";
            if (!drawerImageList.Images.ContainsKey(key))
            {
                drawerImageList.Images.Add(key, GetItemImage(path, isDirectory));
            }

            var img = drawerImageList.Images[key];

            const int horizontalPadding = 4;
            const int verticalPadding = 6;
            const int iconTextSpacing = 10;
            var panelWidth = _iconSize.Width + horizontalPadding * 2;
            var itemInfo = new DrawerItemInfo(path, isDirectory);
            var baseItemColor = GetItemBaseColor();

            var itemPanel = new UIPanel();
            itemPanel.StyleCustomMode = true;
            itemPanel.Width = panelWidth;
            itemPanel.BackColor = baseItemColor;
            itemPanel.FillColor = baseItemColor;
            itemPanel.RectColor = Color.Transparent;
            itemPanel.Margin = new Padding(8, 4, 8, 4);
            itemPanel.Padding = new Padding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
            itemPanel.Tag = itemInfo;
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
            var messageBoxFont = SystemFonts.MessageBoxFont ?? throw new InvalidOperationException("System message box font is unavailable.");
            var labelFontSize = Math.Max(6f, messageBoxFont.SizeInPoints - 0.5f);
            var labelFont = new Font(messageBoxFont.FontFamily, labelFontSize, FontStyle.Regular, GraphicsUnit.Point);
            var lineHeight = TextRenderer.MeasureText("Ag", labelFont).Height;
            var labelHeight = lineHeight * 4;
            var lbl = new UILabel();
            lbl.StyleCustomMode = true;
            lbl.Text = name;
            lbl.ForeColor = _theme.TextPrimary;
            lbl.BackColor = Color.Transparent;
            lbl.Font = labelFont;
            lbl.AutoSize = false;
            lbl.Size = new Size(labelWidth, labelHeight);
            lbl.TextAlign = ContentAlignment.TopCenter;
            lbl.UseMnemonic = false;
            lbl.AutoEllipsis = true;
            lbl.UseCompatibleTextRendering = true;
            lbl.Location = new Point(horizontalPadding, pb.Bottom + iconTextSpacing);

            itemPanel.Controls.Add(pb);
            itemPanel.Controls.Add(lbl);
            itemPanel.Height = lbl.Bottom + verticalPadding;

            AttachItemInteractions(itemPanel, pb, lbl);

            layout.Add(itemPanel);
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

                HighlightItemPanel(panel, highlight: false);
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

        private void AttachItemInteractions(UIPanel panel, params Control[] children)
        {
            panel.MouseEnter += ItemPanel_MouseEnter;
            panel.MouseLeave += ItemPanel_MouseLeave;
            panel.Click += ItemPanel_Click;

            foreach (var child in children)
            {
                child.MouseEnter += ItemChild_MouseEnter;
                child.MouseLeave += ItemChild_MouseLeave;
                child.Click += ItemChild_Click;
            }
        }

        private void ItemPanel_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is UIPanel panel)
            {
                HighlightItemPanel(panel, highlight: true);
            }
        }

        private void ItemPanel_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is UIPanel panel && !IsCursorWithinPanel(panel))
            {
                HighlightItemPanel(panel, highlight: false);
            }
        }

        private void ItemChild_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Control control && control.Parent is UIPanel panel)
            {
                HighlightItemPanel(panel, highlight: true);
            }
        }

        private void ItemChild_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Control control && control.Parent is UIPanel panel && !IsCursorWithinPanel(panel))
            {
                HighlightItemPanel(panel, highlight: false);
            }
        }

        private void ItemPanel_Click(object? sender, EventArgs e)
        {
            if (sender is UIPanel panel && panel.Tag is DrawerItemInfo info)
            {
                OpenItem(info);
            }
        }

        private void ItemChild_Click(object? sender, EventArgs e)
        {
            if (sender is Control control && control.Parent is UIPanel panel && panel.Tag is DrawerItemInfo info)
            {
                OpenItem(info);
            }
        }

        private void OpenItem(DrawerItemInfo info)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = info.Path,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to open {info.Path}: {ex.Message}", "Open Failed", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private Image GetItemImage(string path, bool isDirectory)
        {
            if (isDirectory)
            {
                using var icon = ShellIcon.GetIcon(path, true);
                return (icon ?? SystemIcons.WinLogo).ToBitmap();
            }

            if (IsImageFile(path))
            {
                var thumbnail = TryCreateThumbnail(path, _iconSize);
                if (thumbnail != null)
                {
                    return thumbnail;
                }
            }

            using var fileIcon = ShellIcon.GetIcon(path, false);
            return (fileIcon ?? SystemIcons.WinLogo).ToBitmap();
        }

        private static bool IsImageFile(string path)
        {
            var extension = Path.GetExtension(path);
            return !string.IsNullOrEmpty(extension) && ImageExtensions.Contains(extension);
        }

        private static Image? TryCreateThumbnail(string path, Size targetSize)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var original = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);

                if (original.Width <= 0 || original.Height <= 0)
                {
                    return null;
                }

                var ratio = Math.Min(
                    (float)targetSize.Width / original.Width,
                    (float)targetSize.Height / original.Height);

                var scaledWidth = Math.Max(1, (int)Math.Round(original.Width * ratio));
                var scaledHeight = Math.Max(1, (int)Math.Round(original.Height * ratio));

                var bitmap = new Bitmap(targetSize.Width, targetSize.Height);
                bitmap.SetResolution(original.HorizontalResolution, original.VerticalResolution);

                using var graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                var x = (targetSize.Width - scaledWidth) / 2;
                var y = (targetSize.Height - scaledHeight) / 2;
                var destRect = new Rectangle(x, y, scaledWidth, scaledHeight);
                graphics.DrawImage(original, destRect);

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private void HighlightItemPanel(UIPanel panel, bool highlight)
        {
            var baseColor = GetItemBaseColor();
            var hoverColor = BlendColors(baseColor, _theme.AccentHover, ItemHoverBlendAmount);
            var targetColor = highlight ? hoverColor : baseColor;

            if (panel.BackColor == targetColor && panel.FillColor == targetColor)
            {
                return;
            }

            panel.BackColor = targetColor;
            panel.FillColor = targetColor;
        }

        private static bool IsCursorWithinPanel(UIPanel panel)
        {
            var cursorPoint = panel.PointToClient(Cursor.Position);
            return panel.ClientRectangle.Contains(cursorPoint);
        }

        private Color GetItemBaseColor()
        {
            return _theme.ContentBackground;
        }

        private void UpdateContentPaddingForScrollBar()
        {
            if (drawerContentsLayout == null)
            {
                return;
            }

            var layout = drawerContentsLayout;
            var displayRect = layout.DisplayRectangle;
            var clientSize = layout.ClientSize;
            var reserveScrollArea = displayRect.Height - clientSize.Height > 1;
            var newPadding = CreateContentPadding(reserveScrollArea);

            if (layout.Padding == newPadding)
            {
                return;
            }

            layout.Padding = newPadding;
        }

        private Padding CreateContentPadding(bool reserveScrollArea)
        {
            var layout = drawerContentsLayout;
            var scrollWidth = reserveScrollArea
                ? Math.Max(SystemInformation.VerticalScrollBarWidth, layout?.ScrollBarWidth ?? SystemInformation.VerticalScrollBarWidth)
                : 0;

            return new Padding(
                ContentHorizontalPadding,
                ContentVerticalPadding,
                ContentHorizontalPadding + scrollWidth,
                ContentVerticalPadding);
        }

        private static Color BlendColors(Color background, Color foreground, float amount)
        {
            amount = Math.Clamp(amount, 0f, 1f);
            var r = (int)(background.R * (1 - amount) + foreground.R * amount);
            var g = (int)(background.G * (1 - amount) + foreground.G * amount);
            var b = (int)(background.B * (1 - amount) + foreground.B * amount);
            return Color.FromArgb(r, g, b);
        }
    }
}
