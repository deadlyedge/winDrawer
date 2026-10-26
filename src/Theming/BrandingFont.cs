using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace winDrawer;

internal static class BrandingFont
{
    private const string ResourcePrefix = "winDrawer.Branding.";
    private static readonly Lock SyncLock = new();
    private static PrivateFontCollection? _fontCollection;
    private static FontFamily? _fontFamily;
    private static byte[]? _fontData;
    private static GCHandle _fontDataHandle;
    private static bool _isDisposed;
    private static bool _loadAttempted;

    public static Font Create(float size)
    {
        lock (SyncLock)
        {
            if (TryLoadFont())
            {
                return new Font(_fontFamily!, size, FontStyle.Regular, GraphicsUnit.Point);
            }

            return new Font("Segoe UI", size, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    public static Font CreateFallback(float size)
    {
        return new Font("Segoe UI", size, FontStyle.Regular, GraphicsUnit.Point);
    }

    internal static string LoadedFamilyName
    {
        get
        {
            lock (SyncLock)
            {
                return TryLoadFont() ? _fontFamily!.Name : string.Empty;
            }
        }
    }

    private static bool TryLoadFont()
    {
        if (_fontFamily != null)
        {
            return true;
        }

        if (_isDisposed || _loadAttempted)
        {
            return false;
        }

        _loadAttempted = true;
        var resourceName = ResourcePrefix + AppConstants.Files.LogoFontFileName;
        PrivateFontCollection? fontCollection = null;
        GCHandle fontDataHandle = default;
        try
        {
            using var stream = typeof(BrandingFont).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded branding font resource '{resourceName}' was not found.");
            if (stream.Length > int.MaxValue)
            {
                throw new InvalidDataException($"Embedded branding font resource '{resourceName}' is too large.");
            }

            var fontData = new byte[(int)stream.Length];
            stream.ReadExactly(fontData);
            fontDataHandle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
            fontCollection = new PrivateFontCollection();
            fontCollection.AddMemoryFont(fontDataHandle.AddrOfPinnedObject(), fontData.Length);

            var fontFamily = fontCollection.Families.FirstOrDefault(family =>
                string.Equals(family.Name, "Lobster", StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidDataException($"Embedded branding font resource '{resourceName}' does not contain the Lobster font family.");
            _fontData = fontData;
            _fontDataHandle = fontDataHandle;
            _fontCollection = fontCollection;
            _fontFamily = fontFamily;
            fontDataHandle = default;
            fontCollection = null;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            return true;
        }
        catch (Exception ex)
        {
            DrawerLogger.LogWarning($"Unable to load embedded branding font '{resourceName}'. {ex.Message}");
            if (fontDataHandle.IsAllocated)
            {
                fontDataHandle.Free();
            }

            fontCollection?.Dispose();
            return false;
        }
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        lock (SyncLock)
        {
            _isDisposed = true;
            _fontFamily?.Dispose();
            _fontCollection?.Dispose();
            if (_fontDataHandle.IsAllocated)
            {
                _fontDataHandle.Free();
            }

            _fontFamily = null;
            _fontCollection = null;
            _fontData = null;
        }
    }
}