namespace Electron2D;

/// <summary>Samples the nearest 2D collider along a local ray and caches its last physics-frame result.</summary>
/// <remarks>An enabled node updates in the internal fixed physics lane. Call <see cref="ForceRaycastUpdate"/>
/// to sample immediately, including while disabled. A detached node has no query world.</remarks>
public sealed class RayCast : Entity
{
    private static readonly PropertyDescriptor[] RayProperties =
    [
        new PropertyDescriptor<RayCast, Vector2>(nameof(TargetPosition), ray => ray.TargetPosition,
            (ray, value) => ray.TargetPosition = value, _ => new Vector2(0, 50), stored: true),
        new PropertyDescriptor<RayCast, uint>(nameof(CollisionMask), ray => ray.CollisionMask,
            (ray, value) => ray.CollisionMask = value, _ => 1u, stored: true),
        new PropertyDescriptor<RayCast, bool>(nameof(Enabled), ray => ray.Enabled,
            (ray, value) => ray.Enabled = value, _ => true, stored: true),
        new PropertyDescriptor<RayCast, bool>(nameof(ExcludeParent), ray => ray.ExcludeParent,
            (ray, value) => ray.ExcludeParent = value, _ => true, stored: true),
        new PropertyDescriptor<RayCast, bool>(nameof(HitFromInside), ray => ray.HitFromInside,
            (ray, value) => ray.HitFromInside = value, _ => false, stored: true),
        new PropertyDescriptor<RayCast, bool>(nameof(CollideWithAreas), ray => ray.CollideWithAreas,
            (ray, value) => ray.CollideWithAreas = value, _ => false, stored: true),
        new PropertyDescriptor<RayCast, bool>(nameof(CollideWithBodies), ray => ray.CollideWithBodies,
            (ray, value) => ray.CollideWithBodies = value, _ => true, stored: true)
    ];

    private readonly HashSet<RID> _exceptions = [];
    private RID[] _excludeSnapshot = [];
    private Vector2 _targetPosition = new(0, 50);
    private uint _collisionMask = 1;
    private bool _enabled = true;
    private bool _excludeParent = true;
    private bool _collideWithBodies = true;
    private bool _collideWithAreas;
    private bool _hitFromInside;
    private bool _collided;
    private RID _colliderRID;
    private int _colliderShape;
    private Vector2 _collisionPoint;
    private Vector2 _collisionNormal;

    /// <summary>Creates a detached enabled ray fifty scene units downward.</summary>
    public RayCast() { }

    /// <summary>Gets or sets the finite local endpoint of the ray.</summary>
    /// <value>(0, 50) by default; zero becomes a 0.01-unit downward ray when sampled.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 TargetPosition
    {
        get { ThrowIfDisposed(); return _targetPosition; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _targetPosition = value; }
    }

    /// <summary>Gets or sets the 32-bit collision layers accepted by this ray.</summary>
    /// <value>Layer one by default.</value>
    public uint CollisionMask
    {
        get { ThrowIfDisposed(); return _collisionMask; }
        set { EnsureMutable(); _collisionMask = value; }
    }

    /// <summary>Gets or sets automatic sampling in each eligible fixed physics frame.</summary>
    /// <value>True by default. Disabling clears only <see cref="IsColliding"/>.</value>
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

    /// <summary>Gets or sets whether a filled shape containing the origin can be hit.</summary>
    /// <value>False by default. Such a hit has the origin as its point and zero normal.</value>
    public bool HitFromInside
    {
        get { ThrowIfDisposed(); return _hitFromInside; }
        set { EnsureMutable(); _hitFromInside = value; }
    }

    /// <summary>Gets or sets whether Area sensors are eligible.</summary>
    /// <value>False by default.</value>
    public bool CollideWithAreas
    {
        get { ThrowIfDisposed(); return _collideWithAreas; }
        set { EnsureMutable(); _collideWithAreas = value; }
    }

    /// <summary>Gets or sets whether physics bodies are eligible.</summary>
    /// <value>True by default.</value>
    public bool CollideWithBodies
    {
        get { ThrowIfDisposed(); return _collideWithBodies; }
        set { EnsureMutable(); _collideWithBodies = value; }
    }

