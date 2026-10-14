using System.Buffers.Binary;
using Electron2D;

internal static class Annulus
{
    internal const string FormatID = "consumer.annulus/le-f32/v1";
    internal static ShapeGeometry Create(float inner, float outer)
    {
        if (!float.IsFinite(inner) || !float.IsFinite(outer) || inner <= 0 || outer <= inner) throw new ArgumentException("Invalid annulus radii.");
        Span<byte> data = stackalloc byte[8]; BinaryPrimitives.WriteSingleLittleEndian(data, inner); BinaryPrimitives.WriteSingleLittleEndian(data[4..], outer);
        var lines = new Vector2[128];
        for (var i = 0; i < 32; i++)
        {
            var a = Vector2.Right.Rotated(i * Mathf.Tau / 32); var b = Vector2.Right.Rotated((i + 1) * Mathf.Tau / 32);
            lines[i * 4] = a * inner; lines[i * 4 + 1] = b * inner; lines[i * 4 + 2] = a * outer; lines[i * 4 + 3] = b * outer;
        }
        var area = MathF.PI * (outer * outer - inner * inner);
        return ShapeGeometry.Custom(FormatID, data, new(-outer, -outer, outer * 2, outer * 2),
            new(area, area * (inner * inner + outer * outer) / 2, Vector2.Zero), lines, Collide);
    }
    internal static (float Inner, float Outer) Dimensions(ShapeGeometry geometry)
    {
        if (geometry.Type == PhysicsServer.ShapeType.Circle) return (0, geometry.GetCircleRadius());
        if (geometry.GetCustomFormatID() != FormatID || geometry.GetCustomData().Length != 8) throw new NotSupportedException("Unsupported custom fixture format.");
        var data = geometry.GetCustomData(); return (BinaryPrimitives.ReadSingleLittleEndian(data), BinaryPrimitives.ReadSingleLittleEndian(data[4..]));
    }
    internal static bool Contains(Vector2 point, Vector2 center, float inner, float outer)
    { var d = point.DistanceTo(center); return d >= inner && d <= outer; }
    internal static bool Contact(Vector2 first, float inner, float outer, Vector2 second, float radius,
        out Vector2 normal, out float depth, out Vector2 point)
    {
        var offset = second - first; var d = offset.Length(); var radial = d == 0 ? Vector2.Right : offset / d;
        var outside = outer + radius - d; var inside = d + radius - inner;
        if (outside < 0 || inner > 0 && inside < 0) { normal = default; depth = 0; point = default; return false; }
        var useInner = inner > 0 && radius <= inner && inside < outside;
        normal = useInner ? -radial : radial; depth = useInner ? inside : outside; point = first + radial * (useInner ? inner : outer); return true;
    }
    internal static bool Sweep(Vector2 from, Vector2 motion, Vector2 center, float inner, float outer, out float fraction)
    {
        if (Contains(from, center, inner, outer)) { fraction = 0; return true; }
        var relative = from - center; var radius = relative.Length() < inner ? inner : outer;
        var a = motion.LengthSquared(); var b = relative.Dot(motion); var c = relative.LengthSquared() - radius * radius;
        var d = b * b - a * c;
        if (a == 0 || d < 0) { fraction = 1; return false; }
        var root = MathF.Sqrt(d); var near = (-b - root) / a; var far = (-b + root) / a;
        fraction = near >= 0 ? near : far; return fraction >= 0 && fraction <= 1;
    }
    internal static bool Collide(ShapeGeometry first, Transform firstPose, Vector2 firstMotion,
        ShapeGeometry second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)
    {
        if (second.Type is PhysicsServer.ShapeType.Rectangle or PhysicsServer.ShapeType.Capsule)
            return BackendChecks.CollideContourGeometry(first, firstPose, firstMotion, second, secondPose, secondMotion, contacts, out count);
        var a = Dimensions(first); var b = Dimensions(second);
        if (b.Inner != 0) throw new NotSupportedException("The fixture exercises annulus/circle pairs.");
        count = 0;
        var a0 = firstPose.Origin; var a1 = a0 + firstMotion;
        var b0 = secondPose.Origin; var b1 = b0 + secondMotion;
        Closest(a0, a1, b0, b1, out var nearA, out var nearB);
        var outer = (double)a.Outer + b.Outer; var hole = Math.Max(0, (double)a.Inner - b.Outer);
        var far = Math.Max(Math.Max(Distance(a0, b0), Distance(a0, b1)), Math.Max(Distance(a1, b0), Distance(a1, b1)));
        if (Distance(nearA, nearB) > outer * outer || far < hole * hole) return false;
        if (contacts.Length < 2) return true;
        var best = double.PositiveInfinity; Vector2 firstPoint = default, secondPoint = default;
        // The sweep is an outer capsule with the intersection of endpoint inner disks removed.
        Span<Vector2> axes = stackalloc Vector2[7] { nearB - nearA, b0 - a0, b1 - a0, b0 - a1, b1 - a1,
            new(-firstMotion.Y, firstMotion.X), new(-secondMotion.Y, secondMotion.X) };
        foreach (var direction in axes)
        {
            var normal = direction.LengthSquared() == 0 ? Vector2.Right : direction.Normalized();
            Outer(normal); Outer(-normal);
            Inner(normal); Inner(-normal);
        }
        contacts[0] = firstPoint; contacts[1] = secondPoint; count = 1; return true;

        void Candidate(Vector2 left, Vector2 right, Vector2 normal, bool inner)
        {
            if ((right - left).Dot(normal) * (inner ? 1 : -1) < -.00001f) return;
            var depth = Distance(left, right);
            if (depth >= best) return;
            best = depth; firstPoint = left; secondPoint = right;
        }
        void Outer(Vector2 normal)
        {
            Support(a0, a1, normal, out var left0, out var left1);
            Support(b0, b1, -normal, out var right0, out var right1);
            Closest(left0 + normal * a.Outer, left1 + normal * a.Outer,
                right0 - normal * b.Outer, right1 - normal * b.Outer, out var left, out var right);
            Candidate(left, right, normal, false);
        }
        void Inner(Vector2 normal)
        {
            if (!HoleSupport(a0, a1, a.Inner, normal, out var left)) return;
            Support(b0, b1, normal, out var right0, out var right1);
            var right = Nearest(left, right0 + normal * b.Outer, right1 + normal * b.Outer);
            Candidate(left, right, normal, true);
        }
    }
    private static double Distance(Vector2 a, Vector2 b)
    { var x = (double)a.X - b.X; var y = (double)a.Y - b.Y; return x * x + y * y; }
    private static Vector2 Nearest(Vector2 point, Vector2 start, Vector2 end)
    {
        var motion = end - start; var length = Distance(start, end);
        var fraction = length == 0 ? 0 : Math.Clamp(((double)(point.X - start.X) * motion.X + (double)(point.Y - start.Y) * motion.Y) / length, 0, 1);
        return start + motion * (float)fraction;
    }
    private static void Closest(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1, out Vector2 left, out Vector2 right)
    {
        var a = a1 - a0; var b = b1 - b0; var cross = (double)a.X * b.Y - (double)a.Y * b.X;
        if (cross != 0)
        {
            var offset = b0 - a0;
            var t = ((double)offset.X * b.Y - (double)offset.Y * b.X) / cross;
            var u = ((double)offset.X * a.Y - (double)offset.Y * a.X) / cross;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1) { left = right = a0 + a * (float)t; return; }
        }
        var bestLeft = a0; var bestRight = Nearest(a0, b0, b1); var best = Distance(bestLeft, bestRight);
        Compare(a1, Nearest(a1, b0, b1)); Compare(Nearest(b0, a0, a1), b0); Compare(Nearest(b1, a0, a1), b1);
        left = bestLeft; right = bestRight;
        void Compare(Vector2 x, Vector2 y)
        {
            var distance = Distance(x, y);
            if (distance < best) { best = distance; bestLeft = x; bestRight = y; }
        }
    }
    private static void Support(Vector2 start, Vector2 end, Vector2 direction, out Vector2 first, out Vector2 last)
    {
        var projected = (end - start).Dot(direction);
        first = projected > .000001f ? end : start; last = projected < -.000001f ? start : end;
    }
    private static bool HoleSupport(Vector2 start, Vector2 end, float radius, Vector2 normal, out Vector2 point)
    {
        var motion = end - start; var length = Math.Sqrt(Distance(start, end));
        point = default;
        if (length >= 2 * radius) return false;
        var fromStart = start + normal * radius; var fromEnd = end + normal * radius;
        if (Distance(fromStart, end) <= (double)radius * radius) { point = fromStart; return true; }
        if (Distance(fromEnd, start) <= (double)radius * radius) { point = fromEnd; return true; }
        var perpendicular = new Vector2(-motion.Y, motion.X) / (float)length;
        var height = (float)Math.Sqrt((double)radius * radius - length * length / 4);
        point = (start + end) * .5f + perpendicular * (perpendicular.Dot(normal) >= 0 ? height : -height);
        return true;
    }
}
