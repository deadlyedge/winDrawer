namespace winDrawer.Configuration;

/// <summary>
/// Writes the packaged first-run demo into the user configuration directory. Every file is created
/// only when missing, so a user-edited demo is never overwritten; failures are logged and skipped and
/// can never block startup.
/// </summary>
internal static class DemoDrawerInitializer
{
    /// <summary>Logical-name prefix of the embedded demo folder (configured in winDrawer.csproj).</summary>
    private const string ResourcePrefix = "winDrawer.Demo.";

    public static void EnsureCreated(string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);

        var assembly = typeof(DemoDrawerInitializer).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (resourceNames.Count == 0)
        {
            DrawerLogger.LogWarning(
                $"Embedded demo drawer resources ('{ResourcePrefix}*') were not found; the demo folder was not created.");
            return;
        }

        foreach (var resourceName in resourceNames)
        {
            var relativePath = resourceName[ResourcePrefix.Length..];
            var targetPath = Path.GetFullPath(Path.Combine(targetDirectory, relativePath));
            if (!IsInsideDirectory(targetDirectory, targetPath))
            {
                DrawerLogger.LogWarning(
                    $"Skipped embedded demo resource '{resourceName}' because it resolves outside '{targetDirectory}'.");
                continue;
            }

            if (File.Exists(targetPath))
            {
                continue;
            }

            try
            {
                var directory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var source = assembly.GetManifestResourceStream(resourceName);
                if (source is null)
                {
                    DrawerLogger.LogWarning($"Embedded demo resource '{resourceName}' could not be opened.");
                    continue;
                }

                using var destination = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                source.CopyTo(destination);
                DrawerLogger.LogInformation($"Created demo file '{targetPath}'.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                DrawerLogger.LogWarning($"Unable to create demo file '{targetPath}'. {ex.Message}");
            }
        }
    }

    private static bool IsInsideDirectory(string directory, string path)
    {
        var normalizedDirectory = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return path.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
    }
}
