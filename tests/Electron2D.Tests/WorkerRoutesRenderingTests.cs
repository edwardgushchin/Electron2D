using Electron2D;
using Electron2D.Examples.WorkerRoutes;

internal static class WorkerRoutesRenderingTests
{
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.WorkerPoolLowPriorityThreadRatio, 1);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        var window = new Window { Size = new(64, 64) }; var planner = new RoutePlanner(); window.AddChild(planner); var frames = 0; long allocationStart = 0, allocationEnd = 0;
        Action postDraw = () =>
        {
            frames++;
            if (frames == 24) allocationStart = GC.GetAllocatedBytesForCurrentThread();
            if (frames != 88) return;
            allocationEnd = GC.GetAllocatedBytesForCurrentThread();
            Check(planner.JobBatches == frames && planner.PublishedCount == frames * 4 && planner.OwnerThreadID == Environment.CurrentManagedThreadId, "Every live group publishes all four positions on scene owner.");
            Check(allocationEnd == allocationStart, "64 active rendered worker frames allocate zero managed bytes on owner after 24 warm frames.");
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && RenderingServer.GetCurrentRenderingDriverName() != "software", "Requested hardware renderer.");
            using var pixels = window.GetTexture().GetImage()!;
            Check(pixels.GetPixel(54, 12).G > .9f && pixels.GetPixel(54, 12).B > .9f && pixels.GetPixel(54, 20).R > .9f && pixels.GetPixel(54, 20).G > .9f && pixels.GetPixel(54, 36).R > .9f, "Worker-computed routes reach actual endpoint pixels.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_WORKER_SNAPSHOT") is { } path) pixels.SavePNG(path);
            window.Tree!.Quit();
        };
        window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += postDraw; };
        Engine.Run(window); Check(window.IsDisposed && planner.IsDisposed && !RenderingServer.IsAvailable, "Scene/job/native cleanup.");
        Console.WriteLine($"Worker route rendering passed: {backend}; 88 groups, 352 deferred publications; 64 active frames after 24 warm frames, {allocationEnd - allocationStart} owner managed bytes.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
