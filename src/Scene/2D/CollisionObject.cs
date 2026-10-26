namespace Electron2D;

/// <summary>A spatial scene object that owns collision filtering and motion-recovery priority for physics shapes.</summary>
/// <remarks>Unchanged child geometry and policies retain their backend state on tree entry.
/// Child entry clears server slot overrides and applies changed descriptors before the next interval.</remarks>
public abstract partial class CollisionObject : Entity
{
    private static readonly PropertyDescriptor[] CollisionProperties =
    [
        new PropertyDescriptor<CollisionObject, bool>(nameof(InputPickable), node => node.InputPickable, (node, value) => node.InputPickable = value, node => node is not PhysicsBody, stored: true),
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
    /// Area retains this property but does not obstruct body motion. Edits preserve sleep and fixture identity.
    /// Failed selected publication retains the new value; live query or nonzero-step preparation retries it before numerical use.</remarks>
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
    /// Removal retains RID and shape owners, invalidates live body views and reports peer overlap exits.
    /// A partial selected Body role-publication error retains the committed policy and retries complete role preparation before numerical use.</remarks>
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
            if (value is not CollisionDisableMode.Remove and not CollisionDisableMode.MakeStatic and not CollisionDisableMode.KeepActive) throw new ArgumentOutOfRangeException(nameof(value));
            EnsurePhysicsParticipationChange();
            if (this is PhysicsBody body) body.PreparePendingServerMode(writing: true);
            if (_disableMode == value) return;
            _disableMode = value;
            if (IsInsideTree) UpdatePhysicsParticipation();
        }
    }

    /// <summary>Gets or sets the 32-bit collision category mask.</summary>
    /// <remarks>Assignments update existing fixtures without rebuilding geometry. Body assignments also wake the body and contact neighbors, including for unchanged bits.
    /// A failed numerical update preserves authored bits. Queries and the next nonzero step retry the current effective filter before numerical publication.</remarks>
    /// <value>One by default.</value>
    public uint CollisionLayer
    {
        get { ThrowIfDisposed(); return _collisionLayer; }
        set => SetCollisionFilter(value, _collisionMask);
    }

    /// <summary>Gets or sets the 32-bit categories accepted for collision.</summary>
    /// <remarks>Each dynamic body responds when its own mask includes the other layer; either direction admits the pair.
    /// Body motion queries use only the moving mask against target layers. Body assignments wake the body and contact neighbors.
    /// Changed contact and overlap membership is published on the next physical step.
    /// A failed numerical update preserves authored bits. Queries and the next nonzero step retry the current effective filter before numerical publication.</remarks>
    /// <value>One by default.</value>
    public uint CollisionMask
    {
        get { ThrowIfDisposed(); return _collisionMask; }
        set => SetCollisionFilter(_collisionLayer, value);
    }

    private void SetCollisionFilter(uint layer, uint mask)
    {
        EnsureMutable(); EnsurePhysicsParticipationChange();
        OnCollisionFilterChanged(layer, mask);
        _collisionLayer = layer; _collisionMask = mask;
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
    /// <remarks>Canvas identity and enabled/disabled participation requests from the current processing group
    /// publish on the scene owner after the group joins. Numeric notifications do not change effective ProcessMode.</remarks>
    protected override void OnNotification(int what)
    {
        var reversed = IsNotificationReversed;

        if (!reversed) base.OnNotification(what);
        if (what == NotificationEnterCanvas) PublishCanvasIdentity(GetCanvasLayerNode()?.InstanceID ?? 0);
        else if (what == NotificationExitCanvas) PublishCanvasIdentity(0);
        if (what is NotificationDisabled or NotificationEnabled && IsInsideTree)
            PublishPhysicsParticipation();

        if (reversed) base.OnNotification(what);
    }

    internal void PublishCanvasIdentity(ulong id)
    {
        Tree?.EnsureProcessingAccess(this);
        if (Tree is { IsProcessingWorkerStage: true } tree) { tree.QueueCanvasIdentity(this, id); return; }
        Backend.CanvasInstanceID = id;
    }
    internal void PublishPhysicsParticipation()
    {
        Tree?.EnsureProcessingAccess(this);
        if (Tree is { IsProcessingWorkerStage: true } tree) { tree.QueueScenePublication(this, SceneTree.ScenePublicationKind.PhysicsParticipation); return; }
        UpdatePhysicsParticipation();
    }

    internal void EnsurePhysicsParticipationChange()
    {
        EnsureGeometryAuthoringAccess();
        Tree?.EnsurePhysicsParticipationChange();
        Backend.Space?.EnsureQueryAccess();
        Backend.Space?.EnsureImplementationWriteAccess();
    }

    private bool _validatingSpaceGeometry;
    private static int _spaceGeometryBorrows;
    internal static bool HasSpaceGeometryBorrows => Volatile.Read(ref _spaceGeometryBorrows) != 0;
    internal void EnsureGeometryAuthoringAccess()
    {
        if (_validatingSpaceGeometry) throw new InvalidOperationException("Scene collider authoring cannot change while its space-transfer geometry is borrowed.");
        foreach (var slot in _shapeSlots)
            if (slot.ServerShape is { } server) server.EnsureBindingAccess();
            else slot.Shape.EnsureServerBindingAccess();
    }
    internal void ValidateSpaceGeometry(PhysicsSpace space)
    {
        EnsureGeometryAuthoringAccess(); _validatingSpaceGeometry = true;
        Interlocked.Increment(ref _spaceGeometryBorrows);
        try
        {
            foreach (var slot in ShapeSlots)
                if (!(slot.DisabledOverride ?? slot.Owner.Disabled)) PhysicsShapeBackend.RequireSupported(space, slot.Shape);
        }
        finally { _validatingSpaceGeometry = false; Interlocked.Decrement(ref _spaceGeometryBorrows); }
    }

    internal abstract void UpdatePhysicsParticipation();

    internal virtual void OnCollisionFilterChanged(uint layer, uint mask) { }

    internal abstract void MarkShapesDirty();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally
        {
            if (disposing) { ClearPhysicsEventHandlers(); _shapeOwners.Clear(); _shapeSlots.Clear(); _childOwners.Clear(); PhysicsServer.Service.UnregisterSceneObject(_rid); }
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
