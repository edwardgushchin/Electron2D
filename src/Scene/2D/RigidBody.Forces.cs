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
        if (!force.IsFinite()) throw new ArgumentOutOfRangeException(nameof(force));
        var total = _constantForce + force;
        if (!total.IsFinite()) throw new ArgumentOutOfRangeException(nameof(force));
        _constantForce = total;
        WakeForPersistentForce();
    }

    /// <summary>Adds a persistent force at a world-axis offset from the body origin.</summary>
    /// <param name="force">Force in scene units times kilograms per second squared.</param>
    /// <param name="position">World-axis offset from the body origin in scene units.</param>
    /// <remarks>The moment is accumulated when this method is called and persists until <see cref="ConstantTorque"/> is cleared.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">An input or resulting force/torque is nonfinite.</exception>
    public void AddConstantForce(Vector2 force, Vector2 position = default)
    {
        EnsureMutable();
        ValidateForceAndPosition(force, position);
        if (HasBackend) PrepareBackend();
        var totalForce = _constantForce + force;
        var totalTorque = _constantTorque + (position - CenterOffset()).Cross(force);
        if (!totalForce.IsFinite() || !float.IsFinite(totalTorque))
            throw new ArgumentOutOfRangeException(nameof(force), "The accumulated force or torque is nonfinite.");
        _constantForce = totalForce;
        _constantTorque = totalTorque;
        WakeForPersistentForce();
    }

    /// <summary>Adds a persistent finite torque without changing constant force.</summary>
    /// <param name="torque">Torque in kilograms times squared scene units per squared second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The input or resulting total is nonfinite.</exception>
    public void AddConstantTorque(float torque)
    {
        EnsureMutable();
        Finite(torque);
        var total = _constantTorque + torque;
        if (!float.IsFinite(total)) throw new ArgumentOutOfRangeException(nameof(torque));
        _constantTorque = total;
        WakeForPersistentForce();
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
        if (!HasBackend) throw new InvalidOperationException("Attach the body to a scene tree before applying forces.");
        PrepareBackend();
        var point = WorldPoint(position);
        if (!float.IsFinite((position - CenterOffset()).Cross(force)))
            throw new ArgumentOutOfRangeException(nameof(force), "The positioned force would produce nonfinite torque.");
        b2Body_ApplyForce(BackendID, Shape.ToBackend(force), point, wake: true);
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
        if (!HasBackend) throw new InvalidOperationException("Attach the body to a scene tree before applying impulses.");
        PrepareBackend();
        var point = WorldPoint(position);
        if (!float.IsFinite((position - CenterOffset()).Cross(impulse)))
            throw new ArgumentOutOfRangeException(nameof(impulse), "The positioned impulse would produce nonfinite torque.");
        b2Body_ApplyLinearImpulse(BackendID, Shape.ToBackend(impulse), point, wake: true);
    }

    /// <summary>Applies a finite torque for the current physics step.</summary>
    /// <param name="torque">Torque in kilograms times squared scene units per squared second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The torque is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyTorque(float torque)
    {
        EnsureMutable();
        Finite(torque);
        if (!HasBackend) throw new InvalidOperationException("Attach the body to a scene tree before applying torque.");
        PrepareBackend();
        b2Body_ApplyTorque(BackendID, torque * TorqueUnitScale, wake: true);
    }

    /// <summary>Applies a finite one-time angular impulse.</summary>
    /// <param name="torque">Angular impulse in kilograms times squared scene units per second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The impulse is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyTorqueImpulse(float torque)
    {
        EnsureMutable();
        Finite(torque);
        if (!HasBackend) throw new InvalidOperationException("Attach the body to a scene tree before applying torque impulses.");
        PrepareBackend();
        b2Body_ApplyAngularImpulse(BackendID, torque * TorqueUnitScale, wake: true);
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

    private B2Vec2 WorldPoint(Vector2 position)
    {
        if (!HasBackend)
        {
            var scenePoint = GlobalPosition + position;
            if (!scenePoint.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position));
            return Shape.ToBackend(scenePoint);
        }
        var point = b2Body_GetPosition(BackendID) + Shape.ToBackend(position);
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
            throw new ArgumentOutOfRangeException(nameof(position));
        return point;
    }

    private Vector2 CenterOffset()
    {
        if (!HasBackend) return Vector2.Zero;
        var center = b2Body_GetWorldCenterOfMass(BackendID);
        var origin = b2Body_GetPosition(BackendID);
        return new((center.X - origin.X) * PhysicsSpace.UnitsPerMeter,
            (center.Y - origin.Y) * PhysicsSpace.UnitsPerMeter);
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
