using Box2D.NET;

namespace Electron2D;

/// <summary>Selects solid interior or closed hollow edges for a collision polygon.</summary>
public enum CollisionPolygonBuildMode
{
    /// <summary>Decomposes the contour into solid convex fixtures.</summary>
    Solids = 0,
    /// <summary>Builds only the closed perimeter as paired segment fixtures.</summary>
    Segments = 1
}

/// <summary>Places an editable solid or hollow polygon directly under a physics body or area.</summary>
/// <remarks>The node owns its generated shape resources. Its polygon coordinates are local to this node,
/// and geometry changes reach a direct collision owner before the next fixed physics step.</remarks>
public sealed class CollisionPolygon : Entity, ICollisionGeometry
{
    private static readonly PropertyDescriptor[] PolygonProperties =
    [
        new PropertyDescriptor<CollisionPolygon, CollisionPolygonBuildMode>(nameof(BuildMode), node => node.BuildMode,
            (node, value) => node.BuildMode = value, _ => CollisionPolygonBuildMode.Solids, stored: true),
        new PropertyDescriptor<CollisionPolygon, Vector2[]>(nameof(Polygon), node => node.Polygon,
            (node, value) => node.Polygon = value, _ => [], stored: true),
        new PropertyDescriptor<CollisionPolygon, bool>(nameof(Disabled), node => node.Disabled,
            (node, value) => node.Disabled = value, _ => false, stored: true),
        new PropertyDescriptor<CollisionPolygon, bool>(nameof(OneWayCollision), node => node.OneWayCollision,
            (node, value) => node.OneWayCollision = value, _ => false, stored: true),
        new PropertyDescriptor<CollisionPolygon, Vector2>(nameof(OneWayCollisionDirection), node => node.OneWayCollisionDirection,
            (node, value) => node.OneWayCollisionDirection = value, _ => Vector2.Down, stored: true)
    ];

    private Vector2[] _polygon = [];
    private Shape[] _generatedShapes = [];
    private CollisionObject? _owner;
    private CollisionPolygonBuildMode _buildMode;
    private bool _disabled;
    private bool _oneWayCollision;
    private Vector2 _oneWayCollisionDirection = Vector2.Down;
    private ulong _geometryRevision;

    Entity ICollisionGeometry.Node => this;
    bool ICollisionGeometry.IsActive => !_disabled && _generatedShapes.Length != 0;
    ulong ICollisionGeometry.GeometryRevision => _geometryRevision;
    OneWayContactData? ICollisionGeometry.OneWayContact => _oneWayCollision
        ? new OneWayContactData(_oneWayCollisionDirection.Rotated(Rotation)) : null;

    void ICollisionGeometry.AppendToBody(B2BodyId bodyID, in B2ShapeDef definition, List<B2ShapeId> fixtures)
    {
        foreach (var shape in _generatedShapes)
            shape.AppendToBody(bodyID, Position, Rotation, definition, fixtures);
    }

    /// <summary>Creates an empty solid polygon with no collision fixtures.</summary>
    public CollisionPolygon()
    {
        NotifyLocalTransformChanges = true;
        LocalTransformChanged += _ => _owner?.MarkShapesDirty();
    }

