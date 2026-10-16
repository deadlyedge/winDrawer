namespace winDrawer;

internal sealed record DrawerItemInfo(
    string Path,
    string Name,
    bool IsDirectory,
    DateTime? LastWriteTimeUtc);