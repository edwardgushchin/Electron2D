namespace Electron2D;

internal sealed partial class GPUPhysicsWorldBackend
{
    internal override int PreparedBodyCapacity => GPUStore.BodySlotCount;
    internal override void SetSleepSettings(in PhysicsSleepSettings settings)
    {
        EnsureAccess(); GPUStore.SetSleepSettings(settings); Space.InvalidateGPUStates();
    }
    internal override void SetContactSettings(in PhysicsContactSettings settings)
    {
        EnsureAccess(); GPUStore.SetContactSettings(settings); Space.InvalidateGPUStates();
    }
    internal override void SetSolverIterations(int value)
    {
        EnsureAccess(); GPUStore.SetSolverIterations(value); Space.InvalidateGPUStates();
    }
    internal override void SetConstraintDefaultBias(float value)
    {
        EnsureAccess(); GPUStore.SetConstraintDefaultBias(value);
        foreach (var joint in Space.SnapshotJoints) joint.ApplySolverPolicy();
    }
    internal override void PrepareSolverCapacity() { EnsureAccess(); Space.PrepareGPUCapacity(); }
    internal override void PrepareMonitoringCapacity() { EnsureAccess(); Space.PrepareGPUCapacity(); }
    internal override PhysicsSpace.Statistics ReadStatistics()
    {
        EnsureAccess(); return new(GPUStore.PublishedActiveBodyCount, GPUStore.PairCount, GPUStore.PublishedIslandCount);
    }
}
