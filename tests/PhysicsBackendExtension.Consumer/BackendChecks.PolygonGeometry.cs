using Electron2D;
using System.Runtime.CompilerServices;

public static partial class BackendChecks
{
    private static readonly ConditionalWeakTable<ShapeGeometry, Polygon> Polygons = new();
    private sealed class Polygon
    {
        internal readonly Vector2[] Vertices;
        internal readonly Vector2 Center;
        internal readonly float Area, Moment, Radius, Thickness;
        internal Polygon(ShapeGeometry geometry)
        {
            var points = geometry.GetConvexPolygonPoints();
            if (points.Length > 1 && points[0] == points[^1]) points = points[..^1];
            Vertices = points.ToArray();
            if (points.IsEmpty) return;
            var origin = points[0];
            double crossSum = 0, cx = 0, cy = 0, polar = 0;
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i]; var b = points[(i + 1) % points.Length];
                var ax = (double)a.X - origin.X; var ay = (double)a.Y - origin.Y;
                var bx = (double)b.X - origin.X; var by = (double)b.Y - origin.Y;
                var cross = ax * by - ay * bx;
                crossSum += cross; cx += cross * (ax + bx); cy += cross * (ay + by);
                polar += cross * (ax * ax + ax * bx + bx * bx + ay * ay + ay * by + by * by);
            }
            var centerX = cx / (3 * crossSum); var centerY = cy / (3 * crossSum);
            Center = new((float)(origin.X + centerX), (float)(origin.Y + centerY));
            Area = (float)(Math.Abs(crossSum) / 2);
            Moment = (float)(polar / (6 * crossSum) - centerX * centerX - centerY * centerY);
            if (crossSum < 0) Array.Reverse(Vertices);
            for (var i = 0; i < Vertices.Length; i++) { Vertices[i] -= Center; Radius = MathF.Max(Radius, Vertices[i].Length()); }
            Thickness = float.PositiveInfinity;
            for (var i = 0; i < Vertices.Length; i++)
            {
                var edge = Vertices[(i + 1) % Vertices.Length] - Vertices[i]; if (edge == Vector2.Zero) continue;
                var axis = new Vector2(-edge.Y, edge.X).Normalized(); var lo = float.PositiveInfinity; var hi = float.NegativeInfinity;
                foreach (var p in Vertices) { var value = p.Dot(axis); lo = MathF.Min(lo, value); hi = MathF.Max(hi, value); }
                Thickness = MathF.Min(Thickness, (hi - lo) / 2);
            }
        }
    }
    private static int CoreCount(in Part part) => part.Polygon?.Vertices.Length ?? (part.Contour ? 4 : 1);
    private static Vector2 CoreVertex(in Part part, Transform pose, int index)
    {
        var center = PartAt(part, pose);
        if (part.Polygon is { } polygon)
        { var point = polygon.Vertices[index]; return center + EdgeX(part, pose) * point.X + EdgeY(part, pose) * point.Y; }
        if (!part.Contour) return center;
        var x = EdgeX(part, pose); var y = EdgeY(part, pose);
        return index switch { 0 => center - x - y, 1 => center + x - y, 2 => center + x + y, _ => center - x + y };
    }
    private static float RoundRadius(in Part part) => part.Contour ? part.Inflation : part.Outer;
    private static void Project(in Part part, Transform pose, Vector2 axis, out float minimum, out float maximum)
    {
        minimum = float.PositiveInfinity; maximum = float.NegativeInfinity;
        for (var i = 0; i < CoreCount(part); i++)
        { var p = CoreVertex(part, pose, i).Dot(axis); minimum = MathF.Min(minimum, p); maximum = MathF.Max(maximum, p); }
        minimum -= RoundRadius(part); maximum += RoundRadius(part);
    }
    private static bool PolygonContains(in Part part, Transform pose, Vector2 point)
    {
        var count = CoreCount(part); if (count == 0) return false;
        for (var i = 0; i < count; i++)
        {
            var a = CoreVertex(part, pose, i); var b = CoreVertex(part, pose, (i + 1) % count);
            if (CrossEdges(b - a, point - a) < 0) return false;
        }
        return true;
    }
    private static void PolygonSupport(in Part part, Transform pose, Vector2 normal, Vector2 tangent, out float lo, out float hi)
    {
        Project(part, pose, normal, out _, out var maximum); maximum -= RoundRadius(part);
        lo = float.PositiveInfinity; hi = float.NegativeInfinity;
        for (var i = 0; i < CoreCount(part); i++)
        {
            var p = CoreVertex(part, pose, i);
            if (maximum - p.Dot(normal) > .000001f * MathF.Max(1, part.Outer)) continue;
            var value = p.Dot(tangent); lo = MathF.Min(lo, value); hi = MathF.Max(hi, value);
        }
    }
    private static void ConsiderAxis(in Part first, Transform a, in Part second, Transform b, Vector2 direction,
        ref float gap, ref Vector2 normal)
    {
        if (direction.LengthSquared() < 1e-16f) return;
        var axis = direction.Normalized(); Project(first, a, axis, out var alo, out var ahi); Project(second, b, axis, out var blo, out var bhi);
        var forward = blo - ahi; var backward = alo - bhi; var value = MathF.Max(forward, backward);
        if (value > gap) { gap = value; normal = forward >= backward ? axis : -axis; }
    }
    private static float PolygonGap(in Part first, Transform a, in Part second, Transform b,
        out Vector2 normal, out Vector2 pointA, out Vector2 pointB)
    {
        var gap = float.NegativeInfinity; normal = Vector2.Right;
        ConsiderAxis(first, a, second, b, PartAt(second, b) - PartAt(first, a), ref gap, ref normal);
        ConsiderAxis(first, a, second, b, Vector2.Right, ref gap, ref normal);
        for (var i = 0; i < CoreCount(first); i++)
        {
            var edge = CoreVertex(first, a, (i + 1) % CoreCount(first)) - CoreVertex(first, a, i);
            ConsiderAxis(first, a, second, b, new(-edge.Y, edge.X), ref gap, ref normal);
        }
        for (var i = 0; i < CoreCount(second); i++)
        {
            var edge = CoreVertex(second, b, (i + 1) % CoreCount(second)) - CoreVertex(second, b, i);
            ConsiderAxis(first, a, second, b, new(-edge.Y, edge.X), ref gap, ref normal);
        }
        if (RoundRadius(first) + RoundRadius(second) > 0)
            for (var i = 0; i < CoreCount(first); i++) for (var j = 0; j < CoreCount(second); j++)
                {
                    var av = CoreVertex(first, a, i); var bv = CoreVertex(second, b, j);
                    var ae = CoreVertex(first, a, (i + 1) % CoreCount(first)); var be = CoreVertex(second, b, (j + 1) % CoreCount(second));
                    ConsiderAxis(first, a, second, b, av - ClosestEdge(av, bv, be), ref gap, ref normal);
                    ConsiderAxis(first, a, second, b, bv - ClosestEdge(bv, av, ae), ref gap, ref normal);
                }
        var tangent = new Vector2(-normal.Y, normal.X);
        SupportRange(first, a, normal, tangent, out var alo, out var ahi); SupportRange(second, b, -normal, tangent, out var blo, out var bhi);
        var t = (MathF.Max(alo, blo) + MathF.Min(ahi, bhi)) / 2;
        Project(first, a, normal, out _, out var maximum); Project(second, b, normal, out var minimum, out _);
        pointA = normal * maximum + tangent * Math.Clamp(t, alo, ahi); pointB = normal * minimum + tangent * Math.Clamp(t, blo, bhi);
        return gap;
    }
    private static bool RayPolygon(in Part part, Transform pose, Vector2 from, Vector2 motion, out float fraction, out Vector2 normal)
    {
        var near = 0f; var far = 1f; normal = default;
        for (var i = 0; i < CoreCount(part); i++)
        {
            var a = CoreVertex(part, pose, i); var b = CoreVertex(part, pose, (i + 1) % CoreCount(part)); var edge = b - a;
            if (edge == Vector2.Zero) continue;
            var outward = new Vector2(edge.Y, -edge.X).Normalized(); var distance = (a - from).Dot(outward); var speed = motion.Dot(outward);
            if (speed == 0) { if (distance < 0) { fraction = 1; return false; } continue; }
            var time = distance / speed;
            if (speed < 0 && time > near) { near = time; normal = outward; }
            if (speed > 0) far = MathF.Min(far, time);
            if (near > far) { fraction = 1; return false; }
        }
        fraction = near; return CoreCount(part) > 0 && near <= 1 && far >= 0;
    }
    private static Vector2 SweptVertex(in Part part, Vector2 motion, int index)
    {
        var count = CoreCount(part);
        return CoreVertex(part, Transform.Identity, index % count) + (index >= count ? motion : Vector2.Zero);
    }
    private static void SweptProject(in Part part, Vector2 motion, Vector2 axis, out float lo, out float hi)
    {
        Project(part, Transform.Identity, axis, out lo, out hi);
        var along = motion.Dot(axis); lo += MathF.Min(0, along); hi += MathF.Max(0, along);
    }
    private static void ConsiderSweptAxis(in Part a, Vector2 am, in Part b, Vector2 bm, Vector2 direction,
        ref float gap, ref Vector2 normal)
    {
        if (direction.LengthSquared() < 1e-16f) return;
        var axis = direction.Normalized(); SweptProject(a, am, axis, out var alo, out var ahi); SweptProject(b, bm, axis, out var blo, out var bhi);
        var forward = blo - ahi; var backward = alo - bhi; var value = MathF.Max(forward, backward);
        if (value > gap) { gap = value; normal = forward >= backward ? axis : -axis; }
    }
    private static void SweptSupport(in Part part, Vector2 motion, Vector2 normal, Vector2 tangent, out float lo, out float hi)
    {
        SupportRange(part, Transform.Identity, normal, tangent, out lo, out hi);
        if (MathF.Abs(motion.Dot(normal)) < .000001f) { lo += MathF.Min(0, motion.Dot(tangent)); hi += MathF.Max(0, motion.Dot(tangent)); }
        else if (motion.Dot(normal) > 0) { lo += motion.Dot(tangent); hi += motion.Dot(tangent); }
    }
    private static bool CollidePolygonRegions(in Part a, Vector2 am, in Part b, Vector2 bm, Span<Vector2> output, out int count)
    {
        count = 0; var ac = 2 * CoreCount(a); var bc = 2 * CoreCount(b); if (ac == 0 || bc == 0) return false;
        var gap = float.NegativeInfinity; var normal = Vector2.Right;
        ConsiderSweptAxis(a, am, b, bm, Vector2.Right, ref gap, ref normal);
        // Every hull edge is among these chords; interior chords remain valid separating certificates.
        // shortcut: Prepare swept hulls if profiling large contours shows this streamed chord scan dominates.
        for (var i = 0; i < ac; i++) for (var j = i + 1; j < ac; j++)
            {
                var edge = SweptVertex(a, am, j) - SweptVertex(a, am, i);
                ConsiderSweptAxis(a, am, b, bm, new(-edge.Y, edge.X), ref gap, ref normal);
            }
        for (var i = 0; i < bc; i++) for (var j = i + 1; j < bc; j++)
            {
                var edge = SweptVertex(b, bm, j) - SweptVertex(b, bm, i);
                ConsiderSweptAxis(a, am, b, bm, new(-edge.Y, edge.X), ref gap, ref normal);
            }
        if (RoundRadius(a) + RoundRadius(b) > 0)
        {
            for (var i = 0; i < ac; i++) for (var j = 0; j < bc; j++) for (var k = j; k < bc; k++)
                    {
                        var vertex = SweptVertex(a, am, i); var start = SweptVertex(b, bm, j);
                        ConsiderSweptAxis(a, am, b, bm, vertex - ClosestEdge(vertex, start, SweptVertex(b, bm, k)), ref gap, ref normal);
                    }
            for (var i = 0; i < bc; i++) for (var j = 0; j < ac; j++) for (var k = j; k < ac; k++)
                    {
                        var vertex = SweptVertex(b, bm, i); var start = SweptVertex(a, am, j);
                        ConsiderSweptAxis(a, am, b, bm, vertex - ClosestEdge(vertex, start, SweptVertex(a, am, k)), ref gap, ref normal);
                    }
        }
        if (gap > 0) return false;
        var tangent = new Vector2(-normal.Y, normal.X);
        SweptSupport(a, am, normal, tangent, out var alo, out var ahi); SweptSupport(b, bm, -normal, tangent, out var blo, out var bhi);
        var t = (MathF.Max(alo, blo) + MathF.Min(ahi, bhi)) / 2;
        SweptProject(a, am, normal, out _, out var maximum); SweptProject(b, bm, normal, out var minimum, out _);
        var pa = normal * maximum + tangent * Math.Clamp(t, alo, ahi); var pb = normal * minimum + tangent * Math.Clamp(t, blo, bhi);
        if (a.Inner > 0 || b.Inner > 0)
        {
            var ring = a.Inner > 0 ? a : b; var rm = a.Inner > 0 ? am : bm;
            var other = a.Inner > 0 ? b : a; var om = a.Inner > 0 ? bm : am;
            var farthest = other.Offset; var center = ring.Offset; var distance = 0f;
            for (var i = 0; i < 2 * CoreCount(other); i++) for (var end = 0; end < 2; end++)
                {
                    var vertex = SweptVertex(other, om, i); var endpoint = ring.Offset + rm * end; var value = vertex.DistanceTo(endpoint);
                    if (value > distance) { distance = value; farthest = vertex; center = endpoint; }
                }
            var inner = ring.Inner - distance - RoundRadius(other); if (inner > 0) return false;
            if (inner > gap)
            {
                var radial = (farthest - center).Normalized(); var boundary = center + radial * ring.Inner;
                var tip = farthest + radial * RoundRadius(other); pa = a.Inner > 0 ? boundary : tip; pb = a.Inner > 0 ? tip : boundary;
            }
        }
        if (output.Length >= 2) { output[0] = pa; output[1] = pb; count = 1; }
        return true;
    }

}
