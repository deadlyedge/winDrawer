using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using Sunny.UI;
using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer
{
    public partial class DrawerWindow : Form
    {
        private readonly SettingsRepository _settingsRepository;
        private readonly DrawerDefinition _drawer;
        private readonly List<Image> _imageListImages = new();
        private ThemePalette _theme;
        private Size _lastPersistedSize;

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

        private sealed record DrawerItemInfo(string Path, bool IsDirectory);

        internal DrawerWindow(SettingsRepository settingsRepository, DrawerDefinition drawer, ThemePalette theme)
        {
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));

            DrawerLogger.LogInformation($"Initializing DrawerWindow for '{_drawer.Name}' at '{_drawer.Path}'.");

            InitializeComponent();
            InitializeWindow();
            ApplyTheme(_theme);
        }

        private void InitializeWindow()
        {
            DoubleBuffered = true;
            Text = _drawer.Name;
            fullPathLabel.Text = _drawer.Path;
            DrawerLogger.LogDebug($"Setting up DrawerWindow UI for '{_drawer.Name}'.");

            drawerImageList.ImageSize = GetImageSize(DeviceDpi);
            drawerContentsListView.LargeImageList = drawerImageList;
            drawerContentsListView.MouseDoubleClick += DrawerContentsListView_MouseDoubleClick;
            DpiChanged += DrawerWindow_DpiChanged;

            refreshDrawerButton.Click += (_, _) => PopulateFileList();
            closeDrawerButton.Click += (_, _) => Close();

            Size = _drawer.PreferredSize ?? Size;
            _lastPersistedSize = Size;

            PopulateFileList();
            DrawerLogger.LogInformation($"DrawerWindow ready for '{_drawer.Name}'. Item count: {drawerContentsListView.Items.Count}.");
        }

        public void ApplyTheme(ThemePalette theme)
        {
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));
            DrawerLogger.LogDebug($"Applying theme to drawer '{_drawer.Name}'.");

            Opacity = _theme.WindowOpacity;
            BackColor = _theme.WindowBackground;

            mainTableLayoutPanel.BackColor = _theme.WindowBackground;
            drawerHeaderTableLayoutPanel.BackColor = Color.Transparent;
            drawerButtonTableLayoutPanel.BackColor = Color.Transparent;

            drawerContentsListView.BackColor = _theme.ContentBackground;
            drawerContentsListView.ForeColor = _theme.TextPrimary;

            fullPathLabel.ForeColor = _theme.TextSecondary;
            fullPathLabel.BackColor = Color.Transparent;

            ApplyThemeToButton(refreshDrawerButton);
            ApplyThemeToButton(closeDrawerButton);
        }

        private void DrawerWindow_DpiChanged(object? sender, DpiChangedEventArgs e)
        {
            drawerContentsListView.BeginUpdate();
            try
            {
                drawerContentsListView.Items.Clear();
                drawerImageList.Images.Clear();
                DisposeImageListImages();
                drawerImageList.ImageSize = GetImageSize(e.DeviceDpiNew);
            }
            finally
            {
                drawerContentsListView.EndUpdate();
            }

            PopulateFileList();
        }

        private static Size GetImageSize(int dpi)
        {
            const int logicalImageSize = 48;
            var scaledSize = Math.Max(1, (int)Math.Round(logicalImageSize * dpi / 96d));
            return new Size(scaledSize, scaledSize);
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
            DrawerLogger.LogDebug($"Resize ended for '{_drawer.Name}' at size {Size}.");
            PersistWindowSize();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            DrawerLogger.LogInformation($"DrawerWindow closing for '{_drawer.Name}'.");
            PersistWindowSize();
            base.OnFormClosed(e);
        }

        private void PopulateFileList()
        {
            DrawerLogger.LogInformation($"Refreshing drawer contents for '{_drawer.Name}'.");
            drawerContentsListView.BeginUpdate();
            int directoryCount = 0;
            int fileCount = 0;
            try
            {
                drawerContentsListView.Items.Clear();
                drawerImageList.Images.Clear();
                DisposeImageListImages();
                fullPathLabel.Text = _drawer.Path;

                if (!Directory.Exists(_drawer.Path))
                {
                    fullPathLabel.Text = $"{_drawer.Path} (missing)";
                    DrawerLogger.LogWarning($"Drawer path missing for '{_drawer.Name}': {_drawer.Path}");
                    return;
                }

                foreach (var directory in Directory.EnumerateDirectories(_drawer.Path)
                             .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
                {
                    AddItem(directory, isDirectory: true);
                    directoryCount++;
                }

                foreach (var file in Directory.EnumerateFiles(_drawer.Path)
                             .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase))
                {
                    AddItem(file, isDirectory: false);
                    fileCount++;
                }

                DrawerLogger.LogInformation($"Drawer '{_drawer.Name}' loaded {directoryCount} directories and {fileCount} files.");
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError($"Failed to populate drawer '{_drawer.Name}'.", ex);
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

            var imageKey = drawerImageList.Images.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            using var image = GetItemImage(path, isDirectory);
            var imageListCopy = (Image)image.Clone();
            try
            {
                drawerImageList.Images.Add(imageKey, imageListCopy);
                _imageListImages.Add(imageListCopy);
            }
            catch
            {
                imageListCopy.Dispose();
                throw;
            }

            var item = new ListViewItem(name, imageKey)
            {
                ToolTipText = path,
                Tag = new DrawerItemInfo(path, isDirectory)
            };

            drawerContentsListView.Items.Add(item);
        }

        private void DisposeImageListImages()
        {
            foreach (var image in _imageListImages)
            {
                image.Dispose();
            }

            _imageListImages.Clear();
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

        private void PersistWindowSize()
        {
            if (WindowState != FormWindowState.Normal)
            {
                DrawerLogger.LogDebug($"Skipping persist for '{_drawer.Name}' because window state is {WindowState}.");
                return;
            }

            if (Size == _lastPersistedSize)
            {
                DrawerLogger.LogDebug($"Drawer '{_drawer.Name}' size unchanged ({Size}).");
                return;
            }

            if (_settingsRepository.UpdateDrawerSize(_drawer.Path, Size))
            {
                _lastPersistedSize = Size;
                DrawerLogger.LogInformation($"Persisted drawer '{_drawer.Name}' size {Size}.");
            }
            else
            {
                DrawerLogger.LogWarning($"Failed to persist drawer '{_drawer.Name}' size {Size}.");
            }
        }

        private void DrawerContentsListView_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            var clickedItem = drawerContentsListView.HitTest(e.Location).Item;
            if (clickedItem?.Tag is not DrawerItemInfo item)
            {
                return;
            }

            OpenItem(item);
        }

        private void OpenItem(DrawerItemInfo info)
        {
            try
            {
                var itemType = info.IsDirectory ? "folder" : "file";
                DrawerLogger.LogInformation($"Opening {itemType} '{info.Path}'.");
                var startInfo = new ProcessStartInfo
                {
                    FileName = info.Path,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError($"Failed to open '{info.Path}'.", ex);
                MessageBox.Show(this, $"Failed to open {info.Path}: {ex.Message}", "Open Failed", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private Image GetItemImage(string path, bool isDirectory)
        {
            if (isDirectory)
            {
                return GetShellIconImage(path, isDirectory: true);
            }

            if (IsImageFile(path))
            {
                var thumbnail = TryCreateThumbnail(path, drawerImageList.ImageSize);
                if (thumbnail != null)
                {
                    return thumbnail;
                }
            }

            return GetShellIconImage(path, isDirectory: false);
        }

        private static Image GetShellIconImage(string path, bool isDirectory)
        {
            try
            {
                using var icon = ShellIcon.GetIcon(path, isDirectory);
                return (icon ?? SystemIcons.WinLogo).ToBitmap();
            }
            catch (Exception ex)
            {
                DrawerLogger.LogDebug($"Shell icon lookup failed for '{path}'. {ex}");
                return SystemIcons.WinLogo.ToBitmap();
            }
        }

        private static bool IsImageFile(string path)
        {
            var extension = Path.GetExtension(path);
            return !string.IsNullOrEmpty(extension) && ImageExtensions.Contains(extension);
        }

        private static Image? TryCreateThumbnail(string path, Size targetSize)
        {
            Bitmap? bitmap = null;
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

                bitmap = new Bitmap(targetSize.Width, targetSize.Height);

                using var graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                var x = (targetSize.Width - scaledWidth) / 2;
                var y = (targetSize.Height - scaledHeight) / 2;
                var destRect = new Rectangle(x, y, scaledWidth, scaledHeight);
                graphics.DrawImage(original, destRect);

                var thumbnail = bitmap;
                bitmap = null;
                return thumbnail;
            }
            catch (Exception ex)
            {
                bitmap?.Dispose();
                DrawerLogger.LogDebug($"Thumbnail generation failed for '{path}'. {ex}");
                return null;
            }
        }
    }
}

