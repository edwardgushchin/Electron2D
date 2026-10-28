using System.Diagnostics.Tracing;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static class Program
{
    private static long _sink;
    private static void Main()
    {
        for (var i = 0; i < 128; i++) _sink = GC.GetGCMemoryInfo().HeapSizeBytes;
        Collect();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) _sink = GC.GetGCMemoryInfo().HeapSizeBytes;
        var infoBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        for (var i = 0; i < 128; i++) _sink = GC.GetTotalMemory(false);
        allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) _sink = GC.GetTotalMemory(false);
        var totalBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Collect();
        var initial = GC.GetTotalMemory(false);
        var oraclePeak = Transient();
        Collect(); GC.Collect(0, GCCollectionMode.Forced, true, true);
        GC.Collect(1, GCCollectionMode.Forced, true, true); Collect();
        var after = GC.GetTotalMemory(false);
        var last = GC.GetGCMemoryInfo();
        long lastBefore = 0;
        foreach (var generation in last.GenerationInfo) lastBefore += generation.SizeBeforeBytes;
        var sampledPeak = Math.Max(initial, after);
        if (oraclePeak < initial + 60 * 1024 * 1024 || sampledPeak >= oraclePeak / 2 || lastBefore >= oraclePeak / 2)
            throw new InvalidOperationException("The controlled transient peak witness was not established.");
        using var listener = new RuntimeListener();
        Thread.Sleep(100);
        var listenerOwner = Environment.CurrentManagedThreadId;
        for (var i = 0; i < 4; i++) Collect();
        var deadline = Environment.TickCount64 + 2000;
        while (Volatile.Read(ref listener.HeapStats) == 0 && Environment.TickCount64 < deadline) Thread.Sleep(1);
        var report = new Report(RuntimeInformation.FrameworkDescription, RuntimeFeature.IsDynamicCodeSupported,
            GCSettings.IsServerGC, infoBytes, totalBytes, initial, oraclePeak, after, sampledPeak,
            lastBefore, last.Index, listener.Sources, listener.HeapStats, listener.AllocationTicks,
            listener.CallbackThread, listenerOwner, _sink);
        Console.WriteLine(JsonSerializer.Serialize(report, JsonContext.Default.Report));
    }
    private static void Collect()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Transient()
    {
        var bytes = new byte[64 * 1024 * 1024];
        Array.Fill(bytes, (byte)1);
        var peak = GC.GetTotalMemory(false);
        GC.KeepAlive(bytes);
        return peak;
    }
}

internal sealed class RuntimeListener : EventListener
{
    internal int Sources, HeapStats, AllocationTicks, CallbackThread;
    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name != "Microsoft-Windows-DotNETRuntime") return;
        Interlocked.Increment(ref Sources);
        EnableEvents(source, EventLevel.Verbose, (EventKeywords)1);
    }
    protected override void OnEventWritten(EventWrittenEventArgs data)
    {
        if (data.EventName?.StartsWith("GCHeapStats", StringComparison.Ordinal) == true)
        {
            Volatile.Write(ref CallbackThread, Environment.CurrentManagedThreadId);
            Interlocked.Increment(ref HeapStats);
        }
        if (data.EventName?.StartsWith("GCAllocationTick", StringComparison.Ordinal) == true)
            Interlocked.Increment(ref AllocationTicks);
    }
}

internal sealed record Report(string Runtime, bool DynamicCode, bool ServerGC,
    long MemoryInfoOwnerBytes1024, long TotalMemoryOwnerBytes1024, long InitialManaged,
    long OracleManagedPeak, long FinalManaged, long SampledManagedPeak, long LatestGCBefore,
    long LatestGCIndex, int RuntimeSources, int HeapStatsCallbacks, int AllocationTickCallbacks,
    int CallbackThread, int OwnerThread, long Sink);
[JsonSerializable(typeof(Report))]
internal partial class JsonContext : JsonSerializerContext;
