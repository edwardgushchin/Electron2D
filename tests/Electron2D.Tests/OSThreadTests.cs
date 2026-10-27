using Electron2D;
using HostThread = System.Threading.Thread;

internal static class OSThreadTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void OnThread(Action action)
    {
        Exception? failure = null;
        var thread = new HostThread(() => { try { action(); } catch (Exception error) { failure = error; } });
        thread.Start(); thread.Join();
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    private sealed class Loop(bool failStart = false, bool failStop = false) : MainLoop
    {
        protected override void OnInitialize()
        {
            Check(OS.GetThreadCallerID() == OS.GetMainThreadID(), "Initialization observes the actual lifecycle owner.");
            if (failStart) throw new ArgumentException("identity startup probe");
        }
        protected override bool OnProcess(double delta)
        { Check(OS.GetThreadCallerID() == OS.GetMainThreadID(), "Frames retain the main identity."); return false; }
        protected override void OnFinalize()
        {
            Check(OS.GetThreadCallerID() == OS.GetMainThreadID(), "Finalization retains the main identity until cleanup completes.");
            if (failStop) throw new ArgumentException("identity shutdown probe");
        }
    }
    private sealed class MovingNode : Entity
    {
        protected override void OnProcess(double delta)
        {
            if (OS.GetThreadCallerID() != OS.GetMainThreadID()) throw new InvalidOperationException("Scene processing lost its lifecycle identity.");
            Position += new Vector2((float)delta, 0);
        }
    }
    internal static void Run(string profile)
    {
        if (profile != "managed") { Native(profile); return; }
        ulong firstWorker = 0;
        OnThread(() => { firstWorker = OS.GetThreadCallerID(); Check(firstWorker > OS.GetMainThreadID() && firstWorker == OS.GetThreadCallerID(), "A first-use worker cannot become the main thread."); });
        var ordinary = OS.GetThreadCallerID();
        Check(ordinary > OS.GetMainThreadID() && ordinary != firstWorker, "Ordinary caller identities are distinct and nonzero.");
        var ids = new ulong[12]; var errors = new Exception?[ids.Length]; var threads = new HostThread[ids.Length];
        using (var barrier = new Barrier(ids.Length))
        {
            for (var i = 0; i < threads.Length; i++)
            {
                var slot = i;
                threads[i] = new HostThread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait(); ids[slot] = OS.GetThreadCallerID();
                        for (var n = 0; n < 1000; n++) if (OS.GetThreadCallerID() != ids[slot]) throw new InvalidOperationException("A caller identity changed on an ordinary thread.");
                    }
                    catch (Exception error) { errors[slot] = error; }
                }); threads[i].Start();
            }
            foreach (var thread in threads) thread.Join();
        }
        Check(errors.All(e => e is null) && ids.All(id => id > OS.GetMainThreadID()) && ids.Distinct().Count() == ids.Length, "Concurrent first-use IDs remain unique and stable.");
        using (var loop = new Loop())
        {
            Engine.Start(loop);
            OnThread(() =>
            {
                var before = OS.GetThreadCallerID();
                using var competing = new Loop(); Reject<InvalidOperationException>(() => Engine.Start(competing)); Reject<InvalidOperationException>(Engine.Stop);
                Check(OS.GetThreadCallerID() == before && before != OS.GetMainThreadID(), "Rejected concurrent startup preserves both identities.");
            });
            var owner = Environment.CurrentManagedThreadId;
            var task = WorkerThreadPool.AddTask(() =>
            {
                var id = OS.GetThreadCallerID();
                Check(id == OS.GetThreadCallerID() && (id == OS.GetMainThreadID()) == (Environment.CurrentManagedThreadId == owner), "Pool work reports its actual caller, including owner-side helping.");
            }); WorkerThreadPool.WaitForTaskCompletion(task);
            Engine.AdvanceFrame(1d / 60); Engine.Stop();
        }
        Check(OS.GetThreadCallerID() == ordinary, "Stopping restores the caller's ordinary identity.");
        OnThread(() =>
        {
            var before = OS.GetThreadCallerID();
            using var loop = new Loop(); Engine.Start(loop); Engine.AdvanceFrame(1d / 60);
            // The retained audio service still belongs to the preceding host's thread.
            Reject<InvalidOperationException>(Engine.Stop);
            Check(OS.GetThreadCallerID() == before && Engine.MainLoop is null, "A different actual host owner retires its identity even when retained audio rejects teardown.");
        });
        using (var failed = new Loop(failStart: true)) Reject<ArgumentException>(() => Engine.Start(failed));
        Check(OS.GetThreadCallerID() == ordinary && Engine.MainLoop is null, "Failed initialization releases the main identity.");
        using (var failed = new Loop(failStop: true)) { Engine.Start(failed); Reject<ArgumentException>(Engine.Stop); }
        Check(OS.GetThreadCallerID() == ordinary && Engine.MainLoop is null, "Failed finalization releases the main identity.");
        using (var tree = new SceneTree(new Node()))
        {
            var moving = new MovingNode(); tree.Root.AddChild(moving); Engine.Start(tree);
            for (var i = 0; i < 128; i++) Engine.AdvanceFrame(1d / 60);
            var position = moving.Position.X; var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) { _ = OS.GetThreadCallerID(); _ = OS.GetMainThreadID(); Engine.AdvanceFrame(1d / 60); }
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(bytes == 0 && moving.Position.X > position + 2, "128 complete prepared scene frames retain motion and zero owner managed bytes.");
            Engine.Stop();
        }
        Reject<ArgumentNullException>(() => OS.SetThreadName(null!)); Reject<ArgumentException>(() => OS.SetThreadName("bad\0name"));
        if (OperatingSystem.IsLinux()) OnThread(NativeNames);
        Console.WriteLine($"PASS {_checks} thread identity/lifecycle/native-name contracts; 12 concurrent callers and 128 scene frames.");
    }
    private static void NativeNames()
    {
        static string Read() => File.ReadAllText("/proc/thread-self/comm").TrimEnd('\n');
        var original = Read(); var id = OS.GetThreadCallerID();
        try
        {
            foreach (var name in new[] { "e2d_worker", "e2d_я", "123456789012345", "" })
            { OS.SetThreadName(name); Check(Read() == name && OS.GetThreadCallerID() == id, "The native kernel name changes without changing identity."); }
            OS.SetThreadName("retained");
            Reject<ArgumentException>(() => OS.SetThreadName("1234567890123456"));
            Reject<ArgumentException>(() => OS.SetThreadName("éééééééé"));
            Check(Read() == "retained", "Native UTF-8 byte-limit failures preserve the preceding name.");
        }
        finally { OS.SetThreadName(original); }
    }
    private static void Native(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Disabled);
        var ordinary = OS.GetThreadCallerID(); var root = new Window { Size = new(240, 160) }; var ready = false;
        root.Ready += _ => { ready = true; Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested native renderer actually executes."); Check(OS.GetThreadCallerID() == OS.GetMainThreadID(), "Native readiness observes the window host owner."); };
        Check(Engine.Run((window, frame) =>
        { Check(OS.GetThreadCallerID() == OS.GetMainThreadID(), "The before-frame host hook observes main identity."); if (frame == 2) window.Tree!.Quit(); }, root) == 0 && ready, "The ordinary native host executes and retires.");
        Check(OS.GetThreadCallerID() == ordinary && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native shutdown retires the main role and services.");
        var failing = new Window { Size = new(240, 160) };
        failing.Ready += _ => { Check(OS.GetThreadCallerID() == OS.GetMainThreadID(), "Failing native readiness still has its owner."); throw new ArgumentException("identity native startup probe"); };
        try { Engine.Run(failing); throw new InvalidOperationException("Expected native readiness failure"); }
        catch (Exception error) { Check(error.ToString().Contains("identity native startup probe", StringComparison.Ordinal), "The intended native callback failure propagates."); }
        Check(OS.GetThreadCallerID() == ordinary && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native startup failure releases the main role and services.");
        Console.WriteLine($"PASS {_checks} native thread-owner/retirement checks on {backend}.");
    }
}
