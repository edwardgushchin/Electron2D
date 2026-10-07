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
        for (var index = 0; index < _bodies.Count; index++)
        {
            var body = _bodies[index];
            if (body.GetRID() != rid) continue;
            var backendVelocity = b2Body_GetWorldPointVelocity(body.BackendID, Shape.ToBackend(point));
            velocity = ToScene(backendVelocity);
            layer = body.EffectiveCollisionLayer;
            return true;
        }
        for (var index = 0; index < _serverColliders.Count; index++)
        {
            var body = _serverColliders[index];
            if (body.IsArea || body.RID != rid) continue;
            var backendVelocity = b2Body_GetWorldPointVelocity(body.BackendID, Shape.ToBackend(point));
            velocity = ToScene(backendVelocity);
            layer = body.CollisionLayer;
            return true;
        }
        velocity = default;
        layer = 0;
        return false;
    }

    private readonly List<MotionCandidate> _motionCandidates = [];
    private readonly record struct MotionCandidate(B2ShapeId ShapeID, PhysicsFixtureTag Tag);

    internal MotionResultData TestBodyMotion(RID ownerRID, IReadOnlyList<B2ShapeId> ownShapes,
        Transform from, Vector2 motion, float margin, bool recoveryAsCollision,
        RID[] excludedBodies, ulong[] excludedObjects, bool collideSeparationRay = false)
    {
        PrepareForQuery();
        _motionCandidates.Clear();
        foreach (var other in _bodies)
            if (other.GetRID() != ownerRID)
                AddMotionCandidates(other.BackendShapes, ownerRID, ownShapes, excludedBodies, excludedObjects);
        foreach (var other in _serverColliders)
            if (!other.IsArea && other.RID != ownerRID)
                AddMotionCandidates(other.BackendShapes, ownerRID, ownShapes, excludedBodies, excludedObjects);

        var requested = Shape.ToBackend(motion);
        var fromTransform = new B2Transform(Shape.ToBackend(from.Origin), b2MakeRot(from.Rotation));
        var recovery = new B2Vec2(0, 0);
        var recoveryHit = default(MotionContact);
        var hasRecoveryHit = false;
        var world = b2GetWorldFromId(_worldID);
        var queryMargin = MathF.Max(margin * MetersPerUnit, 0.0001f * MetersPerUnit);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var bestDepth = 0f;
            var bestNormal = new B2Vec2(0, 0);
            var bestContact = default(MotionContact);
            for (var ownIndex = 0; ownIndex < ownShapes.Count; ownIndex++)
            {
                var ownID = ownShapes[ownIndex];
                var ownTag = b2Shape_GetUserData(ownID).GetRef<PhysicsFixtureTag>();
                if (ownTag is null) continue;
                var ray = ownTag.SeparationRay;
                var query = ray is { } data ? PhysicsSeparationRay.WorldProxy(data, fromTransform, recovery) :
                    WorldProxy(b2MakeShapeDistanceProxy(b2GetShape(world, ownID)), fromTransform, recovery);
                if (ray is null) query.radius += queryMargin;
                foreach (var candidate in _motionCandidates)
                {
                    var other = b2MakeShapeDistanceProxy(b2GetShape(world, candidate.ShapeID));
                    var otherTransform = b2Body_GetTransform(b2Shape_GetBody(candidate.ShapeID));
                    B2Manifold manifold;
                    if (ray is not null || candidate.Tag.SeparationRay is not null)
                        manifold = PhysicsSeparationRay.PairContact(query, ray?.SlideOnSlope, other,
                            otherTransform, candidate.Tag.SeparationRay, default, queryMargin);
                    else
                    {
                        if (!PhysicsDirectSpaceState.Overlaps(query, other, otherTransform)) continue;
                        manifold = PhysicsDirectSpaceState.GetManifold(query, other, otherTransform);
                    }
                    if (manifold.pointCount == 0) continue;
                    for (var index = 0; index < manifold.pointCount; index++)
                    {
                        var point = manifold.points[index];
                        var depth = -point.separation;
                        var normal = -manifold.normal;
                        if (depth <= bestDepth || !AcceptOneWay(candidate, normal, depth, queryMargin)) continue;
                        bestDepth = depth;
                        bestNormal = normal;
                        var colliderPoint = point.point + manifold.normal * (point.separation * 0.5f);
                        bestContact = new(candidate, ownTag.ShapeIndex, colliderPoint, normal, depth);
                    }
                }
            }
            if (bestDepth <= 0) break;
            recoveryHit = bestContact;
            hasRecoveryHit = true;
            recovery += bestNormal * MathF.Max(0, bestDepth - queryMargin * 0.05f) * 0.4f;
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
                        var full = PhysicsSeparationRay.PairContact(query, ray?.SlideOnSlope, other,
                            otherTransform, candidate.Tag.SeparationRay, requested * safe, 0);
                        if (full.pointCount == 0) continue;
                        var initial = PhysicsSeparationRay.PairContact(query, ray?.SlideOnSlope, other,
                            otherTransform, candidate.Tag.SeparationRay, default, 0);
                        if (initial.pointCount != 0 && -initial.points[0].separation <= 0.1f * B2_LINEAR_SLOP &&
                            b2Dot(requested, -initial.normal) >= -1e-6f) continue;
                        var lowRay = 0f;
                        var highRay = initial.pointCount == 0 ? safe : 0f;
                        if (initial.pointCount == 0)
                            for (var step = 0; step < 8; step++)
                            {
                                var middle = (lowRay + highRay) * 0.5f;
                                var partial = PhysicsSeparationRay.PairContact(query, ray?.SlideOnSlope, other,
                                    otherTransform, candidate.Tag.SeparationRay, requested * middle, 0);
                                if (partial.pointCount != 0) highRay = middle;
                                else lowRay = middle;
                            }
                        if (lowRay >= safe && hasMotionHit) continue;
                        var rayImpact = query;
                        for (var pointIndex = 0; pointIndex < rayImpact.count; pointIndex++)
                            rayImpact.points[pointIndex] += requested * highRay;
                        var rayContact = PhysicsSeparationRay.PairContact(rayImpact, ray?.SlideOnSlope, other,
                            otherTransform, candidate.Tag.SeparationRay, default, queryMargin);
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
                        var stuck = PhysicsDirectSpaceState.GetManifold(query, other, otherTransform);
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
                    var manifold = PhysicsDirectSpaceState.GetManifold(impact, other, otherTransform);
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
        var scene = PhysicsServer.Service.ResolveSceneObject(rid);
        var candidateBody = b2Shape_GetBody(contact.Candidate.ShapeID);
        var velocity = b2Body_GetWorldPointVelocity(candidateBody, contact.Point);
        return new(ownerRID, rid, scene?.InstanceID ?? 0, contact.LocalShape,
            contact.Candidate.Tag.ShapeIndex, ToScene(contact.Point),
            new(contact.Normal.X, contact.Normal.Y), contact.Depth * UnitsPerMeter,
            ToScene(velocity), travel, remainder, safe, unsafeFraction, true);
    }

    private void AddMotionCandidates(IReadOnlyList<B2ShapeId> shapes, RID ownerRID,
        IReadOnlyList<B2ShapeId> ownShapes, RID[] excludedBodies, ulong[] excludedObjects)
    {
        for (var shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
        {
            var shape = shapes[shapeIndex];
            var tag = b2Shape_GetUserData(shape).GetRef<PhysicsFixtureTag>();
            if (tag is null || tag.ColliderRID == ownerRID || Array.IndexOf(excludedBodies, tag.ColliderRID) >= 0 ||
                PhysicsServer.Service.BodiesExcepted(ownerRID, tag.ColliderRID))
                continue;
            var scene = PhysicsServer.Service.ResolveSceneObject(tag.ColliderRID);
            if (scene is not null && Array.IndexOf(excludedObjects, scene.InstanceID) >= 0) continue;
            var filter = b2Shape_GetFilter(shape);
            var eligible = false;
            for (var ownIndex = 0; ownIndex < ownShapes.Count; ownIndex++)
            {
                var own = ownShapes[ownIndex];
                var ownFilter = b2Shape_GetFilter(own);
                if ((ownFilter.maskBits & filter.categoryBits) != 0 &&
                    (filter.maskBits & ownFilter.categoryBits) != 0) { eligible = true; break; }
            }
            if (eligible) _motionCandidates.Add(new(shape, tag));
        }
    }

    private static B2ShapeProxy WorldProxy(B2ShapeProxy proxy, in B2Transform transform, in B2Vec2 offset)
    {
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
