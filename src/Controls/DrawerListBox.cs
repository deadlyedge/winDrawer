using System.ComponentModel;

namespace winDrawer;

/// <summary>
/// Native owner-drawn list box used by the drawer list. It keeps the colour API used by MainWindow
/// and tracks the hovered row because a plain ListBox does not raise hot-light draw states.
/// </summary>
internal sealed class DrawerListBox : ListBox
{
    private readonly AnimationClock _hoverClock = new();
    private int _hoveredIndex = -1;
    private double _hoverProgress;

    public DrawerListBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        BorderStyle = BorderStyle.None;
        IntegralHeight = false;
        DoubleBuffered = true;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor
    {
        get => BackColor;
        set => BackColor = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor { get; set; } = Color.FromArgb(155, 200, 255);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ItemSelectBackColor { get; set; } = Color.FromArgb(9, 140, 210);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ItemSelectForeColor { get; set; } = Color.White;

    public int HoveredIndex => _hoveredIndex;

    /// <summary>
    /// Highlight strength (0..1) of the hovered row. The paint path blends the fill colour with
    /// <see cref="HoverColor"/> by this value, so moving the pointer fades the row in instead of
    /// switching it on.
    /// </summary>
    public double HoverProgress => _hoverProgress;

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        UpdateHoveredIndex(IndexFromPoint(e.Location));
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        UpdateHoveredIndex(-1);
    }

    private void UpdateHoveredIndex(int index)
    {
        if (_hoveredIndex == index)
        {
            return;
        }

        var previous = _hoveredIndex;
        _hoveredIndex = index;
        InvalidateItem(previous);
        InvalidateItem(_hoveredIndex);

        if (index < 0)
        {
            // Leaving the control drops the highlight at once: there is no fading row to keep.
            _hoverClock.Cancel();
            _hoverProgress = 0d;
            return;
        }

        _hoverClock.Start(AppConstants.Ui.Animation.HoverDurationMilliseconds, progress =>
        {
            _hoverProgress = progress;
            InvalidateItem(_hoveredIndex);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hoverClock.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InvalidateItem(int index)
    {
        if (index < 0 || index >= Items.Count || !IsHandleCreated)
        {
            return;
        }

        Invalidate(GetItemRectangle(index));
    }
}
