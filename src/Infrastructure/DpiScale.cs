namespace winDrawer;

/// <summary>
/// Converts 96-DPI logical pixels to device pixels. Results never fall below one physical pixel so
/// thin chrome elements stay visible at every scaling level.
/// </summary>
internal static class DpiScale
{
    /// <summary>Returns the DPI factor (1.0 at 96 DPI) and clamps invalid DPI values to 1.</summary>
    public static double Factor(int dpi)
    {
        return Math.Max(1, dpi) / 96d;
    }

    public static int Pixels(int logicalPixels, int dpi)
    {
        return Pixels(logicalPixels, Factor(dpi));
    }

    public static int Pixels(int logicalPixels, double scale)
    {
        return Math.Max(1, (int)Math.Round(logicalPixels * scale));
    }
}
