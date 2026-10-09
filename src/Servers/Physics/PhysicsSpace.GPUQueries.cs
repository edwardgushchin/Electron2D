namespace Electron2D;

internal sealed partial class PhysicsSpace
{
    private ulong[] _gpuQueryExclusions = [];
    private GPUPhysicsBodyStore.QueryHit[] _gpuPointHits = [];
    private GPUPhysicsBodyStore.ShapeQueryHit[] _gpuShapeHits = [];
    private readonly GPUPhysicsBodyStore.ShapeQuery[] _gpuShapeQuery = new GPUPhysicsBodyStore.ShapeQuery[1];
    private Shape? _gpuQueryShape;
    private GPUPhysicsBodyStore.QueryGeometry? _gpuQueryGeometry;

    private bool GPUAreaContainsPoint(Area area, Vector2 point, uint mask)
    {
        var capacity = _gpuSensorShapeCount;
        if (_gpuPointHits.Length < capacity) Array.Resize(ref _gpuPointHits, Math.Max(8, capacity * 2));
        Span<GPUPhysicsBodyStore.WorldQuery> query = stackalloc GPUPhysicsBodyStore.WorldQuery[1];
        query[0] = new(point, Mask: mask, Bodies: false, Areas: true, Limit: capacity, Canvas: area.Backend.CanvasInstanceID);
        Span<int> count = stackalloc int[1];
        GPUStore!.Query(query, [], count, _gpuPointHits);
        foreach (ref readonly var hit in _gpuPointHits.AsSpan(0, count[0]))
            if (hit.Collider == (ulong)area.PhysicsRID.GetID()) return true;
        return false;
    }

    private ReadOnlySpan<ulong> GPUExclusions(ReadOnlySpan<RID> source)
    {
        if (_gpuQueryExclusions.Length < source.Length) Array.Resize(ref _gpuQueryExclusions, Math.Max(8, source.Length * 2));
        for (var i = 0; i < source.Length; i++) _gpuQueryExclusions[i] = (ulong)source[i].GetID();
        return _gpuQueryExclusions.AsSpan(0, source.Length);
    }
    internal PhysicsRayResult? GPURay(Vector2 from, Vector2 to, uint mask, RID[] excluded, bool areas, bool bodies, bool inside)
    {
        Span<GPUPhysicsBodyStore.WorldQuery> queries = stackalloc GPUPhysicsBodyStore.WorldQuery[1];
        queries[0] = new(from, to, true, mask, bodies, areas, inside, 1, ExclusionCount: excluded.Length);
        Span<GPUPhysicsBodyStore.QueryHit> hits = stackalloc GPUPhysicsBodyStore.QueryHit[1];
        Span<int> count = stackalloc int[1];
        GPUStore!.Query(queries, GPUExclusions(excluded), count, hits);
        if (count[0] == 0) return null;
        ref readonly var hit = ref hits[0]; var body = _gpuRIDColliders[hit.Collider];
        return new(body.RID, body.SceneOwner, body.ObjectIdentity, hit.LogicalShape, hit.Position, hit.Normal);
    }
    internal void GPUPoint(PhysicsPointQueryParameters parameters, List<PhysicsPointResult> output)
    {
        var capacity = GPUStore!.ShapeCount;
        if (_gpuPointHits.Length < capacity) Array.Resize(ref _gpuPointHits, Math.Max(8, capacity * 2));
        Span<GPUPhysicsBodyStore.WorldQuery> queries = stackalloc GPUPhysicsBodyStore.WorldQuery[1];
        queries[0] = new(parameters.Position, Mask: parameters.CollisionMask, Bodies: parameters.CollideWithBodies,
            Areas: parameters.CollideWithAreas, Limit: capacity, ExclusionCount: parameters.ExclusionsArray.Length, Canvas: parameters.CanvasInstanceID);
        Span<int> count = stackalloc int[1];
        GPUStore.Query(queries, GPUExclusions(parameters.ExclusionsArray), count, _gpuPointHits);
        foreach (ref readonly var hit in _gpuPointHits.AsSpan(0, count[0]))
        {
            var body = _gpuRIDColliders[hit.Collider];
            output.Add(new(body.RID, body.SceneOwner, body.ObjectIdentity, hit.LogicalShape));
        }
    }
    internal ReadOnlySpan<GPUPhysicsBodyStore.ShapeQueryHit> GPUShapeQuery(PhysicsShapeQueryParameters parameters,
        GPUPhysicsBodyStore.ShapeQueryMode mode, int limit)
    {
        var shape = parameters.Shape ?? PhysicsServer.Service.GetShapeGeometry(parameters.ShapeRID);
        if (!ReferenceEquals(_gpuQueryShape, shape))
        {
            var next = GPUStore!.RetainQueryGeometry(shape);
            _gpuQueryGeometry?.Dispose(); _gpuQueryGeometry = next; _gpuQueryShape = shape;
        }
        if (_gpuShapeHits.Length < limit) Array.Resize(ref _gpuShapeHits, Math.Max(8, limit * 2));
        _gpuShapeQuery[0] = new(_gpuQueryGeometry!, parameters.Transform, parameters.Motion, parameters.Margin, mode,
            parameters.CollisionMask, parameters.CollideWithBodies, parameters.CollideWithAreas, limit, ExclusionCount: parameters.ExclusionsArray.Length);
        Span<int> count = stackalloc int[1];
        GPUStore!.QueryShapes(_gpuShapeQuery, GPUExclusions(parameters.ExclusionsArray), count, _gpuShapeHits);
        return _gpuShapeHits.AsSpan(0, count[0]);
    }
    internal PhysicsColliderBackend GPUQueryOwner(ulong id) => _gpuRIDColliders[id];

    private MotionResultData GPUTestMotion(RID owner, Transform from, Vector2 motion, float margin, bool recoveryAsCollision,
        RID[] excludedBodies, ulong[] excludedObjects, bool collideSeparationRay)
    {
        var body = _gpuRIDColliders[(ulong)owner.GetID()];
        Span<GPUPhysicsBodyStore.MotionQuery> query = stackalloc GPUPhysicsBodyStore.MotionQuery[1];
        query[0] = new(body.GPUHandle, from, motion, margin, recoveryAsCollision, collideSeparationRay,
            BodyExclusionCount: excludedBodies.Length, ObjectExclusionCount: excludedObjects.Length);
        Span<GPUPhysicsBodyStore.MotionQueryResult> result = stackalloc GPUPhysicsBodyStore.MotionQueryResult[1];
        GPUStore!.TestMotion(query, GPUExclusions(excludedBodies), excludedObjects, result);
        ref readonly var hit = ref result[0];
        var other = hit.Collided ? _gpuRIDColliders[hit.Collider] : null;
        return new(owner, other?.RID ?? default, hit.Object, hit.LocalShape, hit.ColliderShape, hit.Point, hit.Normal, hit.Depth,
            hit.Velocity, hit.Travel, hit.Remainder, hit.SafeFraction, hit.UnsafeFraction, hit.Collided, other?.ObjectIdentity ?? default);
    }
}
