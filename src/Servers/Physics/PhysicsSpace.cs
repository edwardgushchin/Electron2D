using Box2D.NET;

namespace Electron2D;

internal sealed partial class PhysicsSpace : IDisposable
{
    internal const float MetersPerUnit = 0.01f;
    internal const float UnitsPerMeter = 100f;

    private readonly List<PhysicsBody> _bodies = [];
    private int _serverAreaCount;
    private readonly List<RigidBody> _contactBodies = [];
    private readonly List<Joint> _joints = [];
    private readonly List<PhysicsJointRuntime> _jointRuntimes = [];
    internal float ConstraintDefaultBias { get; private set; }
    internal void SetConstraintDefaultBias(float value)
    {
        EnsureQueryAccess(); EnsureImplementationWriteAccess(); PhysicsJointRuntime.ValidateBias(value);
        if (ConstraintDefaultBias == value) return;
        ConstraintDefaultBias = value; _backend.SetConstraintDefaultBias(value);
    }
    private readonly List<Area> _areas = [];
    private readonly List<PhysicsServerCollider> _serverColliders = [];
    private readonly List<OverlapEvent> _overlapEvents = [];
    private readonly List<ContactEvent> _contactEvents = [];
    private readonly List<RigidBody> _sleepEvents = [];
    private PhysicsWorldBackend _backend = null!;
    private bool _initializing = true;
    private bool _backendCreationStarted;
    private Exception? _gpuFailure;
    internal PhysicsWorldBackend BackendImplementation => _backend ?? throw new InvalidOperationException("The physics implementation is still being created.");
    internal GPUPhysicsWorld EnableGPUIntegration() { EnsureQueryAccess(); return _backend.EnableGPUIntegration(); }
    internal GPUPhysicsWorld EnableGPUSolver() { EnsureQueryAccess(); return _backend.EnableGPUSolver(); }
    private Vector2 _defaultGravity;
    private readonly int _ownerThreadID = Environment.CurrentManagedThreadId;
    internal PhysicsAreaFields DefaultAreaFields { get; }
    private bool _stepping;
    private bool _dispatching;
    private bool _dispatchingContacts;
    private bool _disposed;
    internal ulong Tick { get; private set; }

    internal readonly record struct OverlapEvent(Area Area, PhysicsShapePairChange Change);
    internal readonly record struct ContactEvent(RigidBody Receiver, PhysicsShapePairChange Change);

    internal PhysicsServer.Backend RequestedBackend => BackendImplementation.Requested;
    internal PhysicsServer.Backend ActualBackend => BackendImplementation.Kind;
    internal string? BackendFallbackReason => BackendImplementation.FallbackReason;
    internal string RequestedBackendName { get; private set; } = string.Empty;
    internal string ActualBackendName { get; private set; } = string.Empty;

    internal PhysicsSpace(RID rid)
    {
        RID = rid;
        var settings = ProjectSettings.Service;
        SleepSettings = PhysicsSleepSettings.FromProject(); SleepSettings.Validate();
        ContactSettings = PhysicsContactSettings.FromProject(); ContactSettings.Validate();
        SolverIterations = settings.GetWithOverrideCore(ProjectSettings.Physics2DSolverIterations);
        ConstraintDefaultBias = settings.GetWithOverrideCore(ProjectSettings.Physics2DDefaultConstraintBias);
        DefaultAreaFields = new(settings.GetWithOverrideCore(ProjectSettings.Physics2DDefaultGravity),
            settings.GetWithOverrideCore(ProjectSettings.Physics2DDefaultGravityVector))
        {
            LinearDamp = settings.GetWithOverrideCore(ProjectSettings.Physics2DDefaultLinearDamp),
            AngularDamp = settings.GetWithOverrideCore(ProjectSettings.Physics2DDefaultAngularDamp),
            Priority = -1
        };
        _defaultGravity = DefaultAreaFields.ComputeGravity(Transform.Identity, Vector2.Zero);
        if (!_defaultGravity.IsFinite()) throw new InvalidOperationException("Default physics gravity exceeds the finite simulation range.");
    }

