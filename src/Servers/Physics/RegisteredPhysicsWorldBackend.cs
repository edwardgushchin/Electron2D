namespace Electron2D;

/// <summary>Adapts the full value-and-identity implementation contract to common scene/server ownership.</summary>
internal sealed partial class RegisteredPhysicsWorldBackend : PhysicsWorldBackend
{
    private readonly PhysicsServerBackend _implementation;
    private readonly Dictionary<PhysicsColliderBackend, Collider> _colliders = [];
    private readonly Dictionary<PhysicsJointBackend, Joint> _joints = [];
    private RID[] _portableBodies = [];
    private PhysicsBackendOneWay[] _oneWays = [];
    private readonly PhysicsTestMotionResult _motion = new(implementationOwned: true);
    private readonly List<(RID From, RID To)> _sourceExceptions = [], _exceptions = [];
    private bool _exceptionsDirty = true;
    private bool _disposed, _retiring, _solving, _resultsReady, _readingFailure;
    private Exception? _failure;
    internal RegisteredPhysicsWorldBackend(PhysicsSpace space, PhysicsServerBackend implementation)
        : base(space, PhysicsServer.Backend.Extension)
    {
        ArgumentNullException.ThrowIfNull(implementation); implementation.Claim(space.RID); _implementation = implementation;
    }
    private void Enter() { if (_implementation.IsDisposed) throw new ObjectDisposedException(nameof(PhysicsServerBackend)); if (_disposed) throw new ObjectDisposedException(nameof(RegisteredPhysicsWorldBackend)); Space.BeginImplementationCallback(_retiring); }
    private void Exit() => Space.EndImplementationCallback();
    internal override PhysicsServer.Backend Kind => PhysicsServer.Backend.Extension;
    internal override bool HasFailure
    {
        get
        {
            if (_failure is not null) return true;
            if (_readingFailure) throw new InvalidOperationException("A physics status hook cannot recursively inspect its world.");
            Enter(); _readingFailure = true;
            try { return _implementation.HasFailure; }
            finally { _readingFailure = false; Exit(); }
        }
    }
    internal override bool ResultsReady => _resultsReady;
    internal override int PreparedBodyCapacity { get { Enter(); try { return CountResult(_implementation.PreparedBodyCapacity); } finally { Exit(); } } }
    internal override void EnsureAccess()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RegisteredPhysicsWorldBackend));
        if (_retiring) throw new InvalidOperationException("The selected physics implementation is retiring.");
        if (HasFailure) throw new InvalidOperationException("The registered physics implementation failed; dispose its world before replacement.", _failure);
    }
    internal override void Initialize() { Enter(); try { _implementation.Initialize(); } finally { Exit(); } }
    internal override void ApplyActive(bool active) { Enter(); try { _implementation.SetActive(active); } finally { Exit(); } }
    internal override void FlushQueries() { Enter(); try { _implementation.FlushQueries(); } finally { Exit(); } }
    internal override void EndSync(Exception? failure) { Enter(); try { _implementation.EndSync(failure); } finally { Exit(); } }
    internal override void PrepareCheckpoint() { Enter(); try { _implementation.PrepareCheckpoint(); } finally { Exit(); } }
    internal override void PreparePortableCapture() { Enter(); try { _implementation.PreparePortableCapture(); } finally { Exit(); } }
    internal override void SynchronizeCollisionExceptions()
    {
        Enter();
        try
        {
            if (_exceptionsDirty)
            {
                PhysicsServer.Service.CopyCollisionExceptions(_sourceExceptions, includeJoints: true); _exceptions.Clear();
                foreach (var pair in _sourceExceptions)
                    if (ContainsBody(pair.From) && ContainsBody(pair.To) && !_exceptions.Contains(pair)) _exceptions.Add(pair);
                _implementation.SetCollisionExceptions(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_exceptions));
                _exceptionsDirty = false;
            }
            _implementation.SynchronizeCollisionExceptions();
        }
        finally { Exit(); }
    }
    private bool ContainsBody(RID body)
    {
        if (!PhysicsServer.Service.TryResolveBodyOwners(body, out var owners)) return false;
        return ReferenceEquals(owners.Scene?.Backend.Space ?? owners.Server?.Backend.Space, Space);
    }
    internal override PhysicsBackendCheckpoint CreateCheckpoint()
    {
        Enter();
        try
        {
            var created = _implementation.CreateCheckpoint() ?? throw new InvalidOperationException("A solver history factory returned no object.");
            created.Claim();
            var wrapped = new History(this, created);
            try { wrapped.Capture(); return wrapped; }
            catch (Exception error)
            {
                try { wrapped.Dispose(); }
                catch (Exception cleanup) { throw new AggregateException("Solver history creation and cleanup failed.", error, cleanup); }
                throw;
            }
        }
        finally { Exit(); }
    }
    internal override void BeginPortableRestore() { Enter(); try { _implementation.BeginPortableRestore(); } finally { Exit(); } }
    internal override void SetDebugContacts(int maximum)
    { Enter(); try { _implementation.SetDebugContacts(maximum); } finally { Exit(); } }
    internal override int ReadDebugContacts(Span<Vector2> output)
    {
        Enter();
        try
        {
            var count = _implementation.ReadDebugContacts(output);
            if ((uint)count > (uint)output.Length) throw new InvalidOperationException("Debug contact output exceeded its supplied capacity.");
            for (var i = 0; i < count; i++) VectorResult(output[i]);
            return count;
        }
        finally { Exit(); }
    }
    internal override void SetSleepSettings(in PhysicsSleepSettings settings) { Enter(); try { _implementation.SetSleepSettings(settings); } finally { Exit(); } }
    internal override void SetContactSettings(in PhysicsContactSettings settings) { Enter(); try { _implementation.SetContactSettings(settings); } finally { Exit(); } }
    internal override void SetSolverIterations(int value) { Enter(); try { _implementation.SetSolverIterations(value); } finally { Exit(); } }
    internal override void SetConstraintDefaultBias(float value) { Enter(); try { _implementation.SetConstraintDefaultBias(value); } finally { Exit(); } }
    internal override bool CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second,
        Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)
    { Enter(); try { return _implementation.CollideShapes(first, firstPose, firstMotion, second, secondPose, secondMotion, contacts, out count); } finally { Exit(); } }
    internal override bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion,
        Span<Vector2> contacts, out int count)
    { Enter(); try { return _implementation.BodyCollideShape(body, index, shape, pose, motion, contacts, out count); } finally { Exit(); } }
    internal override PhysicsColliderImplementation CreateCollider(PhysicsColliderBackend collider)
    {
        Enter();
        try
        {
            var area = PhysicsServer.Service.IsAreaCollider(collider.RID);
            var body = _implementation.CreateCollider(collider.RID, area) ?? throw new InvalidOperationException("A collider factory returned no implementation.");
            body.Claim();
            var wrapped = new Collider(this, collider, Space, body); _colliders.Add(collider, wrapped); _exceptionsDirty = true; return wrapped;
        }
        finally { Exit(); }
    }
    internal override PhysicsJointImplementation CreateJoint(PhysicsJointBackend joint)
    {
        Enter();
        try
        {
            var created = _implementation.CreateJoint(joint.RID) ?? throw new InvalidOperationException("A joint factory returned no implementation.");
            created.Claim();
            var wrapped = new Joint(this, joint, Space, created); _joints.Add(joint, wrapped); return wrapped;
        }
        finally { Exit(); }
    }
    internal override PhysicsDirectSpaceState CreateDirectSpaceState()
    { Enter(); try { return _implementation.CreateDirectSpaceState(Space.RID); } finally { Exit(); } }
    internal override PhysicsDirectBodyState CreateDirectBodyState(PhysicsBodyRuntime body)
    { Enter(); try { return _implementation.CreateDirectBodyState(body.RID); } finally { Exit(); } }
    internal override void PrepareSolverCapacity()
    {
        var shapes = 0; foreach (var collider in _colliders.Values) shapes += collider.ShapeCount;
        Enter(); try { _implementation.PrepareSolverCapacity(_colliders.Count, shapes, _joints.Count); } finally { Exit(); }
    }
    internal override void PrepareMonitoringCapacity()
    {
        var shapes = 0; foreach (var collider in _colliders.Values) shapes += collider.ShapeCount;
        Enter(); try { _implementation.PrepareMonitoringCapacity(_colliders.Count, shapes); } finally { Exit(); }
    }
    internal override PhysicsSpace.Statistics ReadStatistics()
    {
        Enter();
        try
        {
            var values = _implementation.ReadStatistics();
            if (values.Active < 0 || values.Pairs < 0 || values.Islands < 0 || values.Islands > values.Active)
                throw new InvalidOperationException("A physics implementation returned invalid statistics.");
            return new(values.Active, values.Pairs, values.Islands);
        }
        finally { Exit(); }
    }
    internal override void ReadPortableBodies(ReadOnlySpan<PhysicsColliderBackend> colliders, bool capture)
    {
        if (_portableBodies.Length < colliders.Length) Array.Resize(ref _portableBodies, colliders.Length);
        for (var i = 0; i < colliders.Length; i++) _portableBodies[i] = colliders[i].RID;
        Enter(); try { _implementation.ReadPortableBodies(_portableBodies.AsSpan(0, colliders.Length), capture); } finally { Exit(); }
    }
    internal override void InvalidateCollisionExceptions(PhysicsColliderBackend collider)
    { _exceptionsDirty = true; Enter(); try { _implementation.InvalidateCollisionExceptions(collider.RID); } finally { Exit(); } }
    internal override float ReadPortableSleepTime(PhysicsColliderBackend collider)
    { Enter(); try { return NonnegativeResult(_implementation.ReadPortableSleepTime(collider.RID)); } finally { Exit(); } }
    internal override void ApplyPortableSleep(PhysicsColliderBackend collider, float time, bool canSleep, bool sleeping)
    { Enter(); try { _implementation.ApplyPortableSleep(collider.RID, time, canSleep, sleeping); } finally { Exit(); } }
    internal override void CompletePortableRestore(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> pairs, ulong tick)
    {
        if (_oneWays.Length < pairs.Count) Array.Resize(ref _oneWays, pairs.Count);
        for (var i = 0; i < pairs.Count; i++)
        {
            var p = pairs[i]; _oneWays[i] = new(map.LocalRID(p.A), p.ShapeA, p.PieceA, map.LocalRID(p.B), p.ShapeB, p.PieceB, p.Allowed);
        }
        Enter(); try { _implementation.CompletePortableRestore(_oneWays.AsSpan(0, pairs.Count), tick, Space.LastStep); } finally { Exit(); }
    }
    internal override void CapturePortableOneWays(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> destination)
    {
        Enter();
        try
        {
            var capacity = _implementation.GetPortableOneWayCount();
            if (capacity < 0) throw new InvalidOperationException("One-way capture returned a negative count.");
            if (_oneWays.Length < capacity) Array.Resize(ref _oneWays, capacity);
            var count = _implementation.CapturePortableOneWays(_oneWays.AsSpan(0, capacity));
            if ((uint)count > (uint)capacity) throw new InvalidOperationException("One-way capture exceeded its supplied capacity.");
            for (var i = 0; i < count; i++)
            {
                var p = _oneWays[i]; destination.Add(new(map.NetworkID(p.A), p.ShapeA, p.PieceA, map.NetworkID(p.B), p.ShapeB, p.PieceB, p.Allowed));
            }
        }
        finally { Exit(); }
    }
    internal override MotionResultData TestBodyMotion(RID body, Transform from, Vector2 motion, float margin,
        bool recoveryAsCollision, RID[] excludedBodies, ulong[] excludedObjects, bool collideSeparationRay = false)
    {
        Enter();
        try
        {
            _motion.BeginImplementationUse();
            try
            {
                _motion.SetMotion(motion);
                var hit = _implementation.TestBodyMotion(body, from, motion, margin, recoveryAsCollision, excludedBodies, excludedObjects, collideSeparationRay, _motion);
                var data = _motion.Data;
                if (hit != data.Collided) throw new InvalidOperationException("A motion implementation returned inconsistent collision status.");
                return hit ? data : data with { OwnerRID = body };
            }
            finally { _motion.EndImplementationUse(); }
        }
        finally { Exit(); }
    }
    public override void Dispose()
    {
        if (_disposed) return; Space.EnsureImplementationRelease(); _retiring = true;
        List<Exception>? errors = null;
        foreach (var pair in _colliders.Values.ToArray()) try { pair.Detach(); } catch (Exception error) { (errors ??= []).Add(error); }
        foreach (var pair in _joints.Values.ToArray()) try { pair.Detach(); } catch (Exception error) { (errors ??= []).Add(error); }
        Enter();
        try
        {
            try { _implementation.Finish(); } catch (Exception error) { (errors ??= []).Add(error); }
            try { _implementation.ReleaseImplementation(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        finally
        {
            Exit(); _disposed = true; _colliders.Clear(); _joints.Clear();
            try { _motion.ReleaseImplementationResult(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        PhysicsSpace.FinishCleanup(errors);
    }
}
