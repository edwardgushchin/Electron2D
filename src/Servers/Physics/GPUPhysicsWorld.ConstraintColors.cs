using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using static Box2D.NET.B2ConstraintGraphs;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ColorChange
    {
        internal int Kind, ID, A, B, Dynamic, Color;
        internal uint Generation;
        internal int Padding;
    }
    private readonly Storage<ColorChange> _colorChanges;
    private readonly Storage<uint> _colorMasks;
    private readonly Storage<int> _colorResults;
    private readonly Action<B2World> _beginConstraintColors, _finishConstraintColors;
    private readonly Func<B2World, int, int, int, int, int, int> _selectConstraintColor;
    private B2World? _colorWorld;
    private bool[] _colorWoken = [];
    private int _colorCount, _colorCursor, _colorWords;
    private bool _colorsReady;
    internal long ConstraintColorSubmissionCount { get; private set; }
    internal long ConstraintColorChangeCount { get; private set; }
    internal long ConstraintColorTransferBytes { get; private set; }
    internal readonly double[] ConstraintColorProfileMS = new double[3];

    internal void EnableConstraintColors(B2World world)
    {
        EnsureOwner(); DetachConstraintColors(); _colorWorld = world;
        world.beginConstraintColors = _beginConstraintColors;
        world.finishConstraintColors = _finishConstraintColors;
        world.selectConstraintColor = _selectConstraintColor;
    }

    private void DetachConstraintColors()
    {
        if (_colorWorld is not null)
        {
            if (_colorWorld.beginConstraintColors == _beginConstraintColors) _colorWorld.beginConstraintColors = null!;
            if (_colorWorld.finishConstraintColors == _finishConstraintColors) _colorWorld.finishConstraintColors = null!;
            if (_colorWorld.selectConstraintColor == _selectConstraintColor) _colorWorld.selectConstraintColor = null!;
        }
        _colorWorld = null; _colorsReady = false;
    }

    private void AddColorChange(B2World world, int kind, int id, int color)
    {
        var a = kind == 0 ? world.contacts.data[id].edges[0].bodyId : world.joints.data[id].edges[0].bodyId;
        var b = kind == 0 ? world.contacts.data[id].edges[1].bodyId : world.joints.data[id].edges[1].bodyId;
        _colorChanges.Data[_colorCount++] = new()
        {
            Kind = kind,
            ID = id,
            A = a,
            B = b,
            Color = color,
            Dynamic = (world.bodies.data[a].type == B2BodyType.b2_dynamicBody ? 1 : 0) | (world.bodies.data[b].type == B2BodyType.b2_dynamicBody ? 2 : 0),
            Generation = kind == 0 ? world.contacts.data[id].generation : world.joints.data[id].generation
        };
    }

    private int ColorSet(B2World world, int body)
    {
        var set = world.bodies.data[body].setIndex;
        return set >= (int)B2SolverSetType.b2_firstSleepingSet && _colorWoken[set] ? (int)B2SolverSetType.b2_awakeSet : set;
    }

    private void PlanColorWake(B2World world, int setIndex)
    {
        _colorWoken[setIndex] = true;
        var set = world.solverSets.data[setIndex];
        for (var i = 0; i < set.contactSims.count; i++) AddColorChange(world, 0, set.contactSims.data[i].contactId, -1);
        for (var i = 0; i < set.jointSims.count; i++) AddColorChange(world, 1, set.jointSims.data[i].jointId, -1);
    }

    private void BeginConstraintColors(B2World world)
    {
        EnsureOwner();
        if (_colorsReady || !ReferenceEquals(_colorWorld, world)) throw new InvalidOperationException("GPU constraint coloring is not available.");
        var start = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        _colorCount = _colorCursor = 0;
        _colorChanges.Reserve(Math.Max(1, checked(world.contacts.count + world.joints.count)));
        if (_colorWoken.Length < world.solverSets.capacity) Array.Resize(ref _colorWoken, world.solverSets.capacity);
        Array.Clear(_colorWoken);
        ref var bits = ref world.taskContexts.data[0].contactStateBitSet;
        for (var block = 0; block < bits.blockCount; block++)
        {
            var value = bits.bits[block];
            while (value != 0)
            {
                var id = 64 * block + System.Numerics.BitOperations.TrailingZeroCount(value); value &= value - 1;
                var c = world.contacts.data[id]; var flags = B2Contacts.b2GetContactSim(world, c).simFlags;
                if ((flags & (uint)B2ContactSimFlags.b2_simDisjoint) != 0)
                { if (c.colorIndex >= 0) AddColorChange(world, 0, id, c.colorIndex); }
                else if ((flags & (uint)B2ContactSimFlags.b2_simStartedTouching) != 0)
                {
                    var a = c.edges[0].bodyId; var b = c.edges[1].bodyId;
                    if (ColorSet(world, a) == (int)B2SolverSetType.b2_awakeSet && ColorSet(world, b) >= (int)B2SolverSetType.b2_firstSleepingSet)
                        PlanColorWake(world, ColorSet(world, b));
                    if (ColorSet(world, b) == (int)B2SolverSetType.b2_awakeSet && ColorSet(world, a) >= (int)B2SolverSetType.b2_firstSleepingSet)
                        PlanColorWake(world, ColorSet(world, a));
                    AddColorChange(world, 0, id, -1);
                }
                else if ((flags & (uint)B2ContactSimFlags.b2_simStoppedTouching) != 0) AddColorChange(world, 0, id, c.colorIndex);
            }
        }
        if (_colorCount == 0) return;
        _colorWords = checked((world.bodies.count + 31) / 32);
        var maskCount = checked(_colorWords * B2_OVERFLOW_INDEX);
        _colorMasks.Reserve(Math.Max(1, checked(maskCount + world.bodies.count))); _colorResults.Reserve(checked(world.contacts.count + world.joints.count + 1));
        Array.Clear(_colorMasks.Data, 0, maskCount);
        for (var color = 0; color < B2_OVERFLOW_INDEX; color++)
        {
            ref var set = ref world.constraintGraph.colors[color].bodySet;
            var words = MemoryMarshal.Cast<ulong, uint>(set.bits.AsSpan(0, set.blockCount));
            words[..Math.Min(words.Length, _colorWords)].CopyTo(_colorMasks.Data.AsSpan(color * _colorWords, _colorWords));
        }
        var packed = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        DispatchConstraintColors(world.bodies.count, maskCount);
        var returned = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        ValidateConstraintColors();
        _colorsReady = true;
        if (PhysicsSpace.ProfilingEnabled)
        {
            ConstraintColorProfileMS[0] += System.Diagnostics.Stopwatch.GetElapsedTime(start, packed).TotalMilliseconds;
            ConstraintColorProfileMS[1] += System.Diagnostics.Stopwatch.GetElapsedTime(packed, returned).TotalMilliseconds;
            ConstraintColorProfileMS[2] += System.Diagnostics.Stopwatch.GetElapsedTime(returned).TotalMilliseconds;
        }
    }

    private void DispatchConstraintColors(int bodies, int maskCount)
    {
        var command = SDL.AcquireGPUCommandBuffer(Device); if (command == 0) throw Failure("acquire constraint coloring commands");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw Failure("begin constraint coloring upload");
            _colorChanges.Upload(copy, _colorCount); _colorMasks.Upload(copy, maskCount); SDL.EndGPUCopyPass(copy);
            Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[2];
            bindings[0] = new() { Buffer = _colorMasks.Handle }; bindings[1] = new() { Buffer = _colorResults.Handle };
            var settings = stackalloc int[8] { _colorCount, bodies, _colorWords, 0, B2_OVERFLOW_INDEX, B2_DYNAMIC_COLOR_COUNT, 0, 0 };
            for (var stage = 0; stage < 2; stage++)
            {
                var pass = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 2);
                if (pass == 0) throw Failure("begin constraint coloring pass");
                SDL.BindGPUComputePipeline(pass, _constraintColorPipeline.DangerousGetHandle());
                var input = _colorChanges.Handle; SDL.BindGPUComputeStorageBuffers(pass, 0, (nint)(&input), 1);
                settings[3] = stage; SDL.PushGPUComputeUniformData(command, 0, (nint)settings, 32);
                SDL.DispatchGPUCompute(pass, stage == 0 ? checked((uint)(bodies + 63) / 64) : 1, 1, 1); SDL.EndGPUComputePass(pass); DispatchCount++;
            }
            copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw Failure("begin constraint color readback");
            _colorResults.Download(copy, _colorCount + 1); SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0; fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit constraint coloring");
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for constraint coloring");
            _colorResults.Read(_colorCount + 1);
            if (_colorResults.Data[_colorCount] != 0) throw new InvalidOperationException("GPU constraint coloring rejected the batch.");
            ConstraintColorSubmissionCount++;
            ConstraintColorTransferBytes += (long)_colorCount * sizeof(ColorChange) + (long)maskCount * sizeof(uint) + (long)(_colorCount + 1) * sizeof(int);
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); if (fence != 0) SDL.ReleaseGPUFence(Device, fence); }
    }

    private void ValidateConstraintColors()
    {
        for (var i = 0; i < _colorCount; i++)
        {
            var op = _colorChanges.Data[i]; var color = _colorResults.Data[i]; var add = op.Color < 0;
            if ((uint)color > B2_OVERFLOW_INDEX || (!add && color != op.Color) ||
                (add && color != B2_OVERFLOW_INDEX && (op.Dynamic == 0 || (op.Dynamic == 3 ? color >= B2_DYNAMIC_COLOR_COUNT : color == 0))))
                throw new InvalidOperationException("GPU constraint color is invalid.");
            if (color == B2_OVERFLOW_INDEX) continue;
            for (var side = 0; side < 2; side++)
            {
                if ((op.Dynamic & (1 << side)) == 0) continue;
                var body = side == 0 ? op.A : op.B; var at = color * _colorWords + body / 32; var bit = 1u << (body % 32);
                if (((_colorMasks.Data[at] & bit) != 0) == add) throw new InvalidOperationException("GPU constraint colors conflict.");
                if (add) _colorMasks.Data[at] |= bit; else _colorMasks.Data[at] &= ~bit;
            }
        }
    }

    private int SelectConstraintColor(B2World world, int kind, int id, int a, int b, int color)
    {
        EnsureOwner();
        if (!_colorsReady) return -1; // Immediate authoring keeps the CPU path outside this collision batch.
        if (!ReferenceEquals(world, _colorWorld) || _colorCursor >= _colorCount) throw new InvalidOperationException("GPU constraint color publication is out of order.");
        var op = _colorChanges.Data[_colorCursor];
        var generation = kind == 0 ? world.contacts.data[id].generation : world.joints.data[id].generation;
        if (op.Kind != kind || op.ID != id || op.A != a || op.B != b || op.Color != color || op.Generation != generation)
            throw new InvalidOperationException("GPU constraint color publication differs.");
        return _colorResults.Data[_colorCursor++];
    }

    private void FinishConstraintColors(B2World world)
    {
        EnsureOwner(); if (!_colorsReady) return;
        if (!ReferenceEquals(world, _colorWorld) || _colorCursor != _colorCount) throw new InvalidOperationException("GPU constraint colors were not fully published.");
        for (var color = 0; color < B2_OVERFLOW_INDEX; color++)
        {
            ref var set = ref world.constraintGraph.colors[color].bodySet;
            var words = MemoryMarshal.Cast<ulong, uint>(set.bits.AsSpan(0, set.blockCount));
            for (var i = 0; i < _colorWords; i++)
                if ((i < words.Length ? words[i] : 0) != _colorMasks.Data[color * _colorWords + i])
                    throw new InvalidOperationException("GPU constraint color occupancy differs.");
        }
        ConstraintColorChangeCount += _colorCount; _colorsReady = false;
    }
}
