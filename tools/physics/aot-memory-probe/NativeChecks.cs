using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessCounts
    {
        public ulong ManagedCurrent, ManagedPeak, NativeCurrent, NativePeak, Current, Peak,
            Allocated, Retired, Allocations, Resizes, Frees, Failures;
    }

    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_native_alloc")]
    private static extern unsafe byte* NativeAllocate(nuint size, nuint alignment, uint zero);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_native_resize")]
    private static extern unsafe byte* NativeResize(byte* value, nuint size);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_native_free")]
    private static extern unsafe void NativeFree(byte* value);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_process_read")]
    private static extern void ProcessSnapshot(out ProcessCounts counts);

    private static readonly List<ProcessRow> processRows = [];
    private static int nativeMidpoint, nativeCompleted;

    private static ProcessCounts ReadProcess()
    {
        ProcessSnapshot(out var c);
        Require(c.Current == c.ManagedCurrent + c.NativeCurrent && c.Peak >= c.Current,
            "Coherent managed/native process current");
        Require(c.Allocated - c.Retired == c.NativeCurrent && c.NativePeak >= c.NativeCurrent,
            "Native lifetime conservation");
        return c;
    }

    private static ProcessCounts CaptureProcess(string phase)
    {
        var c = ReadProcess();
        processRows.Add(new(phase, c.ManagedCurrent, c.ManagedPeak, c.NativeCurrent, c.NativePeak,
            c.Current, c.Peak, c.Allocated, c.Retired, c.Allocations, c.Resizes, c.Frees, c.Failures));
        return c;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void NativePeakOverlap()
    {
        var data = new byte[7000000];
        var disjoint = CaptureProcess("disjoint-process-peak");
        Require(disjoint.Peak < disjoint.ManagedPeak + disjoint.NativePeak, "Independent process peaks were summed");
        var block = NativeAllocate(7000000, 0, 0);
        Require(block != null, "Overlapping native allocation");
        var overlap = ReadProcess();
        Require(overlap.Peak == overlap.Current, "Overlapping process peak publication");
        NativeFree(block);
        var released = ReadProcess();
        Require(released.Current + 7000000 == overlap.Current && released.Peak == overlap.Peak,
            "Native retirement or process peak retention");
        CaptureProcess("overlap-process-retired");
        GC.KeepAlive(data);
    }

    private static unsafe void NativeWorker(Barrier barrier)
    {
        barrier.SignalAndWait();
        for (int i = 0; i < 64; ++i)
        {
            var block = NativeAllocate(31, 0, 0);
            Require(block != null, "Native worker allocation");
            block[0] = 91;
            block = NativeResize(block, 79);
            Require(block != null && block[0] == 91, "Native worker growth");
            block = NativeResize(block, 7);
            Require(block != null && block[0] == 91, "Native worker shrink");
            if (i == 31)
            {
                Volatile.Write(ref nativeMidpoint, 1);
                barrier.SignalAndWait();
            }
            NativeFree(block);
        }
        Volatile.Write(ref nativeCompleted, 1);
    }

    private static unsafe ProcessReport RunNativeChecks(string[] args)
    {
        var baseline = CaptureProcess("startup");
        Require(baseline.NativeCurrent == 0 && baseline.Allocations == 0, "Unexpected native fixture ownership");
        if (args.Contains("--invalid-native")) NativeAllocate(1, 3, 0);
        var block = NativeAllocate(73, 0, 1);
        Require(block != null && block[0] == 0 && block[72] == 0, "Native zeroed payload");
        block[0] = 23; block[72] = 91;
        Require(CaptureProcess("allocated").NativeCurrent == 73, "Native requested payload");
        block = NativeResize(block, 4097);
        Require(block != null && block[0] == 23 && block[72] == 91 &&
            CaptureProcess("grown").NativeCurrent == 4097, "Native grow preservation");
        var stable = ReadProcess();
        Require(NativeResize(block, nuint.MaxValue) == null && block[0] == 23 && block[72] == 91,
            "Rejected native reallocation ownership");
        var failed = ReadProcess();
        Require(failed.Current == stable.Current && failed.Peak == stable.Peak &&
            failed.NativeCurrent == stable.NativeCurrent && failed.Failures == stable.Failures + 1,
            "Rejected native reallocation publication");
        block = NativeResize(block, 7);
        Require(block != null && block[0] == 23 && CaptureProcess("shrunk").NativeCurrent == 7,
            "Native shrink preservation");
        block = NativeResize(block, 0);
        Require(block != null && CaptureProcess("zero-size").NativeCurrent == 0, "Zero-size native ownership");
        NativeFree(block); NativeFree(null);
        for (nuint alignment = 1; alignment <= 4096; alignment *= 2)
        {
            block = NativeAllocate(93, alignment, 1);
            Require(block != null && (nuint)block % alignment == 0 && block[0] == 0 && block[92] == 0,
                "Native aligned zeroed payload");
            block[0] = 31; block[92] = 47;
            Require(ReadProcess().NativeCurrent == 93, "Aligned payload excludes private padding");
            NativeFree(block);
        }
        var beforeFailure = ReadProcess();
        Require(NativeAllocate(nuint.MaxValue, 0, 0) == null && NativeAllocate((nuint)1 << 62, 0, 0) == null,
            "Native range and allocator failures");
        var failures = CaptureProcess("failed-allocations");
        Require(failures.Current == beforeFailure.Current && failures.Peak == beforeFailure.Peak && failures.NativeCurrent == 0 &&
            failures.NativePeak == beforeFailure.NativePeak && failures.Failures == beforeFailure.Failures + 2,
            "Failed native allocation changed ownership");
        using var barrier = new Barrier(2);
        var beforeWorker = ReadProcess();
        var worker = new Thread(() => NativeWorker(barrier));
        worker.Start(); barrier.SignalAndWait();
        bool midpointCollection = false;
        int collections = 0;
        while (Volatile.Read(ref nativeCompleted) == 0)
        {
            bool atMidpoint = !midpointCollection && Volatile.Read(ref nativeMidpoint) != 0;
            Collect(0); ReadProcess(); ++collections;
            if (atMidpoint) { barrier.SignalAndWait(); midpointCollection = true; }
            Thread.Sleep(1);
        }
        worker.Join();
        var afterWorker = CaptureProcess("worker-retired");
        Require(midpointCollection && afterWorker.NativeCurrent == 0 &&
            afterWorker.Allocated == beforeWorker.Allocated + 5056 && afterWorker.Retired == beforeWorker.Retired + 5056,
            "Native worker/GC retirement");
        var beforeHidden = ReadProcess();
        block = NativeAllocate(7000000, 0, 0);
        Require(block != null, "Unobserved native allocation");
        NativeFree(block);
        var hidden = CaptureProcess("unobserved-native-peak");
        Require(hidden.NativeCurrent == 0 && hidden.NativePeak == 7000000 && hidden.Peak == beforeHidden.Current + 7000000,
            "Unobserved native process peak was lost");
        NativePeakOverlap();
        Collect(2);
        var dynamic = Read(12);
        Require(dynamic.Current == 0 && dynamic.Allocated == 7000024 && dynamic.Retired == 7000024,
            "Overlapping dynamic allocation retirement");
        CaptureProcess("managed-retired");
        nuint wide = ((nuint)1 << 32) + 17;
        block = NativeAllocate(wide, 0, 0);
        Require(block != null, "Sparse native allocation above 4 GiB");
        block[0] = 61; block[wide - 1] = 83;
        Require(CaptureProcess("wide-allocation").NativeCurrent == (ulong)wide, "Native width truncation");
        block = NativeResize(block, wide + 16);
        Require(block != null && block[0] == 61 && block[wide - 1] == 83 &&
            CaptureProcess("wide-resize").NativeCurrent == (ulong)wide + 16, "Native wide resize preservation");
        NativeFree(block);
        CaptureProcess("wide-retired");
        void Cycle()
        {
            var value = NativeResize(null, 19);
            Require(value != null, "Prepared native allocation");
            value = NativeResize(value, 37); Require(value != null, "Prepared native resize");
            NativeFree(value); ReadProcess();
        }
        for (int i = 0; i < 128; ++i) Cycle();
        long beforeReads = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20000; ++i) Cycle();
        long readBytes = GC.GetAllocatedBytesForCurrentThread() - beforeReads;
        Require(readBytes == 0 && ReadProcess().NativeCurrent == 0, "Prepared native cycles allocate managed bytes");
        return new(midpointCollection, collections, readBytes, processRows.ToArray());
    }
}
internal sealed record ProcessRow(string Phase, ulong ManagedCurrent, ulong ManagedPeak, ulong NativeCurrent,
    ulong NativePeak, ulong Current, ulong Peak, ulong Allocated, ulong Retired, ulong Allocations,
    ulong Resizes, ulong Frees, ulong Failures);
internal sealed record ProcessReport(bool MidpointCollection, int InterleavedCollections, long ReadBytes, ProcessRow[] Rows);
