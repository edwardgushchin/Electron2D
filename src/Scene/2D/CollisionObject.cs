using Box2D.NET;

namespace Electron2D;

/// <summary>A spatial scene object that owns collision filtering and motion-recovery priority for physics shapes.</summary>
public abstract partial class CollisionObject : Entity
{
    private static readonly PropertyDescriptor[] CollisionProperties =
    [
        new PropertyDescriptor<CollisionObject, uint>(nameof(CollisionLayer), node => node.CollisionLayer, (node, value) => node.CollisionLayer = value, _ => 1u, stored: true),
        new PropertyDescriptor<CollisionObject, uint>(nameof(CollisionMask), node => node.CollisionMask, (node, value) => node.CollisionMask = value, _ => 1u, stored: true),
        new PropertyDescriptor<CollisionObject, float>(nameof(CollisionPriority), node => node.CollisionPriority, (node, value) => node.CollisionPriority = value, _ => 1f, stored: true),
        new PropertyDescriptor<CollisionObject, CollisionDisableMode>(nameof(DisableMode), node => node.DisableMode, (node, value) => node.DisableMode = value, _ => CollisionDisableMode.Remove, stored: true)
    ];

    private CollisionDisableMode _disableMode;
    internal bool PhysicsRemoved => _disableMode == CollisionDisableMode.Remove && IsInsideTree && ProcessingDisabled;
    internal bool PhysicsMadeStatic => _disableMode == CollisionDisableMode.MakeStatic && IsInsideTree && ProcessingDisabled;

    private uint _collisionLayer = 1;
    private uint _collisionMask = 1;
    private readonly RID _rid;
    internal RID PhysicsRID => _rid;

    /// <summary>Creates an object in collision layer one with mask one.</summary>
    protected CollisionObject()
    {
        _rid = PhysicsServer.Service.RegisterSceneObject(this);
        Backend = new(_rid, this);
    }

    internal PhysicsColliderBackend Backend { get; }

    /// <summary>Gets or sets this collider's relative weight when another body recovers from penetration.</summary>
    /// <value>A finite positive weight, one by default.</value>
    /// <remarks>A higher weight favors recovery out of this body. It does not alter rigid contact impulses.
    /// Area retains this property but does not obstruct body motion. Edits preserve sleep and fixture identity.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or not positive.</exception>
    /// <exception cref="InvalidOperationException">Access violates the owner-thread or physics-step boundary.</exception>
    /// <exception cref="ObjectDisposedException">The object is disposed.</exception>
    public float CollisionPriority
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); Backend.Space?.EnsureQueryAccess(); return Backend.CollisionPriority; }
        set { EnsureMutable(); Backend.SetCollisionPriority(value); }
    }

    /// <summary>Gets the stable server identity of this collision object.</summary>
    /// <returns>A nonempty RID unchanged by fixture rebuilds or scene attachment.</returns>
    public RID GetRID()
    {
        ThrowIfDisposed();
        return _rid;
    }

    /// <summary>Gets or sets how effective disabled processing affects physics participation.</summary>
    /// <value><see cref="CollisionDisableMode.Remove"/> by default.</value>
    /// <remarks>Applies synchronously to attached objects, including inherited disabled processing.
    /// Pausing the scene alone does not apply this policy. Area remains a sensor in MakeStatic mode.
    /// Removal retains RID and shape owners, invalidates live body views and reports peer overlap exits.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="InvalidOperationException">Attached mutation is off the owner thread or the world is stepping.</exception>
    /// <exception cref="ObjectDisposedException">The object is disposed.</exception>
    /// <exception cref="Exception">A departure callback throws after the policy and membership change.</exception>
    public CollisionDisableMode DisableMode
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _disableMode; }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_disableMode == value) return;
            EnsurePhysicsParticipationChange();
            _disableMode = value;
            if (IsInsideTree) UpdatePhysicsParticipation();
        }
    }

    /// <summary>Gets or sets the 32-bit collision category mask.</summary>
    /// <value>One by default.</value>
    public uint CollisionLayer
    {
        get { ThrowIfDisposed(); return _collisionLayer; }
        set { EnsureMutable(); _collisionLayer = value; OnCollisionFilterChanged(); }
    }

    /// <summary>Gets or sets the 32-bit categories accepted for collision.</summary>
    /// <value>One by default.</value>
    public uint CollisionMask
    {
        get { ThrowIfDisposed(); return _collisionMask; }
        set { EnsureMutable(); _collisionMask = value; OnCollisionFilterChanged(); }
    }

    /// <summary>Tests a one-based collision-layer bit.</summary>
    /// <param name="layerNumber">A bit number from one through 32.</param>
    /// <returns>Whether the bit is set.</returns>
    public bool GetCollisionLayerValue(int layerNumber) => (CollisionLayer & LayerBit(layerNumber)) != 0;

    /// <summary>Changes a one-based collision-layer bit.</summary>
    /// <param name="layerNumber">A bit number from one through 32.</param>
    /// <param name="value">Whether the bit should be set.</param>
    public void SetCollisionLayerValue(int layerNumber, bool value)
    {
        var bit = LayerBit(layerNumber);
        CollisionLayer = value ? CollisionLayer | bit : CollisionLayer & ~bit;
    }

    /// <summary>Tests a one-based collision-mask bit.</summary>
    /// <param name="layerNumber">A bit number from one through 32.</param>
    /// <returns>Whether the bit is set.</returns>
    public bool GetCollisionMaskValue(int layerNumber) => (CollisionMask & LayerBit(layerNumber)) != 0;

    /// <summary>Changes a one-based collision-mask bit.</summary>
    /// <param name="layerNumber">A bit number from one through 32.</param>
    /// <param name="value">Whether the bit should be set.</param>
    public void SetCollisionMaskValue(int layerNumber, bool value)
    {
        var bit = LayerBit(layerNumber);
        CollisionMask = value ? CollisionMask | bit : CollisionMask & ~bit;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(CollisionProperties);

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what is NotificationDisabled or NotificationEnabled && IsInsideTree)
            UpdatePhysicsParticipation();
    }

    internal void EnsurePhysicsParticipationChange() => Tree?.EnsurePhysicsParticipationChange();

    internal abstract void UpdatePhysicsParticipation();

    internal virtual void OnCollisionFilterChanged() { }

    internal IReadOnlyList<B2ShapeId> BackendShapes => Backend.Shapes;
    internal abstract void MarkShapesDirty();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally
        {
            if (disposing) { _shapeOwners.Clear(); _shapeSlots.Clear(); _childOwners.Clear(); PhysicsServer.Service.UnregisterSceneObject(_rid); }
        }
    }

    private static uint LayerBit(int number)
    {
        if (number is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(number));
        return 1u << (number - 1);
    }
}

internal interface ICollisionGeometry
{
    Entity Node { get; }
    ReadOnlySpan<Shape> OwnerShapes { get; }
    bool Disabled { get; }
    bool OneWayCollision { get; }
    float OneWayCollisionMargin { get; }
    Vector2 OneWayCollisionDirection { get; }
}
