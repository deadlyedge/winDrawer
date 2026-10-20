namespace winDrawer
{
    public partial class MainWindow : Form
    {
        private bool? _toggleHotkeyPreview;
        private bool _toggleHotKeyRegistered;
        private bool _settingsApplied;

        /// <summary>
        /// Handle the shortcut was registered against. It is kept because the handle is already gone
        /// when <see cref="OnHandleDestroyed"/> runs, and reading <c>Handle</c> there would recreate
        /// the window.
        /// </summary>
        private IntPtr _toggleHotKeyHandle;

        /// <summary>
        /// The state that should currently be registered: the settings-panel preview wins while the
        /// panel is open, otherwise the persisted snapshot value applies.
        /// </summary>
        private bool IsToggleHotkeyEnabled => _toggleHotkeyPreview ?? _settingsSnapshot.ToggleHotkeyEnabled;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // ApplySettings runs at the end of the constructor; a handle created before that has no
            // settings to read yet, and that first ApplySettings call registers the shortcut instead.
            if (_settingsApplied)
            {
                SyncHotkeyRegistration(IsToggleHotkeyEnabled);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // The system drops the registration together with the window; releasing it explicitly
            // keeps the intent visible and covers the "hotkey turned off" path.
            ReleaseHotKey();
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == GlobalHotKey.WmHotKey && m.WParam.ToInt32() == AppConstants.HotKeys.ToggleVisibilityId)
            {
                DrawerLogger.LogInformation(
                    $"Global shortcut {AppConstants.HotKeys.ToggleVisibilityDescription} pressed; toggling window visibility.");
                ToggleVisibility();
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// Applies a settings-panel preview (or the persisted value) to the global shortcut. Returns
        /// <c>true</c> when the requested state is in effect.
        /// </summary>
        private bool ApplyToggleHotkey(bool enabled)
        {
            _toggleHotkeyPreview = enabled;
            return SyncHotkeyRegistration(enabled);
        }

        /// <summary>
        /// Makes the registration match <paramref name="enabled"/>. Registering is idempotent and
        /// deferred while the window has no handle yet; a failed registration is reported to the
        /// caller so the settings panel can tell the user the combination is unavailable.
        /// </summary>
        private bool SyncHotkeyRegistration(bool enabled)
        {
            if (!enabled)
            {
                ReleaseHotKey();
                return true;
            }

            if (_toggleHotKeyRegistered)
            {
                return true;
            }

            if (!IsHandleCreated)
            {
                DrawerLogger.LogDebug(
                    $"Deferring the {AppConstants.HotKeys.ToggleVisibilityDescription} registration until the main-window handle exists.");
                return false;
            }

            const GlobalHotKey.Modifiers modifiers =
                GlobalHotKey.Modifiers.Win | GlobalHotKey.Modifiers.Shift | GlobalHotKey.Modifiers.NoRepeat;

            if (GlobalHotKey.TryRegister(Handle, AppConstants.HotKeys.ToggleVisibilityId, modifiers, Keys.D, out var errorCode))
            {
                _toggleHotKeyHandle = Handle;
                _toggleHotKeyRegistered = true;
                DrawerLogger.LogInformation($"Registered global shortcut {AppConstants.HotKeys.ToggleVisibilityDescription}.");
                return true;
            }

            DrawerLogger.LogWarning(
                $"Unable to register global shortcut {AppConstants.HotKeys.ToggleVisibilityDescription} (error {errorCode}: {GlobalHotKey.DescribeError(errorCode)}). The application continues without it.");
            return false;
        }

        private void ReleaseHotKey()
        {
            if (!_toggleHotKeyRegistered)
            {
                return;
            }

            _toggleHotKeyRegistered = false;
            try
            {
                if (!GlobalHotKey.TryUnregister(_toggleHotKeyHandle, AppConstants.HotKeys.ToggleVisibilityId))
                {
                    DrawerLogger.LogDebug(
                        $"The {AppConstants.HotKeys.ToggleVisibilityDescription} registration was already released.");
                }
            }
            catch (Exception ex)
            {
                DrawerLogger.LogWarning(
                    $"Failed to release the global shortcut {AppConstants.HotKeys.ToggleVisibilityDescription}. {ex.Message}");
            }
            finally
            {
                _toggleHotKeyHandle = IntPtr.Zero;
            }
        }
    }
}
