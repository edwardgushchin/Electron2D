using Electron2D;

internal static partial class TilePatternTests
{
    internal static void RunNative()
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS; Engine.MaxFPS = 60;
        try { foreach (var renderer in new[] { "gpu", "compatibility" }) foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) Native(renderer, backend); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); Engine.MaxFPS = fps; }
    }
    private static void Native(string renderer, PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); using var world = new World(backend); var window = new Window { Title = "Tile pattern validation", Size = new(640, 160), World = world }; var libraries = new List<Library>(); var panels = new List<(TileMapLayer Layer, TileMapPattern Pattern)>();
        try
        {
            foreach (var shape in Enum.GetValues<TileShape>())
            {
                var library = new Library(shape, (TileLayout)((int)shape % 2), (TileOffsetAxis)((int)shape % 2)); libraries.Add(library);
                var layer = new TileMapLayer { Name = "Pattern" + shape, TileSet = library.Set, PhysicsQuadrantSize = 1, TextureFilter = TextureFilter.Nearest }; layer.Position = new Vector2((int)shape * 160 + 56, 48) - layer.MapToLocal(new(-3, -3)); window.AddChild(layer);
                layer.SetCell(new(-3, -3), 0, Vector2i.Zero); layer.SetCell(new(-3, -2), 0, Vector2i.Zero, TileSetAtlasSource.TransformTranspose);
                var pattern = layer.GetPattern([new(-3, -3), new(-3, -2), new(-2, -2)]); library.Set.AddPattern(pattern); layer.Clear(); layer.SetPattern(new(-3, -3), library.Set.GetPattern(0)); panels.Add((layer, pattern));
            }
            var frames = 0; long bytes = 0; var finished = false; var erasedPoints = new List<Vector2>();
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePostDraw += () =>
                {
                    frames++;
                    if (frames == 4) CheckPanels(window, world, panels);
                    if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
                    if (frames == 40)
                    {
                        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Prepared pattern frames allocated {bytes} B");
                        using var image = window.GetTexture().GetImage()!; System.IO.Directory.CreateDirectory("bin/pattern-validation"); image.SavePNG($"bin/pattern-validation/pattern-{renderer}-{backend}.png");
                        foreach (var panel in panels) { panel.Layer.Clear(); panel.Layer.SetPattern(new(-2, -2), panel.Pattern); }
                    }
                    if (frames == 41)
                    {
                        CheckPanels(window, world, panels);
                        foreach (var panel in panels) { foreach (var coord in panel.Layer.GetUsedCells()) erasedPoints.Add(panel.Layer.GlobalTransform * panel.Layer.MapToLocal(coord)); panel.Layer.Clear(); }
                    }
                    if (frames == 42)
                    {
                        NavigationServer.Synchronize(); using var image = window.GetTexture().GetImage()!;
                        foreach (var point in erasedPoints) { var color = image.GetPixel((int)point.X, (int)point.Y); Check(color.R < .03f && color.G < .03f && color.B < .03f, "Cleared stamp removes retained pixels"); }
                        foreach (var panel in panels) Check(NavigationServer.MapGetRegions(panel.Layer.GetNavigationMap()).Length == 0, "Cleared stamp releases regions");
                        finished = true; window.Tree!.Quit();
                    }
                    if (frames >= 4 && frames < 40) foreach (var panel in panels) panel.Layer.QueueRedraw();
                };
            };
            Check(Engine.Run(window) == 0 && finished, "Native pattern workflow"); Console.WriteLine($"Native patterns: {renderer}/{backend}, four shapes, capture/re-place, actual pixels/collision/navigation, 24 warmed full owner forced-redraw frames={bytes} B.");
        }
        finally { window.Dispose(); foreach (var panel in panels) panel.Pattern.Dispose(); for (var i = libraries.Count - 1; i >= 0; i--) libraries[i].Dispose(); }
    }
    private static void CheckPanels(Window window, World world, List<(TileMapLayer Layer, TileMapPattern Pattern)> panels)
    {
        NavigationServer.Synchronize(); using var image = window.GetTexture().GetImage()!;
        foreach (var panel in panels)
        {
            Check(panel.Layer.GetUsedCells().Length == 2, "Two solid cells in captured stamp"); VerifyConsumers(panel.Layer, world);
            foreach (var coord in panel.Layer.GetUsedCells())
            {
                var point = panel.Layer.GlobalTransform * panel.Layer.MapToLocal(coord); var actual = image.GetPixel((int)point.X, (int)point.Y); var expected = panel.Layer.GetCellTileData(coord)!.Modulate;
                Check(Math.Abs(actual.R - expected.R) < .03f && Math.Abs(actual.G - expected.G) < .03f && Math.Abs(actual.B - expected.B) < .03f, "Placed pattern pixels");
            }
        }
    }
}
