using SDL3;

namespace Electron2D.Inputs;

[Flags]
public enum MouseButtonFlags : uint
{
    Left = SDL.MouseButtonFlags.Left,
    Middle = SDL.MouseButtonFlags.Middle,
    Right = SDL.MouseButtonFlags.Right,
    X1 = SDL.MouseButtonFlags.X1,
    X2 = SDL.MouseButtonFlags.X2,
}