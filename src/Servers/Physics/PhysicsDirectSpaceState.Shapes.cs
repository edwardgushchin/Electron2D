namespace Electron2D;

/// <summary>A typed collider result from a direct shape-overlap query.</summary>
public readonly struct PhysicsShapeResult
{
    /// <summary>Captures a shape-query result using a live collider's current object association.</summary>
    /// <param name="collider">A live scene or server body or Area RID.</param>
    /// <param name="shapeIndex">The collider's logical shape-owner index.</param>
    /// <remarks>This constructor does not perform a query. Collider access follows its owner-thread,
    /// solver and failed-world guards. The sampled identity survives later rebind or disposal.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live collider.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The logical shape index is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access violates the collider's owner-thread, solver or failed-world guard.</exception>
    /// <exception cref="ObjectDisposedException">The collider's shape or the physics service has been disposed.</exception>
    public PhysicsShapeResult(RID collider, int shapeIndex)
    {
        var captured = PhysicsServer.Service.CaptureResultCollider(collider, shapeIndex);
        ColliderRID = collider; Collider = captured.Scene; Identity = captured.Identity; ShapeIndex = shapeIndex;
    }

    internal PhysicsShapeResult(RID rid, CollisionObject? collider, ObjectIdentity identity, int shapeIndex)
    {
        ColliderRID = rid;
        Collider = collider;
        Identity = identity;
        ShapeIndex = shapeIndex;
    }

    /// <summary>Gets the stable RID of the intersecting collider.</summary>
    /// <value>A nonempty server identity.</value>
    public RID ColliderRID { get; }

    /// <summary>Gets the scene collider, or null for a server-only collider.</summary>
    /// <value>The live scene object when one exists.</value>
    public CollisionObject? Collider { get; }

    /// <summary>Gets the sampled object association ID, including server-bound objects.</summary>
    /// <value>The sampled instance ID, or zero when unassigned.</value>
    public ulong ColliderID => Identity.ID;
    internal ObjectIdentity Identity { get; }
    /// <summary>Gets the live object instance sampled with this hit, including server-bound objects.</summary>
    /// <value>The weakly borrowed instance, or null after disposal/collection or when unassigned.</value>
    public ElectronObject? ColliderObject => Identity.Target;

    /// <summary>Gets the stable direct shape-owner index.</summary>
    /// <value>An index unchanged by compound fixture rebuilds.</value>
    public int ShapeIndex { get; }
}

public partial class PhysicsDirectSpaceState
{
    /// <summary>Finds collider shape owners intersected by a query shape or its motion.</summary>
    /// <param name="parameters">A live Shape resource or shape RID, pose, margin and filters.</param>
    /// <param name="maxResults">Maximum RID/index-ordered results; 32 by default.</param>
    /// <returns>A caller-owned array of unique collider shape-owner results.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The maximum count is negative.</exception>
    public PhysicsShapeResult[] IntersectShape(PhysicsShapeQueryParameters parameters, int maxResults = 32)
    {
        if (maxResults < 0) throw new ArgumentOutOfRangeException(nameof(maxResults));
        if (this is PhysicsDirectSpaceStateExtension extension)
        {
            PrepareQuery(parameters);
            PhysicsShapeResult[] extensionOutput = maxResults == 0 ? [] : new PhysicsShapeResult[maxResults];
            var written = extension.QueryShape(parameters, extensionOutput);
            return written == extensionOutput.Length ? extensionOutput : extensionOutput[..written];
        }
        var hits = CollectShapeHits(parameters);
        var count = Math.Min(maxResults, hits.Count);
        if (count == 0) return [];
        var output = new PhysicsShapeResult[count];
        hits.CopyTo(0, output, 0, count);
        return output;
    }

    /// <summary>Copies ordered unique collider shape-owner hits into caller-owned storage.</summary>
    /// <param name="parameters">The live shape, pose, motion, margin and filters.</param>
    /// <param name="results">Destination storage; its length is the maximum result count.</param>
    /// <returns>The written count. Remaining elements are unchanged; excess hits are truncated in RID/index order.</returns>
    /// <remarks>Reuses query scratch capacity after preparation; no output array is created. Ownership and query errors match the array overload.</remarks>
    /// <exception cref="ArgumentNullException">Parameters are null.</exception>
    /// <exception cref="ArgumentException">The shape RID is invalid or query geometry is invalid.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or the space is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The view or required shape/parameters are disposed.</exception>
    public int IntersectShape(PhysicsShapeQueryParameters parameters, Span<PhysicsShapeResult> results)
    {
        if (this is PhysicsDirectSpaceStateExtension extension)
        {
            PrepareQuery(parameters); return extension.QueryShape(parameters, results);
        }
        var hits = CollectShapeHits(parameters);
        var count = Math.Min(results.Length, hits.Count);
        for (var i = 0; i < count; i++) results[i] = hits[i];
        return count;
    }

    private List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters) =>
        PrepareQuery(parameters).CollectShapeHits(parameters);

    /// <summary>Finds safe and unsafe fractions of a shape's requested global motion.</summary>
    /// <param name="parameters">A live query shape, pose, motion, margin and filters.</param>
    /// <returns>(1, 1) when no new collision occurs; initial overlaps are ignored across every piece of each logical collider shape.</returns>
    public (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters)
    {
        var backend = PrepareQuery(parameters);
        return this is PhysicsDirectSpaceStateExtension extension ? extension.QueryMotion(parameters) : backend.CastMotion(parameters);
    }

}
