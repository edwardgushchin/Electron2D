using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using static Box2D.NET.B2BoardPhases;
using static Box2D.NET.B2DynamicTrees;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct TreeNode
    {
        internal Float4 Bounds;
        internal int Escape, ProxyKey, HasCategory, Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TreeQuery
    {
        internal Float4 Bounds;
        internal int ProxyKey, Offset, Count, Capacity;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BroadPhaseStep
    {
        internal int QueryCount, DynamicStart, NodeCount, Padding;
    }

    private readonly Storage<TreeNode> _treeStorage;
    private readonly Storage<TreeQuery> _treeQueryStorage;
    private readonly Storage<int> _treeCandidateStorage;
    private int[] _candidateCapacities = [];
    internal int BroadPhaseCandidateCount { get; private set; }
    internal int BroadPhaseRetryCount { get; private set; }

    internal void FindBroadPhasePairs(B2World world)
    {
        EnsureOwner();
        var bp = world.broadPhase;
        var count = bp.moveArray.count;
        BroadPhaseCandidateCount = 0;
        BroadPhaseRetryCount = 0;
        if (count == 0) return;
        var nodes = checked(bp.trees[0].nodeCount + bp.trees[1].nodeCount + bp.trees[2].nodeCount);
        _treeStorage.Reserve(Math.Max(1, nodes));
        _treeQueryStorage.Reserve(count);
        _treeCandidateStorage.Reserve(1);
        if (_candidateCapacities.Length < world.shapes.count)
            Array.Resize(ref _candidateCapacities, checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)world.shapes.count)));

        // Threaded pre-order preserves the CPU query's child2-before-child1 order.
        // Escape links avoid a fixed per-invocation stack or a traversal depth cap.
        // ponytail: each moving batch uploads the full tree; resident updates belong with GPU tree maintenance.
        var cursor = 0;
        PackTree(bp.trees[1], B2BodyType.b2_kinematicBody, ref cursor);
        PackTree(bp.trees[0], B2BodyType.b2_staticBody, ref cursor);
        var dynamicStart = cursor;
        PackTree(bp.trees[2], B2BodyType.b2_dynamicBody, ref cursor);
        for (var i = 0; i < count; i++)
        {
            var key = bp.moveArray.data[i];
            _treeQueryStorage.Data[i] = new() { ProxyKey = key };
            if (key != -1)
            {
                _treeQueryStorage.Data[i].Bounds = Bounds(b2DynamicTree_GetAABB(bp.trees[(int)B2_PROXY_TYPE(key)], B2_PROXY_ID(key)));
                _treeQueryStorage.Data[i].Capacity = Math.Max(16, _candidateCapacities[b2BroadPhase_GetShapeIndex(bp, key)]);
            }
        }

        var settings = new BroadPhaseStep { QueryCount = count, DynamicStart = dynamicStart, NodeCount = cursor };
        var maxCount = bp.trees[0].proxyCount + bp.trees[1].proxyCount + bp.trees[2].proxyCount;
        while (true)
        {
            var slots = 0;
            for (var i = 0; i < count; i++)
            {
                ref var query = ref _treeQueryStorage.Data[i];
                query.Offset = slots;
                slots = checked(slots + query.Capacity);
            }
            _treeCandidateStorage.Reserve(Math.Max(1, slots));
            DispatchBroadPhase(in settings, slots, BroadPhaseRetryCount == 0);
            var overflow = false;
            BroadPhaseCandidateCount = 0;
            for (var i = 0; i < count; i++)
            {
                ref var query = ref _treeQueryStorage.Data[i];
                if ((uint)query.Count > (uint)maxCount)
                    throw new InvalidOperationException("GPU broad phase returned an invalid candidate count.");
                BroadPhaseCandidateCount = checked(BroadPhaseCandidateCount + query.Count);
                if (query.Count <= query.Capacity) continue;
                query.Capacity = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)query.Count));
                _candidateCapacities[b2BroadPhase_GetShapeIndex(bp, query.ProxyKey)] = query.Capacity;
                overflow = true;
            }
            if (!overflow) break;
            // Counts are complete even when output is full. Retry this immutable
            // query batch with grown storage before publishing any contacts.
            if (++BroadPhaseRetryCount > 1)
                throw new InvalidOperationException("GPU broad-phase counts changed during an immutable query batch.");
        }

        // Complete candidate ranges are available before invoking shared filters.
        // Existing contacts, moved-pair deduplication, joints and user callbacks
        // retain their original semantics, including deterministic list order.
        var context = new B2QueryPairContext { world = world };
        for (var i = 0; i < count; i++)
        {
            ref readonly var query = ref _treeQueryStorage.Data[i];
            context.moveResult = bp.moveResults[i];
            context.moveResult.pairList = null!;
            if (query.ProxyKey == -1) continue;
            context.queryProxyKey = query.ProxyKey;
            context.queryShapeIndex = b2BroadPhase_GetShapeIndex(bp, query.ProxyKey);
            var end = query.Offset + query.Count;
            for (var j = query.Offset; j < end; j++)
            {
                var key = _treeCandidateStorage.Data[j];
                var type = (int)B2_PROXY_TYPE(key);
                var proxy = B2_PROXY_ID(key);
                if ((uint)type >= 3 || (uint)proxy >= (uint)bp.trees[type].nodeCapacity ||
                    !b2IsLeaf(bp.trees[type].nodes[proxy]) || !b2IsAllocated(bp.trees[type].nodes[proxy]))
                    throw new InvalidOperationException("GPU broad phase returned an invalid proxy.");
                context.queryTreeType = (B2BodyType)type;
                b2PairQueryCallback(proxy, b2DynamicTree_GetUserData(bp.trees[type], proxy), ref context);
            }
        }
    }

    private static Float4 Bounds(in B2AABB box) => new(box.lowerBound.X, box.lowerBound.Y, box.upperBound.X, box.upperBound.Y);

    private void PackTree(B2DynamicTree tree, B2BodyType type, ref int cursor)
    {
        if (tree.nodeCount != 0) PackTreeNode(tree, type, tree.root, ref cursor);
    }

    private void PackTreeNode(B2DynamicTree tree, B2BodyType type, int index, ref int cursor)
    {
        ref readonly var node = ref tree.nodes[index];
        var slot = cursor++;
        var leaf = b2IsLeaf(node);
        if (!leaf)
        {
            PackTreeNode(tree, type, node.children.child2, ref cursor);
            PackTreeNode(tree, type, node.children.child1, ref cursor);
        }
        _treeStorage.Data[slot] = new()
        {
            Bounds = Bounds(in node.aabb),
            Escape = cursor,
            ProxyKey = leaf ? B2_PROXY_KEY(index, type) : -1,
            HasCategory = node.categoryBits != 0 ? 1 : 0
        };
    }

    private void DispatchBroadPhase(in BroadPhaseStep settings, int candidates, bool uploadTree)
    {
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire broad-phase commands");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin broad-phase upload");
            if (uploadTree) _treeStorage.Upload(copy, settings.NodeCount);
            _treeQueryStorage.Upload(copy, settings.QueryCount);
            SDL.EndGPUCopyPass(copy);
            Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[2];
            bindings[0] = new() { Buffer = _treeQueryStorage.Handle };
            bindings[1] = new() { Buffer = _treeCandidateStorage.Handle };
            var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 2);
            if (compute == 0) throw Failure("begin broad-phase traversal");
            SDL.BindGPUComputePipeline(compute, _broadPhase.DangerousGetHandle());
            var tree = _treeStorage.Handle;
            SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)(&tree), 1);
            fixed (BroadPhaseStep* uniform = &settings) SDL.PushGPUComputeUniformData(command, 0, (nint)uniform, (uint)sizeof(BroadPhaseStep));
            SDL.DispatchGPUCompute(compute, checked((uint)(settings.QueryCount + 63) / 64), 1, 1);
            SDL.EndGPUComputePass(compute);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin broad-phase readback");
            _treeQueryStorage.Download(copy, settings.QueryCount);
            _treeCandidateStorage.Download(copy, candidates);
            SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0;
            fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit broad-phase traversal");
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for broad-phase traversal");
            _treeQueryStorage.Read(settings.QueryCount);
            _treeCandidateStorage.Read(candidates);
            DispatchCount++;
        }
        finally
        {
            if (command != 0) SDL.CancelGPUCommandBuffer(command);
            if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
        }
    }
}
