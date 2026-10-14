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
                        Exceptions.Contains((a.RID, b.RID)) || Exceptions.Contains((b.RID, a.RID))) continue;
                    var motion = Motion(a) - Motion(b); var speed = motion.Length() + AngularShapeSpeed(a) + AngularShapeSpeed(b);
                    if (speed == 0) continue;
                    var hit = false; var fraction = 1f;
                    if (a.Dynamic && a.ContinuousMode != CCDMode.Disabled && SweptContact(a, b, motion * duration, duration, out var first)) { hit = true; fraction = first; }
                    if (b.Dynamic && b.ContinuousMode != CCDMode.Disabled && SweptContact(b, a, -motion * duration, duration, out var second)) { hit = true; fraction = MathF.Min(fraction, second); }
                    if (!hit) continue;
                    var extent = MathF.Min(a.MinimumThickness, b.MinimumThickness);
                    interval = MathF.Min(interval, .25f * extent / speed);
                    if (fraction > 0) interval = MathF.Min(interval, fraction * duration + .0001f * extent / speed);
                }
            return interval;
        }
        private static Vector2 Motion(Body body) => body.IsArea || body.Mode == PhysicsServer.BodyMode.Static ||
            body.Dynamic && !body.Awake ? Vector2.Zero : body.Velocity;
        private static float RotationMotion(Body body) => body.IsArea || !body.Awake || body.Locked || body.Mode == PhysicsServer.BodyMode.Static ? 0 : body.Angular;
        private static Vector2 RotationArm(Body body, in Part part) => part.Offset - (body.Dynamic ? body.LocalCenter : Vector2.Zero);
        private static float AngularShapeSpeed(Body body)
        {
            var radius = 0f;
            for (var i = 0; i < body.Shapes; i++) radius = MathF.Max(radius, RotationArm(body, body.Parts[i]).Length() +
                (body.Parts[i].Rectangle ? body.Parts[i].Outer : 0));
            return MathF.Abs(RotationMotion(body)) * radius;
        }
        private bool SweptContact(Body a, Body b, Vector2 motion, float duration, out float fraction)
        {
            fraction = 1; var hit = false;
            var full = a.ContinuousMode == CCDMode.CastShape || b.Dynamic && b.ContinuousMode == CCDMode.CastShape;
            var spinA = full ? RotationMotion(a) : 0; var spinB = RotationMotion(b);
            for (var i = 0; i < a.Shapes; i++) for (var j = 0; j < b.Shapes; j++)
                {
                    var first = a.Parts[i]; var second = b.Parts[j];
                    if (!AllowsContact(a.RID, first.Index, b.RID, second.Index)) continue;
                    if (first.Rectangle || second.Rectangle)
                    {
                        if (SweptContour(a, first, b, second, duration, full, spinA, spinB, out var contour))
                        { hit = true; fraction = MathF.Min(fraction, contour); }
                        continue;
                    }
                    if (spinA * RotationArm(a, first).Length() != 0 || spinB * RotationArm(b, second).Length() != 0)
                    {
                        if (CurvedPart(a, first, b, second, duration, full, spinA, spinB, out var curved))
                        { hit = true; fraction = MathF.Min(fraction, curved); }
                        continue;
                    }
                    var point = a.PartCenter(first) - b.PartCenter(second); var outer = first.Outer + second.Outer;
                    var inner = MathF.Max(0, first.Inner > 0 ? first.Inner - second.Outer : second.Inner - first.Outer);
                    if (!full)
                    { point += motion.Normalized() * first.Outer; outer = second.Outer; inner = second.Inner; }
                    if (SweepPart(point, motion, inner, outer, out var current)) { hit = true; fraction = MathF.Min(fraction, current); }
                }
            return hit;
        }
        private static bool CurvedPart(Body a, in Part first, Body b, in Part second, float duration, bool full, float spinA, float spinB, out float fraction)
        {
            var armA = RotationArm(a, first); var armB = RotationArm(b, second);
            var linear = Motion(a) - Motion(b);
            var speed = linear.Length() + MathF.Abs(spinA) * armA.Length() + MathF.Abs(spinB) * armB.Length();
            var start = a.PartCenter(first) - b.PartCenter(second);
            var radius = full ? first.Outer : 0;
            if (!full) start += linear.Normalized() * first.Outer;
            var inner = MathF.Max(0, full && first.Inner > 0 ? first.Inner - second.Outer : second.Inner - radius);
            var outer = radius + second.Outer;
            var tolerance = .0001f * MathF.Min(first.Outer - first.Inner, second.Outer - second.Inner);
            var originalA = armA.Rotated(a.Rotation); var originalB = armB.Rotated(b.Rotation);
            var acceleration = (double)spinA * spinA * armA.Length() + (double)spinB * spinB * armB.Length();
            double time = 0;
            for (var iteration = 0; iteration < 256; iteration++)
            {
                var elapsed = (float)time;
                var currentA = armA.Rotated(a.Rotation + spinA * elapsed); var currentB = armB.Rotated(b.Rotation + spinB * elapsed);
                var point = start + linear * elapsed + currentA - originalA - (currentB - originalB);
                var distance = point.Length();
                var gap = MathF.Max(distance - outer, inner - distance);
                if (gap <= tolerance) { fraction = elapsed / duration; return true; }
                if (!(speed > 0) || !float.IsFinite(speed) || !float.IsFinite(gap)) throw new InvalidOperationException("Independent angular sweep has invalid geometry or motion.");
                var velocity = linear + new Vector2(-currentA.Y, currentA.X) * spinA - new Vector2(-currentB.Y, currentB.X) * spinB;
                var radial = distance > 0 ? point.Dot(velocity) / distance : velocity.Length();
                var closing = distance > outer ? -radial : radial;
                // Bound the curved path against the current separating axis; tangent separation need not exhaust tiny global-speed advances.
                var curvature = acceleration + (distance < inner && distance > 0 ? velocity.LengthSquared() / (double)distance : 0);
                double advance;
                if (curvature == 0)
                {
                    if (closing <= 0) { fraction = 1; return false; }
                    advance = gap / (double)closing;
                }
                else
                {
                    var root = Math.Sqrt((double)closing * closing + 2 * curvature * gap);
                    advance = closing >= 0 ? 2d * gap / (closing + root) : (root - closing) / curvature;
                }
                if (!(advance > 0) || !double.IsFinite(advance)) throw new InvalidOperationException("Independent angular sweep cannot advance.");
                time += advance;
                if (time > duration) { fraction = 1; return false; }
            }
            throw new InvalidOperationException("Independent angular sweep exceeded its advancement budget.");
        }
        private static bool SweepPart(Vector2 point, Vector2 motion, float inner, float outer, out float fraction)
        {
            var distance = (double)point.X * point.X + (double)point.Y * point.Y;
            fraction = 0;
            if (distance >= (double)inner * inner && distance <= (double)outer * outer) return true;
            var radius = distance < (double)inner * inner ? inner : outer;
            var length = (double)motion.X * motion.X + (double)motion.Y * motion.Y;
            var projection = (double)point.X * motion.X + (double)point.Y * motion.Y;
            var discriminant = projection * projection - length * (distance - (double)radius * radius);
            if (length == 0 || discriminant < 0) return false;
            var root = Math.Sqrt(discriminant); var near = (-projection - root) / length; var far = (-projection + root) / length;
            var value = near >= 0 ? near : far; fraction = (float)value;
            return value >= 0 && value <= 1;
        }
    }
}
