using SDL3;

namespace Electron2D;

public enum MouseWheelDirection
{
    /// <summary>
    /// The scroll direction is normal
    /// </summary>
    Normal = SDL.MouseWheelDirection.Normal,
        
    /// <summary>
    /// The scroll direction is flipped / natural
    /// </summary>
    Flipped = SDL.MouseWheelDirection.Flipped
}