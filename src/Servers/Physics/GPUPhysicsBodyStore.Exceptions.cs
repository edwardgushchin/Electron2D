using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    // Only authored identities live here; device transforms and solved contacts are not mirrored.
    private struct ExceptionSlot
    {
        internal PairData Pair;
        internal int PreviousA, NextA, PreviousB, NextB, NextFree;
        internal bool Alive, Dirty;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ExceptionEdit { internal uint Index, Padding1, Padding2, Padding3; internal PairData Pair; }
    private readonly Dictionary<(int, int), int> _exceptionIndices = [];
    private readonly List<int> _dirtyExceptions = [];
    private ExceptionSlot[] _exceptionSlots = [];
    private ExceptionEdit[] _exceptionEdits = [];
    private int _exceptionHighWater, _exceptionFree = -1;
    private RenderHandle? _exceptionsGPU, _exceptionEditsGPU, _filterPairsGPU;
    private int _exceptionCapacity, _exceptionEditCapacity, _filterPairCapacity;
    internal int CollisionExceptionCount => _exceptionIndices.Count;
    internal long ExceptionUploadBytes { get; private set; }
    internal long FilterSubmissionCount { get; private set; }
    internal double FilterMS { get; private set; }
    internal double FilterWaitMS { get; private set; }

    /// <summary>Changes one directed authored exception; either direction suppresses solid response. Sensors retain their own masks.</summary>
    internal void SetCollisionException(BodyHandle body, BodyHandle target, bool enabled)
    {
        Validate(body); Validate(target);
        var key = (body.Index, target.Index);
        if (_exceptionIndices.TryGetValue(key, out var index))
        {
            if (!enabled) RemoveException(index);
            return;
        }
        if (!enabled) return;
        index = _exceptionFree;
        if (index < 0)
        {
            if (_exceptionHighWater == _exceptionSlots.Length) Array.Resize(ref _exceptionSlots, Math.Max(64, checked(_exceptionSlots.Length * 2)));
            index = _exceptionHighWater++;
        }
        else _exceptionFree = _exceptionSlots[index].NextFree;
        ref var slot = ref _exceptionSlots[index];
        slot.Pair = new() { A = (uint)body.Index, B = (uint)target.Index, GenerationA = body.Generation, GenerationB = target.Generation };
        slot.Alive = true;
        _exceptionIndices.Add(key, index);
        LinkException(index, body.Index);
        if (body != target) LinkException(index, target.Index);
        MarkException(index);
    }

    internal bool HasCollisionException(BodyHandle body, BodyHandle target)
    {
        Validate(body); Validate(target);
        return _exceptionIndices.ContainsKey((body.Index, target.Index));
    }

    private ref int ExceptionNext(int index, int body) => ref (_exceptionSlots[index].Pair.A == body ? ref _exceptionSlots[index].NextA : ref _exceptionSlots[index].NextB);
    private ref int ExceptionPrevious(int index, int body) => ref (_exceptionSlots[index].Pair.A == body ? ref _exceptionSlots[index].PreviousA : ref _exceptionSlots[index].PreviousB);
    private void LinkException(int index, int body)
    {
        ref var first = ref _slots[body].FirstException;
        ExceptionPrevious(index, body) = -1; ExceptionNext(index, body) = first;
        if (first >= 0) ExceptionPrevious(first, body) = index;
        first = index;
    }
    private void UnlinkException(int index, int body)
    {
        var previous = ExceptionPrevious(index, body); var next = ExceptionNext(index, body);
        if (previous < 0) _slots[body].FirstException = next; else ExceptionNext(previous, body) = next;
        if (next >= 0) ExceptionPrevious(next, body) = previous;
    }
    private void RemoveException(int index)
    {
        ref var slot = ref _exceptionSlots[index];
        var a = (int)slot.Pair.A; var b = (int)slot.Pair.B;
        UnlinkException(index, a);
        if (a != b) UnlinkException(index, b);
        _exceptionIndices.Remove((a, b));
        slot.Alive = false; slot.NextFree = _exceptionFree; _exceptionFree = index;
        MarkException(index);
    }
    private void MarkException(int index)
    {
        ref var slot = ref _exceptionSlots[index];
        Wake((int)slot.Pair.A, true); Wake((int)slot.Pair.B, true);
        if (!slot.Dirty) { slot.Dirty = true; _dirtyExceptions.Add(index); }
        _pairBodyVersion = -1;
    }
    private void RemoveBodyExceptions(BodyHandle body)
    {
        while (_slots[body.Index].FirstException is var index && index >= 0) RemoveException(index);
    }
    private void DisposeExceptions() { _exceptionsGPU?.Dispose(); _exceptionEditsGPU?.Dispose(); _filterPairsGPU?.Dispose(); }
}
