using System.Runtime.InteropServices;

namespace ExplorerBrowserProbe.Interop;

/// <summary>
/// Win32 RECT。刻意不复用 <see cref="Rectangle"/>，避免和布局代码里的矩形含义混淆。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public static NativeRect FromClientSize(Size size) => new()
    {
        Left = 0,
        Top = 0,
        Right = size.Width,
        Bottom = size.Height,
    };

    public override string ToString() => $"({Left},{Top})-({Right},{Bottom})";
}

/// <summary>
/// FOLDERSETTINGS（shobjidl_core.h）。ViewMode 取 <see cref="FolderViewModes"/>，
/// Flags 取 <see cref="FolderFlags"/>。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FolderSettings
{
    public uint ViewMode;
    public FolderFlags Flags;
}

/// <summary>Win32 MSG；只为 <see cref="IShellView.TranslateAccelerator"/> 的 vtable 顺序保留。</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeMessage
{
    public IntPtr Window;
    public uint Message;
    public IntPtr WParam;
    public IntPtr LParam;
    public uint Time;
    public int X;
    public int Y;
}

/// <summary>
/// 用到的 CLSID/IID。全部取自本机 Windows SDK 10.0.26100.0 的 ShObjIdl_core.h、ShObjIdl.h 和 ShlGuid.h。
/// 不要凭记忆改写这些 GUID：IExplorerBrowser 的 IID 和 vtable 顺序必须逐字对应，否则调用会落到错误槽位。
/// </summary>
internal static class ShellGuids
{
    public static readonly Guid ExplorerBrowserClass = new("71f96385-ddd6-48d3-a0c1-ae06e8b055fb");
    public static readonly Guid IExplorerBrowser = new("dfd3b6b5-c10c-4be9-85f6-a66969f402f6");
    public static readonly Guid IExplorerBrowserEvents = new("361bbdc7-e6ee-4e13-be58-58e2240c810f");
    public static readonly Guid IExplorerPaneVisibility = new("e07010ec-bc17-44c0-97b0-46c7c95b9edc");
    public static readonly Guid IShellView = new("000214e3-0000-0000-c000-000000000046");
    public static readonly Guid IShellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
    public static readonly Guid IServiceProvider = new("6d5140c1-7436-11ce-8034-00aa006009fa");
    public static readonly Guid IObjectWithSite = new("fc4801a3-2ba9-11cf-a229-00aa003d7352");
    public static readonly Guid IUnknown = new("00000000-0000-0000-c000-000000000046");
    public static readonly Guid IContextMenu = new("000214e4-0000-0000-c000-000000000046");
    public static readonly Guid IPersistFolder2 = new("1ac3d9f0-175c-11d1-95be-00609797ea4f");
}

/// <summary>ShlGuid.h 里的窗格标识（IExplorerPaneVisibility 用）。</summary>
internal static class ShellPanes
{
    public static readonly Guid NavPane = new("cb316b22-25f7-42b8-8a09-540d23a43c2f");
    public static readonly Guid Commands = new("d9745868-ca5f-4a76-91cd-f5a129fbb076");
    public static readonly Guid CommandsOrganize = new("72e81700-e3ec-4660-bf24-3c3b7b648806");
    public static readonly Guid CommandsView = new("21f7c32d-eeaa-439b-bb51-37b96fd6a943");
    public static readonly Guid DetailsPane = new("43abf98b-89b8-472d-b9ce-e69b8229f019");
    public static readonly Guid PreviewPane = new("893c63d1-45c8-4d17-be19-223be71be365");
    public static readonly Guid QueryPane = new("65bcde4f-4f07-4f27-83a7-1afca4df7ddd");
    public static readonly Guid AdvQueryPane = new("b4e9db8b-34ba-4c39-b5cc-16a1bd2c411c");
    public static readonly Guid StatusBar = new("65fe56ce-5cfe-4bc4-ad8a-7ae3fe7e8f7c");
    public static readonly Guid Ribbon = new("d27524a8-c9f2-4834-a106-df8889fd4f37");

