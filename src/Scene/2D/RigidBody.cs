namespace Electron2D;

/// <summary>A collision body moved by the fixed-step two-dimensional physics simulation.</summary>
/// <remarks>Attach it to a SceneTree and add one or more direct CollisionShape children. The tree owns the physics
/// step; caller code can change forces and velocity during a physics callback before that step.
/// Failed selected velocity/sleep publication retains requested input for retry before numerical access, queries or a nonzero step.
/// Retirement preserves requested channels and other selected motion without repairing the discarded attachment.
/// Scene role changes retain committed configuration through selected publication errors and retry the complete role before numerical use.
/// A terminal simulation/restore failure rejects attached motion/role writes, including equal assignments,
/// before authoring changes. Native authored velocity/policy reads remain non-preparing; numerical sleep reads reject.</remarks>
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
        new PropertyDescriptor<RigidBody, CCDMode>(nameof(ContinuousCD), body => body.ContinuousCD,
            (body, value) => body.ContinuousCD = value, _ => CCDMode.Disabled, stored: true),
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
        new PropertyDescriptor<RigidBody, bool>(nameof(CanSleep), body => body.CanSleep, (body, value) => body.CanSleep = value, _ => true, stored: true),
        new PropertyDescriptor<RigidBody, bool>(nameof(Sleeping), body => body.Sleeping, (body, value) => body.Sleeping = value, _ => false, stored: true)
        {
            // Static inactivity must not replace the retained dynamic sleep request in storage.
            StoredGetter = body => { _ = body.Sleeping; return body._sleeping; }
        },
        new PropertyDescriptor<RigidBody, PhysicsMaterial?>(nameof(PhysicsMaterialOverride), body => body.PhysicsMaterialOverride,
            (body, value) => body.PhysicsMaterialOverride = value, _ => null, stored: true)
    ];

    private float _mass = 1f;
    private float _gravityScale = 1f;
    internal bool GPUParametersDirty = true;
    private Vector2 _linearVelocity;
    private float _angularVelocity;
    private float _linearDamp;
    private float _angularDamp;
    private DampMode _linearDampMode;
    private DampMode _angularDampMode;
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
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, inside a numerical hook or after the world has failed, including equal assignments.</exception>
    public PhysicsMaterial? PhysicsMaterialOverride
    {
        get { ThrowIfDisposed(); return MaterialOverride; }
        set => SetMaterialOverride(value);
    }

    /// <summary>Gets or sets positive finite body mass in kilograms.</summary>
    /// <value>One by default. Automatic inertia scales with mass; an explicit Inertia override is retained.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mass is not positive and finite, is below the solver range, or its shape inertia would overflow or underflow the solver range.</exception>
    /// <remarks>A selected publication failure preserves the preceding configured profile; later numerical access prepares it again before use. Configured reads do not perform that repair.</remarks>
    public float Mass
    {
        get { ThrowIfDisposed(); return _mass; }
        set => SetMassProfile(value, _inertia, _centerOfMassMode, _centerOfMass);
    }

    /// <summary>Gets or sets the finite multiplier of selected Area/world gravity.</summary>
    /// <remarks>Leaving an approximately zero previous multiplier follows the body wakeup rule. A selected publication failure retains the new multiplier and retries its effective native value before later numerical access; configured reads do not prepare it.</remarks>
    /// <value>One by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, inside a numerical hook or after the world has failed.</exception>
    public float GravityScale
    {
        get { ThrowIfDisposed(); return _gravityScale; }
        set { EnsureMutable(); Finite(value); EnsurePhysicsParticipationChange(); if (Mathf.IsZeroApprox(_gravityScale)) PhysicsServer.Service.BodyRuntime(PhysicsRID).Wake(); _gravityScale = value; GPUParametersDirty = true; if (HasBackend) Backend.SetGravityScale(_customIntegrator ? 0 : value); }
    }

    /// <summary>Gets or sets linear velocity in scene units per second.</summary>
    /// <remarks>MakeStatic disable entry clears prior velocity; later assignments are stored until the configured dynamic role returns.
    /// An active direct PhysicsServer mode profile shares immediate scene/server velocity state and rejects numerical-hook writes.
    /// Access repairs incomplete direct-mode preparation before consuming or changing motion.</remarks>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 LinearVelocity
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return HasServerMode ? ServerLinearVelocity : _linearVelocity; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value));
            Space?.EnsureImplementationWriteAccess();
            PreparePendingServerMode(writing: true);
            _linearVelocity = value;
            if (HasServerMode) SetServerLinearVelocity(value);
            else if (HasBackend && !_freeze && !PhysicsMadeStatic) Backend.PublishMotion(linear: value, awake: true);
        }
    }

    /// <summary>Gets or sets angular velocity in radians per second.</summary>
    /// <remarks>MakeStatic disable entry clears prior velocity; later assignments are stored until the configured dynamic role returns.
    /// An active direct PhysicsServer mode profile shares immediate scene/server velocity state and rejects numerical-hook writes.
    /// Access repairs incomplete direct-mode preparation before consuming or changing motion.</remarks>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    public float AngularVelocity
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return HasServerMode ? ServerAngularVelocity : _angularVelocity; }
        set { EnsureMutable(); Finite(value); Space?.EnsureImplementationWriteAccess(); PreparePendingServerMode(writing: true); _angularVelocity = value; if (HasServerMode) SetServerAngularVelocity(value); else if (HasBackend && !_freeze && !PhysicsMadeStatic) Backend.PublishMotion(angular: value, awake: true); }
    }

    /// <summary>Gets or sets finite signed linear damping per second.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, inside a numerical hook or after the world has failed.</exception>
    public float LinearDamp
    {
        get { ThrowIfDisposed(); return _linearDamp; }
        set { EnsureMutable(); Finite(value); Space?.EnsureBackendAccess(); _linearDamp = value; GPUParametersDirty = true; }
    }

    /// <summary>Gets or sets finite signed angular damping per second.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, inside a numerical hook or after the world has failed.</exception>
    public float AngularDamp
    {
        get { ThrowIfDisposed(); return _angularDamp; }
        set { EnsureMutable(); Finite(value); Space?.EnsureBackendAccess(); _angularDamp = value; GPUParametersDirty = true; }
    }

    /// <summary>Gets or sets whether linear damping combines with or replaces area and world damping.</summary>
    /// <value><see cref="DampMode.Combine"/> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned mode is undefined.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, inside a numerical hook or after the world has failed.</exception>
    public DampMode LinearDampMode
    {
        get { ThrowIfDisposed(); return _linearDampMode; }
        set { EnsureMutable(); ValidateDampMode(value); Space?.EnsureBackendAccess(); _linearDampMode = value; GPUParametersDirty = true; }
    }

    /// <summary>Gets or sets whether angular damping combines with or replaces area and world damping.</summary>
    /// <value><see cref="DampMode.Combine"/> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned mode is undefined.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, inside a numerical hook or after the world has failed.</exception>
    public DampMode AngularDampMode
    {
        get { ThrowIfDisposed(); return _angularDampMode; }
        set { EnsureMutable(); ValidateDampMode(value); Space?.EnsureBackendAccess(); _angularDampMode = value; GPUParametersDirty = true; }
    }

    /// <summary>Gets or sets whether gravity and forces are disabled under the selected freeze role.</summary>
    /// <remarks>The configured freeze flag is independent of the inherited disable policy.
    /// Attached changes during solver ownership or a numerical implementation hook reject before changing this flag.
    /// A selected publication error retains the new flag; numerical reads, queries, steps and repeated assignments repair its role.</remarks>
    /// <value>False by default.</value>
    public bool Freeze
    {
        get { ThrowIfDisposed(); return _freeze; }
        set
        {
            EnsureMutable();
            Space?.EnsureImplementationWriteAccess();
            PreparePendingServerMode(writing: true);
            if (_freeze == value) return;
            EnsurePhysicsParticipationChange();
            if (HasBackend) PrepareBackend();
            RestoreSceneMode();
            _freeze = value; GPUParametersDirty = true;
            UpdatePhysicsParticipation();
        }
    }

    /// <summary>Gets or sets whether the solver prevents rotation.</summary>
    /// <value>False by default.</value>
    /// <remarks>An unfrozen body selects RigidLinear when true and Rigid when false.
    /// Freeze and the inherited disable policy temporarily override that motion role.
    /// Role changes preserve actual selected linear motion, including pending impulse effects; locked angular motion is zero.
    /// Selected publication errors retain configuration for complete preparation retry.</remarks>
    /// <exception cref="InvalidOperationException">Attached mutation is off-owner, during solver ownership or a numerical implementation hook.</exception>
    public bool LockRotation
    {
        get { ThrowIfDisposed(); return _lockRotation; }
        set
        {
            EnsureMutable(); EnsurePhysicsParticipationChange();
            Space?.EnsureImplementationWriteAccess();
            PreparePendingServerMode(writing: true);
            if (_lockRotation == value) return;
            var serverMode = HasServerMode;
            RestoreSceneMode();
            _lockRotation = value; GPUParametersDirty = true;
            if (serverMode || !_freeze && !PhysicsMadeStatic) PublishBodyMode();
        }
    }

    /// <summary>Gets or sets whether an idle body may sleep.</summary>
    /// <remarks>Disabling automatic sleep wakes a dynamic body, including detached direct server configuration.
    /// Assignment rejects during a borrowed numerical implementation call before changing configuration.
    /// Packed scenes preserve this permission.</remarks>
    /// <value>True by default.</value>
    /// <exception cref="InvalidOperationException">Assignment is off the scene owner thread or inside a borrowed numerical implementation call.</exception>
    public bool CanSleep
    {
        get { ThrowIfDisposed(); return _canSleep; }
        set
        {
            EnsureMutable(); Space?.EnsureImplementationWriteAccess();
            PreparePendingServerMode(writing: true);
            _canSleep = value;
            if (!value && !_freeze) _sleeping = false;
            if (HasServerMode) SetServerCanSleep(value);
            else if (HasBackend) Backend.PublishMotion(canSleep: value,
                awake: !value && EffectiveBodyMode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear ? true : null);
        }
    }

    /// <summary>Gets or sets whether the body is currently asleep.</summary>
    /// <remarks>Explicit dynamic sleep clears velocity and suppresses the automatic sleep-change event.
    /// This state survives reentry even when automatic sleep is disabled. Static participation reports inactivity
    /// while detached without changing the retained dynamic sleep request. Packed scenes retain that request
    /// independently of static inactivity.</remarks>
    /// <value>False for a new dynamic body; true during static participation.</value>
    public bool Sleeping
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); PreparePendingServerMode(); return HasServerMode ? ServerSleeping : HasBackend ? !Backend.IsAwake : EffectiveBodyMode == PhysicsServer.BodyMode.Static || _sleeping; }
        set
        {
            EnsureMutable(); Space?.EnsureImplementationWriteAccess(); PreparePendingServerMode(writing: true); _sleeping = value; _sleepChangePending = false;
            if (value && !_freeze && !PhysicsMadeStatic) { _linearVelocity = Vector2.Zero; _angularVelocity = 0; }
            if (HasServerMode) SetServerSleeping(value);
            else if (HasBackend) Backend.PublishMotion(awake: !value);
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

    internal void CaptureRoleMotion()
    {
        if (!HasBackend || !Backend.IsDynamic) return;
        var motion = Backend.GetRetainedSolverMotion();
        _linearVelocity = motion.LinearVelocity; _angularVelocity = motion.AngularVelocity;
    }

    internal override void OnBodyTypeChanged()
    {
        Backend.SetRotationLocked(!_freeze && _lockRotation);
        ApplyMass(_mass);
        ResetFrozenSolverPose();
        if (_freeze || PhysicsMadeStatic) return;
        Backend.SetLinearVelocity(_linearVelocity);
        Backend.SetAngularVelocity(_lockRotation ? 0 : _angularVelocity);
    }

    internal override bool MovesWithSimulation => !_freeze || FrozenKinematic;
    internal override Vector2 EffectiveGravity => Runtime.Gravity;

    internal override PhysicsServer.BodyMode RequestedBodyMode => !_freeze ?
        (_lockRotation ? PhysicsServer.BodyMode.RigidLinear : PhysicsServer.BodyMode.Rigid) :
        _freezeMode == RigidFreezeMode.Kinematic ? PhysicsServer.BodyMode.Kinematic : PhysicsServer.BodyMode.Static;

    internal override PhysicsBodyConfiguration CreateBodyConfiguration()
    {
        Runtime.FieldsInitialized = false;
        _frozenSolverPose = GlobalTransform;
        _frozenQueryPoseApplied = false;
        return new(RequestedBodyMode, FrozenKinematic ? default : _linearVelocity,
            FrozenKinematic || !_freeze && _lockRotation ? 0 : _angularVelocity, _customIntegrator ? 0 : _gravityScale,
            _canSleep, _sleeping, !_freeze && _lockRotation);
    }

    internal override void OnShapesRebuilt() => ApplyMass(_mass);

    internal override void OnBackendAdvanced()
    {
        if (FrozenKinematic && !HasServerMode) ResetFrozenSolverPose();
        var motion = Backend.GetRetainedSolverMotion();
        _linearVelocity = motion.LinearVelocity;
        _angularVelocity = motion.AngularVelocity;
        var sleeping = motion.Sleeping;
        if (sleeping != _sleeping) _sleepChangePending = true;
        _sleeping = sleeping;
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
