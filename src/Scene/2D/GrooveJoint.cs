namespace Electron2D;

/// <summary>Constrains one body anchor to a finite groove on another body while leaving rotation free.</summary>
/// <remarks>The groove extends along this node's local Y axis from zero to <see cref="Length"/>.
/// The second body's anchor is sampled at <see cref="InitialOffset"/> on that axis when attached.</remarks>
public sealed class GrooveJoint : Joint
{
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
    public GrooveJoint() : base(PhysicsServer.JointType.Groove) { }

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
            Runtime.SetGrooveLength(value);
            if (!Runtime.HasBackend || HasServerOverride) MarkJointDirty();
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

    internal override void ConfigureJoint(PhysicsSpace space, PhysicsBody first, PhysicsBody second, Transform transform) =>
        Runtime.ConfigureSceneGroove(transform, _length, _initialOffset, first.GetRID(), second.GetRID());

    private static void ValidateExtent(float value, string name)
    {
        if (!float.IsFinite(value) || MathF.Abs(value) > PhysicsJointRuntime.MaxExtentSceneUnits)
            throw new ArgumentOutOfRangeException(name);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(GrooveProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateGrooveJoint;

    private static Node CreateGrooveJoint() => new GrooveJoint();
}
