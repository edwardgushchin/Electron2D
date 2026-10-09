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
        new PropertyDescriptor<Joint, float>(nameof(Bias), joint => joint.Bias,
            (joint, value) => joint.Bias = value, _ => 0f, stored: true),
        new PropertyDescriptor<Joint, float>(nameof(MaxBias), joint => joint.MaxBias,
            (joint, value) => joint.MaxBias = value, _ => float.MaxValue, stored: true),
        new PropertyDescriptor<Joint, float>(nameof(MaxForce), joint => joint.MaxForce,
            (joint, value) => joint.MaxForce = value, _ => float.MaxValue, stored: true),
        new PropertyDescriptor<Joint, bool>(nameof(DisableCollision), joint => joint.DisableCollision,
            (joint, value) => joint.DisableCollision = value, _ => true, stored: true)
    ];

    private PhysicsSpace? _space;
    private PhysicsBody? _bodyA;
    private PhysicsBody? _bodyB;
    private bool _serverOverride;
    private string _nodeA = string.Empty;
    private string _nodeB = string.Empty;
    private bool _dirty = true;

    /// <summary>Creates a detached, unconnected joint.</summary>
    /// <param name="type">The immutable concrete scene role.</param>
    private protected Joint(PhysicsServer.JointType type) => Runtime = PhysicsServer.Service.RegisterSceneJoint(this, type);

    internal PhysicsJointRuntime Runtime { get; }
    internal PhysicsSpace? AttachmentSpace => _space;
    internal bool HasServerOverride => _serverOverride;

    /// <summary>Gets or sets the fraction of positional joint error requested for correction per substep.</summary>
    /// <value>A finite value from zero to one. Zero inherits the space constraint bias, initially 0.2.</value>
    /// <remarks>Pin and groove use this for anchor and limit recovery. Springs use their force coefficients and have no positional recovery rows. Live changes wake connected bodies; sampled anchors remain unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the finite zero-to-one range.</exception>
    public float Bias
    {
        get { ThrowIfDisposed(); return Runtime.Bias; }
        set { EnsureJointChange(); Runtime.SetBias(value); }
    }

    /// <summary>Gets or sets the maximum positional correction speed.</summary>
    /// <value>A finite nonnegative cap in scene units per second for linear correction and radians per second for angular stops; defaults to <see cref="float.MaxValue"/>.</value>
    /// <remarks>The vector cap is additionally bounded by the world's 200-unit correction guard. Zero disables positional recovery while preserving velocity constraints. Springs have no positional recovery rows. This projects the shared server setting and is stored in packed scenes.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public float MaxBias
    {
        get { ThrowIfDisposed(); return Runtime.MaxBias; }
        set { EnsureJointChange(); Runtime.SetMaxBias(value); }
    }

    /// <summary>Gets or sets the joint's per-second impulse budget.</summary>
    /// <value>A finite nonnegative scalar, or the default <see cref="float.MaxValue"/> for no cap.</value>
    /// <remarks>Linear impulse uses kilograms times scene units per second, and the separate pure-angular channel uses kilograms times squared scene units per second. Each substep permits this value times its duration in each channel. All linear axes share a vector budget. Springs cap their combined elastic and damping impulse. The pin motor torque limit remains an additional cap. Live edits wake connected bodies and clear old impulses. This projects the shared server setting and is stored in packed scenes.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public float MaxForce
    {
        get { ThrowIfDisposed(); return Runtime.MaxForce; }
        set { EnsureJointChange(); Runtime.SetMaxForce(value); }
    }

    /// <summary>Gets the stable physics server identity of this scene joint.</summary>
    /// <returns>A nonempty RID unchanged by connection rebuilds, clear or tree reentry.</returns>
    public RID GetRID() { ThrowIfDisposed(); return Runtime.RID; }

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
    /// <value>True by default. A change updates contact policy without resampling the local anchors.</value>
    /// <remarks>Changing this value refreshes endpoint fixtures and the pair contribution used by body motion tests.
    /// Other joints and explicit body exceptions can independently retain suppression.</remarks>
    /// <exception cref="InvalidOperationException">An attached write is off the owner thread or occurs during a solver step.</exception>
    public bool DisableCollision
    {
        get { ThrowIfDisposed(); return Runtime.DisableCollision; }
        set { EnsureJointChange(); Runtime.SetDisableCollision(value); }
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
        Runtime.EnsureAccess();
        Runtime.Clear();
        _space = space;
        _serverOverride = false;
        _dirty = true;
    }

    internal void PrepareBackend()
    {
        var space = _space;
        if (space is null || _serverOverride) return;
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
        ValidateJointConfiguration(bodyA, bodyB, transform);
        ConfigureJoint(space, bodyA, bodyB, transform);
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
        if (!Runtime.HasBackend && !_serverOverride) _dirty = true;
    }

    internal void DetachBackend()
    {
        DetachJoint();
        _space = null;
        _serverOverride = false;
        _dirty = true;
    }

    internal virtual void ValidateJointConfiguration(PhysicsBody first, PhysicsBody second, Transform transform)
    {
        ValidateAnchor(first.ToLocal(transform.Origin));
        ValidateAnchor(second.ToLocal(transform.Origin));
    }

    internal static void ValidateAnchor(Vector2 local)
    {
        if (!local.IsFinite())
            throw new InvalidOperationException("Joint anchors must fit the finite physics coordinate range.");
    }

    internal abstract void ConfigureJoint(PhysicsSpace space, PhysicsBody first, PhysicsBody second, Transform transform);

    internal void MarkServerOverride()
    {
        _serverOverride = true; _dirty = false; _bodyA = _bodyB = null;
    }

    internal void EnsureJointChange()
    {
        EnsureMutable();
        _space?.EnsureQueryAccess();
        Runtime.EnsureAccess();
    }

    internal void MarkJointDirty() { InvalidateCanvas(); _serverOverride = false; _dirty = true; }

    private PhysicsBody? Resolve(string path) => string.IsNullOrWhiteSpace(path) ? null : GetNodeOrNull(path) as PhysicsBody;

    private void OnNodeRenamed(SceneTree tree, Node node)
    {
        MarkJointDirty();
    }

    private void DetachJoint()
    {
        Runtime.Clear();
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
    /// <remarks>Records local pin, groove or spring anchor markers when scene collision diagnostics are enabled.</remarks>
    protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationDraw) PhysicsDebugDrawing.Joint(this); }

    /// <summary>Checks scene and dependent joint world ownership before beginning disposal.</summary>
    /// <exception cref="InvalidOperationException">A related active world is off-owner or stepping.</exception>
    protected override void ValidateDisposal()
    {
        _space?.EnsureReleaseAccess();
        Runtime.EnsureAccess(releasing: true);
        base.ValidateDisposal();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _space?.Remove(this);
        if (disposing) PhysicsServer.Service.UnregisterSceneJoint(Runtime.RID);
        base.Dispose(disposing);
    }
}
