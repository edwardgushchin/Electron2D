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
        var a = Dimensions(first); var b = Dimensions(second);
        if (b.Inner != 0) throw new NotSupportedException("The fixture exercises annulus/circle pairs.");
        count = 0;
        if (!Sweep(secondPose.Origin, secondMotion - firstMotion, firstPose.Origin, MathF.Max(0, a.Inner - b.Outer), a.Outer + b.Outer, out var fraction)) return false;
        var center = secondPose.Origin + secondMotion * fraction;
        Contact(firstPose.Origin + firstMotion * fraction, a.Inner, a.Outer, center, b.Outer, out var normal, out _, out var point);
        if (contacts.Length >= 2) { contacts[0] = point; contacts[1] = center - normal * b.Outer; count = 1; }
        return true;
    }
}
