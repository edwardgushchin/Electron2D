using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver : PhysicsServerExtension
    {
        private readonly List<PhysicsPointResult> _pointHits = [];
        internal Action? PointHook;
        internal Action<int>? SpaceQueryHook;
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
            SpaceQueryHook?.Invoke(0);
            Body? best = null; Part part = default; var fraction = 1f; var motion = to - from;
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, mask, collideWithBodies, collideWithAreas, excluded)) continue;
                for (var i = 0; i < b.Shapes; i++)
                {
                    var p = b.Parts[i]; var center = b.PartCenter(p);
                    if (!hitFromInside && LogicalContains(b, p.Index, from) ||
                        !RayPart(p, b.Pose, from, motion, out var f, out _) || f > fraction ||
                        f == fraction && best is not null && CompareQueryIdentity(b.RID, p.Index, best.RID, part.Index) >= 0) continue;
                    best = b; part = p; fraction = f;
                }
            }
            if (best is null) return null;
            var point = from + motion * fraction;
            RayPart(part, new(best.Rotation, best.Position), from, motion, out _, out var hitNormal);
            return new(best.RID, part.Index, point, hitNormal);
        }
        private static int CompareQueryIdentity(RID first, int firstIndex, RID second, int secondIndex) =>
            first == second ? firstIndex.CompareTo(secondIndex) : first.GetID().CompareTo(second.GetID());
        private static bool LogicalContains(Body body, int index, Vector2 point)
        {
            for (var i = 0; i < body.Shapes; i++)
                if (body.Parts[i].Index == index && ContainsPart(body.Parts[i], body.Pose, point)) return true;
            return false;
        }
        private bool InitiallyOverlapping(Body body, int index, PhysicsShapeQueryParameters parameters, ShapeGeometry geometry)
        {
            for (var i = 0; i < body.Shapes; i++)
            {
                if (body.Parts[i].Index != index) continue;
                for (var piece = 0; piece < GeometryCount(geometry); piece++)
                    if (PartGap(QueryPart(parameters, geometry, piece), Transform.Identity, body.Parts[i], body.Pose, out _, out _, out _) <= 0) return true;
            }
            return false;
        }
        internal static Vector2 PartNormal(Vector2 point, Vector2 center, in Part part)
        {
            var delta = point - center; var normal = delta.Normalized();
            return part.Inner > 0 && delta.Length() < (part.Inner + part.Outer) / 2 ? -normal : normal;
        }
        protected override List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            SpaceQueryHook?.Invoke(1);
            PointHook?.Invoke(); _pointHits.Clear();
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded) || b.Canvas != parameters.CanvasInstanceID) continue;
                for (var i = 0; i < b.Shapes; i++)
                { var p = b.Parts[i]; if (ContainsPart(p, new(b.Rotation, b.Position), parameters.Position)) _pointHits.Add(new(b.RID, p.Index)); }
            }
            _pointHits.Sort(static (a, b) => a.ColliderRID == b.ColliderRID ? a.ShapeIndex.CompareTo(b.ShapeIndex) : a.ColliderRID.GetID().CompareTo(b.ColliderRID.GetID())); return _pointHits;
        }
        private Part QueryPart(PhysicsShapeQueryParameters parameters, ShapeGeometry geometry, int element)
        {
            var part = GeometryPart(geometry, parameters.Transform, 0, element);
            return ExpandPart(part, parameters.Margin);
        }
        private static bool QuerySweep(in Part query, Vector2 motion, in Part target, Transform pose, out float fraction)
        {
            if (query.Boundary || target.Boundary || query.Contour || target.Contour) return SweepParts(query, Transform.Identity, motion, target, pose, out fraction);
            return Annulus.Sweep(query.Offset, motion, PartAt(target, pose), MathF.Max(0, target.Inner - query.Outer), target.Outer + query.Outer, out fraction);
        }
        protected override List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            SpaceQueryHook?.Invoke(2);
            _shapeHits.Clear(); var geometry = GetShapeGeometry(parameters.ShapeRID);
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded)) continue;
                for (var i = 0; i < b.Shapes; i++) for (var piece = 0; piece < GeometryCount(geometry); piece++)
                    {
                        var p = b.Parts[i]; var query = QueryPart(parameters, geometry, piece);
                        if (QuerySweep(query, parameters.Motion, p, b.Pose, out _)) _shapeHits.Add(new(b.RID, p.Index));
                    }
            }
            _shapeHits.Sort(static (a, b) => a.ColliderRID == b.ColliderRID ? a.ShapeIndex.CompareTo(b.ShapeIndex) : a.ColliderRID.GetID().CompareTo(b.ColliderRID.GetID()));
            var unique = 0;
            for (var i = 0; i < _shapeHits.Count; i++)
            {
                var hit = _shapeHits[i];
                if (unique != 0 && _shapeHits[unique - 1].ColliderRID == hit.ColliderRID && _shapeHits[unique - 1].ShapeIndex == hit.ShapeIndex) continue;
                _shapeHits[unique++] = hit;
            }
            _shapeHits.RemoveRange(unique, _shapeHits.Count - unique); return _shapeHits;
        }
        protected override (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            SpaceQueryHook?.Invoke(3);
            var fraction = 1f; var safeFraction = 1f; var geometry = GetShapeGeometry(parameters.ShapeRID);
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded)) continue;
                for (var i = 0; i < b.Shapes; i++) for (var piece = 0; piece < GeometryCount(geometry); piece++)
                    {
                        var p = b.Parts[i]; var query = QueryPart(parameters, geometry, piece);
                        if (InitiallyOverlapping(b, p.Index, parameters, geometry)) continue;
                        if (query.Directed || p.Directed)
                        {
                            if (DirectedSweepRange(query, Transform.Identity, parameters.Motion, p, b.Pose, out var low, out var high) && high <= fraction)
                            { safeFraction = low; fraction = high; }
                        }
                        else if (QuerySweep(query, parameters.Motion, p, b.Pose, out var f) && f <= fraction) { safeFraction = fraction = f; }
                    }
            }
            return (safeFraction, fraction);
        }
        private readonly record struct QueryContact(PhysicsBackendShapeContact Contact, Part Query, Part Collider);
        private readonly List<QueryContact> _queryContacts = [];
        private void ClearQueryResults() { _pointHits.Clear(); _shapeHits.Clear(); _shapeContacts.Clear(); _queryContacts.Clear(); }
        protected override List<PhysicsBackendShapeContact> CollectShapeContacts(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded, int limit)
        {
            SpaceQueryHook?.Invoke(4);
            _shapeContacts.Clear(); _queryContacts.Clear(); var geometry = GetShapeGeometry(parameters.ShapeRID);
            foreach (var b in Bodies.Values)
            {
                if (!Eligible(b, parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, excluded)) continue;
                for (var i = 0; i < b.Shapes; i++)
                    for (var piece = 0; piece < GeometryCount(geometry); piece++)
                    {
                        var p = b.Parts[i]; var query = QueryPart(parameters, geometry, piece);
                        if (!QuerySweep(query, parameters.Motion, p, b.Pose, out var f)) continue;
                        Vector2 firstPoint, secondPoint;
                        if (query.Directed || p.Directed)
                        {
                            if (!DirectedContact(query, Transform.Identity, parameters.Motion, p, b.Pose, Vector2.Zero,
                                out _, out firstPoint, out secondPoint, out _)) continue;
                        }
                        else if (query.Boundary || p.Boundary || query.Contour || p.Contour)
                        {
                            var pose = Transform.Identity; pose.Origin = parameters.Motion * f;
                            PartGap(query, pose, p, b.Pose, out _, out firstPoint, out secondPoint);
                        }
                        else
                        {
                            var center = query.Offset + parameters.Motion * f; var at = b.PartCenter(p); var normal = PartNormal(center, at, p);
                            firstPoint = center - normal * query.Outer; secondPoint = at + normal * (center.DistanceTo(at) < p.Inner ? -p.Inner : p.Outer);
                        }
                        _queryContacts.Add(new(new(b.RID, p.Index, piece, firstPoint, secondPoint), query, p with { Source = null }));
                    }
            }
            _queryContacts.Sort(static (a, b) =>
            {
                var order = CompareQueryIdentity(a.Contact.RID, a.Contact.ShapeIndex, b.Contact.RID, b.Contact.ShapeIndex);
                return order != 0 ? order : a.Contact.Piece.CompareTo(b.Contact.Piece);
            });
            for (var i = 0; i < Math.Min(limit, _queryContacts.Count); i++) _shapeContacts.Add(_queryContacts[i].Contact);
            return _shapeContacts;
        }
        protected override PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)
        {
            SpaceQueryHook?.Invoke(5);
            var hits = CollectShapeContacts(parameters, excluded, int.MaxValue); if (hits.Count == 0) return null;
            PhysicsRestInfo? result = null; var deepest = float.NegativeInfinity;
            for (var i = 0; i < hits.Count; i++)
            {
                var h = hits[i]; var body = Bodies[h.RID]; var query = _queryContacts[i].Query; var part = _queryContacts[i].Collider;
                var normal = PartNormal(h.QueryPoint, body.PartCenter(part), part); var depth = h.QueryPoint.DistanceTo(h.ColliderPoint);
                if (query.Directed || part.Directed)
                {
                    DirectedContact(query, Transform.Identity, parameters.Motion, part, body.Pose, Vector2.Zero,
                        out var toward, out _, out _, out depth); normal = -toward;
                }
                else if (query.Boundary || part.Boundary || query.Contour || part.Contour)
                {
                    QuerySweep(query, parameters.Motion, part, body.Pose, out var fraction);
                    var pose = Transform.Identity; pose.Origin = parameters.Motion * fraction;
                    depth = -PartGap(query, pose, part, body.Pose, out var inward, out _, out _); normal = -inward;
                }
                if (depth > deepest)
                { deepest = depth; result = new(h.RID, h.ShapeIndex, h.ColliderPoint, normal, body.PointVelocity(h.ColliderPoint)); }
            }
            return result;
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
            if (a.Type == PhysicsServer.ShapeType.WorldBoundary || b.Type == PhysicsServer.ShapeType.WorldBoundary)
                hit = CollideBoundaryGeometry(a, firstPose, firstMotion, b, secondPose, secondMotion, contacts, out count);
            else if (a.Type == PhysicsServer.ShapeType.SeparationRay || b.Type == PhysicsServer.ShapeType.SeparationRay)
                hit = CollideDirectedGeometry(a, firstPose, firstMotion, b, secondPose, secondMotion, contacts, out count);
            else if (a.Type == PhysicsServer.ShapeType.ConcavePolygon || b.Type == PhysicsServer.ShapeType.ConcavePolygon)
                hit = CollideConcaveGeometry(a, firstPose, firstMotion, b, secondPose, secondMotion, contacts, out count);
            else if (a.Type is PhysicsServer.ShapeType.Rectangle or PhysicsServer.ShapeType.Capsule or PhysicsServer.ShapeType.Segment or PhysicsServer.ShapeType.ConvexPolygon || b.Type is PhysicsServer.ShapeType.Rectangle or PhysicsServer.ShapeType.Capsule or PhysicsServer.ShapeType.Segment or PhysicsServer.ShapeType.ConvexPolygon)
                hit = CollideContourGeometry(a, firstPose, firstMotion, b, secondPose, secondMotion, contacts, out count);
            else if (a.Type == PhysicsServer.ShapeType.Custom)
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
            return TestGeometryBodyMotion(body, from, motion, margin, recoveryAsCollision, collideSeparationRay, result);
        }
    }
}
