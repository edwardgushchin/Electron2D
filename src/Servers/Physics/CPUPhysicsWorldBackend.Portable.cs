using Box2D.NET;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    internal override void PreparePortableCapture() => EnsureAccess();
    internal override void ReadPortableBodies(ReadOnlySpan<PhysicsColliderBackend> colliders, bool capture)
    {
        EnsureAccess();
        foreach (var collider in colliders) _ = NativeCollider(collider);
    }
    internal override void SynchronizeCollisionExceptions() => EnsureAccess();
    private readonly Dictionary<ulong, (ulong ID, int Slot, int Piece)> _portableShapeLookup = [];
    internal override void BeginPortableRestore()
    {
        _frameContacts.Clear(); _frameContactIndices.Clear(); _aggregateContactImpulses = false;
        _oneWayPairs.Clear(); Space.PortableColdStep = true;
    }
    internal override void CompletePortableRestore(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> pairs, ulong tick)
    {
        _contactStep = unchecked((long)tick);
        foreach (var pair in pairs)
        {
            var a = NativeCollider(map.Collider(pair.A).Backend).PortableCPUShape(pair.ShapeA, pair.PieceA);
            var b = NativeCollider(map.Collider(pair.B).Backend).PortableCPUShape(pair.ShapeB, pair.PieceB);
            var first = PackShapeID(a); var second = PackShapeID(b); var key = first < second ? (first, second) : (second, first);
            _oneWayPairs.Add(key, new(pair.Allowed, _contactStep));
        }
        var gravity = Space.DefaultAreaFields.GravityPoint ? default : Space.DefaultAreaFields.GravityVector * Space.DefaultAreaFields.Gravity;
        Space.SetDefaultGravity(gravity);
        b2World_SetGravity(WorldID, PhysicsShapeBackend.ToBackend(gravity));
        Space.PublishStatistics();
    }
    internal override void CapturePortableOneWays(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> destination)
    {
        if (_oneWayPairs.Count == 0) return;
        _portableShapeLookup.Clear();
        foreach (var body in Space.Bodies) AddPortableShapeKeys(map, body.Backend);
        foreach (var area in Space.Areas) AddPortableShapeKeys(map, area.Backend);
        foreach (var body in Space.ServerColliders) AddPortableShapeKeys(map, body.Backend);
        foreach (var pair in _oneWayPairs)
            if (_portableShapeLookup.TryGetValue(pair.Key.Item1, out var a) && _portableShapeLookup.TryGetValue(pair.Key.Item2, out var b))
                destination.Add(new(a.ID, a.Slot, a.Piece, b.ID, b.Slot, b.Piece, pair.Value.Allowed));
    }
    private void AddPortableShapeKeys(PhysicsSnapshotMap map, PhysicsColliderBackend backend)
    {
        var previous = -1; var piece = 0; var id = map.NetworkID(backend.RID);
        for (var i = 0; i < NativeCollider(backend).Shapes.Count; i++)
        {
            var shape = NativeCollider(backend).Shapes[i];
            var tag = b2Shape_GetUserData(shape).GetRef<PhysicsFixtureTag>()!;
            if (tag.ShapeIndex != previous) { piece = 0; previous = tag.ShapeIndex; }
            _portableShapeLookup.Add(PackShapeID(shape), (id, tag.ShapeIndex, piece++));
        }
    }
    private ulong OneWayShapeKey(B2ShapeId shape, PhysicsFixtureTag? tag)
    {
        // Convex decomposition is private: the complete solid has one side episode, not one per artificial hull.
        if (tag is { Compound: not null, Owner: { } owner })
            return PackShapeID(NativeCollider(owner).PortableCPUShape(tag.ShapeIndex, 0));
        return PackShapeID(shape);
    }
}
