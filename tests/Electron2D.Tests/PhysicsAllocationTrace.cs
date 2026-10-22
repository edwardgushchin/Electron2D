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
    }
    [MethodImpl(MethodImplOptions.NoInlining)] private static object Allocate() => new byte[37];
    public void Dispose() => NativeLibrary.Free(_library);
}
