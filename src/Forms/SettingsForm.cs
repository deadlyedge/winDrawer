using System.Diagnostics;
using System.Runtime.InteropServices;
using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer;

public partial class SettingsForm : Form
{
    private const int StartWithWindowsIndex = 0;
    private const int LockPositionIndex = 1;
    private const int AlwaysOnTopIndex = 2;
    private const int ToggleHotkeyIndex = 3;

    private SettingsSnapshot _originalSnapshot;
    private SettingsSnapshot _workingSnapshot;
    private readonly DialogFontSet _fonts = new();
    private bool _allowClose;

    public SettingsForm(SettingsSnapshot snapshot)
    {
        _originalSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _workingSnapshot = snapshot.Clone();

        InitializeComponent();
        DoubleBuffered = true;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        ControlBox = false;
        MaximizeBox = false;
        MinimizeBox = false;

        _fonts.ApplyTo(this);
        ConfigureControls();
        ConfigureButtonPalette();
        ApplySnapshotToUi();
        WireEvents();
        WireDragHandlers();
        ApplyBrandFonts();

        ResultSettings = _originalSnapshot.Clone();
        UpdatePreview();
    }

    public event EventHandler<ThemePreviewEventArgs>? ThemePreviewChanged;

    public event EventHandler? WindowHeightPreviewChanged;

    public event Action<bool>? WindowLockPreviewChanged;

    public event Action<bool>? AlwaysOnTopPreviewChanged;

    /// <summary>
    /// Raised when the "Enable Win+Shift+D" option changes so the host can register or release the
    /// global shortcut immediately.
    /// </summary>
    public event Action<bool>? ToggleHotkeyPreviewChanged;

    public event EventHandler? StartupDefaultsResetRequested;

    public int WindowHeightPreview => _workingSnapshot.WindowHeight;

    public void RestoreWindowHeightPreview()
    {
        _workingSnapshot.WindowHeight = _originalSnapshot.WindowHeight;
        windowHeightTrackBar.Value = _originalSnapshot.WindowHeight;
        UpdateWindowHeightValueLabel();
        WindowHeightPreviewChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool ShutdownRequested { get; private set; }

    public SettingsSnapshot ResultSettings { get; private set; }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        AlignTrackBarsWithTheirRows();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        ApplyBrandFonts();
    }

    /// <summary>
    /// The native track bar paints its thumb and channel flush with the top of the control, so in a
    /// tall row the slider sits visibly higher than the label and the value beside it. Shift the
    /// control down until the thumb centre matches the row centre.
    /// </summary>
    private void AlignTrackBarsWithTheirRows()
    {
        AlignTrackBarWithRow(opacityTrackBar);
        AlignTrackBarWithRow(windowHeightTrackBar);
    }

    private static void AlignTrackBarWithRow(TrackBar trackBar)
    {
        if (trackBar.Parent is not TableLayoutPanel table)
        {
            return;
        }

        var row = table.GetRow(trackBar);
        var rowHeights = table.GetRowHeights();
        var thumb = GetTrackBarThumbRect(trackBar);
        if (row < 0 || row >= rowHeights.Length || thumb.Bottom <= thumb.Top)
        {
            return;
        }

        // Read the thumb rectangle instead of assuming a height so the offset also follows DPI.
        var top = Math.Max(0, (rowHeights[row] - (thumb.Top + thumb.Bottom)) / 2);
        if (trackBar.Margin.Top != top)
        {
            var margin = trackBar.Margin;
            trackBar.Margin = new Padding(margin.Left, top, margin.Right, margin.Bottom);
        }
    }

