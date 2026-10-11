namespace Electron2D;

/// <summary>Retains the current selected joint attachment while common resource identity stays in its runtime.</summary>
internal sealed partial class PhysicsJointBackend(RID rid = default)
{
    internal RID RID { get; } = rid;
    private PhysicsJointImplementation? _implementation;
    internal PhysicsJointImplementation? Implementation => _implementation;
    private PhysicsJointImplementation Attached => _implementation ?? throw new InvalidOperationException("The joint has no solver attachment.");
    internal bool IsAttached => _implementation?.IsAttached == true;

    internal static Transform SampleLocalFrame(PhysicsColliderBackend? body, Transform pose, Vector2 point, float angle) =>
        body?.Space is not null ? body.SampleJointLocalFrame(point, angle) : CPUPhysicsJointImplementation.SampleDetachedLocalFrame(pose, point, angle);

    internal void Attach(PhysicsSpace space, PhysicsColliderBackend first, PhysicsColliderBackend? second, PhysicsJointRuntime settings)
    {
        if (_implementation is not null) throw new InvalidOperationException("A joint already has a backend attachment.");
        _implementation = space.BackendImplementation.CreateJoint(this);
        try { _implementation.Attach(first, second, settings.Configuration); }
        catch (Exception error)
        {
            try { Detach(); }
            catch (Exception cleanup) { throw new AggregateException("Joint attachment and cleanup failed.", error, cleanup); }
            throw;
        }
    }
    internal void Detach()
    {
        try { _implementation?.Detach(); }
        finally { _implementation = null; }
    }
    internal void ApplySolverPolicy(PhysicsJointRuntime settings) => Attached.ApplySolverPolicy(settings.Configuration);
    internal void ApplyPortableFrames(Transform first, Transform second) => Attached.ApplyPortableFrames(first, second);
    internal void SetCollideConnected(bool value) => Attached.SetCollideConnected(value);
    internal void SetPinLimits(bool enabled, float lower, float upper) => Attached.SetPinLimits(enabled, lower, upper);
    internal void SetPinMotorEnabled(bool value) => Attached.SetPinMotorEnabled(value);
    internal void SetPinMotorVelocity(float value) => Attached.SetPinMotorVelocity(value);
    internal void SetPinMotorMaxTorque(float value) => Attached.SetPinMotorMaxTorque(value);
    internal void SetGrooveLimits(float lower, float upper) => Attached.SetGrooveLimits(lower, upper);
}
