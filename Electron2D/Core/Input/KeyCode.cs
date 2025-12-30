using SDL3;

namespace Electron2D;

/// <summary>
/// Физические клавиши (scancode-подобная модель).
/// Значения можно держать совместимыми с SDL scancode индексами.
/// </summary>
public enum KeyCode
{
    Unknown = 0,
    A = 4,
    D = 7,
    W = 26,
    S = 22,
    E = SDL.Scancode.E,
    Q = SDL.Scancode.Q,
    Space = 44,
    Escape = 41,
    Left = 80,
    Right = 79,
    Up = 82,
    Down = 81,
    Kp2 = SDL.Scancode.Kp2,
    Kp8 = SDL.Scancode.Kp8,
    Kp7 = SDL.Scancode.Kp7,
    Kp9 = SDL.Scancode.Kp9,
    Kp4 = SDL.Scancode.Kp4,
    Kp6 = SDL.Scancode.Kp6,
    Kp5 = SDL.Scancode.Kp5,
    Kp0 = SDL.Scancode.Kp0,
    KpPlus = SDL.Scancode.KpPlus,
    KpMinus = SDL.Scancode.KpMinus,
    Backspace = SDL.Scancode.Backspace,
}