using System.Numerics;

namespace Electron2D;

/// <summary>
/// Команда отрисовки одного спрайта (backend-agnostic).
/// </summary>
internal struct SpriteCommand
{
    public Texture Texture;
    public Rect SrcRect;

    public Vector2 PositionWorld;
    public Vector2 SizeWorld;
    public float Rotation;
    public Vector2 OriginWorld;

    public Color Color;

    /// <summary>Ключ сортировки (слой/порядок). Чем меньше — тем раньше рисуется.</summary>
    public uint SortKey;
    
    // NEW: packing rotation/flip спрайта (Sprite.FlipMode)
    public FlipMode FlipMode;

    /// <summary>
    /// Последовательность вставки (назначается очередью) для стабильной сортировки при равных SortKey.
    /// </summary>
    internal uint Sequence;

    internal ulong StableKey => ((ulong)SortKey << 32) | Sequence;

    internal void SetSequence(uint sequence) => Sequence = sequence;
}