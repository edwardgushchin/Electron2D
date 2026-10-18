using System.Diagnostics;
using Electron2D;
using Monitor = Electron2D.Performance.Monitor;

internal static class FrameMonitorContract
{
    internal static void Run()
    {
        Check((int)Monitor.TimeFPS == 0 && (int)Monitor.TimeProcess == 1 &&
            (int)Monitor.TimePhysicsProcess == 2 && (int)Monitor.TimeNavigationProcess == 3, "Timing identities.");
        var ticks = Engine.PhysicsTicksPerSecond;
        var jitter = Engine.PhysicsJitterFix;
        var scale = Engine.TimeScale;
        var print = Engine.PrintErrorMessages;
        var display = DisplayServer.IsAvailable;
        try
        {
            Engine.PhysicsTicksPerSecond = 10; Engine.PhysicsJitterFix = 0; Engine.TimeScale = 1;
            Engine.PrintErrorMessages = false;
            using var loop = new TimedLoop { Work = true };
            Engine.Start(loop);
            Check(Values() == default, "Successful attachment resets every time monitor.");
            for (var i = 0; i < 3; i++) Engine.AdvanceFrame(.25);
            Check(Values() == default, "Unpublished partial window.");
            Engine.AdvanceFrame(.25);
            var published = Values();
            Check(published.FPS == 4 && published.FPS == Engine.FramesPerSecond &&
                published.Process >= .004 && published.Physics >= .005 && published.Navigation >= .003 &&
                loop.Deliveries > 0, "Actual callback and navigation work produces seconds, independent of simulated delta.");
            loop.Work = false;
            Engine.AdvanceFrame(.25);
            Check(Values() == published, "Reads and partial windows retain their completed sample.");
            Engine.Stop();
            Check(Values() == published, "Stop retains completed diagnostics.");
            using (var direct = new TimedLoop())
            {
                direct.Initialize(); direct.PhysicsProcess(.1); direct.Process(2); direct.FinalizeLoop();
            }
            NavigationServer.Step(.1);
            Check(Values() == published, "Independent hosts do not impersonate Engine frames.");
            using (var bad = new BadInitialization()) Reject<InvalidOperationException>(() => Engine.Start(bad));
            Check(Values() == published, "Rejected initialization preserves the prior samples.");

            using var next = new TimedLoop();
            Engine.Start(next);
            Check(Values() == default, "A subsequent successful run starts empty.");
            next.FailProcess = true;
            Reject<InvalidOperationException>(() => Engine.AdvanceFrame(3));
            Check(Values() == default, "Failed frame does not publish or advance its sampling window.");
            next.FailProcess = false; next.FailNavigation = true;
            Reject<AggregateException>(() => Engine.AdvanceFrame(.5));
            Check(Values() == default, "Failed navigation frame remains unpublished.");
            next.FailNavigation = false;
            Reject<ArgumentOutOfRangeException>(() => Engine.AdvanceFrame(double.NaN));
            Engine.TimeScale = 0;
            Engine.AdvanceFrame(.5);
            Check(Values().Process == 0, "Failed intervals and invalid calls did not consume elapsed time.");
            Engine.AdvanceFrame(.5);
            Check(Values().FPS == Engine.FramesPerSecond && Values().Process > 0 && Values().Physics > 0,
                "Zero simulation scale retains actual wall-clock diagnostics.");
            Engine.TimeScale = 1;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            for (var i = 0; i < 128; i++) { Engine.AdvanceFrame(.1); _ = Values(); }
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 256; i++) { Engine.AdvanceFrame(.1); _ = Values(); }
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner;
            var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerBytes == 0 && allBytes == 0, $"Prepared timing/navigation frames: {ownerBytes}/{allBytes} owner/all-thread bytes.");
            // Keep unrelated ThreadPool startup/completion outside the all-thread allocation interval.
            var finalPublished = Values();
            Check(Task.Run(Values).GetAwaiter().GetResult() == finalPublished, "Cross-thread published reads.");
            Reject<InvalidOperationException>(() => Task.Run(() => Engine.AdvanceFrame(0)).GetAwaiter().GetResult());
            Engine.Stop();
            Check(DisplayServer.IsAvailable == display, "Timing never opens a display.");
            Console.WriteLine("Public frame-monitor lifecycle, navigation, failures, cross-thread reads and 256 active frames passed: 0/0 owner/all-thread managed bytes.");
        }
        finally
        {
            if (Engine.MainLoop is not null) Engine.Stop();
            Engine.PhysicsTicksPerSecond = ticks; Engine.PhysicsJitterFix = jitter;
            Engine.TimeScale = scale; Engine.PrintErrorMessages = print;
        }
    }

    internal static (double FPS, double Process, double Physics, double Navigation) Values() =>
        (Performance.GetMonitor(Monitor.TimeFPS), Performance.GetMonitor(Monitor.TimeProcess),
            Performance.GetMonitor(Monitor.TimePhysicsProcess), Performance.GetMonitor(Monitor.TimeNavigationProcess));

    internal static void Burn(double seconds)
    {
        var start = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(start).TotalSeconds < seconds) Thread.SpinWait(16);
    }

    private sealed class TimedLoop : MainLoop
    {
        private readonly RID _map = NavigationServer.MapCreate();
        private readonly RID _agent = NavigationServer.AgentCreate();
        internal bool Work, FailProcess, FailNavigation;
        internal int Deliveries;

        internal TimedLoop()
        {
            NavigationServer.MapSetUseAsyncIterations(_map, false);
            NavigationServer.MapSetActive(_map, true);
            NavigationServer.AgentSetMap(_agent, _map);
            NavigationServer.AgentSetAvoidanceEnabled(_agent, true);
            NavigationServer.AgentSetAvoidanceCallback(_agent, Avoided);
        }

        private void Avoided(Vector2 velocity)
        {
            Deliveries++;
            if (FailNavigation) throw new InvalidOperationException("Navigation timing failure.");
            if (Work) Burn(.003);
        }

        protected override bool OnPhysicsProcess(double delta)
        {
            NavigationServer.Step(delta);
            if (Work) Burn(.002);
            return false;
        }

        protected override bool OnProcess(double delta)
        {
            if (FailProcess) throw new InvalidOperationException("Process timing failure.");
            if (Work) Burn(.004);
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            try { if (disposing) { NavigationServer.FreeRID(_agent); NavigationServer.FreeRID(_map); } }
            finally { base.Dispose(disposing); }
        }
    }

    private sealed class BadInitialization : MainLoop
    {
        protected override void OnInitialize() => throw new InvalidOperationException("Initialization timing failure.");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
