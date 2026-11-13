using System.Diagnostics;
using Electron2D;
using SDL3;

internal static class FrameRateTests
{
    private sealed class Loop : MainLoop { }

    private sealed class Receiver : Logger
    {
        internal readonly List<string> Samples = [];
        protected override void OnLogMessage(string message, bool error)
        {
            if (!error && message.Contains(" FPS: ", StringComparison.Ordinal))
                Samples.Add(message);
        }
    }

    internal static void Managed(bool fromCommandLine)
    {
        var directory = Directory.CreateTempSubdirectory("electron2d-frame-rate-");
        try
        {
            var user = directory.CreateSubdirectory("user").FullName;
            using (var saved = new ProjectSettingsRegistry(directory.FullName, user))
            {
                Check(saved.Get(ProjectSettings.MaxFPS) == 0 && !saved.Get(ProjectSettings.PrintFPS), "Settings default to unlimited and silent.");
                try { saved.Set(ProjectSettings.MaxFPS, -1); throw new InvalidOperationException("Accepted a negative limit."); }
                catch (ArgumentOutOfRangeException) { }
                Check(saved.Get(ProjectSettings.FrameDelayMsec) == 0, "Frame delay defaults to zero.");
                try { saved.Set(ProjectSettings.FrameDelayMsec, -1); throw new InvalidOperationException("Accepted a negative delay."); }
                catch (ArgumentOutOfRangeException) { }
                saved.Set(ProjectSettings.FrameDelayMsec, 63);
                saved.Set(ProjectSettings.MaxFPS, 35);
                saved.Set(ProjectSettings.PrintFPS, true);
                saved.Save();
            }
            using var loaded = new ProjectSettingsRegistry(directory.FullName, user);
            loaded.Load();
            Check(loaded.Get(ProjectSettings.FrameDelayMsec) == 63, "Frame delay round-trips.");
            Check(loaded.Get(ProjectSettings.MaxFPS) == 35 && loaded.Get(ProjectSettings.PrintFPS), "Frame settings round-trip.");
        }
        finally { directory.Delete(recursive: true); }

        using var receiver = new Receiver();
        var output = Engine.PrintToSTDOUT;
        var scale = Engine.TimeScale;
        var editor = Engine.IsEditorHint;
        OS.AddLogger(receiver);
        try
        {
            Engine.PrintToSTDOUT = false;
            Engine.TimeScale = 0;
            RunSamples(fromCommandLine);
            ProjectSettings.SetFeatureOverride(ProjectSettings.PrintFPS, "frame_rate_test", true);
            ProjectSettings.AddCustomFeature("frame_rate_test");
            RunSamples(true);
            Engine.IsEditorHint = true;
            RunSamples(fromCommandLine);
            Engine.IsEditorHint = false;
            ProjectSettings.ClearFeatureOverride(ProjectSettings.PrintFPS, "frame_rate_test");
            RunSamples(fromCommandLine);
        }
        finally
        {
            ProjectSettings.ClearFeatureOverride(ProjectSettings.PrintFPS, "frame_rate_test");
            ProjectSettings.RemoveCustomFeature("frame_rate_test");
            Engine.IsEditorHint = editor;
            Engine.TimeScale = scale;
            Engine.PrintToSTDOUT = output;
            OS.RemoveLogger(receiver);
        }
        Console.WriteLine("Frame-rate settings persistence, reporting cadence, unscaled timing and lifecycle passed.");

        void RunSamples(bool expected)
        {
            receiver.Samples.Clear();
            using var loop = new Loop();
            Engine.Start(loop);
            try
            {
                for (var frame = 0; frame < 15; frame++)
                    Engine.AdvanceFrame(.25);
                Check(receiver.Samples.Count == 0, "The three warmup windows do not emit and the fourth is incomplete.");
                Engine.AdvanceFrame(.25);
                Check(Engine.FramesPerSecond == 4, "FPS uses unscaled completed process frames.");
                var role = Engine.IsEditorHint ? "Editor" : "Project";
                Check(receiver.Samples.SequenceEqual(expected ? [$"{role} FPS: 4 (250.00 mspf)"] : []), "The first complete report reaches loggers with stdout muted.");
                for (var frame = 0; frame < 4; frame++)
                    Engine.AdvanceFrame(.25);
                Check(receiver.Samples.Count == (expected ? 2 : 0), "Exactly one report per subsequent window.");
            }
            finally { Engine.Stop(); }
        }
    }

