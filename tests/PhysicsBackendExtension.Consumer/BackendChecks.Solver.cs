using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver : PhysicsServerExtension
    {
        private readonly List<PhysicsPointResult> _pointHits = [];
        internal Action? PointHook;
        private readonly List<PhysicsShapeResult> _shapeHits = [];
        private readonly List<PhysicsBackendShapeContact> _shapeContacts = [];
        protected override int ReadDebugContacts(Span<Vector2> output)
        {
            Check(output.Length == DebugLimit, "Diagnostic reads use the previously prepared capacity");
            DebugReads++;
            DebugReadHook?.Invoke();
            if (NegativeDebugCount) return -1;
            if (BadDebugCount) return output.Length + 1;
            if (BadDebugPoint && !output.IsEmpty) { output[0] = new(float.NaN, 0); return 1; }
            DebugPoints.AsSpan(0, DebugPointCount).CopyTo(output);
            DebugLastCount = DebugPointCount;
            return DebugPointCount;
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
            Body? best = null; Part part = default; var fraction = 1f; var motion = to - from;
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, mask, collideWithBodies, collideWithAreas, excluded)) continue;
                for (var i = 0; i < b.Shapes; i++)
                {
                    var p = b.Parts[i]; var center = b.PartCenter(p);
                    if (!hitFromInside && Annulus.Contains(from, center, p.Inner, p.Outer) ||
                        !Annulus.Sweep(from, motion, center, p.Inner, p.Outer, out var f) || f > fraction) continue;
                    best = b; part = p; fraction = f;
                }
            }
            if (best is null) return null;
            var point = from + motion * fraction;
            return new(best.RID, part.Index, point, PartNormal(point, best.PartCenter(part), part));
        }
        private static Vector2 PartNormal(Vector2 point, Vector2 center, in Part part)
        {
            var delta = point - center; var normal = delta.Normalized();
            return part.Inner > 0 && delta.Length() < (part.Inner + part.Outer) / 2 ? -normal : normal;
        }
        protected override List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            PointHook?.Invoke(); _pointHits.Clear();
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded) || b.Canvas != parameters.CanvasInstanceID) continue;
                for (var i = 0; i < b.Shapes; i++)
                { var p = b.Parts[i]; if (Annulus.Contains(parameters.Position, b.PartCenter(p), p.Inner, p.Outer)) _pointHits.Add(new(b.RID, p.Index)); }
            }
            _pointHits.Sort(static (a, b) => a.ColliderRID == b.ColliderRID ? a.ShapeIndex.CompareTo(b.ShapeIndex) : a.ColliderRID.GetID().CompareTo(b.ColliderRID.GetID())); return _pointHits;
        }
        private float Radius(RID rid) => Annulus.Dimensions(GetShapeGeometry(rid)).Outer;
        protected override List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            _shapeHits.Clear(); var radius = Radius(parameters.ShapeRID) + parameters.Margin;
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded)) continue;
                for (var i = 0; i < b.Shapes; i++)
                { var p = b.Parts[i]; if (Annulus.Sweep(parameters.Transform.Origin, parameters.Motion, b.PartCenter(p), MathF.Max(0, p.Inner - radius), p.Outer + radius, out _)) _shapeHits.Add(new(b.RID, p.Index)); }
            }
            _shapeHits.Sort(static (a, b) => a.ColliderRID == b.ColliderRID ? a.ShapeIndex.CompareTo(b.ShapeIndex) : a.ColliderRID.GetID().CompareTo(b.ColliderRID.GetID())); return _shapeHits;
        }
        protected override (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            var fraction = 1f; var radius = Radius(parameters.ShapeRID) + parameters.Margin;
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded)) continue;
                for (var i = 0; i < b.Shapes; i++)
                { var p = b.Parts[i]; if (Annulus.Sweep(parameters.Transform.Origin, parameters.Motion, b.PartCenter(p), MathF.Max(0, p.Inner - radius), p.Outer + radius, out var f)) fraction = MathF.Min(fraction, f); }
            }
            return (fraction, fraction);
        }
        protected override List<PhysicsBackendShapeContact> CollectShapeContacts(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded, int limit)
        {
            _shapeContacts.Clear(); var radius = Radius(parameters.ShapeRID) + parameters.Margin;
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded)) continue;
                for (var i = 0; i < b.Shapes && _shapeContacts.Count < limit; i++)
                {
                    var p = b.Parts[i]; var at = b.PartCenter(p);
                    if (!Annulus.Sweep(parameters.Transform.Origin, parameters.Motion, at, MathF.Max(0, p.Inner - radius), p.Outer + radius, out var f)) continue;
                    var center = parameters.Transform.Origin + parameters.Motion * f; var normal = PartNormal(center, at, p);
                    _shapeContacts.Add(new(b.RID, p.Index, 0, center - normal * radius, at + normal * (center.DistanceTo(at) < p.Inner ? -p.Inner : p.Outer)));
                }
            }
            return _shapeContacts;
        }
        protected override PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            var hits = CollectShapeContacts(parameters, excluded, 1); if (hits.Count == 0) return null;
            var h = hits[0]; var b = Bodies[h.RID];
            for (var i = 0; i < b.Shapes; i++) if (b.Parts[i].Index == h.ShapeIndex)
                    return new(h.RID, h.ShapeIndex, h.ColliderPoint, PartNormal(h.QueryPoint, b.PartCenter(b.Parts[i]), b.Parts[i]), b.PointVelocity(h.ColliderPoint));
            throw new InvalidOperationException("A selected shape contact lost its logical slot.");
        }
        internal Action? PairHook;
        internal int PairQueries, PairFault;
        internal ShapeGeometry ReadGeometry(RID shape) => GetShapeGeometry(shape);
        protected override bool CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second,
            Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)
        {
            PairQueries++;
            var a = GetShapeGeometry(first); var b = GetShapeGeometry(second);
            bool hit;
            if (a.Type == PhysicsServer.ShapeType.Custom)
                hit = Annulus.Collide(a, firstPose, firstMotion, b, secondPose, secondMotion, contacts, out count);
            else if (b.Type == PhysicsServer.ShapeType.Custom)
            {
                hit = Annulus.Collide(b, secondPose, secondMotion, a, firstPose, firstMotion, contacts, out count);
                for (var i = 0; i < count * 2; i += 2) (contacts[i], contacts[i + 1]) = (contacts[i + 1], contacts[i]);
            }
            else hit = Pair(firstPose.Origin, a.GetCircleRadius(), firstMotion, secondPose.Origin, b.GetCircleRadius(), secondMotion, contacts, out count);
            PairHook?.Invoke();
            if (PairFault == 1) count = contacts.Length / 2 + 1;
            else if (PairFault == 2) count = -1;
            else if (PairFault == 3) { count = 1; hit = false; }
            else if (PairFault == 4 && !contacts.IsEmpty) { contacts[0] = new(float.NaN, 0); count = 1; }
            else if (PairFault == 5) throw new InvalidOperationException("Pair hook failure");
            return hit;
        }
        protected override bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion, Span<Vector2> contacts, out int count)
        {
            var b = Bodies[body];
            var local = PhysicsServer.BodyGetShapeTransform(body, index);
            return CollideShapes(PhysicsServer.BodyGetShape(body, index), new Transform(b.Rotation, b.Position) * local,
                Vector2.Zero, shape, pose, motion, contacts, out count);
        }
        private static bool Pair(Vector2 first, float firstRadius, Vector2 firstMotion, Vector2 second, float secondRadius,
            Vector2 secondMotion, Span<Vector2> contacts, out int count)
        {
            var endA = first + firstMotion; var endB = second + secondMotion;
            var a = first; var b = Geometry.GetClosestPointToSegment(a, second, endB); var distance = a.DistanceSquaredTo(b);
            Consider(endA, Geometry.GetClosestPointToSegment(endA, second, endB));
            Consider(Geometry.GetClosestPointToSegment(second, first, endA), second);
            Consider(Geometry.GetClosestPointToSegment(endB, first, endA), endB);
            if (Geometry.SegmentIntersectsSegment(first, endA, second, endB) is { } crossing) { a = b = crossing; distance = 0; }
            count = 0; if (distance > (firstRadius + secondRadius) * (firstRadius + secondRadius)) return false;
            if (contacts.Length >= 2)
            {
                var normal = a == b ? Vector2.Right : (b - a).Normalized();
                contacts[0] = a + normal * firstRadius; contacts[1] = b - normal * secondRadius; count = 1;
            }
            return true;
            void Consider(Vector2 x, Vector2 y) { var d = x.DistanceSquaredTo(y); if (d < distance) { a = x; b = y; distance = d; } }
        }
        internal Action<RID, PhysicsTestMotionResult>? MotionResultHook;
        protected override bool TestBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision,
            ReadOnlySpan<RID> excludedBodies, ReadOnlySpan<ulong> excludedObjects, bool collideSeparationRay, PhysicsTestMotionResult result)
        {
            var local = Bodies[body]; Body? best = null; Part source = default, target = default; var fraction = 1f;
            MotionHook?.Invoke(body);
            foreach (var other in Bodies.Values)
            {
                if (other.RID == body || !Eligible(other, local.Mask, true, false, []) || IsBodyExcludedFromMotionTest(other.RID) ||
                    other.ObjectID != 0 && IsObjectExcludedFromMotionTest(other.ObjectID) ||
                    Exceptions.Contains((body, other.RID)) || Exceptions.Contains((other.RID, body))) continue;
                for (var i = 0; i < local.Shapes; i++) for (var j = 0; j < other.Shapes; j++)
                    {
                        var a = local.Parts[i]; var b = other.Parts[j]; var start = from.Origin + a.Offset.Rotated(from.Rotation);
                        if (!Annulus.Sweep(start, motion, other.PartCenter(b), MathF.Max(0, b.Inner - a.Outer - margin), a.Outer + b.Outer + margin, out var f) || f > fraction) continue;
                        best = other; source = a; target = b; fraction = f;
                    }
            }
            if (best is null) { result.SetMotion(motion); return false; }
            var travel = motion * fraction; var center = from.Origin + source.Offset.Rotated(from.Rotation) + travel; var at = best.PartCenter(target);
            var normal = PartNormal(center, at, target); var point = at + normal * (center.DistanceTo(at) < target.Inner ? -target.Inner : target.Outer);
            result.SetCollision(body, source.Index, best.RID, target.Index, point, normal, 0, best.PointVelocity(point), travel, motion - travel, fraction, fraction);
            MotionResultHook?.Invoke(body, result); return true;
        }
    }
}
