using System.Diagnostics;
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
            void previewHandler(object? _, ThemePreviewEventArgs args) => ApplyTheme(args.Palette);
            settings.ThemePreviewChanged += (EventHandler<ThemePreviewEventArgs>?)previewHandler;
            void windowHeightPreviewHandler(object? _1, EventArgs _2)
            {
                var previewSnapshot = _settingsSnapshot.Clone();
                previewSnapshot.WindowHeight = settings.WindowHeightPreview;
                ApplyMainWindowBounds(previewSnapshot);
            }
            settings.WindowHeightPreviewChanged += (EventHandler?)windowHeightPreviewHandler;
            void windowLockPreviewHandler(bool windowLocked)
            {
                _windowLockedPreview = windowLocked;
                logoLabel.Cursor = windowLocked ? Cursors.Default : Cursors.SizeAll;
                if (windowLocked)
                {
                    _isDragging = false;
                }
            }
            settings.WindowLockPreviewChanged += windowLockPreviewHandler;
            void alwaysOnTopPreviewHandler(bool alwaysOnTop)
            {
                _alwaysOnTopPreview = alwaysOnTop;
                ApplyAlwaysOnTop(alwaysOnTop);
            }
            settings.AlwaysOnTopPreviewChanged += alwaysOnTopPreviewHandler;
            void resetDefaultsHandler(object? _, EventArgs _2)
            {
                var confirmation = MessageBox.Show(
                    this,
                    "Reset all supported startup and main-window options to the application defaults? This takes effect immediately, removes the obsolete icon size option, and keeps drawers and unknown settings.",
                    "Reset Startup Settings",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirmation != DialogResult.Yes)
                {
                    return;
                }

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
                    MessageBox.Show(this, $"Unable to reset startup settings:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                        "Reset Startup Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    StartupRegistration.SetEnabled(defaults.StartWithWindows);
                }
                catch (Exception ex)
                {
                    DrawerLogger.LogError("Unable to synchronize Windows login startup for the settings reset.", ex);
                    try
                    {
                        if (StartupRegistration.IsEnabled() != previousStartupState)
                        {
                            StartupRegistration.SetEnabled(previousStartupState);
                        }
                    }
                    catch (Exception rollbackException)
                    {
                        DrawerLogger.LogError("Unable to roll back Windows login startup after the reset registration failed.", rollbackException);
                    }

                    MessageBox.Show(this, $"Windows login startup could not be reset. No settings were changed.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                        "Reset Startup Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!_settingsRepository.ResetStartupToEmbeddedDefaults(TimeSpan.FromSeconds(3), out var resetSnapshot, out var error))
                {
                    try
                    {
                        if (StartupRegistration.IsEnabled() != previousStartupState)
                        {
                            StartupRegistration.SetEnabled(previousStartupState);
                        }
                    }
                    catch (Exception rollbackException)
                    {
                        DrawerLogger.LogError("Unable to restore Windows login startup after a failed settings reset.", rollbackException);
                    }

                    MessageBox.Show(this, error, "Reset Startup Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _settingsSnapshot = resetSnapshot!;
                settings.ApplyResetBaseline(_settingsSnapshot);
                ApplySettings(_settingsSnapshot);
            }
            settings.StartupDefaultsResetRequested += resetDefaultsHandler;

            settings.FormClosed += (_, _) =>
            {
                _windowLockedPreview = null;
                _alwaysOnTopPreview = null;
                if (ReferenceEquals(_settingsForm, settings))
                {
                    _settingsForm = null;
                }

                DrawerLogger.LogInformation($"Settings window closed with result {settings.DialogResult}, shutdown={settings.ShutdownRequested}.");
                try
                {
                    settings.ThemePreviewChanged -= (global::System.EventHandler<ThemePreviewEventArgs>?)previewHandler;
                    settings.WindowHeightPreviewChanged -= (global::System.EventHandler?)windowHeightPreviewHandler;
                    settings.AlwaysOnTopPreviewChanged -= (Action<bool>?)alwaysOnTopPreviewHandler;
                    settings.StartupDefaultsResetRequested -= (global::System.EventHandler?)resetDefaultsHandler;
                }
                catch { }

                // If the settings requested a shutdown we respect it.
                if (settings.ShutdownRequested)
                {
                    DrawerLogger.LogWarning("Settings requested application shutdown.");
                    // Perform only minimal cleanup and exit immediately to avoid blocking UI while
                    // many windows perform synchronous I/O during their close events.
                    try
                    {
                        _trayController?.Dispose();
                        _trayController = null;
                    }
                    catch (Exception ex)
                    {
                        DrawerLogger.LogError("Failed to dispose tray controller during shutdown request.", ex);
                    }

                    // Ensure UI message queue is processed briefly so the cleanup above is applied.
                    try
                    {
                        Application.DoEvents();
                    }
                    catch { }

                    // Try to flush pending settings to disk, wait up to 500ms, then exit.
                    try
                    {
                        var flushTask = _settingsRepository.FlushAsync(TimeSpan.FromMilliseconds(500));
                        if (flushTask == null || !flushTask.Wait(500))
                        {
                            // Flush timed out — force terminate.
                            try { Process.GetCurrentProcess().Kill(); } catch { try { Environment.Exit(0); } catch { } }
                        }
                        else
                        {
                            // Flush completed — exit gracefully.
                            try { Environment.Exit(0); } catch { try { Process.GetCurrentProcess().Kill(); } catch { } }
                        }
                    }
                    catch
                    {
                        // On any unexpected error, force terminate to avoid hanging.
                        try { Process.GetCurrentProcess().Kill(); } catch { try { Environment.Exit(0); } catch { } }
                    }

                    return;
                }

                // Apply changes if user accepted, otherwise revert preview.
                if (settings.DialogResult == DialogResult.OK)
                {
                    DrawerLogger.LogInformation("Applying settings changes from dialog.");
                    var resultSettings = settings.ResultSettings;
                    _settingsSnapshot = new SettingsSnapshot(
                        resultSettings.StartWithWindows,
                        resultSettings.WindowLocked,
                        resultSettings.WindowColor,
                        resultSettings.WindowOpacity,
                        _settingsSnapshot.WindowPosition,
                        _settingsSnapshot.Drawers,
                        resultSettings.WindowHeight,
                        resultSettings.AlwaysOnTop);
                    _settingsRepository.Update(_settingsSnapshot);
                    // Save enqueued by repository's background writer.
                    ApplyMainWindowBounds(_settingsSnapshot);
                    ApplySettings(_settingsSnapshot);
                }
                else
                {
                    DrawerLogger.LogInformation("Settings dialog cancelled; reverting preview.");
                    settings.RestoreWindowHeightPreview();
                    ApplyMainWindowBounds(_settingsSnapshot);
                    ApplyAlwaysOnTop(_settingsSnapshot.AlwaysOnTop);
                    ApplyTheme(_settingsSnapshot.BuildThemePalette());
                }

                try
                {
                    settings.Dispose();
                }
                catch { }
            };

            // Modeless with this window as owner; the initial position is derived from the current
            // main-window position instead of a platform default.
            ApplySettingsWindowPlacement(settings);
            settings.Show(this);
            DrawerLogger.LogDebug("Settings window displayed.");
        }
    }
}
