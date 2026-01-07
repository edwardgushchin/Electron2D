using System.Numerics;

namespace Electron2D;

/// <summary>
/// Box collider (AABB/OBB в зависимости от реализации физического бэкенда).
/// </summary>
public sealed class BoxCollider : Collider
{
    /// <summary>
    /// Размер коллайдера в локальных координатах (world units).
    /// </summary>
    public Vector2 Size { get; set; } = Vector2.One;
}