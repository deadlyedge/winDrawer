using ExplorerBrowserProbe.Interop;

namespace ExplorerBrowserProbe;

/// <summary>
/// 烟测窗口：无标题栏的窗体 + 一条工具条 + 一个外壳文件夹视图。
/// 工具条属于本烟测自己，不属于外壳界面，所以它不会干扰“外围界面是否被隐藏”的判断。
/// </summary>
internal sealed class ProbeForm : Form
{
    private const int WmNcHitTest = 0x0084;
    private const int WmNclButtonDown = 0x00A1;
    private const int HtCaption = 2;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;

    private static readonly uint[] ViewModeCycle =
    {
        FolderViewModes.Icon,
        FolderViewModes.SmallIcon,
        FolderViewModes.List,
        FolderViewModes.Details,
        FolderViewModes.Tile,
    };

    private readonly ExplorerBrowserHost _host;
    private readonly TableLayoutPanel _root;
    private readonly Label _statusLabel;
    private readonly float _baseOpacity;
    private readonly System.Windows.Forms.Timer? _autoCloseTimer;

    public ProbeForm(ProbeOptions options)
    {
        _baseOpacity = Math.Clamp(options.Opacity, 0, 100) / 100f;

        Text = $"ExplorerBrowser probe — {ProbeLog.Path}";
        FormBorderStyle = FormBorderStyle.Sizable;
        ControlBox = false;
        ShowIcon = false;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(32, 32, 34);
        ClientSize = new Size(960, 620);
        MinimumSize = new Size(420, 280);
        Opacity = _baseOpacity;
        KeyPreview = true;

        _host = new ExplorerBrowserHost(options.BrowserOptions, options.ViewMode, options.ExtraFlags, options.PaneProbe)
        {
            Dock = DockStyle.Fill,
        };
        _host.EnableDiagnosticDump(options.DumpOnNavigation);
        _host.StatusChanged += OnHostStatusChanged;

        _statusLabel = new Label
        {
            AutoEllipsis = true,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.Gainsboro,
            BackColor = Color.Transparent,
            Margin = new Padding(6, 0, 4, 0),
            Text = "正在创建外壳浏览器…",
        };
        _statusLabel.MouseDown += BeginWindowDrag;

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        buttons.Controls.Add(CreateButton("Frames", ToggleFrames));
        buttons.Controls.Add(CreateButton("View mode", CycleViewMode));
        buttons.Controls.Add(CreateButton("Reload", () => { _host.Reload(); _host.FocusShellView(); }));
        buttons.Controls.Add(CreateButton("Opacity", ToggleOpacity));
        buttons.Controls.Add(CreateButton("Diagnostics", () => _host.DumpDiagnostics()));
        buttons.Controls.Add(CreateButton("Close", Close));

        _root = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.FromArgb(45, 45, 48),
            Padding = new Padding(6, 6, 6, 6),
        };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        _root.Controls.Add(_statusLabel, 0, 0);
        _root.Controls.Add(buttons, 1, 0);
        _root.Controls.Add(_host, 0, 1);
        _root.SetColumnSpan(_host, 2);

        // 窗体自己的客户区只留一圈窄边给缩放命中区，内容整体内缩，避免子窗口吃掉边框命中。
        var inset = 6;
        _root.Location = new Point(inset, inset);
        _root.Size = new Size(ClientSize.Width - inset * 2, ClientSize.Height - inset * 2);
        _root.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        _root.MouseDown += BeginWindowDrag;

        Controls.Add(_root);

        ProbeLog.Write($"窗口就绪: 目录={options.Directory}, 选项={ShellText.Describe(options.BrowserOptions)}, 视图={ShellText.DescribeViewMode(options.ViewMode)}, 额外标志={ShellText.Describe(options.ExtraFlags)}, 不透明度={options.Opacity}%");
        Report($"目录: {options.Directory}");

        // 句柄还没创建；这里只是把要显示的目录登记给宿主控件。
        _host.Navigate(options.Directory);

