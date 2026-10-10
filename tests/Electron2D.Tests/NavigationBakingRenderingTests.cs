using Electron2D;
using Electron2D.Examples.BakedRoutes;

internal static class NavigationBakingRenderingTests
{
    internal static void RunHost()
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_BAKED_ROUTES") ?? throw new InvalidOperationException("An authored room directory is required.");
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        using var started = new CountdownEvent(2); using var release = new ManualResetEventSlim();
        Action hold = () => { started.Signal(); if (!release.Wait(TimeSpan.FromSeconds(10))) throw new InvalidOperationException("Baking worker release timed out."); };
        var first = WorkerThreadPool.AddTask(hold, highPriority: true); var second = WorkerThreadPool.AddTask(hold, highPriority: true); Check(started.Wait(TimeSpan.FromSeconds(10)), "Both worker lanes are occupied.");
        var window = new Window { Size = new(64, 64) }; var scene = new RouteScene(System.IO.Path.Combine(directory, "room.e2dscene")); window.AddChild(scene); var frames = 0; long activeStart = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames == 8) activeStart = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 29) Check(GC.GetAllocatedBytesForCurrentThread() == activeStart, "Twenty-one active queued-bake frames after eight warm frames allocate zero managed owner bytes.");
                if (frames == 30) { Check(scene.Completions == 0 && scene.Region.IsBaking(), "Rendering advances while the navigation bake is queued."); release.Set(); }
                Check(frames < 120, "Owner completion and committed map become available.");
                if (frames < 40 || scene.Route.Length == 0) return;
                Check(scene.Completions == 1 && !scene.Region.IsBaking() && scene.Route.Length >= 3 && scene.Route.Any(p => p.Y < 20 || p.Y > 44), "Authored collision data produces an actual obstacle-avoiding map route.");
                using var pixels = window.GetTexture().GetImage()!; Check(pixels.GetPixel(32, 32).R > .9f && pixels.GetPixel(10, 32).G > .8f, "Source wall and baked route produce actual retained pixels.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_BAKED_SNAPSHOT") is { } capture) pixels.SavePNG(capture);
                window.Tree!.Quit();
            };
        };
        try { Engine.Run(window); Check(window.IsDisposed && scene.IsDisposed, "Scene/native lifetime cleanup."); }
        finally { release.Set(); WorkerThreadPool.WaitForTaskCompletion(first); WorkerThreadPool.WaitForTaskCompletion(second); }
        Console.WriteLine($"Navigation bake scene passed: {backend}, {frames} rendered frames, queued workers, owner completion, authored geometry and routed pixels.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
