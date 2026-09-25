namespace Electron2D;

/// <summary>Places a borrowed collision shape beneath a physics body or area.</summary>
/// <remarks>The direct parent must be a <see cref="CollisionObject"/> for this shape to participate in physics.
/// A shape resource remains caller-owned. Geometry changes are applied before the next physics step.</remarks>
public sealed class CollisionShape : Entity
{
    private static readonly PropertyDescriptor[] ShapeProperties =
    [
        new PropertyDescriptor<CollisionShape, Shape?>(nameof(Shape), node => node.Shape, (node, value) => node.Shape = value, _ => null, stored: true),
        new PropertyDescriptor<CollisionShape, bool>(nameof(Disabled), node => node.Disabled, (node, value) => node.Disabled = value, _ => false, stored: true),
        new PropertyDescriptor<CollisionShape, bool>(nameof(OneWayCollision), node => node.OneWayCollision, (node, value) => node.OneWayCollision = value, _ => false, stored: true),
        new PropertyDescriptor<CollisionShape, Vector2>(nameof(OneWayCollisionDirection), node => node.OneWayCollisionDirection,
            (node, value) => node.OneWayCollisionDirection = value, _ => Vector2.Down, stored: true)
    ];

    private Shape? _shape;
    private CollisionObject? _owner;
    private bool _disabled;
    private bool _oneWayCollision;
    private Vector2 _oneWayCollisionDirection = Vector2.Down;

    internal ulong GeometryRevision => _shape is { IsDisposed: false } shape ? shape.GeometryRevision : ulong.MaxValue;
    internal OneWayContactData? OneWayContact => _oneWayCollision
        ? new OneWayContactData(_oneWayCollisionDirection.Rotated(Rotation)) : null;

    /// <summary>Creates a detached node with no shape and an enabled collision slot.</summary>
    public CollisionShape()
    {
        NotifyLocalTransformChanges = true;
        LocalTransformChanged += _ => _owner?.MarkShapesDirty();
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
            _owner?.MarkShapesDirty();
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
            _owner?.MarkShapesDirty();
        }
    }

    /// <summary>Gets or sets whether a physics body collides only on the side opposite the configured direction.</summary>
    /// <value>False by default. This has no effect when the parent is an Area.</value>
    /// <remarks>The contact's side is selected when it first touches; it remains selected until separation.
    /// Changes take effect before the next fixed physics step.</remarks>
    public bool OneWayCollision
    {
        get { ThrowIfDisposed(); return _oneWayCollision; }
        set { EnsureMutable(); if (_oneWayCollision == value) return; _oneWayCollision = value; _owner?.MarkShapesDirty(); UpdateConfigurationWarnings(); }
    }

    /// <summary>Gets or sets the local direction through a one-way collision surface.</summary>
    /// <value>(0, 1) by default. Nonzero input is normalized.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 OneWayCollisionDirection
    {
        get { ThrowIfDisposed(); return _oneWayCollisionDirection; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value));
            var length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y);
            var direction = length == 0 ? Vector2.Zero : new Vector2((float)(value.X / length), (float)(value.Y / length));
            if (_oneWayCollisionDirection == direction) return;
            _oneWayCollisionDirection = direction;
            _owner?.MarkShapesDirty();
        }
    }

    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings().ToList();
        if (Parent is not CollisionObject) warnings.Add("CollisionShape requires a direct CollisionObject parent.");
        if (_shape is null || _shape.IsDisposed) warnings.Add("Assign a live collision shape resource.");
        if (_oneWayCollision && Parent is Area) warnings.Add("One-way collision has no effect on an Area sensor.");
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
        if (Parent is not CollisionObject owner) return;
        owner.AttachShape(this);
        _owner = owner;
    }

    /// <inheritdoc />
    protected override void OnExitTree()
    {
        if (_owner is { } owner) { _owner = null; owner.DetachShape(this); }
        base.OnExitTree();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_owner is { } owner) { _owner = null; owner.DetachShape(this); }
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

    private void OnShapeChanged(Resource _) => _owner?.MarkShapesDirty();

    private void OnShapeDisposed(ElectronObject _)
    {
        _owner?.MarkShapesDirty();
    }
}

internal sealed record OneWayContactData(Vector2 LocalDirection);
