using Box2D.NET;
using static Box2D.NET.B2Constants;
using static Box2D.NET.B2Distances;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

/// <summary>Defines reusable two-dimensional collision geometry.</summary>
/// <remarks>A <see cref="CollisionShape"/> borrows caller-owned geometry. Geometry obtained from a server-backed
/// scene owner slot is borrowed from that server RID and cannot be disposed separately; data replacement retires that view.</remarks>
public abstract partial class Shape : Resource
{
    private ulong _revision;
    private readonly object _queryRIDGate = new();
    private RID _queryRID;
    private bool _serverOwned;
    private int _serverReleaseThread;

    /// <summary>Gets the local bounding rectangle of the shape.</summary>
    /// <returns>The local-axis bounds, including drawing padding for a separation ray; a shape need not be centered on its origin.</returns>
    public abstract Rect2 GetRect();

    internal abstract void AppendToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation,
        in B2ShapeDef definition, List<B2ShapeId> fixtures);
    internal abstract void AppendQueryProxies(List<B2ShapeProxy> proxies);

    internal ulong GeometryRevision => _revision;

    /// <summary>Returns the stable physics identity of this geometry.</summary>
    /// <returns>A borrowed shape RID retained across edits until resource disposal; server-owned views share their owning RID.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public override RID GetRID()
    {
        lock (_queryRIDGate)
        {
            ThrowIfDisposed();
            return _queryRID.IsValid() ? _queryRID :
                _queryRID = PhysicsServer.Service.RegisterBorrowedShape(this);
        }
    }

    internal void BindServerOwnedRID(RID rid)
    {
        lock (_queryRIDGate) { _queryRID = rid; _serverOwned = true; }
    }

    internal void ReleaseServerGeometry()
    {
        lock (_queryRIDGate) _serverReleaseThread = Environment.CurrentManagedThreadId;
        try { Dispose(); }
        finally { lock (_queryRIDGate) _serverReleaseThread = 0; }
    }

    /// <summary>Preserves the lifetime of geometry borrowed from a caller-owned server shape.</summary>
    /// <exception cref="InvalidOperationException">The server RID owns this geometry; release that RID instead.</exception>
    protected override void ValidateDisposal()
    {
        lock (_queryRIDGate)
            if (_serverOwned && _serverReleaseThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("This geometry is borrowed from a server shape RID.");
        base.ValidateDisposal();
    }

    internal void EmitGeometryChanged()
    {
        _revision++;
        RID rid;
        lock (_queryRIDGate) rid = _queryRID;
        if (rid.IsValid()) PhysicsServer.Service.MarkBorrowedShapeDirty(rid);
        EmitChanged();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RID rid;
            lock (_queryRIDGate) { rid = _queryRID; _queryRID = default; }
            if (rid.IsValid() && !_serverOwned) PhysicsServer.Service.UnregisterBorrowedShape(rid);
        }
        base.Dispose(disposing);
    }

    internal static B2Vec2 ToBackend(Vector2 value) => new(value.X * PhysicsSpace.MetersPerUnit, value.Y * PhysicsSpace.MetersPerUnit);

    internal static B2ShapeId CreateSegmentOrPoint(B2BodyId bodyID, in B2ShapeDef definition, B2Vec2 pointA, B2Vec2 pointB)
    {
        if (B2MathFunction.b2DistanceSquared(pointA, pointB) <= B2_LINEAR_SLOP * B2_LINEAR_SLOP)
        {
            var center = new B2Vec2(pointA.X + (pointB.X - pointA.X) * 0.5f,
                pointA.Y + (pointB.Y - pointA.Y) * 0.5f);
            return b2CreateCircleShape(bodyID, definition, new B2Circle { center = center, radius = 0 });
        }
        return b2CreateSegmentShape(bodyID, definition, new B2Segment(pointA, pointB));
    }
}

/// <summary>A circular collision shape with a configurable radius.</summary>
public sealed class CircleShape : Shape
{
    private float _radius = 10f;

    /// <summary>Creates a circle with the default ten-unit radius.</summary>
    public CircleShape() { }

    /// <summary>Gets or sets the positive finite radius in scene units.</summary>
    /// <value>Ten by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not positive and finite, or its diameter would overflow.</exception>
    public float Radius
    {
        get { ThrowIfDisposed(); return _radius; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value <= 0 || value > float.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(value));
            _radius = value;
            EmitGeometryChanged();
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<CircleShape, float>(nameof(Radius), s => s.Radius, (s, v) => s.Radius = v, _ => 10f, stored: true);
    }

    /// <inheritdoc />
    public override Rect2 GetRect() { ThrowIfDisposed(); return new(-_radius, -_radius, 2 * _radius, 2 * _radius); }

    internal override void AppendToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation,
        in B2ShapeDef definition, List<B2ShapeId> fixtures)
    {
        ThrowIfDisposed();
        var circle = new B2Circle { center = ToBackend(localPosition), radius = _radius * PhysicsSpace.MetersPerUnit };
        fixtures.Add(b2CreateCircleShape(bodyID, definition, circle));
    }

    internal override void AppendQueryProxies(List<B2ShapeProxy> proxies)
    {
        ThrowIfDisposed();
        proxies.Add(b2MakeProxy(new B2Vec2(0, 0), 1, _radius * PhysicsSpace.MetersPerUnit));
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CircleShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((CircleShape)target)._radius = _radius;
    }
}

