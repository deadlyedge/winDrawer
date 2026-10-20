using System.Runtime.InteropServices;
using System.Text;
using ExplorerBrowserProbe.Interop;

// System.IServiceProvider 与 COM 接口同名，这里明确指向 COM 的那个。
using IServiceProvider = ExplorerBrowserProbe.Interop.IServiceProvider;

namespace ExplorerBrowserProbe;

/// <summary>
/// 把一个 CLSID_ExplorerBrowser 实例放进 WinForms 子控件里，作为“无框架文件夹视图”烟测的宿主。
/// 只做评估需要的最小职责：创建/销毁浏览器、指定目录、切换外壳选项和视图模式、报告导航结果与窗口结构。
/// 外壳视图是本控件的子窗口，因此控件本身不再画自己的内容。
/// </summary>
internal sealed class ExplorerBrowserHost : Control
{
    /// <summary>SVUIA_ACTIVATE_FOCUS：让视图拥有 UI 激活状态，F2 重命名依赖它。</summary>
    private const uint SvUiaActivateFocus = 2;

    /// <summary>窗口被整体重设前（如切换选项）用的状态前缀。</summary>
    private readonly FolderFlags _extraFlags;
    private readonly bool _paneProbeRequested;

    private IExplorerBrowser? _browser;
    private BrowserEventSink? _eventSink;
    private IShellView? _shellView;
    private IntPtr _shellViewWindow;
    private PaneVisibilityProvider? _paneProvider;
    private uint _adviseCookie;
    private bool _advised;
    private bool _paneProviderInstalled;
    private bool _dumpOnNavigation;
    private string? _pendingPath;
    private ExplorerBrowserOptions _options;
    private uint _viewMode;

    public ExplorerBrowserHost(ExplorerBrowserOptions options, uint viewMode, FolderFlags extraFlags, bool paneProbeRequested)
    {
        _options = options;
        _viewMode = viewMode;
        _extraFlags = extraFlags;
        _paneProbeRequested = paneProbeRequested;

        TabStop = false;
        SetStyle(ControlStyles.Selectable, false);
        BackColor = Color.White;
        MinimumSize = new Size(1, 1);
    }

    /// <summary>状态文本（同时写日志）。窗体工具条把它显示出来。</summary>
    public event Action<string>? StatusChanged;

    public ExplorerBrowserOptions Options => _options;

