using System;
using SDL3;

namespace Electron2D;

/// <summary>
/// Режим отражения спрайта (флаги). Значения совместимы с <see cref="SDL.FlipMode"/>.
/// </summary>
[Flags]
public enum FlipMode : int
{
    None = (int)SDL.FlipMode.None,
    Horizontal = (int)SDL.FlipMode.Horizontal,
    Vertical = (int)SDL.FlipMode.Vertical,
    HorizontalAndVertical = (int)SDL.FlipMode.HorizontalAndVertical
}