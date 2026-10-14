using Electron2D;

public static partial class BackendChecks
{
    private static Vector2 BoundaryNormal(in Part plane, Transform pose) => pose.BasisXform(plane.PlaneNormal).Normalized();
    private static float BoundaryGap(in Part first, Transform a, in Part second, Transform b,
        out Vector2 normal, out Vector2 pointA, out Vector2 pointB)
    {
        normal = pointA = pointB = default;
        if (first.Boundary && second.Boundary) return float.PositiveInfinity;
        var plane = first.Boundary ? first : second; var planePose = first.Boundary ? a : b;
        var finite = first.Boundary ? second : first; var finitePose = first.Boundary ? b : a;
        if (finite.Directed && finite.EdgeY == Vector2.Zero || CoreCount(finite) == 0) return float.PositiveInfinity;
        var outward = BoundaryNormal(plane, planePose); var origin = PartAt(plane, planePose);
        var tip = LeadingPart(finite, finitePose, -outward); var gap = (tip - origin).Dot(outward);
        var onPlane = tip - outward * gap;
        normal = first.Boundary ? outward : -outward;
        pointA = first.Boundary ? onPlane : tip; pointB = first.Boundary ? tip : onPlane;
        return gap;
    }
    private static bool SweepBoundary(in Part first, Transform a, Vector2 motion, in Part second, Transform b, out float fraction)
    {
        fraction = 0;
        var gap = BoundaryGap(first, a, second, b, out var towardSecond, out _, out _);
        if (gap <= 0) return true;
        if (!float.IsFinite(gap)) return false;
        var closing = motion.Dot(towardSecond);
        if (closing <= 0) return false;
        fraction = gap / closing; return fraction <= 1;
    }
    private static bool RayBoundary(in Part plane, Transform pose, Vector2 from, Vector2 motion, out float fraction, out Vector2 normal)
    {
        normal = BoundaryNormal(plane, pose); var gap = (from - PartAt(plane, pose)).Dot(normal);
        fraction = 0;
        if (gap <= 0) { normal = Vector2.Zero; return true; }
        var closing = -motion.Dot(normal); if (closing <= 0) return false;
        fraction = gap / closing; return fraction <= 1;
    }
    internal static bool CollideBoundaryGeometry(ShapeGeometry first, Transform a, Vector2 firstMotion,
        ShapeGeometry second, Transform b, Vector2 secondMotion, Span<Vector2> output, out int count)
    {
        count = 0; var firstPlane = first.Type == PhysicsServer.ShapeType.WorldBoundary;
        if (firstPlane && second.Type == PhysicsServer.ShapeType.WorldBoundary) return false;
        var plane = GeometryPart(firstPlane ? first : second, firstPlane ? a : b);
        var finite = firstPlane ? second : first; var pose = firstPlane ? b : a;
        pose.Origin += firstPlane ? secondMotion : firstMotion;
        var hit = false; var outward = plane.PlaneNormal; var tangent = new Vector2(-outward.Y, outward.X);
        for (var i = 0; i < GeometryCount(finite); i++)
        {
            var piece = GeometryPart(finite, pose, 0, i);
            var gap = BoundaryGap(plane, Transform.Identity, piece, Transform.Identity, out _, out _, out var tip);
            if (gap >= 0) continue;
            hit = true;
            SupportRange(piece, Transform.Identity, -outward, tangent, out var lo, out var hi);
            for (var end = 0; end < (lo == hi ? 1 : 2); end++)
            {
                var point = tip + tangent * ((end == 0 ? lo : hi) - tip.Dot(tangent));
                var projected = point - outward * gap;
                if (output.Length < 2) continue;
                var slot = count;
                if (count == output.Length / 2)
                {
                    slot = 0;
                    for (var k = 1; k < count; k++)
                        if (output[2 * k].DistanceSquaredTo(output[2 * k + 1]) < output[2 * slot].DistanceSquaredTo(output[2 * slot + 1])) slot = k;
                    if (gap * gap <= output[2 * slot].DistanceSquaredTo(output[2 * slot + 1])) continue;
                }
                else count++;
                output[2 * slot] = firstPlane ? projected : point; output[2 * slot + 1] = firstPlane ? point : projected;
            }
        }
        return hit;
    }
    private static bool SweptBoundary(Body a, in Part first, Body b, in Part second, float duration, bool full,
        float spinA, float spinB, out float fraction)
    {
        fraction = 1; if (first.Boundary && second.Boundary) return false;
        var firstPlane = first.Boundary; var plane = firstPlane ? first : second; var planeBody = firstPlane ? a : b;
        var finite = firstPlane ? second : first; var finiteBody = firstPlane ? b : a;
        var planeSpin = firstPlane ? spinA : spinB; var finiteSpin = firstPlane ? spinB : spinA;
        var planeMotion = Solver.Motion(planeBody); var finiteMotion = Solver.Motion(finiteBody);
        if (!full && !firstPlane)
        {
            finite = new(first.Index, LeadingPart(first, a.Pose, (finiteMotion - planeMotion).Normalized()), 0, 0);
            finiteSpin = 0;
        }
        var finiteArm = finite.Offset - (finiteBody.Dynamic ? finiteBody.LocalCenter : Vector2.Zero);
        var planeArm = plane.Offset - (planeBody.Dynamic ? planeBody.LocalCenter : Vector2.Zero);
        var relative = finiteMotion - planeMotion; var extent = finite.Outer + finiteArm.Length();
        var separation = (finiteBody.Center - planeBody.Center).Length() + relative.Length() * duration;
        var speed = relative.Length() + MathF.Abs(finiteSpin) * extent + MathF.Abs(planeSpin) * (separation + planeArm.Length() + extent);
        var tolerance = .0001f * MathF.Min(first.Thickness, second.Thickness); var time = 0f;
        for (var iteration = 0; iteration < 1024; iteration++)
        {
            var finitePose = !full && !firstPlane ? Transform.Identity.Translated(finiteMotion * time) : Moving(finiteBody, finiteMotion, finiteSpin, time);
            var planePose = Moving(planeBody, planeMotion, planeSpin, time);
            var gap = BoundaryGap(plane, planePose, finite, finitePose, out _, out _, out _);
            if (gap <= tolerance) { fraction = time / duration; return true; }
            if (!(speed > 0)) return false;
            var advance = gap / speed; var next = time + advance;
            if (next > duration) return false;
            if (!(next > time)) throw new InvalidOperationException("A rotating boundary sweep cannot advance.");
            time = next;
        }
        throw new InvalidOperationException("A rotating boundary sweep exceeded its advancement budget.");
        static Transform Moving(Body body, Vector2 linear, float spin, float time)
        {
            var rotation = body.Rotation + spin * time; var center = body.Dynamic ? body.LocalCenter : Vector2.Zero;
            return new(rotation, body.Position + linear * time + center.Rotated(body.Rotation) - center.Rotated(rotation));
        }
    }
}
