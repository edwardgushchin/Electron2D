using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver
    {
        private static void SolveContact(ref ContactConstraint contact)
        {
            var a = contact.A; var b = contact.B; var normal = contact.Normal; var point = contact.Point;
            if (!ContactActive(a, b)) return;
            var ra = point - a.Center; var rb = point - b.Center;
            var responseA = (a.Mask & b.Layer) != 0; var responseB = (b.Mask & a.Layer) != 0;
            var massA = responseA ? a.InvMass : 0; var massB = responseB ? b.InvMass : 0;
            var inertiaA = responseA ? a.InvInertia : 0; var inertiaB = responseB ? b.InvInertia : 0;
            var relative = b.PointVelocity(point) - a.PointVelocity(point);
            var speed = relative.Dot(normal);
            var inverse = Effective(normal); if (inverse <= 0) return;
            var previous = contact.NormalImpulse;
            contact.NormalImpulse = MathF.Max(0, previous + (contact.Bounce - speed) / inverse);
            var impulse = normal * (contact.NormalImpulse - previous);
            Apply(impulse);
            var tangent = new Vector2(-normal.Y, normal.X);
            relative = b.PointVelocity(point) - a.PointVelocity(point);
            var friction = MathF.Abs(MathF.Min(a.Friction, b.Friction));
            var cap = contact.NormalImpulse * friction; var tangentMass = Effective(tangent); if (tangentMass <= 0) return;
            var previousTangent = contact.TangentImpulse;
            contact.TangentImpulse = Math.Clamp(previousTangent - relative.Dot(tangent) / tangentMass, -cap, cap);
            var shear = tangent * (contact.TangentImpulse - previousTangent); Apply(shear);
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
