namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private readonly List<ServerAreaEvent> _serverAreaEvents = [];
    private bool _dispatchingServerAreas;
    private readonly record struct ServerAreaEvent(PhysicsAreaRuntime Receiver, ulong Generation, PhysicsShapePairChange Change);

    internal void QueueMonitorChanges(PhysicsAreaRuntime receiver)
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
