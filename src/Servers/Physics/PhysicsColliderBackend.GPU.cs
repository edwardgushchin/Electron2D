namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal bool GPUSensor => GPUAttachment.GPUSensor;
    internal uint GPUMask => GPUAttachment.GPUMask;
    internal GPUPhysicsBodyStore.BodyHandle GPUHandle => (_implementation as GPUPhysicsColliderImplementation)?.GPUHandle ?? default;
    internal List<(GPUPhysicsBodyStore.ShapeHandle Handle, int Slot, GPUPhysicsBodyStore.QueryGeometry? Query, Transform Pose)> GPUShapes => GPUAttachment.GPUShapes;
    internal bool GPUStateValid { get => GPUAttachment.GPUStateValid; set => GPUAttachment.GPUStateValid = value; }
    internal void AcceptGPUState(in GPUPhysicsBodyStore.Snapshot state, bool published = false) => GPUAttachment.AcceptGPUState(state, published);
    internal void CompleteGPUStatePublication() => GPUAttachment.CompleteGPUStatePublication();
    internal void RefreshGPUIdentity()
    {
        if (_implementation is GPUPhysicsColliderImplementation gpu) gpu.RefreshGPUIdentity();
    }
    internal void SetGPUConstants(Vector2 force, float torque) => GPUAttachment.SetGPUConstants(force, torque);
    internal void PrepareGPUParameters(PhysicsBodyRuntime runtime, bool force = false) => GPUAttachment.PrepareGPUParameters(runtime, force);
    internal void PublishGPUFields(PhysicsBodyRuntime runtime) => GPUAttachment.PublishGPUFields(runtime);
}
