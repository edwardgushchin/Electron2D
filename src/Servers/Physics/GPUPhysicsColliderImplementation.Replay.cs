namespace Electron2D;

internal sealed partial class GPUPhysicsColliderImplementation
{
    internal readonly record struct ReplayState(GPUPhysicsBodyStore.Snapshot State, bool Valid, bool Published, long Epoch,
        Vector2 Surface, float SurfaceAngular, Vector2 Force, float Torque, System.Numerics.Vector4 Pose, bool PoseValid, long PoseEpoch,
        bool Active, bool ActivityValid, long ActivityEpoch);
    internal ReplayState CaptureReplay() => new(_gpuState, _gpuStateValid, _gpuStateMatchesPublication, _gpuStateEpoch,
        _gpuSurfaceLinear, _gpuSurfaceAngular, _gpuConstantForce, _gpuConstantTorque, _gpuPose, _gpuPoseValid, _gpuPoseEpoch,
        _gpuActive, _gpuActivityValid, _gpuActivityEpoch);
    internal void RestoreReplay(in ReplayState state)
    {
        _gpuParametersPrepared = false;
        _gpuPose = state.Pose; _gpuPoseValid = state.PoseValid; _gpuPoseEpoch = state.PoseEpoch;
        _gpuActive = state.Active; _gpuActivityValid = state.ActivityValid; _gpuActivityEpoch = state.ActivityEpoch;
        _gpuState = state.State; _gpuStateValid = state.Valid; _gpuStateMatchesPublication = state.Published; _gpuStateEpoch = state.Epoch;
        _gpuSurfaceLinear = state.Surface; _gpuSurfaceAngular = state.SurfaceAngular; _gpuConstantForce = state.Force; _gpuConstantTorque = state.Torque;
    }
}
