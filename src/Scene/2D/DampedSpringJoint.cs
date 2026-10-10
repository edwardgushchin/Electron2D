namespace Electron2D;

/// <summary>Connects two body-local anchors with a Hooke spring and axial damping.</summary>
/// <remarks>The second anchor is sampled along local Y at Length when connected. The spring can stretch beyond
/// Length; RestLength controls equilibrium. Scene units convert internally to meters. Forces act at both anchors
/// and can rotate the bodies. The scene owner thread owns attached configuration and simulation.</remarks>
public sealed class DampedSpringJoint : Joint
{
    private static readonly PropertyDescriptor[] SpringProperties =
    [
        new PropertyDescriptor<DampedSpringJoint, float>(nameof(Length), joint => joint.Length,
            (joint, value) => joint.Length = value, _ => 50f, stored: true),
        new PropertyDescriptor<DampedSpringJoint, float>(nameof(RestLength), joint => joint.RestLength,
            (joint, value) => joint.RestLength = value, _ => 0f, stored: true),
        new PropertyDescriptor<DampedSpringJoint, float>(nameof(Stiffness), joint => joint.Stiffness,
            (joint, value) => joint.Stiffness = value, _ => 20f, stored: true),
        new PropertyDescriptor<DampedSpringJoint, float>(nameof(Damping), joint => joint.Damping,
            (joint, value) => joint.Damping = value, _ => 1f, stored: true)
    ];

    private float _length = 50f;
    /// <summary>Creates a detached 50-unit spring with stiffness 20 and damping 1.</summary>
    public DampedSpringJoint() : base(PhysicsServer.JointType.DampedSpring) { }

    /// <summary>Gets or sets the signed local-Y offset used to sample the second body's anchor.</summary>
    /// <value>50 scene units by default; negative values reverse anchor placement.</value>
    /// <remarks>A change rebuilds the connection before the next nonzero fixed step. This value is not a stretch limit.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or exceeds ten million scene units in magnitude.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, or a write occurs during a solver step.</exception>
    public float Length
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _length; }
        set
        {
            EnsureJointChange();
            ValidateExtent(value);
            if (_length == value) return;
            _length = value;
            MarkJointDirty();
        }
    }

    /// <summary>Gets or sets the spring's relaxed anchor separation.</summary>
    /// <value>Zero by default, which uses the magnitude of Length; otherwise a positive scene-unit distance.</value>
    /// <remarks>Changes affect the next solver interval without replacing the body-local anchors.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, nonfinite or exceeds ten million scene units.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, or a write occurs during a solver step.</exception>
    public float RestLength
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return Runtime.SpringRestLength; }
        set { EnsureJointChange(); Runtime.SetSpringRestLength(value, automatic: true); }
    }

    /// <summary>Gets or sets the spring force per unit of extension in kilograms per second squared.</summary>
    /// <value>20 by default. Zero disables the elastic force while retaining damping.</value>
    /// <remarks>The combined elastic and damping impulse observes <see cref="Joint.MaxForce"/>. Unrepresentable uncapped GPU execution leaves the world failed until disposal.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, or a write occurs during a solver step.</exception>
    public float Stiffness
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return Runtime.SpringStiffness; }
        set { EnsureJointChange(); Runtime.SetSpringStiffness(value); }
    }

    /// <summary>Gets or sets the axial damping coefficient in kilograms per second.</summary>
    /// <value>One by default. Zero leaves axial velocity undamped; tangential motion is not directly damped.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, or a write occurs during a solver step.</exception>
    public float Damping
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return Runtime.SpringDamping; }
        set { EnsureJointChange(); Runtime.SetSpringDamping(value); }
    }

    internal override void ValidateJointConfiguration(PhysicsBody first, PhysicsBody second, Transform transform)
    {
        base.ValidateJointConfiguration(first, second, transform);
        var endpoint = transform * new Vector2(0, _length);
        if (!endpoint.IsFinite()) throw new InvalidOperationException("Spring geometry exceeds the finite physics coordinate range.");
        ValidateAnchor(second.ToLocal(endpoint));
    }

    internal override void ConfigureJoint(PhysicsSpace space, PhysicsBody first, PhysicsBody second, Transform transform)
    {
        Runtime.SpringAutomaticLength = _length;
        Runtime.SetSpringRestLength(Runtime.SpringRestLength, automatic: true);
        Runtime.ConfigureSpring(transform.Origin, transform * new Vector2(0, _length), first.GetRID(), second.GetRID(), preserve: true);
    }

    private static void ValidateExtent(float value)
    {
        if (!float.IsFinite(value) || MathF.Abs(value) > PhysicsJointRuntime.MaxExtentSceneUnits) throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SpringProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateSpringJoint;

    private static Node CreateSpringJoint() => new DampedSpringJoint();
}
