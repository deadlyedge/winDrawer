using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;

namespace winDrawer
{
    partial class DrawerWindow
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
            components = new Container();
            mainTableLayoutPanel = new UITableLayoutPanel();
            fullPathLabel = new UILabel();
            drawerContentsListView = new ListView();
            drawerImageList = new ImageList(components);
            mainTableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            mainTableLayoutPanel.BackColor = Color.FromArgb(64, 64, 64);
            mainTableLayoutPanel.ColumnCount = 1;
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Controls.Add(fullPathLabel, 0, 0);
            mainTableLayoutPanel.Controls.Add(drawerContentsListView, 0, 1);
            mainTableLayoutPanel.Dock = DockStyle.Fill;
            mainTableLayoutPanel.Location = new Point(0, 0);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 2;
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Size = new Size(783, 583);
            mainTableLayoutPanel.TabIndex = 0;
            mainTableLayoutPanel.TagString = null;
            // 
            // fullPathLabel
            // 
            fullPathLabel.Dock = DockStyle.Fill;
            fullPathLabel.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point);
            fullPathLabel.ForeColor = Color.FromArgb(224, 224, 224);
            fullPathLabel.Location = new Point(3, 0);
            fullPathLabel.Name = "fullPathLabel";
            fullPathLabel.Padding = new Padding(6, 0, 6, 0);
            fullPathLabel.Size = new Size(777, 42);
            fullPathLabel.TabIndex = 0;
            fullPathLabel.Text = "folderFullPath";
            fullPathLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // drawerContentsListView
            // 
            drawerContentsListView.BackColor = Color.FromArgb(56, 56, 56);
            drawerContentsListView.BorderStyle = BorderStyle.None;
            drawerContentsListView.Dock = DockStyle.Fill;
            drawerContentsListView.ForeColor = Color.WhiteSmoke;
            drawerContentsListView.HideSelection = false;
            drawerContentsListView.Location = new Point(3, 45);
            drawerContentsListView.MultiSelect = false;
            drawerContentsListView.Name = "drawerContentsListView";
            drawerContentsListView.Size = new Size(777, 535);
            drawerContentsListView.TabIndex = 1;
            drawerContentsListView.UseCompatibleStateImageBehavior = false;
            drawerContentsListView.View = View.LargeIcon;
            drawerContentsListView.LargeImageList = drawerImageList;
            drawerContentsListView.SmallImageList = drawerImageList;
            // 
            // drawerImageList
            // 
            drawerImageList.ColorDepth = ColorDepth.Depth32Bit;
            drawerImageList.ImageSize = new Size(48, 48);
            drawerImageList.TransparentColor = Color.Transparent;
            // 
            // DrawerWindow
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.DimGray;
            ClientSize = new Size(783, 583);
            Controls.Add(mainTableLayoutPanel);
            FormBorderStyle = FormBorderStyle.Sizable;
            Name = "DrawerWindow";
            StartPosition = FormStartPosition.Manual;
            Text = "Drawer";
            mainTableLayoutPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private UITableLayoutPanel mainTableLayoutPanel;
        private UILabel fullPathLabel;
        private ListView drawerContentsListView;
        private ImageList drawerImageList;
    }
}
