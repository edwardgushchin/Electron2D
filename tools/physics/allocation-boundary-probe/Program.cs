using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static unsafe class Program
{
    private const int ObjectCount = 512, StringCount = 64, PinnedLength = 131072;
    private static readonly Witness[] Objects = new Witness[ObjectCount];
    private static readonly string[] Strings = new string[StringCount];
    private static byte[] _pinned = [];
    private static void Main(string[] args)
    {
        var traced = args.Contains("--trace"); nint library = 0;
        try
        {
            if (traced)
            {
                library = NativeLibrary.Load(Environment.GetEnvironmentVariable("CORECLR_PROFILER_PATH")!);
                _traceBegin = (delegate* unmanaged[Cdecl]<ulong, void>)NativeLibrary.GetExport(library, "e2d_gc_trace_begin");
                _traceEnd = (delegate* unmanaged[Cdecl]<void>)NativeLibrary.GetExport(library, "e2d_gc_trace_end");
            }
            Run(traced);
        }
        finally { _traceBegin = null; _traceEnd = null; if (library != 0) NativeLibrary.Free(library); }
    }
    private static void Run(bool traced)
    {
        Console.WriteLine($"Runtime boundary: {(RuntimeFeature.IsDynamicCodeSupported ? "CLR" : "NativeAOT")}");
        var token = RuntimeFeature.IsDynamicCodeSupported ? typeof(Witness).MetadataToken : 0;
        FillObjects(); FillStrings(); FillPinned();
        Begin(); Read(out _, out _);
        if (traced) { TraceBegin(9); TraceEnd(); }
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var rows = new Phase[3];
        rows[0] = Measure(1, "objects", ObjectCount, FillObjects, traced);
        rows[1] = Measure(2, "strings", StringCount, FillStrings, traced);
        rows[2] = Measure(3, "pinned-array", 1, FillPinned, traced);
        if (rows.Any(row => row.ManagedBytes <= 0)) throw new InvalidOperationException("Every route must allocate real managed storage.");
        if (!RuntimeFeature.IsDynamicCodeSupported &&
            (rows[0].FastEntries != ObjectCount || rows[0].SlowEntries >= ObjectCount ||
             rows[1].FastEntries != 0 || rows[2].FastEntries != 0 || rows[2].SlowEntries != 0))
            throw new InvalidOperationException("The selected runtime's allocation routes differ from the qualified boundary.");
        var report = new Report(RuntimeInformation.FrameworkDescription, RuntimeFeature.IsDynamicCodeSupported,
            token, ObjectCount, StringCount, PinnedLength, rows);
        Console.WriteLine(JsonSerializer.Serialize(report, JsonContext.Default.Report));
        GC.KeepAlive(Objects); GC.KeepAlive(Strings); GC.KeepAlive(_pinned);
    }
    private static Phase Measure(ulong tag, string name, int count, Action action, bool traced)
    {
        // Prepare the delegate and query paths before opening either native interval.
        var before = GC.GetAllocatedBytesForCurrentThread();
        Begin(); if (traced) TraceBegin(tag);
        action();
        if (traced) TraceEnd(); Read(out var fast, out var slow);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        return new(name, count, bytes, fast, slow);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FillObjects() { for (var i = 0; i < Objects.Length; i++) Objects[i] = new Witness(i); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FillStrings() { for (var i = 0; i < Strings.Length; i++) Strings[i] = new string('x', 128); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FillPinned() => _pinned = GC.AllocateUninitializedArray<byte>(PinnedLength, pinned: true);
    private sealed class Witness(long value) { internal readonly long Value = value; }
    [DllImport("Electron2DMemoryRoute", EntryPoint = "e2d_route_begin")] private static extern void Begin();
    [DllImport("Electron2DMemoryRoute", EntryPoint = "e2d_route_read")] private static extern void Read(out ulong fast, out ulong slow);
    private static delegate* unmanaged[Cdecl]<ulong, void> _traceBegin;
    private static delegate* unmanaged[Cdecl]<void> _traceEnd;
    private static void TraceBegin(ulong tag) => _traceBegin(tag);
    private static void TraceEnd() => _traceEnd();
}
internal sealed record Phase(string Name, int Objects, long ManagedBytes, ulong FastEntries, ulong SlowEntries);
internal sealed record Report(string Runtime, bool DynamicCode, int WitnessToken, int ObjectCount, int StringCount, int PinnedLength, Phase[] Phases);
[JsonSerializable(typeof(Report))]
internal partial class JsonContext : JsonSerializerContext;
