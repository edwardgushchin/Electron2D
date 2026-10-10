namespace Electron2D;

/// <summary>A typed closest contact for a shape at rest in a physics space.</summary>
public readonly struct PhysicsRestInfo
{
    /// <summary>Captures a rest result using a live collider's current object association.</summary>
    /// <param name="collider">A live scene or server body or Area RID.</param>
    /// <param name="shapeIndex">The collider's logical shape-owner index.</param>
    /// <param name="point">The global contact point in scene units.</param>
    /// <param name="normal">The finite global contact normal.</param>
    /// <param name="linearVelocity">Collider velocity at the contact point in scene units per second.</param>
    /// <remarks>This constructor does not perform a query. Collider access follows its owner-thread,
    /// solver and failed-world guards. The sampled identity survives later rebind or disposal.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live collider.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The shape index or a supplied vector is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access violates the collider's owner-thread, solver or failed-world guard.</exception>
    /// <exception cref="ObjectDisposedException">The collider's shape or the physics service has been disposed.</exception>
    public PhysicsRestInfo(RID collider, int shapeIndex, Vector2 point, Vector2 normal, Vector2 linearVelocity)
    {
        if (!point.IsFinite()) throw new ArgumentOutOfRangeException(nameof(point));
        if (!normal.IsFinite()) throw new ArgumentOutOfRangeException(nameof(normal));
        if (!linearVelocity.IsFinite()) throw new ArgumentOutOfRangeException(nameof(linearVelocity));
        var captured = PhysicsServer.Service.CaptureResultCollider(collider, shapeIndex);
        ColliderRID = collider; Collider = captured.Scene; Identity = captured.Identity;
        ShapeIndex = shapeIndex; Point = point; Normal = normal; LinearVelocity = linearVelocity;
    }

    internal PhysicsRestInfo(RID rid, CollisionObject? collider, ObjectIdentity identity, int shapeIndex, Vector2 point,
        Vector2 normal, Vector2 linearVelocity)
    {
        ColliderRID = rid;
        Collider = collider;
        Identity = identity;
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
    /// <summary>Gets the sampled object association ID, including server-bound objects.</summary>
    /// <value>The sampled instance ID, or zero when unassigned.</value>
    public ulong ColliderID => Identity.ID;
    internal ObjectIdentity Identity { get; }
    /// <summary>Gets the live object instance sampled with this hit, including server-bound objects.</summary>
    /// <value>The weakly borrowed instance, or null after disposal/collection or when unassigned.</value>
    public ElectronObject? ColliderObject => Identity.Target;
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
    /// <summary>Returns contact-point pairs between a shape and eligible space colliders.</summary>
    /// <param name="parameters">A live shape, pose, global motion, margin and filters.</param>
    /// <param name="maxResults">Maximum contact pairs, 32 by default.</param>
    /// <returns>A caller-owned even array: query point first, collider point second for each pair.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The maximum is negative.</exception>
    public Vector2[] CollideShape(PhysicsShapeQueryParameters parameters, int maxResults = 32)
    {
        if (maxResults < 0) throw new ArgumentOutOfRangeException(nameof(maxResults));
        var contacts = CollectShapeContacts(parameters, maxResults);
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
        var contacts = CollectShapeContacts(parameters, results.Length / 2);
        var count = Math.Min(results.Length / 2, contacts.Count);
        CopyShapeContacts(contacts, results, count);
        return count;
    }

    private static void CopyShapeContacts(List<PhysicsWorldBackend.ShapeContactPair> contacts, Span<Vector2> results, int count)
    {
        for (var i = 0; i < count; i++)
        {
            results[i * 2] = contacts[i].QueryPoint;
            results[i * 2 + 1] = contacts[i].ColliderPoint;
        }
    }

    private List<PhysicsWorldBackend.ShapeContactPair> CollectShapeContacts(PhysicsShapeQueryParameters parameters, int limit) =>
        PrepareQuery(parameters).CollectShapeContacts(parameters, limit);

    /// <summary>Returns the deepest contact across the shape's pose and motion, with collider velocity.</summary>
    /// <param name="parameters">A live shape, pose, global motion, margin and filters.</param>
    /// <returns>A typed contact, or null when the shape touches no eligible collider.</returns>
    public PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters) =>
        PrepareQuery(parameters).GetRestInfo(parameters);

}
