using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver : PhysicsServerExtension
    {
        private readonly List<PhysicsPointResult> _pointHits = [];
        private readonly List<PhysicsShapeResult> _shapeHits = [];
        private readonly List<PhysicsBackendShapeContact> _shapeContacts = [];
        protected override int ReadDebugContacts(Span<Vector2> output)
        {
            DebugReads++;
            if (BadDebugCount) return output.Length + 1;
            if (BadDebugPoint && !output.IsEmpty) { output[0] = new(float.NaN, 0); return 1; }
            return 0;
        }
        private static bool Eligible(Body b, uint mask, bool bodies, bool areas, ReadOnlySpan<RID> excluded) =>
            b.Shapes != 0 && (b.Layer & mask) != 0 && (b.IsArea ? areas : bodies) && !excluded.Contains(b.RID);
        private static bool Sweep(Vector2 from, Vector2 motion, Vector2 center, float radius, out float fraction)
        {
            var offset = from - center; var c = offset.LengthSquared() - radius * radius;
            if (c <= 0) { fraction = 0; return true; }
            var a = motion.LengthSquared(); var b = offset.Dot(motion); var d = b * b - a * c;
            if (a == 0 || d < 0) { fraction = 1; return false; }
            fraction = (-b - MathF.Sqrt(d)) / a; return fraction >= 0 && fraction <= 1;
        }
        protected override PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, ReadOnlySpan<RID> excluded,
            bool collideWithAreas, bool collideWithBodies, bool hitFromInside)
        {
            Body? best = null; var fraction = 1f; var motion = to - from;
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, mask, collideWithBodies, collideWithAreas, excluded) ||
                    !hitFromInside && Annulus.Contains(from, b.Position, b.Hole, b.Radius) || !Annulus.Sweep(from, motion, b.Position, b.Hole, b.Radius, out var f) || f > fraction) continue;
                best = b; fraction = f;
            }
            if (best is null) return null;
            var point = from + motion * fraction; return new(best.RID, 0, point, (point - best.Position).Normalized());
        }
        protected override List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            _pointHits.Clear();
            foreach (var b in Bodies.Values)
                if (Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded) &&
                    b.Canvas == parameters.CanvasInstanceID && Annulus.Contains(parameters.Position, b.Position, b.Hole, b.Radius)) _pointHits.Add(new(b.RID, 0));
            _pointHits.Sort(static (a, b) => a.ColliderRID.GetID().CompareTo(b.ColliderRID.GetID())); return _pointHits;
        }
        private static float Radius(RID rid) { using var shape = PhysicsServer.ShapeGetData(rid); return Annulus.Dimensions(shape.GetGeometryData()).Outer; }
        protected override List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            _shapeHits.Clear(); var radius = Radius(parameters.ShapeRID) + parameters.Margin;
            foreach (var b in Bodies.Values)
                if (Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded) &&
                    Annulus.Sweep(parameters.Transform.Origin, parameters.Motion, b.Position, MathF.Max(0, b.Hole - radius), b.Radius + radius, out _)) _shapeHits.Add(new(b.RID, 0));
            _shapeHits.Sort(static (a, b) => a.ColliderRID.GetID().CompareTo(b.ColliderRID.GetID())); return _shapeHits;
        }
        protected override (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            var fraction = 1f; var radius = Radius(parameters.ShapeRID) + parameters.Margin;
            foreach (var b in Bodies.Values)
                if (Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded) &&
                    Annulus.Sweep(parameters.Transform.Origin, parameters.Motion, b.Position, MathF.Max(0, b.Hole - radius), b.Radius + radius, out var f)) fraction = MathF.Min(fraction, f);
            return (fraction, fraction);
        }
        protected override List<PhysicsBackendShapeContact> CollectShapeContacts(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded, int limit)
        {
            _shapeContacts.Clear(); var radius = Radius(parameters.ShapeRID) + parameters.Margin;
            foreach (var b in Bodies.Values)
            {
                if (_shapeContacts.Count == limit) break;
                if (!Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded) ||
                    !Annulus.Sweep(parameters.Transform.Origin, parameters.Motion, b.Position, MathF.Max(0, b.Hole - radius), b.Radius + radius, out var f)) continue;
                var center = parameters.Transform.Origin + parameters.Motion * f; var normal = (center - b.Position).Normalized();
                _shapeContacts.Add(new(b.RID, 0, 0, center - normal * radius, b.Position + normal * b.Radius));
            }
            return _shapeContacts;
        }
        protected override PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            var hits = CollectShapeContacts(parameters, excluded, 1); if (hits.Count == 0) return null;
            var h = hits[0]; var b = Bodies[h.RID]; return new(h.RID, 0, h.ColliderPoint, (h.QueryPoint - b.Position).Normalized(), b.PointVelocity(h.ColliderPoint));
        }
        protected override bool CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second,
            Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count) =>
            Pair(firstPose.Origin, Radius(first), secondPose.Origin, Radius(second), firstMotion - secondMotion, contacts, out count);
        protected override bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion, Span<Vector2> contacts, out int count)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index)); var b = Bodies[body];
            return Pair(b.Position, b.Radius, pose.Origin, Radius(shape), -motion, contacts, out count);
        }
        private static bool Pair(Vector2 first, float firstRadius, Vector2 second, float secondRadius, Vector2 motion, Span<Vector2> contacts, out int count)
        {
            count = 0; if (!Sweep(first, motion, second, firstRadius + secondRadius, out var f)) return false;
            if (contacts.Length >= 2)
            {
                var center = first + motion * f; var normal = (second - center).Normalized();
                contacts[0] = center + normal * firstRadius; contacts[1] = second - normal * secondRadius; count = 1;
            }
            return true;
        }
        protected override bool TestBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision,
            ReadOnlySpan<RID> excludedBodies, ReadOnlySpan<ulong> excludedObjects, bool collideSeparationRay, PhysicsTestMotionResult result)
        {
            var local = Bodies[body]; Body? best = null; var fraction = 1f;
            foreach (var other in Bodies.Values)
                if (other.RID != body && Eligible(other, local.Mask, true, false, excludedBodies) &&
                    !Exceptions.Contains((body, other.RID)) && !Exceptions.Contains((other.RID, body)) &&
                    Annulus.Sweep(from.Origin, motion, other.Position, MathF.Max(0, other.Hole - local.Radius - margin), local.Radius + other.Radius + margin, out var f) && f <= fraction)
                { best = other; fraction = f; }
            if (best is null) { result.SetMotion(motion); return false; }
            var travel = motion * fraction; var normal = (from.Origin + travel - best.Position).Normalized(); var point = best.Position + normal * best.Radius;
            result.SetCollision(body, 0, best.RID, 0, point, normal, 0, best.PointVelocity(point), travel, motion - travel, fraction, fraction); return true;
        }
    }
}
