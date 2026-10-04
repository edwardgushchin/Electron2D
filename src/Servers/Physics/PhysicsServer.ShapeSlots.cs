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

    internal RID BodyGetShapeCore(RID body, int index) => GetSlotShape(body, index, isArea: false);

    internal RID AreaGetShapeCore(RID area, int index) => GetSlotShape(area, index, isArea: true);

    internal Transform BodyGetShapeTransformCore(RID body, int index) => GetSlotTransform(body, index, isArea: false);

    internal Transform AreaGetShapeTransformCore(RID area, int index) => GetSlotTransform(area, index, isArea: true);

    internal void BodySetShapeCore(RID body, int index, RID shape) => SetSlotShape(body, index, shape, isArea: false);

    internal void AreaSetShapeCore(RID area, int index, RID shape) => SetSlotShape(area, index, shape, isArea: true);

    internal void BodySetShapeTransformCore(RID body, int index, Transform transform) => SetSlotTransform(body, index, transform, isArea: false);

    internal void AreaSetShapeTransformCore(RID area, int index, Transform transform) => SetSlotTransform(area, index, transform, isArea: true);

    internal void BodyClearShapesCore(RID body) => ClearSlots(body, isArea: false);

    internal void AreaClearShapesCore(RID area) => ClearSlots(area, isArea: true);

    internal void BodySetShapeAsOneWayCollisionCore(RID body, int index, bool enable, float margin, Vector2? direction = null)
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