/// <summary>A vertical capsule collision shape with linked radius and full height.</summary>
/// <remarks>A direct <see cref="CollisionShape"/> borrows this resource. Center segments within the physics
/// backend's linear tolerance use a circular fixture while public dimensions and bounds remain exact.</remarks>
public sealed class CapsuleShape : Shape
{
    private float _radius = 10f;
    private float _height = 30f;

    /// <summary>Creates a capsule with ten-unit radius and thirty-unit full height.</summary>
    public CapsuleShape() { }

    /// <summary>Gets or sets the nonnegative finite end-cap radius in scene units.</summary>
    /// <value>Ten by default; increasing it beyond half the height also increases the height.</value>
    /// <exception cref="ArgumentOutOfRangeException">The radius is negative, nonfinite, or its diameter overflows.</exception>
    public float Radius
    {
        get { ThrowIfDisposed(); return _radius; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value < 0 || value > float.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_radius == value) return;
            _radius = value;
            _height = MathF.Max(_height, 2 * value);
            EmitGeometryChanged();
        }
    }

    /// <summary>Gets or sets the nonnegative finite full height, including both end caps.</summary>
    /// <value>Thirty by default; decreasing it below twice the radius also decreases the radius.</value>
    /// <exception cref="ArgumentOutOfRangeException">The height is negative or nonfinite.</exception>
    public float Height
    {
        get { ThrowIfDisposed(); return _height; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (_height == value) return;
            _height = value;
            _radius = MathF.Min(_radius, value * 0.5f);
            EmitGeometryChanged();
        }
    }

    /// <summary>Gets or sets the center segment height between the two end-cap centers.</summary>
    /// <value>Ten by default; this is <see cref="Height"/> minus twice <see cref="Radius"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The height is negative, nonfinite, or makes the full height overflow.</exception>
    public float MidHeight
    {
        get { ThrowIfDisposed(); return _height - 2 * _radius; }
        set
        {
            ThrowIfDisposed();
            var height = value + 2 * _radius;
            if (!float.IsFinite(value) || value < 0 || !float.IsFinite(height))
                throw new ArgumentOutOfRangeException(nameof(value));
            _height = height;
            EmitGeometryChanged();
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<CapsuleShape, float>(nameof(Radius), s => s.Radius, (s, v) => s.Radius = v, _ => 10f, stored: true);
        yield return new PropertyDescriptor<CapsuleShape, float>(nameof(Height), s => s.Height, (s, v) => s.Height = v, _ => 20f, stored: true);
    }

    /// <inheritdoc />
    public override Rect2 GetRect()
    {
        ThrowIfDisposed();
        return new(-_radius, -_height * 0.5f, 2 * _radius, _height);
    }

    internal override void AppendToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation,
        in B2ShapeDef definition, List<B2ShapeId> fixtures)
    {
        ThrowIfDisposed();
        var midHeight = (_height - 2 * _radius) * PhysicsSpace.MetersPerUnit;
        var circle = new B2Circle { center = ToBackend(localPosition), radius = _radius * PhysicsSpace.MetersPerUnit };
        if (midHeight <= B2_LINEAR_SLOP)
        {
            fixtures.Add(b2CreateCircleShape(bodyID, definition, circle));
            return;
        }
        var offset = new Vector2(0, (_height - 2 * _radius) * 0.5f).Rotated(localRotation);
        var capsule = new B2Capsule(ToBackend(localPosition - offset), ToBackend(localPosition + offset),
            _radius * PhysicsSpace.MetersPerUnit);
        var id = b2CreateCapsuleShape(bodyID, definition, capsule);
        fixtures.Add(id.index1 == 0 ? b2CreateCircleShape(bodyID, definition, circle) : id);
    }

    internal override void AppendQueryProxies(List<B2ShapeProxy> proxies)
    {
        ThrowIfDisposed();
        var midHeight = (_height - 2 * _radius) * PhysicsSpace.MetersPerUnit;
        var radius = _radius * PhysicsSpace.MetersPerUnit;
        proxies.Add(midHeight <= B2_LINEAR_SLOP
            ? b2MakeProxy(new B2Vec2(0, 0), 1, radius)
            : b2MakeProxy(new B2Vec2(0, -midHeight * 0.5f),
                new B2Vec2(0, midHeight * 0.5f), 2, radius));
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CapsuleShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((CapsuleShape)target)._radius = _radius;
        ((CapsuleShape)target)._height = _height;
    }
}

/// <summary>A two-sided line-segment collision shape between two local points.</summary>
/// <remarks>A segment shorter than the backend's linear tolerance uses a point fixture while retaining its exact endpoints and bounds.</remarks>
public sealed class SegmentShape : Shape
{
    private Vector2 _a;
    private Vector2 _b = new(0, 10);

    /// <summary>Creates a segment from the origin to (0, 10) in scene units.</summary>
    public SegmentShape() { }