        if (options.AutoCloseSeconds > 0)
        {
            // 用于自动验证关闭路径：Unadvise/Destroy 是否被调用、进程是否干净退出。
            _autoCloseTimer = new System.Windows.Forms.Timer { Interval = options.AutoCloseSeconds * 1000 };
            _autoCloseTimer.Tick += (_, _) =>
            {
                _autoCloseTimer.Stop();
                Close();
            };
            _autoCloseTimer.Start();
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            // 与 winDrawer 的抽屉窗口一致：去掉标题栏但保留 WS_THICKFRAME，让窗口仍然可以缩放。
            const int wsCaption = 0x00C00000;
            var parameters = base.CreateParams;
            parameters.Style &= ~wsCaption;
            return parameters;
        }
    }

    private static Button CreateButton(string text, Action action)
    {
        var button = new Button
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(4, 0, 0, 0),
            Padding = new Padding(8, 2, 8, 2),
            Text = text,
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void Report(string message)
    {
        ProbeLog.Write(message);
        _statusLabel.Text = message;
    }

    private void OnHostStatusChanged(string message) => _statusLabel.Text = message;

    private void ToggleFrames()
    {
        // 切换选项必须重建浏览器，所以先把键盘焦点交还视图，避免焦点停在已销毁的子窗口上。
        _host.SetFrames(!_host.Options.HasFlag(ExplorerBrowserOptions.ShowFrames));
        _host.FocusShellView();
    }

    private void CycleViewMode()
    {
        var current = Array.IndexOf(ViewModeCycle, _host.ViewMode);
        var next = ViewModeCycle[(current + 1) % ViewModeCycle.Length];
        _host.SetViewMode(next);
        _host.FocusShellView();
    }

    private void ToggleOpacity()
    {
        Opacity = Math.Abs(Opacity - _baseOpacity) < 0.001
            ? Math.Max(0.2f, _baseOpacity - 0.2f)
            : _baseOpacity;
        Report($"窗口不透明度 -> {(int)Math.Round(Opacity * 100)}%（观察外壳视图在分层窗口里的表现）");
    }

    /// <summary>拖动工具条/状态栏文字移动窗口：无标题栏时唯一的移动方式。</summary>
    private void BeginWindowDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(Handle, WmNclButtonDown, HtCaption, IntPtr.Zero);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // 注意：焦点在外壳视图里时按键由外壳处理，这些快捷键只在工具条有焦点时生效。
        switch (e.KeyCode)
        {
            case Keys.Escape:
                Close();
                e.Handled = true;
                break;
            case Keys.F5:
                _host.Reload();
                _host.FocusShellView();
                e.Handled = true;
                break;
            case Keys.F7:
                ToggleFrames();
                e.Handled = true;
                break;
            case Keys.F8:
                CycleViewMode();
                e.Handled = true;
                break;
            case Keys.F9:
                _host.DumpDiagnostics();
                e.Handled = true;
                break;
            default:
                base.OnKeyDown(e);
                break;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        _autoCloseTimer?.Dispose();
        ProbeLog.Write($"窗口已关闭（{e.CloseReason}）。");
    }

    protected override void WndProc(ref Message m)
    {
        // 无标题栏时系统仍会给 WS_THICKFRAME 的窗口留出边框缩放区；
        // 这里把内缩后暴露出来的窄边也当成边框，缩放更好点中。
        if (m.Msg == WmNcHitTest)
        {
            var hit = HitTestResizeBand(m);
            if (hit != 0)
            {
                m.Result = hit;
                return;
            }
        }

        base.WndProc(ref m);
    }

    /// <summary>返回 0 表示交给系统默认的命中测试处理。</summary>
    private int HitTestResizeBand(Message m)
    {
        var x = unchecked((short)(long)m.LParam);
        var y = unchecked((short)((long)m.LParam >> 16));
        var point = PointToClient(new Point(x, y));

        // 命中带就用内容实际内缩的距离，避免和已经过 DPI 缩放的布局对不上。
        var band = Math.Max(4, _root.Left);
        var onLeft = point.X < band;
        var onRight = point.X >= ClientSize.Width - band;
        var onTop = point.Y < band;
        var onBottom = point.Y >= ClientSize.Height - band;

        if (onTop && onLeft) return HtTopLeft;
        if (onTop && onRight) return HtTopRight;
        if (onBottom && onLeft) return HtBottomLeft;
        if (onBottom && onRight) return HtBottomRight;
        if (onLeft) return HtLeft;
        if (onRight) return HtRight;
        if (onTop) return HtTop;
        if (onBottom) return HtBottom;
        return 0;
    }
}
