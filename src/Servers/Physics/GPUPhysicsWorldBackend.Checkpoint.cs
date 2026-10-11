namespace Electron2D;

internal sealed partial class GPUPhysicsWorldBackend
{
    internal override void PrepareCheckpoint()
    {
        EnsureAccess();
        foreach (var body in Space.Bodies) GPUStore.NativeCollider(body.Backend).PrepareGPUParameters(body.Runtime);
        uint areaOrder = 0;
        foreach (var area in Space.Areas) GPUStore.SetAreaFields(GPUStore.NativeCollider(area.Backend).GPUHandle, PhysicsSpace.GPUFields(area.Fields), areaOrder++);
        foreach (var body in Space.ServerColliders)
            if (body.IsArea) GPUStore.SetAreaFields(GPUStore.NativeCollider(body.Backend).GPUHandle, PhysicsSpace.GPUFields(body.AreaFields!), areaOrder++);
            else GPUStore.NativeCollider(body.Backend).PrepareGPUParameters(body.Runtime);
        foreach (var joint in Space.SnapshotJoints) joint.ApplySolverPolicy();
        SynchronizeCollisionExceptions();
    }
    internal override PhysicsBackendCheckpoint CreateCheckpoint() => new PhysicsSpace.GPUCheckpoint(Space, GPUStore);
}
