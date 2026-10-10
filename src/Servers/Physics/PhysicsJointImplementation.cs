namespace Electron2D;

/// <summary>Owns one concrete constraint created by its selected immutable world implementation.</summary>
internal abstract class PhysicsJointImplementation(PhysicsSpace space)
{
    protected PhysicsSpace Space { get; } = space;
    internal abstract bool IsAttached { get; }
    internal abstract void Attach(PhysicsColliderBackend first, PhysicsColliderBackend? second, PhysicsJointRuntime settings);
    internal abstract void Detach();
    internal abstract void ApplySolverPolicy(PhysicsJointRuntime settings);
    internal abstract void ApplyPortableFrames(Transform first, Transform second);
    internal abstract void SetCollideConnected(bool value);
    internal abstract void SetPinLimits(bool enabled, float lower, float upper);
    internal abstract void SetPinMotorEnabled(bool value);
    internal abstract void SetPinMotorVelocity(float value);
    internal abstract void SetPinMotorMaxTorque(float value);
    internal abstract void SetGrooveLimits(float lower, float upper);
}
