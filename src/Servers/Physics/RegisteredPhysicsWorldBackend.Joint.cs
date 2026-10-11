namespace Electron2D;

internal sealed partial class RegisteredPhysicsWorldBackend
{
    private sealed class Joint(RegisteredPhysicsWorldBackend world, PhysicsJointBackend owner, PhysicsSpace space, PhysicsBackendJoint implementation)
        : PhysicsJointImplementation(space)
    {
        internal override bool IsAttached { get { world.Enter(); try { return implementation.IsAttached; } finally { world.Exit(); } } }
        internal override void ApplySolverPolicy(in PhysicsJointConfiguration settings) { world.Enter(); try { implementation.ApplySolverPolicy(settings); } finally { world.Exit(); } }
        internal override void ApplyPortableFrames(Transform first, Transform second) { world.Enter(); try { implementation.ApplyPortableFrames(first, second); } finally { world.Exit(); } }
        internal override void SetCollideConnected(bool value) { world.Enter(); try { implementation.SetCollideConnected(value); } finally { world.Exit(); } }
        internal override void SetPinLimits(bool enabled, float lower, float upper) { world.Enter(); try { implementation.SetPinLimits(enabled, lower, upper); } finally { world.Exit(); } }
        internal override void SetPinMotorEnabled(bool value) { world.Enter(); try { implementation.SetPinMotorEnabled(value); } finally { world.Exit(); } }
        internal override void SetPinMotorVelocity(float value) { world.Enter(); try { implementation.SetPinMotorVelocity(value); } finally { world.Exit(); } }
        internal override void SetPinMotorMaxTorque(float value) { world.Enter(); try { implementation.SetPinMotorMaxTorque(value); } finally { world.Exit(); } }
        internal override void SetGrooveLimits(float lower, float upper) { world.Enter(); try { implementation.SetGrooveLimits(lower, upper); } finally { world.Exit(); } }
        internal override void Attach(PhysicsColliderBackend first, PhysicsColliderBackend? second, in PhysicsJointConfiguration settings)
        { world.Enter(); try { implementation.Attach(first.RID, second?.RID ?? default, settings); } finally { world.Exit(); } }
        internal override void Detach()
        {
            world.Enter();
            try { implementation.Detach(); }
            finally { world.Exit(); if (world._joints.TryGetValue(owner, out var current) && ReferenceEquals(current, this)) world._joints.Remove(owner); }
        }
    }
}
