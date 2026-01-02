using System.Numerics;

namespace Electron2D;

/// <summary>
/// 2D bounds в мировых юнитах (центр + экстенты).
/// </summary>
public readonly struct Bounds(Vector2 center, Vector2 extents)
{
    /// <summary>
    /// Центр bounds в мировых координатах.
    /// </summary>
    public Vector2 Center { get; } = center;

    /// <summary>
    /// Экстенты (половины размеров) по осям в мировых юнитах.
    /// </summary>
    public Vector2 Extents { get; } = extents;
}