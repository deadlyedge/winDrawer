using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace winDrawer;

/// <summary>
/// Thin owner-drawn vertical scrollbar used by the drawer content view. It replaces the native
/// scrollbar so the dark theme does not show the system scrollbar chrome.
/// </summary>
internal sealed class ThemedScrollBar : Control
{
    private int _maximum;
    private int _value;
    private int _largeChange = 1;
    private int _smallChange = 1;
    private int _fillWidth = 3;
    private bool _dragging;
    private bool _hoverThumb;
    private bool _pressedThumb;
    private int _dragOffset;

    public ThemedScrollBar()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.ScrollBar;
        Width = 18;
    }

    public event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            var maximum = Math.Max(0, value);
            if (_maximum == maximum)
            {
                return;
            }

            _maximum = maximum;
            if (_value > _maximum)
            {
                SetValue(_maximum, raiseEvent: true);
            }
            else
            {
                Invalidate();
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set => SetValue(Math.Clamp(value, 0, _maximum), raiseEvent: true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int LargeChange
    {
        get => _largeChange;
        set
        {
            var largeChange = Math.Max(1, value);
            if (_largeChange == largeChange)
            {
                return;
            }

            _largeChange = largeChange;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SmallChange
    {
        get => _smallChange;
        set => _smallChange = Math.Max(1, value);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int FillWidth
    {
        get => _fillWidth;
        set
        {
            var fillWidth = Math.Max(2, value);
            if (_fillWidth == fillWidth)
            {
                return;
            }

            _fillWidth = fillWidth;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor { get; set; } = Color.FromArgb(56, 56, 56);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ThumbColor { get; set; } = Color.FromArgb(110, 110, 110);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor { get; set; } = Color.FromArgb(150, 150, 150);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color PressColor { get; set; } = Color.FromArgb(190, 190, 190);

    protected override void OnPaint(PaintEventArgs e)
    {
        var bounds = ClientRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        using (var backgroundBrush = new SolidBrush(FillColor))
        {
            e.Graphics.FillRectangle(backgroundBrush, bounds);
        }

        var thumbBounds = GetThumbBounds();
        if (thumbBounds.Width <= 0 || thumbBounds.Height <= 0)
        {
            return;
        }

        var thumbColor = _pressedThumb || _dragging
            ? PressColor
            : _hoverThumb
                ? HoverColor
                : ThumbColor;
        using var thumbPath = CreateRoundedRectangle(thumbBounds, 4);
        using var thumbBrush = new SolidBrush(thumbColor);
        e.Graphics.FillPath(thumbBrush, thumbPath);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _maximum <= 0)
        {
            return;
        }

        Focus();
        var thumbBounds = GetThumbBounds();
        if (!thumbBounds.Contains(e.Location))
        {
            Value += e.Y < thumbBounds.Top ? -_largeChange : _largeChange;
            return;
        }

        _dragging = true;
        _pressedThumb = true;
        _dragOffset = e.Y - thumbBounds.Top;
        Capture = true;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            SetValueFromThumbY(e.Y);
            return;
        }

        var hoverThumb = GetThumbBounds().Contains(e.Location);
        if (_hoverThumb == hoverThumb)
        {
            return;
        }

        _hoverThumb = hoverThumb;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragging = false;
        _pressedThumb = false;
        Capture = false;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_dragging)
        {
            return;
        }

        _hoverThumb = false;
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_maximum <= 0 || e.Delta == 0)
        {
            return;
        }

        var step = Math.Max(_smallChange, _largeChange / 3);
        Value -= Math.Sign(e.Delta) * step;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.Up:
                Value -= _smallChange;
                e.Handled = true;
                break;
            case Keys.Down:
                Value += _smallChange;
                e.Handled = true;
                break;
            case Keys.PageUp:
                Value -= _largeChange;
                e.Handled = true;
                break;
            case Keys.PageDown:
                Value += _largeChange;
                e.Handled = true;
                break;
            case Keys.Home:
                Value = 0;
                e.Handled = true;
                break;
            case Keys.End:
                Value = _maximum;
                e.Handled = true;
                break;
        }
    }

    private void SetValueFromThumbY(int y)
    {
        var trackBounds = GetTrackBounds();
        var thumbBounds = GetThumbBounds();
        var travel = trackBounds.Height - thumbBounds.Height;
        if (travel <= 0 || _maximum <= 0)
        {
            return;
        }

        var thumbTop = Math.Clamp(y - _dragOffset, trackBounds.Top, trackBounds.Top + travel);
        var ratio = (thumbTop - trackBounds.Top) / (double)travel;
        Value = (int)Math.Round(ratio * _maximum);
    }

    private void SetValue(int value, bool raiseEvent)
    {
        if (_value == value)
        {
            return;
        }

        _value = value;
        Invalidate();
        if (raiseEvent)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private Rectangle GetTrackBounds()
    {
        var width = Math.Min(_fillWidth, Math.Max(1, ClientSize.Width));
        var left = Math.Max(0, (ClientSize.Width - width) / 2);
        return new Rectangle(left, 2, width, Math.Max(1, ClientSize.Height - 4));
    }

    private Rectangle GetThumbBounds()
    {
        var trackBounds = GetTrackBounds();
        var contentExtent = (double)_maximum + _largeChange;
        var ratio = contentExtent <= 0 ? 1 : _largeChange / contentExtent;
        var minimumThumbHeight = Math.Max(18, (int)Math.Round(24 * DeviceDpi / 96d));
        var thumbHeight = Math.Clamp(
            (int)Math.Round(trackBounds.Height * ratio),
            Math.Min(minimumThumbHeight, trackBounds.Height),
            trackBounds.Height);
        var travel = Math.Max(0, trackBounds.Height - thumbHeight);
        var thumbTop = _maximum <= 0
            ? trackBounds.Top
            : trackBounds.Top + (int)Math.Round(travel * (_value / (double)_maximum));
        return new Rectangle(trackBounds.Left, thumbTop, trackBounds.Width, thumbHeight);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