    public static string Describe(Guid pane)
    {
        if (pane == NavPane) return "EP_NavPane";
        if (pane == Commands) return "EP_Commands";
        if (pane == CommandsOrganize) return "EP_Commands_Organize";
        if (pane == CommandsView) return "EP_Commands_View";
        if (pane == DetailsPane) return "EP_DetailsPane";
        if (pane == PreviewPane) return "EP_PreviewPane";
        if (pane == QueryPane) return "EP_QueryPane";
        if (pane == AdvQueryPane) return "EP_AdvQueryPane";
        if (pane == StatusBar) return "EP_StatusBar";
        if (pane == Ribbon) return "EP_Ribbon";
        return pane.ToString("B");
    }
}


/// <summary>EXPLORER_BROWSER_OPTIONS（shobjidl_core.h）。</summary>
[Flags]
internal enum ExplorerBrowserOptions : uint
{
    None = 0,

    /// <summary>EBO_NAVIGATEONCE：不再导航到初始位置之外。</summary>
    NavigateOnce = 0x1,

    /// <summary>EBO_SHOWFRAMES：使用命令模块、导航、详细信息和预览窗格。不设置时浏览器只用一个视图对象。</summary>
    ShowFrames = 0x2,

    /// <summary>EBO_ALWAYSNAVIGATE：即使目标就是当前文件夹也重新导航。</summary>
    AlwaysNavigate = 0x4,

    /// <summary>EBO_NOTRAVELLOG：不更新前进/后退历史。</summary>
    NoTravelLog = 0x8,

    /// <summary>EBO_NOWRAPPERWINDOW：不要包装窗口，浏览器直接挂在宿主窗口上。</summary>
    NoWrapperWindow = 0x10,

    /// <summary>EBO_NOBORDER：不给浏览器窗口画边框（Windows Vista 起）。</summary>
    NoBorder = 0x40,

    /// <summary>EBO_NOPERSISTVIEWSTATE：不持久化视图状态；烟测默认打开，避免污染用户的资源管理器设置。</summary>
    NoPersistViewState = 0x80,
}

/// <summary>FOLDERFLAGS（shobjidl_core.h）里本次要用的部分。</summary>
[Flags]
internal enum FolderFlags : uint
{
    None = 0,
    AutoArrange = 0x1,
    SnapToGrid = 0x4,
    NoSubFolders = 0x80,
    Transparent = 0x100,
    NoClientEdge = 0x200,
    NoScroll = 0x400,
    AlignLeft = 0x800,
    ShowSelAlways = 0x2000,
    NoGrouping = 0x100000,
    FullRowSelect = 0x200000,
    NoFilters = 0x400000,
    NoColumnHeader = 0x800000,
    NoHeaderInAllViews = 0x1000000,
    NoBrowserViewState = 0x10000000,
}

/// <summary>FOLDERVIEWMODE（shobjidl_core.h）。</summary>
internal static class FolderViewModes
{
    public const uint Auto = 0xFFFFFFFF; // FVM_AUTO == -1
    public const uint Icon = 1;
    public const uint SmallIcon = 2;
    public const uint List = 3;
    public const uint Details = 4;
    public const uint Thumbnail = 5;
    public const uint Tile = 6;
    public const uint Content = 8;

    public static bool TryParse(string value, out uint viewMode)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "auto": viewMode = Auto; return true;
            case "icon": case "icons": viewMode = Icon; return true;
            case "smallicon": case "smallicons": viewMode = SmallIcon; return true;
            case "list": viewMode = List; return true;
            case "details": case "detail": viewMode = Details; return true;
            case "thumbnail": case "thumbnails": viewMode = Thumbnail; return true;
            case "tile": case "tiles": viewMode = Tile; return true;
            case "content": viewMode = Content; return true;
            default: viewMode = Icon; return false;
        }
    }
}

/// <summary>EXPLORERPANESTATE（shobjidl_core.h）。</summary>
internal static class PaneStates
{
    public const uint DontCare = 0;
    public const uint DefaultOn = 0x1;
    public const uint DefaultOff = 0x2;
    public const uint StateMask = 0xFFFF;
    public const uint InitialState = 0x10000;
    public const uint Force = 0x20000;
}

