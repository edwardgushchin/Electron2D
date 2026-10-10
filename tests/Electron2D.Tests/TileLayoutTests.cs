using System.Diagnostics;
using System.Text.Json;
using Electron2D;

internal static partial class TileLayoutTests
{
    internal static void Run()
    {
        Reference(); Validation(); Storage();
        foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU }) Consumers(backend);
        Console.WriteLine("Tile layouts passed: 48 pinned configurations, centers/inverse/boundaries/neighbors, checked limits, storage, CPU/GPU collision and navigation placement, warmed scalar queries.");
    }
    private static JsonDocument Fixture() => JsonDocument.Parse(typeof(TileLayoutTests).Assembly.GetManifestResourceStream("TestTiles.layout-reference.json")!);
    private static Vector2i Cell(JsonElement row, int index = 0) => new(row[index].GetInt32(), row[index + 1].GetInt32());
    private static TileSet Set(JsonElement c) => new() { TileSize = new(30, 22), TileShape = (TileShape)c.GetProperty("shape").GetInt32(), TileLayout = (TileLayout)c.GetProperty("layout").GetInt32(), TileOffsetAxis = (TileOffsetAxis)c.GetProperty("axis").GetInt32() };
    private static void Reference()
    {
        using var fixture = Fixture(); Check(fixture.RootElement.GetProperty("commit").GetString() == "ed1daf0bf001b61586d9930840f2f1394092c079", "Pinned geometry provenance");
        var cases = fixture.RootElement.GetProperty("cases"); Check(cases.GetArrayLength() == 48, "Full grid family");
        foreach (var c in cases.EnumerateArray())
        {
            using var set = Set(c); using var layer = new TileMapLayer { TileSet = set };
            var identity = $"{set.TileShape}/{set.TileLayout}/{set.TileOffsetAxis}";
            foreach (var row in c.GetProperty("cells").EnumerateArray())
            {
                var coords = Cell(row); var expected = new Vector2(row[2].GetSingle(), row[3].GetSingle());
                Check(layer.MapToLocal(coords) == expected, $"{identity} center {coords}");
                Check(layer.LocalToMap(expected) == coords, $"{identity} center roundtrip {coords}");
                var valid = new HashSet<CellNeighbor>();
                foreach (var n in row[4].EnumerateArray())
                {
                    var direction = (CellNeighbor)n[0].GetInt32(); var next = Cell(n, 1); valid.Add(direction);
                    Check(layer.GetNeighborCell(coords, direction) == next, $"{identity} neighbor {coords}/{direction}");
                    Check(layer.GetNeighborCell(next, (CellNeighbor)(((int)direction + 8) % 16)) == coords, $"{identity} reciprocal {coords}/{direction}");
                }
                foreach (var direction in Enum.GetValues<CellNeighbor>()) if (!valid.Contains(direction)) Reject<ArgumentOutOfRangeException>(() => layer.GetNeighborCell(coords, direction));
                Check(layer.GetSurroundingCells(coords).SequenceEqual(row[5].EnumerateArray().Select(n => Cell(n))), $"{identity} surrounding {coords}");
            }
            foreach (var row in c.GetProperty("positions").EnumerateArray())
            {
                var point = new Vector2(row[0].GetSingle(), row[1].GetSingle()); var actual = layer.LocalToMap(point); var expected = Cell(row, 2);
                Check(actual == expected, $"{identity} inverse {point}: {actual} != {expected}");
            }
            for (var i = 0; i < 64; i++) { layer.MapToLocal(new(-2, 3)); layer.LocalToMap(new(20, 30)); layer.GetNeighborCell(default, set.TileShape == TileShape.Square ? CellNeighbor.BottomSide : CellNeighbor.BottomRightSide); }
            var bytes = GC.GetAllocatedBytesForCurrentThread(); var sum = 0f;
            for (var i = 0; i < 1024; i++) { sum += layer.MapToLocal(new(-2, 3)).X; sum += layer.LocalToMap(new(20, 30)).Y; sum += layer.GetNeighborCell(default, set.TileShape == TileShape.Square ? CellNeighbor.BottomSide : CellNeighbor.BottomRightSide).X; }
            Check(GC.GetAllocatedBytesForCurrentThread() == bytes && float.IsFinite(sum), "Warmed scalar geometry allocates zero managed bytes");
        }
    }
    private static void Validation()
    {
        using var set = new TileSet(); using var layer = new TileMapLayer { TileSet = set }; var changes = 0; set.Changed += _ => changes++;
        Check(set.TileShape == TileShape.Square && set.TileLayout == TileLayout.Stacked && set.TileOffsetAxis == TileOffsetAxis.Horizontal, "Geometry defaults");
        set.TileShape = TileShape.Square; set.TileLayout = TileLayout.Stacked; set.TileOffsetAxis = TileOffsetAxis.Horizontal; Check(changes == 0, "Same-value setters are quiet");
        Reject<ArgumentOutOfRangeException>(() => set.TileShape = (TileShape)4); Reject<ArgumentOutOfRangeException>(() => set.TileLayout = (TileLayout)(-1)); Reject<ArgumentOutOfRangeException>(() => set.TileOffsetAxis = (TileOffsetAxis)2); Check(changes == 0 && set.TileShape == TileShape.Square, "Rejected setters preserve state");
        Reject<ArgumentOutOfRangeException>(() => layer.GetNeighborCell(default, (CellNeighbor)16));
        foreach (var invalid in new[] { new Vector2(float.NaN, 0), new Vector2(0, float.PositiveInfinity) }) Reject<ArgumentOutOfRangeException>(() => layer.LocalToMap(invalid));
        set.TileSize = new(1, 1); Reject<OverflowException>(() => layer.LocalToMap(new(float.MaxValue, float.MaxValue)));
        Reject<OverflowException>(() => layer.GetNeighborCell(new(int.MaxValue, 0), CellNeighbor.RightSide)); Reject<OverflowException>(() => layer.GetSurroundingCells(new(int.MinValue, 0)));
        foreach (var layout in Enum.GetValues<TileLayout>()) foreach (var axis in Enum.GetValues<TileOffsetAxis>())
            {
                set.TileShape = TileShape.Isometric; set.TileLayout = layout; set.TileOffsetAxis = axis;
                Reject<OverflowException>(() => layer.LocalToMap(new(float.MaxValue, float.MaxValue)));
                var direction = layout is TileLayout.Stacked or TileLayout.StackedOffset ? (axis == TileOffsetAxis.Horizontal ? CellNeighbor.RightCorner : CellNeighbor.BottomCorner) : CellNeighbor.BottomRightSide;
                var origin = new Vector2i(1_500_000_000, 1_500_000_000); var next = layer.GetNeighborCell(origin, direction);
                Check(layer.GetNeighborCell(next, (CellNeighbor)(((int)direction + 8) % 16)) == origin, "Widened intermediate layout coordinates remain reversible");
            }
        using var missing = new TileMapLayer(); Reject<InvalidOperationException>(() => missing.MapToLocal(default)); Reject<InvalidOperationException>(() => missing.LocalToMap(default)); Reject<InvalidOperationException>(() => missing.GetNeighborCell(default, CellNeighbor.RightSide)); Reject<InvalidOperationException>(() => missing.GetSurroundingCells(default));
        set.Dispose(); Reject<ObjectDisposedException>(() => _ = set.TileShape); Reject<ObjectDisposedException>(() => set.TileLayout = TileLayout.Stacked);
    }
    private static void Storage()
    {
        using var fixture = Fixture(); var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-layouts-" + Guid.NewGuid()); System.IO.Directory.CreateDirectory(directory);
        try
        {
            var index = 0;
            foreach (var c in fixture.RootElement.GetProperty("cases").EnumerateArray())
            {
                using var set = Set(c); using var copy = (TileSet)set.Duplicate(); using var copied = new TileSet(); copied.CopyFromResource(set); Same(set, copy); Same(set, copied);
                using var layer = new TileMapLayer { Name = "Tiles", TileSet = set }; layer.SetCell(new(-2, 3), 0, default(Vector2i)); using var packed = new PackedScene(); packed.Pack(layer);
                var path = System.IO.Path.Combine(directory, $"{index++}.e2dscene"); ResourceSaver.Save(packed, path);
                using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var instance = (TileMapLayer)loaded.Instantiate(); Same(set, instance.TileSet!); Check(instance.GetUsedCells().Single() == new Vector2i(-2, 3), "Packed authored coordinates remain layout coordinates");
            }
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(TileLayoutTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_TILE_LAYOUTS"); start.Environment["ELECTRON2D_TEST_TILE_LAYOUTS_FILE"] = directory;
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh layout scene process."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh layouts passed"), errors.GetAwaiter().GetResult());
        }
        finally { System.IO.Directory.Delete(directory, true); }
    }
    private static void Same(TileSet first, TileSet second) => Check(first.TileShape == second.TileShape && first.TileLayout == second.TileLayout && first.TileOffsetAxis == second.TileOffsetAxis && first.TileSize == second.TileSize, "Authored geometry roundtrip");
    internal static void RunFile(string directory)
    {
        using var fixture = Fixture(); var index = 0;
        foreach (var c in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var expected = Set(c); using var packed = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(directory, $"{index++}.e2dscene"), ResourceLoader.CacheMode.Ignore); using var layer = (TileMapLayer)packed.Instantiate(); Same(expected, layer.TileSet!);
            var row = c.GetProperty("cells").EnumerateArray().Single(r => Cell(r) == new Vector2i(-2, 3)); Check(layer.MapToLocal(new(-2, 3)) == new Vector2(row[2].GetSingle(), row[3].GetSingle()), "Fresh loaded executable geometry");
        }
        Console.WriteLine("Fresh layouts passed: all 48 resource/scene configurations.");
    }
    private static void Consumers(PhysicsServer.Backend backend)
    {
        using var fixture = Fixture(); using var world = new World(backend); using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        using var polygon = new NavigationPolygon { AgentRadius = 0 }; Vector2[] outline = [new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)]; polygon.SetVertices(outline); polygon.AddPolygon([0, 1, 2, 3]); polygon.AddOutline(outline);
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        foreach (var c in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var set = Set(c); using var atlas = new TileSetAtlasSource { Texture = texture }; set.AddPhysicsLayer(); set.AddNavigationLayer(); set.AddSource(atlas, 0); atlas.CreateTile(default);
            var data = atlas.GetTileData(default, 0); data.AddCollisionPolygon(0); data.SetCollisionPolygonPoints(0, 0, outline); data.SetNavigationPolygon(0, polygon);
            var layer = new TileMapLayer { TileSet = set, Position = new(120, 90), Scale = new(-1.5f, 2), Rotation = .2f, PhysicsQuadrantSize = 1 }; layer.SetCell(new(-2, 3), 0, default(Vector2i)); root.AddChild(layer); tree.FlushDeferred(); NavigationServer.Synchronize();
            var row = c.GetProperty("cells").EnumerateArray().Single(r => Cell(r) == new Vector2i(-2, 3)); var center = new Vector2(row[2].GetSingle(), row[3].GetSingle()); var point = layer.GlobalTransform * center;
            using var query = new PhysicsPointQueryParameters { Position = point }; var hit = world.DirectSpaceState.IntersectPoint(query).Single(); Check(hit.ColliderObject == layer && layer.GetCoordsForBodyRID(hit.ColliderRID) == new Vector2i(-2, 3), "Generated selected-backend collision uses layout center and original coordinate identity");
            var map = layer.GetNavigationMap(); var region = NavigationServer.MapGetRegions(map).Single(); Check(NavigationServer.RegionGetTransform(region).Origin.IsEqualApprox(point) && NavigationServer.RegionGetOwnerID(region) == layer.InstanceID, "Navigation region shares grid placement and owner");
            using var source = new NavigationMeshSourceGeometryData(); using var target = new NavigationPolygon { AgentRadius = 0 }; NavigationServer.ParseSourceGeometryData(target, source, layer); Check(source.GetTraversableOutlines().Single().Length == 4 && source.GetTraversableOutlines().Single().All(v => outline.Any(p => v.DistanceSquaredTo(center + p) < 1e-8f)), $"Baking {set.TileShape}/{set.TileLayout}/{set.TileOffsetAxis}: center {center}, actual {string.Join(";", source.GetTraversableOutlines().Single())}");
            // A resource edit rebuilds all consuming lanes while keeping authored coordinates.
            set.TileLayout = (TileLayout)(((int)set.TileLayout + 1) % 6); tree.FlushDeferred(); NavigationServer.Synchronize(); var nextPoint = layer.GlobalTransform * layer.MapToLocal(new(-2, 3)); query.Position = nextPoint;
            Check(world.DirectSpaceState.IntersectPoint(query).Single().ColliderObject == layer && NavigationServer.RegionGetTransform(NavigationServer.MapGetRegions(map).Single()).Origin.IsEqualApprox(nextPoint), "Changed layout propagates to physics and navigation");
            root.RemoveChild(layer); layer.Dispose(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(map).Length == 0 && world.DirectSpaceState.IntersectPoint(query).Length == 0, "Exit releases generated resources");
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
