namespace Electron2D;
/// <summary>Selects portable resource-file dependency, path and binary policies.</summary>
[Flags]
public enum SaverFlags
{
    /// <summary>Uses ordinary absolute dependency paths and little-endian uncompressed data.</summary>
    None = 0,
    /// <summary>Stores external dependency paths relative to the destination.</summary>
    RelativePaths = 1,
    /// <summary>Embeds external resources into this archive.</summary>
    BundleResources = 2,
    /// <summary>Temporarily exposes the destination ResourcePath while saving, then restores it.</summary>
    ChangePath = 4,
    /// <summary>Omits reserved editor metadata properties.</summary>
    OmitEditorProperties = 8,
    /// <summary>Stores numeric payload values with big-endian byte ordering.</summary>
    SaveBigEndian = 16,
    /// <summary>Compresses the archive payload with bounded Deflate.</summary>
    Compress = 32,
    /// <summary>Assigns internal subresource paths after a successful archive replacement.</summary>
    ReplaceSubresourcePaths = 64
}
