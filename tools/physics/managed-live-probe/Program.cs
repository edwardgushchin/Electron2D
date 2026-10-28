using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static unsafe class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Snapshot
    {
        public ulong Current, Peak, Allocated, Retired, Count, Moved, Collections, RawAllocated, FrozenExcluded;
    }

    private sealed class Witness
    {
        public long Value;
        public Witness(long value) => Value = value;
    }

    private sealed class Finalized
    {
        public long Value = 1;
        ~Finalized() => Interlocked.Increment(ref finalized);
    }

    private static int finalized;
    private static int completedWorkers;
    private static delegate* unmanaged<uint, void> tag;
    private static delegate* unmanaged<uint, Snapshot*, void> snapshot;
    private static readonly List<object> reports = [];

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static Snapshot Read(uint value)
    {
        Snapshot result;
        snapshot(value, &result);
        Require(result.Allocated - result.Retired == result.Current, "Broken ledger conservation");
        Require(result.Peak >= result.Current, "Peak below current");
        return result;
    }

    private static Snapshot Check(uint value, ulong current, ulong allocated, ulong count, string phase)
    {
        var result = Read(value);
        Require(result.Current == current && result.Allocated == allocated && result.Count == count,
            $"{phase}: tag={value} current={result.Current} allocated={result.Allocated} count={result.Count}");
        reports.Add(new
        {
            Phase = phase,
            Tag = value,
            result.Current,
            result.Peak,
            result.Allocated,
            result.Retired,
            result.Count,
            result.Moved,
            result.Collections,
            result.RawAllocated
        });
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Fill(object?[] roots)
    {
        tag(1);
        for (int i = 0; i < 1024; ++i) roots[i] = new Witness(i);
        tag(2);
        for (int i = 1024; i < 1089; ++i) roots[i] = new string('x', 128);
        tag(3);
        roots[1089] = GC.AllocateUninitializedArray<byte>(131072, pinned: true);
        tag(4);
        for (int i = 1090; i < 1098; ++i) roots[i] = new Finalized();
        tag(5);
        roots[1098] = new byte[262144];
        tag(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearOdd(object?[] roots)
    {
        for (int i = 1; i < 1024; i += 2) roots[i] = null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropLarge(object?[] roots) => roots[1089] = roots[1098] = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropAll(object?[] roots) => Array.Clear(roots);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void HiddenPeak()
    {
        var roots = new Witness[512];
        tag(6);
        for (int i = 0; i < roots.Length; ++i) roots[i] = new Witness(i);
        tag(0);
        GC.KeepAlive(roots);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Worker(uint value, Barrier barrier)
    {
        var roots = new Witness[4096];
        tag(value);
        for (int i = 0; i < roots.Length; ++i) roots[i] = new Witness(i);
        tag(0);
        barrier.SignalAndWait();
        barrier.SignalAndWait();
        GC.KeepAlive(roots);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StressWorker(uint value, Barrier barrier)
    {
        var roots = new Witness[1024];
        barrier.SignalAndWait();
        tag(value);
        for (int batch = 0; batch < 32; ++batch)
            for (int i = 0; i < roots.Length; ++i) roots[i] = new Witness(i);
        tag(0);
        GC.KeepAlive(roots);
        Interlocked.Increment(ref completedWorkers);
    }

    private static void Collect(int generation) => GC.Collect(generation, GCCollectionMode.Forced, true, true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Warm()
    {
        GC.KeepAlive(new Witness(0));
        GC.KeepAlive(new string('x', 128));
        GC.KeepAlive(GC.AllocateUninitializedArray<byte>(131072, pinned: true));
        GC.KeepAlive(new Finalized());
        GC.KeepAlive(new byte[262144]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Validate(object?[] roots)
    {
        for (int i = 0; i < 1024; i += 2)
            Require(((Witness)roots[i]!).Value == i, "Relocation changed retained payload");
    }

    private static void Main(string[] args)
    {
        nint library = NativeLibrary.Load(Environment.GetEnvironmentVariable("CORECLR_PROFILER_PATH")!);
        tag = (delegate* unmanaged<uint, void>)NativeLibrary.GetExport(library, "e2d_live_tag");
        snapshot = (delegate* unmanaged<uint, Snapshot*, void>)NativeLibrary.GetExport(library, "e2d_live_snapshot");
        tag(args.Contains("--invalid-tag") ? 32U : 0U);
        if (args.Contains("--invalid-snapshot")) Read(32);
        Warm();
        Collect(2);
        GC.WaitForPendingFinalizers();
        Collect(2);
        finalized = 0;
        var roots = new object?[1099];
        Fill(roots);
        Check(1, 24576, 24576, 1024, "allocated");
        var strings = Check(2, 18200, 18200, 65, "strings");
        Require(strings.RawAllocated == 18070, "Raw/aligned string size calibration");
        Check(3, 131096, 131096, 1, "pinned");
        Check(4, 192, 192, 8, "finalizable");
        Check(5, 262168, 262168, 1, "large");
        ClearOdd(roots);
        Collect(0);
        var compacted = Check(1, 12288, 24576, 512, "generation-0");
        Require(compacted.Moved > 0, "Compaction witness did not move");
        DropLarge(roots);
        Collect(0);
        Check(3, 131096, 131096, 1, "pinned-not-collected-in-gen0");
        Check(5, 262168, 262168, 1, "large-not-collected-in-gen0");
        Collect(1);
        Check(1, 12288, 24576, 512, "generation-1");
        Check(3, 131096, 131096, 1, "pinned-not-collected-in-gen1");
        Check(5, 262168, 262168, 1, "large-not-collected-in-gen1");
        Collect(2);
        Check(1, 12288, 24576, 512, "generation-2");
        Check(3, 0, 131096, 0, "pinned-retired");
        Check(5, 0, 262168, 0, "large-retired");
        Validate(roots);
        DropAll(roots);
        Collect(2);
        Check(4, 192, 192, 8, "finalizer-queue-survives");
        GC.WaitForPendingFinalizers();
        Collect(2);
        Require(finalized == 8, "Finalizer count");
        Check(1, 0, 24576, 0, "objects-retired");
        Check(2, 0, 18200, 0, "strings-retired");
        Check(4, 0, 192, 0, "finalizers-retired");
        HiddenPeak();
        Collect(2);
        var hidden = Check(6, 0, 12288, 0, "peak-between-reads");
        Require(hidden.Peak == 12288, "Missed an unobserved peak");
        using var barrier = new Barrier(3);
        var first = new Thread(() => Worker(8, barrier));
        var second = new Thread(() => Worker(9, barrier));
        first.Start(); second.Start();
        barrier.SignalAndWait();
        Collect(2);
        Check(8, 98304, 98304, 4096, "worker-1-survives");
        Check(9, 98304, 98304, 4096, "worker-2-survives");
        barrier.SignalAndWait();
        first.Join(); second.Join();
        Collect(2);
        Check(8, 0, 98304, 0, "worker-1-retired");
        Check(9, 0, 98304, 0, "worker-2-retired");
        using var stressBarrier = new Barrier(3);
        var stressFirst = new Thread(() => StressWorker(10, stressBarrier));
        var stressSecond = new Thread(() => StressWorker(11, stressBarrier));
        stressFirst.Start(); stressSecond.Start();
        stressBarrier.SignalAndWait();
        int concurrentCollections = 0;
        while (Volatile.Read(ref completedWorkers) != 2)
        {
            Collect(0);
            Read(0);
            ++concurrentCollections;
            // Leave a mutator interval between forced collections instead of starving the workers.
            Thread.Sleep(1);
        }
        stressFirst.Join(); stressSecond.Join();
        Collect(2);
        Check(10, 0, 786432, 0, "stress-1-retired");
        Check(11, 0, 786432, 0, "stress-2-retired");
        Require(concurrentCollections > 0, "No collection overlapped the mutator cohort");
        Read(0);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; ++i) { Read(0); Read(1); }
        long readBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(readBytes == 0, "Prepared snapshots allocate managed memory");
        var global = Read(0);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Runtime = RuntimeInformation.FrameworkDescription,
            ServerGC = GCSettingsServer(),
            ReadBytes = readBytes,
            GlobalCurrent = global.Current,
            GlobalPeak = global.Peak,
            GlobalAllocated = global.Allocated,
            GlobalRetired = global.Retired,
            GlobalMoved = global.Moved,
            GlobalCollections = global.Collections,
            global.FrozenExcluded,
            ConcurrentCollections = concurrentCollections,
            Reports = reports
        }));
        GC.KeepAlive(roots);
    }

    private static bool GCSettingsServer() => System.Runtime.GCSettings.IsServerGC;
}
