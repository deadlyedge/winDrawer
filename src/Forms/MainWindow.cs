using System.Diagnostics;
using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer
{
    public partial class MainWindow : Form
    {
        private const string AppIconResourceName = "winDrawer.drawer.icon.4.ico";
        private const int SettingsWindowOffset = 100;
        private const int ShutdownFlushTimeoutMilliseconds = 500;
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
        private readonly List<DrawerDefinition> _currentDrawers = [];
        private DrawerWindow? _currentDrawerWindow;
        private DrawerDefinition? _currentOpenDrawer;
        private bool _suppressSelectionChange;
        private bool? _windowLockedPreview;
        private bool? _alwaysOnTopPreview;
        private TrayController? _trayController;
        private SettingsForm? _settingsForm;
        private bool _settingsCloseHandled;
        private bool _shutdownInProgress;

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
            drawerListBox.DragLeave += DrawerListBox_DragLeave;
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

            // 在窗体加载完成后填充菜单，避免默认打开
        }

        /// <summary>
        /// Flushes pending drawer changes and lets a failed save cancel the close so the user keeps
        /// the chance to fix the settings file.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.Cancel)
            {
                return;
            }

            if (!_settingsRepository.SavePendingChanges(TimeSpan.FromSeconds(2)))
            {
                DrawerLogger.LogError("Pending drawer configuration changes could not be persisted before closing.");
                UiMessages.ShowError(this, "Settings Save Failed", "Some drawer settings could not be saved. Please check that the settings file is writable.");
                e.Cancel = true;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Releasing the shortcut keeps the "hotkey turned off" path explicit; it is a no-op when
            // OnHandleDestroyed already released it.
            ReleaseHotKey();

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

            try
            {
                // The repository owns the background writer task and its synchronization primitives;
                // disposing it performs the final flush, cancels the writer and releases both.
                _settingsRepository.Dispose();
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Failed to dispose the settings repository during shutdown.", ex);
            }

            base.OnFormClosed(e);
        }

        /// <summary>
        /// Single entry point for every shutdown request (tray menu or settings panel). The bounded
        /// pre-flush makes the later <see cref="OnFormClosing"/> flush a no-op, so a normal exit stays
        /// fast, and <see cref="Application.Exit"/> closes the windows through the regular WinForms
        /// path (fonts, tray icon and settings repository are released there) instead of killing the
        /// process abruptly.
        /// </summary>
        private void ShutdownApplication(string reason)
        {
            if (_shutdownInProgress)
            {
                // Owned modeless windows raise FormClosed twice (their own reason, then
                // FormOwnerClosing), so the same request can arrive again while it is being handled.
                DrawerLogger.LogDebug($"Ignoring a duplicate shutdown request ({reason}).");
                return;
            }

            _shutdownInProgress = true;
            DrawerLogger.LogWarning($"Application shutdown requested ({reason}).");

            try
            {
                try
                {
                    if (!_settingsRepository.SavePendingChanges(TimeSpan.FromMilliseconds(ShutdownFlushTimeoutMilliseconds)))
                    {
                        DrawerLogger.LogWarning("Pending settings could not be persisted within the shutdown flush timeout; continuing with the shutdown.");
                    }
                }
                catch (Exception ex)
                {
                    DrawerLogger.LogError("Failed to flush pending settings during shutdown.", ex);
                }

                // Cancelling FormClosing (for example when the settings file cannot be written) keeps
                // the application open, which is why the in-progress flag is only held for this call.
                Application.Exit();
                DrawerLogger.LogInformation("Application.Exit() completed; the message loop stops unless a window cancelled its close.");
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Application.Exit failed during shutdown; forcing process exit.", ex);
                ForceProcessExit();
            }
            finally
            {
                _shutdownInProgress = false;
            }
        }

        /// <summary>
        /// Last-resort termination used only when the regular shutdown path throws. Every step is
        /// logged so an abrupt exit can never hide the reason for it.
        /// </summary>
        private static void ForceProcessExit()
        {
            try
            {
                Environment.Exit(0);
                return;
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Environment.Exit failed during forced shutdown.", ex);
            }

            try
            {
                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Process.Kill failed during forced shutdown.", ex);
            }
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

        private bool IsAlwaysOnTop => _alwaysOnTopPreview ?? _settingsSnapshot.AlwaysOnTop;

        /// <summary>
        /// Applies the always-on-top option to the main window and to the drawer window that is
        /// currently open. Settings panels are never made top-most by this option.
        /// </summary>
        private void ApplyAlwaysOnTop(bool enabled)
        {
            TopMost = enabled;
            if (_currentDrawerWindow != null && !_currentDrawerWindow.IsDisposed)
            {
                _currentDrawerWindow.TopMost = enabled;
            }
        }

        /// <summary>
        /// Places a settings window relative to the current main-window position and keeps it inside
        /// the working area of the screen the main window is on. The position is recalculated for every
        /// new settings window and is never persisted.
        /// </summary>
        private void ApplySettingsWindowPlacement(Form dialog)
        {
            ArgumentNullException.ThrowIfNull(dialog);

            var workingArea = Screen.FromRectangle(Bounds).WorkingArea;
            var maximumLeft = Math.Max(workingArea.Left, workingArea.Right - dialog.Width);
            var maximumTop = Math.Max(workingArea.Top, workingArea.Bottom - dialog.Height);

            dialog.StartPosition = FormStartPosition.Manual;
            dialog.Location = new Point(
                Math.Clamp(Left + SettingsWindowOffset, workingArea.Left, maximumLeft),
                Math.Clamp(Top + SettingsWindowOffset, workingArea.Top, maximumTop));
        }

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
                UiMessages.ShowError(this, "Settings Error", $"Failed to refresh drawers: {ex.Message}");
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
            ApplyAlwaysOnTop(snapshot.AlwaysOnTop);
            ApplyTheme(snapshot.BuildThemePalette());
            PopulateDrawerList(snapshot.Drawers);
            // 如果有当前打开的drawer，更新list中的选中状态
            UpdateSelectedDrawer();

            // The global shortcut follows the same preview/save/cancel semantics as the other startup
            // options. The flag allows OnHandleCreated to register it when this runs before the handle
            // exists (the constructor path).
            _settingsApplied = true;
            SyncHotkeyRegistration(_toggleHotkeyPreview ?? snapshot.ToggleHotkeyEnabled);
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

            UiButtonTheming.ApplyAccentPalette(settingsButton, theme);
            UiButtonTheming.ApplyAccentPalette(addDrawerButton, theme);

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
            // A hidden drawer window is repositioned when it is shown again, so only a visible drawer
            // needs to follow the main window here.
            if (_currentDrawerWindow is { Visible: true })
            {
                PositionDrawerWindowNextToMainWindow();
            }
        }

    }
}


