namespace Electron2D;

/// <summary>Sweeps a borrowed 2D shape from this node toward a local target and caches its physics-frame contacts.</summary>
/// <remarks>An enabled node samples in the internal fixed physics lane. <see cref="ForceShapecastUpdate"/>
/// samples immediately, including while disabled. The caller retains ownership of <see cref="Shape"/>.</remarks>
public sealed class ShapeCast : Entity
{
    private static readonly PropertyDescriptor[] CastProperties =
    [
        new PropertyDescriptor<ShapeCast, Shape?>(nameof(Shape), node => node.Shape,
            (node, value) => node.Shape = value, _ => null, stored: true),
        new PropertyDescriptor<ShapeCast, Vector2>(nameof(TargetPosition), node => node.TargetPosition,
            (node, value) => node.TargetPosition = value, _ => new Vector2(0, 50), stored: true),
        new PropertyDescriptor<ShapeCast, float>(nameof(Margin), node => node.Margin,
            (node, value) => node.Margin = value, _ => 0f, stored: true),
        new PropertyDescriptor<ShapeCast, int>(nameof(MaxResults), node => node.MaxResults,
            (node, value) => node.MaxResults = value, _ => 32, stored: true),
        new PropertyDescriptor<ShapeCast, uint>(nameof(CollisionMask), node => node.CollisionMask,
            (node, value) => node.CollisionMask = value, _ => 1u, stored: true),
        new PropertyDescriptor<ShapeCast, bool>(nameof(Enabled), node => node.Enabled,
            (node, value) => node.Enabled = value, _ => true, stored: true),
        new PropertyDescriptor<ShapeCast, bool>(nameof(ExcludeParent), node => node.ExcludeParent,
            (node, value) => node.ExcludeParent = value, _ => true, stored: true),
        new PropertyDescriptor<ShapeCast, bool>(nameof(CollideWithAreas), node => node.CollideWithAreas,
            (node, value) => node.CollideWithAreas = value, _ => false, stored: true),
        new PropertyDescriptor<ShapeCast, bool>(nameof(CollideWithBodies), node => node.CollideWithBodies,
            (node, value) => node.CollideWithBodies = value, _ => true, stored: true)
    ];

    private readonly PhysicsShapeQueryParameters2D _query = new();
    private readonly HashSet<RID> _exceptions = [];
    private readonly List<PhysicsRestInfo2D> _results = [];
    private readonly List<PhysicsRestInfo2D> _staging = [];
    private RID[] _exceptionSnapshot = [];
    private RID[] _excludeScratch = [];
    private Shape? _shape;
    private Vector2 _targetPosition = new(0, 50);
    private float _margin;
    private int _maxResults = 32;
    private uint _collisionMask = 1;
    private bool _enabled = true;
    private bool _excludeParent = true;
    private bool _collideWithBodies = true;
    private bool _collideWithAreas;
    private bool _collided;
    private float _safeFraction;
    private float _unsafeFraction;

    /// <summary>Creates a detached, enabled shape cast with a fifty-unit local target.</summary>
    public ShapeCast() { }

    /// <summary>Gets or sets the caller-owned query shape.</summary>
    /// <value>Null by default; a null shape cannot be sampled.</value>
    /// <exception cref="ObjectDisposedException">The assigned shape was disposed.</exception>
    public Shape? Shape
    {
        get { ThrowIfDisposed(); return _shape; }
        set
        {
            EnsureMutable();
            if (ReferenceEquals(_shape, value)) return;
            if (value is null) _query.ShapeRID = default;
            else _query.Shape = value;
            _shape = value;
        }
    }

    /// <summary>Gets or sets the finite target position in this node's local coordinates.</summary>
    /// <value>(0, 50) by default; zero samples overlap at the current pose.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 TargetPosition
    {
        get { ThrowIfDisposed(); return _targetPosition; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _targetPosition = value; }
    }

