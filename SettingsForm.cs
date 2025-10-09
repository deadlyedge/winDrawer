using System;
using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;
using winDrawer.Configuration;

namespace winDrawer;

public partial class SettingsForm : UIForm
{
    private readonly SettingsSnapshot _originalSnapshot;
    private readonly SettingsSnapshot _workingSnapshot;

    public SettingsForm(SettingsSnapshot snapshot)
    {
        _originalSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _workingSnapshot = snapshot.Clone();

        InitializeComponent();
        DoubleBuffered = true;

        ConfigureControls();
        ApplySnapshotToUi();
        WireEvents();
        ResultSettings = _originalSnapshot.Clone();
        UpdatePreview();
    }

    public Color SelectedAccentColor { get; private set; }

    public bool LaunchOnStartup => autoLaunchSwitch.Active;

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

        alphaTrackBar.Minimum = 0;
        alphaTrackBar.Maximum = 100;
    }

    private void WireEvents()
    {
        hueTrackBar.ValueChanged += OnThemeTrackValueChanged;
        saturationTrackBar.ValueChanged += OnThemeTrackValueChanged;
        lightnessTrackBar.ValueChanged += OnThemeTrackValueChanged;
        alphaTrackBar.ValueChanged += OnThemeTrackValueChanged;

        autoLaunchSwitch.ActiveChanged += OnAutoLaunchChanged;
        windowLockSwitch.ActiveChanged += OnWindowLockChanged;

        okButton.Click += okButton_Click;
        cancelButton.Click += cancelButton_Click;
        shutdownButton.Click += shutdownButton_Click;
    }

    private void ApplySnapshotToUi()
    {
        autoLaunchSwitch.Active = _workingSnapshot.StartWithWindows;
        windowLockSwitch.Active = _workingSnapshot.WindowLocked;

        hueTrackBar.Value = Math.Clamp(_workingSnapshot.ThemeHue, hueTrackBar.Minimum, hueTrackBar.Maximum);
        saturationTrackBar.Value = Math.Clamp(_workingSnapshot.ThemeSaturation, saturationTrackBar.Minimum, saturationTrackBar.Maximum);
        lightnessTrackBar.Value = Math.Clamp(_workingSnapshot.ThemeLightness, lightnessTrackBar.Minimum, lightnessTrackBar.Maximum);
        alphaTrackBar.Value = Math.Clamp(_workingSnapshot.ThemeAlpha, alphaTrackBar.Minimum, alphaTrackBar.Maximum);

        UpdateValueLabels();
    }

    private void UpdateValueLabels()
    {
        hueValueLabel.Text = $"{hueTrackBar.Value}°";
        saturationValueLabel.Text = $"{saturationTrackBar.Value}%";
        lightnessValueLabel.Text = $"{lightnessTrackBar.Value}%";
        alphaValueLabel.Text = $"{alphaTrackBar.Value}%";
    }

    private void OnThemeTrackValueChanged(object? sender, EventArgs e)
    {
        _workingSnapshot.ThemeHue = hueTrackBar.Value;
        _workingSnapshot.ThemeSaturation = saturationTrackBar.Value;
        _workingSnapshot.ThemeLightness = lightnessTrackBar.Value;
        _workingSnapshot.ThemeAlpha = alphaTrackBar.Value;

        UpdateValueLabels();
        UpdatePreview();
    }

    private void OnAutoLaunchChanged(object? sender, EventArgs e)
    {
        _workingSnapshot.StartWithWindows = autoLaunchSwitch.Active;
    }

    private void OnWindowLockChanged(object? sender, EventArgs e)
    {
        _workingSnapshot.WindowLocked = windowLockSwitch.Active;
    }

    private void okButton_Click(object? sender, EventArgs e)
    {
        ResultSettings = _workingSnapshot.Clone();
        SelectedAccentColor = ResultSettings.GetAccentColor();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void cancelButton_Click(object? sender, EventArgs e)
    {
        ResultSettings = _originalSnapshot.Clone();
        SelectedAccentColor = ResultSettings.GetAccentColor();
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void shutdownButton_Click(object? sender, EventArgs e)
    {
        ShutdownRequested = true;
        DialogResult = DialogResult.Abort;
        Close();
    }

    private void UpdatePreview()
    {
        SelectedAccentColor = _workingSnapshot.GetAccentColor();
        previewPanel.FillColor = SelectedAccentColor;
        previewLabel.Text = $"hsla({_workingSnapshot.ThemeHue}, {_workingSnapshot.ThemeSaturation}%, {_workingSnapshot.ThemeLightness}%, {_workingSnapshot.ThemeAlpha / 100f:0.00})";
    }
}
