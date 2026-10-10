namespace Electron2D;

/// <summary>A typed result for the nearest ray hit in a two-dimensional physics space.</summary>
public readonly struct PhysicsRayResult
{
    /// <summary>Captures a ray result using a live collider's current object association.</summary>
    /// <param name="collider">A live scene or server body or Area RID.</param>
    /// <param name="shapeIndex">The collider's logical shape-owner index.</param>
    /// <param name="position">The global hit position in scene units.</param>
    /// <param name="normal">A finite global normal, or zero for an inside hit.</param>
    /// <remarks>This constructor does not perform a query. The values are supplied by a query implementation.
    /// Collider access follows its owner-thread, solver and failed-world guards. The sampled identity survives later rebind or disposal.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live collider.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The shape index or a supplied vector is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access violates the collider's owner-thread, solver or failed-world guard.</exception>
    /// <exception cref="ObjectDisposedException">The collider's shape or the physics service has been disposed.</exception>
    public PhysicsRayResult(RID collider, int shapeIndex, Vector2 position, Vector2 normal)
    {
        if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position));
        if (!normal.IsFinite()) throw new ArgumentOutOfRangeException(nameof(normal));
        var captured = PhysicsServer.Service.CaptureResultCollider(collider, shapeIndex);
        ColliderRID = collider; Collider = captured.Scene; Identity = captured.Identity;
        ShapeIndex = shapeIndex; Position = position; Normal = normal;
    }

    internal PhysicsRayResult(RID rid, CollisionObject? collider, ObjectIdentity identity, int shapeIndex, Vector2 position, Vector2 normal)
    {
        ColliderRID = rid;
        Collider = collider;
        Identity = identity;
        ShapeIndex = shapeIndex;
        Position = position;
        Normal = normal;
    }

    /// <summary>Gets the stable identity of the collider, including server-only colliders.</summary>
    /// <value>A nonempty RID from the owning physics server.</value>
    public RID ColliderRID { get; }
    /// <summary>Gets the scene collider, or null for a server-only collider.</summary>
    /// <value>The scene object when one exists.</value>
    public CollisionObject? Collider { get; }
    /// <summary>Gets the sampled object association ID, including server-bound objects.</summary>
    /// <value>The sampled instance ID, or zero when unassigned.</value>
    public ulong ColliderID => Identity.ID;
    internal ObjectIdentity Identity { get; }
    /// <summary>Gets the live object instance sampled with this hit, including server-bound objects.</summary>
    /// <value>The weakly borrowed instance, or null after disposal/collection or when unassigned.</value>
    public ElectronObject? ColliderObject => Identity.Target;
    /// <summary>Gets the collider's stable shape-owner index.</summary>
    /// <value>The direct owner slot, unchanged by fixture rebuild.</value>
    public int ShapeIndex { get; }
    /// <summary>Gets the hit position in global scene units.</summary>
    /// <value>The first eligible contact point along the ray.</value>
    public Vector2 Position { get; }
    /// <summary>Gets the outward contact normal, or zero when hit from inside.</summary>
    /// <value>A world-space direction or zero.</value>
    public Vector2 Normal { get; }
}

/// <summary>A typed collider and shape-owner result for a point query.</summary>
public readonly struct PhysicsPointResult
{
    /// <summary>Captures a point-query result using a live collider's current object association.</summary>
    /// <param name="collider">A live scene or server body or Area RID.</param>
    /// <param name="shapeIndex">The collider's logical shape-owner index.</param>
    /// <remarks>This constructor does not perform a query. Collider access follows its owner-thread,
    /// solver and failed-world guards. The sampled identity survives later rebind or disposal.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live collider.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The logical shape index is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access violates the collider's owner-thread, solver or failed-world guard.</exception>
    /// <exception cref="ObjectDisposedException">The collider's shape or the physics service has been disposed.</exception>
    public PhysicsPointResult(RID collider, int shapeIndex)
    {
        var captured = PhysicsServer.Service.CaptureResultCollider(collider, shapeIndex);
        ColliderRID = collider; Collider = captured.Scene; Identity = captured.Identity; ShapeIndex = shapeIndex;
    }

    internal PhysicsPointResult(RID rid, CollisionObject? collider, ObjectIdentity identity, int shapeIndex)
    {
        ColliderRID = rid;
        Collider = collider;
        Identity = identity;
        ShapeIndex = shapeIndex;
    }

    /// <summary>Gets the stable identity of the collider, including server-only colliders.</summary>
    /// <value>A nonempty RID from the owning physics server.</value>
    public RID ColliderRID { get; }
    /// <summary>Gets the scene collider, or null for a server-only collider.</summary>
    /// <value>The scene object when one exists.</value>
    public CollisionObject? Collider { get; }
    /// <summary>Gets the sampled object association ID, including server-bound objects.</summary>
    /// <value>The sampled instance ID, or zero when unassigned.</value>
    public ulong ColliderID => Identity.ID;
    internal ObjectIdentity Identity { get; }
    /// <summary>Gets the live object instance sampled with this hit, including server-bound objects.</summary>
    /// <value>The weakly borrowed instance, or null after disposal/collection or when unassigned.</value>
    public ElectronObject? ColliderObject => Identity.Target;
    /// <summary>Gets the collider's stable shape-owner index.</summary>
    /// <value>The direct owner slot, unchanged by fixture rebuild.</value>
    public int ShapeIndex { get; }
}

