using winDrawer.Configuration;

namespace winDrawer
{
    /// <summary>
    /// Drag-to-reorder behavior for the drawer list: press tracking, the insertion marker and the
    /// persistence call. Kept separate from painting and selection so the reorder state machine stays
    /// in one place.
    /// </summary>
    public partial class MainWindow : Form
    {
        private bool _drawerPointerDown;
        private bool _isDrawerReordering;
        private bool _drawerDragStarted;
        private int _drawerPressedIndex = -1;
        private Point _drawerPressPoint;
        private int _dropInsertionIndex = -1;
        private int _dropRawInsertionIndex = -1;

        private readonly Panel _drawerInsertionMarker = new()
        {
            Height = 3,
            Visible = false,
            Enabled = false
        };

        private void DrawerListBox_MouseDown(object? sender, MouseEventArgs e)
        {
            _drawerDragStarted = false;
            var index = drawerListBox.IndexFromPoint(e.Location);
            if (e.Button == MouseButtons.Right)
            {
                _drawerPointerDown = false;
                _drawerPressedIndex = -1;
                if (index >= 0 && index < _currentDrawers.Count)
                {
                    ShowDrawerSettings(index);
                }
                return;
            }

            if (e.Button != MouseButtons.Left || index < 0 || index >= _currentDrawers.Count)
            {
                _drawerPointerDown = false;
                _drawerPressedIndex = -1;
                return;
            }

            _drawerPointerDown = true;
            _drawerPressedIndex = index;
            _drawerPressPoint = e.Location;
        }

        private void DrawerListBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_drawerPointerDown || _drawerPressedIndex < 0 || e.Button != MouseButtons.Left)
            {
                return;
            }

            var dragBounds = new Rectangle(
                _drawerPressPoint.X - SystemInformation.DragSize.Width / 2,
                _drawerPressPoint.Y - SystemInformation.DragSize.Height / 2,
                SystemInformation.DragSize.Width,
                SystemInformation.DragSize.Height);
            if (dragBounds.Contains(e.Location))
            {
                return;
            }

            _drawerDragStarted = true;
            _isDrawerReordering = true;
            try
            {
                drawerListBox.DoDragDrop(_currentDrawers[_drawerPressedIndex].Path, DragDropEffects.Move);
            }
            finally
            {
                _isDrawerReordering = false;
                _drawerPointerDown = false;
                _drawerPressedIndex = -1;
            }
        }

        private void DrawerListBox_MouseUp(object? sender, MouseEventArgs e)
        {
            var sourceIndex = _drawerPressedIndex;
            var wasDrag = _drawerDragStarted;
            _drawerPointerDown = false;
            _drawerPressedIndex = -1;
            _drawerDragStarted = false;

            if (e.Button == MouseButtons.Left && !wasDrag && sourceIndex >= 0 && sourceIndex < _currentDrawers.Count &&
                drawerListBox.IndexFromPoint(e.Location) == sourceIndex)
            {
                OpenOrToggleDrawer(sourceIndex);
            }
        }

        private void DrawerListBox_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(typeof(string)) == true ? DragDropEffects.Move : DragDropEffects.None;
            UpdateDragInsertionMarker(e);
        }

        private void DrawerListBox_DragOver(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(typeof(string)) == true ? DragDropEffects.Move : DragDropEffects.None;
            UpdateDragInsertionMarker(e);
        }

        private void DrawerListBox_DragLeave(object? sender, EventArgs e)
        {
            ClearDragInsertionMarker();
        }

        private void UpdateDragInsertionMarker(DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(string)) is not string draggedPath || _currentDrawers.Count == 0)
            {
                ClearDragInsertionMarker();
                return;
            }

            var sourceIndex = _currentDrawers.FindIndex(drawer => PathComparison.AreSame(drawer.Path, draggedPath));
            var point = drawerListBox.PointToClient(new Point(e.X, e.Y));
            var rawInsertionIndex = GetRawDropInsertionIndex(point);
            if (sourceIndex < 0 || rawInsertionIndex < 0)
            {
                ClearDragInsertionMarker();
                return;
            }

            _dropRawInsertionIndex = rawInsertionIndex;
            _dropInsertionIndex = Math.Clamp(
                rawInsertionIndex > sourceIndex ? rawInsertionIndex - 1 : rawInsertionIndex,
                0,
                _currentDrawers.Count - 1);

            var markerY = rawInsertionIndex < _currentDrawers.Count
                ? drawerListBox.GetItemRectangle(rawInsertionIndex).Top
                : drawerListBox.GetItemRectangle(_currentDrawers.Count - 1).Bottom;
            var markerHeight = Math.Max(2, DpiScale.Pixels(3, DeviceDpi));
            _drawerInsertionMarker.SetBounds(4, markerY - markerHeight / 2, Math.Max(1, drawerListBox.ClientSize.Width - 8), markerHeight);
            _drawerInsertionMarker.BackColor = _currentTheme.Accent;
            _drawerInsertionMarker.Visible = true;
            _drawerInsertionMarker.BringToFront();
        }

        private int GetRawDropInsertionIndex(Point point)
        {
            var itemIndex = drawerListBox.IndexFromPoint(point);
            if (itemIndex < 0)
            {
                if (point.Y < drawerListBox.GetItemRectangle(0).Top)
                {
                    return 0;
                }

                return point.Y > drawerListBox.ClientSize.Height ? _currentDrawers.Count : -1;
            }

            var itemBounds = drawerListBox.GetItemRectangle(itemIndex);
            return point.Y >= itemBounds.Top + itemBounds.Height / 2 ? itemIndex + 1 : itemIndex;
        }

        private void ClearDragInsertionMarker()
        {
            _dropInsertionIndex = -1;
            _dropRawInsertionIndex = -1;
            _drawerInsertionMarker.Visible = false;
        }

        private void DrawerListBox_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(string)) is not string draggedPath)
            {
                ClearDragInsertionMarker();
                return;
            }

            var sourceIndex = _currentDrawers.FindIndex(drawer => PathComparison.AreSame(drawer.Path, draggedPath));
            if (sourceIndex < 0 || _dropInsertionIndex < 0)
            {
                ClearDragInsertionMarker();
                return;
            }

            var destinationIndex = _dropInsertionIndex;
            if (destinationIndex == sourceIndex)
            {
                ClearDragInsertionMarker();
                return;
            }

            var reorderedDrawers = new List<DrawerDefinition>(_currentDrawers);
            var movedDrawer = reorderedDrawers[sourceIndex];
            reorderedDrawers.RemoveAt(sourceIndex);
            reorderedDrawers.Insert(destinationIndex, movedDrawer);

            try
            {
                var orderedPaths = reorderedDrawers.Select(drawer => drawer.Path).ToList();
                if (!_settingsRepository.ReorderDrawers(orderedPaths, out var error))
                {
                    DrawerLogger.LogWarning($"Drawer reorder was rejected: {error}");
                    UiMessages.ShowWarning(this, "Reorder Drawers", error);
                    ReloadSettingsFromStorage();
                    return;
                }

                ApplySettingsSnapshot(_settingsRepository.GetSnapshot());
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Failed to reorder drawers.", ex);
                UiMessages.ShowError(this, "Reorder Drawers", $"Failed to save drawer order: {ex.Message}");
                ReloadSettingsFromStorage();
            }
            finally
            {
                ClearDragInsertionMarker();
            }
        }
    }
}
