namespace Electron2D;

internal readonly record struct MotionResultData(
    RID OwnerRID, RID ColliderRID, ulong ColliderID, int LocalShape, int ColliderShape,
    Vector2 Point, Vector2 Normal, float Depth, Vector2 ColliderVelocity,
    Vector2 Travel, Vector2 Remainder, float SafeFraction, float UnsafeFraction, bool Collided, ObjectIdentity ColliderIdentity = default)
{
    internal object? LocalSlotIdentity { get; init; }
    internal object? OtherSlotIdentity { get; init; }
}

/// <summary>Configures a body motion test in a registered two-dimensional physics space.</summary>
public sealed class PhysicsTestMotionParameters : ElectronObject
{
    private Transform _from = Transform.Identity;
    private Vector2 _motion;
    private float _margin = 0.08f;
    private bool _recoveryAsCollision;
    private bool _collideSeparationRay;
    private RID[] _excludeBodies = [];
    private ulong[] _excludeObjects = [];

    internal RID[] ExcludedBodies => _excludeBodies;
    internal ulong[] ExcludedObjects => _excludeObjects;

    /// <summary>Creates identity-pose, zero-motion parameters with a 0.08-unit recovery margin.</summary>
    public PhysicsTestMotionParameters() { }

    /// <summary>Gets or sets the finite unit-scale global pose at which testing begins.</summary>
    /// <remarks>One-way initial-overlap departure and new impact admission are distinct: new contacts use their actual surface side.</remarks>
    /// <value>Identity by default.</value>
    public Transform From
    {
        get { ThrowIfDisposed(); return _from; }
        set
        {
            ThrowIfDisposed();
            if (!value.IsFinite() || !value.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(value.Skew))
                throw new ArgumentException("Body motion requires finite translation, unit scale and zero skew.", nameof(value));
            _from = value;
        }
    }

    /// <summary>Gets or sets the finite global displacement to test.</summary>
    /// <value>Zero by default.</value>
    public Vector2 Motion
    {
        get { ThrowIfDisposed(); return _motion; }
        set
        {
            ThrowIfDisposed();
            if (!value.IsFinite() || !float.IsFinite(value.Length()))
                throw new ArgumentOutOfRangeException(nameof(value));
            _motion = value;
        }
    }

    /// <summary>Gets or sets the nonnegative finite recovery margin in scene units.</summary>
    /// <value>0.08 by default.</value>
    public float Margin
    {
        get { ThrowIfDisposed(); return _margin; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _margin = value;
        }
    }

    /// <summary>Gets or sets whether recovery may report a contact remaining at the final tested pose.</summary>
    /// <value>False by default.</value>
    /// <remarks>One-way pairs rejected on the last recovery pass remain excluded from the sweep and final contact sample.</remarks>
    public bool RecoveryAsCollision
    {
        get { ThrowIfDisposed(); return _recoveryAsCollision; }
        set { ThrowIfDisposed(); _recoveryAsCollision = value; }
    }

    /// <summary>Gets or sets whether non-sliding separation rays can stop the requested motion.</summary>
    /// <value>False by default; ray recovery always remains enabled.</value>
    public bool CollideSeparationRay
    {
        get { ThrowIfDisposed(); return _collideSeparationRay; }
        set { ThrowIfDisposed(); _collideSeparationRay = value; }
    }

    /// <summary>Gets or sets copied body RID exclusions.</summary>
    /// <value>Empty by default.</value>
    public RID[] ExcludeBodies
    {
        get { ThrowIfDisposed(); return (RID[])_excludeBodies.Clone(); }
        set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _excludeBodies = (RID[])value.Clone(); }
    }

    /// <summary>Gets or sets copied object association instance-ID exclusions.</summary>
    /// <value>Empty by default. Unassigned bodies have no object instance ID.</value>
    public ulong[] ExcludeObjects
    {
        get { ThrowIfDisposed(); return (ulong[])_excludeObjects.Clone(); }
        set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _excludeObjects = (ulong[])value.Clone(); }
    }
}

