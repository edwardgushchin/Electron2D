using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Constraint
    {
        private Vector2 _linear;
        private float _motor, _stop;
        internal void BeginSolve() { _linear = default; _motor = _stop = 0; }
        internal void Solve(float dt, bool firstIteration)
        {
            if (Settings.Type == PhysicsServer.JointType.DampedSpring) { if (firstIteration) SolveSpring(dt); return; }
            var a = solver.Bodies[_first]; var b = _second.IsValid() ? solver.Bodies[_second] : null;
            var frameA = a.Pose * Settings.FrameA; var frameB = b is null ? Settings.FrameB : b.Pose * Settings.FrameB;
            var pointA = frameA.Origin; var pointB = frameB.Origin;
            var ra = pointA - a.Center; var rb = b is null ? Vector2.Zero : pointB - b.Center;
            var u = Vector2.Right; var v = Vector2.Down; var error = pointB - pointA;
            var lateral = ra; var twoRows = true;
            var softness = Settings.Type == PhysicsServer.JointType.Pin ? Settings.PinSoftness : 0;
            if (Settings.Type == PhysicsServer.JointType.Groove)
            {
                v = new(MathF.Cos(frameA.Rotation), MathF.Sin(frameA.Rotation)); u = new(-v.Y, v.X);
                var translation = error.Dot(v);
                lateral = ra + error;
                error = new(error.Dot(u), translation - Math.Clamp(translation, Settings.LowerTranslation, Settings.UpperTranslation));
                twoRows = error.Y != 0 || Settings.LowerTranslation == Settings.UpperTranslation;
            }
            var qaU = Cross(lateral, u); var qaV = Cross(ra, v);
            var qbU = Cross(rb, u); var qbV = Cross(rb, v);
            var ma = a.InvMass; var mb = b?.InvMass ?? 0; var ia = a.InvInertia; var ib = b?.InvInertia ?? 0;
            var mass = ma + mb;
            var k11 = (double)mass + ia * qaU * qaU + ib * qbU * qbU + softness;
            var k22 = (double)mass + ia * qaV * qaV + ib * qbV * qbV + softness;
            var k12 = (double)ia * qaU * qaV + ib * qbU * qbV;
            var bias = Settings.Bias == 0 ? solver.ConstraintBias : Settings.Bias;
            var correction = Limit(error * (bias / dt), Settings.MaxBias);
            var velocity = Motion(b) - Motion(a); var wa = Spin(a); var wb = Spin(b);
            var rhs = new Vector2(velocity.Dot(u) + wb * qbU - wa * qaU, velocity.Dot(v) + wb * qbV - wa * qaV) + correction + _linear * softness;
            Vector2 impulse = default;
            if (twoRows)
            {
                var determinant = k11 * k22 - k12 * k12;
                if (determinant > 0) impulse = new((float)((k12 * rhs.Y - k22 * rhs.X) / determinant), (float)((k12 * rhs.X - k11 * rhs.Y) / determinant));
            }
            else if (k11 > 0) impulse.X = (float)(-rhs.X / k11);
            var candidate = _linear + impulse;
            if (twoRows && Settings.Type == PhysicsServer.JointType.Groove && Settings.LowerTranslation != Settings.UpperTranslation &&
                (error.Y > 0 && candidate.Y > 0 || error.Y < 0 && candidate.Y < 0))
            {
                var axial = -_linear.Y;
                candidate = new(k11 > 0 ? _linear.X - (float)((rhs.X + k12 * axial) / k11) : 0, 0);
            }
            var cap = Settings.MaxForce == float.MaxValue ? float.PositiveInfinity : Settings.MaxForce * dt;
            var next = Limit(candidate, cap); impulse = next - _linear; _linear = next;
            var linear = u * impulse.X + v * impulse.Y;
            Apply(a, -linear, -qaU * impulse.X - qaV * impulse.Y);
            Apply(b, linear, qbU * impulse.X + qbV * impulse.Y);
            if (Settings.Type == PhysicsServer.JointType.Pin && ia + ib > 0)
                SolveAngular(a, b, frameB.Rotation - frameA.Rotation, ia + ib, bias, cap, dt);
        }
        private void SolveAngular(Body a, Body? b, float angle, float inverse, float bias, float cap, float dt)
        {
            var below = Settings.PinLimitEnabled && angle < Settings.PinLimitLower;
            var above = Settings.PinLimitEnabled && angle > Settings.PinLimitUpper;
            if (Settings.PinMotorEnabled && !(below && Settings.PinMotorVelocity < 0 || above && Settings.PinMotorVelocity > 0))
            {
                var motorCap = MathF.Min(cap - MathF.Abs(_stop), Settings.PinMotorMaxTorque * 10000 * dt);
                var next = Math.Clamp(_motor + (Settings.PinMotorVelocity - (Spin(b) - Spin(a))) / inverse, -motorCap, motorCap);
                Angular(a, b, next - _motor); _motor = next;
            }
            if (below || above)
            {
                var error = angle - (below ? Settings.PinLimitLower : Settings.PinLimitUpper);
                var target = -Math.Clamp(error * bias / dt, -Settings.MaxBias, Settings.MaxBias);
                var stopCap = cap - MathF.Abs(_motor);
                var next = Math.Clamp(_stop + (target - (Spin(b) - Spin(a))) / inverse, below ? 0 : -stopCap, above ? 0 : stopCap);
                Angular(a, b, next - _stop); _stop = next;
            }
        }
        private static Vector2 Motion(Body? b) => b is null || b.Mode == PhysicsServer.BodyMode.Static ? Vector2.Zero : b.Velocity;
        private static float Spin(Body? b) => b is null || b.Mode == PhysicsServer.BodyMode.Static ? 0 : b.Angular;
        private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
        private static Vector2 Limit(Vector2 value, float limit)
        {
            var length = value.Length(); return length > limit ? value * (limit / length) : value;
        }
        private static void Apply(Body? b, Vector2 impulse, float moment)
        {
            if (b is not null && (impulse != Vector2.Zero || moment != 0) && (b.InvMass != 0 || b.InvInertia != 0)) b.AddImpulse(impulse, moment);
        }
        private static void Angular(Body a, Body? b, float impulse) { Apply(a, Vector2.Zero, -impulse); Apply(b, Vector2.Zero, impulse); }
    }
}
