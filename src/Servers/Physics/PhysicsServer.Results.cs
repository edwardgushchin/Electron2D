namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal bool IsAreaCollider(RID collider)
    {
        var scene = ResolveSceneObject(collider);
        if (scene is not null) return scene is Area;
        lock (_registryGate) return _serverColliders.TryGetValue(collider, out var server) && server.IsArea;
    }
    internal bool IsColliderShapeEnabled(RID collider, int slot, bool contactResult = false)
    {
        var owners = ShapeOwners(collider, IsAreaCollider(collider), contactResult: contactResult);
        return owners.Scene?.GlobalShapeSlot(slot).Active ?? !owners.Server!.IsShapeDisabled(slot);
    }
    internal MotionResultData ValidateMotionResult(PhysicsSpace space, RID body, in MotionResultData data,
        RID[] excludedBodies, ulong[] excludedObjects)
    {
        if (data.OwnerRID != body || !data.Travel.IsFinite() || !data.Remainder.IsFinite() ||
            !float.IsFinite(data.SafeFraction) || !float.IsFinite(data.UnsafeFraction) ||
            data.SafeFraction < 0 || data.SafeFraction > data.UnsafeFraction || data.UnsafeFraction > 1)
            throw new InvalidOperationException("A selected motion query returned invalid displacement or fractions.");
        if (!data.Collided)
        {
            if (data.SafeFraction != 1 || data.UnsafeFraction != 1 || data.Remainder != Vector2.Zero)
                throw new InvalidOperationException("An unobstructed motion query must complete its displacement.");
            return new(body, default, 0, 0, 0, default, default, 0, default,
                data.Travel, data.Remainder, data.SafeFraction, data.UnsafeFraction, false);
        }
        if (data.ColliderRID == body || !data.Point.IsFinite() || !data.Normal.IsFinite() ||
            !data.ColliderVelocity.IsFinite() || !float.IsFinite(data.Depth) || data.Depth < 0)
            throw new InvalidOperationException("A selected motion query returned invalid collision geometry.");
        try
        {
            var local = CaptureResultCollider(body, data.LocalShape, bodyOnly: true);
            var remote = CaptureResultCollider(data.ColliderRID, data.ColliderShape, bodyOnly: true);
            var owners = ShapeOwners(body, isArea: false);
            var mask = owners.Scene is PhysicsBody scene ? scene.EffectiveCollisionMask : owners.Server!.CollisionMask;
            var enabled = owners.Scene?.GlobalShapeSlot(data.LocalShape).Active ?? !owners.Server!.IsShapeDisabled(data.LocalShape);
            if (!ReferenceEquals(local.Space, space) || !enabled || BodiesExcepted(body, data.ColliderRID) ||
                remote.Identity.ID != 0 && excludedObjects.AsSpan().Contains(remote.Identity.ID))
                throw new InvalidOperationException("A selected motion query returned an excluded body or disabled moving shape.");
            ValidateQueryResult(data.ColliderRID, data.ColliderShape, space, mask, bodies: true, areas: false, canvas: null, excludedBodies);
            return data with
            {
                ColliderID = remote.Identity.ID,
                ColliderIdentity = remote.Identity,
                LocalSlotIdentity = HistoryShapeIdentity(body, data.LocalShape),
                OtherSlotIdentity = HistoryShapeIdentity(data.ColliderRID, data.ColliderShape)
            };
        }
        catch (Exception error) when (error is ArgumentException or ObjectDisposedException)
        {
            throw new InvalidOperationException("A selected motion query returned an invalid body or logical shape identity.", error);
        }
    }

    internal object? HistoryShapeIdentity(RID rid, int index)
    {
        // Contact workers sample immutable authored slots before the owner joins and permits edits.
        if (ResolveSceneObject(rid) is { } scene)
            return (uint)index < (uint)scene.ShapeSlots.Count ? scene.ShapeSlots[index].HistoryIdentity : null;
        lock (_registryGate)
            return _serverColliders.TryGetValue(rid, out var server) && (uint)index < (uint)server.ShapeCount ? server.GetShapeHistoryIdentity(index) : null;
    }

    internal (CollisionObject? Scene, ObjectIdentity Identity, PhysicsSpace? Space, long Attachment) CaptureResultCollider(RID collider, int shapeIndex, bool bodyOnly = false, bool contactResult = false)
    {
        ThrowIfDisposed();
        var scene = ResolveSceneObject(collider);
        var area = scene is Area;
        if (scene is null)
        {
            lock (_registryGate)
                if (_serverColliders.TryGetValue(collider, out var server)) area = server.IsArea;
        }
        if (bodyOnly && area) throw new ArgumentException("Motion results require a body collider.", nameof(collider));
        var owners = ShapeOwners(collider, area, contactResult: contactResult);
        var count = owners.Scene?.ShapeSlots.Count ?? owners.Server!.ShapeCount;
        if ((uint)shapeIndex >= (uint)count) throw new ArgumentOutOfRangeException(nameof(shapeIndex));
        var shape = owners.Scene?.GlobalShapeSlot(shapeIndex).Shape ?? owners.Server!.GetShape(shapeIndex).Geometry;
        if (shape.IsDisposed) throw new ObjectDisposedException(nameof(Shape));
        var backend = owners.Scene?.Backend ?? owners.Server!.Backend;
        return (owners.Scene, backend.ObjectIdentity, backend.Space, backend.AttachmentVersion);
    }

    internal void ValidateQueryResult(RID rid, int slot, PhysicsSpace space, uint mask, bool bodies, bool areas, ulong? canvas, RID[] excluded)
    {
        var captured = CaptureResultCollider(rid, slot);
        var area = captured.Scene is Area;
        if (captured.Scene is null)
        {
            lock (_registryGate) area = _serverColliders[rid].IsArea;
        }
        var owners = ShapeOwners(rid, area);
        var backend = owners.Scene?.Backend ?? owners.Server!.Backend;
        var layer = owners.Scene is PhysicsBody body ? body.EffectiveCollisionLayer : owners.Scene?.CollisionLayer ?? owners.Server!.CollisionLayer;
        var active = owners.Scene?.GlobalShapeSlot(slot).Active ??
            (!owners.Server!.IsShapeDisabled(slot) && owners.Server.GetShape(slot).Geometry.HasServerData);
        if (!ReferenceEquals(captured.Space, space) || !active || (area ? !areas : !bodies) ||
            (layer & mask) == 0 || excluded.AsSpan().Contains(rid) || canvas.HasValue && backend.CanvasInstanceID != canvas.Value)
            throw new InvalidOperationException("A query hook returned a collider outside its world or filters.");
    }
}