    internal void InitializeBackend(PhysicsServer.Backend backend, bool allowCPUFallback,
        Func<PhysicsSpace, PhysicsWorldBackend>? factory, Action<PhysicsSpace>? initialize, PhysicsBackendRegistry.Selection? selection = null)
    {
        EnsureReleaseAccess();
        if (_backendCreationStarted) throw new InvalidOperationException("A physics implementation can be initialized only once.");
        _backendCreationStarted = true;
        try
        {
            if (factory is not null && selection is not null) throw new ArgumentException("A space cannot select two physics factories.");
            RequestedBackendName = selection?.RequestedName ?? backend.ToString();
            if (selection is { } selected) selected.Registration.Create(this, allowCPUFallback, Initialize);
            else Initialize(factory is null ? PhysicsWorldBackend.Create(this, backend, allowCPUFallback) : factory(this));
            void Initialize(PhysicsWorldBackend created)
            {
                if (created is null || !created.OwnsSpace(this)) throw new InvalidOperationException("A factory must return an implementation owned by this physics space.");
                _backend = created;
                ActualBackendName = selection is { Registration.BuiltIn: null } custom ? custom.Registration.Name : created.Kind.ToString();
                BeginBackendCallback();
                try { created.Initialize(); }
                finally { EndExtensionCallback(); }
                initialize?.Invoke(this);
            }
        }
        finally { _initializing = false; }
    }

    internal Vector2 DefaultGravity => _defaultGravity;
    internal void SetDefaultGravity(Vector2 gravity) => _defaultGravity = gravity;
    internal void CommitAreaScan(Area area) => area.CommitOverlapScan(_overlapEvents);

    internal bool IsActive => _backend?.IsActive == true;
    private bool _changingActive;

    internal void SetActive(bool active)
    {
        EnsureQueryAccess();
        EnsureImplementationWriteAccess();
        EnsureInitializationComplete();
        if (_changingActive) throw new InvalidOperationException("Physics activation cannot reenter its selected implementation.");
        _changingActive = true;
        try { _backend.SetActive(active); }
        finally { _changingActive = false; }
    }

    internal B2WorldId WorldID => BackendImplementation.WorldID;
    internal List<PhysicsBody> Bodies => _bodies;
    internal List<Area> Areas => _areas;
    internal List<RigidBody> ContactBodies => _contactBodies;

    internal string? FindAudioBusOverride(Vector2 position, uint mask)
    {
        PrepareForQuery();
        Area? selected = null;
        foreach (var area in _areas)
            if (area.AudioBusOverride && (area.CollisionLayer & mask) != 0 &&
                (selected is null || area.PhysicsRID < selected.PhysicsRID) && _backend.AreaContainsPoint(area, position, mask))
                selected = area;
        return selected?.AudioBusName;
    }
    internal List<PhysicsServerCollider> ServerColliders => _serverColliders;
    internal bool HasBackendFailure => _checkpointFailure is not null || _gpuFailure is not null || (_backend?.HasFailure ?? false);

