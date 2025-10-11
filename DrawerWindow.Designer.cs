using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;

namespace winDrawer
{
    partial class DrawerWindow
    {
        private IContainer components = null!;

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
            components = new Container();
            mainTableLayoutPanel = new UITableLayoutPanel();
            drawerContentsLayout = new UIFlowLayoutPanel();
            drawerImageList = new ImageList(components);
            drawerHeaderTableLayoutPanel = new UITableLayoutPanel();
            fullPathLabel = new UILabel();
            drawerButtonTableLayoutPanel = new UITableLayoutPanel();
            refreshDrawerButton = new UIButton();
            closeDrawerButton = new UIButton();
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
            mainTableLayoutPanel.Controls.Add(drawerContentsLayout, 0, 0);
            mainTableLayoutPanel.Controls.Add(drawerHeaderTableLayoutPanel, 0, 1);
            mainTableLayoutPanel.Dock = DockStyle.Fill;
            mainTableLayoutPanel.Location = new Point(0, 0);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 2;
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            mainTableLayoutPanel.Size = new Size(762, 583);
            mainTableLayoutPanel.TabIndex = 0;
            mainTableLayoutPanel.TagString = null;
            // 
            // drawerContentsListView
            // 
            drawerContentsLayout.BackColor = Color.FromArgb(56, 56, 56);
            drawerContentsLayout.Dock = DockStyle.Fill;
            drawerContentsLayout.ForeColor = Color.WhiteSmoke;
            drawerContentsLayout.Location = new Point(0, 0);
            drawerContentsLayout.Margin = new Padding(0);
            drawerContentsLayout.Name = "drawerContentsListView";
            drawerContentsLayout.Size = new Size(762, 533);
            drawerContentsLayout.TabIndex = 0;
            // 
            // drawerImageList
            // 
            drawerImageList.ColorDepth = ColorDepth.Depth32Bit;
            drawerImageList.ImageSize = new Size(96, 96);
            drawerImageList.TransparentColor = Color.Transparent;
            // 
            // drawerHeaderTableLayoutPanel
            // 
            drawerHeaderTableLayoutPanel.ColumnCount = 2;
            drawerHeaderTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            drawerHeaderTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            drawerHeaderTableLayoutPanel.Controls.Add(fullPathLabel, 0, 0);
            drawerHeaderTableLayoutPanel.Controls.Add(drawerButtonTableLayoutPanel, 1, 0);
            drawerHeaderTableLayoutPanel.Dock = DockStyle.Fill;
            drawerHeaderTableLayoutPanel.Location = new Point(3, 536);
            drawerHeaderTableLayoutPanel.Name = "drawerHeaderTableLayoutPanel";
            drawerHeaderTableLayoutPanel.RowCount = 1;
            drawerHeaderTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            drawerHeaderTableLayoutPanel.Size = new Size(756, 44);
            drawerHeaderTableLayoutPanel.TabIndex = 0;
            drawerHeaderTableLayoutPanel.TagString = null;
            // 
            // fullPathLabel
            // 
            fullPathLabel.Dock = DockStyle.Fill;
            fullPathLabel.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            fullPathLabel.ForeColor = Color.FromArgb(224, 224, 224);
            fullPathLabel.Location = new Point(3, 0);
            fullPathLabel.Name = "fullPathLabel";
            fullPathLabel.Padding = new Padding(6, 0, 6, 0);
            fullPathLabel.Size = new Size(485, 44);
            fullPathLabel.TabIndex = 0;
            fullPathLabel.Text = "folderFullPath";
            fullPathLabel.TextAlign = ContentAlignment.MiddleLeft;
            fullPathLabel.Click += fullPathLabel_Click;
            // 
            // drawerButtonTableLayoutPanel
            // 
            drawerButtonTableLayoutPanel.ColumnCount = 3;
            drawerButtonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            drawerButtonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            drawerButtonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            drawerButtonTableLayoutPanel.Controls.Add(refreshDrawerButton, 0, 0);
            drawerButtonTableLayoutPanel.Controls.Add(closeDrawerButton, 1, 0);
            drawerButtonTableLayoutPanel.Dock = DockStyle.Fill;
            drawerButtonTableLayoutPanel.Location = new Point(491, 0);
            drawerButtonTableLayoutPanel.Margin = new Padding(0);
            drawerButtonTableLayoutPanel.Name = "drawerButtonTableLayoutPanel";
            drawerButtonTableLayoutPanel.RowCount = 1;
            drawerButtonTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            drawerButtonTableLayoutPanel.Size = new Size(265, 44);
            drawerButtonTableLayoutPanel.TabIndex = 1;
            drawerButtonTableLayoutPanel.TagString = null;
            // 
            // refreshDrawerButton
            // 
            refreshDrawerButton.Cursor = Cursors.Hand;
            refreshDrawerButton.Dock = DockStyle.Left;
            refreshDrawerButton.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            refreshDrawerButton.Location = new Point(3, 3);
            refreshDrawerButton.MinimumSize = new Size(1, 1);
            refreshDrawerButton.Name = "refreshDrawerButton";
            refreshDrawerButton.Radius = 2;
            refreshDrawerButton.Size = new Size(100, 38);
            refreshDrawerButton.TabIndex = 0;
            refreshDrawerButton.Text = "refresh";
            refreshDrawerButton.TipsFont = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            // 
            // closeDrawerButton
            // 
            closeDrawerButton.Cursor = Cursors.Hand;
            closeDrawerButton.Dock = DockStyle.Right;
            closeDrawerButton.Font = new Font("Lobster", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            closeDrawerButton.Location = new Point(131, 3);
            closeDrawerButton.MinimumSize = new Size(1, 1);
            closeDrawerButton.Name = "closeDrawerButton";
            closeDrawerButton.Radius = 2;
            closeDrawerButton.Size = new Size(100, 38);
            closeDrawerButton.TabIndex = 1;
            closeDrawerButton.Text = "close";
            closeDrawerButton.TipsFont = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            // 
            // DrawerWindow
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.DimGray;
            ClientSize = new Size(762, 583);
            ControlBox = false;
            Controls.Add(mainTableLayoutPanel);
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(530, 0);
            Name = "DrawerWindow";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Drawer";
            mainTableLayoutPanel.ResumeLayout(false);
            drawerHeaderTableLayoutPanel.ResumeLayout(false);
            drawerButtonTableLayoutPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private UITableLayoutPanel mainTableLayoutPanel;
        private UITableLayoutPanel drawerHeaderTableLayoutPanel;
        private UILabel fullPathLabel;
        private UITableLayoutPanel drawerButtonTableLayoutPanel;
        private UIButton refreshDrawerButton;
        private UIButton closeDrawerButton;
        private UIFlowLayoutPanel drawerContentsLayout;
        private ImageList drawerImageList;
    }
}
