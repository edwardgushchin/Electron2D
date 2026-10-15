using Electron2D;
using Path = System.IO.Path;
using static MovieWriterTests;

internal static class MovieProfileTests
{
    internal static void Portable()
    {
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-movie-profile-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using (var settings = new ProjectSettingsRegistry(folder))
            {
                Check(settings.Get(ProjectSettings.MovieWriterFile) == "" && settings.Get(ProjectSettings.MovieWriterFPS) == 60 && !settings.Get(ProjectSettings.MovieWriterDisableVSync), "Movie profile defaults.");
                foreach (var value in new[] { 0, -1 }) Reject<ArgumentOutOfRangeException>(() => settings.Set(ProjectSettings.MovieWriterFPS, value));
                settings.Set(ProjectSettings.MovieWriterFPS, int.MaxValue);
                settings.Set(ProjectSettings.MovieWriterFile, "base.avi");
                settings.SetFeatureOverride(ProjectSettings.MovieWriterFile, "dotnet", "profile.ogv");
                settings.SetFeatureOverride(ProjectSettings.MovieWriterFPS, "dotnet", 37);
                settings.SetFeatureOverride(ProjectSettings.MovieWriterDisableVSync, "dotnet", true); settings.Save();
            }
            using var loaded = new ProjectSettingsRegistry(folder); loaded.Load();
            Check(loaded.Get(ProjectSettings.MovieWriterFPS) == int.MaxValue && loaded.GetWithOverride(ProjectSettings.MovieWriterFPS) == 37 && loaded.GetWithOverride(ProjectSettings.MovieWriterFile) == "profile.ogv" && loaded.GetWithOverride(ProjectSettings.MovieWriterDisableVSync), "Profile persistence and feature overrides.");
            using var window = new Window();
            Reject<ArgumentException>(() => Engine.RunMovie(window)); Reject<ArgumentException>(() => Engine.RunMovieAsync(window));
            Check(!window.IsDisposed && Engine.GetWriteMoviePath() == "", "Blank profile fails before reservation.");
            ProjectSettings.Set(ProjectSettings.MovieWriterFile, "profile.png");
            try { Reject<InvalidOperationException>(() => Engine.RunMovieAsync(window)); Check(!window.IsDisposed, "Missing async context retains root ownership."); }
            finally { ProjectSettings.Set(ProjectSettings.MovieWriterFile, ""); }
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine("Movie profile portable: defaults, persistence, feature overrides and validation ownership passed.");
    }

