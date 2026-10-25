using System.ComponentModel;

namespace winDrawer;

/// <summary>
/// Small owner-drawn panel used by the settings dialogs for a clickable colour preview.
/// Native Panel does not render its Text, so this keeps the existing FillColor/RectColor API while
/// using only WinForms primitives.
/// </summary>
internal sealed class ColorPreviewPanel : Panel
{
    private Color _fillColor = Color.Black;
    private Color _borderColor = Color.Gray;
    private ContentAlignment _textAlignment = ContentAlignment.MiddleCenter;

    public ColorPreviewPanel()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            if (_fillColor == value)
            {
                return;
            }

            _fillColor = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color RectColor
    {
        get => _borderColor;
        set
        {
            if (_borderColor == value)
            {
                return;
            }

            _borderColor = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ContentAlignment TextAlignment
    {
        get => _textAlignment;
        set
        {
            if (_textAlignment == value)
            {
                return;
            }

            _textAlignment = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var clientBounds = ClientRectangle;
        if (clientBounds.Width <= 0 || clientBounds.Height <= 0)
        {
            return;
        }

        using (var fillBrush = new SolidBrush(_fillColor))
        {
            e.Graphics.FillRectangle(fillBrush, clientBounds);
        }

        using (var borderPen = new Pen(_borderColor))
        {
            e.Graphics.DrawRectangle(borderPen, 0, 0, clientBounds.Width - 1, clientBounds.Height - 1);
        }

        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        var textBounds = DisplayRectangle;
        if (textBounds.Width <= 0 || textBounds.Height <= 0)
        {
            return;
        }

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textBounds,
            ForeColor,
            GetTextFormatFlags());
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        Invalidate();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        Invalidate();
    }

    protected override void OnPaddingChanged(EventArgs e)
    {
        base.OnPaddingChanged(e);
        Invalidate();
    }

    private TextFormatFlags GetTextFormatFlags()
    {
        var flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;
        return _textAlignment switch
        {
            ContentAlignment.TopLeft => flags | TextFormatFlags.Left | TextFormatFlags.Top,
            ContentAlignment.TopCenter => flags | TextFormatFlags.HorizontalCenter | TextFormatFlags.Top,
            ContentAlignment.TopRight => flags | TextFormatFlags.Right | TextFormatFlags.Top,
            ContentAlignment.MiddleLeft => flags | TextFormatFlags.Left | TextFormatFlags.VerticalCenter,
            ContentAlignment.MiddleCenter => flags | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
            ContentAlignment.MiddleRight => flags | TextFormatFlags.Right | TextFormatFlags.VerticalCenter,
            ContentAlignment.BottomLeft => flags | TextFormatFlags.Left | TextFormatFlags.Bottom,
            ContentAlignment.BottomCenter => flags | TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom,
            ContentAlignment.BottomRight => flags | TextFormatFlags.Right | TextFormatFlags.Bottom,
            _ => flags | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
        };
    }
}
