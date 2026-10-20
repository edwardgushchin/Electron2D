using System.Runtime.CompilerServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class RenderThreadTests
{
    private sealed class ExpectedFailure(string message) : Exception(message);
    private static void Worker(Action action)
    {
        Exception? error = null; var thread = new Thread(() => { try { action(); } catch (Exception next) { error = next; } }); thread.Start();
        Check(thread.Join(10000), "Worker enqueues without waiting for the owner or holding a callback lock."); if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
    internal static void Managed()
    {
        Check(!RenderingServer.IsAvailable, "Unavailable-session checks start without a renderer.");
        Reject<InvalidOperationException>(() => RenderingServer.IsOnRenderThread()); Reject<InvalidOperationException>(() => RenderingServer.CallOnRenderThread(static () => { })); Reject<ArgumentNullException>(() => RenderingServer.CallOnRenderThread(null!));
        Worker(() => { Reject<InvalidOperationException>(() => RenderingServer.IsOnRenderThread()); Reject<InvalidOperationException>(() => RenderingServer.CallOnRenderThread(static () => { })); });
        Console.WriteLine("Render-thread bridge rejects absent sessions and null callbacks.");
    }
    private static void Contract()
    {
        var owner = Environment.CurrentManagedThreadId; Check(RenderingServer.IsOnRenderThread(), "Native renderer shares the current scene owner.");
        Worker(() => { Check(!RenderingServer.IsOnRenderThread(), "Worker identity is observational and false."); Reject<InvalidOperationException>(() => RenderingServer.SetDefaultClearColor(Colors.Red)); });
        var order = new List<int>(); var failure = new ExpectedFailure("queued failure");
        Worker(() =>
        {
            RenderingServer.CallOnRenderThread(() => { Check(RenderingServer.IsOnRenderThread() && Environment.CurrentManagedThreadId == owner, "Queued actions execute on the exact owner."); order.Add(1); RenderingServer.CallOnRenderThread(() => order.Add(2)); Worker(() => RenderingServer.CallOnRenderThread(() => order.Add(6))); });
            RenderingServer.CallOnRenderThread(() => { order.Add(3); throw failure; }); RenderingServer.CallOnRenderThread(() => order.Add(4));
        });
        try { RenderingServer.CallOnRenderThread(() => order.Add(5)); throw new InvalidOperationException("Expected queued failure."); } catch (ExpectedFailure error) { Check(ReferenceEquals(error, failure), "A single queued failure preserves its identity."); }
        Check(order.SequenceEqual(new[] { 1, 2, 3, 4, 5 }), "Owner drains prior FIFO snapshot, invokes nested and requested owner actions, and continues after failure."); RenderingServer.ForceSync(); Check(order.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }), "Requests accepted during a drain wait for a later boundary.");
        var immediate = new ExpectedFailure("inline failure"); try { RenderingServer.CallOnRenderThread(() => throw immediate); throw new InvalidOperationException("Expected inline failure."); } catch (ExpectedFailure error) { Check(ReferenceEquals(error, immediate), "Inline failure propagates synchronously."); }
        Worker(() => RenderingServer.CallOnRenderThread(() => throw failure));
        try { RenderingServer.CallOnRenderThread(() => throw immediate); throw new InvalidOperationException("Expected owner aggregate."); } catch (AggregateException error) { Check(error.Flatten().InnerExceptions.SequenceEqual(new Exception[] { failure, immediate }), "Prior queued error and caller action failure aggregate in execution order."); }
        var pending = false; Worker(() => RenderingServer.CallOnRenderThread(() => pending = true)); Reject<ArgumentOutOfRangeException>(() => RenderingServer.ForceDraw(frameStep: double.NaN)); Check(!pending, "Invalid forced-frame arguments do not consume render-thread requests."); RenderingServer.ForceSync(); Check(pending, "A rejected forced frame leaves its queued request for a valid boundary.");
        var tails = new int[4]; var producers = new Thread[4]; Exception? producerError = null;
        for (var i = 0; i < producers.Length; i++)
        {
            var producer = i; producers[i] = new Thread(() =>
            {
                try { for (var n = 0; n < 64; n++) { var index = n; RenderingServer.CallOnRenderThread(() => { Check(RenderingServer.IsOnRenderThread() && tails[producer]++ == index, "Each concurrent producer retains request order with exactly-once owner execution."); }); } }
                catch (Exception next) { Interlocked.CompareExchange(ref producerError, next, null); }
            }); producers[i].Start();
        }
        foreach (var producer in producers) Check(producer.Join(10000), "Concurrent producer returns after enqueue."); Check(producerError is null, "Concurrent submissions succeed while the session is live."); RenderingServer.ForceSync(); Check(tails.All(n => n == 64), "All 256 concurrent requests execute exactly once.");
        var first = new ExpectedFailure("first"); var second = new ExpectedFailure("second"); var completed = false;
        Worker(() => { RenderingServer.CallOnRenderThread(() => throw first); RenderingServer.CallOnRenderThread(() => throw second); RenderingServer.CallOnRenderThread(() => completed = true); });
        try { RenderingServer.ForceSync(); throw new InvalidOperationException("Expected aggregate."); } catch (AggregateException error) { Check(error.Flatten().InnerExceptions.SequenceEqual(new Exception[] { first, second }) && completed, "Sync reports all callback failures after trying later actions."); }
        var capture = QueueCapture(); RenderingServer.CallOnRenderThread(static () => { }); Collect(); Check(!capture.IsAlive, "Executed queue entries release captured objects.");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QueueCapture(Action? invoked = null)
    { var capture = new object(); var weak = new WeakReference(capture); Worker(() => RenderingServer.CallOnRenderThread(() => { invoked?.Invoke(); GC.KeepAlive(capture); })); return weak; }
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); }
    internal static void Native(string profile)
    {
        var method = profile.StartsWith("gpu", StringComparison.Ordinal) ? "gpu" : "compatibility";
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback); var low = ProjectSettings.Get(ProjectSettings.LowProcessorMode); var sleep = ProjectSettings.Get(ProjectSettings.LowProcessorModeSleepUsec); var fps = Engine.MaxFPS;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ProjectSettings.Set(ProjectSettings.LowProcessorMode, true); ProjectSettings.Set(ProjectSettings.LowProcessorModeSleepUsec, 0); Engine.MaxFPS = 0;
            using var root = new Window { Size = new(256, 128), Title = "Electron2D render-thread bridge" }; var driver = new Driver(root, method, profile.EndsWith("hlsl", StringComparison.Ordinal)); root.AddChild(driver);
            root.Ready += _ => { Contract(); driver.Prepare(); RenderingServer.RenderLoopEnabled = false; };
            try { Check(Engine.Run(root) == 0 && driver.Completed && root.IsDisposed, "Worker/native rendering session completes and retires."); }
            finally { driver.Stop(); }
            Collect(); Check(driver.CanceledCapture is { IsAlive: false } && driver.CanceledInvocations == 0 && !RenderingServer.IsAvailable, "Shutdown cancels unconsumed requests and releases their captures."); Managed();
            using (var reopened = new Window { Size = new(256, 128) }) { reopened.Ready += _ => { Check(RenderingServer.IsOnRenderThread(), "Reopened service reports its current owner."); RenderingServer.CallOnRenderThread(static () => { }); reopened.Tree!.Quit(); }; Check(Engine.Run(reopened) == 0 && driver.CanceledInvocations == 0, "A new session never inherits canceled requests."); }
            FailureFrame(method);
            Console.WriteLine(profile + ": worker FIFO/MPMC/inline/error/lifetime, disabled/idle scheduling, native pixels and 32 full zero-owner/producer-allocation frames passed.");
        }
        finally { Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); ProjectSettings.Set(ProjectSettings.LowProcessorMode, low); ProjectSettings.Set(ProjectSettings.LowProcessorModeSleepUsec, sleep); }
    }
    private static void FailureFrame(string method)
    {
        var failure = new ExpectedFailure("render callback failed"); var after = false; var frames = 0; using var root = new Window { Size = new(256, 128) };
        root.Ready += _ => { root.GetTexture(); Worker(() => { RenderingServer.CallOnRenderThread(() => { RenderingServer.SetDefaultClearColor(Colors.Green); RenderingServer.ForceDraw(); Check(!RenderingServer.HasChanged(), "Nested successful draw consumes current mutations before a callback error."); throw failure; }); RenderingServer.CallOnRenderThread(() => after = true); }); RenderingServer.FramePostDraw += Frame; };
        void Frame() { frames++; using var pixels = root.GetTexture().GetImage()!; Check(pixels.GetPixel(8, 8).G > .99f, "Worker callback output reaches actual pixels before error propagation."); root.Tree!.Quit(); }
        try { Engine.Run(root); throw new InvalidOperationException("Expected frame failure."); } catch (ExpectedFailure error) { Check(ReferenceEquals(error, failure) && frames == 2 && after && root.IsDisposed && !RenderingServer.IsAvailable, "Frame failure propagates after native commitment and session cleanup."); }
    }
}
