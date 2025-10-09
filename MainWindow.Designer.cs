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
            drawerListBox = new UIListBox();
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
            settingsButton.Location = new Point(303, 0);
            settingsButton.Margin = new Padding(0);
            settingsButton.Name = "settingsButton";
            settingsButton.Size = new Size(131, 58);
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
            logoLabel.Size = new Size(240, 58);
            logoLabel.TabIndex = 0;
            logoLabel.Text = "winDrawer";
            // 
            // addDrawerButton
            // 
            addDrawerButton.Anchor = AnchorStyles.None;
            addDrawerButton.BackColor = Color.FromArgb(0, 115, 176);
            addDrawerButton.FlatAppearance.BorderSize = 0;
            addDrawerButton.FlatStyle = FlatStyle.Flat;
            addDrawerButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            addDrawerButton.ForeColor = Color.White;
            addDrawerButton.Location = new Point(88, 343);
            addDrawerButton.Margin = new Padding(0, 7, 0, 7);
            addDrawerButton.Name = "addDrawerButton";
            addDrawerButton.Size = new Size(258, 44);
            addDrawerButton.TabIndex = 3;
            addDrawerButton.Text = "Add Drawer";
            addDrawerButton.UseVisualStyleBackColor = false;
            addDrawerButton.Click += addDrawerButton_Click;
            // 
            // drawerListBox
            // 
            drawerListBox.Dock = DockStyle.Fill;
            drawerListBox.FillColor = Color.Orange;
            drawerListBox.Font = new Font("微软雅黑", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            drawerListBox.HoverColor = Color.FromArgb(155, 200, 255);
            drawerListBox.ItemHeight = 36;
            drawerListBox.ItemSelectForeColor = Color.White;
            drawerListBox.Location = new Point(0, 58);
            drawerListBox.Margin = new Padding(0);
            drawerListBox.MinimumSize = new Size(2, 2);
            drawerListBox.Name = "drawerListBox";
            drawerListBox.Padding = new Padding(4, 4, 4, 4);
            drawerListBox.ScrollBarBackColor = Color.FromArgb(255, 128, 0);
            drawerListBox.ScrollBarHandleWidth = 8;
            drawerListBox.ScrollBarStyleInherited = false;
            drawerListBox.ShowText = false;
            drawerListBox.Size = new Size(434, 278);
            drawerListBox.TabIndex = 1;
            drawerListBox.Text = null;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.Controls.Add(addDrawerButton, 0, 2);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 0, 0);
            tableLayoutPanel1.Controls.Add(drawerListBox, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(6, 5, 6, 5);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            tableLayoutPanel1.Size = new Size(434, 394);
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
            tableLayoutPanel2.Size = new Size(434, 58);
            tableLayoutPanel2.TabIndex = 3;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(242, 143, 45);
            ClientSize = new Size(434, 394);
            Controls.Add(tableLayoutPanel1);
            ForeColor = Color.White;
            Margin = new Padding(4, 4, 4, 4);
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
        private UIListBox drawerListBox;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
    }
}
