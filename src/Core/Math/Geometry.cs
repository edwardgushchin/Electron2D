namespace Electron2D;

/// <summary>Provides backend-independent two-dimensional geometry queries.</summary>
/// <remarks>Methods are stateless. Intersection queries return <see langword="null"/> when no unique intersection exists.</remarks>
public static class Geometry
{
    private const float ReferenceEpsilon = 0.00001f;

    /// <summary>Returns every integer grid point on a rasterized line, including both endpoints.</summary>
    /// <param name="from">The first grid point.</param>
    /// <param name="to">The final grid point.</param>
    /// <returns>The ordered grid points.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The line would require more than <see cref="int.MaxValue"/> points.</exception>
    public static Vector2I[] BresenhamLine(Vector2I from, Vector2I to)
    {
        var dx = Math.Abs((long)to.X - from.X);
        var dy = Math.Abs((long)to.Y - from.Y);
        var count = Math.Max(dx, dy) + 1;
        if (count > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(to), "The rasterized line exceeds the maximum array length.");

        var points = new Vector2I[(int)count];
        var x = from.X;
        var y = from.Y;
        var stepX = Math.Sign((long)to.X - from.X);
        var stepY = Math.Sign((long)to.Y - from.Y);
        var error = Math.Max(dx, dy);
        for (var i = 0; i < points.Length; i++)
        {
            points[i] = new Vector2I(x, y);
            if (i + 1 == points.Length) break;
            if (dx > dy)
            {
                error -= 2 * dy;
                if (error < 0) { y += stepY; error += 2 * dx; }
                x += stepX;
            }
            else
            {
                error -= 2 * dx;
                if (error < 0) { x += stepX; error += 2 * dy; }
                y += stepY;
            }
        }
        return points;
    }

    /// <summary>Returns the nearest point on a finite segment.</summary>
    /// <param name="point">The query point.</param>
    /// <param name="s1">The first endpoint.</param>
    /// <param name="s2">The second endpoint.</param>
    /// <returns>The nearest point, or <paramref name="s1"/> for a degenerate segment.</returns>
    public static Vector2 GetClosestPointToSegment(Vector2 point, Vector2 s1, Vector2 s2)
    {
        var direction = s2 - s1;
        var lengthSquared = direction.LengthSquared();
        if (lengthSquared < 1e-20f) return s1;
        var fraction = direction.Dot(point - s1) / lengthSquared;
        if (fraction <= 0f) return s1;
        if (fraction >= 1f) return s2;
        return s1 + direction * fraction;
    }

    /// <summary>Returns the nearest point on the infinite line through two segment endpoints.</summary>
    /// <param name="point">The query point.</param>
    /// <param name="s1">The first point on the line.</param>
    /// <param name="s2">The second point on the line.</param>
    /// <returns>The projection, or <paramref name="s1"/> if both endpoints coincide.</returns>
    public static Vector2 GetClosestPointToSegmentUncapped(Vector2 point, Vector2 s1, Vector2 s2)
    {
        var direction = s2 - s1;
        var lengthSquared = direction.LengthSquared();
        return lengthSquared < 1e-20f ? s1 : s1 + direction * (direction.Dot(point - s1) / lengthSquared);
    }

    /// <summary>Returns the nearest pair of points on two finite segments.</summary>
    /// <param name="p1">The start of the first segment.</param>
    /// <param name="q1">The end of the first segment.</param>
    /// <param name="p2">The start of the second segment.</param>
    /// <param name="q2">The end of the second segment.</param>
    /// <returns>Two points: first on <paramref name="p1"/>–<paramref name="q1"/>, then on <paramref name="p2"/>–<paramref name="q2"/>.</returns>
    public static Vector2[] GetClosestPointsBetweenSegments(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        var d1 = q1 - p1;
        var d2 = q2 - p2;
        var r = p1 - p2;
        var a = d1.Dot(d1);
        var e = d2.Dot(d2);
        var f = d2.Dot(r);
        float s, t;
        if (a <= ReferenceEpsilon && e <= ReferenceEpsilon) return [p1, p2];
        if (a <= ReferenceEpsilon)
        {
            s = 0f;
            t = Math.Clamp(f / e, 0f, 1f);
        }
        else
        {
            var c = d1.Dot(r);
            if (e <= ReferenceEpsilon)
            {
                t = 0f;
                s = Math.Clamp(-c / a, 0f, 1f);
            }
            else
            {
                var b = d1.Dot(d2);
                var denominator = a * e - b * b;
                s = denominator == 0f ? 0f : Math.Clamp((b * f - c * e) / denominator, 0f, 1f);
                t = (b * s + f) / e;
                if (t < 0f) { t = 0f; s = Math.Clamp(-c / a, 0f, 1f); }
                else if (t > 1f) { t = 1f; s = Math.Clamp((b - c) / a, 0f, 1f); }
            }
        }
        return [p1 + d1 * s, p2 + d2 * t];
    }

