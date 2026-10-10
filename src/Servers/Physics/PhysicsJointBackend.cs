using Box2D.NET;

namespace Electron2D;

/// <summary>Retains the current selected joint attachment while common resource identity stays in its runtime.</summary>
internal sealed partial class PhysicsJointBackend
{
    private PhysicsJointImplementation? _implementation;
    internal PhysicsJointImplementation? Implementation => _implementation;
    private PhysicsJointImplementation Attached => _implementation ?? throw new InvalidOperationException("The joint has no solver attachment.");
    private CPUPhysicsJointImplementation CPU => _implementation as CPUPhysicsJointImplementation ?? throw new InvalidOperationException("The joint has no CPU solver attachment.");
    internal B2JointId ID => (_implementation as CPUPhysicsJointImplementation)?.ID ?? default;
    internal bool IsAttached => _implementation?.IsAttached == true;

    internal static Transform SampleLocalFrame(PhysicsColliderBackend? body, Transform pose, Vector2 point, float angle) =>
        body?.Space is not null ? body.SampleJointLocalFrame(point, angle) : CPUPhysicsJointImplementation.SampleLocalFrame(null, pose, point, angle);

    internal void Attach(PhysicsSpace space, PhysicsColliderBackend first, PhysicsColliderBackend? second, PhysicsJointRuntime settings)
    {
        if (_implementation is not null) throw new InvalidOperationException("A joint already has a backend attachment.");
        _implementation = space.BackendImplementation.CreateJoint();
        try { _implementation.Attach(first, second, settings); }
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
    internal void ApplySolverPolicy(PhysicsJointRuntime settings) => Attached.ApplySolverPolicy(settings);
    internal void ApplyPortableFrames(Transform first, Transform second) => Attached.ApplyPortableFrames(first, second);
    internal void SetCollideConnected(bool value) => Attached.SetCollideConnected(value);
    internal void SetPinLimits(bool enabled, float lower, float upper) => Attached.SetPinLimits(enabled, lower, upper);
    internal void SetPinMotorEnabled(bool value) => Attached.SetPinMotorEnabled(value);
    internal void SetPinMotorVelocity(float value) => Attached.SetPinMotorVelocity(value);
    internal void SetPinMotorMaxTorque(float value) => Attached.SetPinMotorMaxTorque(value);
    internal void SetGrooveLimits(float lower, float upper) => Attached.SetGrooveLimits(lower, upper);
}
