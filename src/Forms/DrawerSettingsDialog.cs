using Sunny.UI;
using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer;

internal sealed class DrawerSettingsDialog : Form
{
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _pathTextBox = new();
    private readonly UIPanel _colorPreview = new();
    private readonly ColorDialog _colorDialog = new();
    private readonly List<Font> _ownedFonts = [];
    private readonly Font _inputFont = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
    private Font? _buttonFont;
    private Font? _titleFont;
    private readonly TableLayoutPanel _layoutPanel = new();
    private readonly TableLayoutPanel _headerPanel = new();
    private readonly Label _titleLabel = new();
    private readonly UIGroupBox _detailsGroup = new();
    private readonly UIGroupBox _overlayGroup = new();
    private readonly UIButton _browseButton = new();
    private readonly UIButton _clearColorButton = new();
    private readonly UIButton _saveButton = new();
    private readonly UIButton _deleteButton = new();
    private readonly UIButton _cancelButton = new();
    private readonly Label _overlayInfoLabel = new();
    private readonly TableLayoutPanel _fieldsPanel = new();
    private readonly FlowLayoutPanel _colorPanel = new();
    private readonly FlowLayoutPanel _footerPanel = new();
    private Color? _selectedColor;
    private bool _isDragging;
    private Point _dragOffset;

