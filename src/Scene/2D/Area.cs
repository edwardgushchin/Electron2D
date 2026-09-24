using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;

namespace Electron2D;

/// <summary>A two-dimensional region that reports physics bodies and other areas crossing its shapes.</summary>
/// <remarks>Attach direct <see cref="CollisionShape"/> children and add the area to a scene tree.
/// Overlap snapshots update after each nonzero fixed physics step; changing a transform does not update a snapshot immediately.</remarks>
public sealed partial class Area : CollisionObject
{
    private static readonly PropertyDescriptor[] AreaProperties =
    [
        new PropertyDescriptor<Area, bool>(nameof(Monitoring), area => area.Monitoring,
            (area, value) => area.Monitoring = value, _ => true, stored: true),
        new PropertyDescriptor<Area, bool>(nameof(Monitorable), area => area.Monitorable,
            (area, value) => area.Monitorable = value, _ => true, stored: true)
    ];

    private readonly List<CollisionShape> _shapes = [];
    private readonly List<B2ShapeId> _backendShapes = [];
    private readonly List<ulong> _appliedShapeRevisions = [];
    private HashSet<CollisionObject> _overlaps = new(ReferenceEqualityComparer.Instance);
    private HashSet<CollisionObject> _nextOverlaps = new(ReferenceEqualityComparer.Instance);
    private PhysicsSpace? _space;
    private B2BodyId _bodyID;
    private Vector2 _lastPosition;
    private float _lastRotation;
    private volatile bool _shapesDirty = true;
    private bool _monitoring = true;
    private bool _monitorable = true;

    /// <summary>Creates a detached area that monitors bodies and is monitorable by other areas.</summary>
    public Area() { }

    /// <summary>Gets or sets whether this area detects overlapping bodies and areas.</summary>
    /// <value>True by default. Disabling takes effect on the next physics step.</value>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool Monitoring
    {
        get { ThrowIfDisposed(); return _monitoring; }
        set { EnsureMutable(); _monitoring = value; }
    }

    /// <summary>Gets or sets whether other monitoring areas can detect this area.</summary>
    /// <value>True by default. Disabling takes effect on their next physics step.</value>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool Monitorable
    {
        get { ThrowIfDisposed(); return _monitorable; }
        set { EnsureMutable(); _monitorable = value; }
    }

    /// <summary>Occurs after a fixed step when another area begins overlapping this area.</summary>
    public event Action<Area>? AreaEntered;

    /// <summary>Occurs after a fixed step when another area stops overlapping this area.</summary>
    public event Action<Area>? AreaExited;

    /// <summary>Occurs after a fixed step when a physics body begins overlapping this area.</summary>
    public event Action<Entity>? BodyEntered;

    /// <summary>Occurs after a fixed step when a physics body stops overlapping this area.</summary>
    public event Action<Entity>? BodyExited;

