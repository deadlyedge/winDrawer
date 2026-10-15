using System.Drawing.Text;
using Sunny.UI;
using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer;

internal sealed class DrawerSettingsDialog : Form
{
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _pathTextBox = new();
    private readonly Panel _colorPreview = new();
    private readonly ColorDialog _colorDialog = new();
    private readonly PrivateFontCollection _fontCollection = new();
    private readonly List<Font> _ownedFonts = new();
    private readonly Font _inputFont = new("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
    private readonly TableLayoutPanel _layoutPanel = new();
    private readonly TableLayoutPanel _headerPanel = new();
    private readonly UILabel _titleLabel = new();
    private readonly UIButton _closeButton = new();
    private readonly UIGroupBox _detailsGroup = new();
    private readonly UIButton _browseButton = new();
    private readonly UIButton _chooseColorButton = new();
    private readonly UIButton _clearColorButton = new();
    private readonly UIButton _saveButton = new();
    private readonly UIButton _deleteButton = new();
    private readonly UIButton _cancelButton = new();
    private readonly UILabel _overlayInfoLabel = new();
    private readonly TableLayoutPanel _fieldsPanel = new();
    private readonly FlowLayoutPanel _colorPanel = new();
    private readonly FlowLayoutPanel _footerPanel = new();
    private Color? _selectedColor;
    private bool _isDragging;
    private Point _dragOffset;

    public DrawerSettingsDialog(DrawerDefinition drawer)
    {
        ArgumentNullException.ThrowIfNull(drawer);

        Text = "Drawer settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(560, 330);
        MinimumSize = new Size(500, 310);
        DoubleBuffered = true;

        ConfigureColorDialog(_colorDialog);

        ConfigureBrandFont();
        ConfigureLayout(drawer);
        ApplyStyle();
    }

    public string DrawerName => _nameTextBox.Text.Trim();

    public string DrawerPath => _pathTextBox.Text.Trim();

    public bool DeleteRequested { get; private set; }

    public Color? OverlayColor
    {
        get
        {
            if (_selectedColor is not { } selected)
            {
                return null;
            }

            return Color.FromArgb(179, selected.R, selected.G, selected.B);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _colorDialog.Dispose();
        }

        base.Dispose(disposing);

        if (disposing)
        {
            foreach (var font in _ownedFonts)
            {
                font.Dispose();
            }

            _inputFont.Dispose();
            _fontCollection.Dispose();
        }
    }

    private void ConfigureBrandFont()
    {
        var fontPath = Path.Combine(
            AppContext.BaseDirectory,
            AppConstants.Directories.Assets,
            AppConstants.Directories.Fonts,
            AppConstants.Files.LogoFontFileName);
        _titleLabel.Font = CreateOwnedFont("Segoe UI Semibold", 14F);
        foreach (var button in GetButtons())
        {
            if (ReferenceEquals(button, _closeButton))
            {
                continue;
            }

            button.Font = CreateOwnedFont("Segoe UI", 10F);
        }

        if (!File.Exists(fontPath))
        {
            return;
        }

        try
        {
            _fontCollection.AddFontFile(fontPath);
            if (_fontCollection.Families.Length > 0)
            {
                var brandFont = _fontCollection.Families[0];
                _titleLabel.Font = CreateBrandFont(brandFont, 14F);
                foreach (var button in GetButtons())
                {
                    if (ReferenceEquals(button, _closeButton))
                    {
                        continue;
                    }

                    button.Font = CreateBrandFont(brandFont, 10.5F);
                }
            }
        }
        catch (Exception ex)
        {
            DrawerLogger.LogWarning($"Unable to load the drawer settings brand font from '{fontPath}'. {ex.Message}");
        }
    }

    private void ConfigureLayout(DrawerDefinition drawer)
    {
        _nameTextBox.Text = drawer.Name;
        _pathTextBox.Text = drawer.Path;
        _selectedColor = drawer.OverlayColor;

        _layoutPanel.Dock = DockStyle.Fill;
        _layoutPanel.ColumnCount = 1;
        _layoutPanel.RowCount = 4;
        _layoutPanel.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        _layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        _layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        _layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));

