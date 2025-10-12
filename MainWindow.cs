using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
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
        private NotifyIcon? _trayIcon;
        private ContextMenuStrip? _trayMenu;

        public MainWindow()
        {
            _settingsRepository = new SettingsRepository(SettingsRepository.GetDefaultPath());
            _settingsSnapshot = _settingsRepository.GetSnapshot();
            _currentTheme = _settingsSnapshot.BuildThemePalette();

            InitializeComponent();
            drawerListBox.SelectedIndexChanged += DrawerListBox_SelectedIndexChanged;

            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            Opacity = _currentTheme.WindowOpacity;

            LoadBrandingAssets();
            InitializeTray();
            WireDragHandlers();
            ApplySettings(_settingsSnapshot);

            LocationChanged += MainWindow_LocationChanged;

            FormClosed += (_, _) =>
            {
                _logoFont?.Dispose();
                // _headerGlyph?.Dispose();
                _fontCollection.Dispose();

                // Safe tray cleanup: do not dispose the Form.Icon here because other UI code
                // (dialogs) may still access it. NotifyIcon.Dispose will release its own icon handle.
                try
                {
                    if (_trayIcon != null)
                    {
                        _trayIcon.Visible = false;
                        _trayIcon.Dispose();
                        _trayIcon = null;
                    }
                }
                catch { }

                try
                {
                    _trayMenu?.Dispose();
                    _trayMenu = null;
                }
                catch { }
            };

            // 在窗体加载完成后填充菜单，避免默认打开
        }

        private void LoadBrandingAssets()
        {
            var baseDir = AppContext.BaseDirectory;

            TryLoadFont(Path.Combine(baseDir, "assets", "fonts", "Lobster-Regular.ttf"));
            TryApplyIcon(Path.Combine(baseDir, "assets", "drawer.icon.4.ico"));
            // TryApplyHeaderIcon(Path.Combine(baseDir, "assets", "icons", "question_unknown.png"));
        }

        private void TryLoadFont(string fontPath)
        {
            if (!File.Exists(fontPath))
            {
                return;
            }

            _fontCollection.AddFontFile(fontPath);
            if (_fontCollection.Families.Length == 0)
            {
                return;
            }

            _logoFont = new Font(_fontCollection.Families[0], 24F, FontStyle.Regular, GraphicsUnit.Point);
            logoLabel.Font = _logoFont;
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
                    Icon = new Icon(iconPath, new Size(32, 32));
                }
                catch
                {
                    // ignore and keep existing icon if loading fails
                }
            }
        }

        private void InitializeTray()
        {
            var baseDir = AppContext.BaseDirectory;
            var iconPath = Path.Combine(baseDir, "assets", "drawer.icon.4.ico");

            try
            {
                _trayMenu = new ContextMenuStrip();
                var showHideItem = new ToolStripMenuItem("Show/Hide");
                showHideItem.Click += (_, _) => ToggleVisibility();

                var settingsItem = new ToolStripMenuItem("Settings");
                settingsItem.Click += (_, _) => settingsButton_Click(this, EventArgs.Empty);

                var exitItem = new ToolStripMenuItem("Exit");
                exitItem.Click += (_, _) =>
                {
                    try
                    {
                        if (_trayIcon != null) _trayIcon.Visible = false;
                    }
                    catch { }
                    Application.Exit();
                };

                _trayMenu.Items.AddRange(new ToolStripItem[] { showHideItem, settingsItem, new ToolStripSeparator(), exitItem });

                _trayIcon = new NotifyIcon
                {
                    ContextMenuStrip = _trayMenu,
                    Text = "winDrawer",
                    Visible = true
                };

                if (File.Exists(iconPath))
                {
                    try
                    {
                        // Prefer a 16x16 frame for tray icons so Windows displays the provided icon
                        _trayIcon.Icon = new Icon(iconPath, new Size(16, 16));
                    }
                    catch
                    {
                        // fallback to form icon if reading the file fails
                        if (Icon != null) _trayIcon.Icon = Icon;
                    }
                }
                else if (Icon != null)
                {
                    // Assign a clone so the tray and form do not share the same Icon instance.
                    _trayIcon.Icon = (Icon)Icon.Clone();
                }

                _trayIcon.DoubleClick += (_, _) => ToggleVisibility();
            }
            catch
            {
                // Non-fatal: tray is optional
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
                Hide();
            }
            else
            {
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
        }

        private void DrawerListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_suppressSelectionChange) return;

            var selectedIndex = drawerListBox.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= _currentDrawers.Count)
            {
                return;
            }

            var selectedDrawer = _currentDrawers[selectedIndex];
            if (_currentOpenDrawer != null && string.Equals(_currentOpenDrawer.Path, selectedDrawer.Path, StringComparison.OrdinalIgnoreCase))
            {
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
                return;
            }

            if (!Directory.Exists(drawer.Path))
            {
                MessageBox.Show($"Folder not found:{Environment.NewLine}{drawer.Path}", "Drawer Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var iconSize = _settingsSnapshot.GetThumbnailSize();
                var drawerWindow = new DrawerWindow(_settingsRepository, drawer, iconSize, _currentTheme);
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
            }
            catch (Exception ex)
            {
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
            _settingsRepository.Save();
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            // Show Settings as a modeless window that closes when it loses focus (click outside).
            var settings = new SettingsForm(_settingsSnapshot);
            EventHandler<ThemePreviewEventArgs>? previewHandler = (_, args) => ApplyTheme(args.Palette);
            settings.ThemePreviewChanged += previewHandler;

            // Close the settings window when it deactivates (user clicked outside)
            settings.Deactivate += (_, _) =>
            {
                try
                {
                    if (!settings.IsDisposed) settings.Close();
                }
                catch { }
            };

            settings.FormClosed += (_, _) =>
            {
                try
                {
                    settings.ThemePreviewChanged -= previewHandler;
                }
                catch { }

                // If the settings requested a shutdown we respect it.
                if (settings.ShutdownRequested)
                {
                    ApplyTheme(_settingsSnapshot.BuildThemePalette());
                    Close();
                    return;
                }

                // Apply changes if user accepted, otherwise revert preview.
                if (settings.DialogResult == DialogResult.OK)
                {
                    _settingsSnapshot = settings.ResultSettings;
                    _settingsRepository.Update(_settingsSnapshot);
                    _settingsRepository.Save();
                    ApplySettings(_settingsSnapshot);
                }
                else
                {
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
        }

        private void addDrawerButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Add Drawer flow goes here.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
            // System.Diagnostics.Process.Start("explorer.exe", @"C:\");
        }

        private void openDrawerFolderButton_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select the folder you want to link as a drawer."
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                MessageBox.Show($"Selected folder: {dialog.SelectedPath}", "Drawer Source", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ReloadSettingsFromStorage()
        {
            try
            {
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
                MessageBox.Show(this, $"Failed to refresh drawers: {ex.Message}", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplySnapshot(SettingsSnapshot snapshot)
        {
            _settingsSnapshot = snapshot;
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
