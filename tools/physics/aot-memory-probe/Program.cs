using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static partial class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Counts
    {
        public ulong Current, Peak, Allocated, Retired, Count, Collections, FastObjects, FastArrays, Slow, FrozenCount, FrozenBytes, Failures;
    }

    private sealed class Ordinary(long value) { public readonly long Value = value; }
    private sealed class Temporary(long value) { public readonly long Value = value; }
    private sealed class WorkerOne(long value) { public readonly long Value = value; }
    private sealed class WorkerTwo(long value) { public readonly long Value = value; }
    private sealed class Finalized
    {
        public long Value = 1;
        ~Finalized() => Interlocked.Increment(ref finalized);
    }

    private static int finalized, completedWorkers, midpointWorkers;
    private static readonly List<Row> rows = [];

    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_watch")]
    private static extern void Watch(uint tag, nint type, long length);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_read")]
    private static extern void Snapshot(uint tag, out Counts result);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_fail_next")]
    private static extern void FailNext(uint tag);

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static Counts Read(uint tag)
    {
        Snapshot(tag, out var c);
        Require(c.Allocated - c.Retired == c.Current && c.Peak >= c.Current, "Ledger conservation");
        return c;
    }

    private static Counts Check(string phase, uint tag, ulong current, ulong allocated, ulong count)
    {
        var c = Read(tag);
        Require(c.Current == current && c.Allocated == allocated && c.Count == count,
            $"{phase}: tag={tag} current={c.Current} allocated={c.Allocated} count={c.Count}");
        rows.Add(new(phase, tag, c.Current, c.Peak, c.Allocated, c.Retired, c.Count,
            c.Collections, c.FastObjects, c.FastArrays, c.Slow, c.Failures));
        return c;
    }

    private static void Collect(int generation) => GC.Collect(generation, GCCollectionMode.Forced, true, true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Fill(object?[] roots)
    {
        for (int i = 0; i < 1024; ++i) roots[i] = new Ordinary(i);
        for (int i = 1024; i < 1089; ++i) roots[i] = new string('x', 128);
        roots[1089] = GC.AllocateUninitializedArray<byte>(131072, pinned: true);
        for (int i = 1090; i < 1098; ++i) roots[i] = new Finalized();
        roots[1098] = new byte[262144];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearOdd(object?[] roots)
    {
        for (int i = 1; i < 1024; i += 2) roots[i] = null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropLarge(object?[] roots) => roots[1089] = roots[1098] = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Validate(object?[] roots)
    {
        for (int i = 0; i < 1024; i += 2)
            Require(((Ordinary)roots[i]!).Value == i, "Retained object payload changed");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropAll(object?[] roots) => Array.Clear(roots);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void HiddenPeak()
    {
        var roots = new Temporary[512];
        for (int i = 0; i < roots.Length; ++i) roots[i] = new Temporary(i);
        GC.KeepAlive(roots);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Worker(bool first, bool stress, Barrier barrier)
    {
        var roots = new object[stress ? 1024 : 4096];
        if (stress) barrier.SignalAndWait();
        for (int batch = 0; batch < (stress ? 32 : 1); ++batch)
        {
            for (int i = 0; i < roots.Length; ++i)
                roots[i] = first ? new WorkerOne(i) : new WorkerTwo(i);
            if (stress && batch == 15)
            {
                Interlocked.Increment(ref midpointWorkers);
                barrier.SignalAndWait();
            }
        }
        if (!stress) { barrier.SignalAndWait(); barrier.SignalAndWait(); }
        for (int i = 0; i < roots.Length; ++i)
            Require((first ? ((WorkerOne)roots[i]).Value : ((WorkerTwo)roots[i]).Value) == i,
                "Worker payload changed across collection");
        GC.KeepAlive(roots);
        if (stress) Interlocked.Increment(ref completedWorkers);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RejectedArray()
    {
        bool rejected = false;
        try { GC.KeepAlive(GC.AllocateUninitializedArray<byte>(int.MaxValue, pinned: true)); }
        catch (OutOfMemoryException) { rejected = true; }
        Require(rejected, "Over-limit array was not rejected");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FailedArray()
    {
        bool rejected = false;
        try { GC.KeepAlive(GC.AllocateUninitializedArray<byte>(123456, pinned: true)); }
        catch (OutOfMemoryException) { rejected = true; }
        Require(rejected, "Injected allocator failure did not propagate");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RetryArray(object?[] roots) => roots[0] = GC.AllocateUninitializedArray<byte>(123456, pinned: true);

    private static void Main(string[] args)
    {
        var roots = new object?[1099];
        Read(0);
        if (args.Contains("--invalid-snapshot")) Read(16);
        Collect(2);
        GC.WaitForPendingFinalizers();
        Collect(2);
        Watch(1, typeof(Ordinary).TypeHandle.Value, -1);
        Watch(2, typeof(string).TypeHandle.Value, 128);
        Watch(3, typeof(byte[]).TypeHandle.Value, 131072);
        Watch(4, typeof(Finalized).TypeHandle.Value, -1);
        Watch(5, typeof(byte[]).TypeHandle.Value, 262144);
        Watch(6, typeof(Temporary).TypeHandle.Value, -1);
        Watch(7, typeof(byte[]).TypeHandle.Value, int.MaxValue);
        Watch(8, typeof(WorkerOne).TypeHandle.Value, -1);
        Watch(9, typeof(WorkerTwo).TypeHandle.Value, -1);
        Watch(10, typeof(byte[]).TypeHandle.Value, 123456);
        Watch(11, typeof(byte[]).TypeHandle.Value, 2400000);
        Watch(12, typeof(byte[]).TypeHandle.Value, 7000000);
        if (args.Contains("--invalid-watch")) Watch(1, typeof(Ordinary).TypeHandle.Value, -1);
        Fill(roots);
        Require(Check("objects", 1, 24576, 24576, 1024).FastObjects > 0, "Missing object fast path");
        Require(Check("strings", 2, 18200, 18200, 65).FastArrays > 0, "Missing string fast path");
        Require(Check("pinned", 3, 131096, 131096, 1).Slow == 1, "Missing pinned direct allocation");
        Require(Check("finalizable", 4, 192, 192, 8).Slow == 8, "Finalizable allocation route");
        Require(Check("large", 5, 262168, 262168, 1).Slow == 1, "Large allocation route");
        ClearOdd(roots);
        Collect(0);
        Check("generation-0", 1, 12288, 24576, 512);
        DropLarge(roots);
        Collect(0);
        Check("pinned-not-collected-in-gen0", 3, 131096, 131096, 1);
        Check("large-not-collected-in-gen0", 5, 262168, 262168, 1);
        Collect(1);
        Check("generation-1", 1, 12288, 24576, 512);
        Check("pinned-not-collected-in-gen1", 3, 131096, 131096, 1);
        Check("large-not-collected-in-gen1", 5, 262168, 262168, 1);
        Collect(2);
        Check("generation-2", 1, 12288, 24576, 512);
        Check("pinned-retired", 3, 0, 131096, 0);
        Check("large-retired", 5, 0, 262168, 0);
        Validate(roots);
        DropAll(roots);
        Collect(2);
        Check("finalizer-queue-survives", 4, 192, 192, 8);
        GC.WaitForPendingFinalizers();
        Collect(2);
        Require(finalized == 8, "Finalizer count");
        Check("objects-retired", 1, 0, 24576, 0);
        Check("strings-retired", 2, 0, 18200, 0);
        Check("finalizers-retired", 4, 0, 192, 0);
        HiddenPeak();
        Collect(2);
        Require(Check("peak-between-reads", 6, 0, 12288, 0).Peak == 12288, "Lost unobserved peak");
        RejectedArray();
        Check("rejected-array", 7, 0, 0, 0);
        FailNext(10);
        FailedArray();
        Require(Check("injected-failure", 10, 0, 0, 0).Failures == 1, "Failed attempt was not observed");
        RetryArray(roots);
        Require(Check("retry-after-failure", 10, 123480, 123480, 1).Slow == 1, "Successful retry was not counted");
        DropAll(roots);
        Collect(2);
        Check("retry-retired", 10, 0, 123480, 0);
        using var barrier = new Barrier(3);
        var first = new Thread(() => Worker(true, false, barrier));
        var second = new Thread(() => Worker(false, false, barrier));
        first.Start(); second.Start();
        barrier.SignalAndWait();
        Collect(2);
        Check("worker-1-survives", 8, 98304, 98304, 4096);
        Check("worker-2-survives", 9, 98304, 98304, 4096);
        barrier.SignalAndWait();
        first.Join(); second.Join();
        Collect(2);
        Check("worker-1-retired", 8, 0, 98304, 0);
        Check("worker-2-retired", 9, 0, 98304, 0);
        using var stressBarrier = new Barrier(3);
        var stressFirst = new Thread(() => Worker(true, true, stressBarrier));
        var stressSecond = new Thread(() => Worker(false, true, stressBarrier));
        stressFirst.Start(); stressSecond.Start();
        stressBarrier.SignalAndWait();
        int concurrentCollections = 0;
        bool midpointCollection = false;
        while (Volatile.Read(ref completedWorkers) != 2)
        {
            bool atMidpoint = !midpointCollection && Volatile.Read(ref midpointWorkers) == 2;
            Collect(0); Read(0); ++concurrentCollections;
            if (atMidpoint)
            {
                stressBarrier.SignalAndWait();
                midpointCollection = true;
            }
            Thread.Sleep(1);
        }
        stressFirst.Join(); stressSecond.Join();
        Collect(2);
        Check("stress-1-retired", 8, 0, 884736, 0);
        Check("stress-2-retired", 9, 0, 884736, 0);
        Require(midpointCollection && concurrentCollections > 0, "No collection at the worker midpoint");
        var frozen = RunFrozenChecks(args);
        var process = RunNativeChecks(args);
        Read(0); Read(1);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; ++i) { Read(0); Read(1); }
        long readBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(readBytes == 0, "Prepared reads allocated");
        var all = Read(0);
        var report = new Report(RuntimeInformation.FrameworkDescription, RuntimeFeature.IsDynamicCodeSupported,
            System.Runtime.GCSettings.IsServerGC, readBytes, concurrentCollections, midpointCollection, all.Current, all.Peak,
            all.Allocated, all.Retired, all.Collections, all.FrozenCount, all.FrozenBytes, rows.ToArray(), frozen, process);
        Console.WriteLine(JsonSerializer.Serialize(report, JsonContext.Default.Report));
        GC.KeepAlive(roots);
    }
}
internal sealed record Row(string Phase, uint Tag, ulong Current, ulong Peak, ulong Allocated, ulong Retired,
    ulong Count, ulong Collections, ulong FastObjects, ulong FastArrays, ulong Slow, ulong Failures);
internal sealed record Report(string Runtime, bool DynamicCode, bool ServerGC, long ReadBytes,
    int ConcurrentCollections, bool MidpointCollection, ulong Current, ulong Peak, ulong Allocated, ulong Retired, ulong Collections,
    ulong FrozenCount, ulong FrozenBytes, Row[] Rows, FrozenReport Frozen, ProcessReport Process);
[JsonSerializable(typeof(Report))]
internal partial class JsonContext : JsonSerializerContext;
