using Electron2D;

internal static class TileNavigationTests
{
    internal static NavigationPolygon Square()
    {
        var p = new NavigationPolygon { AgentRadius = 0 }; var points = NavigationBakingTests.Rect(-8, -8, 16, 16); p.SetVertices(points); p.AddPolygon([0, 1, 2, 3]); p.AddOutline(points); return p;
    }
    internal static void Run()
    {
        Resources(); Scene(); Console.WriteLine("Tile navigation passed: masks/remapping, transforms/cache lifetime, shared resource graphs, runtime data, maps/owners, baking and zero-allocation unchanged updates.");
    }
    private static void Resources()
    {
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image); using var set = new TileSet(); using var atlas = new TileSetAtlasSource { Texture = texture }; using var polygon = Square();
        set.AddNavigationLayer(); set.AddSource(atlas, 0); atlas.CreateTile(default); var data = atlas.GetTileData(default, 0); data.SetNavigationPolygon(0, polygon);
        Check(set.GetNavigationLayersCount() == 1 && set.GetNavigationLayerLayers(0) == 1, "Navigation defaults");
        set.SetNavigationLayerLayerValue(0, 32, true); set.SetNavigationLayerLayerValue(0, 1, false); Check(set.GetNavigationLayerLayers(0) == 0x80000000 && set.GetNavigationLayerLayerValue(0, 32), "Unsigned last membership bit");
        Reject<ArgumentOutOfRangeException>(() => set.SetNavigationLayerLayerValue(0, 0, true)); Reject<ArgumentOutOfRangeException>(() => set.GetNavigationLayerLayerValue(0, 33));
        set.AddNavigationLayer(0); Check(data.GetNavigationPolygon(0) is null && data.GetNavigationPolygon(1) == polygon, "Insertion remaps polygons");
        set.MoveNavigationLayer(1, 0); set.RemoveNavigationLayer(1); Check(data.GetNavigationPolygon(0) == polygon && set.GetNavigationLayerLayers(0) == 0x80000000, "Movement/removal preserve mask and polygon");
        for (var flags = 1; flags < 8; flags++)
        {
            var h = (flags & 1) != 0; var v = (flags & 2) != 0; var t = (flags & 4) != 0;
            var copy = data.GetNavigationPolygon(0, h, v, t)!; Check(copy == data.GetNavigationPolygon(0, h, v, t), "Cached transform identity");
            var vertices = copy.GetVertices(); var expected = polygon.GetVertices()[0]; if (t) expected = new(expected.Y, expected.X); if (h) expected.X = -expected.X; if (v) expected.Y = -expected.Y;
            Check(vertices[0] == expected && copy.AgentRadius == 0 && copy.GetOutlineCount() == 1, "Transformed geometry/settings");
            var indices = copy.GetPolygon(0); var area = 0f; for (var i = 0; i < indices.Length; i++) area += vertices[indices[i]].Cross(vertices[indices[(i + 1) % indices.Length]]); Check(area > 0, "Reflection preserves polygon winding");
        }
        var stale = data.GetNavigationPolygon(0, true)!; polygon.AgentRadius = 2; Check(stale.IsDisposed && data.GetNavigationPolygon(0, true)!.AgentRadius == 2, "Source edits invalidate transformed caches");
        var alt = atlas.CreateAlternativeTile(default); atlas.GetTileData(default, alt).SetNavigationPolygon(0, polygon);
        using var shallow = (TileSet)set.Duplicate(); Check(((TileSetAtlasSource)shallow.GetSource(0)).GetTileData(default, 0).GetNavigationPolygon(0) == polygon, "Shallow graph borrows polygon");
        using var deep = (TileSet)set.Duplicate(true); var deepAtlas = (TileSetAtlasSource)deep.GetSource(0); var deepPolygon = deepAtlas.GetTileData(default, 0).GetNavigationPolygon(0); Check(deepPolygon != polygon && deepPolygon == deepAtlas.GetTileData(default, alt).GetNavigationPolygon(0), "Deep graph duplicates shared polygon once");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tile-navigation-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(set, path); using var loaded = ResourceLoader.Load<TileSet>(path, ResourceLoader.CacheMode.Ignore); var loadedAtlas = (TileSetAtlasSource)loaded!.GetSource(0);
            var loadedPolygon = loadedAtlas.GetTileData(default, 0).GetNavigationPolygon(0); Check(loaded.GetNavigationLayerLayers(0) == 0x80000000 && loadedPolygon?.GetPolygonCount() == 1 && loadedPolygon == loadedAtlas.GetTileData(default, alt).GetNavigationPolygon(0), "Portable resource graph retains masks, geometry and aliasing");
        }
        finally { File.Delete(path); }
        polygon.Dispose(); Check(data.GetNavigationPolygon(0) is null, "Disposed source polygon clears borrowed data");
        using var otherSet = new TileSet(); otherSet.AddSource(atlas); Check(data.Navigation.Count == 0, "Source transfer resizes navigation data");
    }
    private sealed class RuntimeLayer : TileMapLayer
    {
        internal NavigationPolygon? Replacement;
        protected override bool UseTileDataRuntimeUpdate(Vector2i coords) => coords == Vector2i.Zero && Replacement is not null;
        protected override void TileDataRuntimeUpdate(Vector2i coords, TileData tileData) => tileData.SetNavigationPolygon(0, Replacement);
    }
    private static void Scene()
    {
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image); using var set = new TileSet(); using var atlas = new TileSetAtlasSource { Texture = texture }; using var polygon = Square(); set.AddNavigationLayer(); set.AddSource(atlas, 0); atlas.CreateTile(default); atlas.GetTileData(default, 0).SetNavigationPolygon(0, polygon);
        using var world = new World(); using var nextWorld = new World(); using var root = new SubViewport { World = world };
        var layer = new RuntimeLayer { TileSet = set }; root.AddChild(layer); for (var x = 0; x < 3; x++) layer.SetCell(new(x, 0), 0, new Vector2i(0, 0));
        using var tree = new SceneTree(root); tree.FlushDeferred(); NavigationServer.Synchronize(); var map = layer.GetNavigationMap(); var regions = NavigationServer.MapGetRegions(map);
        Check(regions.Length == 3 && layer.GetChildCount() == 0 && regions.All(r => NavigationServer.RegionGetOwnerID(r) == layer.InstanceID), "One node-owned region per occupied layer, no hidden nodes");
        Reject<InvalidOperationException>(() => NavigationServer.FreeRID(regions[0])); Check(NavigationServer.MapGetPath(map, new(1, 8), new(47, 8), true).Last() == new Vector2(47, 8), "Route crosses adjacent tiles");
        tree.DebugNavigationHint = true; layer.NavigationVisibilityMode = TileMapLayer.DebugVisibilityMode.ForceHide; Check(tree.DebugNavigationHint, "Diagnostic property descriptor");
        layer.Hide(); tree.FlushDeferred(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(map).Length == 3, "Hidden drawing retains navigation"); layer.Show(); tree.FlushDeferred();
        regions = NavigationServer.MapGetRegions(map); layer.Position = new(20, 10); layer.Scale = new(2, 1); layer.ForceUpdateTransform(); tree.FlushDeferred(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetClosestPoint(map, new(22, 18)) == new Vector2(22, 18), "Scale/translation update world geometry");
        var transformedRegions = NavigationServer.MapGetRegions(map); layer.Position += new Vector2(1, 0); layer.ForceUpdateTransform(); Check(transformedRegions.SequenceEqual(NavigationServer.MapGetRegions(map)), "Transform updates preserve identities");
        root.World = nextWorld; tree.FlushDeferred(); NavigationServer.Synchronize(); var nextMap = layer.GetNavigationMap(); Check(nextMap != map && NavigationServer.MapGetRegions(map).Length == 0 && NavigationServer.MapGetRegions(nextMap).Length == 3, "World replacement moves all regions");
        var explicitMap = NavigationServer.MapCreate(); NavigationServer.MapSetActive(explicitMap, true);
        try
        {
            layer.SetNavigationMap(explicitMap); tree.FlushDeferred(); NavigationServer.Synchronize(); Check(layer.GetNavigationMap() == explicitMap && NavigationServer.MapGetRegions(nextMap).Length == 0 && NavigationServer.MapGetRegions(explicitMap).Length == 3, "Explicit borrowed map override");
            Reject<ArgumentException>(() => layer.SetNavigationMap(transformedRegions[0])); Check(layer.GetNavigationMap() == explicitMap, "Rejected map preserves override");
            set.SetNavigationLayerLayers(0, 2); tree.FlushDeferred(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetPath(explicitMap, new(22, 18), new(100, 18), true, 1).Length == 0 && NavigationServer.MapGetPath(explicitMap, new(22, 18), new(100, 18), true, 2).Length > 0, "Navigation mask filters queries");
            layer.NavigationEnabled = false; tree.FlushDeferred(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(explicitMap).Length == 0, "Navigation disable releases regions");
            layer.NavigationEnabled = true; layer.EraseCell(new(1, 0)); tree.FlushDeferred(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(explicitMap).Length == 2, "Cell erasure releases its region");
            using var empty = new NavigationPolygon(); layer.Replacement = empty; layer.NotifyRuntimeTileDataUpdate(); tree.FlushDeferred(); NavigationServer.Synchronize(); Check(NavigationServer.MapGetRegions(explicitMap).Length == 1 && layer.GetCellTileData(default)!.GetNavigationPolygon(0) == polygon, "Runtime copy overrides navigation without mutating source");
            layer.Replacement = null; layer.NotifyRuntimeTileDataUpdate(); tree.FlushDeferred(); NavigationServer.Synchronize();
            using var source = new NavigationMeshSourceGeometryData(); using var baked = new NavigationPolygon { AgentRadius = 0, ParsedGeometry = NavigationPolygon.ParsedGeometryType.MeshInstances }; NavigationServer.ParseSourceGeometryData(baked, source, layer); Check(source.GetTraversableOutlines().Length == 2 && source.GetBounds().Position == Vector2.Zero, "Root-local traversable parsing in mesh-only mode"); NavigationServer.BakeFromSourceGeometryData(baked, source); Check(baked.GetPolygonCount() == 2, "Tile outlines bake to actual disconnected geometry");
            polygon.ClearOutlines(); NavigationServer.ParseSourceGeometryData(baked, source, layer); Check(source.GetTraversableOutlines().Length == 2, "Polygon faces supply traversable geometry without outlines");
            for (var i = 0; i < 32; i++) layer.UpdateInternals(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) layer.UpdateInternals(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Unchanged tile updates allocate zero managed bytes");
            layer.Enabled = false; tree.FlushDeferred(); Check(NavigationServer.MapGetRegions(explicitMap).Length == 0, "Layer disable removes navigation"); layer.Enabled = true; tree.FlushDeferred(); root.RemoveChild(layer); Check(NavigationServer.MapGetRegions(explicitMap).Length == 0, "Tree exit releases region ownership");
        }
        finally { layer.SetNavigationMap(default); layer.Dispose(); NavigationServer.FreeRID(explicitMap); }
        using var detached = new TileMapLayer(); Check(!detached.GetNavigationMap().IsValid(), "Detached default map is empty");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