    internal static void Windowed(string backend, int? commandLineLimit)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.MaxFPS, 40);
        ProjectSettings.SetFeatureOverride(ProjectSettings.MaxFPS, "frame_rate_test", 12);
        ProjectSettings.AddCustomFeature("frame_rate_test");
        try
        {
            Run(commandLineLimit ?? 12, verifyWait: commandLineLimit is null);
            Check(Engine.MaxFPS == 0, "A sampled window cap retires with its run.");
            Engine.MaxFPS = 60;
            Run(60, verifyWait: false);
        }
        finally
        {
            ProjectSettings.ClearFeatureOverride(ProjectSettings.MaxFPS, "frame_rate_test");
            ProjectSettings.RemoveCustomFeature("frame_rate_test");
            ProjectSettings.Reset(ProjectSettings.MaxFPS);
            Engine.MaxFPS = 0;
        }
        Console.WriteLine($"Window FPS cap from feature settings and overrides passed on {backend}.");

        void Run(int expected, bool verifyWait)
        {
            using var window = new Window { Size = new(48, 40) };
            using var actor = new Entity();
            actor.Draw += item => item.DrawRect(new(0, 0, 24, 24), Colors.Red);
            window.AddChild(actor);
            using var target = window.GetTexture();
            var start = 0L;
            window.Ready += _ => Check(Engine.MaxFPS == expected, "Effective startup cap before Ready.");
            Engine.Run((root, frame) =>
            {
                if (frame == 1)
                {
                    start = Stopwatch.GetTimestamp();
                    ProjectSettings.Set(ProjectSettings.MaxFPS, 99);
                    Check(Engine.MaxFPS == expected, "Stored edits do not replace the running cap.");
                }
                if (frame == 6)
                {
                    if (verifyWait) Check(Stopwatch.GetElapsedTime(start).TotalSeconds >= .35, "Five intervals honor the 12 FPS cap.");
                    using var pixels = target.GetImage()!;
                    Check(pixels.GetPixel(8, 8).R > .95f, "The configured host rendered real frames.");
                    Engine.MaxFPS = 0;
                    Check(Engine.MaxFPS == 0, "Live assignment can remove the cap.");
                    root.Tree!.Quit();
                }
            }, window);
        }
    }

    internal static void WindowDelay(string backend, int? commandLineDelay)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.FrameDelayMsec, 45);
        ProjectSettings.SetFeatureOverride(ProjectSettings.FrameDelayMsec, "frame_rate_test", 80);
        ProjectSettings.AddCustomFeature("frame_rate_test");
        try
        {
            Run(commandLineDelay is > 0 ? commandLineDelay.Value : 80, asynchronous: false);
            Run(commandLineDelay is > 0 ? commandLineDelay.Value : 45, asynchronous: true);
            ProjectSettings.Set(ProjectSettings.FrameDelayMsec, 2000);
            Engine.IsEditorHint = true;
            Run(commandLineDelay ?? 0, asynchronous: false);
            Engine.IsEditorHint = false;

            if (commandLineDelay is not > 0) VerifyCloseInterruptsDelay();
        }
        finally
        {
            Engine.IsEditorHint = false;
            ProjectSettings.RemoveCustomFeature("frame_rate_test");
            ProjectSettings.ClearFeatureOverride(ProjectSettings.FrameDelayMsec, "frame_rate_test");
            ProjectSettings.Reset(ProjectSettings.FrameDelayMsec);
        }
        Console.WriteLine($"Window fixed delay, startup sampling and event responsiveness passed on {backend}.");

        static void VerifyCloseInterruptsDelay()
        {
            ProjectSettings.Set(ProjectSettings.FrameDelayMsec, int.MaxValue);
            using var window = new Window { Size = new(48, 40) };
            var start = 0L;
            window.CloseRequested += () => Check(Stopwatch.GetElapsedTime(start).TotalSeconds < 3, "Close interrupts the fixed delay.");
            Engine.Run((root, frame) =>
            {
                Check(frame == 1, "The queued close prevents a second frame.");
                var windows = SDL.GetWindows(out _);
                var close = new SDL.Event
                {
                    Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = SDL.GetWindowID(windows![0]) }
                };
                start = Stopwatch.GetTimestamp();
                Check(SDL.PushEvent(ref close), "The native close event was queued.");
            }, window);
            Check(start != 0 && window.IsDisposed, "The close request completed teardown.");
        }
        void Run(int expected, bool asynchronous)
        {
            using var window = new Window { Size = new(48, 40) };
            var start = 0L;
            Action<Window, ulong> advance = (root, frame) =>
            {
                if (frame == 1)
                {
                    ProjectSettings.ClearFeatureOverride(ProjectSettings.FrameDelayMsec, "frame_rate_test");
                    start = Stopwatch.GetTimestamp();
                }
                if (frame != 4) return;
                var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                if (expected > 0)
                    Check(elapsed >= 3d * expected - 2, $"Three intervals honor the sampled fixed delay ({elapsed:0.0} ms).");
                if (Engine.IsEditorHint && expected < 500)
                    Check(elapsed < 3000, "The editor ignores the stored frame delay.");
                root.Tree!.Quit();
            };
            if (!asynchronous)
            {
                Engine.Run(advance, window);
                return;
            }

            var previous = SynchronizationContext.Current;
            var context = new MovieWriterTests.PumpContext();
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                var pending = Engine.RunAsync(advance, window);
                Check(!pending.IsCompleted, "The async host yields on the owner context.");
                var deadline = Stopwatch.StartNew();
                while (!pending.IsCompleted)
                {
                    if (deadline.Elapsed.TotalSeconds > 10) throw new TimeoutException("Async frame delay did not complete.");
                    context.Pump();
                }
                Check(pending.GetAwaiter().GetResult() == 0, "Async pacing reaches a normal quit.");
            }
            finally { SynchronizationContext.SetSynchronizationContext(previous); }
        }
    }

    internal static void InvalidLimit(string option)
    {
        using var window = new Window();
        var ready = false;
        window.Ready += _ => ready = true;
        try
        {
            Engine.Run((root, _) => root.Tree!.Quit(), window);
            throw new InvalidOperationException("Expected a malformed frame pacing argument to reject startup.");
        }
        catch (ArgumentException error)
        {
            Check(error.Message.StartsWith(option + " ", StringComparison.Ordinal), "The failing switch is reported.");
            Check(!ready && window.IsDisposed && !DisplayServer.IsAvailable && Engine.MainLoop is null &&
                Engine.MaxFPS == 0, "Failed pacing validation releases the root and startup reservation.");
        }
        using var loop = new Loop();
        Engine.Start(loop);
        Engine.Stop();
        Console.WriteLine("Invalid window pacing argument rejected with complete startup cleanup.");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
