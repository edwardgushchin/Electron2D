using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver
    {
        private bool HasContinuousBodies()
        {
            foreach (var body in Bodies.Values) if (body.Dynamic && body.ContinuousMode != CCDMode.Disabled) return true;
            return false;
        }
        private void SolveContinuous(float delta)
        {
            foreach (var body in Bodies.Values) body.FrameForcesApplied = body.PrepareAdvance(delta);
            SolveJoints(delta, true);
            var remaining = delta; var intervals = 0;
            while (remaining > 0)
            {
                if (++intervals > 4096) throw new InvalidOperationException("Independent continuous motion exceeded its interval budget.");
                foreach (var body in Bodies.Values)
                    if (!body.FrameForcesApplied) body.FrameForcesApplied = body.PrepareAdvance(remaining);
                var step = ContinuousInterval(remaining);
                if (!(step > 0) || !float.IsFinite(step) || step < remaining && remaining - step == remaining)
                    throw new InvalidOperationException("Independent continuous interval is not representable.");
                foreach (var body in Bodies.Values) body.Move(step);
                SolveContacts(); SolveJoints(delta, false); UpdateSleep(step);
                remaining = step == remaining ? 0 : remaining - step;
            }
        }
        private float ContinuousInterval(float duration)
        {
            var interval = duration;
            foreach (var a in Bodies.Values)
                foreach (var b in Bodies.Values)
                {
                    if (a.RID.GetID() >= b.RID.GetID() || a.IsArea || b.IsArea || a.Shapes == 0 || b.Shapes == 0 ||
                        a.InvMass + b.InvMass == 0 || (a.Mask & b.Layer) == 0 && (b.Mask & a.Layer) == 0 ||
                        Exceptions.Contains((a.RID, b.RID)) || Exceptions.Contains((b.RID, a.RID)) || !AllowsContact(a.RID, b.RID)) continue;
                    var motion = Motion(a) - Motion(b); var speed = motion.Length();
                    if (speed == 0) continue;
                    var hit = false; var fraction = 1f;
                    if (a.Dynamic && a.ContinuousMode != CCDMode.Disabled && SweptContact(a, b, motion * duration, out var first)) { hit = true; fraction = first; }
                    if (b.Dynamic && b.ContinuousMode != CCDMode.Disabled && SweptContact(b, a, -motion * duration, out var second)) { hit = true; fraction = MathF.Min(fraction, second); }
                    if (!hit) continue;
                    var extent = MathF.Min(a.Radius - a.Hole, b.Radius - b.Hole);
                    interval = MathF.Min(interval, .25f * extent / speed);
                    if (fraction > 0) interval = MathF.Min(interval, fraction * duration + .0001f * extent / speed);
                }
            return interval;
        }
        private static Vector2 Motion(Body body) => body.IsArea || body.Mode == PhysicsServer.BodyMode.Static ||
            body.Dynamic && !body.Awake ? Vector2.Zero : body.Velocity;
        private static bool SweptContact(Body a, Body b, Vector2 motion, out float fraction)
        {
            var point = a.Position - b.Position; var outer = a.Radius + b.Radius;
            var inner = MathF.Max(0, a.Hole > 0 ? a.Hole - b.Radius : b.Hole - a.Radius);
            if (a.ContinuousMode == CCDMode.CastRay)
            {
                point += motion.Normalized() * a.Radius; outer = b.Radius; inner = b.Hole;
            }
            var distance = (double)point.X * point.X + (double)point.Y * point.Y;
            fraction = 0;
            if (distance >= (double)inner * inner && distance <= (double)outer * outer) return true;
            var radius = distance < (double)inner * inner ? inner : outer;
            var length = (double)motion.X * motion.X + (double)motion.Y * motion.Y;
            var projection = (double)point.X * motion.X + (double)point.Y * motion.Y;
            var discriminant = projection * projection - length * (distance - (double)radius * radius);
            if (length == 0 || discriminant < 0) return false;
            var root = Math.Sqrt(discriminant); var near = (-projection - root) / length; var far = (-projection + root) / length;
            var hit = near >= 0 ? near : far; fraction = (float)hit;
            return hit >= 0 && hit <= 1;
        }
    }
}