        _headerPanel.Dock = DockStyle.Fill;
        _headerPanel.ColumnCount = 2;
        _headerPanel.RowCount = 1;
        _headerPanel.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        _headerPanel.Padding = new Padding(20, 0, 10, 0);
        _headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38F));
        _headerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _headerPanel.Controls.Add(_titleLabel, 0, 0);
        _headerPanel.Controls.Add(_closeButton, 1, 0);

        _titleLabel.Text = "winDrawer settings";
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _titleLabel.Cursor = Cursors.SizeAll;
        _titleLabel.MouseDown += Header_MouseDown;
        _titleLabel.MouseMove += Header_MouseMove;
        _titleLabel.MouseUp += Header_MouseUp;
        _headerPanel.MouseDown += Header_MouseDown;
        _headerPanel.MouseMove += Header_MouseMove;
        _headerPanel.MouseUp += Header_MouseUp;

        _closeButton.Text = "\uE8BB";
        _closeButton.Font = CreateOwnedFont("Segoe MDL2 Assets", 12F);
        _closeButton.Dock = DockStyle.Fill;
        _closeButton.Margin = new Padding(2, 6, 0, 6);
        _closeButton.Radius = 2;
        _closeButton.Cursor = Cursors.Hand;
        _closeButton.AccessibleName = "Close drawer settings";
        _closeButton.Click += (_, _) => DialogResult = DialogResult.Cancel;

        _detailsGroup.Text = "drawer";
        _detailsGroup.Font = CreateLogoFont(9F);
        _detailsGroup.Dock = DockStyle.Fill;
        _detailsGroup.Margin = new Padding(18, 5, 18, 6);
        _detailsGroup.Padding = new Padding(18, 38, 18, 12);
        _detailsGroup.Controls.Add(_fieldsPanel);

        _fieldsPanel.Dock = DockStyle.Fill;
        _fieldsPanel.ColumnCount = 3;
        _fieldsPanel.RowCount = 3;
        _fieldsPanel.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        _fieldsPanel.Padding = new Padding(2, 4, 2, 0);
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        _fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        _fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        _fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _fieldsPanel.Controls.Add(CreateLabel("Name"), 0, 0);
        _nameTextBox.Dock = DockStyle.Fill;
        _nameTextBox.Margin = new Padding(0, 5, 0, 5);
        _nameTextBox.BorderStyle = BorderStyle.FixedSingle;
        _fieldsPanel.Controls.Add(_nameTextBox, 1, 0);
        _fieldsPanel.SetColumnSpan(_nameTextBox, 2);
        _fieldsPanel.Controls.Add(CreateLabel("Folder"), 0, 1);
        _pathTextBox.Dock = DockStyle.Fill;
        _pathTextBox.Margin = new Padding(0, 5, 0, 5);
        _pathTextBox.BorderStyle = BorderStyle.FixedSingle;
        _fieldsPanel.Controls.Add(_pathTextBox, 1, 1);
        _fieldsPanel.Controls.Add(_browseButton, 2, 1);
        _fieldsPanel.Controls.Add(CreateLabel("Overlay"), 0, 2);

        _colorPanel.Dock = DockStyle.Fill;
        _colorPanel.FlowDirection = FlowDirection.LeftToRight;
        _colorPanel.WrapContents = false;
        _colorPanel.Padding = new Padding(0);
        _colorPreview.Size = new Size(42, 30);
        _colorPreview.Margin = new Padding(0, 2, 10, 0);
        _colorPreview.BorderStyle = BorderStyle.FixedSingle;
        _colorPreview.AccessibleName = "Drawer overlay color preview";
        UpdateColorPreview();

        _chooseColorButton.Text = "choose color";
        _chooseColorButton.Size = new Size(128, 34);
        _chooseColorButton.Margin = new Padding(0, 0, 8, 0);
        _chooseColorButton.Click += ChooseColorButton_Click;

        _clearColorButton.Text = "no overlay";
        _clearColorButton.Size = new Size(112, 34);
        _clearColorButton.Click += (_, _) => SetSelectedColor(null);
        _colorPanel.Controls.Add(_colorPreview);
        _colorPanel.Controls.Add(_chooseColorButton);
        _colorPanel.Controls.Add(_clearColorButton);
        _fieldsPanel.Controls.Add(_colorPanel, 1, 2);
        _fieldsPanel.SetColumnSpan(_colorPanel, 2);
        _overlayInfoLabel.Text = "Overlay uses a fixed 70% blend.";
        _overlayInfoLabel.Font = CreateLogoFont(9F);
        _overlayInfoLabel.Dock = DockStyle.Fill;
        _overlayInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
        _overlayInfoLabel.Padding = new Padding(22, 0, 0, 0);

        _footerPanel.Dock = DockStyle.Fill;
        _footerPanel.FlowDirection = FlowDirection.RightToLeft;
        _footerPanel.WrapContents = false;
        _footerPanel.Padding = new Padding(10, 10, 18, 8);

        ConfigureButton(_cancelButton, "cancel", new Size(92, 38));
        _cancelButton.DialogResult = DialogResult.Cancel;
        ConfigureButton(_saveButton, "save", new Size(92, 38));
        ConfigureButton(_deleteButton, "delete", new Size(92, 38));
        _deleteButton.Click += (_, _) =>
        {
            DeleteRequested = true;
            DialogResult = DialogResult.Yes;
        };
        _saveButton.Click += SaveButton_Click;
        _browseButton.Text = "browse...";
        _browseButton.Dock = DockStyle.Fill;
        _browseButton.Margin = new Padding(8, 3, 0, 3);
        _browseButton.Click += BrowseButton_Click;

        _footerPanel.Controls.AddRange([_saveButton, _cancelButton, _deleteButton]);
        _layoutPanel.Controls.Add(_headerPanel, 0, 0);
        _layoutPanel.Controls.Add(_detailsGroup, 0, 1);
        _layoutPanel.Controls.Add(_overlayInfoLabel, 0, 2);
        _layoutPanel.Controls.Add(_footerPanel, 0, 3);
        Controls.Add(_layoutPanel);

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;
    }

    private void ApplyStyle()
    {
        BackColor = Color.DimGray;
        ForeColor = Color.FromArgb(224, 224, 224);

        _layoutPanel.BackColor = Color.FromArgb(64, 64, 64);
        _headerPanel.BackColor = Color.FromArgb(64, 64, 64);
        _titleLabel.ForeColor = Color.White;

        _detailsGroup.FillColor = Color.FromArgb(64, 64, 64);
        _detailsGroup.RectColor = Color.FromArgb(96, 96, 96);
        _detailsGroup.ForeColor = Color.FromArgb(224, 224, 224);

        _overlayInfoLabel.ForeColor = Color.FromArgb(192, 192, 192);
        _nameTextBox.BackColor = Color.FromArgb(56, 56, 56);
        _nameTextBox.ForeColor = Color.FromArgb(224, 224, 224);
        _nameTextBox.Font = _inputFont;
        _pathTextBox.BackColor = Color.FromArgb(56, 56, 56);
        _pathTextBox.ForeColor = Color.FromArgb(224, 224, 224);
        _pathTextBox.Font = _inputFont;
        _fieldsPanel.BackColor = Color.FromArgb(64, 64, 64);
        _colorPanel.BackColor = Color.FromArgb(64, 64, 64);
        _footerPanel.BackColor = Color.FromArgb(64, 64, 64);

        foreach (var label in FindLabels(this))
        {
            if (ReferenceEquals(label, _titleLabel))
            {
                continue;
            }

            label.ForeColor = Color.FromArgb(224, 224, 224);
            label.BackColor = Color.Transparent;
            label.Font = CreateLogoFont(9F);
        }

        ConfigureButtonStyle(_closeButton, Color.FromArgb(64, 64, 64), Color.FromArgb(96, 96, 96), Color.FromArgb(48, 48, 48));
        ConfigureButtonStyle(_browseButton, Color.FromArgb(9, 140, 210), Color.FromArgb(0, 94, 140), Color.FromArgb(0, 74, 112));
        ConfigureButtonStyle(_chooseColorButton, Color.FromArgb(9, 140, 210), Color.FromArgb(0, 94, 140), Color.FromArgb(0, 74, 112));
        ConfigureButtonStyle(_clearColorButton, Color.FromArgb(96, 96, 96), Color.FromArgb(120, 120, 120), Color.FromArgb(72, 72, 72));
        ConfigureButtonStyle(_saveButton, Color.FromArgb(9, 140, 210), Color.FromArgb(0, 94, 140), Color.FromArgb(0, 74, 112));
        ConfigureButtonStyle(_deleteButton, Color.DarkRed, Color.LightCoral, Color.FromArgb(112, 0, 0));
        ConfigureButtonStyle(_cancelButton, Color.Red, Color.LightCoral, Color.DarkRed);
        UpdateColorPreview();
    }

    private static void ConfigureButtonStyle(UIButton button, Color fill, Color hover, Color pressed)
    {
        button.FillColor = fill;
        button.FillHoverColor = hover;
        button.FillPressColor = pressed;
        button.FillSelectedColor = pressed;
        button.ForeColor = Color.White;
        button.RectColor = fill;
        button.RectHoverColor = hover;
        button.RectPressColor = pressed;
        button.RectSelectedColor = pressed;
        button.Radius = 20;
        button.Cursor = Cursors.Hand;
    }

    private IEnumerable<UIButton> GetButtons()
    {
        yield return _closeButton;
        yield return _browseButton;
        yield return _chooseColorButton;
        yield return _clearColorButton;
        yield return _saveButton;
        yield return _deleteButton;
        yield return _cancelButton;
    }

    private Font CreateBrandFont(FontFamily family, float size)
    {
        var font = new Font(family, size, FontStyle.Regular, GraphicsUnit.Point);
        _ownedFonts.Add(font);
        return font;
    }

    private Font CreateOwnedFont(string familyName, float size)
    {
        var font = new Font(familyName, size, FontStyle.Regular, GraphicsUnit.Point);
        _ownedFonts.Add(font);
        return font;
    }

    private Font CreateLogoFont(float size)
    {
        var family = _fontCollection.Families.FirstOrDefault();
        return family is null
            ? CreateOwnedFont("Segoe UI", size)
            : CreateBrandFont(family, size);
    }

    private static void ConfigureButton(UIButton button, string text, Size size)
    {
        button.Text = text;
        button.Size = size;
        button.MinimumSize = new Size(1, 1);
        button.Margin = new Padding(6, 0, 0, 0);
    }

    private static IEnumerable<Label> FindLabels(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is Label label)
            {
                yield return label;
            }

            foreach (var childLabel in FindLabels(control))
            {
                yield return childLabel;
            }
        }
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            Margin = new Padding(0, 2, 8, 2)
        };
    }

    private void Header_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _isDragging = true;
        ((Control)sender!).Capture = true;
        var screenPoint = ((Control)sender!).PointToScreen(e.Location);
        _dragOffset = new Point(screenPoint.X - Left, screenPoint.Y - Top);
    }

    private void Header_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var screenPoint = ((Control)sender!).PointToScreen(e.Location);
            Location = new Point(screenPoint.X - _dragOffset.X, screenPoint.Y - _dragOffset.Y);
        }
    }

    private void Header_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = false;
            ((Control)sender!).Capture = false;
        }
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the folder for this drawer.",
            ShowNewFolderButton = true,
            SelectedPath = Directory.Exists(_pathTextBox.Text) ? _pathTextBox.Text : string.Empty
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _pathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DrawerName))
        {
            MessageBox.Show(this, "Drawer name cannot be empty.", "Drawer Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _nameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(DrawerPath))
        {
            MessageBox.Show(this, "Folder path cannot be empty.", "Drawer Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _pathTextBox.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
    }

    private void ChooseColorButton_Click(object? sender, EventArgs e)
    {
        _colorDialog.Color = _selectedColor ?? AppConstants.Defaults.ThemeColor;
        _colorDialog.CustomColors = GetDrawerDialogCustomColors(_selectedColor, _colorDialog.Color);

        if (_colorDialog.ShowDialog(this) == DialogResult.OK)
        {
            SetSelectedColor(_colorDialog.Color);
        }
    }

    private void SetSelectedColor(Color? color)
    {
        _selectedColor = color;
        UpdateColorPreview();
    }

    private void UpdateColorPreview()
    {
        if (_selectedColor is not { } color)
        {
            _colorPreview.BackColor = Color.FromArgb(64, 64, 64);
            return;
        }

        _colorPreview.BackColor = ThemePalette.BlendOverlayColor(Color.FromArgb(64, 64, 64), color);
    }

    private static void ConfigureColorDialog(ColorDialog dialog)
    {
        dialog.AllowFullOpen = true;
        dialog.FullOpen = true;
        dialog.AnyColor = true;
        dialog.SolidColorOnly = true;
    }

    private static int[] GetDrawerDialogCustomColors(Color? selectedColor, Color initialColor)
    {
        static int ToDialogColor(Color color) =>
            color.R | (color.G << 8) | (color.B << 16);

        var colors = new List<int> { ToDialogColor(initialColor) };
        if (selectedColor is { } selected)
        {
            colors.Add(ToDialogColor(selected));
        }
        else
        {
            colors.Add(ToDialogColor(Color.FromArgb(0, 5, 51)));
        }

        colors.AddRange(
        [
            ToDialogColor(Color.FromArgb(9, 140, 210)),
            ToDialogColor(Color.FromArgb(64, 64, 64)),
            ToDialogColor(Color.White),
            ToDialogColor(Color.Black),
            ToDialogColor(Color.Red),
            ToDialogColor(Color.Green),
            ToDialogColor(Color.Blue),
            ToDialogColor(Color.Yellow),
            ToDialogColor(Color.Magenta),
            ToDialogColor(Color.Cyan),
            ToDialogColor(Color.Orange),
            ToDialogColor(Color.Purple),
            ToDialogColor(Color.LightGray),
            ToDialogColor(Color.DarkGray)
        ]);
        return colors.ToArray();
    }
}
