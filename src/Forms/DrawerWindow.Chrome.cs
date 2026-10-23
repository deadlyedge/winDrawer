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
///
/// Because the drawer's <c>Opacity</c> is normally below 100%, it is a layered window. In that mode
/// user32 paints the non-client frame into the same surface as the client area, so the reclaimed strip
/// is repainted after every <c>WM_NCPAINT</c>/<c>WM_NCACTIVATE</c>; without that the 13 px band came back
/// as soon as the drawer was activated (showing the frame colour or the desktop behind it).
/// </summary>
public partial class DrawerWindow
{
    private const int WmNcCalcSize = 0x0083;
    private const int WmNcPaint = 0x0085;
    private const int WmNcActivate = 0x0086;
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
            case WmNcCalcSize:
                ApplyCaptionlessTopInset(ref m);
                return;

            case WmNcPaint:
            case WmNcActivate:
                base.WndProc(ref m);
                RepaintTopResizeGrip();
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
    /// Reclaims the caption-less top band for both variants of <c>WM_NCCALCSIZE</c>.
    ///
    /// <para><c>wParam = TRUE</c> (resize/move) points <c>lParam</c> at <c>NCCALCSIZE_PARAMS</c>;
    /// <c>wParam = FALSE</c> (activation, theme, DPI or any other frame recalculation) points it at a
    /// plain <c>RECT</c>. In both variants the rectangle starts as the proposed <em>window</em> rectangle
    /// and must end as the <em>client</em> rectangle, and its <c>top</c> field sits at the same offset, so
    /// the same adjustment works for both.</para>
    ///
    /// <para>Handling only the resize variant would let the top band reappear whenever Windows
    /// recomputed the frame with the plain <c>RECT</c> variant (theme or DPI changes and other
    /// frame recalculations): <c>DefWindowProc</c> restores the caption-less top inset and the
    /// frame paints over the reclaimed pixels. Activation changes on this Windows build do not send
    /// that variant; the layered-window repaint in <see cref="RepaintTopResizeGrip"/> covers
    /// them.</para>
    ///
    /// <para>Left, right and bottom keep the values computed by the system.</para>
    /// </summary>
    private void ApplyCaptionlessTopInset(ref Message m)
    {
        if (m.LParam == IntPtr.Zero)
        {
            base.WndProc(ref m);
            return;
        }

        var windowTop = Marshal.ReadInt32(m.LParam, ClientRectTopOffset);
        base.WndProc(ref m);
        Marshal.WriteInt32(m.LParam, ClientRectTopOffset, windowTop);
        m.Result = IntPtr.Zero;
    }

    /// <summary>
    /// Repaints the reclaimed strip after user32 painted the non-client frame.
    ///
    /// <para><c>Opacity</c> below 100% makes the window layered, and layered windows receive the
    /// non-client frame paint in the same surface as the client area. Skipping this repaint left the
    /// strip unpainted after activation changes, which looked like the old 13 px frame band coming
    /// back. Repainting keeps only the normal window border above the content.</para>
    /// </summary>
    private void RepaintTopResizeGrip()
    {
        if (!IsHandleCreated || IsDisposed || _topResizeGripHeight <= 0)
        {
            return;
        }

        var width = ClientSize.Width;
        var height = Math.Min(_topResizeGripHeight, ClientSize.Height);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        Invalidate(new Rectangle(0, 0, width, height));
        Update();
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
