using System.Runtime.InteropServices;
using winDrawer.Theming;

namespace winDrawer;

internal sealed class ScrollableListView : ListView
{
    private const int ScrollBarVertical = 1;
    private const int ScrollInfoRange = 0x0001;
    private const int ScrollInfoPage = 0x0002;
    private const int ScrollInfoPosition = 0x0004;
    private const int ScrollInfoMask = ScrollInfoRange | ScrollInfoPage | ScrollInfoPosition;
    private const int HorizontalScrollBarStyle = 0x00100000;
    private const int ScrollBarHorizontal = 0;
    private const int WindowStyleIndex = -16;
    private const int WindowStyleChangingMessage = 0x007C;

    private const int ListViewScrollMessage = 0x1014;
    private const int ListViewInsertItemMessage = 0x1007;
    private const int ListViewDeleteItemMessage = 0x1008;
    private const int ListViewDeleteAllItemsMessage = 0x1009;
    private const int ListViewGetItemPositionMessage = 0x1010;
    private const int ListViewGetOriginMessage = 0x1029;
    private const int ListViewGetItemSpacingMessage = 0x1033;
    private const int ListViewSetItemCountMessage = 0x102F;
    private const int WindowSizeMessage = 0x0005;
    private const int WindowStyleChangedMessage = 0x007D;
    private const int LeftButtonUpMessage = 0x0202;
    private const int MouseWheelMessage = 0x020A;
    private const int KeyDownMessage = 0x0100;
    private const int TimerMessage = 0x0113;
    private const int MouseMoveMessage = 0x0200;
    private const int MouseLeaveMessage = 0x02A3;
    private const int WindowNotifyMessage = 0x004E;
    private const int ReflectedNotifyMessage = 0x204E;
    private const int NotificationCustomDraw = -12;
    private const uint CustomDrawStagePrePaint = 0x00000001;
    private const uint CustomDrawStageItemPrePaint = 0x00010001;
    private const int CustomDrawResultDefault = 0x00000000;
    private const int CustomDrawResultNotifyItemDraw = 0x00000020;
    private const int ListViewSetExtendedStyleMessage = 0x1036;
    private const int ListViewGetExtendedStyleMessage = 0x1037;
    private const int ListViewDoubleBufferStyle = 0x00010000;

    /// <summary>How far the hover background is blended from the list background towards the highlight colour.</summary>
    private const float HoverTileBlend = 0.16f;

    private int _updateDepth;
    private bool _viewportChangedDuringUpdate;
    private bool _hasCachedIconScrollRange;
    private int _cachedIconScrollRange;
    private int _hoveredIndex = -1;
    private Color _hoverHighlightColor = Color.Empty;

    public event EventHandler? ViewportChanged;

    /// <summary>
    /// Colour the hovered item's background is blended towards. The background is supplied to the
    /// native item drawing (custom-draw <c>clrTextBk</c>), so the item layout, selection and focus
    /// visuals stay exactly as the system draws them and the highlight never lags behind a repaint.
    /// <see cref="Color.Empty"/> disables it.
    /// </summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color HoverHighlightColor
    {
        get => _hoverHighlightColor;
        set
        {
            if (_hoverHighlightColor == value)
            {
                return;
            }

            _hoverHighlightColor = value;
            InvalidateItem(_hoveredIndex);
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.Style &= ~HorizontalScrollBarStyle;
            return parameters;
        }
    }

