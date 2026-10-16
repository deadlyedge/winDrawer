using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;

namespace winDrawer
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            mainTableLayoutPanel = new UITableLayoutPanel();
            headerTableLayoutPanel = new UITableLayoutPanel();
            logoLabel = new UILabel();
            settingsButton = new UIButton();
            drawerListBox = new UIListBox();
            addDrawerButton = new UIButton();
            mainTableLayoutPanel.SuspendLayout();
            headerTableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            mainTableLayoutPanel.BackColor = Color.Tomato;
            mainTableLayoutPanel.ColumnCount = 1;
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Controls.Add(headerTableLayoutPanel, 0, 0);
            mainTableLayoutPanel.Controls.Add(drawerListBox, 0, 1);
            mainTableLayoutPanel.Controls.Add(addDrawerButton, 0, 2);
            mainTableLayoutPanel.Dock = DockStyle.Fill;
            mainTableLayoutPanel.Location = new Point(0, 0);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 3;
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            mainTableLayoutPanel.Size = new Size(413, 400);
            mainTableLayoutPanel.TabIndex = 0;
            mainTableLayoutPanel.TagString = null;
            // 
            // headerTableLayoutPanel
            // 
            headerTableLayoutPanel.BackColor = Color.Transparent;
            headerTableLayoutPanel.ColumnCount = 2;
            headerTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            headerTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            headerTableLayoutPanel.Controls.Add(logoLabel, 0, 0);
            headerTableLayoutPanel.Controls.Add(settingsButton, 1, 0);
            headerTableLayoutPanel.Dock = DockStyle.Fill;
            headerTableLayoutPanel.Location = new Point(3, 3);
            headerTableLayoutPanel.Name = "headerTableLayoutPanel";
            headerTableLayoutPanel.RowCount = 1;
            headerTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerTableLayoutPanel.Size = new Size(407, 44);
            headerTableLayoutPanel.TabIndex = 0;
            headerTableLayoutPanel.TagString = null;
            // 
            // logoLabel
            // 
            logoLabel.Cursor = Cursors.Default;
            logoLabel.Dock = DockStyle.Fill;
            logoLabel.Font = SystemFonts.MessageBoxFont;
            logoLabel.ForeColor = SystemColors.ButtonFace;
            logoLabel.Location = new Point(3, 0);
            logoLabel.Name = "logoLabel";
            logoLabel.Size = new Size(278, 44);
            logoLabel.Style = UIStyle.Custom;
            logoLabel.TabIndex = 0;
            logoLabel.Text = "winDrawer";
            logoLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // settingsButton
            // 
            settingsButton.Cursor = Cursors.Hand;
            settingsButton.Dock = DockStyle.Right;
            settingsButton.FillColor = Color.FromArgb(66, 66, 66);
            settingsButton.FillHoverColor = Color.FromArgb(84, 84, 84);
            settingsButton.FillPressColor = Color.FromArgb(63, 63, 63);
            settingsButton.FillSelectedColor = Color.FromArgb(63, 63, 63);
            settingsButton.Font = SystemFonts.MessageBoxFont;
            settingsButton.Location = new Point(287, 3);
            settingsButton.MinimumSize = new Size(1, 1);
            settingsButton.Name = "settingsButton";
            settingsButton.Radius = 12;
            settingsButton.RectColor = Color.Transparent;
            settingsButton.RectDisableColor = Color.Transparent;
            settingsButton.RectHoverColor = Color.Transparent;
            settingsButton.RectPressColor = Color.Transparent;
            settingsButton.RectSelectedColor = Color.Transparent;
            settingsButton.Size = new Size(117, 38);
            settingsButton.Style = UIStyle.Custom;
            settingsButton.TabIndex = 1;
            settingsButton.Text = "settings";
            settingsButton.TipsFont = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            settingsButton.Click += SettingsButton_Click;
            // 
            // drawerListBox
            // 
            drawerListBox.Dock = DockStyle.Fill;
            drawerListBox.FillColor = Color.DarkSalmon;
            drawerListBox.Font = new Font("微软雅黑", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            drawerListBox.HoverColor = Color.FromArgb(155, 200, 255);
            drawerListBox.ItemHeight = 50;
            drawerListBox.ItemSelectForeColor = Color.White;
            drawerListBox.Location = new Point(4, 50);
            drawerListBox.Margin = new Padding(4, 0, 4, 0);
            drawerListBox.MinimumSize = new Size(1, 1);
            drawerListBox.Name = "drawerListBox";
            drawerListBox.Opacity = 100;
            drawerListBox.Padding = new Padding(2);
            drawerListBox.RectColor = Color.DimGray;
            drawerListBox.ScrollBarBackColor = Color.DarkSalmon;
            drawerListBox.ScrollBarColor = Color.DimGray;
            drawerListBox.ScrollBarHandleWidth = 10;
            drawerListBox.ScrollBarStyleInherited = false;
            drawerListBox.ShowText = false;
            drawerListBox.Size = new Size(405, 300);
            drawerListBox.TabIndex = 2;
            drawerListBox.Text = "drawerListBox";
            // 
            // addDrawerButton
            // 
            addDrawerButton.Anchor = AnchorStyles.None;
            addDrawerButton.Cursor = Cursors.Hand;
            addDrawerButton.FillColor = Color.FromArgb(9, 140, 210);
            addDrawerButton.FillHoverColor = Color.FromArgb(0, 94, 140);
            addDrawerButton.FillPressColor = Color.FromArgb(0, 94, 140);
            addDrawerButton.FillSelectedColor = Color.FromArgb(0, 94, 140);
            addDrawerButton.Font = SystemFonts.MessageBoxFont;
            addDrawerButton.Location = new Point(121, 353);
            addDrawerButton.MinimumSize = new Size(1, 1);
            addDrawerButton.Name = "addDrawerButton";
            addDrawerButton.Radius = 20;
            addDrawerButton.RectColor = Color.Transparent;
            addDrawerButton.RectDisableColor = Color.Transparent;
            addDrawerButton.RectHoverColor = Color.Transparent;
            addDrawerButton.RectPressColor = Color.Transparent;
            addDrawerButton.RectSelectedColor = Color.Transparent;
            addDrawerButton.Size = new Size(170, 44);
            addDrawerButton.Style = UIStyle.Custom;
            addDrawerButton.TabIndex = 1;
            addDrawerButton.Text = "add drawer";
            addDrawerButton.TipsFont = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            addDrawerButton.Click += AddDrawerButton_Click;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(AppConstants.Defaults.MainWindowWidth, AppConstants.Defaults.MainWindowHeightDefault);
            Controls.Add(mainTableLayoutPanel);
            Name = "MainWindow";
            ShowInTaskbar = false;
            Text = "winDrawerMainWindow";
            mainTableLayoutPanel.ResumeLayout(false);
            headerTableLayoutPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Sunny.UI.UITableLayoutPanel mainTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel headerTableLayoutPanel;
        private Sunny.UI.UILabel logoLabel;
        private Sunny.UI.UIButton settingsButton;
        private Sunny.UI.UIListBox drawerListBox;
        private Sunny.UI.UIButton addDrawerButton;
    }
}
