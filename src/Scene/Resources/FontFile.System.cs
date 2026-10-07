namespace Electron2D;

public partial class FontFile
{
    /// <summary>Gets or sets whether missing text glyphs may use owned platform font fallbacks.</summary>
    /// <value>True initially. Explicit fallback resources retain precedence; unavailable platform catalogs leave missing-glyph output.</value>
    /// <remarks>Discovery/loading occurs on first use outside a prepared interval. Cached fallback faces follow source policy, reader retirement and native ownership.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool AllowSystemFallback
    {
        get => Read(static data => data.AllowSystemFallback);
        set => SetConfiguration(static data => data.AllowSystemFallback, static (data, enabled) => data.AllowSystemFallback = enabled, value);
    }
}
