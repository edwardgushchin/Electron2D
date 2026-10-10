using Box2D.NET;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private readonly List<PhysicsSnapshotMap> _snapshotMaps = [];
    internal bool PortableColdStep;
    private readonly Dictionary<ulong, (ulong ID, int Slot, int Piece)> _portableShapeLookup = [];
    private readonly HashSet<(ulong, int, int, ulong, int, int)> _portableOneWayKeys = [];
    private GPUPhysicsBodyStore.PortableOneWay[] _portableGPUOneWays = [];
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
        foreach (var body in _bodies) body.Backend.PrepareGPUParameters(body.Runtime);
        uint order = 0;
        foreach (var area in _areas) gpu.SetAreaFields(area.Backend.GPUHandle, GPUFields(area.Fields), order++);
        foreach (var body in _serverColliders)
            if (body.IsArea) gpu.SetAreaFields(body.Backend.GPUHandle, GPUFields(body.AreaFields!), order++);
            else body.Backend.PrepareGPUParameters(body.Runtime);
        foreach (var joint in _jointRuntimes) joint.ApplySolverPolicy();
        SyncGPUExceptions(); FlushGPUWakes();
    }
    internal void BeginPortableRestore()
    {
        EnsureCheckpointAccess();
        _frameContacts.Clear(); _frameContactIndices.Clear(); _aggregateContactImpulses = false;
        _gpuReportRanges.Clear(); _oneWayPairs.Clear(); ResetDebugContacts();
        if (GPUStore is { } gpu) gpu.BeginPortableRestore(); else PortableColdStep = true;
    }
    internal void FailPortableRestore(Exception error) => _checkpointFailure = error;
    internal void CompletePortableRestore(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> pairs, ulong tick, float step)
    {
        Tick = tick; LastStep = step; _contactStep = unchecked((long)tick);
        if (GPUStore is { } gpu)
        {
            if (_portableGPUOneWays.Length < pairs.Count) Array.Resize(ref _portableGPUOneWays, pairs.Count);
            for (var i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                _portableGPUOneWays[i] = new(map.Collider(pair.A).Backend.PortableGPUShape(pair.ShapeA), pair.PieceA,
                    map.Collider(pair.B).Backend.PortableGPUShape(pair.ShapeB), pair.PieceB, pair.Allowed);
            }
            gpu.CompletePortableRestore(_portableGPUOneWays.AsSpan(0, pairs.Count)); _gpuWakePending = false;
            InvalidateGPUStates(wake: false); PublishGPU();
            PhysicsServer.Service.PublishStatistics(this, _backend.ReadStatistics());
        }
        else
        {
            foreach (var pair in pairs)
            {
                var a = map.Collider(pair.A).Backend.PortableCPUShape(pair.ShapeA, pair.PieceA);
                var b = map.Collider(pair.B).Backend.PortableCPUShape(pair.ShapeB, pair.PieceB);
                var first = PackShapeID(a); var second = PackShapeID(b); var key = first < second ? (first, second) : (second, first);
                _oneWayPairs.Add(key, new(pair.Allowed, _contactStep));
            }
            _defaultGravity = DefaultAreaFields.GravityPoint ? default : DefaultAreaFields.GravityVector * DefaultAreaFields.Gravity;
            b2World_SetGravity(WorldID, PhysicsShapeBackend.ToBackend(_defaultGravity));
            PublishStatistics();
        }
    }
    internal void CapturePortableOneWays(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> destination)
    {
        destination.Clear();
        if (GPUStore is { } gpu)
        {
            if (_portableGPUOneWays.Length < gpu.OneWayPairCount) Array.Resize(ref _portableGPUOneWays, gpu.OneWayPairCount);
            var count = gpu.ReadPortableOneWays(_portableGPUOneWays);
            for (var i = 0; i < count; i++)
            {
                var p = _portableGPUOneWays[i];
                if (!_gpuShapeOwners.TryGetValue((uint)p.A.Index, out var a) || a.Generation != p.A.Generation ||
                    !_gpuShapeOwners.TryGetValue((uint)p.B.Index, out var b) || b.Generation != p.B.Generation) continue;
                destination.Add(new(map.NetworkID(a.Owner.RID), a.Slot, p.PieceA, map.NetworkID(b.Owner.RID), b.Slot, p.PieceB, p.Allowed));
            }
            return;
        }
        if (_oneWayPairs.Count == 0) return;
        _portableShapeLookup.Clear();
        foreach (var body in _bodies) AddPortableShapeKeys(map, body.Backend);
        foreach (var area in _areas) AddPortableShapeKeys(map, area.Backend);
        foreach (var body in _serverColliders) AddPortableShapeKeys(map, body.Backend);
        foreach (var pair in _oneWayPairs)
            if (_portableShapeLookup.TryGetValue(pair.Key.Item1, out var a) && _portableShapeLookup.TryGetValue(pair.Key.Item2, out var b))
                destination.Add(new(a.ID, a.Slot, a.Piece, b.ID, b.Slot, b.Piece, pair.Value.Allowed));
    }
    private void AddPortableShapeKeys(PhysicsSnapshotMap map, PhysicsColliderBackend backend)
    {
        var previous = -1; var piece = 0; var id = map.NetworkID(backend.RID);
        for (var i = 0; i < backend.Shapes.Count; i++)
        {
            var shape = backend.Shapes[i];
            var tag = b2Shape_GetUserData(shape).GetRef<PhysicsFixtureTag>()!;
            if (tag.ShapeIndex != previous) { piece = 0; previous = tag.ShapeIndex; }
            _portableShapeLookup.Add(PackShapeID(shape), (id, tag.ShapeIndex, piece++));
        }
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
    private static ulong OneWayShapeKey(B2ShapeId shape, PhysicsFixtureTag? tag)
    {
        // Convex decomposition is private: the complete solid has one side episode, not one per artificial hull.
        if (tag is { Compound: not null, Owner: { } owner })
            return PackShapeID(owner.PortableCPUShape(tag.ShapeIndex, 0));
        return PackShapeID(shape);
    }
}
