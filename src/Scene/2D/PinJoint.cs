using Box2D.NET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2RevoluteJoints;

namespace Electron2D;

/// <summary>Fixes two body anchors together while allowing relative rotation, optional limits and a motor.</summary>
/// <remarks>The relative angle starts at zero when the joint attaches. A finite motor torque cap keeps angular
/// limits stable and can be tuned for the connected bodies' inertia.</remarks>
public sealed class PinJoint : Joint
{
    private static readonly PropertyDescriptor[] PinProperties =
    [
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

    private const float MaximumLimit = 0.99f * MathF.PI;
    private bool _angularLimitEnabled;
    private float _angularLimitLower;
    private float _angularLimitUpper;
    private bool _motorEnabled;
    private float _motorTargetVelocity;
    private float _motorMaxTorque = 10f;

    /// <summary>Creates a detached pin joint with no active limit or motor.</summary>
    public PinJoint() { }

    /// <summary>Gets or sets whether the relative angle is limited.</summary>
    /// <value>False by default; enabled limits are measured from the attachment angle.</value>
    /// <exception cref="ArgumentOutOfRangeException">Stored limits are not ordered within ±0.99π radians when enabling.</exception>
    public bool AngularLimitEnabled
    {
        get { ThrowIfDisposed(); return _angularLimitEnabled; }
        set
        {
            EnsureJointChange();
            if (value) ValidateLimits(_angularLimitLower, _angularLimitUpper);
            if (_angularLimitEnabled == value) return;
            _angularLimitEnabled = value;
            ApplyLimits();
        }
    }

    /// <summary>Gets or sets the lower relative-angle limit in radians.</summary>
    /// <value>Zero by default. Enabled limits must be ordered and within ±0.99π radians.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or makes enabled limits invalid.</exception>
    public float AngularLimitLower
    {
        get { ThrowIfDisposed(); return _angularLimitLower; }
        set
        {
            EnsureJointChange();
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_angularLimitEnabled) ValidateLimits(value, _angularLimitUpper);
            if (_angularLimitLower == value) return;
            _angularLimitLower = value;
            ApplyLimits();
        }
    }

    /// <summary>Gets or sets the upper relative-angle limit in radians.</summary>
    /// <value>Zero by default. Enabled limits must be ordered and within ±0.99π radians.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or makes enabled limits invalid.</exception>
    public float AngularLimitUpper
    {
        get { ThrowIfDisposed(); return _angularLimitUpper; }
        set
        {
            EnsureJointChange();
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_angularLimitEnabled) ValidateLimits(_angularLimitLower, value);
            if (_angularLimitUpper == value) return;
            _angularLimitUpper = value;
            ApplyLimits();
        }
    }

    /// <summary>Gets or sets whether the pin drives relative angular velocity.</summary>
    /// <value>False by default.</value>
    /// <remarks>A nonzero <see cref="MotorMaxTorque"/> is required for motor response.</remarks>
    public bool MotorEnabled
    {
        get { ThrowIfDisposed(); return _motorEnabled; }
        set
        {
            EnsureJointChange();
            if (_motorEnabled == value) return;
            _motorEnabled = value;
            if (BackendID.index1 != 0) b2RevoluteJoint_EnableMotor(BackendID, value);
        }
    }

    /// <summary>Gets or sets the motor's relative angular speed in radians per second.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception>
    public float MotorTargetVelocity
    {
        get { ThrowIfDisposed(); return _motorTargetVelocity; }
        set
        {
            EnsureJointChange();
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_motorTargetVelocity == value) return;
            _motorTargetVelocity = value;
            if (BackendID.index1 != 0) b2RevoluteJoint_SetMotorSpeed(BackendID, value);
        }
    }

    /// <summary>Gets or sets the maximum motor torque in newton-meters.</summary>
    /// <value>Ten newton-meters by default. Increase it for heavier or larger bodies.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public float MotorMaxTorque
    {
        get { ThrowIfDisposed(); return _motorMaxTorque; }
        set
        {
            EnsureJointChange();
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (_motorMaxTorque == value) return;
            _motorMaxTorque = value;
            if (BackendID.index1 != 0) b2RevoluteJoint_SetMaxMotorTorque(BackendID, value);
        }
    }

    internal override B2JointId CreateJoint(PhysicsSpace space, PhysicsBody first, PhysicsBody second, Transform transform)
    {
        if (_angularLimitEnabled) ValidateLimits(_angularLimitLower, _angularLimitUpper);
        var definition = b2DefaultRevoluteJointDef();
        definition.@base = BaseDefinition(first, second, transform);
        definition.enableLimit = _angularLimitEnabled;
        definition.lowerAngle = _angularLimitEnabled ? _angularLimitLower : 0;
        definition.upperAngle = _angularLimitEnabled ? _angularLimitUpper : 0;
        definition.enableMotor = _motorEnabled;
        definition.motorSpeed = _motorTargetVelocity;
        definition.maxMotorTorque = _motorMaxTorque;
        return b2CreateRevoluteJoint(space.WorldID, definition);
    }

    private void ApplyLimits()
    {
        if (BackendID.index1 == 0) return;
        if (_angularLimitEnabled)
        {
            b2RevoluteJoint_SetLimits(BackendID, _angularLimitLower, _angularLimitUpper);
            b2RevoluteJoint_EnableLimit(BackendID, true);
        }
        else b2RevoluteJoint_EnableLimit(BackendID, false);
    }

    private static void ValidateLimits(float lower, float upper)
    {
        if (!float.IsFinite(lower) || !float.IsFinite(upper) || lower < -MaximumLimit || upper > MaximumLimit || lower > upper)
            throw new ArgumentOutOfRangeException(nameof(lower), "Enabled pin limits must be ordered within ±0.99π radians.");
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(PinProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreatePinJoint;

    private static Node CreatePinJoint() => new PinJoint();
}
