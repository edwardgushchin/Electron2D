using Electron2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal sealed class PhysicsAllocationTrace : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void BeginTrace(ulong tag);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void EndTrace();
    private static int _runs;
    private readonly nint _library;
    private readonly BeginTrace _begin;
    private readonly EndTrace _end;
    private readonly long[] _frameOwners = new long[129];
    private readonly ulong _base = 1_000_000UL + (ulong)Interlocked.Increment(ref _runs) * 1000;
    private PhysicsAllocationTrace(nint library)
    {
        _library = library;
        _begin = Marshal.GetDelegateForFunctionPointer<BeginTrace>(NativeLibrary.GetExport(library, "e2d_gc_trace_begin"));
        _end = Marshal.GetDelegateForFunctionPointer<EndTrace>(NativeLibrary.GetExport(library, "e2d_gc_trace_end"));
    }
    internal static PhysicsAllocationTrace? Open(PhysicsServer.Backend backend)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_POINT_ALLOCATION_TRACE") is not { } path) return null;
        if (!OperatingSystem.IsLinux() || !RuntimeFeature.IsDynamicCodeSupported || Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING") != "1")
            throw new InvalidOperationException("Point allocation tracing requires a Linux CoreCLR profiling process.");
        if (Environment.GetEnvironmentVariable("E2D_TRACE_STATE") is not null)
            throw new InvalidOperationException("Point allocation tracing uses native begin/end markers, without shared-state overrides.");
        var library = NativeLibrary.Load(System.IO.Path.GetFullPath(path));
        try
        {
            var trace = new PhysicsAllocationTrace(library);
            trace.Begin(trace._base + 999); trace.End();
            trace.Controls(backend);
            return trace;
        }
        catch { NativeLibrary.Free(library); throw; }
    }
    internal ulong Tag(ulong scenario) => _base + scenario;
    internal void Begin(ulong tag) => _begin(tag);
    internal void End() => _end();
    internal void MarkFrame(int frame) => _frameOwners[frame] = GC.GetAllocatedBytesForCurrentThread();
    private long FrameDelta(int frame) => _frameOwners[frame + 1] - _frameOwners[frame];
    internal void ReportFrames(long baseline, long allocated)
    {
        Console.WriteLine($"Point frame boundaries: prefix={_frameOwners[0] - baseline}, suffix={baseline + allocated - _frameOwners[^1]} B.");
        for (var i = 0; i < 128; i++)
            if (FrameDelta(i) != 0) Console.WriteLine($"Point frame {i}: owner={FrameDelta(i)} B.");
    }
    private void Controls(PhysicsServer.Backend backend)
    {
        for (var i = 0; i < 160; i++)
        {
            var positive = i < 128; var tag = _base + (positive ? 100UL : 300UL) + (ulong)(positive ? i : i - 128);
            Begin(tag);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            var value = positive ? Allocate() : null;
            Thread.SpinWait(10000);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            End(); GC.KeepAlive(value);
            Console.WriteLine($"Point trace control {backend}, positive={positive}, trace={tag}, owner/all={owner}/{all} B.");
        }
        MarkFrame(0);
        var frameTag = Tag(400); Begin(frameTag);
        var before = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
        var prefix = Allocate(); object? middle = null;
        for (var i = 0; i < 128; i++) { MarkFrame(i); if (i == 63) middle = Allocate(); }
        MarkFrame(128); var suffix = Allocate();
        var delta = GC.GetAllocatedBytesForCurrentThread() - before; total = GC.GetTotalAllocatedBytes(true) - total;
        End(); GC.KeepAlive(prefix); GC.KeepAlive(middle); GC.KeepAlive(suffix);
        if (delta != 192 || _frameOwners[0] - before != 64 || before + delta - _frameOwners[^1] != 64)
            throw new InvalidOperationException("Frame trace control did not retain its prefix/suffix allocations.");
        for (var i = 0; i < 128; i++)
            if (FrameDelta(i) != (i == 63 ? 64 : 0)) throw new InvalidOperationException("Frame trace control reported the wrong allocation interval.");
        Console.WriteLine($"Frame trace control {backend}, trace={frameTag}, owner/all={delta}/{total} B; prefix/frame63/suffix=64/64/64 B.");
        ReportFrames(before, delta);
    }
    [MethodImpl(MethodImplOptions.NoInlining)] private static object Allocate() => new byte[37];
    public void Dispose() => NativeLibrary.Free(_library);
}
