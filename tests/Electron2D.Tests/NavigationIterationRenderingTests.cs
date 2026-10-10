using Electron2D;
using Electron2D.Examples.IterationRoutes;

internal static class NavigationIterationRenderingTests
{
    internal static void RunHost()
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_ITERATION_ROUTES") ?? throw new InvalidOperationException("An authored directory is required."); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        using var started = new CountdownEvent(2); using var release = new ManualResetEventSlim(); Action hold = () => { started.Signal(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new InvalidOperationException("Worker release timed out."); };
        var first = WorkerThreadPool.AddTask(hold, true); var second = WorkerThreadPool.AddTask(hold, true); Check(started.Wait(TimeSpan.FromSeconds(10)), "Workers occupied");
        var window = new Window { Size = new(64, 64) }; var scene = new IterationScene(directory); window.AddChild(scene); var frames = 0; long bytes = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
            {
                frames++; if (frames == 8) bytes = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 29) Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Twenty-one warmed queued-iteration frames allocate zero managed owner bytes.");
                if (frames == 30) { Check(!scene.Updated && scene.Route.Length == 2 && NavigationServer.MapGetIterationID(scene.Region.GetNavigationMap()) == scene.InitialIteration, "Old query topology remains usable while region/map work is pending."); release.Set(); }
                Check(frames < 120, "Completion timeout"); if (frames < 40 || !scene.Updated) return;
                Check(scene.Route.Length >= 3 && scene.Route.Any(p => p.Y <= 20 || p.Y >= 44), "Completed background topology routes around the center."); using var pixels = window.GetTexture().GetImage()!; Check(pixels.GetPixel(32, 32).R > .9f && pixels.GetPixel(8, 32).G > .8f, "New routed pixels and source wall are actually rendered.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_ITERATION_SNAPSHOT") is { } path) pixels.SavePNG(path); window.Tree!.Quit();
            };
        };
        try { Engine.Run(window); Check(window.IsDisposed && scene.IsDisposed, "Scene and worker-owned topology cleanup"); }
        finally { release.Set(); WorkerThreadPool.WaitForTaskCompletion(first); WorkerThreadPool.WaitForTaskCompletion(second); }
        Console.WriteLine($"Background navigation scene passed: {backend}, {frames} frames, retained old queries, new routed pixels and twenty-one pending frames with zero managed bytes.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