    /// <summary>Gets or sets the nonnegative query margin in scene units.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public float Margin
    {
        get { ThrowIfDisposed(); return _margin; }
        set
        {
            EnsureMutable();
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _margin = value;
        }
    }

    /// <summary>Gets or sets the maximum number of different collider RIDs returned per sample.</summary>
    /// <value>32 by default; zero or a negative value returns no contacts.</value>
    public int MaxResults
    {
        get { ThrowIfDisposed(); return _maxResults; }
        set { EnsureMutable(); _maxResults = value; }
    }

    /// <summary>Gets or sets eligible collider-layer bits.</summary>
    /// <value>Layer one by default.</value>
    public uint CollisionMask
    {
        get { ThrowIfDisposed(); return _collisionMask; }
        set { EnsureMutable(); _collisionMask = value; }
    }

    /// <summary>Gets or sets automatic fixed-frame sampling.</summary>
    /// <value>True by default. Disabling clears only the collision flag.</value>
    public bool Enabled
    {
        get { ThrowIfDisposed(); return _enabled; }
        set
        {
            EnsureMutable();
            _enabled = value;
            if (IsInsideTree) SetInternalProcessing(false, value);
            if (!value) _collided = false;
        }
    }

    /// <summary>Gets or sets whether a direct CollisionObject parent is excluded.</summary>
    /// <value>True by default.</value>
    public bool ExcludeParent
    {
        get { ThrowIfDisposed(); return _excludeParent; }
        set
        {
            EnsureMutable();
            if (_excludeParent == value) return;
            _excludeParent = value;
            if (IsInsideTree && Parent is CollisionObject parent)
            {
                if (value) _exceptions.Add(parent.GetRID());
                else _exceptions.Remove(parent.GetRID());
                RefreshExclusions();
            }
        }
    }

    /// <summary>Gets or sets whether Area sensors can be reported.</summary>
    /// <value>False by default.</value>
    public bool CollideWithAreas
    {
        get { ThrowIfDisposed(); return _collideWithAreas; }
        set { EnsureMutable(); _collideWithAreas = value; }
    }

    /// <summary>Gets or sets whether physics bodies can be reported.</summary>
    /// <value>True by default.</value>
    public bool CollideWithBodies
    {
        get { ThrowIfDisposed(); return _collideWithBodies; }
        set { EnsureMutable(); _collideWithBodies = value; }
    }

