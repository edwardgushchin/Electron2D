using Box2D.NET;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2MathFunction;

namespace Electron2D;

/// <summary>Connects two scene physics bodies in their shared physics world.</summary>
/// <remarks>Node paths resolve from this node. A valid connection is built after both bodies enter the tree
/// and is removed before either backend body leaves. The anchor is sampled at connection time; moving this
/// node alone does not retune it. Concrete engine-owned subclasses supply the constraint.</remarks>
public abstract class Joint : Entity
{
    private static readonly PropertyDescriptor[] JointProperties =
    [
        new PropertyDescriptor<Joint, string>(nameof(NodeA), joint => joint.NodeA,
            (joint, value) => joint.NodeA = value, _ => string.Empty, stored: true),
        new PropertyDescriptor<Joint, string>(nameof(NodeB), joint => joint.NodeB,
            (joint, value) => joint.NodeB = value, _ => string.Empty, stored: true),
        new PropertyDescriptor<Joint, bool>(nameof(DisableCollision), joint => joint.DisableCollision,
            (joint, value) => joint.DisableCollision = value, _ => true, stored: true)
    ];

    private PhysicsSpace? _space;
    private PhysicsBody? _bodyA;
    private PhysicsBody? _bodyB;
    private B2JointId _jointID;
    private string _nodeA = string.Empty;
    private string _nodeB = string.Empty;
    private bool _disableCollision = true;
    private bool _dirty = true;

    /// <summary>Creates a detached, unconnected joint.</summary>
    private protected Joint() { }

    /// <summary>Gets or sets the node path to the first physics body.</summary>
    /// <value>An empty path by default. An unresolved or non-body path leaves the joint unconfigured.</value>
    /// <remarks>The path uses <see cref="Node.GetNodeOrNull(string)"/> syntax. A changed attached path is resolved
    /// before the next fixed step; later body entry or rename can satisfy an unresolved path.</remarks>
    /// <exception cref="ArgumentNullException">The assigned path is null.</exception>
    /// <exception cref="InvalidOperationException">An attached write is off the owner thread or occurs during a solver step.</exception>
    public string NodeA
    {
        get { ThrowIfDisposed(); return _nodeA; }
        set
        {
            EnsureJointChange();
            ArgumentNullException.ThrowIfNull(value);
            if (_nodeA == value) return;
            _nodeA = value;
            MarkJointDirty();
        }
    }

    /// <summary>Gets or sets the node path to the second physics body.</summary>
    /// <value>An empty path by default. Both paths must resolve to distinct attached bodies.</value>
    /// <remarks>Uses the same resolution and fixed-step timing as <see cref="NodeA"/>.</remarks>
    /// <exception cref="ArgumentNullException">The assigned path is null.</exception>
    /// <exception cref="InvalidOperationException">An attached write is off the owner thread or occurs during a solver step.</exception>
    public string NodeB
    {
        get { ThrowIfDisposed(); return _nodeB; }
        set
        {
            EnsureJointChange();
            ArgumentNullException.ThrowIfNull(value);
            if (_nodeB == value) return;
            _nodeB = value;
            MarkJointDirty();
        }
    }

    /// <summary>Gets or sets whether the connected bodies omit mutual contacts.</summary>
    /// <value>True by default. A change rebuilds the solver joint before the next physics step.</value>
    /// <remarks>Changing this value also refreshes endpoint fixtures so existing overlaps adopt the new policy.</remarks>
    /// <exception cref="InvalidOperationException">An attached write is off the owner thread or occurs during a solver step.</exception>
    public bool DisableCollision
    {
        get { ThrowIfDisposed(); return _disableCollision; }
        set
        {
            EnsureJointChange();
            if (_disableCollision == value) return;
            _disableCollision = value;
            MarkJointDirty();
            _bodyA?.MarkShapesDirty();
            _bodyB?.MarkShapesDirty();
        }
    }

