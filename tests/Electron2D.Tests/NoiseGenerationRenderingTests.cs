using Electron2D;
using Electron2D.Examples.ProceduralNoise;

internal static class NoiseGenerationRenderingTests
{
    internal static void RunHost()
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_NOISE_ASSETS") ?? throw new InvalidOperationException("An authored directory is required."); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        var initial = File.ReadAllBytes(System.IO.Path.Combine(directory, "initial.rgba")); var updated = File.ReadAllBytes(System.IO.Path.Combine(directory, "updated.rgba"));
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        using var started = new CountdownEvent(2); using var release = new ManualResetEventSlim(); Action hold = () => { started.Signal(); if (!release.Wait(TimeSpan.FromSeconds(15))) throw new InvalidOperationException("Worker release timed out."); };
        var first = WorkerThreadPool.AddTask(hold, true); var second = WorkerThreadPool.AddTask(hold, true); Check(started.Wait(TimeSpan.FromSeconds(10)), "Both workers occupied");
        var window = new Window { Size = new(64, 32), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var scene = new NoiseScene(directory); window.AddChild(scene); var frames = 0; long bytes = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
            {
                frames++; Check(frames < 180, "Generation completion timeout");
                if (frames == 6)
                {
                    Check(scene.InitiallyEmpty && scene.Publications == 1, "First pixels are published through owner deferred work despite occupied workers"); using var pixels = window.GetTexture().GetImage()!; Pixel(pixels, 8, initial); Pixel(pixels, 40, initial);
                }
                if (frames == 8) { scene.Stage(); Check(scene.Publications == 1, "Source invalidation does not emit pixel completion"); }
                if (frames == 10) bytes = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 35) Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Twenty-five warmed pending frames allocate zero managed owner bytes");
                if (frames == 36)
                {
                    Check(scene.Publications == 1, "Queued worker preserves first image and signal"); using var image = scene.First.GetImage()!; Check(image.GetData().SequenceEqual(initial), "Reads keep the complete original image including mipmaps"); using var pixels = window.GetTexture().GetImage()!; Pixel(pixels, 8, initial); Pixel(pixels, 40, initial);
                }
                if (frames == 40) release.Set();
                if (frames < 48 || scene.Publications < 2) return;
                Check(scene.Publications == 2, "One changed input publishes once"); using var changed = scene.First.GetImage()!; using var independent = scene.Second.GetImage()!;
                Check(changed.GetData().SequenceEqual(updated) && independent.GetData().SequenceEqual(initial), "Background output agrees with separate-process authoring and independent local source");
                using var output = window.GetTexture().GetImage()!; Pixel(output, 8, updated); Pixel(output, 40, initial); if (Environment.GetEnvironmentVariable("ELECTRON2D_NOISE_SNAPSHOT") is { } path) output.SavePNG(path); window.Tree!.Quit();
            };
        };
        try { Engine.Run(window); Check(scene.IsDisposed && scene.First.IsDisposed && scene.Second.IsDisposed, "Scene-local texture cleanup"); }
        finally { release.Set(); WorkerThreadPool.WaitForTaskCompletion(first); WorkerThreadPool.WaitForTaskCompletion(second); }
        Console.WriteLine($"Procedural noise scene passed: {backend}, {frames} frames, deferred first pixels, queued old/new image pixels, independent source graphs and twenty-five pending frame intervals with zero managed bytes.");
    }
    private static void Pixel(Image image, int x, byte[] expected) { var offset = (8 * 32 + 8) * 4; var color = image.GetPixel(x, 8); Check(Math.Abs(color.R - expected[offset] / 255f) <= 2f / 255 && Math.Abs(color.G - expected[offset + 1] / 255f) <= 2f / 255 && Math.Abs(color.B - expected[offset + 2] / 255f) <= 2f / 255, "Rendered source pixel agrees with generated image"); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
