namespace Electron2D;

/// <summary>
/// Отражение спрайта по осям (движковый enum).
/// Runtime маппит его на флаги SDL.
/// </summary>
[Flags]
public enum FlipMode : byte
{
    None = 0,
    Horizontal = 1 << 0,
    Vertical = 1 << 1,
}