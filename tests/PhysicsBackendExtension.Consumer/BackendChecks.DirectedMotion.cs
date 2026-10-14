using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver
    {
        private bool HasDirectedMotionParts(Body body)
        {
            foreach (var b in Bodies.Values) for (var i = 0; i < b.Shapes; i++) if (b.Parts[i].Directed) return true;
            return false;
        }
        private bool TestDirectedBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision,
            bool collideSeparationRay, PhysicsTestMotionResult result)
        {
            var local = Bodies[body]; var recovery = Vector2.Zero; var fraction = 1f; var safeFraction = 1f; Body? best = null;
            Part source = default, target = default; var normal = Vector2.Zero; var point = Vector2.Zero; var depth = 0f;
            MotionHook?.Invoke(body);
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var deepest = 0f; var away = Vector2.Zero;
                foreach (var other in Bodies.Values)
                {
                    if (!EligibleOther(other)) continue;
                    for (var i = 0; i < local.Shapes; i++) for (var j = 0; j < other.Shapes; j++)
                        {
                            var a = ExpandPart(local.Parts[i], MathF.Max(margin, .0001f)); var b = other.Parts[j]; var pose = from; pose.Origin += recovery;
                            var gap = PartGap(a, pose, b, other.Pose, out var toward, out _, out var onOther);
                            if (-gap <= deepest) continue;
                            deepest = -gap; away = -toward; best = other; source = local.Parts[i]; target = b; normal = away; point = onOther; depth = deepest;
                        }
                }
                if (deepest <= 0) break;
                recovery += away * MathF.Max(0, deepest - margin * .05f) * .4f;
            }
            var recovered = best; var start = from; start.Origin += recovery; Body? hit = null;
            foreach (var other in Bodies.Values)
            {
                if (!EligibleOther(other)) continue;
                for (var i = 0; i < local.Shapes; i++) for (var j = 0; j < other.Shapes; j++)
                    {
                        var a = local.Parts[i]; var b = other.Parts[j];
                        if (a.Directed && !a.Slide && !collideSeparationRay) continue;
                        float low, f;
                        if (a.Directed || b.Directed)
                        { if (!DirectedSweepRange(a, start, motion, b, other.Pose, out low, out f)) continue; }
                        else { if (!SweepParts(a, start, motion, b, other.Pose, out f)) continue; low = f; }
                        if (f > fraction) continue;
                        var initial = PartGap(a, start, b, other.Pose, out var toward, out _, out _);
                        if (initial <= 0 && -initial <= .05f && motion.Dot(-toward) >= -.000001f) continue;
                        fraction = f; safeFraction = low; hit = other; source = a; target = b;
                    }
            }
            if (hit is null && (!recoveryAsCollision || recovered is null)) { result.SetMotion(recovery + motion); return false; }
            var travel = recovery + motion * safeFraction;
            if (hit is not null)
            {
                var pose = from; pose.Origin += recovery + motion * fraction;
                depth = MathF.Max(0, -PartGap(ExpandPart(source, MathF.Max(margin, .0001f)), pose, target, hit.Pose, out var toward, out _, out point));
                normal = -toward; best = hit;
            }
            result.SetCollision(body, source.Index, best!.RID, target.Index, point, normal, depth, best.PointVelocity(point),
                travel, motion * (1 - safeFraction), safeFraction, fraction);
            MotionResultHook?.Invoke(body, result); return true;
            bool EligibleOther(Body other) => other.RID != body && Eligible(other, local.Mask, true, false, []) &&
                !IsBodyExcludedFromMotionTest(other.RID) && (other.ObjectID == 0 || !IsObjectExcludedFromMotionTest(other.ObjectID)) &&
                !Exceptions.Contains((body, other.RID)) && !Exceptions.Contains((other.RID, body));
        }
    }
}
