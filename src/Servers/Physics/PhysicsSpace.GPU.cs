namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    internal GPUPhysicsBodyStore? GPUStore { get; }
    internal double GPUPrepareBodiesMS, GPUPrepareReportsMS, GPUPrepareWakesMS, GPUPrepareWakeWaitMS;
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

    private void PrepareGPUCapacity()
    {
        var count = GPUStore!.BodySlotCount;
        if (_gpuChanges.Length < count) Array.Resize(ref _gpuChanges, Math.Max(8, count * 2));
        if (_gpuReportBodies.Length < count)
        {
            var capacity = Math.Max(8, count * 2);
            Array.Resize(ref _gpuReportBodies, capacity); Array.Resize(ref _gpuReportLimits, capacity); Array.Resize(ref _gpuReportCounts, capacity);
            _gpuReportRanges.EnsureCapacity(capacity);
        }
    }
    private void PublishGPU()
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

    private static GPUPhysicsBodyStore.FieldParameters GPUFields(PhysicsAreaFields fields) =>
        new(fields.GravityVector, fields.Gravity, fields.GravityPoint, fields.GravityPointUnitDistance, fields.LinearDamp, fields.AngularDamp,
            fields.GravitySpaceOverride, fields.LinearDampSpaceOverride, fields.AngularDampSpaceOverride, fields.Priority);

    private void PrepareGPUBody(PhysicsBodyRuntime runtime, PhysicsBody? body, double delta, bool captureActivity)
    {
        runtime.Backend.PrepareGPUParameters(runtime);
        if (captureActivity) runtime.ApplyBeforeStep(body);
        else runtime.ActiveBeforeStep = false;
        if (body is AnimatableBody animatable) animatable.PrepareMotion(delta);
        else if (body is CharacterBody character) character.PrepareMotion(delta);
        else if (body is RigidBody rigid) rigid.PrepareFrozenMotion(delta);
        else if (body is null) runtime.Owners.Server!.PrepareMotion(delta);
    }

    private void PrepareGPUReports()
    {
        _gpuReportBodyCount = 0; var contacts = 0;
        foreach (var body in _bodies) Add(body.Backend, body.Runtime);
        foreach (var body in _serverColliders)
            if (!body.IsArea) Add(body.Backend, body.Runtime);

        void Add(PhysicsColliderBackend backend, PhysicsBodyRuntime runtime)
        {
            var limit = runtime.ContactLimit;
            if (limit == 0) return;
            _gpuReportBodies[_gpuReportBodyCount] = backend.GPUHandle;
            _gpuReportLimits[_gpuReportBodyCount++] = limit; contacts = checked(contacts + limit);
        }
        if (_gpuReports.Length < contacts) Array.Resize(ref _gpuReports, Math.Max(8, contacts * 2));
        GPUStore!.CaptureContactReports = _gpuReportBodyCount > 0;
    }
    private void ReadGPUReports()
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
        _gpuChanges = []; _gpuReportBodies = []; _gpuReportLimits = []; _gpuReportCounts = []; _gpuReports = [];
        _gpuQueryExclusions = []; _gpuPointHits = []; _gpuShapeHits = []; _gpuAreaHits = [];
        _gpuShapeQuery[0] = default; _gpuAreaQuery[0] = default;
        GPUStore!.Dispose();
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

    private void StepGPU(double delta)
    {
        _stepping = true; List<Exception>? errors = null; var advanced = false;
        var intervalEntered = false; var intervalSubmissions = 0L;
        var profileMark = ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        if (ProfilingEnabled) _profileAllocated = GC.GetAllocatedBytesForCurrentThread();
        try
        {
            LastStep = (float)delta;
            foreach (var body in _bodies) body.PrepareBackend();
            foreach (var area in _areas) area.PrepareBackend();
            foreach (var body in _serverColliders) body.PrepareBackend();
            foreach (var joint in _joints) joint.PrepareBackend();
            RecordStepPhase(0, ref profileMark);
            var callbacks = false; var pendingForces = false;
            foreach (var body in _bodies)
            {
                var runtime = body.Runtime;
                callbacks |= RequiresBodySnapshot(runtime, body);
                pendingForces |= runtime.PendingForce != Vector2.Zero || runtime.PendingTorque != 0;
            }
            foreach (var body in _serverColliders)
                if (!body.IsArea)
                {
                    var runtime = body.Runtime;
                    callbacks |= RequiresBodySnapshot(runtime, null);
                    pendingForces |= runtime.PendingForce != Vector2.Zero || runtime.PendingTorque != 0;
                }
            var captureActivity = callbacks || pendingForces;
            if (captureActivity) PublishGPU();
            uint areaOrder = 0;
            foreach (var area in _areas) GPUStore!.SetAreaFields(area.Backend.GPUHandle, GPUFields(area.Fields), areaOrder++);
            foreach (var body in _serverColliders)
                if (body.IsArea) GPUStore!.SetAreaFields(body.Backend.GPUHandle, GPUFields(body.AreaFields!), areaOrder++);
            RecordStepPhase(1, ref profileMark);
            _callbackBodies.Clear();
            foreach (var body in _bodies) PrepareGPUBody(body.Runtime, body, delta, captureActivity);
            foreach (var body in _serverColliders)
                if (!body.IsArea) PrepareGPUBody(body.Runtime, null, delta, captureActivity);
            if (callbacks)
            {
                foreach (var body in _bodies) _callbackBodies.Add(new(body.Runtime, body.Backend, body.Backend.AttachmentVersion, body));
                foreach (var body in _serverColliders) if (!body.IsArea) _callbackBodies.Add(new(body.Runtime, body.Backend, body.Backend.AttachmentVersion, null));
            }
            foreach (var joint in _jointRuntimes) joint.ApplySolverPolicy();
            SyncGPUExceptions();
            var prepareMark = ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            if (ProfilingEnabled) GPUPrepareBodiesMS = System.Diagnostics.Stopwatch.GetElapsedTime(profileMark, prepareMark).TotalMilliseconds;
            PrepareGPUReports();
            if (ProfilingEnabled) { GPUPrepareReportsMS = System.Diagnostics.Stopwatch.GetElapsedTime(prepareMark).TotalMilliseconds; prepareMark = System.Diagnostics.Stopwatch.GetTimestamp(); }
            var wakeWait = ProfilingEnabled ? GPUStore!.WaitMS : 0;
            FlushGPUWakes();
            if (ProfilingEnabled) { GPUPrepareWakesMS = System.Diagnostics.Stopwatch.GetElapsedTime(prepareMark).TotalMilliseconds; GPUPrepareWakeWaitMS = GPUStore!.WaitMS - wakeWait; }
            RecordStepPhase(2, ref profileMark);
            intervalSubmissions = GPUStore!.SubmissionCount; intervalEntered = true;
            GPUStore.SimulateFields((float)delta, GPUFields(DefaultAreaFields));
            CaptureDebugContacts();
            RecordStepPhase(3, ref profileMark);
            var statistics = new Statistics(GPUStore.PublishedActiveBodyCount, GPUStore.PairCount, GPUStore.PublishedIslandCount);
            PublishGPU(); ReadGPUReports(); advanced = true;
            RecordStepPhase(4, ref profileMark);
            foreach (var body in _serverColliders)
            {
                if (!body.IsArea) body.Backend.PublishGPUFields(body.Runtime);
                body.CompleteMotion();
            }
            foreach (var body in _bodies)
            {
                try
                {
                    body.Backend.PublishGPUFields(body.Runtime);
                    body.CompleteBackend();
                    if (body is AnimatableBody animatable) animatable.SyncPose();
                    else if (body is CharacterBody character) { character.CaptureSolverPose(); character.SetResolvedGravity(body.Runtime.Gravity); }
                }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
            RecordStepPhase(5, ref profileMark);
            CollectBodyContactRange(0, _bodies.Count, 0, this);
            foreach (var body in _bodies)
                if (body is RigidBody rigid) { if (rigid.TakeSleepChange()) _sleepEvents.Add(rigid); rigid.QueueContactChanges(_contactEvents); }
            ScanGPUAreas(); CaptureBodyStates();
            PhysicsServer.Service.PublishStatistics(this, statistics);
            RecordStepPhase(6, ref profileMark);
        }
        catch (Exception error)
        {
            if (intervalEntered && (GPUStore!.HasFailed || GPUStore.SubmissionCount != intervalSubmissions)) _gpuFailure = error;
            (errors ??= []).Add(error);
        }
        finally { _stepping = false; }
        try { if (advanced) DispatchBodyStates(); else _callbackBodies.Clear(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        try { DispatchEvents(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        RecordStepPhase(7, ref profileMark);
        if (errors is not null) throw new AggregateException("GPU physics-world step failed.", errors);
    }
}
