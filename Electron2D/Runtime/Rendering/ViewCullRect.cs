using System.Numerics;
using System.Runtime.CompilerServices;

namespace Electron2D;

internal readonly struct ViewCullRect
{
    public readonly float MinX;
    public readonly float MinY;
    public readonly float MaxX;
    public readonly float MaxY;

    public ViewCullRect(float minX, float minY, float maxX, float maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(in Vector2 aabbMin, in Vector2 aabbMax)
    {
        // Separating axis test (AABB vs AABB)
        return !(aabbMax.X < MinX || aabbMax.Y < MinY || aabbMin.X > MaxX || aabbMin.Y > MaxY);
    }
}