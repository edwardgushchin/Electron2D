namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal readonly record struct ReplayState(GPUPhysicsBodyStore.Snapshot State, bool Valid, bool Published, long Epoch,
        Vector2 Surface, float SurfaceAngular, Vector2 Force, float Torque);
    internal ReplayState CaptureReplay() => (_implementation as GPUPhysicsColliderImplementation)?.CaptureReplay() ?? default;
    internal void RestoreReplay(in ReplayState state)
    {
        if (_implementation is GPUPhysicsColliderImplementation gpu) gpu.RestoreReplay(state);
    }
}
