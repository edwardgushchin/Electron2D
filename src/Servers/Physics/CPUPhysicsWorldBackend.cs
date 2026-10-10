using Box2D.NET;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

/// <summary>Owns the CPU world, retained worker scheduler and optional diagnostic GPU stages.</summary>
internal sealed partial class CPUPhysicsWorldBackend : PhysicsWorldBackend
{
    private readonly B2WorldId _worldID;
    private readonly PhysicsTaskScheduler _tasks;
    private GPUPhysicsWorld? _stageGPU;
    private bool _disposed;
    internal override PhysicsServer.Backend Kind => PhysicsServer.Backend.CPU;
    internal override B2WorldId WorldID { get { ThrowIfDisposed(); return _worldID; } }
    internal override PhysicsTaskScheduler Tasks => _tasks;
    internal override GPUPhysicsWorld? StageGPU => _stageGPU;

    internal CPUPhysicsWorldBackend(PhysicsSpace space, PhysicsServer.Backend requested, string? fallbackReason = null) : base(space, requested, fallbackReason)
    {
        _tasks = new(OperatingSystem.IsBrowser() ? 1 : Math.Min(4, Environment.ProcessorCount));
        try
        {
            var definition = space.CreateCPUDefinition();
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
            space.InstallCPUCallbacks(_worldID);
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
        if (b2GetWorldFromId(_worldID).locked) throw new InvalidOperationException("Physics state is owned by the solver.");
    }
    internal override void Step(double delta) => Space.StepCPU(delta);
    internal override void StepNative(float delta, int substeps) { ThrowIfDisposed(); b2World_Step(_worldID, delta, substeps); }

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
        _pointHits.Clear(); _shapeHits.Clear(); _contactPairs.Clear(); _shapeCandidates.Clear(); _queryProxies.Clear();
        List<Exception>? errors = null;
        try { _tasks.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        if (b2World_IsValid(_worldID))
        {
            var world = b2GetWorldFromId(_worldID);
            world.integrateBodyStage = null!; world.solveConstraints = null!;
            world.generateManifolds = null!; world.findBroadPhasePairs = null!;
        }
        try { _stageGPU?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        _stageGPU = null;
        try { if (b2World_IsValid(_worldID)) b2DestroyWorld(_worldID); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("CPU physics-world cleanup failed.", errors);
    }
    private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(CPUPhysicsWorldBackend)); }
}