/// <summary>Queries the live solver state of one two-dimensional physics space.</summary>
/// <remarks>A view becomes unusable when its owning space is freed. Queries require that space's owner thread
/// and cannot run during its solver step. Built-in views use that space's selected implementation and prepared scratch storage.
/// Consumer implementations derive from PhysicsDirectSpaceStateExtension; inherited public queries dispatch its guarded typed hooks.</remarks>
public partial class PhysicsDirectSpaceState : ElectronObject
{
    private readonly RID _spaceRID;

    internal PhysicsDirectSpaceState(RID spaceRID) => _spaceRID = spaceRID;
    internal void EnsureContext(PhysicsSpace space)
    {
        ThrowIfDisposed(); space.EnsureQueryAccess();
        if (_spaceRID != space.RID) throw new InvalidOperationException("The returned direct-space view belongs to another world.");
    }

    /// <summary>Finds the nearest eligible collider along a ray.</summary>
    /// <param name="parameters">Global endpoints, filtering and exclusion options.</param>
    /// <returns>The nearest typed hit, or null when the ray finds none.</returns>
    /// <remarks>Origin containment applies to the complete logical shape, including compound polygon pieces.
    /// Inside hits are skipped unless enabled; enabled inside hits use the origin and a zero normal.</remarks>
    /// <exception cref="ArgumentNullException">Parameters are null.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the world is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The view has been disposed.</exception>
    public PhysicsRayResult? IntersectRay(PhysicsRayQueryParameters parameters)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parameters);
        return IntersectRay(parameters.From, parameters.To, parameters.CollisionMask, parameters.ExclusionsArray,
            parameters.CollideWithAreas, parameters.CollideWithBodies, parameters.HitFromInside);
    }

    internal PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, RID[] excluded,
        bool collideWithAreas, bool collideWithBodies, bool hitFromInside)
    {
        ThrowIfDisposed();
        if (!from.IsFinite() || !to.IsFinite())
            throw new ArgumentOutOfRangeException(nameof(from), "Ray endpoints must be finite.");
        var space = PhysicsServer.Service.GetSceneSpace(_spaceRID);
        space.PrepareForQuery();
        var motion = to - from;
        if (!motion.IsFinite()) throw new ArgumentOutOfRangeException(nameof(to), "Ray span exceeds the finite range.");
        if (motion == Vector2.Zero || mask == 0 || !collideWithBodies && !collideWithAreas) return null;
        return this is PhysicsDirectSpaceStateExtension extension
            ? extension.QueryRay(from, to, mask, excluded, collideWithAreas, collideWithBodies, hitFromInside)
            : space.BackendImplementation.IntersectRay(from, to, mask, excluded, collideWithAreas, collideWithBodies, hitFromInside);
    }

    /// <summary>Finds filled shapes containing a global point.</summary>
    /// <param name="parameters">Point, filtering and exclusion options.</param>
    /// <param name="maxResults">Maximum results after stable RID/shape-index ordering; 32 by default.</param>
    /// <returns>A caller-owned array of typed collider hits, empty when none qualify.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The maximum result count is negative.</exception>
    /// <exception cref="ArgumentNullException">Parameters are null.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the world is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The view has been disposed.</exception>
    public PhysicsPointResult[] IntersectPoint(PhysicsPointQueryParameters parameters, int maxResults = 32)
    {
        if (maxResults < 0) throw new ArgumentOutOfRangeException(nameof(maxResults));
        if (this is PhysicsDirectSpaceStateExtension extension)
        {
            PrepareQuery(parameters);
            PhysicsPointResult[] extensionOutput = maxResults == 0 ? [] : new PhysicsPointResult[maxResults];
            var written = extension.QueryPoint(parameters, extensionOutput);
            return written == extensionOutput.Length ? extensionOutput : extensionOutput[..written];
        }
        var hits = CollectPointHits(parameters);
        var count = Math.Min(maxResults, hits.Count);
        if (count == 0) return [];
        var output = new PhysicsPointResult[count];
        hits.CopyTo(0, output, 0, count);
        return output;
    }

    /// <summary>Copies ordered unique point-query hits into caller-owned storage.</summary>
    /// <param name="parameters">The global point and collider filters.</param>
    /// <param name="results">Destination storage; its length is the maximum result count.</param>
    /// <returns>The written count. Remaining elements are unchanged; excess hits are truncated in RID/index order.</returns>
    /// <remarks>Reuses prepared query scratch capacity and creates no output array.</remarks>
    /// <exception cref="ArgumentNullException">Parameters are null.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the world is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The view has been disposed.</exception>
    public int IntersectPoint(PhysicsPointQueryParameters parameters, Span<PhysicsPointResult> results)
    {
        if (this is PhysicsDirectSpaceStateExtension extension)
        {
            PrepareQuery(parameters); return extension.QueryPoint(parameters, results);
        }
        var hits = CollectPointHits(parameters);
        var count = Math.Min(results.Length, hits.Count);
        for (var i = 0; i < count; i++) results[i] = hits[i];
        return count;
    }

    internal List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters) =>
        PrepareQuery(parameters).CollectPointHits(parameters);

    private PhysicsWorldBackend PrepareQuery(object parameters)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parameters);
        var space = PhysicsServer.Service.GetSceneSpace(_spaceRID);
        space.PrepareForQuery();
        return space.BackendImplementation;
    }
}
