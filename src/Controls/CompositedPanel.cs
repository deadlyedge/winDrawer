namespace winDrawer;

/// <summary>
/// Host container that enables <c>WS_EX_COMPOSITED</c> for its children. The native drawer list
/// cannot be double-buffered through WinForms (<c>DoubleBuffered</c> only affects controls that paint
/// through <c>UserPaint</c>), so a single item invalidation made it repaint the whole client and
/// flash. Compositing paints the fixed list content of this container off-screen first.
/// </summary>
internal sealed class CompositedPanel : Panel
{
    private const int WsExComposited = 0x02000000;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExComposited;
            return parameters;
        }
    }
}
