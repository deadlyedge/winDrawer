using winDrawer.Configuration;
using winDrawer.Theming;

namespace winDrawer
{
    public partial class MainWindow : Form
    {
        private void SettingsButton_Click(object sender, EventArgs e)
        {
            DrawerLogger.LogInformation("Opening settings window.");

            if (_settingsForm is { IsDisposed: false } existingSettings)
            {
                if (!existingSettings.Visible)
                {
                    existingSettings.Show(this);
                }

                existingSettings.Activate();
                return;
            }

            // Show settings as a modeless window; it stays open until saved, cancelled, or the app exits.
            var settings = new SettingsForm(_settingsSnapshot);
            _settingsForm = settings;
            AttachSettingsHandlers(settings);

            // Modeless with this window as owner; the initial position is derived from the current
            // main-window position instead of a platform default.
            ApplySettingsWindowPlacement(settings);
            settings.Show(this);
            DrawerLogger.LogDebug("Settings window displayed.");
        }

        /// <summary>
        /// Attaches the settings-panel handlers as named methods so the very same references can be
        /// detached again once the panel closes.
        /// </summary>
        private void AttachSettingsHandlers(SettingsForm settings)
        {
            _settingsCloseHandled = false;
            settings.ThemePreviewChanged += Settings_ThemePreviewChanged;
            settings.WindowHeightPreviewChanged += Settings_WindowHeightPreviewChanged;
            settings.WindowLockPreviewChanged += Settings_WindowLockPreviewChanged;
            settings.AlwaysOnTopPreviewChanged += Settings_AlwaysOnTopPreviewChanged;
            settings.ToggleHotkeyPreviewChanged += Settings_ToggleHotkeyPreviewChanged;
            settings.StartupDefaultsResetRequested += Settings_StartupDefaultsResetRequested;
            settings.FormClosed += Settings_FormClosed;
        }

        private void DetachSettingsHandlers(SettingsForm settings)
        {
            try
            {
                settings.ThemePreviewChanged -= Settings_ThemePreviewChanged;
                settings.WindowHeightPreviewChanged -= Settings_WindowHeightPreviewChanged;
                settings.WindowLockPreviewChanged -= Settings_WindowLockPreviewChanged;
                settings.AlwaysOnTopPreviewChanged -= Settings_AlwaysOnTopPreviewChanged;
                settings.ToggleHotkeyPreviewChanged -= Settings_ToggleHotkeyPreviewChanged;
                settings.StartupDefaultsResetRequested -= Settings_StartupDefaultsResetRequested;
                settings.FormClosed -= Settings_FormClosed;
            }
            catch (Exception ex)
            {
                DrawerLogger.LogWarning($"Failed to detach settings panel handlers. {ex.Message}");
            }
        }

        private void Settings_FormClosed(object? sender, FormClosedEventArgs e)
        {
            if (sender is not SettingsForm settings)
            {
                return;
            }

            // Owned modeless windows raise FormClosed twice: first with their own close reason and then
            // again with FormOwnerClosing while the owner closes. The settings bookkeeping below must
            // only run once.
            if (_settingsCloseHandled)
            {
                DrawerLogger.LogDebug($"Settings window FormClosed ({e.CloseReason}) ignored after the first pass.");
                return;
            }

            _settingsCloseHandled = true;
            _windowLockedPreview = null;
            _alwaysOnTopPreview = null;
            _toggleHotkeyPreview = null;
            if (ReferenceEquals(_settingsForm, settings))
            {
                _settingsForm = null;
            }

            DrawerLogger.LogInformation($"Settings window closed with result {settings.DialogResult}, shutdown={settings.ShutdownRequested}.");
            DetachSettingsHandlers(settings);

            // If the settings requested a shutdown we respect it. The tray menu and the settings panel
            // share one shutdown path so both flush pending settings and release the tray icon, the
            // branding fonts and the settings repository.
            if (settings.ShutdownRequested)
            {
                ShutdownApplication("settings panel");
                return;
            }

            if (settings.DialogResult == DialogResult.OK)
            {
                ApplyAcceptedSettings(settings);
            }
            else
            {
                RevertSettingsPreview(settings);
            }

            try
            {
                settings.Dispose();
            }
            catch (Exception ex)
            {
                DrawerLogger.LogWarning($"Failed to dispose the settings panel. {ex.Message}");
            }
        }

        private void ApplyAcceptedSettings(SettingsForm settings)
        {
            DrawerLogger.LogInformation("Applying settings changes from dialog.");
            var resultSettings = settings.ResultSettings;
            _settingsSnapshot = new SettingsSnapshot(
                startWithWindows: resultSettings.StartWithWindows,
                windowLocked: resultSettings.WindowLocked,
                windowColor: resultSettings.WindowColor,
                windowOpacity: resultSettings.WindowOpacity,
                windowPosition: _settingsSnapshot.WindowPosition,
                drawers: _settingsSnapshot.Drawers,
                windowHeight: resultSettings.WindowHeight,
                alwaysOnTop: resultSettings.AlwaysOnTop,
                toggleHotkeyEnabled: resultSettings.ToggleHotkeyEnabled,
                maxDrawerItems: _settingsSnapshot.MaxDrawerItems);
            _settingsRepository.Update(_settingsSnapshot);
            // Saving is handled by the repository's background writer.
            ApplyMainWindowBounds(_settingsSnapshot);
            ApplySettings(_settingsSnapshot);
        }

