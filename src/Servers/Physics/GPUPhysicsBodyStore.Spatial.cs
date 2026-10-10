using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SpatialSettings
    {
        internal uint Stage, Count, Bodies, Shapes;
        internal uint Leaves, Geometry, Vertices, Pairs;
        internal Float4 Tolerances;
        internal uint CollisionFilters, FilterPairs, Padding1, Padding2;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct TreeSettings
    {
        internal uint Count, Leaves, Operation, Start, Stage, Stride, Padding1, Padding2;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct PairData { internal uint A, B, GenerationA, GenerationB; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ProxyData { internal Float4 Bounds; internal int Type, Shape, Active, ExtraMask; }

    private RenderHandle? _spatialPipeline, _residentTreePipeline;
    private RenderHandle? _verticesGPU, _geometryGPU, _shapesGPU, _proxiesGPU, _nodesGPU, _orderGPU, _pairsGPU, _spatialSummary;
    private RenderHandle? _vertexEditsGPU, _geometryEditsGPU, _shapeEditsGPU, _spatialUpload, _spatialDownload;
    private int _vertexCapacity, _geometryCapacity, _shapeCapacity, _proxyCapacity, _nodeCapacity, _orderCapacity, _pairCapacity;
    private int _vertexEditCapacity, _geometryEditCapacity, _shapeEditCapacity, _spatialUploadBytes, _spatialDownloadBytes;
    private int _refitsSinceSort;
    private float _pairMargin, _pairSweepDelta;
    private bool _pairSweepCorrections;

    internal int FindPairs(float margin = 0, float sweepDelta = 0, bool sweepCorrections = false)
    {
        EnsureAccess();
        if (!float.IsFinite(margin) || margin < 0) throw new ArgumentOutOfRangeException(nameof(margin));
        if (!float.IsFinite(sweepDelta) || sweepDelta < 0) throw new ArgumentOutOfRangeException(nameof(sweepDelta));
        if (_shapeHighWater == 0) return 0;
        Step(0, default); FlushJoints();
        if (_pairBodyVersion == _bodyVersion && _pairShapeVersion == _shapeVersion && _spatialEpoch == Shape.GeometryEpoch && _pairMargin == margin && _pairSweepDelta == sweepDelta && _pairSweepCorrections == sweepCorrections) return PairCount;
        _pairBodyVersion = -1; _pairMargin = margin; _pairSweepDelta = sweepDelta; _pairSweepCorrections = sweepCorrections;
        PrepareGeometry(); EnsureSpatial();
        var retry = false;
        while (true)
        {
            var count = DispatchSpatial(retry);
            if (count > _pairCapacity)
            {
                if (retry) { _failed = true; throw new InvalidOperationException("The immutable GPU pair batch changed during capacity recovery."); }
                Grow(ref _pairsGPU, ref _pairCapacity, count, sizeof(PairData), false);
                PairCapacityRetries++; retry = true; continue;
            }
            PairCount = count; break;
        }
        CommitGeometry();
        _pairBodyVersion = _boundsBodyVersion = _bodyVersion; _pairShapeVersion = _boundsShapeVersion = _shapeVersion;
        return PairCount;
    }

    internal int ReadPairs(Span<ShapePair> destination)
    {
        var count = FindPairs();
        if (destination.Length < count) throw new ArgumentException("The pair destination is too small.", nameof(destination));
        if (count == 0) return 0;
        DownloadSpatial(_pairsGPU!, 0, checked((uint)(count * sizeof(PairData))));
        var mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload!.DangerousGetHandle(), false);
        if (mapped == 0) { _failed = true; throw GPUPhysicsDevice.Failure("map resident pairs"); }
        try
        {
            var pairs = new ReadOnlySpan<PairData>((void*)mapped, count);
            for (var i = 0; i < count; i++)
                destination[i] = new(new((int)pairs[i].A, pairs[i].GenerationA, _identity), new((int)pairs[i].B, pairs[i].GenerationB, _identity));
        }
        finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
        return count;
    }

    internal Rect2? ReadShapeBounds(ShapeHandle shape, float sweepDelta = 0)
    {
        Validate(shape); FindPairs(sweepDelta: sweepDelta);
        DownloadSpatial(_proxiesGPU!, checked((uint)(shape.Index * sizeof(ProxyData))), (uint)sizeof(ProxyData));
        var mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload!.DangerousGetHandle(), false);
        if (mapped == 0) { _failed = true; throw GPUPhysicsDevice.Failure("map resident bounds"); }
        try
        {
            var proxy = *(ProxyData*)mapped;
            return proxy.Active == 0 ? null : new Rect2(proxy.Bounds.X, proxy.Bounds.Y, proxy.Bounds.Z - proxy.Bounds.X, proxy.Bounds.W - proxy.Bounds.Y);
        }
        finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
    }

    private void EnsureSpatial()
    {
        _spatialPipeline ??= _context.CreatePipeline("PhysicsResidentShapes.comp.spv");
        _residentTreePipeline ??= _context.CreatePipeline("PhysicsTree.comp.spv");
        _spatialSummary ??= Buffer(8);
        Grow(ref _verticesGPU, ref _vertexCapacity, Math.Max(1, _vertexHighWater), sizeof(Vector2), true);
        Grow(ref _geometryGPU, ref _geometryCapacity, Math.Max(1, _geometryEntries.Count), sizeof(GeometryData), true);
        var previous = _shapeCapacity;
        Grow(ref _shapesGPU, ref _shapeCapacity, Math.Max(1, _shapeHighWater), sizeof(ShapeData), true);
        _treeTopologyDirty |= previous != _shapeCapacity;
        Grow(ref _proxiesGPU, ref _proxyCapacity, _shapeCapacity, sizeof(ProxyData), false);
        Grow(ref _nodesGPU, ref _nodeCapacity, checked(2 * _shapeCapacity), 32, false);
        Grow(ref _orderGPU, ref _orderCapacity, _shapeCapacity, 8, false);
        Grow(ref _pairsGPU, ref _pairCapacity, 1, sizeof(PairData), false);
        Grow(ref _vertexEditsGPU, ref _vertexEditCapacity, Math.Max(1, _vertexEditCount), sizeof(VertexEdit), false);
        Grow(ref _geometryEditsGPU, ref _geometryEditCapacity, Math.Max(1, _dirtyGeometry.Count), sizeof(GeometryEdit), false);
        Grow(ref _shapeEditsGPU, ref _shapeEditCapacity, Math.Max(1, _dirtyShapes.Count), sizeof(ShapeEdit), false);
        var uploadBytes = checked(8 + _vertexEditCount * sizeof(VertexEdit) + _dirtyGeometry.Count * sizeof(GeometryEdit) + _dirtyShapes.Count * sizeof(ShapeEdit));
        GrowTransfer(ref _spatialUpload, ref _spatialUploadBytes, uploadBytes, SDL.GPUTransferBufferUsage.Upload);
        GrowTransfer(ref _spatialDownload, ref _spatialDownloadBytes, 32, SDL.GPUTransferBufferUsage.Download);
    }

    private void Grow(ref RenderHandle? target, ref int capacity, int count, int stride, bool preserve)
    {
        if (count <= capacity) return;
        var next = Capacity(count);
        var replacement = Buffer(checked((uint)(next * stride)));
        try
        {
            if (preserve && target is not null)
            {
                var command = SDL.AcquireGPUCommandBuffer(Device);
                if (command == 0) throw GPUPhysicsDevice.Failure("acquire geometry growth commands");
                try
                {
                    var copy = SDL.BeginGPUCopyPass(command);
                    if (copy == 0) throw GPUPhysicsDevice.Failure("begin geometry growth");
                    var bytes = checked((uint)(capacity * stride));
                    SDL.CopyGPUBufferToBuffer(copy, new() { Buffer = target.DangerousGetHandle() }, new() { Buffer = replacement.DangerousGetHandle() }, bytes, false);
                    SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command); _failed = false;
                    DeviceCopyBytes += bytes;
                }
                finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
            }
            target?.Dispose(); target = replacement; replacement = null!; capacity = next;
        }
        finally { replacement?.Dispose(); }
    }

    private void GrowTransfer(ref RenderHandle? target, ref int capacity, int bytes, SDL.GPUTransferBufferUsage usage)
    {
        if (bytes <= capacity) return;
        var next = Capacity(bytes); var replacement = Transfer((uint)next, usage);
        target?.Dispose(); target = replacement; capacity = next;
    }

    private int DispatchSpatial(bool retry, bool pairs = true)
    {
        var batchVelocity = _batchedVelocity.HasValue;
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire resident spatial work");
        var uniformBytes = 0L;
        var vertexBytes = retry ? 0 : checked((uint)(_vertexEditCount * sizeof(VertexEdit)));
        var geometryBytes = retry ? 0 : checked((uint)(_dirtyGeometry.Count * sizeof(GeometryEdit)));
        var shapeBytes = retry ? 0 : checked((uint)(_dirtyShapes.Count * sizeof(ShapeEdit)));
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _spatialUpload!.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map resident geometry edits");
            try
            {
                *(ulong*)mapped = 0;
                fixed (VertexEdit* source = _vertexEdits) System.Buffer.MemoryCopy(source, (byte*)mapped + 8, vertexBytes, vertexBytes);
                fixed (GeometryEdit* source = _geometryEdits) System.Buffer.MemoryCopy(source, (byte*)mapped + 8 + vertexBytes, geometryBytes, geometryBytes);
                fixed (ShapeEdit* source = _shapeEdits) System.Buffer.MemoryCopy(source, (byte*)mapped + 8 + vertexBytes + geometryBytes, shapeBytes, shapeBytes);
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _spatialUpload.DangerousGetHandle()); }
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin resident geometry upload");
            UploadSpatial(copy, _spatialSummary!, 0, 8);
            if (batchVelocity) UploadSpatial(copy, _status!, 0, 8);
            if (vertexBytes > 0) UploadSpatial(copy, _vertexEditsGPU!, 8, vertexBytes);
            if (geometryBytes > 0) UploadSpatial(copy, _geometryEditsGPU!, 8 + vertexBytes, geometryBytes);
            if (shapeBytes > 0) UploadSpatial(copy, _shapeEditsGPU!, 8 + vertexBytes + geometryBytes, shapeBytes);
            SDL.EndGPUCopyPass(copy);
            DispatchBatchedVelocity(command);
            if (!retry)
            {
                uniformBytes += SpatialPass(command, 0, _vertexEditCount);
                uniformBytes += SpatialPass(command, 1, _dirtyGeometry.Count);
                uniformBytes += SpatialPass(command, 2, _dirtyShapes.Count);
                uniformBytes += SpatialPass(command, 3, _shapeCapacity);
                if (_treeTopologyDirty) uniformBytes += ResidentTreePass(command, 1, _shapeCapacity);
                uniformBytes += RefitResidentTree(command);
                if (_treeTopologyDirty || ++_refitsSinceSort >= 32)
                {
                    uniformBytes += ResidentTreePass(command, 4, _shapeCapacity);
                    // ponytail: bitonic rebuilds cost O(n log² n); use radix sorting if measured rebuild cost dominates.
                    for (var stage = 2; stage <= _shapeCapacity; stage *= 2)
                        for (var stride = stage / 2; stride > 0; stride /= 2)
                            uniformBytes += ResidentTreePass(command, 5, _shapeCapacity, stage: stage, stride: stride);
                    uniformBytes += RefitResidentTree(command); _refitsSinceSort = 0;
                }
            }
            if (pairs) uniformBytes += SpatialPass(command, 4, _shapeHighWater);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin resident pair summary");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _spatialSummary!.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _spatialDownload!.DangerousGetHandle() });
            if (batchVelocity) SDL.DownloadFromGPUBuffer(copy, new() { Buffer = _status!.DangerousGetHandle(), Size = 8 }, new() { TransferBuffer = _spatialDownload.DangerousGetHandle(), Offset = 8 });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command);
            UploadBytes += 8 + vertexBytes + geometryBytes + shapeBytes; UniformBytes += uniformBytes; ReadbackBytes += 8;
            if (batchVelocity) { UploadBytes += 8; ReadbackBytes += 8; }
            GeometryUploadBytes += vertexBytes + geometryBytes; ShapeUploadBytes += shapeBytes; if (pairs) BroadPhaseSubmissionCount++; else QuerySpatialSubmissionCount++;
            mapped = SDL.MapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle(), false);
            if (mapped == 0) throw GPUPhysicsDevice.Failure("map resident pair summary");
            uint count;
            try
            {
                if (*(uint*)mapped != 0) throw new InvalidOperationException("GPU resident geometry returned invalid bounds or pair state.");
                if (batchVelocity && ((uint*)mapped)[2] != 0) throw new InvalidOperationException("GPU resident velocity work returned invalid state.");
                count = ((uint*)mapped)[1];
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _spatialDownload.DangerousGetHandle()); }
            if (count > int.MaxValue) throw new InvalidOperationException("The GPU pair count exceeds addressable storage.");
            _failed = false; _treeTopologyDirty = false;
            return (int)count;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }

    private void UploadSpatial(nint copy, RenderHandle target, uint offset, uint bytes) =>
        SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _spatialUpload!.DangerousGetHandle(), Offset = offset }, new() { Buffer = target.DangerousGetHandle(), Size = bytes }, false);

    private int SpatialPass(nint command, uint stage, int count)
    {
        if (count == 0) return 0;
        var bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[7];
        bindings[0] = new() { Buffer = _verticesGPU!.DangerousGetHandle() }; bindings[1] = new() { Buffer = _geometryGPU!.DangerousGetHandle() };
        bindings[2] = new() { Buffer = _shapesGPU!.DangerousGetHandle() }; bindings[3] = new() { Buffer = _proxiesGPU!.DangerousGetHandle() };
        bindings[4] = new() { Buffer = _spatialSummary!.DangerousGetHandle() }; bindings[5] = new() { Buffer = _pairsGPU!.DangerousGetHandle() };
        bindings[6] = new() { Buffer = _centers!.DangerousGetHandle() };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)bindings, 7);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident spatial compute");
        SDL.BindGPUComputePipeline(compute, _spatialPipeline!.DangerousGetHandle());
        var inputs = stackalloc nint[8] { _bodies!.DangerousGetHandle(), _vertexEditsGPU!.DangerousGetHandle(), _geometryEditsGPU!.DangerousGetHandle(), _shapeEditsGPU!.DangerousGetHandle(), _nodesGPU!.DangerousGetHandle(), _filterPairsGPU?.DangerousGetHandle() ?? _nodesGPU.DangerousGetHandle(), _jointFiltersGPU?.DangerousGetHandle() ?? _nodesGPU.DangerousGetHandle(), (_positionCorrectionsGPU ?? _shapeEditsGPU!).DangerousGetHandle() };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 8);
        var settings = new SpatialSettings
        {
            Stage = stage,
            Count = (uint)count,
            Bodies = (uint)_highWater,
            Shapes = (uint)_shapeHighWater,
            Leaves = (uint)_shapeCapacity,
            Geometry = (uint)_geometryEntries.Count,
            Vertices = (uint)_vertexCapacity,
            Pairs = (uint)_pairCapacity,
            Tolerances = new(_pairMargin, _pairSweepDelta, _pairSweepCorrections ? 1 : 0, 0),
            CollisionFilters = JointCount > 0 || CollisionExceptionCount > 0 ? (uint)_jointFilterCapacity : 0,
            FilterPairs = (uint)(_jointHighWater + _exceptionHighWater),
            Padding1 = _captureReports ? 1u : 0u
        };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(SpatialSettings));
        SDL.DispatchGPUCompute(compute, ((uint)count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
        return sizeof(SpatialSettings);
    }

    private int RefitResidentTree(nint command)
    {
        var bytes = ResidentTreePass(command, 2, _shapeCapacity);
        for (var start = _shapeCapacity / 2; start > 0; start /= 2) bytes += ResidentTreePass(command, 3, start, start);
        return bytes;
    }
    private int ResidentTreePass(nint command, uint operation, int count, int start = 0, int stage = 0, int stride = 0)
    {
        var bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[3];
        bindings[0] = new() { Buffer = _proxiesGPU!.DangerousGetHandle() }; bindings[1] = new() { Buffer = _nodesGPU!.DangerousGetHandle() }; bindings[2] = new() { Buffer = _orderGPU!.DangerousGetHandle() };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)bindings, 3);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident tree maintenance");
        SDL.BindGPUComputePipeline(compute, _residentTreePipeline!.DangerousGetHandle());
        // Update stage zero is unused: resident bounds already wrote proxies on the GPU.
        var unusedUpdates = _shapeEditsGPU!.DangerousGetHandle(); SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)(&unusedUpdates), 1);
        var settings = new TreeSettings { Count = (uint)count, Leaves = (uint)_shapeCapacity, Operation = operation, Start = (uint)start, Stage = (uint)stage, Stride = (uint)stride };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(TreeSettings));
        SDL.DispatchGPUCompute(compute, ((uint)count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
        return sizeof(TreeSettings);
    }
    private void DownloadSpatial(RenderHandle source, uint offset, uint bytes)
    {
        GrowTransfer(ref _spatialDownload, ref _spatialDownloadBytes, checked((int)bytes), SDL.GPUTransferBufferUsage.Download);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw GPUPhysicsDevice.Failure("acquire spatial read commands");
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw GPUPhysicsDevice.Failure("begin spatial read");
            SDL.DownloadFromGPUBuffer(copy, new() { Buffer = source.DangerousGetHandle(), Offset = offset, Size = bytes }, new() { TransferBuffer = _spatialDownload!.DangerousGetHandle() });
            SDL.EndGPUCopyPass(copy); _failed = true; Finish(ref command); _failed = false; ReadbackBytes += bytes;
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }
    private void DisposeSpatial()
    {
        _spatialPipeline?.Dispose(); _residentTreePipeline?.Dispose();
        _verticesGPU?.Dispose(); _geometryGPU?.Dispose(); _shapesGPU?.Dispose(); _proxiesGPU?.Dispose(); _nodesGPU?.Dispose(); _orderGPU?.Dispose(); _pairsGPU?.Dispose(); _spatialSummary?.Dispose();
        _vertexEditsGPU?.Dispose(); _geometryEditsGPU?.Dispose(); _shapeEditsGPU?.Dispose(); _spatialUpload?.Dispose(); _spatialDownload?.Dispose();
        foreach (var entry in _geometryEntries) entry.Source = null;
        _geometryByResource.Clear(); _geometryEntries.Clear(); Array.Clear(_shapeSlots);
    }
}
