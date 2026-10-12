namespace Electron2D;

/// <summary>Defines the complete selected implementation boundary using logical identities and engine-unit values.</summary>
/// <remarks>Numerical hooks borrow their selected world. Body authoring through the server facade or direct-body
/// views rejects before mutation during those hooks. Joint clear, connection replacement, paths, geometry and parameters use the same guard,
/// including initial attachment. Guarded reads remain available. Caller-created body-state
/// extensions may forward an ordinary requested state write to their inner view outside a numerical hook.</remarks>
public abstract class PhysicsServerBackend : ElectronObject
{
    private int _claimed;
    private RID _space;
    private readonly object _lifetime = new();
    private bool _release;
    internal void Claim(RID space)
    {
        lock (_lifetime)
        {
            ThrowIfDisposed();
            if (_claimed != 0) throw new InvalidOperationException("A physics implementation can own only one immutable world.");
            _claimed = 1; _space = space;
        }
    }
    /// <summary>Gets the immutable engine-assigned world identity during implementation hooks.</summary>
    /// <value>A live space after factory binding; invalid before binding.</value>
    protected RID SpaceRID => _space;
    /// <summary>Gets whether this world's completed physics notifications are being delivered.</summary>
    /// <value>True during body force/integration/state and sleep callbacks, selected query flushing,
    /// and contact/Area notification delivery; false when this world has no active delivery phase.</value>
    /// <remarks>This shared lifecycle read does not inspect numerical state or allocate. It remains available
    /// during terminal-error notification cleanup. Nested queries retain an ongoing delivery phase;
    /// nested other-world delivery has independent status.
    /// Normal completion and callback errors clear the delivery phase.</remarks>
    /// <exception cref="InvalidOperationException">The implementation is unbound or called off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The implementation or its world is disposed.</exception>
    public bool IsFlushingQueries
    {
        get
        {
            ThrowIfDisposed();
            if (!_space.IsValid()) throw new InvalidOperationException("The physics implementation is not bound to a world.");
            return PhysicsServer.SpaceIsFlushingQueries(_space);
        }
    }
    /// <summary>Tests whether a RID is excluded from this world's current synchronous body-motion test.</summary>
    /// <param name="body">The identity to test for exact membership, including inert or foreign RIDs.</param>
    /// <returns>True when listed in the active motion test's body exclusions; false outside a motion test.</returns>
    /// <remarks>This reads authored inputs without observing numerical state or allocating a copy.
    /// Nested ordinary queries retain the motion scope; recursive motion tests in the same world reject.
    /// Normal return and exceptions clear the scope. Calls require this implementation's owner thread.</remarks>
    /// <exception cref="InvalidOperationException">The implementation is unbound, called off-owner, or its world has failed outside a numerical hook.</exception>
    /// <exception cref="ObjectDisposedException">The implementation or its world is disposed.</exception>
    public bool IsBodyExcludedFromMotionTest(RID body)
    {
        ThrowIfDisposed();
        if (!_space.IsValid()) throw new InvalidOperationException("The physics implementation is not bound to a world.");
        return PhysicsServer.Service.GetSceneSpace(_space).IsBodyExcludedFromMotionTest(body);
    }
    /// <summary>Tests whether an object identity is excluded from this world's current synchronous body-motion test.</summary>
    /// <param name="objectID">The object identity to test for exact membership. No object resolution occurs.</param>
    /// <returns>True when listed in the active motion test's object exclusions; false outside a motion test.</returns>
    /// <remarks>This reads authored inputs without numerical observation or allocation. The scope and lifetime
    /// guards are the same as <see cref="IsBodyExcludedFromMotionTest"/>.</remarks>
    /// <exception cref="InvalidOperationException">The implementation is unbound, called off-owner, or its world has failed outside a numerical hook.</exception>
    /// <exception cref="ObjectDisposedException">The implementation or its world is disposed.</exception>
    public bool IsObjectExcludedFromMotionTest(ulong objectID)
    {
        ThrowIfDisposed();
        if (!_space.IsValid()) throw new InvalidOperationException("The physics implementation is not bound to a world.");
        return PhysicsServer.Service.GetSceneSpace(_space).IsObjectExcludedFromMotionTest(objectID);
    }
    /// <summary>Returns the body's retained explicit and active-joint collision exceptions.</summary>
    /// <param name="body">A live scene or server body in this world, or a detached body.</param>
    /// <returns>A caller-owned deduplicated RID array, with explicit entries in insertion order before active joint targets. Empty and retired target RIDs remain inert entries.</returns>
    /// <remarks>This input read is available during numerical hooks, including solving, without observing numerical state.
    /// Each call creates a snapshot; retain supplied exception-pair spans or implementation-owned buffers for warmed solver work.</remarks>
    /// <exception cref="ArgumentException">The owner is not a live body or belongs to another world.</exception>
    /// <exception cref="InvalidOperationException">The implementation is unbound, called off-owner, or its world has failed outside a numerical hook.</exception>
    /// <exception cref="ObjectDisposedException">The implementation or its world is disposed.</exception>
    protected RID[] GetCollisionExceptions(RID body)
    {
        ThrowIfDisposed();
        if (!_space.IsValid()) throw new InvalidOperationException("The physics implementation is not bound to a world.");
        return PhysicsServer.Service.GetBodyCollisionExceptions(body, PhysicsServer.Service.GetSceneSpace(_space));
    }
    /// <summary>Reads immutable current authored geometry without cloning its shape resource.</summary>
    /// <param name="shape">A live shape RID; the resource need not be attached to this world.</param>
    /// <returns>The cached exact local geometry, valid independently of later edits or resource disposal.</returns>
    /// <remarks>Available during numerical hooks without querying solver state. A procedural supplier runs
    /// under world borrowing and must not mutate authoring or lifetime. Engine reads reuse cached data;
    /// supplier code controls allocations on the first demand after an edit.</remarks>
    /// <exception cref="ArgumentException">The RID is not a live shape.</exception>
    /// <exception cref="InvalidOperationException">The implementation is unbound, access is forbidden, or geometry evaluation fails.</exception>
    /// <exception cref="ObjectDisposedException">The implementation, world or shape is disposed.</exception>
    protected ShapeGeometry GetShapeGeometry(RID shape)
    {
        ThrowIfDisposed();
        if (!_space.IsValid()) throw new InvalidOperationException("The physics implementation is not bound to a world.");
        var space = PhysicsServer.Service.GetSceneSpace(_space);
        space.EnsureImplementationInputReadAccess();
        space.BeginImplementationCallback();
        try { return PhysicsServer.Service.GetShapeGeometry(shape).GetGeometryData(); }
        finally { space.EndImplementationCallback(); }
    }
    /// <summary>Rejects caller disposal while this implementation belongs to its world.</summary>
    protected sealed override void ValidateDisposal()
    {
        lock (_lifetime)
            if (_claimed > 0 && !_release) throw new InvalidOperationException("The world owns this physics implementation; release the world instead.");
            else if (_claimed == 0) _claimed = -1;
        base.ValidateDisposal();
    }
    internal void ReleaseImplementation()
    {
        lock (_lifetime) _release = true;
        Dispose();
    }
    /// <summary>Supplies the selected implementation operation: has failure.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract bool HasFailure { get; }
    /// <summary>Supplies the selected implementation operation: results ready.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract bool ResultsReady { get; }
    /// <summary>Supplies the selected implementation operation: prepared body capacity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract int PreparedBodyCapacity { get; }
    /// <summary>Supplies the selected implementation operation: initialize.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void Initialize();
    /// <summary>Supplies the selected implementation operation: set active.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetActive(bool active);
    /// <summary>Supplies the selected implementation operation: prepare checkpoint.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void PrepareCheckpoint();
    /// <summary>Supplies the selected implementation operation: prepare portable capture.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void PreparePortableCapture();
    /// <summary>Supplies the selected implementation operation: read portable bodies.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ReadPortableBodies(ReadOnlySpan<RID> colliders, bool capture);
    /// <summary>Supplies the selected implementation operation: invalidate collision exceptions.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void InvalidateCollisionExceptions(RID collider);
    /// <summary>Supplies the current world's deduplicated explicit and active-joint exclusion pairs.</summary>
    /// <remarks>Pairs contain only live attachments in this world. Either direction suppresses solid response and body motion;
    /// Area sensing and ordinary direct-space queries retain their own filtering. The span is borrowed for this call only.
    /// Use the scoped exception getter when retained ordered or inert authoring entries are required.</remarks>
    protected internal abstract void SetCollisionExceptions(ReadOnlySpan<(RID From, RID To)> pairs);
    /// <summary>Supplies the selected implementation operation: synchronize collision exceptions.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SynchronizeCollisionExceptions();
    /// <summary>Supplies the selected implementation operation: create checkpoint.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract PhysicsBackendCheckpoint CreateCheckpoint();
    /// <summary>Supplies the selected implementation operation: begin portable restore.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void BeginPortableRestore();
    /// <summary>Supplies the selected implementation operation: complete portable restore.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void CompletePortableRestore(ReadOnlySpan<PhysicsBackendOneWay> pairs, ulong tick);
    /// <summary>Supplies the selected implementation operation: get portable one way count.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    /// <returns>The current nonnegative maximum number of episodes supplied by the next capture call.</returns>
    protected internal abstract int GetPortableOneWayCount();
    /// <summary>Supplies the selected implementation operation: capture portable one ways.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    /// <param name="destination">Borrowed storage bounded by the current declared count; retained storage beyond this span is unavailable.</param>
    /// <returns>The number written, at most the supplied length. Each pair requires different bound colliders and enabled valid shape/piece identities; duplicate reversed pairs reject.</returns>
    protected internal abstract int CapturePortableOneWays(Span<PhysicsBackendOneWay> destination);
    /// <summary>Supplies the selected implementation operation: read portable sleep time.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract float ReadPortableSleepTime(RID collider);
    /// <summary>Supplies the selected implementation operation: apply portable sleep.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ApplyPortableSleep(RID collider, float time, bool canSleep, bool sleeping);
    /// <summary>Supplies the selected implementation operation: read debug contacts.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract int ReadDebugContacts(Span<Vector2> output);
    /// <summary>Supplies the selected implementation operation: set sleep settings.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetSleepSettings(in PhysicsSleepSettings settings);
    /// <summary>Supplies the selected implementation operation: set contact settings.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetContactSettings(in PhysicsContactSettings settings);
    /// <summary>Supplies the selected implementation operation: set solver iterations.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetSolverIterations(int value);
    /// <summary>Supplies the selected implementation operation: set constraint default bias.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SetConstraintDefaultBias(float value);
    /// <summary>Supplies the selected implementation operation: prepare solver capacity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void PrepareSolverCapacity(int bodies, int shapes, int joints);
    /// <summary>Supplies the selected implementation operation: prepare monitoring capacity.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void PrepareMonitoringCapacity(int bodies, int shapes);
    /// <summary>Supplies the selected implementation operation: read statistics.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract PhysicsBackendStatistics ReadStatistics();
    /// <summary>Supplies the selected implementation operation: create collider.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract PhysicsBackendCollider CreateCollider(RID collider, bool area);
    /// <summary>Supplies the selected implementation operation: create joint.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract PhysicsBackendJoint CreateJoint(RID joint);
    /// <summary>Supplies the selected implementation operation: create direct space state.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal virtual PhysicsDirectSpaceState CreateDirectSpaceState(RID space) => new(space);
    /// <summary>Supplies the selected implementation operation: create direct body state.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal virtual PhysicsDirectBodyState CreateDirectBodyState(RID body) => new(PhysicsServer.Service.BodyRuntime(body), PhysicsServer.Service.GetSceneSpace(_space));
    /// <summary>Supplies the selected implementation operation: begin step.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void BeginStep(double delta);
    /// <summary>Supplies the selected implementation operation: prepare world.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void PrepareWorld(double delta, in PhysicsAreaFieldConfiguration fields);
    /// <summary>Supplies the selected implementation operation: prepare bodies.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void PrepareBodies(double delta);
    /// <summary>Supplies the selected implementation operation: solve.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void Solve(double delta);
    /// <summary>Supplies the selected implementation operation: sync results.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void SyncResults();
    /// <summary>Supplies the selected implementation operation: complete body.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void CompleteBody(RID body);
    /// <summary>Supplies the selected implementation operation: collect contacts.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void CollectContacts();
    /// <summary>Supplies the selected implementation operation: scan areas.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void ScanAreas();
    /// <summary>Supplies the selected implementation operation: get area overlap count.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract int GetAreaOverlapCount(RID area);
    /// <summary>Supplies the selected implementation operation: read area overlaps.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract int ReadAreaOverlaps(RID area, Span<PhysicsBackendOverlap> output);
    /// <summary>Supplies the selected implementation operation: area contains point.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract bool AreaContainsPoint(RID area, Vector2 point, uint mask);
    /// <summary>Supplies the selected implementation operation: end step.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void EndStep(Exception? failure);
    /// <summary>Supplies the selected implementation operation: flush queries.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void FlushQueries();
    /// <summary>Supplies the selected implementation operation: end sync.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void EndSync(Exception? failure);
    /// <summary>Supplies the selected implementation operation: intersect ray.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, ReadOnlySpan<RID> excluded, bool collideWithAreas, bool collideWithBodies, bool hitFromInside);
    /// <summary>Supplies the selected implementation operation: collect point hits.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters, ReadOnlySpan<RID> excluded);
    /// <summary>Supplies the selected implementation operation: collect shape hits.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded);
    /// <summary>Supplies the selected implementation operation: cast motion.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded);
    /// <summary>Supplies the selected implementation operation: collect shape contacts.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract List<PhysicsBackendShapeContact> CollectShapeContacts(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded, int limit);
    /// <summary>Supplies the selected implementation operation: get rest info.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded);
    /// <summary>Supplies the selected implementation operation: collide shapes.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract bool CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count);
    /// <summary>Supplies the selected implementation operation: body collide shape.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion, Span<Vector2> contacts, out int count);
    /// <summary>Supplies the selected implementation operation: test body motion.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.
    /// Collision output must retain the requested moving body's identity. The engine validates that identity
    /// before replacing caller output; a result constructed for another live body is rejected.</remarks>
    protected internal abstract bool TestBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision, ReadOnlySpan<RID> excludedBodies, ReadOnlySpan<ulong> excludedObjects, bool collideSeparationRay, PhysicsTestMotionResult result);
    /// <summary>Supplies the selected implementation operation: finish.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void Finish();
}
