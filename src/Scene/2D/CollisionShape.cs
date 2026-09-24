namespace Electron2D;

/// <summary>Places a borrowed collision shape beneath a physics body.</summary>
/// <remarks>The direct parent must be a <see cref="PhysicsBody"/> for this shape to participate in physics.
/// A shape resource remains caller-owned. Geometry changes are applied before the next physics step.</remarks>
public sealed class CollisionShape : Entity
{
    private static readonly PropertyDescriptor[] ShapeProperties =
    [
        new PropertyDescriptor<CollisionShape, Shape?>(nameof(Shape), node => node.Shape, (node, value) => node.Shape = value, _ => null, stored: true),
        new PropertyDescriptor<CollisionShape, bool>(nameof(Disabled), node => node.Disabled, (node, value) => node.Disabled = value, _ => false, stored: true)
    ];

    private Shape? _shape;
    private PhysicsBody? _body;
    private bool _disabled;

    /// <summary>Creates a detached node with no shape and an enabled collision slot.</summary>
    public CollisionShape()
    {
        NotifyLocalTransformChanges = true;
        LocalTransformChanged += _ => _body?.MarkShapesDirty();
    }

    /// <summary>Gets or sets the borrowed shape resource, or null to remove collision geometry.</summary>
    /// <value>Null by default. The caller owns any assigned resource.</value>
    /// <exception cref="ObjectDisposedException">The assigned shape or node has been disposed.</exception>
    public Shape? Shape
    {
        get { ThrowIfDisposed(); return _shape; }
        set
        {
            EnsureMutable();
            if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
            if (ReferenceEquals(value, _shape)) return;
            DetachShapeEvents(_shape);
            _shape = value;
            AttachShapeEvents(value);
            _body?.MarkShapesDirty();
            UpdateConfigurationWarnings();
        }
    }

    /// <summary>Gets or sets whether this node contributes collision geometry.</summary>
    /// <value>False by default.</value>
    public bool Disabled
    {
        get { ThrowIfDisposed(); return _disabled; }
        set
        {
            EnsureMutable();
            if (_disabled == value) return;
            _disabled = value;
            _body?.MarkShapesDirty();
        }
    }

    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings().ToList();
        if (Parent is not PhysicsBody) warnings.Add("CollisionShape requires a direct PhysicsBody parent.");
        if (_shape is null || _shape.IsDisposed) warnings.Add("Assign a live collision shape resource.");
        return warnings.ToArray();
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(ShapeProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(CollisionShape)
        ? CreateCollisionShape : base.CreateSceneInstanceFactory();

    private static Node CreateCollisionShape() => new CollisionShape();

    /// <inheritdoc />
    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        if (Parent is not PhysicsBody body) return;
        body.AttachShape(this);
        _body = body;
    }

    /// <inheritdoc />
    protected override void OnExitTree()
    {
        if (_body is { } body) { _body = null; body.DetachShape(this); }
        base.OnExitTree();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_body is { } body) { _body = null; body.DetachShape(this); }
            DetachShapeEvents(_shape);
            _shape = null;
        }
        base.Dispose(disposing);
    }

    private void AttachShapeEvents(Shape? shape)
    {
        if (shape is null) return;
        shape.Changed += OnShapeChanged;
        shape.Disposed += OnShapeDisposed;
    }

    private void DetachShapeEvents(Shape? shape)
    {
        if (shape is null) return;
        shape.Changed -= OnShapeChanged;
        shape.Disposed -= OnShapeDisposed;
    }

    private void OnShapeChanged(Resource _) => _body?.MarkShapesDirty();

    private void OnShapeDisposed(ElectronObject _)
    {
        _body?.MarkShapesDirty();
    }
}