    /// <summary>Gets or sets whether the contour creates solid or hollow collision geometry.</summary>
    /// <value><see cref="CollisionPolygonBuildMode.Solids"/> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    public CollisionPolygonBuildMode BuildMode
    {
        get { ThrowIfDisposed(); return _buildMode; }
        set
        {
            EnsureMutable();
            if (value is not (CollisionPolygonBuildMode.Solids or CollisionPolygonBuildMode.Segments))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_buildMode == value) return;
            var generated = BuildShapes(_polygon, value);
            ReplaceShapes(generated);
            _buildMode = value;
            UpdateConfigurationWarnings();
        }
    }

    /// <summary>Gets or sets the caller-owned closed polygon contour in local coordinates.</summary>
    /// <value>An empty array by default; reads and writes are copied.</value>
    /// <remarks>Consecutive vertices form edges and the final vertex connects to the first.
    /// Invalid topology remains stored but contributes no solid fixtures; fewer than two points contribute no segments.</remarks>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    /// <exception cref="ArgumentException">A coordinate or contour span is nonfinite.</exception>
    public Vector2[] Polygon
    {
        get { ThrowIfDisposed(); return (Vector2[])_polygon.Clone(); }
        set
        {
            EnsureMutable();
            ArgumentNullException.ThrowIfNull(value);
            var copy = (Vector2[])value.Clone();
            ValidatePolygon(copy);
            var generated = BuildShapes(copy, _buildMode);
            ReplaceShapes(generated);
            _polygon = copy;
            UpdateConfigurationWarnings();
        }
    }

    /// <summary>Gets or sets whether the polygon contributes fixtures to its collision owner.</summary>
    /// <value>False by default.</value>
    public bool Disabled
    {
        get { ThrowIfDisposed(); return _disabled; }
        set { EnsureMutable(); if (_disabled == value) return; _disabled = value; _owner?.MarkShapesDirty(); }
    }

    /// <summary>Gets or sets whether body contacts are accepted only from the configured side.</summary>
    /// <value>False by default. Area sensors ignore this setting.</value>
    public bool OneWayCollision
    {
        get { ThrowIfDisposed(); return _oneWayCollision; }
        set { EnsureMutable(); if (_oneWayCollision == value) return; _oneWayCollision = value; _owner?.MarkShapesDirty(); UpdateConfigurationWarnings(); }
    }

    /// <summary>Gets or sets the local pass-through direction for one-way body contacts.</summary>
    /// <value>(0, 1) by default; finite nonzero input is normalized.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 OneWayCollisionDirection
    {
        get { ThrowIfDisposed(); return _oneWayCollisionDirection; }
        set
        {
            EnsureMutable();
            var direction = CollisionShape.NormalizeOneWayDirection(value);
            if (_oneWayCollisionDirection == direction) return;
            _oneWayCollisionDirection = direction;
            _owner?.MarkShapesDirty();
        }
    }

    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings().ToList();
        if (Parent is not CollisionObject) warnings.Add("CollisionPolygon requires a direct CollisionObject parent.");
        if (_polygon.Length == 0) warnings.Add("An empty polygon has no collision geometry.");
        else if (_buildMode == CollisionPolygonBuildMode.Solids && _polygon.Length < 3)
            warnings.Add("A solid polygon needs at least three vertices.");
        else if (_buildMode == CollisionPolygonBuildMode.Segments && _polygon.Length < 2)
            warnings.Add("A segment polygon needs at least two vertices.");
        else if (_generatedShapes.Length == 0) warnings.Add("The polygon cannot produce valid collision geometry.");
        if (_oneWayCollision && Parent is Area) warnings.Add("One-way collision has no effect on an Area sensor.");
        return warnings.ToArray();
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(PolygonProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateCollisionPolygon;

    private static Node CreateCollisionPolygon() => new CollisionPolygon();

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
            DisposeShapes(_generatedShapes);
            _generatedShapes = [];
        }
        base.Dispose(disposing);
    }

    private void ReplaceShapes(Shape[] shapes)
    {
        var old = _generatedShapes;
        _generatedShapes = shapes;
        _geometryRevision++;
        _owner?.MarkShapesDirty();
        DisposeShapes(old);
    }

    private static void DisposeShapes(Shape[] shapes)
    {
        foreach (var shape in shapes) shape.Dispose();
    }

    private static void ValidatePolygon(Vector2[] points)
    {
        if (points.Length == 0) return;
        var min = points[0];
        var max = min;
        foreach (var point in points)
        {
            if (!point.IsFinite()) throw new ArgumentException("Polygon vertices must be finite.", nameof(points));
            min = new(MathF.Min(min.X, point.X), MathF.Min(min.Y, point.Y));
            max = new(MathF.Max(max.X, point.X), MathF.Max(max.Y, point.Y));
        }
        if (!(max - min).IsFinite()) throw new ArgumentException("Polygon bounds exceed the finite range.", nameof(points));
    }

    private static Shape[] BuildShapes(Vector2[] points, CollisionPolygonBuildMode mode)
    {
        if (mode == CollisionPolygonBuildMode.Segments)
        {
            if (points.Length < 2) return [];
            var segments = new Vector2[checked(points.Length * 2)];
            for (var index = 0; index < points.Length; index++)
            {
                segments[index * 2] = points[index];
                segments[index * 2 + 1] = points[(index + 1) % points.Length];
            }
            var shape = new ConcavePolygonShape();
            try { shape.Segments = segments; return [shape]; }
            catch { shape.Dispose(); throw; }
        }

        if (points.Length < 3) return [];
        var parts = Geometry.DecomposePolygonInConvex(points);
        var generated = new List<Shape>(parts.Length);
        try
        {
            foreach (var part in parts)
            {
                var shape = new ConvexPolygonShape();
                generated.Add(shape);
                shape.Points = part;
            }
            return generated.ToArray();
        }
        catch (ArgumentException)
        {
            foreach (var shape in generated) shape.Dispose();
            return [];
        }
        catch
        {
            foreach (var shape in generated) shape.Dispose();
            throw;
        }
    }
}
