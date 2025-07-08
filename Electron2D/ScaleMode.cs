using SDL3;

namespace Electron2D;

/// <summary>
/// The scaling mode.
/// </summary>
/// <since>This enum is available since SDL 3.2.0</since>
public enum ScaleMode
{
    Invalid = SDL.ScaleMode.Invalid,
        
    /// <summary>
    /// nearest pixel sampling
    /// </summary>
    Nearest = SDL.ScaleMode.Nearest,

    /// <summary>
    /// linear filtering
    /// </summary>
    Linear = SDL.ScaleMode.Linear,
        
    /// <summary>
    /// nearest pixel sampling with improved scaling for pixel art
    /// </summary>
    PixelArt =  SDL.ScaleMode.PixelArt,
}