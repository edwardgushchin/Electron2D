namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal GPUPhysicsBodyStore? GPUStore => _backend?.GPUStore;
    internal GPUPhysicsColliderImplementation GPUCollider(PhysicsColliderBackend collider) => GPUStore!.NativeCollider(collider);
    internal double GPUPrepareBodiesMS, GPUPrepareReportsMS, GPUPrepareWakesMS, GPUPrepareWakeWaitMS;
    // Diagnostic controls for comparing identical worlds with repeated policy checks and angle reconstruction.
    internal bool ForceGPUParameterRefresh, DecodeGPUTransforms, ForceGPUStatePublication, ForceFullGPUContactReports;
    private bool _gpuWakePending;
    internal long GPUStateEpoch { get; private set; }
    internal long GPUVelocityEpoch { get; private set; }
    internal long GPUStatePublicationEpoch { get; private set; } = -1;
    /// <summary>Retires complete observations and optionally the shared velocity qualification.</summary>
    /// <param name="wake">Whether queued wake propagation must be published before observing activity.</param>
    /// <param name="velocities">Whether velocity observations beyond a separately invalidated collider must be retired.</param>
    internal void InvalidateGPUStates(bool wake = true, bool velocities = true)
    {
        _gpuWakePending |= wake;
        GPUStateEpoch++;
        if (velocities) GPUVelocityEpoch++;
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
    internal bool TryGetGPUShapeOwner(uint index, out (PhysicsColliderBackend Owner, uint Generation, int Slot) owner) => _gpuShapeOwners.TryGetValue(index, out owner);
    internal int GPUSensorShapeCount => _gpuSensorShapeCount;
    /// <summary>Invalidates every published contact range while retaining slot-indexed capacity.</summary>
    internal void ClearGPUReports() { Array.Clear(_gpuReportRanges); _gpuUseContactKeys = false; }
    internal bool TryGetGPUCollider(RID rid, out PhysicsColliderBackend collider) => _gpuRIDColliders.TryGetValue((ulong)rid.GetID(), out collider!);
    internal bool TryGetGPUCollider(int index, out PhysicsColliderBackend collider) => _gpuColliders.TryGetValue(index, out collider!);
    private GPUPhysicsBodyStore.BodyChange[] _gpuChanges = [];
    private uint[] _gpuActivity = [];
    private GPUPhysicsBodyStore.BodyHandle[] _gpuObservedBodies = [];
    private GPUPhysicsBodyStore.Snapshot[] _gpuObservedStates = [];
    private PhysicsColliderBackend?[] _gpuObservedBackends = [];
    private GPUPhysicsBodyStore.BodyHandle[] _gpuReportBodies = [];
    private int[] _gpuReportLimits = [], _gpuReportCounts = [];
    private GPUPhysicsBodyStore.ContactReport[] _gpuReports = [];
    private int _gpuReportBodyCount;
    private (int Offset, int Count, bool Keys)[] _gpuReportRanges = [];

    internal void RegisterGPUCollider(PhysicsColliderBackend backend)
    {
        _gpuColliders.Add(GPUCollider(backend).GPUHandle.Index, backend);
        _gpuRIDColliders.Add((ulong)backend.RID.GetID(), backend);
        _backend.InvalidateCollisionExceptions(backend);
        GPUStore!.SetCollisionPriority(GPUCollider(backend).GPUHandle, backend.CollisionPriority);
        if (PhysicsServer.Service.TryResolveBodyOwners(backend.RID, out var owners))
            GPUStore.SetContactReporting(GPUCollider(backend).GPUHandle,
                (owners.Scene?.Runtime ?? owners.Server!.Runtime).ContactLimit > 0);
        PrepareGPUCapacity();
    }
    internal void UnregisterGPUCollider(PhysicsColliderBackend backend)
    {
        _gpuColliders.Remove(GPUCollider(backend).GPUHandle.Index); _gpuRIDColliders.Remove((ulong)backend.RID.GetID());
        _backend.InvalidateCollisionExceptions(backend);
        foreach (var shape in GPUCollider(backend).GPUShapes) UnregisterGPUShape(shape.Handle);
    }
    internal void RegisterGPUShape(PhysicsColliderBackend backend, GPUPhysicsBodyStore.ShapeHandle handle, int slot)
    {
        _gpuShapeOwners.Add((uint)handle.Index, (backend, handle.Generation, slot));
        if (GPUCollider(backend).GPUSensor) _gpuSensorShapeCount++;
    }
    internal void UnregisterGPUShape(GPUPhysicsBodyStore.ShapeHandle handle)
    {
        if (_gpuShapeOwners.Remove((uint)handle.Index, out var previous) && GPUCollider(previous.Owner).GPUSensor) _gpuSensorShapeCount--;
    }

    internal void PrepareGPUCapacity()
    {
        var count = GPUStore!.BodySlotCount;
        if (_gpuChanges.Length < count)
        {
            var capacity = Math.Max(8, count * 2);
            Array.Resize(ref _gpuChanges, capacity);
            Array.Resize(ref _gpuActivity, capacity);
        }
        if (_gpuReportBodies.Length < count)
        {
            var capacity = Math.Max(8, count * 2);
            Array.Resize(ref _gpuReportBodies, capacity); Array.Resize(ref _gpuReportLimits, capacity); Array.Resize(ref _gpuReportCounts, capacity);
            Array.Resize(ref _gpuReportRanges, capacity); Array.Resize(ref _gpuReportFormats, capacity);
            Array.Resize(ref _gpuReportSourceOffsets, capacity); Array.Resize(ref _gpuReportNativeOffsets, capacity);
        }
    }
    internal void PublishGPU()
    {
        FlushGPUWakes();
        if (!ForceGPUStatePublication && GPUStatePublicationEpoch == GPUStateEpoch)
        {
            var current = true;
            foreach (var backend in _gpuColliders.Values)
                if (!GPUCollider(backend).GPUStateValid) { current = false; break; }
            if (current) return;
        }
        PrepareGPUCapacity();
        var count = GPUStore!.ReadChanges(_gpuChanges);
        GPUStatePublicationEpoch = GPUStateEpoch;
        foreach (ref readonly var change in _gpuChanges.AsSpan(0, count))
            if (change.Alive != 0 && _gpuColliders.TryGetValue((int)change.Index, out var backend) && GPUCollider(backend).GPUHandle.Generation == change.Generation)
                GPUCollider(backend).AcceptGPUState(change.State, published: true);
        // A selected intermediate read may differ from unchanged publication history.
        foreach (var backend in _gpuColliders.Values) GPUCollider(backend).CompleteGPUStatePublication();
    }

    internal void CaptureGPUActivity()
    {
        PrepareGPUCapacity(); FlushGPUWakes();
        GPUStore!.ReadActivity(_gpuActivity);
        foreach (var backend in _gpuColliders.Values)
        {
            var collider = GPUCollider(backend);
            collider.AcceptGPUActivity(_gpuActivity[collider.GPUHandle.Index] != 0);
        }
    }

    internal static bool RequiresGPUCompletion(PhysicsServerCollider body) =>
        !body.IsArea && (RequiresBodySnapshot(body.Runtime, null) || body.Mode == PhysicsServer.BodyMode.Kinematic ||
            body.Runtime.ContactLimit > 0 || body.Runtime.View is { IsDisposed: false });

    internal void PublishGPUCompletion()
    {
        var bodies = _gpuReportBodies.AsSpan(0, _gpuReportBodyCount); var limits = _gpuReportLimits.AsSpan(0, _gpuReportBodyCount);
        if (!_gpuUseContactKeys) PublishGPUCompletion(new(bodies, limits, _gpuReportCounts, _gpuReports));
        else if (_gpuReportFullSlots == 0) PublishGPUCompletion(new(bodies, limits, _gpuReportCounts, _gpuReportIdentities, _gpuReportSourceOffsets));
        else PublishGPUCompletion(new(bodies, limits, _gpuReportCounts, _gpuReports, _gpuReportIdentities, _gpuReportSourceOffsets,
            _gpuReportFormats.AsSpan(0, _gpuReportBodyCount)));
        _gpuContactResultVersion = GPUStore!.ContactResultVersion;
        Array.Clear(_gpuReportRanges);
        var fullOffset = 0; var keyOffset = 0;
        for (var i = 0; i < _gpuReportBodyCount; i++)
        {
            var key = _gpuReportFormats[i] != 0;
            _gpuReportRanges[_gpuReportBodies[i].Index] = (key ? keyOffset : fullOffset, _gpuReportCounts[i], key);
            _gpuReportNativeOffsets[_gpuReportBodies[i].Index] = _gpuReportSourceOffsets[i];
            if (key) keyOffset += _gpuReportLimits[i]; else fullOffset += _gpuReportLimits[i];
        }
    }

    private void PublishGPUCompletion(GPUPhysicsBodyStore.ContactRead reports)
    {
        if (_serverColliders.Count == 0 && _bodies.Count == 0) { PublishGPU(); GPUStore!.ReadContactReports(reports); return; }
        var observed = _bodies.Count;
        var likelyChanges = _serverColliders.Count + _areas.Count;
        foreach (var body in _bodies) if (GPUCollider(body.Backend).GPUChangeLikely) likelyChanges++;
        foreach (var body in _serverColliders) if (RequiresGPUCompletion(body)) observed++;
        PrepareGPUObservedCapacity(observed);
        // Last observations predict cost only. Both paths acquire validated current device state.
        var changedBytes = 8 + (long)likelyChanges * System.Runtime.CompilerServices.Unsafe.SizeOf<GPUPhysicsBodyStore.BodyChange>();
        var selectedBytes = 16 + (long)observed * System.Runtime.CompilerServices.Unsafe.SizeOf<GPUPhysicsBodyStore.Snapshot>();
        if (_bodies.Count > 0 && changedBytes < selectedBytes) { PublishGPU(); GPUStore!.ReadContactReports(reports); return; }
        PrepareGPUCapacity();
        var count = 0;
        foreach (var body in _bodies) Observe(body.Backend);
        foreach (var body in _serverColliders)
            if (RequiresGPUCompletion(body)) Observe(body.Backend);
        if (count == 0) { GPUStore!.ReadContactReports(reports); return; }
        FlushGPUWakes();
        GPUStore!.Read(_gpuObservedBodies.AsSpan(0, count), _gpuObservedStates.AsSpan(0, count), reports);
        for (var i = 0; i < count; i++)
        {
            GPUCollider(_gpuObservedBackends[i]!).AcceptGPUState(_gpuObservedStates[i]);
            _gpuObservedBackends[i] = null;
        }
        void Observe(PhysicsColliderBackend backend)
        {
            if (count == _gpuObservedStates.Length) PrepareGPUObservedCapacity(count + 1);
            _gpuObservedBackends[count] = backend; _gpuObservedBodies[count++] = GPUCollider(backend).GPUHandle;
        }
    }

    private void PrepareGPUObservedCapacity(int count)
    {
        if (count <= _gpuObservedStates.Length) return;
        var capacity = Math.Max(8, count * 2);
        Array.Resize(ref _gpuObservedBodies, capacity); Array.Resize(ref _gpuObservedStates, capacity); Array.Resize(ref _gpuObservedBackends, capacity);
    }

    internal static GPUPhysicsBodyStore.FieldParameters GPUFields(PhysicsAreaFields fields) =>
        new(fields.GravityVector, fields.Gravity, fields.GravityPoint, fields.GravityPointUnitDistance, fields.LinearDamp, fields.AngularDamp,
            fields.GravitySpaceOverride, fields.LinearDampSpaceOverride, fields.AngularDampSpaceOverride, fields.Priority);

    internal void PrepareGPUMotion(PhysicsBodyRuntime runtime, PhysicsBody? body, double delta, bool captureActivity)
    {
        if (captureActivity) runtime.ApplyBeforeStep(body);
        else runtime.ActiveBeforeStep = false;
        if (body is { HasServerMode: true }) body.PrepareServerMotion(delta);
        else if (body is AnimatableBody animatable) animatable.PrepareMotion(delta);
        else if (body is CharacterBody character) character.PrepareMotion(delta);
        else if (body is RigidBody rigid) rigid.PrepareFrozenMotion(delta);
        else if (body is null) runtime.Owners.Server!.PrepareMotion(delta);
    }

    internal void PrepareGPUReports()
    {
        // Attachment, limit edits and replay already publish report flags; pack each receiver's demand here.
        _gpuUseContactKeys = false; _gpuReportFullSlots = 0;
        _gpuReportBodyCount = 0; var keySlots = 0;
        foreach (var body in _bodies) Add(body.Backend, body.Runtime);
        foreach (var body in _serverColliders)
            if (!body.IsArea) Add(body.Backend, body.Runtime);

        void Add(PhysicsColliderBackend backend, PhysicsBodyRuntime runtime)
        {
            var requested = runtime.GPUContactDetailsRequested; runtime.GPUContactDetailsRequested = false;
            var limit = runtime.ContactLimit;
            if (limit == 0) return;
            var key = !ForceFullGPUContactReports && !requested && !RequiresGPUContactDetails(runtime);
            if (key) { _gpuUseContactKeys = true; keySlots = checked(keySlots + limit); }
            else _gpuReportFullSlots = checked(_gpuReportFullSlots + limit);
            if (_gpuContactScratch.Length < limit) Array.Resize(ref _gpuContactScratch, limit);
            _gpuReportBodies[_gpuReportBodyCount] = GPUCollider(backend).GPUHandle;
            _gpuReportFormats[_gpuReportBodyCount] = key ? (byte)1 : (byte)0;
            _gpuReportLimits[_gpuReportBodyCount++] = limit;
        }
        if (_gpuReportIdentities.Length < keySlots) Array.Resize(ref _gpuReportIdentities, Math.Max(8, checked(keySlots * 2)));
        if (_gpuReports.Length < _gpuReportFullSlots) Array.Resize(ref _gpuReports, Math.Max(8, checked(_gpuReportFullSlots * 2)));
        GPUStore!.CaptureContactReports = _gpuReportBodyCount > 0;
    }
    private void ReleaseGPUState()
    {
        _gpuQueryGeometry?.Dispose(); _gpuQueryGeometry = null; _gpuQueryShape = null;
        _gpuColliders.Clear(); _gpuRIDColliders.Clear(); _gpuShapeOwners.Clear(); _gpuReportRanges = [];
        _gpuChanges = []; _gpuActivity = []; _gpuObservedBodies = []; _gpuObservedStates = []; _gpuObservedBackends = []; _gpuReportBodies = []; _gpuReportLimits = []; _gpuReportCounts = []; _gpuReports = [];
        _gpuReportFormats = []; _gpuReportIdentities = []; _gpuReportSourceOffsets = []; _gpuReportNativeOffsets = []; _gpuContactScratch = [];
        _gpuQueryExclusions = []; _gpuPointHits = []; _gpuShapeHits = [];
        _gpuShapeQuery[0] = default;
    }
    internal void CaptureGPUViewContacts(PhysicsColliderBackend backend, PhysicsDirectBodyState view, int limit)
    {
        var index = GPUCollider(backend).GPUHandle.Index;
        if ((uint)index >= (uint)_gpuReportRanges.Length) return;
        var range = _gpuReportRanges[index];
        if (range.Count == 0) return;
        if (range.Keys)
        {
            view.BeginGPUContacts(_gpuContactResultVersion, _gpuReportNativeOffsets[index], range.Count);
            for (var i = 0; i < range.Count; i++)
            {
                ref readonly var key = ref _gpuReportIdentities[range.Offset + i];
                if (!_gpuShapeOwners.TryGetValue(key.Shape, out var own) || own.Generation != key.ShapeGeneration ||
                    !_gpuShapeOwners.TryGetValue(key.ColliderShape, out var other) || other.Generation != key.ColliderShapeGeneration) continue;
                var slot = view.SelectContactSlot(0, limit);
                if (slot < 0) continue;
                var identity = other.Owner.ObjectIdentity;
                view.StoreContact(slot, new(other.Owner.RID, identity.ID, own.Slot, other.Slot, default, default,
                    default, default, default, default, 0, other.Owner.SceneOwnerReference, identity));
                view.SetGPUContactSource(slot, i);
            }
            return;
        }
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
