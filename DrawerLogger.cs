using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace winDrawer;

/// <summary>
/// Minimal logger that persists messages to <c>Drawer.log</c> and mirrors output to the debugger.
/// </summary>
internal static class DrawerLogger
{
    private static readonly object SyncRoot = new();
    private static readonly string LogFilePath = ResolveLogFilePath();

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

    public static void LogDebug(string message, [CallerMemberName] string? memberName = null)
        => Write("DEBUG", message, null, memberName);

    public static void LogInformation(string message, [CallerMemberName] string? memberName = null)
        => Write("INFO", message, null, memberName);

    public static void LogWarning(string message, [CallerMemberName] string? memberName = null)
        => Write("WARN", message, null, memberName);

    public static void LogError(string message, Exception? exception = null, [CallerMemberName] string? memberName = null)
        => Write("ERROR", message, exception, memberName);

    private static void Write(string level, string message, Exception? exception, string? memberName)
    {
        try
        {
            var timestamp = DateTime.Now.ToString(AppConstants.Logging.TimestampFormat, CultureInfo.InvariantCulture);
            var origin = string.IsNullOrWhiteSpace(memberName) ? "?" : memberName;
            var line = $"[{timestamp}] [{level}] {origin}: {message}";

            if (exception != null)
            {
                line = $"{line}{Environment.NewLine}{exception}";
            }

            lock (SyncRoot)
            {
                var directory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }

            Debug.WriteLine(line);
        }
        catch (Exception loggingFailure)
        {
            Debug.WriteLine($"[DrawerLogger] Failed to write log entry: {loggingFailure}");
        }
    }
}

