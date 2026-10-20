using System.Runtime.InteropServices;
using ExplorerBrowserProbe.Interop;

// System.IServiceProvider 与 COM 接口同名，这里明确指向 COM 的那个。
using IServiceProvider = ExplorerBrowserProbe.Interop.IServiceProvider;

namespace ExplorerBrowserProbe;

/// <summary>
/// 实验性：宿主侧的 IExplorerPaneVisibility + IServiceProvider。
/// 官方文档要求宿主实现 QueryService(SID_ExplorerPaneVisibility) 并且“必须在站点链里”，
/// 但 IExplorerBrowser 没有提供 SetSite，所以烟测用 IObjectWithSite 把本提供者装到视图站点上，
/// 并把其它服务请求转发给原来的站点，然后靠日志观察外壳究竟有没有来查询。
/// 这一步不是必需项：关闭 EBO_SHOWFRAMES 时本来就没有外围窗格。
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class PaneVisibilityProvider : IExplorerPaneVisibility, IServiceProvider
{
    private readonly IServiceProvider? _previousSite;

    /// <param name="previousSite">
    /// 视图原来的站点（通常是 IExplorerBrowser 自己实现的 IShellBrowser），用于转发非窗格的服务请求。
    /// 这里刻意不释放它的 RCW：它与 IExplorerBrowser 的 RCW 可能是同一个对象，提前释放会让浏览器失效。
    /// </param>
    public PaneVisibilityProvider(IServiceProvider? previousSite)
    {
        _previousSite = previousSite;
    }

    /// <summary>烟测策略：已知的外围窗格全部返回“默认关闭 + 强制”。</summary>
    public int GetPaneState(ref Guid ep, out uint peps)
    {
        var hide = IsKnownPane(ep);
        peps = hide ? PaneStates.DefaultOff | PaneStates.Force : PaneStates.DontCare;
        ProbeLog.Write($"IExplorerPaneVisibility.GetPaneState({ShellPanes.Describe(ep)}) -> {(hide ? "EPS_DEFAULT_OFF|EPS_FORCE" : "EPS_DONTCARE")}");
        return 0;
    }

    public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;

        if (guidService == ShellGuids.IExplorerPaneVisibility)
        {
            ppvObject = Marshal.GetComInterfaceForObject(this, typeof(IExplorerPaneVisibility));
            ProbeLog.Write("IServiceProvider.QueryService(SID_ExplorerPaneVisibility) -> 返回本烟测的实现");
            return 0;
        }

        if (_previousSite is not null)
        {
            ProbeLog.Write($"IServiceProvider.QueryService({guidService:B}, {riid:B}) -> 转发给原站点");
            return _previousSite.QueryService(ref guidService, ref riid, out ppvObject);
        }

        ProbeLog.Write($"IServiceProvider.QueryService({guidService:B}, {riid:B}) -> E_NOINTERFACE（没有可转发的原站点）");
        return HResult.ENoInterface;
    }

    private static bool IsKnownPane(Guid pane) =>
        pane == ShellPanes.NavPane
        || pane == ShellPanes.Commands
        || pane == ShellPanes.CommandsOrganize
        || pane == ShellPanes.CommandsView
        || pane == ShellPanes.DetailsPane
        || pane == ShellPanes.PreviewPane
        || pane == ShellPanes.QueryPane
        || pane == ShellPanes.AdvQueryPane
        || pane == ShellPanes.StatusBar
        || pane == ShellPanes.Ribbon;
}
