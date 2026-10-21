namespace Electron2D;

/// <summary>Defines one locale-specific source file for a logical resource path.</summary>
/// <remarks>Project settings copy these immutable records. Sources match exact resource-cache paths after UID
/// resolution. The selected file retains the source's logical ResourcePath; mappings do not chain.</remarks>
public sealed class ResourceLocaleRemap
{
    private static readonly System.Text.UTF8Encoding UTF8 = new(false, true);
    /// <summary>Initializes a locale candidate for one logical resource.</summary>
    /// <param name="sourcePath">Original native, res:// or user:// file path.</param>
    /// <param name="locale">Nonempty language/script/region identifier used by locale scoring.</param>
    /// <param name="localizedPath">Localized native, virtual or registered UID file path.</param>
    /// <exception cref="ArgumentException">A path or locale is empty, contains invalid UTF-16 or control characters,
    /// exceeds its bound, names a subresource, or uses a UID instead of a logical source file path.</exception>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public ResourceLocaleRemap(string sourcePath, string locale, string localizedPath)
    {
        ValidatePath(sourcePath, nameof(sourcePath)); ValidatePath(localizedPath, nameof(localizedPath));
        if (sourcePath.StartsWith("uid://", StringComparison.Ordinal)) throw new ArgumentException("A remap source uses the logical file path, after UID resolution.", nameof(sourcePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        if (locale.Length > 128 || locale.Any(char.IsControl)) throw new ArgumentException("A locale must contain at most 128 non-control characters.", nameof(locale));
        UTF8.GetByteCount(locale);
        SourcePath = sourcePath; Locale = locale.Trim(); LocalizedPath = localizedPath;
    }
    /// <summary>Gets the exact logical file path to localize.</summary>
    /// <value>The original path, independent of the selected physical file.</value>
    public string SourcePath { get; }
    /// <summary>Gets this candidate's locale identifier.</summary>
    /// <value>The trimmed locale used by TranslationServer.CompareLocales.</value>
    public string Locale { get; }
    /// <summary>Gets the file to decode when this candidate wins.</summary>
    /// <value>A native, virtual or registered UID file path.</value>
    public string LocalizedPath { get; }
    private static void ValidatePath(string path, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, parameter);
        if (path.Length > 4096 || path.Any(char.IsControl) || path.Contains("::", StringComparison.Ordinal)) throw new ArgumentException("A resource file path must be bounded, contain no control characters and omit subresource IDs.", parameter);
        UTF8.GetByteCount(path);
    }
}
