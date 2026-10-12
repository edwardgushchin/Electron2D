namespace Electron2D;

/// <summary>Retains immutable solved body-contact values with engine-sampled weak collider identity.</summary>
/// <remarks>Construct while both bodies and their logical slots are live in the same world. Values and sampled IDs
/// survive later shape edits, rebinds or collider release; the weak object target expires on disposal or collection.
/// Positions, normals and velocities use global axes. Local identifies this body, not a coordinate system.
/// Construction also runs inside an engine-qualified selected contact-publication hook after solving;
/// this grants only immutable contact identity capture, while ordinary solver queries and state mutation remain forbidden.</remarks>
public readonly struct PhysicsBodyContact
{
    /// <summary>Captures validated logical identities and finite scene-unit contact values.</summary>
    /// <remarks>Derives signed depth by projecting the collider-minus-local boundary-anchor difference along the normalized normal.</remarks>
    /// <param name="body">The observed live body RID.</param>
    /// <param name="localShape">The observed body's logical slot.</param>
    /// <param name="collider">The other live body RID, including tile/server bodies.</param>
    /// <param name="colliderShape">The other body's logical slot.</param>
    /// <param name="localPosition">This body's global contact point in scene units.</param>
    /// <param name="colliderPosition">The other body's global contact point in scene units.</param>
    /// <param name="normal">The finite global normal pointing away from the collider.</param>
    /// <param name="localVelocity">This body's global point velocity in scene units per second.</param>
    /// <param name="colliderVelocity">The other body's global point velocity in scene units per second.</param>
    /// <param name="impulse">The full-step impulse applied to this body, in scene units times kilograms per second.</param>
    /// <exception cref="ArgumentException">A RID is not a body, the bodies are detached or belong to different worlds.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A slot or supplied vector is invalid.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or a required world is solving/failed.</exception>
    /// <exception cref="ObjectDisposedException">A required resource is disposed.</exception>
    public PhysicsBodyContact(RID body, int localShape, RID collider, int colliderShape, Vector2 localPosition,
        Vector2 colliderPosition, Vector2 normal, Vector2 localVelocity, Vector2 colliderVelocity, Vector2 impulse)
        : this(body, localShape, collider, colliderShape, localPosition, colliderPosition, normal,
            localVelocity, colliderVelocity, impulse, (colliderPosition - localPosition).Dot(normal.Normalized()))
    { }

    /// <summary>Samples immutable contact values with an explicit signed numerical penetration depth.</summary>
    /// <param name="body">The observed live scene/server body RID.</param>
    /// <param name="localShape">Its enabled logical shape index.</param>
    /// <param name="collider">The other live body RID in the same world.</param>
    /// <param name="colliderShape">The other body's enabled logical shape index.</param>
    /// <param name="localPosition">This body's global boundary point.</param>
    /// <param name="colliderPosition">The other body's global boundary point.</param>
    /// <param name="normal">The global normal pointing away from the collider.</param>
    /// <param name="localVelocity">This body's global contact-point velocity.</param>
    /// <param name="colliderVelocity">The other body's global contact-point velocity.</param>
    /// <param name="impulse">The completed-step impulse applied to this body.</param>
    /// <param name="depth">Finite signed depth in scene units; positive penetration, negative separation.</param>
    /// <exception cref="ArgumentException">Identities, attachment or logical slots are invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Depth or a supplied vector is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The caller is off-owner or a required world is solving/failed.</exception>
    /// <exception cref="ObjectDisposedException">A required resource is disposed.</exception>
    public PhysicsBodyContact(RID body, int localShape, RID collider, int colliderShape, Vector2 localPosition,
        Vector2 colliderPosition, Vector2 normal, Vector2 localVelocity, Vector2 colliderVelocity, Vector2 impulse, float depth)
    {
        if (!float.IsFinite(depth)) throw new ArgumentOutOfRangeException(nameof(depth));
        Depth = depth;
        if (!localPosition.IsFinite() || !colliderPosition.IsFinite() || !normal.IsFinite() || !localVelocity.IsFinite() ||
            !colliderVelocity.IsFinite() || !impulse.IsFinite()) throw new ArgumentOutOfRangeException(nameof(localPosition));
        if (body == collider) throw new ArgumentException("A body contact requires a distinct collider.", nameof(collider));
        var first = PhysicsServer.Service.CaptureResultCollider(body, localShape, bodyOnly: true, contactResult: true);
        var second = PhysicsServer.Service.CaptureResultCollider(collider, colliderShape, bodyOnly: true, contactResult: true);
        if (first.Space is null || !ReferenceEquals(first.Space, second.Space))
            throw new ArgumentException("Body contact identities require one live shared physics world.", nameof(collider));
        BodyRID = body; BodyAttachment = first.Attachment; LocalShape = localShape; ColliderRID = collider; ColliderShape = colliderShape; Identity = second.Identity;
        LocalPosition = localPosition; ColliderPosition = colliderPosition; Normal = normal; LocalVelocity = localVelocity;
        ColliderVelocity = colliderVelocity; Impulse = impulse;
        ColliderOwner = second.Scene?.Backend.SceneOwnerReference;
    }
    internal PhysicsBodyContact(RID body, long attachment, in PhysicsDirectBodyState.Contact contact)
    {
        Depth = contact.Depth; BodyRID = body; BodyAttachment = attachment; LocalShape = contact.LocalShape;
        ColliderRID = contact.Collider; ColliderShape = contact.ColliderShape; Identity = contact.Identity; ColliderOwner = contact.ColliderOwner;
        LocalPosition = contact.LocalPoint; ColliderPosition = contact.ColliderPoint; Normal = contact.Normal;
        LocalVelocity = contact.LocalVelocity; ColliderVelocity = contact.ColliderVelocity; Impulse = contact.Impulse;
    }
    /// <summary>Gets sampled signed penetration depth in scene units.</summary>
    /// <value>Positive for penetration, negative for separation; an explicit frame sample may retain the greatest interval depth.</value>
    public float Depth { get; }
    /// <summary>Gets the observed body identity.</summary>
    /// <value>The sampled physical RID.</value>
    public RID BodyRID { get; }
    /// <summary>Gets the observed body's sampled logical shape index.</summary>
    /// <value>The index at capture; later structural edits may reindex slots.</value>
    public int LocalShape { get; }
    /// <summary>Gets the other body's physical identity.</summary>
    /// <value>The sampled RID, retained after collider release.</value>
    public RID ColliderRID { get; }
    /// <summary>Gets the other body's sampled logical shape index.</summary>
    /// <value>The index at capture; later structural edits may reindex slots.</value>
    public int ColliderShape { get; }
    /// <summary>Gets the sampled collider object association ID.</summary>
    /// <value>The sampled ID, or zero when unassigned.</value>
    public ulong ColliderID => Identity.ID;
    /// <summary>Gets the live weak object association sampled at capture.</summary>
    /// <value>The sampled object, or null after disposal/collection or when unassigned.</value>
    public ElectronObject? ColliderObject => Identity.Target;
    internal ObjectIdentity Identity { get; }
    internal WeakReference<CollisionObject>? ColliderOwner { get; }
    internal long BodyAttachment { get; }
    /// <summary>Gets this body's global contact position.</summary>
    /// <value>The finite point in scene units.</value>
    public Vector2 LocalPosition { get; }
    /// <summary>Gets the other body's global contact position.</summary>
    /// <value>The finite point in scene units.</value>
    public Vector2 ColliderPosition { get; }
    /// <summary>Gets the global contact normal pointing away from the collider.</summary>
    /// <value>The finite normal supplied by the contact implementation.</value>
    public Vector2 Normal { get; }
    /// <summary>Gets this body's global contact-point velocity.</summary>
    /// <value>The finite velocity in scene units per second.</value>
    public Vector2 LocalVelocity { get; }
    /// <summary>Gets the other body's global contact-point velocity.</summary>
    /// <value>The finite velocity in scene units per second.</value>
    public Vector2 ColliderVelocity { get; }
    /// <summary>Gets the impulse applied to this body across the completed step.</summary>
    /// <value>The finite global impulse in scene units times kilograms per second.</value>
    public Vector2 Impulse { get; }
}
