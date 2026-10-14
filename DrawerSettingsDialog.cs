using winDrawer.Configuration;

namespace winDrawer;

internal sealed class DrawerSettingsDialog : Form
{
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _pathTextBox = new();
    private readonly Panel _colorPreview = new();
    private readonly ColorDialog _colorDialog = new();
    private Color? _selectedColor;

    public DrawerSettingsDialog(DrawerDefinition drawer)
    {
        ArgumentNullException.ThrowIfNull(drawer);

        Text = "Drawer settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(520, 270);

        _nameTextBox.Text = drawer.Name;
        _nameTextBox.SetBounds(126, 20, 370, 27);
        _nameTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _pathTextBox.Text = drawer.Path;
        _pathTextBox.SetBounds(126, 61, 285, 27);
        _pathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var nameLabel = CreateLabel("Name", 20, 22);
        var pathLabel = CreateLabel("Folder", 20, 63);
        var browseButton = new Button { Text = "Browse...", Bounds = new Rectangle(421, 60, 75, 29), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        browseButton.Click += BrowseButton_Click;

        _selectedColor = drawer.OverlayColor;
        _colorPreview.SetBounds(126, 104, 44, 30);
        _colorPreview.BorderStyle = BorderStyle.FixedSingle;
        UpdateColorPreview();
        _colorPreview.AccessibleName = "Drawer overlay color preview";

        var colorLabel = CreateLabel("Overlay", 20, 108);
        var chooseColorButton = new Button { Text = "Choose color...", Bounds = new Rectangle(180, 104, 112, 30) };
        chooseColorButton.Click += ChooseColorButton_Click;
        var clearColorButton = new Button { Text = "No overlay", Bounds = new Rectangle(300, 104, 95, 30) };
        clearColorButton.Click += (_, _) => SetSelectedColor(null);

        var overlayInfoLabel = CreateLabel("Fixed 70% opacity", 126, 153);

        var saveButton = new Button { Text = "Save", Bounds = new Rectangle(250, 222, 78, 30) };
        var deleteButton = new Button { Text = "Delete", Bounds = new Rectangle(334, 222, 78, 30) };
        var cancelButton = new Button { Text = "Cancel", Bounds = new Rectangle(418, 222, 78, 30), DialogResult = DialogResult.Cancel };
        saveButton.Click += SaveButton_Click;
        deleteButton.Click += (_, _) =>
        {
            DeleteRequested = true;
            DialogResult = DialogResult.Yes;
        };

        Controls.AddRange(new Control[]
        {
            nameLabel, _nameTextBox, pathLabel, _pathTextBox, browseButton,
            colorLabel, _colorPreview, chooseColorButton, clearColorButton,
            overlayInfoLabel,
            saveButton, deleteButton, cancelButton
        });

        AcceptButton = saveButton;
        CancelButton = cancelButton;
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
    }

    private static Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Location = new Point(x, y)
        };
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
        if (_selectedColor is { } current)
        {
            _colorDialog.Color = current;
        }

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
            _colorPreview.BackColor = SystemColors.Control;
            return;
        }

        const double opacity = 0.7d;
        _colorPreview.BackColor = Color.FromArgb(
            (int)Math.Round(SystemColors.Control.R * (1 - opacity) + color.R * opacity),
            (int)Math.Round(SystemColors.Control.G * (1 - opacity) + color.G * opacity),
            (int)Math.Round(SystemColors.Control.B * (1 - opacity) + color.B * opacity));
    }
}