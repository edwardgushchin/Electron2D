using System.Diagnostics;
using Electron2D;

internal static partial class TileTerrainTests
{
    private static void Consumers()
    {
        foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU })
        {
            using var library = new Library(TileShape.Hexagon, TileLayout.DiamondDown, TileOffsetAxis.Vertical, TerrainMode.MatchSides);
            using var polygon = new NavigationPolygon { AgentRadius = 0 }; Vector2[] outline = [new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)]; polygon.SetVertices(outline); polygon.AddPolygon([0, 1, 2, 3]); polygon.AddOutline(outline);
            library.Set.AddPhysicsLayer(); library.Set.AddNavigationLayer();
            for (var tile = 0; tile < library.Atlas.GetTilesCount(); tile++)
            {
                var coords = library.Atlas.GetTileID(tile);
                for (var alternative = 0; alternative < library.Atlas.GetAlternativeTilesCount(coords); alternative++)
                {
                    var data = library.Atlas.GetTileData(coords, library.Atlas.GetAlternativeTileID(coords, alternative)); data.AddCollisionPolygon(0); data.SetCollisionPolygonPoints(0, 0, outline); data.SetNavigationPolygon(0, polygon);
                }
            }
            using var world = new World(backend); using var root = new SubViewport { World = world }; var layer = new TileMapLayer { Name = "Terrain", TileSet = library.Set, PhysicsQuadrantSize = 1, Position = new(120, 90), Scale = new(-1.5f, 2), Rotation = .2f }; root.AddChild(layer); using var tree = new SceneTree(root);
            var a = new Vector2i(-2, 1); var b = layer.GetNeighborCell(a, CellNeighbor.BottomRightSide); var c = layer.GetNeighborCell(b, CellNeighbor.BottomSide); layer.SetCellsTerrainPath([a, b, c], 0, 0); tree.FlushDeferred(); NavigationServer.Synchronize();
            VerifyTerrainConsumers(layer, world, [a, b, c]);
            layer.SetCellsTerrainConnect([b], 0, -1, false); tree.FlushDeferred(); NavigationServer.Synchronize();
            foreach (var coord in layer.GetUsedCells()) VerifyTerrainConsumers(layer, world, [coord]);
            for (var i = 0; i < 32; i++) layer.UpdateInternals(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) layer.UpdateInternals(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Unchanged painted layer consumes zero managed bytes");
            Console.WriteLine($"Terrain consumers {backend}: checking a fresh scene process."); SaveFreshTerrain(layer);
            using var source = new NavigationMeshSourceGeometryData(); using var target = new NavigationPolygon { AgentRadius = 0 }; NavigationServer.ParseSourceGeometryData(target, source, layer); Check(source.GetTraversableOutlines().Length == layer.GetUsedCells().Length, "Painted cells reach root-local baking");
            layer.Clear(); tree.FlushDeferred(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(layer.GetNavigationMap()).Length == 0, "Painted region cleanup");
        }
    }
    private static void VerifyTerrainConsumers(TileMapLayer layer, World world, Vector2i[] coords)
    {
        var regions = NavigationServer.MapGetRegions(layer.GetNavigationMap());
        foreach (var coord in coords)
        {
            var position = layer.GlobalTransform * layer.MapToLocal(coord); using var query = new PhysicsPointQueryParameters { Position = position };
            Check(world.DirectSpaceState.IntersectPoint(query).Any(h => h.ColliderObject == layer && layer.GetCoordsForBodyRID(h.ColliderRID) == coord), "Painted cells generate selected-backend owned collision");
            Check(regions.Any(r => NavigationServer.RegionGetOwnerID(r) == layer.InstanceID && NavigationServer.RegionGetTransform(r).Origin.IsEqualApprox(position)), "Painted cells generate placed navigation regions");
        }
    }
    private static void SaveFreshTerrain(TileMapLayer layer)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-terrain-scene-" + Guid.NewGuid() + ".e2dscene"); using var packed = new PackedScene(); packed.Pack(layer);
        try
        {
            ResourceSaver.Save(packed, path); using var local = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var instance = (TileMapLayer)local.Instantiate(); Check(instance.TileSet!.GetTerrainSetsCount() == 1 && instance.GetUsedCells().OrderBy(c => c.X).ThenBy(c => c.Y).SequenceEqual(layer.GetUsedCells().OrderBy(c => c.X).ThenBy(c => c.Y)), "Local authored terrain scene roundtrip");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(TileTerrainTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_TILE_TERRAINS"); start.Environment.Remove("ELECTRON2D_TEST_TILE_TERRAINS_NATIVE"); start.Environment["ELECTRON2D_TEST_TILE_TERRAINS_FILE"] = path;
            var elapsed = Stopwatch.StartNew();
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            var exited = process.WaitForExit(30000);
            if (!exited)
            {
                process.Kill(true);
                if (!process.WaitForExit(5000)) throw new TimeoutException($"Fresh terrain scene process {process.Id} did not terminate after the 30-second deadline and kill.");
            }
            var stdout = output.GetAwaiter().GetResult(); var stderr = errors.GetAwaiter().GetResult();
            var details = $"Fresh terrain scene process {process.Id}: {elapsed.Elapsed.TotalSeconds:F3} s, exit {process.ExitCode}.\nstdout:\n{stdout}\nstderr:\n{stderr}";
            if (!exited) throw new TimeoutException(details);
            Check(process.ExitCode == 0 && stdout.Contains("Fresh terrain scene passed"), details);
            Console.WriteLine(details);
        }
        finally { System.IO.File.Delete(path); }
    }
    internal static void RunFile(string path)
    {
        var elapsed = Stopwatch.StartNew(); Console.WriteLine("Fresh terrain scene: loading archive.");
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        Console.WriteLine($"Fresh terrain scene: archive loaded at {elapsed.Elapsed.TotalSeconds:F3} s; instantiating.");
        var layer = (TileMapLayer)packed.Instantiate();
        Console.WriteLine($"Fresh terrain scene: instantiated at {elapsed.Elapsed.TotalSeconds:F3} s; attaching consumers.");
        using var root = new SubViewport(); root.AddChild(layer); using var tree = new SceneTree(root); tree.FlushDeferred(); NavigationServer.Synchronize();
        Console.WriteLine($"Fresh terrain scene: consumers attached at {elapsed.Elapsed.TotalSeconds:F3} s; querying and repainting.");
        Check(layer.TileSet!.TileShape == TileShape.Hexagon && layer.TileSet.TileLayout == TileLayout.DiamondDown && layer.TileSet.GetTerrainSetMode(0) == TerrainMode.MatchSides && layer.GetUsedCells().Length > 0, "Fresh geometry/terrain definitions and cells");
        VerifyTerrainConsumers(layer, layer.GetWorld()!, layer.GetUsedCells()); var coord = layer.GetUsedCells()[0]; layer.SetCellsTerrainConnect([coord], 0, 0); tree.FlushDeferred(); NavigationServer.Synchronize(); VerifyTerrainConsumers(layer, layer.GetWorld()!, [coord]);
        Console.WriteLine("Fresh terrain scene passed: complete definitions/atlas patterns, executable re-painting, physics and navigation.");
    }
}
