namespace Electron2D;

internal sealed partial class GPUPhysicsWorldBackend
{
    internal override PhysicsBackendCheckpoint CreateCheckpoint() => new SolverCheckpoint(GPUStore.CreateCheckpoint());
    private sealed class SolverCheckpoint(GPUPhysicsBodyStore.Checkpoint kernel) : PhysicsBackendCheckpoint
    {
        internal override long DeviceCapacityBytes => kernel.DeviceCapacityBytes;
        internal override void Capture() => kernel.Capture();
        internal override void ValidateRestore() => kernel.ValidateRestore();
        internal override void Restore() => kernel.Restore();
        public override void Dispose() => kernel.Dispose();
    }
}
