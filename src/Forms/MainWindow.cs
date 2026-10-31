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
        private Font? _headerIconFont;
        private WindowFade? _windowFade;
        private bool _isDragging;
        private Point _dragOffset;
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
            InitializeFirstRunDemo();
            _settingsSnapshot = _settingsRepository.GetSnapshot();
            _currentTheme = _settingsSnapshot.BuildThemePalette();

            DrawerLogger.LogDebug($"Loaded {_settingsSnapshot.Drawers.Count} drawers from settings.");
            InitializeComponent();
            ApplyHeaderButtonGlyphs();
            drawerListBox.MouseDown += DrawerListBox_MouseDown;
            drawerListBox.MouseUp += DrawerListBox_MouseUp;
            drawerListBox.MouseMove += DrawerListBox_MouseMove;
            drawerListBox.DragEnter += DrawerListBox_DragEnter;
            drawerListBox.DragOver += DrawerListBox_DragOver;
            drawerListBox.DragLeave += DrawerListBox_DragLeave;
            drawerListBox.DragDrop += DrawerListBox_DragDrop;
            drawerListBox.AllowDrop = true;
            drawerListBox.DrawItem += DrawerListBox_DrawItem;
            drawerListBox.DrawMode = DrawMode.OwnerDrawFixed;
            drawerListBox.Controls.Add(_drawerInsertionMarker);
            _drawerInsertionMarker.BringToFront();

            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            Opacity = _currentTheme.WindowOpacity;

            LoadBrandingAssets();
            InitializeTray();
            WireDragHandlers();
            ApplySettingsSnapshot(_settingsSnapshot);
            DpiChanged += (_, _) => ApplyMainWindowBounds(_settingsSnapshot);

            DrawerLogger.LogInformation("MainWindow initialization completed.");

            LocationChanged += MainWindow_LocationChanged;

            // 在窗体加载完成后填充菜单，避免默认打开
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Startup is the one show path that does not go through ShowMainWindowAndDrawer.
            PlayWindowFadeIn();
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
            // Stop the transitions first: their timers must not outlive the resources released below.
            _windowFade?.Dispose();
            _windowFade = null;

            // Releasing the shortcut keeps the "hotkey turned off" path explicit; it is a no-op when
            // OnHandleDestroyed already released it.
            ReleaseHotKey();

            _logoFont?.Dispose();
            _headerIconFont?.Dispose();

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

        /// <summary>
        /// Copies the embedded demo folder once, when the settings file was just created. The drawer
        /// entries come from the template, so the demo only needs to be written on the first run.
        /// </summary>
        private void InitializeFirstRunDemo()
        {
            if (!_settingsRepository.WasInitializedFromDefaults)
            {
                return;
            }

            try
            {
                var demoDirectory = Path.Combine(
                    AppConstants.GetUserConfigurationDirectory(),
                    AppConstants.Directories.DemoDrawer);
                DemoDrawerInitializer.EnsureCreated(demoDirectory);
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Failed to create the first-run demo drawer.", ex);
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
            Font? headerIconFont = null;
            try
            {
                logoFont = BrandingFont.Create(AppConstants.Defaults.LogoFontSize);
                headerIconFont = CreateHeaderIconFont();
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
                headerIconFont?.Dispose();
                DrawerLogger.LogWarning($"Unable to apply the embedded branding font. {ex.Message}");
                logoFont = BrandingFont.CreateFallback(AppConstants.Defaults.LogoFontSize);
                headerIconFont = BrandingFont.CreateFallback(AppConstants.Defaults.CompactButtonFontSize);
            }

            _logoFont = logoFont;
            _headerIconFont = headerIconFont;
            logoLabel.Font = _logoFont;
            addDrawerButton.Font = _headerIconFont;
            settingsButton.Font = _headerIconFont;
        }

        /// <summary>
        /// The header actions are icon buttons, so their captions are resolved from the installed
        /// symbol font at runtime instead of being hard-coded in the designer file.
        /// </summary>
        private void ApplyHeaderButtonGlyphs()
        {
            addDrawerButton.Text = IconGlyphs.AddDrawer;
            settingsButton.Text = IconGlyphs.Settings;

            // A glyph caption is not readable, so assistive tools get the action names instead.
            addDrawerButton.AccessibleName = "Add drawer";
            settingsButton.AccessibleName = "Settings";
            logoLabel.AccessibleDescription = "Drag to move the window. Right-click to open settings.";
        }

        private static Font CreateHeaderIconFont()
        {
            return new Font(IconGlyphs.FontFamilyName, AppConstants.Defaults.CompactButtonFontSize, FontStyle.Regular, GraphicsUnit.Point);
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
            logoLabel.MouseDown += LogoBar_MouseDown;
            logoLabel.MouseMove += HeaderDragMove;
            logoLabel.MouseUp += HeaderDragEnd;
            headerTableLayoutPanel.MouseDown += LogoBar_MouseDown;
        }

        /// <summary>
        /// Opens the settings panel from the title bar. Using the right button keeps the left button
        /// free for dragging and gives a locked window a way to reach the settings even though the
        /// header buttons are hidden.
        /// </summary>
        private void LogoBar_MouseDown(object? sender, MouseEventArgs args)
        {
            if (args.Button != MouseButtons.Right)
            {
                return;
            }

            DrawerLogger.LogInformation("Settings requested from the main-window title bar.");
            OpenSettingsPanel();
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

            CancelDrawerAppearanceTransition();
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
        /// Applies the window-lock state: dragging is disabled, the header action buttons are hidden
        /// and the title spans all header columns, so only the title text and the drawer list stay
        /// clickable. Settings remain reachable from the tray menu or a right-click on the title bar.
        /// </summary>
        private void ApplyWindowLocked(bool locked)
        {
            logoLabel.Cursor = locked ? Cursors.Default : Cursors.SizeAll;
            if (locked)
            {
                _isDragging = false;
            }

            addDrawerButton.Visible = !locked;
            settingsButton.Visible = !locked;

            // Let the title span all three header columns while the action buttons are hidden, so the
            // brand text keeps its full width without caching (DPI-dependent) column widths.
            headerTableLayoutPanel.SetColumnSpan(logoLabel, locked ? 3 : 1);
            headerTableLayoutPanel.PerformLayout();

            headerToolTip.SetToolTip(
                logoLabel,
                locked ? "Right-click for settings" : "Drag to move · Right-click for settings");
        }

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
                    BeginInvoke(new Action(() => ApplySettingsSnapshot(snapshot)));
                }
                else
                {
                    ApplySettingsSnapshot(snapshot);
                }
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Failed to reload settings from repository.", ex);
                UiMessages.ShowError(this, "Settings Error", $"Failed to refresh drawers: {ex.Message}");
            }
        }

        private void ApplySettingsSnapshot(SettingsSnapshot snapshot)
        {
            _settingsSnapshot = snapshot;
            DrawerLogger.LogDebug("Applying settings snapshot to UI.");

            StartPosition = FormStartPosition.Manual;
            ApplyWindowLocked(snapshot.WindowLocked);

            // The frame interval lives in the TOML file; applying it here covers startup and reloads.
            Animations.Configure(snapshot.AnimationFrameMilliseconds);

            ApplyMainWindowBounds(snapshot);
            ApplyAlwaysOnTop(snapshot.AlwaysOnTop);
            ApplyTheme(snapshot.BuildThemePalette());
            PopulateDrawerList(snapshot.Drawers);
            // Keep the list selection in sync with the drawer that is still open.
            UpdateSelectedDrawer();

            // The global shortcut follows the same preview/save/cancel semantics as the other startup
            // options. The flag allows OnHandleCreated to register it when this runs before the handle
            // exists (the constructor path).
            _settingsApplied = true;
            SyncHotkeyRegistration(_toggleHotkeyPreview ?? snapshot.ToggleHotkeyEnabled);
        }

        private void ApplyMainWindowBounds(SettingsSnapshot snapshot)
        {
            var dpiScale = DpiScale.Factor(DeviceDpi);
            var desiredWidth = DpiScale.Pixels(AppConstants.Defaults.MainWindowWidth, dpiScale);
            var desiredHeight = DpiScale.Pixels(snapshot.WindowHeight, dpiScale);
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

            ApplyBaseWindowOpacity(theme.WindowOpacity);
            BackColor = theme.WindowBackground;

            mainTableLayoutPanel.BackColor = theme.WindowBackground;
            headerTableLayoutPanel.BackColor = Color.Transparent;

            logoLabel.ForeColor = TextContrast.For(theme.WindowBackground);
            logoLabel.BackColor = Color.Transparent;

            UiButtonTheming.ApplyAccentPalette(settingsButton, theme);
            UiButtonTheming.ApplyAccentPalette(addDrawerButton, theme);

            drawerListBox.FillColor = theme.ContentBackground;
            drawerListBox.ForeColor = theme.ContentTextPrimary;
            drawerListBox.HoverColor = theme.CardBackground;
            drawerListBox.ItemSelectBackColor = theme.Accent;
            drawerListBox.ItemSelectForeColor = theme.AccentText;

            if (_currentDrawerWindow != null && !_currentDrawerWindow.IsDisposed)
            {
                _currentDrawerWindow.ApplyTheme(theme);
            }
        }

        /// <summary>
        /// The theme opacity is the fade base. Updating it while a fade runs keeps the current frame and
        /// continues towards the new value, so the settings preview never fights the transition.
        /// </summary>
        private void ApplyBaseWindowOpacity(double opacity)
        {
            if (_windowFade is null)
            {
                Opacity = Math.Clamp(opacity, 0d, 1d);
                return;
            }

            _windowFade.BaseOpacity = opacity;
        }

        /// <summary>Fades the main window in from a fraction of the configured opacity.</summary>
        private void PlayWindowFadeIn()
        {
            if (_shutdownInProgress)
            {
                return;
            }

            _windowFade ??= new WindowFade(this, _currentTheme.WindowOpacity);
            _windowFade.BaseOpacity = _currentTheme.WindowOpacity;
            _windowFade.FadeIn(
                AppConstants.Ui.Animation.ShortDurationMilliseconds,
                AppConstants.Ui.Animation.FadeInStartFactor);
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


