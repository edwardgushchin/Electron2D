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
    private static Counts Read()
    {
        Snapshot(out var c);
        Require(c.ManagedCurrent > 0 && c.Current == c.ManagedCurrent + c.NativeCurrent && c.Peak >= c.Current,
            "The source runtime and engine must share one ledger");
        Require(c.Allocated - c.Retired == c.NativeCurrent, "Native ownership conservation");
        return c;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Main()
    {
        Require(!RuntimeFeature.IsDynamicCodeSupported, "This host requires the source-instrumented NativeAOT profile");
        using (var warmup = new StreamPeerGZIP()) { warmup.StartCompression(); warmup.Clear(); }
        var before = Read();
        ulong contribution, retainedPeak;
        using (var codec = new StreamPeerGZIP())
        {
            codec.StartCompression();
            var active = Read();
            contribution = active.NativeCurrent - before.NativeCurrent;
            Require(contribution > 300000 && active.NativePeak >= active.NativeCurrent,
                "The real engine codec did not enter the runtime ledger");
            codec.Clear();
            var retired = Read();
            Require(retired.NativeCurrent == before.NativeCurrent && retired.Peak >= active.Peak,
                "Engine clear did not retire shared ownership or preserve peak");
            retainedPeak = retired.Peak;
        }
        for (int i = 0; i < 64; ++i) { using var codec = new StreamPeerGZIP(); codec.StartCompression(); codec.Clear(); }
        Require(Read().NativeCurrent == before.NativeCurrent, "Repeated engine sessions leaked shared native payload");
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Read();
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20000; ++i) Read();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        Require(bytes == 0, "Prepared shared snapshots allocate");
        var report = new Report(RuntimeInformation.FrameworkDescription, contribution, retainedPeak, bytes, Read().NativeCurrent);
        Console.WriteLine(JsonSerializer.Serialize(report, Context.Default.Report));
    }
}
internal sealed record Report(string Runtime, ulong EngineNativeContribution, ulong RetainedPeak, long ReadBytes, ulong NativeCurrent);
[JsonSerializable(typeof(Report))]
internal partial class Context : JsonSerializerContext;
