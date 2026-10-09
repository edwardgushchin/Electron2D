namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal readonly record struct ReplayState(GPUPhysicsBodyStore.Snapshot State, bool Valid, bool Published, long Epoch,
        Vector2 Surface, float SurfaceAngular, Vector2 Force, float Torque);
    internal ReplayState CaptureReplay() => new(_gpuState, _gpuStateValid, _gpuStateMatchesPublication, _gpuStateEpoch,
        _gpuSurfaceLinear, _gpuSurfaceAngular, _gpuConstantForce, _gpuConstantTorque);
    internal void RestoreReplay(in ReplayState state)
    {
        _gpuState = state.State; _gpuStateValid = state.Valid; _gpuStateMatchesPublication = state.Published; _gpuStateEpoch = state.Epoch;
        _gpuSurfaceLinear = state.Surface; _gpuSurfaceAngular = state.SurfaceAngular; _gpuConstantForce = state.Force; _gpuConstantTorque = state.Torque;
    }
}
