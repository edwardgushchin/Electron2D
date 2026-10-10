using Electron2D;
using Electron2D.Examples.TileRoutes;

internal static class TileNavigationRenderingTests
{
    internal static void RunHost()
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_TILE_ROUTES") ?? throw new InvalidOperationException("An authored tile scene directory is required.");
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        var window = new Window { Size = new(64, 64) }; var scene = new TileRouteScene(System.IO.Path.Combine(directory, "tiles.e2dscene")); window.AddChild(scene); var frames = 0; long bytes = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames is 12 or 16 or 20)
                {
                    using var pixels = window.GetTexture().GetImage()!; var green = pixels.GetPixel(4, 24).G;
                    Check(frames == 16 ? green < .01f : green > .3f, "ForceShow, ForceHide and Default tree hint have actual pixel consumers.");
                    if (frames == 12) scene.Layer.NavigationVisibilityMode = TileMapLayer.DebugVisibilityMode.ForceHide;
                    if (frames == 16) { scene.Layer.NavigationVisibilityMode = TileMapLayer.DebugVisibilityMode.Default; window.Tree!.DebugNavigationHint = true; }
                }
                if (frames == 24) bytes = GC.GetAllocatedBytesForCurrentThread();
                if (frames != 40) return;
                Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Sixteen unchanged rendered frames allocate zero managed owner bytes.");
                Check(scene.Route.Length >= 3 && scene.Route.First() == new Vector2(8, 8) && scene.Route.Last() == new Vector2(56, 56) && scene.Route.Any(p => p.X <= 16 || p.X >= 48 || p.Y <= 16 || p.Y >= 48), "Loaded tile map produces an obstacle-avoiding route.");
                using var image = window.GetTexture().GetImage()!; Check(image.GetPixel(32, 32).G < .01f && image.GetPixel(8, 8).B > .8f, "Void stays dark and route produces actual pixels.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_TILE_SNAPSHOT") is { } path) image.SavePNG(path); window.Tree!.Quit();
            };
        };
        Engine.Run(window); Check(window.IsDisposed && scene.IsDisposed, "Native scene cleanup"); Console.WriteLine($"Tile navigation scene passed: {backend}, {frames} frames, fresh-process scene, route and debug pixels, sixteen unchanged frames with zero managed bytes.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
