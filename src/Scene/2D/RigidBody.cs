using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>A collision body moved by the fixed-step two-dimensional physics simulation.</summary>
/// <remarks>Attach it to a SceneTree and add one or more direct CollisionShape children. The tree owns the physics
/// step; caller code can change forces and velocity during a physics callback before that step.</remarks>
public sealed class RigidBody : PhysicsBody
{
    private static readonly PropertyDescriptor[] BodyProperties =
    [
        new PropertyDescriptor<RigidBody, float>(nameof(Mass), body => body.Mass, (body, value) => body.Mass = value, _ => 1f, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(GravityScale), body => body.GravityScale, (body, value) => body.GravityScale = value, _ => 1f, stored: true),
        new PropertyDescriptor<RigidBody, Vector2>(nameof(LinearVelocity), body => body.LinearVelocity, (body, value) => body.LinearVelocity = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(AngularVelocity), body => body.AngularVelocity, (body, value) => body.AngularVelocity = value, _ => 0f, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(LinearDamp), body => body.LinearDamp, (body, value) => body.LinearDamp = value, _ => 0f, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(AngularDamp), body => body.AngularDamp, (body, value) => body.AngularDamp = value, _ => 0f, stored: true),
        new PropertyDescriptor<RigidBody, bool>(nameof(Freeze), body => body.Freeze, (body, value) => body.Freeze = value, _ => false, stored: true),
        new PropertyDescriptor<RigidBody, bool>(nameof(LockRotation), body => body.LockRotation, (body, value) => body.LockRotation = value, _ => false, stored: true)
    ];

    private float _mass = 1f;
    private float _gravityScale = 1f;
    private Vector2 _linearVelocity;
    private float _angularVelocity;
    private float _linearDamp;
    private float _angularDamp;
    private bool _freeze;
    private bool _lockRotation;
    private bool _canSleep = true;
    private bool _sleeping;

    /// <summary>Creates a detached dynamic body with one-unit mass and ordinary gravity.</summary>
    public RigidBody() { }

    /// <summary>Gets or sets positive finite body mass in kilograms.</summary>
    /// <value>One by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mass is not positive and finite, or its shape inertia would overflow.</exception>
    public float Mass
    {
        get { ThrowIfDisposed(); return _mass; }
        set { EnsureMutable(); Positive(value); ApplyMass(value); _mass = value; }
    }

    /// <summary>Gets or sets the finite multiplier of the world's downward gravity.</summary>
    /// <value>One by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    public float GravityScale
    {
        get { ThrowIfDisposed(); return _gravityScale; }
        set { EnsureMutable(); Finite(value); _gravityScale = value; if (HasBackend) b2Body_SetGravityScale(BackendID, value); }
    }

