namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal GPUPhysicsBodyStore? GPUStore => _backend.GPUStore;
    internal double GPUPrepareBodiesMS, GPUPrepareReportsMS, GPUPrepareWakesMS, GPUPrepareWakeWaitMS;
    // Diagnostic controls for comparing identical worlds with repeated policy checks and angle reconstruction.
    internal bool ForceGPUParameterRefresh, DecodeGPUTransforms;
    private bool _gpuWakePending;
    internal long GPUStateEpoch { get; private set; }
    internal long GPUStatePublicationEpoch { get; private set; } = -1;
    internal void InvalidateGPUStates(bool wake = true)
    {
        _gpuWakePending |= wake;
        GPUStateEpoch++;
    }
    internal void FlushGPUWakes()
    {
        if (!_gpuWakePending) return;
        GPUStore!.PublishExternalWakes(); _gpuWakePending = false;
    }
    private readonly Dictionary<int, PhysicsColliderBackend> _gpuColliders = [];
    private readonly Dictionary<ulong, PhysicsColliderBackend> _gpuRIDColliders = [];
    private readonly Dictionary<uint, (PhysicsColliderBackend Owner, uint Generation, int Slot)> _gpuShapeOwners = [];
    private int _gpuSensorShapeCount;
    private GPUPhysicsBodyStore.BodyChange[] _gpuChanges = [];
    private GPUPhysicsBodyStore.BodyHandle[] _gpuObservedBodies = [];
    private GPUPhysicsBodyStore.Snapshot[] _gpuObservedStates = [];
    private PhysicsColliderBackend?[] _gpuObservedBackends = [];
    private GPUPhysicsBodyStore.BodyHandle[] _gpuReportBodies = [];
    private int[] _gpuReportLimits = [], _gpuReportCounts = [];
    private GPUPhysicsBodyStore.ContactReport[] _gpuReports = [];
    private int _gpuReportBodyCount;
    private readonly Dictionary<int, (int Offset, int Count)> _gpuReportRanges = [];

    internal void RegisterGPUCollider(PhysicsColliderBackend backend)
    {
        _gpuColliders.Add(backend.GPUHandle.Index, backend);
        _gpuRIDColliders.Add((ulong)backend.RID.GetID(), backend);
        InvalidateGPUExceptions();
        GPUStore!.SetCollisionPriority(backend.GPUHandle, backend.CollisionPriority);
        PrepareGPUCapacity();
    }
    internal void UnregisterGPUCollider(PhysicsColliderBackend backend)
    {
        _gpuColliders.Remove(backend.GPUHandle.Index); _gpuRIDColliders.Remove((ulong)backend.RID.GetID());
        InvalidateGPUExceptions();
        foreach (var shape in backend.GPUShapes) UnregisterGPUShape(shape.Handle);
    }
    internal void RegisterGPUShape(PhysicsColliderBackend backend, GPUPhysicsBodyStore.ShapeHandle handle, int slot)
    {
        _gpuShapeOwners.Add((uint)handle.Index, (backend, handle.Generation, slot));
        if (backend.GPUSensor) _gpuSensorShapeCount++;
    }
    internal void UnregisterGPUShape(GPUPhysicsBodyStore.ShapeHandle handle)
    {
        if (_gpuShapeOwners.Remove((uint)handle.Index, out var previous) && previous.Owner.GPUSensor) _gpuSensorShapeCount--;
    }

    internal void PrepareGPUCapacity()
    {
        var count = GPUStore!.BodySlotCount;
        if (_gpuChanges.Length < count)
        {
            var capacity = Math.Max(8, count * 2);
            Array.Resize(ref _gpuChanges, capacity);
        }
        if (_gpuReportBodies.Length < count)
        {
            var capacity = Math.Max(8, count * 2);
            Array.Resize(ref _gpuReportBodies, capacity); Array.Resize(ref _gpuReportLimits, capacity); Array.Resize(ref _gpuReportCounts, capacity);
            _gpuReportRanges.EnsureCapacity(capacity);
        }
    }
    internal void PublishGPU()
    {
        FlushGPUWakes();
        PrepareGPUCapacity();
        var count = GPUStore!.ReadChanges(_gpuChanges);
        GPUStatePublicationEpoch = GPUStateEpoch;
        foreach (ref readonly var change in _gpuChanges.AsSpan(0, count))
            if (change.Alive != 0 && _gpuColliders.TryGetValue((int)change.Index, out var backend) && backend.GPUHandle.Generation == change.Generation)
                backend.AcceptGPUState(change.State, published: true);
        // A selected intermediate read may differ from unchanged publication history.
        foreach (var backend in _gpuColliders.Values) backend.CompleteGPUStatePublication();
    }

    internal static bool RequiresGPUCompletion(PhysicsServerCollider body, bool callbacks) =>
        !body.IsArea && (callbacks || body.Mode == PhysicsServer.BodyMode.Kinematic ||
            body.Runtime.ContactLimit > 0 || body.Runtime.View is { IsDisposed: false });

    internal void PublishGPUCompletion(bool callbacks)
    {
        if (callbacks || _serverColliders.Count == 0) { PublishGPU(); return; }
        PrepareGPUCapacity();
        var count = 0;
        foreach (var body in _bodies) Observe(body.Backend);
        foreach (var area in _areas) Observe(area.Backend);
        foreach (var body in _serverColliders)
            if (RequiresGPUCompletion(body, callbacks: false)) Observe(body.Backend);
        if (count == 0) return;
        FlushGPUWakes();
        GPUStore!.Read(_gpuObservedBodies.AsSpan(0, count), _gpuObservedStates.AsSpan(0, count));
        for (var i = 0; i < count; i++)
        {
            _gpuObservedBackends[i]!.AcceptGPUState(_gpuObservedStates[i]);
            _gpuObservedBackends[i] = null;
        }
        void Observe(PhysicsColliderBackend backend)
        {
            if (count == _gpuObservedStates.Length)
            {
                var capacity = Math.Max(8, count * 2);
                Array.Resize(ref _gpuObservedBodies, capacity); Array.Resize(ref _gpuObservedStates, capacity); Array.Resize(ref _gpuObservedBackends, capacity);
            }
            _gpuObservedBackends[count] = backend; _gpuObservedBodies[count++] = backend.GPUHandle;
        }
    }

    internal static GPUPhysicsBodyStore.FieldParameters ReplayFields(PhysicsAreaFields fields) => GPUFields(fields);

    internal static GPUPhysicsBodyStore.FieldParameters GPUFields(PhysicsAreaFields fields) =>
        new(fields.GravityVector, fields.Gravity, fields.GravityPoint, fields.GravityPointUnitDistance, fields.LinearDamp, fields.AngularDamp,
            fields.GravitySpaceOverride, fields.LinearDampSpaceOverride, fields.AngularDampSpaceOverride, fields.Priority);

    internal void PrepareGPUMotion(PhysicsBodyRuntime runtime, PhysicsBody? body, double delta, bool captureActivity)
    {
        if (captureActivity) runtime.ApplyBeforeStep(body);
        else runtime.ActiveBeforeStep = false;
        if (body is AnimatableBody animatable) animatable.PrepareMotion(delta);
        else if (body is CharacterBody character) character.PrepareMotion(delta);
        else if (body is RigidBody rigid) rigid.PrepareFrozenMotion(delta);
        else if (body is null) runtime.Owners.Server!.PrepareMotion(delta);
    }

    internal void PrepareGPUReports()
    {
        _gpuReportBodyCount = 0; var contacts = 0;
        foreach (var body in _bodies) Add(body.Backend, body.Runtime);
        foreach (var body in _serverColliders)
            if (!body.IsArea) Add(body.Backend, body.Runtime);

        void Add(PhysicsColliderBackend backend, PhysicsBodyRuntime runtime)
        {
            var limit = runtime.ContactLimit;
            backend.SetContactReporting(limit > 0);
            if (limit == 0) return;
            _gpuReportBodies[_gpuReportBodyCount] = backend.GPUHandle;
            _gpuReportLimits[_gpuReportBodyCount++] = limit; contacts = checked(contacts + limit);
        }
        if (_gpuReports.Length < contacts) Array.Resize(ref _gpuReports, Math.Max(8, contacts * 2));
        GPUStore!.CaptureContactReports = _gpuReportBodyCount > 0;
    }
    internal void ReadGPUReports()
    {
        _gpuReportRanges.Clear();
        if (_gpuReportBodyCount == 0) return;
        GPUStore!.ReadContactReports(_gpuReportBodies.AsSpan(0, _gpuReportBodyCount), _gpuReportLimits.AsSpan(0, _gpuReportBodyCount),
            _gpuReportCounts, _gpuReports);
        var offset = 0;
        for (var i = 0; i < _gpuReportBodyCount; i++)
        {
            _gpuReportRanges.Add(_gpuReportBodies[i].Index, (offset, _gpuReportCounts[i]));
            offset += _gpuReportLimits[i];
        }
    }
    private void ReleaseGPUState()
    {
        _gpuQueryGeometry?.Dispose(); _gpuQueryGeometry = null; _gpuQueryShape = null;
        _gpuColliders.Clear(); _gpuRIDColliders.Clear(); _gpuShapeOwners.Clear(); _gpuReportRanges.Clear();
        _gpuExceptions.Clear(); _gpuNextExceptions.Clear();
        _gpuChanges = []; _gpuObservedBodies = []; _gpuObservedStates = []; _gpuObservedBackends = []; _gpuReportBodies = []; _gpuReportLimits = []; _gpuReportCounts = []; _gpuReports = [];
        _gpuQueryExclusions = []; _gpuPointHits = []; _gpuShapeHits = []; _gpuAreaHits = [];
        _gpuShapeQuery[0] = default; _gpuAreaQuery[0] = default;
    }
    internal void CaptureGPUViewContacts(PhysicsColliderBackend backend, PhysicsDirectBodyState view, int limit)
    {
        if (!_gpuReportRanges.TryGetValue(backend.GPUHandle.Index, out var range)) return;
        foreach (ref readonly var contact in _gpuReports.AsSpan(range.Offset, range.Count))
        {
            if (!_gpuShapeOwners.TryGetValue(contact.Shape, out var own) || own.Generation != contact.ShapeGeneration ||
                !_gpuShapeOwners.TryGetValue(contact.ColliderShape, out var other) || other.Generation != contact.ColliderShapeGeneration) continue;
            var slot = view.SelectContactSlot(contact.Depth, limit);
            if (slot < 0) continue;
            var identity = other.Owner.ObjectIdentity;
            view.StoreContact(slot, new(other.Owner.RID, identity.ID, own.Slot, other.Slot, contact.Position, contact.ColliderPosition,
                contact.Normal, contact.Velocity, contact.ColliderVelocity, contact.Impulse, contact.Depth, other.Owner.SceneOwnerReference, identity));
        }
    }

}
