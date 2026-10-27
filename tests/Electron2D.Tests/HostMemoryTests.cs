using Electron2D;
using System.Runtime.InteropServices;

internal static class HostMemoryTests
{
    internal static void Run(string[] args)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_MEMORY_FIXTURE") is { } path) { NativeFailures(path); return; }
        Check(OS.MemoryBytes(0, 4096) == 0 && OS.AddMemoryBytes(0, 0) == 0, "Known zero budgets are preserved.");
        Check(OS.MemoryBytes(1, 0) == -1 && OS.MemoryBytes(ulong.MaxValue, 1) == -1 && OS.MemoryBytes((ulong)long.MaxValue / 4096 + 1, 4096) == -1, "Unknown unit size and overflow cannot wrap into a usable budget.");
        Check(OS.MemoryBytes((ulong)long.MaxValue, 1) == long.MaxValue && OS.AddMemoryBytes(long.MaxValue - 1, 1) == long.MaxValue, "Largest representable byte count remains exact.");
        Check(OS.AddMemoryBytes(-1, 10) == -1 && OS.AddMemoryBytes(10, -1) == -1 && OS.AddMemoryBytes(long.MaxValue, 1) == -1, "Unavailable components and sum overflow remain unknown.");
        HostMemoryChecks.Run(args);
        var retained = OS.GetMemoryInfo();
        using (var loop = new MemoryLoop())
        {
            Engine.Start(loop);
            try { Engine.AdvanceFrame(1d / 60); }
            finally { Engine.Stop(); }
        }
        Check(OS.GetMemoryInfo().Physical == retained.Physical, "Query and retained snapshots survive lifecycle retirement.");
        Console.WriteLine("Host memory startup/frame/finalization and retirement passed.");
    }
    private sealed class MemoryLoop : MainLoop
    {
        protected override void OnInitialize() => Check(OS.GetMemoryInfo().Physical > 0, "Initialization can query native host memory.");
        protected override bool OnProcess(double delta) { Check(OS.GetMemoryInfo().Physical > 0, "Frame callbacks can query native host memory."); return false; }
        protected override void OnFinalize() => Check(OS.GetMemoryInfo().Physical > 0, "Finalization can query native host memory.");
    }
    private static void NativeFailures(string path)
    {
        var library = NativeLibrary.Load(path);
        var install = Marshal.GetDelegateForFunctionPointer<InstallFilter>(NativeLibrary.GetExport(library, "memory_filter"));
        foreach (var denyMemory in new[] { true, false })
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var before = OS.GetMemoryInfo();
                    Check(install(denyMemory ? 1 : 0) == 0, "Install the current-thread native failure filter.");
                    var actual = OS.GetMemoryInfo();
                    if (denyMemory) Check(actual.Physical == -1 && actual.Free == -1 && actual.Available == -1 && actual.Stack == before.Stack, "A failed kernel memory query preserves the independently successful stack budget.");
                    else Check(actual.Physical == before.Physical && actual.Free >= 0 && actual.Available >= 0 && actual.Stack == -1, "A failed kernel stack query preserves independently successful memory budgets.");
                }
                catch (Exception error) { failure = error; }
            });
            thread.Start(); thread.Join();
            if (failure is not null) throw failure;
        }
        NativeLibrary.Free(library);
        Console.WriteLine("Host memory native ABI: real sysinfo and resource-limit failure isolation passed.");
    }
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int InstallFilter(int denyMemory);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
