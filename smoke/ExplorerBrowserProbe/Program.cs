using ExplorerBrowserProbe.Interop;

namespace ExplorerBrowserProbe;

/// <summary>
/// 入口点。解析参数、初始化 OLE，然后打开烟测窗口。
/// </summary>
internal static class Program
{
    private static int _errorDialogShown;

    [STAThread]
    private static void Main(string[] args)
    {
        // 参数解析在初始化前完成，这样即使初始化失败也能把参数记进日志。
        var options = ProbeOptions.Parse(args);

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportUnexpected(e.Exception);

        // 外壳视图自己就是拖放目标（拖入），拖出也依赖 OLE 数据传输。
        // 交互式 STA 线程在消息循环里通常已经初始化过 OLE，这里显式调用以免拖放悄悄失效。
        try
        {
            Application.OleRequired();
        }
        catch (Exception ex)
        {
            ProbeLog.Write($"Application.OleRequired 失败，拖放可能不可用: {ex.Message}");
        }

        if (options.Dark)
        {
            ApplyDarkModeExperiment();
        }

        ProbeLog.Write("=== ExplorerBrowser 烟测启动（参数说明: --help） ===");
        ProbeLog.Write($"目录        : {options.Directory}");
        ProbeLog.Write($"浏览器选项  : {ShellText.Describe(options.BrowserOptions)}");
        ProbeLog.Write($"视图模式    : {ShellText.DescribeViewMode(options.ViewMode)}");
        ProbeLog.Write($"文件夹标志  : {ShellText.Describe(options.ExtraFlags)}");
        ProbeLog.Write($"宿主背景色  : {(options.Background is { } background ? $"#{background.R:X2}{background.G:X2}{background.B:X2}" : "默认白")}");
        ProbeLog.Write($"文件夹打开  : {ExplorerBrowserHost.DescribeFolderOpenMode(options.FolderOpen)}");
        ProbeLog.Write($"窗格探针    : {(options.PaneProbe ? "开" : "关")}");
        ProbeLog.Write($"自检/菜单/像素 : {(options.SelfTest ? "开" : "关")}/{(options.DumpMenu ? "开" : "关")}/{(options.SamplePixels ? "开" : "关")}");
        ProbeLog.Write($"深色实验    : {(options.Dark ? "开" : "关")}");
        ProbeLog.Write($"不透明度    : {options.Opacity}%");

        if (options.ShowHelp)
        {
            ProbeLog.WriteBlock(ProbeOptions.Usage);
            MessageBox.Show(ProbeOptions.Usage, "ExplorerBrowserProbe", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!Directory.Exists(options.Directory))
        {
            ProbeLog.Write($"目录不存在: {options.Directory}");
            MessageBox.Show(
                $"目录不存在: {options.Directory}{Environment.NewLine}{Environment.NewLine}窗口仍会打开，用来观察外壳在这种情况下给出什么界面和 HRESULT。",
                "ExplorerBrowserProbe",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        try
        {
            Application.Run(new ProbeForm(options));
        }
        catch (Exception ex)
        {
            ReportUnexpected(ex);
        }

        ProbeLog.Write($"=== 烟测结束，日志: {ProbeLog.Path} ===");
    }

    /// <summary>
    /// 实验：让外壳控件走深色渲染。uxtheme 的 SetPreferredAppMode 是未公开导出
    /// （不同 Windows 版本的序号可能不同），所以只做“能调就调、失败只记日志”，
    /// 并且必须在创建任何窗口之前调用。
    /// </summary>
    private static void ApplyDarkModeExperiment()
    {
        try
        {
            var previous = NativeMethods.SetPreferredAppMode(2); // ForceDark
            ProbeLog.Write($"实验：SetPreferredAppMode(ForceDark) -> 之前的模式={previous}（0=Default，1=AllowDark，2=ForceDark，3=ForceLight）");
        }
        catch (Exception ex)
        {
            ProbeLog.Write($"实验：SetPreferredAppMode 不可用（{ex.GetType().Name}: {ex.Message}）");
        }
    }

    private static void ReportUnexpected(Exception exception)
    {
        ProbeLog.Write($"未处理异常: {exception}");

        if (Interlocked.Exchange(ref _errorDialogShown, 1) != 0)
        {
            return;
        }

        try
        {
            MessageBox.Show(
                $"烟测遇到未处理异常，详细信息见:{Environment.NewLine}{ProbeLog.Path}{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "ExplorerBrowserProbe",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception dialogFailure)
        {
            ProbeLog.Write($"显示异常对话框失败: {dialogFailure.Message}");
        }
    }
}
