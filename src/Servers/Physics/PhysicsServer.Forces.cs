namespace Electron2D;

public sealed partial class PhysicsServer
{
    private PhysicsBodyRuntime ForceRuntime(RID body, bool prepareGeometry = false)
    {
        ThrowIfDisposed();
        var runtime = BodyRuntime(body);
        runtime.PrepareForceAccess(prepareGeometry);
        return runtime;
    }

    internal void BodyApplyCentralForceCore(RID body, Vector2 force)
    {
        PhysicsBodyRuntime.Finite(force);
        ForceRuntime(body, true).AddForce(force, 0);
    }

    internal void BodyApplyForceCore(RID body, Vector2 force, Vector2 position = default)
    {
        PhysicsBodyRuntime.Finite(force); PhysicsBodyRuntime.Finite(position);
        var runtime = ForceRuntime(body, true);
        var moment = (position - runtime.CenterOffset).Cross(force);
        PhysicsBodyRuntime.Finite(moment);
        runtime.AddForce(force, moment);
    }

    internal void BodyApplyTorqueCore(RID body, float torque)
    {
        PhysicsBodyRuntime.Finite(torque);
        ForceRuntime(body, true).AddForce(Vector2.Zero, torque);
    }

    internal void BodyApplyCentralImpulseCore(RID body, Vector2 impulse)
    {
        PhysicsBodyRuntime.Finite(impulse);
        ForceRuntime(body, true).ApplyImpulse(impulse, 0);
    }

    internal void BodyApplyImpulseCore(RID body, Vector2 impulse, Vector2 position = default)
    {
        PhysicsBodyRuntime.Finite(impulse); PhysicsBodyRuntime.Finite(position);
        var runtime = ForceRuntime(body, true);
        var moment = (position - runtime.CenterOffset).Cross(impulse);
        PhysicsBodyRuntime.Finite(moment);
        runtime.ApplyImpulse(impulse, moment);
    }

    internal void BodyApplyTorqueImpulseCore(RID body, float impulse)
    {
        PhysicsBodyRuntime.Finite(impulse);
        ForceRuntime(body, true).ApplyImpulse(Vector2.Zero, impulse);
    }

    internal void BodyAddConstantCentralForceCore(RID body, Vector2 force)
    {
        PhysicsBodyRuntime.Finite(force);
        var runtime = ForceRuntime(body);
        runtime.SetConstants(runtime.GetConstantForce() + force, runtime.GetConstantTorque(), true);
    }

    internal void BodyAddConstantForceCore(RID body, Vector2 force, Vector2 position = default)
    {
        PhysicsBodyRuntime.Finite(force); PhysicsBodyRuntime.Finite(position);
        var runtime = ForceRuntime(body, true);
        runtime.SetConstants(runtime.GetConstantForce() + force,
            runtime.GetConstantTorque() + (position - runtime.CenterOffset).Cross(force), true);
    }

    internal void BodyAddConstantTorqueCore(RID body, float torque)
    {
        PhysicsBodyRuntime.Finite(torque);
        var runtime = ForceRuntime(body);
        runtime.SetConstants(runtime.GetConstantForce(), runtime.GetConstantTorque() + torque, true);
    }

    internal void BodySetConstantForceCore(RID body, Vector2 force)
    {
        PhysicsBodyRuntime.Finite(force);
        var runtime = ForceRuntime(body);
        runtime.SetConstants(force, runtime.GetConstantTorque(), force != Vector2.Zero);
    }

    internal Vector2 BodyGetConstantForceCore(RID body) => ForceRuntime(body).GetConstantForce();

    internal void BodySetConstantTorqueCore(RID body, float torque)
    {
        PhysicsBodyRuntime.Finite(torque);
        var runtime = ForceRuntime(body);
        runtime.SetConstants(runtime.GetConstantForce(), torque, torque != 0);
    }

    internal float BodyGetConstantTorqueCore(RID body) => ForceRuntime(body).GetConstantTorque();
}
