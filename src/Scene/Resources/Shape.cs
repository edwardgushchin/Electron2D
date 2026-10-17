namespace Electron2D;

/// <summary>Defines reusable two-dimensional collision geometry.</summary>
/// <remarks>A <see cref="CollisionShape"/> borrows caller-owned geometry. Geometry obtained from a server-backed
/// scene owner slot is borrowed from that server RID and cannot be disposed separately; data replacement retires that view.
/// During a resource or selected shape-pair query, both operands reject geometry/policy mutation, inherited reset/copy and disposal.
/// Server data reads, replacements and owned RID release also freeze the resource's collision consumers until their guarded operation ends.
/// Ordinary Shape constructors supply usable geometry. A private view obtained from an unconfigured server RID
/// retains template fields but cannot participate in collision queries or simulation until ShapeSetData succeeds.</remarks>
public abstract partial class Shape : Resource
{
    private float _customSolverBias;
    private bool _checkingAuthoringAccess;
    private readonly List<CollisionObject> _policySceneUsers = [];
    private readonly List<PhysicsServerCollider> _policyServerUsers = [];
    private static long _solverPolicyEpoch;
    internal static long SolverPolicyEpoch => Volatile.Read(ref _solverPolicyEpoch);

    /// <summary>Gets or sets this shape's contact correction fraction, or zero to inherit the world's default.</summary>
    /// <value>A finite fraction from zero to one; zero by default.</value>
    /// <remarks>One nonzero shape bias overrides the world; two nonzero biases are averaged.
    /// The fraction is distributed over substeps within the nominal physics tick. It affects positional correction,
    /// not restitution or query margins. Edits preserve geometry and are observed before the next query or step,
    /// waking affected bodies. All attached scene/raw owners are checked before mutation, including equal writes.
    /// Accepted equal writes are silent. This borrowed resource must remain live and unchanged during a step.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or outside [0,1].</exception>
    /// <exception cref="InvalidOperationException">An attached owner rejects authoring, or this resource's geometry or policy access hook is borrowed.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public float CustomSolverBias
    {
        get { ThrowIfDisposed(); return _customSolverBias; }
        set
        {
            ThrowIfDisposed(); if (!PhysicsContactSettings.ValidBias(value)) throw new ArgumentOutOfRangeException(nameof(value));
            EnsureAuthoringAccess(observeBackend: true, resourceGeometry: false);
            lock (_geometryGate)
            {
                ThrowIfDisposed();
                if (_customSolverBias == value) return;
                _customSolverBias = value; Interlocked.Increment(ref _solverPolicyEpoch);
            }
            EmitChanged();
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<Shape, float>(nameof(CustomSolverBias), s => s.CustomSolverBias, (s, v) => s.CustomSolverBias = v, _ => 0f, stored: true);
    }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
        ((Shape)target).CustomSolverBias = _customSolverBias;

    private ulong _revision;
    private static long _geometryEpoch;
    internal static long GeometryEpoch => Volatile.Read(ref _geometryEpoch);
    private readonly object _queryRIDGate = new();
    private RID _queryRID;
    private bool _serverOwned;
    internal bool HasServerData { get; private set; } = true;
    private int _serverReleaseThread;

    /// <summary>Gets the local bounding rectangle of the shape.</summary>
    /// <returns>The local-axis bounds, including drawing padding for a separation ray; a shape need not be centered on its origin.</returns>
    public virtual Rect2 GetRect() => GeometryData.Bounds;

    /// <summary>Supplies immutable exact collision geometry for this resource.</summary>
    /// <returns>Validated geometry in local scene units; it is retained until EmitGeometryChanged.</returns>
    /// <remarks>This hook must not mutate or dispose the resource or recursively request its geometry.
    /// It runs on first geometry demand after an edit. The runtime uses the same data for physics,
    /// bounds and diagnostic drawing. Unsupported collision algorithms require a solver implementation.</remarks>
    protected abstract ShapeGeometry GetGeometryCore();

    /// <summary>Gets immutable exact geometry for the current authored resource revision.</summary>
    /// <returns>The cached geometry data; retained data remains valid independently of later edits or resource disposal.</returns>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">The geometry hook recurses or returns null.</exception>
    public ShapeGeometry GetGeometryData() => GeometryData;

    private ShapeGeometry? _geometryData;
    private bool _readingGeometry;
    private int _collisionUses;
    private readonly object _geometryGate = new();
    internal void BeginCollisionUse(bool allowUnconfigured = false)
    {
        lock (_geometryGate)
        {
            ThrowIfDisposed();
            if (!allowUnconfigured && !HasServerData) throw new InvalidOperationException("Server shape data must be configured before collision geometry is borrowed.");
            if (_checkingAuthoringAccess) throw new InvalidOperationException("Collision geometry cannot be borrowed during an authoring access hook.");
            _collisionUses++;
        }
    }
    internal void EndCollisionUse() { lock (_geometryGate) _collisionUses--; }
    internal void EnsureCollisionMutationAccess()
    {
        if (Volatile.Read(ref _collisionUses) != 0) throw new InvalidOperationException("Collision geometry and lifetime cannot change while a shape pair is borrowed.");
    }
    private ShapeGeometry GeometryData
    {
        get
        {
            lock (_geometryGate)
            {
                ThrowIfDisposed();
                if (_geometryData is not null) return _geometryData;
                if (_readingGeometry) throw new InvalidOperationException("Shape geometry construction cannot recurse.");
                _readingGeometry = true;
                try { return _geometryData = GetGeometryCore() ?? throw new InvalidOperationException("A shape must supply geometry."); }
                finally { _readingGeometry = false; }
            }
        }
    }
    internal PhysicsShapeGeometry GetGeometry() => GeometryData.Borrow();

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

    internal void BindServerOwnedRID(RID rid, bool configured = true)
    {
        lock (_queryRIDGate) { _queryRID = rid; _serverOwned = true; HasServerData = configured; }
    }

    internal void EnsureServerBindingAccess()
    {
        if (_queryRID.IsValid() && !IsDisposed) PhysicsServer.Service.EnsureShapeBindingAccess(_queryRID);
    }

    internal void ReleaseServerGeometry()
    {
        lock (_queryRIDGate) _serverReleaseThread = Environment.CurrentManagedThreadId;
        try { Dispose(); }
        finally { lock (_queryRIDGate) _serverReleaseThread = 0; }
    }

    /// <summary>Preserves the lifetime of geometry borrowed from a caller-owned server shape.</summary>
    /// <exception cref="InvalidOperationException">The server RID owns this geometry, or its geometry, policy access hook or shape-pair use is borrowed.</exception>
    protected override void ValidateDisposal()
    {
        if (_serverReleaseThread != Environment.CurrentManagedThreadId) EnsureServerBindingAccess();
        EnsureCollisionMutationAccess();
        lock (_geometryGate)
            if (_readingGeometry || _checkingAuthoringAccess) throw new InvalidOperationException("A shape cannot be disposed during its geometry or authoring access hook.");
        lock (_queryRIDGate)
            if (_serverOwned && _serverReleaseThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("This geometry is borrowed from a server shape RID.");
        base.ValidateDisposal();
    }

    /// <summary>Checks every borrower before changing this resource's authored geometry.</summary>
    /// <remarks>Call after validating input and before changing private data, including an equal assignment.
    /// Call <see cref="EmitGeometryChanged"/> after committing the edit. Access hooks run without resource locks.</remarks>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">An owner, phase or borrowed geometry/authoring hook or shape-pair use rejects this edit.</exception>
    protected void EnsureGeometryChangeAccess() => EnsureAuthoringAccess(observeBackend: true, resourceGeometry: true);

    internal override void ValidateStateMutation() => EnsureGeometryChangeAccess();

    private void EnsureAuthoringAccess(bool observeBackend, bool resourceGeometry)
    {
        EnsureServerBindingAccess();
        EnsureCollisionMutationAccess();
        if (Volatile.Read(ref _readingGeometry)) throw new InvalidOperationException("Shape authoring cannot change during its geometry construction hook.");
        lock (_geometryGate)
        {
            ThrowIfDisposed();
            if (_readingGeometry || _checkingAuthoringAccess) throw new InvalidOperationException("Shape authoring cannot change while its geometry or access hook is borrowed.");
            _checkingAuthoringAccess = true;
        }
        try { PhysicsServer.Service.EnsureShapePolicyAccess(this, _policySceneUsers, _policyServerUsers, observeBackend, resourceGeometry); }
        finally { lock (_geometryGate) _checkingAuthoringAccess = false; }
    }

    /// <summary>Invalidates compiled geometry and notifies all borrowers after an authored geometry edit.</summary>
    /// <remarks>Call <see cref="EnsureGeometryChangeAccess"/> before committing private data.
    /// This publication checks owner/phase access without calling selected status hooks again. It cannot undo private subclass writes.
    /// The shape RID remains stable; the next query or step rebuilds affected attachments.</remarks>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">An owner, phase or borrowed geometry/authoring hook or shape-pair use rejects publication.</exception>
    protected internal void EmitGeometryChanged()
    {
        EnsureAuthoringAccess(observeBackend: false, resourceGeometry: true);
        lock (_geometryGate)
        {
            ThrowIfDisposed();
            if (_readingGeometry) throw new InvalidOperationException("Geometry cannot change during its construction hook.");
            _geometryData = null;
            _revision++;
        }
        Interlocked.Increment(ref _geometryEpoch);
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
            Interlocked.Increment(ref _geometryEpoch);
            RID rid;
            lock (_queryRIDGate) { rid = _queryRID; _queryRID = default; }
            if (rid.IsValid() && !_serverOwned) PhysicsServer.Service.UnregisterBorrowedShape(rid);
        }
        base.Dispose(disposing);
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
    /// <exception cref="InvalidOperationException">A borrower owner, phase or geometry/authoring hook or shape-pair use rejects the edit before mutation.</exception>
    public float Radius
    {
        get { ThrowIfDisposed(); return _radius; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value <= 0 || value > float.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(value));
            EnsureGeometryChangeAccess();
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

    /// <inheritdoc />
    protected override ShapeGeometry GetGeometryCore()
    {
        ThrowIfDisposed();
        return ShapeGeometry.Circle(_radius);
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CircleShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        ((CircleShape)target)._radius = _radius;
        ((Shape)target).EmitGeometryChanged();
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
    /// <exception cref="InvalidOperationException">A borrower owner, phase or geometry/authoring hook or shape-pair use rejects the edit before mutation.</exception>
    public float Radius
    {
        get { ThrowIfDisposed(); return _radius; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value < 0 || value > float.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(value));
            EnsureGeometryChangeAccess();
            if (_radius == value) return;
            _radius = value;
            _height = MathF.Max(_height, 2 * value);
            EmitGeometryChanged();
        }
    }

    /// <summary>Gets or sets the nonnegative finite full height, including both end caps.</summary>
    /// <value>Thirty by default; decreasing it below twice the radius also decreases the radius.</value>
    /// <exception cref="ArgumentOutOfRangeException">The height is negative or nonfinite.</exception>
    /// <exception cref="InvalidOperationException">A borrower owner, phase or geometry/authoring hook or shape-pair use rejects the edit before mutation.</exception>
    public float Height
    {
        get { ThrowIfDisposed(); return _height; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            EnsureGeometryChangeAccess();
            if (_height == value) return;
            _height = value;
            _radius = MathF.Min(_radius, value * 0.5f);
            EmitGeometryChanged();
        }
    }

    /// <summary>Gets or sets the center segment height between the two end-cap centers.</summary>
    /// <value>Ten by default; this is <see cref="Height"/> minus twice <see cref="Radius"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The height is negative, nonfinite, or makes the full height overflow.</exception>
    /// <exception cref="InvalidOperationException">A borrower owner, phase or geometry/authoring hook or shape-pair use rejects the edit before mutation.</exception>
    public float MidHeight
    {
        get { ThrowIfDisposed(); return _height - 2 * _radius; }
        set
        {
            ThrowIfDisposed();
            var height = value + 2 * _radius;
            if (!float.IsFinite(value) || value < 0 || !float.IsFinite(height))
                throw new ArgumentOutOfRangeException(nameof(value));
            EnsureGeometryChangeAccess();
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

    /// <inheritdoc />
    protected override ShapeGeometry GetGeometryCore()
    {
        ThrowIfDisposed();
        return ShapeGeometry.Capsule(_radius, _height);
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CapsuleShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        ((CapsuleShape)target)._radius = _radius;
        ((CapsuleShape)target)._height = _height;
        ((Shape)target).EmitGeometryChanged();
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
    /// <exception cref="InvalidOperationException">A borrower owner, phase or geometry/authoring hook or shape-pair use rejects the edit before mutation.</exception>
    public Vector2 A
    {
        get { ThrowIfDisposed(); return _a; }
        set
        {
            ThrowIfDisposed();
            ValidateEndpoint(value, _b);
            EnsureGeometryChangeAccess();
            if (_a == value) return;
            _a = value;
            EmitGeometryChanged();
        }
    }

    /// <summary>Gets or sets the second finite local endpoint.</summary>
    /// <value>(0, 10) by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The endpoint is nonfinite or produces unrepresentable bounds.</exception>
    /// <exception cref="InvalidOperationException">A borrower owner, phase or geometry/authoring hook or shape-pair use rejects the edit before mutation.</exception>
    public Vector2 B
    {
        get { ThrowIfDisposed(); return _b; }
        set
        {
            ThrowIfDisposed();
            ValidateEndpoint(value, _a);
            EnsureGeometryChangeAccess();
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

    /// <inheritdoc />
    protected override ShapeGeometry GetGeometryCore()
    {
        ThrowIfDisposed();
        return ShapeGeometry.Segment(_a, _b);
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SegmentShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        ((SegmentShape)target)._a = _a;
        ((SegmentShape)target)._b = _b;
        ((Shape)target).EmitGeometryChanged();
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
    /// <exception cref="InvalidOperationException">A borrower owner, phase or geometry/authoring hook or shape-pair use rejects the edit before mutation.</exception>
    public Vector2 Size
    {
        get { ThrowIfDisposed(); return _size; }
        set
        {
            ThrowIfDisposed();
            if (!value.IsFinite() || value.X <= 0 || value.Y <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            EnsureGeometryChangeAccess();
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

    /// <inheritdoc />
    protected override ShapeGeometry GetGeometryCore()
    {
        ThrowIfDisposed();
        return ShapeGeometry.Rectangle(_size);
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new RectangleShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        ((RectangleShape)target)._size = _size;
        ((Shape)target).EmitGeometryChanged();
    }
}
