namespace Electron2D;

/// <summary>Defines the complete selected implementation boundary using logical identities and engine-unit values.</summary>
internal abstract class PhysicsBackendContract
{
    private int _claimed;
    private RID _space;
    internal void Claim(RID space) { if (Interlocked.CompareExchange(ref _claimed, 1, 0) != 0) throw new InvalidOperationException("A physics implementation can own only one immutable world."); _space = space; }
    internal abstract bool HasFailure { get; }
    internal abstract bool ResultsReady { get; }
    internal abstract int PreparedBodyCapacity { get; }
    internal abstract void Initialize();
    internal abstract void SetActive(bool active);
    internal abstract void PrepareCheckpoint();
    internal abstract void PreparePortableCapture();
    internal abstract void ReadPortableBodies(ReadOnlySpan<RID> colliders, bool capture);
    internal abstract void InvalidateCollisionExceptions(RID collider);
    internal abstract void SetCollisionExceptions(ReadOnlySpan<(RID From, RID To)> pairs);
    internal abstract void SynchronizeCollisionExceptions();
    internal abstract PhysicsBackendCheckpoint CreateCheckpoint();
    internal abstract void BeginPortableRestore();
    internal abstract void CompletePortableRestore(ReadOnlySpan<PhysicsBackendOneWay> pairs, ulong tick);
    internal abstract int GetPortableOneWayCount();
    internal abstract int CapturePortableOneWays(Span<PhysicsBackendOneWay> destination);
    internal abstract float ReadPortableSleepTime(RID collider);
    internal abstract void ApplyPortableSleep(RID collider, float time, bool canSleep, bool sleeping);
    internal abstract int ReadDebugContacts(Span<Vector2> output);
    internal abstract void SetSleepSettings(in PhysicsSleepSettings settings);
    internal abstract void SetContactSettings(in PhysicsContactSettings settings);
    internal abstract void SetSolverIterations(int value);
    internal abstract void SetConstraintDefaultBias(float value);
    internal abstract void PrepareSolverCapacity(int bodies, int shapes, int joints);
    internal abstract void PrepareMonitoringCapacity(int bodies, int shapes);
    internal abstract PhysicsBackendStatistics ReadStatistics();
    internal abstract PhysicsBackendCollider CreateCollider(RID collider, bool area);
    internal abstract PhysicsBackendJoint CreateJoint(RID joint);
    internal virtual PhysicsDirectSpaceState CreateDirectSpaceState(RID space) => new(space);
    internal virtual PhysicsDirectBodyState CreateDirectBodyState(RID body) => new(PhysicsServer.Service.BodyRuntime(body), PhysicsServer.Service.GetSceneSpace(_space));
    internal abstract void BeginStep(double delta);
    internal abstract void PrepareWorld(double delta, in PhysicsAreaFieldConfiguration fields);
    internal abstract void PrepareBodies(double delta);
    internal abstract void Solve(double delta);
    internal abstract void SyncResults();
    internal abstract void CompleteBody(RID body);
    internal abstract void CollectContacts();
    internal abstract void ScanAreas();
    internal abstract int GetAreaOverlapCount(RID area);
    internal abstract int ReadAreaOverlaps(RID area, Span<PhysicsBackendOverlap> output);
    internal abstract bool AreaContainsPoint(RID area, Vector2 point, uint mask);
    internal abstract void EndStep(Exception? failure);
    internal abstract void FlushQueries();
    internal abstract void EndSync(Exception? failure);
    internal abstract PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, ReadOnlySpan<RID> excluded, bool collideWithAreas, bool collideWithBodies, bool hitFromInside);
    internal abstract List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters);
    internal abstract List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters);
    internal abstract (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters);
    internal abstract List<PhysicsBackendShapeContact> CollectShapeContacts(PhysicsShapeQueryParameters parameters, int limit);
    internal abstract PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters);
    internal abstract bool CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count);
    internal abstract bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion, Span<Vector2> contacts, out int count);
    internal abstract bool TestBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision, ReadOnlySpan<RID> excludedBodies, ReadOnlySpan<ulong> excludedObjects, bool collideSeparationRay, PhysicsTestMotionResult result);
    internal abstract void Finish();
}
