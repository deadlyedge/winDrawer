using System.Reflection;

namespace winDrawer;

/// <summary>
/// Reads the version that the build produced. The release workflow overrides the MSBuild
/// <c>Version</c> property, so the settings footer always matches the published executable.
/// </summary>
internal static class AppVersion
{
    /// <summary>Version without the leading <c>v</c>, for example <c>26.11.1</c>.</summary>
    public static string Current { get; } = Resolve();

    /// <summary>Text shown in the bottom-left corner of the settings panel.</summary>
    public static string VersionText => $"v{Current}";

    private static string Resolve()
    {
        var assembly = typeof(AppVersion).Assembly;

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            // Source-control metadata is appended as "+<commit>"; the UI only needs the version.
            var metadataIndex = informational.IndexOf('+');
            return metadataIndex > 0 ? informational[..metadataIndex] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
