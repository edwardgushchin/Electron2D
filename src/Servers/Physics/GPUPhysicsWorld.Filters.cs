using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FilterStep
    {
        internal int ShapeCount, JointCount, QueryCount, Epoch;
        internal int Operation, Padding1, Padding2, Padding3;
    }

    private readonly Storage<BroadPhaseShape> _shapeFilterUpdateStorage;
    private readonly Storage<BroadPhaseJoint> _jointFilterUpdateStorage;
    private readonly Action<int> _shapeFilterChanged, _jointFilterChanged;
    // Low bit distinguishes a joint slot from a shape slot.
    private readonly List<int> _filterChanges = [];
    private bool[] _filterDirty = [];
    private B2World? _filterTrackingWorld;
    private B2BroadPhase? _residentFilterSource;
    private bool _resetFilters;
    private int _shapeFilterCount, _jointFilterCount, _filterEpoch;
    internal long FilterSnapshotCount { get; private set; }
    internal long FilterUpdatedShapes { get; private set; }
    internal long FilterUpdatedJoints { get; private set; }
    internal long FilterUploadBytes { get; private set; }

    private void ReserveFilterJournal(int capacity)
    {
        if (_filterDirty.Length >= capacity) return;
        capacity = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(64, capacity)));
        Array.Resize(ref _filterDirty, capacity);
        _filterChanges.EnsureCapacity(capacity);
    }

    private void MarkFilterChanged(int key)
    {
        ReserveFilterJournal(checked(key + 1));
        if (_filterDirty[key]) return;
        _filterDirty[key] = true;
        _filterChanges.Add(key);
    }

    private void MarkShapeFilterChanged(int id) => MarkFilterChanged(checked(2 * id));

    private void MarkJointFilterChanged(int id)
    {
        MarkFilterChanged(checked(2 * id + 1));
        var world = _filterTrackingWorld!;
        var joint = world.joints.data[id];
        // Shapes cache their body's adjacency head/count. Read final values when
        // packing, including changes made later in the same create/destroy call.
        for (var edge = 0; edge < 2; edge++)
            for (var shape = world.bodies.data[joint.edges[edge].bodyId].headShapeId; shape != -1; shape = world.shapes.data[shape].nextShapeId)
                MarkShapeFilterChanged(shape);
    }

    private void ClearFilterJournal()
    {
        foreach (var key in _filterChanges) _filterDirty[key] = false;
        _filterChanges.Clear();
    }

    private void DetachFilterTracking()
    {
        if (_filterTrackingWorld is not null)
        {
            if (_filterTrackingWorld.shapeFilterChanged == _shapeFilterChanged) _filterTrackingWorld.shapeFilterChanged = null!;
            if (_filterTrackingWorld.jointFilterChanged == _jointFilterChanged) _filterTrackingWorld.jointFilterChanged = null!;
        }
        _filterTrackingWorld = null;
        _residentFilterSource = null;
        ClearFilterJournal();
    }

    private static BroadPhaseShape ShapeFilter(B2World world, int id)
    {
        var shape = world.shapes.data[id];
        if (shape.id != id) return new() { ID = id, BodyIndex = -1, JointHead = -1 };
        var body = world.bodies.data[shape.bodyId];
        return new()
        {
            CategoryLow = (uint)shape.filter.categoryBits,
            CategoryHigh = (uint)(shape.filter.categoryBits >> 32),
            MaskLow = (uint)shape.filter.maskBits,
            MaskHigh = (uint)(shape.filter.maskBits >> 32),
            BodyIndex = shape.bodyId,
            GroupIndex = shape.filter.groupIndex,
            JointHead = body.headJointKey,
            JointCount = body.jointCount,
            Flags = shape.sensorIndex != -1 ? 1 : 0,
            ID = id
        };
    }

    private static BroadPhaseJoint JointFilter(B2World world, int id)
    {
        var joint = world.joints.data[id];
        if (joint.jointId != id) return new() { ID = id, BodyA = -1, BodyB = -1, NextA = -1, NextB = -1 };
        return new()
        {
            BodyA = joint.edges[0].bodyId,
            BodyB = joint.edges[1].bodyId,
            NextA = joint.edges[0].nextKey,
            NextB = joint.edges[1].nextKey,
            CollideConnected = joint.collideConnected ? 1 : 0,
            ID = id
        };
    }

    private void PrepareFilters(B2World world)
    {
        _filterEpoch = unchecked(_filterEpoch + 1);
        _resetFilters = _filterEpoch == 0 || !ReferenceEquals(_residentFilterSource, world.broadPhase) ||
            !ReferenceEquals(_filterTrackingWorld, world) || world.shapeFilterChanged != _shapeFilterChanged ||
            world.jointFilterChanged != _jointFilterChanged || world.shapes.capacity > _broadShapeStorage.Data.Length ||
            world.joints.capacity > _broadJointStorage.Data.Length;
        // Zero belongs to shapes absent from this batch. Reset on wrap so an old
        // moved marker cannot alias a new query epoch.
        if (_filterEpoch == 0) _filterEpoch = 1;
        if (!ReferenceEquals(_filterTrackingWorld, world) || world.shapeFilterChanged != _shapeFilterChanged || world.jointFilterChanged != _jointFilterChanged)
        {
            DetachFilterTracking();
            _filterTrackingWorld = world;
            world.shapeFilterChanged = _shapeFilterChanged;
            world.jointFilterChanged = _jointFilterChanged;
        }
        _residentFilterSource = null;
        _broadShapeStorage.Reserve(Math.Max(1, world.shapes.capacity));
        _broadJointStorage.Reserve(Math.Max(1, world.joints.capacity));
        _shapeFilterUpdateStorage.Reserve(_broadShapeStorage.Data.Length);
        _jointFilterUpdateStorage.Reserve(_broadJointStorage.Data.Length);
        ReserveFilterJournal(checked(2 * Math.Max(_broadShapeStorage.Data.Length, _broadJointStorage.Data.Length)));
        _shapeFilterCount = _jointFilterCount = 0;
        if (_resetFilters)
        {
            for (var i = 0; i < world.shapes.count; i++) _broadShapeStorage.Data[i] = ShapeFilter(world, i);
            for (var i = 0; i < world.joints.count; i++) _broadJointStorage.Data[i] = JointFilter(world, i);
            _shapeFilterCount = world.shapes.count;
            _jointFilterCount = world.joints.count;
        }
        else
        {
            foreach (var key in _filterChanges)
                if ((key & 1) == 0) _shapeFilterUpdateStorage.Data[_shapeFilterCount++] = ShapeFilter(world, key >> 1);
                else _jointFilterUpdateStorage.Data[_jointFilterCount++] = JointFilter(world, key >> 1);
        }
    }

    private void UploadFilterChanges(nint copy)
    {
        (_resetFilters ? _broadShapeStorage : _shapeFilterUpdateStorage).Upload(copy, _shapeFilterCount);
        (_resetFilters ? _broadJointStorage : _jointFilterUpdateStorage).Upload(copy, _jointFilterCount);
        var bytes = (long)_shapeFilterCount * sizeof(BroadPhaseShape) + (long)_jointFilterCount * sizeof(BroadPhaseJoint);
        FilterUploadBytes += bytes;
        BroadPhaseUploadBytes += bytes;
    }

    private void UpdateFilters(nint command, int queryCount)
    {
        if (!_resetFilters && (_shapeFilterCount != 0 || _jointFilterCount != 0))
            FilterPass(command, 0, queryCount, Math.Max(_shapeFilterCount, _jointFilterCount));
        // Mark directly from the query buffer already uploaded for traversal.
        // A separate pass orders writes after metadata updates and before reads.
        FilterPass(command, 1, queryCount, queryCount);
    }

    private void FilterPass(nint command, int operation, int queryCount, int count)
    {
        Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[2];
        bindings[0] = new() { Buffer = _broadShapeStorage.Handle };
        bindings[1] = new() { Buffer = _broadJointStorage.Handle };
        var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 2);
        if (compute == 0) throw Failure("begin resident filter maintenance");
        SDL.BindGPUComputePipeline(compute, _filterPipeline.DangerousGetHandle());
        nint* inputs = stackalloc nint[] { _shapeFilterUpdateStorage.Handle, _jointFilterUpdateStorage.Handle, _treeQueryStorage.Handle };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 3);
        var settings = new FilterStep { ShapeCount = _shapeFilterCount, JointCount = _jointFilterCount, QueryCount = queryCount, Epoch = _filterEpoch, Operation = operation };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(FilterStep));
        SDL.DispatchGPUCompute(compute, checked((uint)(count + 63) / 64), 1, 1);
        SDL.EndGPUComputePass(compute);
        DispatchCount++;
    }

    private void CommitFilters(B2World world)
    {
        if (_resetFilters) FilterSnapshotCount++;
        else { FilterUpdatedShapes += _shapeFilterCount; FilterUpdatedJoints += _jointFilterCount; }
        _residentFilterSource = world.broadPhase;
        ClearFilterJournal();
    }
}
