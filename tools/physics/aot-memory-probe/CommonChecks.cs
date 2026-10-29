using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static partial class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct CommonCounts
    {
        public ulong Attempts, SOH, FreeList, Segment, Completed, Failed, Reverted, Bytes, Pending;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct DelayCounts { public ulong ExpectedPeak, FreedBeforeTyped, Completed; }

    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_common_read")]
    private static extern void CommonSnapshot(out CommonCounts counts);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_finalizer_fail_next")]
    private static extern void FinalizerFailNext(uint tag);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_delay_arm")]
    private static extern unsafe void DelayArm(uint tag, byte* block);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_delay_join")]
    private static extern void DelayJoin(out DelayCounts counts);

    private static int witnessConstructed, witnessFinalized;
    private sealed class FinalizerWitness
    {
        public long Value;
        public FinalizerWitness() { Value = 91; Interlocked.Increment(ref witnessConstructed); }
        ~FinalizerWitness() => Interlocked.Increment(ref witnessFinalized);
    }
    private static readonly List<CommonRow> commonRows = [];

    private static CommonCounts ReadCommon()
    {
        CommonSnapshot(out var c);
        Require(c.Attempts == c.Completed + c.Failed + c.Pending &&
            c.SOH + c.FreeList + c.Segment >= c.Completed + c.Reverted, "Common scope conservation");
        return c;
    }
    private static CommonCounts CaptureCommon(string phase)
    {
        var c = ReadCommon();
        Require(c.Pending == 0 && c.SOH + c.FreeList + c.Segment == c.Completed + c.Reverted,
            "Common reservation/typed completion conservation");
        commonRows.Add(new(phase, c.Attempts, c.SOH, c.FreeList, c.Segment, c.Completed, c.Failed, c.Reverted, c.Bytes, c.Pending));
        return c;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReuseCommonHole()
    {
        var roots = new byte[8][];
        for (int i = 0; i < roots.Length; ++i) roots[i] = new byte[1000000];
        for (int i = 2; i < 6; ++i) roots[i] = null!;
        Collect(2);
        var before = ReadCommon();
        var replacement = new byte[1000000];
        replacement[0] = 31; replacement[^1] = 47;
        Require(ReadCommon().FreeList > before.FreeList && replacement[0] == 31 && replacement[^1] == 47,
            "Common UOH free-list reservation was not exercised");
        GC.KeepAlive(roots); GC.KeepAlive(replacement);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe (ulong Expected, ulong Observed) DelayCommonCompletion()
    {
        Collect(2); GC.WaitForPendingFinalizers(); Collect(2);
        var block = NativeAllocate((nuint)5 << 30, 0, 0);
        Require(block != null, "Sparse native overlap allocation");
        ulong collections = Read(0).Collections;
        DelayArm(13, block);
        var before = ReadProcess();
        var value = GC.AllocateArray<byte>(4096, pinned: true);
        value[0] = 61; value[^1] = 83;
        DelayJoin(out var delay);
        var after = ReadProcess();
        Require(Read(0).Collections == collections, "Common overlap witness collected during allocation");
        Require(delay.Completed == 1 && delay.FreedBeforeTyped == 1 && after.NativeCurrent == 0,
            "Native free did not precede common typed completion");
        if (after.Peak != before.Current + 4120 || delay.ExpectedPeak != after.Peak)
            throw new InvalidOperationException($"Delayed common peak missed reservation: before={before.Current}, expected={before.Current + 4120}, peak={after.Peak}, raw={delay.ExpectedPeak}, tagged={Read(13).Allocated}");
        Require(value[0] == 61 && value[^1] == 83, "Delayed common payload");
        GC.KeepAlive(value);
        return (before.Current + 4120, after.Peak);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FailFinalizerRegistration()
    {
        FinalizerFailNext(15);
        try { _ = new FinalizerWitness(); }
        catch (OutOfMemoryException) { return; }
        throw new InvalidOperationException("Finalizer registration failure was not propagated");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RetryFinalizerRegistration()
    {
        var value = new FinalizerWitness();
        Require(value.Value == 91 && Read(15).Current == 24, "Finalizer registration retry ownership");
        GC.KeepAlive(value);
    }

    private static CommonReport RunCommonChecks()
    {
        var baseline = CaptureCommon("startup");
        Require(baseline.SOH > 0 && baseline.Segment > 0, "Missing common small/UOH paths");
        ReuseCommonHole(); CaptureCommon("free-list-reuse");
        var delayed = DelayCommonCompletion(); CaptureCommon("delayed-completion");
        var beforeFailure = ReadCommon();
        FailFinalizerRegistration();
        var failed = CaptureCommon("finalizer-failure");
        Require(failed.Failed == beforeFailure.Failed + 1 && failed.Reverted == beforeFailure.Reverted + 1 &&
            Read(15).Allocated == 0 && witnessConstructed == 0, "Failed finalizer reservation did not roll back");
        RetryFinalizerRegistration(); CaptureCommon("finalizer-retry");
        Collect(2); GC.WaitForPendingFinalizers(); Collect(2);
        CaptureCommon("retired");
        Require(witnessConstructed == 1 && witnessFinalized == 1 && Read(15).Current == 0 &&
            Read(15).Allocated == 24 && Read(15).Retired == 24 && Read(13).Current == 0,
            "Failed finalizer or retry retirement");
        ReadCommon();
        long beforeReads = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20000; ++i) ReadCommon();
        long readBytes = GC.GetAllocatedBytesForCurrentThread() - beforeReads;
        Require(readBytes == 0, "Prepared common snapshots allocate");
        CaptureCommon("prepared-reads");
        return new(delayed.Expected, delayed.Observed, readBytes, commonRows.ToArray());
    }

    private static void RunCommonOOM()
    {
        long limit = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        Require(limit > 0 && limit <= 134217728, "Common OOM control requires a 128 MiB heap limit");
        var before = ReadCommon(); ulong collections = Read(0).Collections;
        bool failed = false;
        try { GC.KeepAlive(GC.AllocateArray<byte>(268435456, pinned: true)); }
        catch (OutOfMemoryException) { failed = true; }
        var after = ReadCommon(); var tagged = Read(14); ulong afterCollections = Read(0).Collections;
        Require(failed && after.Failed == before.Failed + 1 && after.Reverted == before.Reverted && after.Pending == 0 &&
            tagged.Current == 0 && tagged.Allocated == 0 && tagged.Failures == 1 && afterCollections > collections,
            "Common bounded OOM reservation/GC rollback");
        var report = new CommonOOMReport(RuntimeInformation.FrameworkDescription, System.Runtime.GCSettings.IsServerGC,
            limit, collections, afterCollections, after.Failed - before.Failed, after.Reverted - before.Reverted,
            after.Bytes - before.Bytes, tagged.Current, tagged.Allocated);
        Console.WriteLine(JsonSerializer.Serialize(report, JsonContext.Default.CommonOOMReport));
    }
}
internal sealed record CommonRow(string Phase, ulong Attempts, ulong SOH, ulong FreeList, ulong Segment,
    ulong Completed, ulong Failed, ulong Reverted, ulong Bytes, ulong Pending);
internal sealed record CommonReport(ulong ExpectedDelayedPeak, ulong ObservedDelayedPeak, long ReadBytes, CommonRow[] Rows);
internal sealed record CommonOOMReport(string Runtime, bool ServerGC, long HeapLimit, ulong CollectionsBefore,
    ulong CollectionsAfter, ulong Failed, ulong Reverted, ulong RawBytes, ulong TaggedCurrent, ulong TaggedAllocated);
