using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace winDrawer;

internal static class ShellIcon
{
    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiLargeIcon = 0x000000000;
    private const uint ShgfiUseFileAttributes = 0x000000010;
    private const uint FileAttributeDirectory = 0x00000010;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Shfileinfo
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out Shfileinfo psfi, uint cbFileInfo, uint uFlags);

    [DllImport("User32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public static Icon? GetIcon(string path, bool isDirectory)
    {
        var attributes = isDirectory ? FileAttributeDirectory : 0;
        var flags = ShgfiIcon | ShgfiLargeIcon;

        if (!Directory.Exists(path) && !File.Exists(path))
        {
            flags |= ShgfiUseFileAttributes;
        }

        var result = SHGetFileInfo(path, attributes, out var shinfo, (uint)Marshal.SizeOf<Shfileinfo>(), flags);
        if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var icon = Icon.FromHandle(shinfo.hIcon);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(shinfo.hIcon);
        }
    }
}
