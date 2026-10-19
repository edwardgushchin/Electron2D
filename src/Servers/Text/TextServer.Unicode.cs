namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Tests the language component against the supported right-to-left language policy.</summary>
    /// <param name="locale">Locale with an underscore or hyphen separator.</param><returns>True for ar, dv, he, fa, ff, ku or ur; an empty locale returns false.</returns>
    /// <exception cref="ArgumentNullException">A string argument is null.</exception>
    /// <exception cref="ArgumentException">A source, language or resulting string exceeds 64 MiB of UTF-16.</exception>
    public bool IsLocaleRightToLeft(string locale) { NativeTextBreak.ValidateString(locale); return locale.Length != 0 && NativeTextBreak.IsLocaleRTL(locale); }
    /// <summary>Tests an NFC Unicode identifier in recommended scripts, with contextual join controls.</summary>
    /// <param name="text">Candidate identifier.</param><returns>True for a complete valid identifier; false for empty or malformed text.</returns>
    /// <remarks>Mixed scripts are permitted. This does not perform confusable detection.</remarks>
    /// <exception cref="ArgumentNullException">A string argument is null.</exception>
    /// <exception cref="ArgumentException">A source, language or resulting string exceeds 64 MiB of UTF-16.</exception>
    public bool IsValidIdentifier(string text) => NativeTextBreak.IsIdentifier(text);
    /// <summary>Tests whether a Unicode scalar has a letter general category.</summary>
    /// <param name="unicode">Unicode scalar value.</param><returns>False for nonletters, invalid scalars and surrogate values.</returns>
    public bool IsValidLetter(int unicode) => NativeTextBreak.IsLetter(unicode);
    /// <summary>Resolves a registered feature or variation name, or a literal four-byte tag.</summary>
    /// <param name="name">Case-sensitive name; custom_ markers are removed from literal tags.</param><returns>A big-endian unsigned 32-bit OpenType tag, zero for an empty literal; short literals are space-padded.</returns>
    /// <exception cref="ArgumentNullException">A string argument is null.</exception>
    /// <exception cref="ArgumentException">A source, language or resulting string exceeds 64 MiB of UTF-16.</exception>
    public uint NameToTag(string name) { NativeTextBreak.ValidateString(name); return OpenTypeFeatureTags.Resolve(name); }
    /// <summary>Returns a registered tag name or its custom_ four-byte spelling.</summary>
    /// <param name="tag">Big-endian unsigned 32-bit OpenType tag.</param><returns>The canonical name, or a Latin-1 spelling terminated at the first zero byte.</returns>
    public string TagToName(uint tag) => OpenTypeFeatureTags.GetName(tag);
    /// <summary>Returns composite character end boundaries using the locale-aware Unicode iterator.</summary>
    /// <param name="text">Source text.</param><param name="language">Explicit locale, or empty for the tool locale.</param><returns>Independent Unicode scalar offsets, including the nonempty text end.</returns>
    /// <exception cref="ArgumentNullException">A string argument is null.</exception>
    /// <exception cref="ArgumentException">A source, language or resulting string exceeds 64 MiB of UTF-16.</exception>
    public int[] StringGetCharacterBreaks(string text, string language = "") => NativeTextBreak.StringCharacterBreaks(text, language);
    /// <summary>Returns word ranges, or scalar-width line ranges when a positive limit is supplied.</summary>
    /// <param name="text">Source text.</param><param name="language">Explicit locale, or empty for the tool locale.</param><param name="charsPerLine">Positive scalar limit for wrapping; nonpositive selects words.</param>
    /// <returns>Independent alternating Unicode scalar starts and ends; hard separators are excluded.</returns>
    /// <exception cref="ArgumentNullException">A string argument is null.</exception>
    /// <exception cref="ArgumentException">A source, language or resulting string exceeds 64 MiB of UTF-16.</exception>
    public int[] StringGetWordBreaks(string text, string language = "", int charsPerLine = 0) => NativeTextBreak.StringWordBreaks(text, language, charsPerLine);
    /// <summary>Converts text to lowercase with full Unicode and locale-sensitive rules.</summary>
    /// <param name="text">Source text.</param><param name="language">Explicit locale, or empty for the tool locale.</param><returns>Converted text; embedded zeroes and unpaired UTF-16 units are retained.</returns>
    /// <exception cref="ArgumentNullException">A string argument is null.</exception>
    /// <exception cref="ArgumentException">A source, language or resulting string exceeds 64 MiB of UTF-16.</exception>
    public string StringToLower(string text, string language = "") => NativeTextBreak.TransformString(text, language, 0);
    /// <summary>Titlecases Unicode words, lowercasing their remaining characters.</summary>
    /// <param name="text">Source text.</param><param name="language">Explicit locale, or empty for the tool locale.</param><returns>Locale-sensitive titlecase text with unchanged source control units.</returns>
    /// <exception cref="ArgumentNullException">A string argument is null.</exception>
    /// <exception cref="ArgumentException">A source, language or resulting string exceeds 64 MiB of UTF-16.</exception>
    public string StringToTitle(string text, string language = "") => NativeTextBreak.TransformString(text, language, 1);
    /// <summary>Converts text to uppercase with full Unicode expansions and locale tailoring.</summary>
    /// <param name="text">Source text.</param><param name="language">Explicit locale, or empty for the tool locale.</param><returns>Converted text; unchanged input can retain its original string.</returns>
    /// <exception cref="ArgumentNullException">A string argument is null.</exception>
    /// <exception cref="ArgumentException">A source, language or resulting string exceeds 64 MiB of UTF-16.</exception>
    public string StringToUpper(string text, string language = "")
    {
        NativeTextBreak.ValidateString(text); NativeTextBreak.ValidateString(language); var result = TextCase.ToUpper(text, language); NativeTextBreak.ValidateString(result); return result;
    }
    /// <summary>Applies compatibility decomposition and removes nonzero canonical combining classes.</summary>
    /// <param name="text">Source text.</param><returns>Decomposed text without nonzero combining classes; zero-class marks, embedded zeroes and unpaired UTF-16 units remain.</returns>
    /// <exception cref="ArgumentNullException">A string argument is null.</exception>
    /// <exception cref="ArgumentException">A source, language or resulting string exceeds 64 MiB of UTF-16.</exception>
    public string StripDiacritics(string text) => NativeTextBreak.TransformString(text, string.Empty, 2);
}
