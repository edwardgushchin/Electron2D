using Electron2D;
using System.Globalization;

internal static class HostMemoryChecks
{
    internal static void Run(string[] args)
    {
        // First-use contention also exercises retained OS service initialization.
        var threads = new Thread[4];
        var snapshots = new OSMemoryInfo[threads.Length];
        Exception? failure = null;
        using var ready = new Barrier(threads.Length);
        for (var i = 0; i < threads.Length; i++)
        {
            var index = i;
            threads[i] = new Thread(() => { try { ready.SignalAndWait(); snapshots[index] = OS.GetMemoryInfo(); } catch (Exception error) { Interlocked.CompareExchange(ref failure, error, null); } }, 1024 * 1024);
            threads[i].Start();
        }
        foreach (var thread in threads) thread.Join();
        if (failure is not null) throw failure;
        var first = OS.GetMemoryInfo();
        Check(first.Physical > 0 && first.Free >= 0 && first.Free <= first.Physical && first.Available >= 0, "Native host memory is available.");
        foreach (var snapshot in snapshots)
        {
            Check(snapshot.Physical == first.Physical && snapshot.Free >= 0 && snapshot.Available >= 0, "Worker snapshots query the same host.");
            if (!OperatingSystem.IsWindows()) Check(snapshot.Stack == first.Stack, "Unix stack budget is the process soft limit, independent of requested managed thread stack size.");
            else Check(snapshot.Stack > 0, "Windows reports the caller's native stack range.");
        }
        if (args.Length != 0) Check(first.Stack == long.Parse(args[0], CultureInfo.InvariantCulture), "Stack budget matches the independently configured child limit.");
        if (OperatingSystem.IsLinux())
        {
            var total = File.ReadLines("/proc/meminfo").First(line => line.StartsWith("MemTotal:", StringComparison.Ordinal));
            var bytes = long.Parse(total.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1], CultureInfo.InvariantCulture) * 1024;
            Check(first.Physical == bytes, "Physical total agrees with the independent kernel proc view.");
            Check(!File.ReadLines("/proc/self/maps").Any(line => line.Contains("libSDL3", StringComparison.Ordinal)), "Native memory queries do not load SDL.");
        }
        for (var i = 0; i < 32; i++) _ = OS.GetMemoryInfo();
        var before = GC.GetAllocatedBytesForCurrentThread();
        long observed = 0;
        for (var i = 0; i < 1024; i++) observed |= OS.GetMemoryInfo().Physical;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(observed > 0 && allocated == 0, "Prepared snapshots allocate zero managed caller bytes.");
        Check(first.Physical > 0 && !RenderingServer.IsAvailable, "Snapshots remain independent and never open a renderer.");
        Console.WriteLine($"Host memory: physical={first.Physical}, free={first.Free}, available={first.Available}, stack={first.Stack}; workers=4; snapshots=1024; owner_bytes={allocated}.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
