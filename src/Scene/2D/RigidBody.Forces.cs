using Box2D.NET;
using static Box2D.NET.B2Bodies;

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

    internal void SetConstantTotals(Vector2 force, float torque) { _constantForce = force; _constantTorque = torque; }

    private const float TorqueUnitScale = PhysicsSpace.MetersPerUnit * PhysicsSpace.MetersPerUnit;
    private Vector2 _constantForce;
    private float _constantTorque;

    /// <summary>Gets or sets the finite force applied at the center of mass every physics step.</summary>
    /// <value>Zero by default; the value persists until replaced or cleared.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 ConstantForce
    {
        get { ThrowIfDisposed(); return _constantForce; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value));
            _constantForce = value;
            if (value != Vector2.Zero) WakeForPersistentForce();
        }
    }

    /// <summary>Gets or sets the finite torque applied every physics step.</summary>
    /// <value>Zero by default; the value persists until replaced or cleared.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float ConstantTorque
    {
        get { ThrowIfDisposed(); return _constantTorque; }
        set
        {
            EnsureMutable();
            Finite(value);
            _constantTorque = value;
            if (value != 0) WakeForPersistentForce();
        }
    }

    /// <summary>Adds a persistent force at the center of mass without changing torque.</summary>
    /// <param name="force">Force in scene units times kilograms per second squared.</param>
    /// <exception cref="ArgumentOutOfRangeException">The force or resulting total is nonfinite.</exception>
    public void AddConstantCentralForce(Vector2 force)
    {
        EnsureMutable();
        PhysicsServer.Instance.BodyAddConstantCentralForce(GetRID(), force);
    }

    /// <summary>Adds a persistent force at a world-axis offset from the body origin.</summary>
    /// <param name="force">Force in scene units times kilograms per second squared.</param>
    /// <param name="position">World-axis offset from the body origin in scene units.</param>
    /// <remarks>The moment is accumulated when this method is called and persists until <see cref="ConstantTorque"/> is cleared.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An input or resulting force/torque is nonfinite.</exception>
    public void AddConstantForce(Vector2 force, Vector2 position = default)
    {
        EnsureMutable();
        PhysicsServer.Instance.BodyAddConstantForce(GetRID(), force, position);
    }

    /// <summary>Adds a persistent finite torque without changing constant force.</summary>
    /// <param name="torque">Torque in kilograms times squared scene units per squared second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The input or resulting total is nonfinite.</exception>
    public void AddConstantTorque(float torque)
    {
        EnsureMutable();
        PhysicsServer.Instance.BodyAddConstantTorque(GetRID(), torque);
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
        PhysicsServer.Instance.BodyApplyForce(GetRID(), force, position);
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
        PhysicsServer.Instance.BodyApplyImpulse(GetRID(), impulse, position);
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
        PhysicsServer.Instance.BodyApplyTorque(GetRID(), torque);
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
        PhysicsServer.Instance.BodyApplyTorqueImpulse(GetRID(), torque);
    }

    /// <summary>Replaces the velocity component along the supplied axis, retaining perpendicular velocity.</summary>
    /// <param name="axisVelocity">An axis whose direction and length give the new component in scene units per second.</param>
    /// <remarks>A zero vector leaves the velocity unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An input or resulting velocity is nonfinite.</exception>
    public void SetAxisVelocity(Vector2 axisVelocity)
    {
        EnsureMutable();
        if (!axisVelocity.IsFinite()) throw new ArgumentOutOfRangeException(nameof(axisVelocity));
        var axis = axisVelocity.Normalized();
        var velocity = LinearVelocity;
        var result = velocity - axis * axis.Dot(velocity) + axisVelocity;
        if (!result.IsFinite()) throw new ArgumentOutOfRangeException(nameof(axisVelocity));
        LinearVelocity = result;
        if (HasBackend && !_freeze && !PhysicsMadeStatic) b2Body_SetAwake(BackendID, true);
    }

    internal void ApplyConstantForces()
    {
        if (_freeze || PhysicsMadeStatic || !HasBackend || !b2Body_IsAwake(BackendID)) return;
        if (_constantForce != Vector2.Zero)
            b2Body_ApplyForceToCenter(BackendID, Shape.ToBackend(_constantForce), wake: false);
        if (_constantTorque != 0)
            b2Body_ApplyTorque(BackendID, _constantTorque * TorqueUnitScale, wake: false);
    }

    private void WakeForPersistentForce()
    {
        if (HasBackend && !_freeze && !PhysicsMadeStatic) b2Body_SetAwake(BackendID, true);
        else _sleeping = false;
    }

    private static void ValidateForceAndPosition(Vector2 force, Vector2 position)
    {
        if (!force.IsFinite()) throw new ArgumentOutOfRangeException(nameof(force));
        if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position));
    }
}