    /// <summary>Returns the current area-overlap snapshot.</summary>
    /// <returns>A caller-owned array updated from the last nonzero fixed step.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public Area[] GetOverlappingAreas()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        var result = new List<Area>();
        foreach (var other in _overlaps)
            if (other is Area area) result.Add(area);
        return result.ToArray();
    }

    /// <summary>Returns the current body-overlap snapshot.</summary>
    /// <returns>A caller-owned array of spatial bodies updated from the last nonzero fixed step.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public Entity[] GetOverlappingBodies()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        var result = new List<Entity>();
        foreach (var other in _overlaps)
            if (other is PhysicsBody body) result.Add(body);
        return result.ToArray();
    }

    /// <summary>Tests whether the last fixed step found any overlapping areas.</summary>
    /// <returns>True when at least one monitorable area is in the snapshot.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool HasOverlappingAreas()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        foreach (var other in _overlaps)
            if (other is Area) return true;
        return false;
    }

    /// <summary>Tests whether the last fixed step found any overlapping bodies.</summary>
    /// <returns>True when at least one physics body is in the snapshot.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool HasOverlappingBodies()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        foreach (var other in _overlaps)
            if (other is PhysicsBody) return true;
        return false;
    }

    /// <summary>Tests whether the given scene node is in the current area-overlap snapshot.</summary>
    /// <param name="area">The area node to test.</param>
    /// <returns>False for null, foreign, detached or nonarea nodes.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool OverlapsArea(Node? area)
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return area is Area other && _overlaps.Contains(other);
    }

    /// <summary>Tests whether the given scene node is in the current body-overlap snapshot.</summary>
    /// <param name="body">The body node to test.</param>
    /// <returns>False for null, foreign, detached or nonbody nodes.</returns>
    /// <exception cref="InvalidOperationException">An attached area is read off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool OverlapsBody(Node? body)
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return body is PhysicsBody other && _overlaps.Contains(other);
    }

    internal override IReadOnlyList<B2ShapeId> BackendShapes => _backendShapes;

    internal override void AttachShape(CollisionShape shape)
    {
        if (_shapes.Contains(shape)) return;
        _shapes.Add(shape);
        MarkShapesDirty();
    }

    internal override void DetachShape(CollisionShape shape)
    {
        if (_shapes.Remove(shape)) MarkShapesDirty();
    }

    internal override void MarkShapesDirty() => _shapesDirty = true;
    internal override void OnCollisionFilterChanged() => MarkShapesDirty();

    internal void AttachBackend(PhysicsSpace space)
    {
        if (_space is not null) throw new InvalidOperationException("An area already belongs to a physics world.");
        ValidatePhysicsTransform();
        var definition = b2DefaultBodyDef();
        definition.type = B2BodyType.b2_staticBody;
        _lastPosition = GlobalPosition;
        _lastRotation = GlobalRotation;
        definition.position = Shape.ToBackend(_lastPosition);
        definition.rotation = b2MakeRot(_lastRotation);
        _bodyID = b2CreateBody(space.WorldID, definition);
        _space = space;
        _shapesDirty = true;
        try { RebuildShapes(); }
        catch { DetachBackend(); throw; }
    }

    internal void DetachBackend()
    {
        if (_space is null) return;
        b2DestroyBody(_bodyID);
        _backendShapes.Clear();
        _appliedShapeRevisions.Clear();
        _space = null;
        _shapesDirty = true;
    }

    internal void PrepareBackend()
    {
        if (_space is null) return;
        ValidatePhysicsTransform();
        if (!_shapesDirty)
            for (var index = 0; index < _shapes.Count; index++)
                if (_shapes[index].GeometryRevision != _appliedShapeRevisions[index]) { MarkShapesDirty(); break; }
        if (_shapesDirty) RebuildShapes();
        var position = GlobalPosition;
        var rotation = GlobalRotation;
        if (position != _lastPosition || rotation != _lastRotation)
        {
            b2Body_SetTransform(_bodyID, Shape.ToBackend(position), b2MakeRot(rotation));
            _lastPosition = position;
            _lastRotation = rotation;
        }
    }

    internal void BeginOverlapScan() => _nextOverlaps.Clear();

    internal void Observe(CollisionObject other) => _nextOverlaps.Add(other);

    internal void CommitOverlapScan(List<PhysicsSpace.OverlapEvent> events)
    {
        foreach (var other in _overlaps)
            if (!_nextOverlaps.Contains(other)) events.Add(new(this, other, false));
        foreach (var other in _nextOverlaps)
            if (!_overlaps.Contains(other)) events.Add(new(this, other, true));
        (_overlaps, _nextOverlaps) = (_nextOverlaps, _overlaps);
    }

    internal void Forget(CollisionObject other, List<PhysicsSpace.OverlapEvent> events)
    {
        _nextOverlaps.Remove(other);
        if (_overlaps.Remove(other)) events.Add(new(this, other, false));
    }

    internal void ClearOverlaps()
    {
        _overlaps.Clear();
        _nextOverlaps.Clear();
    }

    internal bool ContainsOverlap(CollisionObject other) => _overlaps.Contains(other);

    internal void RaiseOverlap(CollisionObject other, bool entered)
    {
        if (other is Area area)
        {
            if (entered) AreaEntered?.Invoke(area);
            else AreaExited?.Invoke(area);
        }
        else if (other is PhysicsBody body)
        {
            if (entered) BodyEntered?.Invoke(body);
            else BodyExited?.Invoke(body);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(AreaProperties).Concat(FieldProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Area)
        ? CreateArea : base.CreateSceneInstanceFactory();

    private static Node CreateArea() => new Area();

    /// <inheritdoc />
    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        Tree?.RegisterPhysicsArea(this);
    }

    /// <inheritdoc />
    protected override void OnExitTree()
    {
        try { Tree?.UnregisterPhysicsArea(this); }
        finally { base.OnExitTree(); }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _space?.Remove(this);
            _shapes.Clear();
            ClearOverlaps();
        }
        base.Dispose(disposing);
    }

    private void RebuildShapes()
    {
        foreach (var node in _shapes)
        {
            if (node.Disabled || node.Shape is null || node.Shape.IsDisposed) continue;
            if (!node.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(node.Skew))
                throw new InvalidOperationException("Physics shapes require unit scale and zero skew.");
        }

        foreach (var id in _backendShapes) b2DestroyShape(id, updateBodyMass: false);
        _backendShapes.Clear();
        var definition = b2DefaultShapeDef();
        definition.filter.categoryBits = CollisionLayer;
        definition.filter.maskBits = CollisionMask;
        definition.density = 0;
        definition.isSensor = true;
        foreach (var node in _shapes)
        {
            if (node.Disabled || node.Shape is not { IsDisposed: false } shape) continue;
            shape.AppendToBody(_bodyID, node.Position, node.Rotation, definition, _backendShapes);
        }
        _appliedShapeRevisions.Clear();
        foreach (var node in _shapes) _appliedShapeRevisions.Add(node.GeometryRevision);
        _shapesDirty = false;
    }

    private void ValidatePhysicsTransform()
    {
        var transform = GlobalTransform;
        if (!transform.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(transform.Skew))
            throw new InvalidOperationException("Physics areas require unit global scale and zero skew.");
    }
}
