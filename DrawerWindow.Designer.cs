using Sunny.UI;
using System.ComponentModel;

namespace winDrawer
{
    partial class DrawerWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
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
            mainTableLayoutPanel = new UITableLayoutPanel();
            drawerHeaderTableLayoutPanel = new UITableLayoutPanel();
            drawerButtonTableLayoutPanel = new UITableLayoutPanel();
            closeDrawerButton = new UIButton();
            refreshDrawerButton = new UIButton();
            fullPathLabel = new UILabel();
            drawerContentFlowLayoutPanel = new UIFlowLayoutPanel();
            mainTableLayoutPanel.SuspendLayout();
            drawerHeaderTableLayoutPanel.SuspendLayout();
            drawerButtonTableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            mainTableLayoutPanel.BackColor = Color.FromArgb(64, 64, 64);
            mainTableLayoutPanel.ColumnCount = 1;
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Controls.Add(drawerHeaderTableLayoutPanel, 0, 0);
            mainTableLayoutPanel.Controls.Add(drawerContentFlowLayoutPanel, 0, 1);
            mainTableLayoutPanel.Dock = DockStyle.Fill;
            mainTableLayoutPanel.Location = new Point(0, 0);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 2;
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            mainTableLayoutPanel.Size = new Size(783, 583);
            mainTableLayoutPanel.TabIndex = 1;
            mainTableLayoutPanel.TagString = null;
            // 
            // drawerHeaderTableLayoutPanel
            // 
            drawerHeaderTableLayoutPanel.ColumnCount = 2;
            drawerHeaderTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            drawerHeaderTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            drawerHeaderTableLayoutPanel.Controls.Add(fullPathLabel, 0, 0);
            drawerHeaderTableLayoutPanel.Controls.Add(drawerButtonTableLayoutPanel, 1, 0);
            drawerHeaderTableLayoutPanel.Dock = DockStyle.Fill;
            drawerHeaderTableLayoutPanel.Location = new Point(3, 3);
            drawerHeaderTableLayoutPanel.Name = "drawerHeaderTableLayoutPanel";
            drawerHeaderTableLayoutPanel.RowCount = 1;
            drawerHeaderTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            drawerHeaderTableLayoutPanel.Size = new Size(777, 44);
            drawerHeaderTableLayoutPanel.TabIndex = 0;
            drawerHeaderTableLayoutPanel.TagString = null;
            // 
            // drawerButtonTableLayoutPanel
            // 
            drawerButtonTableLayoutPanel.ColumnCount = 2;
            drawerButtonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            drawerButtonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            drawerButtonTableLayoutPanel.Controls.Add(closeDrawerButton, 1, 0);
            drawerButtonTableLayoutPanel.Controls.Add(refreshDrawerButton, 0, 0);
            drawerButtonTableLayoutPanel.Dock = DockStyle.Fill;
            drawerButtonTableLayoutPanel.Location = new Point(546, 3);
            drawerButtonTableLayoutPanel.Name = "drawerButtonTableLayoutPanel";
            drawerButtonTableLayoutPanel.RowCount = 1;
            drawerButtonTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            drawerButtonTableLayoutPanel.Size = new Size(228, 38);
            drawerButtonTableLayoutPanel.TabIndex = 1;
            drawerButtonTableLayoutPanel.TagString = null;
            // 
            // closeDrawerButton
            // 
            closeDrawerButton.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            closeDrawerButton.Location = new Point(117, 3);
            closeDrawerButton.MinimumSize = new Size(1, 1);
            closeDrawerButton.Name = "closeDrawerButton";
            closeDrawerButton.Radius = 32;
            closeDrawerButton.Size = new Size(85, 32);
            closeDrawerButton.TabIndex = 0;
            closeDrawerButton.Text = "close";
            closeDrawerButton.TipsFont = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            // 
            // refreshDrawerButton
            // 
            refreshDrawerButton.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            refreshDrawerButton.Location = new Point(3, 3);
            refreshDrawerButton.MinimumSize = new Size(1, 1);
            refreshDrawerButton.Name = "refreshDrawerButton";
            refreshDrawerButton.Radius = 32;
            refreshDrawerButton.Size = new Size(85, 32);
            refreshDrawerButton.TabIndex = 0;
            refreshDrawerButton.Text = "refresh";
            refreshDrawerButton.TipsFont = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            // 
            // fullPathLabel
            // 
            fullPathLabel.Dock = DockStyle.Fill;
            fullPathLabel.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            fullPathLabel.ForeColor = Color.FromArgb(224, 224, 224);
            fullPathLabel.Location = new Point(3, 0);
            fullPathLabel.Name = "fullPathLabel";
            fullPathLabel.Size = new Size(537, 44);
            fullPathLabel.TabIndex = 2;
            fullPathLabel.Text = "folderFullPath";
            fullPathLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // drawerContentFlowLayoutPanel
            // 
            drawerContentFlowLayoutPanel.Dock = DockStyle.Fill;
            drawerContentFlowLayoutPanel.Font = new Font("宋体", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            drawerContentFlowLayoutPanel.Location = new Point(4, 55);
            drawerContentFlowLayoutPanel.Margin = new Padding(4, 5, 4, 5);
            drawerContentFlowLayoutPanel.MinimumSize = new Size(1, 1);
            drawerContentFlowLayoutPanel.Name = "drawerContentFlowLayoutPanel";
            drawerContentFlowLayoutPanel.Padding = new Padding(2);
            drawerContentFlowLayoutPanel.ShowText = false;
            drawerContentFlowLayoutPanel.Size = new Size(775, 523);
            drawerContentFlowLayoutPanel.TabIndex = 1;
            drawerContentFlowLayoutPanel.Text = "uiFlowLayoutPanel1";
            drawerContentFlowLayoutPanel.TextAlignment = ContentAlignment.MiddleCenter;
            // 
            // DrawerWindow
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.DimGray;
            ClientSize = new Size(783, 583);
            Controls.Add(mainTableLayoutPanel);
            Name = "DrawerWindow";
            mainTableLayoutPanel.ResumeLayout(false);
            drawerHeaderTableLayoutPanel.ResumeLayout(false);
            drawerButtonTableLayoutPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Sunny.UI.UITableLayoutPanel mainTableLayoutPanel;
        private UITableLayoutPanel drawerHeaderTableLayoutPanel;
        private UIButton closeDrawerButton;
        private UITableLayoutPanel drawerButtonTableLayoutPanel;
        private UIButton refreshDrawerButton;
        private UILabel fullPathLabel;
        private UIFlowLayoutPanel drawerContentFlowLayoutPanel;
    }
}