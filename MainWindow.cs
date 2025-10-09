using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace winDrawer
{
    public partial class MainWindow : Form
    {
        private readonly PrivateFontCollection _fontCollection = new();
        private Font? _logoFont;
        // private Image? _headerGlyph;
        private bool _isDragging;
        private Point _dragOffset;
        private readonly string[] _sampleDrawerNames =
        {
            "Drawer name here",
            "Project: Icon Drawer",
            "Archive / 2024",
            "UI Concepts",
            "Marketing Pack",
            "UI Concepts",
            "Marketing Pack",
            "UI Concepts",
            "Marketing Pack",
            "UI Concepts",
            "Marketing Pack",
            "UI Concepts",
            "Marketing Pack",
            "Drawer name here"
        };

        public MainWindow()
        {
            InitializeComponent();

            // 去除滚动条背景色
            drawerListBox.ScrollBarBackColor = Color.Orange;

            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            Opacity = 0.92;

            settingsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(84, 84, 84);
            settingsButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(63, 63, 63);
            addDrawerButton.FlatAppearance.BorderSize = 0;
            addDrawerButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(9, 140, 210);
            addDrawerButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 94, 140);
            // openDrawerFolderButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(112, 79, 54);
            // openDrawerFolderButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(86, 59, 39);

            LoadBrandingAssets();
            WireDragHandlers();

            FormClosed += (_, _) =>
            {
                _logoFont?.Dispose();
                // _headerGlyph?.Dispose();
                _fontCollection.Dispose();
            };

            // 在窗体加载完成后填充菜单，避免默认打开
            this.Load += (s, e) => PopulateDrawerList(_sampleDrawerNames);
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

        private void PopulateDrawerList(IEnumerable<string> drawerNames)
        {
            drawerListBox.Items.Clear();

            foreach (var drawerName in drawerNames)
            {
                drawerListBox.Items.Add(drawerName);
            }

            // 添加点击事件处理器
            drawerListBox.SelectedIndexChanged += (s, e) =>
            {
                if (s is UIListBox lb && lb.SelectedIndex >= 0 && lb.SelectedIndex < lb.Items.Count)
                {
                    string drawerName = lb.Items[lb.SelectedIndex] as string ?? string.Empty;
                    BeginInvoke(new Action(() =>
                    {
                        ShowDrawerDetails(drawerName);
                    }));
                }
            };
        }

        private void ShowDrawerDetails(string drawerName)
        {
            MessageBox.Show($"Open drawer: {drawerName}", "Drawer Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            using var settings = new SettingsForm();
            var dialogResult = settings.ShowDialog(this);

            if (settings.ShutdownRequested)
            {
                Close();
                return;
            }

            if (dialogResult == DialogResult.OK)
            {
                ApplyAccentColor(settings.SelectedAccentColor);
            }
        }

        private void addDrawerButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Add Drawer flow goes here.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void ApplyAccentColor(Color accent)
        {
            if (accent.A == 0)
            {
                return;
            }

            var opaqueAccent = Color.FromArgb(255, accent.R, accent.G, accent.B);
            // headerPanel.BackColor = opaqueAccent;
            // accentStripe.BackColor = opaqueAccent;
            addDrawerButton.FlatAppearance.MouseOverBackColor = ControlPaint.Light(opaqueAccent);
            addDrawerButton.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(opaqueAccent);
        }


    }
}
