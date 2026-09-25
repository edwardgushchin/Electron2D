using Box2D.NET;

namespace Electron2D;

/// <summary>A spatial scene object that owns collision filtering for physics shapes.</summary>
public abstract class CollisionObject : Entity
{
    private static readonly PropertyDescriptor[] CollisionProperties =
    [
        new PropertyDescriptor<CollisionObject, uint>(nameof(CollisionLayer), node => node.CollisionLayer, (node, value) => node.CollisionLayer = value, _ => 1u, stored: true),
        new PropertyDescriptor<CollisionObject, uint>(nameof(CollisionMask), node => node.CollisionMask, (node, value) => node.CollisionMask = value, _ => 1u, stored: true)
    ];

    private uint _collisionLayer = 1;
    private uint _collisionMask = 1;
    private readonly RID _rid;

    /// <summary>Creates an object in collision layer one with mask one.</summary>
    protected CollisionObject() => _rid = PhysicsServer2D.Instance.RegisterSceneObject(this);

    /// <summary>Gets the stable server identity of this collision object.</summary>
    /// <returns>A nonempty RID unchanged by fixture rebuilds or scene attachment.</returns>
    public RID GetRID()
    {
        ThrowIfDisposed();
        return _rid;
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

    internal virtual void OnCollisionFilterChanged() { }

    internal abstract IReadOnlyList<B2ShapeId> BackendShapes { get; }
    internal abstract void AttachShape(ICollisionGeometry shape);
    internal abstract void DetachShape(ICollisionGeometry shape);
    internal abstract void MarkShapesDirty();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally { if (disposing) PhysicsServer2D.Instance.UnregisterSceneObject(_rid); }
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
    bool IsActive { get; }
    ulong GeometryRevision { get; }
    OneWayContactData? OneWayContact { get; }
    void AppendToBody(B2BodyId bodyID, in B2ShapeDef definition, List<B2ShapeId> fixtures);
}
