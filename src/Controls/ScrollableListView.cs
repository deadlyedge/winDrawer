using System.Runtime.InteropServices;

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

    private int _updateDepth;
    private bool _viewportChangedDuringUpdate;
    private bool _hasCachedIconScrollRange;
    private int _cachedIconScrollRange;

    public event EventHandler? ViewportChanged;

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

        if (message is ListViewScrollMessage or WindowSizeMessage or LeftButtonUpMessage or MouseWheelMessage or
            KeyDownMessage or TimerMessage)
        {
            NotifyViewportChanged();
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        HideNativeScrollBars();
    }

    private void HideNativeScrollBars()
    {
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }
        ShowScrollBar(Handle, ScrollBarHorizontal, false);
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