    public uint ViewMode => _viewMode;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            // 外壳视图是本控件的子窗口。没有 WS_CLIPCHILDREN 时，本控件的背景绘制会盖住外壳视图。
            parameters.Style |= NativeMethods.WsClipChildren;
            return parameters;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        CreateBrowser();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        DestroyBrowser();
        base.OnHandleDestroyed(e);
    }

    protected override void OnClientSizeChanged(EventArgs e)
    {
        base.OnClientSizeChanged(e);
        ApplyRect();
    }

    /// <summary>指定要显示的目录。句柄尚未创建时先记下来，等浏览器建立后再导航。</summary>
    public void Navigate(string path)
    {
        _pendingPath = path;
        if (_browser is not null)
        {
            NavigateCore(path);
        }
    }

    /// <summary>
    /// 重新创建一个 IExplorerBrowser。SetOptions 只对后续导航生效，切换 EBO_* 必须走这条路。
    /// 也是顺带验证“销毁 + 重建”生命周期是否可靠。
    /// </summary>
    public void Recreate()
    {
        DestroyBrowser();
        CreateBrowser();
    }

    public void SetFrames(bool enabled)
    {
        var updated = enabled
            ? _options | ExplorerBrowserOptions.ShowFrames
            : _options & ~ExplorerBrowserOptions.ShowFrames;

        if (updated == _options)
        {
            return;
        }

        _options = updated;
        ProbeLog.Write($"切换选项: {ShellText.Describe(_options)}");
        Report($"选项 -> {ShellText.Describe(_options)}");
        Recreate();
    }

    /// <summary>用 IExplorerBrowser::SetFolderSettings 请求另一种视图模式，并记录外壳实际接受的值。</summary>
    public void SetViewMode(uint viewMode)
    {
        _viewMode = viewMode;
        if (_browser is null)
        {
            return;
        }

        var settings = new FolderSettings { ViewMode = _viewMode, Flags = CurrentFlags() };
        var hr = _browser.SetFolderSettings(ref settings);
        ProbeLog.Write($"IExplorerBrowser.SetFolderSettings(viewMode={ShellText.DescribeViewMode(viewMode)}, flags={ShellText.Describe(settings.Flags)}) -> {HResult.Describe(hr)}");
        Report($"视图模式 -> {ShellText.DescribeViewMode(viewMode)}");

        if (_shellView is not null)
        {
            var refreshHr = _shellView.Refresh();
            ProbeLog.Write($"IShellView.Refresh() -> {HResult.Describe(refreshHr)}");
        }

        LogCurrentInfo("视图模式切换后");
    }

    /// <summary>重新读取当前目录：优先用 IShellView::Refresh，没有视图时重新导航。</summary>
    public void Reload()
    {
        if (_shellView is not null)
        {
            var hr = _shellView.Refresh();
            ProbeLog.Write($"IShellView.Refresh() -> {HResult.Describe(hr)}");
            Report($"刷新 -> {HResult.Describe(hr)}");
            return;
        }

        if (_pendingPath is not null)
        {
            NavigateCore(_pendingPath);
        }
    }

    /// <summary>把键盘焦点交回外壳视图。点过工具条按钮后需要它，否则 F2 到不了视图。</summary>
    public void FocusShellView()
    {
        if (_shellViewWindow != IntPtr.Zero)
        {
            NativeMethods.SetFocus(_shellViewWindow);
        }
    }

    private FolderFlags CurrentFlags() => _extraFlags | FolderFlags.NoBrowserViewState;

    private void CreateBrowser()
    {
        if (_browser is not null || !IsHandleCreated)
        {
            return;
        }

        try
        {
            var classId = ShellGuids.ExplorerBrowserClass;
            var interfaceId = ShellGuids.IExplorerBrowser;
            HResult.Check(
                NativeMethods.CoCreateInstance(ref classId, IntPtr.Zero, NativeMethods.ClsctxInProcServer, ref interfaceId, out var instance),
                "CoCreateInstance(CLSID_ExplorerBrowser)");
            _browser = (IExplorerBrowser)instance;

            var settings = new FolderSettings { ViewMode = _viewMode, Flags = CurrentFlags() };
            var rect = NativeRect.FromClientSize(ClientSize);
            var initializeHr = _browser.Initialize(Handle, ref rect, ref settings);
            ProbeLog.Write($"IExplorerBrowser.Initialize(host=0x{Handle.ToInt64():X}, rect={rect}, viewMode={ShellText.DescribeViewMode(_viewMode)}, flags={ShellText.Describe(settings.Flags)}) -> {HResult.Describe(initializeHr)}");
            HResult.Check(initializeHr, "IExplorerBrowser.Initialize");

            var optionsHr = _browser.SetOptions(_options);
            ProbeLog.Write($"IExplorerBrowser.SetOptions({ShellText.Describe(_options)}) -> {HResult.Describe(optionsHr)}");

            _eventSink = new BrowserEventSink(this);
            HResult.Check(_browser.Advise(_eventSink, out _adviseCookie), "IExplorerBrowser.Advise");
            _advised = true;

            Report($"浏览器就绪: {ShellText.Describe(_options)}");
            if (_pendingPath is not null)
            {
                NavigateCore(_pendingPath);
            }
        }
        catch (Exception ex)
        {
            ProbeLog.Write($"创建浏览器失败: {ex}");
            Report($"创建浏览器失败: {ex.Message}");
            DestroyBrowser();
        }
    }

    private void DestroyBrowser()
    {
        _paneProviderInstalled = false;
        _paneProvider = null;

        if (_shellView is not null)
        {
            Marshal.ReleaseComObject(_shellView);
            _shellView = null;
        }

        _shellViewWindow = IntPtr.Zero;

        if (_browser is not null)
        {
            if (_advised)
            {
                var unadviseHr = _browser.Unadvise(_adviseCookie);
                ProbeLog.Write($"IExplorerBrowser.Unadvise -> {HResult.Describe(unadviseHr)}");
                _advised = false;
            }

            var destroyHr = _browser.Destroy();
            ProbeLog.Write($"IExplorerBrowser.Destroy -> {HResult.Describe(destroyHr)}");
            Marshal.ReleaseComObject(_browser);
            _browser = null;
        }

        _eventSink = null;
        _adviseCookie = 0;
    }

    private void NavigateCore(string path)
    {
        if (_browser is null)
        {
            return;
        }

        try
        {
            var interfaceId = ShellGuids.IShellItem;
            var hr = NativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, ref interfaceId, out var shellItem);
            if (hr < 0 || shellItem is null)
            {
                ProbeLog.Write($"SHCreateItemFromParsingName('{path}') -> {HResult.Describe(hr)}");
                Report($"无法打开 '{path}': {HResult.Describe(hr)}");
                return;
            }

            try
            {
                var browseHr = _browser.BrowseToObject(shellItem, 0);
                ProbeLog.Write($"IExplorerBrowser.BrowseToObject('{path}') -> {HResult.Describe(browseHr)}");
                if (browseHr < 0)
                {
                    Report($"导航失败: {HResult.Describe(browseHr)}");
                }
            }
            finally
            {
                Marshal.ReleaseComObject(shellItem);
            }
        }
        catch (Exception ex)
        {
            ProbeLog.Write($"导航 '{path}' 失败: {ex.Message}");
            Report($"无法打开 '{path}': {ex.Message}");
        }
    }

    private void ApplyRect()
    {
        if (_browser is null || !IsHandleCreated)
        {
            return;
        }

        // 外壳视图的坐标是宿主控件的客户区坐标，因此左上角恒为 (0,0)。
        var rect = NativeRect.FromClientSize(ClientSize);
        var hr = _browser.SetRect(IntPtr.Zero, rect);
        if (hr < 0)
        {
            ProbeLog.Write($"IExplorerBrowser.SetRect({rect}) -> {HResult.Describe(hr)}");
        }
    }

    /// <summary>打开 --dump 后，导航完成时自动输出一次窗口结构诊断。</summary>
    public void EnableDiagnosticDump(bool enabled) => _dumpOnNavigation = enabled;

    /// <summary>导航完成：抓取 IShellView、激活它、按需装站点、记录外壳实际使用的视图设置。</summary>
    internal void HandleNavigationComplete()
    {
        CaptureShellView();

        if (_shellView is not null)
        {
            var activateHr = _shellView.UIActivate(SvUiaActivateFocus);
            ProbeLog.Write($"IShellView.UIActivate(SVUIA_ACTIVATE_FOCUS) -> {HResult.Describe(activateHr)}");
        }

        FocusShellView();
        LogCurrentInfo("导航完成后");

        if (_paneProbeRequested && !_paneProviderInstalled)
        {
            InstallPaneProvider();
        }

        Report($"已显示: {_pendingPath}");

        if (_dumpOnNavigation)
        {
            DumpDiagnostics();
        }
    }

    internal void HandleNavigationFailed() => Report($"导航失败: {_pendingPath}");

    private void CaptureShellView()
    {
        if (_browser is null)
        {
            return;
        }

        if (_shellView is not null)
        {
            Marshal.ReleaseComObject(_shellView);
            _shellView = null;
        }

        _shellViewWindow = IntPtr.Zero;

        var interfaceId = ShellGuids.IShellView;
        var hr = _browser.GetCurrentView(ref interfaceId, out var viewPointer);
        if (hr < 0 || viewPointer == IntPtr.Zero)
        {
            ProbeLog.Write($"IExplorerBrowser.GetCurrentView(IID_IShellView) -> {HResult.Describe(hr)}");
            return;
        }

        try
        {
            _shellView = (IShellView)Marshal.GetObjectForIUnknown(viewPointer);
        }
        finally
        {
            Marshal.Release(viewPointer);
        }

        if (_shellView.GetWindow(out var viewWindow) >= 0 && viewWindow != IntPtr.Zero)
        {
            _shellViewWindow = viewWindow;
            ProbeLog.Write($"IShellView.GetWindow() -> 0x{viewWindow.ToInt64():X} {DescribeWindow(viewWindow)}");
        }
    }

    private void InstallPaneProvider()
    {
        if (_shellView is null)
        {
            return;
        }

        _paneProviderInstalled = true;

        // 先探测：现有站点链里有没有人提供 SID_ExplorerPaneVisibility。
        ProbeExistingPaneService();

        IServiceProvider? previousSite = null;
        if (_shellView is IObjectWithSite withSite)
        {
            var interfaceId = ShellGuids.IServiceProvider;
            if (withSite.GetSite(ref interfaceId, out var sitePointer) >= 0 && sitePointer != IntPtr.Zero)
            {
                try
                {
                    previousSite = Marshal.GetObjectForIUnknown(sitePointer) as IServiceProvider;
                }
                finally
                {
                    Marshal.Release(sitePointer);
                }
            }

            _paneProvider = new PaneVisibilityProvider(previousSite);
            var setSiteHr = withSite.SetSite(_paneProvider);
            ProbeLog.Write($"IObjectWithSite.SetSite(烟测的 IExplorerPaneVisibility 提供者) -> {HResult.Describe(setSiteHr)}");

            // 窗格是在导航过程中创建的，装好站点后再导航一次才能看出外壳是否来查询。
            Post(NavigateAgain);
        }
    }

    private void ProbeExistingPaneService()
    {
        if (_shellView is not IServiceProvider provider)
        {
            ProbeLog.Write("IShellView 不支持 IServiceProvider，无法探测既有窗格服务。");
            return;
        }

        var service = ShellGuids.IExplorerPaneVisibility;
        var interfaceId = ShellGuids.IExplorerPaneVisibility;
        var hr = provider.QueryService(ref service, ref interfaceId, out var pointer);
        ProbeLog.Write($"视图 QueryService(SID_ExplorerPaneVisibility) -> {HResult.Describe(hr)}，ppv=0x{pointer.ToInt64():X}");
        if (pointer != IntPtr.Zero)
        {
            Marshal.Release(pointer);
        }
    }

    private void NavigateAgain()
    {
        if (_pendingPath is not null)
        {
            NavigateCore(_pendingPath);
        }
    }

    private void Post(Action action)
    {
        if (!IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(action);
        }
        catch (InvalidOperationException ex)
        {
            ProbeLog.Write($"BeginInvoke 失败: {ex.Message}");
        }
    }

    private void LogCurrentInfo(string context)
    {
        if (_shellView is null)
        {
            return;
        }

        var settings = new FolderSettings();
        var hr = _shellView.GetCurrentInfo(ref settings);
        if (hr < 0)
        {
            ProbeLog.Write($"IShellView.GetCurrentInfo({context}) -> {HResult.Describe(hr)}");
            return;
        }

        ProbeLog.Write($"IShellView.GetCurrentInfo({context}): viewMode={ShellText.DescribeViewMode(settings.ViewMode)}, flags={ShellText.Describe(settings.Flags)}");
    }

    /// <summary>
    /// 把宿主窗口下面的整个窗口树打印出来。这是判断“外围界面到底有没有被隐藏”的客观证据：
    /// 如果外壳仍在创建命令栏/状态栏，这里会出现 ToolbarWindow32、msctls_statusbar32 之类的窗口。
    /// </summary>
    public void DumpDiagnostics()
    {
        var builder = new StringBuilder();
        builder.AppendLine("---- 诊断 ----");
        builder.AppendLine($"目录            : {_pendingPath}");
        builder.AppendLine($"选项            : {ShellText.Describe(_options)}");
        builder.AppendLine($"请求的视图模式  : {ShellText.DescribeViewMode(_viewMode)}");
        builder.AppendLine($"请求的文件夹标志: {ShellText.Describe(CurrentFlags())}");
        builder.AppendLine($"宿主窗口        : 0x{Handle.ToInt64():X} {DescribeWindow(Handle)}");
        if (_shellViewWindow != IntPtr.Zero)
        {
            builder.AppendLine($"外壳视图窗口    : 0x{_shellViewWindow.ToInt64():X} {DescribeWindow(_shellViewWindow)}");
        }

        builder.AppendLine("宿主窗口的后代窗口（EnumChildWindows，扁平列出）:");
        NativeMethods.EnumChildWindows(Handle, (child, _) =>
        {
            builder.AppendLine($"  0x{child.ToInt64():X} {DescribeWindow(child)}");
            return true;
        }, IntPtr.Zero);

        ProbeLog.WriteBlock(builder.ToString().TrimEnd());
        Report("诊断已写入日志");
    }

    private static string DescribeWindow(IntPtr window)
    {
        var style = unchecked((uint)NativeMethods.GetWindowLong(window, NativeMethods.GwlStyle));
        var styleText = new StringBuilder();
        if ((style & NativeMethods.WsChild) != 0) styleText.Append("WS_CHILD|");
        if ((style & NativeMethods.WsClipChildren) != 0) styleText.Append("WS_CLIPCHILDREN|");
        if ((style & NativeMethods.WsClipSiblings) != 0) styleText.Append("WS_CLIPSIBLINGS|");
        if ((style & NativeMethods.WsBorder) != 0) styleText.Append("WS_BORDER|");
        if ((style & NativeMethods.WsDisabled) != 0) styleText.Append("WS_DISABLED|");
        if ((style & NativeMethods.WsHScroll) != 0) styleText.Append("WS_HSCROLL|");
        if ((style & NativeMethods.WsVScroll) != 0) styleText.Append("WS_VSCROLL|");
        if (styleText.Length == 0) styleText.Append("无|");

        var bounds = NativeMethods.GetWindowRect(window, out var rect) ? rect.ToString() : "?";
        var visible = NativeMethods.IsWindowVisible(window) ? "visible" : "hidden";
        var parent = NativeMethods.GetParent(window);

        return $"class='{GetWindowClassName(window)}' parent=0x{parent.ToInt64():X} {visible} rect={bounds} style={styleText.ToString().TrimEnd('|')}";
    }

    private static string GetWindowClassName(IntPtr window)
    {
        var buffer = new StringBuilder(256);
        var length = NativeMethods.GetClassName(window, buffer, buffer.Capacity);
        return length > 0 ? buffer.ToString() : "?";
    }

    private void Report(string message)
    {
        ProbeLog.Write(message);
        StatusChanged?.Invoke(message);
    }
}



