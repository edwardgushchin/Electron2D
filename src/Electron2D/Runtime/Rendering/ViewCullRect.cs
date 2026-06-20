using System.Numerics;
using System.Runtime.CompilerServices;

namespace Electron2D;

/// <summary>
/// Оси-ориентированный прямоугольник отсечения (view culling) в мировых координатах.
/// </summary>
internal readonly struct ViewCullRect(float minX, float minY, float maxX, float maxY)
{
    #region Instance fields

    public readonly float MinX = minX;
    public readonly float MinY = minY;
    public readonly float MaxX = maxX;
    public readonly float MaxY = maxY;

    #endregion

    #region Public API

    /// <summary>
    /// Проверяет пересечение прямоугольника отсечения с AABB, заданным минимумом и максимумом.
    /// </summary>
    /// <param name="aabbMin">Минимальная точка AABB (X/Y).</param>
    /// <param name="aabbMax">Максимальная точка AABB (X/Y).</param>
    /// <returns><c>true</c>, если прямоугольники пересекаются; иначе <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(in Vector2 aabbMin, in Vector2 aabbMax)
    {
        // Separating Axis Test (AABB vs AABB) без аллокаций.
        // Условие "не пересекаются" эквивалентно наличию разделяющей оси по X или Y.
        return !(aabbMax.X < MinX ||
                 aabbMax.Y < MinY ||
                 aabbMin.X > MaxX ||
                 aabbMin.Y > MaxY);
    }

    #endregion
}