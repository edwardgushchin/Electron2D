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
    private readonly nint _contextLibrary, _contextSnapshot;
    private readonly ulong[]? _frameContexts;
    private readonly long[] _frameOwners = new long[129];
    private readonly ulong _base = 1_000_000UL + (ulong)Interlocked.Increment(ref _runs) * 1000;
    private unsafe PhysicsAllocationTrace(nint library)
    {
        _library = library;
        _begin = Marshal.GetDelegateForFunctionPointer<BeginTrace>(NativeLibrary.GetExport(library, "e2d_gc_trace_begin"));
        _end = Marshal.GetDelegateForFunctionPointer<EndTrace>(NativeLibrary.GetExport(library, "e2d_gc_trace_end"));
        if (Environment.GetEnvironmentVariable("ELECTRON2D_POINT_CONTEXT_TRACE") is { } contextPath)
        {
            _contextLibrary = NativeLibrary.Load(System.IO.Path.GetFullPath(contextPath));
            try
            {
                var method = typeof(GC).GetMethod(nameof(GC.GetAllocatedBytesForCurrentThread), Type.EmptyTypes)!;
                RuntimeHelpers.PrepareMethod(method.MethodHandle);
                var initialize = (delegate* unmanaged<nint, int>)NativeLibrary.GetExport(_contextLibrary, "context_initialize");
                if (initialize(method.MethodHandle.GetFunctionPointer()) != 0)
                    throw new InvalidOperationException("The allocation-context helper does not support this runtime counter implementation.");
                _contextSnapshot = NativeLibrary.GetExport(_contextLibrary, "context_snapshot");
                _frameContexts = new ulong[129 * 5];
            }
            catch { NativeLibrary.Free(_contextLibrary); throw; }
        }
    }
    internal static PhysicsAllocationTrace? Open(PhysicsServer.Backend backend)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_POINT_ALLOCATION_TRACE") is not { } path) return null;
        if (!OperatingSystem.IsLinux() || !RuntimeFeature.IsDynamicCodeSupported || Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING") != "1")
            throw new InvalidOperationException("Point allocation tracing requires a Linux CoreCLR profiling process.");
        if (Environment.GetEnvironmentVariable("E2D_TRACE_STATE") is not null)
            throw new InvalidOperationException("Point allocation tracing uses native begin/end markers, without shared-state overrides.");
        var library = NativeLibrary.Load(System.IO.Path.GetFullPath(path));
        PhysicsAllocationTrace? trace = null;
        try
        {
            trace = new PhysicsAllocationTrace(library);
            trace.Begin(trace._base + 999); trace.End();
            trace.Controls(backend);
            return trace;
        }
        catch { if (trace is null) NativeLibrary.Free(library); else trace.Dispose(); throw; }
    }
    internal ulong Tag(ulong scenario) => _base + scenario;
    internal static void VerifyContextLifetime(string incorrectHelper, string correctHelper)
    {
        incorrectHelper = System.IO.Path.GetFullPath(incorrectHelper); correctHelper = System.IO.Path.GetFullPath(correctHelper);
        var original = Environment.GetEnvironmentVariable("ELECTRON2D_POINT_CONTEXT_TRACE");
        try
        {
            foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU })
            {
                Environment.SetEnvironmentVariable("ELECTRON2D_POINT_CONTEXT_TRACE", incorrectHelper);
                var rejected = false;
                try { using var trace = Open(backend); }
                catch (InvalidOperationException error) when (error.Message == "Allocation-context calibration disagrees with the managed counter.") { rejected = true; }
                if (!rejected || File.ReadAllText("/proc/self/maps").Contains(incorrectHelper, StringComparison.Ordinal))
                    throw new InvalidOperationException("Calibration failure must release its context helper.");
                Environment.SetEnvironmentVariable("ELECTRON2D_POINT_CONTEXT_TRACE", correctHelper);
                using (var recovered = Open(backend)) { }
                if (File.ReadAllText("/proc/self/maps").Contains(correctHelper, StringComparison.Ordinal))
                    throw new InvalidOperationException("Normal tracing disposal must release its context helper.");
            }
        }
        finally { Environment.SetEnvironmentVariable("ELECTRON2D_POINT_CONTEXT_TRACE", original); }
        Console.WriteLine("Context calibration failure and normal disposal release the helper; both backend labels recover in the same process.");
    }
    internal void Begin(ulong tag) => _begin(tag);
    internal void End() => _end();
    internal unsafe void MarkFrame(int frame)
    {
        if (_frameContexts is not null)
            fixed (ulong* output = &_frameContexts[frame * 5])
                ((delegate* unmanaged[SuppressGCTransition]<ulong*, void>)_contextSnapshot)(output);
        _frameOwners[frame] = GC.GetAllocatedBytesForCurrentThread();
    }
    private long FrameDelta(int frame) => _frameOwners[frame + 1] - _frameOwners[frame];
    internal void ReportFrames(long baseline, long allocated)
    {
        Console.WriteLine($"Point frame boundaries: prefix={_frameOwners[0] - baseline}, suffix={baseline + allocated - _frameOwners[^1]} B.");
        for (var i = 0; i < 128; i++)
            if (FrameDelta(i) != 0)
            {
                Console.WriteLine($"Point frame {i}: owner={FrameDelta(i)} B.");
                if (_frameContexts is not null)
                    for (var boundary = i; boundary <= i + 1; boundary++)
                    {
                        var offset = boundary * 5;
                        Console.WriteLine($"Point context {boundary}: TLS={_frameContexts[offset]:x}, ptr={_frameContexts[offset + 1]:x}, limit={_frameContexts[offset + 2]:x}, SOH={_frameContexts[offset + 3]}, UOH={_frameContexts[offset + 4]}, owner={_frameOwners[boundary]}.");
                    }
            }
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
        if (_frameContexts is not null)
        {
            for (var i = 0; i < 129; i++)
            {
                var offset = i * 5;
                var computed = unchecked((long)(_frameContexts[offset + 3] + _frameContexts[offset + 4] - _frameContexts[offset + 2] + _frameContexts[offset + 1]));
                if (computed != _frameOwners[i]) throw new InvalidOperationException("Allocation-context calibration disagrees with the managed counter.");
            }
            Console.WriteLine($"Point context calibration {backend}: 129 native snapshots agree with the managed counter.");
        }
        Console.WriteLine($"Frame trace control {backend}, trace={frameTag}, owner/all={delta}/{total} B; prefix/frame63/suffix=64/64/64 B.");
        ReportFrames(before, delta);
    }
    [MethodImpl(MethodImplOptions.NoInlining)] private static object Allocate() => new byte[37];
    public void Dispose()
    {
        if (_contextLibrary != 0) NativeLibrary.Free(_contextLibrary);
        NativeLibrary.Free(_library);
    }
}
