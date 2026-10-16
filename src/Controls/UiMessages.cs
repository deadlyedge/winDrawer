namespace winDrawer;

/// <summary>
/// Thin wrapper over <see cref="MessageBox"/> so captions, icons and button sets are decided in one
/// place instead of being spelled out again at every call site.
/// </summary>
internal static class UiMessages
{
    public static void ShowError(IWin32Window? owner, string caption, string message)
    {
        Show(owner, caption, message, MessageBoxIcon.Error);
    }

    public static void ShowWarning(IWin32Window? owner, string caption, string message)
    {
        Show(owner, caption, message, MessageBoxIcon.Warning);
    }

    public static void ShowInformation(IWin32Window? owner, string caption, string message)
    {
        Show(owner, caption, message, MessageBoxIcon.Information);
    }

    /// <summary>Asks a Yes/No question and reports whether the user confirmed it.</summary>
    public static bool Confirm(IWin32Window? owner, string caption, string message)
    {
        return MessageBox.Show(owner, message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
    }

    private static void Show(IWin32Window? owner, string caption, string message, MessageBoxIcon icon)
    {
        MessageBox.Show(owner, message, caption, MessageBoxButtons.OK, icon);
    }
}
