using Electron2D;

public static partial class BackendChecks
{
    private readonly record struct Part(int Index, Vector2 Offset, float Inner, float Outer,
        Vector2 EdgeX = default, Vector2 EdgeY = default, float Inflation = 0, bool Capsule = false, bool Segment = false, Polygon? Polygon = null, bool Concave = false, Vector2 PlaneNormal = default, bool Directed = false, bool Slide = false, OneWayContactData? OneWay = null, Shape? Source = null, float Bias = 0)
    {
        internal bool Boundary => PlaneNormal != Vector2.Zero;
        internal bool Rectangle => Polygon is null && EdgeX != Vector2.Zero;
        internal bool Contour => Rectangle || Capsule || Segment || Polygon is not null || Directed;
        internal float LineLength => Segment || Capsule && Inflation == 0 ? 2 * EdgeY.Length() : 0;
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
        internal float Area => Boundary || Directed ? 0 : Polygon is { } polygon ? polygon.Area : Segment ? 0 : Capsule ? CapsuleArea : Rectangle ? 4 * MathF.Abs(CrossEdges(EdgeX, EdgeY)) : MathF.PI * (Outer * Outer - Inner * Inner);
        internal float Moment => Boundary || Directed ? 0 : Polygon is { } polygon ? polygon.Moment : Segment ? EdgeY.LengthSquared() / 3 : Capsule ? CapsuleMoment : Rectangle ? (EdgeX.LengthSquared() + EdgeY.LengthSquared()) / 3 : (Outer * Outer + Inner * Inner) / 2;
        internal float Thickness => Directed ? .5f : Boundary ? float.PositiveInfinity : Polygon is { } polygon ? polygon.Thickness : Segment ? .5f : Capsule ? Inflation > 0 ? Inflation : .5f : Rectangle ? MathF.Abs(CrossEdges(EdgeX, EdgeY)) / MathF.Max(EdgeX.Length(), EdgeY.Length()) : Outer - Inner;
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
            var required = 0;
            foreach (ref readonly var slot in slots)
                if (slot.Enabled) required += GeometryCount(slot.Shape.GetGeometryData());
            if (Parts.Length < required) Array.Resize(ref Parts, required);
            Shapes = 0;
            foreach (ref readonly var slot in slots)
            {
                if (!slot.Enabled) continue;
                var geometry = slot.Shape.GetGeometryData();
                for (var element = 0; element < GeometryCount(geometry); element++)
                {
                    var part = GeometryPart(geometry, slot.Transform, slot.Index, element) with { OneWay = slot.OneWay, Source = slot.Shape, Bias = slot.Shape.CustomSolverBias };
                    if (part.Polygon is { Vertices.Length: 0 }) continue;
                    if (part.Segment && part.LineLength <= .5f) part = part with { EdgeY = default, Outer = 0 };
                    Parts[Shapes++] = part;
                }
            }
            Parts.AsSpan(Shapes).Clear();
        }
        internal PhysicsMassProperties PartMass(float mass, float inertia, Vector2? center)
        {
            var weight = 0f; var weighted = Vector2.Zero;
            for (var i = 0; i < Shapes; i++)
            { var p = Parts[i]; weight += p.Area; weighted += p.Offset * p.Area; }
            var rods = weight == 0;
            if (rods)
                for (var i = 0; i < Shapes; i++)
                { var p = Parts[i]; weight += p.LineLength; weighted += p.Offset * p.LineLength; }
            Mass = mass; _center = center ?? (weight > 0 ? weighted / weight : Vector2.Zero);
            var automatic = 0f;
            if (weight > 0)
                for (var i = 0; i < Shapes; i++)
                {
                    var p = Parts[i]; var sample = rods ? p.LineLength : p.Area;
                    automatic += sample / weight * mass * (p.Moment + p.Offset.DistanceSquaredTo(_center));
                }
            Inertia = inertia > 0 ? inertia : automatic;
            return new(Mass, Inertia, _center);
        }
    }
    private sealed partial class Solver
    {
        internal static bool PartsOverlap(Body a, in Part first, Body b, in Part second)
        {
            if (first.Boundary || second.Boundary || first.Contour || second.Contour)
                return PartGap(first, new(a.Rotation, a.Position), second, new(b.Rotation, b.Position), out _, out _, out _) <= 0;
            var d = a.PartCenter(first).DistanceTo(b.PartCenter(second));
            return d <= first.Outer + second.Outer && d + second.Outer >= first.Inner && d + first.Outer >= second.Inner;
        }
        private static bool PartContact(Body a, in Part first, Body b, in Part second, out Vector2 normal, out float depth, out Vector2 point)
        {
            if (first.Boundary || second.Boundary || first.Contour || second.Contour)
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
            for (var i = 0; i < a.Shapes; i++) for (var j = 0; j < b.Shapes; j++) if (PartsOverlap(a, a.Parts[i], b, b.Parts[j])) return true;
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
