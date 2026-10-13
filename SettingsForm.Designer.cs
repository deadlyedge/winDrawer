using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;

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
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        ComponentResourceManager resources = new ComponentResourceManager(typeof(SettingsForm));
        mainTableLayoutPanel = new UITableLayoutPanel();
        headerTableLayoutPanel = new UITableLayoutPanel();
        headerSettings = new UILabel();
        settingsTableLayoutPanel = new UITableLayoutPanel();
        behaviorCheckBoxGroup = new UICheckBoxGroup();
        visualGroupBox = new UIGroupBox();
        generalGroupBox = new UIGroupBox();
        generalFlowPanel = new FlowLayoutPanel();
        visualTableLayoutPanel = new UITableLayoutPanel();
        lightnessValueLabel = new UILabel();
        opacityValueLabel = new UILabel();
        saturationValueLabel = new UILabel();
        hueValueLabel = new UILabel();
        previewLabel = new UILabel();
        saturationTrackBar = new UITrackBar();
        opacityTrackBar = new UITrackBar();
        lightnessTrackBar = new UITrackBar();
        opacityLabel = new UILabel();
        lightnessLabel = new UILabel();
        saturationLabel = new UILabel();
        hueLabel = new UILabel();
        hueTrackBar = new UITrackBar();
        previewPanel = new UIPanel();
        footerGroupBox = new UIGroupBox();
        shutDownButton = new UIButton();
        defautlSettingsButton = new UIButton();
        openSettingsTomlButton = new UIButton();
        cancelButton = new UIButton();
        saveButton = new UIButton();
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
        mainTableLayoutPanel.Size = new Size(783, 583);
        mainTableLayoutPanel.TabIndex = 1;
        mainTableLayoutPanel.TagString = null;
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
        headerTableLayoutPanel.RowCount = 1;
        headerTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        headerTableLayoutPanel.Size = new Size(777, 44);
        headerTableLayoutPanel.TabIndex = 0;
        headerTableLayoutPanel.TagString = null;
        // 
        // headerSettings
        // 
        headerSettings.Cursor = Cursors.SizeAll;
        headerSettings.Dock = DockStyle.Left;
        headerSettings.Font = new Font("Lobster", 13.875F, FontStyle.Regular, GraphicsUnit.Point, 0);
        headerSettings.ForeColor = Color.White;
        headerSettings.Location = new Point(3, 0);
        headerSettings.Name = "headerSettings";
        headerSettings.Size = new Size(303, 44);
        headerSettings.TabIndex = 0;
        headerSettings.Text = "winDrawer settings";
        headerSettings.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // settingsTableLayoutPanel
        // 
        settingsTableLayoutPanel.ColumnCount = 1;
        settingsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        settingsTableLayoutPanel.Controls.Add(behaviorCheckBoxGroup, 0, 0);
        settingsTableLayoutPanel.Controls.Add(visualGroupBox, 0, 1);
        settingsTableLayoutPanel.Controls.Add(generalGroupBox, 0, 2);
        settingsTableLayoutPanel.Dock = DockStyle.Fill;
        settingsTableLayoutPanel.Location = new Point(3, 53);
        settingsTableLayoutPanel.Name = "settingsTableLayoutPanel";
        settingsTableLayoutPanel.RowCount = 3;
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        settingsTableLayoutPanel.Size = new Size(777, 442);
        settingsTableLayoutPanel.TabIndex = 2;
        settingsTableLayoutPanel.TagString = null;
        // 
        // behaviorCheckBoxGroup
        // 
        behaviorCheckBoxGroup.CheckBoxSize = 32;
        behaviorCheckBoxGroup.ColumnCount = 2;
        behaviorCheckBoxGroup.ColumnInterval = 200;
        behaviorCheckBoxGroup.Dock = DockStyle.Fill;
        behaviorCheckBoxGroup.FillColor = Color.FromArgb(64, 64, 64);
        behaviorCheckBoxGroup.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        behaviorCheckBoxGroup.ForeColor = Color.FromArgb(224, 224, 224);
        behaviorCheckBoxGroup.HoverColor = Color.Gray;
        behaviorCheckBoxGroup.Items.AddRange(new object[] { "Start with windows", "Lock position" });
        behaviorCheckBoxGroup.Location = new Point(10, 10);
        behaviorCheckBoxGroup.Margin = new Padding(10);
        behaviorCheckBoxGroup.MinimumSize = new Size(1, 1);
        behaviorCheckBoxGroup.Name = "behaviorCheckBoxGroup";
        behaviorCheckBoxGroup.Padding = new Padding(0, 32, 0, 0);
        behaviorCheckBoxGroup.SelectedIndexes = (List<int>)resources.GetObject("behaviorCheckBoxGroup.SelectedIndexes");
        behaviorCheckBoxGroup.Size = new Size(757, 106);
        behaviorCheckBoxGroup.StartPos = new Point(12, 40);
        behaviorCheckBoxGroup.TabIndex = 0;
        behaviorCheckBoxGroup.Text = "behavior";
        behaviorCheckBoxGroup.TextAlignment = ContentAlignment.MiddleLeft;
        // 
        // visualGroupBox
        // 
        visualGroupBox.Controls.Add(visualTableLayoutPanel);
        visualGroupBox.Dock = DockStyle.Fill;
        visualGroupBox.FillColor = Color.FromArgb(64, 64, 64);
        visualGroupBox.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        visualGroupBox.ForeColor = Color.FromArgb(224, 224, 224);
        visualGroupBox.Location = new Point(10, 136);
        visualGroupBox.Margin = new Padding(10);
        visualGroupBox.MinimumSize = new Size(1, 1);
        visualGroupBox.Name = "visualGroupBox";
        visualGroupBox.Padding = new Padding(32, 64, 32, 32);
        visualGroupBox.Size = new Size(757, 296);
        visualGroupBox.TabIndex = 1;
        visualGroupBox.Text = "visual";
        visualGroupBox.TextAlignment = ContentAlignment.MiddleLeft;
        //
        // generalGroupBox
        //
        generalGroupBox.Controls.Add(generalFlowPanel);
        generalGroupBox.Dock = DockStyle.Fill;
        generalGroupBox.FillColor = Color.FromArgb(64, 64, 64);
        generalGroupBox.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        generalGroupBox.ForeColor = Color.FromArgb(224, 224, 224);
        generalGroupBox.Location = new Point(10, 350);
        generalGroupBox.Margin = new Padding(10);
        generalGroupBox.MinimumSize = new Size(1, 1);
        generalGroupBox.Name = "generalGroupBox";
        generalGroupBox.Padding = new Padding(24, 40, 24, 12);
        generalGroupBox.Size = new Size(757, 112);
        generalGroupBox.TabIndex = 2;
        generalGroupBox.Text = "general";
        generalGroupBox.TextAlignment = ContentAlignment.MiddleLeft;
        //
        // generalFlowPanel
        //
        generalFlowPanel.BackColor = Color.FromArgb(64, 64, 64);
        generalFlowPanel.Controls.Add(shutDownButton);
        generalFlowPanel.Controls.Add(defautlSettingsButton);
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
        visualTableLayoutPanel.ColumnCount = 4;
        visualTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        visualTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        visualTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
        visualTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
        visualTableLayoutPanel.Controls.Add(lightnessValueLabel, 2, 2);
        visualTableLayoutPanel.Controls.Add(opacityValueLabel, 2, 3);
        visualTableLayoutPanel.Controls.Add(saturationValueLabel, 2, 1);
        visualTableLayoutPanel.Controls.Add(hueValueLabel, 2, 0);
        visualTableLayoutPanel.Controls.Add(previewLabel, 3, 0);
        visualTableLayoutPanel.Controls.Add(saturationTrackBar, 1, 1);
        visualTableLayoutPanel.Controls.Add(opacityTrackBar, 1, 3);
        visualTableLayoutPanel.Controls.Add(lightnessTrackBar, 1, 2);
        visualTableLayoutPanel.Controls.Add(opacityLabel, 0, 3);
        visualTableLayoutPanel.Controls.Add(lightnessLabel, 0, 2);
        visualTableLayoutPanel.Controls.Add(saturationLabel, 0, 1);
        visualTableLayoutPanel.Controls.Add(hueLabel, 0, 0);
        visualTableLayoutPanel.Controls.Add(hueTrackBar, 1, 0);
        visualTableLayoutPanel.Controls.Add(previewPanel, 3, 1);
        visualTableLayoutPanel.Dock = DockStyle.Fill;
        visualTableLayoutPanel.Location = new Point(32, 64);
        visualTableLayoutPanel.Name = "visualTableLayoutPanel";
        visualTableLayoutPanel.RowCount = 4;
        visualTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        visualTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        visualTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        visualTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        visualTableLayoutPanel.Size = new Size(693, 200);
        visualTableLayoutPanel.TabIndex = 0;
        visualTableLayoutPanel.TagString = null;
        // 
        // lightnessValueLabel
        // 
        lightnessValueLabel.Dock = DockStyle.Left;
        lightnessValueLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lightnessValueLabel.ForeColor = Color.FromArgb(224, 224, 224);
        lightnessValueLabel.Location = new Point(418, 100);
        lightnessValueLabel.Name = "lightnessValueLabel";
        lightnessValueLabel.Size = new Size(63, 50);
        lightnessValueLabel.TabIndex = 12;
        lightnessValueLabel.Text = "100";
        lightnessValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        lightnessValueLabel.Click += uiLabel5_Click;
        // 
        // opacityValueLabel
        // 
        opacityValueLabel.Dock = DockStyle.Left;
        opacityValueLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        opacityValueLabel.ForeColor = Color.FromArgb(224, 224, 224);
        opacityValueLabel.Location = new Point(418, 150);
        opacityValueLabel.Name = "opacityValueLabel";
        opacityValueLabel.Size = new Size(63, 50);
        opacityValueLabel.TabIndex = 11;
        opacityValueLabel.Text = "100";
        opacityValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // saturationValueLabel
        // 
        saturationValueLabel.Dock = DockStyle.Left;
        saturationValueLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        saturationValueLabel.ForeColor = Color.FromArgb(224, 224, 224);
        saturationValueLabel.Location = new Point(418, 50);
        saturationValueLabel.Name = "saturationValueLabel";
        saturationValueLabel.Size = new Size(63, 50);
        saturationValueLabel.TabIndex = 10;
        saturationValueLabel.Text = "90";
        saturationValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // hueValueLabel
        // 
        hueValueLabel.Dock = DockStyle.Left;
        hueValueLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        hueValueLabel.ForeColor = Color.FromArgb(224, 224, 224);
        hueValueLabel.Location = new Point(418, 0);
        hueValueLabel.Name = "hueValueLabel";
        hueValueLabel.Size = new Size(63, 50);
        hueValueLabel.TabIndex = 9;
        hueValueLabel.Text = "80";
        hueValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // previewLabel
        // 
        previewLabel.Dock = DockStyle.Fill;
        previewLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        previewLabel.ForeColor = Color.FromArgb(224, 224, 224);
        previewLabel.Location = new Point(487, 0);
        previewLabel.Name = "previewLabel";
        previewLabel.Size = new Size(203, 50);
        previewLabel.TabIndex = 8;
        previewLabel.Text = "Preview";
        previewLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // saturationTrackBar
        // 
        saturationTrackBar.Dock = DockStyle.Fill;
        saturationTrackBar.FillColor = Color.FromArgb(64, 64, 64);
        saturationTrackBar.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        saturationTrackBar.Location = new Point(141, 53);
        saturationTrackBar.MinimumSize = new Size(1, 1);
        saturationTrackBar.Name = "saturationTrackBar";
        saturationTrackBar.Size = new Size(271, 44);
        saturationTrackBar.TabIndex = 7;
        // 
        // opacityTrackBar
        // 
        opacityTrackBar.Dock = DockStyle.Fill;
        opacityTrackBar.FillColor = Color.FromArgb(64, 64, 64);
        opacityTrackBar.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        opacityTrackBar.Location = new Point(141, 153);
        opacityTrackBar.MinimumSize = new Size(1, 1);
        opacityTrackBar.Name = "opacityTrackBar";
        opacityTrackBar.Size = new Size(271, 44);
        opacityTrackBar.TabIndex = 6;
        // 
        // lightnessTrackBar
        // 
        lightnessTrackBar.Dock = DockStyle.Fill;
        lightnessTrackBar.FillColor = Color.FromArgb(64, 64, 64);
        lightnessTrackBar.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        lightnessTrackBar.Location = new Point(141, 103);
        lightnessTrackBar.MinimumSize = new Size(1, 1);
        lightnessTrackBar.Name = "lightnessTrackBar";
        lightnessTrackBar.Size = new Size(271, 44);
        lightnessTrackBar.TabIndex = 5;
        // 
        // opacityLabel
        // 
        opacityLabel.Dock = DockStyle.Left;
        opacityLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        opacityLabel.ForeColor = Color.FromArgb(224, 224, 224);
        opacityLabel.Location = new Point(3, 150);
        opacityLabel.Name = "opacityLabel";
        opacityLabel.Size = new Size(132, 50);
        opacityLabel.TabIndex = 3;
        opacityLabel.Text = "Opacity";
        opacityLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lightnessLabel
        // 
        lightnessLabel.Dock = DockStyle.Left;
        lightnessLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        lightnessLabel.ForeColor = Color.FromArgb(224, 224, 224);
        lightnessLabel.Location = new Point(3, 100);
        lightnessLabel.Name = "lightnessLabel";
        lightnessLabel.Size = new Size(132, 50);
        lightnessLabel.TabIndex = 2;
        lightnessLabel.Text = "Lightness";
        lightnessLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // saturationLabel
        // 
        saturationLabel.Dock = DockStyle.Left;
        saturationLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        saturationLabel.ForeColor = Color.FromArgb(224, 224, 224);
        saturationLabel.Location = new Point(3, 50);
        saturationLabel.Name = "saturationLabel";
        saturationLabel.Size = new Size(132, 50);
        saturationLabel.TabIndex = 1;
        saturationLabel.Text = "Saturation";
        saturationLabel.TextAlign = ContentAlignment.MiddleLeft;
        // saturationLabel.Click += uiLabel1_Click;
        // 
        // hueLabel
        // 
        hueLabel.Dock = DockStyle.Left;
        hueLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        hueLabel.ForeColor = Color.FromArgb(224, 224, 224);
        hueLabel.Location = new Point(3, 0);
        hueLabel.Name = "hueLabel";
        hueLabel.Size = new Size(132, 50);
        hueLabel.TabIndex = 0;
        hueLabel.Text = "Hue";
        hueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // hueTrackBar
        // 
        hueTrackBar.Dock = DockStyle.Fill;
        hueTrackBar.FillColor = Color.FromArgb(64, 64, 64);
        hueTrackBar.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        hueTrackBar.Location = new Point(141, 3);
        hueTrackBar.MinimumSize = new Size(1, 1);
        hueTrackBar.Name = "hueTrackBar";
        hueTrackBar.Size = new Size(271, 44);
        hueTrackBar.TabIndex = 4;
        // 
        // previewPanel
        // 
        previewPanel.Dock = DockStyle.Fill;
        previewPanel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        previewPanel.ForeColor = Color.Gray;
        previewPanel.Location = new Point(488, 55);
        previewPanel.Margin = new Padding(4, 5, 4, 5);
        previewPanel.MinimumSize = new Size(1, 1);
        previewPanel.Name = "previewPanel";
        previewPanel.Padding = new Padding(10);
        visualTableLayoutPanel.SetRowSpan(previewPanel, 3);
        previewPanel.Size = new Size(201, 140);
        previewPanel.TabIndex = 13;
        previewPanel.Text = "color preview";
        previewPanel.TextAlignment = ContentAlignment.MiddleCenter;
        // 
        // footerGroupBox
        // 
        footerGroupBox.Controls.Add(cancelButton);
        footerGroupBox.Controls.Add(saveButton);
        footerGroupBox.Dock = DockStyle.Fill;
        footerGroupBox.FillColor = Color.FromArgb(64, 64, 64);
        footerGroupBox.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        footerGroupBox.Location = new Point(4, 503);
        footerGroupBox.Margin = new Padding(0);
        footerGroupBox.MinimumSize = new Size(1, 1);
        footerGroupBox.Name = "footerGroupBox";
        footerGroupBox.Padding = new Padding(0);
        footerGroupBox.RectSides = ToolStripStatusLabelBorderSides.None;
        footerGroupBox.Size = new Size(775, 40);
        footerGroupBox.TabIndex = 3;
        footerGroupBox.Text = null;
        footerGroupBox.TextAlignment = ContentAlignment.MiddleLeft;
        // 
        // shutDownButton
        // 
        shutDownButton.FillColor = Color.DarkRed;
        shutDownButton.FillDisableColor = Color.DarkGray;
        shutDownButton.FillHoverColor = Color.LightCoral;
        shutDownButton.FillPressColor = Color.DarkRed;
        shutDownButton.FillSelectedColor = Color.Brown;
        shutDownButton.Font = new Font("Lobster", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        shutDownButton.Location = new Point(3, 4);
        shutDownButton.Margin = new Padding(3, 4, 12, 4);
        shutDownButton.MinimumSize = new Size(1, 1);
        shutDownButton.Name = "shutDownButton";
        shutDownButton.Radius = 20;
        shutDownButton.Size = new Size(215, 40);
        shutDownButton.TabIndex = 4;
        shutDownButton.Text = "shut down app";
        shutDownButton.TipsFont = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
        // 
        // defautlSettingsButton
        // 
        defautlSettingsButton.FillColor = Color.YellowGreen;
        defautlSettingsButton.FillDisableColor = Color.DarkGray;
        defautlSettingsButton.FillHoverColor = Color.LightCoral;
        defautlSettingsButton.FillPressColor = Color.DarkRed;
        defautlSettingsButton.FillSelectedColor = Color.Brown;
        defautlSettingsButton.Font = new Font("Lobster", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        defautlSettingsButton.Location = new Point(233, 4);
        defautlSettingsButton.Margin = new Padding(3, 4, 12, 4);
        defautlSettingsButton.MinimumSize = new Size(1, 1);
        defautlSettingsButton.Name = "defautlSettingsButton";
        defautlSettingsButton.Radius = 20;
        defautlSettingsButton.Size = new Size(120, 40);
        defautlSettingsButton.TabIndex = 3;
        defautlSettingsButton.Text = "default";
        defautlSettingsButton.TipsFont = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
        //
        // openSettingsTomlButton
        //
        openSettingsTomlButton.FillColor = Color.SteelBlue;
        openSettingsTomlButton.FillDisableColor = Color.DarkGray;
        openSettingsTomlButton.FillHoverColor = Color.LightCoral;
        openSettingsTomlButton.FillPressColor = Color.DarkRed;
        openSettingsTomlButton.FillSelectedColor = Color.Brown;
        openSettingsTomlButton.Font = new Font("Lobster", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        openSettingsTomlButton.Location = new Point(368, 4);
        openSettingsTomlButton.Margin = new Padding(3, 4, 3, 4);
        openSettingsTomlButton.MinimumSize = new Size(1, 1);
        openSettingsTomlButton.Name = "openSettingsTomlButton";
        openSettingsTomlButton.Radius = 20;
        openSettingsTomlButton.Size = new Size(240, 40);
        openSettingsTomlButton.TabIndex = 5;
        openSettingsTomlButton.Text = "open settings TOML";
        openSettingsTomlButton.TipsFont = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
        // 
        // cancelButton
        // 
        cancelButton.FillColor = Color.Red;
        cancelButton.FillDisableColor = Color.DarkGray;
        cancelButton.FillHoverColor = Color.LightCoral;
        cancelButton.FillPressColor = Color.DarkRed;
        cancelButton.FillSelectedColor = Color.Brown;
        cancelButton.Font = new Font("Lobster", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        cancelButton.Location = new Point(509, 8);
        cancelButton.MinimumSize = new Size(1, 1);
        cancelButton.Name = "cancelButton";
        cancelButton.Radius = 20;
        cancelButton.Size = new Size(120, 40);
        cancelButton.TabIndex = 2;
        cancelButton.Text = "cancel";
        cancelButton.TipsFont = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
        // 
        // saveButton
        // 
        saveButton.Font = new Font("Lobster", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        saveButton.Location = new Point(643, 8);
        saveButton.MinimumSize = new Size(1, 1);
        saveButton.Name = "saveButton";
        saveButton.Radius = 20;
        saveButton.Size = new Size(120, 40);
        saveButton.TabIndex = 1;
        saveButton.Text = "save";
        saveButton.TipsFont = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
        // 
        // SettingsForm
        // 
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.DimGray;
        ClientSize = new Size(783, 760);
        Controls.Add(mainTableLayoutPanel);
        Name = "SettingsForm";
        RectColor = Color.Transparent;
        ShowTitle = false;
        Text = "";
        ZoomScaleRect = new Rectangle(0, 0, 783, 760);
        mainTableLayoutPanel.ResumeLayout(false);
        headerTableLayoutPanel.ResumeLayout(false);
        settingsTableLayoutPanel.ResumeLayout(false);
        visualGroupBox.ResumeLayout(false);
        visualTableLayoutPanel.ResumeLayout(false);
        footerGroupBox.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Sunny.UI.UITableLayoutPanel mainTableLayoutPanel;
    private Sunny.UI.UITableLayoutPanel headerTableLayoutPanel;
    private Sunny.UI.UILabel headerSettings;
    private Sunny.UI.UITableLayoutPanel settingsTableLayoutPanel;
    private Sunny.UI.UICheckBoxGroup behaviorCheckBoxGroup;
    private Sunny.UI.UIGroupBox visualGroupBox;
    private Sunny.UI.UIGroupBox generalGroupBox;
    private System.Windows.Forms.FlowLayoutPanel generalFlowPanel;
    private Sunny.UI.UITableLayoutPanel visualTableLayoutPanel;
    private Sunny.UI.UILabel lightnessValueLabel;
    private Sunny.UI.UILabel opacityValueLabel;
    private Sunny.UI.UILabel saturationValueLabel;
    private Sunny.UI.UILabel hueValueLabel;
    private Sunny.UI.UILabel previewLabel;
    private Sunny.UI.UITrackBar saturationTrackBar;
    private Sunny.UI.UITrackBar opacityTrackBar;
    private Sunny.UI.UITrackBar lightnessTrackBar;
    private Sunny.UI.UILabel opacityLabel;
    private Sunny.UI.UILabel lightnessLabel;
    private Sunny.UI.UILabel saturationLabel;
    private Sunny.UI.UILabel hueLabel;
    private Sunny.UI.UITrackBar hueTrackBar;
    private Sunny.UI.UIPanel previewPanel;
    private Sunny.UI.UIGroupBox footerGroupBox;
    private Sunny.UI.UIButton cancelButton;
    private Sunny.UI.UIButton saveButton;
    private Sunny.UI.UIButton defautlSettingsButton;
    private Sunny.UI.UIButton shutDownButton;
    private Sunny.UI.UIButton openSettingsTomlButton;
}
