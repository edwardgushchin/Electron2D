using System.Diagnostics;
using Electron2D;

internal static partial class TilePatternTests
{
    private sealed class Library : IDisposable
    {
        internal readonly TileSet Set = new() { TileSize = new(16, 16) };
        internal readonly TileSetAtlasSource Atlas = new() { TextureRegionSize = new(16, 16), UseTexturePadding = false };
        private readonly ImageTexture _texture;
        private readonly NavigationPolygon _polygon = new() { AgentRadius = 0 };
        internal Library(TileShape shape, TileLayout layout, TileOffsetAxis axis)
        {
            Set.TileShape = shape; Set.TileLayout = layout; Set.TileOffsetAxis = axis; Set.AddPhysicsLayer(); Set.AddNavigationLayer();
            using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White); _texture = ImageTexture.CreateFromImage(image); Atlas.Texture = _texture;
            Atlas.CreateTile(default); Set.AddSource(Atlas, 0); Vector2[] outline = [new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)]; _polygon.SetVertices(outline); _polygon.AddPolygon([0, 1, 2, 3]); _polygon.AddOutline(outline);
            var data = Atlas.GetTileData(default, 0); data.Modulate = new(.25f, .7f, .4f); data.AddCollisionPolygon(0); data.SetCollisionPolygonPoints(0, 0, outline); data.SetNavigationPolygon(0, _polygon);
        }
        public void Dispose() { Set.Dispose(); Atlas.Dispose(); _texture.Dispose(); _polygon.Dispose(); }
    }
    private static TileMapPattern Stamp()
    {
        var pattern = new TileMapPattern(); pattern.SetCell(default, 0, Vector2i.Zero, 0); pattern.SetCell(new(1, 0), 0, Vector2i.Zero, TileSetAtlasSource.TransformFlipH); pattern.SetCell(new(0, 1)); pattern.SetSize(new(4, 4)); return pattern;
    }
    private static void Consumers()
    {
        foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU })
        {
            using var library = new Library(TileShape.Hexagon, TileLayout.DiamondDown, TileOffsetAxis.Vertical); using var world = new World(backend); using var root = new SubViewport { World = world };
            var layer = new TileMapLayer { Name = "Stamp", TileSet = library.Set, PhysicsQuadrantSize = 1, Position = new(120, 80), Scale = new(-1.5f, 2), Rotation = .2f }; root.AddChild(layer); using var tree = new SceneTree(root); using var pattern = Stamp(); library.Set.AddPattern(pattern);
            layer.SetCell(new(0, 1), 0, Vector2i.Zero); layer.SetPattern(default, pattern); tree.FlushDeferred(); NavigationServer.Synchronize();
            Check(layer.GetUsedCells().Length == 2 && layer.GetCellSourceID(new(0, 1)) == -1, "Explicit pattern erasure reaches layer"); VerifyConsumers(layer, world);
            var anchor = new Vector2i(-3, 5); layer.SetPattern(anchor, pattern); tree.FlushDeferred(); NavigationServer.Synchronize(); VerifyConsumers(layer, world);
            layer.Enabled = false; tree.FlushDeferred(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(layer.GetNavigationMap()).Length == 0, "Disabled pattern regions released"); layer.Enabled = true; tree.FlushDeferred(); NavigationServer.Synchronize(); VerifyConsumers(layer, world);
            for (var i = 0; i < 32; i++) layer.UpdateInternals(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) layer.UpdateInternals(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Unchanged pattern consumers allocate zero owner bytes");
            using var source = new NavigationMeshSourceGeometryData(); using var target = new NavigationPolygon { AgentRadius = 0 }; NavigationServer.ParseSourceGeometryData(target, source, layer); Check(source.GetTraversableOutlines().Length == 4, "Placed patterns reach navigation baking");
            SaveFresh(layer); layer.Clear(); tree.FlushDeferred(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(layer.GetNavigationMap()).Length == 0, "Pattern clear cleanup");
        }
        using var large = new TileMapPattern(); large.SetCell(new(100000, 200000), 0, Vector2i.Zero, 0); large.SetSize(new(100010, 200020));
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pattern-" + Guid.NewGuid() + ".e2dres");
        try { ResourceSaver.Save(large, path); using var loaded = ResourceLoader.Load<TileMapPattern>(path, ResourceLoader.CacheMode.Ignore); Check(loaded.GetSize() == large.GetSize() && loaded.HasCell(new(100000, 200000)), "Full pattern coordinates and margins survive files"); }
        finally { System.IO.File.Delete(path); }
    }
    private static void VerifyConsumers(TileMapLayer layer, World world)
    {
        var regions = NavigationServer.MapGetRegions(layer.GetNavigationMap()).Where(r => NavigationServer.RegionGetOwnerID(r) == layer.InstanceID).ToArray(); Check(regions.Length == layer.GetUsedCells().Length, "One region per placed solid cell");
        foreach (var coord in layer.GetUsedCells())
        {
            var point = layer.GlobalTransform * layer.MapToLocal(coord); using var query = new PhysicsPointQueryParameters { Position = point };
            Check(world.DirectSpaceState.IntersectPoint(query).Any(h => h.ColliderObject == layer && layer.GetCoordsForBodyRID(h.ColliderRID) == coord), "Selected backend reports placed tile owner and cell");
            Check(regions.Any(r => NavigationServer.RegionGetOwnerID(r) == layer.InstanceID && NavigationServer.RegionGetTransform(r).Origin.IsEqualApprox(point)), "Placed navigation transform");
        }
    }
    private static void SaveFresh(TileMapLayer layer)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pattern-scene-" + Guid.NewGuid() + ".e2dscene"); using var packed = new PackedScene(); packed.Pack(layer);
        try
        {
            ResourceSaver.Save(packed, path); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var local = (TileMapLayer)loaded.Instantiate(); Check(local.TileSet!.GetPattern(0).GetSize() == new Vector2i(4, 4) && local.TileSet.GetPattern(0).HasCell(new(0, 1)), "Local graph preserves explicit emptiness and margins");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(TilePatternTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_TILE_PATTERNS"); start.Environment.Remove("ELECTRON2D_TEST_TILE_PATTERNS_NATIVE"); start.Environment["ELECTRON2D_TEST_TILE_PATTERNS_FILE"] = path;
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh pattern scene process."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh pattern scene passed"), errors.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
    }
    internal static void RunFile(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var layer = (TileMapLayer)packed.Instantiate(); using var root = new SubViewport(); root.AddChild(layer); using var tree = new SceneTree(root);
        var pattern = layer.TileSet!.GetPattern(0); Check(pattern.GetSize() == new Vector2i(4, 4) && pattern.GetUsedCells().Length == 3 && pattern.GetCellSourceID(new(0, 1)) == -1, "Fresh stored pattern");
        layer.Clear(); layer.SetPattern(new(-4, 5), pattern); tree.FlushDeferred(); NavigationServer.Synchronize(); VerifyConsumers(layer, layer.GetWorld()!);
        Console.WriteLine("Fresh pattern scene passed: typed pattern list, explicit emptiness/margins, executable placement, collision/navigation.");
    }
}
