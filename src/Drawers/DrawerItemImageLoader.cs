using System.Drawing.Drawing2D;

namespace winDrawer;

internal static class DrawerItemImageLoader
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp",
        ".gif",
        ".ico",
        ".jpeg",
        ".jpg",
        ".jfif",
        ".png",
        ".tif",
        ".tiff",
        ".webp"
    };

    public static Image Load(DrawerItemInfo item, Size imageSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageSize));
        }

        cancellationToken.ThrowIfCancellationRequested();
        Image? image = null;
        try
        {
            if (!item.IsDirectory && IsImageFile(item.Path))
            {
                image = TryCreateThumbnail(item.Path, imageSize, cancellationToken);
            }

            image ??= GetShellIconImage(item.Path, item.IsDirectory);
            cancellationToken.ThrowIfCancellationRequested();

            var result = image;
            image = null;
            return result;
        }
        finally
        {
            image?.Dispose();
        }
    }

    private static Bitmap GetShellIconImage(string path, bool isDirectory)
    {
        try
        {
            using var icon = ShellIcon.GetIcon(path, isDirectory);
            return (icon ?? SystemIcons.WinLogo).ToBitmap();
        }
        catch (Exception ex)
        {
            DrawerLogger.LogDebug($"Shell icon lookup failed for '{path}'. {ex}");
            return SystemIcons.WinLogo.ToBitmap();
        }
    }

    private static bool IsImageFile(string path)
    {
        var extension = Path.GetExtension(path);
        return !string.IsNullOrEmpty(extension) && ImageExtensions.Contains(extension);
    }

    private static Bitmap? TryCreateThumbnail(string path, Size targetSize, CancellationToken cancellationToken)
    {
        Bitmap? bitmap = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var original = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);

            if (original.Width <= 0 || original.Height <= 0)
            {
                return null;
            }

            var ratio = Math.Min(
                (float)targetSize.Width / original.Width,
                (float)targetSize.Height / original.Height);
            var scaledWidth = Math.Max(1, (int)Math.Round(original.Width * ratio));
            var scaledHeight = Math.Max(1, (int)Math.Round(original.Height * ratio));

            bitmap = new Bitmap(targetSize.Width, targetSize.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                var x = (targetSize.Width - scaledWidth) / 2;
                var y = (targetSize.Height - scaledHeight) / 2;
                graphics.DrawImage(original, new Rectangle(x, y, scaledWidth, scaledHeight));
            }

            cancellationToken.ThrowIfCancellationRequested();
            var thumbnail = bitmap;
            bitmap = null;
            return thumbnail;
        }
        catch (OperationCanceledException)
        {
            bitmap?.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            bitmap?.Dispose();
            DrawerLogger.LogDebug($"Thumbnail generation failed for '{path}'. {ex}");
            return null;
        }
    }
}