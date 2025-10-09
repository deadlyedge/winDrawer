using Sunny.UI;
using System.Drawing;

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
        generalGroup = new UIGroupBox();
        windowLockLabel = new UILabel();
        windowLockSwitch = new UISwitch();
        autoLaunchLabel = new UILabel();
        autoLaunchSwitch = new UISwitch();
        themeGroup = new UIGroupBox();
        alphaValueLabel = new UILabel();
        lightnessValueLabel = new UILabel();
        saturationValueLabel = new UILabel();
        hueValueLabel = new UILabel();
        alphaTrackBar = new UITrackBar();
        lightnessTrackBar = new UITrackBar();
        saturationTrackBar = new UITrackBar();
        hueTrackBar = new UITrackBar();
        alphaLabel = new UILabel();
        lightnessLabel = new UILabel();
        saturationLabel = new UILabel();
        hueLabel = new UILabel();
        previewPanel = new UIPanel();
        previewTitleLabel = new UILabel();
        previewLabel = new UILabel();
        okButton = new UIButton();
        cancelButton = new UIButton();
        shutdownButton = new UIButton();
        generalGroup.SuspendLayout();
        themeGroup.SuspendLayout();
        previewPanel.SuspendLayout();
        SuspendLayout();
        // 
        // generalGroup
        // 
        generalGroup.Controls.Add(windowLockLabel);
        generalGroup.Controls.Add(windowLockSwitch);
        generalGroup.Controls.Add(autoLaunchLabel);
        generalGroup.Controls.Add(autoLaunchSwitch);
        generalGroup.FillColor = Color.FromArgb(38, 45, 54);
        generalGroup.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        generalGroup.Location = new Point(24, 64);
        generalGroup.Margin = new Padding(0, 12, 0, 12);
        generalGroup.MinimumSize = new Size(1, 1);
        generalGroup.Name = "generalGroup";
        generalGroup.Padding = new Padding(0, 32, 0, 0);
        generalGroup.RectColor = Color.FromArgb(56, 63, 72);
        generalGroup.Size = new Size(576, 120);
        generalGroup.Style = UIStyle.Custom;
        generalGroup.StyleCustomMode = true;
        generalGroup.TabIndex = 0;
        generalGroup.Text = "³£¹æ";
        generalGroup.TextAlignment = ContentAlignment.MiddleLeft;
        // 
        // windowLockLabel
        // 
        windowLockLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        windowLockLabel.ForeColor = Color.FromArgb(48, 48, 48);
        windowLockLabel.Location = new Point(76, 76);
        windowLockLabel.Name = "windowLockLabel";
        windowLockLabel.Size = new Size(298, 24);
        windowLockLabel.Style = UIStyle.Custom;
        windowLockLabel.StyleCustomMode = true;
        windowLockLabel.TabIndex = 3;
        windowLockLabel.Text = "Ëø¶¨´°¿ÚÎ»ÖÃ";
        windowLockLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // windowLockSwitch
        // 
        windowLockSwitch.Font = new Font("ËÎÌå", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        windowLockSwitch.Location = new Point(20, 72);
        windowLockSwitch.Margin = new Padding(4);
        windowLockSwitch.MinimumSize = new Size(1, 1);
        windowLockSwitch.Name = "windowLockSwitch";
        windowLockSwitch.Size = new Size(48, 28);
        windowLockSwitch.Style = UIStyle.Custom;
        windowLockSwitch.StyleCustomMode = true;
        windowLockSwitch.TabIndex = 2;
        // 
        // autoLaunchLabel
        // 
        autoLaunchLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        autoLaunchLabel.ForeColor = Color.FromArgb(48, 48, 48);
        autoLaunchLabel.Location = new Point(76, 40);
        autoLaunchLabel.Name = "autoLaunchLabel";
        autoLaunchLabel.Size = new Size(298, 24);
        autoLaunchLabel.Style = UIStyle.Custom;
        autoLaunchLabel.StyleCustomMode = true;
        autoLaunchLabel.TabIndex = 1;
        autoLaunchLabel.Text = "¿ª»úÊ±×Ô¶¯Æô¶¯Ó¦ÓÃ";
        autoLaunchLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // autoLaunchSwitch
        // 
        autoLaunchSwitch.Font = new Font("ËÎÌå", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
        autoLaunchSwitch.Location = new Point(20, 36);
        autoLaunchSwitch.Margin = new Padding(4);
        autoLaunchSwitch.MinimumSize = new Size(1, 1);
        autoLaunchSwitch.Name = "autoLaunchSwitch";
        autoLaunchSwitch.Size = new Size(48, 28);
        autoLaunchSwitch.Style = UIStyle.Custom;
        autoLaunchSwitch.StyleCustomMode = true;
        autoLaunchSwitch.TabIndex = 0;
        // 
        // themeGroup
        // 
        themeGroup.Controls.Add(alphaValueLabel);
        themeGroup.Controls.Add(lightnessValueLabel);
        themeGroup.Controls.Add(saturationValueLabel);
        themeGroup.Controls.Add(hueValueLabel);
        themeGroup.Controls.Add(alphaTrackBar);
        themeGroup.Controls.Add(lightnessTrackBar);
        themeGroup.Controls.Add(saturationTrackBar);
        themeGroup.Controls.Add(hueTrackBar);
        themeGroup.Controls.Add(alphaLabel);
        themeGroup.Controls.Add(lightnessLabel);
        themeGroup.Controls.Add(saturationLabel);
        themeGroup.Controls.Add(hueLabel);
        themeGroup.Controls.Add(previewPanel);
        themeGroup.FillColor = Color.FromArgb(38, 45, 54);
        themeGroup.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        themeGroup.Location = new Point(24, 204);
        themeGroup.Margin = new Padding(0, 12, 0, 12);
        themeGroup.MinimumSize = new Size(1, 1);
        themeGroup.Name = "themeGroup";
        themeGroup.Padding = new Padding(0, 32, 0, 0);
        themeGroup.RectColor = Color.FromArgb(56, 63, 72);
        themeGroup.Size = new Size(576, 244);
        themeGroup.Style = UIStyle.Custom;
        themeGroup.StyleCustomMode = true;
        themeGroup.TabIndex = 1;
        themeGroup.Text = "Ö÷ÌâÑÕÉ«";
        themeGroup.TextAlignment = ContentAlignment.MiddleLeft;
        // 
        // alphaValueLabel
        // 
        alphaValueLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        alphaValueLabel.ForeColor = Color.FromArgb(48, 48, 48);
        alphaValueLabel.Location = new Point(320, 196);
        alphaValueLabel.Name = "alphaValueLabel";
        alphaValueLabel.Size = new Size(64, 24);
        alphaValueLabel.Style = UIStyle.Custom;
        alphaValueLabel.StyleCustomMode = true;
        alphaValueLabel.TabIndex = 12;
        alphaValueLabel.Text = "60%";
        alphaValueLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // lightnessValueLabel
        // 
        lightnessValueLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        lightnessValueLabel.ForeColor = Color.FromArgb(48, 48, 48);
        lightnessValueLabel.Location = new Point(320, 152);
        lightnessValueLabel.Name = "lightnessValueLabel";
        lightnessValueLabel.Size = new Size(64, 24);
        lightnessValueLabel.Style = UIStyle.Custom;
        lightnessValueLabel.StyleCustomMode = true;
        lightnessValueLabel.TabIndex = 11;
        lightnessValueLabel.Text = "41%";
        lightnessValueLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // saturationValueLabel
        // 
        saturationValueLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        saturationValueLabel.ForeColor = Color.FromArgb(48, 48, 48);
        saturationValueLabel.Location = new Point(320, 108);
        saturationValueLabel.Name = "saturationValueLabel";
        saturationValueLabel.Size = new Size(64, 24);
        saturationValueLabel.Style = UIStyle.Custom;
        saturationValueLabel.StyleCustomMode = true;
        saturationValueLabel.TabIndex = 10;
        saturationValueLabel.Text = "100%";
        saturationValueLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // hueValueLabel
        // 
        hueValueLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        hueValueLabel.ForeColor = Color.FromArgb(48, 48, 48);
        hueValueLabel.Location = new Point(320, 64);
        hueValueLabel.Name = "hueValueLabel";
        hueValueLabel.Size = new Size(64, 24);
        hueValueLabel.Style = UIStyle.Custom;
        hueValueLabel.StyleCustomMode = true;
        hueValueLabel.TabIndex = 9;
        hueValueLabel.Text = "194¡ã";
        hueValueLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // alphaTrackBar
        // 
        alphaTrackBar.FillColor = Color.FromArgb(24, 27, 34);
        alphaTrackBar.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        alphaTrackBar.Location = new Point(112, 192);
        alphaTrackBar.Margin = new Padding(4);
        alphaTrackBar.MinimumSize = new Size(1, 1);
        alphaTrackBar.Name = "alphaTrackBar";
        alphaTrackBar.RectColor = Color.FromArgb(70, 75, 82);
        alphaTrackBar.Size = new Size(204, 28);
        alphaTrackBar.Style = UIStyle.Custom;
        alphaTrackBar.StyleCustomMode = true;
        alphaTrackBar.TabIndex = 8;
        // 
        // lightnessTrackBar
        // 
        lightnessTrackBar.FillColor = Color.FromArgb(24, 27, 34);
        lightnessTrackBar.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        lightnessTrackBar.Location = new Point(112, 148);
        lightnessTrackBar.Margin = new Padding(4);
        lightnessTrackBar.MinimumSize = new Size(1, 1);
        lightnessTrackBar.Name = "lightnessTrackBar";
        lightnessTrackBar.RectColor = Color.FromArgb(70, 75, 82);
        lightnessTrackBar.Size = new Size(204, 28);
        lightnessTrackBar.Style = UIStyle.Custom;
        lightnessTrackBar.StyleCustomMode = true;
        lightnessTrackBar.TabIndex = 6;
        // 
        // saturationTrackBar
        // 
        saturationTrackBar.FillColor = Color.FromArgb(24, 27, 34);
        saturationTrackBar.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        saturationTrackBar.Location = new Point(112, 104);
        saturationTrackBar.Margin = new Padding(4);
        saturationTrackBar.MinimumSize = new Size(1, 1);
        saturationTrackBar.Name = "saturationTrackBar";
        saturationTrackBar.RectColor = Color.FromArgb(70, 75, 82);
        saturationTrackBar.Size = new Size(204, 28);
        saturationTrackBar.Style = UIStyle.Custom;
        saturationTrackBar.StyleCustomMode = true;
        saturationTrackBar.TabIndex = 4;
        // 
        // hueTrackBar
        // 
        hueTrackBar.FillColor = Color.FromArgb(24, 27, 34);
        hueTrackBar.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        hueTrackBar.Location = new Point(112, 60);
        hueTrackBar.Margin = new Padding(4);
        hueTrackBar.MinimumSize = new Size(1, 1);
        hueTrackBar.Name = "hueTrackBar";
        hueTrackBar.RectColor = Color.FromArgb(70, 75, 82);
        hueTrackBar.Size = new Size(204, 28);
        hueTrackBar.Style = UIStyle.Custom;
        hueTrackBar.StyleCustomMode = true;
        hueTrackBar.TabIndex = 2;
        // 
        // alphaLabel
        // 
        alphaLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        alphaLabel.ForeColor = Color.FromArgb(48, 48, 48);
        alphaLabel.Location = new Point(20, 196);
        alphaLabel.Name = "alphaLabel";
        alphaLabel.Size = new Size(88, 24);
        alphaLabel.Style = UIStyle.Custom;
        alphaLabel.StyleCustomMode = true;
        alphaLabel.TabIndex = 7;
        alphaLabel.Text = "Í¸Ã÷¶È";
        alphaLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lightnessLabel
        // 
        lightnessLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        lightnessLabel.ForeColor = Color.FromArgb(48, 48, 48);
        lightnessLabel.Location = new Point(20, 152);
        lightnessLabel.Name = "lightnessLabel";
        lightnessLabel.Size = new Size(88, 24);
        lightnessLabel.Style = UIStyle.Custom;
        lightnessLabel.StyleCustomMode = true;
        lightnessLabel.TabIndex = 5;
        lightnessLabel.Text = "ÁÁ¶È";
        lightnessLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // saturationLabel
        // 
        saturationLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        saturationLabel.ForeColor = Color.FromArgb(48, 48, 48);
        saturationLabel.Location = new Point(20, 108);
        saturationLabel.Name = "saturationLabel";
        saturationLabel.Size = new Size(88, 24);
        saturationLabel.Style = UIStyle.Custom;
        saturationLabel.StyleCustomMode = true;
        saturationLabel.TabIndex = 3;
        saturationLabel.Text = "±¥ºÍ¶È";
        saturationLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // hueLabel
        // 
        hueLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        hueLabel.ForeColor = Color.FromArgb(48, 48, 48);
        hueLabel.Location = new Point(20, 64);
        hueLabel.Name = "hueLabel";
        hueLabel.Size = new Size(88, 24);
        hueLabel.Style = UIStyle.Custom;
        hueLabel.StyleCustomMode = true;
        hueLabel.TabIndex = 1;
        hueLabel.Text = "É«Ïà";
        hueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // previewPanel
        // 
        previewPanel.Controls.Add(previewTitleLabel);
        previewPanel.Controls.Add(previewLabel);
        previewPanel.FillColor = Color.FromArgb(30, 34, 41);
        previewPanel.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        previewPanel.Location = new Point(376, 60);
        previewPanel.Margin = new Padding(4);
        previewPanel.MinimumSize = new Size(1, 1);
        previewPanel.Name = "previewPanel";
        previewPanel.RectColor = Color.FromArgb(70, 75, 82);
        previewPanel.Size = new Size(196, 160);
        previewPanel.Style = UIStyle.Custom;
        previewPanel.StyleCustomMode = true;
        previewPanel.TabIndex = 0;
        previewPanel.Text = null;
        previewPanel.TextAlignment = ContentAlignment.MiddleCenter;
        // 
        // previewTitleLabel
        // 
        previewTitleLabel.Font = new Font("Î¢ÈíÑÅºÚ", 9F, FontStyle.Bold);
        previewTitleLabel.ForeColor = Color.White;
        previewTitleLabel.Location = new Point(12, 12);
        previewTitleLabel.Name = "previewTitleLabel";
        previewTitleLabel.Size = new Size(96, 24);
        previewTitleLabel.Style = UIStyle.Custom;
        previewTitleLabel.StyleCustomMode = true;
        previewTitleLabel.TabIndex = 1;
        previewTitleLabel.Text = "Ô¤ÀÀ";
        previewTitleLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // previewLabel
        // 
        previewLabel.Font = new Font("Consolas", 9F);
        previewLabel.ForeColor = Color.White;
        previewLabel.Location = new Point(12, 52);
        previewLabel.Name = "previewLabel";
        previewLabel.Size = new Size(170, 92);
        previewLabel.Style = UIStyle.Custom;
        previewLabel.StyleCustomMode = true;
        previewLabel.TabIndex = 0;
        previewLabel.Text = "hsla(194, 100%, 41%, 0.60)";
        previewLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // okButton
        // 
        okButton.Cursor = Cursors.Hand;
        okButton.FillColor = Color.FromArgb(0, 168, 95);
        okButton.FillHoverColor = Color.FromArgb(0, 186, 106);
        okButton.FillPressColor = Color.FromArgb(0, 143, 80);
        okButton.Font = new Font("Î¢ÈíÑÅºÚ", 9F, FontStyle.Bold);
        okButton.Location = new Point(236, 468);
        okButton.MinimumSize = new Size(1, 1);
        okButton.Name = "okButton";
        okButton.RectColor = Color.Transparent;
        okButton.Size = new Size(96, 34);
        okButton.Style = UIStyle.Custom;
        okButton.StyleCustomMode = true;
        okButton.TabIndex = 2;
        okButton.Text = "±£´æ";
        okButton.TipsFont = new Font("ËÎÌå", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
        // 
        // cancelButton
        // 
        cancelButton.Cursor = Cursors.Hand;
        cancelButton.FillColor = Color.FromArgb(64, 70, 78);
        cancelButton.FillHoverColor = Color.FromArgb(82, 88, 96);
        cancelButton.FillPressColor = Color.FromArgb(52, 58, 66);
        cancelButton.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        cancelButton.Location = new Point(348, 468);
        cancelButton.MinimumSize = new Size(1, 1);
        cancelButton.Name = "cancelButton";
        cancelButton.RectColor = Color.Transparent;
        cancelButton.Size = new Size(96, 34);
        cancelButton.Style = UIStyle.Custom;
        cancelButton.StyleCustomMode = true;
        cancelButton.TabIndex = 3;
        cancelButton.Text = "È¡Ïû";
        cancelButton.TipsFont = new Font("ËÎÌå", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
        // 
        // shutdownButton
        // 
        shutdownButton.Cursor = Cursors.Hand;
        shutdownButton.FillColor = Color.FromArgb(208, 72, 72);
        shutdownButton.FillHoverColor = Color.FromArgb(224, 96, 96);
        shutdownButton.FillPressColor = Color.FromArgb(176, 56, 56);
        shutdownButton.Font = new Font("Î¢ÈíÑÅºÚ", 9F);
        shutdownButton.Location = new Point(460, 468);
        shutdownButton.MinimumSize = new Size(1, 1);
        shutdownButton.Name = "shutdownButton";
        shutdownButton.RectColor = Color.Transparent;
        shutdownButton.Size = new Size(96, 34);
        shutdownButton.Style = UIStyle.Custom;
        shutdownButton.StyleCustomMode = true;
        shutdownButton.TabIndex = 4;
        shutdownButton.Text = "ÍË³ö³ÌÐò";
        shutdownButton.TipsFont = new Font("ËÎÌå", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
        // 
        // SettingsForm
        // 
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(24, 27, 34);
        ClientSize = new Size(624, 560);
        Controls.Add(shutdownButton);
        Controls.Add(cancelButton);
        Controls.Add(okButton);
        Controls.Add(themeGroup);
        Controls.Add(generalGroup);
        ForeColor = Color.White;
        MinimumSize = new Size(624, 560);
        Name = "SettingsForm";
        Padding = new Padding(24, 16, 24, 24);
        RectColor = Color.FromArgb(56, 63, 72);
        Style = UIStyle.Custom;
        StyleCustomMode = true;
        Text = "ÉèÖÃ";
        ZoomScaleRect = new Rectangle(0, 0, 614, 590);
        generalGroup.ResumeLayout(false);
        themeGroup.ResumeLayout(false);
        previewPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private UIGroupBox generalGroup;
    private UILabel windowLockLabel;
    private UISwitch windowLockSwitch;
    private UILabel autoLaunchLabel;
    private UISwitch autoLaunchSwitch;
    private UIGroupBox themeGroup;
    private UILabel alphaValueLabel;
    private UILabel lightnessValueLabel;
    private UILabel saturationValueLabel;
    private UILabel hueValueLabel;
    private UITrackBar alphaTrackBar;
    private UITrackBar lightnessTrackBar;
    private UITrackBar saturationTrackBar;
    private UITrackBar hueTrackBar;
    private UILabel alphaLabel;
    private UILabel lightnessLabel;
    private UILabel saturationLabel;
    private UILabel hueLabel;
    private UIPanel previewPanel;
    private UILabel previewTitleLabel;
    private UILabel previewLabel;
    private UIButton okButton;
    private UIButton cancelButton;
    private UIButton shutdownButton;


}




