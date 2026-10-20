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
        private ThemePalette _theme;
        private Size _lastPersistedSize;

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

            drawerContentsListView.MouseDoubleClick += DrawerContentsListView_MouseDoubleClick;
            InitializeFileList();

            UpdateStandardFolderIcon(DeviceDpi);
            UpdateTopResizeGrip(DeviceDpi);
            drawerPathTableLayoutPanel.Click += OpenFolderButton_Click;
            openFolderIconPictureBox.Click += OpenFolderButton_Click;
            fullPathLabel.Click += OpenFolderButton_Click;
            refreshDrawerButton.Click += RefreshDrawerButton_Click;
            closeDrawerButton.Click += (_, _) => Close();

            Size = _drawer.PreferredSize ?? Size;
            _lastPersistedSize = Size;
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
            // The client area is covered by mainTableLayoutPanel except for the top resize strip, which
            // must be indistinguishable from the list content instead of showing the window frame.
            BackColor = contentBackground;

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
                CancelFileListWork();
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
                UiMessages.ShowError(this, "Open Folder Failed", $"Failed to open folder {_drawer.Path}:{Environment.NewLine}{ex.Message}");
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
            UiButtonTheming.ApplyBorderPalette(button, _theme.Border);
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
                UiMessages.ShowError(this, "Open Failed", $"Failed to open {info.Path}: {ex.Message}");
            }
        }
    }
}

