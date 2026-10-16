using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Serialization;

namespace winDrawer.Configuration;

internal sealed partial class SettingsRepository
{
    /// <summary>
    /// Force a flush: waits up to <paramref name="timeout"/> for pending writes to complete.
    /// </summary>
    public async Task<bool> FlushAsync(TimeSpan timeout)
    {
        DrawerLogger.LogInformation($"Flush requested with timeout {timeout}.");
        TaskCompletionSource<bool> tcs;
        lock (_writeLock)
        {
            if (!_dirty && !_writeInProgress)
            {
                DrawerLogger.LogDebug("Flush skipped because repository is not dirty.");
                return true; // nothing to flush
            }

            if (_flushTcs?.Task.IsCompleted == true)
            {
                _flushTcs = null;
            }

            _flushTcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            tcs = _flushTcs;
            if (_dirty)
            {
                _writeSignal.Release();
            }
        }

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout)).ConfigureAwait(false);
        var success = completed == tcs.Task && tcs.Task.IsCompletedSuccessfully;
        lock (_writeLock)
        {
            if (!success && ReferenceEquals(_flushTcs, tcs))
            {
                _flushTcs = null;
            }
        }

        DrawerLogger.LogInformation($"Flush completed. Success={success}.");
        return success;
    }

    public bool SavePendingChanges(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            return false;
        }

        if (_writerTask == null || _writerTask.IsCompleted)
        {
            string content;
            lock (_writeLock)
            {
                content = SerializeSettings(_root);
                _dirty = false;
            }

            try
            {
                WriteSettingsContent(_settingsPath, content);
                return true;
            }
            catch (Exception ex)
            {
                DrawerLogger.LogError("Failed to synchronously persist pending settings.", ex);
                lock (_writeLock)
                {
                    _dirty = true;
                }

                return false;
            }
        }

        try
        {
            return FlushAsync(timeout).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            DrawerLogger.LogError("Failed to flush pending settings.", ex);
            return false;
        }
    }

    private async Task BackgroundWriterLoop(CancellationToken token)
    {
        try
        {
            DrawerLogger.LogInformation("Background settings writer loop started.");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _writeSignal.WaitAsync(token);
                }
                catch (OperationCanceledException)
                {
                    DrawerLogger.LogInformation("Background writer cancellation requested.");
                    break;
                }

                bool shouldWrite = false;
                TaskCompletionSource<bool>? tcs = null;

                lock (_writeLock)
                {
                    if (_dirty)
                    {
                        _dirty = false;
                        _writeInProgress = true;
                        shouldWrite = true;
                    }
                }

                if (shouldWrite)
                {
                    try
                    {
                        DrawerLogger.LogDebug("Background writer persisting settings to disk.");
                        string content;
                        lock (_writeLock)
                        {
                            content = SerializeSettings(_root);
                        }

                        if (token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(token);
                        }

                        lock (_writeLock)
                        {
                            if (_dirty)
                            {
                                _writeInProgress = false;
                                continue;
                            }
                        }

                        await WriteSettingsContentAsync(_settingsPath, content, token);
                        DrawerLogger.LogInformation("Background settings write completed.");
                        lock (_writeLock)
                        {
                            _writeInProgress = false;
                            if (!_dirty)
                            {
                                tcs = _flushTcs;
                                _flushTcs = null;
                            }
                        }

                        tcs?.TrySetResult(true);
                    }
                    catch (OperationCanceledException)
                    {
                        DrawerLogger.LogInformation("Background writer write canceled.");
                        lock (_writeLock)
                        {
                            _writeInProgress = false;
                            if (!_dirty)
                            {
                                tcs = _flushTcs;
                                _flushTcs = null;
                            }
                        }

                        tcs?.TrySetCanceled();
                        break;
                    }
                    catch (Exception ex)
                    {
                        DrawerLogger.LogError("Background writer failed to persist settings.", ex);
                        lock (_writeLock)
                        {
                            _writeInProgress = false;
                            _dirty = true;
                            tcs = _flushTcs;
                            _flushTcs = null;
                        }

                        try { tcs?.TrySetException(ex); } catch { }
                    }
                }
            }
        }
        finally
        {
            DrawerLogger.LogInformation("Background settings writer loop stopping.");
            // Attempt one last write if dirty
            bool needFinal = false;
            lock (_writeLock)
            {
                if (_dirty)
                {
                    _dirty = false;
                    needFinal = true;
                }
            }

            if (needFinal)
            {
                try
                {
                    DrawerLogger.LogInformation("Background writer performing final flush.");
                    string content;
                    lock (_writeLock)
                    {
                        content = SerializeSettings(_root);
                    }

                    WriteSettingsContent(_settingsPath, content);
                    DrawerLogger.LogInformation("Final settings flush completed.");
                }
                catch (Exception ex)
                {
                    DrawerLogger.LogError("Final settings flush failed.", ex);
                }
            }
        }
    }

    public void Dispose()
    {
        DrawerLogger.LogInformation("Disposing SettingsRepository.");
        try
        {
            if (_writerTask is { IsCompleted: false })
            {
                FlushAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }

            _writerCts.Cancel();
            _writeSignal.Release();
            _writerTask?.Wait(500);
        }
        catch { }

        _writerCts.Dispose();
        _writeSignal.Dispose();
        DrawerLogger.LogInformation("SettingsRepository disposed.");
    }

    private TomlTable LoadOrCreateTable(string path)
    {
        if (!File.Exists(path))
        {
            DrawerLogger.LogInformation($"Settings file not found. Initializing from embedded defaults at '{path}'.");
            var table = LoadEmbeddedDefaultTable();
            WriteTable(path, table);
            return table;
        }

        try
        {
            var settings = TomlSerializer.Deserialize<TomlTable>(File.ReadAllText(path));
            DrawerLogger.LogInformation($"Settings file loaded from '{path}'.");
            return settings ?? new TomlTable();
        }
        catch (TomlException ex)
        {
            DrawerLogger.LogError($"Failed to parse settings file '{path}'.", ex);
            throw new InvalidDataException($"Failed to parse settings file '{path}'.", ex);
        }
    }

    private static TomlTable LoadEmbeddedDefaultTable()
    {
        using var stream = typeof(SettingsRepository).Assembly.GetManifestResourceStream("winDrawer.DefaultSettings.toml")
            ?? throw new InvalidDataException("The embedded default settings template is missing.");
        using var reader = new StreamReader(stream);
        var settings = TomlSerializer.Deserialize<TomlTable>(reader.ReadToEnd());
        return settings ?? throw new InvalidDataException("The embedded default settings template is empty.");
    }

    private void WriteTable(string path, TomlTable table)
    {
        WriteSettingsContent(path, SerializeSettings(table));
    }

    private static void WriteSettingsContent(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var outputDirectory = string.IsNullOrEmpty(directory) ? Environment.CurrentDirectory : directory;
        var temporaryPath = Path.Combine(outputDirectory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, content);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static async Task WriteSettingsContentAsync(string path, string content, CancellationToken token)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var outputDirectory = string.IsNullOrEmpty(directory) ? Environment.CurrentDirectory : directory;
        var temporaryPath = Path.Combine(outputDirectory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporaryPath, content, token);
            token.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
