using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver
    {
        private static Vector2 SolveContact(Body a, Body b, Vector2 normal, Vector2 point)
        {
            var ra = point - a.Center; var rb = point - b.Center;
            var responseA = (a.Mask & b.Layer) != 0; var responseB = (b.Mask & a.Layer) != 0;
            var massA = responseA ? a.InvMass : 0; var massB = responseB ? b.InvMass : 0;
            var inertiaA = responseA ? a.InvInertia : 0; var inertiaB = responseB ? b.InvInertia : 0;
            var relative = b.PointVelocity(point) - a.PointVelocity(point);
            var speed = relative.Dot(normal);
            if (speed >= 0) return Vector2.Zero;
            var inverse = Effective(normal);
            var bounce = Math.Clamp(a.Bounce + b.Bounce, 0, 1);
            var normalImpulse = -(1 + bounce) * speed / inverse;
            var impulse = normal * normalImpulse;
            Apply(impulse);
            var tangent = new Vector2(-normal.Y, normal.X);
            relative = b.PointVelocity(point) - a.PointVelocity(point);
            var friction = MathF.Abs(MathF.Min(a.Friction, b.Friction));
            var cap = normalImpulse * friction;
            var tangentImpulse = Math.Clamp(-relative.Dot(tangent) / Effective(tangent), -cap, cap);
            var shear = tangent * tangentImpulse; Apply(shear);
            return impulse + shear;
            float Effective(Vector2 axis)
            {
                var ca = Cross(ra, axis); var cb = Cross(rb, axis);
                return massA + massB + ca * ca * inertiaA + cb * cb * inertiaB;
            }
            void Apply(Vector2 value) { if (responseA) a.AddImpulse(-value, -Cross(ra, value)); if (responseB) b.AddImpulse(value, Cross(rb, value)); }
        }
        private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
    }
}
