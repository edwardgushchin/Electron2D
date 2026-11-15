using Electron2D;

internal static class EngineObservationTests
{
    internal static void Native(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var writer = new Writer();
        MovieWriter.AddWriter(writer);
        try
        {
            foreach (var recording in new[] { false, true })
            {
                using var window = new Window { Size = new(32, 32) };
                MainLoop? attached = null;
                var before = Engine.ProcessFrames;
                window.Ready += _ =>
                {
                    attached = Engine.MainLoop;
                    Check(ReferenceEquals(attached, window.Tree), "Owned startup publishes the tree before readiness.");
                };
                void BeforeFrame(Window root, ulong index)
                {
                    Check(ReferenceEquals(Engine.MainLoop, attached), "Owned frame identity remains stable.");
                    Check(Engine.ProcessFrames == before + index - 1, "Owned process completion is visible before the next frame.");
                    if (index == 8)
                    {
                        Check(Engine.FramesPerSecond is > 0 and < 5.8,
                            "Owned FPS measures elapsed wall time, independently of the recording cadence.");
                        root.Tree!.Quit();
                    }
                    else Thread.Sleep(220);
                }
                if (recording) Engine.Run(BeforeFrame, window, "observations.frameobs", 60);
                else Engine.Run(BeforeFrame, window);
                Check(Engine.MainLoop is null && Engine.ProcessFrames == before + 7,
                    "Owned quit skips the pending frame and clears the attachment.");
            }
        }
        finally { writer.Dispose(); }
        Console.WriteLine($"Owned observations passed on {backend}: ordinary and fixed recording use measured FPS, exact counters, startup/frame identity, quit and restart.");
    }

    private sealed class Writer : MovieWriter
    {
        protected override string[] OnGetSupportedExtensions() => ["frameobs"];
        protected override bool OnHandlesFile(string path) => System.IO.Path.GetExtension(path) == ".frameobs";
        protected override void OnWriteBegin(Vector2i size, int fps, string path) { }
        protected override void OnWriteFrame(Image image, ReadOnlySpan<int> pcm) { }
        protected override void OnWriteEnd() { }
    }

    internal static void Run()
    {
        var scale = Engine.TimeScale;
        try
        {
            foreach (var value in new[] { 0d, .5d, 2d })
            {
                Engine.TimeScale = value;
                using var loop = new Loop();
                var before = Engine.ProcessFrames;
                Engine.Start(loop);
                Check(Engine.FramesPerSecond == 0, "A successful attachment resets FPS.");
                for (var i = 0; i < 4; i++) Engine.AdvanceFrame(0);
                Check(Engine.FramesPerSecond == 0, "Zero elapsed time cannot complete a window.");
                Engine.AdvanceFrame(.25);
                Engine.AdvanceFrame(.75);
                Check(Engine.FramesPerSecond == 6, "Zero-delta completed frames belong to their elapsed window.");
                Engine.AdvanceFrame(1.5);
                Near(Engine.FramesPerSecond, 1 / 1.5, "Long interval rate uses the full elapsed time.");
                Engine.AdvanceFrame(.5);
                Near(Engine.FramesPerSecond, 1 / 1.5, "Retired elapsed time cannot enter a new window.");
                Engine.AdvanceFrame(.5);
                Near(Engine.FramesPerSecond, 2, "The next window has matching frames and elapsed time.");
                Check(Engine.ProcessFrames == before + 9, "Completed process frames survive window retirement.");
                loop.Fail = true;
                try { Engine.AdvanceFrame(1); throw new Exception("Missing callback error."); }
                catch (InvalidOperationException) { }
                Check(Engine.ProcessFrames == before + 9 && Engine.FramesPerSecond == 2,
                    "Failed callbacks publish neither process completion nor an FPS sample.");
                loop.Fail = false;
                loop.Stop = true;
                Check(Engine.AdvanceFrame(1), "Process stop request reaches the host.");
                Check(Engine.ProcessFrames == before + 10 && Engine.FramesPerSecond == 1,
                    "Successful stop callbacks count once.");
                Task.Run(ReadPrepared).GetAwaiter().GetResult();
                Engine.Stop();
                Check(Engine.MainLoop is null && !loop.IsDisposed, "Manual stop detaches the borrowed loop.");
            }
        }
        finally
        {
            if (Engine.MainLoop is not null) Engine.Stop();
            Engine.TimeScale = scale;
        }
        Console.WriteLine("Engine observations passed: zero/boundary/irregular/long windows, scales, restart, failures, stop, identity, prepared off-thread getters.");
    }

    private static void ReadPrepared()
    {
        for (var i = 0; i < 256; i++) Read();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) Read();
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared observation getters allocate zero managed bytes.");
    }

    private static void Read()
    {
        _ = Engine.ProcessFrames;
        _ = Engine.PhysicsFrames;
        _ = Engine.FramesPerSecond;
        _ = Engine.PhysicsInterpolationFraction;
        _ = Engine.IsInPhysicsFrame;
        _ = Engine.MainLoop;
    }

    private static void Near(double actual, double expected, string message) => Check(Math.Abs(actual - expected) < 1e-10, message);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class Loop : MainLoop
    {
        internal bool Fail, Stop;
        protected override void OnInitialize() => Check(ReferenceEquals(Engine.MainLoop, this), "Initialization identity.");
        protected override bool OnProcess(double delta)
        {
            Check(ReferenceEquals(Engine.MainLoop, this) && !Engine.IsInPhysicsFrame, "Process identity and phase.");
            if (Fail) throw new InvalidOperationException("Observation callback failure.");
            return Stop;
        }
        protected override void OnFinalize() => Check(ReferenceEquals(Engine.MainLoop, this), "Finalization identity.");
    }
}
