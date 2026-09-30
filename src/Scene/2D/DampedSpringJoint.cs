using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

/// <summary>Connects two body-local anchors with a Hooke spring and axial damping.</summary>
/// <remarks>The second anchor is sampled along local Y at Length when connected. The spring can stretch beyond
/// Length; RestLength controls equilibrium. Scene units convert internally to meters. Forces act at both anchors
/// and can rotate the bodies. The scene owner thread owns attached configuration and simulation.</remarks>
public sealed class DampedSpringJoint : Joint
{
    private static readonly float MaxExtentSceneUnits = B2_HUGE * PhysicsSpace.UnitsPerMeter;
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
    private float _restLength;
    private float _stiffness = 20f;
    private float _damping = 1f;
    private B2BodyId _firstID;
    private B2BodyId _secondID;
    private B2Vec2 _anchorA;
    private B2Vec2 _anchorB;
    private B2Vec2 _pointA;
    private B2Vec2 _pointB;
    private B2Vec2 _pendingImpulse;

    /// <summary>Creates a detached 50-unit spring with stiffness 20 and damping 1.</summary>
    public DampedSpringJoint() { }

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
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _restLength; }
        set
        {
            EnsureJointChange();
            ValidateExtent(value);
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _restLength = value;
        }
    }

    /// <summary>Gets or sets the spring force per unit of extension in kilograms per second squared.</summary>
    /// <value>20 by default. Zero disables the elastic force while retaining damping.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, or a write occurs during a solver step.</exception>
    public float Stiffness
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _stiffness; }
        set { EnsureJointChange(); ValidateCoefficient(value); _stiffness = value; }
    }

    /// <summary>Gets or sets the axial damping coefficient in kilograms per second.</summary>
    /// <value>One by default. Zero leaves axial velocity undamped; tangential motion is not directly damped.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner, or a write occurs during a solver step.</exception>
    public float Damping
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _damping; }
        set { EnsureJointChange(); ValidateCoefficient(value); _damping = value; }
    }

    internal override void ValidateJointConfiguration(PhysicsBody first, PhysicsBody second, Transform transform)
    {
        base.ValidateJointConfiguration(first, second, transform);
        var endpoint = transform * new Vector2(0, _length);
        if (!endpoint.IsFinite()) throw new InvalidOperationException("Spring geometry exceeds the finite physics coordinate range.");
        ValidateAnchor(second.ToLocal(endpoint));
    }

    internal override B2JointId CreateJoint(PhysicsSpace space, PhysicsBody first, PhysicsBody second, Transform transform)
    {
        var definition = b2DefaultFilterJointDef();
        definition.@base = BaseDefinition(first, second, transform);
        definition.@base.localFrameA.p = b2Body_GetLocalPoint(first.BackendID, Shape.ToBackend(transform.Origin));
        definition.@base.localFrameB.p = b2Body_GetLocalPoint(second.BackendID, Shape.ToBackend(transform * new Vector2(0, _length)));
        var id = b2CreateFilterJoint(space.WorldID, definition);
        _firstID = first.BackendID;
        _secondID = second.BackendID;
        _anchorA = definition.@base.localFrameA.p;
        _anchorB = definition.@base.localFrameB.p;
        return id;
    }

    internal override void PrepareSolverStep(float delta)
    {
        _pendingImpulse = default;
        if (BackendID.index1 == 0 || _stiffness == 0 && _damping == 0) return;
        _pointA = b2Body_GetWorldPoint(_firstID, _anchorA);
        _pointB = b2Body_GetWorldPoint(_secondID, _anchorB);
        var dx = (double)_pointB.X - _pointA.X;
        var dy = (double)_pointB.Y - _pointA.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < FLT_EPSILON) return;
        var nx = dx / distance;
        var ny = dy / distance;
        var first = PhysicsBodyRuntime.Simulation(_firstID);
        var second = PhysicsBodyRuntime.Simulation(_secondID);
        var crossA = ((double)_pointA.X - first.center.X) * ny - ((double)_pointA.Y - first.center.Y) * nx;
        var crossB = ((double)_pointB.X - second.center.X) * ny - ((double)_pointB.Y - second.center.Y) * nx;
        var inverse = first.invMass + (double)second.invMass + first.invInertia * crossA * crossA + second.invInertia * crossB * crossB;
        if (inverse == 0) return;
        var velocityA = b2Body_GetLinearVelocity(_firstID);
        var velocityB = b2Body_GetLinearVelocity(_secondID);
        var angularA = b2Body_GetAngularVelocity(_firstID);
        var angularB = b2Body_GetAngularVelocity(_secondID);
        var speed = ((double)velocityB.X - velocityA.X) * nx + ((double)velocityB.Y - velocityA.Y) * ny + angularB * crossB - angularA * crossA;
        var rest = (_restLength == 0 ? Math.Abs(_length) : _restLength) * (double)PhysicsSpace.MetersPerUnit;
        var elastic = (rest - distance) * _stiffness * delta;
        var decay = Math.Exp(-(double)_damping * delta * inverse);
        var total = elastic * decay - speed * (1 - decay) / inverse;
        var impulse = new B2Vec2((float)(nx * total), (float)(ny * total));
        if (!float.IsFinite(impulse.X) || !float.IsFinite(impulse.Y))
            throw new InvalidOperationException("Spring impulse exceeds the finite physics range.");
        _pendingImpulse = impulse;
    }

    internal override void ValidateSolverStep(PhysicsSpace space)
    {
        if (_pendingImpulse.X == 0 && _pendingImpulse.Y == 0) return;
        space.ValidateJointImpulse(_firstID, new(-_pendingImpulse.X, -_pendingImpulse.Y), _pointA);
        space.ValidateJointImpulse(_secondID, _pendingImpulse, _pointB);
    }

    internal override void ApplySolverStep()
    {
        if (_pendingImpulse.X == 0 && _pendingImpulse.Y == 0) return;
        b2Body_ApplyLinearImpulse(_firstID, new(-_pendingImpulse.X, -_pendingImpulse.Y), _pointA, wake: true);
        b2Body_ApplyLinearImpulse(_secondID, _pendingImpulse, _pointB, wake: true);
    }

    private static void ValidateExtent(float value)
    {
        if (!float.IsFinite(value) || MathF.Abs(value) > MaxExtentSceneUnits) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void ValidateCoefficient(float value)
    {
        if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SpringProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateSpringJoint;

    private static Node CreateSpringJoint() => new DampedSpringJoint();
}
