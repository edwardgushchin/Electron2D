namespace Electron2D;

internal sealed partial class GPUPhysicsWorldBackend
{
    private readonly List<PhysicsPointResult> _pointHits = [];
    private readonly List<PhysicsShapeResult> _shapeHits = [];
    private readonly List<ShapeContactPair> _contactPairs = [];

    internal override PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, RID[] excluded,
        bool collideWithAreas, bool collideWithBodies, bool hitFromInside) =>
        Space.GPURay(from, to, mask, excluded, collideWithAreas, collideWithBodies, hitFromInside);

    internal override List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters)
    {
        _pointHits.Clear();
        if (parameters.CollisionMask != 0 && (parameters.CollideWithBodies || parameters.CollideWithAreas))
            Space.GPUPoint(parameters, _pointHits);
        return _pointHits;
    }

    internal override List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters)
    {
        _shapeHits.Clear();
        foreach (ref readonly var hit in Space.GPUShapeQuery(parameters, GPUPhysicsBodyStore.ShapeQueryMode.Intersect, GPUStore.ShapeCount))
        {
            var owner = Space.GPUQueryOwner(hit.Collider);
            _shapeHits.Add(new(owner.RID, owner.SceneOwner, owner.ObjectIdentity, hit.LogicalShape));
        }
        return _shapeHits;
    }

    internal override (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters)
    {
        var hits = Space.GPUShapeQuery(parameters, GPUPhysicsBodyStore.ShapeQueryMode.Cast, 1);
        return hits.IsEmpty ? (1, 1) : (hits[0].SafeFraction, hits[0].UnsafeFraction);
    }

    internal override List<ShapeContactPair> CollectShapeContacts(PhysicsShapeQueryParameters parameters, int limit)
    {
        _contactPairs.Clear();
        foreach (ref readonly var hit in Space.GPUShapeQuery(parameters, GPUPhysicsBodyStore.ShapeQueryMode.Contacts, limit))
            _contactPairs.Add(new(Space.GPUQueryOwner(hit.Collider).RID, hit.LogicalShape, (int)hit.Piece, hit.QueryPoint, hit.ColliderPoint));
        return _contactPairs;
    }

    internal override PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters)
    {
        var hits = Space.GPUShapeQuery(parameters, GPUPhysicsBodyStore.ShapeQueryMode.Rest, 1);
        if (hits.IsEmpty) return null;
        ref readonly var hit = ref hits[0]; var owner = Space.GPUQueryOwner(hit.Collider);
        return new(owner.RID, owner.SceneOwner, owner.ObjectIdentity, hit.LogicalShape, hit.ColliderPoint, hit.Normal, hit.Velocity);
    }
}
