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
    private static delegate* unmanaged[Cdecl]<uint, void> _fail;
    private static void Fail(uint position) => _fail(position);
    private static (long Current, long Peak) Read()
    {
        Counts counts; _snapshot(&counts);
        if (counts.Allocated - counts.Retired != counts.NativeCurrent) throw new InvalidOperationException("Native ownership conservation failed.");
        return ((long)counts.NativeCurrent, (long)counts.NativePeak);
    }
    private static void Main(string[] args)
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException("This native measurement host requires Linux x64.");
        var path = Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libElectron2DENet.so");
        var library = NativeLibrary.Load(path);
        // The runtime retains the same process-owned library and its allocation records.
        _snapshot = (delegate* unmanaged[Cdecl]<Counts*, void>)NativeLibrary.GetExport(library, "e2d_memory_snapshot");
        if (args.Length == 1 && args[0] == "regex-fault")
        {
            _fail = (delegate* unmanaged[Cdecl]<uint, void>)NativeLibrary.GetExport(library, "e2d_test_fail_after");
            NativeRegExAllocationChecks.Faults(Read, Fail);
        }
        else if (args.Length == 1 && args[0] == "regex") NativeRegExAllocationChecks.Run(Read, fresh: true);
        else if (args.Length == 0) NativeICUAllocationChecks.Run(Read, fresh: true);
        else throw new ArgumentException("Expected no arguments, regex or regex-fault.");
    }
}
