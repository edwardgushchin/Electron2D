using Electron2D;

public static partial class BackendChecks
{
    private static bool RayCapsule(in Part part, Transform pose, Vector2 from, Vector2 motion,
        out float fraction, out Vector2 normal)
    {
        var center = PartAt(part, pose); var edge = EdgeY(part, pose); var half = edge.Length();
        var axis = half > 0 ? edge / half : Vector2.Down; var side = new Vector2(axis.Y, -axis.X);
        var start = new Vector2((from - center).Dot(side), (from - center).Dot(axis));
        var delta = new Vector2(motion.Dot(side), motion.Dot(axis)); var r = part.Inflation;
        fraction = 1; normal = Vector2.Zero;
        if (ContainsPart(part, pose, from)) { fraction = 0; return true; }
        var hit = false;
        if (delta.X != 0)
        {
            for (var sign = -1; sign <= 1; sign += 2)
            {
                var t = (sign * r - start.X) / delta.X;
                if (t < 0 || t > fraction || MathF.Abs(start.Y + delta.Y * t) > half) continue;
                fraction = t; normal = side * sign; hit = true;
            }
        }
        for (var sign = -1; sign <= 1; sign += 2)
        {
            var cap = center + edge * sign;
            if (!Annulus.Sweep(from, motion, cap, 0, r, out var t) || t > fraction) continue;
            var point = from + motion * t;
            if ((point - center).Dot(axis) * sign < half) continue;
            fraction = t; normal = (point - cap).Normalized(); hit = true;
        }
        return hit;
    }
}
