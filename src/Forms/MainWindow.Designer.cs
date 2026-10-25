using System.Drawing;
using System.Windows.Forms;

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
            mainTableLayoutPanel = new TableLayoutPanel();
            headerTableLayoutPanel = new TableLayoutPanel();
            logoLabel = new Label();
            settingsButton = new Button();
            drawerListBox = new DrawerListBox();
            addDrawerButton = new Button();
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
            // 
            // logoLabel
            // 
            logoLabel.Cursor = Cursors.Default;
            logoLabel.Dock = DockStyle.Fill;
            logoLabel.Font = SystemFonts.MessageBoxFont;
            logoLabel.ForeColor = SystemColors.ButtonFace;
            logoLabel.Location = new Point(3, 0);
            logoLabel.Name = "logoLabel";
            logoLabel.AutoSize = false;
            logoLabel.BackColor = Color.Transparent;
            logoLabel.Size = new Size(278, 44);
            logoLabel.TabIndex = 0;
            logoLabel.Text = "winDrawer";
            logoLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // settingsButton
            // 
            settingsButton.Cursor = Cursors.Hand;
            settingsButton.Dock = DockStyle.Right;
            settingsButton.Font = SystemFonts.MessageBoxFont;
            settingsButton.Location = new Point(287, 3);
            settingsButton.MinimumSize = new Size(1, 1);
            settingsButton.Name = "settingsButton";
            settingsButton.Size = new Size(117, 38);
            settingsButton.TabIndex = 1;
            settingsButton.Text = "settings";
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
            drawerListBox.Size = new Size(405, 300);
            drawerListBox.TabIndex = 2;
            drawerListBox.Text = "drawerListBox";
            // 
            // addDrawerButton
            // 
            addDrawerButton.Anchor = AnchorStyles.None;
            addDrawerButton.Cursor = Cursors.Hand;
            addDrawerButton.Font = SystemFonts.MessageBoxFont;
            addDrawerButton.Location = new Point(121, 353);
            addDrawerButton.MinimumSize = new Size(1, 1);
            addDrawerButton.Name = "addDrawerButton";
            addDrawerButton.Size = new Size(170, 44);
            addDrawerButton.TabIndex = 1;
            addDrawerButton.Text = "add drawer";
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

        private TableLayoutPanel mainTableLayoutPanel;
        private TableLayoutPanel headerTableLayoutPanel;
        private Label logoLabel;
        private Button settingsButton;
        private DrawerListBox drawerListBox;
        private Button addDrawerButton;
    }
}
