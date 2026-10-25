namespace winDrawer;

/// <summary>
/// Lets a borderless dialog be moved by dragging its header controls. The offset is measured in
/// screen coordinates, so the window does not jump when the handle does not start at the client
/// origin. The behaviour lives as long as the controls keep their event subscriptions.
/// </summary>
internal sealed class FormDragBehavior
{
    private readonly Form _form;
    private bool _isDragging;
    private Point _dragOffset;

    private FormDragBehavior(Form form, Control[] handles)
    {
        _form = form ?? throw new ArgumentNullException(nameof(form));

        foreach (var handle in handles)
        {
            handle.Cursor = Cursors.SizeAll;
            handle.MouseDown += HandleMouseDown;
            handle.MouseMove += HandleMouseMove;
            handle.MouseUp += HandleMouseUp;
        }
    }

    public static void Attach(Form form, params Control[] handles)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(handles);
        _ = new FormDragBehavior(form, handles);
    }

    private void HandleMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || sender is not Control handle)
        {
            return;
        }

        _isDragging = true;
        handle.Capture = true;

        var screenPoint = handle.PointToScreen(e.Location);
        _dragOffset = new Point(screenPoint.X - _form.Left, screenPoint.Y - _form.Top);
    }

    private void HandleMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDragging || sender is not Control handle)
        {
            return;
        }

        var screenPoint = handle.PointToScreen(e.Location);
        _form.Location = new Point(screenPoint.X - _dragOffset.X, screenPoint.Y - _dragOffset.Y);
    }

    private void HandleMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _isDragging = false;
        if (sender is Control handle)
        {
            handle.Capture = false;
        }
    }
}
