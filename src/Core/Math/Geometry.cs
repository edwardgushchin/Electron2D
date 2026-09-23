using Clipper2Lib;

namespace Electron2D;

/// <summary>Provides backend-independent two-dimensional geometry queries.</summary>
/// <remarks>Methods are stateless. Intersection queries return <see langword="null"/> when no unique intersection exists.</remarks>
public static class Geometry
{
    private const float ReferenceEpsilon = 0.00001f;
    private const int ClipperPrecision = 5;
    private const double ClipperCoordinateLimit = InternalClipper.MaxCoord / 100000d;

    /// <summary>Identifies a boolean operation on polygon regions.</summary>
    public enum PolyBooleanOperation
    {
        /// <summary>Combines both regions.</summary>
        Union = 0,
        /// <summary>Removes the second region from the first.</summary>
        Difference = 1,
        /// <summary>Keeps only the common region.</summary>
        Intersection = 2,
        /// <summary>Keeps points belonging to exactly one region.</summary>
        XOR = 3,
    }

    /// <summary>Selects the shape of an offset polygon corner or polyline join.</summary>
    public enum PolyJoinType
    {
        /// <summary>Clips the corner with a square join.</summary>
        Square = 0,
        /// <summary>Rounds the corner.</summary>
        Round = 1,
        /// <summary>Extends the two offset edges until they meet, subject to the miter limit.</summary>
        Miter = 2,
    }

    /// <summary>Selects the cap or closure of an offset polyline.</summary>
    public enum PolyEndType
    {
        /// <summary>Closes the input as a polygon; use <see cref="OffsetPolygon"/> for this shape.</summary>
        Polygon = 0,
        /// <summary>Joins both ends of a closed line.</summary>
        Joined = 1,
        /// <summary>Stops at the line endpoints.</summary>
        Butt = 2,
        /// <summary>Adds square endpoint caps.</summary>
        Square = 3,
        /// <summary>Adds round endpoint caps.</summary>
        Round = 4,
    }

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

    /// <summary>Partitions a simple polygon into convex polygons.</summary>
    /// <param name="polygon">The input contour in perimeter order, in either winding direction.</param>
    /// <returns>Caller-owned counterclockwise convex contours, or an empty array when decomposition fails.</returns>
    /// <remarks>An already convex contour is returned as one copy. Concave contours are triangulated and neighboring parts are merged while convexity is preserved. The input is unchanged; nonfinite coordinates have no defined result.</remarks>
    public static Vector2[][] DecomposePolygonInConvex(ReadOnlySpan<Vector2> polygon)
    {
        if (polygon.Length < 3) return [];
        double area = 0d;
        for (var i = 0; i < polygon.Length; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Length];
            area += (double)a.X * b.Y - (double)a.Y * b.X;
        }
        if (area == 0d) return [];

        var convex = true;
        for (var i = 0; i < polygon.Length; i++)
        {
            var turn = TriangleCross(polygon[(i + polygon.Length - 1) % polygon.Length], polygon[i], polygon[(i + 1) % polygon.Length]);
            if (area > 0d ? turn >= 0d : turn <= 0d) continue;
            convex = false;
            break;
        }
        if (convex)
        {
            var result = polygon.ToArray();
            if (area < 0d) Array.Reverse(result);
            return [result];
        }

        var triangles = TriangulatePolygon(polygon);
        if (triangles.Length == 0) return [];
        var parts = new List<int[]>(triangles.Length / 3);
        for (var i = 0; i < triangles.Length; i += 3)
            parts.Add([triangles[i], triangles[i + 1], triangles[i + 2]]);

        // Convex-part merging adapts Ivan Fratric's PolyPartition algorithm; see docs/licenses/PolyPartition-LICENSE.txt.
        // ponytail: pairwise edge search is cubic; use an edge index only if large contour decomposition needs it.
        for (var first = 0; first < parts.Count; first++)
        {
            for (var second = first + 1; second < parts.Count; second++)
            {
                if (!TryMergeConvexParts(polygon, parts[first], parts[second], out var merged)) continue;
                parts[first] = merged;
                parts.RemoveAt(second);
                second = first;
            }
        }

