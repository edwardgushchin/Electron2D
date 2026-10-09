using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Manifolds;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

/// <summary>A typed closest contact for a shape at rest in a physics space.</summary>
public readonly struct PhysicsRestInfo
{
    internal PhysicsRestInfo(RID rid, CollisionObject? collider, int shapeIndex, Vector2 point,
        Vector2 normal, Vector2 linearVelocity)
    {
        ColliderRID = rid;
        Collider = collider;
        ShapeIndex = shapeIndex;
        Point = point;
        Normal = normal;
        LinearVelocity = linearVelocity;
    }

    /// <summary>Gets the stable RID of the intersecting collider.</summary>
    /// <value>A nonempty server identity.</value>
    public RID ColliderRID { get; }
    /// <summary>Gets the live scene collider, or null for a server-only collider.</summary>
    /// <value>A scene object when one exists.</value>
    public CollisionObject? Collider { get; }
    /// <summary>Gets the scene object instance ID, or zero for a server-only collider.</summary>
    /// <value>A managed instance ID or zero.</value>
    public ulong ColliderID => Collider?.InstanceID ?? 0;
    /// <summary>Gets the collider's direct shape-owner index.</summary>
    /// <value>The index remains stable across fixture rebuilds.</value>
    public int ShapeIndex { get; }
    /// <summary>Gets the collider contact point in global scene units.</summary>
    /// <value>The selected point on the intersecting shape.</value>
    public Vector2 Point { get; }
    /// <summary>Gets the contact normal pointing away from the collider.</summary>
    /// <value>A global unit direction for a resolved contact.</value>
    public Vector2 Normal { get; }
    /// <summary>Gets the collider velocity at Point in scene units per second.</summary>
    /// <value>Zero for an Area sensor.</value>
    public Vector2 LinearVelocity { get; }
}

public sealed partial class PhysicsDirectSpaceState
{
    private readonly List<ContactPair> _contactPairs = [];

    private readonly record struct ContactPair(RID RID, int ShapeIndex, int Piece, Vector2 QueryPoint,
        Vector2 ColliderPoint);

    /// <summary>Returns contact-point pairs between a shape and eligible space colliders.</summary>
    /// <param name="parameters">A live shape, pose, global motion, margin and filters.</param>
    /// <param name="maxResults">Maximum contact pairs, 32 by default.</param>
    /// <returns>A caller-owned even array: query point first, collider point second for each pair.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The maximum is negative.</exception>
    public Vector2[] CollideShape(PhysicsShapeQueryParameters parameters, int maxResults = 32)
    {
        if (maxResults < 0) throw new ArgumentOutOfRangeException(nameof(maxResults));
        var contacts = CollectShapeContacts(parameters);
        var count = Math.Min(maxResults, contacts.Count);
        if (count == 0) return [];
        var result = new Vector2[count * 2];
        CopyShapeContacts(contacts, result, count);
        return result;
    }

    /// <summary>Copies ordered query/collider contact-point pairs into caller-owned storage.</summary>
    /// <param name="parameters">The live shape, pose, motion, margin and filters.</param>
    /// <param name="results">Destination points; only complete pairs are written.</param>
    /// <returns>The number of pairs written, at most half the destination length. Unused elements are unchanged.</returns>
    /// <remarks>Reuses prepared query scratch capacity and creates no output array. Pair ordering and query errors match the array overload.</remarks>
    /// <exception cref="ArgumentNullException">Parameters are null.</exception>
    /// <exception cref="ArgumentException">The shape RID is invalid or query geometry is invalid.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the space is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The view or required shape/parameters are disposed.</exception>
    public int CollideShape(PhysicsShapeQueryParameters parameters, Span<Vector2> results)
    {
        var contacts = CollectShapeContacts(parameters);
        var count = Math.Min(results.Length / 2, contacts.Count);
        CopyShapeContacts(contacts, results, count);
        return count;
    }

    private static void CopyShapeContacts(List<ContactPair> contacts, Span<Vector2> results, int count)
    {
        for (var i = 0; i < count; i++)
        {
            results[i * 2] = contacts[i].QueryPoint;
            results[i * 2 + 1] = contacts[i].ColliderPoint;
        }
    }

    private List<ContactPair> CollectShapeContacts(PhysicsShapeQueryParameters parameters)
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

    /// <summary>Returns the deepest contact across the shape's pose and motion, with collider velocity.</summary>
    /// <param name="parameters">A live shape, pose, global motion, margin and filters.</param>
    /// <returns>A typed contact, or null when the shape touches no eligible collider.</returns>
    public PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters)
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
                        PhysicsServer.Service.ResolveSceneObject(candidate.Tag.ColliderRID),
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
