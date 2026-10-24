namespace winDrawer;

/// <summary>
/// Single-line label used for the drawer folder path.
///
/// <see cref="Label"/> with <c>AutoEllipsis = true</c> still word-wraps when the control is tall
/// enough: the ellipsis simply lands at the end of the last visible line, so a long path in a
/// narrow drawer breaks into several lines. Drawing the text ourselves with
/// <see cref="TextFormatFlags.SingleLine"/> guarantees exactly one line plus a trailing ellipsis at
/// any DPI and for any text content.
/// </summary>
internal sealed class EllipsisLabel : Label
{
    public EllipsisLabel()
    {
        AutoSize = false;
        AutoEllipsis = false;
        DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // The background is painted by Control.OnPaintBackground (including Color.Transparent);
        // base.OnPaint would draw the wrapped text, so it is intentionally not called.
        var textBounds = Rectangle.FromLTRB(
            Padding.Left,
            Padding.Top,
            Math.Max(Padding.Left + 1, ClientSize.Width - Padding.Right),
            Math.Max(Padding.Top + 1, ClientSize.Height - Padding.Bottom));

        var flags = TextFormatFlags.SingleLine |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding |
                    TextFormatFlags.NoPrefix |
                    GetHorizontalAlignment(TextAlign);

        if (RightToLeft == RightToLeft.Yes)
        {
            flags |= TextFormatFlags.RightToLeft;
        }

        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, ForeColor, flags);
    }

    private static TextFormatFlags GetHorizontalAlignment(ContentAlignment alignment)
    {
        return alignment switch
        {
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter =>
                TextFormatFlags.HorizontalCenter,
            ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight =>
                TextFormatFlags.Right,
            _ => TextFormatFlags.Left
        };
    }
}
