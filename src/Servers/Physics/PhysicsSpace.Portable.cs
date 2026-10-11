namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private readonly List<PhysicsSnapshotMap> _snapshotMaps = [];
    internal bool PortableColdStep;
    private readonly HashSet<(ulong, int, int, ulong, int, int)> _portableOneWayKeys = [];
    internal List<PhysicsJointRuntime> SnapshotJoints => _jointRuntimes;
    internal void RegisterSnapshotMap(PhysicsSnapshotMap map) => _snapshotMaps.Add(map);
    internal void UnregisterSnapshotMap(PhysicsSnapshotMap map) => _snapshotMaps.Remove(map);
    internal PhysicsReplayEntry? FindSnapshotObject(RID rid)
    {
        foreach (var body in _bodies) if (body.PhysicsRID == rid) return new(body.Backend, body, null);
        foreach (var area in _areas) if (area.PhysicsRID == rid) return new(area.Backend, area, null);
        foreach (var body in _serverColliders) if (body.RID == rid) return new(body.Backend, null, body);
        return null;
    }
    internal PhysicsJointRuntime? FindSnapshotJoint(RID rid)
    {
        foreach (var joint in _jointRuntimes) if (joint.RID == rid) return joint;
        return null;
    }
    internal void PreparePortableCapture()
    {
        EnsureCheckpointAccess(); PrepareForQuery(); foreach (var joint in _joints) joint.PrepareBackend();
        BeginBackendCallback();
        try { _backend.PreparePortableCapture(); }
        finally { EndExtensionCallback(); }
    }
    internal void BeginPortableRestore()
    {
        EnsureCheckpointAccess(); ResetDebugContacts(); _backend.BeginPortableRestore();
    }
    internal void FailPortableRestore(Exception error) => _checkpointFailure = error;
    internal void CompletePortableRestore(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> pairs, ulong tick, float step)
    {
        Tick = tick; LastStep = step; _backend.CompletePortableRestore(map, pairs, tick);
    }
    internal void CompleteGPUPortableRestore()
    {
        _gpuWakePending = false; InvalidateGPUStates(wake: false); PublishGPU();
        PhysicsServer.Service.PublishStatistics(this, _backend.ReadStatistics());
    }
    internal void CapturePortableOneWays(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> destination)
    {
        destination.Clear(); _backend.CapturePortableOneWays(map, destination);
        try { ValidatePortableOneWays(map, destination); }
        catch (InvalidDataException error) { throw new InvalidOperationException("A selected implementation returned invalid one-way continuation.", error); }
    }
    internal void ValidatePortableOneWays(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> pairs)
    {
        _portableOneWayKeys.Clear(); _portableOneWayKeys.EnsureCapacity(pairs.Count);
        foreach (var pair in pairs)
        {
            var first = map.Collider(pair.A).Backend; var second = map.Collider(pair.B).Backend;
            PhysicsSnapshotReader.Require(first != second && pair.ShapeA >= 0 && pair.ShapeB >= 0 && pair.PieceA >= 0 && pair.PieceB >= 0);
            var key = pair.A < pair.B ? (pair.A, pair.ShapeA, pair.PieceA, pair.B, pair.ShapeB, pair.PieceB) : (pair.B, pair.ShapeB, pair.PieceB, pair.A, pair.ShapeA, pair.PieceA);
            PhysicsSnapshotReader.Require(_portableOneWayKeys.Add(key));
            try
            {
                PhysicsSnapshotReader.Require(PhysicsServer.Service.IsColliderShapeEnabled(first.RID, pair.ShapeA) &&
                    PhysicsServer.Service.IsColliderShapeEnabled(second.RID, pair.ShapeB));
                first.ValidatePortablePiece(pair.ShapeA, pair.PieceA); second.ValidatePortablePiece(pair.ShapeB, pair.PieceB);
            }
            catch (Exception error) when (error is ArgumentException or ObjectDisposedException)
            { throw new InvalidDataException("Snapshot references an unavailable collision shape or piece.", error); }
        }
    }
}
