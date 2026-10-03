namespace Electron2D;

/// <summary>Selects the project default transport without a recursive Default option.</summary>
public enum AudioDefaultPlaybackType
{
    /// <summary>Mix prepared stream blocks.</summary>
    Stream = 0,
    /// <summary>Use native finite samples when supported by a resource, otherwise stream.</summary>
    Sample = 1
}
