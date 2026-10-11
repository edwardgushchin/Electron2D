using Box2D.NET;
using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private sealed class ViewBackend : PhysicsWorldBackend
    {
        private readonly PhysicsWorldBackend _inner;
        internal PhysicsColliderImplementation NativeCollider(PhysicsColliderBackend collider) => Kind == PhysicsServer.Backend.CPU ?
            ((CPUPhysicsWorldBackend)_inner).NativeCollider(collider) : _inner.GPUStore!.NativeCollider(collider);
        internal Func<PhysicsDirectSpaceState>? SpaceFactory;
        internal Func<PhysicsBodyRuntime, PhysicsDirectBodyState>? BodyFactory;
        internal int SpaceCreations, BodyCreations;
        internal ShapeInputHook? ShapeHook;
        internal float ImpulseScale = 1;
        internal PairQueryHook? PairHook;
        internal int ShapePairQueries, BodyPairQueries;
        internal Action? InitializeHook, FlushHook;
        internal Action<bool>? ActiveHook;
        internal Action<Exception?>? EndSyncHook;
        internal Action? CheckpointPreparationHook;
        internal int CheckpointPreparations;
        internal Action? CheckpointCaptureHook, CheckpointValidationHook, CheckpointRestoreHook, CheckpointReleaseHook;
        internal int CheckpointCaptures, CheckpointValidations, CheckpointRestores, CheckpointReleases;
        internal int PortablePreparations, PortableReads;
        internal Action? PortablePreparationHook, PortableReadHook;
        internal int Initializations, Activations, Flushes, EndSynchronizations;
        internal override void Initialize() { Initializations++; InitializeHook?.Invoke(); _inner.Initialize(); }
        internal override void ApplyActive(bool active) { Activations++; ActiveHook?.Invoke(active); _inner.SetActive(active); }
        internal override void FlushQueries() { Flushes++; FlushHook?.Invoke(); base.FlushQueries(); }
        internal override void EndSync(Exception? failure) { EndSynchronizations++; EndSyncHook?.Invoke(failure); base.EndSync(failure); }
        internal ViewBackend(PhysicsSpace space, PhysicsServer.Backend backend) : base(space, backend) => _inner = PhysicsWorldBackend.Create(space, backend, false);
        internal override PhysicsDirectSpaceState CreateDirectSpaceState() { SpaceCreations++; return SpaceFactory is null ? base.CreateDirectSpaceState() : SpaceFactory(); }
        internal override PhysicsDirectBodyState CreateDirectBodyState(PhysicsBodyRuntime body) { BodyCreations++; return BodyFactory is null ? base.CreateDirectBodyState(body) : BodyFactory(body); }
        internal override void PrepareInterval() => _inner.PrepareInterval();
        internal override void StepNative(float delta, int substeps) => _inner.StepNative(delta, substeps);
        internal override GPUPhysicsWorld EnableGPUIntegration() => _inner.EnableGPUIntegration();
        internal override GPUPhysicsWorld EnableGPUSolver() => _inner.EnableGPUSolver();
        internal override GPUPhysicsBodyStore? GPUStore => _inner.GPUStore;
        internal override B2WorldId WorldID => _inner.WorldID;
        internal override PhysicsTaskScheduler Tasks => _inner.Tasks;
        internal override GPUPhysicsWorld? StageGPU => _inner.StageGPU;
        internal override bool HasFailure => _inner.HasFailure;
        internal override PhysicsServer.Backend Kind => _inner.Kind;
        internal override int PreparedBodyCapacity => _inner.PreparedBodyCapacity;
        internal override bool ResultsReady => _inner.ResultsReady;
        internal override void PrepareCheckpoint() { CheckpointPreparations++; CheckpointPreparationHook?.Invoke(); _inner.PrepareCheckpoint(); }
        internal override void PreparePortableCapture() { PortablePreparations++; PortablePreparationHook?.Invoke(); _inner.PreparePortableCapture(); }
        internal override void ReadPortableBodies(ReadOnlySpan<PhysicsColliderBackend> colliders, bool capture)
        { PortableReads++; PortableReadHook?.Invoke(); _inner.ReadPortableBodies(colliders, capture); }
        internal override void InvalidateCollisionExceptions(PhysicsColliderBackend collider) => _inner.InvalidateCollisionExceptions(collider);
        internal override void SynchronizeCollisionExceptions() => _inner.SynchronizeCollisionExceptions();
        internal override PhysicsBackendCheckpoint CreateCheckpoint() => new WrappedCheckpoint(this, _inner.CreateCheckpoint());
        internal override void BeginPortableRestore() => _inner.BeginPortableRestore();
        internal override float ReadPortableSleepTime(PhysicsColliderBackend collider) => _inner.ReadPortableSleepTime(collider);
        internal override void ApplyPortableSleep(PhysicsColliderBackend collider, float time, bool canSleep, bool sleeping) => _inner.ApplyPortableSleep(collider, time, canSleep, sleeping);
        internal override void CompletePortableRestore(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> pairs, ulong tick) => _inner.CompletePortableRestore(map, pairs, tick);
        internal override void CapturePortableOneWays(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> destination) => _inner.CapturePortableOneWays(map, destination);
        internal override int ReadDebugContacts(Span<Vector2> output) => _inner.ReadDebugContacts(output);
        internal override void EnsureAccess() => _inner.EnsureAccess();
        internal override void SetSleepSettings(in PhysicsSleepSettings settings) => _inner.SetSleepSettings(settings);
        internal override void SetContactSettings(in PhysicsContactSettings settings) => _inner.SetContactSettings(settings);
        internal override void SetSolverIterations(int value) => _inner.SetSolverIterations(value);
        internal override void SetConstraintDefaultBias(float value) => _inner.SetConstraintDefaultBias(value);
        internal override void PrepareSolverCapacity() => _inner.PrepareSolverCapacity();
        internal override void PrepareMonitoringCapacity() => _inner.PrepareMonitoringCapacity();
        internal override PhysicsSpace.Statistics ReadStatistics() => _inner.ReadStatistics();
        internal override bool CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second,
            Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)
        {
            ShapePairQueries++;
            if (PairHook is not null) return PairHook(contacts, out count);
            return _inner.CollideShapes(first, firstPose, firstMotion, second, secondPose, secondMotion, contacts, out count);
        }
        internal override bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion, Span<Vector2> contacts, out int count)
        {
            BodyPairQueries++;
            if (PairHook is not null) return PairHook(contacts, out count);
            return _inner.BodyCollideShape(body, index, shape, pose, motion, contacts, out count);
        }
        internal override PhysicsColliderImplementation CreateCollider(PhysicsColliderBackend collider)
        {
            var created = _inner.CreateCollider(collider);
            return new WrappedCollider(collider, Space, created) { ShapeInputs = ShapeHook, ImpulseScale = ImpulseScale };
        }
        internal override PhysicsJointImplementation CreateJoint(PhysicsJointBackend joint) => new WrappedJoint(Space, _inner.CreateJoint(joint));
        internal CPUPhysicsJointImplementation NativeJoint(PhysicsJointBackend joint) => ((CPUPhysicsWorldBackend)_inner).NativeJoint(joint);
        internal override void BeginStep(double delta) => _inner.BeginStep(delta);
        internal override void PrepareWorld(double delta) => _inner.PrepareWorld(delta);
        internal override void PrepareBodies(double delta) => _inner.PrepareBodies(delta);
        internal override void Solve(double delta) => _inner.Solve(delta);
        internal override void SyncResults() => _inner.SyncResults();
        internal override void CompleteBody(PhysicsServerCollider body) => _inner.CompleteBody(body);
        internal override void CompleteBody(PhysicsBody body) => _inner.CompleteBody(body);
        internal override void CollectContacts() => _inner.CollectContacts();
        internal override void ScanAreas() => _inner.ScanAreas();
        internal override bool AreaContainsPoint(Area area, Vector2 point, uint mask) => _inner.AreaContainsPoint(area, point, mask);
        internal override void EndStep(Exception? failure) => _inner.EndStep(failure);
        internal override PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, RID[] excluded, bool collideWithAreas, bool collideWithBodies, bool hitFromInside) => _inner.IntersectRay(from, to, mask, excluded, collideWithAreas, collideWithBodies, hitFromInside);
        internal override List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters) => _inner.CollectPointHits(parameters);
        internal override List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters) => _inner.CollectShapeHits(parameters);
        internal override (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters) => _inner.CastMotion(parameters);
        internal override List<ShapeContactPair> CollectShapeContacts(PhysicsShapeQueryParameters parameters, int limit) => _inner.CollectShapeContacts(parameters, limit);
        internal override PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters) => _inner.GetRestInfo(parameters);
        internal override MotionResultData TestBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision, RID[] excludedBodies, ulong[] excludedObjects, bool collideSeparationRay = false) => _inner.TestBodyMotion(body, from, motion, margin, recoveryAsCollision, excludedBodies, excludedObjects, collideSeparationRay);
        public override void Dispose() => _inner.Dispose();
    }
}
