using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct PairKey
    {
        internal uint Low, High;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PairUpdate
    {
        internal uint ID, Low, High, Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PairTableStep
    {
        internal uint Count, KeyCount, Mask, Operation;
        internal uint ResetStatus, Padding1, Padding2, Padding3;
    }

    private readonly Storage<PairKey> _pairKeyStorage;
    private readonly Storage<PairUpdate> _pairUpdateStorage;
    private readonly Storage<uint> _pairStatusStorage;
    private readonly Action<int> _pairChanged;
    private readonly List<int> _pairChanges = [];
    private bool[] _pairDirty = [];
    private B2World? _pairTrackingWorld;
    private B2BroadPhase? _residentPairSource;
    private bool _resetPairTable, _rebuildPairTable;
    private int _pairUpdateCount, _pairChangesSinceRebuild;
    internal long PairTableSnapshotCount { get; private set; }
    internal long PairTableRebuildCount { get; private set; }
    internal long PairTableUpdatedSlots { get; private set; }
    internal long PairTableUploadBytes { get; private set; }

    private void ReservePairJournal(int capacity)
    {
        if (_pairDirty.Length >= capacity) return;
        capacity = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(64, capacity)));
        Array.Resize(ref _pairDirty, capacity);
        _pairChanges.EnsureCapacity(capacity);
    }

    private void MarkPairChanged(int id)
    {
        ReservePairJournal(checked(id + 1));
        if (_pairDirty[id]) return;
        _pairDirty[id] = true;
        _pairChanges.Add(id);
    }

    private void ClearPairJournal()
    {
        foreach (var id in _pairChanges) _pairDirty[id] = false;
        _pairChanges.Clear();
    }

    private void DetachPairTracking()
    {
        if (_pairTrackingWorld is not null && _pairTrackingWorld.contactPairChanged == _pairChanged)
            _pairTrackingWorld.contactPairChanged = null!;
        _pairTrackingWorld = null;
        _residentPairSource = null;
        ClearPairJournal();
    }

    private static PairKey ContactKey(B2World world, int id)
    {
        var contact = world.contacts.data[id];
        return contact.contactId == id ? new()
        {
            Low = (uint)Math.Max(contact.shapeIdA, contact.shapeIdB),
            High = (uint)Math.Min(contact.shapeIdA, contact.shapeIdB)
        } : default;
    }

    private void PreparePairTable(B2World world)
    {
        _resetPairTable = !ReferenceEquals(_residentPairSource, world.broadPhase) ||
            !ReferenceEquals(_pairTrackingWorld, world) || world.contactPairChanged != _pairChanged ||
            world.contacts.capacity > _pairKeyStorage.Data.Length;
        if (!ReferenceEquals(_pairTrackingWorld, world) || world.contactPairChanged != _pairChanged)
        {
            DetachPairTracking();
            _pairTrackingWorld = world;
            world.contactPairChanged = _pairChanged;
        }
        _residentPairSource = null;
        _pairKeyStorage.Reserve(Math.Max(1, world.contacts.capacity));
        _existingPairStorage.Reserve(checked(2 * _pairKeyStorage.Data.Length));
        _pairUpdateStorage.Reserve(_pairKeyStorage.Data.Length);
        _pairStatusStorage.Reserve(1);
        ReservePairJournal(_pairKeyStorage.Data.Length);
        _pairUpdateCount = _resetPairTable ? 0 : _pairChanges.Count;
        if (_resetPairTable)
        {
            _pairKeyStorage.Data.AsSpan().Clear();
            for (var i = 0; i < world.contacts.count; i++) _pairKeyStorage.Data[i] = ContactKey(world, i);
        }
        else
        {
            for (var i = 0; i < _pairUpdateCount; i++)
            {
                var id = _pairChanges[i];
                var key = ContactKey(world, id);
                _pairUpdateStorage.Data[i] = new() { ID = (uint)id, Low = key.Low, High = key.High };
            }
        }
        // At most half the slots are live. Rebuild before accumulated changes
        // could fill another quarter with tombstones, keeping empty probe stops.
        _rebuildPairTable = _resetPairTable || _pairChangesSinceRebuild + _pairUpdateCount >= _existingPairStorage.Data.Length / 4;
    }

    private void UploadPairChanges(nint copy)
    {
        long bytes;
        if (_resetPairTable)
        {
            _pairKeyStorage.Upload(copy, _pairKeyStorage.Data.Length);
            bytes = (long)_pairKeyStorage.Data.Length * sizeof(PairKey);
        }
        else
        {
            _pairUpdateStorage.Upload(copy, _pairUpdateCount);
            bytes = (long)_pairUpdateCount * sizeof(PairUpdate);
        }
        PairTableUploadBytes += bytes;
        BroadPhaseUploadBytes += bytes;
    }

    private void UpdatePairTable(nint command)
    {
        if (!_resetPairTable && _pairUpdateCount != 0) PairTablePass(command, 1, _pairUpdateCount);
        if (_rebuildPairTable)
        {
            PairTablePass(command, 0, _existingPairStorage.Data.Length);
            PairTablePass(command, 3, _pairKeyStorage.Data.Length);
        }
        else if (_pairUpdateCount != 0) PairTablePass(command, 2, _pairUpdateCount);
    }

    private void PairTablePass(nint command, uint operation, int count)
    {
        Span<SDL.GPUStorageBufferReadWriteBinding> bindings = stackalloc SDL.GPUStorageBufferReadWriteBinding[3];
        bindings[0] = new() { Buffer = _pairKeyStorage.Handle };
        bindings[1] = new() { Buffer = _existingPairStorage.Handle };
        bindings[2] = new() { Buffer = _pairStatusStorage.Handle };
        var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, bindings, 3);
        if (compute == 0) throw Failure("begin resident pair-table maintenance");
        SDL.BindGPUComputePipeline(compute, _pairTablePipeline.DangerousGetHandle());
        var updates = _pairUpdateStorage.Handle;
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)(&updates), 1);
        var settings = new PairTableStep
        {
            Count = (uint)count,
            KeyCount = (uint)_pairKeyStorage.Data.Length,
            Mask = (uint)(_existingPairStorage.Data.Length - 1),
            Operation = operation,
            ResetStatus = _resetPairTable ? 1u : 0u
        };
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(PairTableStep));
        SDL.DispatchGPUCompute(compute, checked((uint)(count + 63) / 64), 1, 1);
        SDL.EndGPUComputePass(compute);
        DispatchCount++;
    }

    private void CommitPairTable(B2World world)
    {
        if (_resetPairTable) PairTableSnapshotCount++;
        if (_rebuildPairTable) { PairTableRebuildCount++; _pairChangesSinceRebuild = 0; }
        else _pairChangesSinceRebuild += _pairUpdateCount;
        PairTableUpdatedSlots += _pairUpdateCount;
        _residentPairSource = world.broadPhase;
        ClearPairJournal();
    }
}
