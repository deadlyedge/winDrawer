using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;
using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer
{
    public partial class MainWindow : Form
    {
        private readonly PrivateFontCollection _fontCollection = new();
        private Font? _logoFont;
        // private Image? _headerGlyph;
        private bool _isDragging;
        private Point _dragOffset;
        private readonly SettingsRepository _settingsRepository;
        private SettingsSnapshot _settingsSnapshot;
        private ThemePalette _currentTheme;
        private readonly List<DrawerDefinition> _currentDrawers = new();
        private DrawerWindow? _currentDrawerWindow;
        private DrawerDefinition? _currentOpenDrawer;
        private bool _suppressSelectionChange;
        private TrayController? _trayController;

        public MainWindow()
        {
            DrawerLogger.LogInformation("Initializing MainWindow.");

            _settingsRepository = new SettingsRepository(SettingsRepository.GetDefaultPath());
            _settingsSnapshot = _settingsRepository.GetSnapshot();
            _currentTheme = _settingsSnapshot.BuildThemePalette();

            DrawerLogger.LogDebug($"Loaded {_settingsSnapshot.Drawers.Count} drawers from settings.");

            InitializeComponent();
            drawerListBox.SelectedIndexChanged += DrawerListBox_SelectedIndexChanged;

            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            Opacity = _currentTheme.WindowOpacity;

            LoadBrandingAssets();
            InitializeTray();
            WireDragHandlers();
            ApplySettings(_settingsSnapshot);

            DrawerLogger.LogInformation("MainWindow initialization completed.");

            LocationChanged += MainWindow_LocationChanged;

            FormClosed += (_, _) =>
            {
                _logoFont?.Dispose();
                // _headerGlyph?.Dispose();
                _fontCollection.Dispose();

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
            var fontPath = Path.Combine(assetsDirectory, AppConstants.Directories.Fonts, AppConstants.Files.LogoFontFileName);
            var iconPath = Path.Combine(assetsDirectory, AppConstants.Files.AppIconFileName);

            DrawerLogger.LogDebug($"Loading branding assets from '{assetsDirectory}'.");

            TryLoadFont(fontPath);
            TryApplyIcon(iconPath);
            // TryApplyHeaderIcon(Path.Combine(baseDir, "assets", "icons", "question_unknown.png"));
        }

        private void TryLoadFont(string fontPath)
        {
            if (!File.Exists(fontPath))
            {
                DrawerLogger.LogWarning($"Logo font not found at '{fontPath}'.");
                return;
            }

            _fontCollection.AddFontFile(fontPath);
            if (_fontCollection.Families.Length == 0)
            {
                DrawerLogger.LogWarning($"No font families discovered in '{fontPath}'.");
                return;
            }

            _logoFont = new Font(_fontCollection.Families[0], AppConstants.Defaults.LogoFontSize, FontStyle.Regular, GraphicsUnit.Point);
            logoLabel.Font = _logoFont;

            DrawerLogger.LogInformation($"Loaded logo font '{_fontCollection.Families[0].Name}'.");
        }

        private void TryApplyIcon(string iconPath)
        {
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

        private void InitializeTray()
        {
            var baseDir = AppContext.BaseDirectory;
            var iconPath = Path.Combine(baseDir, AppConstants.Directories.Assets, AppConstants.Files.AppIconFileName);

            DrawerLogger.LogDebug($"Initializing tray icon using '{iconPath}'.");

            _trayController = new TrayController(
                ToggleVisibility,
                () => settingsButton_Click(this, EventArgs.Empty),
                () =>
                {
                    DrawerLogger.LogInformation("Application exit initiated from tray.");
                    Application.Exit();
                });

            _trayController.Initialize(iconPath, Icon, AppConstants.Branding.ApplicationDisplayName);
        }

        // Message filter used to detect clicks outside the settings window so we can close it.
        // It will only act after the settings window has been activated (to avoid closing
        // when the mouse merely moves over/near the window).
        private class ClickOutsideFilter : IMessageFilter
        {
            private readonly Form _target;
            public bool WasActivated { get; set; }

            public ClickOutsideFilter(Form target)
            {
                _target = target;
                WasActivated = false;
            }

            public bool PreFilterMessage(ref Message m)
            {
                const int WM_LBUTTONDOWN = 0x0201;
                const int WM_RBUTTONDOWN = 0x0204;
                const int WM_MBUTTONDOWN = 0x0207;

                // Only close on an actual mouse button down and only after the settings window
                // received focus/activation. This prevents spurious closes when the mouse moves.
                if (!WasActivated) return false;

                if (m.Msg == WM_LBUTTONDOWN || m.Msg == WM_RBUTTONDOWN || m.Msg == WM_MBUTTONDOWN)
                {
                    try
                    {
                        var pt = Control.MousePosition;
                        if (_target == null || _target.IsDisposed) return false;
                        // Use Bounds which is in screen coordinates.
                        if (!_target.Bounds.Contains(pt))
                        {
                            // Close on UI thread
                            _target.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    if (!_target.IsDisposed) _target.Close();
                                }
                                catch { }
                            }));
                            // We don't need to consume the message; return false to allow normal processing.
                            return false;
                        }
                    }
                    catch { }
                }

                return false;
            }
        }

        private void ToggleVisibility()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ToggleVisibility));
                return;
            }

            if (Visible && WindowState != FormWindowState.Minimized)
            {
                DrawerLogger.LogInformation("Hiding main window from tray request.");
                Hide();
            }
            else
            {
                DrawerLogger.LogInformation("Showing main window from tray request.");
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }

        // private void TryApplyHeaderIcon(string iconPath)
        // {
        //     if (!File.Exists(iconPath))
        //     {
        //         headerIcon.Visible = false;
        //         return;
        //     }

        //     _headerGlyph = Image.FromFile(iconPath);
        //     headerIcon.Image = _headerGlyph;
        //     headerIcon.Visible = true;
        // }
        private void PopulateDrawerList(IEnumerable<DrawerDefinition> drawers)
        {
            drawerListBox.SelectedIndexChanged -= DrawerListBox_SelectedIndexChanged;

            drawerListBox.Items.Clear();
            _currentDrawers.Clear();

            if (drawers != null)
            {
                foreach (var drawer in drawers)
                {
                    if (drawer == null)
                    {
                        continue;
                    }

                    var displayName = string.IsNullOrWhiteSpace(drawer.Name)
                        ? "(unnamed drawer)"
                        : drawer.Name;

                    _currentDrawers.Add(drawer);
                    var itemLabel = Directory.Exists(drawer.Path)
                        ? displayName
                        : $"{displayName} (missing)";
                    drawerListBox.Items.Add(itemLabel);
                }
            }

            if (_currentDrawers.Count == 0)
            {
                drawerListBox.Items.Add("No drawers configured");
                drawerListBox.Enabled = false;
            }
            else
            {
                drawerListBox.Enabled = true;
            }

            _suppressSelectionChange = true;
            drawerListBox.SelectedIndex = -1;
            _suppressSelectionChange = false;
            drawerListBox.SelectedIndexChanged += DrawerListBox_SelectedIndexChanged;

            DrawerLogger.LogInformation($"Drawer list populated with {_currentDrawers.Count} entries.");
        }

        private void DrawerListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_suppressSelectionChange) return;

            var selectedIndex = drawerListBox.SelectedIndex;
            DrawerLogger.LogDebug($"Drawer list selection changed to index {selectedIndex}.");
            if (selectedIndex < 0 || selectedIndex >= _currentDrawers.Count)
            {
                return;
            }

            var selectedDrawer = _currentDrawers[selectedIndex];
            if (_currentOpenDrawer != null && string.Equals(_currentOpenDrawer.Path, selectedDrawer.Path, StringComparison.OrdinalIgnoreCase))
            {
                DrawerLogger.LogInformation($"Closing drawer '{selectedDrawer.Name}' via toggle.");
                // 点击同一个drawer，切换：关闭
                _currentDrawerWindow?.Close();
                _currentDrawerWindow = null;
                _currentOpenDrawer = null;
                _suppressSelectionChange = true;
                drawerListBox.SelectedIndex = -1; // 立即清空选中状态
                _suppressSelectionChange = false;
            }
            else
            {
                DrawerLogger.LogInformation($"Opening drawer '{selectedDrawer.Name}'.");
                // 点击不同drawer或没有打开的，关闭当前的（如果有），打开新的
                _suppressSelectionChange = true;
                drawerListBox.SelectedIndex = -1; // 立即清空旧的选中状态
                _suppressSelectionChange = false;
                if (_currentDrawerWindow != null)
                {
                    _currentDrawerWindow.Close();
                }
                OpenDrawer(selectedDrawer);
                _currentOpenDrawer = selectedDrawer;
                _suppressSelectionChange = true;
                drawerListBox.SelectedIndex = selectedIndex; // 设置新的选中状态
                _suppressSelectionChange = false;
            }
        }

        private void OpenDrawer(DrawerDefinition drawer)
        {
            if (string.IsNullOrWhiteSpace(drawer.Path))
            {
                DrawerLogger.LogWarning($"Requested to open drawer '{drawer.Name}' without a path.");
                return;
            }

            if (!Directory.Exists(drawer.Path))
            {
                DrawerLogger.LogWarning($"Drawer path not found for '{drawer.Name}': {drawer.Path}");
                MessageBox.Show($"Folder not found:{Environment.NewLine}{drawer.Path}", "Drawer Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                DrawerLogger.LogInformation($"Creating drawer window for '{drawer.Name}' ({drawer.Path}).");
                var drawerWindow = new DrawerWindow(_settingsRepository, drawer, _currentTheme);
                drawerWindow.StartPosition = FormStartPosition.Manual;
                drawerWindow.Location = new Point(Right, Top);
                _currentDrawerWindow = drawerWindow;
                drawerWindow.FormClosed += (_, _) =>
                {
                    ReloadSettingsFromStorage();
                    _currentDrawerWindow = null;
                    _currentOpenDrawer = null;
                    // 清空list中的选中状态
                    if (drawerListBox.InvokeRequired)
                    {
                        drawerListBox.BeginInvoke(new Action(() =>
                        {
                            _suppressSelectionChange = true;
                            drawerListBox.SelectedIndex = -1;
                            _suppressSelectionChange = false;
                        }));
                    }
                    else
                    {
                        _suppressSelectionChange = true;
                        drawerListBox.SelectedIndex = -1;
                        _suppressSelectionChange = false;
                    }
                };
                drawerWindow.Show(this);
                DrawerLogger.LogInformation($"Drawer window displayed for '{drawer.Name}'.");
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError($"Failed to open drawer '{drawer.Name}'.", ex);
                MessageBox.Show($"Failed to open drawer '{drawer.Name}': {ex.Message}", "Drawer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WireDragHandlers()
        {
            // headerPanel.MouseDown += HeaderDragBegin;
            // headerPanel.MouseMove += HeaderDragMove;
            // headerPanel.MouseUp += HeaderDragEnd;

            logoLabel.MouseDown += HeaderDragBegin;
            logoLabel.MouseMove += HeaderDragMove;
            logoLabel.MouseUp += HeaderDragEnd;

            // headerIcon.MouseDown += HeaderDragBegin;
            // headerIcon.MouseMove += HeaderDragMove;
            // headerIcon.MouseUp += HeaderDragEnd;
        }

        private void HeaderDragBegin(object? sender, MouseEventArgs args)
        {
            if (_settingsSnapshot.WindowLocked)
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
            if (!_isDragging)
            {
                return;
            }

            var screenPoint = PointToScreen(args.Location);
            Location = new Point(screenPoint.X - _dragOffset.X, screenPoint.Y - _dragOffset.Y);
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

        private void settingsButton_Click(object sender, EventArgs e)
        {
            DrawerLogger.LogInformation("Opening settings window.");

            // Show Settings as a modeless window that closes when the user *clicks outside* the window.
            var settings = new SettingsForm(_settingsSnapshot);
            EventHandler<ThemePreviewEventArgs>? previewHandler = (_, args) => ApplyTheme(args.Palette);
            settings.ThemePreviewChanged += previewHandler;

            // Use a message filter to detect actual mouse-downs outside the settings window.
            var filter = new ClickOutsideFilter(settings);
            EventHandler? activatedHandler = (_, _) => { filter.WasActivated = true; };
            settings.Activated += activatedHandler;
            Application.AddMessageFilter(filter);

            settings.FormClosed += (_, _) =>
            {
                DrawerLogger.LogInformation($"Settings window closed with result {settings.DialogResult}, shutdown={settings.ShutdownRequested}.");
                try
                {
                    settings.ThemePreviewChanged -= previewHandler;
                }
                catch { }

                // Remove the message filter we added and the activation handler.
                try
                {
                    settings.Activated -= activatedHandler;
                }
                catch { }
                try
                {
                    Application.RemoveMessageFilter(filter);
                }
                catch { }

                // If the settings requested a shutdown we respect it.
                if (settings.ShutdownRequested)
                {
                    DrawerLogger.LogWarning("Settings requested application shutdown.");
                    // Perform only minimal cleanup and exit immediately to avoid blocking UI while
                    // many windows perform synchronous I/O during their close events.
                    try
                    {
                        _trayController?.Dispose();
                        _trayController = null;
                    }
                    catch (Exception ex)
                    {
                        DrawerLogger.LogError("Failed to dispose tray controller during shutdown request.", ex);
                    }

                    // Ensure UI message queue is processed briefly so the cleanup above is applied.
                    try
                    {
                        Application.DoEvents();
                    }
                    catch { }

                    // Try to flush pending settings to disk, wait up to 500ms, then exit.
                    try
                    {
                        var flushTask = _settingsRepository.FlushAsync(TimeSpan.FromMilliseconds(500));
                        if (flushTask == null || !flushTask.Wait(500))
                        {
                            // Flush timed out — force terminate.
                            try { Process.GetCurrentProcess().Kill(); } catch { try { Environment.Exit(0); } catch { } }
                        }
                        else
                        {
                            // Flush completed — exit gracefully.
                            try { Environment.Exit(0); } catch { try { Process.GetCurrentProcess().Kill(); } catch { } }
                        }
                    }
                    catch
                    {
                        // On any unexpected error, force terminate to avoid hanging.
                        try { Process.GetCurrentProcess().Kill(); } catch { try { Environment.Exit(0); } catch { } }
                    }

                    return;
                }

                // Apply changes if user accepted, otherwise revert preview.
                if (settings.DialogResult == DialogResult.OK)
                {
                    DrawerLogger.LogInformation("Applying settings changes from dialog.");
                    _settingsSnapshot = settings.ResultSettings;
                    _settingsRepository.Update(_settingsSnapshot);
                    // Save enqueued by repository's background writer.
                    ApplySettings(_settingsSnapshot);
                }
                else
                {
                    DrawerLogger.LogInformation("Settings dialog cancelled; reverting preview.");
                    ApplyTheme(_settingsSnapshot.BuildThemePalette());
                }

                try
                {
                    settings.Dispose();
                }
                catch { }
            };

            // Show modeless with this window as owner so it appears above and is centered relative to owner.
            settings.Show(this);
            DrawerLogger.LogDebug("Settings window displayed.");
        }

        private void addDrawerButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the folder you want to link as a drawer.",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                DrawerLogger.LogDebug("Add drawer dialog cancelled.");
                return;
            }

            var selectedPath = dialog.SelectedPath;
            if (string.IsNullOrWhiteSpace(selectedPath) || !Directory.Exists(selectedPath))
            {
                DrawerLogger.LogWarning($"Invalid drawer folder selected: '{selectedPath}'.");
                MessageBox.Show(this, $"Invalid folder selected:{Environment.NewLine}{selectedPath}", "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var name = Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            try
            {
                DrawerLogger.LogInformation($"Attempting to add drawer '{name}' at '{selectedPath}'.");
                var added = _settingsRepository.AddDrawer(selectedPath, name);
                if (!added)
                {
                    DrawerLogger.LogInformation($"Drawer '{selectedPath}' already exists.");
                    MessageBox.Show(this, "The selected folder is already configured as a drawer.", "Add Drawer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Reload settings from storage to refresh the UI
                ReloadSettingsFromStorage();
                DrawerLogger.LogInformation($"Drawer added successfully: '{selectedPath}'.");
                MessageBox.Show(this, $"Drawer added: {selectedPath}", "Add Drawer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError($"Failed to add drawer '{selectedPath}'.", ex);
                MessageBox.Show(this, $"Failed to add drawer: {ex.Message}", "Add Drawer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void openDrawerFolderButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the folder you want to link as a drawer."
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                DrawerLogger.LogInformation($"Drawer source folder selected: '{dialog.SelectedPath}'.");
                MessageBox.Show($"Selected folder: {dialog.SelectedPath}", "Drawer Source", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                DrawerLogger.LogDebug("Drawer source folder selection cancelled.");
            }
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

        private void ApplyAccentColor(Color accent)
        {
            if (accent.A == 0)
            {
                return;
            }

            var opaqueAccent = Color.FromArgb(255, accent.R, accent.G, accent.B);
            addDrawerButton.FillColor = opaqueAccent;
            addDrawerButton.FillHoverColor = ControlPaint.Light(opaqueAccent);
            addDrawerButton.FillPressColor = ControlPaint.Dark(opaqueAccent);
            addDrawerButton.FillSelectedColor = addDrawerButton.FillPressColor;
        }

        private void ApplySettings(SettingsSnapshot snapshot)
        {
            StartPosition = FormStartPosition.Manual;
            Location = snapshot.WindowPosition;
            ApplyTheme(snapshot.BuildThemePalette());
            PopulateDrawerList(snapshot.Drawers);
            // 如果有当前打开的drawer，更新list中的选中状态
            UpdateSelectedDrawer();
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

        private void UpdateSelectedDrawer()
        {
            _suppressSelectionChange = true;
            try
            {
                if (_currentOpenDrawer != null)
                {
                    for (int i = 0; i < _currentDrawers.Count; i++)
                    {
                        if (string.Equals(_currentDrawers[i].Path, _currentOpenDrawer.Path, StringComparison.OrdinalIgnoreCase))
                        {
                            drawerListBox.SelectedIndex = i;
                            return;
                        }
                    }
                }
                else
                {
                    drawerListBox.SelectedIndex = -1;
                }
            }
            finally
            {
                _suppressSelectionChange = false;
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


