using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Manifolds;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    private readonly List<ShapeContactPair> _contactPairs = [];

    internal override List<ShapeContactPair> CollectShapeContacts(PhysicsShapeQueryParameters parameters, int limit)
    {
        var space = PrepareShapeQuery(parameters);
        var contacts = _contactPairs;
        contacts.Clear();
        if (_queryProxies.Count == 0 || _shapeCandidates.Count == 0) return contacts;
        var world = b2GetWorldFromId(space.WorldID);
        var motion = PhysicsShapeBackend.ToBackend(parameters.Motion);
        foreach (var candidate in _shapeCandidates)
        {
            var backendShape = b2GetShape(world, candidate.ShapeID);
            var other = PhysicsShapeBackend.GetQueryProxy(backendShape);
            var otherTransform = b2Body_GetTransform(b2Shape_GetBody(candidate.ShapeID));
            for (var piece = 0; piece < _queryProxies.Count; piece++)
            {
                var query = _queryProxies[piece];
                if (!TryQueryContact(query, other, otherTransform, candidate.Tag, motion, out var manifold)) continue;
                for (var index = 0; index < manifold.pointCount; index++)
                {
                    var contact = manifold.points[index];
                    var halfSeparation = contact.separation * 0.5f;
                    var queryPoint = contact.point - manifold.normal * halfSeparation;
                    var colliderPoint = contact.point + manifold.normal * halfSeparation;
                    contacts.Add(new(candidate.Tag.ColliderRID, candidate.Tag.ShapeIndex, piece,
                        ToScene(queryPoint), ToScene(colliderPoint)));
                }
            }
        }
        contacts.Sort(static (left, right) =>
        {
            var order = left.RID.CompareTo(right.RID);
            if (order != 0) return order;
            order = left.ShapeIndex.CompareTo(right.ShapeIndex);
            return order != 0 ? order : left.Piece.CompareTo(right.Piece);
        });
        return contacts;
    }

    internal override PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters)
    {
        var space = PrepareShapeQuery(parameters);
        var world = b2GetWorldFromId(space.WorldID);
        var motion = PhysicsShapeBackend.ToBackend(parameters.Motion);
        PhysicsRestInfo? best = null;
        var bestDepth = float.NegativeInfinity;
        foreach (var candidate in _shapeCandidates)
        {
            var backendShape = b2GetShape(world, candidate.ShapeID);
            var other = PhysicsShapeBackend.GetQueryProxy(backendShape);
            var bodyID = b2Shape_GetBody(candidate.ShapeID);
            var otherTransform = b2Body_GetTransform(bodyID);
            for (var piece = 0; piece < _queryProxies.Count; piece++)
            {
                var query = _queryProxies[piece];
                if (!TryQueryContact(query, other, otherTransform, candidate.Tag, motion, out var manifold)) continue;
                for (var index = 0; index < manifold.pointCount; index++)
                {
                    var contact = manifold.points[index];
                    var depth = -contact.separation;
                    if (depth < bestDepth || depth == bestDepth && best is { } prior &&
                        (candidate.Tag.ColliderRID > prior.ColliderRID ||
                         candidate.Tag.ColliderRID == prior.ColliderRID && candidate.Tag.ShapeIndex >= prior.ShapeIndex))
                        continue;
                    var colliderPoint = contact.point + manifold.normal * (contact.separation * 0.5f);
                    var velocity = b2Shape_IsSensor(candidate.ShapeID) ? default :
                        b2Body_GetWorldPointVelocity(bodyID, colliderPoint);
                    bestDepth = depth;
                    best = new PhysicsRestInfo(candidate.Tag.ColliderRID,
                        PhysicsServer.Service.ResolveSceneObject(candidate.Tag.ColliderRID), candidate.Tag.ObjectIdentity,
                        candidate.Tag.ShapeIndex, ToScene(colliderPoint),
                        new(-manifold.normal.X, -manifold.normal.Y),
                        new(velocity.X * PhysicsSpace.UnitsPerMeter,
                            velocity.Y * PhysicsSpace.UnitsPerMeter));
                }
            }
        }
        return best;
    }

    private static bool TryContact(in B2ShapeProxy query, in B2ShapeProxy other,
        in B2Transform otherTransform, in B2Vec2 motion, out B2Manifold manifold)
    {
        var contactQuery = query;
        if (!Overlaps(query, other, otherTransform))
        {
            var motionLength = b2Length(motion);
            if (motionLength == 0)
            {
                manifold = default;
                return false;
            }
            var hit = Cast(query, other, otherTransform, motion, 1f);
            if (!hit.hit)
            {
                manifold = default;
                return false;
            }
            var fraction = MathF.Min(1f, hit.fraction + 2f * B2Constants.B2_LINEAR_SLOP / motionLength);
            if (contactQuery.isBoundary) contactQuery.boundary.offset += fraction * b2Dot(contactQuery.boundary.normal, motion);
            for (var point = 0; point < contactQuery.count; point++)
                contactQuery.points[point] += fraction * motion;
        }
        manifold = GetManifold(contactQuery, other, otherTransform);
        return manifold.pointCount != 0;
    }

    private bool TryQueryContact(in B2ShapeProxy query, in B2ShapeProxy other,
        in B2Transform otherTransform, PhysicsFixtureTag tag, in B2Vec2 motion, out B2Manifold manifold)
    {
        if (query.isBoundary || other.isBoundary) return TryContact(BoundaryQuery(query), other, otherTransform, motion, out manifold);
        if (_queryRaySlide is null && tag.SeparationRay is null)
            return TryContact(query, other, otherTransform, motion, out manifold);
        manifold = PhysicsSeparationRay.PairContact(query, _queryRaySlide, other, otherTransform,
            tag.SeparationRay, motion, _queryMargin);
        return manifold.pointCount != 0;
    }

    internal static B2Manifold GetManifold(in B2ShapeProxy query, in B2ShapeProxy other,
        in B2Transform otherTransform)
    {
        if (query.isBoundary || other.isBoundary) return B2Boundaries.Contact(query, b2Transform_identity, other, otherTransform);
        var identity = b2Transform_identity;
        var queryKind = query.count == 1 ? 1 : query.count == 2 ? 2 : 3;
        var otherKind = other.count == 1 ? 1 : other.count == 2 ? 2 : 3;
        var flipped = false;
        B2Manifold manifold;
        if (queryKind == 1)
        {
            var circle = new B2Circle { center = query.points[0], radius = query.radius };
            if (otherKind == 1)
                manifold = b2CollideCircles(circle, identity,
                    new B2Circle { center = other.points[0], radius = other.radius }, otherTransform);
            else if (otherKind == 2)
            {
                manifold = b2CollideCapsuleAndCircle(Capsule(other), otherTransform, circle, identity);
                flipped = true;
            }
            else
            {
                var polygon = Polygon(other);
                manifold = b2CollidePolygonAndCircle(ref polygon, otherTransform, circle, identity);
                flipped = true;
            }
        }
        else if (queryKind == 2)
        {
            var capsule = Capsule(query);
            if (otherKind == 1)
                manifold = b2CollideCapsuleAndCircle(capsule, identity,
                    new B2Circle { center = other.points[0], radius = other.radius }, otherTransform);
            else if (otherKind == 2)
                manifold = b2CollideCapsules(capsule, identity, Capsule(other), otherTransform);
            else
            {
                var polygon = Polygon(other);
                manifold = b2CollidePolygonAndCapsule(ref polygon, otherTransform, capsule, identity);
                flipped = true;
            }
        }
        else
        {
            var polygon = Polygon(query);
            if (otherKind == 1)
                manifold = b2CollidePolygonAndCircle(ref polygon, identity,
                    new B2Circle { center = other.points[0], radius = other.radius }, otherTransform);
            else if (otherKind == 2)
                manifold = b2CollidePolygonAndCapsule(ref polygon, identity, Capsule(other), otherTransform);
            else
            {
                var otherPolygon = Polygon(other);
                manifold = b2CollidePolygons(ref polygon, identity, ref otherPolygon, otherTransform);
            }
        }
        if (flipped) manifold.normal = -manifold.normal;
        return manifold;
    }

    private static B2Capsule Capsule(in B2ShapeProxy proxy) =>
        new(proxy.points[0], proxy.points[1], proxy.radius);

    private static B2Polygon Polygon(in B2ShapeProxy proxy)
    {
        var hull = new B2Hull { count = proxy.count, points = proxy.points };
        return b2MakePolygon(hull, proxy.radius);
    }

    private static Vector2 ToScene(B2Vec2 value) =>
        new(value.X * PhysicsSpace.UnitsPerMeter, value.Y * PhysicsSpace.UnitsPerMeter);
}
