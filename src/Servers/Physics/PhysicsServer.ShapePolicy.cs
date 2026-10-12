namespace Electron2D;

public sealed partial class PhysicsServer
{
    internal void EnsureShapePolicyAccess(Shape shape, List<CollisionObject> scenes, List<PhysicsServerCollider> servers, bool observeBackend = true, bool resourceGeometry = false)
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
            foreach (var scene in scenes)
            {
                var space = scene is PhysicsBody body ? body.Space : ((Area)scene).Space;
                if (resourceGeometry && space is null) { scene.EnsureGeometryAuthoringAccess(); continue; }
                if (resourceGeometry && space is { IsOwnerThread: false })
                {
                    scene.EnsureGeometryAuthoringAccess(); space.EnsureResourceGeometryAccess(); continue;
                }
                if (observeBackend) EnsureSceneShapeAccess(scene, writing: true);
                else
                {
                    scene.Tree?.EnsureOwnerThread(); scene.EnsureGeometryAuthoringAccess();
                    space?.EnsureReleaseAccess(); space?.EnsureImplementationWriteAccess();
                }
            }
            foreach (var server in servers)
            {
                server.EnsureAuthoringAccess();
                if (resourceGeometry && server.Space is { IsOwnerThread: false } space)
                { space.EnsureResourceGeometryAccess(); continue; }
                if (observeBackend) server.Space?.EnsureQueryAccess(); else server.Space?.EnsureReleaseAccess();
                server.Space?.EnsureImplementationWriteAccess();
            }
        }
        finally { scenes.Clear(); servers.Clear(); }
    }
}
