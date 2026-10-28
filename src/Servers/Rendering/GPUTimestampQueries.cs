using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class GPUTimestampQueries : IDisposable
{
    private readonly nint _device;
    private readonly RenderHandle _pool;
    internal GPUTimestampQueries(nint device, SafeHandle parent, uint capacity)
    {
        _device = device; int status; nint pool;
        try { status = NativeTimestampCreate(device, 1, capacity, out pool); }
        catch (Exception error) when (error is EntryPointNotFoundException or DllNotFoundException) { throw new NotSupportedException("Native timestamp queries require the current native Vulkan adapter.", error); }
        if (status == 0) throw new NotSupportedException("This device has no integrated native timestamp query adapter.");
        if (status != 1 || pool == 0) throw new InvalidOperationException("The native timestamp query pool could not be created.");
        try { _pool = new(pool, h => NativeTimestampDestroy(device, 1, h), parent); }
        catch { NativeTimestampDestroy(device, 1, pool); throw; }
    }
    internal void Reset(nint command)
    { if (NativeTimestampReset(_device, 1, _pool.DangerousGetHandle(), command) != 1) throw new InvalidOperationException("The native timestamp query pool could not be reset."); }
    internal void Write(nint command, uint index)
    { if (NativeTimestampWrite(_device, 1, _pool.DangerousGetHandle(), command, index) != 1) throw new InvalidOperationException("The native timestamp could not be recorded."); }
    internal void Read(Span<ulong> values)
    {
        fixed (ulong* data = values)
            if (NativeTimestampRead(_device, 1, _pool.DangerousGetHandle(), (uint)values.Length, data) != 1) throw new InvalidOperationException("Completed native timestamp queries could not be read.");
    }
    internal static void RequireElapsed()
    {
        try { _ = NativeTimestampElapsed(0, 1, 0, out _); }
        catch (Exception error) when (error is EntryPointNotFoundException or DllNotFoundException) { throw new NotSupportedException("Native elapsed queries require the current native Vulkan adapter.", error); }
    }
    internal ulong ReadElapsed()
    {
        if (NativeTimestampElapsed(_device, 1, _pool.DangerousGetHandle(), out var elapsed) != 1) throw new InvalidOperationException("Completed native elapsed queries could not be read.");
        return elapsed;
    }
    public void Dispose() => _pool.Dispose();
    [LibraryImport("Electron2DRender", EntryPoint = "e2d_render_timestamp_create")] private static partial int NativeTimestampCreate(nint device, uint abi, uint capacity, out nint pool);
    [LibraryImport("Electron2DRender", EntryPoint = "e2d_render_timestamp_reset")] private static partial int NativeTimestampReset(nint device, uint abi, nint pool, nint command);
    [LibraryImport("Electron2DRender", EntryPoint = "e2d_render_timestamp_write")] private static partial int NativeTimestampWrite(nint device, uint abi, nint pool, nint command, uint index);
    [LibraryImport("Electron2DRender", EntryPoint = "e2d_render_timestamp_read")] private static partial int NativeTimestampRead(nint device, uint abi, nint pool, uint count, ulong* values);
    [LibraryImport("Electron2DRender", EntryPoint = "e2d_render_timestamp_elapsed")] private static partial int NativeTimestampElapsed(nint device, uint abi, nint pool, out ulong elapsed);
    [LibraryImport("Electron2DRender", EntryPoint = "e2d_render_timestamp_destroy")] private static partial void NativeTimestampDestroy(nint device, uint abi, nint pool);
}
