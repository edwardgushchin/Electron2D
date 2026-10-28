namespace Electron2D;

internal sealed partial class GPUPhysicsWorldBackend
{
    internal override void PrepareCheckpoint()
    {
        EnsureAccess(); Space.MaterializeGPUContactReports();
        foreach (var body in Space.Bodies) SynchronizeIntegrationInputs(GPUStore.NativeCollider(body.Backend), body.Runtime);
        uint areaOrder = 0;
        foreach (var area in Space.Areas) GPUStore.SetAreaFields(GPUStore.NativeCollider(area.Backend).GPUHandle, PhysicsSpace.GPUFields(area.Fields), areaOrder++);
        foreach (var body in Space.ServerColliders)
            if (body.IsArea) GPUStore.SetAreaFields(GPUStore.NativeCollider(body.Backend).GPUHandle, PhysicsSpace.GPUFields(body.AreaFields!), areaOrder++);
            else SynchronizeIntegrationInputs(GPUStore.NativeCollider(body.Backend), body.Runtime);
        foreach (var joint in Space.SnapshotJoints) joint.ApplySolverPolicy();
        SynchronizeCollisionExceptions();
    }
    internal override PhysicsBackendCheckpoint CreateCheckpoint() => new PhysicsSpace.GPUCheckpoint(Space, GPUStore);
}
