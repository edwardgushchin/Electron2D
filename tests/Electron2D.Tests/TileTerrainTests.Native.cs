using Electron2D;

internal static partial class TileTerrainTests
{
    internal static void RunNative()
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS; Engine.MaxFPS = 60;
        try { foreach (var renderer in new[] { "gpu", "compatibility" }) foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) Native(renderer, backend); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); Engine.MaxFPS = fps; }
    }
    private static void Native(string renderer, PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); using var world = new World(backend); var window = new Window { Title = "Terrain painting validation", Size = new(640, 384), World = world }; var libraries = new List<Library>(); var panels = new List<(TileMapLayer Layer, Vector2i[] Cells)>();
        using var polygon = new NavigationPolygon { AgentRadius = 0 }; Vector2[] outline = [new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)]; polygon.SetVertices(outline); polygon.AddPolygon([0, 1, 2, 3]);
        try
        {
            var index = 0;
            foreach (var shape in Enum.GetValues<TileShape>()) foreach (var mode in Enum.GetValues<TerrainMode>())
                {
                    var axis = (TileOffsetAxis)(((int)shape + (int)mode) % 2); var library = new Library(shape, (TileLayout)(((int)mode + 2) % 6), axis, mode); libraries.Add(library); library.PrepareDrawing(); library.Set.AddPhysicsLayer(); library.Set.AddNavigationLayer();
                    for (var tile = 0; tile < library.Atlas.GetTilesCount(); tile++)
                    {
                        var coords = library.Atlas.GetTileID(tile);
                        for (var alternative = 0; alternative < library.Atlas.GetAlternativeTilesCount(coords); alternative++)
                        {
                            var data = library.Atlas.GetTileData(coords, library.Atlas.GetAlternativeTileID(coords, alternative)); data.AddCollisionPolygon(0); data.SetCollisionPolygonPoints(0, 0, outline); data.SetNavigationPolygon(0, polygon);
                            var active = Enumerable.Range(0, 16).Count(i => data.IsValidTerrainPeeringBit((CellNeighbor)i) && data.GetTerrainPeeringBit((CellNeighbor)i) >= 0); data.Modulate = data.Terrain >= 0 ? new Color(.25f + active * .025f, .7f, .25f) : new Color(.2f, .3f + active * .025f, .85f);
                        }
                    }
                    var layer = new TileMapLayer { Name = "Terrain" + index, TileSet = library.Set, PhysicsQuadrantSize = 1, TextureFilter = TextureFilter.Nearest }; var a = Vector2i.Zero;
                    var first = shape == TileShape.Square || (int)shape > 1 && axis == TileOffsetAxis.Horizontal ? CellNeighbor.RightSide : CellNeighbor.BottomRightSide;
                    var second = shape == TileShape.Square || (int)shape > 1 && axis == TileOffsetAxis.Vertical ? CellNeighbor.BottomSide : shape == TileShape.Isometric ? CellNeighbor.BottomLeftSide : CellNeighbor.BottomRightSide;
                    var b = layer.GetNeighborCell(a, first); var c = layer.GetNeighborCell(a, second); var anchor = new Vector2(index % 4 * 160 + 56, index / 4 * 128 + 48); index++;
                    layer.Position = anchor - layer.MapToLocal(a); window.AddChild(layer); layer.SetCellsTerrainConnect([a, b, c], 0, 0, false); panels.Add((layer, [a, b, c]));
                }
            var frames = 0; long bytes = 0; var finished = false;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePostDraw += () =>
                {
                    frames++;
                    if (frames == 4) CheckNativePanels(window, world, panels);
                    if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
                    if (frames == 40)
                    {
                        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Prepared terrain owner frames allocated {bytes} bytes");
                        using var image = window.GetTexture().GetImage()!; System.IO.Directory.CreateDirectory("bin/terrain-validation"); image.SavePNG($"bin/terrain-validation/terrain-{renderer}-{backend}.png");
                        foreach (var panel in panels) panel.Layer.SetCellsTerrainPath(panel.Cells, 0, -1, false);
                    }
                    if (frames == 41)
                    {
                        CheckNativePanels(window, world, panels); finished = true; window.Tree!.Quit();
                    }
                    if (frames >= 4 && frames < 40) foreach (var panel in panels) panel.Layer.QueueRedraw();
                };
            };
            Check(Engine.Run(window) == 0 && finished, "Native terrain workflow completed");
            Console.WriteLine($"Native terrains: {renderer}/{backend}, 12 shape/mode panels, connect/path edits, actual pixels/owned collision/navigation, 24 warmed full forced-redraw owner frames={bytes} B.");
        }
        finally { window.Dispose(); for (var i = libraries.Count - 1; i >= 0; i--) libraries[i].Dispose(); }
    }
    private static void CheckNativePanels(Window window, World world, List<(TileMapLayer Layer, Vector2i[] Cells)> panels)
    {
        NavigationServer.Synchronize(); using var image = window.GetTexture().GetImage()!;
        foreach (var panel in panels) foreach (var coord in panel.Cells) if (panel.Layer.GetCellTileData(coord) is null)
                {
                    var point = panel.Layer.GlobalTransform * panel.Layer.MapToLocal(coord); var color = image.GetPixel((int)point.X, (int)point.Y); Check(color.R < .03f && color.G < .03f && color.B < .03f, "Erased terrain clears its retained pixels");
                }
        foreach (var panel in panels) foreach (var coord in panel.Layer.GetUsedCells())
            {
                var point = panel.Layer.GlobalTransform * panel.Layer.MapToLocal(coord); var expected = panel.Layer.GetCellTileData(coord)!.Modulate; var actual = image.GetPixel((int)point.X, (int)point.Y);
                Check(Math.Abs(actual.R - expected.R) < .03f && Math.Abs(actual.G - expected.G) < .03f && Math.Abs(actual.B - expected.B) < .03f, "Selected terrain pattern draws its authored color");
                VerifyTerrainConsumers(panel.Layer, world, [coord]);
            }
    }
}