    internal void EnsureReleaseAccess()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (Environment.CurrentManagedThreadId != _ownerThreadID)
            throw new InvalidOperationException("Physics queries require the space owner thread.");
        if (_stepping) throw new InvalidOperationException("A physics space cannot be queried while stepping.");
    }

    internal void EnsureQueryAccess()
    {
        EnsureReleaseAccess();
        if (_checkpointFailure is not null) throw new InvalidOperationException("Physics checkpoint restore failed; dispose this world before creating a replacement.", _checkpointFailure);
        BackendImplementation.EnsureAccess();
        if (_gpuFailure is not null) throw new InvalidOperationException("The GPU physics world failed; dispose it before creating a replacement.", _gpuFailure);
    }

    internal void EnsureWorldBindingChange() { EnsureQueryAccess(); EnsureWorldRelease(); }
    private int _extensionCallbackDepth;
    private int _implementationCallbackDepth;
    internal void BeginExtensionCallback() { EnsureQueryAccess(); _extensionCallbackDepth++; }
    internal void BeginImplementationCallback(bool cleanup = false)
    {
        if (_disposed && !cleanup) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (Environment.CurrentManagedThreadId != _ownerThreadID) throw new InvalidOperationException("Physics implementations require the space owner thread.");
        _extensionCallbackDepth++;
        _implementationCallbackDepth++;
    }
    internal void EndImplementationCallback() { _implementationCallbackDepth--; EndExtensionCallback(); }
    internal void EnsureImplementationWriteAccess()
    {
        if (_implementationCallbackDepth != 0) throw new InvalidOperationException("Physics authoring cannot change while a numerical physics implementation hook is borrowed.");
    }
    internal void EnsureImplementationRelease()
    {
        if (Environment.CurrentManagedThreadId != _ownerThreadID || _stepping || _extensionCallbackDepth != 0)
            throw new InvalidOperationException("A selected physics implementation cannot be released off-owner or while borrowed.");
    }
    internal void EndExtensionCallback() => _extensionCallbackDepth--;
    internal void BeginBackendCallback() { EnsureReleaseAccess(); _extensionCallbackDepth++; }
    internal void BeginBackendPublication()
    {
        if (_disposed || !_stepping || !BackendImplementation.ResultsReady || Environment.CurrentManagedThreadId != _ownerThreadID)
            throw new InvalidOperationException("Selected body publication requires this world's completed solver interval.");
        _extensionCallbackDepth++;
    }
    private int _contactResultDepth;
    internal void BeginContactResultPublication()
    {
        if (HasBackendFailure) throw new InvalidOperationException("A failed world cannot publish contact results.");
        BeginBackendPublication(); _contactResultDepth++;
    }
    internal void EndContactResultPublication() { _contactResultDepth--; EndExtensionCallback(); }
    internal void EnsureContactResultAccess()
    {
        if (_contactResultDepth == 0) { EnsureQueryAccess(); return; }
        if (_disposed || !_stepping || HasBackendFailure || !BackendImplementation.ResultsReady || Environment.CurrentManagedThreadId != _ownerThreadID)
            throw new InvalidOperationException("Contact result construction requires the owner of a completed selected interval.");
    }
    internal void EnsureExtensionHierarchyChange()
    {
        if (_extensionCallbackDepth != 0) throw new InvalidOperationException("Physics bindings and scene hierarchy cannot change while a physics extension hook is borrowed.");
    }
    private bool DispatchingCallbacks => _backend?.IsFlushingQueries == true || _extensionCallbackDepth != 0 || _dispatchingBodyStates || _dispatching || _dispatchingContacts || _dispatchingServerAreas;
    internal void EnsureWorldRelease()
    {
        EnsureReleaseAccess();
        EnsureInitializationComplete();
        if (DispatchingCallbacks) throw new InvalidOperationException("World binding cannot change while physics callbacks are being dispatched.");
    }

    private void EnsureInitializationComplete()
    {
        if (_initializing) throw new InvalidOperationException("The physics implementation is still initializing.");
    }
    internal void EnsureAttachmentAccess() { EnsureQueryAccess(); EnsureInitializationComplete(); EnsureExtensionHierarchyChange(); }

    internal void PrepareForQuery()
    {
        EnsureQueryAccess();
        foreach (var body in _bodies) body.PrepareBackend();
        foreach (var area in _areas) area.PrepareBackend();
        foreach (var collider in _serverColliders) collider.PrepareBackend();
        _backend.SynchronizeCollisionExceptions();
    }

    internal void PrepareMonitoringCapacity()
    {
        var objects = _bodies.Count + _areas.Count + _serverColliders.Count;
        var shapes = 0; var contactEvents = 0;
        _contactBodies.Clear();
        foreach (var body in _bodies)
        {
            shapes += body.Backend.ShapeCount;
            if (body is not RigidBody rigid) continue;
            var limit = rigid.MaxContactsReported;
            contactEvents += checked(limit * 4);
            if (limit > 0 || rigid.ContactMonitor) _contactBodies.Add(rigid);
        }
        foreach (var area in _areas) shapes += area.Backend.ShapeCount;
        foreach (var collider in _serverColliders) shapes += collider.Backend.ShapeCount;
        var overlapEvents = 0; var serverEvents = 0;
        foreach (var area in _areas)
        {
            var pairs = checked(area.Backend.ShapeCount * shapes);
            area.PrepareOverlaps(objects, pairs); overlapEvents += checked(pairs * 4);
            if (PhysicsServer.Service.FindAreaRuntime(area.PhysicsRID) is { } runtime) { runtime.Prepare(pairs); serverEvents += checked(pairs * 2); }
        }
        foreach (var collider in _serverColliders)
            if (collider.IsArea && PhysicsServer.Service.FindAreaRuntime(collider.RID) is { } runtime)
            {
                var pairs = checked(collider.Backend.ShapeCount * shapes);
                runtime.Prepare(pairs); serverEvents += checked(pairs * 2);
            }
        _contactEvents.EnsureCapacity(contactEvents); _sleepEvents.EnsureCapacity(_bodies.Count);
        _overlapEvents.EnsureCapacity(overlapEvents); _serverAreaEvents.EnsureCapacity(serverEvents);
        _backend.PrepareMonitoringCapacity();
    }

    private void PrepareSolverCapacity() => _backend.PrepareSolverCapacity();

    internal void Add(PhysicsBody body)
    {
        EnsureAttachmentAccess();
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (_stepping) throw new InvalidOperationException("Physics bodies cannot enter a world while it is stepping.");
        PhysicsServer.Service.EnsureJointBodyMembershipChange(body.PhysicsRID);
        _bodies.EnsureCapacity(_bodies.Count + 1);
        body.AttachBackend(this);
        _bodies.Add(body);
        PrepareSolverCapacity();
        if (_areas.Count == 0 && _serverColliders.Count == 0 && (body is not RigidBody rigid || rigid.MaxContactsReported == 0))
            _sleepEvents.EnsureCapacity(_bodies.Count);
        else PrepareMonitoringCapacity();
        foreach (var joint in _joints) joint.BodyArrived();
        PhysicsServer.Service.NotifyJointBodySpaceChanged(body.PhysicsRID);
    }

    internal void Remove(PhysicsBody body)
    {
        EnsureExtensionHierarchyChange();
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Physics bodies cannot leave a world while it is stepping.");
        EnsureReleaseAccess();
        PhysicsServer.Service.EnsureJointBodyMembershipChange(body.PhysicsRID, releasing: true);
        if (_bodies.Count > 0 && ReferenceEquals(_bodies[^1], body)) _bodies.RemoveAt(_bodies.Count - 1);
        else if (!_bodies.Remove(body)) return;
        foreach (var runtime in _jointRuntimes) runtime.BodyLeaving(body.PhysicsRID);
        foreach (var joint in _joints) joint.BodyLeaving(body);
        if (body is RigidBody departing) { _contactBodies.Remove(departing); departing.CaptureBackendSleep(); }
        body.DetachBackend();
        if (!HasBackendFailure) PhysicsServer.Service.NotifyJointBodySpaceChanged(body.PhysicsRID);
        if (body is RigidBody removed) removed.ClearContactState();
        foreach (var rigid in _contactBodies) rigid.ForgetContact(body.PhysicsRID, _contactEvents);
        foreach (var area in _areas) area.Forget(body, _overlapEvents);
        ForgetAreaMonitors(body.PhysicsRID);
        _backend.FlushQueryEvents();
    }

    internal void Add(Joint joint)
    {
        EnsureAttachmentAccess();
        _joints.EnsureCapacity(_joints.Count + 1);
        joint.AttachBackend(this);
        _joints.Add(joint);
    }

    internal void AddJointRuntime(PhysicsJointRuntime runtime) => _jointRuntimes.Add(runtime);
    internal void RemoveJointRuntime(PhysicsJointRuntime runtime) => _jointRuntimes.Remove(runtime);


    internal void Remove(Joint joint)
    {
        EnsureExtensionHierarchyChange();
        if (_disposed) return;
        EnsureReleaseAccess();
        if (_joints.Remove(joint)) joint.DetachBackend();
    }

    internal void Add(Area area)
    {
        EnsureAttachmentAccess();
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (_stepping) throw new InvalidOperationException("Physics areas cannot enter a world while it is stepping.");
        _areas.EnsureCapacity(_areas.Count + 1);
        area.AttachBackend(this);
        _areas.Add(area);
        PrepareSolverCapacity();
        PrepareMonitoringCapacity();
    }

    internal void Add(PhysicsServerCollider collider, RID spaceRID)
    {
        EnsureAttachmentAccess();
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (_stepping) throw new InvalidOperationException("Server colliders cannot enter while stepping.");
        if (!collider.IsArea) PhysicsServer.Service.EnsureJointBodyMembershipChange(collider.RID);
        _serverColliders.EnsureCapacity(_serverColliders.Count + 1);
        collider.AttachBackend(this, spaceRID);
        _serverColliders.Add(collider);
        if (collider.IsArea) _serverAreaCount++;
        PrepareSolverCapacity();
        if (_areas.Count != 0 || _serverAreaCount != 0) PrepareMonitoringCapacity();
        if (!collider.IsArea) PhysicsServer.Service.NotifyJointBodySpaceChanged(collider.RID);
    }

    internal void Remove(PhysicsServerCollider collider)
    {
        EnsureExtensionHierarchyChange();
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Server colliders cannot leave while stepping.");
        if (!collider.IsArea) PhysicsServer.Service.EnsureJointBodyMembershipChange(collider.RID, releasing: true);
        if (_serverColliders.Count != 0 && ReferenceEquals(_serverColliders[^1], collider))
            _serverColliders.RemoveAt(_serverColliders.Count - 1);
        else if (!_serverColliders.Remove(collider)) return;
        if (collider.IsArea) _serverAreaCount--;
        if (!collider.IsArea) foreach (var runtime in _jointRuntimes) runtime.BodyLeaving(collider.RID);
        foreach (var rigid in _contactBodies) rigid.ForgetContact(collider.RID, _contactEvents);
        foreach (var area in _areas) area.ForgetRID(collider.RID, _overlapEvents);
        ForgetAreaMonitors(collider.RID);
        collider.DetachBackend();
        if (!HasBackendFailure && !collider.IsArea) PhysicsServer.Service.NotifyJointBodySpaceChanged(collider.RID);
        _backend.FlushQueryEvents();
    }

    internal void Remove(Area area)
    {
        EnsureExtensionHierarchyChange();
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Physics areas cannot leave a world while it is stepping.");
        if (!_areas.Remove(area)) return;
        area.DetachBackend();
        area.ClearOverlaps();
        foreach (var other in _areas) other.Forget(area, _overlapEvents);
        ForgetAreaMonitors(area.PhysicsRID);
        _backend.FlushQueryEvents();
    }

    internal static bool ProfilingEnabled;
    internal readonly double[] ProfileMS = new double[8];
    internal readonly long[] ProfileBytes = new long[8];
    private long _profileAllocated;

    private void RecordStepPhase(int index, ref long mark)
    {
        if (!ProfilingEnabled) return;
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        ProfileBytes[index] = allocated - _profileAllocated;
        _profileAllocated = allocated;
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        ProfileMS[index] = System.Diagnostics.Stopwatch.GetElapsedTime(mark, now).TotalMilliseconds;
        mark = now;
    }

    internal void Step(double delta)
    {
        EnsureInitializationComplete();
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        EnsureQueryAccess();
        if (_stepping || DispatchingCallbacks) throw new InvalidOperationException("A physics world cannot step recursively.");
        if (!IsActive || !PhysicsServer.Service.IsActive || delta == 0) return;
        if (Tick == ulong.MaxValue) throw new InvalidOperationException("The physics world tick counter is exhausted.");
        ResetDebugContacts();
        _backend.Step(delta);
    }

    internal static void CollectBodyContactRange(int start, int end, uint worker, object context)
    {
        var space = (PhysicsSpace)context;
        for (var i = start; i < end; i++)
            if (space._bodies[i] is RigidBody { NeedsContactSnapshot: true } rigid)
                rigid.CollectContacts(rigid.Runtime.GetView(space));
    }

    public void Dispose()
    {
        if (_disposed) return;
        EnsureWorldRelease();
        List<Exception>? errors = null;
        foreach (var map in _snapshotMaps.ToArray()) Release(map.Dispose);
        _snapshotMaps.Clear();
        foreach (var checkpoint in _checkpoints.ToArray()) Release(checkpoint.Dispose);
        _checkpoints.Clear();
        _debugContacts = []; _debugContactLimit = _debugContactCount = 0;
        foreach (var joint in _joints.ToArray()) Release(joint.DetachBackend);
        _joints.Clear();
        foreach (var runtime in _jointRuntimes.ToArray()) Release(runtime.DetachSpace);
        _jointRuntimes.Clear();
        foreach (var body in _bodies.ToArray())
        {
            if (body is RigidBody departing) Release(departing.CaptureBackendSleep);
            Release(body.DetachBackend);
            if (body is RigidBody rigid) Release(rigid.ClearContactState);
        }
        foreach (var area in _areas.ToArray()) { Release(area.DetachBackend); Release(area.ClearOverlaps); }
        foreach (var collider in _serverColliders.ToArray()) Release(collider.DetachBackend);
        foreach (var body in _bodies) Release(() => PhysicsServer.Service.NotifyJointBodySpaceChanged(body.PhysicsRID));
        foreach (var collider in _serverColliders)
            if (!collider.IsArea) Release(() => PhysicsServer.Service.NotifyJointBodySpaceChanged(collider.RID));
        _bodies.Clear(); _contactBodies.Clear(); _areas.Clear(); _serverColliders.Clear();
        _overlapEvents.Clear(); _serverAreaEvents.Clear(); _contactEvents.Clear(); _sleepEvents.Clear();
        _portableOneWayKeys.Clear();
        _shapePairs.Clear();
        if (GPUStore is not null) Release(ReleaseGPUState);
        _disposed = true;
        if (_backend is not null) Release(_backend.Dispose);
        if (errors is not null) throw new AggregateException("Physics-world cleanup failed.", errors);

        void Release(Action action)
        {
            try { action(); }
            catch (Exception error) { _checkpointFailure ??= error; (errors ??= []).Add(error); }
        }
    }

    internal void DispatchBackendEvents()
    {
        List<Exception>? errors = null;
        try { DispatchContactEvents(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        try { DispatchOverlapEvents(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        try { DispatchAreaMonitors(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Physics callbacks failed.", errors);
    }

    private void DispatchContactEvents()
    {
        if (_dispatchingContacts || (_sleepEvents.Count == 0 && _contactEvents.Count == 0)) return;
        _dispatchingContacts = true;
        List<Exception>? errors = null;
        try
        {
            foreach (var body in _sleepEvents)
            {
                if (!body.IsInsideTree) continue;
                try { body.RaiseSleepingStateChanged(); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
            _sleepEvents.Clear();
            for (var index = 0; index < _contactEvents.Count; index++)
            {
                var change = _contactEvents[index];
                if (!change.Receiver.IsInsideTree || !change.Receiver.ContactMonitor ||
                    (change.Change.Entered && !change.Receiver.ContainsContact(change.Change))) continue;
                try { change.Receiver.RaiseContact(change.Change); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        finally
        {
            _sleepEvents.Clear();
            _contactEvents.Clear();
            _dispatchingContacts = false;
        }
        if (errors is not null) throw new AggregateException("Rigid-body contact callbacks failed.", errors);
    }

    private void DispatchOverlapEvents()
    {
        if (_dispatching || _overlapEvents.Count == 0) return;
        _dispatching = true;
        List<Exception>? errors = null;
        try
        {
            for (var index = 0; index < _overlapEvents.Count; index++)
            {
                var change = _overlapEvents[index];
                if (!change.Area.IsInsideTree || (change.Change.Entered && !change.Area.ContainsOverlap(change.Change))) continue;
                try { change.Area.RaiseOverlap(change.Change); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        finally
        {
            _overlapEvents.Clear();
            _dispatching = false;
        }
        if (errors is not null) throw new AggregateException("Physics-area overlap callbacks failed.", errors);
    }

}
