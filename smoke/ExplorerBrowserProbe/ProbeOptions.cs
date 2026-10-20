using ExplorerBrowserProbe.Interop;

namespace ExplorerBrowserProbe;

/// <summary>
/// 命令行参数。所有开关都有默认值，直接运行就能看到默认的“无框架文件夹视图”。
/// </summary>
internal sealed class ProbeOptions
{
    /// <summary>用法说明；同时写进日志，方便事后对照。（WinExe 没有控制台，所以用消息框显示。）</summary>
    public const string Usage =
        "用法: ExplorerBrowserProbe [目录] [开关]\r\n"
        + "\r\n"
        + "  目录                 要显示的文件夹；默认是当前用户的桌面。\r\n"
        + "\r\n"
        + "  --frames             打开 EBO_SHOWFRAMES（显示命令栏/导航窗格/详细信息窗格）。默认关闭，即无框架视图。\r\n"
        + "  --no-wrapper         加 EBO_NOWRAPPERWINDOW：不创建包装窗口。\r\n"
        + "  --no-border          加 EBO_NOBORDER：不给浏览器窗口画边框。\r\n"
        + "  --navigate-once      加 EBO_NAVIGATEONCE：不再允许继续导航。\r\n"
        + "  --always-navigate    加 EBO_ALWAYSNAVIGATE。\r\n"
        + "  --view=<mode>        icon|smallicon|list|details|thumbnail|tile|content|auto，默认 icon。\r\n"
        + "  --hide-header        加 FWF_NOCOLUMNHEADER|FWF_NOHEADERINALLVIEWS。\r\n"
        + "  --transparent        加 FWF_TRANSPARENT：视图背景使用父窗口背景。\r\n"
        + "  --pane-probe         实验：把本烟测的 IExplorerPaneVisibility 装到视图站点上，观察外壳是否来查询窗格。\r\n"
        + "  --opacity=<0-100>    窗口整体不透明度，默认 100；用来观察外壳视图在分层窗口里的表现。\r\n"
        + "  --dump               导航完成后把窗口结构诊断写进日志。\r\n"
        + "  --auto-close=<秒>    打开后自动关窗（用于验证销毁路径，退出码应为 0）。\r\n"
        + "  --help               显示本说明。\r\n";

    public required string Directory { get; init; }

    public ExplorerBrowserOptions BrowserOptions { get; init; } = ExplorerBrowserOptions.None;

    public uint ViewMode { get; init; } = FolderViewModes.Icon;

    public FolderFlags ExtraFlags { get; init; } = FolderFlags.None;

    public bool PaneProbe { get; init; }

    public int Opacity { get; init; } = 100;

    public bool DumpOnNavigation { get; init; }

    public int AutoCloseSeconds { get; init; }

    public bool ShowHelp { get; init; }

    public static ProbeOptions Parse(string[] args)
    {
        var directory = string.Empty;
        var options = ExplorerBrowserOptions.NoPersistViewState;
        var viewMode = FolderViewModes.Icon;
        var extraFlags = FolderFlags.None;
        var paneProbe = false;
        var opacity = 100;
        var dump = false;
        var autoCloseSeconds = 0;
        var help = false;

        foreach (var raw in args)
        {
            var argument = raw.Trim();
            if (argument.Length == 0)
            {
                continue;
            }

            if (!argument.StartsWith('-'))
            {
                directory = argument;
                continue;
            }

            var separator = argument.IndexOf('=');
            var name = (separator < 0 ? argument : argument[..separator]).TrimStart('-').ToLowerInvariant();
            var value = separator < 0 ? string.Empty : argument[(separator + 1)..];

            switch (name)
            {
                case "frames":
                    options |= ExplorerBrowserOptions.ShowFrames;
                    break;
                case "no-wrapper":
                    options |= ExplorerBrowserOptions.NoWrapperWindow;
                    break;
                case "no-border":
                    options |= ExplorerBrowserOptions.NoBorder;
                    break;
                case "navigate-once":
                    options |= ExplorerBrowserOptions.NavigateOnce;
                    break;
                case "always-navigate":
                    options |= ExplorerBrowserOptions.AlwaysNavigate;
                    break;
                case "hide-header":
                    extraFlags |= FolderFlags.NoColumnHeader | FolderFlags.NoHeaderInAllViews;
                    break;
                case "transparent":
                    extraFlags |= FolderFlags.Transparent;
                    break;
                case "pane-probe":
                    paneProbe = true;
                    break;
                case "dump":
                    dump = true;
                    break;
                case "auto-close":
                    if (!int.TryParse(value, out autoCloseSeconds) || autoCloseSeconds < 0)
                    {
                        ProbeLog.Write($"无法识别的 --auto-close 值 '{value}'，改用 0。");
                        autoCloseSeconds = 0;
                    }
                    break;
                case "help":
                case "h":
                case "?":
                    help = true;
                    break;
                case "view":
                    if (!FolderViewModes.TryParse(value, out viewMode))
                    {
                        ProbeLog.Write($"无法识别的 --view 值 '{value}'，改用 icon。");
                    }
                    break;
                case "opacity":
                    if (!int.TryParse(value, out opacity) || opacity is < 0 or > 100)
                    {
                        ProbeLog.Write($"无法识别的 --opacity 值 '{value}'，改用 100。");
                        opacity = 100;
                    }
                    break;
                default:
                    ProbeLog.Write($"忽略无法识别的参数 '{argument}'。");
                    break;
            }
        }

        if (directory.Length == 0)
        {
            directory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (directory.Length == 0)
            {
                directory = Environment.CurrentDirectory;
            }
        }

        return new ProbeOptions
        {
            Directory = directory,
            BrowserOptions = options,
            ViewMode = viewMode,
            ExtraFlags = extraFlags,
            PaneProbe = paneProbe,
            Opacity = opacity,
            DumpOnNavigation = dump,
            AutoCloseSeconds = autoCloseSeconds,
            ShowHelp = help,
        };
    }
}
