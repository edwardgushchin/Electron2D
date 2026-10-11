using System.Diagnostics;
using Electron2D;

internal static partial class TileCustomDataTests
{
    private readonly record struct AppValue(int Code);
    private static void PrepareCodec()
    { if (!ResourceFileTypes.HasCodec(typeof(AppValue))) ResourceFileTypes.RegisterValueCodec<AppValue>(static (s, v) => s.Put32(v.Code), static s => new AppValue(s.Get32())); }
    private sealed class Layer : TileMapLayer
    {
        protected override bool UseTileDataRuntimeUpdate(Vector2i coords) => true;
        protected override void TileDataRuntimeUpdate(Vector2i coords, TileData tileData)
        {
            tileData.Modulate = tileData.GetCustomData<Color>("Tint");
            if (!tileData.GetCustomData<bool>("Solid")) { tileData.SetCollisionPolygonsCount(0, 0); tileData.SetNavigationPolygon(0, null); }
            tileData.SetCustomData("Score", tileData.GetCustomData<long>("Score") + coords.X);
        }
    }
    private static Layer CreateLayer() => new();
    private static void PrepareLayerFactory()
    { PrepareCodec(); try { ResourceFileTypes.Get(typeof(Layer)); } catch (NotSupportedException) { ResourceFileTypes.RegisterNode("CustomDataFixtureLayer", CreateLayer); } }
    private static void Configure(TileSet set, TileSetAtlasSource atlas)
    {
        set.AddCustomDataLayer(); set.SetCustomDataLayerName(0, "Tint"); set.SetCustomDataLayerType<Color>(0);
        set.AddCustomDataLayer(); set.SetCustomDataLayerName(1, "Solid"); set.SetCustomDataLayerType<bool>(1);
        set.AddCustomDataLayer(); set.SetCustomDataLayerName(2, "Score"); set.SetCustomDataLayerType<long>(2);
        var data = atlas.GetTileData(default, 0); data.SetCustomData("Tint", new Color(.2f, .6f, .85f)); data.SetCustomData("Solid", true); data.SetCustomData("Score", 100L);
        PrepareCodec(); set.AddCustomDataLayer(); set.SetCustomDataLayerName(3, "Application"); set.SetCustomDataLayerType(3, typeof(AppValue)); data.SetCustomData("Application", new AppValue(17));
    }
    private static void Consumers()
    {
        PrepareLayerFactory();
        foreach (var backend in new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU })
        {
            using var library = new TilePatternTests.Library(TileShape.Square, TileLayout.Stacked, TileOffsetAxis.Horizontal); Configure(library.Set, library.Atlas);
            using var world = new World(backend); using var root = new SubViewport { World = world }; var layer = new Layer { Name = "Custom", TileSet = library.Set, PhysicsQuadrantSize = 1 }; root.AddChild(layer); using var tree = new SceneTree(root);
            layer.SetCell(new(1, 0), 0, Vector2i.Zero); layer.SetCell(new(2, 0), 0, Vector2i.Zero); tree.FlushDeferred(); NavigationServer.Synchronize(); VerifyCustomConsumers(layer, world, true);
            Check(library.Atlas.GetTileData(default, 0).GetCustomData<long>("Score") == 100, "Runtime callback value edits do not mutate authored values");
            SaveFreshCustom(layer);
            library.Atlas.GetTileData(default, 0).SetCustomData("Solid", false); tree.FlushDeferred(); NavigationServer.Synchronize(); VerifyCustomConsumers(layer, world, false);
            for (var i = 0; i < 32; i++) layer.UpdateInternals(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) layer.UpdateInternals(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Prepared custom-data layer updates allocate zero owner bytes");
        }
    }
    private static void VerifyCustomConsumers(TileMapLayer layer, World world, bool solid)
    {
        Check(NavigationServer.MapGetRegions(layer.GetNavigationMap()).Length == (solid ? layer.GetUsedCells().Length : 0), "Custom Solid controls actual navigation regions");
        foreach (var coord in layer.GetUsedCells())
        {
            using var query = new PhysicsPointQueryParameters { Position = layer.GlobalTransform * layer.MapToLocal(coord) };
            Check(world.DirectSpaceState.IntersectPoint(query).Any(h => h.ColliderObject == layer) == solid, "Custom Solid controls selected-backend collision");
        }
    }
    private static void SaveFreshCustom(Layer layer)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "custom-scene-" + Guid.NewGuid() + ".e2dscene"); using var packed = new PackedScene(); packed.Pack(layer);
        try
        {
            ResourceSaver.Save(packed, path); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var local = (Layer)loaded.Instantiate(); Check(local.TileSet!.GetCustomDataLayerByName("Tint") == 0 && local.TileSet.GetCustomDataLayerType(0) == typeof(Color), "Local custom schema graph");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(TileCustomDataTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_TILE_CUSTOM"); start.Environment.Remove("ELECTRON2D_TEST_TILE_CUSTOM_NATIVE"); start.Environment["ELECTRON2D_TEST_TILE_CUSTOM_FILE"] = path;
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh custom-data scene process."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh custom-data scene passed"), errors.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
    }
    internal static void RunFile(string path)
    {
        PrepareLayerFactory(); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var layer = (Layer)loaded.Instantiate(); using var root = new SubViewport(); root.AddChild(layer); using var tree = new SceneTree(root); tree.FlushDeferred(); NavigationServer.Synchronize(); VerifyCustomConsumers(layer, layer.GetWorld()!, true);
        var data = ((TileSetAtlasSource)layer.TileSet!.GetSource(0)).GetTileData(default, 0); Check(data.GetCustomData<Color>("Tint").IsEqualApprox(new(.2f, .6f, .85f)) && data.GetCustomData<long>("Score") == 100, "Fresh actual typed values");
        Check(data.GetCustomData<AppValue>("Application") == new AppValue(17), "Fresh explicit application codec/type factory");
        data.SetCustomData("Solid", false); tree.FlushDeferred(); NavigationServer.Synchronize(); VerifyCustomConsumers(layer, layer.GetWorld()!, false); Console.WriteLine("Fresh custom-data scene passed: definitions/values, executable runtime rules, collision/navigation.");
    }
}
