namespace Electron2D;

internal sealed class PhysicsAreaRuntime(RID rid)
{
    internal RID RID { get; } = rid;
    internal Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int>? BodyCallback;
    internal Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int>? AreaCallback;
    internal readonly PhysicsShapePairTracker Pairs = new();
    internal readonly List<PhysicsShapePairChange> Changes = [];
    internal ulong Generation;
    private bool _dispatching;

    internal (Area? Scene, PhysicsServerCollider? Server) Owners => PhysicsServer.Instance.ResolveAreaOwners(RID);
    internal PhysicsSpace? Space { get { var owners = Owners; return owners.Scene?.Space ?? owners.Server?.Space; } }
    internal uint Layer { get { var owners = Owners; return owners.Scene?.CollisionLayer ?? owners.Server!.CollisionLayer; } }
    internal uint Mask { get { var owners = Owners; return owners.Scene?.CollisionMask ?? owners.Server!.CollisionMask; } }
    internal bool Monitoring => BodyCallback is not null || AreaCallback is not null;
    internal bool Monitorable { get { var owners = Owners; return owners.Scene?.Monitorable ?? owners.Server!.Monitorable; } }

    internal void EnsureAccess(bool writing = false)
    {
        var owners = Owners;
        owners.Scene?.Tree?.EnsureOwnerThread();
        owners.Scene?.EnsurePhysicsParticipationChange();
        Space?.EnsureQueryAccess();
        if (writing)
        {
            if (_dispatching) throw new InvalidOperationException("Area monitor configuration cannot change from its callback.");
            owners.Scene?.EnsureMonitorConfigurationChange();
        }
    }

    internal void SetCallback(Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int>? callback, bool area)
    {
        EnsureAccess(true);
        if (area) AreaCallback = callback; else BodyCallback = callback;
        Reset();
    }

    internal void Reset() { Pairs.Clear(); Changes.Clear(); Generation++; }

    internal void Raise(PhysicsShapePairChange change)
    {
        if (change.ObjectEvent) return;
        var callback = change.Pair.IsArea ? AreaCallback : BodyCallback;
        if (callback is null) return;
        _dispatching = true;
        try
        {
            callback(change.Entered ? PhysicsServer.AreaBodyStatus.Added : PhysicsServer.AreaBodyStatus.Removed,
                change.Pair.RID, change.Pair.InstanceID, change.Pair.OtherShape, change.Pair.LocalShape);
        }
        finally { _dispatching = false; }
    }
}
