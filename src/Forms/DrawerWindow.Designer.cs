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
                drawerContentsListView.Items.Clear();
                drawerContentsListView.LargeImageList = null;
                drawerImageList.Images.Clear();
                DisposeFileListImages();
                DisposeDrawerContextMenu();
                openFolderIconPictureBox.Image?.Dispose();
                openFolderIconPictureBox.Image = null;
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new Container();
            drawerToolTip = new ToolTip(components);
            mainTableLayoutPanel = new UITableLayoutPanel();
            drawerContentsHostPanel = new Panel();
            drawerContentsListView = new ScrollableListView();
            drawerContentsScrollBar = new UIScrollBar();
            drawerImageList = new ImageList(components);
            drawerHeaderTableLayoutPanel = new UITableLayoutPanel();
            drawerPathTableLayoutPanel = new TableLayoutPanel();
            openFolderIconPictureBox = new PictureBox();
            fullPathLabel = new UILabel();
            drawerButtonTableLayoutPanel = new FlowLayoutPanel();
            closeDrawerButton = new UIButton();
            mainTableLayoutPanel.SuspendLayout();
            drawerHeaderTableLayoutPanel.SuspendLayout();
            drawerPathTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)openFolderIconPictureBox).BeginInit();
            drawerButtonTableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            mainTableLayoutPanel.BackColor = Color.FromArgb(64, 64, 64);
            mainTableLayoutPanel.ColumnCount = 1;
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Margin = new Padding(0);
            mainTableLayoutPanel.Padding = new Padding(0);
            mainTableLayoutPanel.Controls.Add(drawerContentsHostPanel, 0, 0);
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
            // drawerContentsHostPanel
            //
            drawerContentsHostPanel.BackColor = Color.FromArgb(56, 56, 56);
            drawerContentsHostPanel.Controls.Add(drawerContentsListView);
            drawerContentsHostPanel.Controls.Add(drawerContentsScrollBar);
            drawerContentsHostPanel.Dock = DockStyle.Fill;
            drawerContentsHostPanel.Margin = new Padding(0);
            drawerContentsHostPanel.Location = new Point(0, 0);
            drawerContentsHostPanel.Name = "drawerContentsHostPanel";
            drawerContentsHostPanel.Size = new Size(762, 533);
            drawerContentsHostPanel.TabIndex = 0;
            // 
            // drawerContentsListView
            // 
            drawerContentsListView.BackColor = Color.FromArgb(56, 56, 56);
            drawerContentsListView.BorderStyle = BorderStyle.None;
            drawerContentsListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            drawerContentsListView.ForeColor = Color.WhiteSmoke;
            drawerContentsListView.HideSelection = false;
            drawerContentsListView.Location = new Point(0, 0);
            drawerContentsListView.Margin = new Padding(0);
            drawerContentsListView.MultiSelect = false;
            drawerContentsListView.Name = "drawerContentsListView";
            drawerContentsListView.ShowItemToolTips = true;
            drawerContentsListView.Size = new Size(762, 533);
            drawerContentsListView.TabIndex = 0;
            drawerContentsListView.UseCompatibleStateImageBehavior = false;
            drawerContentsListView.View = View.LargeIcon;
            // 
            // drawerImageList
            // 
            drawerImageList.ColorDepth = ColorDepth.Depth32Bit;
            drawerImageList.ImageSize = new Size(48, 48);
            drawerImageList.TransparentColor = Color.Transparent;
            // 
            // drawerHeaderTableLayoutPanel
            // 
            drawerHeaderTableLayoutPanel.ColumnCount = 2;
            drawerHeaderTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            drawerHeaderTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            drawerHeaderTableLayoutPanel.Controls.Add(drawerPathTableLayoutPanel, 0, 0);
            drawerHeaderTableLayoutPanel.Controls.Add(drawerButtonTableLayoutPanel, 1, 0);
            drawerHeaderTableLayoutPanel.Dock = DockStyle.Fill;
            drawerHeaderTableLayoutPanel.Location = new Point(3, 536);
            drawerHeaderTableLayoutPanel.Margin = new Padding(0);
            drawerHeaderTableLayoutPanel.Name = "drawerHeaderTableLayoutPanel";
            drawerHeaderTableLayoutPanel.RowCount = 1;
            drawerHeaderTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            drawerHeaderTableLayoutPanel.Size = new Size(756, 44);
            drawerHeaderTableLayoutPanel.TabIndex = 0;
            drawerHeaderTableLayoutPanel.TagString = null;
            // drawerPathTableLayoutPanel
            drawerPathTableLayoutPanel.ColumnCount = 2;
            drawerPathTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42F));
            drawerPathTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            drawerPathTableLayoutPanel.Controls.Add(openFolderIconPictureBox, 0, 0);
            drawerPathTableLayoutPanel.Controls.Add(fullPathLabel, 1, 0);
            drawerPathTableLayoutPanel.BackColor = Color.Transparent;
            drawerPathTableLayoutPanel.Dock = DockStyle.Fill;
            drawerPathTableLayoutPanel.Margin = new Padding(0);
            drawerPathTableLayoutPanel.Name = "drawerPathTableLayoutPanel";
            drawerPathTableLayoutPanel.RowCount = 1;
            drawerPathTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            drawerPathTableLayoutPanel.Size = new Size(485, 44);
            drawerPathTableLayoutPanel.TabIndex = 0;
            drawerPathTableLayoutPanel.Cursor = Cursors.Hand;
            drawerPathTableLayoutPanel.AccessibleName = "Open drawer folder in File Explorer";
            drawerPathTableLayoutPanel.AccessibleDescription = "Click to open this folder in File Explorer.";
            drawerToolTip.SetToolTip(drawerPathTableLayoutPanel, "Click to open folder in File Explorer");
            // openFolderIconPictureBox
            openFolderIconPictureBox.AccessibleName = "Open folder in File Explorer";
            openFolderIconPictureBox.AccessibleDescription = "Opens this drawer's folder in File Explorer.";
            openFolderIconPictureBox.Cursor = Cursors.Hand;
            openFolderIconPictureBox.Dock = DockStyle.Fill;
            openFolderIconPictureBox.Margin = new Padding(0);
            openFolderIconPictureBox.Name = "openFolderIconPictureBox";
            openFolderIconPictureBox.Size = new Size(42, 44);
            openFolderIconPictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
            openFolderIconPictureBox.TabIndex = 0;
            openFolderIconPictureBox.TabStop = false;
            drawerToolTip.SetToolTip(openFolderIconPictureBox, "Click to open folder in File Explorer");
            // fullPathLabel
            fullPathLabel.Dock = DockStyle.Fill;
            fullPathLabel.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            fullPathLabel.ForeColor = Color.FromArgb(224, 224, 224);
            fullPathLabel.Location = new Point(45, 0);
            fullPathLabel.Name = "fullPathLabel";
            fullPathLabel.Padding = new Padding(6, 0, 6, 0);
            fullPathLabel.Size = new Size(444, 44);
            fullPathLabel.TabIndex = 1;
            fullPathLabel.Text = "folderFullPath";
            fullPathLabel.TextAlign = ContentAlignment.MiddleLeft;
            fullPathLabel.AutoEllipsis = true;
            fullPathLabel.AccessibleDescription = "Path of the drawer folder.";
            fullPathLabel.AccessibleName = "Open drawer folder in File Explorer";
            fullPathLabel.Cursor = Cursors.Hand;
            drawerToolTip.SetToolTip(fullPathLabel, "Click to open folder in File Explorer");
            //
            // drawerButtonTableLayoutPanel
            // 
            drawerButtonTableLayoutPanel.Controls.Add(closeDrawerButton);
            drawerButtonTableLayoutPanel.Dock = DockStyle.Fill;
            drawerButtonTableLayoutPanel.Location = new Point(491, 0);
            drawerButtonTableLayoutPanel.Margin = new Padding(0);
            drawerButtonTableLayoutPanel.Name = "drawerButtonTableLayoutPanel";
            drawerButtonTableLayoutPanel.Size = new Size(265, 44);
            drawerButtonTableLayoutPanel.TabIndex = 1;
            drawerButtonTableLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
            drawerButtonTableLayoutPanel.WrapContents = false;
            drawerButtonTableLayoutPanel.Padding = new Padding(0, 3, 3, 3);
            // 
            // closeDrawerButton
            // 
            closeDrawerButton.Cursor = Cursors.Hand;
            closeDrawerButton.Font = new Font("Segoe MDL2 Assets", 14F, FontStyle.Regular, GraphicsUnit.Point, 0);
            closeDrawerButton.Location = new Point(176, 3);
            closeDrawerButton.MinimumSize = new Size(1, 1);
            closeDrawerButton.Name = "closeDrawerButton";
            closeDrawerButton.Radius = 2;
            closeDrawerButton.Size = new Size(40, 38);
            closeDrawerButton.TabIndex = 1;
            closeDrawerButton.Text = "\uE8BB";
            closeDrawerButton.AccessibleName = "Close drawer";
            closeDrawerButton.AccessibleDescription = "Closes this drawer window.";
            closeDrawerButton.TipsFont = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            drawerToolTip.SetToolTip(closeDrawerButton, "Close drawer");
            // 
            // DrawerWindow
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.DimGray;
            ClientSize = new Size(762, 583);
            ControlBox = false;
            Controls.Add(mainTableLayoutPanel);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(530, 320);
            Name = "DrawerWindow";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Drawer";
            mainTableLayoutPanel.ResumeLayout(false);
            drawerHeaderTableLayoutPanel.ResumeLayout(false);
            drawerPathTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)openFolderIconPictureBox).EndInit();
            drawerButtonTableLayoutPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private UITableLayoutPanel mainTableLayoutPanel;
        private Panel drawerContentsHostPanel;
        private UITableLayoutPanel drawerHeaderTableLayoutPanel;
        private TableLayoutPanel drawerPathTableLayoutPanel;
        private PictureBox openFolderIconPictureBox;
        private UILabel fullPathLabel;
        private FlowLayoutPanel drawerButtonTableLayoutPanel;
        private UIButton closeDrawerButton;
        private ScrollableListView drawerContentsListView;
        private UIScrollBar drawerContentsScrollBar;
        private ImageList drawerImageList;
        private ToolTip drawerToolTip;
    }
}
