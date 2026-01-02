using System.Numerics;

namespace Electron2D;

/// <summary>
/// Команда отрисовки одного спрайта (backend-agnostic).
/// </summary>
internal struct SpriteCommand
{
    #region Instance fields

    /// <summary>Текстура спрайта.</summary>
    public Texture Texture;

    /// <summary>Источник в текстуре (в texel-координатах).</summary>
    public Rect SrcRect;

    /// <summary>Позиция спрайта в мировых координатах.</summary>
    public Vector2 PositionWorld;

    /// <summary>Размер спрайта в мировых координатах.</summary>
    public Vector2 SizeWorld;

    /// <summary>Поворот (в радианах).</summary>
    public float Rotation;

    /// <summary>Точка начала (pivot) в мировых координатах.</summary>
    public Vector2 OriginWorld;

    /// <summary>Модуляция цвета.</summary>
    public Color Color;

    /// <summary>Ключ сортировки (слой/порядок). Чем меньше — тем раньше рисуется.</summary>
    public uint SortKey;

    /// <summary>Флаги отражения/поворота, связанные со спрайтом (например, Sprite.FlipMode).</summary>
    public FlipMode FlipMode;

    /// <summary>
    /// Последовательность вставки (назначается очередью) для стабильной сортировки при равных SortKey.
    /// </summary>
    internal uint Sequence;

    #endregion

    #region Properties

    /// <summary>
    /// Стабильный ключ сортировки: старшие 32 бита — <see cref="SortKey"/>, младшие 32 бита — <see cref="Sequence"/>.
    /// </summary>
    internal readonly ulong StableKey => ((ulong)SortKey << 32) | Sequence;

    #endregion

    #region Internal helpers

    internal void SetSequence(uint sequence) => Sequence = sequence;

    #endregion
}