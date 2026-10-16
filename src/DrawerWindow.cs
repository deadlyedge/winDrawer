using System.Drawing.Drawing2D;
using System.Diagnostics;
using Sunny.UI;
using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer
{
    public partial class DrawerWindow : Form
    {
        private readonly SettingsRepository _settingsRepository;
        private DrawerDefinition _drawer;
        private readonly List<Image> _imageListImages = [];
        private ThemePalette _theme;
        private Size _lastPersistedSize;
        private CancellationTokenSource? _fileListLoadCancellation;
        private int _fileListLoadGeneration;

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
        private sealed record LoadedDrawerItem(string Path, string Name, bool IsDirectory, Image Image);

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
            drawerToolTip.SetToolTip(fullPathLabel, _drawer.Path);
            DrawerLogger.LogDebug($"Setting up DrawerWindow UI for '{_drawer.Name}'.");

            drawerImageList.ImageSize = GetImageSize(DeviceDpi);
            drawerContentsListView.LargeImageList = drawerImageList;
            drawerContentsListView.MouseDoubleClick += DrawerContentsListView_MouseDoubleClick;
            drawerContentsListView.ViewportChanged += DrawerContentsListView_ViewportChanged;
            drawerContentsScrollBar.ValueChanged += DrawerContentsScrollBar_ValueChanged;
            drawerContentsHostPanel.Resize += DrawerContentsHostPanel_Resize;
            DpiChanged += DrawerWindow_DpiChanged;
            UpdateDrawerScrollBarDpi(DeviceDpi);

            UpdateStandardFolderIcon(DeviceDpi);
            drawerPathTableLayoutPanel.Click += OpenFolderButton_Click;
            openFolderIconPictureBox.Click += OpenFolderButton_Click;
            fullPathLabel.Click += OpenFolderButton_Click;
            refreshDrawerButton.Click += RefreshDrawerButton_Click;
            closeDrawerButton.Click += (_, _) => Close();
            Disposed += (_, _) => CancelFileListLoad();

            Size = _drawer.PreferredSize ?? Size;
            _lastPersistedSize = Size;

            ShowListStatus("Loading...");
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int wsCaption = 0x00C00000;
                var parameters = base.CreateParams;
                parameters.Style &= ~wsCaption;
                return parameters;
            }
        }

        public void ApplyTheme(ThemePalette theme)
        {
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));
            DrawerLogger.LogDebug($"Applying theme to drawer '{_drawer.Name}'.");

            Opacity = _theme.WindowOpacity;
            var contentBackground = _drawer.OverlayColor is { } contentOverlay
                ? BlendColor(_theme.ContentBackground, contentOverlay)
                : _theme.ContentBackground;
            var windowBackground = _drawer.OverlayColor is { } overlayColor
                ? BlendColor(_theme.WindowBackground, overlayColor)
                : _theme.WindowBackground;
            BackColor = windowBackground;

            mainTableLayoutPanel.BackColor = windowBackground;
            drawerHeaderTableLayoutPanel.BackColor = Color.Transparent;
            drawerButtonTableLayoutPanel.BackColor = Color.Transparent;

            drawerContentsListView.BackColor = contentBackground;
            drawerContentsListView.ForeColor = _theme.TextPrimary;
            drawerContentsScrollBar.FillColor = contentBackground;
            drawerContentsScrollBar.FillWidth = Math.Max(6, (int)Math.Round(10 * DeviceDpi / 96d));
            drawerContentsScrollBar.ForeColor = _theme.Border;
            drawerContentsScrollBar.HoverColor = _theme.Border;
            drawerContentsScrollBar.PressColor = _theme.Border;
            SynchronizeDrawerScrollBar();

            fullPathLabel.ForeColor = _theme.TextSecondary;
            fullPathLabel.BackColor = Color.Transparent;

            ApplyThemeToButton(refreshDrawerButton, isCloseButton: false);
            ApplyThemeToButton(closeDrawerButton, isCloseButton: true);
        }

        public void ApplyDrawerDefinition(DrawerDefinition drawer)
        {
            _drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));
            Text = _drawer.Name;
            fullPathLabel.Text = _drawer.Path;
            drawerToolTip.SetToolTip(fullPathLabel, _drawer.Path);
            ApplyTheme(_theme);
        }

        private static Color BlendColor(Color background, Color overlay)
        {
            return ThemePalette.BlendOverlayColor(background, overlay);
        }

        private void DrawerWindow_DpiChanged(object? sender, DpiChangedEventArgs e)
        {
            UpdateStandardFolderIcon(e.DeviceDpiNew);
            UpdateDrawerScrollBarDpi(e.DeviceDpiNew);
            CancelFileListLoad();
            ClearFileList();
            drawerImageList.ImageSize = GetImageSize(e.DeviceDpiNew);
            _ = PopulateFileListAsync();
        }

        private void UpdateDrawerScrollBarDpi(int dpi)
        {
            var scale = Math.Max(1, dpi) / 96f;
            drawerContentsScrollBar.FillWidth = Math.Max(6, (int)Math.Round(10 * scale));
            drawerContentsScrollBar.Width = Math.Max(18, (int)Math.Round(18 * scale));
            drawerContentsScrollBar.AccessibleName = "Scroll folder contents";
            drawerContentsScrollBar.AccessibleRole = AccessibleRole.ScrollBar;
            PositionDrawerScrollBar();
        }

        private void PositionDrawerScrollBar()
        {
            if (!drawerContentsListView.IsHandleCreated)
            {
                return;
            }

            var scrollBarWidth = drawerContentsScrollBar.Width;
            var viewportWidth = Math.Max(1, drawerContentsHostPanel.ClientSize.Width - scrollBarWidth);
            drawerContentsListView.SetBounds(0, 0, drawerContentsHostPanel.ClientSize.Width, drawerContentsHostPanel.ClientSize.Height);
            drawerContentsScrollBar.SetBounds(viewportWidth, 0, scrollBarWidth, drawerContentsHostPanel.ClientSize.Height);
        }

        private void DrawerContentsListView_ViewportChanged(object? sender, EventArgs e)
        {
            SynchronizeDrawerScrollBar();
        }

        private void DrawerContentsHostPanel_Resize(object? sender, EventArgs e)
        {
            PositionDrawerScrollBar();
            SynchronizeDrawerScrollBar();
        }

        private void DrawerContentsScrollBar_ValueChanged(object? sender, EventArgs e)
        {
            drawerContentsListView.ScrollToVerticalPosition(drawerContentsScrollBar.Value);
        }

        private void SynchronizeDrawerScrollBar()
        {
            if (IsDisposed || Disposing || !drawerContentsListView.IsHandleCreated)
            {
                return;
            }

            if (!drawerContentsListView.TryGetVerticalScrollInfo(out var maximumPosition, out var position, out var viewportSize) ||
                maximumPosition <= 0)
            {
                drawerContentsScrollBar.Visible = false;
                PositionDrawerScrollBar();
                return;
            }

            drawerContentsScrollBar.Maximum = maximumPosition;
            drawerContentsScrollBar.Value = position;
            drawerContentsScrollBar.Visible = true;
            PositionDrawerScrollBar();
            drawerContentsScrollBar.BringToFront();
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

            PositionDrawerScrollBar();
            SynchronizeDrawerScrollBar();
            _ = PopulateFileListAsync();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (!e.Cancel)
            {
                CancelFileListLoad();
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

        private async void RefreshDrawerButton_Click(object? sender, EventArgs e)
        {
            await PopulateFileListAsync();
        }

        private void OpenFolderButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists(_drawer.Path))
                {
                    throw new DirectoryNotFoundException($"Folder not found: {_drawer.Path}");
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = _drawer.Path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError($"Failed to open drawer folder '{_drawer.Path}'.", ex);
                MessageBox.Show(this, $"Failed to open folder {_drawer.Path}:{Environment.NewLine}{ex.Message}",
                    "Open Folder Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStandardFolderIcon(int dpi)
        {
            Image? folderImage = null;
            try
            {
                using var icon = ShellIcon.GetIcon("winDrawer-folder-icon", isDirectory: true, useFileAttributes: true);
                using var source = (icon ?? SystemIcons.WinLogo).ToBitmap();
                folderImage = ResizeIconToFit(source, GetFolderIconSize(dpi));
            }
            catch (Exception ex)
            {
                DrawerLogger.LogWarning($"Failed to load the standard Windows folder icon. {ex}");
                using var fallback = SystemIcons.WinLogo.ToBitmap();
                folderImage = ResizeIconToFit(fallback, GetFolderIconSize(dpi));
            }

            var previousImage = openFolderIconPictureBox.Image;
            openFolderIconPictureBox.Image = folderImage;
            previousImage?.Dispose();
        }

        private static Size GetFolderIconSize(int dpi)
        {
            const int logicalSize = 24;
            var size = Math.Max(1, (int)Math.Round(logicalSize * dpi / 96d));
            return new Size(size, size);
        }

        private static Image ResizeIconToFit(Image source, Size maximumSize)
        {
            var scale = Math.Min(
                1d,
                Math.Min(
                    (double)maximumSize.Width / source.Width,
                    (double)maximumSize.Height / source.Height));
            var width = Math.Max(1, (int)Math.Round(source.Width * scale));
            var height = Math.Max(1, (int)Math.Round(source.Height * scale));
            var resized = new Bitmap(width, height);

            using var graphics = Graphics.FromImage(resized);
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, width, height));
            return resized;
        }

        private async Task PopulateFileListAsync()
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            var previousCancellation = _fileListLoadCancellation;
            var cancellation = new CancellationTokenSource();
            _fileListLoadCancellation = cancellation;
            var generation = ++_fileListLoadGeneration;
            previousCancellation?.Cancel();

            DrawerLogger.LogInformation($"Refreshing drawer contents for '{_drawer.Name}'.");
            ClearFileList();
            fullPathLabel.Text = _drawer.Path;
            ShowListStatus("Loading...");

            try
            {
                var imageSize = drawerImageList.ImageSize;
                var items = await Task.Run(
                    () => LoadDrawerItems(_drawer.Path, imageSize, cancellation.Token));

                if (!IsCurrentLoad(generation, cancellation))
                {
                    DisposeLoadedItems(items);
                    return;
                }

                if (items == null)
                {
                    fullPathLabel.Text = $"{_drawer.Path} (missing)";
                    ShowListStatus("Folder not found or inaccessible");
                    DrawerLogger.LogWarning($"Drawer path missing for '{_drawer.Name}': {_drawer.Path}");
                    return;
                }

                try
                {
                    ApplyLoadedItems(items);
                }
                finally
                {
                    DisposeLoadedItems(items);
                }

                var directoryCount = items.Count(item => item.IsDirectory);
                var fileCount = items.Count - directoryCount;
                DrawerLogger.LogInformation($"Drawer '{_drawer.Name}' loaded {directoryCount} directories and {fileCount} files.");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                DrawerLogger.LogDebug($"Discarded an outdated load for drawer '{_drawer.Name}'.");
            }
            catch (Exception ex)
            {
                if (!IsCurrentLoad(generation, cancellation))
                {
                    return;
                }

                DrawerLogger.LogError($"Failed to populate drawer '{_drawer.Name}'.", ex);
                ClearFileList();
                ShowListStatus("Failed to load", ex.Message);
                MessageBox.Show(this, $"Failed to load drawer contents: {ex.Message}", "Drawer Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (ReferenceEquals(_fileListLoadCancellation, cancellation))
                {
                    _fileListLoadCancellation = null;
                }

                cancellation.Dispose();
            }
        }

        private static List<LoadedDrawerItem>? LoadDrawerItems(string folderPath, Size imageSize, CancellationToken cancellationToken)
        {
            var items = new List<LoadedDrawerItem>();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Directory.Exists(folderPath))
                {
                    return null;
                }

                var directories = new List<string>();
                foreach (var directory in Directory.EnumerateDirectories(folderPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    directories.Add(directory);
                }

                var files = new List<string>();
                foreach (var file in Directory.EnumerateFiles(folderPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    files.Add(file);
                }

                directories.Sort((left, right) => CompareItemNames(left, right, cancellationToken));
                files.Sort((left, right) => CompareItemNames(left, right, cancellationToken));

                foreach (var directory in directories)
                {
                    AddLoadedItem(items, directory, isDirectory: true, imageSize, cancellationToken);
                }

                foreach (var file in files)
                {
                    AddLoadedItem(items, file, isDirectory: false, imageSize, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                return items;
            }
            catch
            {
                DisposeLoadedItems(items);
                throw;
            }
        }

        private static int CompareItemNames(string left, string right, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return StringComparer.CurrentCultureIgnoreCase.Compare(Path.GetFileName(left), Path.GetFileName(right));
        }

        private static void AddLoadedItem(
            ICollection<LoadedDrawerItem> items,
            string path,
            bool isDirectory,
            Size imageSize,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var image = GetItemImage(path, isDirectory, imageSize);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = Path.GetFileName(path);
                items.Add(new LoadedDrawerItem(path, string.IsNullOrEmpty(name) ? path : name, isDirectory, image));
            }
            catch
            {
                image.Dispose();
                throw;
            }
        }

        private void ApplyLoadedItems(IReadOnlyList<LoadedDrawerItem> items)
        {
            drawerContentsListView.BeginUpdate();
            try
            {
                ClearFileList();
                for (var index = 0; index < items.Count; index++)
                {
                    var loadedItem = items[index];
                    var imageKey = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var imageListCopy = (Image)loadedItem.Image.Clone();
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

                    var item = new ListViewItem(loadedItem.Name, imageKey)
                    {
                        ToolTipText = loadedItem.Path,
                        Tag = new DrawerItemInfo(loadedItem.Path, loadedItem.IsDirectory)
                    };

                    drawerContentsListView.Items.Add(item);
                }

                if (items.Count == 0)
                {
                    ShowListStatus("Folder is empty");
                }
            }
            catch
            {
                ClearFileList();
                throw;
            }
            finally
            {
                drawerContentsListView.EndUpdate();
            }

            SynchronizeDrawerScrollBar();
        }

        private void ClearFileList()
        {
            drawerContentsListView.BeginUpdate();
            try
            {
                drawerContentsListView.Items.Clear();
                drawerImageList.Images.Clear();
                DisposeImageListImages();
            }
            finally
            {
                drawerContentsListView.EndUpdate();
            }

            SynchronizeDrawerScrollBar();
        }

        private void ShowListStatus(string text, string? toolTip = null)
        {
            drawerContentsListView.Items.Clear();
            var statusItem = new ListViewItem(text)
            {
                ToolTipText = toolTip ?? text
            };
            drawerContentsListView.Items.Add(statusItem);
            SynchronizeDrawerScrollBar();
        }

        private bool IsCurrentLoad(int generation, CancellationTokenSource cancellation)
        {
            return !IsDisposed && !Disposing &&
                   generation == _fileListLoadGeneration &&
                   ReferenceEquals(_fileListLoadCancellation, cancellation) &&
                   !cancellation.IsCancellationRequested;
        }

        private void CancelFileListLoad()
        {
            _fileListLoadGeneration++;
            var cancellation = _fileListLoadCancellation;
            _fileListLoadCancellation = null;
            cancellation?.Cancel();
        }

        private static void DisposeLoadedItems(IEnumerable<LoadedDrawerItem>? items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                item.Image.Dispose();
            }
        }

        private void DisposeImageListImages()
        {
            foreach (var image in _imageListImages)
            {
                image.Dispose();
            }

            _imageListImages.Clear();
        }

        private void ApplyThemeToButton(UIButton button, bool isCloseButton)
        {
            if (button == null)
            {
                return;
            }

            var fillColor = isCloseButton ? Color.FromArgb(180, 45, 45) : _theme.Accent;
            var hoverColor = isCloseButton ? Color.FromArgb(146, 36, 36) : _theme.AccentHover;
            var pressColor = isCloseButton ? Color.FromArgb(112, 25, 25) : _theme.AccentPressed;
            var disabledFillColor = BlendButtonColor(fillColor, _theme.WindowBackground);

            button.FillColor = fillColor;
            button.FillHoverColor = hoverColor;
            button.FillPressColor = pressColor;
            button.FillSelectedColor = pressColor;
            button.FillDisableColor = disabledFillColor;
            button.ForeColor = isCloseButton ? Color.White : ThemePalette.GetReadableTextColor(fillColor);
            button.ForeHoverColor = isCloseButton ? Color.White : ThemePalette.GetReadableTextColor(hoverColor);
            button.ForePressColor = isCloseButton ? Color.White : ThemePalette.GetReadableTextColor(pressColor);
            button.ForeSelectedColor = button.ForePressColor;
            button.ForeDisableColor = ThemePalette.GetReadableTextColor(disabledFillColor);
            button.RectColor = _theme.Border;
            button.RectDisableColor = _theme.Border;
            button.RectHoverColor = _theme.Border;
            button.RectPressColor = _theme.Border;
            button.RectSelectedColor = _theme.Border;
        }

        private static Color BlendButtonColor(Color color, Color background)
        {
            return Color.FromArgb(
                (color.R + background.R) / 2,
                (color.G + background.G) / 2,
                (color.B + background.B) / 2);
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

        private static Image GetItemImage(string path, bool isDirectory, Size imageSize)
        {
            if (isDirectory)
            {
                return GetShellIconImage(path, isDirectory: true);
            }

            if (IsImageFile(path))
            {
                var thumbnail = TryCreateThumbnail(path, imageSize);
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

        private static Bitmap? TryCreateThumbnail(string path, Size targetSize)
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