    public DrawerSettingsDialog(DrawerDefinition drawer)
    {
        ArgumentNullException.ThrowIfNull(drawer);

        Text = $"Drawer settings: {drawer.Name}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(600, 500);
        MinimumSize = new Size(540, 480);
        DoubleBuffered = true;

        ConfigureColorDialog(_colorDialog);

        ConfigureBrandFont();
        ConfigureLayout(drawer);
        ApplyStyle();
        ApplyBrandFonts();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyBrandFonts();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // A user close without an explicit result (for example Alt+F4) must behave like Cancel and
        // must not submit the draft values.
        if (e.CloseReason == CloseReason.UserClosing && DialogResult == DialogResult.None)
        {
            DialogResult = DialogResult.Cancel;
        }

        base.OnFormClosing(e);
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
        }
    }

    private void ConfigureBrandFont()
    {
        _buttonFont = BrandingFont.Create(10.5F);
        _titleFont = BrandingFont.Create(13F);
        _ownedFonts.Add(_buttonFont);
        _ownedFonts.Add(_titleFont);
    }

    private void ApplyBrandFonts()
    {
        _titleLabel.Font = _titleFont!;
        foreach (var button in GetButtons())
        {
            button.Font = _buttonFont!;
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
        _layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 168F));
        _layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));

        _headerPanel.Dock = DockStyle.Fill;
        _headerPanel.ColumnCount = 1;
        _headerPanel.RowCount = 1;
        _headerPanel.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        _headerPanel.Padding = new Padding(20, 0, 10, 0);
        _headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _headerPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _headerPanel.Controls.Add(_titleLabel, 0, 0);

        _titleLabel.Text = $"Drawer settings: {_nameTextBox.Text}";
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.AutoSize = false;
        _titleLabel.BackColor = Color.Transparent;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _titleLabel.Cursor = Cursors.SizeAll;
        _titleLabel.MouseDown += Header_MouseDown;
        _titleLabel.MouseMove += Header_MouseMove;
        _titleLabel.MouseUp += Header_MouseUp;
        _headerPanel.MouseDown += Header_MouseDown;
        _headerPanel.MouseMove += Header_MouseMove;
        _headerPanel.MouseUp += Header_MouseUp;

        _detailsGroup.Text = "Drawer details";
        _detailsGroup.Font = _inputFont;
        _detailsGroup.Dock = DockStyle.Fill;
        _detailsGroup.Margin = new Padding(18, 5, 18, 6);
        _detailsGroup.Padding = new Padding(18, 38, 18, 10);
        _detailsGroup.Controls.Add(_fieldsPanel);

        _fieldsPanel.Dock = DockStyle.Fill;
        _fieldsPanel.ColumnCount = 2;
        _fieldsPanel.RowCount = 3;
        _fieldsPanel.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        _fieldsPanel.Padding = new Padding(2, 4, 2, 0);
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
        _fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        _fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        _fieldsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        _fieldsPanel.Controls.Add(CreateLabel("Name"), 0, 0);
        _nameTextBox.Dock = DockStyle.Fill;
        _nameTextBox.Margin = new Padding(0, 5, 0, 5);
        _nameTextBox.BorderStyle = BorderStyle.FixedSingle;
        _fieldsPanel.Controls.Add(_nameTextBox, 1, 0);
        _fieldsPanel.Controls.Add(CreateLabel("Folder"), 0, 1);
        ConfigurePathDisplay();
        _fieldsPanel.Controls.Add(_pathTextBox, 1, 1);

        // Browse gets its own full-width row below the path, so narrowing the dialog cannot squeeze
        // the path and the button against each other.
        _browseButton.Dock = DockStyle.Fill;
        _browseButton.Margin = new Padding(0, 4, 0, 4);
        _fieldsPanel.Controls.Add(_browseButton, 1, 2);

        ConfigureOverlayGroup();

        _footerPanel.Dock = DockStyle.Fill;
        _footerPanel.FlowDirection = FlowDirection.RightToLeft;
        _footerPanel.WrapContents = false;
        _footerPanel.Padding = new Padding(10, 10, 18, 8);

        ConfigureButton(_cancelButton, "Cancel", new Size(92, 38));
        _cancelButton.DialogResult = DialogResult.Cancel;
        ConfigureButton(_saveButton, "Save", new Size(92, 38));
        ConfigureButton(_deleteButton, "Delete", new Size(92, 38));
        _deleteButton.Click += (_, _) =>
        {
            DeleteRequested = true;
            DialogResult = DialogResult.Yes;
        };
        _saveButton.Click += SaveButton_Click;
        _browseButton.Text = "Browse...";
        _browseButton.Click += BrowseButton_Click;

        _footerPanel.Controls.AddRange([_saveButton, _cancelButton, _deleteButton]);
        _layoutPanel.Controls.Add(_headerPanel, 0, 0);
        _layoutPanel.Controls.Add(_detailsGroup, 0, 1);
        _layoutPanel.Controls.Add(_overlayGroup, 0, 2);
        _layoutPanel.Controls.Add(_footerPanel, 0, 3);
        Controls.Add(_layoutPanel);

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;
    }

    /// <summary>
    /// The folder path is a display-only field: clicking or activating it runs the same folder picker
    /// as the Browse button, so the draft path can only change through the picker.
    /// </summary>
    private void ConfigurePathDisplay()
    {
        _pathTextBox.Dock = DockStyle.Fill;
        _pathTextBox.Margin = new Padding(0, 5, 0, 5);
        _pathTextBox.BorderStyle = BorderStyle.FixedSingle;
        _pathTextBox.ReadOnly = true;
        _pathTextBox.TabStop = true;
        _pathTextBox.Cursor = Cursors.Hand;
        _pathTextBox.AccessibleName = "Drawer folder path";
        _pathTextBox.AccessibleDescription = "Read-only folder path. Activate to choose a different folder.";
        _pathTextBox.Click += BrowseButton_Click;
        _pathTextBox.KeyDown += PathDisplay_KeyDown;
    }

    private void PathDisplay_KeyDown(object? sender, KeyEventArgs e)
    {
        UiDialogHelpers.HandleActivationKey(e, () => BrowseButton_Click(sender, EventArgs.Empty));
    }

    private void ConfigureOverlayGroup()
    {
        _overlayGroup.Text = "Overlay color";
        _overlayGroup.Font = _inputFont;
        _overlayGroup.Dock = DockStyle.Fill;
        _overlayGroup.Margin = new Padding(18, 0, 18, 4);
        _overlayGroup.Padding = new Padding(14, 34, 14, 8);
        var overlayContentPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.FromArgb(64, 64, 64),
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        overlayContentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        overlayContentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        overlayContentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        overlayContentPanel.Controls.Add(_colorPanel, 0, 0);
        overlayContentPanel.Controls.Add(_overlayInfoLabel, 0, 1);
        _overlayGroup.Controls.Add(overlayContentPanel);

        _colorPanel.Dock = DockStyle.Fill;
        _colorPanel.FlowDirection = FlowDirection.LeftToRight;
        _colorPanel.WrapContents = false;
        _colorPanel.Padding = new Padding(0);

        _colorPreview.Size = new Size(240, 84);
        _colorPreview.MinimumSize = new Size(1, 1);
        _colorPreview.Font = _inputFont;
        _colorPreview.Margin = new Padding(0, 0, 12, 0);
        _colorPreview.Padding = new Padding(8, 4, 8, 4);
        _colorPreview.TextAlignment = ContentAlignment.MiddleCenter;
        _colorPreview.AccessibleRole = AccessibleRole.PushButton;
        _colorPreview.AccessibleName = "Overlay color preview";
        _colorPreview.Cursor = Cursors.Hand;
        _colorPreview.TabStop = true;
        _colorPreview.Click += ChooseColorButton_Click;
        _colorPreview.KeyDown += ColorPreview_KeyDown;
        _colorPanel.Controls.Add(_colorPreview);

        _clearColorButton.Text = "No overlay";
        _clearColorButton.Size = new Size(132, 40);
        _clearColorButton.Margin = new Padding(0, 20, 0, 0);
        _clearColorButton.Click += (_, _) => SetSelectedColor(null);
        _colorPanel.Controls.Add(_clearColorButton);

        _overlayInfoLabel.Text = "Overlay uses a fixed 70% blend and affects this drawer only.";
        _overlayInfoLabel.Font = _inputFont;
        _overlayInfoLabel.Dock = DockStyle.Fill;
        _overlayInfoLabel.AutoSize = false;
        _overlayInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
        _overlayInfoLabel.Margin = new Padding(0);

        UpdateColorPreview();
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
        _overlayGroup.FillColor = Color.FromArgb(64, 64, 64);
        _overlayGroup.RectColor = Color.FromArgb(96, 96, 96);
        _overlayGroup.ForeColor = Color.FromArgb(224, 224, 224);

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
            label.Font = _inputFont;
        }

        ConfigureButtonStyle(_browseButton, Color.SteelBlue, Color.CornflowerBlue, Color.DarkSlateBlue);
        ConfigureButtonStyle(_clearColorButton, Color.SteelBlue, Color.CornflowerBlue, Color.DarkSlateBlue);
        ConfigureButtonStyle(_saveButton, Color.SeaGreen, Color.MediumSeaGreen, Color.DarkGreen);
        ConfigureButtonStyle(_deleteButton, Color.SteelBlue, Color.CornflowerBlue, Color.DarkSlateBlue);
        ConfigureButtonStyle(_cancelButton, Color.Firebrick, Color.IndianRed, Color.DarkRed);
        UpdateColorPreview();
    }

    private static void ConfigureButtonStyle(UIButton button, Color fill, Color hover, Color pressed)
    {
        UiButtonTheming.ApplyColorPalette(button, fill, hover, pressed, Color.White);
        button.Radius = 20;
        button.Cursor = Cursors.Hand;
    }

    private IEnumerable<UIButton> GetButtons()
    {
        yield return _browseButton;
        yield return _clearColorButton;
        yield return _saveButton;
        yield return _deleteButton;
        yield return _cancelButton;
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
            UiMessages.ShowWarning(this, "Drawer Settings", "Drawer name cannot be empty.");
            _nameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(DrawerPath))
        {
            UiMessages.ShowWarning(this, "Drawer Settings", "Folder path cannot be empty.");
            _pathTextBox.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
    }

    private void ChooseColorButton_Click(object? sender, EventArgs e)
    {
        _colorDialog.Color = _selectedColor ?? AppConstants.Defaults.ThemeColor;
        _colorDialog.CustomColors = UiDialogHelpers.BuildColorDialogCustomColors(_colorDialog.Color, _selectedColor);

        if (_colorDialog.ShowDialog(this) == DialogResult.OK)
        {
            SetSelectedColor(_colorDialog.Color);
        }
    }

    private void ColorPreview_KeyDown(object? sender, KeyEventArgs e)
    {
        UiDialogHelpers.HandleActivationKey(e, () => ChooseColorButton_Click(sender, EventArgs.Empty));
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
            _colorPreview.FillColor = Color.FromArgb(64, 64, 64);
            _colorPreview.RectColor = Color.FromArgb(128, 128, 128);
            _colorPreview.ForeColor = Color.FromArgb(224, 224, 224);
            _colorPreview.AccessibleDescription = "No overlay. Click to choose an overlay color.";
            _colorPreview.Text = $"Click to change color{Environment.NewLine}No overlay";
            return;
        }

        var blendedColor = ThemePalette.BlendOverlayColor(Color.FromArgb(64, 64, 64), color);
        _colorPreview.FillColor = blendedColor;
        _colorPreview.RectColor = ThemePalette.GetReadableTextColor(blendedColor);
        _colorPreview.ForeColor = ThemePalette.GetReadableTextColor(blendedColor);
        _colorPreview.AccessibleDescription = $"Overlay color {ThemePalette.FormatHexColor(color)}. Click to change color.";
        _colorPreview.Text = $"Click to change color{Environment.NewLine}{ThemePalette.FormatHexColor(color)}";
    }

    private static void ConfigureColorDialog(ColorDialog dialog)
    {
        dialog.AllowFullOpen = true;
        dialog.FullOpen = true;
        dialog.AnyColor = true;
        dialog.SolidColorOnly = true;
    }
}