        var output = new Vector2[parts.Count][];
        for (var i = 0; i < parts.Count; i++)
        {
            output[i] = new Vector2[parts[i].Length];
            for (var j = 0; j < parts[i].Length; j++) output[i][j] = polygon[parts[i][j]];
        }
        return output;
    }

    /// <summary>Subtracts one polygon region from another.</summary>
    /// <param name="polygonA">The region to clip.</param>
    /// <param name="polygonB">The region to remove.</param>
    /// <returns>Boundary and hole contours of the remaining region.</returns>
    /// <remarks>Holes have the opposite winding from boundaries. Clipping uses five decimal digits of internal precision before converting to single-precision values.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is nonfinite or outside the clipping range.</exception>
    /// <exception cref="InvalidOperationException">The clipping backend fails.</exception>
    public static Vector2[][] ClipPolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB) =>
        OperatePolygons(polygonA, polygonB, PolyBooleanOperation.Difference, false);

    /// <summary>Removes the part of an open polyline inside a polygon.</summary>
    /// <param name="polyline">The open input line.</param>
    /// <param name="polygon">The clipping region.</param>
    /// <returns>The portions of the line outside the polygon.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is nonfinite or outside the clipping range.</exception>
    /// <exception cref="InvalidOperationException">The clipping backend fails.</exception>
    public static Vector2[][] ClipPolylineWithPolygon(ReadOnlySpan<Vector2> polyline, ReadOnlySpan<Vector2> polygon) =>
        OperatePolygons(polyline, polygon, PolyBooleanOperation.Difference, true);

    /// <summary>Returns the portions of two polygons outside their common area.</summary>
    /// <param name="polygonA">The first region.</param>
    /// <param name="polygonB">The second region.</param>
    /// <returns>Boundary and hole contours of the exclusive regions.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is nonfinite or outside the clipping range.</exception>
    /// <exception cref="InvalidOperationException">The clipping backend fails.</exception>
    public static Vector2[][] ExcludePolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB) =>
        OperatePolygons(polygonA, polygonB, PolyBooleanOperation.XOR, false);

    /// <summary>Returns the area common to two polygons.</summary>
    /// <param name="polygonA">The first region.</param>
    /// <param name="polygonB">The second region.</param>
    /// <returns>Boundary and hole contours of the intersection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is nonfinite or outside the clipping range.</exception>
    /// <exception cref="InvalidOperationException">The clipping backend fails.</exception>
    public static Vector2[][] IntersectPolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB) =>
        OperatePolygons(polygonA, polygonB, PolyBooleanOperation.Intersection, false);

    /// <summary>Returns the portions of an open polyline inside a polygon.</summary>
    /// <param name="polyline">The open input line.</param>
    /// <param name="polygon">The clipping region.</param>
    /// <returns>The portions of the line inside the polygon.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is nonfinite or outside the clipping range.</exception>
    /// <exception cref="InvalidOperationException">The clipping backend fails.</exception>
    public static Vector2[][] IntersectPolylineWithPolygon(ReadOnlySpan<Vector2> polyline, ReadOnlySpan<Vector2> polygon) =>
        OperatePolygons(polyline, polygon, PolyBooleanOperation.Intersection, true);

    /// <summary>Combines two polygon regions.</summary>
    /// <param name="polygonA">The first region.</param>
    /// <param name="polygonB">The second region.</param>
    /// <returns>Boundary and hole contours of the union.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is nonfinite or outside the clipping range.</exception>
    /// <exception cref="InvalidOperationException">The clipping backend fails.</exception>
    public static Vector2[][] MergePolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB) =>
        OperatePolygons(polygonA, polygonB, PolyBooleanOperation.Union, false);

    /// <summary>Expands or shrinks a closed polygon.</summary>
    /// <param name="polygon">The input contour.</param>
    /// <param name="delta">Positive expansion or negative contraction in coordinate units.</param>
    /// <param name="joinType">The shape of each corner.</param>
    /// <returns>Boundary and hole contours after offsetting.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate, distance or join choice is invalid for clipping.</exception>
    public static Vector2[][] OffsetPolygon(ReadOnlySpan<Vector2> polygon, float delta, PolyJoinType joinType = PolyJoinType.Square) =>
        OffsetPath(polygon, delta, joinType, PolyEndType.Polygon);

    /// <summary>Expands an open or joined polyline into polygons.</summary>
    /// <param name="polyline">The input line.</param>
    /// <param name="delta">The nonnegative distance to each side of the line.</param>
    /// <param name="joinType">The shape of each bend.</param>
    /// <param name="endType">The endpoint cap or joined-line shape.</param>
    /// <returns>Offset polygon contours; negative distances return an empty array.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate, distance, join or end choice is invalid for clipping.</exception>
    public static Vector2[][] OffsetPolyline(ReadOnlySpan<Vector2> polyline, float delta,
        PolyJoinType joinType = PolyJoinType.Square, PolyEndType endType = PolyEndType.Square)
    {
        if (endType == PolyEndType.Polygon) return [];
        return OffsetPath(polyline, delta, joinType, endType);
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

    /// <summary>Triangulates a simple polygon into counterclockwise triples of input vertex indices.</summary>
    /// <param name="polygon">The polygon vertices in perimeter order, in either winding direction.</param>
    /// <returns>Three indices per triangle, or an empty array when the contour cannot be triangulated.</returns>
    /// <remarks>The contour is not modified. Double-precision area and orientation intermediates avoid overflow for finite float coordinates. Collinear vertices may produce flat triangles as a last resort. Nonfinite coordinates have no defined result.</remarks>
    public static int[] TriangulatePolygon(ReadOnlySpan<Vector2> polygon)
    {
        if (polygon.Length < 3) return [];
        var triangles = new int[checked((polygon.Length - 2) * 3)];
        return TryTriangulatePolygon(polygon, new int[polygon.Length], triangles) ? triangles : [];
    }

    /// <summary>Triangulates a set of points so each triangle has an empty circumcircle.</summary>
    /// <param name="points">Input points; the returned indices refer to their original order.</param>
    /// <returns>Consecutive triples of input indices, or an empty array if no triangles can be formed.</returns>
    /// <remarks>Uses incremental circumcircle removal. Cocircular points have no unique diagonal; input order determines ties. Nonfinite coordinates have no defined result.</remarks>
    public static int[] TriangulateDelaunay(ReadOnlySpan<Vector2> points)
    {
        var count = points.Length;
        if (count < 3) return [];

        var vertices = new Vector2[checked(count + 3)];
        points.CopyTo(vertices);
        var minimum = points[0];
        var maximum = points[0];
        for (var i = 1; i < count; i++)
        {
            minimum = new Vector2(System.MathF.Min(minimum.X, points[i].X), System.MathF.Min(minimum.Y, points[i].Y));
            maximum = new Vector2(System.MathF.Max(maximum.X, points[i].X), System.MathF.Max(maximum.Y, points[i].Y));
        }
        var span = maximum - minimum;
        var delta = System.MathF.Max(span.X, span.Y);
        var center = minimum + span * 0.5f;
        vertices[count] = new Vector2(center.X - delta * 16f, center.Y - delta);
        vertices[count + 1] = new Vector2(center.X, center.Y + delta * 16f);
        vertices[count + 2] = new Vector2(center.X + delta * 16f, center.Y - delta);

        var triangles = new List<(int A, int B, int C, Vector2 Center, float RadiusSquared)>
        {
            CreateDelaunayTriangle(vertices, count, count + 1, count + 2)
        };
        // ponytail: the reference scans triangles and boundary edges quadratically per insertion; replace only if large point sets need it.
        for (var i = 0; i < count; i++)
        {
            var edges = new List<(int A, int B, bool Bad)>();
            for (var j = triangles.Count - 1; j >= 0; j--)
            {
                var triangle = triangles[j];
                if (!(vertices[i].DistanceSquaredTo(triangle.Center) < triangle.RadiusSquared)) continue;
                AddDelaunayEdge(edges, triangle.A, triangle.B);
                AddDelaunayEdge(edges, triangle.B, triangle.C);
                AddDelaunayEdge(edges, triangle.C, triangle.A);
                triangles.RemoveAt(j);
            }

            for (var j = 0; j < edges.Count; j++)
            {
                if (edges[j].Bad) continue;
                for (var k = j + 1; k < edges.Count; k++)
                {
                    if (edges[k].A != edges[j].A || edges[k].B != edges[j].B) continue;
                    edges[j] = (edges[j].A, edges[j].B, true);
                    edges[k] = (edges[k].A, edges[k].B, true);
                    break;
                }
                if (!edges[j].Bad) triangles.Add(CreateDelaunayTriangle(vertices, edges[j].A, edges[j].B, i));
            }
        }

        var indices = new List<int>();
        foreach (var triangle in triangles)
        {
            if (triangle.A >= count || triangle.B >= count || triangle.C >= count) continue;
            indices.Add(triangle.A);
            indices.Add(triangle.B);
            indices.Add(triangle.C);
        }
        return indices.ToArray();
    }

    /// <summary>Packs rectangular tiles into an atlas using the reference scanline layout search.</summary>
    /// <param name="sizes">Tile sizes in input order; components are truncated to integer pixels.</param>
    /// <returns>Tile origins in input order and the unrounded bounding size of the atlas.</returns>
    /// <exception cref="ArgumentException">No tile sizes were supplied.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A truncated size is nonpositive, a width exceeds 4096 pixels, or no layout fits a 32-bit integer atlas height.</exception>
    /// <remarks>Input and returned positions are independent. Candidate strip widths are powers of two up to 4096, but the returned bounds are the actual occupied size. Equal-width tiles retain input order.</remarks>
    public static (Vector2[] Points, Vector2I Size) MakeAtlas(ReadOnlySpan<Vector2> sizes)
    {
        if (sizes.IsEmpty) throw new ArgumentException("At least one tile size is required.", nameof(sizes));
        var rectangles = new (int Width, int Height, int Index)[sizes.Length];
        for (var i = 0; i < sizes.Length; i++)
        {
            var size = new Vector2I(sizes[i]);
            if (size.X <= 0 || size.Y <= 0 || size.X > 4096)
                throw new ArgumentOutOfRangeException(nameof(sizes), "Tile dimensions must be positive and width must not exceed 4096 pixels.");
            rectangles[i] = (size.X, size.Y, i);
        }
        Array.Sort(rectangles, static (a, b) =>
        {
            var widthOrder = b.Width.CompareTo(a.Width);
            return widthOrder == 0 ? a.Index.CompareTo(b.Index) : widthOrder;
        });

        var candidatePoints = new Vector2[sizes.Length];
        var bestPoints = new Vector2[sizes.Length];
        var bestSize = Vector2I.Zero;
        var bestAspect = double.PositiveInfinity;
        for (var width = 1; width <= 4096; width <<= 1)
        {
            if (width < rectangles[0].Width) continue;
            var skyline = new long[width];
            var offset = 0;
            long limitHeight = 0;
            long maxHeight = 0;
            var maxWidth = 0;
            foreach (var rectangle in rectangles)
            {
                if (offset + rectangle.Width > width) offset = 0;
                long fromY = 0;
                for (var x = offset; x < offset + rectangle.Width; x++)
                    fromY = Math.Max(fromY, skyline[x]);
                var endHeight = fromY + rectangle.Height;
                var endWidth = offset + rectangle.Width;
                candidatePoints[rectangle.Index] = new Vector2(offset, (float)fromY);
                if (offset == 0) limitHeight = endHeight;
                for (var x = offset; x < endWidth; x++) skyline[x] = endHeight;
                maxHeight = Math.Max(maxHeight, endHeight);
                maxWidth = Math.Max(maxWidth, endWidth);
                if (offset == 0 || endHeight > limitHeight) offset = endWidth;
            }
            if (maxHeight > int.MaxValue) continue;
            var powerHeight = System.Numerics.BitOperations.RoundUpToPowerOf2((uint)maxHeight);
            var powerWidth = System.Numerics.BitOperations.RoundUpToPowerOf2((uint)maxWidth);
            var aspect = powerHeight > powerWidth ? (double)powerHeight / powerWidth : (double)powerWidth / powerHeight;
            if (aspect >= bestAspect) continue;
            bestAspect = aspect;
            bestSize = new Vector2I(maxWidth, (int)maxHeight);
            candidatePoints.CopyTo(bestPoints, 0);
        }
        if (double.IsPositiveInfinity(bestAspect))
            throw new ArgumentOutOfRangeException(nameof(sizes), "No atlas layout fits a 32-bit integer height.");
        return (bestPoints, bestSize);
    }

    // The caller supplies reusable buffers so retained canvas redraws allocate nothing.
    internal static bool TryTriangulatePolygon(ReadOnlySpan<Vector2> polygon, Span<int> remaining, Span<int> triangles)
    {
        if (polygon.Length < 3) return false;
        double area = 0d;
        for (var i = 0; i < polygon.Length; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Length];
            area += (double)a.X * b.Y - (double)a.Y * b.X;
        }
        for (var i = 0; i < polygon.Length; i++) remaining[i] = area > 0d ? i : polygon.Length - 1 - i;

        var count = polygon.Length;
        var cursor = count - 1;
        var attempts = 2L * count;
        var relaxed = false;
        var output = 0;
        // ponytail: ear clipping is cubic in the worst case; use a spatial index if large contours become a measured bottleneck.
        while (count > 2)
        {
            if (attempts-- == 0)
            {
                if (relaxed) return false;
                relaxed = true;
                attempts = 2L * count;
            }

            var previous = cursor % count;
            cursor = (previous + 1) % count;
            var next = (cursor + 1) % count;
            var a = polygon[remaining[previous]];
            var b = polygon[remaining[cursor]];
            var c = polygon[remaining[next]];
            if (TriangleCross(a, b, c) < (relaxed ? -ReferenceEpsilon : ReferenceEpsilon)) continue;

            var contains = false;
            for (var i = 0; i < count; i++)
            {
                if (i == previous || i == cursor || i == next) continue;
                var point = polygon[remaining[i]];
                var ab = TriangleCross(a, b, point);
                var bc = TriangleCross(b, c, point);
                var ca = TriangleCross(c, a, point);
                if (relaxed ? ab > 0d && bc > 0d && ca > 0d : ab >= 0d && bc >= 0d && ca >= 0d)
                {
                    contains = true;
                    break;
                }
            }
            if (contains) continue;

            triangles[output++] = remaining[previous];
            triangles[output++] = remaining[cursor];
            triangles[output++] = remaining[next];
            remaining[(cursor + 1)..count].CopyTo(remaining[cursor..]);
            count--;
            attempts = 2L * count;
        }
        return true;
    }

    private static double TriangleCross(Vector2 a, Vector2 b, Vector2 c) =>
        ((double)b.X - a.X) * ((double)c.Y - a.Y) - ((double)b.Y - a.Y) * ((double)c.X - a.X);

    private static Vector2[][] OperatePolygons(ReadOnlySpan<Vector2> first, ReadOnlySpan<Vector2> second,
        PolyBooleanOperation operation, bool open)
    {
        var pathA = ToClipperPath(first, nameof(first));
        var pathB = ToClipperPath(second, nameof(second));
        var clipper = new ClipperD(ClipperPrecision) { PreserveCollinear = false };
        if (open) clipper.AddOpenSubject(pathA);
        else clipper.AddSubject(pathA);
        clipper.AddClip(pathB);
        var clipType = operation switch
        {
            PolyBooleanOperation.Union => ClipType.Union,
            PolyBooleanOperation.Difference => ClipType.Difference,
            PolyBooleanOperation.Intersection => ClipType.Intersection,
            PolyBooleanOperation.XOR => ClipType.Xor,
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
        if (open)
        {
            var paths = new PathsD();
            if (!clipper.Execute(clipType, FillRule.EvenOdd, new PolyTreeD(), paths))
                throw new InvalidOperationException("Polyline clipping failed.");
            return FromClipperPaths(paths);
        }
        else
        {
            var paths = new PathsD();
            if (!clipper.Execute(clipType, FillRule.EvenOdd, paths))
                throw new InvalidOperationException("Polygon clipping failed.");
            return FromClipperPaths(paths);
        }
    }

    private static Vector2[][] OffsetPath(ReadOnlySpan<Vector2> points, float delta, PolyJoinType joinType, PolyEndType endType)
    {
        var path = ToClipperPath(points, nameof(points));
        if (!float.IsFinite(delta) || Math.Abs((double)delta) > ClipperCoordinateLimit)
            throw new ArgumentOutOfRangeException(nameof(delta));
        var join = joinType switch
        {
            PolyJoinType.Square => JoinType.Square,
            PolyJoinType.Round => JoinType.Round,
            PolyJoinType.Miter => JoinType.Miter,
            _ => throw new ArgumentOutOfRangeException(nameof(joinType)),
        };
        var end = endType switch
        {
            PolyEndType.Polygon => EndType.Polygon,
            PolyEndType.Joined => EndType.Joined,
            PolyEndType.Butt => EndType.Butt,
            PolyEndType.Square => EndType.Square,
            PolyEndType.Round => EndType.Round,
            _ => throw new ArgumentOutOfRangeException(nameof(endType)),
        };
        if (endType != PolyEndType.Polygon && delta < 0f) return [];
        return FromClipperPaths(Clipper.InflatePaths(new PathsD { path }, delta, join, end, 2d, ClipperPrecision, 0.25d));
    }

    private static PathD ToClipperPath(ReadOnlySpan<Vector2> points, string parameterName)
    {
        var path = new PathD(points.Length);
        foreach (var point in points)
        {
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) ||
                Math.Abs((double)point.X) > ClipperCoordinateLimit || Math.Abs((double)point.Y) > ClipperCoordinateLimit)
                throw new ArgumentOutOfRangeException(parameterName, "Polygon coordinates must be finite and fit the clipping range.");
            path.Add(new PointD(point.X, point.Y));
        }
        return path;
    }

    private static Vector2[][] FromClipperPaths(PathsD paths)
    {
        var result = new Vector2[paths.Count][];
        for (var i = 0; i < paths.Count; i++)
        {
            result[i] = new Vector2[paths[i].Count];
            for (var j = 0; j < paths[i].Count; j++)
                result[i][j] = new Vector2((float)paths[i][j].x, (float)paths[i][j].y);
        }
        return result;
    }

    private static bool TryMergeConvexParts(ReadOnlySpan<Vector2> points, int[] left, int[] right, out int[] merged)
    {
        for (var i = 0; i < left.Length; i++)
        {
            var nextLeft = (i + 1) % left.Length;
            for (var j = 0; j < right.Length; j++)
            {
                var nextRight = (j + 1) % right.Length;
                if (left[i] != right[nextRight] || left[nextLeft] != right[j]) continue;
                if (TriangleCross(points[left[(i + left.Length - 1) % left.Length]], points[left[i]],
                        points[right[(nextRight + 1) % right.Length]]) <= 0d ||
                    TriangleCross(points[right[(j + right.Length - 1) % right.Length]], points[left[nextLeft]],
                        points[left[(nextLeft + 1) % left.Length]]) <= 0d) continue;

                merged = new int[left.Length + right.Length - 2];
                var count = 0;
                for (var index = nextLeft; index != i; index = (index + 1) % left.Length) merged[count++] = left[index];
                for (var index = nextRight; index != j; index = (index + 1) % right.Length) merged[count++] = right[index];
                return true;
            }
        }
        merged = [];
        return false;
    }

    private static (int A, int B, int C, Vector2 Center, float RadiusSquared) CreateDelaunayTriangle(
        ReadOnlySpan<Vector2> vertices, int first, int second, int third)
    {
        var a = vertices[second] - vertices[first];
        var b = vertices[third] - vertices[first];
        var offset = (b * a.LengthSquared() - a * b.LengthSquared()).Orthogonal() / (a.Cross(b) * 2f);
        return (first, second, third, offset + vertices[first], offset.LengthSquared());
    }

    private static void AddDelaunayEdge(List<(int A, int B, bool Bad)> edges, int first, int second) =>
        edges.Add(first > second ? (second, first, false) : (first, second, false));
}