    /// <summary>Adds a live scene collision object to the RID exception set.</summary>
    /// <param name="node">The scene body or Area to exclude.</param>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public void AddException(CollisionObject node)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(node);
        AddExceptionRID(node.GetRID());
    }

    /// <summary>Adds a collider RID to the exception set.</summary>
    /// <param name="rid">The identity to exclude; duplicates are ignored.</param>
    public void AddExceptionRID(RID rid)
    {
        EnsureMutable();
        if (_exceptions.Add(rid)) RefreshExclusions();
    }

    /// <summary>Removes a scene collision object from the RID exception set.</summary>
    /// <param name="node">The scene body or Area to include again.</param>
    /// <exception cref="ArgumentNullException">The node is null.</exception>
    public void RemoveException(CollisionObject node)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(node);
        RemoveExceptionRID(node.GetRID());
    }

    /// <summary>Removes one collider RID from the exception set.</summary>
    /// <param name="rid">The identity to include again.</param>
    public void RemoveExceptionRID(RID rid)
    {
        EnsureMutable();
        if (_exceptions.Remove(rid)) RefreshExclusions();
    }

    /// <summary>Clears explicit exceptions, retaining an enabled direct-parent exception.</summary>
    public void ClearExceptions()
    {
        EnsureMutable();
        _exceptions.Clear();
        if (_excludeParent && IsInsideTree && Parent is CollisionObject parent)
            _exceptions.Add(parent.GetRID());
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

    /// <summary>Returns whether the most recent automatic or forced sample hit a collider.</summary>
    /// <returns>False before the first sample, after a miss, or immediately after disabling.</returns>
    public bool IsColliding() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _collided; }

    /// <summary>Returns the last sampled scene collider when it is still alive.</summary>
    /// <returns>A scene object, or null for a miss, server-only collider, or disposed scene object.</returns>
    public ElectronObject? GetCollider()
    {
        ThrowIfDisposed();
        Tree?.EnsureOwnerThread();
        return PhysicsServer.Service.ResolveSceneObject(_colliderRID);
    }

    /// <summary>Returns the RID retained from the most recent hit.</summary>
    /// <returns>The collider identity, or an empty RID after a miss or before first sampling.</returns>
    public RID GetColliderRID() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _colliderRID; }

    /// <summary>Returns the last hit's shape-owner index.</summary>
    /// <returns>Zero before a hit or after a miss.</returns>
    public int GetColliderShape() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _colliderShape; }

    /// <summary>Returns the last hit point in global scene coordinates.</summary>
    /// <returns>Zero before any hit; the last point remains available after a miss or disable.</returns>
    public Vector2 GetCollisionPoint() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _collisionPoint; }

    /// <summary>Returns the last hit normal in global coordinates.</summary>
    /// <returns>Zero before any hit; the last normal remains available after a miss or disable.</returns>
    public Vector2 GetCollisionNormal() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _collisionNormal; }

    /// <summary>Samples the current physics space immediately, regardless of Enabled.</summary>
    /// <exception cref="InvalidOperationException">The node is detached, off-owner, or the world is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The ray node has been disposed.</exception>
    public void ForceRaycastUpdate()
    {
        EnsureMutable();
        var world = GetWorld() ?? throw new InvalidOperationException("A raycast requires an attached scene world.");
        var transform = GetGlobalTransform();
        var target = _targetPosition == Vector2.Zero ? new Vector2(0, 0.01f) : _targetPosition;
        var result = world.DirectSpaceState.IntersectRay(transform.Origin, transform * target,
            _collisionMask, _excludeSnapshot, _collideWithAreas, _collideWithBodies, _hitFromInside);
        if (result is { } hit)
        {
            _collided = true;
            _colliderRID = hit.ColliderRID;
            _colliderShape = hit.ShapeIndex;
            _collisionPoint = hit.Position;
            _collisionNormal = hit.Normal;
        }
        else
        {
            _collided = false;
            _colliderRID = default;
            _colliderShape = 0;
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(RayProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateRayCast;

    private static Node CreateRayCast() => new RayCast();

    /// <inheritdoc />
    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        if (_excludeParent && Parent is CollisionObject parent && _exceptions.Add(parent.GetRID()))
            RefreshExclusions();
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
        if (what == NotificationInternalPhysicsProcess && _enabled) ForceRaycastUpdate();
        base.OnNotification(what);
    }

    private void RefreshExclusions() => _excludeSnapshot = _exceptions.ToArray();

    private static uint LayerBit(int number)
    {
        if (number is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(number));
        return 1u << (number - 1);
    }
}
