using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal bool TryGetBodyPointMotion(RID rid, Vector2 point, out Vector2 velocity, out uint layer)
    {
        // ponytail: Scan registered bodies for platform velocity; index by RID if large-world profiling needs it.
        EnsureQueryAccess();
        if (GPUStore is not null)
        {
            if (_gpuRIDColliders.TryGetValue((ulong)rid.GetID(), out var backend) && !backend.GPUSensor)
            {
                velocity = backend.GetPointVelocity(point - backend.GetPose().Position);
                var owners = PhysicsServer.Service.BodyRuntime(rid).Owners;
                layer = owners.Scene?.EffectiveCollisionLayer ?? owners.Server!.CollisionLayer; return true;
            }
            velocity = default; layer = 0; return false;
        }
        for (var index = 0; index < _bodies.Count; index++)
        {
            var body = _bodies[index];
            if (body.GetRID() != rid) continue;
            var backendVelocity = b2Body_GetWorldPointVelocity(body.BackendID, PhysicsShapeBackend.ToBackend(point));
            velocity = ToScene(backendVelocity);
            layer = body.EffectiveCollisionLayer;
            return true;
        }
        for (var index = 0; index < _serverColliders.Count; index++)
        {
            var body = _serverColliders[index];
            if (body.IsArea || body.RID != rid) continue;
            var backendVelocity = b2Body_GetWorldPointVelocity(body.BackendID, PhysicsShapeBackend.ToBackend(point));
            velocity = ToScene(backendVelocity);
            layer = body.CollisionLayer;
            return true;
        }
        velocity = default;
        layer = 0;
        return false;
    }

    private readonly List<MotionCandidate> _motionCandidates = [];
    private readonly record struct MotionCandidate(B2ShapeId ShapeID, PhysicsFixtureTag Tag, float Priority);

    internal MotionResultData TestBodyMotion(RID ownerRID, IReadOnlyList<B2ShapeId> ownShapes,
        Transform from, Vector2 motion, float margin, bool recoveryAsCollision,
        RID[] excludedBodies, ulong[] excludedObjects, bool collideSeparationRay = false)
    {
        PrepareForQuery();
        if (GPUStore is not null) return GPUTestMotion(ownerRID, from, motion, margin, recoveryAsCollision, excludedBodies, excludedObjects, collideSeparationRay);
        _motionCandidates.Clear();
        foreach (var other in _bodies)
            if (other.GetRID() != ownerRID)
                AddMotionCandidates(other.BackendShapes, other.Backend.CollisionPriority, ownerRID, ownShapes, excludedBodies, excludedObjects);
        foreach (var other in _serverColliders)
            if (!other.IsArea && other.RID != ownerRID)
                AddMotionCandidates(other.BackendShapes, other.Backend.CollisionPriority, ownerRID, ownShapes, excludedBodies, excludedObjects);

        var requested = PhysicsShapeBackend.ToBackend(motion);
        var fromTransform = new B2Transform(PhysicsShapeBackend.ToBackend(from.Origin), b2MakeRot(from.Rotation));
        var recovery = new B2Vec2(0, 0);
        var recoveryHit = default(MotionContact);
        var hasRecoveryHit = false;
        var world = b2GetWorldFromId(_worldID);
        var queryMargin = MathF.Max(margin * MetersPerUnit, 0.0001f * MetersPerUnit);
        Span<System.Numerics.Vector4> recoveryPlanes = stackalloc System.Numerics.Vector4[32];

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var bestDepth = 0f;
            var bestContact = default(MotionContact);
            var planeCount = 0;
            for (var ownIndex = 0; ownIndex < ownShapes.Count; ownIndex++)
            {
                var ownID = ownShapes[ownIndex];
                var ownTag = b2Shape_GetUserData(ownID).GetRef<PhysicsFixtureTag>();
                if (ownTag is null || ownTag.Compound is not null && ownTag.CompoundPiece != 0) continue;
                var ray = ownTag.SeparationRay;
                var query = ray is { } data ? PhysicsSeparationRay.WorldProxy(data, fromTransform, recovery) :
                    WorldProxy(b2MakeShapeDistanceProxy(b2GetShape(world, ownID)), fromTransform, recovery);
                if (ray is null) query.radius += queryMargin;
                foreach (var candidate in _motionCandidates)
                {
                    if (candidate.Tag.Compound is not null && candidate.Tag.CompoundPiece != 0) continue;
                    var other = b2MakeShapeDistanceProxy(b2GetShape(world, candidate.ShapeID));
                    var otherTransform = b2Body_GetTransform(b2Shape_GetBody(candidate.ShapeID));
                    B2Manifold manifold;
                    if (ray is not null || candidate.Tag.SeparationRay is not null)
                        manifold = MotionRayContact(query, ownTag, fromTransform, recovery, other, candidate.Tag, otherTransform, default, queryMargin);
                    else
                    {
                        if (ownTag.Compound is null && candidate.Tag.Compound is null && !PhysicsDirectSpaceState.Overlaps(query, other, otherTransform)) continue;
                        manifold = MotionManifold(query, ownTag, fromTransform, recovery, other, candidate.Tag, otherTransform);
                    }
                    if (manifold.pointCount == 0) continue;
                    for (var index = 0; index < manifold.pointCount; index++)
                    {
                        var point = manifold.points[index];
                        var depth = -point.separation;
                        var normal = -manifold.normal;
                        if (depth <= 0 || !AcceptOneWay(candidate, normal, depth, queryMargin)) continue;
                        RetainRecoveryPlane(recoveryPlanes, ref planeCount, new(normal.X, normal.Y, depth, candidate.Priority));
                        if (depth <= bestDepth) continue;
                        bestDepth = depth;
                        var colliderPoint = point.point + manifold.normal * (point.separation * 0.5f);
                        bestContact = new(candidate, ownTag.ShapeIndex, colliderPoint, normal, depth);
                    }
                }
            }
            if (bestDepth <= 0) break;
            recoveryHit = bestContact;
            hasRecoveryHit = true;
            double totalPriority = 0;
            for (var i = 0; i < planeCount; i++) totalPriority += recoveryPlanes[i].W;
            var normalization = totalPriority < 0.00001f ? 1 : planeCount / totalPriority;
            var correction = new B2Vec2(0, 0);
            for (var i = 0; i < planeCount; i++)
            {
                var plane = recoveryPlanes[i]; var normal = new B2Vec2(plane.X, plane.Y);
                var depth = plane.Z - b2Dot(normal, correction) - queryMargin * 0.05f;
                if (depth > 0.00001f * MetersPerUnit) correction += normal * (depth * 0.4f * (float)(plane.W * normalization));
            }
            if (correction.X == 0 && correction.Y == 0) break;
            recovery += correction;
        }

        var safe = 1f;
        var unsafeFraction = 1f;
        var motionHit = default(MotionContact);
        var hasMotionHit = false;
        if (requested.X != 0 || requested.Y != 0)
        {
            for (var ownIndex = 0; ownIndex < ownShapes.Count; ownIndex++)
            {
                var ownID = ownShapes[ownIndex];
                var ownTag = b2Shape_GetUserData(ownID).GetRef<PhysicsFixtureTag>();
                if (ownTag is null) continue;
                var ray = ownTag.SeparationRay;
                if (ray is { SlideOnSlope: false } && !collideSeparationRay) continue;
                var query = ray is { } data ? PhysicsSeparationRay.WorldProxy(data, fromTransform, recovery) :
                    WorldProxy(b2MakeShapeDistanceProxy(b2GetShape(world, ownID)), fromTransform, recovery);
                foreach (var candidate in _motionCandidates)
                {
                    var other = b2MakeShapeDistanceProxy(b2GetShape(world, candidate.ShapeID));
                    var otherTransform = b2Body_GetTransform(b2Shape_GetBody(candidate.ShapeID));
                    if (!AcceptOneWayMotion(candidate, requested, otherTransform)) continue;
                    if (ray is not null || candidate.Tag.SeparationRay is not null)
                    {
                        if (ownTag.Compound is not null && ownTag.CompoundPiece != 0 ||
                            candidate.Tag.Compound is not null && candidate.Tag.CompoundPiece != 0) continue;
                        var full = MotionRayContact(query, ownTag, fromTransform, recovery, other, candidate.Tag, otherTransform, requested * safe, 0);
                        if (full.pointCount == 0) continue;
                        var initial = MotionRayContact(query, ownTag, fromTransform, recovery, other, candidate.Tag, otherTransform, default, 0);
                        if (initial.pointCount != 0 && -initial.points[0].separation <= 0.1f * B2_LINEAR_SLOP &&
                            b2Dot(requested, -initial.normal) >= -1e-6f) continue;
                        var lowRay = 0f;
                        var highRay = initial.pointCount == 0 ? safe : 0f;
                        if (initial.pointCount == 0)
                            for (var step = 0; step < 8; step++)
                            {
                                var middle = (lowRay + highRay) * 0.5f;
                                var partial = MotionRayContact(query, ownTag, fromTransform, recovery, other, candidate.Tag, otherTransform, requested * middle, 0);
                                if (partial.pointCount != 0) highRay = middle;
                                else lowRay = middle;
                            }
                        if (lowRay >= safe && hasMotionHit) continue;
                        var rayImpact = query;
                        for (var pointIndex = 0; pointIndex < rayImpact.count; pointIndex++)
                            rayImpact.points[pointIndex] += requested * highRay;
                        var rayContact = MotionRayContact(rayImpact, ownTag, fromTransform, recovery + requested * highRay, other, candidate.Tag, otherTransform, default, queryMargin);
                        if (rayContact.pointCount == 0) rayContact = full;
                        var point = rayContact.points[0];
                        var normal = -rayContact.normal;
                        var depth = MathF.Max(0, -point.separation);
                        if (!AcceptOneWay(candidate, normal, depth, queryMargin)) continue;
                        safe = lowRay;
                        unsafeFraction = highRay;
                        motionHit = new(candidate, ownTag.ShapeIndex,
                            point.point + rayContact.normal * (point.separation * 0.5f), normal, depth);
                        hasMotionHit = true;
                        continue;
                    }
                    if (PhysicsDirectSpaceState.Overlaps(query, other, otherTransform))
                    {
                        var stuck = MotionManifold(query, ownTag, fromTransform, recovery, other, candidate.Tag, otherTransform);
                        var depth = stuck.pointCount == 0 ? 0 : -stuck.points[0].separation;
                        var normal = -stuck.normal;
                        if (stuck.pointCount != 0 &&
                            (depth > 0.1f * B2_LINEAR_SLOP || b2Dot(requested, normal) < -1e-6f) &&
                            AcceptOneWay(candidate, normal, depth, queryMargin))
                        {
                            safe = unsafeFraction = 0;
                            motionHit = new(candidate, ownTag.ShapeIndex,
                                stuck.points[0].point + stuck.normal * (stuck.points[0].separation * 0.5f),
                                normal, MathF.Max(0, depth));
                            hasMotionHit = true;
                        }
                        continue;
                    }
                    var cast = PhysicsDirectSpaceState.Cast(query, other, otherTransform, requested, safe);
                    if (!cast.hit) continue;
                    var low = 0f;
                    var high = safe;
                    for (var step = 0; step < 8; step++)
                    {
                        var middle = (low + high) * 0.5f;
                        if (PhysicsDirectSpaceState.Cast(query, other, otherTransform, requested, middle).hit)
                            high = middle;
                        else low = middle;
                    }
                    if (low >= safe) continue;
                    safe = low;
                    unsafeFraction = high;
                    var impact = WorldProxy(b2MakeShapeDistanceProxy(b2GetShape(world, ownID)), fromTransform,
                        recovery + requested * MathF.Min(1f, high + 2f * B2_LINEAR_SLOP / b2Length(requested)));
                    var manifold = MotionManifold(impact, ownTag, fromTransform,
                        recovery + requested * MathF.Min(1f, high + 2f * B2_LINEAR_SLOP / b2Length(requested)), other, candidate.Tag, otherTransform);
                    if (manifold.pointCount != 0)
                    {
                        var point = manifold.points[0];
                        motionHit = new(candidate, ownTag.ShapeIndex,
                            point.point + manifold.normal * (point.separation * 0.5f),
                            -manifold.normal, MathF.Max(0, -point.separation));
                    }
                    else motionHit = new(candidate, ownTag.ShapeIndex, cast.point, cast.normal, 0);
                    hasMotionHit = true;
                }
            }
        }

        var contact = hasMotionHit ? motionHit : recoveryHit;
        var collided = hasMotionHit || recoveryAsCollision && hasRecoveryHit;
        var travel = ToScene(recovery + requested * safe);
        var remainder = motion * (1f - safe);
        if (!collided)
            return new(ownerRID, default, 0, 0, 0, default, default, 0, default,
                travel, remainder, safe, unsafeFraction, false);

        var rid = contact.Candidate.Tag.ColliderRID;
        var identity = contact.Candidate.Tag.ObjectIdentity;
        var candidateBody = b2Shape_GetBody(contact.Candidate.ShapeID);
        var velocity = b2Body_GetWorldPointVelocity(candidateBody, contact.Point);
        return new(ownerRID, rid, identity.ID, contact.LocalShape,
            contact.Candidate.Tag.ShapeIndex, ToScene(contact.Point),
            new(contact.Normal.X, contact.Normal.Y), contact.Depth * UnitsPerMeter,
            ToScene(velocity), travel, remainder, safe, unsafeFraction, true, identity);
    }

    private static B2Manifold MotionRayContact(in B2ShapeProxy query, PhysicsFixtureTag own, B2Transform from, B2Vec2 recovery,
        in B2ShapeProxy other, PhysicsFixtureTag target, B2Transform otherPose, B2Vec2 motion, float margin)
    {
        if (own.SeparationRay is not null && target.Compound is { } targetContour &&
            PhysicsShapeCollision.FullMotionRegionContains(targetContour.Points, ScenePose(otherPose, default) * targetContour.LocalPose,
                Vector2.Zero, other.radius, query.points[0])) return default;
        if (target.SeparationRay is { } ray && own.Compound is { } ownContour &&
            PhysicsShapeCollision.FullMotionRegionContains(ownContour.Points, ScenePose(from, recovery) * ownContour.LocalPose,
                ToScene(motion), query.radius, b2TransformPoint(otherPose, ray.From))) return default;
        var compound = own.SeparationRay is not null ? target.Compound : target.SeparationRay is not null ? own.Compound : null;
        if (compound is not null)
        {
            if (!compound.Source.TryGetTarget(out var source)) throw new ObjectDisposedException(nameof(ConvexPolygonShape));
            var pose = (own.SeparationRay is not null ? ScenePose(otherPose, default) : ScenePose(from, recovery)) * compound.LocalPose;
            var transform = new B2Transform(PhysicsShapeBackend.ToBackend(pose.Origin), new B2Rot(pose.X.X, pose.X.Y));
            var hulls = PhysicsShapeBackend.GetHulls(source, compound.Points); var best = default(B2Manifold);
            // A partitioned contour contributes its outermost directed entry once, not one recovery plane per internal piece.
            foreach (var hull in hulls)
            {
                var piece = new B2ShapeProxy { points = hull.points, count = hull.count, radius = own.SeparationRay is null ? query.radius : other.radius };
                var candidate = own.SeparationRay is not null
                    ? PhysicsSeparationRay.PairContact(query, own.SeparationRay.Value.SlideOnSlope, piece, transform, null, motion, margin)
                    : PhysicsSeparationRay.PairContact(WorldProxy(piece, transform, default), null, other, otherPose, target.SeparationRay, motion, margin);
                if (candidate.pointCount > 0 && (best.pointCount == 0 || candidate.points[0].separation < best.points[0].separation)) best = candidate;
            }
            return best;
        }
        return PhysicsSeparationRay.PairContact(query, own.SeparationRay?.SlideOnSlope, other, otherPose, target.SeparationRay, motion, margin);
    }

    private static B2Manifold MotionManifold(in B2ShapeProxy query, PhysicsFixtureTag own, B2Transform from, B2Vec2 recovery,
        in B2ShapeProxy other, PhysicsFixtureTag target, B2Transform otherPose)
    {
        if (query.isBoundary || other.isBoundary || own.Compound is null && target.Compound is null) return PhysicsDirectSpaceState.GetManifold(query, other, otherPose);
        // ponytail: reuse full-contour SAT scratch; cache transformed hulls if compound CPU queries dominate measured cost.
        var firstPose = ScenePose(from, recovery) * (own.Compound?.LocalPose ?? Transform.Identity);
        var secondPose = ScenePose(otherPose, default) * (target.Compound?.LocalPose ?? Transform.Identity);
        ReadOnlySpan<Vector2> first = own.Compound is { } a ? a.Points : [];
        ReadOnlySpan<Vector2> second = target.Compound is { } b ? b.Points : [];
        return PhysicsShapeCollision.FullMotionContact(query, first, firstPose, WorldProxy(other, otherPose, default), second, secondPose);
    }
    private static Transform ScenePose(B2Transform pose, B2Vec2 offset) =>
        new(new Vector2(pose.q.c, pose.q.s), new Vector2(-pose.q.s, pose.q.c), ToScene(pose.p + offset));

    private static void RetainRecoveryPlane(Span<System.Numerics.Vector4> planes, ref int count, System.Numerics.Vector4 plane)
    {
        if (count < planes.Length) { planes[count++] = plane; return; }
        var shallowest = 0;
        for (var i = 1; i < count; i++) if (planes[i].Z < planes[shallowest].Z) shallowest = i;
        if (plane.Z > planes[shallowest].Z) planes[shallowest] = plane;
    }

    private void AddMotionCandidates(IReadOnlyList<B2ShapeId> shapes, float priority, RID ownerRID,
        IReadOnlyList<B2ShapeId> ownShapes, RID[] excludedBodies, ulong[] excludedObjects)
    {
        for (var shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
        {
            var shape = shapes[shapeIndex];
            var tag = b2Shape_GetUserData(shape).GetRef<PhysicsFixtureTag>();
            if (tag is null || tag.ColliderRID == ownerRID || Array.IndexOf(excludedBodies, tag.ColliderRID) >= 0 ||
                PhysicsServer.Service.BodiesExcepted(ownerRID, tag.ColliderRID))
                continue;
            var identity = tag.ObjectIdentity;
            if (identity.ID != 0 && Array.IndexOf(excludedObjects, identity.ID) >= 0) continue;
            var filter = b2Shape_GetFilter(shape);
            var eligible = false;
            for (var ownIndex = 0; ownIndex < ownShapes.Count; ownIndex++)
            {
                var own = ownShapes[ownIndex];
                var ownFilter = b2Shape_GetFilter(own);
                if ((ownFilter.maskBits & filter.categoryBits) != 0 &&
                    (filter.maskBits & ownFilter.categoryBits) != 0) { eligible = true; break; }
            }
            if (eligible) _motionCandidates.Add(new(shape, tag, priority));
        }
    }

    private static B2ShapeProxy WorldProxy(B2ShapeProxy proxy, in B2Transform transform, in B2Vec2 offset)
    {
        if (proxy.isBoundary)
        {
            var pose = transform; pose.p += offset; proxy.boundary = B2Boundaries.Transform(proxy.boundary, pose);
        }
        for (var index = 0; index < proxy.count; index++)
            proxy.points[index] = b2TransformPoint(transform, proxy.points[index]) + offset;
        return proxy;
    }

    private static bool AcceptOneWay(in MotionCandidate candidate, in B2Vec2 normal,
        float depth, float queryMargin)
    {
        var data = candidate.Tag.OneWay;
        if (data is null) return true;
        var rotation = b2Body_GetRotation(b2Shape_GetBody(candidate.ShapeID));
        var pass = b2RotateVector(rotation, new(data.LocalDirection.X, data.LocalDirection.Y));
        return b2Dot(normal, pass) < 0 && depth <= MathF.Max(data.Margin * MetersPerUnit, queryMargin);
    }

    private static bool AcceptOneWayMotion(in MotionCandidate candidate, in B2Vec2 motion,
        in B2Transform otherTransform)
    {
        var data = candidate.Tag.OneWay;
        if (data is null) return true;
        var pass = b2RotateVector(otherTransform.q, new(data.LocalDirection.X, data.LocalDirection.Y));
        return b2Dot(motion, pass) > 0;
    }

    private static Vector2 ToScene(B2Vec2 value) => new(value.X * UnitsPerMeter, value.Y * UnitsPerMeter);

    private readonly record struct MotionContact(MotionCandidate Candidate, int LocalShape,
        B2Vec2 Point, B2Vec2 Normal, float Depth);
}
