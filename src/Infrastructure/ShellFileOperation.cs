using System.Runtime.InteropServices;

namespace winDrawer;

/// <summary>Outcome of a queued Shell file operation.</summary>
internal readonly record struct ShellFileOperationResult(int HResult, bool Aborted)
{
    private const int ErrorCancelled = unchecked((int)0x800704C7);
    private const int CopyEngineUserCancelled = unchecked((int)0x80270000);

    public bool Succeeded => HResult >= 0 && !Aborted;

    public bool Canceled => HResult == ErrorCancelled || HResult == CopyEngineUserCancelled;
}

/// <summary>
/// Moves and copies files through <c>IFileOperation</c>, so Windows owns the progress dialog,
/// conflict handling, error reporting and the undo record. The application never re-implements file
/// transfers and never handles the individual files itself.
/// </summary>
internal static class ShellFileOperation
{
    private const uint FofAllowUndo = 0x0040;

    private static readonly Guid FileOperationClsid = new("3ad05575-8857-4850-9277-11b85bdb8e09");
    private static readonly Guid ShellItemIid = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");

    /// <summary>
    /// Queues every move first, then every copy, and performs them as one Shell operation owned by
    /// <paramref name="ownerWindow"/>. Must be called from an STA thread (the WinForms UI thread);
    /// the call blocks until the Shell finished or the user cancelled.
    /// </summary>
    public static ShellFileOperationResult Perform(
        IntPtr ownerWindow,
        string destinationFolder,
        IReadOnlyCollection<string> movePaths,
        IReadOnlyCollection<string> copyPaths)
    {
        if (movePaths.Count == 0 && copyPaths.Count == 0)
        {
            return new ShellFileOperationResult(0, Aborted: false);
        }

        IFileOperation? operation = null;
        var createdItems = new List<IShellItem>();
        try
        {
            operation = (IFileOperation)Activator.CreateInstance(Type.GetTypeFromCLSID(FileOperationClsid)!)!;

            var hr = operation.SetOperationFlags(FofAllowUndo);
            if (hr < 0)
            {
                return new ShellFileOperationResult(hr, Aborted: false);
            }

            if (ownerWindow != IntPtr.Zero)
            {
                operation.SetOwnerWindow(ownerWindow);
            }

            var destination = CreateShellItem(destinationFolder, createdItems);
            foreach (var path in movePaths)
            {
                var item = CreateShellItem(path, createdItems);
                hr = operation.MoveItem(item, destination, null, IntPtr.Zero);
                if (hr < 0)
                {
                    return new ShellFileOperationResult(hr, Aborted: false);
                }
            }

            foreach (var path in copyPaths)
            {
                var item = CreateShellItem(path, createdItems);
                hr = operation.CopyItem(item, destination, null, IntPtr.Zero);
                if (hr < 0)
                {
                    return new ShellFileOperationResult(hr, Aborted: false);
                }
            }

            hr = operation.PerformOperations();
            var aborted = false;
            if (hr >= 0)
            {
                operation.GetAnyOperationsAborted(out aborted);
            }

            return new ShellFileOperationResult(hr, aborted);
        }
        finally
        {
            foreach (var item in createdItems)
            {
                ReleaseComObject(item);
            }

            ReleaseComObject(operation);
        }
    }

    private static IShellItem CreateShellItem(string path, ICollection<IShellItem> createdItems)
    {
        var iid = ShellItemIid;
        var hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out var item);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        createdItems.Add(item);
        return item;
    }

    private static void ReleaseComObject(object? instance)
    {
        if (instance is null || !Marshal.IsComObject(instance))
        {
            return;
        }

        try
        {
            Marshal.FinalReleaseComObject(instance);
        }
        catch (Exception)
        {
            // Releasing an already released or not-owned RCW must not mask the real result.
        }
    }

    /// <summary>
    /// Minimal <c>IFileOperation</c> declaration. Every member up to the ones actually used must be
    /// declared in the exact order of <c>shobjidl_core.h</c>; otherwise the vtable slots shift and a
    /// call silently lands on another method. Unused members are placeholders.
    /// </summary>
    [ComImport]
    [Guid("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOperation
    {
        [PreserveSig] int Advise(IntPtr progressSink, out uint cookie);

        [PreserveSig] int Unadvise(uint cookie);

        [PreserveSig] int SetOperationFlags(uint operationFlags);

        [PreserveSig] int SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string? message);

        [PreserveSig] int SetProgressDialog(IntPtr progressDialog);

        [PreserveSig] int SetProperties(IntPtr propertyChangeArray);

        [PreserveSig] int SetOwnerWindow(IntPtr ownerWindow);

        [PreserveSig] int ApplyPropertiesToItem(IntPtr shellItem);

        [PreserveSig] int ApplyPropertiesToItems(IntPtr items);

        [PreserveSig]
        int RenameItem(
            IntPtr shellItem,
            [MarshalAs(UnmanagedType.LPWStr)] string? newName,
            IntPtr progressSink);

        [PreserveSig]
        int RenameItems(
            IntPtr items,
            [MarshalAs(UnmanagedType.LPWStr)] string? newName);

        [PreserveSig]
        int MoveItem(
            IShellItem shellItem,
            IShellItem destinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string? newName,
            IntPtr progressSink);

        [PreserveSig] int MoveItems(IntPtr items, IShellItem destinationFolder, IntPtr progressSink);

        [PreserveSig]
        int CopyItem(
            IShellItem shellItem,
            IShellItem destinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string? copyName,
            IntPtr progressSink);

        [PreserveSig] int CopyItems(IntPtr items, IShellItem destinationFolder, IntPtr progressSink);

        [PreserveSig] int DeleteItem(IntPtr shellItem, IntPtr progressSink);

        [PreserveSig] int DeleteItems(IntPtr items, IntPtr progressSink);

        [PreserveSig]
        int NewItem(
            IntPtr destinationFolder,
            uint fileAttributes,
            [MarshalAs(UnmanagedType.LPWStr)] string? name,
            [MarshalAs(UnmanagedType.LPWStr)] string? templateName,
            IntPtr progressSink);

        [PreserveSig] int PerformOperations();

        [PreserveSig] int GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool anyAborted);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        [PreserveSig] int BindToHandler(IntPtr bindContext, ref Guid handler, ref Guid riid, out IntPtr result);

        [PreserveSig] int GetParent(out IShellItem parent);

        [PreserveSig] int GetDisplayName(uint format, [MarshalAs(UnmanagedType.LPWStr)] out string name);

        [PreserveSig] int GetAttributes(uint mask, out uint attributes);

        [PreserveSig] int Compare(IShellItem other, uint hint, out int order);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string path,
        IntPtr bindContext,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);
}
