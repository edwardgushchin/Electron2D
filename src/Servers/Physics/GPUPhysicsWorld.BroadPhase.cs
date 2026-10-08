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
        internal int Escape, ProxyKey, HasCategory, ShapeIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TreeQuery
    {
        internal Float4 Bounds;
        internal int ProxyKey, Offset, Count, Capacity;
        internal int ShapeIndex, Padding1, Padding2, Padding3;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BroadPhaseShape
    {
        internal uint CategoryLow, CategoryHigh, MaskLow, MaskHigh;
        internal int BodyIndex, GroupIndex, JointHead, JointCount;
        internal int Flags, Padding1, Padding2, Padding3;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BroadPhaseJoint
    {
        internal int BodyA, BodyB, NextA, NextB;
        internal int CollideConnected, Padding1, Padding2, Padding3;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BroadPhaseStep
    {
        internal int QueryCount, DynamicStart, NodeCount, PairMask;
    }

    private readonly Storage<TreeNode> _treeStorage;
    private readonly Storage<TreeQuery> _treeQueryStorage;
    private readonly Storage<int> _treeCandidateStorage;
    private readonly Storage<BroadPhaseShape> _broadShapeStorage;
    private readonly Storage<BroadPhaseJoint> _broadJointStorage;
    private readonly Storage<uint> _existingPairStorage;
    private int[] _candidateCapacities = [];
    internal int BroadPhaseCandidateCount { get; private set; }
    internal int BroadPhaseRetryCount { get; private set; }
    internal long BroadPhaseCandidateTotal { get; private set; }
    internal long BroadPhaseUploadBytes { get; private set; }
    internal long BroadPhaseReadbackBytes { get; private set; }
    internal readonly double[] BroadPhaseProfileMS = new double[4];

    internal void FindBroadPhasePairs(B2World world)
    {
        EnsureOwner();
        var bp = world.broadPhase;
        var count = bp.moveArray.count;
        BroadPhaseCandidateCount = 0;
        BroadPhaseRetryCount = 0;
        if (count == 0) return;
        var profileStart = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        var priorCommands = BroadPhaseProfileMS[1]; var priorWait = BroadPhaseProfileMS[2];
        var nodes = checked(bp.trees[0].nodeCount + bp.trees[1].nodeCount + bp.trees[2].nodeCount);
        _treeStorage.Reserve(Math.Max(1, nodes));
        _treeQueryStorage.Reserve(count);
        _treeCandidateStorage.Reserve(1);
        _broadShapeStorage.Reserve(Math.Max(1, world.shapes.count));
        _broadJointStorage.Reserve(Math.Max(1, world.joints.count));
        PreparePairTable(world);
        for (var i = 0; i < world.joints.count; i++)
        {
            var joint = world.joints.data[i];
            _broadJointStorage.Data[i] = new()
            {
                BodyA = joint.edges[0].bodyId,
                BodyB = joint.edges[1].bodyId,
                NextA = joint.edges[0].nextKey,
                NextB = joint.edges[1].nextKey,
                CollideConnected = joint.collideConnected ? 1 : 0
            };
        }
        // Pack by shape slot, not tree visitation: body/filter records are reused
        // by every query, while moved flags can be marked directly from moveArray.
        for (var i = 0; i < world.shapes.count; i++)
        {
            var shape = world.shapes.data[i];
            if (shape.proxyKey == -1) continue;
            var body = world.bodies.data[shape.bodyId];
            _broadShapeStorage.Data[i] = new()
            {
                CategoryLow = (uint)shape.filter.categoryBits,
                CategoryHigh = (uint)(shape.filter.categoryBits >> 32),
                MaskLow = (uint)shape.filter.maskBits,
                MaskHigh = (uint)(shape.filter.maskBits >> 32),
                BodyIndex = shape.bodyId,
                GroupIndex = shape.filter.groupIndex,
                JointHead = body.headJointKey,
                JointCount = body.jointCount,
                Flags = shape.sensorIndex != -1 ? 1 : 0
            };
        }
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
                var shape = b2BroadPhase_GetShapeIndex(bp, key);
                _broadShapeStorage.Data[shape].Flags |= 2;
                _treeQueryStorage.Data[i].ShapeIndex = shape;
                _treeQueryStorage.Data[i].Capacity = Math.Max(16, _candidateCapacities[shape]);
            }
        }

        var settings = new BroadPhaseStep { QueryCount = count, DynamicStart = dynamicStart, NodeCount = cursor, PairMask = _existingPairStorage.Data.Length - 1 };
        var profilePacked = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
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
            DispatchBroadPhase(world, in settings, slots, BroadPhaseRetryCount == 0);
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

        // The GPU has completed built-in filters; user code stays on the owner.
        // No callbacks or pair publication occur before the whole batch fits.
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
                var otherShape = (int)b2DynamicTree_GetUserData(bp.trees[type], proxy);
                var first = key < query.ProxyKey;
                b2AddFilteredPair(first ? otherShape : query.ShapeIndex, first ? query.ShapeIndex : otherShape, ref context);
            }
        }
        BroadPhaseCandidateTotal += BroadPhaseCandidateCount;
        if (PhysicsSpace.ProfilingEnabled)
        {
            BroadPhaseProfileMS[0] += System.Diagnostics.Stopwatch.GetElapsedTime(profileStart, profilePacked).TotalMilliseconds;
            BroadPhaseProfileMS[3] += System.Diagnostics.Stopwatch.GetElapsedTime(profilePacked).TotalMilliseconds -
                (BroadPhaseProfileMS[1] - priorCommands) - (BroadPhaseProfileMS[2] - priorWait);
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
            HasCategory = node.categoryBits != 0 ? 1 : 0,
            ShapeIndex = leaf ? (int)node.children.userData : -1
        };
    }

    private void DispatchBroadPhase(B2World world, in BroadPhaseStep settings, int candidates, bool uploadTree)
    {
        var profileStart = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire broad-phase commands");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin broad-phase upload");
            if (uploadTree)
            {
                _treeStorage.Upload(copy, settings.NodeCount);
                _broadShapeStorage.Upload(copy, world.shapes.count);
                _broadJointStorage.Upload(copy, world.joints.count);
                UploadPairChanges(copy);
                BroadPhaseUploadBytes += (long)settings.NodeCount * sizeof(TreeNode) + (long)world.shapes.count * sizeof(BroadPhaseShape) +
                    (long)world.joints.count * sizeof(BroadPhaseJoint);
            }
            _treeQueryStorage.Upload(copy, settings.QueryCount);
            BroadPhaseUploadBytes += (long)settings.QueryCount * sizeof(TreeQuery);
            SDL.EndGPUCopyPass(copy);
            if (uploadTree) UpdatePairTable(command);
            Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[2];
            bindings[0] = new() { Buffer = _treeQueryStorage.Handle };
            bindings[1] = new() { Buffer = _treeCandidateStorage.Handle };
            var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 2);
            if (compute == 0) throw Failure("begin broad-phase traversal");
            SDL.BindGPUComputePipeline(compute, _broadPhase.DangerousGetHandle());
            nint* inputs = stackalloc nint[] { _treeStorage.Handle, _broadShapeStorage.Handle, _broadJointStorage.Handle, _existingPairStorage.Handle, _pairKeyStorage.Handle };
            SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 5);
            fixed (BroadPhaseStep* uniform = &settings) SDL.PushGPUComputeUniformData(command, 0, (nint)uniform, (uint)sizeof(BroadPhaseStep));
            SDL.DispatchGPUCompute(compute, checked((uint)(settings.QueryCount + 63) / 64), 1, 1);
            SDL.EndGPUComputePass(compute);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin broad-phase readback");
            _treeQueryStorage.Download(copy, settings.QueryCount);
            _treeCandidateStorage.Download(copy, candidates);
            _pairStatusStorage.Download(copy, 1);
            SDL.EndGPUCopyPass(copy);
            var profileRecorded = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            var submitted = command; command = 0;
            fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit broad-phase traversal");
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for broad-phase traversal");
            _treeQueryStorage.Read(settings.QueryCount);
            _treeCandidateStorage.Read(candidates);
            _pairStatusStorage.Read(1);
            if (_pairStatusStorage.Data[0] != 0)
                throw new InvalidOperationException($"GPU pair-table maintenance failed: {_pairStatusStorage.Data[0]}.");
            if (uploadTree) CommitPairTable(world);
            BroadPhaseReadbackBytes += (long)settings.QueryCount * sizeof(TreeQuery) + (long)candidates * sizeof(int) + sizeof(uint);
            if (PhysicsSpace.ProfilingEnabled)
            {
                BroadPhaseProfileMS[1] += System.Diagnostics.Stopwatch.GetElapsedTime(profileStart, profileRecorded).TotalMilliseconds;
                BroadPhaseProfileMS[2] += System.Diagnostics.Stopwatch.GetElapsedTime(profileRecorded).TotalMilliseconds;
            }
            DispatchCount++;
        }
        finally
        {
            if (command != 0) SDL.CancelGPUCommandBuffer(command);
            if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
        }
    }
}
