namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private readonly GPUPhysicsBodyStore.ShapeQuery[] _gpuAreaQuery = new GPUPhysicsBodyStore.ShapeQuery[1];
    private GPUPhysicsBodyStore.ShapeQueryHit[] _gpuAreaHits = [];

    internal void ScanGPUAreas()
    {
        foreach (var area in _areas)
            ScanGPUArea(area.Backend, area, PhysicsServer.Service.FindAreaRuntime(area.PhysicsRID));
        foreach (var area in _serverColliders)
            if (area.IsArea) ScanGPUArea(area.Backend, null, PhysicsServer.Service.FindAreaRuntime(area.RID));
    }
    private void ScanGPUArea(PhysicsColliderBackend backend, Area? scene, PhysicsAreaRuntime? runtime)
    {
        scene?.BeginOverlapScan(); runtime?.Pairs.Begin();
        var sceneMonitoring = scene?.Monitoring == true;
        if (sceneMonitoring || runtime?.Monitoring == true)
        {
            var capacity = GPUStore!.ShapeCount;
            if (_gpuAreaHits.Length < capacity) Array.Resize(ref _gpuAreaHits, Math.Max(8, capacity * 2));
            var transform = backend.GetTransform();
            Span<ulong> exclude = stackalloc ulong[1] { (ulong)backend.RID.GetID() };
            Span<int> counts = stackalloc int[1];
            foreach (var local in backend.GPUShapes)
            {
                _gpuAreaQuery[0] = new(local.Query!, transform * local.Pose, Mask: backend.GPUMask, Areas: true,
                    Limit: capacity, ExclusionCount: 1);
                GPUStore.QueryShapes(_gpuAreaQuery, exclude, counts, _gpuAreaHits);
                foreach (ref readonly var hit in _gpuAreaHits.AsSpan(0, counts[0]))
                {
                    if (!_gpuColliders.TryGetValue((int)hit.Body, out var other) || other.GPUHandle.Generation != hit.BodyGeneration) continue;
                    if (other.GPUSensor)
                    {
                        var owners = PhysicsServer.Service.ResolveAreaOwners(other.RID);
                        if (!(owners.Scene?.Monitorable ?? owners.Server!.Monitorable)) continue;
                    }
                    var pair = new PhysicsShapePair(other.RID, other.ObjectIdentity, other.GPUSensor, hit.LogicalShape, local.Slot);
                    if (sceneMonitoring) scene!.Observe(pair);
                    if (other.GPUSensor ? runtime?.AreaCallback is not null : runtime?.BodyCallback is not null) runtime!.Pairs.Observe(pair);
                }
            }
        }
        scene?.CommitOverlapScan(_overlapEvents);
        if (runtime is not null) { runtime.Changes.Clear(); runtime.Pairs.Commit(runtime.Changes); QueueMonitorChanges(runtime); }
    }
}
