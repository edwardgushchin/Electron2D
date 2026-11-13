using System.Diagnostics;
using Electron2D;

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
                saved.Set(ProjectSettings.MaxFPS, 35);
                saved.Set(ProjectSettings.PrintFPS, true);
                saved.Save();
            }
            using var loaded = new ProjectSettingsRegistry(directory.FullName, user);
            loaded.Load();
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

    internal static void InvalidLimit()
    {
        using var window = new Window();
        var ready = false;
        window.Ready += _ => ready = true;
        try
        {
            Engine.Run((root, _) => root.Tree!.Quit(), window);
            throw new InvalidOperationException("Expected a malformed frame-cap argument to reject startup.");
        }
        catch (ArgumentException)
        {
            Check(!ready && window.IsDisposed && !DisplayServer.IsAvailable && Engine.MainLoop is null &&
                Engine.MaxFPS == 0, "Failed cap validation releases the root and startup reservation.");
        }
        using var loop = new Loop();
        Engine.Start(loop);
        Engine.Stop();
        Console.WriteLine("Invalid window FPS cap rejected with complete startup cleanup.");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
