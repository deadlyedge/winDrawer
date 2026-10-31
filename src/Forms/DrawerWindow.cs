using System.Drawing.Drawing2D;
using System.Diagnostics;
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
            InitializeDrawerContextMenu();
            InitializeDragDrop();

            UpdateStandardFolderIcon(DeviceDpi);
            UpdateTopResizeGrip(DeviceDpi);
            UpdateFooterLayout(DeviceDpi);
            InitializeFolderLink();
            InitializeTransitions();
            closeDrawerButton.Click += (_, _) => Close();

            Size = _drawer.PreferredSize ?? Size;
            _lastPersistedSize = Size;
        }

        /// <summary>
        /// Makes the folder icon, the path text and the gap between them behave as a single click
        /// target: every part forwards to <see cref="OpenFolderButton_Click"/> and shares the same
        /// cursor and tooltip, so the footer never looks like two separate controls.
        /// </summary>
        private void InitializeFolderLink()
        {
            drawerPathTableLayoutPanel.Click += OpenFolderButton_Click;
            openFolderIconPictureBox.Click += OpenFolderButton_Click;
            fullPathLabel.Click += OpenFolderButton_Click;

            drawerPathTableLayoutPanel.Cursor = Cursors.Hand;
            openFolderIconPictureBox.Cursor = Cursors.Hand;
            fullPathLabel.Cursor = Cursors.Hand;

            const string openHint = "Click to open folder in File Explorer";
            drawerToolTip.SetToolTip(drawerPathTableLayoutPanel, openHint);
            drawerToolTip.SetToolTip(openFolderIconPictureBox, openHint);
            // The path label keeps showing the full path as its tooltip, which stays useful when the
            // text is truncated; it is refreshed in InitializeWindow and ApplyDrawerDefinition.
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

            // The configured opacity is the fade base: a running transition continues towards this
            // value instead of being snapped back.
            ApplyBaseOpacity(_theme.WindowOpacity);
            var contentBackground = _drawer.OverlayColor is { } contentOverlay
                ? ThemePalette.BlendOverlayColor(_theme.ContentBackground, contentOverlay)
                : _theme.ContentBackground;
            var windowBackground = _drawer.OverlayColor is { } overlayColor
                ? ThemePalette.BlendOverlayColor(_theme.WindowBackground, overlayColor)
                : _theme.WindowBackground;
            // The client area is covered by mainTableLayoutPanel except for the top resize strip, which
            // must be indistinguishable from the list content instead of showing the window frame.
            BackColor = contentBackground;

            mainTableLayoutPanel.BackColor = windowBackground;
            drawerHeaderTableLayoutPanel.BackColor = Color.Transparent;
            drawerButtonTableLayoutPanel.BackColor = Color.Transparent;

            drawerContentsListView.BackColor = contentBackground;
            drawerContentsListView.ForeColor = _theme.ContentTextPrimary;
            drawerContentsListView.HoverHighlightColor = _theme.ContentTextPrimary;
            drawerContentsScrollBar.FillColor = contentBackground;
            drawerContentsScrollBar.ThumbColor = _theme.Border;
            drawerContentsScrollBar.HoverColor = _theme.Border;
            drawerContentsScrollBar.PressColor = _theme.Border;
            SynchronizeDrawerScrollBar();

            fullPathLabel.ForeColor = _theme.TextSecondary;
            fullPathLabel.BackColor = Color.Transparent;

            ApplyThemeToButton(closeDrawerButton, isCloseButton: true);

            if (_drawerContextMenu is not null)
            {
                UiContextMenuTheming.Apply(_drawerContextMenu, _theme);
            }
        }

        public void ApplyDrawerDefinition(DrawerDefinition drawer)
        {
            var previousSortMode = _drawer.SortMode;
            _drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));
            Text = _drawer.Name;
            fullPathLabel.Text = _drawer.Path;
            drawerToolTip.SetToolTip(fullPathLabel, _drawer.Path);
            ApplyTheme(_theme);
            UpdateSortModeChecks();

            // A definition that arrives with another mode (for example after the settings file was
            // edited outside the drawer) must be reflected in the list immediately.
            if (_drawer.SortMode != previousSortMode)
            {
                _ = PopulateFileListAsync();
            }
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
                CancelAppearanceTransition();
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

        /// <summary>
        /// Applies the DPI-scaled footer metrics. The window uses <c>AutoScaleMode.None</c>, so the
        /// padding, icon column and text inset are recomputed explicitly instead of being auto-scaled
        /// by WinForms. Without this the fixed 42 px icon column clipped the 24 DIP icon at 200%
        /// scaling. The footer row keeps its fixed height so the close button, which is not scaled,
        /// stays where it was.
        /// </summary>
        private void UpdateFooterLayout(int dpi)
        {
            var scale = DpiScale.Factor(dpi);
            var iconSize = GetFolderIconSize(dpi);

            drawerPathTableLayoutPanel.Padding = new Padding(
                DpiScale.Pixels(AppConstants.Ui.DrawerFooter.PaddingLeft, scale),
                0,
                DpiScale.Pixels(AppConstants.Ui.DrawerFooter.PaddingRight, scale),
                0);

            if (drawerPathTableLayoutPanel.ColumnStyles.Count > 0)
            {
                // The icon column is exactly the icon width, so no dead area appears left of the
                // path; the gap to the text lives in the label padding and stays clickable.
                drawerPathTableLayoutPanel.ColumnStyles[0].Width = iconSize.Width;
            }

            var iconTextGap = DpiScale.Pixels(AppConstants.Ui.DrawerFooter.IconTextGap, scale);
            fullPathLabel.Padding = new Padding(iconTextGap, 0, 0, 0);
        }

        private static Size GetFolderIconSize(int dpi)
        {
            var size = DpiScale.Pixels(AppConstants.Ui.DrawerFooter.FolderIconSize, dpi);
            return new Size(size, size);
        }

        private static Bitmap ResizeIconToFit(Image source, Size maximumSize)
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

        private void ApplyThemeToButton(Button button, bool isCloseButton)
        {
            var fillColor = isCloseButton ? Color.FromArgb(180, 45, 45) : _theme.Accent;
            var hoverColor = isCloseButton ? Color.FromArgb(146, 36, 36) : _theme.AccentHover;
            var pressColor = isCloseButton ? Color.FromArgb(112, 25, 25) : _theme.AccentPressed;
            var textColor = isCloseButton ? Color.White : TextContrast.For(fillColor);

            UiButtonTheming.ApplyColorPalette(button, fillColor, hoverColor, pressColor, textColor);
            UiButtonTheming.ApplyBorderPalette(button, _theme.Border);
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

