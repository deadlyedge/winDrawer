using System.Drawing;
using System.Windows.Forms;

namespace winDrawer
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            headerPanel = new Panel();
            titleLabel = new Label();
            contentPanel = new Panel();
            previewPanel = new Panel();
            previewLabel = new Label();
            alphaTrackBar = new TrackBar();
            lightnessTrackBar = new TrackBar();
            saturationTrackBar = new TrackBar();
            hueTrackBar = new TrackBar();
            alphaLabel = new Label();
            lightnessLabel = new Label();
            saturationLabel = new Label();
            hueLabel = new Label();
            autoLaunchCheckBox = new CheckBox();
            okButton = new Button();
            cancelButton = new Button();
            shutdownButton = new Button();
            headerPanel.SuspendLayout();
            contentPanel.SuspendLayout();
            previewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)alphaTrackBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lightnessTrackBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)saturationTrackBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)hueTrackBar).BeginInit();
            SuspendLayout();
            // 
            // headerPanel
            // 
            headerPanel.BackColor = Color.FromArgb(242, 143, 45);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Margin = new Padding(0);
            headerPanel.Name = "headerPanel";
            headerPanel.Padding = new Padding(20, 16, 20, 16);
            headerPanel.Size = new Size(460, 68);
            headerPanel.TabIndex = 0;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            titleLabel.ForeColor = Color.White;
            titleLabel.Location = new Point(20, 18);
            titleLabel.Margin = new Padding(0);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(86, 30);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "\u8bbe\u7f6e";
            // 
            // contentPanel
            // 
            contentPanel.BackColor = Color.FromArgb(45, 48, 56);
            contentPanel.Controls.Add(previewPanel);
            contentPanel.Controls.Add(alphaTrackBar);
            contentPanel.Controls.Add(lightnessTrackBar);
            contentPanel.Controls.Add(saturationTrackBar);
            contentPanel.Controls.Add(hueTrackBar);
            contentPanel.Controls.Add(alphaLabel);
            contentPanel.Controls.Add(lightnessLabel);
            contentPanel.Controls.Add(saturationLabel);
            contentPanel.Controls.Add(hueLabel);
            contentPanel.Controls.Add(autoLaunchCheckBox);
            contentPanel.Controls.Add(okButton);
            contentPanel.Controls.Add(cancelButton);
            contentPanel.Controls.Add(shutdownButton);
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Location = new Point(0, 68);
            contentPanel.Margin = new Padding(0);
            contentPanel.Name = "contentPanel";
            contentPanel.Padding = new Padding(24, 24, 24, 24);
            contentPanel.Size = new Size(460, 362);
            contentPanel.TabIndex = 1;
            // 
            // previewPanel
            // 
            previewPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            previewPanel.BackColor = Color.FromArgb(0, 0, 0, 0);
            previewPanel.BorderStyle = BorderStyle.FixedSingle;
            previewPanel.Controls.Add(previewLabel);
            previewPanel.Location = new Point(279, 28);
            previewPanel.Margin = new Padding(12, 0, 0, 12);
            previewPanel.Name = "previewPanel";
            previewPanel.Padding = new Padding(8);
            previewPanel.Size = new Size(157, 92);
            previewPanel.TabIndex = 12;
            // 
            // previewLabel
            // 
            previewLabel.Dock = DockStyle.Fill;
            previewLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            previewLabel.ForeColor = Color.White;
            previewLabel.Location = new Point(8, 8);
            previewLabel.Margin = new Padding(0);
            previewLabel.Name = "previewLabel";
            previewLabel.Size = new Size(139, 74);
            previewLabel.TabIndex = 0;
            previewLabel.Text = "hsla(0, 0%, 0%, 0.00)";
            previewLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // alphaTrackBar
            // 
            alphaTrackBar.Location = new Point(24, 260);
            alphaTrackBar.Margin = new Padding(0, 24, 0, 0);
            alphaTrackBar.Maximum = 100;
            alphaTrackBar.Name = "alphaTrackBar";
            alphaTrackBar.Size = new Size(244, 45);
            alphaTrackBar.TabIndex = 7;
            alphaTrackBar.TickFrequency = 10;
            alphaTrackBar.Value = 80;
            alphaTrackBar.Scroll += ChannelTrackBar_Scroll;
            // 
            // lightnessTrackBar
            // 
            lightnessTrackBar.Location = new Point(24, 197);
            lightnessTrackBar.Margin = new Padding(0, 24, 0, 0);
            lightnessTrackBar.Maximum = 100;
            lightnessTrackBar.Name = "lightnessTrackBar";
            lightnessTrackBar.Size = new Size(244, 45);
            lightnessTrackBar.TabIndex = 6;
            lightnessTrackBar.TickFrequency = 10;
            lightnessTrackBar.Value = 36;
            lightnessTrackBar.Scroll += ChannelTrackBar_Scroll;
            // 
            // saturationTrackBar
            // 
            saturationTrackBar.Location = new Point(24, 134);
            saturationTrackBar.Margin = new Padding(0, 24, 0, 0);
            saturationTrackBar.Maximum = 100;
            saturationTrackBar.Name = "saturationTrackBar";
            saturationTrackBar.Size = new Size(244, 45);
            saturationTrackBar.TabIndex = 5;
            saturationTrackBar.TickFrequency = 10;
            saturationTrackBar.Value = 40;
            saturationTrackBar.Scroll += ChannelTrackBar_Scroll;
            // 
            // hueTrackBar
            // 
            hueTrackBar.Location = new Point(24, 71);
            hueTrackBar.Margin = new Padding(0, 24, 0, 0);
            hueTrackBar.Maximum = 360;
            hueTrackBar.Name = "hueTrackBar";
            hueTrackBar.Size = new Size(244, 45);
            hueTrackBar.TabIndex = 4;
            hueTrackBar.TickFrequency = 30;
            hueTrackBar.Value = 192;
            hueTrackBar.Scroll += ChannelTrackBar_Scroll;
            // 
            // alphaLabel
            // 
            alphaLabel.AutoSize = true;
            alphaLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            alphaLabel.ForeColor = Color.White;
            alphaLabel.Location = new Point(24, 239);
            alphaLabel.Margin = new Padding(0);
            alphaLabel.Name = "alphaLabel";
            alphaLabel.Size = new Size(57, 15);
            alphaLabel.TabIndex = 3;
            alphaLabel.Text = "\u900f\u660e\u5ea6 (A)";
            // 
            // lightnessLabel
            // 
            lightnessLabel.AutoSize = true;
            lightnessLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            lightnessLabel.ForeColor = Color.White;
            lightnessLabel.Location = new Point(24, 176);
            lightnessLabel.Margin = new Padding(0);
            lightnessLabel.Name = "lightnessLabel";
            lightnessLabel.Size = new Size(59, 15);
            lightnessLabel.TabIndex = 2;
            lightnessLabel.Text = "\u4eae\u5ea6 (L)";
            // 
            // saturationLabel
            // 
            saturationLabel.AutoSize = true;
            saturationLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            saturationLabel.ForeColor = Color.White;
            saturationLabel.Location = new Point(24, 113);
            saturationLabel.Margin = new Padding(0);
            saturationLabel.Name = "saturationLabel";
            saturationLabel.Size = new Size(59, 15);
            saturationLabel.TabIndex = 1;
            saturationLabel.Text = "\u9971\u548c\u5ea6 (S)";
            // 
            // hueLabel
            // 
            hueLabel.AutoSize = true;
            hueLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            hueLabel.ForeColor = Color.White;
            hueLabel.Location = new Point(24, 50);
            hueLabel.Margin = new Padding(0);
            hueLabel.Name = "hueLabel";
            hueLabel.Size = new Size(54, 15);
            hueLabel.TabIndex = 0;
            hueLabel.Text = "\u8272\u76f8 (H)";
            // 
            // autoLaunchCheckBox
            // 
            autoLaunchCheckBox.AutoSize = true;
            autoLaunchCheckBox.ForeColor = Color.White;
            autoLaunchCheckBox.Location = new Point(24, 315);
            autoLaunchCheckBox.Margin = new Padding(0, 24, 0, 0);
            autoLaunchCheckBox.Name = "autoLaunchCheckBox";
            autoLaunchCheckBox.Size = new Size(135, 19);
            autoLaunchCheckBox.TabIndex = 8;
            autoLaunchCheckBox.Text = "\u5f00\u673a\u65f6\u81ea\u52a8\u542f\u52a8\u5e94\u7528";
            autoLaunchCheckBox.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            okButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            okButton.BackColor = Color.FromArgb(35, 150, 80);
            okButton.FlatAppearance.BorderSize = 0;
            okButton.FlatStyle = FlatStyle.Flat;
            okButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            okButton.ForeColor = Color.White;
            okButton.Location = new Point(180, 315);
            okButton.Margin = new Padding(0);
            okButton.Name = "okButton";
            okButton.Size = new Size(84, 28);
            okButton.TabIndex = 9;
            okButton.Text = "OK";
            okButton.UseVisualStyleBackColor = false;
            okButton.Click += okButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            cancelButton.BackColor = Color.FromArgb(100, 102, 107);
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            cancelButton.ForeColor = Color.White;
            cancelButton.Location = new Point(274, 315);
            cancelButton.Margin = new Padding(10, 0, 0, 0);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(84, 28);
            cancelButton.TabIndex = 10;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = false;
            cancelButton.Click += cancelButton_Click;
            // 
            // shutdownButton
            // 
            shutdownButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            shutdownButton.BackColor = Color.FromArgb(191, 68, 55);
            shutdownButton.FlatAppearance.BorderSize = 0;
            shutdownButton.FlatStyle = FlatStyle.Flat;
            shutdownButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            shutdownButton.ForeColor = Color.White;
            shutdownButton.Location = new Point(368, 315);
            shutdownButton.Margin = new Padding(10, 0, 0, 0);
            shutdownButton.Name = "shutdownButton";
            shutdownButton.Size = new Size(84, 28);
            shutdownButton.TabIndex = 11;
            shutdownButton.Text = "Shutdown";
            shutdownButton.UseVisualStyleBackColor = false;
            shutdownButton.Click += shutdownButton_Click;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(460, 430);
            Controls.Add(contentPanel);
            Controls.Add(headerPanel);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            contentPanel.ResumeLayout(false);
            contentPanel.PerformLayout();
            previewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)alphaTrackBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)lightnessTrackBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)saturationTrackBar).EndInit();
            ((System.ComponentModel.ISupportInitialize)hueTrackBar).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel headerPanel;
        private Label titleLabel;
        private Panel contentPanel;
        private TrackBar hueTrackBar;
        private TrackBar saturationTrackBar;
        private TrackBar lightnessTrackBar;
        private TrackBar alphaTrackBar;
        private Label hueLabel;
        private Label saturationLabel;
        private Label lightnessLabel;
        private Label alphaLabel;
        private Panel previewPanel;
        private Label previewLabel;
        private CheckBox autoLaunchCheckBox;
        private Button okButton;
        private Button cancelButton;
        private Button shutdownButton;
    }
}
