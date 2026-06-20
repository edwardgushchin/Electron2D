using System.Numerics;

namespace Electron2D;

/// <summary>
/// Axis-aligned bounding box (AABB) in 2D, Unity-like API.
/// Stored as Center + Extents (half-size).
/// </summary>
public struct Bounds
{
    private Vector2 _center;
    private Vector2 _extents;

    /// <summary>The center of the bounding box.</summary>
    public Vector2 Center
    {
        readonly get => _center; 
        set => _center = value;
    }

    /// <summary>The total size of the box. This is always twice as large as the extents.</summary>
    public Vector2 Size
    {
        readonly get => _extents * 2f;
        set => _extents = Abs(value) * 0.5f;
    }

    /// <summary>The extents of the box. This is always half of the size.</summary>
    public Vector2 Extents
    {
        readonly get => _extents;
        set => _extents = Abs(value);
    }

    /// <summary>The minimal point of the box. This is always equal to center-extents.</summary>
    public Vector2 Min
    {
        readonly get => _center - _extents;
        set => SetMinMax(value, Max);
    }

    /// <summary>The maximal point of the box. This is always equal to center+extents.</summary>
    public Vector2 Max
    {
        readonly get => _center + _extents;
        set => SetMinMax(Min, value);
    }

    /// <summary>
    /// Creates new Bounds with a given center and total size. Bound extents will be half the given size.
    /// Unity signature: Bounds(center, size).
    /// </summary>
    public Bounds(Vector2 center, Vector2 size)
    {
        _center = center;
        _extents = Abs(size) * 0.5f;
    }

    /// <summary>Sets the bounds to the min and max value of the box.</summary>
    public void SetMinMax(Vector2 min, Vector2 max)
    {
        var mn = new Vector2(MathF.Min(min.X, max.X), MathF.Min(min.Y, max.Y));
        var mx = new Vector2(MathF.Max(min.X, max.X), MathF.Max(min.Y, max.Y));

        _extents = (mx - mn) * 0.5f;
        _center = mn + _extents;
    }

    /// <summary>Is point contained in the bounding box?</summary>
    public readonly bool Contains(Vector2 point)
    {
        var mn = Min;
        var mx = Max;
        return point.X >= mn.X && point.X <= mx.X &&
               point.Y >= mn.Y && point.Y <= mx.Y;
    }

    /// <summary>The closest point on the bounding box.</summary>
    public readonly Vector2 ClosestPoint(Vector2 point)
    {
        var mn = Min;
        var mx = Max;
        return new Vector2(
            Clamp(point.X, mn.X, mx.X),
            Clamp(point.Y, mn.Y, mx.Y)
        );
    }

    /// <summary>Grows the Bounds to include the point.</summary>
    public void Encapsulate(Vector2 point)
    {
        var mn = Min;
        var mx = Max;

        mn = new Vector2(MathF.Min(mn.X, point.X), MathF.Min(mn.Y, point.Y));
        mx = new Vector2(MathF.Max(mx.X, point.X), MathF.Max(mx.Y, point.Y));

        SetMinMax(mn, mx);
    }

    /// <summary>Grows the Bounds to include the other bounds.</summary>
    public void Encapsulate(Bounds bounds)
    {
        Encapsulate(bounds.Min);
        Encapsulate(bounds.Max);
    }

    /// <summary>
    /// Expand the bounds by increasing its size by amount along each side.
    /// Unity: size += (amount, amount).
    /// </summary>
    public void Expand(float amount)
    {
        Size = Size + new Vector2(amount, amount);
    }

    public void Expand(Vector2 amount)
    {
        Size = Size + amount;
    }

    /// <summary>Does another bounding box intersect with this bounding box?</summary>
    public readonly bool Intersects(Bounds bounds)
    {
        var aMin = Min;
        var aMax = Max;
        var bMin = bounds.Min;
        var bMax = bounds.Max;

        return aMin.X <= bMax.X && aMax.X >= bMin.X &&
               aMin.Y <= bMax.Y && aMax.Y >= bMin.Y;
    }

    /// <summary>The smallest squared distance between the point and this bounding box.</summary>
    public readonly float SqrDistance(Vector2 point)
    {
        var mn = Min;
        var mx = Max;

        var dx = 0f;
        if (point.X < mn.X) dx = mn.X - point.X;
        else if (point.X > mx.X) dx = point.X - mx.X;

        var dy = 0f;
        if (point.Y < mn.Y) dy = mn.Y - point.Y;
        else if (point.Y > mx.Y) dy = point.Y - mx.Y;

        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Does ray intersect this bounding box?
    /// If origin is inside bounds, distance is 0.
    /// </summary>
    public readonly bool IntersectRay(Ray ray, out float distance)
    {
        var mn = Min;
        var mx = Max;

        const float eps = 1e-8f;

        var tmin = 0f;
        var tmax = float.PositiveInfinity;

        // X slab
        if (MathF.Abs(ray.Direction.X) < eps)
        {
            if (ray.Origin.X < mn.X || ray.Origin.X > mx.X)
            {
                distance = 0f;
                return false;
            }
        }
        else
        {
            var inv = 1f / ray.Direction.X;
            var t1 = (mn.X - ray.Origin.X) * inv;
            var t2 = (mx.X - ray.Origin.X) * inv;
            if (t1 > t2) (t1, t2) = (t2, t1);

            tmin = MathF.Max(tmin, t1);
            tmax = MathF.Min(tmax, t2);

            if (tmin > tmax)
            {
                distance = 0f;
                return false;
            }
        }

        // Y slab
        if (MathF.Abs(ray.Direction.Y) < eps)
        {
            if (ray.Origin.Y < mn.Y || ray.Origin.Y > mx.Y)
            {
                distance = 0f;
                return false;
            }
        }
        else
        {
            var inv = 1f / ray.Direction.Y;
            var t1 = (mn.Y - ray.Origin.Y) * inv;
            var t2 = (mx.Y - ray.Origin.Y) * inv;
            if (t1 > t2) (t1, t2) = (t2, t1);

            tmin = MathF.Max(tmin, t1);
            tmax = MathF.Min(tmax, t2);

            if (tmin > tmax)
            {
                distance = 0f;
                return false;
            }
        }

        distance = tmin;
        return true;
    }

    public readonly bool IntersectRay(Ray ray) => IntersectRay(ray, out _);

    public readonly override string ToString()
        => $"Center: ({_center.X:0.###}, {_center.Y:0.###}), Extents: ({_extents.X:0.###}, {_extents.Y:0.###})";

    private static float Clamp(float v, float mn, float mx) => v < mn ? mn : (v > mx ? mx : v);
    private static Vector2 Abs(Vector2 v) => new(MathF.Abs(v.X), MathF.Abs(v.Y));
}
