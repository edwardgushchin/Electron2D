using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace : IDisposable
{
    internal const float MetersPerUnit = 0.01f;
    internal const float UnitsPerMeter = 100f;
    private const ulong RoughMaterial = 1;
    private const ulong AbsorbentMaterial = 2;

    private readonly List<PhysicsBody> _bodies = [];
    private int _serverAreaCount;
    private readonly List<RigidBody> _contactBodies = [];
    private readonly List<Joint> _joints = [];
    private readonly List<PhysicsJointRuntime> _jointRuntimes = [];
    private B2BodyId _jointWorldBody;
    internal float ConstraintDefaultBias { get; private set; }
    internal void SetConstraintDefaultBias(float value)
    {
        EnsureQueryAccess(); PhysicsJointRuntime.ValidateBias(value);
        if (ConstraintDefaultBias == value) return;
        ConstraintDefaultBias = value; GPUStore?.SetConstraintDefaultBias(value);
        foreach (var joint in _jointRuntimes) joint.ApplySolverPolicy();
    }
    private readonly List<Area> _areas = [];
    private readonly List<PhysicsServerCollider> _serverColliders = [];
    private readonly List<(PhysicsAreaFields Fields, uint Mask, IReadOnlyList<B2ShapeId> Shapes, Transform Transform)> _fieldAreas = [];
    private readonly List<OverlapEvent> _overlapEvents = [];
    private readonly List<ContactEvent> _contactEvents = [];
    private readonly List<RigidBody> _sleepEvents = [];
    private readonly Dictionary<(ulong, ulong), OneWayPair> _oneWayPairs = [];
    private readonly List<(ulong, ulong)> _staleOneWayPairs = [];
    private readonly B2WorldId _worldID;
    private readonly PhysicsTaskScheduler? _tasks;
    private GPUPhysicsWorld? _gpuWorld;
    private Exception? _gpuFailure;

    // Historical CPU-hosted stage controls; public GPU spaces use the independent resident store.
    internal GPUPhysicsWorld EnableGPUIntegration()
    {
        EnsureQueryAccess();
        if (GPUStore is not null) throw new InvalidOperationException("GPU stage controls require a CPU-hosted world.");
        if (_gpuWorld is not null) return _gpuWorld;
        var gpu = new GPUPhysicsWorld();
        b2GetWorldFromId(_worldID).integrateBodyStage = gpu.Integrate;
        return _gpuWorld = gpu;
    }

    internal GPUPhysicsWorld EnableGPUSolver()
    {
        var gpu = EnableGPUIntegration();
        var world = b2GetWorldFromId(_worldID);
        world.integrateBodyStage = null!;
        world.solveConstraints = gpu.Solve;
        world.generateManifolds = gpu.UpdateContacts;
        world.findBroadPhasePairs = gpu.FindBroadPhasePairs;
        gpu.EnableContactCreation(world);
        gpu.EnableIslandSplitting(world);
        gpu.EnableIslandChanges(world);
        gpu.EnableConstraintColors(world);
        gpu.EnableBodyFinalization(world);
        return gpu;
    }
    private Vector2 _defaultGravity;
    private readonly int _ownerThreadID = Environment.CurrentManagedThreadId;
    internal PhysicsAreaFields DefaultAreaFields { get; }
    private bool _stepping;
    private bool _dispatching;
    private bool _dispatchingContacts;
    private bool _disposed;
    private long _contactStep;
    private int _preparedBodyCapacity;
    private int _preparedSleepCapacity;

    internal readonly record struct OverlapEvent(Area Area, PhysicsShapePairChange Change);
    internal readonly record struct ContactEvent(RigidBody Receiver, PhysicsShapePairChange Change);
    private readonly record struct OneWayPair(bool Allowed, long SeenStep);

    internal PhysicsServer.Backend RequestedBackend { get; }
    internal PhysicsServer.Backend ActualBackend => GPUStore is null ? PhysicsServer.Backend.CPU : PhysicsServer.Backend.GPU;
    internal string? BackendFallbackReason { get; }

    internal PhysicsSpace(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU, bool allowCPUFallback = false)
    {
        if (!Enum.IsDefined(backend)) throw new ArgumentOutOfRangeException(nameof(backend));
        RequestedBackend = backend;
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
        if (backend == PhysicsServer.Backend.GPU)
        {
            try
            {
                GPUStore = new();
                GPUStore.SetSleepSettings(SleepSettings); GPUStore.SetContactSettings(ContactSettings);
                GPUStore.SetSolverIterations(SolverIterations); GPUStore.SetConstraintDefaultBias(ConstraintDefaultBias);
            }
            catch (Exception error) when (allowCPUFallback && error is InvalidOperationException or NotSupportedException or DllNotFoundException or EntryPointNotFoundException)
            {
                GPUStore?.Dispose(); GPUStore = null; BackendFallbackReason = error.Message;
            }
            catch { GPUStore?.Dispose(); throw; }
            if (GPUStore is not null) return;
        }
        var definition = b2DefaultWorldDef();
        definition.gravity = PhysicsShapeBackend.ToBackend(_defaultGravity);
        definition.restitutionThreshold = 0;
        definition.enableContinuous = false; // Per-body modes use the shared scene/server trajectory pass.
        definition.frictionCallback = CombineFriction;
        definition.restitutionCallback = CombineBounce;
        _tasks = new(OperatingSystem.IsBrowser() ? 1 : Math.Min(4, Environment.ProcessorCount));
        definition.workerCount = _tasks.WorkerCount;
        definition.enqueueTask = _tasks.Enqueue;
        definition.finishTask = _tasks.Finish;
        _worldID = b2CreateWorld(definition);
        var world = b2GetWorldFromId(_worldID);
        world.sleepAngularThreshold = SleepSettings.AngularThreshold; world.timeToSleep = SleepSettings.TimeToSleep;
        world.solverIterations = SolverIterations;
        world.contactRecycleRadius = ContactSettings.RecycleRadius * MetersPerUnit; world.contactMaxSeparation = ContactSettings.MaxSeparation * MetersPerUnit;
        world.contactBias = ContactSettings.Bias; world.contactAllowedPenetration = ContactSettings.AllowedPenetration * MetersPerUnit;
        _tasks.Bind(world);
        world.workerCount = 1;
        b2World_SetPreSolveCallback(_worldID, PreSolveContact, this);
    }

    private bool _isActive;
    internal bool IsActive => Volatile.Read(ref _isActive);

    internal void SetActive(bool active)
    {
        EnsureQueryAccess();
        Volatile.Write(ref _isActive, active);
    }

    internal B2WorldId WorldID => GPUStore is null ? _worldID : throw new InvalidOperationException("This physics space has no CPU solver world.");
    internal IReadOnlyList<PhysicsBody> Bodies => _bodies;
    internal IReadOnlyList<Area> Areas => _areas;

    internal string? FindAudioBusOverride(Vector2 position, uint mask)
    {
        PrepareForQuery();
        var point = PhysicsShapeBackend.ToBackend(position);
        Area? selected = null;
        foreach (var area in _areas)
        {
            if (!area.AudioBusOverride || (area.CollisionLayer & mask) == 0 ||
                selected is not null && area.PhysicsRID >= selected.PhysicsRID) continue;
            if (GPUStore is not null) { if (GPUAreaContainsPoint(area, position, mask)) selected = area; continue; }
            var shapes = area.BackendShapes;
            for (var i = 0; i < shapes.Count; i++)
                if (b2Shape_TestPoint(shapes[i], point)) { selected = area; break; }
        }
        return selected?.AudioBusName;
    }
    internal IReadOnlyList<PhysicsServerCollider> ServerColliders => _serverColliders;
    internal bool HasBackendFailure => _gpuFailure is not null || _continuousFailure is not null || GPUStore?.HasFailed == true;

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
        if (GPUStore?.HasFailed == true) throw new InvalidOperationException("The GPU physics world failed; dispose it before creating a replacement.");
        if (GPUStore is null && b2GetWorldFromId(_worldID).locked) throw new InvalidOperationException("Physics state is owned by the solver.");
        if (_continuousFailure is not null) throw new InvalidOperationException("The continuous physics step failed; dispose this world before creating a replacement.", _continuousFailure);
        if (_gpuFailure is not null) throw new InvalidOperationException("The GPU physics world failed; dispose it before creating a replacement.", _gpuFailure);
    }

    internal void EnsureWorldBindingChange() { EnsureQueryAccess(); EnsureWorldRelease(); }
    internal void EnsureWorldRelease() { EnsureReleaseAccess(); if (_dispatchingBodyStates) throw new InvalidOperationException("World binding cannot change while live body state callbacks are being dispatched."); }

    internal void PrepareForQuery()
    {
        EnsureQueryAccess();
        foreach (var body in _bodies) body.PrepareBackend();
        foreach (var area in _areas) area.PrepareBackend();
        foreach (var collider in _serverColliders) collider.PrepareBackend();
        SyncGPUExceptions();
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
        _fieldAreas.EnsureCapacity(_areas.Count + _serverColliders.Count);
        if (GPUStore is not null) return;
        var world = b2GetWorldFromId(_worldID);
        foreach (var sensor in world.sensors.data.AsSpan(0, world.sensors.count))
        {
            Box2D.NET.B2Arrays.b2Array_Reserve(ref sensor.hits, world.shapes.count);
            Box2D.NET.B2Arrays.b2Array_Reserve(ref sensor.overlaps1, world.shapes.count);
            Box2D.NET.B2Arrays.b2Array_Reserve(ref sensor.overlaps2, world.shapes.count);
        }

    }

    private void PrepareSolverCapacity()
    {
        if (GPUStore is not null) { PrepareGPUCapacity(); return; }
        var world = b2GetWorldFromId(_worldID);
        var count = world.bodyIdPool.nextIndex;
        var capacity = (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(8, count));
        if (capacity <= _preparedBodyCapacity && count <= _preparedSleepCapacity) return;
        var sleeping = 0;
        foreach (var body in world.bodies.data.AsSpan(0, world.bodies.count))
            if (body.id >= 0 && body.type == B2BodyType.b2_dynamicBody && body.enableSleep) sleeping++;
        var sleepCapacity = sleeping == 0 ? 0 : (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)sleeping);
        if (capacity <= _preparedBodyCapacity && sleepCapacity <= _preparedSleepCapacity) return;
        _preparedBodyCapacity = Math.Max(_preparedBodyCapacity, capacity);
        _preparedSleepCapacity = Math.Max(_preparedSleepCapacity, sleepCapacity);
        capacity = _preparedBodyCapacity; sleepCapacity = _preparedSleepCapacity;
        if (_bodyMotions.Length < capacity) Array.Resize(ref _bodyMotions, capacity);
        // Dormant island storage follows bodies that can sleep; active stress particles need no dormant copies.
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.solverSets, sleepCapacity + 3);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.solverSetIdPool.freeArray, sleepCapacity + 3);
        while (world.solverSets.count < sleepCapacity + 3)
        {
            var index = world.solverSetIdPool.nextIndex++;
            var set = new B2SolverSet { setIndex = B2_NULL_INDEX };
            Box2D.NET.B2Arrays.b2Array_Push(ref world.solverSets, set);
            Box2D.NET.B2IdPools.b2FreeId(world.solverSetIdPool, index);
        }
        var awake = world.solverSets.data[(int)B2SolverSetType.b2_awakeSet];
        Box2D.NET.B2Arrays.b2Array_Reserve(ref awake.bodyStates, capacity);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.bodyMoveEvents, capacity);
        // ponytail: Four contacts per body is the prepared graph budget; larger topologies need explicit capacity preparation.
        var contacts = checked(capacity * 4);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.contacts, contacts);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.contactIdPool.freeArray, contacts);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.islands, capacity);
        Box2D.NET.B2Arrays.b2Array_Reserve(ref world.islandIdPool.freeArray, capacity);
        for (var i = 0; i < world.solverSets.count; i++)
        {
            var set = world.solverSets.data[i];
            // ponytail: Dormant budget is 16 bodies/32 contacts; larger island topologies need explicit preparation.
            Box2D.NET.B2Arrays.b2Array_Reserve(ref set.bodySims, i < 3 ? capacity : 16);
            Box2D.NET.B2Arrays.b2Array_Reserve(ref set.contactSims, i < 3 ? contacts : 32);
            Box2D.NET.B2Arrays.b2Array_Reserve(ref set.jointSims, 4);
            Box2D.NET.B2Arrays.b2Array_Reserve(ref set.islandSims, i < 3 ? capacity : 1);
        }
        foreach (var task in world.taskContexts.data.AsSpan(0, world.taskContexts.count))
        {
            Box2D.NET.B2BitSets.b2SetBitCountAndClear(ref task.contactStateBitSet, contacts);
            Box2D.NET.B2BitSets.b2SetBitCountAndClear(ref task.enlargedSimBitSet, capacity);
            Box2D.NET.B2BitSets.b2SetBitCountAndClear(ref task.awakeIslandBitSet, capacity);
        }
        for (var index = 0; index < world.constraintGraph.colors.Length; index++)
        {
            ref var color = ref world.constraintGraph.colors[index];
            // A regular color contains at most one constraint per dynamic body; overflow has no such bound.
            Box2D.NET.B2Arrays.b2Array_Reserve(ref color.contactSims,
                index == Box2D.NET.B2ConstraintGraphs.B2_OVERFLOW_INDEX ? contacts : capacity);
            if (index != Box2D.NET.B2ConstraintGraphs.B2_OVERFLOW_INDEX && color.bodySet.blockCount < (capacity + 63) / 64)
                Box2D.NET.B2BitSets.b2GrowBitSet(ref color.bodySet, (capacity + 63) / 64);
        }
        if (capacity >= 256) PrepareArenaCapacity(world, capacity, contacts);
    }

    private static void PrepareArenaCapacity(B2World world, int bodies, int contacts)
    {
        var arena = world.arena;
        arena.GetOrCreateFor<int>().Reserve(checked(bodies * 9 + 96));
        arena.GetOrCreateFor<B2MoveResult>().Reserve(bodies + 32);
        arena.GetOrCreateFor<B2MovePair>().Reserve(checked(bodies * 32));
        arena.GetOrCreateFor<B2ContactSim>().Reserve(contacts + 128);
        arena.GetOrCreateFor<B2ContactConstraintSIMD>().Reserve(contacts / B2Cores.B2_SIMD_WIDTH + 128);
        arena.GetOrCreateFor<B2ContactConstraint>().Reserve(contacts);
        arena.GetOrCreateFor<B2SolverBlock>().Reserve((bodies + contacts * 2) / 32 + 512);
        arena.GetOrCreateFor<B2SolverStage>().Reserve(256);
    }

    internal void Add(PhysicsBody body)
    {
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
        DispatchEvents();
    }

    internal void Add(Joint joint)
    {
        EnsureQueryAccess();
        _joints.EnsureCapacity(_joints.Count + 1);
        joint.AttachBackend(this);
        _joints.Add(joint);
    }

    internal void AddJointRuntime(PhysicsJointRuntime runtime) => _jointRuntimes.Add(runtime);
    internal void RemoveJointRuntime(PhysicsJointRuntime runtime) => _jointRuntimes.Remove(runtime);

    internal B2BodyId GetJointWorldBody()
    {
        if (_jointWorldBody.index1 == 0)
        {
            var definition = b2DefaultBodyDef();
            definition.type = B2BodyType.b2_staticBody;
            _jointWorldBody = b2CreateBody(_worldID, definition);
        }
        return _jointWorldBody;
    }

    internal void Remove(Joint joint)
    {
        if (_disposed) return;
        EnsureReleaseAccess();
        if (_joints.Remove(joint)) joint.DetachBackend();
    }

    internal void Add(Area area)
    {
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
        DispatchEvents();
    }

    internal void Remove(Area area)
    {
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Physics areas cannot leave a world while it is stepping.");
        if (!_areas.Remove(area)) return;
        area.DetachBackend();
        area.ClearOverlaps();
        foreach (var other in _areas) other.Forget(area, _overlapEvents);
        ForgetAreaMonitors(area.PhysicsRID);
        DispatchEvents();
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
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        EnsureQueryAccess();
        if (!IsActive || !PhysicsServer.Service.IsActive || delta == 0) return;
        if (_stepping || _dispatchingBodyStates) throw new InvalidOperationException("A physics world cannot step recursively.");
        ResetDebugContacts();
        if (_bodies.Count == 0 && _areas.Count == 0 && _serverColliders.Count == 0)
        {
            PhysicsServer.Service.PublishStatistics(this, default);
            return;
        }
        if (GPUStore is not null) { StepGPU(delta); return; }
        _stepping = true;
        List<Exception>? errors = null;
        var solverAdvanced = false;
        var profileMark = ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        if (ProfilingEnabled) _profileAllocated = GC.GetAllocatedBytesForCurrentThread();
        try
        {
            foreach (var body in _bodies) body.PrepareBackend();
            foreach (var area in _areas) area.PrepareBackend();
            foreach (var collider in _serverColliders) collider.PrepareBackend();
            foreach (var joint in _joints) joint.PrepareBackend();
            RecordStepPhase(0, ref profileMark);
            PrepareAreaFields();
            RecordStepPhase(1, ref profileMark);
            var hasKinematicBodies = PrepareBodyStates(delta);
            var world = b2GetWorldFromId(_worldID);
            world.workerCount = (_gpuWorld is null || world.solveConstraints is not null) && world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodySims.count >= 256 ? _tasks!.WorkerCount : 1;
            RecordStepPhase(2, ref profileMark);
            world.contactBiasDuration = (float)delta;
            StepKinematicPaths(delta, hasKinematicBodies);
            RecordStepPhase(3, ref profileMark);
            solverAdvanced = true; CaptureDebugContacts();
            foreach (var collider in _serverColliders) collider.CompleteMotion();
            foreach (var body in _bodies)
            {
                try
                {
                    body.CompleteBackend();
                    if (body is AnimatableBody animatable) animatable.SyncPose();
                    else if (body is CharacterBody character) character.CaptureSolverPose();
                }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
            RecordStepPhase(4, ref profileMark);
            CaptureBodyMotions();
            if (world.workerCount == 1 || _bodies.Count < 256)
                CollectBodyContactRange(0, _bodies.Count, 0, this);
            else
            {
                var end = _bodies.Count * (world.workerCount - 1) / world.workerCount;
                var contactTask = _tasks!.Enqueue(CollectBodyContacts, end, end / (world.workerCount - 1), this, this);
                try { CollectBodyContactRange(end, _bodies.Count, 0, this); }
                finally { if (contactTask is not null) _tasks.Finish(contactTask, this); }
            }
            foreach (var body in _bodies)
            {
                if (body is not RigidBody rigid) continue;
                if (rigid.TakeSleepChange()) _sleepEvents.Add(rigid);
                rigid.QueueContactChanges(_contactEvents);
            }
            RecordStepPhase(5, ref profileMark);
            ScanAreas();
            ScanAreaMonitors();
            CaptureBodyStates();
            PublishStatistics();
            RecordStepPhase(6, ref profileMark);
        }
        catch (Exception error) { (errors ??= []).Add(error); }
        finally { PruneOneWayPairs(); _stepping = false; }
        try { if (solverAdvanced) DispatchBodyStates(); else _callbackBodies.Clear(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        try { DispatchEvents(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        RecordStepPhase(7, ref profileMark);
        if (errors is not null) throw new AggregateException("Physics-world step failed.", errors);
    }

    private static readonly b2TaskCallback CollectBodyContacts = CollectBodyContactRange;

    private static void CollectBodyContactRange(int start, int end, uint worker, object context)
    {
        var space = (PhysicsSpace)context;
        for (var i = start; i < end; i++)
            if (space._bodies[i] is RigidBody { NeedsContactSnapshot: true } rigid)
                rigid.CollectContacts(rigid.Runtime.GetView(space));
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_stepping || _dispatchingBodyStates) throw new InvalidOperationException("A physics world cannot be disposed during a step.");
        _debugContacts = []; _debugContactLimit = _debugContactCount = 0;
        _tasks?.Dispose();
        if (_continuousTree is not null) Box2D.NET.B2DynamicTrees.b2DynamicTree_Destroy(_continuousTree);
        _continuousTree = null; _continuousBodies.Clear(); _continuousShapes.Clear(); _continuousProxies.Clear(); _continuousBoundaries.Clear();
        _continuousForces.Clear(); _continuousJointBudgets.Clear();
        _continuousBodies.Capacity = _continuousShapes.Capacity = _continuousProxies.Capacity = _continuousBoundaries.Capacity = 0;
        _continuousForces.Capacity = _continuousJointBudgets.Capacity = 0; _continuousFinalize = null;
        if (GPUStore is null)
        {
            b2GetWorldFromId(_worldID).integrateBodyStage = null!;
            b2GetWorldFromId(_worldID).solveConstraints = null!;
            b2GetWorldFromId(_worldID).generateManifolds = null!;
            b2GetWorldFromId(_worldID).findBroadPhasePairs = null!;
        }
        _gpuWorld?.Dispose();
        foreach (var joint in _joints) joint.DetachBackend();
        _joints.Clear();
        while (_jointRuntimes.Count > 0) _jointRuntimes[^1].DetachSpace();
        foreach (var body in _bodies)
        {
            if (body is RigidBody departing) departing.CaptureBackendSleep();
            body.DetachBackend();
            if (body is RigidBody rigid) rigid.ClearContactState();
        }
        foreach (var area in _areas) { area.DetachBackend(); area.ClearOverlaps(); }
        foreach (var collider in _serverColliders) collider.DetachBackend();
        foreach (var body in _bodies) PhysicsServer.Service.NotifyJointBodySpaceChanged(body.PhysicsRID);
        foreach (var collider in _serverColliders)
            if (!collider.IsArea) PhysicsServer.Service.NotifyJointBodySpaceChanged(collider.RID);
        _bodies.Clear();
        _contactBodies.Clear();
        _areas.Clear();
        _serverColliders.Clear();
        _fieldAreas.Clear();
        _overlapEvents.Clear();
        _serverAreaEvents.Clear();
        _contactEvents.Clear();
        _sleepEvents.Clear();
        _oneWayPairs.Clear();
        _staleOneWayPairs.Clear();
        if (GPUStore is null) b2DestroyWorld(_worldID);
        else ReleaseGPUState();
        _disposed = true;
    }

    private static bool PreSolveContact(B2ShapeId first, B2ShapeId second, B2Vec2 point,
        B2Vec2 normal, object context) => ((PhysicsSpace)context).AllowBodyContact(first, second, normal);

    private bool AllowBodyContact(B2ShapeId first, B2ShapeId second, B2Vec2 normal)
    {
        var firstTag = b2Shape_GetUserData(first).GetRef<PhysicsFixtureTag>();
        var secondTag = b2Shape_GetUserData(second).GetRef<PhysicsFixtureTag>();
        if (firstTag is not null && secondTag is not null &&
            PhysicsServer.Service.BodiesExcepted(firstTag.ColliderRID, secondTag.ColliderRID)) return false;
        var firstData = firstTag?.OneWay;
        var secondData = secondTag?.OneWay;
        if (firstData is null && secondData is null) return true;

        lock (_oneWayPairs)
        {
            var firstKey = PackShapeID(first);
            var secondKey = PackShapeID(second);
            var key = firstKey < secondKey ? (firstKey, secondKey) : (secondKey, firstKey);
            if (_oneWayPairs.TryGetValue(key, out var previous))
            {
                _oneWayPairs[key] = previous with { SeenStep = _contactStep };
                return previous.Allowed;
            }

            var allowed = (firstData is null || FacesContact(first, firstData, normal, firstSurface: true)) &&
                (secondData is null || FacesContact(second, secondData, normal, firstSurface: false));
            _oneWayPairs.Add(key, new(allowed, _contactStep));
            return allowed;
        }
    }

    private static bool FacesContact(B2ShapeId shape, OneWayContactData data, B2Vec2 normal, bool firstSurface)
    {
        var local = data.LocalDirection;
        var direction = b2RotateVector(b2Body_GetRotation(b2Shape_GetBody(shape)), new(local.X, local.Y));
        var facing = normal.X * direction.X + normal.Y * direction.Y;
        return firstSurface ? facing < -1e-6f : facing > 1e-6f;
    }

    private static ulong PackShapeID(B2ShapeId shape) =>
        ((ulong)(uint)shape.index1 << 32) | ((ulong)shape.world0 << 16) | shape.generation;

    private void PruneOneWayPairs()
    {
        _staleOneWayPairs.Clear();
        foreach (var pair in _oneWayPairs)
            if (pair.Value.SeenStep != _contactStep) _staleOneWayPairs.Add(pair.Key);
        foreach (var key in _staleOneWayPairs) _oneWayPairs.Remove(key);
    }

    private void ScanAreas()
    {
        // ponytail: Pairwise shape scans are quadratic; use a broad-phase candidate index if large worlds show a measured cost.
        foreach (var area in _areas)
        {
            area.BeginOverlapScan();
            if (area.Monitoring)
            {
                foreach (var body in _bodies)
                    if ((area.CollisionMask & body.CollisionLayer) != 0)
                        ScanOverlapPairs(area, body.GetRID(), body, false, body.BackendShapes);
                foreach (var other in _areas)
                    if (!ReferenceEquals(area, other) && other.Monitorable &&
                        (area.CollisionMask & other.CollisionLayer) != 0)
                        ScanOverlapPairs(area, other.GetRID(), other, true, other.BackendShapes);
                foreach (var other in _serverColliders)
                    if ((!other.IsArea || other.Monitorable) && (area.CollisionMask & other.CollisionLayer) != 0)
                        ScanOverlapPairs(area, other.RID, null, other.IsArea, other.BackendShapes);
            }
            area.CommitOverlapScan(_overlapEvents);
        }
    }

    private void ScanOverlapPairs(Area area, RID rid, CollisionObject? other, bool isArea, IReadOnlyList<B2ShapeId> otherShapes)
    {
        for (var localIndex = 0; localIndex < area.BackendShapes.Count; localIndex++)
        {
            var local = area.BackendShapes[localIndex];
            var localTag = b2Shape_GetUserData(local).GetRef<PhysicsFixtureTag>();
            if (localTag is null) continue;
            for (var remoteIndex = 0; remoteIndex < otherShapes.Count; remoteIndex++)
            {
                var remote = otherShapes[remoteIndex];
                var remoteTag = b2Shape_GetUserData(remote).GetRef<PhysicsFixtureTag>();
                if (remoteTag is not null && ShapePairOverlaps(local, remote))
                    area.Observe(new(rid, remoteTag.ObjectIdentity, isArea, remoteTag.ShapeIndex, localTag.ShapeIndex));
            }
        }
    }

    private void PrepareAreaFields()
    {
        var gravity = DefaultAreaFields.GravityPoint ? Vector2.Zero : DefaultAreaFields.GravityVector * DefaultAreaFields.Gravity;
        if (!gravity.IsFinite()) throw new InvalidOperationException("Default physics gravity exceeds the finite simulation range.");
        if (gravity != _defaultGravity)
        {
            b2World_SetGravity(_worldID, PhysicsShapeBackend.ToBackend(gravity));
            _defaultGravity = gravity;
        }
        // ponytail: Stable insertion order is quadratic in field areas; use indexed sorting if large-world profiling needs it.
        _fieldAreas.Clear();
        foreach (var area in _areas)
            AddFieldArea(area.Fields, area.CollisionMask, area.BackendShapes, area.GlobalTransform);
        foreach (var collider in _serverColliders)
            if (collider.AreaFields is { } fields)
                AddFieldArea(fields, collider.CollisionMask, collider.BackendShapes, collider.GetTransform());

    }

    private void AddFieldArea(PhysicsAreaFields fields, uint mask, IReadOnlyList<B2ShapeId> shapes, Transform transform)
    {
        if (!fields.HasOverrides) return;
        var index = _fieldAreas.Count;
        var area = (fields, mask, shapes, transform);
        _fieldAreas.Add(area);
        while (index > 0 && _fieldAreas[index - 1].Fields.Priority < fields.Priority)
        {
            _fieldAreas[index] = _fieldAreas[index - 1];
            index--;
        }
        _fieldAreas[index] = area;
    }

    private void ResolveAreaFields(uint layer, IReadOnlyList<B2ShapeId> shapes, Vector2 position,
        out Vector2 gravity, out float linearDamp, out float angularDamp)
    {
        gravity = Vector2.Zero;
        linearDamp = 0f;
        angularDamp = 0f;
        var gravityDone = false;
        var linearDone = false;
        var angularDone = false;
        foreach (var area in _fieldAreas)
        {
            if ((area.Mask & layer) == 0 || !ShapesOverlap(area.Shapes, shapes)) continue;
            if (!gravityDone)
            {
                var mode = area.Fields.GravitySpaceOverride;
                if (mode is Area.SpaceOverride.Combine or Area.SpaceOverride.CombineReplace)
                    gravity += area.Fields.ComputeGravity(area.Transform, position);
                else if (mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine)
                    gravity = area.Fields.ComputeGravity(area.Transform, position);
                gravityDone = mode is Area.SpaceOverride.CombineReplace or Area.SpaceOverride.Replace;
            }
            if (!linearDone)
            {
                var mode = area.Fields.LinearDampSpaceOverride;
                if (mode is Area.SpaceOverride.Combine or Area.SpaceOverride.CombineReplace)
                    linearDamp += area.Fields.LinearDamp;
                else if (mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine)
                    linearDamp = area.Fields.LinearDamp;
                linearDone = mode is Area.SpaceOverride.CombineReplace or Area.SpaceOverride.Replace;
            }
            if (!angularDone)
            {
                var mode = area.Fields.AngularDampSpaceOverride;
                if (mode is Area.SpaceOverride.Combine or Area.SpaceOverride.CombineReplace)
                    angularDamp += area.Fields.AngularDamp;
                else if (mode is Area.SpaceOverride.Replace or Area.SpaceOverride.ReplaceCombine)
                    angularDamp = area.Fields.AngularDamp;
                angularDone = mode is Area.SpaceOverride.CombineReplace or Area.SpaceOverride.Replace;
            }
            if (gravityDone && linearDone && angularDone) break;
        }
        if (!gravityDone) gravity += DefaultAreaFields.ComputeGravity(Transform.Identity, position);
        if (!linearDone) linearDamp += DefaultAreaFields.LinearDamp;
        if (!angularDone) angularDamp += DefaultAreaFields.AngularDamp;
    }

    private bool ShapesOverlap(CollisionObject area, CollisionObject other) =>
        ShapesOverlap(area.BackendShapes, other.BackendShapes);

    private bool ShapesOverlap(IReadOnlyList<B2ShapeId> areaShapes, IReadOnlyList<B2ShapeId> otherShapes)
    {
        for (var first = 0; first < areaShapes.Count; first++)
            for (var second = 0; second < otherShapes.Count; second++)
                if (ShapePairOverlaps(areaShapes[first], otherShapes[second])) return true;
        return false;
    }

    private bool ShapePairOverlaps(B2ShapeId first, B2ShapeId second)
    {
        var world = b2GetWorldFromId(_worldID);
        var shapeA = b2GetShape(world, first); var shapeB = b2GetShape(world, second);
        if ((shapeA.type == B2ShapeType.b2_boundaryShape || shapeB.type == B2ShapeType.b2_boundaryShape) &&
            (shapeA.userData.GetRef<PhysicsFixtureTag>()?.SeparationRay is not null || shapeB.userData.GetRef<PhysicsFixtureTag>()?.SeparationRay is not null))
        {
            var directedCache = default(B2SimplexCache);
            return PhysicsSeparationRay.SolverContact(shapeA, b2Body_GetTransform(b2Shape_GetBody(first)), shapeB,
                b2Body_GetTransform(b2Shape_GetBody(second)), ref directedCache).pointCount != 0;
        }
        if (shapeA.type == B2ShapeType.b2_boundaryShape || shapeB.type == B2ShapeType.b2_boundaryShape)
            return B2Boundaries.Contact(b2MakeShapeDistanceProxy(shapeA), b2Body_GetTransform(b2Shape_GetBody(first)),
                b2MakeShapeDistanceProxy(shapeB), b2Body_GetTransform(b2Shape_GetBody(second)), 0).pointCount != 0;
        var aabbA = b2Shape_GetAABB(first);
        var rayA = b2Shape_GetUserData(first).GetRef<PhysicsFixtureTag>()?.SeparationRay;
        var rayB = b2Shape_GetUserData(second).GetRef<PhysicsFixtureTag>()?.SeparationRay;
        var aabbB = b2Shape_GetAABB(second);
        if (rayA is null && rayB is null &&
            (aabbA.upperBound.X < aabbB.lowerBound.X || aabbA.lowerBound.X > aabbB.upperBound.X ||
            aabbA.upperBound.Y < aabbB.lowerBound.Y || aabbA.lowerBound.Y > aabbB.upperBound.Y))
            return false;
        var input = new B2DistanceInput
        {
            proxyA = b2MakeShapeDistanceProxy(shapeA),
            proxyB = b2MakeShapeDistanceProxy(shapeB),
            transformA = b2Body_GetTransform(b2Shape_GetBody(first)),
            transformB = b2Body_GetTransform(b2Shape_GetBody(second)),
            useRadii = true
        };
        if (rayA is not null || rayB is not null)
        {
            var query = rayA is { } ray ? PhysicsSeparationRay.WorldProxy(ray, input.transformA, default) :
                WorldProxy(input.proxyA, input.transformA, default);
            if (PhysicsSeparationRay.PairContact(query, rayA?.SlideOnSlope, input.proxyB,
                input.transformB, rayB, default, 0).pointCount != 0) return true;
            return false;
        }
        var cache = new B2SimplexCache();
        return b2ShapeDistance(ref input, ref cache, null, 0).distance <= 0.1f * B2_LINEAR_SLOP;
    }

    private void DispatchEvents()
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

    internal static void SetMaterial(ref B2ShapeDef definition, PhysicsMaterial? material)
    {
        SetMaterial(ref definition, material?.ComputedFriction ?? 1f, material?.ComputedBounce ?? 0f);
    }

    internal static void SetMaterial(ref B2ShapeDef definition, float friction, float bounce)
    {
        definition.material.friction = MathF.Abs(friction);
        definition.material.restitution = MathF.Abs(bounce);
        definition.material.userMaterialId = (friction < 0 ? RoughMaterial : 0) |
            (bounce < 0 ? AbsorbentMaterial : 0);
    }

    internal static float CombineFriction(float a, ulong aFlags, float b, ulong bFlags) =>
        MathF.Abs(MathF.Min((aFlags & RoughMaterial) != 0 ? -a : a,
            (bFlags & RoughMaterial) != 0 ? -b : b));

    internal static float CombineBounce(float a, ulong aFlags, float b, ulong bFlags) =>
        Math.Clamp(((aFlags & AbsorbentMaterial) != 0 ? -a : a) +
            ((bFlags & AbsorbentMaterial) != 0 ? -b : b), 0f, 1f);
}
