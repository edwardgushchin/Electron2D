using System.Numerics;

namespace Electron2D;

/// <summary>
/// 2D bounds в мировых юнитах (центр + экстенты).
/// </summary>
public readonly struct Bounds
{
    public Vector2 Center { get; }
    public Vector2 Extents { get; }

    public Bounds(Vector2 center, Vector2 extents)
    {
        Center = center;
        Extents = extents;
    }
}