    /// <summary>Gets or sets the first finite local endpoint.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The endpoint is nonfinite or produces unrepresentable bounds.</exception>
    public Vector2 A
    {
        get { ThrowIfDisposed(); return _a; }
        set
        {
            ThrowIfDisposed();
            ValidateEndpoint(value, _b);
            if (_a == value) return;
            _a = value;
            EmitGeometryChanged();
        }
    }

    /// <summary>Gets or sets the second finite local endpoint.</summary>
    /// <value>(0, 10) by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The endpoint is nonfinite or produces unrepresentable bounds.</exception>
    public Vector2 B
    {
        get { ThrowIfDisposed(); return _b; }
        set
        {
            ThrowIfDisposed();
            ValidateEndpoint(value, _a);
            if (_b == value) return;
            _b = value;
            EmitGeometryChanged();
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<SegmentShape, Vector2>(nameof(A), s => s.A, (s, v) => s.A = v, _ => new(0, -10), stored: true);
        yield return new PropertyDescriptor<SegmentShape, Vector2>(nameof(B), s => s.B, (s, v) => s.B = v, _ => new(0, 10), stored: true);
    }

    /// <inheritdoc />
    public override Rect2 GetRect()
    {
        ThrowIfDisposed();
        return new(MathF.Min(_a.X, _b.X), MathF.Min(_a.Y, _b.Y),
            MathF.Abs(_b.X - _a.X), MathF.Abs(_b.Y - _a.Y));
    }

    internal override void AppendToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation,
        in B2ShapeDef definition, List<B2ShapeId> fixtures)
    {
        ThrowIfDisposed();
        var pointA = ToBackend(localPosition + _a.Rotated(localRotation));
        var pointB = ToBackend(localPosition + _b.Rotated(localRotation));
        fixtures.Add(CreateSegmentOrPoint(bodyID, definition, pointA, pointB));
    }

    internal override void AppendQueryProxies(List<B2ShapeProxy> proxies)
    {
        ThrowIfDisposed();
        var first = ToBackend(_a);
        var second = ToBackend(_b);
        proxies.Add(B2MathFunction.b2DistanceSquared(first, second) <= B2_LINEAR_SLOP * B2_LINEAR_SLOP
            ? b2MakeProxy(new B2Vec2(first.X + (second.X - first.X) * 0.5f,
                    first.Y + (second.Y - first.Y) * 0.5f), 1, 0)
            : b2MakeProxy(first, second, 2, 0));
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SegmentShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((SegmentShape)target)._a = _a;
        ((SegmentShape)target)._b = _b;
    }

    private static void ValidateEndpoint(Vector2 value, Vector2 other)
    {
        if (!value.IsFinite() || !(value - other).IsFinite())
            throw new ArgumentOutOfRangeException(nameof(value));
    }
}

/// <summary>A rectangular collision shape centered at its origin.</summary>
public sealed class RectangleShape : Shape
{
    private Vector2 _size = new(20, 20);

    /// <summary>Creates a rectangle with the default twenty-unit dimensions.</summary>
    public RectangleShape() { }

    /// <summary>Gets or sets the finite positive width and height in scene units.</summary>
    /// <value>(20, 20) by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">Either component is not positive and finite.</exception>
    public Vector2 Size
    {
        get { ThrowIfDisposed(); return _size; }
        set
        {
            ThrowIfDisposed();
            if (!value.IsFinite() || value.X <= 0 || value.Y <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            _size = value;
            EmitGeometryChanged();
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<RectangleShape, Vector2>(nameof(Size), s => s.Size, (s, v) => s.Size = v, _ => new(20, 20), stored: true);
    }

    /// <inheritdoc />
    public override Rect2 GetRect() { ThrowIfDisposed(); return new(-_size.X / 2, -_size.Y / 2, _size.X, _size.Y); }

    internal override void AppendToBody(B2BodyId bodyID, Vector2 localPosition, float localRotation,
        in B2ShapeDef definition, List<B2ShapeId> fixtures)
    {
        ThrowIfDisposed();
        var polygon = b2MakeOffsetBox(_size.X * PhysicsSpace.MetersPerUnit / 2,
            _size.Y * PhysicsSpace.MetersPerUnit / 2, ToBackend(localPosition), B2MathFunction.b2MakeRot(localRotation));
        fixtures.Add(b2CreatePolygonShape(bodyID, definition, polygon));
    }

    internal override void AppendQueryProxies(List<B2ShapeProxy> proxies)
    {
        ThrowIfDisposed();
        var halfWidth = _size.X * PhysicsSpace.MetersPerUnit * 0.5f;
        var halfHeight = _size.Y * PhysicsSpace.MetersPerUnit * 0.5f;
        Span<B2Vec2> points = stackalloc B2Vec2[4]
        {
            new(-halfWidth, -halfHeight), new(halfWidth, -halfHeight),
            new(halfWidth, halfHeight), new(-halfWidth, halfHeight)
        };
        proxies.Add(b2MakeProxy(points, 4, 0));
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new RectangleShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((RectangleShape)target)._size = _size;
    }
}
