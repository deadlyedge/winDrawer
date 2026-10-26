using System.Drawing.Text;

namespace winDrawer;

/// <summary>
/// Symbol glyphs for the main-window header buttons. Windows 10 and 11 ship Segoe MDL2 Assets; when
/// that font is missing, plain Unicode symbols from Segoe UI Symbol keep the buttons readable instead
/// of showing empty boxes.
/// </summary>
internal static class IconGlyphs
{
    private const string SymbolFontFamilyName = "Segoe MDL2 Assets";
    private const string FallbackFontFamilyName = "Segoe UI Symbol";

    private static bool? _symbolFontInstalled;

    /// <summary>Glyph for the "add drawer" action.</summary>
    public static string AddDrawer => SymbolFontInstalled ? "\uE710" : "+";

    /// <summary>Glyph for the "settings" action.</summary>
    public static string Settings => SymbolFontInstalled ? "\uE713" : "\u2699";

    /// <summary>Font family that contains the glyphs returned by this class.</summary>
    public static string FontFamilyName => SymbolFontInstalled ? SymbolFontFamilyName : FallbackFontFamilyName;

    private static bool SymbolFontInstalled => _symbolFontInstalled ??= IsFontInstalled(SymbolFontFamilyName);

    private static bool IsFontInstalled(string familyName)
    {
        using var installedFonts = new InstalledFontCollection();
        return installedFonts.Families.Any(family => string.Equals(family.Name, familyName, StringComparison.OrdinalIgnoreCase));
    }
}
