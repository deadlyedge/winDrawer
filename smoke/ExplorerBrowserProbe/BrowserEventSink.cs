using System.Runtime.InteropServices;
using ExplorerBrowserProbe.Interop;

namespace ExplorerBrowserProbe;

/// <summary>
/// IExplorerBrowserEvents 的托管实现。外壳在导航过程中回调这里；日志是烟测的主要证据来源：
/// 能看到“导航完成/失败”，也能在完成时抓取 IShellView 来做激活和诊断。
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class BrowserEventSink : IExplorerBrowserEvents
{
    private readonly ExplorerBrowserHost _host;

    public BrowserEventSink(ExplorerBrowserHost host)
    {
        _host = host;
    }

    public int OnNavigationPending(IntPtr pidlFolder)
    {
        // 放行还是拦下由宿主决定：返回失败码即取消导航。
        return _host.HandleNavigationPending(pidlFolder);
    }

    public int OnViewCreated(IntPtr psv)
    {
        ProbeLog.Write($"IExplorerBrowserEvents.OnViewCreated(IShellView=0x{psv.ToInt64():X})");
        return 0;
    }

    public int OnNavigationComplete(IntPtr pidlFolder)
    {
        ProbeLog.Write("IExplorerBrowserEvents.OnNavigationComplete");
        _host.HandleNavigationComplete(pidlFolder);
        return 0;
    }

    public int OnNavigationFailed(IntPtr pidlFolder)
    {
        ProbeLog.Write("IExplorerBrowserEvents.OnNavigationFailed");
        _host.HandleNavigationFailed(pidlFolder);
        return 0;
    }
}
