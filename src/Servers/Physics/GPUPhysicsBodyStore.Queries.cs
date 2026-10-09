using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    /// <summary>One ray or point query in scene units. Exclusion indices address the batch's collider-key span; points match the exact canvas, including zero. Rays ignore canvas association.</summary>
    internal readonly record struct WorldQuery(Vector2 From, Vector2 To = default, bool Ray = false, uint Mask = uint.MaxValue,
        bool Bodies = true, bool Areas = false, bool HitFromInside = false, int Limit = 32,
        int ExclusionStart = 0, int ExclusionCount = 0, ulong Canvas = 0);
    /// <summary>A selected logical hit with the current physical generations; these store-local identities are not network IDs.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct QueryHit
    {
        internal ulong Collider;
        internal int LogicalShape;
        internal uint Shape, ShapeGeneration, Body, BodyGeneration, Padding;
        internal Float4 PointNormal;
        internal float Fraction;
        internal uint Padding1, Padding2, Padding3;
        internal readonly Vector2 Position => new(PointNormal.X, PointNormal.Y);
        internal readonly Vector2 Normal => new(PointNormal.Z, PointNormal.W);
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct QueryMapping { internal ulong Collider; internal uint LogicalShape, Generation; internal ulong Canvas; internal ulong ObjectIdentity; }
    [StructLayout(LayoutKind.Sequential)]
    private struct QueryInput
    {
        internal Float4 Ray;
        internal uint Mask, Flags, Limit, Offset, ExclusionStart, ExclusionCount;
        internal ulong Canvas;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct QuerySettings { internal uint Count, Leaves, Shapes, Bodies; }
    private QueryInput[] _queryInputs = [];
    private int[] _queryLimits = [];
    private long _boundsBodyVersion = -1, _boundsShapeVersion = -1;
    private QueryMapping[] _queryMappings = [];
    private int _queryMappingUsed;
    private bool _queryMappingDirty;
    private RenderHandle? _queryPipeline, _queryMappingGPU, _queryInputGPU, _queryExcludedGPU, _queryOutputGPU, _queryCountsGPU, _queryStatus, _queryUpload, _queryDownload;
    private int _queryMappingCapacity, _queryInputCapacity, _queryExcludedCapacity, _queryOutputCapacity, _queryCountCapacity, _queryUploadCapacity, _queryDownloadCapacity;
    internal long QuerySpatialSubmissionCount { get; private set; }
    internal long QuerySubmissionCount { get; private set; }

    /// <summary>Assigns a logical ordering/exclusion key, canvas and optional object identity to a physical shape. A public-world adapter supplies its collider RID and logical slot.</summary>
    internal void SetQueryIdentity(ShapeHandle shape, ulong collider, int logicalShape, ulong canvas = 0, ulong objectID = 0)
    {
        Validate(shape);
        if (collider == 0 || logicalShape < 0) throw new ArgumentOutOfRangeException(nameof(collider));
        EnsureQueryMappings();
        ref var value = ref _queryMappings[shape.Index];
        if (value.Collider == collider && value.LogicalShape == logicalShape && value.Canvas == canvas && value.ObjectIdentity == objectID) return;
        value.Collider = collider; value.LogicalShape = (uint)logicalShape; value.Canvas = canvas; value.ObjectIdentity = objectID; _queryMappingDirty = true;
    }
    private void ResetQueryIdentity(int index)
    {
        if (index >= _queryMappingUsed) return;
        // Untagged internal stores use physical slot ordering. Public adapters explicitly supply stable logical identities.
        _queryMappings[index] = new() { Collider = (ulong)_shapeSlots[index].Body.Index + 1, LogicalShape = (uint)index, Generation = _shapeSlots[index].Generation };
        _queryMappingDirty = true;
    }
    private void EnsureQueryMappings()
    {
        if (_queryMappings.Length < _shapeHighWater) Array.Resize(ref _queryMappings, Capacity(_shapeHighWater));
        while (_queryMappingUsed < _shapeHighWater) { var at = _queryMappingUsed++; ResetQueryIdentity(at); }
    }
    private void PrepareQuerySpatial()
    {
        Step(0, default);
        if (_shapeHighWater == 0 || _boundsBodyVersion == _bodyVersion && _boundsShapeVersion == _shapeVersion && _spatialEpoch == Shape.GeometryEpoch) return;
        _pairBodyVersion = -1; _pairMargin = _pairSweepDelta = 0; _pairSweepCorrections = false;
        PrepareGeometry(); EnsureSpatial(); DispatchSpatial(false, pairs: false); CommitGeometry();
        _boundsBodyVersion = _bodyVersion; _boundsShapeVersion = _shapeVersion;
    }
    private static int QueryLimit(in WorldQuery query) => query.Ray ? Math.Min(1, query.Limit) : query.Limit;

    /// <summary>Executes batched ray/point queries on current device geometry. Results occupy capped segments; unwritten destination entries stay unchanged.</summary>
    internal void Query(ReadOnlySpan<WorldQuery> queries, ReadOnlySpan<ulong> exclusions, Span<int> counts, Span<QueryHit> hits)
    {
        EnsureAccess();
        if (counts.Length < queries.Length) throw new ArgumentException("The query count destination is too small.", nameof(counts));
        var total = 0;
        foreach (ref readonly var q in queries)
        {
            if (!q.From.IsFinite() || !q.To.IsFinite() || q.Ray && !(q.To - q.From).IsFinite() || q.Limit < 0 ||
                q.ExclusionStart < 0 || q.ExclusionStart > exclusions.Length || q.ExclusionCount < 0 || q.ExclusionCount > exclusions.Length - q.ExclusionStart)
                throw new ArgumentOutOfRangeException(nameof(queries));
            total = checked(total + QueryLimit(q));
        }
        if (hits.Length < total) throw new ArgumentException("The query hit destination is too small.", nameof(hits));
        if (queries.IsEmpty) return;
        PrepareQueryRequests(queries.Length);
        uint offset = 0;
        for (var i = 0; i < queries.Length; i++)
        {
            ref readonly var q = ref queries[i]; var motion = q.Ray ? q.To - q.From : Vector2.Zero; var limit = QueryLimit(q);
            _queryLimits[i] = limit;
            _queryInputs[i] = new()
            {
                Ray = new(q.From.X, q.From.Y, motion.X, motion.Y),
                Mask = q.Mask,
                Flags = (q.Bodies ? 1u : 0) | (q.Areas ? 2u : 0) | (q.Ray ? 4u : 0) | (q.HitFromInside ? 8u : 0),
                Limit = (uint)limit,
                Offset = offset,
                ExclusionStart = (uint)q.ExclusionStart,
                ExclusionCount = (uint)q.ExclusionCount,
                Canvas = q.Canvas
            };
            offset += (uint)limit;
        }
        ExecuteQueries<QueryInput, QueryHit>(_queryInputs.AsSpan(0, queries.Length), exclusions, counts, hits, total, ref _queryPipeline, "PhysicsResidentQueries.comp.spv");
    }
    private void PrepareQueryRequests(int count)
    {
        if (_queryLimits.Length >= count) return;
        var capacity = Capacity(count); Array.Resize(ref _queryLimits, capacity); Array.Resize(ref _queryInputs, capacity);
    }
    private void ExecuteQueries<TInput, THit>(ReadOnlySpan<TInput> queries, ReadOnlySpan<ulong> exclusions, Span<int> counts, Span<THit> hits,
        int total, ref RenderHandle? pipeline, string shader, bool centers = false, int scratchBytesPerQuery = 0) where TInput : unmanaged where THit : unmanaged
    {
        PrepareQuerySpatial();
        if (ShapeCount == 0 || total == 0) { counts[..queries.Length].Clear(); return; }
        EnsureQueryMappings();
        pipeline ??= _context.CreatePipeline(shader);
        var mappingBytes = _queryMappingDirty ? checked(_shapeHighWater * sizeof(QueryMapping)) : 0;
        var inputBytes = checked(queries.Length * sizeof(TInput)); var excludedBytes = checked(exclusions.Length * 8);
        var outputBytes = checked(total * sizeof(THit)); var countBytes = checked(queries.Length * 4);
        Grow(ref _queryMappingGPU, ref _queryMappingCapacity, _shapeHighWater, sizeof(QueryMapping), true);
        Grow(ref _queryInputGPU, ref _queryInputCapacity, inputBytes, 1, false);
        Grow(ref _queryExcludedGPU, ref _queryExcludedCapacity, Math.Max(1, exclusions.Length), 8, false);
        Grow(ref _queryOutputGPU, ref _queryOutputCapacity, checked(outputBytes + queries.Length * scratchBytesPerQuery), 1, false);
        Grow(ref _queryCountsGPU, ref _queryCountCapacity, queries.Length, 4, false);
        _queryStatus ??= Buffer(8);
        GrowTransfer(ref _queryUpload, ref _queryUploadCapacity, checked(8 + mappingBytes + inputBytes + excludedBytes), SDL.GPUTransferBufferUsage.Upload);
        GrowTransfer(ref _queryDownload, ref _queryDownloadCapacity, checked(8 + countBytes + outputBytes), SDL.GPUTransferBufferUsage.Download);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire resident world queries");
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _queryUpload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map resident query input");
            try
            {
                *(ulong*)mapped = 0;
                fixed (QueryMapping* source = _queryMappings) System.Buffer.MemoryCopy(source, (byte*)mapped + 8, mappingBytes, mappingBytes);
                fixed (TInput* source = queries) System.Buffer.MemoryCopy(source, (byte*)mapped + 8 + mappingBytes, inputBytes, inputBytes);
                fixed (ulong* source = exclusions) System.Buffer.MemoryCopy(source, (byte*)mapped + 8 + mappingBytes + inputBytes, excludedBytes, excludedBytes);
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _queryUpload.DangerousGetHandle()); }
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin resident query uploads");
            UploadQuery(copy, _queryStatus, 0, 8);
            if (mappingBytes > 0) UploadQuery(copy, _queryMappingGPU!, 8, mappingBytes);
            UploadQuery(copy, _queryInputGPU!, 8 + mappingBytes, inputBytes);
            if (excludedBytes > 0) UploadQuery(copy, _queryExcludedGPU!, 8 + mappingBytes + inputBytes, excludedBytes);
            SDL.EndGPUCopyPass(copy);
            var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[4];
            outputs[0] = new() { Buffer = _queryOutputGPU!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _queryCountsGPU!.DangerousGetHandle() }; outputs[2] = new() { Buffer = _queryStatus.DangerousGetHandle() };
            if (centers) outputs[3] = new() { Buffer = _centers!.DangerousGetHandle() };
            var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, centers ? 4u : 3u);
            if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident queries");
            SDL.BindGPUComputePipeline(compute, pipeline.DangerousGetHandle());
            var inputs = stackalloc nint[8] { _bodies!.DangerousGetHandle(), _verticesGPU!.DangerousGetHandle(), _geometryGPU!.DangerousGetHandle(), _shapesGPU!.DangerousGetHandle(),
                _nodesGPU!.DangerousGetHandle(), _queryMappingGPU!.DangerousGetHandle(), _queryInputGPU!.DangerousGetHandle(), _queryExcludedGPU!.DangerousGetHandle() };
            SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 8);
            var settings = new QuerySettings { Count = (uint)queries.Length, Leaves = (uint)_shapeCapacity, Shapes = (uint)_shapeHighWater, Bodies = (uint)_highWater };
            SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(QuerySettings));
            SDL.DispatchGPUCompute(compute, ((uint)queries.Length + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin resident query results");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _queryStatus.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _queryDownload!.DangerousGetHandle() });
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _queryCountsGPU.DangerousGetHandle(), Size = (uint)countBytes }, new() { TransferBuffer = _queryDownload.DangerousGetHandle(), Offset = 8 });
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _queryOutputGPU.DangerousGetHandle(), Size = (uint)outputBytes }, new() { TransferBuffer = _queryDownload.DangerousGetHandle(), Offset = (uint)(8 + countBytes) });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            UploadBytes += 8 + mappingBytes + inputBytes + excludedBytes; ReadbackBytes += 8 + countBytes + outputBytes; UniformBytes += sizeof(QuerySettings); QuerySubmissionCount++;
            mapped = SDL.MapGPUTransferBuffer(Device, _queryDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map resident query results");
            try
            {
                if (*(uint*)mapped != 0) throw new InvalidOperationException($"GPU world query failed (status 0x{*(uint*)mapped:X}).");
                var found = (uint*)((byte*)mapped + 8);
                for (var i = 0; i < queries.Length; i++) if (found[i] > _queryLimits[i]) throw new InvalidOperationException("GPU world query exceeded its result limit.");
                var offset = 0;
                fixed (THit* target = hits)
                    for (var i = 0; i < queries.Length; i++)
                    {
                        var bytes = found[i] * (uint)sizeof(THit);
                        System.Buffer.MemoryCopy((byte*)mapped + 8 + countBytes + offset * sizeof(THit), target + offset, bytes, bytes);
                        counts[i] = (int)found[i]; offset += _queryLimits[i];
                    }
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _queryDownload.DangerousGetHandle()); }
            _failed = false; _queryMappingDirty = false;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }
    private void UploadQuery(nint copy, RenderHandle target, int offset, int bytes) => SDL.UploadToGPUBuffer(copy,
        new() { TransferBuffer = _queryUpload!.DangerousGetHandle(), Offset = (uint)offset }, new() { Buffer = target.DangerousGetHandle(), Size = (uint)bytes }, false);
    private void DisposeQueries()
    { _motionQueryPipeline?.Dispose(); _shapeQueryPipeline?.Dispose(); _queryPipeline?.Dispose(); _queryMappingGPU?.Dispose(); _queryInputGPU?.Dispose(); _queryExcludedGPU?.Dispose(); _queryOutputGPU?.Dispose(); _queryCountsGPU?.Dispose(); _queryStatus?.Dispose(); _queryUpload?.Dispose(); _queryDownload?.Dispose(); }
}
