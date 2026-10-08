namespace Electron2D;

/// <summary>Fixes two body anchors together while allowing relative rotation, optional limits and a motor.</summary>
/// <remarks>The relative angle starts at zero when the joint attaches. A finite motor torque cap keeps angular
/// limits stable and can be tuned for the connected bodies' inertia.</remarks>
public sealed class PinJoint : Joint
{
    private static readonly PropertyDescriptor[] PinProperties =
    [
        new PropertyDescriptor<PinJoint, float>(nameof(Softness), joint => joint.Softness,
            (joint, value) => joint.Softness = value, _ => 0f, stored: true),
        new PropertyDescriptor<PinJoint, bool>(nameof(AngularLimitEnabled), joint => joint.AngularLimitEnabled,
            (joint, value) => joint.AngularLimitEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<PinJoint, float>(nameof(AngularLimitLower), joint => joint.AngularLimitLower,
            (joint, value) => joint.AngularLimitLower = value, _ => 0f, stored: true),
        new PropertyDescriptor<PinJoint, float>(nameof(AngularLimitUpper), joint => joint.AngularLimitUpper,
            (joint, value) => joint.AngularLimitUpper = value, _ => 0f, stored: true),
        new PropertyDescriptor<PinJoint, bool>(nameof(MotorEnabled), joint => joint.MotorEnabled,
            (joint, value) => joint.MotorEnabled = value, _ => false, stored: true),
        new PropertyDescriptor<PinJoint, float>(nameof(MotorTargetVelocity), joint => joint.MotorTargetVelocity,
            (joint, value) => joint.MotorTargetVelocity = value, _ => 0f, stored: true),
        new PropertyDescriptor<PinJoint, float>(nameof(MotorMaxTorque), joint => joint.MotorMaxTorque,
            (joint, value) => joint.MotorMaxTorque = value, _ => 10f, stored: true)
    ];

    /// <summary>Creates a detached pin joint with no active limit or motor.</summary>
    public PinJoint() : base(PhysicsServer.JointType.Pin) { }

    /// <summary>Gets or sets linear anchor compliance in inverse-kilogram units.</summary>
    /// <value>A finite nonnegative value; zero keeps a rigid anchor. Larger values allow more relative anchor velocity.</value>
    /// <remarks>This changes the linear effective mass and impulse feedback, not an angular spring. Live edits preserve sampled anchors, wake connected bodies and clear old joint impulses.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public float Softness
    {
        get { ThrowIfDisposed(); return Runtime.PinSoftness; }
        set { EnsureJointChange(); Runtime.SetPinSoftness(value); }
    }

    /// <summary>Gets or sets whether the relative angle is limited.</summary>
    /// <value>False by default; enabled limits are measured from the attachment angle.</value>
    /// <exception cref="ArgumentOutOfRangeException">Stored limits are not ordered within ±0.99π radians when enabling.</exception>
    public bool AngularLimitEnabled
    {
        get { ThrowIfDisposed(); return Runtime.PinLimitEnabled; }
        set { EnsureJointChange(); Runtime.SetPinLimitEnabled(value); }
    }

    /// <summary>Gets or sets the lower relative-angle limit in radians.</summary>
    /// <value>Zero by default. Enabled limits must be ordered and within ±0.99π radians.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or makes enabled limits invalid.</exception>
    public float AngularLimitLower
    {
        get { ThrowIfDisposed(); return Runtime.PinLimitLower; }
        set { EnsureJointChange(); Runtime.SetPinLimitLower(value); }
    }

    /// <summary>Gets or sets the upper relative-angle limit in radians.</summary>
    /// <value>Zero by default. Enabled limits must be ordered and within ±0.99π radians.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or makes enabled limits invalid.</exception>
    public float AngularLimitUpper
    {
        get { ThrowIfDisposed(); return Runtime.PinLimitUpper; }
        set { EnsureJointChange(); Runtime.SetPinLimitUpper(value); }
    }

    /// <summary>Gets or sets whether the pin drives relative angular velocity.</summary>
    /// <value>False by default.</value>
    /// <remarks>A nonzero <see cref="MotorMaxTorque"/> is required for motor response.</remarks>
    public bool MotorEnabled
    {
        get { ThrowIfDisposed(); return Runtime.PinMotorEnabled; }
        set { EnsureJointChange(); Runtime.SetPinMotorEnabled(value); }
    }

    /// <summary>Gets or sets the motor's relative angular speed in radians per second.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float MotorTargetVelocity
    {
        get { ThrowIfDisposed(); return Runtime.PinMotorVelocity; }
        set { EnsureJointChange(); Runtime.SetPinMotorVelocity(value); }
    }

    /// <summary>Gets or sets the maximum motor torque in newton-meters.</summary>
    /// <value>Ten newton-meters by default. Increase it for heavier or larger bodies.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public float MotorMaxTorque
    {
        get { ThrowIfDisposed(); return Runtime.PinMotorMaxTorque; }
        set { EnsureJointChange(); Runtime.SetPinMotorMaxTorque(value); }
    }
    internal override void ConfigureJoint(PhysicsSpace space, PhysicsBody first, PhysicsBody second, Transform transform) =>
        Runtime.ConfigurePin(transform.Origin, first.GetRID(), second.GetRID(), preserve: true);

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(PinProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreatePinJoint;

    private static Node CreatePinJoint() => new PinJoint();
}
