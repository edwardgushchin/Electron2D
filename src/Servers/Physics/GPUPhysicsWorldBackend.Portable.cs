namespace Electron2D;

internal sealed partial class GPUPhysicsWorldBackend
{
    private GPUPhysicsBodyStore.BodyHandle[] _portableBodies = [];
    private GPUPhysicsBodyStore.Snapshot[] _portableStates = [];
    internal override void ReadPortableBodies(ReadOnlySpan<PhysicsColliderBackend> colliders, bool capture)
    {
        EnsureAccess();
        if (_portableBodies.Length < colliders.Length)
        { Array.Resize(ref _portableBodies, colliders.Length); Array.Resize(ref _portableStates, colliders.Length); }
        for (var i = 0; i < colliders.Length; i++) _portableBodies[i] = GPUStore.NativeCollider(colliders[i]).GPUHandle;
        if (colliders.IsEmpty && !capture) GPUStore.Step(0, default);
        else GPUStore.Read(_portableBodies.AsSpan(0, colliders.Length), _portableStates);
        for (var i = 0; i < colliders.Length; i++) GPUStore.NativeCollider(colliders[i]).AcceptGPUState(_portableStates[i]);
    }
    internal override void PreparePortableCapture() { PrepareCheckpoint(); Space.FlushGPUWakes(); }
    internal override float ReadPortableSleepTime(PhysicsColliderBackend collider) =>
        GPUStore.NativeCollider(collider).PortableSleepTime;
    // Resident motion restoration already commits sleep time and flags in its device write.
    internal override void ApplyPortableSleep(PhysicsColliderBackend collider, float time, bool canSleep, bool sleeping) { }
    private GPUPhysicsBodyStore.PortableOneWay[] _portableGPUOneWays = [];
    internal override void BeginPortableRestore() { Space.ClearGPUReports(); GPUStore.BeginPortableRestore(); }
    internal override void CompletePortableRestore(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> pairs, ulong tick)
    {
        if (_portableGPUOneWays.Length < pairs.Count) Array.Resize(ref _portableGPUOneWays, pairs.Count);
        for (var i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            _portableGPUOneWays[i] = new(GPUStore.NativeCollider(map.Collider(pair.A).Backend).PortableGPUShape(pair.ShapeA), pair.PieceA,
                GPUStore.NativeCollider(map.Collider(pair.B).Backend).PortableGPUShape(pair.ShapeB), pair.PieceB, pair.Allowed);
        }
        GPUStore.CompletePortableRestore(_portableGPUOneWays.AsSpan(0, pairs.Count)); Space.CompleteGPUPortableRestore();
    }
    internal override void CapturePortableOneWays(PhysicsSnapshotMap map, List<PhysicsPortableOneWay> destination)
    {
        if (_portableGPUOneWays.Length < GPUStore.OneWayPairCount) Array.Resize(ref _portableGPUOneWays, GPUStore.OneWayPairCount);
        var count = GPUStore.ReadPortableOneWays(_portableGPUOneWays);
        for (var i = 0; i < count; i++)
        {
            var p = _portableGPUOneWays[i];
            if (!Space.TryGetGPUShapeOwner((uint)p.A.Index, out var a) || a.Generation != p.A.Generation ||
                !Space.TryGetGPUShapeOwner((uint)p.B.Index, out var b) || b.Generation != p.B.Generation) continue;
            destination.Add(new(map.NetworkID(a.Owner.RID), a.Slot, p.PieceA, map.NetworkID(b.Owner.RID), b.Slot, p.PieceB, p.Allowed));
        }
    }
}
