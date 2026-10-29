using System.Runtime.InteropServices;

internal static unsafe class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Counts
    {
        public ulong ManagedCurrent, ManagedPeak, NativeCurrent, NativePeak, Current, Peak,
            Allocated, Retired, Allocations, Resizes, Frees, Failures;
    }
    private static delegate* unmanaged[Cdecl]<Counts*, void> _snapshot;
    private static (long Current, long Peak) Read()
    {
        Counts counts; _snapshot(&counts);
        if (counts.Allocated - counts.Retired != counts.NativeCurrent) throw new InvalidOperationException("Native ownership conservation failed.");
        return ((long)counts.NativeCurrent, (long)counts.NativePeak);
    }
    private static void Main()
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException("This native measurement host requires Linux x64.");
        var path = Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libElectron2DENet.so");
        var library = NativeLibrary.Load(path);
        // The runtime retains the same process-owned library and its allocation records.
        _snapshot = (delegate* unmanaged[Cdecl]<Counts*, void>)NativeLibrary.GetExport(library, "e2d_memory_snapshot");
        NativeICUAllocationChecks.Run(Read, fresh: true);
    }
}
