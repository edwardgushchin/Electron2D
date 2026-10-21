using Box2D.NET;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

/// <summary>Owns the CPU world, retained native collider attachments, policies, field/Area geometry, capacity, statistics, workers and optional diagnostic GPU stages.</summary>
internal sealed partial class CPUPhysicsWorldBackend : PhysicsWorldBackend
{
    private readonly B2WorldId _worldID;
    private B2BodyId _jointWorldBody;
    private readonly PhysicsTaskScheduler _tasks;
    private GPUPhysicsWorld? _stageGPU;
    private bool _disposed;
    internal override bool HasFailure => _solverFailure is not null;
    internal override PhysicsServer.Backend Kind => PhysicsServer.Backend.CPU;
    internal override B2WorldId WorldID { get { ThrowIfDisposed(); return _worldID; } }
    internal override PhysicsTaskScheduler Tasks => _tasks;
    internal override GPUPhysicsWorld? StageGPU => _stageGPU;

    internal CPUPhysicsWorldBackend(PhysicsSpace space, PhysicsServer.Backend requested, string? fallbackReason = null) : base(space, requested, fallbackReason)
    {
        _tasks = new(OperatingSystem.IsBrowser() ? 1 : Math.Min(4, Environment.ProcessorCount));
        try
        {
            var definition = B2Types.b2DefaultWorldDef();
            definition.gravity = PhysicsShapeBackend.ToBackend(space.DefaultGravity);
            definition.restitutionThreshold = 0;
            definition.enableContinuous = false;
            definition.frictionCallback = CPUPhysicsWorldBackend.CombineFriction; definition.restitutionCallback = CPUPhysicsWorldBackend.CombineBounce;
            definition.workerCount = _tasks.WorkerCount;
            definition.enqueueTask = _tasks.Enqueue; definition.finishTask = _tasks.Finish;
            _worldID = b2CreateWorld(definition);
            var world = b2GetWorldFromId(_worldID);
            world.sleepAngularThreshold = space.SleepSettings.AngularThreshold; world.timeToSleep = space.SleepSettings.TimeToSleep;
            world.solverIterations = space.SolverIterations;
            world.contactRecycleRadius = space.ContactSettings.RecycleRadius * PhysicsSpace.MetersPerUnit;
            world.contactMaxSeparation = space.ContactSettings.MaxSeparation * PhysicsSpace.MetersPerUnit;
            world.contactBias = space.ContactSettings.Bias;
            world.contactAllowedPenetration = space.ContactSettings.AllowedPenetration * PhysicsSpace.MetersPerUnit;
            _tasks.Bind(world); world.workerCount = 1;
            b2World_SetPreSolveCallback(_worldID, PreSolveContact, this);
        }
        catch (Exception error)
        {
            try { Dispose(); }
            catch (Exception cleanup) { throw new AggregateException("CPU physics-world creation and cleanup failed.", error, cleanup); }
            throw;
        }
    }
    internal override void EnsureAccess()
    {
        ThrowIfDisposed();
        if (_solverFailure is not null) throw new InvalidOperationException("The CPU numerical physics step failed; dispose this world before creating a replacement.", _solverFailure);
        if (b2GetWorldFromId(_worldID).locked) throw new InvalidOperationException("Physics state is owned by the solver.");
    }
    private readonly Dictionary<PhysicsColliderBackend, CPUPhysicsColliderImplementation> _colliders = [];
    internal CPUPhysicsColliderImplementation NativeCollider(PhysicsColliderBackend collider) =>
        _colliders.TryGetValue(collider, out var value) ? value : throw new InvalidOperationException("The collider has no attachment owned by this CPU world.");
    internal void RetireCollider(PhysicsColliderBackend collider, CPUPhysicsColliderImplementation implementation)
    {
        if (_colliders.TryGetValue(collider, out var current) && ReferenceEquals(current, implementation)) _colliders.Remove(collider);
    }
    internal override PhysicsColliderImplementation CreateCollider(PhysicsColliderBackend collider)
    {
        EnsureAccess();
        var implementation = new CPUPhysicsColliderImplementation(collider, Space, this);
        _colliders.Add(collider, implementation);
        return implementation;
    }
    internal override float ReadPortableSleepTime(PhysicsColliderBackend collider) => NativeCollider(collider).PortableSleepTime;
    internal override void ApplyPortableSleep(PhysicsColliderBackend collider, float time, bool canSleep, bool sleeping) =>
        NativeCollider(collider).ApplyPortableSleep(time, canSleep, sleeping);
    /// <summary>Creates or borrows the fixed joint endpoint and prepares solver and contact-motion capacity for its body slot.</summary>
    internal B2BodyId GetJointWorldBody()
    {
        EnsureAccess();
        if (_jointWorldBody.index1 == 0)
        {
            var definition = B2Types.b2DefaultBodyDef();
            definition.type = B2BodyType.b2_staticBody;
            _jointWorldBody = B2Bodies.b2CreateBody(WorldID, definition);
            // The fixed anchor consumes a body slot just like a collider attachment.
            PrepareSolverCapacity();
        }
        return _jointWorldBody;
    }
    private readonly Dictionary<PhysicsJointBackend, CPUPhysicsJointImplementation> _jointAttachments = [];
    internal CPUPhysicsJointImplementation NativeJoint(PhysicsJointBackend joint) =>
        _jointAttachments.TryGetValue(joint, out var value) ? value : throw new InvalidOperationException("The joint has no attachment owned by this CPU world.");
    internal void RetireJoint(PhysicsJointBackend joint, CPUPhysicsJointImplementation implementation)
    {
        if (_jointAttachments.TryGetValue(joint, out var current) && ReferenceEquals(current, implementation)) _jointAttachments.Remove(joint);
    }
    internal override PhysicsJointImplementation CreateJoint(PhysicsJointBackend joint)
    {
        EnsureAccess(); var implementation = new CPUPhysicsJointImplementation(Space, this, joint);
        _jointAttachments.Add(joint, implementation); return implementation;
    }
    internal override void StepNative(float delta, int substeps) { ThrowIfDisposed(); _nativeStepStarted = true; b2World_Step(_worldID, delta, substeps); }

