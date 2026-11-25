using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Electron2D;

internal static class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Counts
    {
        public ulong ManagedCurrent, ManagedPeak, NativeCurrent, NativePeak, Current, Peak,
            Allocated, Retired, Allocations, Resizes, Frees, Failures;
    }
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_aot_process_read")]
    private static extern void Snapshot(out Counts counts);
    [DllImport("Electron2DAOTMemory", EntryPoint = "e2d_live_tag")]
    private static extern void RequireCLRProfiler(uint tag);
    private static Counts Read()
    {
        Snapshot(out var c);
        Require(c.ManagedCurrent > 0 && c.Current == c.ManagedCurrent + c.NativeCurrent && c.Peak >= c.Current,
            "The source runtime and engine must share one ledger");
        Require(c.Allocated - c.Retired == c.NativeCurrent, "Native ownership conservation");
        return c;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Main(string[] args)
    {
        if (args is ["--native-only"])
        {
            CheckWithoutManagedProducer();
            return;
        }

        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            RequireCLRProfiler(0);
        }
        Read();
        using (var warmup = new StreamPeerGZIP())
        {
            warmup.StartCompression();
            warmup.Clear();
        }
        var before = Read();
        ulong contribution, retainedPeak;
        using (var codec = new StreamPeerGZIP())
        {
            codec.StartCompression();
            var active = Read();
            var engineNative = NativeAllocation.Snapshot();
            if (active.NativeCurrent <= before.NativeCurrent ||
                engineNative != ((long)active.NativeCurrent, (long)active.NativePeak))
            {
                throw new InvalidOperationException("The engine codec did not increase the shared native ledger");
            }
            contribution = active.NativeCurrent - before.NativeCurrent;
            codec.Clear();
            var retired = Read();
            Require(retired.NativeCurrent == before.NativeCurrent && retired.Peak >= active.Peak,
                "Engine clear did not retire shared ownership or preserve peak");
            retainedPeak = retired.Peak;
        }
        for (int i = 0; i < 64; ++i)
        {
            using var codec = new StreamPeerGZIP();
            codec.StartCompression();
            codec.Clear();
        }
        Require(Read().NativeCurrent == before.NativeCurrent, "Repeated engine sessions leaked shared native payload");
        CheckEngineSnapshots();
        CheckTransientAndDisjointPeaks();
        Collect();
        Read();
        NativeAllocation.ProcessSnapshot();
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20000; ++i)
        {
            NativeAllocation.ProcessSnapshot();
        }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        if (bytes != 0) throw new InvalidOperationException($"Prepared shared snapshots allocate {bytes} bytes");
        var report = new Report(RuntimeInformation.FrameworkDescription, contribution, retainedPeak, bytes, Read().NativeCurrent);
        Console.WriteLine(JsonSerializer.Serialize(report, Context.Default.Report));
        CheckNativeModule();
    }

    private static void CheckNativeModule()
    {
        var engine = File.ReadLines("/proc/self/maps")
            .Where(line => line.EndsWith("/libElectron2D.so", StringComparison.Ordinal))
            .Select(line => line[line.IndexOf('/')..])
            .Distinct(StringComparer.Ordinal)
            .Single();
#if MEMORY_CHECK_PACKAGE
        var expected = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libElectron2D.so");
        Require(engine == expected, "The loaded engine ledger differs from the application's native module");
#endif
        Console.WriteLine($"Loaded engine ledger: {engine}");
    }

    private static unsafe void CheckWithoutManagedProducer()
    {
        var baseline = NativeAllocation.Snapshot();
        var block = NativeAllocation.Alloc(4096);
        try
        {
            var active = NativeAllocation.Snapshot();
            Require(active.Current == baseline.Current + 4096 && active.Peak >= active.Current, "Native accounting is unavailable");
            try
            {
                NativeAllocation.ProcessSnapshot();
                throw new InvalidOperationException("A native-only snapshot was reported as a process total");
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("Native-only accounting works and rejects a process total.");
            }
        }
        finally
        {
            NativeAllocation.Free(block);
        }

        Require(NativeAllocation.Snapshot().Current == baseline.Current, "Native release did not restore current bytes");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void CheckEngineSnapshots()
    {
        const int managedSize = 16 * 1024 * 1024;
        const int nativeSize = 8 * 1024 * 1024;
        var baseline = NativeAllocation.ProcessSnapshot();
        var nativeBaseline = NativeAllocation.Snapshot();

        var retained = new byte[managedSize];
        var managedOnly = NativeAllocation.ProcessSnapshot();
        Require(managedOnly.Current >= baseline.Current + managedSize, "Managed retention is missing");
        Require(NativeAllocation.Snapshot() == nativeBaseline, "Managed bytes changed the native snapshot");

        var block = NativeAllocation.Alloc(nativeSize);
        (long Current, long Peak) overlapping;
        try
        {
            overlapping = NativeAllocation.ProcessSnapshot();
            Require(overlapping.Current >= managedOnly.Current + nativeSize, "Native retention is missing from the total");
            Require(overlapping.Peak >= overlapping.Current, "The common peak lost overlapping ownership");
            Require(NativeAllocation.Snapshot().Current == nativeBaseline.Current + nativeSize, "Native snapshot changed its scope");
        }
        finally
        {
            NativeAllocation.Free(block);
        }

        GC.KeepAlive(retained);
        var nativeRetired = NativeAllocation.Snapshot();
        Require(nativeRetired.Current == nativeBaseline.Current, "Native release did not restore current bytes");
        Console.WriteLine($"Combined (current, peak): baseline {baseline}, managed {managedOnly}, overlap {overlapping}. Native: baseline {nativeBaseline}, after release {nativeRetired}.");
    }

    private static unsafe void CheckTransientAndDisjointPeaks()
    {
        Collect();
        var before = NativeAllocation.ProcessSnapshot();
        const int size = 32 * 1024 * 1024;
        AllocateTransient(size);
        Collect();
        var after = NativeAllocation.ProcessSnapshot();
        Require(after.Current < before.Current + size, "Collected managed storage still contributes to current bytes");
        Require(after.Peak >= before.Current + size && after.Peak > before.Peak, "An unobserved managed allocation did not raise the peak");

        var block = NativeAllocation.Alloc(size);
        (long Current, long Peak) disjoint;
        try
        {
            disjoint = NativeAllocation.ProcessSnapshot();
            Require(disjoint.Current >= after.Current + size, "Disjoint native allocation is missing from current bytes");
            Require(disjoint.Peak >= after.Peak && disjoint.Peak < after.Current + 2 * size,
                "The shared peak combined two allocations that never coexisted");
        }
        finally
        {
            NativeAllocation.Free(block);
        }

        var retired = NativeAllocation.ProcessSnapshot();
        Console.WriteLine($"Combined (current, peak): before transient {before}, after collection {after}, disjoint {disjoint}, after release {retired}.");
        Require(retired.Current == disjoint.Current - size && retired.Peak == disjoint.Peak,
            "Free changed the retained common peak or failed to retire bytes");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AllocateTransient(int size)
    {
        GC.KeepAlive(new byte[size]);
    }

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
internal sealed record Report(string Runtime, ulong EngineNativeContribution, ulong RetainedPeak, long ReadBytes, ulong NativeCurrent);
[JsonSerializable(typeof(Report))]
internal partial class Context : JsonSerializerContext;
