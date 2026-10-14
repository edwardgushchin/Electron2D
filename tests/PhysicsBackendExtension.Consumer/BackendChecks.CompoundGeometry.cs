using Electron2D;

public static partial class BackendChecks
{
    private readonly record struct Part(int Index, Vector2 Offset, float Inner, float Outer,
        Vector2 EdgeX = default, Vector2 EdgeY = default, float Inflation = 0, bool Capsule = false)
    {
        internal bool Rectangle => EdgeX != Vector2.Zero;
        internal bool Contour => Rectangle || Capsule;
        internal float CapsuleArea => 4 * Inflation * EdgeY.Length() + MathF.PI * Inflation * Inflation;
        internal float CapsuleMoment
        {
            get
            {
                var r = Inflation; var h = EdgeY.Length();
                var integral = 4 * r * h * (r * r + h * h) / 3 +
                    MathF.PI * r * r * (r * r / 2 + h * h) + 8 * h * r * r * r / 3;
                return CapsuleArea > 0 ? integral / CapsuleArea : h * h / 3;
            }
        }
        internal float Area => Capsule ? CapsuleArea : Rectangle ? 4 * MathF.Abs(CrossEdges(EdgeX, EdgeY)) : MathF.PI * (Outer * Outer - Inner * Inner);
        internal float Moment => Capsule ? CapsuleMoment : Rectangle ? (EdgeX.LengthSquared() + EdgeY.LengthSquared()) / 3 : (Outer * Outer + Inner * Inner) / 2;
        internal float Thickness => Capsule ? MathF.Max(Inflation, .001f) : Rectangle ? MathF.Abs(CrossEdges(EdgeX, EdgeY)) / MathF.Max(EdgeX.Length(), EdgeY.Length()) : Outer - Inner;
    }
    private sealed partial class Body
    {
        internal Part[] Parts = [];
        internal Vector2 PartCenter(in Part part) => Position + part.Offset.Rotated(Rotation);
        internal float MinimumThickness
        {
            get { var minimum = float.PositiveInfinity; for (var i = 0; i < Shapes; i++) minimum = MathF.Min(minimum, Parts[i].Thickness); return minimum; }
        }
        private void RebuildParts(ReadOnlySpan<PhysicsColliderShape> slots)
        {
            if (Parts.Length < slots.Length) Array.Resize(ref Parts, slots.Length);
            Shapes = 0;
            foreach (ref readonly var slot in slots)
            {
                if (!slot.Enabled) continue;
                Parts[Shapes++] = GeometryPart(slot.Shape.GetGeometryData(), slot.Transform, slot.Index);
            }
            Parts.AsSpan(Shapes).Clear();
        }
        private PhysicsMassProperties PartMass(float mass, float inertia, Vector2? center)
        {
            var area = 0f; var weighted = Vector2.Zero;
            for (var i = 0; i < Shapes; i++)
            { var p = Parts[i]; var weight = p.Area; area += weight; weighted += p.Offset * weight; }
            Mass = mass; _center = center ?? (area > 0 ? weighted / area : Vector2.Zero);
            var automatic = 0f;
            if (area > 0)
                for (var i = 0; i < Shapes; i++)
                {
                    var p = Parts[i];
                    automatic += p.Area / area * mass * (p.Moment + p.Offset.DistanceSquaredTo(_center));
                }
            Inertia = inertia > 0 ? inertia : MathF.Max(.001f, automatic);
            return new(Mass, Inertia, _center);
        }
    }
    private sealed partial class Solver
    {
        private static bool PartOverlap(Body a, in Part first, Body b, in Part second)
        {
            if (first.Contour || second.Contour)
                return PartGap(first, new(a.Rotation, a.Position), second, new(b.Rotation, b.Position), out _, out _, out _) <= 0;
            var d = a.PartCenter(first).DistanceTo(b.PartCenter(second));
            return d <= first.Outer + second.Outer && d + second.Outer >= first.Inner && d + first.Outer >= second.Inner;
        }
        private static bool PartContact(Body a, in Part first, Body b, in Part second, out Vector2 normal, out float depth, out Vector2 point)
        {
            if (first.Contour || second.Contour)
            {
                var gap = PartGap(first, new(a.Rotation, a.Position), second, new(b.Rotation, b.Position), out normal, out var firstPoint, out var secondPoint);
                depth = MathF.Max(0, -gap); point = (firstPoint + secondPoint) / 2; return gap <= 0;
            }
            if (first.Inner > 0 && second.Inner > 0) throw new NotSupportedException("The fixture exercises custom annulus/circle contacts.");
            if (second.Inner > 0)
            {
                var hit = Annulus.Contact(b.PartCenter(second), second.Inner, second.Outer, a.PartCenter(first), first.Outer, out normal, out depth, out point);
                normal = -normal; return hit;
            }
            return Annulus.Contact(a.PartCenter(first), first.Inner, first.Outer, b.PartCenter(second), second.Outer, out normal, out depth, out point);
        }
        private static bool Overlap(Body a, Body b)
        {
            for (var i = 0; i < a.Shapes; i++) for (var j = 0; j < b.Shapes; j++) if (PartOverlap(a, a.Parts[i], b, b.Parts[j])) return true;
            return false;
        }
        private bool Contact(Body a, Body b, out Vector2 normal, out float depth, out Vector2 point)
        {
            for (var i = 0; i < a.Shapes; i++) for (var j = 0; j < b.Shapes; j++)
                    if (AllowsContact(a.RID, a.Parts[i].Index, b.RID, b.Parts[j].Index) && PartContact(a, a.Parts[i], b, b.Parts[j], out normal, out depth, out point)) return true;
            normal = default; depth = 0; point = default; return false;
        }
        private bool AllowsContact(RID a, int ai, RID b, int bi)
        {
            foreach (var pair in OneWays)
                if (pair.A == a && pair.ShapeA == ai && pair.B == b && pair.ShapeB == bi ||
                    pair.A == b && pair.ShapeA == bi && pair.B == a && pair.ShapeB == ai) return pair.Allowed;
            return true;
        }
    }
}
