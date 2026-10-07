namespace Electron2D;

public sealed partial class OS
{
    private readonly NativeSystemFonts _fonts = new();
    internal static NativeSystemFonts FontCatalog => Service._fonts;
    /// <summary>Returns installed scalable system font family names.</summary>
    /// <returns>An independent ordered array; empty when the platform catalog is unavailable.</returns>
    /// <remarks>Discovery is a cold operating-system query. The current Linux profile uses an owned optional Fontconfig configuration.</remarks>
    public static string[] GetSystemFonts() => FontCatalog.Names();
    /// <summary>Returns the platform's closest scalable font file for a preferred family and style.</summary>
    /// <param name="fontName">Preferred family; an empty name uses platform defaults.</param>
    /// <param name="weight">Preferred weight, clamped to 100 through 999.</param><param name="stretch">Preferred width percent, clamped to 50 through 200.</param><param name="italic">Whether italic is preferred.</param>
    /// <returns>The matched file path, or an empty string when no catalog match exists.</returns>
    /// <exception cref="ArgumentException">The family contains NUL or exceeds the text-query budget.</exception>
    /// <exception cref="ArgumentNullException">The family is null.</exception>
    public static string GetSystemFontPath(string fontName, int weight = 400, int stretch = 100, bool italic = false)
    {
        ValidateFontQuery(fontName); var matches = FontCatalog.Match(fontName, "", "", weight, stretch, italic); return matches.Length == 0 ? "" : matches[0].Path;
    }
    /// <summary>Returns ordered system font files covering text for a preferred family, locale and style.</summary>
    /// <param name="fontName">Preferred family, or empty for platform defaults.</param><param name="text">Text whose Unicode scalar coverage is requested.</param>
    /// <param name="locale">Optional locale preference.</param><param name="script">Optional script metadata; Linux matching uses character coverage and locale.</param>
    /// <param name="weight">Preferred weight, clamped to 100 through 999.</param><param name="stretch">Preferred width percent, clamped to 50 through 200.</param><param name="italic">Whether italic is preferred.</param>
    /// <returns>Independent distinct file paths, or an empty array when no catalog match exists.</returns>
    /// <exception cref="ArgumentException">A query contains NUL or exceeds 65536 UTF16 units.</exception><exception cref="ArgumentNullException">A query is null.</exception>
    public static string[] GetSystemFontPathForText(string fontName, string text, string locale = "", string script = "", int weight = 400, int stretch = 100, bool italic = false)
    {
        ValidateFontQuery(fontName); ValidateFontQuery(text); ValidateFontQuery(locale); ValidateFontQuery(script);
        return FontCatalog.Match(fontName, text, locale, weight, stretch, italic).Select(match => match.Path).Distinct(StringComparer.Ordinal).ToArray();
    }
    private static void ValidateFontQuery(string value) { ArgumentNullException.ThrowIfNull(value); if (value.Length > 65536 || value.Contains('\0')) throw new ArgumentException("Invalid system font query.", nameof(value)); }
}
