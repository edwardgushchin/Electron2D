using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>A collision body moved by the fixed-step two-dimensional physics simulation.</summary>
/// <remarks>Attach it to a SceneTree and add one or more direct CollisionShape children. The tree owns the physics
/// step; caller code can change forces and velocity during a physics callback before that step.</remarks>
public partial class RigidBody : PhysicsBody
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
        new PropertyDescriptor<RigidBody, bool>(nameof(CustomIntegrator), body => body.CustomIntegrator,
            (body, value) => body.CustomIntegrator = value, _ => false, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(Mass), body => body.Mass, (body, value) => body.Mass = value, _ => 1f, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(Inertia), body => body.Inertia, (body, value) => body.Inertia = value, _ => 0f, stored: true),
        new PropertyDescriptor<RigidBody, RigidCenterOfMassMode>(nameof(CenterOfMassMode), body => body.CenterOfMassMode, (body, value) => body.CenterOfMassMode = value, _ => RigidCenterOfMassMode.Auto, stored: true),
        new PropertyDescriptor<RigidBody, Vector2>(nameof(CenterOfMass), body => body.CenterOfMass, (body, value) => body.CenterOfMass = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(GravityScale), body => body.GravityScale, (body, value) => body.GravityScale = value, _ => 1f, stored: true),
        new PropertyDescriptor<RigidBody, Vector2>(nameof(LinearVelocity), body => body.LinearVelocity, (body, value) => body.LinearVelocity = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(AngularVelocity), body => body.AngularVelocity, (body, value) => body.AngularVelocity = value, _ => 0f, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(LinearDamp), body => body.LinearDamp, (body, value) => body.LinearDamp = value, _ => 0f, stored: true),
        new PropertyDescriptor<RigidBody, float>(nameof(AngularDamp), body => body.AngularDamp, (body, value) => body.AngularDamp = value, _ => 0f, stored: true),
        new PropertyDescriptor<RigidBody, DampMode>(nameof(LinearDampMode), body => body.LinearDampMode,
            (body, value) => body.LinearDampMode = value, _ => DampMode.Combine, stored: true),
        new PropertyDescriptor<RigidBody, DampMode>(nameof(AngularDampMode), body => body.AngularDampMode,
            (body, value) => body.AngularDampMode = value, _ => DampMode.Combine, stored: true),
        new PropertyDescriptor<RigidBody, RigidFreezeMode>(nameof(FreezeMode), body => body.FreezeMode, (body, value) => body.FreezeMode = value, _ => RigidFreezeMode.Static, stored: true),
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
    /// <value>One by default. Automatic inertia scales with mass; an explicit Inertia override is retained.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mass is not positive and finite, is below the solver range, or its shape inertia would overflow or underflow the solver range.</exception>
    public float Mass
    {
        get { ThrowIfDisposed(); return _mass; }
        set => SetMassProfile(value, _inertia, _centerOfMassMode, _centerOfMass);
    }

    /// <summary>Gets or sets the finite multiplier of selected Area/world gravity.</summary>
    /// <remarks>Leaving an approximately zero previous multiplier follows the body wakeup rule.</remarks>
    /// <value>One by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    public float GravityScale
    {
        get { ThrowIfDisposed(); return _gravityScale; }
        set { EnsureMutable(); Finite(value); EnsurePhysicsParticipationChange(); if (Mathf.IsZeroApprox(_gravityScale)) PhysicsServer.Service.BodyRuntime(PhysicsRID).Wake(); _gravityScale = value; if (HasBackend) b2Body_SetGravityScale(BackendID, _customIntegrator ? 0 : value); }
    }

    /// <summary>Gets or sets linear velocity in scene units per second.</summary>
    /// <remarks>MakeStatic disable entry clears prior velocity; later assignments are stored until the configured dynamic role returns.</remarks>
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
            if (HasBackend && !_freeze && !PhysicsMadeStatic) b2Body_SetLinearVelocity(BackendID, Shape.ToBackend(value));
        }
    }

    /// <summary>Gets or sets angular velocity in radians per second.</summary>
    /// <remarks>MakeStatic disable entry clears prior velocity; later assignments are stored until the configured dynamic role returns.</remarks>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    public float AngularVelocity
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _angularVelocity; }
        set { EnsureMutable(); Finite(value); _angularVelocity = value; if (HasBackend && !_freeze && !PhysicsMadeStatic) b2Body_SetAngularVelocity(BackendID, value); }
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

    /// <summary>Gets or sets whether gravity and forces are disabled under the selected freeze role.</summary>
    /// <remarks>The configured freeze flag is independent of the inherited disable policy.
    /// Attached changes during solver ownership reject before changing this flag.</remarks>
    /// <value>False by default.</value>
    public bool Freeze
    {
        get { ThrowIfDisposed(); return _freeze; }
        set
        {
            EnsureMutable();
            if (_freeze == value) return;
            EnsurePhysicsParticipationChange();
            if (HasBackend) PrepareBackend();
            _freeze = value;
            UpdatePhysicsParticipation();
        }
    }

    /// <summary>Gets or sets whether the solver prevents rotation.</summary>
    /// <value>False by default.</value>
    public bool LockRotation
    {
        get { ThrowIfDisposed(); return _lockRotation; }
        set { EnsureMutable(); EnsurePhysicsParticipationChange(); _lockRotation = value; if (HasBackend) b2Body_SetMotionLocks(BackendID, new(false, false, !_freeze && value)); }
    }

    /// <summary>Gets or sets whether an idle body may sleep.</summary>
    /// <remarks>Disabling automatic sleep wakes a dynamic body, including detached configuration.</remarks>
    /// <value>True by default.</value>
    public bool CanSleep
    {
        get { ThrowIfDisposed(); return _canSleep; }
        set { EnsureMutable(); _canSleep = value; if (HasBackend) b2Body_EnableSleep(BackendID, value); else if (!value && !_freeze) _sleeping = false; }
    }

    /// <summary>Gets or sets whether the body is currently asleep.</summary>
    /// <remarks>Explicit dynamic sleep clears velocity and suppresses the automatic sleep-change event.
    /// This state survives reentry even when automatic sleep is disabled.</remarks>
    /// <value>False by default.</value>
    public bool Sleeping
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return HasBackend ? !b2Body_IsAwake(BackendID) : _sleeping; }
        set
        {
            EnsureMutable(); _sleeping = value; _sleepChangePending = false;
            if (HasBackend) b2Body_SetAwake(BackendID, !value);
            if (value && !_freeze && !PhysicsMadeStatic) { _linearVelocity = Vector2.Zero; _angularVelocity = 0; }
        }
    }

    /// <summary>Applies a finite force at the center of mass during the current physics step.</summary>
    /// <param name="force">Force in scene units times kilograms per second squared.</param>
    /// <exception cref="ArgumentOutOfRangeException">The force is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyCentralForce(Vector2 force)
    {
        EnsureMutable();
        if (!force.IsFinite()) throw new ArgumentOutOfRangeException(nameof(force));
        if (!HasBackend) throw new InvalidOperationException("Attach the body before applying forces or impulses.");
        PhysicsServer.BodyApplyCentralForce(GetRID(), force);
    }

    /// <summary>Applies a finite instantaneous impulse at the center of mass.</summary>
    /// <param name="impulse">Impulse in scene units times kilograms per second.</param>
    /// <exception cref="ArgumentOutOfRangeException">The impulse is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The body is detached from a scene tree.</exception>
    public void ApplyCentralImpulse(Vector2 impulse = default)
    {
        EnsureMutable();
        if (!impulse.IsFinite()) throw new ArgumentOutOfRangeException(nameof(impulse));
        if (!HasBackend) throw new InvalidOperationException("Attach the body before applying forces or impulses.");
        PhysicsServer.BodyApplyCentralImpulse(GetRID(), impulse);
    }

    internal override void OnMadeStatic()
    {
        if (_freeze) return;
        _linearVelocity = Vector2.Zero;
        _angularVelocity = 0;
    }

    internal override void OnBodyTypeChanged()
    {
        b2Body_SetMotionLocks(BackendID, new(false, false, !_freeze && _lockRotation));
        ApplyMass(_mass);
        ResetFrozenSolverPose();
        if (_freeze || PhysicsMadeStatic) return;
        b2Body_SetLinearVelocity(BackendID, Shape.ToBackend(_linearVelocity));
        b2Body_SetAngularVelocity(BackendID, _angularVelocity);
    }

    internal override bool MovesWithSimulation => !_freeze || FrozenKinematic;
    internal override Vector2 EffectiveGravity => _effectiveGravity;

    internal override B2BodyType RequestedBodyType => !_freeze ? B2BodyType.b2_dynamicBody :
        _freezeMode == RigidFreezeMode.Kinematic ? B2BodyType.b2_kinematicBody : B2BodyType.b2_staticBody;

    internal override B2BodyDef CreateBodyDefinition()
    {
        _fieldsInitialized = false;
        _frozenSolverPose = GlobalTransform;
        _frozenQueryPoseApplied = false;
        _frozenNativePose = new B2Transform(Shape.ToBackend(_frozenSolverPose.Origin), B2MathFunction.b2MakeRot(_frozenSolverPose.Rotation));
        var definition = b2DefaultBodyDef();
        definition.type = RequestedBodyType;
        definition.linearVelocity = FrozenKinematic ? default : Shape.ToBackend(_linearVelocity);
        definition.angularVelocity = FrozenKinematic ? 0 : _angularVelocity;
        definition.linearDamping = 0;
        definition.angularDamping = 0;
        definition.gravityScale = _customIntegrator ? 0 : _gravityScale;
        definition.enableSleep = _canSleep;
        definition.isAwake = !_sleeping;
        definition.motionLocks.angularZ = !_freeze && _lockRotation;
        return definition;
    }

    internal override void OnShapesRebuilt() => ApplyMass(_mass);

    internal override void OnBackendAdvanced(B2World world, B2Body body)
    {
        if (FrozenKinematic) ResetFrozenSolverPose();
        var state = b2GetBodyState(world, body);
        var velocity = state?.linearVelocity ?? default;
        _linearVelocity = new(velocity.X * PhysicsSpace.UnitsPerMeter, velocity.Y * PhysicsSpace.UnitsPerMeter);
        _angularVelocity = state?.angularVelocity ?? 0;
        var sleeping = state is null;
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
            !float.IsFinite(resolvedLinear) || !float.IsFinite(resolvedAngular) ||
            !float.IsFinite(linearFactor) || !float.IsFinite(angularFactor))
            throw new InvalidOperationException("The resolved physics field exceeds the finite simulation range.");

        var changed = _fieldsInitialized && (scaledGravity != _effectiveGravity ||
            resolvedLinear != _effectiveLinearDamp || resolvedAngular != _effectiveAngularDamp);
        var active = !_freeze && !PhysicsMadeStatic && !_customIntegrator && HasBackend && (changed || b2Body_IsAwake(BackendID));
        var dampedVelocity = default(B2Vec2);
        var dampedAngularVelocity = 0f;
        var force = default(B2Vec2);
        if (active)
        {
            dampedVelocity = b2Body_GetLinearVelocity(BackendID) * linearFactor;
            dampedAngularVelocity = b2Body_GetAngularVelocity(BackendID) * angularFactor;
            if (extraAcceleration != Vector2.Zero) force = Shape.ToBackend(extraAcceleration) * b2Body_GetMass(BackendID);
            if (!float.IsFinite(dampedVelocity.X) || !float.IsFinite(dampedVelocity.Y) ||
                !float.IsFinite(dampedAngularVelocity) || !float.IsFinite(force.X) || !float.IsFinite(force.Y))
                throw new InvalidOperationException("The resolved physics field would produce nonfinite motion.");
        }
        _effectiveGravity = scaledGravity;
        _effectiveLinearDamp = resolvedLinear;
        _effectiveAngularDamp = resolvedAngular;
        _fieldsInitialized = true;
        if (_customIntegrator && changed && HasBackend && !_freeze && !PhysicsMadeStatic) b2Body_SetAwake(BackendID, true);
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

    private static void Finite(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void ValidateDampMode(DampMode value)
    {
        if (value is not DampMode.Combine and not DampMode.Replace)
            throw new ArgumentOutOfRangeException(nameof(value));
    }
}
