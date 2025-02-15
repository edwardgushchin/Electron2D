using SDL3;

namespace Electron2D.Input;

/// <summary>
/// <para>Standard gamepad types.</para>
/// <para>This type does not necessarily map to first-party controllers from
/// Microsoft/Sony/Nintendo; in many cases, third-party controllers can report
/// as these, either because they were designed for a specific console, or they
/// simply most closely match that console's controllers (does it have A/B/X/Y
/// buttons or X/O/Square/Triangle? Does it have a touchpad? etc).</para>
/// </summary>
public enum GamepadType
{
    Unknown = SDL.GamepadType.Unknown,
    Standard = SDL.GamepadType.Standard,
    Xbox360 = SDL.GamepadType.Xbox360,
    XboxOne = SDL.GamepadType.XboxOne,
    PS3 = SDL.GamepadType.PS3,
    PS4 = SDL.GamepadType.PS4,
    PS5 = SDL.GamepadType.PS5,
    NintendoSwitchPro = SDL.GamepadType.NintendoSwitchPro,
    NintendoSwitchJoyconLeft = SDL.GamepadType.NintendoSwitchJoyconLeft,
    NintendoSwitchJoyconRight = SDL.GamepadType.NintendoSwitchJoyconRight,
    NintendoSwitchJoyconPair = SDL.GamepadType.NintendoSwitchJoyconPair
}