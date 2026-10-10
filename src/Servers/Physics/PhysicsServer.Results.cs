namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal (CollisionObject? Scene, ObjectIdentity Identity, PhysicsSpace? Space) CaptureResultCollider(RID collider, int shapeIndex, bool bodyOnly = false)
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
        return (owners.Scene, backend.ObjectIdentity, backend.Space);
    }
}