    /// <summary>Returns warnings for missing, invalid or duplicate body endpoints.</summary>
    /// <returns>Current scene configuration warnings.</returns>
    /// <remarks>A detached joint has no endpoint warnings. This query does not configure a constraint.</remarks>
    /// <exception cref="InvalidOperationException">An attached query is off the scene owner thread.</exception>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings().ToList();
        if (!IsInsideTree) return warnings.ToArray();
        var first = Resolve(_nodeA);
        var second = Resolve(_nodeB);
        if (first is null || second is null)
            warnings.Add("Both joint paths must resolve to physics bodies in this scene.");
        else if (ReferenceEquals(first, second))
            warnings.Add("Joint endpoints must be different physics bodies.");
        else if (!first.HasBackend || !second.HasBackend || !ReferenceEquals(first.Space, second.Space))
            warnings.Add("Joint endpoints must belong to the same active physics world.");
        return warnings.ToArray();
    }

    internal void AttachBackend(PhysicsSpace space)
    {
        _space = space;
        _dirty = true;
    }

    internal void PrepareBackend()
    {
        var space = _space;
        if (space is null) return;
        if (_bodyA is { } first && _bodyB is { } second &&
            (!ReferenceEquals(first.Space, space) || !ReferenceEquals(second.Space, space)))
            _dirty = true;
        if (!_dirty) return;

        var bodyA = Resolve(_nodeA);
        var bodyB = Resolve(_nodeB);
        if (bodyA is null || bodyB is null || ReferenceEquals(bodyA, bodyB) ||
            !ReferenceEquals(bodyA.Space, space) || !ReferenceEquals(bodyB.Space, space))
        {
            DetachJoint();
            _dirty = false;
            return;
        }

        var transform = GlobalTransform;
        if (!transform.IsFinite() || !transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(transform.Skew))
            throw new InvalidOperationException("Physics joints require unit global scale and zero skew.");
        DetachJoint();
        _jointID = CreateJoint(space, bodyA, bodyB, transform);
        if (_jointID.index1 == 0) throw new InvalidOperationException("The physics solver did not create the joint.");
        _bodyA = bodyA;
        _bodyB = bodyB;
        _dirty = false;
    }

    internal void BodyLeaving(PhysicsBody body)
    {
        if (!ReferenceEquals(body, _bodyA) && !ReferenceEquals(body, _bodyB)) return;
        DetachJoint();
        _dirty = true;
    }

    internal void BodyArrived()
    {
        if (_jointID.index1 == 0) _dirty = true;
    }

    internal void DetachBackend()
    {
        DetachJoint();
        _space = null;
        _dirty = true;
    }

    internal B2JointId BackendID => _jointID;
    internal B2JointDef BaseDefinition(PhysicsBody first, PhysicsBody second, Transform transform)
    {
        var definition = b2DefaultJointDef();
        definition.bodyIdA = first.BackendID;
        definition.bodyIdB = second.BackendID;
        definition.localFrameA.p = Shape.ToBackend(first.ToLocal(transform.Origin));
        definition.localFrameB.p = Shape.ToBackend(second.ToLocal(transform.Origin));
        definition.localFrameA.q = b2MakeRot(transform.Rotation - first.GlobalRotation);
        definition.localFrameB.q = b2MakeRot(transform.Rotation - second.GlobalRotation);
        definition.collideConnected = !_disableCollision;
        return definition;
    }

    internal abstract B2JointId CreateJoint(PhysicsSpace space, PhysicsBody first, PhysicsBody second, Transform transform);

    internal void EnsureJointChange()
    {
        EnsureMutable();
        _space?.EnsureQueryAccess();
    }

    internal void MarkJointDirty() => _dirty = true;

    private PhysicsBody? Resolve(string path) => string.IsNullOrWhiteSpace(path) ? null : GetNodeOrNull(path) as PhysicsBody;

    private void OnNodeRenamed(SceneTree tree, Node node)
    {
        _dirty = true;
    }

    private void DetachJoint()
    {
        if (_jointID.index1 != 0) b2DestroyJoint(_jointID, wakeAttached: true);
        _jointID = default;
        _bodyA = null;
        _bodyB = null;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(JointProperties);

    /// <inheritdoc />
    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        Tree?.RegisterPhysicsJoint(this);
        if (Tree is { } tree) tree.NodeRenamed += OnNodeRenamed;
    }

    /// <inheritdoc />
    protected override void OnReady()
    {
        base.OnReady();
        PrepareBackend();
    }

    /// <inheritdoc />
    protected override void OnExitTree()
    {
        if (Tree is { } tree) tree.NodeRenamed -= OnNodeRenamed;
        try { Tree?.UnregisterPhysicsJoint(this); }
        finally { base.OnExitTree(); }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _space?.Remove(this);
        base.Dispose(disposing);
    }
}
