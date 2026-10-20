namespace winDrawer;

/// <summary>
/// Order in which a drawer lists its direct child items. Directories and files stay mixed in every
/// mode (the confirmed contract from stage 7.1); only the primary key changes.
/// </summary>
public enum DrawerSortMode
{
    /// <summary>Newest last-write time first (default).</summary>
    NewestFirst = 0,

    /// <summary>Oldest last-write time first.</summary>
    OldestFirst = 1,

    /// <summary>Name ascending, compared with <see cref="StringComparer.OrdinalIgnoreCase"/>.</summary>
    NameAscending = 2
}

/// <summary>
/// TOML representation of <see cref="DrawerSortMode"/>. Kept next to the enum so the persisted values
/// and the parser cannot drift apart.
/// </summary>
internal static class DrawerSortModeText
{
    public const string Newest = "newest";
    public const string Oldest = "oldest";
    public const string NameValue = "name";

    public static string ToTomlValue(DrawerSortMode mode)
    {
        return mode switch
        {
            DrawerSortMode.OldestFirst => Oldest,
            DrawerSortMode.NameAscending => NameValue,
            _ => Newest
        };
    }

    /// <summary>
    /// Parses the persisted value. Missing, unknown or wrongly typed values fall back to the default
    /// mode instead of failing the whole configuration.
    /// </summary>
    public static DrawerSortMode Parse(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Oldest => DrawerSortMode.OldestFirst,
            NameValue => DrawerSortMode.NameAscending,
            _ => DrawerSortMode.NewestFirst
        };
    }
}
