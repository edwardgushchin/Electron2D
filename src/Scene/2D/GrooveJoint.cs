using Box2D.NET;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2WheelJoints;

namespace Electron2D;

/// <summary>Constrains one body anchor to a finite groove on another body while leaving rotation free.</summary>
/// <remarks>The groove extends along this node's local Y axis from zero to <see cref="Length"/>.
/// The second body's anchor is sampled at <see cref="InitialOffset"/> on that axis when attached.</remarks>
public sealed class GrooveJoint : Joint
{
    private static readonly float MaxExtentSceneUnits = B2_HUGE * PhysicsSpace.UnitsPerMeter;
    private static readonly PropertyDescriptor[] GrooveProperties =
    [
        new PropertyDescriptor<GrooveJoint, float>(nameof(Length), joint => joint.Length,
            (joint, value) => joint.Length = value, _ => 50f, stored: true),
        new PropertyDescriptor<GrooveJoint, float>(nameof(InitialOffset), joint => joint.InitialOffset,
            (joint, value) => joint.InitialOffset = value, _ => 25f, stored: true)
    ];

    private float _length = 50f;
    private float _initialOffset = 25f;

    /// <summary>Creates a detached groove with a 50-unit length and 25-unit second-body offset.</summary>
    public GrooveJoint() { }

    /// <summary>Gets or sets the signed distance from the groove origin to its far endpoint.</summary>
    /// <value>50 scene units by default. Zero keeps the anchor at one point while permitting rotation.</value>
    /// <remarks>Negative values reverse the groove direction. An attached change updates both solver limits immediately
    /// while retaining the current body-local anchors. The solver accepts magnitudes through ten million scene units.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or exceeds the solver's extent range.</exception>
    public float Length
    {
        get { ThrowIfDisposed(); return _length; }
        set
        {
            EnsureJointChange();
            ValidateExtent(value, nameof(value));
            if (_length == value) return;
            _length = value;
            if (BackendID.index1 != 0)
                b2WheelJoint_SetLimits(BackendID, MathF.Min(0, value) * PhysicsSpace.MetersPerUnit,
                    MathF.Max(0, value) * PhysicsSpace.MetersPerUnit);
            else MarkJointDirty();
        }
    }

    /// <summary>Gets or sets the local Y offset used to sample the second body's anchor at attachment.</summary>
    /// <value>25 scene units by default; the value may lie outside the groove, within the solver's extent range.</value>
    /// <remarks>An attached change rebuilds the constraint before the next fixed step.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or exceeds the solver's extent range.</exception>
    public float InitialOffset
    {
        get { ThrowIfDisposed(); return _initialOffset; }
        set
        {
            EnsureJointChange();
            ValidateExtent(value, nameof(value));
            if (_initialOffset == value) return;
            _initialOffset = value;
            MarkJointDirty();
        }
    }

    internal override void ValidateJointConfiguration(PhysicsBody first, PhysicsBody second, Transform transform)
    {
        base.ValidateJointConfiguration(first, second, transform);
        var farEndpoint = transform * new Vector2(0, _length);
        var anchorB = transform * new Vector2(0, _initialOffset);
        if (!farEndpoint.IsFinite() || !anchorB.IsFinite())
            throw new InvalidOperationException("The groove endpoint and anchor must fit the finite physics coordinate range.");
        ValidateAnchor(second.ToLocal(anchorB));
    }

    internal override B2JointId CreateJoint(PhysicsSpace space, PhysicsBody first, PhysicsBody second, Transform transform)
    {
        var definition = b2DefaultWheelJointDef();
        definition.@base = BaseDefinition(first, second, transform);
        var anchorB = transform * new Vector2(0, _initialOffset);
        definition.@base.localFrameB.p = Shape.ToBackend(second.ToLocal(anchorB));
        var axisAngle = transform.Rotation + MathF.PI / 2f;
        definition.@base.localFrameA.q = b2MakeRot(axisAngle - first.GlobalRotation);
        definition.@base.localFrameB.q = b2MakeRot(axisAngle - second.GlobalRotation);
        definition.enableSpring = false;
        definition.enableLimit = true;
        definition.lowerTranslation = MathF.Min(0, _length) * PhysicsSpace.MetersPerUnit;
        definition.upperTranslation = MathF.Max(0, _length) * PhysicsSpace.MetersPerUnit;
        return b2CreateWheelJoint(space.WorldID, definition);
    }

    private static void ValidateExtent(float value, string name)
    {
        if (!float.IsFinite(value) || MathF.Abs(value) > MaxExtentSceneUnits)
            throw new ArgumentOutOfRangeException(name);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(GrooveProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateGrooveJoint;

    private static Node CreateGrooveJoint() => new GrooveJoint();
}
