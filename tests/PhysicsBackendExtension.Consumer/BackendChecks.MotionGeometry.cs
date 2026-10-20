using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver
    {
        private readonly record struct MotionRejectedPair(ulong Source, RID Collider, int Shape);
        private static bool RejectedMotionPair(ReadOnlySpan<MotionRejectedPair> pairs, int count, in Part source, Body other, in Part target)
        {
            for (var i = 0; i < count; i++)
                if (pairs[i] == new MotionRejectedPair(source.Source!.InstanceID, other.RID, target.Index)) return true;
            return false;
        }

        private bool TestGeometryBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision,
            bool collideSeparationRay, PhysicsTestMotionResult result)
        {
            var local = Bodies[body]; var recovery = Vector2.Zero; var fraction = 1f; var safeFraction = 1f; Body? best = null;
            Part source = default, target = default; var normal = Vector2.Zero; var point = Vector2.Zero; var depth = 0f;
            MotionHook?.Invoke(body);
            var queryMargin = MathF.Max(margin, .0001f);
            Span<MotionRejectedPair> rejected = stackalloc MotionRejectedPair[32]; var rejectedCount = 0;
            Span<System.Numerics.Vector4> planes = stackalloc System.Numerics.Vector4[32];
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var deepest = 0f; var count = 0; rejectedCount = 0;
                foreach (var other in Bodies.Values)
                {
                    if (!EligibleOther(other)) continue;
                    for (var ownStart = 0; ownStart < local.Shapes;)
                    {
                        var ownEnd = ownStart + 1;
                        while (ownEnd < local.Shapes && local.Parts[ownEnd].Index == local.Parts[ownStart].Index) ownEnd++;
                        var first = ownStart; ownStart = ownEnd;
                        for (var otherStart = 0; otherStart < other.Shapes;)
                        {
                            var otherEnd = otherStart + 1;
                            while (otherEnd < other.Shapes && other.Parts[otherEnd].Index == other.Parts[otherStart].Index) otherEnd++;
                            var second = otherStart; otherStart = otherEnd;
                            var accepted = false; var invalid = false;
                            for (var i = first; i < ownEnd; i++) for (var j = second; j < otherEnd; j++)
                                {
                                    var own = local.Parts[i]; var b = other.Parts[j];
                                    if (own.Concave && b.Concave) continue;
                                    var a = MotionRecoveryPart(own, b, queryMargin); var pose = from; pose.Origin += recovery;
                                    var gap = PartGap(a, pose, b, other.Pose, out var toward, out _, out var onOther);
                                    var away = -toward;
                                    if (gap > 0) continue;
                                    if (!AcceptRecovery(own, b, other, away, -gap, queryMargin)) { invalid = true; continue; }
                                    accepted = true;
                                    if (gap == 0) continue;
                                    RetainMotionPlane(planes, ref count, new(away.X, away.Y, -gap, other.Priority));
                                    if (-gap <= deepest) continue;
                                    deepest = -gap; best = other; source = own; target = b; normal = away; point = onOther; depth = deepest;
                                }
                            if (invalid && !accepted && rejectedCount < rejected.Length)
                                rejected[rejectedCount++] = new(local.Parts[first].Source!.InstanceID, other.RID, other.Parts[second].Index);
                        }
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
                        if (a.Concave && b.Concave || RejectedMotionPair(rejected, rejectedCount, own, other, b)) continue;
                        var sweptMotion = a.Boundary || !b.Boundary && (a.Concave || b.Directed) ? Vector2.Zero : motion;
                        if (!a.Directed && b.OneWay is { } policy && motion.Dot(policy.LocalDirection.Rotated(other.Rotation)) <= 0) continue;
                        if (a.Directed && !a.Slide && !collideSeparationRay) continue;
                        float low, f;
                        if (a.Directed || b.Directed)
                        { if (!DirectedSweepRange(a, start, sweptMotion, b, other.Pose, out low, out f)) continue; }
                        else { if (!SweepParts(a, start, sweptMotion, b, other.Pose, out f)) continue; low = f; }
                        if (f > fraction) continue;
                        var initial = PartGap(a, start, b, other.Pose, out var toward, out _, out _);
                        if (initial <= 0 && -initial <= .05f && motion.Dot(-toward) >= -.000001f) continue;
                        fraction = f; safeFraction = low; hit = other; source = own; target = b;
                    }
            }
            if (hit is null && (!recoveryAsCollision || recovered is null)) { result.SetMotion(recovery + motion); return false; }
            var travel = recovery + motion * safeFraction;
            var restPose = from; restPose.Origin += recovery + motion * fraction;
            Body? rest = null; var deepestRest = MathF.Min(motion.Length(), queryMargin * .05f);
            foreach (var other in Bodies.Values)
            {
                if (!EligibleOther(other)) continue;
                for (var i = 0; i < local.Shapes; i++) for (var j = 0; j < other.Shapes; j++)
                    {
                        var own = local.Parts[i]; var b = other.Parts[j];
                        if (hit is not null && own.Index != source.Index || own.Concave && b.Concave ||
                            RejectedMotionPair(rejected, rejectedCount, own, other, b)) continue;
                        var gap = PartGap(MotionRecoveryPart(own, b, queryMargin), restPose, b, other.Pose, out var toward, out _, out var onOther);
                        var away = -toward;
                        if (hit is not null && fraction > 0 && !own.Directed && !b.Directed && away.Dot(motion) > 0) away = -away;
                        if (-gap <= deepestRest || !AcceptRecovery(own, b, other, away, -gap, queryMargin)) continue;
                        deepestRest = -gap; rest = other; target = b; point = onOther; normal = away; depth = -gap;
                        source = own;
                    }
            }
            if (rest is null) { result.SetMotion(recovery + motion); return false; }
            result.SetCollision(body, source.Index, rest.RID, target.Index, point, normal, depth, rest.PointVelocity(point),
                travel, motion * (1 - safeFraction), safeFraction, fraction);
            MotionResultHook?.Invoke(body, result); return true;
            bool EligibleOther(Body other) => other.RID != body && Eligible(other, local.Mask, true, false, []) &&
                !IsBodyExcludedFromMotionTest(other.RID) && (other.ObjectID == 0 || !IsObjectExcludedFromMotionTest(other.ObjectID)) &&
                !Exceptions.Contains((body, other.RID)) && !Exceptions.Contains((other.RID, body));
        }
        private static Part MotionRecoveryPart(in Part own, in Part target, float margin) =>
            own.Directed && target.Boundary ? own with { Inflation = own.Inflation + margin } : ExpandPart(own, margin);
        private bool AcceptRecovery(in Part source, in Part part, Body body, Vector2 normal, float depth, float margin)
        {
            if (source.Directed || part.OneWay is not { } policy) return true;
            var pass = policy.LocalDirection.Rotated(body.Rotation);
            var allowance = MathF.Max(policy.Margin, margin);
            var velocity = body.Velocity + (body.Mode == PhysicsServer.BodyMode.Kinematic ? body.Surface : Vector2.Zero);
            if (body.Mode != PhysicsServer.BodyMode.Static) allowance += Delta * MathF.Max(0, -velocity.Dot(pass));
            return normal.Dot(pass) < 0 && depth <= allowance + .00001f;
        }
        private static void RetainMotionPlane(Span<System.Numerics.Vector4> planes, ref int count, System.Numerics.Vector4 plane)
        {
            if (count < planes.Length) { planes[count++] = plane; return; }
            var shallow = 0;
            for (var i = 1; i < count; i++) if (planes[i].Z < planes[shallow].Z) shallow = i;
            if (plane.Z > planes[shallow].Z) planes[shallow] = plane;
        }
    }
}