/// <summary>
/// IExplorerBrowser（IID dfd3b6b5-c10c-4be9-85f6-a66969f402f6）。
/// 方法顺序必须和 shobjidl_core.h 一致。
/// </summary>
[ComImport]
[Guid("dfd3b6b5-c10c-4be9-85f6-a66969f402f6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IExplorerBrowser
{
    [PreserveSig]
    int Initialize(IntPtr hwndParent, ref NativeRect prc, ref FolderSettings pfs);

    [PreserveSig]
    int Destroy();

    /// <summary>RECT 在 ABI 中按值传递（16 字节，x64 下是调用方副本的指针），C# 按值声明与头文件一致。</summary>
    [PreserveSig]
    int SetRect(IntPtr phdwp, NativeRect rcBrowser);

    [PreserveSig]
    int SetPropertyBag([MarshalAs(UnmanagedType.LPWStr)] string pszPropertyBag);

    [PreserveSig]
    int SetEmptyText([MarshalAs(UnmanagedType.LPWStr)] string pszEmptyText);

    [PreserveSig]
    int SetFolderSettings(ref FolderSettings pfs);

    [PreserveSig]
    int Advise([MarshalAs(UnmanagedType.Interface)] IExplorerBrowserEvents psbe, out uint pdwCookie);

    [PreserveSig]
    int Unadvise(uint dwCookie);

    [PreserveSig]
    int SetOptions(ExplorerBrowserOptions dwFlag);

    [PreserveSig]
    int GetOptions(out ExplorerBrowserOptions pdwFlag);

    [PreserveSig]
    int BrowseToIDList(IntPtr pidl, uint uFlags);

    [PreserveSig]
    int BrowseToObject([MarshalAs(UnmanagedType.Interface)] object punk, uint uFlags);

    [PreserveSig]
    int FillFromObject([MarshalAs(UnmanagedType.Interface)] object punk, uint dwFlags);

    [PreserveSig]
    int RemoveAll();

    [PreserveSig]
    int GetCurrentView(ref Guid riid, out IntPtr ppv);
}

/// <summary>
/// IExplorerBrowserEvents（IID 361bbdc7-e6ee-4e13-be58-58e2240c810f）。
/// 由托管侧实现，外壳在导航过程中回调。
/// </summary>
[ComImport]
[Guid("361bbdc7-e6ee-4e13-be58-58e2240c810f")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IExplorerBrowserEvents
{
    [PreserveSig]
    int OnNavigationPending(IntPtr pidlFolder);

    [PreserveSig]
    int OnViewCreated(IntPtr psv);

    [PreserveSig]
    int OnNavigationComplete(IntPtr pidlFolder);

    [PreserveSig]
    int OnNavigationFailed(IntPtr pidlFolder);
}

/// <summary>
/// IExplorerPaneVisibility（IID e07010ec-bc17-44c0-97b0-46c7c95b9edc，服务 ID 为 SID_ExplorerPaneVisibility）。
/// </summary>
[ComImport]
[Guid("e07010ec-bc17-44c0-97b0-46c7c95b9edc")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IExplorerPaneVisibility
{
    [PreserveSig]
    int GetPaneState(ref Guid ep, out uint peps);
}

/// <summary>IServiceProvider（IID 6d5140c1-7436-11ce-8034-00aa006009fa）。</summary>
[ComImport]
[Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IServiceProvider
{
    [PreserveSig]
    int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject);
}

/// <summary>IObjectWithSite（IID fc4801a3-2ba9-11cf-a229-00aa003d7352）。</summary>
[ComImport]
[Guid("fc4801a3-2ba9-11cf-a229-00aa003d7352")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IObjectWithSite
{
    [PreserveSig]
    int SetSite([MarshalAs(UnmanagedType.IUnknown)] object? pUnkSite);

    [PreserveSig]
    int GetSite(ref Guid riid, out IntPtr ppvSite);
}

/// <summary>IShellItem。烟测只把它当作 BrowseToObject 的输入对象，方法签名按头文件补全但不调用。</summary>
[ComImport]
[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    [PreserveSig]
    int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

    [PreserveSig]
    int GetParent(out IntPtr ppsi);

    [PreserveSig]
    int GetDisplayName(uint sigdnName, out IntPtr ppszName);

    [PreserveSig]
    int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

    [PreserveSig]
    int Compare(IShellItem psi, uint hint, out int piOrder);
}

/// <summary>
/// IShellView（IID 000214e3-0000-0000-c000-000000000046）。它派生自 IOleWindow，
/// 所以 IUnknown 之后先是 GetWindow/ContextSensitiveHelp，之后才是 IShellView 自己的方法。
/// </summary>
[ComImport]
[Guid("000214e3-0000-0000-c000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellView
{
    [PreserveSig]
    int GetWindow(out IntPtr phwnd);

    [PreserveSig]
    int ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool fEnterMode);

    [PreserveSig]
    int TranslateAccelerator(ref NativeMessage pmsg);

    [PreserveSig]
    int EnableModeless([MarshalAs(UnmanagedType.Bool)] bool fEnable);

    /// <summary>uState 取 SVUIA_*：0=DEACTIVATE、1=ACTIVATE_NOFOCUS、2=ACTIVATE_FOCUS、3=INPLACEACTIVATE。</summary>
    [PreserveSig]
    int UIActivate(uint uState);

    [PreserveSig]
    int Refresh();

    [PreserveSig]
    int CreateViewWindow(
        [MarshalAs(UnmanagedType.Interface)] IShellView? psvPrevious,
        ref FolderSettings pfs,
        [MarshalAs(UnmanagedType.Interface)] object? psb,
        ref NativeRect prcView,
        out IntPtr phWnd);

    [PreserveSig]
    int DestroyViewWindow();

    [PreserveSig]
    int GetCurrentInfo(ref FolderSettings pfs);

    [PreserveSig]
    int AddPropertySheetPages(uint dwReserved, IntPtr pfn, IntPtr lparam);

    [PreserveSig]
    int SaveViewState();

    [PreserveSig]
    int SelectItem(IntPtr pidlItem, uint uFlags);

    [PreserveSig]
    int GetItemObject(uint uItem, ref Guid riid, out IntPtr ppv);
}

internal static class NativeMethods
{
    public const uint ClsctxInProcServer = 0x1;
    public const int GwlStyle = -16;

    public const int WsChild = 0x40000000;
    public const int WsClipChildren = 0x02000000;
    public const int WsClipSiblings = 0x04000000;
    public const int WsBorder = 0x00800000;
    public const int WsDisabled = 0x08000000;
    public const int WsHScroll = 0x00100000;
    public const int WsVScroll = 0x00200000;

    public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("ole32.dll")]
    public static extern int CoCreateInstance(
        ref Guid rclsid,
        IntPtr pUnkOuter,
        uint dwClsContext,
        ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHCreateItemFromParsingName(
        string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hwnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern IntPtr GetParent(IntPtr hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    public static extern int GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hwnd, out NativeRect lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr SetFocus(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    public const uint MfByPosition = 0x400;

    /// <summary>CMF_NORMAL：给 QueryContextMenu 传的默认标志。</summary>
    public const uint CmfNormal = 0;

    [DllImport("shell32.dll")]
    public static extern int SHGetNameFromIDList(IntPtr pidl, uint sigdnName, out IntPtr ppszName);

    [DllImport("user32.dll")]
    public static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    public static extern int GetMenuItemCount(IntPtr hMenu);

    [DllImport("user32.dll")]
    public static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetMenuString(IntPtr hMenu, uint uIDItem, System.Text.StringBuilder lpString, int cchMax, uint flags);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern uint GetPixel(IntPtr hdc, int x, int y);

    /// <summary>
    /// uxtheme 未公开导出（序号 135）：SetPreferredAppMode，让外壳控件走深色渲染。
    /// 0=Default、1=AllowDark、2=ForceDark、3=ForceLight；返回值是之前的模式。
    /// 仅在 --dark 实验里调用，失败只记日志。
    /// </summary>
    [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
    public static extern int SetPreferredAppMode(int appMode);
}

/// <summary>把 HRESULT 变成可读文本；只有真正失败（小于 0）时才抛异常。</summary>
internal static class HResult
{
    public const int ENoInterface = unchecked((int)0x80004002);

    /// <summary>OnNavigationPending 返回任意失败码都会取消导航；这里用 E_ABORT 表达“宿主有意拦下”。</summary>
    public const int EAbort = unchecked((int)0x80004004);

    public static void Check(int hr, string operation)
    {
        if (hr < 0)
        {
            throw new COMException($"{operation} 失败，HRESULT 0x{hr:X8}", hr);
        }
    }

    public static string Describe(int hr) => hr < 0 ? $"0x{hr:X8}（失败）" : $"0x{hr:X8}（成功）";
}

/// <summary>把外壳枚举值转成日志里可读的名字。</summary>
internal static class ShellText
{
    public static string Describe(ExplorerBrowserOptions options)
    {
        if (options == ExplorerBrowserOptions.None)
        {
            return "EBO_NONE";
        }

        var names = new List<string>();
        foreach (var value in Enum.GetValues<ExplorerBrowserOptions>())
        {
            if (value != ExplorerBrowserOptions.None && options.HasFlag(value))
            {
                names.Add(value.ToString());
            }
        }

        return names.Count == 0 ? "EBO_NONE" : string.Join("|", names);
    }

    public static string Describe(FolderFlags flags)
    {
        if (flags == FolderFlags.None)
        {
            return "FWF_NONE";
        }

        var names = new List<string>();
        foreach (var value in Enum.GetValues<FolderFlags>())
        {
            if (value != FolderFlags.None && flags.HasFlag(value))
            {
                names.Add(value.ToString());
            }
        }

        return names.Count == 0 ? "FWF_NONE" : string.Join("|", names);
    }

    public static string DescribeViewMode(uint viewMode) => viewMode switch
    {
        FolderViewModes.Auto => "FVM_AUTO",
        FolderViewModes.Icon => "FVM_ICON",
        FolderViewModes.SmallIcon => "FVM_SMALLICON",
        FolderViewModes.List => "FVM_LIST",
        FolderViewModes.Details => "FVM_DETAILS",
        FolderViewModes.Thumbnail => "FVM_THUMBNAIL",
        FolderViewModes.Tile => "FVM_TILE",
        FolderViewModes.Content => "FVM_CONTENT",
        _ => $"0x{viewMode:X}",
    };
}

/// <summary>SIGDN_*（shobjidl_core.h）：取项目的显示名/解析名。</summary>
internal static class ShellDisplayNames
{
    public const uint NormalDisplay = 0x00000000;
    public const uint DesktopAbsoluteParsing = 0x80028000;
    public const uint FileSystemPath = 0x80058000;
}

/// <summary>SVGIO_*（shobjidl_core.h）：IShellView::GetItemObject 要取哪一部分。</summary>
internal static class ShellViewItems
{
    public const uint Background = 0;
    public const uint Selection = 0x1;
    public const uint AllView = 0x2;
    public const uint Checked = 0x3;
}

/// <summary>IContextMenu（IID 000214e4-0000-0000-c000-000000000046）。</summary>
[ComImport]
[Guid("000214e4-0000-0000-c000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IContextMenu
{
    [PreserveSig]
    int QueryContextMenu(IntPtr hMenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags);

    [PreserveSig]
    int InvokeCommand(IntPtr pici);

    [PreserveSig]
    int GetCommandString(IntPtr idCmd, uint uType, IntPtr pReserved, IntPtr pszName, uint cchMax);
}

/// <summary>
/// IPersistFolder2（IID 1ac3d9f0-175c-11d1-95be-00609797ea4f）。
/// 继承链是 IPersist → IPersistFolder → IPersistFolder2，vtable 顺序必须按此列出。
/// 由 DefView（外壳视图）实现，用来问“视图现在到底在哪个目录”。
/// </summary>
[ComImport]
[Guid("1ac3d9f0-175c-11d1-95be-00609797ea4f")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPersistFolder2
{
    [PreserveSig]
    int GetClassID(out Guid pClassId);

    [PreserveSig]
    int Initialize(IntPtr pidl);

    [PreserveSig]
    int GetCurFolder(out IntPtr ppidl);
}
