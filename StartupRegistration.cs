// using System;
// using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace winDrawer;

internal static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "winDrawer";

    public static void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("Unable to open the current user's startup registry key.");
            runKey.SetValue(ValueName, GetStartupCommand(), RegistryValueKind.String);
        }
        else
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            runKey?.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        if (IsEnabled() != enabled)
        {
            throw new IOException("The current user's startup registration does not match the requested state.");
        }
    }

    public static bool IsEnabled()
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        if (runKey?.GetValue(ValueName) is not string registeredCommand)
        {
            return false;
        }

        return string.Equals(registeredCommand, GetStartupCommand(), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetStartupCommand()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("Unable to determine the application executable path.");
        }

        var command = Quote(processPath);
        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw new InvalidOperationException("Unable to determine the application assembly name for startup.");
            }

            var assemblyPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException("Unable to locate the application assembly for startup.", assemblyPath);
            }

            command += " " + Quote(assemblyPath);
        }

        return command;
    }

    private static string Quote(string path) => $"\"{path}\"";
}
