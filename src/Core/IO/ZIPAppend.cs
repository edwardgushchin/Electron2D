namespace Electron2D;

/// <summary>Selects creation or extension of a ZIP archive.</summary>
public enum ZIPAppend
{
    /// <summary>Creates or truncates the destination.</summary>
    Create = 0,
    /// <summary>Creates an archive after existing non-archive bytes.</summary>
    CreateAfter = 1,
    /// <summary>Preserves an existing archive's entries and adds new entries.</summary>
    AddInZIP = 2,
}

/// <summary>Names commonly selected deflate levels; every integer from -1 through 9 is supported.</summary>
public enum ZIPCompressionLevel
{
    /// <summary>Uses the compressor's default balance.</summary>
    Default = -1,
    /// <summary>Writes uncompressed blocks within the deflate stream.</summary>
    None = 0,
    /// <summary>Prioritizes compression speed.</summary>
    Fast = 1,
    /// <summary>Prioritizes compressed size.</summary>
    Best = 9,
}
