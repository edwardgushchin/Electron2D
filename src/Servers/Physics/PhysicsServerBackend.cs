namespace Electron2D;

/// <summary>Defines the complete selected implementation boundary using logical identities and engine-unit values.</summary>
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
    /// <summary>Supplies the selected implementation operation: set collision exceptions.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
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
    protected internal abstract int GetPortableOneWayCount();
    /// <summary>Supplies the selected implementation operation: capture portable one ways.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
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
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract bool TestBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision, ReadOnlySpan<RID> excludedBodies, ReadOnlySpan<ulong> excludedObjects, bool collideSeparationRay, PhysicsTestMotionResult result);
    /// <summary>Supplies the selected implementation operation: finish.</summary>
    /// <remarks>Invoked by the owning world under its lifetime and owner-thread guard. Values use engine units; spans are borrowed for this call only.</remarks>
    protected internal abstract void Finish();
}
