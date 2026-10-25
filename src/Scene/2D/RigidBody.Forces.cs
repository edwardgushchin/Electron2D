namespace Electron2D;

public partial class RigidBody
{
    private static readonly PropertyDescriptor[] ForceProperties =
    [
        new PropertyDescriptor<RigidBody, Vector2>(nameof(ConstantForce), body => body.ConstantForce,
            (body, value) => body.ConstantForce = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(ConstantTorque), body => body.ConstantTorque,
            (body, value) => body.ConstantTorque = value, _ => 0f, stored: true)
    ];

    internal void SetConstantTotals(Vector2 force, float torque) { _constantForce = force; _constantTorque = torque; GPUParametersDirty = true; }

    private Vector2 _constantForce;
    private float _constantTorque;

    /// <summary>Gets or sets the finite force applied at the center of mass every physics step.</summary>
    /// <value>Zero by default; the value persists until replaced or cleared.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, inside a numerical hook or after the world has failed, including zero and equal assignments.</exception>
    public Vector2 ConstantForce
    {
        get { ThrowIfDisposed(); return _constantForce; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value));
            EnsurePhysicsParticipationChange();
            _constantForce = value; GPUParametersDirty = true;
            if (value != Vector2.Zero) WakeForPersistentForce();
        }
    }

    /// <summary>Gets or sets the finite torque applied every physics step.</summary>
    /// <value>Zero by default; the value persists until replaced or cleared.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, inside a numerical hook or after the world has failed, including zero and equal assignments.</exception>
    public float ConstantTorque
    {
        get { ThrowIfDisposed(); return _constantTorque; }
        set
        {
            EnsureMutable();
            Finite(value);
            EnsurePhysicsParticipationChange();
            _constantTorque = value; GPUParametersDirty = true;
            if (value != 0) WakeForPersistentForce();
        }
    }

    /// <summary>Adds a persistent force at the center of mass without changing torque.</summary>
    /// <param name="force">Force in scene units times kilograms per second squared.</param>
    /// <exception cref="ArgumentOutOfRangeException">The force or resulting total is nonfinite.</exception>
    public void AddConstantCentralForce(Vector2 force)
    {
        EnsureMutable();
        PhysicsServer.BodyAddConstantCentralForce(GetRID(), force);
    }

    /// <summary>Adds a persistent force at a world-axis offset from the body origin.</summary>
    /// <param name="force">Force in scene units times kilograms per second squared.</param>
    /// <param name="position">World-axis offset from the body origin in scene units.</param>
    /// <remarks>The moment is accumulated when this method is called and persists until <see cref="ConstantTorque"/> is cleared.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An input or resulting force/torque is nonfinite.</exception>
    public void AddConstantForce(Vector2 force, Vector2 position = default)
    {
        EnsureMutable();
        PhysicsServer.BodyAddConstantForce(GetRID(), force, position);
    }

    /// <summary>Adds a persistent finite torque without changing constant force.</summary>
    /// <param name="torque">Torque in kilograms times squared scene units per squared second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The input or resulting total is nonfinite.</exception>
    public void AddConstantTorque(float torque)
    {
        EnsureMutable();
        PhysicsServer.BodyAddConstantTorque(GetRID(), torque);
    }

    /// <summary>Applies a force for the current physics step at a world-axis offset from the body origin.</summary>
    /// <param name="force">Force in scene units times kilograms per second squared.</param>
    /// <param name="position">World-axis offset from the body origin in scene units.</param>
    /// <exception cref="ArgumentOutOfRangeException">An input or resulting world point is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyForce(Vector2 force, Vector2 position = default)
    {
        EnsureMutable();
        ValidateForceAndPosition(force, position);
        if (!HasBackend) throw new InvalidOperationException("Attach the body before applying forces or impulses.");
        PhysicsServer.BodyApplyForce(GetRID(), force, position);
    }

    /// <summary>Applies a one-time impulse at a world-axis offset from the body origin.</summary>
    /// <param name="impulse">Impulse in scene units times kilograms per second.</param>
    /// <param name="position">World-axis offset from the body origin in scene units.</param>
    /// <exception cref="ArgumentOutOfRangeException">An input or resulting world point is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyImpulse(Vector2 impulse, Vector2 position = default)
    {
        EnsureMutable();
        ValidateForceAndPosition(impulse, position);
        if (!HasBackend) throw new InvalidOperationException("Attach the body before applying forces or impulses.");
        PhysicsServer.BodyApplyImpulse(GetRID(), impulse, position);
    }

    /// <summary>Applies a finite torque for the current physics step.</summary>
    /// <param name="torque">Torque in kilograms times squared scene units per squared second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The torque is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyTorque(float torque)
    {
        EnsureMutable();
        Finite(torque);
        if (!HasBackend) throw new InvalidOperationException("Attach the body before applying forces or impulses.");
        PhysicsServer.BodyApplyTorque(GetRID(), torque);
    }

    /// <summary>Applies a finite one-time angular impulse.</summary>
    /// <param name="torque">Angular impulse in kilograms times squared scene units per second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The impulse is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyTorqueImpulse(float torque)
    {
        EnsureMutable();
        Finite(torque);
        if (!HasBackend) throw new InvalidOperationException("Attach the body before applying forces or impulses.");
        PhysicsServer.BodyApplyTorqueImpulse(GetRID(), torque);
    }

    /// <summary>Replaces the velocity component along the supplied axis, retaining perpendicular velocity.</summary>
    /// <param name="axisVelocity">An axis whose direction and length give the new component in scene units per second.</param>
    /// <remarks>Active bodies project current selected motion, including impulses not yet synchronized to scene properties.
    /// Inactive native roles retain their configured future velocity. A zero vector keeps velocity and wakes eligible dynamic bodies.
    /// Selected publication errors retain the requested velocity for the ordinary motion preparation retry.
    /// Current-group calls project the prepared logical velocity and queue its result for the owner barrier.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An input or resulting velocity is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, inside a numerical hook or after world failure.</exception>
    public void SetAxisVelocity(Vector2 axisVelocity)
    {
        if (IsGroupMotionAuthoring)
        {
            EnsureMotionMutable();
            LinearVelocity = PhysicsBodyRuntime.ProjectAxisVelocity(LinearVelocity, axisVelocity);
        }
        else
        {
            EnsureMutable();
            PhysicsServer.BodySetAxisVelocity(GetRID(), axisVelocity);
        }
    }

    private void WakeForPersistentForce()
    {
        if (HasBackend && !_freeze && !PhysicsMadeStatic) Backend.SetAwake(true);
        else _sleeping = false;
    }

    private static void ValidateForceAndPosition(Vector2 force, Vector2 position)
    {
        if (!force.IsFinite()) throw new ArgumentOutOfRangeException(nameof(force));
        if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position));
    }
}
