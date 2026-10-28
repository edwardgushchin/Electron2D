using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FrozenCounts
    {
        public ulong Current, Peak, Allocated, Retired, Segments, Registrations, Updates, Removals,
            Failures, ManagedCurrent, ManagedPeak;
    }

    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_frozen_read")]
    private static extern void FrozenSnapshot(out FrozenCounts counts);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_frozen_fail_next")]
    private static extern void FrozenFailNext();
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_frozen_create")]
    private static extern nint FrozenCreate(nint type, uint capacity, uint count);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_frozen_resize")]
    private static extern void FrozenResize(nint fixture, uint count);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_frozen_commit")]
    private static extern void FrozenCommit(nint fixture, uint pages);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_frozen_address")]
    private static extern nint FrozenAddress(nint fixture, uint index);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_frozen_destroy")]
    private static extern void FrozenDestroy(nint fixture);

    private static readonly List<FrozenRow> frozenRows = [];
    private static int frozenMidpoint, frozenCompleted;

    private static FrozenCounts ReadFrozen()
    {
        FrozenSnapshot(out var c);
        Require(c.Allocated - c.Retired == c.Current && c.Peak >= c.Current, "Frozen conservation");
        Require(c.ManagedCurrent >= c.Current && c.ManagedPeak >= c.ManagedCurrent, "Managed composition");
        return c;
    }

    private static FrozenCounts CaptureFrozen(string phase)
    {
        var dynamic = Read(0);
        var c = ReadFrozen();
        Require(c.ManagedCurrent == c.Current + dynamic.Current, "Coherent managed current");
        frozenRows.Add(new(phase, c.Current, c.Peak, c.Allocated, c.Retired, c.Segments, c.Registrations,
            c.Updates, c.Removals, c.Failures, c.ManagedCurrent, c.ManagedPeak, dynamic.Current, dynamic.Peak));
        return c;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateFrozen(nint address, long expected, bool collect)
    {
        var value = Unsafe.As<nint, Ordinary>(ref address);
        Require(value.Value == expected, "Frozen payload before collection");
        if (collect) Collect(2);
        Require(value.Value == expected, "Frozen payload after collection");
        GC.KeepAlive(value);
    }

    private static void CheckFrozenBytes(FrozenCounts c, FrozenCounts baseline, ulong bytes)
    {
        Require(c.Current == baseline.Current + bytes && c.Segments == baseline.Segments + 1,
            "Frozen registered payload");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FrozenPeakOverlap(nint type)
    {
        var data = new byte[2400000];
        var disjoint = CaptureFrozen("disjoint-peak");
        Require(disjoint.ManagedPeak < disjoint.Peak + frozenRows[^1].DynamicPeak, "Independent domain peaks were summed");
        var fixture = FrozenCreate(type, 100000, 100000);
        Require(fixture != 0, "Overlap fixture allocation");
        var overlap = ReadFrozen();
        Require(overlap.ManagedPeak == overlap.ManagedCurrent, "Overlap peak publication");
        FrozenDestroy(fixture);
        var released = ReadFrozen();
        Require(released.ManagedCurrent + 2400000 == overlap.ManagedCurrent && released.ManagedPeak == overlap.ManagedPeak,
            "Overlap retirement or peak retention");
        CaptureFrozen("overlap-retired");
        GC.KeepAlive(data);
    }

    private static void FrozenWorker(nint type, Barrier barrier)
    {
        barrier.SignalAndWait();
        for (int i = 0; i < 64; ++i)
        {
            var fixture = FrozenCreate(type, 8, 2);
            Require(fixture != 0, "Concurrent frozen creation");
            FrozenResize(fixture, 5);
            FrozenResize(fixture, 1);
            if (i == 31)
            {
                Volatile.Write(ref frozenMidpoint, 1);
                barrier.SignalAndWait();
            }
            FrozenDestroy(fixture);
        }
        Volatile.Write(ref frozenCompleted, 1);
    }

    private static FrozenReport RunFrozenChecks(string[] args)
    {
        nint type = typeof(Ordinary).TypeHandle.Value;
        var baseline = CaptureFrozen("startup");
        Require(baseline.Current > 0 && baseline.Registrations > 0 && baseline.Segments > 0,
            "Startup image registration was not observed");
        if (args.Contains("--invalid-frozen")) FrozenResize(0, 1);
        var collections = Read(0).Collections;
        FrozenFailNext();
        Require(FrozenCreate(type, 8, 2) == 0, "Frozen registration failure did not propagate");
        var failed = CaptureFrozen("registration-failure");
        Require(failed.Current == baseline.Current && failed.Segments == baseline.Segments &&
            failed.Allocated == baseline.Allocated && failed.Failures == baseline.Failures + 1,
            "Failed registration was published");
        var fixture = FrozenCreate(type, 8, 0);
        Require(fixture != 0, "Empty frozen registration");
        CheckFrozenBytes(CaptureFrozen("empty"), baseline, 0);
        FrozenResize(fixture, 2);
        CheckFrozenBytes(CaptureFrozen("grow-two"), baseline, 48);
        FrozenResize(fixture, 5);
        CheckFrozenBytes(CaptureFrozen("grow-five"), baseline, 120);
        ValidateFrozen(FrozenAddress(fixture, 4), 0x1234567c, false);
        FrozenResize(fixture, 1);
        var shrunk = CaptureFrozen("shrink-one");
        CheckFrozenBytes(shrunk, baseline, 24);
        Require(shrunk.Allocated == baseline.Allocated + 120 && shrunk.Retired == baseline.Retired + 96,
            "Frozen growth/shrink conservation");
        FrozenCommit(fixture, 1);
        FrozenCommit(fixture, 2);
        FrozenResize(fixture, 1);
        var same = CaptureFrozen("commit-and-noop");
        Require(same.Current == shrunk.Current && same.Allocated == shrunk.Allocated && same.Retired == shrunk.Retired,
            "Committed/reserved padding changed payload accounting");
        Require(Read(0).Collections == collections, "Frozen lifecycle reads caused collection");
        ValidateFrozen(FrozenAddress(fixture, 0), 0x12345678, true);
        FrozenDestroy(fixture);
        var removed = CaptureFrozen("unregistered");
        Require(removed.Current == baseline.Current && removed.Segments == baseline.Segments &&
            removed.Allocated == baseline.Allocated + 120 && removed.Retired == baseline.Retired + 120,
            "Unregistered payload did not retire");
        Collect(2);
        Require(Read(0).FrozenBytes == removed.Current, "Frozen lifecycle and independent heap census differ");
        using var barrier = new Barrier(2);
        var beforeWorker = ReadFrozen();
        var worker = new Thread(() => FrozenWorker(type, barrier));
        worker.Start(); barrier.SignalAndWait();
        bool midpointCollection = false;
        int interleaved = 0;
        while (Volatile.Read(ref frozenCompleted) == 0)
        {
            bool atMidpoint = !midpointCollection && Volatile.Read(ref frozenMidpoint) != 0;
            Collect(0); ReadFrozen(); ++interleaved;
            if (atMidpoint) { barrier.SignalAndWait(); midpointCollection = true; }
            Thread.Sleep(1);
        }
        worker.Join();
        var afterWorker = CaptureFrozen("concurrent-lifetime");
        Require(midpointCollection && afterWorker.Current == beforeWorker.Current && afterWorker.Segments == beforeWorker.Segments &&
            afterWorker.Allocated == beforeWorker.Allocated + 7680 && afterWorker.Retired == beforeWorker.Retired + 7680,
            "Concurrent frozen lifecycle conservation");
        var beforeLarge = ReadFrozen();
        var large = FrozenCreate(type, 100000, 100000);
        Require(large != 0, "Large frozen fixture");
        FrozenDestroy(large);
        var hidden = CaptureFrozen("unobserved-frozen-peak");
        Require(hidden.Current == beforeLarge.Current && hidden.Peak == beforeLarge.Current + 2400000,
            "Unobserved frozen peak was lost");
        FrozenPeakOverlap(type);
        Collect(2);
        var dynamic = Read(11);
        Require(dynamic.Current == 0 && dynamic.Allocated == 2400024 && dynamic.Retired == 2400024,
            "Disjoint dynamic peak retirement");
        CaptureFrozen("peak-retained");
        ReadFrozen();
        long beforeReads = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20000; ++i) ReadFrozen();
        long readBytes = GC.GetAllocatedBytesForCurrentThread() - beforeReads;
        Require(readBytes == 0, "Frozen prepared reads allocate");
        return new(true, true, midpointCollection, interleaved, readBytes, frozenRows.ToArray());
    }
}
internal sealed record FrozenRow(string Phase, ulong Current, ulong Peak, ulong Allocated, ulong Retired,
    ulong Segments, ulong Registrations, ulong Updates, ulong Removals, ulong Failures,
    ulong ManagedCurrent, ulong ManagedPeak, ulong DynamicCurrent, ulong DynamicPeak);
internal sealed record FrozenReport(bool StartupObserved, bool NoGCTransitions, bool MidpointCollection,
    int InterleavedCollections, long ReadBytes, FrozenRow[] Rows);