    internal override GPUPhysicsWorld EnableGPUIntegration()
    {
        EnsureAccess();
        if (_stageGPU is not null) return _stageGPU;
        var gpu = new GPUPhysicsWorld();
        b2GetWorldFromId(_worldID).integrateBodyStage = gpu.Integrate;
        return _stageGPU = gpu;
    }
    internal override GPUPhysicsWorld EnableGPUSolver()
    {
        var gpu = EnableGPUIntegration(); var world = b2GetWorldFromId(_worldID);
        world.integrateBodyStage = null!; world.solveConstraints = gpu.Solve;
        world.generateManifolds = gpu.UpdateContacts; world.findBroadPhasePairs = gpu.FindBroadPhasePairs;
        gpu.EnableContactCreation(world); gpu.EnableIslandSplitting(world); gpu.EnableIslandChanges(world);
        gpu.EnableConstraintColors(world); gpu.EnableBodyFinalization(world);
        return gpu;
    }
    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _fieldAreas.Clear();
        _pointHits.Clear(); _shapeHits.Clear(); _contactPairs.Clear(); _shapeCandidates.Clear(); _queryProxies.Clear(); _motionCandidates.Clear();
        List<Exception>? errors = null;
        try { _tasks.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        try { ReleaseSolverScratch(); } catch (Exception error) { (errors ??= []).Add(error); }
        if (b2World_IsValid(_worldID))
        {
            var world = b2GetWorldFromId(_worldID);
            world.integrateBodyStage = null!; world.solveConstraints = null!;
            world.generateManifolds = null!; world.findBroadPhasePairs = null!;
        }
        try { _stageGPU?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        _stageGPU = null;
        try { if (b2World_IsValid(_worldID)) b2DestroyWorld(_worldID); } catch (Exception error) { (errors ??= []).Add(error); }
        _sleepStorage?.Clear(); _sleepStorage = null;
        if (errors is not null) throw new AggregateException("CPU physics-world cleanup failed.", errors);
    }
    private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(CPUPhysicsWorldBackend)); }
}
