using System.Runtime.InteropServices;

namespace winDrawer;

/// <summary>
/// Minimal wrapper around the Win32 global hotkey API. The application registers one system-wide
/// shortcut on the main-window handle; that window receives <see cref="WmHotKey"/> when the user
/// presses it, and the registration disappears with the handle (so it must be re-applied whenever
/// WinForms recreates the handle).
/// </summary>
internal static class GlobalHotKey
{
    /// <summary>Message the system posts to the registered window when the shortcut is pressed.</summary>
    public const int WmHotKey = 0x0312;

    private const int ErrorInvalidHandle = 6;

    /// <summary>Win32 modifier flags accepted by <c>RegisterHotKey</c>.</summary>
    [Flags]
    internal enum Modifiers : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,

        /// <summary>Suppresses the auto-repeat stream while the combination is held down.</summary>
        NoRepeat = 0x4000
    }

    /// <summary>
    /// Registers <paramref name="key"/> with <paramref name="modifiers"/> for
    /// <paramref name="windowHandle"/>. Returns <c>false</c> and an OS error code when the handle is
    /// missing or the combination is already owned by another program (1409).
    /// </summary>
    public static bool TryRegister(IntPtr windowHandle, int id, Modifiers modifiers, Keys key, out int errorCode)
    {
        errorCode = 0;
        if (windowHandle == IntPtr.Zero)
        {
            errorCode = ErrorInvalidHandle;
            return false;
        }

        if (RegisterHotKey(windowHandle, id, modifiers, (uint)(key & Keys.KeyCode)))
        {
            return true;
        }

        errorCode = Marshal.GetLastWin32Error();
        return false;
    }

    /// <summary>Releases a previously registered shortcut; failing to release is not an error.</summary>
    public static bool TryUnregister(IntPtr windowHandle, int id)
    {
        return windowHandle != IntPtr.Zero && UnregisterHotKey(windowHandle, id);
    }

    /// <summary>Short explanation for the common registration failures, used in diagnostics.</summary>
    public static string DescribeError(int errorCode)
    {
        return errorCode switch
        {
            0 => "no error",
            5 => "access denied",
            1409 => "the key combination is already registered by another program",
            _ => "unknown error"
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr windowHandle, int id, Modifiers modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);
}
