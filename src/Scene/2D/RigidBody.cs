using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>A collision body moved by the fixed-step two-dimensional physics simulation.</summary>
/// <remarks>Attach it to a SceneTree and add one or more direct CollisionShape children. The tree owns the physics
/// step; caller code can change forces and velocity during a physics callback before that step.</remarks>
public sealed partial class RigidBody : PhysicsBody
{
    /// <summary>Determines whether the body's damping adds to or replaces resolved world and area damping.</summary>
    public enum DampMode
    {
        /// <summary>Adds body damping to area and world damping.</summary>
        Combine = 0,
        /// <summary>Uses only body damping.</summary>
        Replace = 1
    }

    private static readonly PropertyDescriptor[] BodyProperties =
    [
        new PropertyDescriptor<RigidBody, float>(nameof(Mass), body => body.Mass, (body, value) => body.Mass = value, _ => 1f, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(GravityScale), body => body.GravityScale, (body, value) => body.GravityScale = value, _ => 1f, stored: true),
        new PropertyDescriptor<RigidBody, Vector2>(nameof(LinearVelocity), body => body.LinearVelocity, (body, value) => body.LinearVelocity = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(AngularVelocity), body => body.AngularVelocity, (body, value) => body.AngularVelocity = value, _ => 0f, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(LinearDamp), body => body.LinearDamp, (body, value) => body.LinearDamp = value, _ => 0f, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(AngularDamp), body => body.AngularDamp, (body, value) => body.AngularDamp = value, _ => 0f, stored: true),
        new PropertyDescriptor<RigidBody, DampMode>(nameof(LinearDampMode), body => body.LinearDampMode,
            (body, value) => body.LinearDampMode = value, _ => DampMode.Combine, stored: true),
        new PropertyDescriptor<RigidBody, DampMode>(nameof(AngularDampMode), body => body.AngularDampMode,
            (body, value) => body.AngularDampMode = value, _ => DampMode.Combine, stored: true),
        new PropertyDescriptor<RigidBody, bool>(nameof(Freeze), body => body.Freeze, (body, value) => body.Freeze = value, _ => false, stored: true),
        new PropertyDescriptor<RigidBody, bool>(nameof(LockRotation), body => body.LockRotation, (body, value) => body.LockRotation = value, _ => false, stored: true),
        new PropertyDescriptor<RigidBody, PhysicsMaterial?>(nameof(PhysicsMaterialOverride), body => body.PhysicsMaterialOverride,
            (body, value) => body.PhysicsMaterialOverride = value, _ => null, stored: true)
    ];

    private float _mass = 1f;
    private float _gravityScale = 1f;
    private Vector2 _linearVelocity;
    private float _angularVelocity;
    private float _linearDamp;
    private float _angularDamp;
    private DampMode _linearDampMode;
    private DampMode _angularDampMode;
    private Vector2 _effectiveGravity;
    private float _effectiveLinearDamp;
    private float _effectiveAngularDamp;
    private bool _fieldsInitialized;
    private bool _freeze;
    private bool _lockRotation;
    private bool _canSleep = true;
    private bool _sleeping;

    /// <summary>Creates a detached dynamic body with one-unit mass and ordinary gravity.</summary>
    public RigidBody() { }

    /// <summary>Gets or sets a borrowed surface material for every child collision shape.</summary>
    /// <value>Null by default, which uses friction one and bounce zero.</value>
    /// <remarks>Property edits rebuild fixtures before the next physics step. The caller owns the material.</remarks>
    /// <exception cref="ObjectDisposedException">The assigned material or body has been disposed.</exception>
    public PhysicsMaterial? PhysicsMaterialOverride
    {
        get { ThrowIfDisposed(); return MaterialOverride; }
        set => SetMaterialOverride(value);
    }

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

    /// <summary>Gets or sets finite signed linear damping per second.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    public float LinearDamp
    {
        get { ThrowIfDisposed(); return _linearDamp; }
        set { EnsureMutable(); Finite(value); _linearDamp = value; }
    }

    /// <summary>Gets or sets finite signed angular damping per second.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    public float AngularDamp
    {
        get { ThrowIfDisposed(); return _angularDamp; }
        set { EnsureMutable(); Finite(value); _angularDamp = value; }
    }

    /// <summary>Gets or sets whether linear damping combines with or replaces area and world damping.</summary>
    /// <value><see cref="DampMode.Combine"/> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned mode is undefined.</exception>
    public DampMode LinearDampMode
    {
        get { ThrowIfDisposed(); return _linearDampMode; }
        set { EnsureMutable(); ValidateDampMode(value); _linearDampMode = value; }
    }

