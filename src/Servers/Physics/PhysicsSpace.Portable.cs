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
        if (GPUStore is not { } gpu) return;
        foreach (var body in _bodies) GPUCollider(body.Backend).PrepareGPUParameters(body.Runtime);
        uint order = 0;
        foreach (var area in _areas) gpu.SetAreaFields(GPUCollider(area.Backend).GPUHandle, GPUFields(area.Fields), order++);
        foreach (var body in _serverColliders)
            if (body.IsArea) gpu.SetAreaFields(GPUCollider(body.Backend).GPUHandle, GPUFields(body.AreaFields!), order++);
            else GPUCollider(body.Backend).PrepareGPUParameters(body.Runtime);
        foreach (var joint in _jointRuntimes) joint.ApplySolverPolicy();
        SyncGPUExceptions(); FlushGPUWakes();
    }
    internal void BeginPortableRestore()
    {
        EnsureCheckpointAccess(); _gpuReportRanges.Clear(); ResetDebugContacts(); _backend.BeginPortableRestore();
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
    }
    internal void ValidatePortableOneWays(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> pairs)
    {
        _portableOneWayKeys.Clear(); _portableOneWayKeys.EnsureCapacity(pairs.Count);
        foreach (var pair in pairs)
        {
            var first = map.Collider(pair.A).Backend; var second = map.Collider(pair.B).Backend;
            PhysicsSnapshotReader.Require(first != second);
            var key = pair.A < pair.B ? (pair.A, pair.ShapeA, pair.PieceA, pair.B, pair.ShapeB, pair.PieceB) : (pair.B, pair.ShapeB, pair.PieceB, pair.A, pair.ShapeA, pair.PieceA);
            PhysicsSnapshotReader.Require(_portableOneWayKeys.Add(key));
            first.ValidatePortablePiece(pair.ShapeA, pair.PieceA); second.ValidatePortablePiece(pair.ShapeB, pair.PieceB);
        }
    }
}
