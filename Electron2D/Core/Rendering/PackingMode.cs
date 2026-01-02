namespace Electron2D;

/// <summary>
/// Режим упаковки/обрезки спрайта (как интерпретировать исходную область при рендере/атласе).
/// </summary>
public enum PackingMode : byte
{
    None = 0,
    Rectangle = 1,
    Tight = 2
}