using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

#nullable disable

namespace winDrawer;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }
        base.Dispose(disposing);
        if (disposing)
        {
            foreach (var font in _ownedFonts)
            {
                font.Dispose();
            }
        }
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        mainTableLayoutPanel = new TableLayoutPanel();
        headerTableLayoutPanel = new TableLayoutPanel();
        headerSettings = new Label();
        settingsTableLayoutPanel = new TableLayoutPanel();
        windowHeightTableLayoutPanel = new TableLayoutPanel();
        windowHeightLabel = new Label();
        windowHeightTrackBar = new TrackBar();
        windowHeightValueLabel = new Label();
        behaviorCheckBoxGroup = new BehaviorCheckBoxGroup();
        visualGroupBox = new GroupBox();
        generalGroupBox = new GroupBox();
        generalFlowPanel = new FlowLayoutPanel();
        visualTableLayoutPanel = new TableLayoutPanel();
        opacityValueLabel = new Label();
        opacityTrackBar = new TrackBar();
        opacityLabel = new Label();
        previewPanel = new ColorPreviewPanel();
        footerGroupBox = new GroupBox();
        shutDownButton = new Button();
        defaultSettingsButton = new Button();
        openSettingsTomlButton = new Button();
        cancelButton = new Button();
        saveButton = new Button();
        mainTableLayoutPanel.SuspendLayout();
        headerTableLayoutPanel.SuspendLayout();
        settingsTableLayoutPanel.SuspendLayout();
        visualGroupBox.SuspendLayout();
        visualTableLayoutPanel.SuspendLayout();
        footerGroupBox.SuspendLayout();
        SuspendLayout();
        // 
        // mainTableLayoutPanel
        // 
        mainTableLayoutPanel.BackColor = Color.FromArgb(64, 64, 64);
        mainTableLayoutPanel.ColumnCount = 1;
        mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainTableLayoutPanel.Controls.Add(headerTableLayoutPanel, 0, 0);
        mainTableLayoutPanel.Controls.Add(settingsTableLayoutPanel, 0, 1);
        mainTableLayoutPanel.Controls.Add(footerGroupBox, 0, 2);
        mainTableLayoutPanel.Dock = DockStyle.Fill;
        mainTableLayoutPanel.Location = new Point(0, 0);
        mainTableLayoutPanel.Name = "mainTableLayoutPanel";
        mainTableLayoutPanel.RowCount = 3;
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        mainTableLayoutPanel.Size = new Size(783, 649);
        mainTableLayoutPanel.TabIndex = 1;
        // 
        // headerTableLayoutPanel
        // 
        headerTableLayoutPanel.ColumnCount = 2;
        headerTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
        headerTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        headerTableLayoutPanel.Controls.Add(headerSettings, 0, 0);
        headerTableLayoutPanel.Dock = DockStyle.Fill;
        headerTableLayoutPanel.Location = new Point(3, 3);
        headerTableLayoutPanel.Name = "headerTableLayoutPanel";
        headerTableLayoutPanel.BackColor = Color.FromArgb(64, 64, 64);
        headerTableLayoutPanel.RowCount = 1;
        headerTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        headerTableLayoutPanel.Size = new Size(777, 44);
        headerTableLayoutPanel.TabIndex = 0;
        // 
        // headerSettings
        // 
        headerSettings.Cursor = Cursors.SizeAll;
        headerSettings.Dock = DockStyle.Left;
        headerSettings.ForeColor = Color.White;
        headerSettings.Location = new Point(3, 0);
        headerSettings.Name = "headerSettings";
        headerSettings.AutoSize = false;
        headerSettings.BackColor = Color.Transparent;
        headerSettings.Size = new Size(303, 44);
        headerSettings.TabIndex = 0;
        headerSettings.Text = "winDrawer settings";
        headerSettings.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // settingsTableLayoutPanel
        // 
        settingsTableLayoutPanel.ColumnCount = 1;
        settingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        settingsTableLayoutPanel.Controls.Add(windowHeightTableLayoutPanel, 0, 0);
        settingsTableLayoutPanel.Controls.Add(behaviorCheckBoxGroup, 0, 1);
        settingsTableLayoutPanel.Controls.Add(visualGroupBox, 0, 2);
        settingsTableLayoutPanel.Controls.Add(generalGroupBox, 0, 3);
        settingsTableLayoutPanel.Dock = DockStyle.Fill;
        settingsTableLayoutPanel.Location = new Point(3, 53);
        settingsTableLayoutPanel.Name = "settingsTableLayoutPanel";
        settingsTableLayoutPanel.BackColor = Color.FromArgb(64, 64, 64);
        settingsTableLayoutPanel.RowCount = 4;
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 176F));
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        settingsTableLayoutPanel.Size = new Size(777, 442);
        settingsTableLayoutPanel.TabIndex = 2;
        //
        // windowHeightTableLayoutPanel
        //
        windowHeightTableLayoutPanel.BackColor = Color.FromArgb(64, 64, 64);
        windowHeightTableLayoutPanel.ColumnCount = 3;
        windowHeightTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        windowHeightTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
        windowHeightTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
        windowHeightTableLayoutPanel.Controls.Add(windowHeightLabel, 0, 0);
        windowHeightTableLayoutPanel.Controls.Add(windowHeightTrackBar, 1, 0);
        windowHeightTableLayoutPanel.Controls.Add(windowHeightValueLabel, 2, 0);
        windowHeightTableLayoutPanel.Dock = DockStyle.Fill;
        windowHeightTableLayoutPanel.Location = new Point(3, 3);
        windowHeightTableLayoutPanel.Name = "windowHeightTableLayoutPanel";
        windowHeightTableLayoutPanel.RowCount = 1;
        windowHeightTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        windowHeightTableLayoutPanel.Size = new Size(777, 60);
        windowHeightTableLayoutPanel.TabIndex = 3;
        windowHeightLabel.Dock = DockStyle.Fill;
        windowHeightLabel.ForeColor = Color.FromArgb(224, 224, 224);
        windowHeightLabel.Name = "windowHeightLabel";
        windowHeightLabel.AutoSize = false;
        windowHeightLabel.BackColor = Color.Transparent;
        windowHeightLabel.TabIndex = 0;
        windowHeightLabel.Text = "Window height (100–1000 DIP)";
        windowHeightLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        windowHeightTrackBar.Dock = DockStyle.Fill;
        windowHeightTrackBar.MinimumSize = new Size(1, 1);
        windowHeightTrackBar.Name = "windowHeightTrackBar";
        windowHeightTrackBar.AutoSize = false;
        windowHeightTrackBar.BackColor = Color.FromArgb(64, 64, 64);
        windowHeightTrackBar.TickStyle = TickStyle.None;
        windowHeightTrackBar.Size = new Size(510, 60);
        windowHeightTrackBar.TabIndex = 1;
        //
        // windowHeightValueLabel
        //
        windowHeightValueLabel.Dock = DockStyle.Fill;
        windowHeightValueLabel.ForeColor = Color.FromArgb(224, 224, 224);
        windowHeightValueLabel.Location = new Point(701, 0);
        windowHeightValueLabel.Name = "windowHeightValueLabel";
        windowHeightValueLabel.AutoSize = false;
        windowHeightValueLabel.BackColor = Color.Transparent;
        windowHeightValueLabel.Size = new Size(73, 60);
        windowHeightValueLabel.TabIndex = 2;
        windowHeightValueLabel.Text = "400";
        windowHeightValueLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // behaviorCheckBoxGroup
        // 
        behaviorCheckBoxGroup.Dock = DockStyle.Fill;
        behaviorCheckBoxGroup.BackColor = Color.FromArgb(64, 64, 64);
        behaviorCheckBoxGroup.ForeColor = Color.FromArgb(224, 224, 224);
        behaviorCheckBoxGroup.Margin = new Padding(10);
        behaviorCheckBoxGroup.MinimumSize = new Size(1, 1);
        behaviorCheckBoxGroup.Name = "behaviorCheckBoxGroup";
        behaviorCheckBoxGroup.Size = new Size(757, 106);
        behaviorCheckBoxGroup.TabIndex = 0;
        behaviorCheckBoxGroup.Text = "Behavior";
        // 
        // visualGroupBox
        // 
        visualGroupBox.Controls.Add(visualTableLayoutPanel);
        visualGroupBox.Dock = DockStyle.Fill;
        visualGroupBox.BackColor = Color.FromArgb(64, 64, 64);
        visualGroupBox.ForeColor = Color.FromArgb(224, 224, 224);
        visualGroupBox.Location = new Point(10, 136);
        visualGroupBox.Margin = new Padding(10);
        visualGroupBox.MinimumSize = new Size(1, 1);
        visualGroupBox.Name = "visualGroupBox";
        visualGroupBox.Padding = new Padding(24, 48, 24, 24);
        visualGroupBox.Size = new Size(757, 296);
        visualGroupBox.TabIndex = 1;
        visualGroupBox.Text = "Appearance";
        //
        // generalGroupBox
        //
        generalGroupBox.Controls.Add(generalFlowPanel);
        generalGroupBox.Dock = DockStyle.Fill;
        generalGroupBox.BackColor = Color.FromArgb(64, 64, 64);
        generalGroupBox.ForeColor = Color.FromArgb(224, 224, 224);
        generalGroupBox.Location = new Point(10, 350);
        generalGroupBox.Margin = new Padding(10);
        generalGroupBox.MinimumSize = new Size(1, 1);
        generalGroupBox.Name = "generalGroupBox";
        generalGroupBox.Padding = new Padding(24, 36, 24, 12);
        generalGroupBox.Size = new Size(757, 112);
        generalGroupBox.TabIndex = 2;
        generalGroupBox.Text = "General";
        //
        // generalFlowPanel
        //
        generalFlowPanel.BackColor = Color.FromArgb(64, 64, 64);
        generalFlowPanel.Controls.Add(shutDownButton);
        generalFlowPanel.Controls.Add(defaultSettingsButton);
        generalFlowPanel.Controls.Add(openSettingsTomlButton);
        generalFlowPanel.Dock = DockStyle.Fill;
        generalFlowPanel.FlowDirection = FlowDirection.LeftToRight;
        generalFlowPanel.Location = new Point(24, 40);
        generalFlowPanel.Name = "generalFlowPanel";
        generalFlowPanel.Padding = new Padding(0, 4, 0, 0);
        generalFlowPanel.Size = new Size(709, 60);
        generalFlowPanel.TabIndex = 0;
        generalFlowPanel.WrapContents = false;
        // 
        // visualTableLayoutPanel
        //
        visualTableLayoutPanel.ColumnCount = 3;
        visualTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        visualTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
        visualTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
        visualTableLayoutPanel.Controls.Add(previewPanel, 0, 0);
        visualTableLayoutPanel.SetColumnSpan(previewPanel, 3);
        visualTableLayoutPanel.Controls.Add(opacityLabel, 0, 1);
        visualTableLayoutPanel.Controls.Add(opacityTrackBar, 1, 1);
        visualTableLayoutPanel.Controls.Add(opacityValueLabel, 2, 1);
        visualTableLayoutPanel.Dock = DockStyle.Fill;
        visualTableLayoutPanel.Location = new Point(32, 64);
        visualTableLayoutPanel.Name = "visualTableLayoutPanel";
        visualTableLayoutPanel.BackColor = Color.FromArgb(64, 64, 64);
        visualTableLayoutPanel.RowCount = 2;
        visualTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        visualTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        visualTableLayoutPanel.Size = new Size(693, 200);
        visualTableLayoutPanel.TabIndex = 0;
        //
        // opacityValueLabel
        //
        opacityValueLabel.Dock = DockStyle.Fill;
        opacityValueLabel.ForeColor = Color.FromArgb(224, 224, 224);
        opacityValueLabel.Name = "opacityValueLabel";
        opacityValueLabel.AutoSize = false;
        opacityValueLabel.BackColor = Color.Transparent;
        opacityValueLabel.TabIndex = 11;
        opacityValueLabel.Text = "95%";
        opacityValueLabel.TextAlign = ContentAlignment.MiddleCenter;
        //
        // opacityTrackBar
        //
        opacityTrackBar.Dock = DockStyle.Fill;
        opacityTrackBar.MinimumSize = new Size(1, 1);
        opacityTrackBar.Name = "opacityTrackBar";
        opacityTrackBar.AutoSize = false;
        opacityTrackBar.BackColor = Color.FromArgb(64, 64, 64);
        opacityTrackBar.TickStyle = TickStyle.None;
        opacityTrackBar.Size = new Size(510, 60);
        opacityTrackBar.TabIndex = 6;
        //
        // opacityLabel
        //
        opacityLabel.Dock = DockStyle.Fill;
        opacityLabel.ForeColor = Color.FromArgb(224, 224, 224);
        opacityLabel.Name = "opacityLabel";
        opacityLabel.AutoSize = false;
        opacityLabel.BackColor = Color.Transparent;
        opacityLabel.TabIndex = 3;
        opacityLabel.Text = "Window opacity";
        opacityLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // previewPanel
        //
        previewPanel.Dock = DockStyle.Fill;
        previewPanel.ForeColor = Color.Gray;
        previewPanel.Margin = new Padding(4, 5, 4, 10);
        previewPanel.MinimumSize = new Size(1, 1);
        previewPanel.Name = "previewPanel";
        previewPanel.Padding = new Padding(10);
        previewPanel.TabIndex = 13;
        previewPanel.Text = "#000533";
        previewPanel.TextAlignment = ContentAlignment.MiddleCenter;
        // 
        // footerGroupBox
        // 
        footerGroupBox.Controls.Add(cancelButton);
        footerGroupBox.Controls.Add(saveButton);
        footerGroupBox.Dock = DockStyle.Fill;
        footerGroupBox.BackColor = Color.FromArgb(64, 64, 64);
        footerGroupBox.Location = new Point(4, 503);
        footerGroupBox.Margin = new Padding(0);
        footerGroupBox.MinimumSize = new Size(1, 1);
        footerGroupBox.Name = "footerGroupBox";
        footerGroupBox.Padding = new Padding(0);
        footerGroupBox.TabIndex = 3;
        footerGroupBox.Text = null;
        // 
        // shutDownButton
        // 
        shutDownButton.Location = new Point(3, 4);
        shutDownButton.Margin = new Padding(3, 4, 12, 4);
        shutDownButton.MinimumSize = new Size(1, 1);
        shutDownButton.Name = "shutDownButton";
        shutDownButton.Size = new Size(215, 40);
        shutDownButton.TabIndex = 4;
        shutDownButton.Text = "Close app";
        //
        // defaultSettingsButton
        //
        defaultSettingsButton.Location = new Point(233, 4);
        defaultSettingsButton.Margin = new Padding(3, 4, 12, 4);
        defaultSettingsButton.MinimumSize = new Size(1, 1);
        defaultSettingsButton.Name = "defaultSettingsButton";
        defaultSettingsButton.Size = new Size(120, 40);
        defaultSettingsButton.TabIndex = 3;
        defaultSettingsButton.Text = "Default";
        //
        // openSettingsTomlButton
        //
        openSettingsTomlButton.Location = new Point(368, 4);
        openSettingsTomlButton.Margin = new Padding(3, 4, 3, 4);
        openSettingsTomlButton.MinimumSize = new Size(1, 1);
        openSettingsTomlButton.Name = "openSettingsTomlButton";
        openSettingsTomlButton.Size = new Size(240, 40);
        openSettingsTomlButton.TabIndex = 5;
        openSettingsTomlButton.Text = "Open settings TOML";
        // 
        // cancelButton
        // 
        cancelButton.Location = new Point(509, 8);
        cancelButton.MinimumSize = new Size(1, 1);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(120, 40);
        cancelButton.TabIndex = 2;
        cancelButton.Text = "Cancel";
        // 
        // saveButton
        // 
        saveButton.Location = new Point(643, 8);
        saveButton.MinimumSize = new Size(1, 1);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(120, 40);
        saveButton.TabIndex = 1;
        saveButton.Text = "Save";
        // 
        // SettingsForm
        // 
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.DimGray;
        ClientSize = new Size(783, 826);
        Controls.Add(mainTableLayoutPanel);
        Name = "SettingsForm";
        Text = "";
        mainTableLayoutPanel.ResumeLayout(false);
        headerTableLayoutPanel.ResumeLayout(false);
        settingsTableLayoutPanel.ResumeLayout(false);
        visualGroupBox.ResumeLayout(false);
        visualTableLayoutPanel.ResumeLayout(false);
        footerGroupBox.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel mainTableLayoutPanel;
    private TableLayoutPanel headerTableLayoutPanel;
    private Label headerSettings;
    private TableLayoutPanel settingsTableLayoutPanel;
    private TableLayoutPanel windowHeightTableLayoutPanel;
    private Label windowHeightLabel;
    private TrackBar windowHeightTrackBar;
    private Label windowHeightValueLabel;
    private BehaviorCheckBoxGroup behaviorCheckBoxGroup;
    private GroupBox visualGroupBox;
    private GroupBox generalGroupBox;
    private System.Windows.Forms.FlowLayoutPanel generalFlowPanel;
    private TableLayoutPanel visualTableLayoutPanel;
    private Label opacityValueLabel;
    private TrackBar opacityTrackBar;
    private Label opacityLabel;
    private ColorPreviewPanel previewPanel;
    private GroupBox footerGroupBox;
    private Button cancelButton;
    private Button saveButton;
    private Button defaultSettingsButton;
    private Button shutDownButton;
    private Button openSettingsTomlButton;
}