    /// <summary>Gets or sets linear velocity in scene units per second.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 LinearVelocity
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _linearVelocity; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value));
            _linearVelocity = value;
            if (HasBackend && !_freeze) b2Body_SetLinearVelocity(BackendID, Shape.ToBackend(value));
        }
    }

    /// <summary>Gets or sets angular velocity in radians per second.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    public float AngularVelocity
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _angularVelocity; }
        set { EnsureMutable(); Finite(value); _angularVelocity = value; if (HasBackend && !_freeze) b2Body_SetAngularVelocity(BackendID, value); }
    }

    /// <summary>Gets or sets finite nonnegative linear damping per second.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative or nonfinite.</exception>
    public float LinearDamp
    {
        get { ThrowIfDisposed(); return _linearDamp; }
        set { EnsureMutable(); Nonnegative(value); _linearDamp = value; if (HasBackend) b2Body_SetLinearDamping(BackendID, value); }
    }

    /// <summary>Gets or sets finite nonnegative angular damping per second.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative or nonfinite.</exception>
    public float AngularDamp
    {
        get { ThrowIfDisposed(); return _angularDamp; }
        set { EnsureMutable(); Nonnegative(value); _angularDamp = value; if (HasBackend) b2Body_SetAngularDamping(BackendID, value); }
    }

    /// <summary>Gets or sets whether simulation treats this body as static.</summary>
    /// <value>False by default.</value>
    public bool Freeze
    {
        get { ThrowIfDisposed(); return _freeze; }
        set
        {
            EnsureMutable();
            if (_freeze == value) return;
            _freeze = value;
            if (HasBackend)
            {
                b2Body_SetType(BackendID, value ? B2BodyType.b2_staticBody : B2BodyType.b2_dynamicBody);
                if (!value)
                {
                    b2Body_SetLinearVelocity(BackendID, Shape.ToBackend(_linearVelocity));
                    b2Body_SetAngularVelocity(BackendID, _angularVelocity);
                }
                MarkShapesDirty();
            }
        }
    }

    /// <summary>Gets or sets whether the solver prevents rotation.</summary>
    /// <value>False by default.</value>
    public bool LockRotation
    {
        get { ThrowIfDisposed(); return _lockRotation; }
        set { EnsureMutable(); _lockRotation = value; if (HasBackend) b2Body_SetMotionLocks(BackendID, new(false, false, value)); }
    }

    /// <summary>Gets or sets whether an idle body may sleep.</summary>
    /// <value>True by default.</value>
    public bool CanSleep
    {
        get { ThrowIfDisposed(); return _canSleep; }
        set { EnsureMutable(); _canSleep = value; if (HasBackend) b2Body_EnableSleep(BackendID, value); }
    }

    /// <summary>Gets or sets whether the body is currently asleep.</summary>
    /// <value>False by default.</value>
    public bool Sleeping
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return HasBackend ? !b2Body_IsAwake(BackendID) : _sleeping; }
        set { EnsureMutable(); _sleeping = value; if (HasBackend) b2Body_SetAwake(BackendID, !value); }
    }

    /// <summary>Applies a finite force at the center of mass during the current physics step.</summary>
    /// <param name="force">Force in scene units times kilograms per second squared.</param>
    /// <exception cref="ArgumentOutOfRangeException">The force is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyCentralForce(Vector2 force)
    {
        EnsureMutable();
        if (!force.IsFinite()) throw new ArgumentOutOfRangeException(nameof(force));
        if (!HasBackend) throw new InvalidOperationException("Attach the body to a scene tree before applying forces.");
        b2Body_ApplyForceToCenter(BackendID, Shape.ToBackend(force), wake: true);
    }

    /// <summary>Applies a finite instantaneous impulse at the center of mass.</summary>
    /// <param name="impulse">Impulse in scene units times kilograms per second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The impulse is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyCentralImpulse(Vector2 impulse)
    {
        EnsureMutable();
        if (!impulse.IsFinite()) throw new ArgumentOutOfRangeException(nameof(impulse));
        if (!HasBackend) throw new InvalidOperationException("Attach the body to a scene tree before applying impulses.");
        b2Body_ApplyLinearImpulseToCenter(BackendID, Shape.ToBackend(impulse), wake: true);
    }

    internal override bool MovesWithSimulation => !_freeze;

    internal override B2BodyDef CreateBodyDefinition()
    {
        var definition = b2DefaultBodyDef();
        definition.type = _freeze ? B2BodyType.b2_staticBody : B2BodyType.b2_dynamicBody;
        definition.linearVelocity = Shape.ToBackend(_linearVelocity);
        definition.angularVelocity = _angularVelocity;
        definition.linearDamping = _linearDamp;
        definition.angularDamping = _angularDamp;
        definition.gravityScale = _gravityScale;
        definition.enableSleep = _canSleep;
        definition.isAwake = !_sleeping;
        definition.motionLocks.angularZ = _lockRotation;
        return definition;
    }

    internal override void OnShapesRebuilt() => ApplyMass(_mass);

    internal override void OnBackendAdvanced()
    {
        var velocity = b2Body_GetLinearVelocity(BackendID);
        _linearVelocity = new(velocity.X * PhysicsSpace.UnitsPerMeter, velocity.Y * PhysicsSpace.UnitsPerMeter);
        _angularVelocity = b2Body_GetAngularVelocity(BackendID);
        _sleeping = !b2Body_IsAwake(BackendID);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(BodyProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(RigidBody)
        ? CreateRigidBody : base.CreateSceneInstanceFactory();

    private static Node CreateRigidBody() => new RigidBody();

    private void ApplyMass(float desiredMass)
    {
        if (!HasBackend || _freeze) return;
        var data = b2Body_GetMassData(BackendID);
        if (data.mass <= 0) return;
        var ratio = desiredMass / data.mass;
        var inertia = data.rotationalInertia * ratio;
        if (!float.IsFinite(ratio) || !float.IsFinite(inertia))
            throw new ArgumentOutOfRangeException(nameof(desiredMass), "Mass exceeds the current shape's representable inertia range.");
        data.mass = desiredMass;
        data.rotationalInertia = inertia;
        b2Body_SetMassData(BackendID, data);
    }

    private static void Finite(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void Nonnegative(float value)
    {
        if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void Positive(float value)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
    }
}
