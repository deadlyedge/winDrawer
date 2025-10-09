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
            settingsButton = new Button();
            logoLabel = new Label();
            addDrawerButton = new Button();
            drawerListMenu = new UINavMenu();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel2 = new TableLayoutPanel();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            SuspendLayout();
            // 
            // settingsButton
            // 
            settingsButton.BackColor = Color.FromArgb(66, 66, 66);
            settingsButton.Dock = DockStyle.Right;
            settingsButton.FlatAppearance.BorderSize = 0;
            settingsButton.FlatStyle = FlatStyle.Flat;
            settingsButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            settingsButton.ForeColor = Color.White;
            settingsButton.Location = new Point(202, 0);
            settingsButton.Margin = new Padding(0);
            settingsButton.Name = "settingsButton";
            settingsButton.Size = new Size(84, 32);
            settingsButton.TabIndex = 1;
            settingsButton.Text = "Settings";
            settingsButton.UseVisualStyleBackColor = false;
            settingsButton.Click += settingsButton_Click;
            // 
            // logoLabel
            // 
            logoLabel.AutoSize = true;
            logoLabel.Cursor = Cursors.SizeAll;
            logoLabel.Dock = DockStyle.Left;
            logoLabel.Font = new Font("Lobster", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            logoLabel.ForeColor = Color.White;
            logoLabel.Location = new Point(0, 0);
            logoLabel.Margin = new Padding(0);
            logoLabel.Name = "logoLabel";
            logoLabel.Size = new Size(111, 32);
            logoLabel.TabIndex = 0;
            logoLabel.Text = "goDrawer";
            // 
            // addDrawerButton
            // 
            addDrawerButton.Anchor = AnchorStyles.None;
            addDrawerButton.BackColor = Color.FromArgb(0, 115, 176);
            addDrawerButton.FlatAppearance.BorderSize = 0;
            addDrawerButton.FlatStyle = FlatStyle.Flat;
            addDrawerButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            addDrawerButton.ForeColor = Color.White;
            addDrawerButton.Location = new Point(78, 253);
            addDrawerButton.Margin = new Padding(0, 4, 0, 4);
            addDrawerButton.Name = "addDrawerButton";
            addDrawerButton.Size = new Size(129, 24);
            addDrawerButton.TabIndex = 3;
            addDrawerButton.Text = "Add Drawer";
            addDrawerButton.UseVisualStyleBackColor = false;
            addDrawerButton.Click += addDrawerButton_Click;
            //
            // drawerListMenu
            //
            drawerListMenu.Dock = DockStyle.Fill;
            drawerListMenu.Location = new Point(0, 32);
            drawerListMenu.Margin = new Padding(0);
            drawerListMenu.Name = "drawerListMenu";
            drawerListMenu.Size = new Size(286, 217);
            drawerListMenu.TabIndex = 1;
            // Event handling will be done in MainWindow.cs
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.Controls.Add(addDrawerButton, 0, 2);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 0);
            tableLayoutPanel1.Controls.Add(drawerListMenu, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            tableLayoutPanel1.Size = new Size(286, 281);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tableLayoutPanel2.Controls.Add(settingsButton, 1, 0);
            tableLayoutPanel2.Controls.Add(logoLabel, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(0, 0);
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(286, 32);
            tableLayoutPanel2.TabIndex = 3;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(242, 143, 45);
            ClientSize = new Size(286, 281);
            Controls.Add(tableLayoutPanel1);
            ForeColor = Color.White;
            Margin = new Padding(2);
            Name = "MainWindow";
            Opacity = 0.8D;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "goDrawer";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private Button settingsButton;
        private Label logoLabel;
        private Button addDrawerButton;
        private UINavMenu drawerListMenu;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
    }
}