    /// <summary>Tests whether a point lies inside or on a circle.</summary>
    /// <param name="point">The query point.</param>
    /// <param name="circlePosition">The center.</param>
    /// <param name="circleRadius">The radius; its sign does not affect this squared-distance test.</param>
    /// <returns><see langword="true"/> when the squared distance is no greater than the squared radius.</returns>
    public static bool IsPointInCircle(Vector2 point, Vector2 circlePosition, float circleRadius) =>
        point.DistanceSquaredTo(circlePosition) <= circleRadius * circleRadius;

    /// <summary>Returns the unique intersection of two infinite lines.</summary>
    /// <param name="fromA">A point on the first line.</param>
    /// <param name="dirA">The first line's direction.</param>
    /// <param name="fromB">A point on the second line.</param>
    /// <param name="dirB">The second line's direction.</param>
    /// <returns>The intersection or <see langword="null"/> for parallel or coincident lines.</returns>
    public static Vector2? LineIntersectsLine(Vector2 fromA, Vector2 dirA, Vector2 fromB, Vector2 dirB)
    {
        var denominator = dirB.Y * dirA.X - dirB.X * dirA.Y;
        if (System.MathF.Abs(denominator) < ReferenceEpsilon) return null;
        var offset = fromA - fromB;
        var fraction = (dirB.X * offset.Y - dirB.Y * offset.X) / denominator;
        return fromA + dirA * fraction;
    }

    /// <summary>Returns the unique intersection of two finite segments.</summary>
    /// <param name="fromA">The first segment's start.</param>
    /// <param name="toA">The first segment's end.</param>
    /// <param name="fromB">The second segment's start.</param>
    /// <param name="toB">The second segment's end.</param>
    /// <returns>The intersection or <see langword="null"/> for disjoint, parallel, collinear, or degenerate segments.</returns>
    public static Vector2? SegmentIntersectsSegment(Vector2 fromA, Vector2 toA, Vector2 fromB, Vector2 toB)
    {
        var direction = toA - fromA;
        var relativeB = fromB - fromA;
        var relativeEndB = toB - fromA;
        var lengthSquared = direction.Dot(direction);
        if (lengthSquared <= 0f) return null;
        var normalized = direction / lengthSquared;
        var c = new Vector2(relativeB.Dot(normalized), relativeB.Y * normalized.X - relativeB.X * normalized.Y);
        var d = new Vector2(relativeEndB.Dot(normalized), relativeEndB.Y * normalized.X - relativeEndB.X * normalized.Y);
        if ((c.Y < -ReferenceEpsilon && d.Y < -ReferenceEpsilon) ||
            (c.Y > ReferenceEpsilon && d.Y > ReferenceEpsilon) ||
            c.Y == d.Y || System.MathF.Abs(c.Y - d.Y) < ReferenceEpsilon * System.MathF.Max(1f, System.MathF.Abs(c.Y))) return null;
        var fraction = d.X + (c.X - d.X) * d.Y / (d.Y - c.Y);
        return fraction < 0f || fraction > 1f ? null : fromA + direction * fraction;
    }

