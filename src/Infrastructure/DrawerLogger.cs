using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace winDrawer;

/// <summary>
/// Severity levels understood by <see cref="DrawerLogger"/>. Messages below the configured minimum
/// level are dropped before any string or file work happens.
/// </summary>
internal enum DrawerLogLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3,
    Off = 4
}

/// <summary>
/// Minimal logger that persists messages to <c>Drawer.log</c> and mirrors output to the debugger.
/// The active file is rotated to <c>Drawer.log.1</c> once it exceeds
/// <see cref="AppConstants.Logging.MaxLogFileBytes"/> so a tray session running for weeks cannot grow
/// the log without bound. The minimum level is <see cref="DrawerLogLevel.Information"/>
/// (<see cref="DrawerLogLevel.Debug"/> while a debugger is attached) and can be overridden with the
/// <see cref="AppConstants.Logging.LevelEnvironmentVariable"/> environment variable.
/// </summary>
internal static class DrawerLogger
{
    private static readonly Lock SyncRoot = new();
    private static readonly DrawerLogLevel MinimumLevel = ResolveMinimumLevel();
    private static string _logFilePath = ResolveLogFilePath();
    private static long _logFileSize = -1;

    /// <summary>Absolute path of the active log file; empty when it could not be resolved.</summary>
    internal static string LogFilePath => _logFilePath;

    private static string ResolveLogFilePath()
    {
        try
        {
            return Path.Combine(AppConstants.GetUserConfigurationDirectory(), AppConstants.Files.LogFileName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DrawerLogger] Failed to resolve the user log path: {ex}");
            return string.Empty;
        }
    }

    private static DrawerLogLevel ResolveMinimumLevel()
    {
        var configured = Environment.GetEnvironmentVariable(AppConstants.Logging.LevelEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured) &&
            Enum.TryParse<DrawerLogLevel>(configured.Trim(), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return Debugger.IsAttached ? DrawerLogLevel.Debug : DrawerLogLevel.Information;
    }

    public static void LogDebug(string message, [CallerMemberName] string? memberName = null)
        => Write(DrawerLogLevel.Debug, "DEBUG", message, null, memberName);

    public static void LogInformation(string message, [CallerMemberName] string? memberName = null)
        => Write(DrawerLogLevel.Information, "INFO", message, null, memberName);

    public static void LogWarning(string message, [CallerMemberName] string? memberName = null)
        => Write(DrawerLogLevel.Warning, "WARN", message, null, memberName);

    public static void LogError(string message, Exception? exception = null, [CallerMemberName] string? memberName = null)
        => Write(DrawerLogLevel.Error, "ERROR", message, exception, memberName);

    private static void Write(DrawerLogLevel level, string levelLabel, string message, Exception? exception, string? memberName)
    {
        if (level < MinimumLevel)
        {
            return;
        }

        try
        {
            var timestamp = DateTime.Now.ToString(AppConstants.Logging.TimestampFormat, CultureInfo.InvariantCulture);
            var origin = string.IsNullOrWhiteSpace(memberName) ? "?" : memberName;
            var line = $"[{timestamp}] [{levelLabel}] {origin}: {message}";

            if (exception != null)
            {
                line = $"{line}{Environment.NewLine}{exception}";
            }

            lock (SyncRoot)
            {
                AppendEntry(line);
            }

            Debug.WriteLine(line);
        }
        catch (Exception loggingFailure)
        {
            Debug.WriteLine($"[DrawerLogger] Failed to write log entry: {loggingFailure}");
        }
    }

    /// <summary>
    /// Appends a single entry and rotates the file first when it already reached the size limit.
    /// Must be called while <see cref="SyncRoot"/> is held.
    /// </summary>
    private static void AppendEntry(string line)
    {
        if (string.IsNullOrEmpty(_logFilePath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (_logFileSize < 0)
        {
            // Adopt the size of the log file left behind by an earlier session.
            _logFileSize = TryGetLogFileSize();
        }

        if (_logFileSize >= AppConstants.Logging.MaxLogFileBytes)
        {
            RotateLogFile();
        }

        var entry = line + Environment.NewLine;
        File.AppendAllText(_logFilePath, entry);
        _logFileSize += Encoding.UTF8.GetByteCount(entry);
    }

    private static long TryGetLogFileSize()
    {
        try
        {
            return new FileInfo(_logFilePath).Length;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DrawerLogger] Unable to read the current log size: {ex}");
            return 0;
        }
    }

    /// <summary>
    /// Moves the active log to <c>Drawer.log.1</c>, replacing an earlier rotated file. A rotation
    /// failure never drops entries: logging simply continues in the existing file and the size budget
    /// is restarted so a locked file cannot trigger a retry on every single entry.
    /// </summary>
    private static void RotateLogFile()
    {
        try
        {
            File.Move(_logFilePath, _logFilePath + AppConstants.Logging.RotatedLogFileSuffix, overwrite: true);
            Debug.WriteLine($"[DrawerLogger] Rotated the log file after reaching {AppConstants.Logging.MaxLogFileBytes} bytes.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DrawerLogger] Unable to rotate the log file: {ex}");
        }

        _logFileSize = 0;
    }
}

