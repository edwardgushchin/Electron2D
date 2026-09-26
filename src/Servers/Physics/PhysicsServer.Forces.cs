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

    /// <summary>Applies a force for the next eligible fixed step without adding torque.</summary>
    /// <remarks>Pending input survives removal, static and dormant participation. Omission clears it on eligible integration.</remarks>
    /// <param name="body">A live scene or server body RID, including a detached body.</param>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodyApplyCentralForce(RID body, Vector2 force)
    {
        PhysicsBodyRuntime.Finite(force);
        ForceRuntime(body, true).AddForce(force, 0);
    }

    /// <summary>Applies a positioned force for the next eligible fixed step.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <param name="position">Global-axis offset from body origin in scene units; zero by default.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodyApplyForce(RID body, Vector2 force, Vector2 position = default)
    {
        PhysicsBodyRuntime.Finite(force); PhysicsBodyRuntime.Finite(position);
        var runtime = ForceRuntime(body, true);
        var moment = (position - runtime.CenterOffset).Cross(force);
        PhysicsBodyRuntime.Finite(moment);
        runtime.AddForce(force, moment);
    }

    /// <summary>Applies torque for the next eligible fixed step.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="torque">Finite kilograms times squared scene units per squared second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodyApplyTorque(RID body, float torque)
    {
        PhysicsBodyRuntime.Finite(torque);
        ForceRuntime(body, true).AddForce(Vector2.Zero, torque);
    }

    /// <summary>Applies an instantaneous impulse without adding angular motion.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="impulse">Finite global scene units times kilograms per second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodyApplyCentralImpulse(RID body, Vector2 impulse)
    {
        PhysicsBodyRuntime.Finite(impulse);
        ForceRuntime(body, true).ApplyImpulse(impulse, 0);
    }

    /// <summary>Applies an instantaneous positioned impulse using the current mass profile.</summary>
    /// <remarks>Detached resource geometry resolves without creating a native body or world; static/kinematic inverse values ignore impulses.</remarks>
    /// <param name="body">A live body RID.</param>
    /// <param name="impulse">Finite global scene units times kilograms per second.</param>
    /// <param name="position">Global-axis body-origin offset in scene units; zero by default.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodyApplyImpulse(RID body, Vector2 impulse, Vector2 position = default)
    {
        PhysicsBodyRuntime.Finite(impulse); PhysicsBodyRuntime.Finite(position);
        var runtime = ForceRuntime(body, true);
        var moment = (position - runtime.CenterOffset).Cross(impulse);
        PhysicsBodyRuntime.Finite(moment);
        runtime.ApplyImpulse(impulse, moment);
    }

    /// <summary>Applies an instantaneous angular impulse.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="impulse">Finite kilograms times squared scene units per second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodyApplyTorqueImpulse(RID body, float impulse)
    {
        PhysicsBodyRuntime.Finite(impulse);
        ForceRuntime(body, true).ApplyImpulse(Vector2.Zero, impulse);
    }

    /// <summary>Adds a persistent central force without changing torque.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodyAddConstantCentralForce(RID body, Vector2 force)
    {
        PhysicsBodyRuntime.Finite(force);
        var runtime = ForceRuntime(body);
        runtime.SetConstants(runtime.GetConstantForce() + force, runtime.GetConstantTorque(), true);
    }

    /// <summary>Adds persistent force and its moment about the current center.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="force">Finite global force in scene units times kilograms per squared second.</param>
    /// <param name="position">Global-axis body-origin offset; zero by default.</param>
    /// <remarks>The moment is captured at this call and persists independently of later center or pose changes.</remarks>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodyAddConstantForce(RID body, Vector2 force, Vector2 position = default)
    {
        PhysicsBodyRuntime.Finite(force); PhysicsBodyRuntime.Finite(position);
        var runtime = ForceRuntime(body, true);
        runtime.SetConstants(runtime.GetConstantForce() + force,
            runtime.GetConstantTorque() + (position - runtime.CenterOffset).Cross(force), true);
    }

    /// <summary>Adds persistent torque without changing force.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="torque">Finite kilograms times squared scene units per squared second.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodyAddConstantTorque(RID body, float torque)
    {
        PhysicsBodyRuntime.Finite(torque);
        var runtime = ForceRuntime(body);
        runtime.SetConstants(runtime.GetConstantForce(), runtime.GetConstantTorque() + torque, true);
    }

    /// <summary>Replaces the persistent global force.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="force">Finite force; zero clears it without waking the body.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodySetConstantForce(RID body, Vector2 force)
    {
        PhysicsBodyRuntime.Finite(force);
        var runtime = ForceRuntime(body);
        runtime.SetConstants(force, runtime.GetConstantTorque(), force != Vector2.Zero);
    }

    /// <summary>Returns the persistent global force in scene units.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Force in scene units times kilograms per squared second.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public Vector2 BodyGetConstantForce(RID body) => ForceRuntime(body).GetConstantForce();

    /// <summary>Replaces the persistent torque.</summary>
    /// <param name="body">A live body RID.</param>
    /// <param name="torque">Finite torque; zero clears it without waking the body.</param>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public void BodySetConstantTorque(RID body, float torque)
    {
        PhysicsBodyRuntime.Finite(torque);
        var runtime = ForceRuntime(body);
        runtime.SetConstants(runtime.GetConstantForce(), torque, torque != 0);
    }

    /// <summary>Returns persistent torque in scene units.</summary>
    /// <param name="body">A live body RID.</param>
    /// <returns>Kilograms times squared scene units per squared second.</returns>
    /// <exception cref="ArgumentException">The RID does not identify a live body.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or the solver owns the world.</exception>
    public float BodyGetConstantTorque(RID body) => ForceRuntime(body).GetConstantTorque();
}