    /// <summary>Tests whether a point is inside a polygon or on its boundary.</summary>
    /// <param name="point">The query point.</param>
    /// <param name="polygon">The polygon vertices in perimeter order.</param>
    /// <returns><see langword="true"/> for interior and boundary points; <see langword="false"/> for fewer than three vertices.</returns>
    /// <remarks>Uses an odd-even crossing rule, so self-intersecting polygons follow that fill rule. Coordinates should be finite.</remarks>
    public static bool IsPointInPolygon(Vector2 point, ReadOnlySpan<Vector2> polygon)
    {
        if (polygon.Length < 3) return false;
        var inside = false;
        for (var i = 0; i < polygon.Length; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Length];
            if (GetClosestPointToSegment(point, a, b).IsEqualApprox(point)) return true;
            if ((a.Y > point.Y) == (b.Y > point.Y)) continue;
            var crossingX = (double)a.X + ((double)point.Y - a.Y) * ((double)b.X - a.X) / ((double)b.Y - a.Y);
            if (point.X < crossingX) inside = !inside;
        }
        return inside;
    }

    /// <summary>Tests whether polygon vertices run clockwise in Cartesian coordinates.</summary>
    /// <param name="polygon">The perimeter vertices.</param>
    /// <returns><see langword="true"/> for clockwise winding with positive Y up; <see langword="false"/> for fewer than three vertices or zero signed area.</returns>
    /// <remarks>Screen coordinates normally have positive Y down; there, a true result appears counterclockwise.</remarks>
    public static bool IsPolygonClockwise(ReadOnlySpan<Vector2> polygon)
    {
        if (polygon.Length < 3) return false;
        float sum = 0f;
        for (var i = 0; i < polygon.Length; i++)
        {
            var current = polygon[i];
            var next = polygon[(i + 1) % polygon.Length];
            sum += (next.X - current.X) * (next.Y + current.Y);
        }
        return sum > 0f;
    }

    /// <summary>Tests whether a point lies inside a triangle.</summary>
    /// <param name="point">The query point.</param>
    /// <param name="a">The first vertex.</param>
    /// <param name="b">The second vertex.</param>
    /// <param name="c">The third vertex.</param>
    /// <returns><see langword="true"/> when the three oriented edge tests agree.</returns>
    /// <remarks>Boundary and degenerate-triangle behavior follows strict signed-crossing tests; use <see cref="IsPointInPolygon"/> for inclusive polygon boundaries.</remarks>
    public static bool PointIsInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        var an = a - point;
        var bn = b - point;
        var cn = c - point;
        var orientation = an.Cross(bn) > 0f;
        return (bn.Cross(cn) > 0f) == orientation && (cn.Cross(an) > 0f) == orientation;
    }

    /// <summary>Returns the boundary of the convex hull of a point set.</summary>
    /// <param name="points">The input points; the input is not reordered.</param>
    /// <returns>Hull vertices in counterclockwise Cartesian order, with the first vertex repeated at the end when at least two points are supplied.</returns>
    /// <remarks>Collinear interior points are discarded. Results for nonfinite points are unspecified. The returned array is caller-owned.</remarks>
    public static Vector2[] ConvexHull(ReadOnlySpan<Vector2> points)
    {
        if (points.IsEmpty) return [];
        var sorted = points.ToArray();
        Array.Sort(sorted, static (left, right) => left.X == right.X ? left.Y.CompareTo(right.Y) : left.X.CompareTo(right.X));
        var hull = new Vector2[checked(sorted.Length * 2)];
        var count = 0;
        for (var i = 0; i < sorted.Length; i++)
        {
            while (count >= 2 && (hull[count - 1] - hull[count - 2]).Cross(sorted[i] - hull[count - 2]) <= 0f) count--;
            hull[count++] = sorted[i];
        }
        for (int i = sorted.Length - 2, start = count + 1; i >= 0; i--)
        {
            while (count >= start && (hull[count - 1] - hull[count - 2]).Cross(sorted[i] - hull[count - 2]) <= 0f) count--;
            hull[count++] = sorted[i];
        }
        Array.Resize(ref hull, count);
        return hull;
    }

    /// <summary>Returns the first boundary crossing of a segment and a circle.</summary>
    /// <param name="segmentFrom">The segment start.</param>
    /// <param name="segmentTo">The segment end.</param>
    /// <param name="circlePosition">The circle center.</param>
    /// <param name="circleRadius">The radius; its sign does not affect this squared-radius calculation.</param>
    /// <returns>A fraction in the closed interval zero to one, or -1 when there is no crossing on the segment.</returns>
    /// <remarks>If the segment starts inside the circle, the exit crossing is returned. A zero-length segment has no crossing.</remarks>
    public static float SegmentIntersectsCircle(Vector2 segmentFrom, Vector2 segmentTo, Vector2 circlePosition, float circleRadius)
    {
        var direction = segmentTo - segmentFrom;
        var offset = segmentFrom - circlePosition;
        var a = direction.Dot(direction);
        if (a == 0f) return -1f;
        var b = 2f * offset.Dot(direction);
        var c = offset.Dot(offset) - circleRadius * circleRadius;
        var discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) return -1f;
        var root = System.MathF.Sqrt(discriminant);
        var first = (-b - root) / (2f * a);
        var second = (-b + root) / (2f * a);
        if (first >= 0f && first <= 1f) return first;
        if (second >= 0f && second <= 1f) return second;
        return -1f;
    }
}
