using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver
    {
        private static Vector2 SolveContact(Body a, Body b, Vector2 normal, Vector2 point)
        {
            var ra = point - a.Center; var rb = point - b.Center;
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
                return a.InvMass + b.InvMass + ca * ca * a.InvInertia + cb * cb * b.InvInertia;
            }
            void Apply(Vector2 value) { a.AddImpulse(-value, -Cross(ra, value)); b.AddImpulse(value, Cross(rb, value)); }
        }
        private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
    }
}
