namespace Electron2D;

/// <summary>
/// В каком координатном пространстве интерпретируется команда рендера.
/// </summary>
/// <remarks>
/// World: мировые координаты (камера, y-up).
/// Screen: экранные координаты (render space), (0,0) = top-left, y-down.
/// </remarks>
public enum RenderSpace : byte
{
    World = 0,
    Screen = 1
}