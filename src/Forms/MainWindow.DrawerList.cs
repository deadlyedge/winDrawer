using winDrawer.Configuration;

namespace winDrawer
{
    public partial class MainWindow : Form
    {
        private void PopulateDrawerList(IEnumerable<DrawerDefinition> drawers)
        {
            drawerListBox.BeginUpdate();
            try
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

                        _currentDrawers.Add(drawer);
                        drawerListBox.Items.Add(CreateDrawerListItem(drawer));
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
            }
            finally
            {
                drawerListBox.EndUpdate();
            }

            ClearDragInsertionMarker();
            DrawerLogger.LogInformation($"Drawer list populated with {_currentDrawers.Count} entries.");
        }

        /// <summary>
        /// Whether the new snapshot would produce exactly the same list rows. Size and sort mode are
        /// intentionally ignored: neither is shown here, and rebuilding for them caused a visible
        /// flash every time a drawer closed (the window persists its size on close).
        /// </summary>
        private static bool HasSameDrawerPresentation(
            IReadOnlyList<DrawerDefinition> current,
            IReadOnlyList<DrawerDefinition> next)
        {
            if (current.Count != next.Count)
            {
                return false;
            }

            for (var index = 0; index < current.Count; index++)
            {
                var left = current[index];
                var right = next[index];
                if (!PathComparison.AreSame(left.Path, right.Path) ||
                    !string.Equals(left.Name, right.Name, StringComparison.Ordinal) ||
                    left.OverlayColor != right.OverlayColor ||
                    Directory.Exists(left.Path) != Directory.Exists(right.Path))
                {
                    return false;
                }
            }

            return true;
        }

        private void ReplaceCurrentDrawers(IReadOnlyList<DrawerDefinition> drawers)
        {
            _currentDrawers.Clear();
            _currentDrawers.AddRange(drawers);
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
                ClearDrawerListSelection();
                return;
            }

            DrawerLogger.LogInformation($"Opening drawer '{selectedDrawer.Name}'.");
            // Keep the clicked row selected for the whole open/switch operation. Clearing it here (or
            // letting the old window's FormClosed handler clear it) made the row fall back to its
            // normal overlay colour for a few frames before the new drawer appeared.
            _switchingDrawer = true;
            try
            {
                if (_currentDrawerWindow != null)
                {
                    _currentDrawerWindow.Close();
                }

                var opened = OpenDrawer(selectedDrawer);
                _currentOpenDrawer = opened ? selectedDrawer : null;

                _suppressSelectionChange = true;
                try
                {
                    drawerListBox.SelectedIndex = opened ? selectedIndex : -1;
                }
                finally
                {
                    _suppressSelectionChange = false;
                }
            }
            finally
            {
                _switchingDrawer = false;
            }
        }

        private void UpdateSelectedDrawer()
        {
            // A drawer switch keeps the clicked row selected until the new window is ready.
            if (_switchingDrawer)
            {
                return;
            }

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
