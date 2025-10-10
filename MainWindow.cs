using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using Sunny.UI;
using winDrawer.Configuration;

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
        private readonly List<DrawerDefinition> _currentDrawers = new();
        private bool _suppressDrawerSelection;

        public MainWindow()
        {
            _settingsRepository = new SettingsRepository(SettingsRepository.GetDefaultPath());
            _settingsSnapshot = _settingsRepository.GetSnapshot();

            InitializeComponent();
            drawerListBox.SelectedIndexChanged += DrawerListBox_SelectedIndexChanged;

            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            Opacity = 0.92;

            settingsButton.FillColor = Color.FromArgb(66, 66, 66);
            settingsButton.FillHoverColor = Color.FromArgb(84, 84, 84);
            settingsButton.FillPressColor = Color.FromArgb(63, 63, 63);
            settingsButton.FillSelectedColor = settingsButton.FillPressColor;
            settingsButton.RectColor = Color.Transparent;
            settingsButton.RectDisableColor = Color.Transparent;
            settingsButton.RectHoverColor = Color.Transparent;
            settingsButton.RectPressColor = Color.Transparent;
            settingsButton.RectSelectedColor = Color.Transparent;

            var defaultAccent = Color.FromArgb(9, 140, 210);
            addDrawerButton.RectColor = Color.Transparent;
            addDrawerButton.RectDisableColor = Color.Transparent;
            addDrawerButton.RectHoverColor = Color.Transparent;
            addDrawerButton.RectPressColor = Color.Transparent;
            addDrawerButton.RectSelectedColor = Color.Transparent;
            addDrawerButton.FillColor = defaultAccent;
            addDrawerButton.FillHoverColor = ControlPaint.Light(defaultAccent);
            addDrawerButton.FillPressColor = Color.FromArgb(0, 94, 140);
            addDrawerButton.FillSelectedColor = addDrawerButton.FillPressColor;
            // openDrawerFolderButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(112, 79, 54);
            // openDrawerFolderButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(86, 59, 39);

            LoadBrandingAssets();
            WireDragHandlers();
            ApplySettings(_settingsSnapshot);

            FormClosed += (_, _) =>
            {
                _logoFont?.Dispose();
                // _headerGlyph?.Dispose();
                _fontCollection.Dispose();
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
                Icon = new Icon(iconPath);
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

            drawerListBox.SelectedIndex = -1;
            drawerListBox.SelectedIndexChanged += DrawerListBox_SelectedIndexChanged;
        }

        private void DrawerListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_suppressDrawerSelection)
            {
                return;
            }

            var selectedIndex = drawerListBox.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= _currentDrawers.Count)
            {
                return;
            }

            var selectedDrawer = _currentDrawers[selectedIndex];
            OpenDrawer(selectedDrawer);

            _suppressDrawerSelection = true;
            BeginInvoke(new Action(() =>
            {
                try
                {
                    drawerListBox.SelectedIndex = -1;
                }
                finally
                {
                    _suppressDrawerSelection = false;
                }
            }));
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
                var drawerWindow = new DrawerWindow(_settingsRepository, drawer, iconSize);
                drawerWindow.FormClosed += (_, _) => ReloadSettingsFromStorage();
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
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            using var settings = new SettingsForm(_settingsSnapshot);
            var dialogResult = settings.ShowDialog(this);

            if (settings.ShutdownRequested)
            {
                Close();
                return;
            }

            if (dialogResult == DialogResult.OK)
            {
                _settingsSnapshot = settings.ResultSettings;
                _settingsRepository.Update(_settingsSnapshot);
                _settingsRepository.Save();
                ApplySettings(_settingsSnapshot);
            }
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
            ApplyAccentColor(snapshot.GetAccentColor());
            PopulateDrawerList(snapshot.Drawers);
        }


    }
}
