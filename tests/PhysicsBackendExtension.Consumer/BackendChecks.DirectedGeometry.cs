using Electron2D;

public static partial class BackendChecks
{
    private static Part ExpandPart(in Part part, float margin)
    {
        if (part.Directed)
        {
            var axis = part.EdgeY.Normalized();
            return part with { Offset = part.Offset + axis * (margin / 2), EdgeY = part.EdgeY + axis * (margin / 2), Outer = part.Outer + margin / 2 };
        }
        if (part.Boundary) return part with { Offset = part.Offset + part.PlaneNormal * margin };
        return part.Contour ? part with { Inflation = part.Inflation + margin } : part with
        { Outer = part.Outer + margin, Inner = MathF.Max(0, part.Inner - margin) };
    }
    private static bool DirectedContact(in Part first, Transform a, Vector2 am, in Part second, Transform b, Vector2 bm,
        out Vector2 normal, out Vector2 pointA, out Vector2 pointB, out float depth)
    {
        normal = pointA = pointB = default; depth = 0;
        if (first.Directed && second.Directed) return false;
        var flip = second.Directed; var ray = flip ? second : first; var pose = flip ? b : a;
        var other = flip ? first : second; var otherPose = flip ? a : b; var otherMotion = flip ? am : bm;
        var edge = EdgeY(ray, pose); var origin = PartAt(ray, pose) - edge;
        if (edge == Vector2.Zero) return false;
        var axis = edge.Normalized(); var endpoint = origin + axis * (2 * edge.Length() + MathF.Max(0, axis.Dot(flip ? bm : am)));
        if (other.Boundary)
        {
            var movedA = a; movedA.Origin += am; var movedB = b; movedB.Origin += bm;
            var gap = BoundaryGap(first, movedA, second, movedB, out normal, out pointA, out pointB);
            depth = -gap; return gap <= 0;
        }
        var translation = endpoint - origin;
        if (!RaySweptTarget(other, otherPose, otherMotion, origin, translation, out var fraction, out var outward) ||
            outward.Dot(-translation) < .000001f) return false;
        var point = origin + translation * fraction; depth = endpoint.DistanceTo(point);
        var away = ray.Slide ? outward : -axis;
        var contact = ray.Slide ? endpoint + away * depth : point;
        normal = flip ? away : -away; pointA = flip ? contact : endpoint; pointB = flip ? endpoint : contact;
        return true;
    }
    private static bool RaySweptTarget(in Part part, Transform pose, Vector2 motion, Vector2 from, Vector2 translation,
        out float fraction, out Vector2 normal)
    {
        fraction = 1; normal = default;
        if (part.Directed) return false;
        if (motion == Vector2.Zero && !(part.Contour && !part.Capsule && part.Inflation > 0))
            return !ContainsPart(part, pose, from) && RayPart(part, pose, from, translation, out fraction, out normal);
        var retained = part; var count = CoreCount(part); if (count == 0) return false;
        if (part.Inner > 0) return RaySweptRing(part, pose, motion, from, translation, out fraction, out normal);
        var radius = RoundRadius(part); var near = 0f; var far = 1f; var hullNormal = Vector2.Zero; var inside = true; var edges = 0; var planes = 0;
        var hit = false; var roundedInside = false;
        for (var i = 0; i < 2 * count; i++) for (var j = i + 1; j < 2 * count; j++)
            {
                var start = Vertex(i); var end = Vertex(j); var edge = end - start;
                if (edge == Vector2.Zero) continue;
                var positive = false; var negative = false;
                for (var k = 0; k < 2 * count; k++)
                {
                    var side = CrossEdges(edge, Vertex(k) - start);
                    positive |= side > .000001f; negative |= side < -.000001f;
                }
                if (positive && negative) continue;
                edges++;
                if (radius > 0 || !positive && !negative)
                {
                    var segment = new Part(0, (start + end) / 2, 0, edge.Length() / 2 + radius, default, edge / 2, radius, true);
                    roundedInside |= from.DistanceSquaredTo(ClosestEdge(from, start, end)) <= radius * radius;
                    if (RayCapsule(segment, Transform.Identity, from, translation, out var f, out var n) && f <= fraction)
                    { hit = true; fraction = f; normal = n; }
                }
                if (!positive && !negative) continue;
                planes++;
                var outward = new Vector2(edge.Y, -edge.X).Normalized() * (negative ? -1 : 1);
                var distance = (start - from).Dot(outward); var rate = translation.Dot(outward);
                inside &= distance >= 0;
                if (rate == 0) { if (distance < 0) far = -1; continue; }
                var t = distance / rate;
                if (rate < 0 && t > near) { near = t; hullNormal = outward; }
                if (rate > 0) far = MathF.Min(far, t);
            }
        if (roundedInside || planes > 0 && inside) return false;
        if (edges > 0 && hullNormal != Vector2.Zero && near <= far && near <= fraction && far >= 0)
        { hit = true; fraction = near; normal = hullNormal; }
        return hit;
        Vector2 Vertex(int index) => CoreVertex(retained, pose, index % count) + (index >= count ? motion : Vector2.Zero);
    }
    private static bool RaySweptRing(in Part part, Transform pose, Vector2 motion, Vector2 from, Vector2 translation,
        out float fraction, out Vector2 normal)
    {
        var center = PartAt(part, pose); var end = center + motion;
        var capsule = new Part(0, (center + end) / 2, 0, motion.Length() / 2 + part.Outer, default, motion / 2, part.Outer, true);
        var inOuter = from.DistanceTo(ClosestEdge(from, center, end)) <= part.Outer;
        var inHole = from.DistanceTo(center) < part.Inner && from.DistanceTo(end) < part.Inner;
        fraction = 1; normal = default;
        if (inOuter && !inHole) return false;
        if (!inHole) return RayCapsule(capsule, Transform.Identity, from, translation, out fraction, out normal);
        var hit = false;
        for (var i = 0; i < 2; i++)
        {
            var at = i == 0 ? center : end; var offset = from - at;
            var length = translation.LengthSquared(); var along = offset.Dot(translation);
            var discriminant = along * along - length * (offset.LengthSquared() - part.Inner * part.Inner);
            if (length == 0 || discriminant < 0) continue;
            var f = (-along + MathF.Sqrt(discriminant)) / length;
            if (f < 0 || f > fraction) continue;
            fraction = f; normal = -(from + translation * f - at).Normalized(); hit = true;
        }
        return hit;
    }
    private static bool SweepDirected(in Part first, Transform a, Vector2 motion, in Part second, Transform b, out float fraction)
        => DirectedSweepRange(first, a, motion, second, b, out _, out fraction);
    private static bool DirectedSweepRange(in Part first, Transform a, Vector2 motion, in Part second, Transform b,
        out float safe, out float fraction)
    {
        safe = fraction = 0;
        if (DirectedContact(first, a, Vector2.Zero, second, b, Vector2.Zero, out _, out _, out _, out _)) return true;
        if (!DirectedContact(first, a, motion, second, b, Vector2.Zero, out _, out _, out _, out _)) return false;
        var low = 0f; var high = 1f;
        for (var i = 0; i < 8; i++)
        {
            var middle = (low + high) / 2;
            if (DirectedContact(first, a, motion * middle, second, b, Vector2.Zero, out _, out _, out _, out _)) high = middle; else low = middle;
        }
        safe = low; fraction = high; return true;
    }
    internal static bool CollideDirectedGeometry(ShapeGeometry first, Transform a, Vector2 am,
        ShapeGeometry second, Transform b, Vector2 bm, Span<Vector2> output, out int count)
    {
        count = 0; var hit = false; var deepest = float.NegativeInfinity;
        var firstRay = first.Type == PhysicsServer.ShapeType.SeparationRay;
        if (firstRay && second.Type == PhysicsServer.ShapeType.SeparationRay) return false;
        if (firstRay) bm = Vector2.Zero; else am = Vector2.Zero;
        for (var i = 0; i < GeometryCount(first); i++) for (var j = 0; j < GeometryCount(second); j++)
            {
                var left = GeometryPart(first, a, 0, i); var right = GeometryPart(second, b, 0, j);
                if (!DirectedContact(left, Transform.Identity, am, right, Transform.Identity, bm, out _, out var pa, out var pb, out var depth)) continue;
                hit = true;
                if (depth < deepest || output.Length < 2) continue;
                deepest = depth; count = 1; output[0] = pa; output[1] = pb;
            }
        return hit;
    }
}
