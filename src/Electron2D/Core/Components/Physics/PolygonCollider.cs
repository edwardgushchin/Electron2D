using System.Numerics;

namespace Electron2D;

/// <summary>
/// Полигональный коллайдер (многоугольник в локальных координатах).
/// </summary>
public class PolygonCollider : Collider
{
    /// <summary>
    /// Вершины полигона в локальных координатах.
    /// </summary>
    public Vector2[] Points { get; set; } =
    [
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
        new Vector2(0f, 0.5f)
    ];
}