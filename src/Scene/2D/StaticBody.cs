namespace Electron2D;

/// <summary>A stationary collision body that constrains simulated dynamic bodies.</summary>
/// <remarks>Surface velocity authoring commits before selected publication. A partial failure retries current requested values
/// before numerical access, live queries or a nonzero step, including an equal-property assignment after failure.
/// Owner, numerical-borrow and solver/failure preconditions reject before authoring changes, including unchanged assignments.
/// Authoring getters remain non-preparing; detachment retains the requested values without repairing the retired attachment.</remarks>
public partial class StaticBody : PhysicsBody
{
    private static readonly PropertyDescriptor[] BodyProperties =
    [
        new PropertyDescriptor<StaticBody, Vector2>(nameof(ConstantLinearVelocity), body => body.ConstantLinearVelocity,
            (body, value) => body.ConstantLinearVelocity = value, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<StaticBody, float>(nameof(ConstantAngularVelocity), body => body.ConstantAngularVelocity,
            (body, value) => body.ConstantAngularVelocity = value, _ => 0, stored: true),
        new PropertyDescriptor<StaticBody, PhysicsMaterial?>(nameof(PhysicsMaterialOverride), body => body.PhysicsMaterialOverride,
            (body, value) => body.PhysicsMaterialOverride = value, _ => null, stored: true)
    ];

    private Vector2 _constantLinearVelocity;
    private float _constantAngularVelocity;

    /// <summary>Gets or sets global-axis surface velocity without translating the body.</summary>
    /// <value>Finite scene units per second; zero by default.</value>
    /// <remarks>Contact response and point-velocity queries include this velocity. On an AnimatableBody
    /// it adds to target motion without changing the target pose. Edits wake touching bodies.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The velocity is not finite.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owner thread or solver phase.</exception>
    /// <exception cref="ObjectDisposedException">The body has been disposed.</exception>
    public Vector2 ConstantLinearVelocity
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _constantLinearVelocity; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value));
            PreparePendingServerMode(writing: true);
            if (_constantLinearVelocity == value) return;
            SetSurfaceLinearVelocity(value, false);
        }
    }

    /// <summary>Gets or sets surface rotation speed without rotating the body.</summary>
    /// <value>Finite radians per second; zero by default. Positive values follow the scene rotation direction.</value>
    /// <remarks>Point velocity includes angular speed around the body center of mass. On an AnimatableBody
    /// it adds to target motion without changing the target pose. Edits wake touching bodies.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The velocity is not finite.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owner thread or solver phase.</exception>
    /// <exception cref="ObjectDisposedException">The body has been disposed.</exception>
    public float ConstantAngularVelocity
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _constantAngularVelocity; }
        set
        {
            EnsureMutable();
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            PreparePendingServerMode(writing: true);
            if (_constantAngularVelocity == value) return;
            SetSurfaceAngularVelocity(value, false);
        }
    }

    internal void SetSurfaceLinearVelocity(Vector2 value, bool clearSolver)
    {
        PreparePendingServerMode(writing: true); _constantLinearVelocity = value;
        if (HasBackend) Backend.PublishMotion(linear: clearSolver ? Vector2.Zero : null, surface: (value, _constantAngularVelocity));
    }
    internal void SetSurfaceAngularVelocity(float value, bool clearSolver)
    {
        PreparePendingServerMode(writing: true); _constantAngularVelocity = value;
        if (HasBackend) Backend.PublishMotion(angular: clearSolver ? 0 : null, surface: (_constantLinearVelocity, value));
    }

    internal override void OnShapesRebuilt()
    {
        base.OnShapesRebuilt();
        if (!HasServerMode) Backend.SetSurfaceVelocity(_constantLinearVelocity, _constantAngularVelocity);
    }

    internal override void OnBodyTypeChanged() =>
        Backend.SetSurfaceVelocity(_constantLinearVelocity, _constantAngularVelocity);

    /// <summary>Creates a detached static body with no collision shapes.</summary>
    public StaticBody() { }

    /// <summary>Gets or sets a borrowed surface material for every child collision shape.</summary>
    /// <value>Null by default, which uses friction one and bounce zero.</value>
    /// <remarks>Property edits rebuild fixtures before the next physics step. The caller owns the material.</remarks>
    /// <exception cref="ObjectDisposedException">The assigned material or body has been disposed.</exception>
    public PhysicsMaterial? PhysicsMaterialOverride
    {
        get { ThrowIfDisposed(); return MaterialOverride; }
        set => SetMaterialOverride(value);
    }

    internal override bool MovesWithSimulation => false;

    internal override PhysicsServer.BodyMode RequestedBodyMode => PhysicsServer.BodyMode.Static;

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(BodyProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(StaticBody)
        ? CreateStaticBody : base.CreateSceneInstanceFactory();

    private static Node CreateStaticBody() => new StaticBody();
}
