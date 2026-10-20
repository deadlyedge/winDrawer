using System.Diagnostics;
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

    /// <summary>命令行追加的 FOLDERFLAGS，叠加在烟测固定的 FWF_NOBROWSERVIEWSTATE 之上。</summary>
    private readonly FolderFlags _extraFlags;

    /// <summary>
    /// 透明实验时故意不加 WS_CLIPCHILDREN：FWF_TRANSPARENT 要靠父窗口先画背景，
    /// 而 WS_CLIPCHILDREN 会把父窗口在子窗口区域的绘制裁掉，两者互斥。
    /// </summary>
    private readonly bool _clipChildren;

    private readonly bool _paneProbeRequested;
    private readonly bool _dumpMenuOnNavigation;
    private readonly bool _samplePixelsOnNavigation;

    private IExplorerBrowser? _browser;
    private BrowserEventSink? _eventSink;
    private IShellView? _shellView;
    private IntPtr _shellViewWindow;
    private PaneVisibilityProvider? _paneProvider;
    private uint _adviseCookie;
    private bool _advised;
    private bool _paneProviderInstalled;
    private bool _dumpOnNavigation;
    private int _navigationPendingCount;
    private int _navigationCompleteCount;
    private bool _persistFolderLogged;
    private string? _pendingPath;
    private ExplorerBrowserOptions _options;
    private uint _viewMode;
    private FolderOpenMode _folderOpenMode;

    public ExplorerBrowserHost(ProbeOptions options)
    {
        _options = options.BrowserOptions;
        _viewMode = options.ViewMode;
        _extraFlags = options.ExtraFlags;
        _paneProbeRequested = options.PaneProbe;
        _dumpMenuOnNavigation = options.DumpMenu;
        _samplePixelsOnNavigation = options.SamplePixels;
        _folderOpenMode = options.FolderOpen;
        _clipChildren = (options.ExtraFlags & FolderFlags.Transparent) == 0;

        TabStop = false;
        SetStyle(ControlStyles.Selectable, false);
        BackColor = options.Background ?? Color.White;
        MinimumSize = new Size(1, 1);
    }

    /// <summary>状态文本（同时写日志）。窗体工具条把它显示出来。</summary>
    public event Action<string>? StatusChanged;

    /// <summary>每次导航完成后触发一次，自检脚本用它串流程。</summary>
    public event Action? NavigationCompleted;

    public ExplorerBrowserOptions Options => _options;

    public uint ViewMode => _viewMode;

    public FolderOpenMode FolderOpenMode => _folderOpenMode;

    /// <summary>外壳回调 OnNavigationPending 的次数（自检据此判断导航请求有没有真的发生）。</summary>
    public int NavigationPendingCount => _navigationPendingCount;

    public int NavigationCompleteCount => _navigationCompleteCount;

    /// <summary>切换双击文件夹的去向；不需要重建浏览器（只影响 OnNavigationPending 的决策）。</summary>
    public void SetFolderOpenMode(FolderOpenMode mode)
    {
        _folderOpenMode = mode;
        ProbeLog.Write($"文件夹打开方式 -> {DescribeFolderOpenMode(mode)}");
        Report($"文件夹打开方式 -> {DescribeFolderOpenMode(mode)}");
    }

    public static string DescribeFolderOpenMode(FolderOpenMode mode) => mode switch
    {
        FolderOpenMode.OpenInExplorer => "explorer（拦下导航，另开资源管理器窗口）",
        FolderOpenMode.Block => "block（只拦下导航）",
        _ => "inplace（在本窗口里继续浏览）",
    };

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            if (_clipChildren)
            {
                // 外壳视图是本控件的子窗口。没有 WS_CLIPCHILDREN 时，本控件的背景绘制会盖住外壳视图。
                parameters.Style |= NativeMethods.WsClipChildren;
            }

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

    /// <summary>
    /// 自检用：请求一次导航，但不改变锚定目录，用来验证 OnNavigationPending 的拦截是否生效。
    /// </summary>
    public void NavigateForSelfTest(string path) => NavigateCore(path);

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

    /// <summary>
    /// 外壳在导航前回调。inplace 模式放行；其它模式拦下对“非锚定目录”的导航，
    /// 需要时用 Windows Shell 另开一个资源管理器窗口（不自己拼命令行）。
    /// 返回失败码即取消导航，这是文档规定的做法。
    /// </summary>
    internal int HandleNavigationPending(IntPtr pidlFolder)
    {
        _navigationPendingCount++;
        var target = ResolveShellName(pidlFolder);
        if (target.Length == 0)
        {
            ProbeLog.Write("OnNavigationPending：pidl 解析不出名字，放行。");
            return 0;
        }

        if (_folderOpenMode == FolderOpenMode.Inplace)
        {
            ProbeLog.Write($"OnNavigationPending('{target}') -> 放行（inplace）");
            return 0;
        }

        if (IsSamePath(target, _pendingPath))
        {
            ProbeLog.Write($"OnNavigationPending('{target}') -> 放行（正是锚定目录）");
            return 0;
        }

        if (_folderOpenMode == FolderOpenMode.OpenInExplorer)
        {
            OpenFolderInExplorer(target);
        }

        ProbeLog.Write($"OnNavigationPending('{target}') -> 已取消导航（{DescribeFolderOpenMode(_folderOpenMode)}）");
        return HResult.EAbort;
    }

    /// <summary>把 pidl 解析成名字：优先解析路径，失败退回普通显示名。</summary>
    private static string ResolveShellName(IntPtr pidl)
    {
        if (pidl == IntPtr.Zero)
        {
            return string.Empty;
        }

        foreach (var sigdn in new[] { ShellDisplayNames.DesktopAbsoluteParsing, ShellDisplayNames.NormalDisplay })
        {
            if (NativeMethods.SHGetNameFromIDList(pidl, sigdn, out var pointer) >= 0 && pointer != IntPtr.Zero)
            {
                try
                {
                    return Marshal.PtrToStringUni(pointer) ?? string.Empty;
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pointer);
                }
            }
        }

        return string.Empty;
    }

    /// <summary>只判断“是否同一个目录”，够烟测用；主程序里已有统一的 PathComparison。</summary>
    private static bool IsSamePath(string left, string? right)
    {
        if (string.IsNullOrEmpty(right))
        {
            return false;
        }

        return string.Equals(
            left.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            right.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>交给 Windows Shell 打开：等价于在资源管理器里双击文件夹，不自己拼命令行。</summary>
    private static void OpenFolderInExplorer(string path)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            ProbeLog.Write($"Shell 打开 '{path}' -> 已请求（pid={(process is null ? "未知" : process.Id.ToString())}）");
        }
        catch (Exception ex)
        {
            ProbeLog.Write($"Shell 打开 '{path}' 失败: {ex.Message}");
        }
    }

    /// <summary>问外壳视图“你现在在哪个目录”（IPersistFolder2），用来验证导航是否真的被拦下。</summary>
    public string GetCurrentViewPath()
    {
        if (_shellView is not IPersistFolder2 persist)
        {
            if (!_persistFolderLogged)
            {
                _persistFolderLogged = true;
                ProbeLog.Write("外壳视图不支持 IPersistFolder2，无法自报当前目录（改用导航事件里的 pidl 判断）。");
            }

            return string.Empty;
        }

        var hr = persist.GetCurFolder(out var pidl);
        if (hr < 0 || pidl == IntPtr.Zero)
        {
            if (!_persistFolderLogged)
            {
                _persistFolderLogged = true;
                ProbeLog.Write($"IPersistFolder2.GetCurFolder -> {HResult.Describe(hr)}");
            }

            return string.Empty;
        }

        try
        {
            return ResolveShellName(pidl);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pidl);
        }
    }

    /// <summary>
    /// 枚举外壳视图背景右键菜单里实际存在的项，用来回答“排序/分组能不能隐藏”：
    /// 菜单内容由外壳按 FOLDERFLAGS 和视图模式生成，只有拿到真实菜单才知道标志有没有起作用。
    /// </summary>
    public void DumpBackgroundMenu()
    {
        if (_shellView is null)
        {
            Report("还没有外壳视图，无法转储菜单");
            return;
        }

        var interfaceId = ShellGuids.IContextMenu;
        var hr = _shellView.GetItemObject(ShellViewItems.Background, ref interfaceId, out var pointer);
        if (hr < 0 || pointer == IntPtr.Zero)
        {
            ProbeLog.Write($"IShellView.GetItemObject(SVGIO_BACKGROUND, IID_IContextMenu) -> {HResult.Describe(hr)}");
            Report($"取不到背景菜单（{HResult.Describe(hr)}）");
            return;
        }

        IContextMenu? menu = null;
        var popup = IntPtr.Zero;
        try
        {
            menu = (IContextMenu)Marshal.GetObjectForIUnknown(pointer);
            popup = NativeMethods.CreatePopupMenu();
            var queryHr = menu.QueryContextMenu(popup, 0, 1, 0x7FFF, NativeMethods.CmfNormal);

            var builder = new StringBuilder();
            builder.AppendLine($"背景右键菜单（QueryContextMenu -> {HResult.Describe(queryHr)}）");
            builder.AppendLine($"  选项={ShellText.Describe(_options)} 标志={ShellText.Describe(CurrentFlags())} 视图={ShellText.DescribeViewMode(_viewMode)}");
            AppendMenuItems(builder, popup, "  ", 0);
            ProbeLog.WriteBlock(builder.ToString().TrimEnd());
            Report("背景菜单已写入日志");
        }
        catch (Exception ex)
        {
            ProbeLog.Write($"背景菜单转储失败: {ex.Message}");
            Report("背景菜单转储失败");
        }
        finally
        {
            if (popup != IntPtr.Zero)
            {
                NativeMethods.DestroyMenu(popup);
            }

            if (menu is not null)
            {
                Marshal.ReleaseComObject(menu);
            }

            Marshal.Release(pointer);
        }
    }

    private static void AppendMenuItems(StringBuilder builder, IntPtr hMenu, string indent, int depth)
    {
        var count = NativeMethods.GetMenuItemCount(hMenu);
        for (var index = 0; index < count; index++)
        {
            var text = new StringBuilder(256);
            var length = NativeMethods.GetMenuString(hMenu, (uint)index, text, text.Capacity, NativeMethods.MfByPosition);
            builder.AppendLine($"{indent}[{index}] {(length > 0 ? text.ToString() : "（分隔符）")}");

            var subMenu = NativeMethods.GetSubMenu(hMenu, index);
            if (subMenu != IntPtr.Zero && depth < 2)
            {
                AppendMenuItems(builder, subMenu, indent + "    ", depth + 1);
            }
        }
    }

    /// <summary>
    /// 采样外壳视图窗口的像素，客观判断“视图背景到底是系统色还是宿主窗口的颜色”。
    /// 需要窗口在真实桌面上可见；隐藏桌面/远程会话里 GetPixel 的结果不可信（日志会标注）。
    /// </summary>
    public void SampleViewPixels()
    {
        if (_shellViewWindow == IntPtr.Zero)
        {
            Report("还没有外壳视图窗口，无法采样");
            return;
        }

        var hdc = NativeMethods.GetDC(_shellViewWindow);
        if (hdc == IntPtr.Zero)
        {
            Report("GetDC 失败，无法采样像素");
            return;
        }

        try
        {
            var background = BackColor;
            var expected = ColorRef(background);
            var samples = new Dictionary<uint, int>();
            var size = ClientSize;

            for (var row = 1; row <= 5; row++)
            {
                for (var column = 1; column <= 5; column++)
                {
                    var color = NativeMethods.GetPixel(hdc, size.Width * column / 6, size.Height * row / 6) & 0xFFFFFF;
                    samples[color] = samples.TryGetValue(color, out var seen) ? seen + 1 : 1;
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine($"外壳视图像素采样 5x5（宿主背景=#{background.R:X2}{background.G:X2}{background.B:X2}，标志={ShellText.Describe(CurrentFlags())}，视图={ShellText.DescribeViewMode(_viewMode)}）:");
            foreach (var sample in samples.OrderByDescending(pair => pair.Value))
            {
                var rgb = $"#{sample.Key & 0xFF:X2}{(sample.Key >> 8) & 0xFF:X2}{(sample.Key >> 16) & 0xFF:X2}";
                builder.AppendLine($"  {rgb} x{sample.Value}{(sample.Key == expected ? "  <= 与宿主背景一致" : string.Empty)}");
            }

            if (!NativeMethods.IsWindowVisible(_shellViewWindow))
            {
                builder.AppendLine("  注意：外壳视图窗口当前不可见，结果不可信。");
            }

            ProbeLog.WriteBlock(builder.ToString().TrimEnd());
            Report(samples.ContainsKey(expected)
                ? "采样：出现了宿主背景色，说明该区域由宿主窗口绘制"
                : "采样：没有出现宿主背景色，说明视图自己画了不透明背景");
        }
        finally
        {
            NativeMethods.ReleaseDC(_shellViewWindow, hdc);
        }
    }

    private static uint ColorRef(Color color) => (uint)(color.R | (color.G << 8) | (color.B << 16));

    /// <summary>打开 --dump 后，导航完成时自动输出一次窗口结构诊断。</summary>
    public void EnableDiagnosticDump(bool enabled) => _dumpOnNavigation = enabled;

    /// <summary>导航完成：抓取 IShellView、激活它、按需装站点、记录外壳实际使用的视图设置。</summary>
    internal void HandleNavigationComplete(IntPtr pidlFolder)
    {
        _navigationCompleteCount++;
        CaptureShellView();

        if (_shellView is not null)
        {
            var activateHr = _shellView.UIActivate(SvUiaActivateFocus);
            ProbeLog.Write($"IShellView.UIActivate(SVUIA_ACTIVATE_FOCUS) -> {HResult.Describe(activateHr)}");
        }

        FocusShellView();
        LogCurrentInfo("导航完成后");

        // 事件里的 pidl 和视图自报的当前目录应当一致；不同就说明导航状态与视图状态脱节了。
        var eventPath = ResolveShellName(pidlFolder);
        var viewPath = GetCurrentViewPath();
        ProbeLog.Write($"导航目标：事件='{eventPath}'，视图自报='{viewPath}'");

        if (_paneProbeRequested && !_paneProviderInstalled)
        {
            InstallPaneProvider();
        }

        Report($"已显示: {(eventPath.Length > 0 ? eventPath : _pendingPath)}");

        if (_dumpOnNavigation)
        {
            DumpDiagnostics();
        }

        if (_dumpMenuOnNavigation)
        {
            DumpBackgroundMenu();
        }

        if (_samplePixelsOnNavigation)
        {
            SampleViewPixels();
        }

        NavigationCompleted?.Invoke();
    }

    internal void HandleNavigationFailed(IntPtr pidlFolder)
    {
        var target = ResolveShellName(pidlFolder);
        Report($"导航失败: {(target.Length > 0 ? target : _pendingPath)}");
    }

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



