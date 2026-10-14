using System.Runtime.InteropServices;

internal static class NativeAllocationProbe
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Counts
    {
        internal ulong Allocations, RequestedBytes, Frees, Reallocations, Failures;
        internal void Add(Counts other)
        {
            Allocations += other.Allocations; RequestedBytes += other.RequestedBytes;
            Frees += other.Frees; Reallocations += other.Reallocations; Failures += other.Failures;
        }
    }
    private const string Library = "libElectron2DAllocationProbe.so";
    [DllImport(Library, EntryPoint = "electron2d_native_alloc_attached", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Attached();
    [DllImport(Library, EntryPoint = "electron2d_native_alloc_size", CallingConvention = CallingConvention.Cdecl)]
    private static extern nuint Size();
    [DllImport(Library, EntryPoint = "electron2d_native_alloc_begin", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Start();
    [DllImport(Library, EntryPoint = "electron2d_native_alloc_end", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Stop(out Counts counts);
    [DllImport(Library, EntryPoint = "electron2d_native_alloc_smoke", CallingConvention = CallingConvention.Cdecl)]
    private static extern void Smoke();
    internal static void Prepare()
    {
        if (!OperatingSystem.IsLinux() || Attached() != 1 || Size() != (nuint)Marshal.SizeOf<Counts>())
            throw new InvalidOperationException("Native allocation profiling requires the calibrated Linux glibc probe in LD_PRELOAD.");
        Begin(); _ = End(); Smoke(); Begin(); Smoke(); var counts = End();
        if (counts.Allocations != 4 || counts.RequestedBytes != 131 || counts.Frees != 3 || counts.Reallocations != 1 || counts.Failures != 0)
            throw new InvalidOperationException("Native allocation probe calibration failed.");
    }
    internal static void Begin() { if (Start() != 0) throw new InvalidOperationException("Native allocation window is already active."); }
    internal static Counts End()
    {
        if (Stop(out var counts) != 0) throw new InvalidOperationException("Native allocation window is inactive.");
        return counts;
    }
}
