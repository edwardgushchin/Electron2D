using SDL3;

namespace Electron2D.Input;

public enum GamepadAxis
{
    Invalid = SDL.GamepadAxis.Invalid,
    LeftX = SDL.GamepadAxis.LeftX,
    LeftY = SDL.GamepadAxis.LeftY,
    RightX = SDL.GamepadAxis.RightX,
    RightY = SDL.GamepadAxis.RightY,
    LeftTrigger = SDL.GamepadAxis.LeftTrigger,
    RightTrigger = SDL.GamepadAxis.RightTrigger,
    Count = SDL.GamepadAxis.Count
}