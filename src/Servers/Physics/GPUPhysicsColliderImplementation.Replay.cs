namespace Electron2D;

internal sealed partial class GPUPhysicsColliderImplementation
{
    internal readonly record struct ReplayState(GPUPhysicsBodyStore.Snapshot State, bool Valid, bool Published, long Epoch,
        Vector2 Surface, float SurfaceAngular, Vector2 Force, float Torque, System.Numerics.Vector4 Pose, bool PoseValid, long PoseEpoch);
    internal ReplayState CaptureReplay() => new(_gpuState, _gpuStateValid, _gpuStateMatchesPublication, _gpuStateEpoch,
        _gpuSurfaceLinear, _gpuSurfaceAngular, _gpuConstantForce, _gpuConstantTorque, _gpuPose, _gpuPoseValid, _gpuPoseEpoch);
    internal void RestoreReplay(in ReplayState state)
    {
        _gpuParametersPrepared = false;
        _gpuPose = state.Pose; _gpuPoseValid = state.PoseValid; _gpuPoseEpoch = state.PoseEpoch;
        _gpuState = state.State; _gpuStateValid = state.Valid; _gpuStateMatchesPublication = state.Published; _gpuStateEpoch = state.Epoch;
        _gpuSurfaceLinear = state.Surface; _gpuSurfaceAngular = state.SurfaceAngular; _gpuConstantForce = state.Force; _gpuConstantTorque = state.Torque;
    }
}
