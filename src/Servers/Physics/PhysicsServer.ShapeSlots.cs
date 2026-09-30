namespace Electron2D;

public sealed partial class PhysicsServer
{
    private (CollisionObject? Scene, PhysicsServerCollider? Server) ShapeOwners(RID rid, bool isArea, bool writing = false)
    {
        ThrowIfDisposed();
        var scene = ResolveSceneObject(rid);
        if (isArea ? scene is Area : scene is PhysicsBody)
        {
            EnsureSceneShapeAccess(scene!, writing);
            return (scene, null);
        }
        var server = GetCollider(rid, isArea);
        EnsureColliderSpaceAccessible(server);
        return (null, server);
    }

    private static void EnsureSceneShapeAccess(CollisionObject scene, bool writing = false)
    {
        scene.Tree?.EnsureOwnerThread();
        if (writing) scene.EnsurePhysicsParticipationChange();
        (scene is PhysicsBody body ? body.Space : ((Area)scene).Space)?.EnsureQueryAccess();
    }

    private List<CollisionObject> SceneShapeUsers(PhysicsServerShape shape)
    {
        var result = new List<CollisionObject>();
        lock (_registryGate)
            foreach (var weak in _sceneObjects.Values)
                if (weak.TryGetTarget(out var scene) && !scene.IsDisposed && scene.UsesServerShape(shape)) result.Add(scene);
        return result;
    }

    private RID GetSlotShape(RID rid, int index, bool isArea)
    {
        var owners = ShapeOwners(rid, isArea);
        if (owners.Scene is { } scene)
        {
            var slot = scene.GlobalShapeSlot(index);
            return slot.Shape.IsDisposed ? default : slot.ServerShape?.RID ?? slot.Shape.GetRID();
        }
        var shape = owners.Server!.GetShape(index);
        return shape.Geometry.IsDisposed ? default : shape.RID;
    }

    private Transform GetSlotTransform(RID rid, int index, bool isArea)
    {
        var owners = ShapeOwners(rid, isArea);
        return owners.Scene?.GlobalShapeSlot(index).Transform ?? owners.Server!.GetShapeTransform(index);
    }

    private void SetSlotShape(RID rid, int index, RID shape, bool isArea)
    {
        var owners = ShapeOwners(rid, isArea, writing: true);
        var resource = GetShape(shape);
        if (owners.Scene is { } scene)
        {
            var slot = scene.GlobalShapeSlot(index);
            if (ReferenceEquals(slot.ServerShape, resource)) return;
            slot.ServerShape = resource; scene.MarkShapesDirty();
        }
        else owners.Server!.SetShape(index, resource);
    }

    private void SetSlotTransform(RID rid, int index, Transform transform, bool isArea)
    {
        var owners = ShapeOwners(rid, isArea, writing: true);
        CollisionObject.ValidateOwnerTransform(transform);
        if (owners.Scene is { } scene)
        {
            var slot = scene.GlobalShapeSlot(index);
            if (slot.Transform == transform) return;
            slot.TransformOverride = transform; scene.MarkShapesDirty();
        }
        else owners.Server!.SetShapeTransform(index, transform);
    }

    private void ClearSlots(RID rid, bool isArea)
    {
        var owners = ShapeOwners(rid, isArea, writing: true);
        if (owners.Scene is { } scene) scene.ClearGlobalShapes(); else owners.Server!.ClearShapes();
    }

    /// <summary>Returns the current shape resource RID at a body's global logical index.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based global logical slot, including disabled slots.</param>
    /// <returns>The borrowed shape RID, or empty for a retained disposed resource slot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The owner RID is stale or not a body.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public RID BodyGetShape(RID body, int index) => GetSlotShape(body, index, isArea: false);

    /// <summary>Returns the current shape resource RID at an Area's global logical index.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="index">Zero-based global logical slot, including disabled slots.</param>
    /// <returns>The borrowed shape RID, or empty for a retained disposed resource slot.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The owner RID is stale or not an Area.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public RID AreaGetShape(RID area, int index) => GetSlotShape(area, index, isArea: true);

    /// <summary>Returns a body's effective slot-local translation and rotation.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <returns>The configured local pose, including a raw per-slot override.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The owner RID is stale or not a body.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public Transform BodyGetShapeTransform(RID body, int index) => GetSlotTransform(body, index, isArea: false);

