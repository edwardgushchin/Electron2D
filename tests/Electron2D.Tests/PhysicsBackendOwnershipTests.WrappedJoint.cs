using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private sealed class WrappedJoint(PhysicsSpace space, PhysicsJointImplementation inner) : PhysicsJointImplementation(space)
    {
        internal PhysicsJointConfiguration Last;
        internal int Attachments, Policies;
        internal override bool IsAttached => inner.IsAttached;
        internal override void Attach(PhysicsColliderBackend first, PhysicsColliderBackend? second, in PhysicsJointConfiguration settings)
        { Last = settings; Attachments++; inner.Attach(first, second, settings); }
        internal override void ApplySolverPolicy(in PhysicsJointConfiguration settings)
        { Last = settings; Policies++; inner.ApplySolverPolicy(settings); }
        internal override void Detach() => inner.Detach();
        internal override void ApplyPortableFrames(Transform first, Transform second) => inner.ApplyPortableFrames(first, second);
        internal override void SetCollideConnected(bool value) => inner.SetCollideConnected(value);
        internal override void SetPinLimits(bool enabled, float lower, float upper) => inner.SetPinLimits(enabled, lower, upper);
        internal override void SetPinMotorEnabled(bool value) => inner.SetPinMotorEnabled(value);
        internal override void SetPinMotorVelocity(float value) => inner.SetPinMotorVelocity(value);
        internal override void SetPinMotorMaxTorque(float value) => inner.SetPinMotorMaxTorque(value);
        internal override void SetGrooveLimits(float lower, float upper) => inner.SetGrooveLimits(lower, upper);
    }
}
