namespace Electron2D;

/// <summary>Owns the independent resident GPU store and its world-step dispatch.</summary>
internal sealed partial class GPUPhysicsWorldBackend : PhysicsWorldBackend
{
    private bool _disposed;
    internal override PhysicsServer.Backend Kind => PhysicsServer.Backend.GPU;
    internal override GPUPhysicsBodyStore GPUStore { get; }
    internal GPUPhysicsWorldBackend(PhysicsSpace space) : base(space, PhysicsServer.Backend.GPU)
    {
        GPUStore = new();
        try
        {
            GPUStore.SetSleepSettings(space.SleepSettings); GPUStore.SetContactSettings(space.ContactSettings);
            GPUStore.SetSolverIterations(space.SolverIterations); GPUStore.SetConstraintDefaultBias(space.ConstraintDefaultBias);
        }
        catch (Exception error)
        {
            try { Dispose(); }
            catch (Exception cleanup) { throw new AggregateException("GPU physics-world creation and cleanup failed.", error, cleanup); }
            throw;
        }
    }
    internal override void EnsureAccess()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(GPUPhysicsWorldBackend));
        if (GPUStore.HasFailed) throw new InvalidOperationException("The GPU physics world failed; dispose it before creating a replacement.");
    }
    internal override void Step(double delta) => Space.StepGPU(delta);
    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pointHits.Clear(); _shapeHits.Clear(); _contactPairs.Clear();
        GPUStore.Dispose();
    }
}
