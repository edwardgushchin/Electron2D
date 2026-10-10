namespace Electron2D;

internal sealed partial class GPUPhysicsColliderImplementation
{
    internal PhysicsColliderBackend.ReplayState CaptureReplay() => new(_gpuState, _gpuStateValid, _gpuStateMatchesPublication, _gpuStateEpoch,
        _gpuSurfaceLinear, _gpuSurfaceAngular, _gpuConstantForce, _gpuConstantTorque);
    internal void RestoreReplay(in PhysicsColliderBackend.ReplayState state)
    {
        _gpuParametersPrepared = false;
        _gpuState = state.State; _gpuStateValid = state.Valid; _gpuStateMatchesPublication = state.Published; _gpuStateEpoch = state.Epoch;
        _gpuSurfaceLinear = state.Surface; _gpuSurfaceAngular = state.SurfaceAngular; _gpuConstantForce = state.Force; _gpuConstantTorque = state.Torque;
    }
}
