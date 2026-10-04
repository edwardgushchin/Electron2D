namespace Electron2D;

/// <summary>Identifies the CompressionMode domain of an ENet transport.</summary>
public enum ENetCompressionMode
{
    /// <summary>Disables payload compression.</summary>
    None = 0,
    /// <summary>Uses the prepared ENet range coder.</summary>
    RangeCoder = 1,
    /// <summary>Uses the FastLZ block codec.</summary>
    FASTLZ = 2,
    /// <summary>Uses the zlib block codec.</summary>
    ZLIB = 3,
    /// <summary>Uses the Zstandard block codec.</summary>
    ZSTD = 4,
}
