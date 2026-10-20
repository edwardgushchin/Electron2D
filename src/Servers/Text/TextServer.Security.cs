namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Finds the first dictionary entry with the same Unicode confusable skeleton.</summary>
    /// <param name="text">Candidate text.</param><param name="dictionary">Ordered strings, consumed during this call.</param>
    /// <returns>The first matching index, or -1.</returns>
    /// <remarks>Case-sensitive Unicode 17 UTS 39 skeletons include explicit zeroes and malformed UTF-16 units. Use SpoofCheck for invisible characters.</remarks>
    /// <exception cref="ArgumentNullException">Text or a dictionary entry is null.</exception>
    /// <exception cref="ArgumentException">Combined input or a resulting skeleton exceeds 64 MiB of UTF-16, or the dictionary exceeds 1,048,576 entries.</exception>
    public int IsConfusable(string text, ReadOnlySpan<string> dictionary) => NativeTextBreak.IsConfusable(text, dictionary);
    /// <summary>Checks Unicode security restrictions, invisible sequences, mixed numbers and allowed identifier characters.</summary>
    /// <param name="text">Candidate text.</param><returns>True when any Unicode security check fails.</returns>
    /// <remarks>Uses Unicode 17 recommended/inclusion characters and moderately restrictive script policy; this is separate from identifier syntax validation.</remarks>
    /// <exception cref="ArgumentNullException">Text is null.</exception><exception cref="ArgumentException">Text exceeds 64 MiB of UTF-16.</exception>
    public bool SpoofCheck(string text) => NativeTextBreak.SpoofCheck(text);
    /// <summary>Returns an independent copy of the embedded native text database.</summary>
    /// <returns>The pinned ICU break, dictionary, property, normalization and confusable data.</returns>
    public byte[] GetSupportData() => NativeTextBreak.GetSupportData();
    /// <summary>Returns the default database export filename.</summary><returns>TextBreak78.dat.</returns>
    public string GetSupportDataFilename() => "TextBreak78.dat";
    /// <summary>Describes the embedded native text database.</summary><returns>The ICU and Unicode versions and supported data roles.</returns>
    public string GetSupportDataInfo() => "ICU 78.3 / Unicode 17 break, dictionary, property, normalization and confusable data (TextBreak78.dat).";
    /// <summary>Initializes the immutable embedded native text database.</summary>
    /// <param name="filename">Optional external database path; the embedded-data backend uses its packaged database regardless of this value.</param>
    /// <returns>True after initialization succeeds.</returns>
    /// <remarks>Calling this after text use has no effect. Native initialization failures propagate.</remarks>
    /// <exception cref="ArgumentNullException">Filename is null.</exception><exception cref="ArgumentException">Filename exceeds 64 MiB of UTF-16.</exception>
    public bool LoadSupportData(string filename) { NativeTextBreak.ValidateString(filename); NativeTextBreak.LoadSupportData(); return true; }
    /// <summary>Exports the embedded text database through the engine file service.</summary>
    /// <param name="filename">Writable destination path, including res:// or user://.</param><returns>True after writing and flushing; false for file access failures.</returns>
    /// <exception cref="ArgumentNullException">Filename is null.</exception>
    public bool SaveSupportData(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename); var data = GetSupportData();
        try { using var file = FileAccess.Open(filename, FileAccessModeFlags.Write); file.WriteBytes(data); file.Flush(); return true; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { return false; }
    }
    /// <summary>Tests whether a locale uses dictionary support for word or line breaking.</summary>
    /// <param name="locale">Case-sensitive locale with an underscore or hyphen separator.</param><returns>True for my, zh, ja, ko, km, lo or th.</returns>
    /// <exception cref="ArgumentNullException">Locale is null.</exception><exception cref="ArgumentException">Locale exceeds 64 MiB of UTF-16.</exception>
    public bool IsLocaleUsingSupportData(string locale)
    {
        NativeTextBreak.ValidateString(locale); var language = locale.AsSpan(); var separator = language.IndexOfAny('_', '-'); if (separator >= 0) language = language[..separator];
        return language.SequenceEqual("my") || language.SequenceEqual("zh") || language.SequenceEqual("ja") || language.SequenceEqual("ko") || language.SequenceEqual("km") || language.SequenceEqual("lo") || language.SequenceEqual("th");
    }
}