    /// <summary>Adds a scene collider RID to the exception set.</summary>
    /// <param name="node">A live scene body or Area.</param>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public void AddException(CollisionObject node)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(node);
        AddExceptionRID(node.GetRID());
    }

    /// <summary>Adds a collider RID to the exception set.</summary>
    /// <param name="rid">The collider identity; duplicate additions do nothing.</param>
    public void AddExceptionRID(RID rid)
    {
        EnsureMutable();
        if (_exceptions.Add(rid)) RefreshExclusions();
    }

    /// <summary>Removes a scene collider RID from the exception set.</summary>
    /// <param name="node">A live scene body or Area.</param>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public void RemoveException(CollisionObject node)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(node);
        RemoveExceptionRID(node.GetRID());
    }

    /// <summary>Removes a collider RID from the exception set.</summary>
    /// <param name="rid">The identity to include again.</param>
    public void RemoveExceptionRID(RID rid)
    {
        EnsureMutable();
        if (_exceptions.Remove(rid)) RefreshExclusions();
    }

    /// <summary>Clears all current exceptions, including an active parent exception.</summary>
    public void ClearExceptions()
    {
        EnsureMutable();
        if (_exceptions.Count == 0) return;
        _exceptions.Clear();
        RefreshExclusions();
    }

    /// <summary>Tests a one-based collision-layer bit.</summary>
    /// <param name="layerNumber">A bit number from one through 32.</param>
    /// <returns>Whether the bit is set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The layer number is outside one through 32.</exception>
    public bool GetCollisionMaskValue(int layerNumber) => (CollisionMask & LayerBit(layerNumber)) != 0;

    /// <summary>Changes a one-based collision-layer bit.</summary>
    /// <param name="layerNumber">A bit number from one through 32.</param>
    /// <param name="value">Whether the bit should be set.</param>
    /// <exception cref="ArgumentOutOfRangeException">The layer number is outside one through 32.</exception>
    public void SetCollisionMaskValue(int layerNumber, bool value)
    {
        var bit = LayerBit(layerNumber);
        CollisionMask = value ? CollisionMask | bit : CollisionMask & ~bit;
    }

    /// <summary>Returns the number of cached contacts.</summary>
    /// <returns>Zero before a successful hit or after a miss.</returns>
    public int GetCollisionCount() { EnsureReadable(); return _results.Count; }

    /// <summary>Returns whether the last sample detected any collider.</summary>
    /// <returns>False before a hit, after a miss, or immediately after disabling.</returns>
    public bool IsColliding() { EnsureReadable(); return _collided; }

    /// <summary>Returns the currently live scene collider at a cached result index.</summary>
    /// <param name="index">Zero-based cached contact index.</param>
    /// <returns>A scene object, or null for a server-only or disposed collider.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is not present in the last snapshot.</exception>
    public ElectronObject? GetCollider(int index)
    {
        var result = Result(index);
        return PhysicsServer.Instance.ResolveSceneObject(result.ColliderRID);
    }

    /// <summary>Returns a cached collider RID.</summary>
    /// <param name="index">Zero-based cached contact index.</param>
    /// <returns>The collider identity at the last sample.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is not present in the last snapshot.</exception>
    public RID GetColliderRID(int index) => Result(index).ColliderRID;

    /// <summary>Returns a cached collider shape-owner index.</summary>
    /// <param name="index">Zero-based cached contact index.</param>
    /// <returns>The direct shape-owner index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is not present in the last snapshot.</exception>
    public int GetColliderShape(int index) => Result(index).ShapeIndex;

    /// <summary>Returns a cached collider contact point in global scene units.</summary>
    /// <param name="index">Zero-based cached contact index.</param>
    /// <returns>The contact point.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is not present in the last snapshot.</exception>
    public Vector2 GetCollisionPoint(int index) => Result(index).Point;

    /// <summary>Returns a cached normal pointing away from the collider.</summary>
    /// <param name="index">Zero-based cached contact index.</param>
    /// <returns>The global contact normal.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is not present in the last snapshot.</exception>
    public Vector2 GetCollisionNormal(int index) => Result(index).Normal;

    /// <summary>Returns a caller-owned copy of every cached typed contact.</summary>
    /// <value>An empty array before a hit or after a miss; scene references resolve at read time.</value>
    public PhysicsRestInfo2D[] CollisionResult
    {
        get
        {
            EnsureReadable();
            var results = _results.ToArray();
            for (var index = 0; index < results.Length; index++)
            {
                var hit = results[index];
                results[index] = new PhysicsRestInfo2D(hit.ColliderRID,
                    PhysicsServer.Instance.ResolveSceneObject(hit.ColliderRID), hit.ShapeIndex,
                    hit.Point, hit.Normal, hit.LinearVelocity);
            }
            return results;
        }
    }

    /// <summary>Returns the last safe fraction of the local target displacement.</summary>
    /// <returns>Zero before a moving sample; one on a moving sample without a new collision.</returns>
    public float GetClosestCollisionSafeFraction() { EnsureReadable(); return _safeFraction; }

    /// <summary>Returns the last unsafe fraction of the local target displacement.</summary>
    /// <returns>Zero before a moving sample; one on a moving sample without a new collision.</returns>
    public float GetClosestCollisionUnsafeFraction() { EnsureReadable(); return _unsafeFraction; }

    /// <summary>Samples the current space immediately, including while disabled or before the first physics frame.</summary>
    /// <exception cref="InvalidOperationException">The node is detached, off-owner, stepping, or has no live Shape.</exception>
    public void ForceShapecastUpdate()
    {
        EnsureMutable();
        if (_shape is null || _shape.IsDisposed)
            throw new InvalidOperationException("A shape cast requires a live Shape resource.");
        var world = GetWorld2D() ?? throw new InvalidOperationException("A shape cast requires an attached scene world.");
        var transform = GetGlobalTransform();
        var motion = transform.BasisXform(_targetPosition);
        _query.Transform = transform;
        _query.Motion = motion;
        _query.Margin = _margin;
        _query.CollisionMask = _collisionMask;
        _query.CollideWithAreas = _collideWithAreas;
        _query.CollideWithBodies = _collideWithBodies;
        PrepareExclusions();
        var direct = world.DirectSpaceState;
        var safe = 0f;
        var unsafeFraction = 0f;
        if (_targetPosition != Vector2.Zero)
        {
            (safe, unsafeFraction) = direct.CastMotion(_query);
            if (unsafeFraction < 1f)
            {
                var distance = motion.Length() * PhysicsSpace.MetersPerUnit;
                var fraction = MathF.Min(1f, unsafeFraction + 2f * Box2D.NET.B2Constants.B2_LINEAR_SLOP / distance);
                _query.Transform = new Transform(transform.Rotation, Vector2.One, 0,
                    transform.Origin + motion * fraction);
            }
        }
        _query.Motion = Vector2.Zero;
        _staging.Clear();
        var used = _exceptionSnapshot.Length;
        while (_staging.Count < _maxResults && direct.GetRestInfo(_query) is { } hit)
        {
            _staging.Add(hit);
            if (used == _excludeScratch.Length)
            {
                Array.Resize(ref _excludeScratch, Math.Max(4, used * 2));
                _query.BorrowExclusions(_excludeScratch);
            }
            _excludeScratch[used++] = hit.ColliderRID;
        }
        _results.Clear();
        _results.AddRange(_staging);
        _collided = _results.Count != 0;
        _safeFraction = safe;
        _unsafeFraction = unsafeFraction;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(CastProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateShapeCast;

    private static Node CreateShapeCast() => new ShapeCast();

    /// <inheritdoc />
    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        if (Parent is CollisionObject parent)
        {
            var rid = parent.GetRID();
            if (_excludeParent) _exceptions.Add(rid);
            else _exceptions.Remove(rid);
            RefreshExclusions();
        }
        SetInternalProcessing(false, _enabled);
    }

    /// <inheritdoc />
    protected override void OnExitTree()
    {
        SetInternalProcessing(false, false);
        if (_excludeParent && Parent is CollisionObject parent && _exceptions.Remove(parent.GetRID()))
            RefreshExclusions();
        base.OnExitTree();
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationInternalPhysicsProcess && _enabled) ForceShapecastUpdate();
        base.OnNotification(what);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally { if (disposing) _query.Dispose(); }
    }

    private void PrepareExclusions()
    {
        if (_excludeScratch.Length < _exceptionSnapshot.Length + 1)
            Array.Resize(ref _excludeScratch, Math.Max(4, _exceptionSnapshot.Length + 1));
        Array.Clear(_excludeScratch);
        _exceptionSnapshot.CopyTo(_excludeScratch, 0);
        _query.BorrowExclusions(_excludeScratch);
    }

    private void RefreshExclusions() => _exceptionSnapshot = _exceptions.ToArray();

    private void EnsureReadable() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }

    private PhysicsRestInfo2D Result(int index)
    {
        EnsureReadable();
        if ((uint)index >= (uint)_results.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return _results[index];
    }

    private static uint LayerBit(int number)
    {
        if (number is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(number));
        return 1u << (number - 1);
    }
}
