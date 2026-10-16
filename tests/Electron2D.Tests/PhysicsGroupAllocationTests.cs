using System.Reflection;
using Electron2D;

internal static class PhysicsGroupAllocationTests
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void TouchGroupContext() { var previous = SceneTree.CurrentProcessingGroup; SceneTree.CurrentProcessingGroup = previous; }

    internal static void ThreadStaticControl()
    {
        StartupContracts();
        ContentionControl();
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(SceneTree).TypeHandle);
        using var pool = new WorkerPoolRuntime(2, .5f, 8);
        using var ready = new Barrier(2);
        var first = new long[2]; var second = new long[2]; var threads = new int[2];
        var job = pool.AddGroup(index =>
        {
            threads[index] = Environment.CurrentManagedThreadId; ready.SignalAndWait();
            var before = GC.GetAllocatedBytesForCurrentThread(); TouchGroupContext(); first[index] = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread(); TouchGroupContext(); second[index] = GC.GetAllocatedBytesForCurrentThread() - before;
        }, 2, 2, true, "TLS allocation control");
        pool.Wait(job, true);
        if (threads[0] == threads[1]) throw new InvalidOperationException("TLS control needs two actual workers.");
        Console.WriteLine($"TLS first touch={first[0]}/{first[1]} B, repeat={second[0]}/{second[1]} B.");
        if (first.Any(bytes => bytes != 0) || second.Any(bytes => bytes != 0)) throw new InvalidOperationException("Every worker must prepare group TLS before accepting a job.");
    }

    internal static void ContentionControl()
    {
        static long BlockedEntry(object gate)
        {
            var entering = false; var allocated = 0L;
            var thread = new Thread(() =>
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                Volatile.Write(ref entering, true);
                lock (gate) { }
                allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            });
            lock (gate)
            {
                thread.Start();
                SpinWait.SpinUntil(() => Volatile.Read(ref entering) && (thread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0);
            }
            thread.Join(); return allocated;
        }
        var freshGate = new object();
        var first = BlockedEntry(freshGate); var repeat = BlockedEntry(freshGate);
        using var pool = new WorkerPoolRuntime(2, .5f, 8);
        var poolGate = typeof(WorkerPoolRuntime).GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pool)!;
        var prepared = BlockedEntry(poolGate);
        Console.WriteLine($"Contended monitor entry: fresh={first} B, repeat={repeat} B, prepared pool={prepared} B.");
        if (repeat != 0 || prepared != 0) throw new InvalidOperationException("Prepared pool gate contention must allocate zero bytes.");
    }

    private static void StartupContracts()
    {
        var original = WorkerPoolRuntime.InitializeThread;
        using var entered = new CountdownEvent(2); using var release = new ManualResetEventSlim();
        using var completed = new ManualResetEventSlim();
        WorkerPoolRuntime? created = null; Exception? failure = null;
        WorkerPoolRuntime.InitializeThread = () => { original?.Invoke(); entered.Signal(); release.Wait(); };
        var constructor = new Thread(() =>
        {
            try { created = new WorkerPoolRuntime(2, .5f, 8); }
            catch (Exception error) { failure = error; }
            finally { completed.Set(); }
        });
        try
        {
            constructor.Start();
            if (!entered.Wait(3000) || completed.IsSet) throw new InvalidOperationException("Pool construction must wait for every worker's initialization.");
            release.Set();
            if (!constructor.Join(3000) || failure is not null || created is null) throw new InvalidOperationException("Prepared pool construction failed.", failure);
        }
        finally { release.Set(); constructor.Join(3000); created?.Dispose(); WorkerPoolRuntime.InitializeThread = original; }

        var expected = new InvalidOperationException("Expected worker initialization failure");
        var attempts = 0; var threads = new Thread[2];
        WorkerPoolRuntime.InitializeThread = () =>
        {
            original?.Invoke(); var index = Interlocked.Increment(ref attempts) - 1; threads[index] = Thread.CurrentThread;
            if (index == 0) throw expected;
        };
        try
        {
            try { using var rejected = new WorkerPoolRuntime(2, .5f, 8); throw new InvalidOperationException("Failed initialization must reject construction."); }
            catch (InvalidOperationException error) when (ReferenceEquals(error, expected)) { }
            if (attempts != 2 || threads.Any(thread => thread.IsAlive)) throw new InvalidOperationException("Failed startup must join every started worker.");
        }
        finally { WorkerPoolRuntime.InitializeThread = original; }
    }

    internal static void Run(string mode)
    {
        if (mode is not ("cpu-serial" or "gpu-serial" or "gpu-separate" or "registered-serial" or "registered-separate")) throw new ArgumentException("Unsupported group allocation profile.", nameof(mode));
        var field = typeof(WorkerThreadPool).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic)!;
        long Sample() => ((Lazy<WorkerPoolRuntime>)field.GetValue(WorkerThreadPool.Service)!).Value.WorkerAllocatedBytes;
        BackendChecks.RunNodeGroups(mode.StartsWith("cpu", StringComparison.Ordinal) ? PhysicsServer.Backend.CPU : mode.StartsWith("gpu", StringComparison.Ordinal) ? PhysicsServer.Backend.GPU : null,
            mode.EndsWith("separate", StringComparison.Ordinal), Sample);
    }
}
