using Electron2D;

internal static partial class TileLayoutTests
{
    internal static void RunNative()
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var previousFPS = Engine.MaxFPS; Engine.MaxFPS = 60;
        try
        {
            foreach (var renderer in new[] { "gpu", "compatibility" }) foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) Native(renderer, backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); Engine.MaxFPS = previousFPS; }
    }
    private static void Native(string renderer, PhysicsServer.Backend backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); using var fixture = Fixture(); using var world = new World(backend);
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.Red); using var texture = ImageTexture.CreateFromImage(image);
        var assets = new List<IDisposable>(); var probes = new List<(Vector2 Point, TileMapLayer Layer)>(); var layers = new List<TileMapLayer>();
        var window = new Window { Title = "Tile grid layout validation", Size = new(768, 384), World = world }; var index = 0;
        try
        {
            foreach (var c in fixture.RootElement.GetProperty("cases").EnumerateArray())
            {
                var set = Set(c); var atlas = new TileSetAtlasSource { Texture = texture }; assets.Add(set); assets.Add(atlas); set.AddPhysicsLayer(); set.AddSource(atlas, 0); atlas.CreateTile(default);
                var data = atlas.GetTileData(default, 0); data.AddCollisionPolygon(0); data.SetCollisionPolygonPoints(0, 0, [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]);
                var row = c.GetProperty("cells").EnumerateArray().Single(r => Cell(r) == new Vector2i(-2, 3)); var center = new Vector2(row[2].GetSingle(), row[3].GetSingle()); var anchor = new Vector2(index % 8 * 96 + 24, index / 8 * 64 + 20); index++;
                var layer = new TileMapLayer { Name = "Tiles" + index, TileSet = set, Position = anchor - center, TextureFilter = TextureFilter.Nearest, PhysicsQuadrantSize = 1 }; window.AddChild(layer); layers.Add(layer); layer.SetCell(new(-2, 3), 0, default(Vector2i)); probes.Add((anchor, layer));
                // Place a second cell from the independent neighbor fixture to exercise spacing, not just origins.
                var neighbor = row[4][0]; var next = Cell(neighbor, 1); var nextRow = c.GetProperty("cells").EnumerateArray().Single(r => Cell(r) == next); var nextCenter = new Vector2(nextRow[2].GetSingle(), nextRow[3].GetSingle());
                layer.SetCell(next, 0, default(Vector2i)); probes.Add((anchor + nextCenter - center, layer));
            }
            var frames = 0; long bytes = 0; var finished = false;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePostDraw += () =>
                {
                    frames++;
                    if (frames == 4)
                    {
                        using var pixels = window.GetTexture().GetImage()!;
                        foreach (var probe in probes)
                        {
                            var color = pixels.GetPixel((int)probe.Point.X, (int)probe.Point.Y); Check(color.R > .95f && color.G < .03f && color.B < .03f, $"Rendered {renderer}/{backend} tile layout center at {probe.Point}");
                            using var query = new PhysicsPointQueryParameters { Position = probe.Point }; Check(world.DirectSpaceState.IntersectPoint(query).Any(h => h.ColliderObject == probe.Layer), "Rendered cell and selected-backend collision agree");
                        }
                    }
                    if (frames == 16) bytes = GC.GetAllocatedBytesForCurrentThread();
                    if (frames == 40)
                    {
                        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(bytes == 0, $"Native owner frames allocated {bytes} bytes");
                        using var pixels = window.GetTexture().GetImage()!; System.IO.Directory.CreateDirectory("bin/tile-layout-validation"); pixels.SavePNG($"bin/tile-layout-validation/layouts-{renderer}-{backend}.png");
                        // Stored geometry edits must also refresh the retained drawing commands.
                        foreach (var layer in layers) layer.TileSet!.TileShape = TileShape.Square;
                    }
                    if (frames == 41)
                    {
                        using var pixels = window.GetTexture().GetImage()!;
                        foreach (var layer in layers)
                        {
                            var point = layer.Position + layer.MapToLocal(new(-2, 3)); if (point.X < 0 || point.Y < 0 || point.X >= 768 || point.Y >= 384) continue;
                            Check(pixels.GetPixel((int)point.X, (int)point.Y).R > .95f, "Shape edits redraw at the updated center");
                        }
                        finished = true; window.Tree!.Quit();
                    }
                    if (frames >= 4 && frames < 40) foreach (var layer in layers) layer.QueueRedraw();
                };
            };
            Check(Engine.Run(window) == 0 && finished, "Native layout workflow completes");
            Console.WriteLine($"Native layouts: {renderer}/{backend}, 48 configurations, 96 rendered centers/collision probes, 24 measured forced-redraw owner frames={bytes} B, geometry edits rendered.");
        }
        finally { window.Dispose(); for (var i = assets.Count - 1; i >= 0; i--) assets[i].Dispose(); }
    }
}
