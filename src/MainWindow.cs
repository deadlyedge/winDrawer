using System.Diagnostics;
using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer
{
    public partial class MainWindow : Form
    {
        private const string AppIconResourceName = "winDrawer.drawer.icon.4.ico";
        private Font? _logoFont;
        private Font? _settingsButtonFont;
        private Font? _addDrawerButtonFont;
        private bool _isDragging;
        private Point _dragOffset;
        private bool _drawerPointerDown;
        private bool _isDrawerReordering;
        private bool _drawerDragStarted;
        private int _drawerPressedIndex = -1;
        private Point _drawerPressPoint;
        private int _dropInsertionIndex = -1;
        private int _dropRawInsertionIndex = -1;
        private readonly Panel _drawerInsertionMarker = new()
        {
            Height = 3,
            Visible = false,
            Enabled = false
        };
        private readonly SettingsRepository _settingsRepository;
        private SettingsSnapshot _settingsSnapshot;
        private ThemePalette _currentTheme;
        private readonly List<DrawerDefinition> _currentDrawers = new();
        private DrawerWindow? _currentDrawerWindow;
        private DrawerDefinition? _currentOpenDrawer;
        private bool _suppressSelectionChange;
        private bool? _windowLockedPreview;
        private TrayController? _trayController;
        private SettingsForm? _settingsForm;

        public MainWindow()
        {
            DrawerLogger.LogInformation("Initializing MainWindow.");

            _settingsRepository = new SettingsRepository(SettingsRepository.GetDefaultPath());
            _settingsSnapshot = _settingsRepository.GetSnapshot();
            _currentTheme = _settingsSnapshot.BuildThemePalette();

            DrawerLogger.LogDebug($"Loaded {_settingsSnapshot.Drawers.Count} drawers from settings.");
            InitializeComponent();
            drawerListBox.MouseDown += DrawerListBox_MouseDown;
            drawerListBox.MouseUp += DrawerListBox_MouseUp;
            drawerListBox.MouseMove += DrawerListBox_MouseMove;
            drawerListBox.DragEnter += DrawerListBox_DragEnter;
            drawerListBox.DragOver += DrawerListBox_DragOver;
            drawerListBox.DragDrop += DrawerListBox_DragDrop;
            drawerListBox.AllowDrop = true;
            drawerListBox.DrawItem += DrawerListBox_DrawItem;
            drawerListBox.ListBox.DrawMode = DrawMode.OwnerDrawFixed;
            drawerListBox.Controls.Add(_drawerInsertionMarker);
            _drawerInsertionMarker.BringToFront();

            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            Opacity = _currentTheme.WindowOpacity;

            LoadBrandingAssets();
            InitializeTray();
            WireDragHandlers();
            ApplySettings(_settingsSnapshot);
            DpiChanged += (_, _) => ApplyMainWindowBounds(_settingsSnapshot);

            DrawerLogger.LogInformation("MainWindow initialization completed.");

            LocationChanged += MainWindow_LocationChanged;

            FormClosing += (_, e) =>
            {
                if (!_settingsRepository.SavePendingChanges(TimeSpan.FromSeconds(2)))
                {
                    DrawerLogger.LogError("Pending drawer configuration changes could not be persisted before closing.");
                    MessageBox.Show(this, "Some drawer settings could not be saved. Please check that the settings file is writable.",
                        "Settings Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    e.Cancel = true;
                }
            };

            FormClosed += (_, _) =>
            {
                _logoFont?.Dispose();
                _settingsButtonFont?.Dispose();
                _addDrawerButtonFont?.Dispose();

                try
                {
                    _trayController?.Dispose();
                    _trayController = null;
                }
                catch (Exception ex)
                {
                    DrawerLogger.LogError("Failed to dispose tray controller during shutdown.", ex);
                }
            };

            // 在窗体加载完成后填充菜单，避免默认打开
        }

        private void LoadBrandingAssets()
        {
            var baseDir = AppContext.BaseDirectory;

            var assetsDirectory = Path.Combine(baseDir, AppConstants.Directories.Assets);
            var iconPath = Path.Combine(assetsDirectory, AppConstants.Files.AppIconFileName);

            DrawerLogger.LogDebug($"Loading branding assets from '{assetsDirectory}'.");

            ApplyEmbeddedBrandFonts();
            TryApplyIcon(iconPath);
        }

        private void ApplyEmbeddedBrandFonts()
        {
            Font? logoFont = null;
            Font? settingsButtonFont = null;
            Font? addDrawerButtonFont = null;
            try
            {
                logoFont = BrandingFont.Create(AppConstants.Defaults.LogoFontSize);
                settingsButtonFont = BrandingFont.Create(10.5F);
                addDrawerButtonFont = BrandingFont.Create(10.5F);
                var loadedFamily = BrandingFont.LoadedFamilyName;
                if (string.IsNullOrEmpty(loadedFamily))
                {
                    DrawerLogger.LogWarning("The embedded Lobster font was unavailable; using the system font for main-window branding.");
                }
                else
                {
                    DrawerLogger.LogInformation($"Loaded embedded branding font '{loadedFamily}'.");
                }
            }
            catch (Exception ex)
            {
                logoFont?.Dispose();
                settingsButtonFont?.Dispose();
                addDrawerButtonFont?.Dispose();
                DrawerLogger.LogWarning($"Unable to apply the embedded branding font. {ex.Message}");
                logoFont = BrandingFont.CreateFallback(AppConstants.Defaults.LogoFontSize);
                settingsButtonFont = BrandingFont.CreateFallback(10.5F);
                addDrawerButtonFont = BrandingFont.CreateFallback(10.5F);
            }

            _logoFont = logoFont;
            _settingsButtonFont = settingsButtonFont;
            _addDrawerButtonFont = addDrawerButtonFont;
            logoLabel.Font = _logoFont;
            settingsButton.Font = _settingsButtonFont;
            addDrawerButton.Font = _addDrawerButtonFont;
        }

        private void TryApplyIcon(string iconPath)
        {
            try
            {
                using var iconStream = typeof(MainWindow).Assembly.GetManifestResourceStream(AppIconResourceName);
                if (iconStream != null)
                {
                    using var embeddedIcon = new Icon(iconStream, AppConstants.Ui.WindowIconSize);
                    Icon = (Icon)embeddedIcon.Clone();
                    DrawerLogger.LogInformation("Applied window icon from embedded application resources.");
                    return;
                }
            }
            catch (Exception ex)
            {
                DrawerLogger.LogWarning($"Failed to load embedded window icon. {ex}");
            }

            if (File.Exists(iconPath))
            {
                try
                {
                    // Load window icon from file. Do NOT call Dispose on the existing Icon
                    // because other parts of the app (e.g. cloned tray icons) may still
                    // reference the same instance and disposing it can cause ObjectDisposedException.
                    Icon = new Icon(iconPath, AppConstants.Ui.WindowIconSize);
                    DrawerLogger.LogInformation($"Applied window icon from '{iconPath}'.");
                }
                catch
                {
                    // ignore and keep existing icon if loading fails
                    DrawerLogger.LogWarning($"Failed to apply window icon from '{iconPath}'.");
                }
            }
            else
            {
                DrawerLogger.LogWarning($"Window icon not found at '{iconPath}'.");
            }
        }

        private void WireDragHandlers()
        {
            logoLabel.MouseDown += HeaderDragBegin;
            logoLabel.MouseMove += HeaderDragMove;
            logoLabel.MouseUp += HeaderDragEnd;
        }

        private void HeaderDragBegin(object? sender, MouseEventArgs args)
        {
            if (IsWindowLocked)
            {
                return;
            }

            if (args.Button != MouseButtons.Left)
            {
                return;
            }

            _isDragging = true;
            _dragOffset = args.Location;
        }

        private void HeaderDragMove(object? sender, MouseEventArgs args)
        {
            if (IsWindowLocked)
            {
                _isDragging = false;
                return;
            }

            if (!_isDragging)
            {
                return;
            }

            var screenPoint = PointToScreen(args.Location);
            Location = new Point(screenPoint.X - _dragOffset.X, screenPoint.Y - _dragOffset.Y);
        }

        private bool IsWindowLocked => _windowLockedPreview ?? _settingsSnapshot.WindowLocked;

        private void HeaderDragEnd(object? sender, MouseEventArgs args)
        {
            if (args.Button != MouseButtons.Left)
            {
                return;
            }

            _isDragging = false;
            _settingsSnapshot.WindowPosition = Location;
            _settingsRepository.Update(_settingsSnapshot);
            DrawerLogger.LogDebug($"Window drag ended at location {Location}.");
            // Enqueue save: background writer in SettingsRepository will persist asynchronously.
        }

        private void ReloadSettingsFromStorage()
        {
            try
            {
                DrawerLogger.LogInformation("Reloading settings from repository.");
                var snapshot = _settingsRepository.GetSnapshot();
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => ApplySnapshot(snapshot)));
                }
                else
                {
                    ApplySnapshot(snapshot);
                }
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Failed to reload settings from repository.", ex);
                MessageBox.Show(this, $"Failed to refresh drawers: {ex.Message}", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplySnapshot(SettingsSnapshot snapshot)
        {
            _settingsSnapshot = snapshot;
            DrawerLogger.LogDebug("Applying settings snapshot to UI.");
            ApplySettings(_settingsSnapshot);
        }

        private void ApplySettings(SettingsSnapshot snapshot)
        {
            StartPosition = FormStartPosition.Manual;
            logoLabel.Cursor = snapshot.WindowLocked ? Cursors.Default : Cursors.SizeAll;
            if (snapshot.WindowLocked)
            {
                _isDragging = false;
            }

            ApplyMainWindowBounds(snapshot);
            ApplyTheme(snapshot.BuildThemePalette());
            PopulateDrawerList(snapshot.Drawers);
            // 如果有当前打开的drawer，更新list中的选中状态
            UpdateSelectedDrawer();
        }

        private void ApplyMainWindowBounds(SettingsSnapshot snapshot)
        {
            var dpiScale = DeviceDpi / 96d;
            var desiredWidth = Math.Max(1, (int)Math.Round(AppConstants.Defaults.MainWindowWidth * dpiScale));
            var desiredHeight = Math.Max(1, (int)Math.Round(snapshot.WindowHeight * dpiScale));
            var desiredBounds = new Rectangle(snapshot.WindowPosition, new Size(desiredWidth, desiredHeight));
            var workArea = Screen.FromRectangle(desiredBounds).WorkingArea;

            ClientSize = new Size(
                Math.Min(desiredWidth, workArea.Width),
                Math.Min(desiredHeight, workArea.Height));
            Location = new Point(
                ClampToWorkArea(snapshot.WindowPosition.X, workArea.Left, workArea.Right - Width),
                ClampToWorkArea(snapshot.WindowPosition.Y, workArea.Top, workArea.Bottom - Height));
        }

        private static int ClampToWorkArea(int value, int minimum, int maximum)
        {
            return Math.Clamp(value, minimum, Math.Max(minimum, maximum));
        }

        private void ApplyTheme(ThemePalette theme)
        {
            _currentTheme = theme;
            DrawerLogger.LogDebug($"Applying theme palette. Accent={theme.Accent}, WindowOpacity={theme.WindowOpacity:0.00}.");

            Opacity = theme.WindowOpacity;
            BackColor = theme.WindowBackground;

            mainTableLayoutPanel.BackColor = theme.WindowBackground;
            headerTableLayoutPanel.BackColor = Color.Transparent;

            logoLabel.ForeColor = theme.TextPrimary;
            logoLabel.BackColor = Color.Transparent;

            settingsButton.FillColor = theme.Accent;
            settingsButton.FillHoverColor = theme.AccentHover;
            settingsButton.FillPressColor = theme.AccentPressed;
            settingsButton.FillSelectedColor = theme.AccentPressed;
            settingsButton.ForeColor = theme.AccentText;
            settingsButton.RectColor = theme.Border;
            settingsButton.RectDisableColor = theme.Border;
            settingsButton.RectHoverColor = theme.Border;
            settingsButton.RectPressColor = theme.Border;
            settingsButton.RectSelectedColor = theme.Border;

            addDrawerButton.FillColor = theme.Accent;
            addDrawerButton.FillHoverColor = theme.AccentHover;
            addDrawerButton.FillPressColor = theme.AccentPressed;
            addDrawerButton.FillSelectedColor = theme.AccentPressed;
            addDrawerButton.ForeColor = theme.AccentText;
            addDrawerButton.RectColor = theme.Border;
            addDrawerButton.RectDisableColor = theme.Border;
            addDrawerButton.RectHoverColor = theme.Border;
            addDrawerButton.RectPressColor = theme.Border;
            addDrawerButton.RectSelectedColor = theme.Border;

            drawerListBox.FillColor = theme.ContentBackground;
            drawerListBox.ForeColor = theme.TextPrimary;
            drawerListBox.HoverColor = theme.CardBackground;
            drawerListBox.RectColor = theme.Border;
            drawerListBox.ItemSelectForeColor = theme.AccentText;
            drawerListBox.ScrollBarBackColor = theme.ContentBackground;
            drawerListBox.ScrollBarColor = theme.Border;

            if (_currentDrawerWindow != null && !_currentDrawerWindow.IsDisposed)
            {
                _currentDrawerWindow.ApplyTheme(theme);
            }
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (_currentDrawerWindow != null && _currentDrawerWindow.Visible && _currentDrawerWindow.WindowState == FormWindowState.Normal)
            {
                _currentDrawerWindow.Location = new Point(Right, Top);
            }
        }

    }
}