    protected override void WndProc(ref Message m)
    {
        var message = m.Msg;
        if (message is WindowNotifyMessage or ReflectedNotifyMessage && TryApplyItemBackground(ref m))
        {
            // The custom-draw notification is answered here; passing it on would let WinForms' own
            // handler answer it as well. Windows sends it to the parent, and WinForms reflects it to
            // this control as WM_REFLECT|WM_NOTIFY.
            return;
        }

        if (message == WindowStyleChangingMessage &&
            m.WParam.ToInt32() == WindowStyleIndex &&
            m.LParam != IntPtr.Zero)
        {
            var proposedStyle = Marshal.ReadInt32(m.LParam, sizeof(int));
            Marshal.WriteInt32(m.LParam, sizeof(int), proposedStyle & ~HorizontalScrollBarStyle);
        }

        base.WndProc(ref m);

        if (message is WindowSizeMessage or WindowStyleChangedMessage or ListViewInsertItemMessage or
            ListViewDeleteItemMessage or ListViewDeleteAllItemsMessage or ListViewSetItemCountMessage or
            ListViewScrollMessage)
        {
            HideNativeScrollBars();
        }

        if (message is WindowSizeMessage or ListViewInsertItemMessage or ListViewDeleteItemMessage or
            ListViewDeleteAllItemsMessage or ListViewSetItemCountMessage)
        {
            _hasCachedIconScrollRange = false;
        }

        if (message == ListViewDeleteAllItemsMessage)
        {
            // A refreshed folder rebuilds the items; an index kept from the old list would highlight
            // whatever item happens to land on it next.
            ResetHover();
        }

        if (message is ListViewScrollMessage or WindowSizeMessage or LeftButtonUpMessage or MouseWheelMessage or
            KeyDownMessage or TimerMessage)
        {
            NotifyViewportChanged();
        }

        // Hover bookkeeping runs after the native window procedure returned, so the hit test and the
        // item geometry queries below never execute while the list view is still handling its own
        // message.
        switch (message)
        {
            case MouseMoveMessage:
                var coordinates = m.LParam.ToInt64();
                UpdateHoverFromClientPoint(
                    unchecked((short)(coordinates & 0xFFFF)),
                    unchecked((short)((coordinates >> 16) & 0xFFFF)));
                break;

            case MouseLeaveMessage:
                UpdateHoveredIndex(-1);
                break;

            case ListViewScrollMessage:
            case MouseWheelMessage:
                RefreshHoverFromPointer();
                break;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        HideNativeScrollBars();
        EnableDoubleBuffering();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible)
        {
            ResetHover();
        }
    }

    /// <summary>
    /// Turns on the native double buffering of the list view. Without it every item repaint (hover,
    /// image binding, scrolling) shows the background erase for one frame.
    /// </summary>
    private void EnableDoubleBuffering()
    {
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }

