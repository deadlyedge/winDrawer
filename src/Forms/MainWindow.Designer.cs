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
            components = new System.ComponentModel.Container();
            mainTableLayoutPanel = new TableLayoutPanel();
            headerTableLayoutPanel = new TableLayoutPanel();
            logoLabel = new Label();
            addDrawerButton = new Button();
            settingsButton = new Button();
            drawerListBox = new DrawerListBox();
            drawerListHostPanel = new CompositedPanel();
            headerToolTip = new ToolTip(components);
            mainTableLayoutPanel.SuspendLayout();
            headerTableLayoutPanel.SuspendLayout();
            drawerListHostPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            mainTableLayoutPanel.BackColor = Color.Tomato;
            mainTableLayoutPanel.ColumnCount = 1;
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Controls.Add(headerTableLayoutPanel, 0, 0);
            mainTableLayoutPanel.Controls.Add(drawerListHostPanel, 0, 1);
            mainTableLayoutPanel.Dock = DockStyle.Fill;
            mainTableLayoutPanel.Location = new Point(0, 0);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 2;
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Size = new Size(300, 400);
            mainTableLayoutPanel.TabIndex = 0;
            // 
            // headerTableLayoutPanel
            // 
            headerTableLayoutPanel.BackColor = Color.Transparent;
            headerTableLayoutPanel.ColumnCount = 3;
            headerTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            headerTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            headerTableLayoutPanel.Controls.Add(logoLabel, 0, 0);
            headerTableLayoutPanel.Controls.Add(addDrawerButton, 1, 0);
            headerTableLayoutPanel.Controls.Add(settingsButton, 2, 0);
            headerTableLayoutPanel.Dock = DockStyle.Fill;
            headerTableLayoutPanel.Location = new Point(3, 3);
            headerTableLayoutPanel.Name = "headerTableLayoutPanel";
            headerTableLayoutPanel.RowCount = 1;
            headerTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerTableLayoutPanel.Size = new Size(294, 44);
            headerTableLayoutPanel.TabIndex = 0;
            // 
            // logoLabel
            // 
            logoLabel.AutoSize = false;
            logoLabel.BackColor = Color.Transparent;
            logoLabel.Cursor = Cursors.Default;
            logoLabel.Dock = DockStyle.Fill;
            logoLabel.Font = SystemFonts.MessageBoxFont;
            logoLabel.ForeColor = SystemColors.ButtonFace;
            logoLabel.Location = new Point(3, 0);
            logoLabel.Name = "logoLabel";
            logoLabel.Size = new Size(200, 44);
            logoLabel.TabIndex = 0;
            logoLabel.Text = "winDrawer";
            logoLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // addDrawerButton
            // 
            addDrawerButton.Cursor = Cursors.Hand;
            addDrawerButton.Dock = DockStyle.Fill;
            addDrawerButton.Font = SystemFonts.MessageBoxFont;
            addDrawerButton.Location = new Point(209, 3);
            addDrawerButton.Margin = new Padding(3);
            addDrawerButton.MinimumSize = new Size(1, 1);
            addDrawerButton.Name = "addDrawerButton";
            addDrawerButton.Size = new Size(38, 38);
            addDrawerButton.TabIndex = 1;
            addDrawerButton.TextAlign = ContentAlignment.MiddleCenter;
            addDrawerButton.UseMnemonic = false;
            addDrawerButton.Click += AddDrawerButton_Click;
            headerToolTip.SetToolTip(addDrawerButton, "Add drawer");
            // 
            // settingsButton
            // 
            settingsButton.Cursor = Cursors.Hand;
            settingsButton.Dock = DockStyle.Fill;
            settingsButton.Font = SystemFonts.MessageBoxFont;
            settingsButton.Location = new Point(253, 3);
            settingsButton.Margin = new Padding(3);
            settingsButton.MinimumSize = new Size(1, 1);
            settingsButton.Name = "settingsButton";
            settingsButton.Size = new Size(38, 38);
            settingsButton.TabIndex = 2;
            settingsButton.TextAlign = ContentAlignment.MiddleCenter;
            settingsButton.UseMnemonic = false;
            settingsButton.Click += SettingsButton_Click;
            headerToolTip.SetToolTip(settingsButton, "Settings");
            // 
            // drawerListBox
            // 
            drawerListBox.Dock = DockStyle.Fill;
            drawerListBox.FillColor = Color.DarkSalmon;
            drawerListBox.Font = new Font("微软雅黑", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            drawerListBox.HoverColor = Color.FromArgb(155, 200, 255);
            drawerListBox.ItemHeight = 50;
            drawerListBox.ItemSelectForeColor = Color.White;
            drawerListBox.Location = new Point(0, 0);
            drawerListBox.Margin = Padding.Empty;
            drawerListBox.MinimumSize = new Size(1, 1);
            drawerListBox.Name = "drawerListBox";
            drawerListBox.Size = new Size(292, 350);
            drawerListBox.TabIndex = 0;
            drawerListBox.Text = "drawerListBox";
            //
            // drawerListHostPanel
            //
            drawerListHostPanel.BackColor = Color.FromArgb(64, 64, 64);
            drawerListHostPanel.Controls.Add(drawerListBox);
            drawerListHostPanel.Dock = DockStyle.Fill;
            drawerListHostPanel.Location = new Point(4, 50);
            drawerListHostPanel.Margin = new Padding(4, 0, 4, 0);
            drawerListHostPanel.Name = "drawerListHostPanel";
            drawerListHostPanel.Size = new Size(292, 350);
            drawerListHostPanel.TabIndex = 3;
            // 
            // headerToolTip
            // 
            headerToolTip.AutoPopDelay = 8000;
            headerToolTip.InitialDelay = 400;
            headerToolTip.ReshowDelay = 100;
            // The main window is a borderless, non-taskbar window that is often not the active window;
            // without ShowAlways the tooltips of the icon buttons would never appear.
            headerToolTip.ShowAlways = true;
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
            drawerListHostPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel mainTableLayoutPanel;
        private TableLayoutPanel headerTableLayoutPanel;
        private Label logoLabel;
        private Button addDrawerButton;
        private Button settingsButton;
        private DrawerListBox drawerListBox;
        private CompositedPanel drawerListHostPanel;
        private ToolTip headerToolTip;
    }
}
