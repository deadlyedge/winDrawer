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
        windowHeightTableLayoutPanel = new UITableLayoutPanel();
        windowHeightLabel = new UILabel();
        windowHeightTrackBar = new UITrackBar();
        windowHeightValueLabel = new UILabel();
        behaviorCheckBoxGroup = new UICheckBoxGroup();
        visualGroupBox = new UIGroupBox();
        generalGroupBox = new UIGroupBox();
        generalFlowPanel = new FlowLayoutPanel();
        visualTableLayoutPanel = new UITableLayoutPanel();
        opacityValueLabel = new UILabel();
        opacityTrackBar = new UITrackBar();
        opacityLabel = new UILabel();
        previewPanel = new UIPanel();
        footerGroupBox = new UIGroupBox();
        shutDownButton = new UIButton();
        defaultSettingsButton = new UIButton();
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
        mainTableLayoutPanel.Size = new Size(783, 649);
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
        settingsTableLayoutPanel.Controls.Add(windowHeightTableLayoutPanel, 0, 0);
        settingsTableLayoutPanel.Controls.Add(behaviorCheckBoxGroup, 0, 1);
        settingsTableLayoutPanel.Controls.Add(visualGroupBox, 0, 2);
        settingsTableLayoutPanel.Controls.Add(generalGroupBox, 0, 3);
        settingsTableLayoutPanel.Dock = DockStyle.Fill;
        settingsTableLayoutPanel.Location = new Point(3, 53);
        settingsTableLayoutPanel.Name = "settingsTableLayoutPanel";
        settingsTableLayoutPanel.RowCount = 4;
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        settingsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
        settingsTableLayoutPanel.Size = new Size(777, 442);
        settingsTableLayoutPanel.TabIndex = 2;
        settingsTableLayoutPanel.TagString = null;
        //
        // windowHeightTableLayoutPanel
        //
        windowHeightTableLayoutPanel.BackColor = Color.FromArgb(64, 64, 64);
        windowHeightTableLayoutPanel.ColumnCount = 3;
        windowHeightTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
        windowHeightTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F));
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
        windowHeightTableLayoutPanel.TagString = null;
        //
        windowHeightTrackBar.TabIndex = 1;
        //
        // windowHeightValueLabel
        //
        windowHeightValueLabel.Dock = DockStyle.Fill;
        windowHeightValueLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        windowHeightValueLabel.ForeColor = Color.FromArgb(224, 224, 224);
        windowHeightValueLabel.Location = new Point(701, 0);
        windowHeightValueLabel.Name = "windowHeightValueLabel";
        windowHeightValueLabel.Size = new Size(73, 60);
        windowHeightValueLabel.TabIndex = 2;
        windowHeightValueLabel.Text = "400";
        windowHeightValueLabel.TextAlign = ContentAlignment.MiddleCenter;
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
        opacityTrackBar.Location = new Point(141, 123);
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
        visualTableLayoutPanel.RowCount = 2;
        visualTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        visualTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        visualTableLayoutPanel.Size = new Size(693, 200);
        visualTableLayoutPanel.TabIndex = 0;
        visualTableLayoutPanel.TagString = null;
        //
        // opacityValueLabel
        //
        opacityValueLabel.Dock = DockStyle.Fill;
        opacityValueLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        opacityValueLabel.ForeColor = Color.FromArgb(224, 224, 224);
        opacityValueLabel.Name = "opacityValueLabel";
        opacityValueLabel.TabIndex = 11;
        opacityValueLabel.Text = "95%";
        opacityValueLabel.TextAlign = ContentAlignment.MiddleCenter;
        //
        // opacityTrackBar
        //
        opacityTrackBar.Dock = DockStyle.Fill;
        opacityTrackBar.FillColor = Color.FromArgb(64, 64, 64);
        opacityTrackBar.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        opacityTrackBar.MinimumSize = new Size(1, 1);
        opacityTrackBar.Name = "opacityTrackBar";
        opacityTrackBar.Size = new Size(510, 60);
        opacityTrackBar.TabIndex = 6;
        //
        // opacityLabel
        //
        opacityLabel.Dock = DockStyle.Fill;
        opacityLabel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
        opacityLabel.ForeColor = Color.FromArgb(224, 224, 224);
        opacityLabel.Name = "opacityLabel";
        opacityLabel.TabIndex = 3;
        opacityLabel.Text = "Opacity";
        opacityLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // previewPanel
        //
        previewPanel.Dock = DockStyle.Fill;
        previewPanel.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
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
        footerGroupBox.FillColor = Color.FromArgb(64, 64, 64);
        footerGroupBox.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        footerGroupBox.Location = new Point(4, 503);
        footerGroupBox.Margin = new Padding(0);
        footerGroupBox.MinimumSize = new Size(1, 1);
        footerGroupBox.Name = "footerGroupBox";
        footerGroupBox.Padding = new Padding(0);
        footerGroupBox.RectSides = ToolStripStatusLabelBorderSides.None;
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
        // defaultSettingsButton
        //
        defaultSettingsButton.FillColor = Color.YellowGreen;
        defaultSettingsButton.FillDisableColor = Color.DarkGray;
        defaultSettingsButton.FillHoverColor = Color.LightCoral;
        defaultSettingsButton.FillPressColor = Color.DarkRed;
        defaultSettingsButton.FillSelectedColor = Color.Brown;
        defaultSettingsButton.Font = new Font("Lobster", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 0);
        defaultSettingsButton.Location = new Point(233, 4);
        defaultSettingsButton.Margin = new Padding(3, 4, 12, 4);
        defaultSettingsButton.MinimumSize = new Size(1, 1);
        defaultSettingsButton.Name = "defaultSettingsButton";
        defaultSettingsButton.Radius = 20;
        defaultSettingsButton.Size = new Size(120, 40);
        defaultSettingsButton.TabIndex = 3;
        defaultSettingsButton.Text = "default";
        defaultSettingsButton.TipsFont = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
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
        ClientSize = new Size(783, 826);
        Controls.Add(mainTableLayoutPanel);
        Name = "SettingsForm";
        RectColor = Color.Transparent;
        ShowTitle = false;
        Text = "";
        ZoomScaleRect = new Rectangle(0, 0, 783, 826);
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
    private Sunny.UI.UITableLayoutPanel windowHeightTableLayoutPanel;
    private Sunny.UI.UILabel windowHeightLabel;
    private Sunny.UI.UITrackBar windowHeightTrackBar;
    private Sunny.UI.UILabel windowHeightValueLabel;
    private Sunny.UI.UICheckBoxGroup behaviorCheckBoxGroup;
    private Sunny.UI.UIGroupBox visualGroupBox;
    private Sunny.UI.UIGroupBox generalGroupBox;
    private System.Windows.Forms.FlowLayoutPanel generalFlowPanel;
    private Sunny.UI.UITableLayoutPanel visualTableLayoutPanel;
    private Sunny.UI.UILabel opacityValueLabel;
    private Sunny.UI.UITrackBar opacityTrackBar;
    private Sunny.UI.UILabel opacityLabel;
    private Sunny.UI.UIPanel previewPanel;
    private Sunny.UI.UIGroupBox footerGroupBox;
    private Sunny.UI.UIButton cancelButton;
    private Sunny.UI.UIButton saveButton;
    private Sunny.UI.UIButton defaultSettingsButton;
    private Sunny.UI.UIButton shutDownButton;
    private Sunny.UI.UIButton openSettingsTomlButton;
}
