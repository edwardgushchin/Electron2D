using SDL3;

namespace Electron2D;

[Flags]
public enum FlipMode
{
    None = SDL.FlipMode.None,
    Horizontal = SDL.FlipMode.Horizontal,
    Vertical = SDL.FlipMode.Vertical,
    HorizontalAndVertical = SDL.FlipMode.HorizontalAndVertical
}