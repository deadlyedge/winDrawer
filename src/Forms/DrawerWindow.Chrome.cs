using System.Runtime.InteropServices;

namespace winDrawer;

/// <summary>
/// Window-chrome interop for <see cref="DrawerWindow"/>.
///
/// The drawer hides its title bar by removing <c>WS_CAPTION</c> while keeping <c>WS_THICKFRAME</c>, so
/// the window stays resizable. Windows still reserves the full caption-less frame at the <em>top</em>:
/// the measured non-client insets are symmetric (13 px per side at 200% scaling on this machine), but
/// DWM only hides the 11 px invisible resize border on the left, right and bottom. That left a
/// visible ~13 px border-coloured band above the list while the other three sides showed ~2 px.
///
/// <see cref="WmNcCalcSize"/> drops the top inset so the client area starts at the visible top edge,
/// and a small content-coloured strip inside the client area keeps the top edge (and the top corners)
/// resizable, because points inside the client area are hit-tested by the list control rather than by
/// this window.
/// </summary>
public partial class DrawerWindow
{
    private const int WmNcCalcSize = 0x0083;
    private const int WmNcHitTest = 0x0084;
    private const int HtClient = 1;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;

    /// <summary>Offset of <c>NCCALCSIZE_PARAMS.rgrc[0].top</c> inside the message lParam.</summary>
    private const int ClientRectTopOffset = 4;

    /// <summary>Logical thickness of the invisible top resize strip.</summary>
    private const int TopResizeGripLogicalHeight = 8;

    private int _topResizeGripHeight;

    /// <summary>Current thickness (physical pixels) of the invisible top resize strip.</summary>
    private int TopResizeGripHeight => _topResizeGripHeight;

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case WmNcCalcSize when m.WParam != IntPtr.Zero:
                // Let the system compute the frame, then reclaim the caption-less top band by starting
                // the client area at the visible top edge. Left, right and bottom stay untouched.
                var windowTop = Marshal.ReadInt32(m.LParam, ClientRectTopOffset);
                base.WndProc(ref m);
                Marshal.WriteInt32(m.LParam, ClientRectTopOffset, windowTop);
                m.Result = IntPtr.Zero;
                return;

            case WmNcHitTest:
                base.WndProc(ref m);
                if (m.Result.ToInt32() == HtClient && TryHitTopResizeGrip(m.LParam, out var hitResult))
                {
                    m.Result = hitResult;
                }
                return;

            default:
                base.WndProc(ref m);
                return;
        }
    }

    /// <summary>
    /// Keeps the top edge resizable. Only points the system reports as <c>HTCLIENT</c> are remapped, so
    /// the native left/right/bottom resize borders keep working unchanged.
    /// </summary>
    private bool TryHitTopResizeGrip(IntPtr lParam, out IntPtr hitResult)
    {
        hitResult = IntPtr.Zero;

        var grip = TopResizeGripHeight;
        if (grip <= 0)
        {
            return false;
        }

        // WM_NCHITTEST packs the screen coordinates into the low/high word of lParam.
        var coordinates = lParam.ToInt64();
        var screenX = unchecked((short)(coordinates & 0xFFFF));
        var screenY = unchecked((short)((coordinates >> 16) & 0xFFFF));
        var clientPoint = PointToClient(new Point(screenX, screenY));
        if (clientPoint.Y < 0 || clientPoint.Y >= grip)
        {
            return false;
        }

        var cornerGrip = Math.Min(grip, Math.Max(1, ClientRectangle.Width / 3));
        hitResult = clientPoint.X < cornerGrip
            ? HtTopLeft
            : clientPoint.X >= ClientRectangle.Width - cornerGrip
                ? HtTopRight
                : HtTop;
        return true;
    }

    /// <summary>
    /// Reserves the invisible strip at the top of the client area. The strip uses the window's
    /// <see cref="Control.BackColor"/>, which <c>ApplyTheme</c> keeps equal to the list colour, so it
    /// cannot be distinguished from the content below it.
    /// </summary>
    private void UpdateTopResizeGrip(int dpi)
    {
        var grip = Math.Max(1, (int)Math.Round(TopResizeGripLogicalHeight * Math.Max(1, dpi) / 96d));
        if (_topResizeGripHeight == grip && Padding.Top == grip)
        {
            return;
        }

        _topResizeGripHeight = grip;
        Padding = new Padding(Padding.Left, grip, Padding.Right, Padding.Bottom);
        DrawerLogger.LogDebug($"Drawer top resize strip set to {grip} px for {dpi} dpi.");
    }
}
