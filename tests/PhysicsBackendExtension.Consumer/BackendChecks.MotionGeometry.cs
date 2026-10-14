using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver
    {
        private bool TestGeometryBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision,
            bool collideSeparationRay, PhysicsTestMotionResult result)
        {
            var local = Bodies[body]; var recovery = Vector2.Zero; var fraction = 1f; var safeFraction = 1f; Body? best = null;
            Part source = default, target = default; var normal = Vector2.Zero; var point = Vector2.Zero; var depth = 0f;
            MotionHook?.Invoke(body);
            var queryMargin = MathF.Max(margin, .0001f);
            Span<System.Numerics.Vector4> planes = stackalloc System.Numerics.Vector4[32];
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var deepest = 0f; var count = 0;
                foreach (var other in Bodies.Values)
                {
                    if (!EligibleOther(other)) continue;
                    for (var i = 0; i < local.Shapes; i++) for (var j = 0; j < other.Shapes; j++)
                        {
                            var own = local.Parts[i]; var b = other.Parts[j];
                            var a = MotionRecoveryPart(own, b, queryMargin); var pose = from; pose.Origin += recovery;
                            var gap = PartGap(a, pose, b, other.Pose, out var toward, out _, out var onOther);
                            var away = -toward;
                            if (gap >= 0 || !AcceptRecovery(b, other, away, -gap, queryMargin)) continue;
                            RetainMotionPlane(planes, ref count, new(away.X, away.Y, -gap, other.Priority));
                            if (-gap <= deepest) continue;
                            deepest = -gap; best = other; source = local.Parts[i]; target = b; normal = away; point = onOther; depth = deepest;
                        }
                }
                if (deepest <= 0) break;
                double total = 0;
                for (var i = 0; i < count; i++) total += planes[i].W;
                var normalization = total < .00001f ? 1 : count / total;
                var correction = Vector2.Zero;
                for (var i = 0; i < count; i++)
                {
                    var plane = planes[i]; var direction = new Vector2(plane.X, plane.Y);
                    var excess = plane.Z - direction.Dot(correction) - queryMargin * .05f;
                    if (excess > .00001f) correction += direction * (excess * .4f * (float)(plane.W * normalization));
                }
                if (correction == Vector2.Zero) break;
                recovery += correction;
            }
            var recovered = best; var start = from; start.Origin += recovery; Body? hit = null;
            foreach (var other in Bodies.Values)
            {
                if (motion == Vector2.Zero || !EligibleOther(other)) continue;
                for (var i = 0; i < local.Shapes; i++) for (var j = 0; j < other.Shapes; j++)
                    {
                        var own = local.Parts[i]; var a = own; var b = other.Parts[j];
                        if (b.OneWay is { } policy && motion.Dot(policy.LocalDirection.Rotated(other.Rotation)) <= 0) continue;
                        if (a.Directed && !a.Slide && !collideSeparationRay) continue;
                        float low, f;
                        if (a.Directed || b.Directed)
                        { if (!DirectedSweepRange(a, start, motion, b, other.Pose, out low, out f)) continue; }
                        else { if (!SweepParts(a, start, motion, b, other.Pose, out f)) continue; low = f; }
                        if (f > fraction) continue;
                        var initial = PartGap(a, start, b, other.Pose, out var toward, out _, out _);
                        if (initial <= 0 && -initial <= .05f && motion.Dot(-toward) >= -.000001f) continue;
                        fraction = f; safeFraction = low; hit = other; source = own; target = b;
                    }
            }
            if (hit is null && (!recoveryAsCollision || recovered is null)) { result.SetMotion(recovery + motion); return false; }
            var travel = recovery + motion * safeFraction;
            if (hit is not null)
            {
                var pose = from; pose.Origin += recovery + motion * fraction;
                depth = MathF.Max(0, -PartGap(MotionRecoveryPart(source, target, queryMargin), pose, target, hit.Pose, out var toward, out _, out point));
                normal = -toward; best = hit;
            }
            result.SetCollision(body, source.Index, best!.RID, target.Index, point, normal, depth, best.PointVelocity(point),
                travel, motion * (1 - safeFraction), safeFraction, fraction);
            MotionResultHook?.Invoke(body, result); return true;
            bool EligibleOther(Body other) => other.RID != body && Eligible(other, local.Mask, true, false, []) &&
                !IsBodyExcludedFromMotionTest(other.RID) && (other.ObjectID == 0 || !IsObjectExcludedFromMotionTest(other.ObjectID)) &&
                !Exceptions.Contains((body, other.RID)) && !Exceptions.Contains((other.RID, body));
        }
        private static Part MotionRecoveryPart(in Part own, in Part target, float margin) =>
            own.Directed && target.Boundary ? own with { Inflation = own.Inflation + margin } : ExpandPart(own, margin);
        private static bool AcceptRecovery(in Part part, Body body, Vector2 normal, float depth, float margin) =>
            part.OneWay is not { } policy || normal.Dot(policy.LocalDirection.Rotated(body.Rotation)) < 0 && depth <= MathF.Max(policy.Margin, margin);
        private static void RetainMotionPlane(Span<System.Numerics.Vector4> planes, ref int count, System.Numerics.Vector4 plane)
        {
            if (count < planes.Length) { planes[count++] = plane; return; }
            var shallow = 0;
            for (var i = 1; i < count; i++) if (planes[i].Z < planes[shallow].Z) shallow = i;
            if (plane.Z > planes[shallow].Z) planes[shallow] = plane;
        }
    }
}
