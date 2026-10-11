using Electron2D;

internal static partial class TileProxyTests
{
    internal static void RunNative()
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS; Engine.MaxFPS = 60;
        try { foreach (var renderer in new[] { "gpu", "compatibility" }) foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) Native(renderer, backend); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); Engine.MaxFPS = fps; }
    }
    private static void Native(string renderer, PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal); Configure(library.Set); using var world = new World(backend); var window = new Window { Title = "Tile proxy validation", Size = new(320, 160), World = world };
        var layer = new TileMapLayer { TileSet = library.Set, PhysicsQuadrantSize = 1, Position = new(96, 96), TextureFilter = TextureFilter.Nearest }; window.AddChild(layer); PaintThree(layer);
        try
        {
            var frames = 0; long bytes = 0; var finished = false; Vector2[] erased = [];
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePostDraw += () =>
                {
                    frames++;
                    if (frames == 4) CheckPixels(window, world, layer);
                    if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
                    if (frames == 40)
                    {
                        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Prepared proxy frames allocated {bytes} B");
                        using var image = window.GetTexture().GetImage()!; System.IO.Directory.CreateDirectory("bin/proxy-validation"); image.SavePNG($"bin/proxy-validation/proxy-{renderer}-{backend}.png");
                        library.Set.SetAlternativeLevelTileProxy(102, new(4, 4), 5, 0, default, -1); PaintThree(layer);
                    }
                    if (frames == 41) { Check(layer.GetUsedCells().Length == 2, "Mapped empty alternative erases destination"); CheckPixels(window, world, layer); erased = layer.GetUsedCells().Select(c => layer.GlobalTransform * layer.MapToLocal(c)).ToArray(); layer.Clear(); }
                    if (frames == 42)
                    {
                        NavigationServer.Synchronize(); using var image = window.GetTexture().GetImage()!;
                        foreach (var point in erased) { var color = image.GetPixel((int)point.X, (int)point.Y); Check(color.R < .03f && color.G < .03f && color.B < .03f, "Cleared mapped cells remove retained pixels"); }
                        Check(NavigationServer.MapGetRegions(layer.GetNavigationMap()).Length == 0, "Cleared mapped regions released"); finished = true; window.Tree!.Quit();
                    }
                    if (frames >= 4 && frames < 40) layer.QueueRedraw();
                };
            };
            Check(Engine.Run(window) == 0 && finished, "Native proxy workflow completed"); Console.WriteLine($"Native tile proxies: {renderer}/{backend}, three levels, edit/erase, actual pixels/collision/navigation, 24 warmed full forced-redraw owner frames={bytes} B.");
        }
        finally { window.Dispose(); }
    }
    private static void CheckPixels(Window window, World world, TileMapLayer layer)
    {
        NavigationServer.Synchronize(); Verify(layer, world); using var image = window.GetTexture().GetImage()!;
        foreach (var coord in new[] { new Vector2i(-2, -2), new(-1, -2), new(0, -2) })
        {
            var point = layer.GlobalTransform * layer.MapToLocal(coord); var actual = image.GetPixel((int)point.X, (int)point.Y);
            if (layer.GetCellSourceID(coord) == -1) Check(actual.R < .03f && actual.G < .03f && actual.B < .03f, "Proxy erasure removes retained pixel");
            else { var expected = layer.GetCellTileData(coord)!.Modulate; Check(Math.Abs(actual.R - expected.R) < .03f && Math.Abs(actual.G - expected.G) < .03f && Math.Abs(actual.B - expected.B) < .03f, "Mapped destination draws authored pixels"); }
        }
    }
}
