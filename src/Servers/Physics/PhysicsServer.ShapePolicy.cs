namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal void EnsureShapePolicyAccess(Shape shape, List<CollisionObject> scenes, List<PhysicsServerCollider> servers)
    {
        ThrowIfDisposed();
        try
        {
            lock (_registryGate)
            {
                foreach (var weak in _sceneObjects.Values)
                    if (weak.TryGetTarget(out var scene) && !scene.IsDisposed)
                    {
                        var slots = scene.ShapeSlots;
                        for (var i = 0; i < slots.Count; i++)
                            if (ReferenceEquals(slots[i].Shape, shape)) { scenes.Add(scene); break; }
                    }
                foreach (var server in _serverColliders.Values)
                    for (var i = 0; i < server.ShapeCount; i++)
                        if (ReferenceEquals(server.GetShape(i).Geometry, shape)) { servers.Add(server); break; }
            }
            // Selected access hooks run outside the resource registry lock.
            foreach (var scene in scenes) EnsureSceneShapeAccess(scene, writing: true);
            foreach (var server in servers)
            {
                server.EnsureAuthoringAccess();
                server.Space?.EnsureQueryAccess();
                server.Space?.EnsureImplementationWriteAccess();
            }
        }
        finally { scenes.Clear(); servers.Clear(); }
    }
}
