using System.Diagnostics;
using Electron2D;

internal static partial class TileMapLayerTests
{
    internal static void RunNative()
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var previousFPS = Engine.MaxFPS; Engine.MaxFPS = 60;
        try
        {
            foreach (var renderer in new[] { "gpu", "compatibility" })
                foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) Native(renderer, backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); Engine.MaxFPS = previousFPS; }
    }
    private static void Native(string renderer, PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
        using var assets = new Assets();
        for (var y = 0; y < 16; y++) for (var x = 0; x < 16; x++) assets.Image.SetPixel(x, y, y < 8 ? x < 8 ? Colors.Red : Colors.Blue : x < 8 ? Colors.Green : Colors.Yellow);
        assets.Texture.Update(assets.Image);
        var alternative = assets.Atlas.CreateAlternativeTile(default); var data = assets.Atlas.GetTileData(default, alternative); data.FlipH = true; data.Transpose = true;
        using var world = new World(backend);
        var window = new Window { Title = "Tile atlas and physics validation", Size = new(256, 160), World = world };
        var layer = new TileMapLayer { Name = "Tiles", TileSet = assets.Set, Position = new(32, 32), Scale = new(2, 2), TextureFilter = TextureFilter.Nearest };
        layer.SetCell(default, 3, default(Vector2i)); layer.SetCell(new(2, 0), 3, default(Vector2i), TileSetAtlasSource.TransformFlipH);
        layer.SetCell(new(4, 0), 3, default(Vector2i), alternative | TileSetAtlasSource.TransformTranspose);
        for (var x = 0; x < 6; x++) layer.SetCell(new(x, 3), 3, default(Vector2i));
        window.AddChild(layer);
        using var ballShape = new CircleShape { Radius = 5 };
        var ball = new RigidBody { Name = "Ball", Position = new(64, 80), CanSleep = false }; ball.AddChild(new CollisionShape { Shape = ballShape }); ball.AddChild(new BallVisual()); window.AddChild(ball);
        var frames = 0; long began = 0, owner = 0, total = 0; var finished = false; double seconds = 0; ulong ticks = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                frames++;
                if (frames == 4)
                {
                    using var pixels = window.GetTexture().GetImage()!;
                    Pixel(pixels, 36, 36, Colors.Red); Pixel(pixels, 56, 36, Colors.Blue);
                    Pixel(pixels, 100, 36, Colors.Blue); Pixel(pixels, 120, 36, Colors.Red);
                    Pixel(pixels, 164, 36, Colors.Green); Pixel(pixels, 184, 36, Colors.Yellow);
                    Check(Hit(world.DirectSpaceState, new(40, 40)).ColliderObject == layer, "Rendered tile and actual collision share placement");
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                    GC.GetTotalAllocatedBytes(true);
                }
                if (frames == 64)
                {
                    began = Stopwatch.GetTimestamp(); ticks = window.Tree!.PhysicsFrameCount;
                    total = GC.GetTotalAllocatedBytes(true); owner = GC.GetAllocatedBytesForCurrentThread();
                }
                if (frames == 128)
                {
                    owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
                    seconds = Stopwatch.GetElapsedTime(began).TotalSeconds; ticks = window.Tree!.PhysicsFrameCount - ticks;
                    Check(ticks > 0 && ball.Position.Y < 129 && ball.Position.Y > 115, "Active body settles on the rendered tile floor");
                    Check(owner == 0 && total == 0, $"Whole native tile frames allocated {owner}/{total} B");
                    using var pixels = window.GetTexture().GetImage()!;
                    System.IO.Directory.CreateDirectory("bin/physics-tiles-validation/2026-10-10");
                    pixels.SavePNG($"bin/physics-tiles-validation/2026-10-10/tiles-{renderer}-{backend}.png");
                    layer.CollisionVisibilityMode = TileMapLayer.DebugVisibilityMode.ForceShow;
                }
                if (frames == 129)
                {
                    using var pixels = window.GetTexture().GetImage()!; Check(pixels.GetPixel(40, 40) != Colors.Red, "Forced collision overlay renders");
                    layer.CollisionVisibilityMode = TileMapLayer.DebugVisibilityMode.ForceHide; window.Tree!.DebugCollisionsHint = true;
                }
                if (frames == 130)
                {
                    using var pixels = window.GetTexture().GetImage()!; Pixel(pixels, 36, 36, Colors.Red);
                    finished = true; window.Tree!.Quit();
                }
                if (frames > 4 && frames < 128) layer.QueueRedraw();
            };
        };
        Check(Engine.Run(window) == 0 && finished, "Native tile workflow completed");
        Console.WriteLine($"Native tiles: renderer={renderer}, physics={backend}, 256x160, 64 measured full frames, {seconds:F3}s, {64 / seconds:F2} FPS, {ticks} physics ticks, managed owner/all={owner}/{total} B, forced redraw and pixel assertions passed.");
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    { var actual = image.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .03f && Math.Abs(actual.G - expected.G) < .03f && Math.Abs(actual.B - expected.B) < .03f, $"Pixel {x},{y}: {actual} != {expected}"); }
    private sealed class BallVisual : Entity { protected override void OnDraw() => DrawCircle(default, 5, Colors.Pink); }

    private static void VerifyFreshScene(TileMapLayer layer)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-tile-scene-" + Guid.NewGuid() + ".e2dscene");
        using var packed = new PackedScene(); packed.Pack(layer);
        try
        {
            ResourceSaver.Save(packed, path);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(typeof(TileMapLayerTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_TILES"); start.Environment["ELECTRON2D_TEST_TILES_FILE"] = path;
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Tile scene child."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh tile scene passed"), errors.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
    }
    internal static void RunFile(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        var layer = (TileMapLayer)packed.Instantiate(); using var root = new SubViewport(); root.AddChild(layer); using var tree = new SceneTree(root); tree.FlushDeferred();
        Check(layer.GetUsedCells().Length == 9 && layer.GetCellSourceID(new(0, -1)) == 3, "Fresh process restores wrapped authored cells");
        using var query = new PhysicsPointQueryParameters { Position = new(8, 8) }; var hits = layer.GetWorld()!.DirectSpaceState.IntersectPoint(query);
        Check(hits.Length > 0 && hits.All(h => h.ColliderObject == layer), "Fresh scene reconstructs actual tile physics");
        Console.WriteLine("Fresh tile scene passed");
    }
}
