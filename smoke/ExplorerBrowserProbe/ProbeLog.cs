using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ExplorerBrowserProbe;

/// <summary>
/// 烟测专用的极简日志：写文件 + Debug 输出。刻意不复用主程序的 DrawerLogger：
/// 这是独立的一次性评估工具，不应把主程序内部类型拖进来。
/// </summary>
internal static class ProbeLog
{
    private static readonly Lock SyncRoot = new();
    private static readonly string LogPath = ResolveLogPath();

    /// <summary>日志文件绝对路径；解析失败时为空字符串。窗口标题里会显示它。</summary>
    public static string Path => LogPath;

    private static string ResolveLogPath()
    {
        try
        {
            return System.IO.Path.Combine(AppContext.BaseDirectory, "probe.log");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProbeLog] 无法解析日志路径: {ex.Message}");
            return string.Empty;
        }
    }

    public static void Write(string message)
    {
        var line = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}] {message}";
        Debug.WriteLine(line);

        if (LogPath.Length == 0)
        {
            return;
        }

        try
        {
            lock (SyncRoot)
            {
                File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ProbeLog] 写日志失败: {ex.Message}");
        }
    }

    public static void WriteBlock(string text)
    {
        Write(text.Replace(Environment.NewLine, Environment.NewLine + "    ", StringComparison.Ordinal));
    }
}
