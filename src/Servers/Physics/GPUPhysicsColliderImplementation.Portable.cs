namespace Electron2D;

internal sealed partial class GPUPhysicsColliderImplementation
{
    internal override Transform PortablePose
    {
        get
        {
            ref readonly var state = ref GPUState;
            return new(new(state.Pose.Z, state.Pose.W), new(-state.Pose.W, state.Pose.Z), state.Position);
        }
    }
    internal override void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping,
        Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized)
    {
        GPU.SetPortableMotion(GPUHandle, pose, linear, angular, sleepTime, canSleep, sleeping, surface, surfaceAngular, force, torque, gravity, linearDamp, angularDamp, fieldsInitialized);
        _gpuSurfaceLinear = surface; _gpuSurfaceAngular = surfaceAngular; _gpuConstantForce = force; _gpuConstantTorque = torque;
        GPUStateValid = false; _gpuPoseValid = false;
    }
    internal GPUPhysicsBodyStore.ShapeHandle PortableGPUShape(int slot)
    {
        foreach (var shape in GPUShapes) if (shape.Slot == slot) return shape.Handle;
        throw new InvalidDataException("Snapshot references an unavailable collision shape.");
    }
    internal override void ValidatePortablePiece(int slot, int piece) => GPU.ValidatePortablePiece(PortableGPUShape(slot), piece);
}