        var currentStyles = SendMessage(Handle, ListViewGetExtendedStyleMessage, IntPtr.Zero, IntPtr.Zero).ToInt64();
        var wantedStyles = currentStyles | ListViewDoubleBufferStyle;
        SendMessage(
            Handle,
            ListViewSetExtendedStyleMessage,
            new IntPtr(ListViewDoubleBufferStyle),
            new IntPtr(wantedStyles));
    }

    /// <summary>
    /// Answers the list view's custom-draw notification so the hovered item is painted with a
    /// different background by the control itself. Only <c>clrTextBk</c> is supplied; the icon, the
    /// label, the selection and the focus rectangle keep the native drawing.
    /// </summary>
    private bool TryApplyItemBackground(ref Message m)
    {
        var header = Marshal.PtrToStructure<NativeNotificationHeader>(m.LParam);
        if (header.Code != NotificationCustomDraw)
        {
            return false;
        }

        var customDraw = Marshal.PtrToStructure<NativeListCustomDraw>(m.LParam);
        switch (customDraw.DrawStage)
        {
            case CustomDrawStagePrePaint:
                // Ask for one notification per item before the control paints it.
                m.Result = new IntPtr(CustomDrawResultNotifyItemDraw);
                return true;

            case CustomDrawStageItemPrePaint:
                if (ShouldHighlightItem(customDraw.ItemIndex))
                {
                    customDraw.TextBackground = ToColorRef(
                        ThemePalette.Blend(BackColor, _hoverHighlightColor, HoverTileBlend));
                    Marshal.StructureToPtr(customDraw, m.LParam, fDeleteOld: false);
                }

                m.Result = new IntPtr(CustomDrawResultDefault);
                return true;

            default:
                m.Result = new IntPtr(CustomDrawResultDefault);
                return true;
        }
    }

    private bool ShouldHighlightItem(long itemIndex)
    {
        // Only the cached index is consulted: reading item state here would send messages to the list
        // view from inside its own custom-draw notification. A selected item keeps the native
        // selection rendering on top, so skipping the check cannot show a wrong background.
        return !_hoverHighlightColor.IsEmpty &&
               itemIndex >= 0 &&
               itemIndex == _hoveredIndex &&
               itemIndex < Items.Count;
    }

    private static uint ToColorRef(Color color)
    {
        // COLORREF is 0x00BBGGRR.
        return (uint)(color.R | (color.G << 8) | (color.B << 16));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeNotificationHeader
    {
        public IntPtr WindowFrom;
        public IntPtr IdFrom;
        public int Code;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>
    /// <c>NMLVCUSTOMDRAW</c>: <c>NMCUSTOMDRAW</c> followed by the list view's text/background colours.
    /// The trailing Vista+ fields are declared so the structure matches the size Windows passes in.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeListCustomDraw
    {
        public NativeNotificationHeader Header;
        public uint DrawStage;
        public IntPtr Hdc;
        public NativeRectangle Bounds;
        public IntPtr ItemSpec;
        public uint ItemState;
        public IntPtr ItemLParam;
        public uint TextColor;
        public uint TextBackground;
        public int SubItem;
        public uint ItemType;
        public uint FaceColor;
        public int IconEffect;
        public int IconPhase;
        public int PartId;
        public int StateId;
        public NativeRectangle TextBounds;
        public int Align;

        /// <summary>Item index reported for <c>CDDS_ITEMPREPAINT</c>.</summary>
        public long ItemIndex => (long)ItemSpec;
    }

    private void HideNativeScrollBars()
    {
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }
        ShowScrollBar(Handle, ScrollBarHorizontal, false);
    }

    private void UpdateHoverFromClientPoint(int x, int y)
    {
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }

        var hit = HitTest(x, y);
        UpdateHoveredIndex(hit.Item?.Index ?? -1);
    }

    private void RefreshHoverFromPointer()
    {
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }

        var point = PointToClient(Cursor.Position);
        UpdateHoverFromClientPoint(point.X, point.Y);
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
    }

    private void ResetHover()
    {
        _hoveredIndex = -1;
    }

    private void InvalidateItem(int index)
    {
        if (index < 0 || index >= Items.Count || !IsHandleCreated)
        {
            return;
        }

        Invalidate(Items[index].Bounds);
    }

    public new void BeginUpdate()
    {
        _updateDepth++;
        base.BeginUpdate();
    }

    public new void EndUpdate()
    {
        if (_updateDepth == 0)
        {
            base.EndUpdate();
            return;
        }

        _updateDepth--;
        base.EndUpdate();

        if (_updateDepth == 0 && _viewportChangedDuringUpdate)
        {
            _viewportChangedDuringUpdate = false;
            ViewportChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool TryGetVerticalScrollInfo(out int maximumPosition, out int position, out int viewportSize)
    {
        maximumPosition = 0;
        position = 0;
        viewportSize = 0;

        if (!IsHandleCreated || IsDisposed)
        {
            return false;
        }

        var info = new NativeScrollInfo
        {
            Size = Marshal.SizeOf<NativeScrollInfo>(),
            Mask = ScrollInfoMask
        };

        if (GetScrollInfo(Handle, ScrollBarVertical, ref info))
        {
            var pageAdjustment = info.Page > 0 ? (long)info.Page - 1 : 0;
            viewportSize = (int)Math.Clamp((long)info.Page, 1, int.MaxValue);
            var maximumNativePosition = Math.Max(info.Minimum, (long)info.Maximum - pageAdjustment);
            var scrollableRange = Math.Max(0, maximumNativePosition - info.Minimum);
            maximumPosition = (int)Math.Min(scrollableRange, int.MaxValue);
            position = (int)Math.Clamp((long)info.Position - info.Minimum, 0, maximumPosition);
            return true;
        }

        if (View is not (View.LargeIcon or View.SmallIcon))
        {
            return false;
        }

        position = GetIconScrollPosition();
        viewportSize = Math.Max(1, ClientSize.Height);
        maximumPosition = Math.Max(position, GetMaximumIconScrollPosition(viewportSize));
        position = Math.Min(position, maximumPosition);
        return true;
    }

    public void ScrollToVerticalPosition(int position)
    {
        if (!TryGetVerticalScrollInfo(out var maximumPosition, out var currentPosition, out _))
        {
            return;
        }

        var targetPosition = Math.Clamp(position, 0, maximumPosition);
        var delta = targetPosition - currentPosition;
        if (delta == 0)
        {
            return;
        }

        SendMessage(Handle, ListViewScrollMessage, IntPtr.Zero, new IntPtr(delta));
        NotifyViewportChanged();
    }

    /// <summary>
    /// Reports the index range that intersects the visible area. The range is derived from the item
    /// rectangles, so it is exact for the current layout but costs one native query per item; callers
    /// use it after a short debounce instead of on every scroll message.
    /// </summary>
    public bool TryGetVisibleItemRange(out int firstIndex, out int lastIndex)
    {
        firstIndex = -1;
        lastIndex = -1;

        if (IsDisposed || Disposing || !IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0 || Items.Count == 0)
        {
            return false;
        }

        var viewport = ClientRectangle;
        for (var index = 0; index < Items.Count; index++)
        {
            var itemBounds = GetItemRect(index);
            if (!itemBounds.IntersectsWith(viewport))
            {
                continue;
            }

            if (firstIndex < 0)
            {
                firstIndex = index;
            }

            lastIndex = index;
        }

        return firstIndex >= 0;
    }

    private int GetIconScrollPosition()
    {
        var origin = new NativePoint();
        if (SendMessage(Handle, ListViewGetOriginMessage, IntPtr.Zero, ref origin) == IntPtr.Zero)
        {
            return 0;
        }

        return Math.Max(0, -origin.Y);
    }

    private int GetMaximumIconScrollPosition(int viewportSize)
    {
        if (_hasCachedIconScrollRange)
        {
            return _cachedIconScrollRange;
        }

        _hasCachedIconScrollRange = true;
        _cachedIconScrollRange = 0;
        if (Items.Count == 0)
        {
            return _cachedIconScrollRange;
        }

        var spacing = SendMessage(Handle, ListViewGetItemSpacingMessage, IntPtr.Zero, IntPtr.Zero).ToInt64();
        var itemHeight = Math.Max(1, (int)((spacing >> 16) & 0xFFFF));
        var scrollPosition = GetIconScrollPosition();
        long contentBottom = 0;

        for (var index = 0; index < Items.Count; index++)
        {
            var itemPosition = new NativePoint();
            if (SendMessage(Handle, ListViewGetItemPositionMessage, new IntPtr(index), ref itemPosition) != IntPtr.Zero)
            {
                // LVM_GETITEMPOSITION is viewport-relative in icon view. Add the current origin so the
                // computed content extent stays stable while scrolling.
                contentBottom = Math.Max(contentBottom, (long)itemPosition.Y + itemHeight + scrollPosition);
            }
        }

        var range = Math.Max(0, contentBottom - viewportSize);
        _cachedIconScrollRange = (int)Math.Min(range, int.MaxValue);
        return _cachedIconScrollRange;
    }

    private void NotifyViewportChanged()
    {
        if (_updateDepth > 0)
        {
            _viewportChangedDuringUpdate = true;
            return;
        }

        ViewportChanged?.Invoke(this, EventArgs.Empty);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeScrollInfo
    {
        public int Size;
        public int Mask;
        public int Minimum;
        public int Maximum;
        public uint Page;
        public int Position;
        public int TrackPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetScrollInfo(IntPtr windowHandle, int bar, ref NativeScrollInfo scrollInfo);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowScrollBar(IntPtr windowHandle, int bar, [MarshalAs(UnmanagedType.Bool)] bool show);


    [DllImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr wParam, ref NativePoint lParam);
}
