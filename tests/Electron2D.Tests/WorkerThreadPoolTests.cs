using Electron2D;
using System.Runtime.CompilerServices;

internal static class WorkerThreadPoolTests
{
    internal static void Run()
    {
        ValidationAndCapacity(); ParallelGroups(); Priorities(); NestedWaits(); ConcurrentWaits(false); ConcurrentWaits(true); FailuresAndLifetime(); ContextAndShutdown(); AllocationBudget(); PublicSceneWorkflow();
        Console.WriteLine("Worker pool: identities, bounded preparation, priority/parallel groups, progress, nested/concurrent waits, failures, lifetime, context, scene publication and warmed sender/worker allocation checks passed.");
    }
    private static void ValidationAndCapacity()
    {
        using var pool = new WorkerPoolRuntime(1, 0, 2);
        Reject<ArgumentNullException>(() => pool.AddTask(null!)); Reject<ArgumentNullException>(() => pool.AddTask(static () => { }, description: null!));
        Reject<ArgumentNullException>(() => pool.AddGroup(null!, 0)); Reject<ArgumentOutOfRangeException>(() => pool.AddGroup(static _ => { }, -1)); Reject<ArgumentOutOfRangeException>(() => pool.AddGroup(static _ => { }, 1, 0));
        var empty = pool.AddGroup(static _ => throw new Exception("Empty callback ran."), 0, 0);
        var empty2 = pool.AddGroup(static _ => { }, 0, int.MinValue);
        Check(pool.IsCompleted(empty, true) && pool.Processed(empty) == 0 && pool.PendingCount == 2, "Empty groups finish but retain completion ownership.");
        Reject<InvalidOperationException>(() => pool.AddTask(static () => { })); Reject<ArgumentException>(() => pool.Wait(empty, false));
        pool.Wait(empty, true); Reject<ArgumentException>(() => pool.Processed(empty));
        var next = pool.AddTask(static () => { }); Check(next > empty2, "IDs never reused after capacity recovery."); pool.Wait(next, false); pool.Wait(empty2, true);
        Reject<ArgumentException>(() => pool.Wait(next, false)); Reject<ArgumentException>(() => pool.IsCompleted(-1, false));
        foreach (var runnerCount in new[] { 1, int.MaxValue, -2, int.MinValue }) { var hits = 0; var group = pool.AddGroup(_ => Interlocked.Increment(ref hits), 7, runnerCount); pool.Wait(group, true); Check(hits == 7, "Runner limits preserve exact index count."); }
        using var settings = new ProjectSettingsRegistry(Environment.CurrentDirectory, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-worker-user"));
        Check(settings.Get(ProjectSettings.WorkerPoolMaxThreads) == -1 && settings.Get(ProjectSettings.WorkerPoolMaxPendingTasks) == 1024, "Startup settings registered.");
        Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.WorkerPoolMaxThreads, 0)); Reject<ArgumentException>(() => settings.Set(ProjectSettings.WorkerPoolLowPriorityThreadRatio, float.NaN)); Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.WorkerPoolLowPriorityThreadRatio, 2)); Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.WorkerPoolMaxPendingTasks, 0));
    }
    private static void ParallelGroups()
    {
        using var pool = new WorkerPoolRuntime(2, 1, 8); using var entered = new CountdownEvent(2); using var release = new ManualResetEventSlim();
        var hits = new int[257]; var threads = new int[2]; var caller = Environment.CurrentManagedThreadId;
        var group = pool.AddGroup(index =>
        {
            Check(pool.CallerTaskID == -1 && pool.CallerGroupID > 0 && Environment.CurrentManagedThreadId != caller, "Group callback identity/thread.");
            if (index < 2) { threads[index] = Environment.CurrentManagedThreadId; entered.Signal(); Await(release); }
            Interlocked.Increment(ref hits[index]);
        }, hits.Length, highPriority: true);
        Await(entered); Check(pool.Processed(group) == 0 && !pool.IsCompleted(group, true) && threads[0] != threads[1], "Progress excludes running callbacks and group runs in parallel.");
        release.Set(); pool.Wait(group, true); Check(hits.All(v => v == 1), "Every index exactly once.");
        Check(pool.CallerTaskID == -1 && pool.CallerGroupID == -1, "Outside identity.");
    }
    private static void Priorities()
    {
        using var pool = new WorkerPoolRuntime(1, 1, 8); using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        var blocker = pool.AddTask(() => { entered.Set(); Await(release); }, true); Await(entered);
        var order = new List<int>(); var low1 = pool.AddTask(() => order.Add(1)); var low2 = pool.AddTask(() => order.Add(2)); var high1 = pool.AddTask(() => order.Add(3), true); var high2 = pool.AddTask(() => order.Add(4), true);
        release.Set(); foreach (var id in new[] { blocker, low1, low2, high1, high2 }) pool.Wait(id, false);
        Check(order.SequenceEqual(new[] { 3, 4, 1, 2 }), "Nonpreemptive priority and FIFO.");
        using var limited = new WorkerPoolRuntime(2, 0, 4); using var lowEntered = new ManualResetEventSlim(); using var lowRelease = new ManualResetEventSlim(); using var highEntered = new ManualResetEventSlim();
        var low = limited.AddTask(() => { lowEntered.Set(); Await(lowRelease); }); Await(lowEntered);
        var high = limited.AddTask(highEntered.Set, true); Await(highEntered); lowRelease.Set(); limited.Wait(low, false); limited.Wait(high, false);
    }
    private static void NestedWaits()
    {
        using var pool = new WorkerPoolRuntime(1, 1, 16); var hits = 0;
        var parent = pool.AddTask(() =>
        {
            var parentID = pool.CallerTaskID; Check(parentID > 0 && pool.CallerGroupID == -1, "Regular caller ID."); Reject<InvalidOperationException>(() => pool.Wait(parentID, false));
            var child = pool.AddTask(() => { var childID = pool.CallerTaskID; Check(childID > parentID, "Nested caller ID."); var group = pool.AddGroup(_ => { Check(pool.CallerTaskID == -1 && pool.CallerGroupID > childID, "Nested group ID."); Interlocked.Increment(ref hits); }, 17); pool.Wait(group, true); Check(pool.CallerTaskID == childID, "Child context restored."); }, true);
            pool.Wait(child, false); Check(pool.CallerTaskID == parentID && pool.CallerGroupID == -1, "Parent context restored.");
        }); pool.Wait(parent, false); Check(hits == 17, "Single-worker cooperative wait completes nested regular/group work.");
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        var blocker = pool.AddTask(() => { entered.Set(); Await(release); }, true); Await(entered);
        var order = new List<int>(); long secondChild = -1;
        var older = pool.AddTask(() => order.Add(1)); var newer = pool.AddTask(() =>
        {
            Reject<InvalidOperationException>(() => pool.Wait(older, false));
            var child = pool.AddTask(() => order.Add(2)); secondChild = pool.AddTask(() => order.Add(3)); pool.Wait(child, false);
            var current = pool.CallerTaskID; var failed = pool.AddTask(static () => throw new IOException("nested"), true); Reject<IOException>(() => pool.Wait(failed, false)); Check(pool.CallerTaskID == current, "Failed nested callback restores parent context.");
        }, true);
        release.Set(); pool.Wait(blocker, false); pool.Wait(newer, false); pool.Wait(older, false); pool.Wait(secondChild, false);
        Check(order.SequenceEqual(new[] { 2, 1, 3 }), "Helping skips older work without changing remaining FIFO order.");
        var completed = pool.AddGroup(static _ => { }, 0); var waitCompleted = pool.AddTask(() => pool.Wait(completed, true)); pool.Wait(waitCompleted, false);
    }
    private static void ConcurrentWaits(bool group)
    {
        using var pool = new WorkerPoolRuntime(2, 1, 8); using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        Action callback = () => { entered.Set(); Await(release); };
        var id = group ? pool.AddGroup(_ => callback(), 1) : pool.AddTask(callback); Await(entered);
        var first = Task.Run(() => pool.Wait(id, group)); var second = Task.Run(() => pool.Wait(id, group));
        Check(SpinWait.SpinUntil(() => pool.WaitingCount(id, group) == 2, TimeSpan.FromSeconds(10)), "Both waiters registered.");
        release.Set(); Check(Task.WaitAll([first, second], TimeSpan.FromSeconds(10)), "Concurrent waiters finish safely."); Reject<ArgumentException>(() => pool.IsCompleted(id, group));
    }
    private static void FailuresAndLifetime()
    {
        using var pool = new WorkerPoolRuntime(2, 1, 8); var failure = new IOException("regular callback"); var task = pool.AddTask(() => throw failure);
        try { pool.Wait(task, false); throw new Exception("Failure missing."); } catch (IOException error) { Check(ReferenceEquals(error, failure) && error.StackTrace!.Contains(nameof(FailuresAndLifetime)), "Preserved callback failure/stack."); }
        var hits = new int[31]; var group = pool.AddGroup(index => { Interlocked.Increment(ref hits[index]); if (index == 0 || index == 19) throw new IOException("index " + index); }, hits.Length);
        Check(SpinWait.SpinUntil(() => pool.IsCompleted(group, true), TimeSpan.FromSeconds(10)) && pool.Processed(group) == hits.Length, "Failed attempts count as finished progress.");
        try { pool.Wait(group, true); throw new Exception("Failure missing."); } catch (IOException error) { Check(error.Message == "index 0", "Deterministic lowest-index group failure."); }
        Check(hits.All(v => v == 1) && pool.PendingCount == 0, "Failure drains all indices and retires records.");
        var weak = RetainedCallback(pool); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); Check(!weak.IsAlive, "Retirement releases callback targets.");
    }
    private sealed class Payload { internal void Invoke() { } }
    [MethodImpl(MethodImplOptions.NoInlining)] private static WeakReference RetainedCallback(WorkerPoolRuntime pool) { var payload = new Payload(); var weak = new WeakReference(payload); var id = pool.AddTask(payload.Invoke); pool.Wait(id, false); return weak; }
    private static void ContextAndShutdown()
    {
        var ambient = new AsyncLocal<string?> { Value = "caller" }; using var pool = new WorkerPoolRuntime(1, 1, 8);
        var context = pool.AddTask(() => Check(ambient.Value is null && SynchronizationContext.Current is null, "Caller execution context is not captured.")); pool.Wait(context, false);
        var rejected = pool.AddTask(() => Reject<InvalidOperationException>(pool.Dispose)); pool.Wait(rejected, false);
        var draining = new WorkerPoolRuntime(2, 1, 128); var hits = 0; for (var i = 0; i < 100; i++) draining.AddTask(() => Interlocked.Increment(ref hits)); draining.Dispose(); Check(hits == 100, "Shutdown drains accepted work."); Reject<ObjectDisposedException>(() => draining.AddTask(static () => { }));
    }
    private static void AllocationBudget()
    {
        using var pool = new WorkerPoolRuntime(2, 1, 16); Action regular = static () => { }; Action<int> grouped = static _ => { };
        void Batch() { var a = pool.AddTask(regular); var b = pool.AddGroup(grouped, 64); _ = pool.IsCompleted(a, false); _ = pool.Processed(b); pool.Wait(a, false); pool.Wait(b, true); }
        for (var i = 0; i < 128; i++) Batch();
        var sender = GC.GetAllocatedBytesForCurrentThread(); var workers = pool.WorkerAllocatedBytes;
        for (var i = 0; i < 1024; i++) Batch();
        Check(GC.GetAllocatedBytesForCurrentThread() == sender && pool.WorkerAllocatedBytes == workers, "1024 regular/group submit, query, wait and worker callback dispatch cycles allocate zero managed bytes after 128 warm cycles.");
    }
    private static void PublicSceneWorkflow()
    {
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.WorkerPoolLowPriorityThreadRatio, 1); ProjectSettings.Set(ProjectSettings.WorkerPoolMaxPendingTasks, 64);
        Check(WorkerThreadPool.GetCallerTaskID() == -1 && WorkerThreadPool.GetCallerGroupID() == -1, "Unstarted caller queries.");
        Check(ReferenceEquals(Engine.GetSingleton<WorkerThreadPool>(nameof(WorkerThreadPool)), WorkerThreadPool.Service), "Retained named service identity.");
        Reject<InvalidOperationException>(WorkerThreadPool.Service.Dispose); Reject<InvalidOperationException>(() => Engine.UnregisterSingleton(nameof(WorkerThreadPool))); Check(!WorkerThreadPool.Service.IsDisposed, "Permanent service survives rejected disposal/removal.");
        Check(typeof(WorkerThreadPool).GetProperty("Instance") is null && typeof(WorkerThreadPool).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Instance).Length == 0, "Static public facade.");
        using var root = new Node(); using var tree = new SceneTree(root); var grids = new AStarGrid[4]; var routes = new Vector2[4][]; var published = new Vector2[4][]; var owner = Environment.CurrentManagedThreadId;
        for (var i = 0; i < grids.Length; i++) { grids[i] = new AStarGrid { Region = new(0, 0, 16, 16), CellSize = new(2, 2) }; grids[i].Update(); }
        try
        {
            var regular = WorkerThreadPool.AddTask(() => Check(WorkerThreadPool.GetCallerTaskID() > 0 && WorkerThreadPool.GetCallerGroupID() == -1, "Public regular caller."), description: "regular identity"); WorkerThreadPool.WaitForTaskCompletion(regular);
            var group = WorkerThreadPool.AddGroupTask(index => { Check(WorkerThreadPool.GetCallerTaskID() == -1 && WorkerThreadPool.GetCallerGroupID() > 0, "Public group caller."); routes[index] = grids[index].GetPointPath(new(1, index + 1), new(13, index + 1)); tree.Defer(() => { Check(Environment.CurrentManagedThreadId == owner, "Deferred publication on scene owner."); published[index] = routes[index]; }); }, grids.Length, highPriority: true, description: "parallel routes");
            Check(SpinWait.SpinUntil(() => WorkerThreadPool.IsGroupTaskCompleted(group), TimeSpan.FromSeconds(10)), "Public group completes."); Check(WorkerThreadPool.GetGroupProcessedElementCount(group) == 4, "Public group progress."); WorkerThreadPool.WaitForGroupTaskCompletion(group);
            Check(published.All(route => route is null), "Workers publish only via deferred lane."); tree.FlushDeferred(); Check(published.All(route => route.Length >= 2), "Independent route computations reach scene owner.");
            Reject<ArgumentException>(() => WorkerThreadPool.IsTaskCompleted(group));
            ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 1); var sampled = WorkerThreadPool.AddTask(static () => { }); WorkerThreadPool.WaitForTaskCompletion(sampled);
        }
        finally { foreach (var grid in grids) grid.Dispose(); ProjectSettings.Reset(ProjectSettings.WorkerPoolMaxThreads); ProjectSettings.Reset(ProjectSettings.WorkerPoolLowPriorityThreadRatio); ProjectSettings.Reset(ProjectSettings.WorkerPoolMaxPendingTasks); }
    }
    internal static void Await(ManualResetEventSlim signal) => Check(signal.Wait(TimeSpan.FromSeconds(10)), "Worker test signal timed out.");
    private static void Await(CountdownEvent signal) => Check(signal.Wait(TimeSpan.FromSeconds(10)), "Parallel worker test timed out.");
    internal static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
