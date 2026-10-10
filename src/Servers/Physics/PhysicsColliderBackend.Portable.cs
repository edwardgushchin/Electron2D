using Box2D.NET;

namespace Electron2D;

internal sealed partial class PhysicsColliderBackend
{
    internal Transform PortablePose => Attached.PortablePose;
    internal float PortableSleepTime => CPU.PortableSleepTime;
    internal void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping,
        Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized) =>
        Attached.ApplyPortableMotion(pose, linear, angular, sleepTime, canSleep, sleeping, surface, surfaceAngular, force, torque, gravity, linearDamp, angularDamp, fieldsInitialized);
    internal void ApplyPortableSleep(float time, bool canSleep, bool sleeping)
    {
        if (_implementation is CPUPhysicsColliderImplementation cpu) cpu.ApplyPortableSleep(time, canSleep, sleeping);
    }
    internal B2ShapeId PortableCPUShape(int slot, int piece) => CPU.PortableCPUShape(slot, piece);
    internal GPUPhysicsBodyStore.ShapeHandle PortableGPUShape(int slot) => GPUAttachment.PortableGPUShape(slot);
    internal void ValidatePortablePiece(int slot, int piece) => Attached.ValidatePortablePiece(slot, piece);
}
