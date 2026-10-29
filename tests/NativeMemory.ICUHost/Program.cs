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
        if (args.Length == 2 && args[0] == "shaping-mixed")
        {
            RejectFontBundle(args[1]);
        }
        else if (args.Length == 2 && args[0] == "shaping-old-core")
        {
            if (NativeLibrary.TryGetExport(library, "e2d_memory_failure_epoch", out _)) throw new InvalidOperationException("Expected an older core.");
            RejectFontBundle(args[1]);
        }
        else if (args.Length == 2 && args[0] is "shaping" or "shaping-fault" or "shaping-oracle")
            NativeShapingAllocationChecks.Run(Read, File.ReadAllBytes(args[1]), args[0] == "shaping-fault", args[0] == "shaping-oracle");
        else if (args.Length == 1 && args[0] == "regex-fault")
        {
            _fail = (delegate* unmanaged[Cdecl]<uint, void>)NativeLibrary.GetExport(library, "e2d_test_fail_after");
            NativeRegExAllocationChecks.Faults(Read, Fail);
        }
        else if (args.Length == 2 && args[0] == "msdf") NativeMSDFAllocationChecks.Run(Read, File.ReadAllBytes(args[1]));
        else if (args.Length == 1 && args[0] == "regex") NativeRegExAllocationChecks.Run(Read, fresh: true);
        else if (args.Length == 0) NativeICUAllocationChecks.Run(Read, fresh: true);
        else throw new ArgumentException("Expected no arguments, regex, regex-fault, msdf FONT, shaping FONT, shaping-fault FONT, shaping-oracle FONT, shaping-mixed FONT or shaping-old-core FONT.");
    }
    private static void RejectFontBundle(string source)
    {
        using var font = new Electron2D.FontFile(); var changed = 0; font.Changed += _ => changed++;
        try { font.Data = File.ReadAllBytes(source); throw new InvalidOperationException("A mismatched bundle accepted a font owner."); }
        catch (PlatformNotSupportedException)
        {
            if (font.Data.Length != 0 || changed != 0 || Read().Current != 0) throw new InvalidOperationException("Rejected native preparation published font state.");
            Console.WriteLine("Mismatched bundle rejected before font owner publication; bytes/events/native current unchanged.");
        }
    }
}
