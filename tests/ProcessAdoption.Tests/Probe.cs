using System.Runtime.InteropServices;

namespace Electron2D;

// Included only in the instrumented engine, never in a normal runtime or package.
internal static partial class ProcessAdoptionFault
{
    internal static int Stage = -1;
    internal static int PID;
    internal static nint Process, Thread, Observer;
    internal static readonly OutOfMemoryException Failure = new("Test-only post-create allocation failure.");
    [LibraryImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
    private static partial nint Open(uint access, int inherit, int pid);
    internal static void Created(nint process, nint thread, int pid)
    {
        if (Stage < 0) return;
        Process = process; Thread = thread; PID = pid; Observer = Open(0x100001, 0, pid);
        if (Observer == 0) throw new InvalidOperationException("Cannot independently retain the created child.");
    }
    internal static void Reach(int stage) { if (Stage == stage) { Stage = -1; throw Failure; } }
}
