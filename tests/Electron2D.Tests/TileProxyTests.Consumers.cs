using System.Diagnostics;
using Electron2D;

internal static partial class TileProxyTests
{
    private static void Configure(TileSet set)
    {
        set.SetSourceLevelTileProxy(100, 0);
        set.SetCoordsLevelTileProxy(101, new(4, 4), 0, default);
        set.SetAlternativeLevelTileProxy(102, new(4, 4), 5, 0, default, TileSetAtlasSource.TransformTranspose);
    }
    private static void PaintThree(TileMapLayer layer)
    {
        Paint(layer, new(-2, -2), 100, default, 0);
        Paint(layer, new(-1, -2), 101, new(4, 4), TileSetAtlasSource.TransformFlipH);
        Paint(layer, new(0, -2), 102, new(4, 4), 5);
    }
    private static void Consumers()
    {
        foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU })
        {
            using var library = new TilePatternTests.Library(TileShape.Hexagon, TileLayout.DiamondDown, TileOffsetAxis.Vertical); Configure(library.Set);
            using var world = new World(backend); using var root = new SubViewport { World = world }; var layer = new TileMapLayer { Name = "Proxies", TileSet = library.Set, PhysicsQuadrantSize = 1, Position = new(120, 80), Scale = new(-1.5f, 2), Rotation = .2f }; root.AddChild(layer); using var tree = new SceneTree(root);
            layer.SetCell(new(3, 3), 100, Vector2i.Zero); Check(layer.GetCellSourceID(new(3, 3)) == 100 && layer.GetCellTileData(new(3, 3)) is null, "Raw layer identities remain explicit"); layer.Clear(); PaintThree(layer); tree.FlushDeferred(); NavigationServer.Synchronize(); Verify(layer, world);
            using var pattern = layer.GetPattern(layer.GetUsedCells()); layer.Clear(); layer.SetPattern(new(3, 3), pattern); tree.FlushDeferred(); NavigationServer.Synchronize(); Verify(layer, world);
            for (var i = 0; i < 32; i++) layer.UpdateInternals(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) layer.UpdateInternals(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Prepared mapped tile consumers allocate zero owner bytes");
            using var source = new NavigationMeshSourceGeometryData(); using var target = new NavigationPolygon { AgentRadius = 0 }; NavigationServer.ParseSourceGeometryData(target, source, layer); Check(source.GetTraversableOutlines().Length == 3, "Mapped cells reach baking"); SaveFresh(layer);
            library.Set.ClearTileProxies(); Check(layer.GetUsedCells().All(c => layer.GetCellSourceID(c) == 0), "Table edits do not rewrite already mapped cells"); layer.Clear(); tree.FlushDeferred(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(layer.GetNavigationMap()).Length == 0, "Mapped cell region cleanup");
        }
    }
    private static void Verify(TileMapLayer layer, World world)
    {
        var regions = NavigationServer.MapGetRegions(layer.GetNavigationMap());
        foreach (var coord in layer.GetUsedCells())
        {
            var point = layer.GlobalTransform * layer.MapToLocal(coord); using var query = new PhysicsPointQueryParameters { Position = point };
            Check(world.DirectSpaceState.IntersectPoint(query).Any(h => h.ColliderObject == layer && layer.GetCoordsForBodyRID(h.ColliderRID) == coord), "Mapped destination produces selected-backend tile-owned collision");
            Check(regions.Any(r => NavigationServer.RegionGetOwnerID(r) == layer.InstanceID && NavigationServer.RegionGetTransform(r).Origin.IsEqualApprox(point)), "Mapped destination navigation transform");
        }
    }
    private static void SaveFresh(TileMapLayer layer)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "proxy-scene-" + Guid.NewGuid() + ".e2dscene"); using var packed = new PackedScene(); packed.Pack(layer);
        try
        {
            ResourceSaver.Save(packed, path); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var local = (TileMapLayer)loaded.Instantiate(); Check(local.TileSet!.GetAlternativeLevelTileProxy(102, new(4, 4), 5) == (0, Vector2i.Zero, TileSetAtlasSource.TransformTranspose), "Local proxy graph roundtrip");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(TileProxyTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_TILE_PROXIES"); start.Environment.Remove("ELECTRON2D_TEST_TILE_PROXIES_NATIVE"); start.Environment["ELECTRON2D_TEST_TILE_PROXIES_FILE"] = path;
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh proxy scene process."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh proxy scene passed"), errors.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
    }
    internal static void RunFile(string path)
    {
        using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var layer = (TileMapLayer)loaded.Instantiate(); using var root = new SubViewport(); root.AddChild(layer); using var tree = new SceneTree(root);
        layer.Clear(); PaintThree(layer); tree.FlushDeferred(); NavigationServer.Synchronize(); Check(layer.GetUsedCells().Length == 3 && layer.GetUsedCells().All(c => layer.GetCellSourceID(c) == 0), "Fresh stored proxies execute mapping"); Verify(layer, layer.GetWorld()!);
        Console.WriteLine("Fresh proxy scene passed: all stored proxy levels, explicit remapping, collision and navigation.");
    }
}
