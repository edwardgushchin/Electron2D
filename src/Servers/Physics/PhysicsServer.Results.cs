namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal (CollisionObject? Scene, ObjectIdentity Identity, PhysicsSpace? Space, long Attachment) CaptureResultCollider(RID collider, int shapeIndex, bool bodyOnly = false)
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
        var owners = ShapeOwners(collider, area);
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
        var active = owners.Scene?.GlobalShapeSlot(slot).Active ?? !owners.Server!.IsShapeDisabled(slot);
        if (!ReferenceEquals(captured.Space, space) || !active || (area ? !areas : !bodies) ||
            (layer & mask) == 0 || excluded.AsSpan().Contains(rid) || canvas.HasValue && backend.CanvasInstanceID != canvas.Value)
            throw new InvalidOperationException("A query hook returned a collider outside its world or filters.");
    }
}
