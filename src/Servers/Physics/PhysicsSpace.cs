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
    private bool _constraintDefaultBiasDirty;
    internal void SetConstraintDefaultBias(float value)
    {
        EnsureQueryAccess(); EnsureImplementationWriteAccess(); PhysicsJointRuntime.ValidateBias(value);
        if (ConstraintDefaultBias == value && !_constraintDefaultBiasDirty) return;
        ConstraintDefaultBias = value;
        _constraintDefaultBiasDirty = true;
        PrepareConstraintDefaultBias();
    }
    private void PrepareConstraintDefaultBias()
    {
        if (!_constraintDefaultBiasDirty) return;
        EnsureImplementationWriteAccess();
        _backend.SetConstraintDefaultBias(ConstraintDefaultBias);
        _constraintDefaultBiasDirty = false;
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
    private readonly bool _separateSolverThread;
    private PhysicsSolverThread? _solverThread;
    private int _solverBorrowThreadID;
    private bool IsSolverThread => _solverBorrowThreadID != 0 && Environment.CurrentManagedThreadId == _solverBorrowThreadID;
    internal bool IsNumericalInputThread => _implementationCallbackDepth != 0 && IsSolverThread;
    internal void EnsureSceneInputOwner(SceneTree tree)
    {
        if (!tree.HasOwnerThread(_ownerThreadID)) throw new InvalidOperationException("Scene physics inputs require the same world owner.");
    }
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

    internal PhysicsSpace(RID rid, bool separateSolverThread = false)
    {
        RID = rid;
        _separateSolverThread = separateSolverThread;
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
    internal bool IsFlushingQueries
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (Environment.CurrentManagedThreadId != _ownerThreadID)
                throw new InvalidOperationException("Physics notification status requires the space owner thread.");
            return _dispatchingBodyStates || _backend?.IsFlushingQueries == true;
        }
    }
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
        if (_sceneTransformTree is not null) throw new InvalidOperationException("Physics state is borrowed by a parallel scene transform stage.");
        if (_stepping) throw new InvalidOperationException("A physics space cannot be queried while stepping.");
    }

    private SceneTree? _sceneTransformTree;
    internal void BeginSceneTransformAuthoring(SceneTree tree)
    {
        EnsureInitializationComplete(); EnsureReleaseAccess(); EnsureImplementationWriteAccess();
        EnsureBackendAccess();
        if (_extensionCallbackDepth != 0 || DispatchingCallbacks) throw new InvalidOperationException("Scene transform authoring cannot overlap a backend or body-state callback.");
        if (!tree.HasOwnerThread(_ownerThreadID)) throw new InvalidOperationException("A scene transform stage requires the same world owner.");
        _sceneTransformTree = tree;
    }
    internal void EndSceneTransformAuthoring() => _sceneTransformTree = null;
    internal void EnsureSceneTransformWriteAccess(SceneTree? tree)
    {
        if (_sceneTransformTree is { } borrowed && ReferenceEquals(borrowed, tree) && ReferenceEquals(SceneTree.CurrentProcessingGroup?.Tree, tree) &&
            !_stepping && _extensionCallbackDepth == 0) return;
        EnsureImplementationWriteAccess();
    }

    internal bool IsOwnerThread => Environment.CurrentManagedThreadId == _ownerThreadID;
    internal void EnsureResourceGeometryAccess()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Volatile.Read(ref _stepping) || Volatile.Read(ref _implementationCallbackDepth) != 0)
            throw new InvalidOperationException("Resource geometry cannot change during physics stepping or a numerical implementation hook.");
    }

    internal void EnsureQueryAccess()
    {
        EnsureReleaseAccess();
        EnsureBackendAccess();
    }
    // Common terminal failures apply inside allowed callbacks without imposing the public query phase.
    internal void EnsureBackendAccess()
    {
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
        if (Environment.CurrentManagedThreadId != _ownerThreadID && !IsSolverThread) throw new InvalidOperationException("Physics implementations require the owner or its borrowed solver thread.");
        _extensionCallbackDepth++;
        _implementationCallbackDepth++;
    }
    internal void EndImplementationCallback() { _implementationCallbackDepth--; EndExtensionCallback(); }
    internal void EnsureImplementationInputReadAccess()
    {
        if (_implementationCallbackDepth == 0) { EnsureQueryAccess(); return; }
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Environment.CurrentManagedThreadId != _ownerThreadID && !IsSolverThread)
            throw new InvalidOperationException("Physics implementation inputs require the owner or its borrowed solver thread.");
    }
    internal void EnsureImplementationWriteAccess()
    {
        if (Environment.CurrentManagedThreadId != _ownerThreadID) throw new InvalidOperationException("Physics authoring requires the space owner thread.");
        if (_sceneTransformTree is not null) throw new InvalidOperationException("Native physics authoring cannot overlap a parallel scene transform stage.");
        if (_implementationCallbackDepth != 0) throw new InvalidOperationException("Physics authoring cannot change while a numerical physics implementation hook is borrowed.");
    }
    internal void EnsureImplementationRelease()
    {
        if (Environment.CurrentManagedThreadId != _ownerThreadID || _stepping || _extensionCallbackDepth != 0)
            throw new InvalidOperationException("A selected physics implementation cannot be released off-owner or while borrowed.");
    }
    internal void EnsureDirectStateRelease()
    {
        if (!_disposed && _implementationCallbackDepth != 0)
            throw new InvalidOperationException("Direct physics views cannot be disposed while their world's numerical implementation is borrowed.");
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
        PrepareWorldPolicies();
        PreparePendingColliderMetadata();
        foreach (var body in _bodies) body.PrepareBackend();
        foreach (var area in _areas) area.PrepareBackend();
        foreach (var collider in _serverColliders) collider.PrepareBackend();
        _backend.SynchronizeCollisionExceptions();
    }

    private bool _colliderMetadataDirty;
    internal void MarkColliderMetadataDirty() => _colliderMetadataDirty = true;
    private void PreparePendingColliderMetadata()
    {
        if (!_colliderMetadataDirty) return;
        foreach (var body in _bodies)
        {
            body.Backend.PreparePendingMotion();
            body.PreparePendingServerMode();
            body.PreparePendingPose();
            body.Backend.PrepareMetadata(body.EffectiveCollisionLayer, body.EffectiveCollisionMask);
        }
        foreach (var area in _areas) area.Backend.PrepareMetadata(area.CollisionLayer, area.CollisionMask);
        foreach (var collider in _serverColliders)
        {
            collider.Backend.PreparePendingMotion();
            collider.PreparePendingMode();
            collider.PreparePendingPose();
            collider.Backend.PrepareMetadata(collider.CollisionLayer, collider.CollisionMask);
        }
        _colliderMetadataDirty = false;
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
        try
        {
            PrepareSolverCapacity();
            if (_areas.Count == 0 && _serverColliders.Count == 0 && (body is not RigidBody rigid || rigid.MaxContactsReported == 0))
                _sleepEvents.EnsureCapacity(_bodies.Count);
            else PrepareMonitoringCapacity();
            foreach (var joint in _joints) joint.BodyArrived();
            PhysicsServer.Service.NotifyJointBodySpaceChanged(body.PhysicsRID);
        }
        catch (Exception error)
        {
            try { Remove(body, captureMotion: false); }
            catch (Exception cleanup) { throw new AggregateException("Body initialization and cleanup failed.", error, cleanup); }
            throw;
        }
    }

    internal void Remove(PhysicsBody body, bool captureMotion = true)
    {
        EnsureExtensionHierarchyChange();
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Physics bodies cannot leave a world while it is stepping.");
        EnsureReleaseAccess();
        PhysicsServer.Service.EnsureJointBodyMembershipChange(body.PhysicsRID, releasing: true);
        if (_bodies.Count > 0 && ReferenceEquals(_bodies[^1], body)) _bodies.RemoveAt(_bodies.Count - 1);
        else if (!_bodies.Remove(body)) return;
        List<Exception>? errors = null;
        foreach (var runtime in _jointRuntimes) Cleanup(() => runtime.BodyLeaving(body.PhysicsRID), ref errors);
        foreach (var joint in _joints) Cleanup(() => joint.BodyLeaving(body), ref errors);
        if (body is RigidBody departing) { _contactBodies.Remove(departing); if (captureMotion) Cleanup(departing.CaptureBackendSleep, ref errors); }
        if (captureMotion) Cleanup(body.DetachBackend, ref errors);
        else Cleanup(() => body.DetachBackend(captureMotion: false), ref errors);
        Cleanup(() => { if (!HasBackendFailure) PhysicsServer.Service.NotifyJointBodySpaceChanged(body.PhysicsRID); }, ref errors);
        if (body is RigidBody removed) Cleanup(removed.ClearContactState, ref errors);
        foreach (var rigid in _contactBodies) Cleanup(() => rigid.ForgetContact(body.PhysicsRID, _contactEvents), ref errors);
        foreach (var area in _areas) Cleanup(() => area.Forget(body, _overlapEvents), ref errors);
        Cleanup(() => ForgetAreaMonitors(body.PhysicsRID), ref errors);
        Cleanup(_backend.FlushQueryEvents, ref errors);
        FinishCleanup(errors);
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
        try { PrepareSolverCapacity(); PrepareMonitoringCapacity(); }
        catch (Exception error)
        {
            try { Remove(area); }
            catch (Exception cleanup) { throw new AggregateException("Area initialization and cleanup failed.", error, cleanup); }
            throw;
        }
    }

    internal void Add(PhysicsServerCollider collider)
    {
        EnsureAttachmentAccess();
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (_stepping) throw new InvalidOperationException("Server colliders cannot enter while stepping.");
        if (!collider.IsArea) PhysicsServer.Service.EnsureJointBodyMembershipChange(collider.RID);
        _serverColliders.EnsureCapacity(_serverColliders.Count + 1);
        collider.AttachBackend(this);
        _serverColliders.Add(collider);
        if (collider.IsArea) _serverAreaCount++;
        try
        {
            PrepareSolverCapacity();
            if (_areas.Count != 0 || _serverAreaCount != 0) PrepareMonitoringCapacity();
            if (!collider.IsArea) PhysicsServer.Service.NotifyJointBodySpaceChanged(collider.RID);
        }
        catch (Exception error)
        {
            try { Remove(collider, captureMotion: false); }
            catch (Exception cleanup) { throw new AggregateException("Collider initialization and cleanup failed.", error, cleanup); }
            throw;
        }
    }

    internal void Remove(PhysicsServerCollider collider, bool captureMotion = true)
    {
        EnsureExtensionHierarchyChange();
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Server colliders cannot leave while stepping.");
        if (!collider.IsArea) PhysicsServer.Service.EnsureJointBodyMembershipChange(collider.RID, releasing: true);
        if (_serverColliders.Count != 0 && ReferenceEquals(_serverColliders[^1], collider))
            _serverColliders.RemoveAt(_serverColliders.Count - 1);
        else if (!_serverColliders.Remove(collider)) return;
        if (collider.IsArea) _serverAreaCount--;
        List<Exception>? errors = null;
        if (!collider.IsArea) foreach (var runtime in _jointRuntimes) Cleanup(() => runtime.BodyLeaving(collider.RID), ref errors);
        foreach (var rigid in _contactBodies) Cleanup(() => rigid.ForgetContact(collider.RID, _contactEvents), ref errors);
        foreach (var area in _areas) Cleanup(() => area.ForgetRID(collider.RID, _overlapEvents), ref errors);
        Cleanup(() => ForgetAreaMonitors(collider.RID), ref errors);
        if (captureMotion) Cleanup(collider.DetachBackend, ref errors);
        else Cleanup(() => collider.DetachBackend(captureMotion: false), ref errors);
        Cleanup(() => { if (!HasBackendFailure && !collider.IsArea) PhysicsServer.Service.NotifyJointBodySpaceChanged(collider.RID); }, ref errors);
        Cleanup(_backend.FlushQueryEvents, ref errors);
        FinishCleanup(errors);
    }

    internal void Remove(Area area)
    {
        EnsureExtensionHierarchyChange();
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Physics areas cannot leave a world while it is stepping.");
        if (!_areas.Remove(area)) return;
        List<Exception>? errors = null;
        Cleanup(area.DetachBackend, ref errors);
        Cleanup(area.ClearOverlaps, ref errors);
        foreach (var other in _areas) Cleanup(() => other.Forget(area, _overlapEvents), ref errors);
        Cleanup(() => ForgetAreaMonitors(area.PhysicsRID), ref errors);
        Cleanup(_backend.FlushQueryEvents, ref errors);
        FinishCleanup(errors);
    }

    internal static void Cleanup(Action action, ref List<Exception>? errors)
    {
        try { action(); }
        catch (Exception error) { (errors ??= []).Add(error); }
    }

    internal static void FinishCleanup(List<Exception>? errors)
    {
        if (errors is null) return;
        if (errors.Count == 1) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        throw new AggregateException("Physics-resource cleanup failed.", errors);
    }

    internal static bool ProfilingEnabled;
    internal readonly double[] ProfileMS = new double[8];
    internal readonly long[] ProfileBytes = new long[8];
    internal readonly double[] ProfileCompletionMS = new double[3];
    internal bool ForceSerialContactPublication;
    private void RecordCompletionPhase(int index, ref long mark)
    {
        if (!ProfilingEnabled) return;
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        ProfileCompletionMS[index] = System.Diagnostics.Stopwatch.GetElapsedTime(mark, now).TotalMilliseconds; mark = now;
    }
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
        PrepareWorldPolicies();
        PreparePendingColliderMetadata();
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
        if (_solverThread is not null) Release(_solverThread.Dispose);
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
        PhysicsSpace.FinishCleanup(errors);

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
