using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer
{
    public partial class MainWindow : Form
    {
        private void PopulateDrawerList(IEnumerable<DrawerDefinition> drawers)
        {
            drawerListBox.Items.Clear();
            _currentDrawers.Clear();

            if (drawers != null)
            {
                foreach (var drawer in drawers)
                {
                    if (drawer == null)
                    {
                        continue;
                    }

                    var displayName = string.IsNullOrWhiteSpace(drawer.Name)
                        ? "(unnamed drawer)"
                        : drawer.Name;

                    _currentDrawers.Add(drawer);
                    var itemLabel = Directory.Exists(drawer.Path)
                        ? displayName
                        : $"{displayName} (missing)";
                    drawerListBox.Items.Add(drawer.OverlayColor is { } overlayColor
                        ? new DrawerListItem(itemLabel, overlayColor)
                        : itemLabel);
                }
            }

            if (_currentDrawers.Count == 0)
            {
                drawerListBox.Items.Add("No drawers configured");
                drawerListBox.Enabled = false;
            }
            else
            {
                drawerListBox.Enabled = true;
            }

            _suppressSelectionChange = true;
            drawerListBox.SelectedIndex = -1;
            _suppressSelectionChange = false;
            ClearDragInsertionMarker();
            DrawerLogger.LogInformation($"Drawer list populated with {_currentDrawers.Count} entries.");
        }

        private sealed record DrawerListItem(string Text, Color OverlayColor)
        {
            public override string ToString() => Text;
        }

        private void DrawerListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= drawerListBox.Items.Count)
            {
                return;
            }

            var bounds = e.Bounds;
            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var background = drawerListBox.FillColor;
            var foreground = drawerListBox.ForeColor;
            if (selected)
            {
                background = drawerListBox.ItemSelectBackColor;
                foreground = drawerListBox.ItemSelectForeColor;
            }
            else if (drawerListBox.HoveredIndex == e.Index)
            {
                background = drawerListBox.HoverColor;
            }

            var item = drawerListBox.Items[e.Index];
            if (item is DrawerListItem coloredDrawerItem)
            {
                background = ThemePalette.BlendOverlayColor(background, coloredDrawerItem.OverlayColor);
            }

            using var backgroundBrush = new SolidBrush(background);
            e.Graphics.FillRectangle(backgroundBrush, bounds);

            var text = item is DrawerListItem drawerItem
                ? drawerItem.Text
                : drawerListBox.GetItemText(item);
            bounds.Inflate(-8, 0);
            TextRenderer.DrawText(
                e.Graphics,
                text,
                drawerListBox.Font,
                bounds,
                foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            e.DrawFocusRectangle();
        }

        private void OpenOrToggleDrawer(int selectedIndex)
        {
            if (_suppressSelectionChange || _isDrawerReordering)
            {
                return;
            }

            DrawerLogger.LogDebug($"Drawer list selection changed to index {selectedIndex}.");
            if (selectedIndex < 0 || selectedIndex >= _currentDrawers.Count)
            {
                return;
            }

            var selectedDrawer = _currentDrawers[selectedIndex];
            if (_currentOpenDrawer != null && string.Equals(_currentOpenDrawer.Path, selectedDrawer.Path, StringComparison.OrdinalIgnoreCase))
            {
                DrawerLogger.LogInformation($"Closing drawer '{selectedDrawer.Name}' via toggle.");
                // 点击同一个drawer，切换：关闭
                _currentDrawerWindow?.Close();
                _currentDrawerWindow = null;
                _currentOpenDrawer = null;
                _suppressSelectionChange = true;
                drawerListBox.SelectedIndex = -1; // 立即清空选中状态
                _suppressSelectionChange = false;
            }
            else
            {
                DrawerLogger.LogInformation($"Opening drawer '{selectedDrawer.Name}'.");
                // 点击不同drawer或没有打开的，关闭当前的（如果有），打开新的
                _suppressSelectionChange = true;
                drawerListBox.SelectedIndex = -1; // 立即清空旧的选中状态
                _suppressSelectionChange = false;
                if (_currentDrawerWindow != null)
                {
                    _currentDrawerWindow.Close();
                }
                OpenDrawer(selectedDrawer);
                _currentOpenDrawer = selectedDrawer;
                _suppressSelectionChange = true;
                drawerListBox.SelectedIndex = selectedIndex; // 设置新的选中状态
                _suppressSelectionChange = false;
            }
        }

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
        private void UpdateSelectedDrawer()
        {
            _suppressSelectionChange = true;
            try
            {
                if (_currentOpenDrawer != null)
                {
                    for (int i = 0; i < _currentDrawers.Count; i++)
                    {
                        if (PathComparison.AreSame(_currentDrawers[i].Path, _currentOpenDrawer.Path))
                        {
                            drawerListBox.SelectedIndex = i;
                            return;
                        }
                    }
                }
                else
                {
                    drawerListBox.SelectedIndex = -1;
                }
            }
            finally
            {
                _suppressSelectionChange = false;
            }
        }
    }
}
