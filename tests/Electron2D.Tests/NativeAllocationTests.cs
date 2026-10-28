using Electron2D;

internal static unsafe class NativeAllocationTests
{
    internal static void Run()
    {
        var before = NativeAllocation.Snapshot();
        var block = (byte*)NativeAllocation.AllocZeroed(73);
        try
        {
            Check(new ReadOnlySpan<byte>(block, 73).IndexOfAnyExcept((byte)0) < 0 && (nuint)block % (nuint)(IntPtr.Size * 2) == 0, "Zeroed payload retains native fundamental alignment.");
            block[0] = 23; block[72] = 91;
            Check(NativeAllocation.Snapshot().Current == before.Current + 73, "Only owned requested payload bytes contribute.");
            block = (byte*)NativeAllocation.Realloc(block, 4097);
            Check(block[0] == 23 && block[72] == 91 && NativeAllocation.Snapshot().Current == before.Current + 4097, "Growth preserves contents and replaces the preceding contribution.");
            var stable = NativeAllocation.Snapshot(); var retained = block;
            Reject<OutOfMemoryException>(() => NativeAllocation.Realloc(retained, nuint.MaxValue));
            if (Environment.Is64BitProcess) Reject<OutOfMemoryException>(() => NativeAllocation.Realloc(retained, unchecked((nuint)(1UL << 62))));
            Check(NativeAllocation.Snapshot() == stable && block[0] == 23 && block[72] == 91, "Rejected reallocation leaves the live block, data and both counters intact.");
            block = (byte*)NativeAllocation.Realloc(block, 7);
            Check(block[0] == 23 && NativeAllocation.Snapshot().Current == before.Current + 7, "Shrinking releases only the removed payload contribution.");
            block = (byte*)NativeAllocation.Realloc(block, 0);
            Check(block is not null && NativeAllocation.Snapshot().Current == before.Current, "Zero-size reallocation keeps a releasable allocation without payload bytes.");
        }
        finally { NativeAllocation.Free(block); }
        for (nuint alignment = 1; alignment <= 4096; alignment *= 2)
        {
            var value = (byte*)NativeAllocation.AlignedAlloc(93, alignment);
            try
            {
                Check((nuint)value % alignment == 0 && NativeAllocation.Snapshot().Current == before.Current + 93, "Aligned payload counts exclude private prefix and padding.");
                value[0] = 31; value[92] = 47;
                Reject<ArgumentException>(() => NativeAllocation.Realloc(value, 100));
                Check(value[0] == 31 && value[92] == 47, "Wrong reallocator rejects without retiring aligned ownership.");
            }
            finally { NativeAllocation.AlignedFree(value); }
        }
        Reject<ArgumentException>(() => NativeAllocation.AlignedAlloc(1, 0)); Reject<ArgumentException>(() => NativeAllocation.AlignedAlloc(1, 3));
        var previous = NativeAllocation.Snapshot(); Reject<OutOfMemoryException>(() => NativeAllocation.Alloc(nuint.MaxValue)); Check(NativeAllocation.Snapshot() == previous, "Rejected allocation never publishes a contribution.");
        if (Environment.Is64BitProcess) { Reject<OutOfMemoryException>(() => NativeAllocation.Alloc(unchecked((nuint)(1UL << 62)))); Check(NativeAllocation.Snapshot() == previous, "Native allocation failure also preserves both counters."); }
        var transientSize = checked((nuint)(previous.Peak - previous.Current + 4096));
        var transient = NativeAllocation.Alloc(transientSize); NativeAllocation.Free(transient);
        Check(NativeAllocation.Snapshot().Current == before.Current && NativeAllocation.Snapshot().Peak == previous.Current + (long)transientSize, "Allocation and free between reads still publish the actual lifetime high-water mark.");
        NativeAllocation.Free(null); NativeAllocation.AlignedFree(null); var zero = NativeAllocation.Realloc(null, 0); Check(zero is not null, "Null reallocation uses ordinary allocation semantics."); NativeAllocation.Free(zero);
        NativeFailures();
        Parallel.For(0, 4, _ =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var value = (byte*)NativeAllocation.Alloc(51); value[0] = 11; value = (byte*)NativeAllocation.Realloc(value, 97); Check(value[0] == 11, "Concurrent resize preserves independent ownership."); NativeAllocation.Free(value);
                var sample = NativeAllocation.Snapshot(); Check(sample.Current >= before.Current && sample.Peak >= sample.Current, "Concurrent snapshots are coherent.");
            }
        });
        Check(NativeAllocation.Snapshot().Current == before.Current, "All worker allocations drain without resetting the process peak.");
        void Cycle() { var value = NativeAllocation.Alloc(19); value = NativeAllocation.Realloc(value, 37); NativeAllocation.Free(value); _ = NativeAllocation.Snapshot(); }
        for (var i = 0; i < 128; i++) Cycle(); AllocationWarmupCheck.Collect(); for (var i = 0; i < 128; i++) Cycle();
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 1024; i++) Cycle();
        Check(GC.GetAllocatedBytesForCurrentThread() == own && GC.GetTotalAllocatedBytes(true) == all && NativeAllocation.Snapshot().Current == before.Current, "1024 prepared allocation/resize/free/read cycles allocate zero managed bytes.");
        Console.WriteLine("Native allocation accounting: resize/alignment/zero/error/transient peak/4 workers/native callback failures and 1024 zero-managed-allocation cycles passed.");
    }
    private static void NativeFailures()
    {
        var allocate = (delegate* unmanaged[Cdecl]<nuint, void*>)FAudioContext.AllocateAddress;
        var resize = (delegate* unmanaged[Cdecl]<void*, nuint, void*>)FAudioContext.ReallocateAddress;
        var release = (delegate* unmanaged[Cdecl]<void*, void>)FAudioContext.FreeAddress;
        var before = NativeAllocation.Snapshot(); Check(allocate(nuint.MaxValue) is null && NativeAllocation.Snapshot() == before, "Allocation failure returns null across the real unmanaged callback boundary.");
        var value = (byte*)allocate(16); Check(value is not null, "Native callback allocates tracked storage.");
        try { value[0] = 61; var stable = NativeAllocation.Snapshot(); Check(resize(value, nuint.MaxValue) is null && value[0] == 61 && NativeAllocation.Snapshot() == stable, "Native failed resize returns null and retains caller ownership/data/accounting."); }
        finally { release(value); }
        Check(NativeAllocation.Snapshot().Current == before.Current, "Native callback free drains its payload.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
