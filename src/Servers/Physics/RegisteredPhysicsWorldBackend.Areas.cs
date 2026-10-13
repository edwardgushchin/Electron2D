namespace Electron2D;

internal sealed partial class RegisteredPhysicsWorldBackend
{
    private PhysicsBackendOverlap[] _overlaps = [];
    private PhysicsShapePair[] _validatedOverlaps = [];
    internal override void ScanAreas(ref List<Exception>? errors)
    {
        Enter(); try { _implementation.ScanAreas(); } finally { Exit(); }
        foreach (var area in Space.Areas)
        {
            try
            {
                var count = area.Monitoring ? ReadOverlaps(area.GetRID(), area.CollisionMask) : 0;
                area.BeginOverlapScan();
                for (var i = 0; i < count; i++) area.Observe(_validatedOverlaps[i]);
                Space.CommitAreaScan(area);
            }
            catch (Exception error) { if (HasFailure) throw; (errors ??= []).Add(error); }
        }
        foreach (var area in Space.Areas)
            try { ScanMonitor(area.GetRID()); } catch (Exception error) { if (HasFailure) throw; (errors ??= []).Add(error); }
        foreach (var area in Space.ServerColliders)
            if (area.IsArea)
                try { ScanMonitor(area.RID); } catch (Exception error) { if (HasFailure) throw; (errors ??= []).Add(error); }
    }
    private void ScanMonitor(RID rid)
    {
        var runtime = PhysicsServer.Service.FindAreaRuntime(rid);
        if (runtime is not { Monitoring: true }) return;
        var count = ReadOverlaps(rid, runtime.Mask);
        runtime.Pairs.Begin();
        for (var i = 0; i < count; i++)
        {
            var pair = _validatedOverlaps[i];
            if (pair.IsArea ? runtime.AreaCallback is not null : runtime.BodyCallback is not null) runtime.Pairs.Observe(pair, rid);
        }
        runtime.Changes.Clear(); runtime.Pairs.Commit(runtime.Changes); Space.QueueMonitorChanges(runtime);
    }
    private int ReadOverlaps(RID area, uint mask)
    {
        Space.BeginContactResultPublication(); Enter();
        try
        {
            var capacity = _implementation.GetAreaOverlapCount(area);
            if (capacity < 0) throw new InvalidOperationException("An Area returned a negative overlap count.");
            if (_overlaps.Length < capacity) { Array.Resize(ref _overlaps, capacity); Array.Resize(ref _validatedOverlaps, capacity); }
            var count = _implementation.ReadAreaOverlaps(area, _overlaps.AsSpan(0, capacity));
            if ((uint)count > (uint)capacity) throw new InvalidOperationException("An Area returned too many overlaps.");
            for (var i = 0; i < count; i++)
            {
                var p = _overlaps[i];
                var local = PhysicsServer.Service.CaptureResultCollider(area, p.LocalShape, contactResult: true);
                var other = PhysicsServer.Service.CaptureResultCollider(p.Other, p.RemoteShape, contactResult: true);
                if (!ReferenceEquals(local.Space, Space) || !ReferenceEquals(other.Space, Space) || area == p.Other)
                    throw new InvalidOperationException("An Area overlap belongs to a different space or its own receiver.");
                if (!PhysicsServer.Service.IsColliderShapeEnabled(area, p.LocalShape, contactResult: true) ||
                    !PhysicsServer.Service.IsColliderShapeEnabled(p.Other, p.RemoteShape, contactResult: true))
                    throw new InvalidOperationException("An Area overlap belongs to a disabled logical shape.");
                var scene = other.Scene; var isArea = PhysicsServer.Service.IsAreaCollider(p.Other);
                var owners = isArea ? PhysicsServer.Service.ResolveAreaOwners(p.Other) : (Scene: (CollisionObject?)null, Server: (PhysicsServerCollider?)null);
                var layer = scene?.CollisionLayer ?? (isArea ? owners.Server!.CollisionLayer : PhysicsServer.Service.ResolveBodyOwners(p.Other).Server!.CollisionLayer);
                var monitorable = !isArea || scene is Area remote && remote.Monitorable || scene is null && owners.Server!.Monitorable;
                if ((layer & mask) == 0 || !monitorable) throw new InvalidOperationException("An Area overlap violates collision filters or monitorability.");
                _validatedOverlaps[i] = new(p.Other, other.Identity, isArea, p.RemoteShape, p.LocalShape);
            }
            return count;
        }
        finally { Exit(); Space.EndContactResultPublication(); }
    }
}
