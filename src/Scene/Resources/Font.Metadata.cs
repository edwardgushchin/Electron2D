namespace Electron2D;

public partial class Font
{
    /// <summary>Copies the primary face's supported OpenType shaping feature descriptions.</summary>
    /// <returns>A caller-owned dictionary keyed by numeric four-byte tags; empty when the primary source has no feature tables.</returns>
    /// <remarks>Includes GSUB and GPOS tags, with GPOS winning a duplicate. Labels use the tool locale captured when
    /// the native face was realized. Fallback features and configured feature overrides are not merged into this result.
    /// Lookup may realize native data and allocates the returned snapshot; retain it outside frame loops.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    /// <exception cref="InvalidDataException">Native source tables exceed a supported count or text range.</exception>
    public Dictionary<uint, FontFeatureInfo> GetSupportedFeatureList()
    {
        if (TryGetCallbackSource(out _)) return QueryCallbackSource(static (server, rid) => server.FontSupportedFeatureList(rid));
        lock (FontGate) { ThrowIfDisposed(); return PrimaryData is { } data ? new(data.SupportedFeatures) : []; }
    }

    /// <summary>Copies localized OpenType name strings from the primary face.</summary>
    /// <returns>A caller-owned language-to-name dictionary with independently copied inner dictionaries.</returns>
    /// <remarks>Languages use native BCP 47 spellings. Name keys include family_name, full_name, version, license and
    /// other standard table roles; unrecognized IDs use unknown_ followed by their decimal ID. Empty strings are omitted.
    /// A 64 MiB UTF-16 text budget bounds each metadata capture before publication.
    /// No names are inferred from resource properties or fallback faces. Lookup may realize native data and allocates a snapshot.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    /// <exception cref="InvalidDataException">Native source tables exceed a supported count or text range.</exception>
    public Dictionary<string, Dictionary<string, string>> GetOTNameStrings()
    {
        if (TryGetCallbackSource(out _)) return QueryCallbackSource(static (server, rid) => server.FontGetOTNameStrings(rid));
        lock (FontGate)
        {
            ThrowIfDisposed(); var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            if (PrimaryData is { } data)
                foreach (var pair in data.GetNameStrings()) result.Add(pair.Key, new(pair.Value, StringComparer.Ordinal));
            return result;
        }
    }
}