/// <summary>Contains the completed values of a server body motion test.</summary>
/// <remarks>Public construction creates caller-owned storage. A selected implementation hook instead receives borrowed
/// library storage usable only during that hook on its owner thread; do not retain or dispose it. World retirement releases it.</remarks>
public sealed class PhysicsTestMotionResult : ElectronObject
{
    private MotionResultData _data;
    private readonly bool _implementationOwned;
    private int _implementationThread, _releaseThread;
    internal MotionResultData Data { get { EnsureResultAccess(); return _data; } }
    internal PhysicsTestMotionResult(bool implementationOwned) => _implementationOwned = implementationOwned;
    internal void BeginImplementationUse()
    {
        ThrowIfDisposed();
        if (!_implementationOwned || _implementationThread != 0) throw new InvalidOperationException("Motion output borrowing is unbalanced.");
        Volatile.Write(ref _implementationThread, Environment.CurrentManagedThreadId);
    }
    internal void EndImplementationUse() { _data = default; Volatile.Write(ref _implementationThread, 0); }
    internal void ReleaseImplementationResult()
    {
        Volatile.Write(ref _releaseThread, Environment.CurrentManagedThreadId);
        try { Dispose(); }
        finally { Volatile.Write(ref _releaseThread, 0); }
    }
    private void EnsureResultAccess()
    {
        ThrowIfDisposed();
        if (_implementationOwned && Volatile.Read(ref _implementationThread) != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Borrowed motion output requires its active implementation hook on the owning thread.");
    }
    /// <summary>Preserves the lifetime of motion output owned by a selected physics implementation.</summary>
    /// <exception cref="InvalidOperationException">The physics implementation owns this result and has not authorized its retirement.</exception>
    protected override void ValidateDisposal()
    {
        if (_implementationOwned && Volatile.Read(ref _releaseThread) != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("A borrowed motion result cannot be disposed by its consumer.");
        base.ValidateDisposal();
    }

    /// <summary>Creates empty caller-owned output for a motion test or explicit result filling.</summary>
    public PhysicsTestMotionResult() { }

    internal void Set(in MotionResultData data) { EnsureResultAccess(); _data = data; }

    /// <summary>Replaces this result with an unobstructed completed motion.</summary>
    /// <param name="travel">The completed global displacement, including any recovery, in scene units.</param>
    /// <remarks>Clears prior collision identity and geometry, sets the remainder to zero and both fractions to one.
    /// This method does not execute a motion test and reuses the result's existing storage.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Travel is not finite.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    public void SetMotion(Vector2 travel)
    {
        EnsureResultAccess();
        if (!travel.IsFinite()) throw new ArgumentOutOfRangeException(nameof(travel));
        _data = new(default, default, 0, 0, 0, default, default, 0, default, travel, default, 1, 1, false);
    }

    /// <summary>Replaces this result with validated geometry and identity supplied by a motion-query implementation.</summary>
    /// <param name="body">The live moving body's RID.</param>
    /// <param name="localShape">The moving body's logical shape index.</param>
    /// <param name="collider">The live body collider's RID in the same space.</param>
    /// <param name="colliderShape">The collider's logical shape index.</param>
    /// <param name="point">The global collision point in scene units.</param>
    /// <param name="normal">The finite global separation normal.</param>
    /// <param name="depth">Nonnegative penetration depth in scene units.</param>
    /// <param name="colliderVelocity">Global collider point velocity in scene units per second.</param>
    /// <param name="travel">Completed global displacement including recovery, in scene units.</param>
    /// <param name="remainder">Remaining requested displacement in scene units.</param>
    /// <param name="safeFraction">A safe motion fraction from zero through one.</param>
    /// <param name="unsafeFraction">An unsafe fraction from safeFraction through one.</param>
    /// <remarks>This method does not execute a motion test. Both bodies follow their owner-thread,
    /// solver and failed-world guards. Identity is sampled once from the collider's current association;
    /// later rebind/disposal does not retarget this result. Validation precedes replacement and warmed calls allocate no managed storage.
    /// The supplied moving-body identity is preserved; selected motion-query output must use its requested body.</remarks>
    /// <exception cref="ArgumentException">An RID is not a live body, or the moving body is its own collider.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A shape index, vector, depth or fraction is invalid.</exception>
    /// <exception cref="InvalidOperationException">Bodies do not share a live space, access violates its guard, or borrowed output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result or a selected shape has been disposed.</exception>
    public void SetCollision(RID body, int localShape, RID collider, int colliderShape, Vector2 point,
        Vector2 normal, float depth, Vector2 colliderVelocity, Vector2 travel, Vector2 remainder,
        float safeFraction, float unsafeFraction)
    {
        EnsureResultAccess();
        if (!point.IsFinite()) throw new ArgumentOutOfRangeException(nameof(point));
        if (!normal.IsFinite()) throw new ArgumentOutOfRangeException(nameof(normal));
        if (!colliderVelocity.IsFinite()) throw new ArgumentOutOfRangeException(nameof(colliderVelocity));
        if (!travel.IsFinite()) throw new ArgumentOutOfRangeException(nameof(travel));
        if (!remainder.IsFinite()) throw new ArgumentOutOfRangeException(nameof(remainder));
        if (!float.IsFinite(depth) || depth < 0) throw new ArgumentOutOfRangeException(nameof(depth));
        if (!float.IsFinite(safeFraction) || safeFraction < 0 || safeFraction > 1) throw new ArgumentOutOfRangeException(nameof(safeFraction));
        if (!float.IsFinite(unsafeFraction) || unsafeFraction < safeFraction || unsafeFraction > 1) throw new ArgumentOutOfRangeException(nameof(unsafeFraction));
        if (body == collider) throw new ArgumentException("A moving body cannot be its own motion collider.", nameof(collider));
        var local = PhysicsServer.Service.CaptureResultCollider(body, localShape, bodyOnly: true);
        var remote = PhysicsServer.Service.CaptureResultCollider(collider, colliderShape, bodyOnly: true);
        if (local.Space is null || !ReferenceEquals(local.Space, remote.Space))
            throw new InvalidOperationException("Motion result bodies must share a live physics space.");
        _data = new(body, collider, remote.Identity.ID, localShape, colliderShape, point, normal, depth,
            colliderVelocity, travel, remainder, safeFraction, unsafeFraction, true, remote.Identity);
    }

    /// <summary>Returns the live object association captured by the motion test.</summary>
    /// <returns>The borrowed instance, including a generated tile-layer owner, or null when unassigned, disposed or collected.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public ElectronObject? GetCollider() { return Data.ColliderIdentity.Target; }

    /// <summary>Returns the sampled object association ID, including server-bound instances.</summary>
    /// <returns>The sampled object instance ID or zero.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public ulong GetColliderID() { return Data.ColliderID; }

    /// <summary>Returns the sampled collider RID.</summary>
    /// <returns>An empty RID when no collision occurred.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public RID GetColliderRID() { return Data.ColliderRID; }

    /// <summary>Returns the collider's direct shape-owner index.</summary>
    /// <returns>The index, or zero before a collision.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public int GetColliderShape() { return Data.ColliderShape; }

    /// <summary>Returns collider velocity at the contact point in scene units per second.</summary>
    /// <returns>Zero when no collision occurred.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public Vector2 GetColliderVelocity() { return Data.ColliderVelocity; }

    /// <summary>Returns margin-inclusive penetration depth at the recovered unsafe pose in scene units.</summary>
    /// <returns>Zero when no collision occurred.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public float GetCollisionDepth() { return Data.Depth; }

    /// <summary>Returns the moving body's direct shape-owner index.</summary>
    /// <returns>The index, or zero when no collision occurred.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public int GetCollisionLocalShape() { return Data.LocalShape; }

    /// <summary>Returns the global contact normal pointing away from the collider.</summary>
    /// <returns>Zero when no collision occurred.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public Vector2 GetCollisionNormal() { return Data.Normal; }

    /// <summary>Returns the global collider point sampled at the recovered unsafe pose.</summary>
    /// <returns>Zero when no collision occurred.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public Vector2 GetCollisionPoint() { return Data.Point; }

    /// <summary>Returns the safe motion fraction before first impact.</summary>
    /// <returns>One on a miss.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public float GetCollisionSafeFraction() { return Data.SafeFraction; }

    /// <summary>Returns the unsafe motion fraction at first impact.</summary>
    /// <returns>One on a miss.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public float GetCollisionUnsafeFraction() { return Data.UnsafeFraction; }

    /// <summary>Returns untraveled requested displacement.</summary>
    /// <returns>Zero on an unobstructed motion.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public Vector2 GetRemainder() { return Data.Remainder; }

    /// <summary>Returns displacement including recovery before first impact.</summary>
    /// <returns>The requested motion plus recovery on a miss.</returns>
    /// <exception cref="InvalidOperationException">Borrowed implementation output is used outside its owning active hook.</exception>
    /// <exception cref="ObjectDisposedException">The result has been disposed.</exception>
    public Vector2 GetTravel() { return Data.Travel; }
}
