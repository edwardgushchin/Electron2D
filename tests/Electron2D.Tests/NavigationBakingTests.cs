using Electron2D;

internal static class NavigationBakingTests
{
    internal static Vector2[] Rect(float x, float y, float width, float height) => [new(x, y), new(x + width, y), new(x + width, y + height), new(x, y + height)];
    internal static void Run()
    {
        using var polygon = new NavigationPolygon { AgentRadius = 0 }; polygon.AddOutline(Rect(0, 0, 100, 100)); using var source = new NavigationMeshSourceGeometryData(); source.AddObstructionOutline(Rect(40, 30, 20, 40));
        Check(!source.HasData() && source.GetBounds() == new Rect2(40, 30, 20, 40), "Obstruction-only data/bounds.");
        var input = Rect(1, 2, 3, 4); source.AddTraversableOutline(input); input[0] = new(999, 999); Check(source.GetTraversableOutlines()[0][0] == new Vector2(1, 2), "Source owns copied arrays."); source.SetTraversableOutlines([]);
        var callbacks = 0; NavigationServer.BakeFromSourceGeometryData(polygon, source, () => { Check(!NavigationServer.IsBakingNavigationPolygon(polygon), "Callback follows completion."); callbacks++; });
        Check(callbacks == 1 && polygon.GetPolygonCount() > 1, "Convex partition creates an actual hole-bearing surface.");
        var map = NavigationServer.MapCreate(); var region = NavigationServer.RegionCreate();
        try
        {
            NavigationServer.MapSetActive(map, true); NavigationServer.RegionSetMap(region, map); NavigationServer.RegionSetNavigationPolygon(region, polygon); NavigationServer.Synchronize();
            var path = NavigationServer.MapGetPath(map, new(10, 50), new(90, 50), true); Check(path.Length >= 3 && path.Any(p => p.Y <= 30 || p.Y >= 70), "Baked routing avoids the blocked middle.");
            Check(NavigationServer.MapGetClosestPoint(map, new(50, 50)) != new Vector2(50, 50), "The hole is not walkable.");
            polygon.SamplePartition = NavigationPolygon.SamplePartitionType.Triangulate; NavigationServer.BakeFromSourceGeometryData(polygon, source); Check(Enumerable.Range(0, polygon.GetPolygonCount()).All(i => polygon.GetPolygon(i).Length == 3), "Triangulate has a distinct polygon consumer.");
            polygon.CellSize = 2; Reject<InvalidOperationException>(NavigationServer.Synchronize); NavigationServer.MapSetCellSize(map, 2); NavigationServer.Synchronize();
        }
        finally { NavigationServer.FreeRID(region); NavigationServer.FreeRID(map); }
        using var clipped = new NavigationPolygon { AgentRadius = 2, BakingRect = new(.25f, .5f, 50, 50), BorderSize = 3 }; clipped.AddOutline(Rect(-10, -10, 100, 100));
        using var empty = new NavigationMeshSourceGeometryData(); NavigationServer.BakeFromSourceGeometryData(clipped, empty); var vertices = clipped.GetVertices(); Check(vertices.Min(p => p.X) >= 3.25f && vertices.Min(p => p.Y) >= 3.5f && vertices.Max(p => p.X) <= 47.25f, "Fractional clipping, clearance and border produce finite geometry.");
        var previous = clipped.GetVertices(); using var invalid = new NavigationMeshSourceGeometryData(); invalid.AddObstructionOutline([new(1e30f, 0), new(1e30f, 1), new(0, 1)]); Reject<ArgumentOutOfRangeException>(() => NavigationServer.BakeFromSourceGeometryData(clipped, invalid)); Check(clipped.GetVertices().SequenceEqual(previous), "Failed preparation preserves previous geometry.");
        SourceAndScene(); MeshesAndTiles(); Async(polygon, source); Edges(); Storage(polygon, source);
        Console.WriteLine("Navigation baking: source ownership, holes, routing, partition, clearance/border, failures, scene parsing, async publication and storage passed.");
    }
    private static void SourceAndScene()
    {
        using var polygon = new NavigationPolygon { AgentRadius = 0 }; polygon.AddOutline(Rect(0, 0, 100, 100)); using var source = new NavigationMeshSourceGeometryData(); using var root = new Entity { Position = new(100, 200) };
        var mesh = new Polygon { Vertices = Rect(0, 0, 10, 10), Position = new(20, 30), Offset = new(2, 3) }; root.AddChild(mesh);
        var body = new StaticBody { Position = new(50, 60), CollisionLayer = 2 }; using var shape = new RectangleShape { Size = new(8, 8) }; body.AddChild(new CollisionShape { Shape = shape, Position = new(3, 4) }); root.AddChild(body);
        NavigationServer.ParseSourceGeometryData(polygon, source, root); Check(source.GetObstructionOutlines().Length == 2 && source.GetBounds().HasPoint(new(22, 33)), "Detached owner geometry uses root-local transforms, offsets and shape owners.");
        polygon.ParsedCollisionMask = 1; NavigationServer.ParseSourceGeometryData(polygon, source, root); Check(source.GetObstructionOutlines().Length == 1, "Collision mask filters actual body geometry.");
        var parser = NavigationServer.SourceGeometryParserCreate(); var seen = 0; NavigationServer.SourceGeometryParserSetCallback(parser, (_, data, node) => { if (node == root) { seen++; data.AddTraversableOutline(Rect(0, 0, 100, 100)); Reject<InvalidOperationException>(() => NavigationServer.ParseSourceGeometryData(polygon, data, root)); } });
        try { NavigationServer.ParseSourceGeometryData(polygon, source, root); Check(seen == 1 && source.HasData(), "Typed parser registration and same-target reentry rejection."); }
        finally { NavigationServer.FreeRID(parser); }
        using var region = new NavigationRegion { NavigationPolygon = polygon }; var finished = 0; region.BakeFinished += () => finished++; region.BakeNavigationPolygon(false); Check(finished == 1 && !region.IsBaking(), "Region integrates parsing, publication and completion.");
    }
    private static void Async(NavigationPolygon polygon, NavigationMeshSourceGeometryData source)
    {
        using var root = new Node(); using var tree = new SceneTree(root); var owner = Environment.CurrentManagedThreadId; var completed = 0;
        NavigationServer.BakeFromSourceGeometryDataAsync(polygon, source, () => { Check(Environment.CurrentManagedThreadId == owner, "Owner callback."); completed++; }, tree);
        Reject<InvalidOperationException>(() => NavigationServer.BakeFromSourceGeometryDataAsync(polygon, source, publicationTree: tree));
        Check(SpinWait.SpinUntil(() => !NavigationServer.IsBakingNavigationPolygon(polygon), TimeSpan.FromSeconds(10)) && completed == 1, "Owner polls publish async geometry and retire the task.");
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) Check(!NavigationServer.IsBakingNavigationPolygon(polygon), "Stable idle status."); Check(bytes == GC.GetAllocatedBytesForCurrentThread(), "Warm completed status allocates zero managed bytes.");
    }
    private static void MeshesAndTiles()
    {
        using var polygon = new NavigationPolygon(); using var data = new NavigationMeshSourceGeometryData(); using var root = new Entity();
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = Rect(0, 0, 4, 4), Indices = [0, 1, 2, 0, 2, 3] }); root.AddChild(new MeshInstance { Mesh = mesh, Position = new(10, 20) });
        using var multi = new MultiMesh { Mesh = mesh, InstanceCount = 2, VisibleInstanceCount = 1 }; multi.SetInstanceTransform2D(0, new Transform(0, new(30, 40))); multi.SetInstanceTransform2D(1, new Transform(0, new(300, 400))); root.AddChild(new MultiMeshInstance { MultiMesh = multi });
        NavigationServer.ParseSourceGeometryData(polygon, data, root); Check(data.GetObstructionOutlines().Length == 4 && data.GetBounds() == new Rect2(10, 20, 24, 24), "Mesh triangles and visible transformed MultiMesh instances have concrete source consumers.");
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image); using var atlas = new TileSetAtlasSource { Texture = texture }; using var set = new TileSet(); set.AddPhysicsLayer(); set.AddSource(atlas, 3); atlas.CreateTile(default);
        var tile = atlas.GetTileData(default, 0); tile.AddCollisionPolygon(0); tile.SetCollisionPolygonPoints(0, 0, Rect(1, 2, 3, 4)); var tiles = new TileMapLayer { TileSet = set }; tiles.SetCell(default, 3, new Vector2i(0, 0), TileSetAtlasSource.TransformTranspose | TileSetAtlasSource.TransformFlipH); root.AddChild(tiles);
        polygon.ParsedGeometry = NavigationPolygon.ParsedGeometryType.StaticColliders; NavigationServer.ParseSourceGeometryData(polygon, data, root); Check(data.GetObstructionOutlines().Length == 1, "Tile collision metadata participates in scene baking.");
        var points = data.GetObstructionOutlines()[0]; var center = tiles.MapToLocal(default); Check(points.Any(p => p.IsEqualApprox(center + new Vector2(-2, 1))), "Tile transpose/reflection matches the physical source transform.");
    }
    private static void Storage(NavigationPolygon polygon, NavigationMeshSourceGeometryData source)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-nav-bake-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(polygon, path); using var loaded = ResourceLoader.Load<NavigationPolygon>(path, ResourceLoader.CacheMode.IgnoreDeep); Check(loaded.GetOutlineCount() == 1 && loaded.CellSize == 2 && loaded.SamplePartition == NavigationPolygon.SamplePartitionType.Triangulate && loaded.GetVertices().SequenceEqual(polygon.GetVertices()), "Baked geometry/configuration/outlines survive file reconstruction.");
            source.AddProjectedObstruction(Rect(4, 5, 6, 7), true); ResourceSaver.Save(source, path); using var data = ResourceLoader.Load<NavigationMeshSourceGeometryData>(path, ResourceLoader.CacheMode.IgnoreDeep); Check(data.GetProjectedObstructions()[0].Carve && data.GetObstructionOutlines().Length == 1, "Typed source geometry file registration.");
        }
        finally { File.Delete(path); }
    }
    private static void Edges()
    {
        using var polygon = new NavigationPolygon { AgentRadius = 5 }; polygon.AddOutline(Rect(0, 0, 100, 100)); using var source = new NavigationMeshSourceGeometryData(); source.AddProjectedObstruction(Rect(40, 40, 20, 20), false);
        var map = NavigationServer.MapCreate(); var region = NavigationServer.RegionCreate();
        try
        {
            NavigationServer.MapSetActive(map, true); NavigationServer.RegionSetMap(region, map); NavigationServer.RegionSetNavigationPolygon(region, polygon);
            NavigationServer.BakeFromSourceGeometryData(polygon, source); NavigationServer.Synchronize(); Check(NavigationServer.MapGetClosestPoint(map, new(37, 50)) != new Vector2(37, 50), "Non-carve obstruction expands by agent radius.");
            source.ClearProjectedObstructions(); source.AddProjectedObstruction(Rect(40, 40, 20, 20), true); NavigationServer.BakeFromSourceGeometryData(polygon, source); NavigationServer.Synchronize(); Check(NavigationServer.MapGetClosestPoint(map, new(37, 50)).IsEqualApprox(new(37, 50)), "Carve obstruction bypasses radius expansion.");
            polygon.ClearOutlines(); var old = polygon.GetVertices(); source.Clear(); NavigationServer.BakeFromSourceGeometryData(polygon, source); Check(polygon.GetVertices().SequenceEqual(old), "No traversable input retains authored geometry.");
            polygon.AddOutline(Rect(0, 0, 2, 2)); NavigationServer.BakeFromSourceGeometryData(polygon, source); Check(polygon.GetPolygonCount() == 0, "Complete erosion publishes an empty surface.");
        }
        finally { NavigationServer.FreeRID(region); NavigationServer.FreeRID(map); }
        using var root = new Entity(); using var tree = new SceneTree(root); var parent = new Entity(); var child = new Polygon { Vertices = Rect(0, 0, 4, 4) }; parent.AddChild(child); root.AddChild(parent); parent.AddToGroup("bake");
        polygon.SourceGeometryGroupName = "bake"; polygon.SourceGeometry = NavigationPolygon.SourceGeometryMode.GroupsExplicit; NavigationServer.ParseSourceGeometryData(polygon, source, root); Check(source.GetObstructionOutlines().Length == 0, "Explicit groups omit descendants.");
        polygon.SourceGeometry = NavigationPolygon.SourceGeometryMode.GroupsWithChildren; NavigationServer.ParseSourceGeometryData(polygon, source, root); Check(source.GetObstructionOutlines().Length == 1, "Grouped root traversal includes descendants.");
        var obstruction = new NavigationObstacle { AffectNavigationMesh = true, CarveNavigationMesh = true, Vertices = Rect(10, 10, 4, 4), Radius = 2 }; root.AddChild(obstruction); polygon.SourceGeometry = NavigationPolygon.SourceGeometryMode.RootNodeChildren; NavigationServer.ParseSourceGeometryData(polygon, source, root); Check(source.GetProjectedObstructions().Length == 2 && source.GetProjectedObstructions().All(p => p.Carve), "Obstacle contour/disc have actual bake consumers.");
        polygon.ClearOutlines(); polygon.AddOutline(Rect(0, 0, 100, 100)); polygon.AgentRadius = 0; source.Clear();
        var failureTree = new SceneTree(new Node()); NavigationServer.BakeFromSourceGeometryDataAsync(polygon, source, publicationTree: failureTree); failureTree.Dispose();
        Check(SpinWait.SpinUntil(() => { try { return !NavigationServer.IsBakingNavigationPolygon(polygon); } catch (ObjectDisposedException) { return true; } }, TimeSpan.FromSeconds(10)), "Closed owner retires async work.");
        using var bad = new NavigationMeshSourceGeometryData(); bad.AddObstructionOutline([new(1e30f, 0), new(1e30f, 1), new(0, 1)]); var before = polygon.GetVertices(); NavigationServer.BakeFromSourceGeometryDataAsync(polygon, bad);
        Exception? captured = null; Check(SpinWait.SpinUntil(() => { try { return !NavigationServer.IsBakingNavigationPolygon(polygon); } catch (Exception error) { captured = error; return true; } }, TimeSpan.FromSeconds(10)) && captured is ArgumentOutOfRangeException && polygon.GetVertices().SequenceEqual(before), "Worker failure surfaces once and preserves data.");
        NavigationServer.BakeFromSourceGeometryData(polygon, source, () => NavigationServer.BakeFromSourceGeometryData(polygon, source)); Check(!NavigationServer.IsBakingNavigationPolygon(polygon), "Completion may start later work.");
        Reject<InvalidOperationException>(() => NavigationServer.BakeFromSourceGeometryData(polygon, source, () => throw new InvalidOperationException("callback"))); Check(!NavigationServer.IsBakingNavigationPolygon(polygon) && polygon.GetPolygonCount() != 0, "Callback failure follows commit and releases active state.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
