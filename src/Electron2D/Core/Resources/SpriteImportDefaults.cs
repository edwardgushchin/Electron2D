using System.Numerics;

namespace Electron2D;

/// <summary>
/// Дефолтные импорт-настройки для спрайтов, привязанные к текстуре/спрайтшиту.
/// Цель: убрать повторяющийся boilerplate (PPU/pivot/filter) из кода анимаций.
/// </summary>
public readonly record struct SpriteImportDefaults(
    float PixelsPerUnit,
    Vector2 Pivot,
    FilterMode FilterMode)
{
    public static SpriteImportDefaults Default => new(
        PixelsPerUnit: 100f,
        Pivot: new Vector2(0.5f, 0.5f),
        FilterMode: FilterMode.Inherit);
}