    public void ApplyResetBaseline(SettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _originalSnapshot = snapshot.Clone();
        _workingSnapshot = snapshot.Clone();
        ResultSettings = snapshot.Clone();
        ApplySnapshotToUi();
        UpdatePreview();
        WindowHeightPreviewChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyBrandFonts()
    {
        headerSettings.Font = _fonts.Title;
        cancelButton.Font = _fonts.PrimaryButton;
        saveButton.Font = _fonts.PrimaryButton;
    }

    private void ConfigureButtonPalette()
    {
        UiButtonTheming.ApplyPrimaryPalette(saveButton);
        UiButtonTheming.ApplyDangerPalette(cancelButton);
        UiButtonTheming.ApplyDangerPalette(shutDownButton);
        UiButtonTheming.ApplySecondaryPalette(defaultSettingsButton);
        UiButtonTheming.ApplySecondaryPalette(openSettingsTomlButton);
    }

    private void ConfigureControls()
    {
        opacityTrackBar.Minimum = AppConstants.Theme.OpacityMin;
        opacityTrackBar.Maximum = AppConstants.Theme.OpacityMax;
        windowHeightTrackBar.Minimum = AppConstants.Defaults.MainWindowHeightMin;
        windowHeightTrackBar.Maximum = AppConstants.Defaults.MainWindowHeightMax;

        behaviorCheckBoxGroup.AccessibleName = "Behavior options";
        behaviorCheckBoxGroup.AccessibleDescription = "Toggles for startup, window position lock, always-on-top and the Win+Shift+D shortcut.";

        previewPanel.FillColor = _workingSnapshot.WindowColor;
        previewPanel.RectColor = ThemePalette.GetReadableTextColor(_workingSnapshot.WindowColor);

        previewPanel.Cursor = Cursors.Hand;
        previewPanel.TabStop = true;
        previewPanel.AccessibleRole = AccessibleRole.PushButton;
        previewPanel.AccessibleName = "Theme color preview. Activate to choose a color.";
        previewPanel.AccessibleDescription = "Click to change the color. The RGB color code is shown in the preview.";
        CancelButton = cancelButton;
    }

    private void WireEvents()
    {
        previewPanel.Click += OnThemeColorPreviewClick;
        previewPanel.KeyDown += OnThemeColorPreviewKeyDown;
        opacityTrackBar.ValueChanged += OnOpacityValueChanged;
        windowHeightTrackBar.ValueChanged += OnWindowHeightValueChanged;
        behaviorCheckBoxGroup.ValueChanged += OnBehaviorValueChanged;

        saveButton.Click += SaveButton_Click;
        cancelButton.Click += CancelButton_Click;
        shutDownButton.Click += ShutDownButton_Click;
        defaultSettingsButton.Click += DefaultSettingsButton_Click;
        openSettingsTomlButton.Click += OpenSettingsTomlButton_Click;
    }

    private void WireDragHandlers()
    {
        FormDragBehavior.Attach(this, headerTableLayoutPanel, headerSettings);
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

        if (_workingSnapshot.AlwaysOnTop)
        {
            selected.Add(AlwaysOnTopIndex);
        }

        if (_workingSnapshot.ToggleHotkeyEnabled)
        {
            selected.Add(ToggleHotkeyIndex);
        }

        behaviorCheckBoxGroup.SelectedIndexes = selected;

        windowHeightTrackBar.Value = Math.Clamp(
            _workingSnapshot.WindowHeight,
            windowHeightTrackBar.Minimum,
            windowHeightTrackBar.Maximum);
        UpdateWindowHeightValueLabel();

        opacityTrackBar.Value = Math.Clamp(_workingSnapshot.WindowOpacity, opacityTrackBar.Minimum, opacityTrackBar.Maximum);

        UpdateValueLabels();
    }

    private void UpdateValueLabels()
    {
        opacityValueLabel.Text = $"{opacityTrackBar.Value}%";
    }

    private void OnWindowHeightValueChanged(object? sender, EventArgs e)
    {
        _workingSnapshot.WindowHeight = windowHeightTrackBar.Value;
        UpdateWindowHeightValueLabel();
        WindowHeightPreviewChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateWindowHeightValueLabel()
    {
        windowHeightValueLabel.Text = $"{windowHeightTrackBar.Value} DIP";
    }

    private void OnOpacityValueChanged(object? sender, EventArgs e)
    {
        _workingSnapshot.WindowOpacity = opacityTrackBar.Value;

        UpdateValueLabels();
        UpdatePreview();
    }

    private void OnThemeColorPreviewClick(object? sender, EventArgs e)
    {
        if (UiDialogHelpers.ShowColorDialog(this, _workingSnapshot.WindowColor) is not { } color)
        {
            return;
        }

        _workingSnapshot.WindowColor = Color.FromArgb(color.R, color.G, color.B);
        UpdatePreview();
    }

    private void OnThemeColorPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        UiDialogHelpers.HandleActivationKey(e, () => OnThemeColorPreviewClick(sender, EventArgs.Empty));
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        DrawerLogger.LogInformation("SettingsForm: save requested.");
        if (!TrySynchronizeStartupRegistration())
        {
            return;
        }

        SyncWorkingSnapshotFromUi();
        ResultSettings = _workingSnapshot.Clone();
        DialogResult = DialogResult.OK;
        _allowClose = true;
        Close();
    }

    private void CancelButton_Click(object? sender, EventArgs e)
    {
        DrawerLogger.LogInformation("SettingsForm: cancel requested.");
        RestoreWindowHeightPreview();
        ResultSettings = _originalSnapshot.Clone();
        ThemePreviewChanged?.Invoke(this, new ThemePreviewEventArgs(_originalSnapshot.BuildThemePalette()));
        WindowLockPreviewChanged?.Invoke(_originalSnapshot.WindowLocked);
        AlwaysOnTopPreviewChanged?.Invoke(_originalSnapshot.AlwaysOnTop);
        ToggleHotkeyPreviewChanged?.Invoke(_originalSnapshot.ToggleHotkeyEnabled);
        DialogResult = DialogResult.Cancel;
        _allowClose = true;
        Close();
    }

    private void ShutDownButton_Click(object? sender, EventArgs e)
    {
        DrawerLogger.LogWarning("SettingsForm: shutdown requested.");
        ShutdownRequested = true;
        DialogResult = DialogResult.Abort;
        _allowClose = true;
        Close();
    }

    private void DefaultSettingsButton_Click(object? sender, EventArgs e)
    {
        DrawerLogger.LogInformation("SettingsForm: startup defaults reset requested.");
        StartupDefaultsResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OpenSettingsTomlButton_Click(object? sender, EventArgs e)
    {
        var settingsPath = SettingsRepository.GetDefaultPath();
        try
        {
            if (!File.Exists(settingsPath))
            {
                throw new FileNotFoundException("The settings file does not exist.", settingsPath);
            }

            var notepadPath = Path.Combine(Environment.SystemDirectory, "notepad.exe");
            var startInfo = new ProcessStartInfo(notepadPath)
            {
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(settingsPath);
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError($"Unable to open settings TOML file '{settingsPath}'.", ex);
            UiMessages.ShowError(
                this,
                "Open Settings TOML",
                $"Unable to open the settings file:{Environment.NewLine}{settingsPath}{Environment.NewLine}{Environment.NewLine}{ex.Message}");
        }
    }

    private bool TrySynchronizeStartupRegistration()
    {
        var requestedState = IsBehaviorChecked(StartWithWindowsIndex);
        bool previousState;
        try
        {
            previousState = StartupRegistration.IsEnabled();
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Unable to read the current user's Windows startup registration.", ex);
            UiMessages.ShowError(
                this,
                "Windows Startup",
                $"Unable to verify Windows login startup. No settings were saved.{Environment.NewLine}{Environment.NewLine}{ex.Message}");
            return false;
        }

        try
        {
            StartupRegistration.SetEnabled(requestedState);
            _workingSnapshot.StartWithWindows = requestedState;
            return true;
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Unable to update the current user's Windows startup registration.", ex);
            var rollbackSucceeded = true;
            try
            {
                if (StartupRegistration.IsEnabled() != previousState)
                {
                    StartupRegistration.SetEnabled(previousState);
                }
            }
            catch (Exception rollbackException)
            {
                rollbackSucceeded = false;
                DrawerLogger.LogError("Unable to restore the previous Windows login startup state.", rollbackException);
            }

            _workingSnapshot.StartWithWindows = previousState;
            SetStartWithWindowsCheckBox(previousState);
            UiMessages.ShowWarning(
                this,
                "Windows Startup",
                $"Unable to {(requestedState ? "enable" : "disable")} Windows login startup. The previous registration state was restored to {(previousState ? "enabled" : "disabled")}.{(rollbackSucceeded ? string.Empty : " Windows may still have a different registration state.")}{Environment.NewLine}{Environment.NewLine}{ex.Message}");
            return false;
        }
    }
    private void SetStartWithWindowsCheckBox(bool enabled)
    {
        var selected = behaviorCheckBoxGroup.SelectedIndexes ?? [];
        selected.Remove(StartWithWindowsIndex);
        if (enabled)
        {
            selected.Add(StartWithWindowsIndex);
        }

        behaviorCheckBoxGroup.SelectedIndexes = selected;
    }

    private void UpdatePreview()
    {
        var palette = _workingSnapshot.BuildThemePalette();
        previewPanel.FillColor = _workingSnapshot.WindowColor;
        previewPanel.RectColor = palette.Border;
        previewPanel.ForeColor = ThemePalette.GetReadableTextColor(_workingSnapshot.WindowColor);
        previewPanel.Text = $"Click to change color{Environment.NewLine}{ThemePalette.FormatHexColor(_workingSnapshot.WindowColor)}";
        ThemePreviewChanged?.Invoke(this, new ThemePreviewEventArgs(palette));
    }

    private void SyncWorkingSnapshotFromUi()
    {
        var selected = behaviorCheckBoxGroup.SelectedIndexes ?? [];

        _workingSnapshot.StartWithWindows = selected.Contains(StartWithWindowsIndex);
        _workingSnapshot.WindowLocked = selected.Contains(LockPositionIndex);
        _workingSnapshot.AlwaysOnTop = selected.Contains(AlwaysOnTopIndex);
        _workingSnapshot.ToggleHotkeyEnabled = selected.Contains(ToggleHotkeyIndex);
        _workingSnapshot.WindowHeight = windowHeightTrackBar.Value;
    }

    private void OnBehaviorValueChanged(object? sender, EventArgs e)
    {
        var windowLocked = IsBehaviorChecked(LockPositionIndex);
        if (_workingSnapshot.WindowLocked != windowLocked)
        {
            _workingSnapshot.WindowLocked = windowLocked;
            WindowLockPreviewChanged?.Invoke(windowLocked);
        }

        var alwaysOnTop = IsBehaviorChecked(AlwaysOnTopIndex);
        if (_workingSnapshot.AlwaysOnTop != alwaysOnTop)
        {
            _workingSnapshot.AlwaysOnTop = alwaysOnTop;
            AlwaysOnTopPreviewChanged?.Invoke(alwaysOnTop);
        }

        var toggleHotkey = IsBehaviorChecked(ToggleHotkeyIndex);
        if (_workingSnapshot.ToggleHotkeyEnabled != toggleHotkey)
        {
            _workingSnapshot.ToggleHotkeyEnabled = toggleHotkey;
            ToggleHotkeyPreviewChanged?.Invoke(toggleHotkey);
        }
    }

    private bool IsBehaviorChecked(int index)
    {
        return behaviorCheckBoxGroup.SelectedIndexes != null && behaviorCheckBoxGroup.SelectedIndexes.Contains(index);
    }

    private static NativeRect GetTrackBarThumbRect(TrackBar trackBar)
    {
        var rect = default(NativeRect);
        SendMessage(trackBar.Handle, TrackBarThumbRectMessage, IntPtr.Zero, ref rect);
        return rect;
    }

    /// <summary><c>TBM_GETTHUMBRECT</c>: writes the thumb rectangle, in client coordinates, into lParam.</summary>
    private const int TrackBarThumbRectMessage = 0x0419;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wParam, ref NativeRect lParam);
}