    internal static void Native(string backend, string folder)
    {
        Directory.CreateDirectory(folder); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var source = Source();
        try
        {
            foreach (var extension in new[] { "png", "avi", "ogv" })
            {
                var path = Path.Combine(folder, "profile." + extension); Configure(path, 37, true);
                var window = Scene(source); window.Size = new(66, 50); var stop = new Frames(37); window.AddChild(stop);
                window.Ready += _ =>
                {
                    var startupMode = DisplayServer.WindowGetVSyncMode();
                    DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled);
                    var supportedMode = DisplayServer.WindowGetVSyncMode();
                    Check(Engine.GetWriteMoviePath() == path && startupMode == supportedMode, $"Profile path and supported native presentation apply before Ready: {Engine.GetWriteMoviePath()}, {startupMode}, expected {supportedMode}.");
                    File.WriteAllText(path + ".presentation.txt", startupMode.ToString());
                    Configure(Path.Combine(folder, "must-not-create." + extension), 11, false);
                };
                Check(Engine.RunMovie(window) == 23 && window.IsDisposed && stop.Count == 4, "Profile recording owns and finishes the scene.");
                File.WriteAllText(path + ".expect.json", "{\"frames\":3,\"rate\":48000,\"channels\":2,\"fps\":37,\"width\":66,\"height\":50,\"keyframes\":64}");
            }
            Configure(Path.Combine(folder, "ignored.ogv"), 11, true);
            var explicitPath = Path.Combine(folder, "explicit.png"); var explicitWindow = Scene(source); explicitWindow.AddChild(new Frames(59));
            explicitWindow.Ready += _ => Check(Engine.GetWriteMoviePath() == explicitPath, "Explicit path precedes profile override.");
            Engine.Run(explicitWindow, explicitPath, 59);
            File.WriteAllText(explicitPath + ".expect.json", "{\"frames\":3,\"rate\":48000,\"channels\":2,\"fps\":59,\"width\":64,\"height\":48}");
            var defaultPath = Path.Combine(folder, "explicit-default.png"); var defaults = Scene(source); defaults.AddChild(new Frames(60)); Engine.Run(defaults, defaultPath);
            File.WriteAllText(defaultPath + ".expect.json", "{\"frames\":3,\"rate\":48000,\"channels\":2,\"fps\":60,\"width\":64,\"height\":48}");
            var ordinary = new Window { Size = new(64, 48) };
            ordinary.Ready += _ =>
            {
                Check(Engine.GetWriteMoviePath() == "", "Configured output does not enable ordinary recording.");
                Check(DisplayServer.WindowGetVSyncMode() == (backend == "gpu" ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled), "Movie presentation request does not persist into an ordinary run."); ordinary.Tree!.Quit();
            };
            Engine.Run(ordinary);
            var asyncPath = Path.Combine(folder, "async.avi"); Configure(asyncPath, 43, false);
            var asyncWindow = Scene(source); asyncWindow.AddChild(new Frames(43));
            asyncWindow.Ready += _ => Check(DisplayServer.WindowGetVSyncMode() == (backend == "gpu" ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled), "False movie policy retains backend presentation default.");
            var prior = SynchronizationContext.Current; var context = new PumpContext(); SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                var pending = Engine.RunMovieAsync(asyncWindow); var timer = System.Diagnostics.Stopwatch.StartNew();
                while (!pending.IsCompleted) { if (timer.Elapsed.TotalSeconds > 15) throw new TimeoutException("Configured async movie did not finish."); context.Pump(); }
                Check(pending.GetAwaiter().GetResult() == 23 && asyncWindow.IsDisposed, "Async project movie finishes on its application owner.");
            }
            finally { SynchronizationContext.SetSynchronizationContext(prior); }
            File.WriteAllText(asyncPath + ".expect.json", "{\"frames\":3,\"rate\":48000,\"channels\":2,\"fps\":43,\"width\":64,\"height\":48}");
            Configure(Path.Combine(folder, "absent/fail.png"), 37, true);
            var failed = Scene(source); Reject<DirectoryNotFoundException>(() => Engine.RunMovie(failed)); Check(failed.IsDisposed && Engine.GetWriteMoviePath() == "", "Profile Begin failure cleans reserved state.");
            Configure(Path.Combine(folder, "ready-failure.png"), 37, true);
            var readyFailure = Scene(source, failReady: true); Reject<AggregateException>(() => Engine.RunMovie(readyFailure)); Check(readyFailure.IsDisposed && Engine.GetWriteMoviePath() == "", "Profile Ready failure cleans recording and native state.");
            Configure(Path.Combine(folder, "restart.ogv"), 37, true);
            var restart = new Window { Size = new(66, 50) }; restart.Ready += _ => restart.Tree!.Quit(); Engine.RunMovie(restart);
            Check(!File.Exists(Path.Combine(folder, "ignored.ogv")) && !Directory.EnumerateFiles(folder, "must-not-create*").Any(), "Sampled and explicit paths stay authoritative.");
        }
        finally
        {
            ProjectSettings.ClearFeatureOverride(ProjectSettings.MovieWriterFile, "dotnet"); ProjectSettings.ClearFeatureOverride(ProjectSettings.MovieWriterFPS, "dotnet"); ProjectSettings.ClearFeatureOverride(ProjectSettings.MovieWriterDisableVSync, "dotnet");
            ProjectSettings.Set(ProjectSettings.MovieWriterFile, ""); ProjectSettings.Set(ProjectSettings.MovieWriterFPS, 60); ProjectSettings.Set(ProjectSettings.MovieWriterDisableVSync, false);
        }
        Console.WriteLine("Movie project profile: all codecs, feature overrides, snapshots, explicit precedence, VSync, ordinary/async lifecycle and failure cleanup passed.");
    }
    internal static void WriteProject(string folder, string backend)
    {
        Directory.CreateDirectory(folder); using var settings = new ProjectSettingsRegistry(folder);
        settings.Set(ProjectSettings.MovieWriterFile, "unused.avi"); settings.SetFeatureOverride(ProjectSettings.MovieWriterFile, "dotnet", "fresh.ogv");
        settings.SetFeatureOverride(ProjectSettings.MovieWriterFPS, "dotnet", 31); settings.SetFeatureOverride(ProjectSettings.MovieWriterDisableVSync, "dotnet", true);
        settings.Set(ProjectSettings.RenderingMethod, backend); settings.Set(ProjectSettings.RenderingFallback, false); settings.Save();
    }
    internal static void ReadProject()
    {
        ProjectSettings.Load(); using var source = Source(); var window = Scene(source); window.Size = new(66, 50); window.AddChild(new Frames(31));
        window.Ready += _ => Check(Engine.GetWriteMoviePath() == "fresh.ogv", "Fresh project profile restores the requested path.");
        Check(Engine.RunMovie(window) == 23 && window.IsDisposed, "Fresh project recording completes.");
        File.WriteAllText("fresh.ogv.expect.json", "{\"frames\":3,\"rate\":48000,\"channels\":2,\"fps\":31,\"width\":66,\"height\":50,\"keyframes\":64}");
    }
    private static void Configure(string path, int fps, bool disableVSync)
    {
        ProjectSettings.Set(ProjectSettings.MovieWriterFile, "unused.movie"); ProjectSettings.Set(ProjectSettings.MovieWriterFPS, 13); ProjectSettings.Set(ProjectSettings.MovieWriterDisableVSync, !disableVSync);
        ProjectSettings.SetFeatureOverride(ProjectSettings.MovieWriterFile, "dotnet", path); ProjectSettings.SetFeatureOverride(ProjectSettings.MovieWriterFPS, "dotnet", fps); ProjectSettings.SetFeatureOverride(ProjectSettings.MovieWriterDisableVSync, "dotnet", disableVSync);
    }
    private sealed class Frames(int fps) : Entity
    {
        internal int Count;
        protected override void OnReady() => Draw += item => item.DrawRect(new(32, 0, 34, 50), (Count & 1) == 0 ? Colors.Green : Colors.Blue);
        protected override void OnProcess(double delta) { Check(Math.Abs(delta - 1d / fps) < 1e-12, "Profile cadence is sampled before scene callbacks."); QueueRedraw(); if (++Count == 4) Tree!.Quit(23); }
    }
}
