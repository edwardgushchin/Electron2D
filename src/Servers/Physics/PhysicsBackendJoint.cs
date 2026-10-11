namespace Electron2D;

/// <summary>Defines one selected constraint using logical body identities and immutable solver policy.</summary>
internal abstract class PhysicsBackendJoint
{
    private int _claimed;
    internal void Claim() { if (Interlocked.CompareExchange(ref _claimed, 1, 0) != 0) throw new InvalidOperationException("A joint implementation can belong to only one attachment."); }
    internal abstract bool IsAttached { get; }
    internal abstract void Attach(RID first, RID second, in PhysicsJointConfiguration settings);
    internal abstract void Detach();
    internal abstract void ApplySolverPolicy(in PhysicsJointConfiguration settings);
    internal abstract void ApplyPortableFrames(Transform first, Transform second);
    internal abstract void SetCollideConnected(bool value);
    internal abstract void SetPinLimits(bool enabled, float lower, float upper);
    internal abstract void SetPinMotorEnabled(bool value);
    internal abstract void SetPinMotorVelocity(float value);
    internal abstract void SetPinMotorMaxTorque(float value);
    internal abstract void SetGrooveLimits(float lower, float upper);
}