    /// <summary>Returns an Area's effective slot-local translation and rotation.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <returns>The configured local pose, including a raw per-slot override.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The owner RID is stale or not an Area.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public Transform AreaGetShapeTransform(RID area, int index) => GetSlotTransform(area, index, isArea: true);

    /// <summary>Replaces one body's slot resource while retaining its index and local policies.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <param name="shape">A live owned or borrowed shape RID.</param>
    /// <remarks>The collider borrows geometry; it does not free either resource. Child node properties are unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">An owner or shape RID is stale or has the wrong kind.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public void BodySetShape(RID body, int index, RID shape) => SetSlotShape(body, index, shape, isArea: false);

    /// <summary>Replaces one Area's slot resource while retaining its index and local policies.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <param name="shape">A live owned or borrowed shape RID.</param>
    /// <remarks>The collider borrows geometry; it does not free either resource. Child node properties are unchanged.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">An owner or shape RID is stale or has the wrong kind.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public void AreaSetShape(RID area, int index, RID shape) => SetSlotShape(area, index, shape, isArea: true);

    /// <summary>Changes one body slot's local pose without moving the body or other slots.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <param name="transform">Finite translation/rotation with unit scale and zero skew.</param>
    /// <remarks>A later group or child transform edit replaces its slot overrides.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The pose is nonfinite/scaled/skewed or the RID has the wrong kind.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public void BodySetShapeTransform(RID body, int index, Transform transform) => SetSlotTransform(body, index, transform, isArea: false);

    /// <summary>Changes one Area slot's local pose without moving the Area or other slots.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <param name="transform">Finite translation/rotation with unit scale and zero skew.</param>
    /// <remarks>A later group or child transform edit replaces its slot overrides.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent.</exception>
    /// <exception cref="ArgumentException">The pose is nonfinite/scaled/skewed or the RID has the wrong kind.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public void AreaSetShapeTransform(RID area, int index, Transform transform) => SetSlotTransform(area, index, transform, isArea: true);

    /// <summary>Removes all body slots and fixtures without freeing their shape resources.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <remarks>Scene owner identities and child nodes remain; a later child resource edit can rebuild its group.</remarks>
    /// <exception cref="ArgumentException">The owner RID is stale or not a body.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public void BodyClearShapes(RID body) => ClearSlots(body, isArea: false);

    /// <summary>Removes all Area slots and sensor fixtures without freeing their shape resources.</summary>
    /// <param name="area">A live scene or server Area RID.</param>
    /// <remarks>Scene owner identities and child nodes remain; a later child resource edit can rebuild its group.</remarks>
    /// <exception cref="ArgumentException">The owner RID is stale or not an Area.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public void AreaClearShapes(RID area) => ClearSlots(area, isArea: true);

    /// <summary>Changes one body slot's one-way policy for contacts and body motion tests.</summary>
    /// <param name="body">A live scene or server body RID.</param>
    /// <param name="index">Zero-based global logical slot.</param>
    /// <param name="enable">Whether the slot permits directional pass-through.</param>
    /// <param name="margin">Finite nonnegative recovery depth in scene units.</param>
    /// <param name="direction">Finite slot-local direction, normalized once; null selects downward and zero remains zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is absent or margin is invalid.</exception>
    /// <exception cref="ArgumentException">Direction is nonfinite, or the RID is stale/wrong-kind.</exception>
    /// <exception cref="InvalidOperationException">The attached world is off-owner or stepping.</exception>
    public void BodySetShapeAsOneWayCollision(RID body, int index, bool enable, float margin, Vector2? direction = null)
    {
        var owners = ShapeOwners(body, isArea: false, writing: true);
        if (!float.IsFinite(margin) || margin < 0) throw new ArgumentOutOfRangeException(nameof(margin));
        var normalized = CollisionShape.NormalizeOneWayDirection(direction ?? Vector2.Down);
        if (owners.Scene is { } scene)
        {
            var slot = scene.GlobalShapeSlot(index);
            if (slot.OneWayOverride == enable && slot.MarginOverride == margin && slot.DirectionOverride == normalized) return;
            slot.OneWayOverride = enable; slot.MarginOverride = margin; slot.DirectionOverride = normalized;
            scene.MarkShapesDirty();
        }
        else owners.Server!.SetShapeOneWay(index, enable, margin, normalized);
    }
}
