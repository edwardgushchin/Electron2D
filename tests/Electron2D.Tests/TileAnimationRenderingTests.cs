using Electron2D;
using Electron2D.Examples.AnimatedTiles;

internal static class TileAnimationRenderingTests
{
    internal static void RunHost()
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATED_TILES") ?? throw new InvalidOperationException("An authored animated scene directory is required."); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        var window = new Window { Size = new(80, 32) }; var scene = new AnimatedTileScene(System.IO.Path.Combine(directory, "animated-tiles.e2dscene")); window.AddChild(scene); var frames = 0; var reds = 0; var greens = 0; var different = false; long bytes = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames <= 40)
                {
                    using var pixels = window.GetTexture().GetImage()!; var first = pixels.GetPixel(8, 8); var red = first.R > .9f;
                    if (red) reds++; else if (first.G > .9f) greens++;
                    for (var x = 1; x < 4; x++) Check(pixels.GetPixel(x * 16 + 8, 8) == first, "Default cells share the same animation phase.");
                    for (var x = 0; x < 4; x++) if (pixels.GetPixel(x * 16 + 8, 24) != first) different = true;
                    Check(pixels.GetPixel(15, 8) == first && pixels.GetPixel(72, 8).B > .9f, "Linear tile edges stay correct and animation scope ends before static tiles.");
                    if (frames == 40) { scene.Layer.QueueRedraw(); if (Environment.GetEnvironmentVariable("ELECTRON2D_ANIMATED_TILE_SNAPSHOT") is { } path) pixels.SavePNG(path); }
                }
                if (frames == 60) bytes = GC.GetAllocatedBytesForCurrentThread();
                if (frames != 100) return;
                Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Forty continuously animating rendered frames allocate zero managed owner bytes.");
                Check(reds > 0 && greens > reds && reds + greens == 40 && different, "Unequal temporal durations and random cell phases have actual pixel consumers."); window.Tree!.Quit();
            };
        };
        Engine.Run(window); Check(window.IsDisposed && scene.IsDisposed, "Scene, padding and renderer teardown"); Console.WriteLine($"Animated tile scene passed: {backend}, {frames} frames, {reds} red/{greens} green probes, stable default/random phases, padded edges and forty warmed animated frames with zero managed bytes.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
