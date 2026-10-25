using System.ComponentModel;

namespace winDrawer;

/// <summary>
/// Native replacement for SunnyUI's two-column check-box group. The option set is fixed by the
/// settings page; keeping it here preserves the SelectedIndexes/ValueChanged API used by SettingsForm.
/// </summary>
internal sealed class BehaviorCheckBoxGroup : GroupBox
{
    private static readonly string[] OptionTexts =
    [
        "Start with Windows",
        "Lock position",
        "Always on top",
        "Enable Win+Shift+D"
    ];

    private readonly List<CheckBox> _checkBoxes = [];
    private bool _updatingSelection;

    public BehaviorCheckBoxGroup()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(64, 64, 64);
        ForeColor = Color.FromArgb(224, 224, 224);
        Padding = new Padding(12, 36, 12, 8);

        for (var index = 0; index < OptionTexts.Length; index++)
        {
            var checkBox = new CheckBox
            {
                AutoSize = false,
                BackColor = BackColor,
                ForeColor = ForeColor,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Text = OptionTexts[index],
                TextAlign = ContentAlignment.MiddleLeft,
                AccessibleName = OptionTexts[index],
                TabStop = true
            };
            checkBox.CheckedChanged += CheckBox_CheckedChanged;
            _checkBoxes.Add(checkBox);
            Controls.Add(checkBox);
        }
    }

    public event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<int> SelectedIndexes
    {
        get
        {
            var selected = new List<int>();
            for (var index = 0; index < _checkBoxes.Count; index++)
            {
                if (_checkBoxes[index].Checked)
                {
                    selected.Add(index);
                }
            }

            return selected;
        }
        set
        {
            _updatingSelection = true;
            try
            {
                var selected = value ?? [];
                for (var index = 0; index < _checkBoxes.Count; index++)
                {
                    _checkBoxes[index].Checked = selected.Contains(index);
                }
            }
            finally
            {
                _updatingSelection = false;
            }
        }
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        const int columnCount = 2;
        const int itemHeight = 34;
        const int rowGap = 12;
        var availableWidth = Math.Max(1, ClientSize.Width - Padding.Horizontal);
        var columnWidth = Math.Max(1, availableWidth / columnCount);

        for (var index = 0; index < _checkBoxes.Count; index++)
        {
            var column = index % columnCount;
            var row = index / columnCount;
            var bounds = new Rectangle(
                Padding.Left + (column * columnWidth),
                Padding.Top + (row * (itemHeight + rowGap)),
                columnWidth,
                itemHeight);
            _checkBoxes[index].Bounds = bounds;
        }
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        foreach (var checkBox in _checkBoxes)
        {
            checkBox.BackColor = BackColor;
        }
    }

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        foreach (var checkBox in _checkBoxes)
        {
            checkBox.ForeColor = ForeColor;
        }
    }

    private void CheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (!_updatingSelection)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
