using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;
using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer;

public partial class SettingsForm : UIForm
{
    private const int StartWithWindowsIndex = 0;
    private const int LockPositionIndex = 1;

    private readonly SettingsSnapshot _originalSnapshot;
    private readonly SettingsSnapshot _workingSnapshot;
    private bool _isDragging;
    private Point _dragOffset;

    public SettingsForm(SettingsSnapshot snapshot)
    {
        _originalSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _workingSnapshot = snapshot.Clone();

        InitializeComponent();
        DoubleBuffered = true;

        ConfigureControls();
        ApplySnapshotToUi();
        WireEvents();
        WireDragHandlers();

        ResultSettings = _originalSnapshot.Clone();
        UpdatePreview();
    }

    public event EventHandler<ThemePreviewEventArgs>? ThemePreviewChanged;

    public Color SelectedAccentColor { get; private set; }

    public bool LaunchOnStartup => IsBehaviorChecked(StartWithWindowsIndex);

    public bool ShutdownRequested { get; private set; }

    public SettingsSnapshot ResultSettings { get; private set; }

    private void ConfigureControls()
    {
        hueTrackBar.Minimum = 0;
        hueTrackBar.Maximum = 360;

        saturationTrackBar.Minimum = 0;
        saturationTrackBar.Maximum = 100;

        lightnessTrackBar.Minimum = 0;
        lightnessTrackBar.Maximum = 100;

        opacityTrackBar.Minimum = 0;
        opacityTrackBar.Maximum = 100;

        behaviorCheckBoxGroup.SelectedIndexes ??= new List<int>();
    }

    private void WireEvents()
    {
        hueTrackBar.ValueChanged += OnThemeTrackValueChanged;
        saturationTrackBar.ValueChanged += OnThemeTrackValueChanged;
        lightnessTrackBar.ValueChanged += OnThemeTrackValueChanged;
        opacityTrackBar.ValueChanged += OnThemeTrackValueChanged;

        saveButton.Click += saveButton_Click;
        cancelButton.Click += cancelButton_Click;
        shutDownButton.Click += shutDownButton_Click;
        defautlSettingsButton.Click += defautlSettingsButton_Click;
    }

    private void WireDragHandlers()
    {
        headerTableLayoutPanel.Cursor = Cursors.SizeAll;
        headerSettings.Cursor = Cursors.SizeAll;

        headerTableLayoutPanel.MouseDown += HeaderDragBegin;
        headerTableLayoutPanel.MouseMove += HeaderDragMove;
        headerTableLayoutPanel.MouseUp += HeaderDragEnd;

        headerSettings.MouseDown += HeaderDragBegin;
        headerSettings.MouseMove += HeaderDragMove;
        headerSettings.MouseUp += HeaderDragEnd;
    }

    private void ApplySnapshotToUi()
    {
        var selected = new List<int>();
        if (_workingSnapshot.StartWithWindows)
        {
            selected.Add(StartWithWindowsIndex);
        }

        if (_workingSnapshot.WindowLocked)
        {
            selected.Add(LockPositionIndex);
        }

        behaviorCheckBoxGroup.SelectedIndexes = selected;

        hueTrackBar.Value = Math.Clamp(_workingSnapshot.ThemeHue, hueTrackBar.Minimum, hueTrackBar.Maximum);
        saturationTrackBar.Value = Math.Clamp(_workingSnapshot.ThemeSaturation, saturationTrackBar.Minimum, saturationTrackBar.Maximum);
        lightnessTrackBar.Value = Math.Clamp(_workingSnapshot.ThemeLightness, lightnessTrackBar.Minimum, lightnessTrackBar.Maximum);
        opacityTrackBar.Value = Math.Clamp(_workingSnapshot.ThemeAlpha, opacityTrackBar.Minimum, opacityTrackBar.Maximum);

        UpdateValueLabels();
    }

    private void UpdateValueLabels()
    {
        hueValueLabel.Text = $"{hueTrackBar.Value}\u00B0";
        saturationValueLabel.Text = $"{saturationTrackBar.Value}%";
        lightnessValueLabel.Text = $"{lightnessTrackBar.Value}%";
        opacityValueLabel.Text = $"{opacityTrackBar.Value}%";
    }

    private void OnThemeTrackValueChanged(object? sender, EventArgs e)
    {
        _workingSnapshot.ThemeHue = hueTrackBar.Value;
        _workingSnapshot.ThemeSaturation = saturationTrackBar.Value;
        _workingSnapshot.ThemeLightness = lightnessTrackBar.Value;
        _workingSnapshot.ThemeAlpha = opacityTrackBar.Value;

        UpdateValueLabels();
        UpdatePreview();
    }

    private void saveButton_Click(object? sender, EventArgs e)
    {
        SyncWorkingSnapshotFromUi();
        ResultSettings = _workingSnapshot.Clone();
        SelectedAccentColor = ResultSettings.GetAccentColor();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void cancelButton_Click(object? sender, EventArgs e)
    {
        ResultSettings = _originalSnapshot.Clone();
        SelectedAccentColor = ResultSettings.GetAccentColor();
        ThemePreviewChanged?.Invoke(this, new ThemePreviewEventArgs(_originalSnapshot.BuildThemePalette()));
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void shutDownButton_Click(object? sender, EventArgs e)
    {
        ShutdownRequested = true;
        DialogResult = DialogResult.Abort;
        Close();
    }

    private void defautlSettingsButton_Click(object? sender, EventArgs e)
    {
        Console.WriteLine("[SettingsForm] default settings button clicked.");
    }

    private void uiLabel5_Click(object? sender, EventArgs e)
    {
        Console.WriteLine("[SettingsForm] lightness value label clicked.");
    }

    // private void uiLabel1_Click(object? sender, EventArgs e)
    // {
    //     Console.WriteLine("[SettingsForm] saturation label clicked.");
    // }

    private void UpdatePreview()
    {
        var palette = _workingSnapshot.BuildThemePalette();
        SelectedAccentColor = palette.Accent;
        previewPanel.FillColor = palette.Accent;
        previewPanel.RectColor = palette.Border;
        ThemePreviewChanged?.Invoke(this, new ThemePreviewEventArgs(palette));
        // previewLabel.Text = $"hsla({_workingSnapshot.ThemeHue}, {_workingSnapshot.ThemeSaturation}%, {_workingSnapshot.ThemeLightness}%, {_workingSnapshot.ThemeAlpha / 100f:0.00})";
    }

    private void SyncWorkingSnapshotFromUi()
    {
        var selected = behaviorCheckBoxGroup.SelectedIndexes ?? new List<int>();

        _workingSnapshot.StartWithWindows = selected.Contains(StartWithWindowsIndex);
        _workingSnapshot.WindowLocked = selected.Contains(LockPositionIndex);
    }

    private bool IsBehaviorChecked(int index)
    {
        return behaviorCheckBoxGroup.SelectedIndexes != null && behaviorCheckBoxGroup.SelectedIndexes.Contains(index);
    }

    private void HeaderDragBegin(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _isDragging = true;
        _dragOffset = e.Location;
    }

    private void HeaderDragMove(object? sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        var screenPoint = PointToScreen(e.Location);
        Location = new Point(screenPoint.X - _dragOffset.X, screenPoint.Y - _dragOffset.Y);
    }

    private void HeaderDragEnd(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _isDragging = false;
    }
}
