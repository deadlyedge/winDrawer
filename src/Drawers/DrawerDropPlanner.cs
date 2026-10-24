namespace winDrawer;

/// <summary>Reason a drop into a drawer is refused.</summary>
internal enum DrawerDropRejection
{
    None,
    NoSources,
    DestinationUnavailable,
    DestinationInsideSource,
    NoAllowedEffect
}

/// <summary>
/// Result of planning a drop into a drawer folder: which entries the Shell should move, which it
/// should copy, and whether the drop must be refused.
/// </summary>
internal readonly record struct DrawerDropPlan(
    IReadOnlyList<string> MovePaths,
    IReadOnlyList<string> CopyPaths,
    DrawerDropRejection Rejection)
{
    public bool HasWork => MovePaths.Count > 0 || CopyPaths.Count > 0;

    /// <summary>Effects the drawer advertises while the drag hovers over the list.</summary>
    public DragDropEffects AllowedEffect =>
        (MovePaths.Count > 0 ? DragDropEffects.Move : DragDropEffects.None) |
        (CopyPaths.Count > 0 ? DragDropEffects.Copy : DragDropEffects.None);

    public static DrawerDropPlan Reject(DrawerDropRejection rejection)
    {
        return new(Array.Empty<string>(), Array.Empty<string>(), rejection);
    }
}

/// <summary>
/// Decides what a drop into a drawer folder means. The planner only does string/volume analysis; it
/// never touches the file system, so the rules can be checked independently of Explorer.
///
/// The default follows Windows: entries from the same volume as the drawer folder are moved, entries
/// from another volume are copied. Holding Ctrl forces a copy, holding Shift forces a move, and the
/// effects the drag source allows always win over the default.
/// </summary>
internal static class DrawerDropPlanner
{
    public static DrawerDropPlan Plan(
        IEnumerable<string> sourcePaths,
        string destinationFolder,
        DragDropEffects allowedEffects,
        Keys modifierKeys)
    {
        var allowCopy = (allowedEffects & DragDropEffects.Copy) != 0;
        var allowMove = (allowedEffects & DragDropEffects.Move) != 0;
        if (!allowCopy && !allowMove)
        {
            return DrawerDropPlan.Reject(DrawerDropRejection.NoAllowedEffect);
        }

        var destination = GetNormalizedPath(destinationFolder);
        if (destination is null)
        {
            return DrawerDropPlan.Reject(DrawerDropRejection.DestinationUnavailable);
        }

        var forceCopy = (modifierKeys & Keys.Control) != 0;
        var forceMove = (modifierKeys & Keys.Shift) != 0;
        if (forceCopy == forceMove)
        {
            // Neither key or both keys pressed: fall back to the volume-based default.
            forceCopy = false;
            forceMove = false;
        }

        var movePaths = new List<string>();
        var copyPaths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in sourcePaths)
        {
            var fullSource = GetNormalizedPath(source);
            if (fullSource is null || !seen.Add(fullSource))
            {
                continue;
            }

            // An entry that already lives in the drawer folder would be a no-op.
            var sourceParent = Path.GetDirectoryName(fullSource);
            if (sourceParent is not null &&
                string.Equals(sourceParent, destination, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Moving/copying a folder into itself or into one of its subfolders is never valid.
            if (IsSameOrChildPath(destination, fullSource))
            {
                return DrawerDropPlan.Reject(DrawerDropRejection.DestinationInsideSource);
            }

            var useCopy = forceCopy || (!forceMove && !IsSameVolume(fullSource, destination));
            if (!(useCopy ? allowCopy : allowMove))
            {
                // The preferred effect is not offered by the drag source; use the other one, which
                // is known to be allowed because at least one effect is.
                useCopy = !useCopy;
            }

            (useCopy ? copyPaths : movePaths).Add(fullSource);
        }

        if (movePaths.Count == 0 && copyPaths.Count == 0)
        {
            return DrawerDropPlan.Reject(DrawerDropRejection.NoSources);
        }

        return new DrawerDropPlan(movePaths, copyPaths, DrawerDropRejection.None);
    }

    /// <summary>
    /// Returns the canonical absolute path without a trailing separator (except for a bare root),
    /// or <c>null</c> when the value is empty or cannot be turned into a path.
    /// </summary>
    public static string? GetNormalizedPath(string? path)
    {
        var trimmed = PathComparison.Normalize(path);
        if (trimmed.Length == 0)
        {
            return null;
        }

        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(trimmed));
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Volume root used for the same-volume rule, or <c>null</c> when undeterminable.</summary>
    public static string? GetVolumeRoot(string? path)
    {
        var normalized = GetNormalizedPath(path);
        return normalized is null ? null : Path.GetPathRoot(normalized);
    }

    /// <summary>Whether both paths resolve to the same volume (drive or UNC share).</summary>
    public static bool IsSameVolume(string? first, string? second)
    {
        var firstRoot = GetVolumeRoot(first);
        var secondRoot = GetVolumeRoot(second);
        return firstRoot is not null &&
               secondRoot is not null &&
               string.Equals(firstRoot, secondRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether <paramref name="candidate"/> equals or lies below <paramref name="parent"/>.</summary>
    public static bool IsSameOrChildPath(string? candidate, string? parent)
    {
        var normalizedCandidate = GetNormalizedPath(candidate);
        var normalizedParent = GetNormalizedPath(parent);
        if (normalizedCandidate is null || normalizedParent is null)
        {
            return false;
        }

        if (string.Equals(normalizedCandidate, normalizedParent, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefix = normalizedParent.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedParent
            : normalizedParent + Path.DirectorySeparatorChar;

        return normalizedCandidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