        private void RevertSettingsPreview(SettingsForm settings)
        {
            DrawerLogger.LogInformation("Settings dialog cancelled; reverting preview.");
            settings.RestoreWindowHeightPreview();
            ApplyMainWindowBounds(_settingsSnapshot);
            ApplyAlwaysOnTop(_settingsSnapshot.AlwaysOnTop);
            ApplyTheme(_settingsSnapshot.BuildThemePalette());
            SyncHotkeyRegistration(_settingsSnapshot.ToggleHotkeyEnabled);
        }

        private void Settings_ThemePreviewChanged(object? sender, ThemePreviewEventArgs e)
        {
            ApplyTheme(e.Palette);
        }

        private void Settings_WindowHeightPreviewChanged(object? sender, EventArgs e)
        {
            if (sender is not SettingsForm settings)
            {
                return;
            }

            var previewSnapshot = _settingsSnapshot.Clone();
            previewSnapshot.WindowHeight = settings.WindowHeightPreview;
            ApplyMainWindowBounds(previewSnapshot);
        }

        private void Settings_WindowLockPreviewChanged(bool windowLocked)
        {
            _windowLockedPreview = windowLocked;
            logoLabel.Cursor = windowLocked ? Cursors.Default : Cursors.SizeAll;
            if (windowLocked)
            {
                _isDragging = false;
            }
        }

        private void Settings_AlwaysOnTopPreviewChanged(bool alwaysOnTop)
        {
            _alwaysOnTopPreview = alwaysOnTop;
            ApplyAlwaysOnTop(alwaysOnTop);
        }

        /// <summary>
        /// Applies the "Enable Win+Shift+D" preview. A registration failure keeps the user's choice
        /// (it is what gets saved) and only tells the user that the combination is unavailable; the
        /// next start or handle recreation retries the registration.
        /// </summary>
        private void Settings_ToggleHotkeyPreviewChanged(bool enabled)
        {
            if (ApplyToggleHotkey(enabled) || !enabled || !IsHandleCreated)
            {
                return;
            }

            var owner = _settingsForm is { IsDisposed: false } settings ? settings : (IWin32Window)this;
            UiMessages.ShowWarning(
                owner,
                AppConstants.Branding.ApplicationDisplayName,
                $"The shortcut {AppConstants.HotKeys.ToggleVisibilityDescription} could not be registered because another program already uses it. "
                + "The option stays enabled and winDrawer will try again the next time it starts.");
        }

        private void Settings_StartupDefaultsResetRequested(object? sender, EventArgs e)
        {
            if (sender is not SettingsForm settings)
            {
                return;
            }

            if (!UiMessages.Confirm(
                    this,
                    "Reset Startup Settings",
                    "Reset all supported startup and main-window options to the application defaults? This takes effect immediately, removes the obsolete icon size option, and keeps drawers and unknown settings."))
            {
                return;
            }

            ResetStartupToEmbeddedDefaults(settings);
        }

        /// <summary>
        /// Applies the embedded startup defaults while keeping Windows login startup in sync. Every
        /// failure path restores the previous registration state and leaves the settings untouched.
        /// </summary>
        private void ResetStartupToEmbeddedDefaults(SettingsForm settings)
        {
            bool previousStartupState;
            SettingsSnapshot defaults;
            try
            {
                previousStartupState = StartupRegistration.IsEnabled();
                defaults = _settingsRepository.GetEmbeddedStartupDefaults();
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Unable to prepare the startup settings reset.", ex);
                UiMessages.ShowError(this, "Reset Startup Settings", $"Unable to reset startup settings:{Environment.NewLine}{Environment.NewLine}{ex.Message}");
                return;
            }

            try
            {
                StartupRegistration.SetEnabled(defaults.StartWithWindows);
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Unable to synchronize Windows login startup for the settings reset.", ex);
                RestoreStartupRegistration(previousStartupState, "Unable to roll back Windows login startup after the reset registration failed.");
                UiMessages.ShowError(this, "Reset Startup Settings", $"Windows login startup could not be reset. No settings were changed.{Environment.NewLine}{Environment.NewLine}{ex.Message}");
                return;
            }

            if (!_settingsRepository.ResetStartupToEmbeddedDefaults(TimeSpan.FromSeconds(3), out var resetSnapshot, out var error))
            {
                RestoreStartupRegistration(previousStartupState, "Unable to restore Windows login startup after a failed settings reset.");
                UiMessages.ShowError(this, "Reset Startup Settings", error);
                return;
            }

            _settingsSnapshot = resetSnapshot!;
            // The reset replaces the dialog baseline, so a pending hotkey preview must not survive it.
            _toggleHotkeyPreview = null;
            settings.ApplyResetBaseline(_settingsSnapshot);
            ApplySettings(_settingsSnapshot);
        }

        private static void RestoreStartupRegistration(bool previousStartupState, string failureMessage)
        {
            try
            {
                if (StartupRegistration.IsEnabled() != previousStartupState)
                {
                    StartupRegistration.SetEnabled(previousStartupState);
                }
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError(failureMessage, ex);
            }
        }
    }
}
