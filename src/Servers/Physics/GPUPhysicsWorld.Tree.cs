using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using static Box2D.NET.B2BoardPhases;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct TreeProxy
    {
        internal Float4 Bounds;
        internal int ProxyKey, ShapeIndex, HasCategory, Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TreeOrder
    {
        internal uint Key, Index;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TreeBuildStep
    {
        internal uint Count, LeafCount, Operation, Start;
        internal uint Stage, Stride, Padding1, Padding2;
    }

    private readonly Storage<TreeProxy> _proxyStorage, _proxyUpdateStorage;
    private readonly Storage<TreeOrder> _treeOrderStorage;
    private readonly Action<int, bool> _proxyChanged;
    private readonly Comparison<int> _compareCandidateOrder;
    private readonly List<int> _proxyChanges = [];
    private bool[] _proxyDirty = [];
    private int[] _candidateOrder = [];
    private B2BroadPhase? _proxyTrackingSource, _residentTreeSource;
    private bool _treeTopologyChanged, _resetTree, _sortTree;
    private int _proxyUpdateCount, _movesSinceSort;
    internal long TreeSnapshotCount { get; private set; }
    internal long TreeRebuildCount { get; private set; }
    internal long TreeRefitCount { get; private set; }
    internal long TreeUpdatedProxies { get; private set; }
    internal long TreeUploadBytes { get; private set; }

    private int CompareCandidateOrder(int a, int b) => _candidateOrder[a].CompareTo(_candidateOrder[b]);

    private void ReserveProxyJournal(int count)
    {
        if (_proxyDirty.Length >= count) return;
        count = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(64, count)));
        Array.Resize(ref _proxyDirty, count);
        _proxyChanges.EnsureCapacity(count);
    }

    private void MarkProxyChanged(int shape, bool topology)
    {
        ReserveProxyJournal(checked(shape + 1));
        _treeTopologyChanged |= topology;
        if (_proxyDirty[shape]) return;
        _proxyDirty[shape] = true;
        _proxyChanges.Add(shape);
    }

    private void ClearProxyJournal()
    {
        foreach (var id in _proxyChanges) _proxyDirty[id] = false;
        _proxyChanges.Clear();
        _treeTopologyChanged = false;
    }

    private void DetachProxyTracking()
    {
        if (_proxyTrackingSource?.trees is { } trees)
            foreach (var tree in trees)
                if (tree.proxyChanged == _proxyChanged) tree.proxyChanged = null!;
        _proxyTrackingSource = _residentTreeSource = null;
        ClearProxyJournal();
    }

    private static TreeProxy ReadProxy(B2World world, int id)
    {
        var result = new TreeProxy { ProxyKey = -1, ShapeIndex = id };
        if (id >= world.shapes.count) return result;
        var key = world.shapes.data[id].proxyKey;
        if (key == -1) return result;
        ref readonly var node = ref world.broadPhase.trees[(int)B2_PROXY_TYPE(key)].nodes[B2_PROXY_ID(key)];
        result.Bounds = Bounds(node.aabb);
        if (!Finite(result.Bounds) || result.Bounds.X > result.Bounds.Z || result.Bounds.Y > result.Bounds.W)
            throw new InvalidOperationException("GPU tree proxy bounds must be finite and ordered.");
        result.ProxyKey = key;
        result.HasCategory = node.categoryBits != 0 ? 1 : 0;
        return result;
    }

    private void PrepareTree(B2World world)
    {
        var source = world.broadPhase;
        var changed = !ReferenceEquals(_proxyTrackingSource, source);
        foreach (var tree in source.trees) changed |= tree.proxyChanged != _proxyChanged;
        _resetTree = changed || !ReferenceEquals(_residentTreeSource, source) || world.shapes.capacity > _proxyStorage.Data.Length;
        if (changed)
        {
            DetachProxyTracking();
            _proxyTrackingSource = source;
            foreach (var tree in source.trees) tree.proxyChanged = _proxyChanged;
        }
        _residentTreeSource = null;
        _proxyStorage.Reserve(Math.Max(1, world.shapes.capacity));
        var count = _proxyStorage.Data.Length;
        _proxyUpdateStorage.Reserve(count);
        _treeOrderStorage.Reserve(count);
        _treeStorage.Reserve(checked(2 * count));
        if (_candidateOrder.Length < count) Array.Resize(ref _candidateOrder, count);
        ReserveProxyJournal(count);
        _proxyUpdateCount = _resetTree ? 0 : _proxyChanges.Count;
        if (_resetTree)
            for (var i = 0; i < count; i++) _proxyStorage.Data[i] = ReadProxy(world, i);
        else
            for (var i = 0; i < _proxyUpdateCount; i++) _proxyUpdateStorage.Data[i] = ReadProxy(world, _proxyChanges[i]);
        _sortTree = _resetTree || _treeTopologyChanged || _movesSinceSort + _proxyUpdateCount >= count / 2;
    }

    private void UploadTreeChanges(nint copy)
    {
        var count = _resetTree ? _proxyStorage.Data.Length : _proxyUpdateCount;
        if (_resetTree) _proxyStorage.Upload(copy, count);
        else _proxyUpdateStorage.Upload(copy, count);
        var bytes = (long)count * sizeof(TreeProxy);
        TreeUploadBytes += bytes;
        BroadPhaseUploadBytes += bytes;
    }

    private void UpdateTree(nint command)
    {
        var count = _proxyStorage.Data.Length;
        if (_resetTree) TreePass(command, 1, count);
        else if (_proxyUpdateCount != 0) TreePass(command, 0, _proxyUpdateCount);
        if (!_resetTree && _proxyUpdateCount == 0) return;
        RefitTree(command);
        if (!_sortTree) return;
        TreePass(command, 4, count);
        // ponytail: bitonic sorting is O(n log² n); use parallel radix sorting if rebuild timings dominate.
        for (var stage = 2; stage <= count; stage *= 2)
            for (var stride = stage / 2; stride > 0; stride /= 2)
                TreePass(command, 5, count, stage: stage, stride: stride);
        RefitTree(command);
    }

    private void RefitTree(nint command)
    {
        var count = _proxyStorage.Data.Length;
        TreePass(command, 2, count);
        for (var start = count / 2; start > 0; start /= 2) TreePass(command, 3, start, start);
    }

    private void TreePass(nint command, uint operation, int count, int start = 0, int stage = 0, int stride = 0)
    {
        Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[3];
        bindings[0] = new() { Buffer = _proxyStorage.Handle };
        bindings[1] = new() { Buffer = _treeStorage.Handle };
        bindings[2] = new() { Buffer = _treeOrderStorage.Handle };
        var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 3);
        if (compute == 0) throw Failure("begin GPU tree maintenance");
        SDL.BindGPUComputePipeline(compute, _treePipeline.DangerousGetHandle());
        var updates = _proxyUpdateStorage.Handle;
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)(&updates), 1);
        var settings = new TreeBuildStep
        {
            Count = (uint)count,
            LeafCount = (uint)_proxyStorage.Data.Length,
            Operation = operation,
            Start = (uint)start,
            Stage = (uint)stage,
            Stride = (uint)stride
        };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(TreeBuildStep));
        SDL.DispatchGPUCompute(compute, checked((uint)(count + 63) / 64), 1, 1);
        SDL.EndGPUComputePass(compute);
        DispatchCount++;
    }

    private void CommitTree(B2World world)
    {
        if (_resetTree) TreeSnapshotCount++;
        if (_sortTree) { TreeRebuildCount++; _movesSinceSort = 0; }
        else _movesSinceSort += _proxyUpdateCount;
        if (_resetTree || _proxyUpdateCount != 0) TreeRefitCount++;
        TreeUpdatedProxies += _proxyUpdateCount;
        _residentTreeSource = world.broadPhase;
        ClearProxyJournal();
    }
}
