using SDL3;

namespace Electron2D;

/// <summary>
/// Физические клавиши (scancode-подобная модель).
/// Значения можно держать совместимыми с SDL scancode индексами.
/// </summary>
public enum Key
{
    Unknown = 0,
    A = 4,
    D = 7,
    W = 26,
    S = 22,
    Space = 44,
    Escape = 41,
    Left = 80,
    Right = 79,
    Up = 82,
    Down = 81,
    Backspace = SDL.Scancode.Backspace,
}