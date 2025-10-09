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
            PopulateDrawerList(_sampleDrawerNames);

            FormClosed += (_, _) =>
            {
                _logoFont?.Dispose();
                // _headerGlyph?.Dispose();
                _fontCollection.Dispose();
            };
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
            drawerListPanel.SuspendLayout();
            drawerListPanel.Controls.Clear();

            foreach (var drawerName in drawerNames)
            {
                drawerListPanel.Controls.Add(CreateDrawerChip(drawerName));
            }

            drawerListPanel.ResumeLayout();
        }

        private Control CreateDrawerChip(string drawerName)
        {
            var availableWidth = drawerListPanel.ClientSize.Width - drawerListPanel.Padding.Horizontal;
            if (availableWidth <= 0)
            {
                availableWidth = drawerListPanel.Width - drawerListPanel.Padding.Horizontal - drawerListPanel.Margin.Horizontal;
            }
            availableWidth = Math.Max(150, availableWidth);

            var wrapper = new Panel
            {
                Width = availableWidth,
                Height = 40,
                Margin = new Padding(0, 0, 0, 10),
                // BackColor = Color.FromArgb(80, 51, 31)
            };

            var label = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = drawerName,
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0)
            };

            wrapper.Controls.Add(label);

            wrapper.MouseEnter += (_, _) => wrapper.BackColor = Color.FromArgb(96, 62, 37);
            wrapper.MouseLeave += (_, _) => wrapper.BackColor = Color.FromArgb(80, 51, 31);
            wrapper.Cursor = Cursors.Hand;
            wrapper.Click += (_, _) => ShowDrawerDetails(drawerName);
            label.Click += (_, _) => ShowDrawerDetails(drawerName);

            return wrapper;
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

        private void drawerListPanel_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
