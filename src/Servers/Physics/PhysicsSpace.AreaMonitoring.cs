using Box2D.NET;
using static Box2D.NET.B2Shapes;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private readonly List<ServerAreaEvent> _serverAreaEvents = [];
    private bool _dispatchingServerAreas;
    private readonly record struct ServerAreaEvent(PhysicsAreaRuntime Receiver, ulong Generation, PhysicsShapePairChange Change);

    private void ScanAreaMonitors()
    {
        foreach (var area in _areas)
        {
            var runtime = PhysicsServer.Service.FindAreaRuntime(area.PhysicsRID);
            if (runtime is { Monitoring: true }) ScanAreaMonitor(runtime, area.BackendShapes);
        }
        foreach (var area in _serverColliders)
        {
            if (!area.IsArea) continue;
            var runtime = PhysicsServer.Service.FindAreaRuntime(area.RID);
            if (runtime is { Monitoring: true }) ScanAreaMonitor(runtime, area.BackendShapes);
        }
    }

    private void ScanAreaMonitor(PhysicsAreaRuntime receiver, IReadOnlyList<B2ShapeId> localShapes)
    {
        receiver.Pairs.Begin();
        if (receiver.BodyCallback is not null)
            foreach (var body in _bodies)
                if ((receiver.Mask & body.CollisionLayer) != 0)
                    ScanMonitorPairs(receiver, localShapes, body.PhysicsRID, body, false, body.BackendShapes);
        if (receiver.AreaCallback is not null)
            foreach (var area in _areas)
                if (area.PhysicsRID != receiver.RID && area.Monitorable && (receiver.Mask & area.CollisionLayer) != 0)
                    ScanMonitorPairs(receiver, localShapes, area.PhysicsRID, area, true, area.BackendShapes);
        foreach (var collider in _serverColliders)
        {
            if (collider.RID == receiver.RID || (receiver.Mask & collider.CollisionLayer) == 0) continue;
            if (collider.IsArea ? receiver.AreaCallback is null || !collider.Monitorable : receiver.BodyCallback is null) continue;
            ScanMonitorPairs(receiver, localShapes, collider.RID, null, collider.IsArea, collider.BackendShapes);
        }
        receiver.Changes.Clear(); receiver.Pairs.Commit(receiver.Changes); QueueMonitorChanges(receiver);
    }

    private void ScanMonitorPairs(PhysicsAreaRuntime receiver, IReadOnlyList<B2ShapeId> localShapes,
        RID otherRID, CollisionObject? other, bool isArea, IReadOnlyList<B2ShapeId> otherShapes)
    {
        for (var localIndex = 0; localIndex < localShapes.Count; localIndex++)
        {
            var local = localShapes[localIndex];
            var localTag = b2Shape_GetUserData(local).GetRef<PhysicsFixtureTag>();
            if (localTag is null) continue;
            for (var remoteIndex = 0; remoteIndex < otherShapes.Count; remoteIndex++)
            {
                var remote = otherShapes[remoteIndex];
                var tag = b2Shape_GetUserData(remote).GetRef<PhysicsFixtureTag>();
                if (tag is not null && ShapePairOverlaps(local, remote))
                    receiver.Pairs.Observe(new(otherRID, tag.ObjectIdentity, isArea, tag.ShapeIndex, localTag.ShapeIndex));
            }
        }
    }

    private void QueueMonitorChanges(PhysicsAreaRuntime receiver)
    {
        foreach (var change in receiver.Changes)
            if (!change.ObjectEvent) _serverAreaEvents.Add(new(receiver, receiver.Generation, change));
        receiver.Changes.Clear();
    }

    private void ForgetAreaMonitors(RID otherRID)
    {
        foreach (var area in _areas) Forget(PhysicsServer.Service.FindAreaRuntime(area.PhysicsRID), otherRID);
        foreach (var area in _serverColliders)
            if (area.IsArea) Forget(PhysicsServer.Service.FindAreaRuntime(area.RID), otherRID);
    }

    private void Forget(PhysicsAreaRuntime? receiver, RID otherRID)
    {
        if (receiver is null) return;
        receiver.Changes.Clear(); receiver.Pairs.Forget(otherRID, receiver.Changes); QueueMonitorChanges(receiver);
    }

    private void DispatchAreaMonitors()
    {
        if (_dispatchingServerAreas || _serverAreaEvents.Count == 0) return;
        _dispatchingServerAreas = true; List<Exception>? errors = null;
        try
        {
            for (var index = 0; index < _serverAreaEvents.Count; index++)
            {
                var item = _serverAreaEvents[index]; var receiver = item.Receiver;
                if (PhysicsServer.Service.FindAreaRuntime(receiver.RID) != receiver || receiver.Generation != item.Generation ||
                    receiver.Space != this || item.Change.Entered && !receiver.Pairs.Contains(item.Change.Pair)) continue;
                try { receiver.Raise(item.Change); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        finally { _serverAreaEvents.Clear(); _dispatchingServerAreas = false; }
        if (errors is not null) throw new AggregateException("Area monitor callbacks failed.", errors);
    }
}
