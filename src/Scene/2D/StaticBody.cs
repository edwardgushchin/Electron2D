namespace Electron2D;

/// <summary>A stationary collision body that constrains simulated dynamic bodies.</summary>
public class StaticBody : PhysicsBody
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
            if (_constantLinearVelocity == value) return;
            if (HasBackend) Backend.SetSurfaceVelocity(value, _constantAngularVelocity);
            _constantLinearVelocity = value;
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
            if (_constantAngularVelocity == value) return;
            if (HasBackend) Backend.SetSurfaceVelocity(_constantLinearVelocity, value);
            _constantAngularVelocity = value;
        }
    }

    internal override void OnShapesRebuilt()
    {
        base.OnShapesRebuilt();
        Backend.SetSurfaceVelocity(_constantLinearVelocity, _constantAngularVelocity);
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
