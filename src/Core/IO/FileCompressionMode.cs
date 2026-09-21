namespace Electron2D;

/// <summary>Identifies the codec used by a compressed <see cref="FileAccess"/> container.</summary>
public enum FileCompressionMode
{
    /// <summary>Uses the FastLZ codec. The current runtime has no FastLZ provider.</summary>
    FastLz = 0,

    /// <summary>Uses the DEFLATE codec.</summary>
    Deflate = 1,

    /// <summary>Uses the Zstandard codec. The current runtime has no Zstandard provider.</summary>
    Zstandard = 2,

    /// <summary>Uses the GZip container and DEFLATE codec.</summary>
    Gzip = 3,

    /// <summary>Uses the Brotli codec.</summary>
    Brotli = 4
}
