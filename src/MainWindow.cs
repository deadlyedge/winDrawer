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

        private void InitializeTray()
        {
            var baseDir = AppContext.BaseDirectory;
            var iconPath = Path.Combine(baseDir, AppConstants.Directories.Assets, AppConstants.Files.AppIconFileName);

            DrawerLogger.LogDebug($"Initializing tray icon using '{iconPath}'.");

            _trayController = new TrayController(
                ToggleVisibility,
                () => SettingsButton_Click(this, EventArgs.Empty),
                () =>
                {
                    DrawerLogger.LogInformation("Application exit initiated from tray.");
                    Application.Exit();
                });

            _trayController.Initialize(iconPath, Icon, AppConstants.Branding.ApplicationDisplayName);
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

        private void PopulateDrawerList(IEnumerable<DrawerDefinition> drawers)
        {
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
                    drawerListBox.Items.Add(drawer.OverlayColor is { } overlayColor
                        ? new DrawerListItem(itemLabel, overlayColor)
                        : itemLabel);
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
            ClearDragInsertionMarker();
            DrawerLogger.LogInformation($"Drawer list populated with {_currentDrawers.Count} entries.");
        }

        private sealed record DrawerListItem(string Text, Color OverlayColor)
        {
            public override string ToString() => Text;
        }

        private void DrawerListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= drawerListBox.Items.Count)
            {
                return;
            }

            var bounds = e.Bounds;
            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var background = drawerListBox.FillColor;
            var foreground = drawerListBox.ForeColor;
            if (selected)
            {
                background = drawerListBox.ItemSelectBackColor;
                foreground = drawerListBox.ItemSelectForeColor;
            }
            else if ((e.State & DrawItemState.HotLight) == DrawItemState.HotLight)
            {
                background = drawerListBox.HoverColor;
            }

            var item = drawerListBox.Items[e.Index];
            if (item is DrawerListItem coloredDrawerItem)
            {
                background = ThemePalette.BlendOverlayColor(background, coloredDrawerItem.OverlayColor);
            }

            using var backgroundBrush = new SolidBrush(background);
            e.Graphics.FillRectangle(backgroundBrush, bounds);

            var text = item is DrawerListItem drawerItem
                ? drawerItem.Text
                : drawerListBox.GetItemText(item);
            bounds.Inflate(-8, 0);
            TextRenderer.DrawText(
                e.Graphics,
                text,
                drawerListBox.Font,
                bounds,
                foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            e.DrawFocusRectangle();
        }

        private void OpenOrToggleDrawer(int selectedIndex)
        {
            if (_suppressSelectionChange || _isDrawerReordering)
            {
                return;
            }

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

        private void DrawerListBox_MouseDown(object? sender, MouseEventArgs e)
        {
            _drawerDragStarted = false;
            var index = drawerListBox.IndexFromPoint(e.Location);
            if (e.Button == MouseButtons.Right)
            {
                _drawerPointerDown = false;
                _drawerPressedIndex = -1;
                if (index >= 0 && index < _currentDrawers.Count)
                {
                    ShowDrawerSettings(index);
                }
                return;
            }

            if (e.Button != MouseButtons.Left || index < 0 || index >= _currentDrawers.Count)
            {
                _drawerPointerDown = false;
                _drawerPressedIndex = -1;
                return;
            }

            _drawerPointerDown = true;
            _drawerPressedIndex = index;
            _drawerPressPoint = e.Location;
        }

        private void DrawerListBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_drawerPointerDown || _drawerPressedIndex < 0 || e.Button != MouseButtons.Left)
            {
                return;
            }

            var dragBounds = new Rectangle(
                _drawerPressPoint.X - SystemInformation.DragSize.Width / 2,
                _drawerPressPoint.Y - SystemInformation.DragSize.Height / 2,
                SystemInformation.DragSize.Width,
                SystemInformation.DragSize.Height);
            if (dragBounds.Contains(e.Location))
            {
                return;
            }

            _drawerDragStarted = true;
            _isDrawerReordering = true;
            try
            {
                drawerListBox.DoDragDrop(_currentDrawers[_drawerPressedIndex].Path, DragDropEffects.Move);
            }
            finally
            {
                _isDrawerReordering = false;
                _drawerPointerDown = false;
                _drawerPressedIndex = -1;
            }
        }

        private void DrawerListBox_MouseUp(object? sender, MouseEventArgs e)
        {
            var sourceIndex = _drawerPressedIndex;
            var wasDrag = _drawerDragStarted;
            _drawerPointerDown = false;
            _drawerPressedIndex = -1;
            _drawerDragStarted = false;

            if (e.Button == MouseButtons.Left && !wasDrag && sourceIndex >= 0 && sourceIndex < _currentDrawers.Count &&
                drawerListBox.IndexFromPoint(e.Location) == sourceIndex)
            {
                OpenOrToggleDrawer(sourceIndex);
            }
        }

        private void DrawerListBox_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(typeof(string)) == true ? DragDropEffects.Move : DragDropEffects.None;
            UpdateDragInsertionMarker(e);
        }

        private void DrawerListBox_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(typeof(string)) == true ? DragDropEffects.Move : DragDropEffects.None;
            UpdateDragInsertionMarker(e);
        }

        private void DrawerListBox_DragLeave(object? sender, EventArgs e)
        {
            ClearDragInsertionMarker();
        }

        private void UpdateDragInsertionMarker(DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(string)) is not string draggedPath || _currentDrawers.Count == 0)
            {
                ClearDragInsertionMarker();
                return;
            }

            var sourceIndex = _currentDrawers.FindIndex(drawer => PathsEqual(drawer.Path, draggedPath));
            var point = drawerListBox.PointToClient(new Point(e.X, e.Y));
            var rawInsertionIndex = GetRawDropInsertionIndex(point);
            if (sourceIndex < 0 || rawInsertionIndex < 0)
            {
                ClearDragInsertionMarker();
                return;
            }

            _dropRawInsertionIndex = rawInsertionIndex;
            _dropInsertionIndex = Math.Clamp(
                rawInsertionIndex > sourceIndex ? rawInsertionIndex - 1 : rawInsertionIndex,
                0,
                _currentDrawers.Count - 1);

            var markerY = rawInsertionIndex < _currentDrawers.Count
                ? drawerListBox.GetItemRectangle(rawInsertionIndex).Top
                : drawerListBox.GetItemRectangle(_currentDrawers.Count - 1).Bottom;
            var markerHeight = Math.Max(2, (int)Math.Round(3 * DeviceDpi / 96d));
            _drawerInsertionMarker.SetBounds(4, markerY - markerHeight / 2, Math.Max(1, drawerListBox.ClientSize.Width - 8), markerHeight);
            _drawerInsertionMarker.BackColor = _currentTheme.Accent;
            _drawerInsertionMarker.Visible = true;
            _drawerInsertionMarker.BringToFront();
        }

        private int GetRawDropInsertionIndex(Point point)
        {
            var itemIndex = drawerListBox.IndexFromPoint(point);
            if (itemIndex < 0)
            {
                if (point.Y < drawerListBox.GetItemRectangle(0).Top)
                {
                    return 0;
                }

                return point.Y > drawerListBox.ClientSize.Height ? _currentDrawers.Count : -1;
            }

            var itemBounds = drawerListBox.GetItemRectangle(itemIndex);
            return point.Y >= itemBounds.Top + itemBounds.Height / 2 ? itemIndex + 1 : itemIndex;
        }

        private void ClearDragInsertionMarker()
        {
            _dropInsertionIndex = -1;
            _dropRawInsertionIndex = -1;
            _drawerInsertionMarker.Visible = false;
        }
        private void DrawerListBox_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(string)) is not string draggedPath)
            {
                ClearDragInsertionMarker();
                return;
            }

            var sourceIndex = _currentDrawers.FindIndex(drawer => PathsEqual(drawer.Path, draggedPath));
            if (sourceIndex < 0 || _dropInsertionIndex < 0)
            {
                ClearDragInsertionMarker();
                return;
            }

            var destinationIndex = _dropInsertionIndex;
            if (destinationIndex == sourceIndex)
            {
                ClearDragInsertionMarker();
                return;
            }

            var reorderedDrawers = new List<DrawerDefinition>(_currentDrawers);
            var movedDrawer = reorderedDrawers[sourceIndex];
            reorderedDrawers.RemoveAt(sourceIndex);
            reorderedDrawers.Insert(destinationIndex, movedDrawer);

            try
            {
                var orderedPaths = reorderedDrawers.Select(drawer => drawer.Path).ToList();
                if (!_settingsRepository.ReorderDrawers(orderedPaths, out var error))
                {
                    DrawerLogger.LogWarning($"Drawer reorder was rejected: {error}");
                    MessageBox.Show(this, error, "Reorder Drawers", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ReloadSettingsFromStorage();
                    return;
                }

                ApplySnapshot(_settingsRepository.GetSnapshot());
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Failed to reorder drawers.", ex);
                MessageBox.Show(this, $"Failed to save drawer order: {ex.Message}", "Reorder Drawers", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ReloadSettingsFromStorage();
            }
            finally
            {
                ClearDragInsertionMarker();
            }
        }
        private static bool PathsEqual(string first, string second)
        {
            var left = Environment.ExpandEnvironmentVariables(first).Trim();
            var right = Environment.ExpandEnvironmentVariables(second).Trim();
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private void ShowDrawerSettings(int index)
        {
            if (index < 0 || index >= _currentDrawers.Count)
            {
                return;
            }

            var drawer = _currentDrawers[index];
            using var dialog = new DrawerSettingsDialog(drawer);
            var result = dialog.ShowDialog(this);
            if (result == DialogResult.Cancel)
            {
                return;
            }

            if (result == DialogResult.Yes && dialog.DeleteRequested)
            {
                var confirmation = MessageBox.Show(
                    this,
                    $"Remove '{drawer.Name}' from the drawer list? The folder and its files will not be deleted.",
                    "Delete Drawer",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirmation != DialogResult.Yes)
                {
                    return;
                }

                if (!_settingsRepository.DeleteDrawer(drawer.Path, out var deleteError))
                {
                    MessageBox.Show(this, deleteError, "Delete Drawer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                CloseDrawerIfOpen(drawer.Path);
                ReloadSettingsFromStorage();
                return;
            }

            if (result != DialogResult.OK)
            {
                return;
            }

            var requestedPath = Environment.ExpandEnvironmentVariables(dialog.DrawerPath).Trim();
            if (!PathsEqual(drawer.Path, requestedPath) && !Directory.Exists(requestedPath))
            {
                MessageBox.Show(this, $"The selected folder does not exist or is inaccessible:{Environment.NewLine}{requestedPath}",
                    "Drawer Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var pathChanged = !PathsEqual(drawer.Path, requestedPath);
            if (pathChanged && _currentDrawers.Any(existing => !PathsEqual(existing.Path, drawer.Path) && PathsEqual(existing.Path, requestedPath)))
            {
                MessageBox.Show(this, "Another drawer already uses the selected folder.", "Drawer Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_settingsRepository.UpdateDrawer(drawer.Path, dialog.DrawerName, requestedPath, dialog.OverlayColor, out var updateError))
            {
                MessageBox.Show(this, updateError, "Drawer Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (pathChanged)
            {
                CloseDrawerIfOpen(drawer.Path);
            }
            else
            {
                _currentDrawerWindow?.ApplyDrawerDefinition(_settingsRepository.GetSnapshot().Drawers[index]);
            }

            ReloadSettingsFromStorage();
        }

        private void CloseDrawerIfOpen(string path)
        {
            if (_currentOpenDrawer == null || !PathsEqual(_currentOpenDrawer.Path, path))
            {
                return;
            }

            _currentDrawerWindow?.Close();
            _currentDrawerWindow = null;
            _currentOpenDrawer = null;
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
                var drawerWindow = new DrawerWindow(_settingsRepository, drawer, _currentTheme)
                {
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(Right, Top)
                };
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

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            DrawerLogger.LogInformation("Opening settings window.");

            if (_settingsForm is { IsDisposed: false } existingSettings)
            {
                existingSettings.Activate();
                return;
            }

            // Show Settings as a modeless window that closes when the user *clicks outside* the window.
            var settings = new SettingsForm(_settingsSnapshot);
            _settingsForm = settings;
            void previewHandler(object? _, ThemePreviewEventArgs args) => ApplyTheme(args.Palette);
            settings.ThemePreviewChanged += (EventHandler<ThemePreviewEventArgs>?)previewHandler;
            void windowHeightPreviewHandler(object? _1, EventArgs _2)
            {
                var previewSnapshot = _settingsSnapshot.Clone();
                previewSnapshot.WindowHeight = settings.WindowHeightPreview;
                ApplyMainWindowBounds(previewSnapshot);
            }
            settings.WindowHeightPreviewChanged += (EventHandler?)windowHeightPreviewHandler;
            void resetDefaultsHandler(object? _, EventArgs _2)
            {
                var confirmation = MessageBox.Show(
                    this,
                    "Reset all supported startup and main-window options to the application defaults? This takes effect immediately, removes the obsolete icon size option, and keeps drawers and unknown settings.",
                    "Reset Startup Settings",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirmation != DialogResult.Yes)
                {
                    return;
                }

                bool previousStartupState;
                SettingsSnapshot defaults;
                try
                {
                    previousStartupState = StartupRegistration.IsEnabled();
                    defaults = _settingsRepository.GetEmbeddedStartupDefaults();
                }
                catch (Exception ex)
                {
                    DrawerLogger.LogError("Unable to prepare the startup settings reset.", ex);
                    MessageBox.Show(this, $"Unable to reset startup settings:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                        "Reset Startup Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    StartupRegistration.SetEnabled(defaults.StartWithWindows);
                }
                catch (Exception ex)
                {
                    DrawerLogger.LogError("Unable to synchronize Windows login startup for the settings reset.", ex);
                    try
                    {
                        if (StartupRegistration.IsEnabled() != previousStartupState)
                        {
                            StartupRegistration.SetEnabled(previousStartupState);
                        }
                    }
                    catch (Exception rollbackException)
                    {
                        DrawerLogger.LogError("Unable to roll back Windows login startup after the reset registration failed.", rollbackException);
                    }

                    MessageBox.Show(this, $"Windows login startup could not be reset. No settings were changed.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                        "Reset Startup Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!_settingsRepository.ResetStartupToEmbeddedDefaults(TimeSpan.FromSeconds(3), out var resetSnapshot, out var error))
                {
                    try
                    {
                        if (StartupRegistration.IsEnabled() != previousStartupState)
                        {
                            StartupRegistration.SetEnabled(previousStartupState);
                        }
                    }
                    catch (Exception rollbackException)
                    {
                        DrawerLogger.LogError("Unable to restore Windows login startup after a failed settings reset.", rollbackException);
                    }

                    MessageBox.Show(this, error, "Reset Startup Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _settingsSnapshot = resetSnapshot!;
                settings.ApplyResetBaseline(_settingsSnapshot);
                ApplySettings(_settingsSnapshot);
            }
            settings.StartupDefaultsResetRequested += resetDefaultsHandler;

            settings.FormClosed += (_, _) =>
            {
                if (ReferenceEquals(_settingsForm, settings))
                {
                    _settingsForm = null;
                }

                DrawerLogger.LogInformation($"Settings window closed with result {settings.DialogResult}, shutdown={settings.ShutdownRequested}.");
                try
                {
                    settings.ThemePreviewChanged -= (global::System.EventHandler<ThemePreviewEventArgs>?)previewHandler;
                    settings.WindowHeightPreviewChanged -= (global::System.EventHandler?)windowHeightPreviewHandler;
                    settings.StartupDefaultsResetRequested -= (global::System.EventHandler?)resetDefaultsHandler;
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
                    var resultSettings = settings.ResultSettings;
                    _settingsSnapshot = new SettingsSnapshot(
                        resultSettings.StartWithWindows,
                        resultSettings.WindowLocked,
                        resultSettings.WindowColor,
                        resultSettings.WindowOpacity,
                        _settingsSnapshot.WindowPosition,
                        _settingsSnapshot.Drawers,
                        resultSettings.WindowHeight);
                    _settingsRepository.Update(_settingsSnapshot);
                    // Save enqueued by repository's background writer.
                    ApplyMainWindowBounds(_settingsSnapshot);
                    ApplySettings(_settingsSnapshot);
                }
                else
                {
                    DrawerLogger.LogInformation("Settings dialog cancelled; reverting preview.");
                    settings.RestoreWindowHeightPreview();
                    ApplyMainWindowBounds(_settingsSnapshot);
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

        private void AddDrawerButton_Click(object sender, EventArgs e)
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


