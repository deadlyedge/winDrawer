namespace winDrawer;

/// <summary>
/// Single definition of the path-equality rule used by the settings repository and the drawer list:
/// environment variables are expanded, surrounding whitespace is ignored and the comparison follows
/// the platform (case-insensitive on Windows). It replaces the two subtly different copies that used
/// to live in <c>SettingsRepository</c> and <c>MainWindow</c>.
/// </summary>
internal static class PathComparison
{
    /// <summary>Expands environment variables and trims the stored form of a path.</summary>
    public static string Normalize(string? path)
    {
        return Environment.ExpandEnvironmentVariables(path ?? string.Empty).Trim();
    }

    /// <summary>Returns whether both values refer to the same path.</summary>
    public static bool AreSame(string? first, string? second)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(Normalize(first), Normalize(second), comparison);
    }
}
