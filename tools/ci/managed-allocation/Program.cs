using System.Runtime.InteropServices;

internal static unsafe class Program
{
    private static object? _one, _two;
    private static delegate* unmanaged[Cdecl]<ulong, void> Begin;
    private static delegate* unmanaged[Cdecl]<void> End;

    private static void Main(string[] args)
    {
        var library = NativeLibrary.Load(Environment.GetEnvironmentVariable("CORECLR_PROFILER_PATH")!);
        try
        {
            if (args.Length == 1 && args[0] == "--fail-output")
                ((delegate* unmanaged[Cdecl]<void>)NativeLibrary.GetExport(library, "e2d_gc_trace_test_unwritable_output"))();
            Begin = (delegate* unmanaged[Cdecl]<ulong, void>)NativeLibrary.GetExport(library, "e2d_gc_trace_begin");
            End = (delegate* unmanaged[Cdecl]<void>)NativeLibrary.GetExport(library, "e2d_gc_trace_end");
            Console.WriteLine("Allocation calibration architecture: " + RuntimeInformation.ProcessArchitecture);
            Begin(9000); End(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            AllocationWarmupCheck.Verify(AllocationWarmupCheck.Collect);
            AllocationWarmupCheck.Collect();
            Empty(4); Sample(1, false); Sample(2, true);
            var ready = false; var go = false; var done = false; var stop = false; var workerBytes = 0L;
            var thread = new Thread(() =>
            {
                Begin(9001); End(); _ = GC.GetAllocatedBytesForCurrentThread();
                Volatile.Write(ref ready, true); while (!Volatile.Read(ref go)) Thread.Yield();
                var before = GC.GetAllocatedBytesForCurrentThread(); Begin(3); _one = new byte[40]; End();
                workerBytes = GC.GetAllocatedBytesForCurrentThread() - before;
                Volatile.Write(ref done, true); while (!Volatile.Read(ref stop)) Thread.Yield();
            }) { Name = "Calibration worker" };
            thread.Start(); while (!Volatile.Read(ref ready)) Thread.Yield();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            Volatile.Write(ref go, true); while (!Volatile.Read(ref done)) Thread.Yield();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Volatile.Write(ref stop, true); thread.Join();
            Console.WriteLine($"Worker control own/all/worker={own}/{all}/{workerBytes}");
            if (own != 0 || all != 64 || workerBytes != 64) throw new InvalidOperationException("Worker calibration.");
            Empty(5);
        }
        finally { Begin = null; End = null; NativeLibrary.Free(library); }
    }

    private static void Sample(ulong tag, bool pair)
    {
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); Begin(tag);
        if (pair) { _one = new byte[8]; _two = new byte[8]; } else _one = new byte[40];
        End(); own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Console.WriteLine($"Owner control {tag}: {own}/{all}");
        if (own != 64 || all != 64) throw new InvalidOperationException("Owner calibration.");
    }

    private static void Empty(ulong tag)
    {
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); Begin(tag);
        Thread.SpinWait(10000);
        End(); own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Console.WriteLine($"Empty control {tag}: {own}/{all}");
        if (own != 0 || all != 0) throw new InvalidOperationException("Empty calibration.");
    }
}
