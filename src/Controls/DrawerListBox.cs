using System.ComponentModel;

namespace winDrawer;

/// <summary>
/// Native owner-drawn list box used by the drawer list. It keeps the colour API used by MainWindow
/// and tracks the hovered row because a plain ListBox does not raise hot-light draw states.
/// </summary>
internal sealed class DrawerListBox : ListBox
{
    private int _hoveredIndex = -1;

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

        InvalidateItem(_hoveredIndex);
        _hoveredIndex = index;
        InvalidateItem(_hoveredIndex);
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
