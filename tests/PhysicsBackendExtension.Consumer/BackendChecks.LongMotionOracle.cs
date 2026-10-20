using Electron2D;

public static partial class BackendChecks
{
    private static void CheckLongMotionOracle()
    {
        Vector2[] box = [new(-1, -3), new(1, -3), new(1, 3), new(-1, 3)]; Vector2[] line = [new(0, -2), new(0, 2)];
        Span<(double X, double Y)> hull = stackalloc (double, double)[32];
        var count = LongMotionHull(box, Transform.Identity, box, new(0, new Vector2(10, 0)), hull);
        Check(Math.Abs(LongHullEntry(hull[..count], new(20, 0), 0) - .4) < 1e-12 && Math.Abs(LongHullEntry(hull[..count], new(20, 0), .2) - .39) < 1e-12, "The independent hull oracle retains planar entry and rounded margin");
        count = LongMotionHull(line, Transform.Identity, line, new(0, new Vector2(10, 4.5f)), hull);
        Check(double.IsPositiveInfinity(LongHullEntry(hull[..count], new(20, 0), 0)) && Math.Abs(LongHullEntry(hull[..count], new(20, 0), .5) - .5) < 1e-12, "The independent hull oracle separates a finite segment miss from endpoint tangency");
        count = LongMotionHull(line, Transform.Identity, line, new(Vector2.Down, Vector2.Left, new(10, 0)), hull);
        Check(Math.Abs(LongHullEntry(hull[..count], new(20, 0), 0) - .4) < 1e-12, "Crossed segments form the expected independent Minkowski rectangle");
    }
    private static bool SweepFlatParts(in Part first, Transform a, Vector2 motion, in Part second, Transform b, out float fraction)
    {
        var low = 0d; var high = 1d; fraction = 0; var tested = false;
        for (var side = 0; side < 2; side++)
        {
            var part = side == 0 ? first : second; var pose = side == 0 ? a : b; var count = CoreCount(part);
            for (var i = 0; i < count; i++) for (var direction = 0; direction < 2; direction++)
                {
                    var p = CoreVertex(part, pose, i); var q = CoreVertex(part, pose, (i + 1) % count);
                    var x = direction == 0 ? (double)p.Y - q.Y : (double)q.X - p.X; var y = direction == 0 ? (double)q.X - p.X : (double)q.Y - p.Y;
                    if (x == 0 && y == 0) continue;
                    tested = true;
                    var a0 = double.PositiveInfinity; var a1 = double.NegativeInfinity; var b0 = double.PositiveInfinity; var b1 = double.NegativeInfinity;
                    for (var j = 0; j < CoreCount(first); j++) { var v = CoreVertex(first, a, j); var dot = v.X * x + v.Y * y; a0 = Math.Min(a0, dot); a1 = Math.Max(a1, dot); }
                    for (var j = 0; j < CoreCount(second); j++) { var v = CoreVertex(second, b, j); var dot = v.X * x + v.Y * y; b0 = Math.Min(b0, dot); b1 = Math.Max(b1, dot); }
                    var speed = motion.X * x + motion.Y * y;
                    if (speed == 0) { if (a1 < b0 || b1 < a0) return false; continue; }
                    var enter = (b0 - a1) / speed; var leave = (b1 - a0) / speed;
                    if (speed < 0) (enter, leave) = (leave, enter);
                    low = Math.Max(low, enter); high = Math.Min(high, leave); if (low > high) return false;
                }
        }
        if (!tested) return Annulus.Sweep(PartAt(first, a), motion, PartAt(second, b), 0, 0, out fraction);
        fraction = (float)low; return low <= 1 && high >= 0;
    }
    private static int LongMotionHull(ReadOnlySpan<Vector2> a, Transform first, ReadOnlySpan<Vector2> b, Transform second, Span<(double X, double Y)> hull)
    {
        Span<(double X, double Y)> points = stackalloc (double, double)[16]; var count = 0;
        foreach (var av in a) foreach (var bv in b)
            { var p = first * av; var q = second * bv; points[count++] = ((double)q.X - p.X, (double)q.Y - p.Y); }
        for (var i = 1; i < count; i++)
        {
            var value = points[i]; var j = i;
            while (j > 0 && (points[j - 1].X > value.X || points[j - 1].X == value.X && points[j - 1].Y > value.Y)) { points[j] = points[j - 1]; j--; }
            points[j] = value;
        }
        var unique = 0;
        for (var i = 0; i < count; i++) if (unique == 0 || points[i] != points[unique - 1]) points[unique++] = points[i];
        if (unique == 1) { hull[0] = points[0]; return 1; }
        var size = 0;
        for (var i = 0; i < unique; i++) { while (size >= 2 && Turn(hull[size - 2], hull[size - 1], points[i]) <= 0) size--; hull[size++] = points[i]; }
        var lower = size;
        for (var i = unique - 2; i >= 0; i--) { while (size > lower && Turn(hull[size - 2], hull[size - 1], points[i]) <= 0) size--; hull[size++] = points[i]; }
        return size - 1;
        static double Turn((double X, double Y) p, (double X, double Y) q, (double X, double Y) r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
    }
    private static double LongHullEntry(ReadOnlySpan<(double X, double Y)> hull, Vector2 motion, double radius)
    {
        var best = double.PositiveInfinity;
        if (hull.Length >= 3)
        {
            var low = 0d; var high = 1d;
            for (var i = 0; i < hull.Length; i++)
            {
                var a = hull[i]; var b = hull[(i + 1) % hull.Length]; var dx = b.X - a.X; var dy = b.Y - a.Y;
                var numerator = dx * a.Y - dy * a.X; var denominator = dx * motion.Y - dy * motion.X;
                if (denominator == 0) { if (numerator > 0) high = -1; }
                else if (denominator > 0) low = Math.Max(low, numerator / denominator); else high = Math.Min(high, numerator / denominator);
            }
            if (low <= high) best = low;
        }
        for (var i = 0; i < hull.Length; i++)
        {
            var a = hull[i]; var b = hull[(i + 1) % hull.Length]; var ex = b.X - a.X; var ey = b.Y - a.Y; var size = Math.Sqrt(ex * ex + ey * ey);
            if (size == 0) continue;
            ex /= size; ey /= size;
            var x = -a.X * ey + a.Y * ex; var y = -a.X * ex - a.Y * ey;
            var dx = motion.X * ey - motion.Y * ex; var dy = motion.X * ex + motion.Y * ey;
            if (dx != 0) for (var sign = -1; sign <= 1; sign += 2) { var t = (sign * radius - x) / dx; if (t >= 0 && y + t * dy >= 0 && y + t * dy <= size) best = Math.Min(best, t); }
            var speed = Math.Sqrt(dx * dx + dy * dy);
            for (var end = 0; end < 2; end++)
            {
                var cy = y - end * size; var along = (x * dx + cy * dy) / speed; var side = (x * dy - cy * dx) / speed;
                var discriminant = (radius - Math.Abs(side)) * (radius + Math.Abs(side));
                if (along >= 0 || discriminant < 0) continue;
                var t = (-along - Math.Sqrt(discriminant)) / speed; var at = y + t * dy;
                if (t >= 0 && (end == 0 ? at <= 0 : at >= size)) best = Math.Min(best, t);
            }
        }
        return best;
    }
}