    /// <summary>Gets or sets whether angular damping combines with or replaces area and world damping.</summary>
    /// <value><see cref="DampMode.Combine"/> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned mode is undefined.</exception>
    public DampMode AngularDampMode
    {
        get { ThrowIfDisposed(); return _angularDampMode; }
        set { EnsureMutable(); ValidateDampMode(value); _angularDampMode = value; }
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
        set { EnsureMutable(); _sleeping = value; _sleepChangePending = false; if (HasBackend) b2Body_SetAwake(BackendID, !value); }
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
        PrepareBackend();
        b2Body_ApplyForceToCenter(BackendID, Shape.ToBackend(force), wake: true);
    }

    /// <summary>Applies a finite instantaneous impulse at the center of mass.</summary>
    /// <param name="impulse">Impulse in scene units times kilograms per second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The impulse is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyCentralImpulse(Vector2 impulse = default)
    {
        EnsureMutable();
        if (!impulse.IsFinite()) throw new ArgumentOutOfRangeException(nameof(impulse));
        if (!HasBackend) throw new InvalidOperationException("Attach the body to a scene tree before applying impulses.");
        PrepareBackend();
        b2Body_ApplyLinearImpulseToCenter(BackendID, Shape.ToBackend(impulse), wake: true);
    }

    internal override bool MovesWithSimulation => !_freeze;
    internal override Vector2 EffectiveGravity => _effectiveGravity;

    internal override B2BodyDef CreateBodyDefinition()
    {
        _fieldsInitialized = false;
        var definition = b2DefaultBodyDef();
        definition.type = _freeze ? B2BodyType.b2_staticBody : B2BodyType.b2_dynamicBody;
        definition.linearVelocity = Shape.ToBackend(_linearVelocity);
        definition.angularVelocity = _angularVelocity;
        definition.linearDamping = 0;
        definition.angularDamping = 0;
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
        var sleeping = !b2Body_IsAwake(BackendID);
        if (sleeping != _sleeping) _sleepChangePending = true;
        _sleeping = sleeping;
    }

    internal void ApplyAreaFields(Vector2 gravity, float linearDamp, float angularDamp,
        Vector2 defaultGravity, double delta)
    {
        var scaledGravity = gravity * _gravityScale;
        var resolvedLinear = _linearDampMode == DampMode.Replace ? _linearDamp : linearDamp + _linearDamp;
        var resolvedAngular = _angularDampMode == DampMode.Replace ? _angularDamp : angularDamp + _angularDamp;
        var linearFactor = MathF.Max(0, 1 - (float)delta * resolvedLinear);
        var angularFactor = MathF.Max(0, 1 - (float)delta * resolvedAngular);
        var extraAcceleration = scaledGravity - defaultGravity * _gravityScale;
        if (!scaledGravity.IsFinite() || !extraAcceleration.IsFinite() ||
            !float.IsFinite(linearFactor) || !float.IsFinite(angularFactor))
            throw new InvalidOperationException("The resolved physics field exceeds the finite simulation range.");

        var changed = _fieldsInitialized && (scaledGravity != _effectiveGravity ||
            resolvedLinear != _effectiveLinearDamp || resolvedAngular != _effectiveAngularDamp);
        var active = !_freeze && HasBackend && (changed || b2Body_IsAwake(BackendID));
        var dampedVelocity = default(B2Vec2);
        var dampedAngularVelocity = 0f;
        var force = default(B2Vec2);
        if (active)
        {
            dampedVelocity = b2Body_GetLinearVelocity(BackendID) * linearFactor;
            dampedAngularVelocity = b2Body_GetAngularVelocity(BackendID) * angularFactor;
            force = Shape.ToBackend(extraAcceleration) * b2Body_GetMass(BackendID);
            if (!float.IsFinite(dampedVelocity.X) || !float.IsFinite(dampedVelocity.Y) ||
                !float.IsFinite(dampedAngularVelocity) || !float.IsFinite(force.X) || !float.IsFinite(force.Y))
                throw new InvalidOperationException("The resolved physics field would produce nonfinite motion.");
        }
        _effectiveGravity = scaledGravity;
        _effectiveLinearDamp = resolvedLinear;
        _effectiveAngularDamp = resolvedAngular;
        _fieldsInitialized = true;
        if (!active) return;
        if (changed) b2Body_SetAwake(BackendID, true);
        if (linearFactor != 1) b2Body_SetLinearVelocity(BackendID, dampedVelocity);
        if (angularFactor != 1) b2Body_SetAngularVelocity(BackendID, dampedAngularVelocity);
        if (force.X != 0 || force.Y != 0) b2Body_ApplyForceToCenter(BackendID, force, wake: false);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(BodyProperties).Concat(ForceProperties).Concat(ContactProperties);

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

    private static void Positive(float value)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void ValidateDampMode(DampMode value)
    {
        if (value is not DampMode.Combine and not DampMode.Replace)
            throw new ArgumentOutOfRangeException(nameof(value));
    }
}
