using Electron2D;

internal static class NavigationTests
{
    internal static NavigationPolygon Rectangle(float x, float y, float width, float height)
    { var p = new NavigationPolygon(); p.SetVertices([new(x, y), new(x + width, y), new(x + width, y + height), new(x, y + height)]); p.AddPolygon([0, 1, 2, 3]); return p; }
    internal static void Run()
    {
        using var left = Rectangle(0, 0, 10, 20); using var bridge = Rectangle(10, 0, 10, 10); using var right = Rectangle(20, 0, 10, 20);
        left.Clear(); left.SetVertices([new(0, 0), new(10, 0), new(10, 10), new(0, 10), new(10, 20), new(0, 20)]); left.AddPolygon([0, 1, 2, 3]); left.AddPolygon([3, 2, 4, 5]);
        right.Clear(); right.SetVertices([new(20, 0), new(30, 0), new(30, 10), new(20, 10), new(30, 20), new(20, 20)]); right.AddPolygon([0, 1, 2, 3]); right.AddPolygon([3, 2, 4, 5]);
        using var duplicate = (NavigationPolygon)left.Duplicate(); left.GetVertices()[0] = new(99, 99); Check(duplicate.GetVertices()[0] == Vector2.Zero && left.GetVertices()[0] == Vector2.Zero, "Independent public arrays and immutable resource copies.");
        Reject<ArgumentException>(() => left.AddPolygon([0, 1, 8])); Reject<ArgumentException>(() => left.SetVertices([new(float.NaN, 0)])); Check(left.GetPolygonCount() == 2, "Rejected edits preserve geometry.");
        using var star = new NavigationPolygon(); star.SetVertices([new(0, -10), new(6, 8), new(-9, -3), new(9, -3), new(-6, 8)]); Reject<ArgumentException>(() => star.AddPolygon([0, 1, 2, 3, 4]));
        var map = NavigationServer.MapCreate(); var a = NavigationServer.RegionCreate(); var b = NavigationServer.RegionCreate(); var c = NavigationServer.RegionCreate();
        Check(!NavigationServer.MapIsActive(map) && NavigationServer.MapGetUseEdgeConnections(map) && NavigationServer.MapGetEdgeConnectionMargin(map) == 1 && NavigationServer.RegionGetEnabled(a) && NavigationServer.RegionGetUseEdgeConnections(a) && NavigationServer.RegionGetNavigationLayers(a) == 1 && NavigationServer.RegionGetEnterCost(a) == 0 && NavigationServer.RegionGetTravelCost(a) == 1 && NavigationServer.RegionGetTransform(a) == Transform.Identity, "Map and region defaults match authored topology profile.");
        NavigationServer.RegionSetOwnerID(a, 123); Check(NavigationServer.RegionGetOwnerID(a) == 123, "Logical scene owner identity round-trips.");
        NavigationServer.MapSetActive(map, true); Bind(a, left); Bind(b, bridge); Bind(c, right);
        Check(NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true).Length == 0 && NavigationServer.MapGetIterationID(map) == 0, "Topology is deferred until synchronization.");
        NavigationServer.Synchronize(); var path = NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true);
        NavigationServer.MapSetUseEdgeConnections(map, false); NavigationServer.RegionSetUseEdgeConnections(a, false); NavigationServer.Synchronize(); Check(NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true).SequenceEqual(path), "Exact shared edges remain connected when margin flags are disabled.");
        NavigationServer.MapSetUseEdgeConnections(map, true); NavigationServer.RegionSetUseEdgeConnections(a, true); NavigationServer.Synchronize();
        Check(path.Length >= 3 && path[0] == new Vector2(5, 15) && path[^1] == new Vector2(25, 15), "Portal corridor routes around missing lower bridge.");
        Check(path.SequenceEqual(new Vector2[] { new(5, 15), new(10, 10), new(20, 10), new(25, 15) }), "Funnel chooses the two obstacle-adjacent corridor corners.");
        Check(path.Skip(1).SkipLast(1).Any(p => p.Y <= 10), "Optimized path bends at corridor boundary.");
        var raw = NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), false); Check(raw.Length >= 4, "Unoptimized route retains crossed portal midpoints.");
        Check(NavigationServer.MapGetClosestPoint(map, new(-3, 5)) == new Vector2(0, 5) && NavigationServer.MapGetClosestPointOwner(map, new(25, 5)) == c && NavigationServer.RegionOwnsPoint(c, new(25, 5)), "Map projection and region ownership.");
        var iteration = NavigationServer.MapGetIterationID(map); NavigationServer.RegionSetEnabled(b, false); Check(NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true)[^1] == new Vector2(25, 15), "Staged edits preserve published iteration."); NavigationServer.Synchronize(); path = NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true); Check(path[^1].X <= 10 && NavigationServer.MapGetIterationID(map) > iteration, "Disconnected route ends at closest reachable boundary.");
        NavigationServer.RegionSetEnabled(b, true); NavigationServer.RegionSetNavigationLayers(b, 2); NavigationServer.Synchronize(); Check(NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true, 1)[^1].X <= 10 && NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true, 3)[^1].X == 25, "Layer masks filter traversal.");
        Reject<ArgumentOutOfRangeException>(() => NavigationServer.RegionSetTravelCost(b, float.NaN)); Reject<ArgumentException>(() => NavigationServer.RegionSetTransform(b, new(float.NaN, Vector2.Zero))); Reject<ArgumentException>(() => NavigationServer.RegionSetMap(b, a));
        var changes = 0; Action<RID> callback = changed => { if (changed == map) { changes++; Reject<InvalidOperationException>(NavigationServer.Synchronize); NavigationServer.RegionSetTravelCost(b, 2); } }; NavigationServer.MapChanged += callback; NavigationServer.RegionSetTravelCost(b, 3); NavigationServer.Synchronize(); NavigationServer.MapChanged -= callback; Check(changes == 1, "Publication callbacks can stage future edits but cannot re-enter synchronization."); NavigationServer.Synchronize();
        for (var i = 0; i < 64; i++) _ = NavigationServer.MapGetClosestPoint(map, new(3, 5)); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 2000; i++) _ = NavigationServer.MapGetClosestPoint(map, new(3, 5)); Check(GC.GetAllocatedBytesForCurrentThread() == before, "2000 prepared closest-point queries allocate zero bytes.");
        NavigationServer.Synchronize(); for (var i = 0; i < 64; i++) NavigationServer.Synchronize(); before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 2000; i++) NavigationServer.Synchronize(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "2000 unchanged synchronization boundaries allocate zero bytes.");
        NavigationServer.FreeRID(b); NavigationServer.Synchronize(); Reject<ArgumentException>(() => NavigationServer.RegionGetMap(b)); NavigationServer.FreeRID(a); NavigationServer.FreeRID(c); NavigationServer.FreeRID(map);
        using var world = new World(); Check(world.NavigationMap.IsValid() && NavigationServer.MapIsActive(world.NavigationMap), "World lazily owns a real active map."); Reject<InvalidOperationException>(() => NavigationServer.FreeRID(world.NavigationMap)); var wrid = world.NavigationMap; world.Dispose(); Reject<ArgumentException>(() => NavigationServer.MapGetRegions(wrid));
        using var geometry = Rectangle(0, 0, 20, 20); var root = new SubViewport(); var region = new NavigationRegion { NavigationPolygon = geometry }; root.AddChild(region); using var tree = new SceneTree(root); var srid = region.GetRID(); Reject<InvalidOperationException>(() => NavigationServer.FreeRID(srid)); tree.PhysicsFrame(0); Check(NavigationServer.MapGetClosestPointOwner(root.World!.NavigationMap, new(2, 2)) == srid, "Scene region publishes through the selected World map on a physics boundary.");
        using var other = new World(); root.World = other; tree.PhysicsFrame(0); Check(region.GetNavigationMap() == other.NavigationMap && NavigationServer.MapGetClosestPointOwner(other.NavigationMap, new(2, 2)) == srid, "World replacement preserves region identity and updates map membership."); region.Position = new(30, 0); tree.ProcessFrame(0); tree.PhysicsFrame(0); Check(NavigationServer.MapGetClosestPoint(other.NavigationMap, new(0, 0)) == new Vector2(30, 0), "Global scene transform changes reach committed topology.");
        WeightedRoutes(); AtomicPublication(); WeakSceneLifetime(); LargeCoordinates(); Storage();
        Console.WriteLine("Navigation authored polygons, deferred maps, paths, ownership, scene worlds and 2000 allocation-free projections passed.");
        void Bind(RID region, NavigationPolygon polygon) { NavigationServer.RegionSetMap(region, map); NavigationServer.RegionSetNavigationPolygon(region, polygon); }
    }
    private static void WeightedRoutes()
    {
        using var left = Rectangle(0, 0, 10, 30); using var right = Rectangle(20, 0, 10, 30); using var upper = Rectangle(10, 0, 10, 10); using var lower = Rectangle(10, 20, 10, 10);
        var map = NavigationServer.MapCreate(); NavigationServer.MapSetActive(map, true); var ids = new List<RID>();
        foreach (var p in new[] { left, right, upper, lower }) { var rid = NavigationServer.RegionCreate(); ids.Add(rid); NavigationServer.RegionSetMap(rid, map); NavigationServer.RegionSetNavigationPolygon(rid, p); }
        NavigationServer.Synchronize(); Check(NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true).Length >= 3, "Partial edge overlap connects unsplit convex region boundaries.");
        NavigationServer.RegionSetEnterCost(ids[2], 100); NavigationServer.Synchronize(); Check(NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true).Any(p => p.Y >= 20), "Entry cost selects the alternative lower corridor.");
        NavigationServer.RegionSetEnterCost(ids[2], 0); NavigationServer.RegionSetTravelCost(ids[3], 100); NavigationServer.Synchronize(); Check(NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true).Any(p => p.Y <= 10), "Travel cost selects the upper corridor.");
        // A mirrored corridor exchanges which projected endpoint lies outside the longer edge.
        foreach (var id in ids) NavigationServer.RegionSetTransform(id, new(new Vector2(-1, 0), new Vector2(0, 1), Vector2.Zero));
        NavigationServer.Synchronize(); Check(NavigationServer.MapGetPath(map, new(-5, 15), new(-25, 15), true)[^1] == new Vector2(-25, 15), "Reversed partial-overlap projections preserve corridor connectivity.");
        foreach (var id in ids) NavigationServer.RegionSetTransform(id, Transform.Identity);
        NavigationServer.MapSetUseEdgeConnections(map, false); NavigationServer.Synchronize(); Check(NavigationServer.MapGetPath(map, new(5, 15), new(25, 15), true)[^1].X <= 10, "Disabling margin edges separates unsplit regions.");
        foreach (var id in ids) NavigationServer.FreeRID(id); NavigationServer.FreeRID(map);
    }
    private static void AtomicPublication()
    {
        using var polygon = Rectangle(0, 0, 10, 10);
        var first = NavigationServer.MapCreate(); var second = NavigationServer.MapCreate(); var region = NavigationServer.RegionCreate();
        NavigationServer.MapSetActive(first, true); NavigationServer.MapSetActive(second, true); NavigationServer.RegionSetMap(region, second); NavigationServer.RegionSetNavigationPolygon(region, polygon); NavigationServer.Synchronize();
        var version = NavigationServer.MapGetIterationID(first); NavigationServer.MapSetEdgeConnectionMargin(first, 2);
        NavigationServer.RegionSetTransform(region, new(new Vector2(float.MaxValue, 0), new Vector2(0, 1), Vector2.Zero));
        Reject<ArgumentException>(NavigationServer.Synchronize);
        Check(NavigationServer.MapGetIterationID(first) == version && NavigationServer.MapGetClosestPoint(second, new(3, 4)) == new Vector2(3, 4), "Failed dirty-map build preserves every published iteration.");
        NavigationServer.RegionSetTransform(region, new(new Vector2(1, 0), new Vector2(0, 1), new Vector2(1e20f, 0))); Reject<ArgumentException>(NavigationServer.Synchronize);
        Check(NavigationServer.MapGetIterationID(first) == version, "Float-rounded collapsed transformed polygon rejects before publication.");
        NavigationServer.RegionSetTransform(region, Transform.Identity); NavigationServer.Synchronize(); Check(NavigationServer.MapGetIterationID(first) > version, "Valid retry commits retained dirty maps.");
        var delivered = 0; Action<RID> fail = _ => throw new InvalidOperationException("Observer"); Action<RID> observe = _ => delivered++;
        NavigationServer.MapChanged += fail; NavigationServer.MapChanged += observe; NavigationServer.MapSetEdgeConnectionMargin(first, 3);
        Reject<AggregateException>(NavigationServer.Synchronize); NavigationServer.MapChanged -= fail; NavigationServer.MapChanged -= observe; Check(delivered == 1 && NavigationServer.MapGetEdgeConnectionMargin(first) == 3, "Observer failure preserves publication and delivers remaining subscribers.");
        NavigationServer.RegionSetMap(region, default); NavigationServer.RegionSetEnabled(region, false); NavigationServer.Synchronize();
        Check(NavigationServer.RegionGetBounds(region).Size == new Vector2(10, 10) && NavigationServer.RegionGetClosestPoint(region, new(-2, 4)) == new Vector2(0, 4), "Detached disabled regions retain their committed geometry queries.");
        NavigationServer.FreeRID(region); NavigationServer.FreeRID(first); NavigationServer.FreeRID(second);
        using var node = new NavigationRegion(); node.SetNavigationLayerValue(32, true); Check(node.GetNavigationLayerValue(32), "High navigation-layer bit round-trips."); Reject<ArgumentOutOfRangeException>(() => node.SetNavigationLayerValue(0, true)); Reject<ArgumentOutOfRangeException>(() => node.GetNavigationLayerValue(33)); var signals = 0; node.NavigationPolygonChanged += () => signals++; node.NavigationPolygon = polygon; polygon.ClearPolygons(); Check(signals == 2, "Polygon reference and authored resource edits both notify the scene region.");
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (WeakReference<NavigationRegion> Weak, RID RID) AbandonedRegion(NavigationPolygon polygon)
    { var node = new NavigationRegion { NavigationPolygon = polygon }; return (new(node), node.GetRID()); }
    private static void WeakSceneLifetime()
    {
        using var polygon = Rectangle(0, 0, 10, 10); var abandoned = AbandonedRegion(polygon);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Check(!abandoned.Weak.TryGetTarget(out _), "Server/resource subscriptions retain scene identity weakly.");
        NavigationServer.Synchronize(); Reject<ArgumentException>(() => NavigationServer.RegionGetMap(abandoned.RID)); polygon.ClearPolygons();
    }
    private static void LargeCoordinates()
    {
        using var polygon = Rectangle(0, 0, 1e20f, 1e20f); var map = NavigationServer.MapCreate(); var region = NavigationServer.RegionCreate();
        NavigationServer.MapSetActive(map, true); NavigationServer.RegionSetMap(region, map); NavigationServer.RegionSetNavigationPolygon(region, polygon); NavigationServer.Synchronize();
        Check(NavigationServer.MapGetClosestPoint(map, new(2e20f, 5e19f)).IsEqualApprox(new(1e20f, 5e19f)) && NavigationServer.MapGetClosestPoint(map, new(5e19f, 5e19f)) == new Vector2(5e19f, 5e19f), "Projection keeps finite results when single-precision squared edge lengths overflow.");
        NavigationServer.FreeRID(region); NavigationServer.FreeRID(map);
    }
    private static void Storage()
    {
        using var polygon = Rectangle(2, 3, 20, 12);
        var geometry = (PropertyDescriptor<NavigationPolygon, byte[]>)polygon.GetPropertyList().First(p => p.Name == "_navigation_geometry"); var payload = geometry.GetValue(polygon);
        var corrupt = (byte[])payload.Clone(); corrupt[0] = 99; Reject<InvalidDataException>(() => geometry.SetValue(polygon, corrupt));
        Reject<InvalidDataException>(() => geometry.SetValue(polygon, payload[..^1])); Reject<InvalidDataException>(() => geometry.SetValue(polygon, [.. payload, 0]));
        corrupt = (byte[])payload.Clone(); System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(corrupt.AsSpan(corrupt.Length - 4), 100); Reject<InvalidDataException>(() => geometry.SetValue(polygon, corrupt));
        Check(polygon.GetPolygonCount() == 1 && polygon.GetVertices()[0] == new Vector2(2, 3), "Malformed version, truncation, trailing bytes and invalid indices preserve authored resource data.");
        using var region = new NavigationRegion { Name = "Region", NavigationPolygon = polygon, EnterCost = 2, NavigationLayers = 3 }; using var packed = new PackedScene(); packed.Pack(region);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-navigation-" + Guid.NewGuid() + ".e2dscene");
        try
        {
            ResourceSaver.Save(packed, path); RunChild(path); var start = new System.Diagnostics.ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(NavigationTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_NAVIGATION"); start.Environment["ELECTRON2D_TEST_NAVIGATION_CHILD"] = path;
            using var child = System.Diagnostics.Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Navigation archive child."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh navigation scene passed"), error.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
    }
    internal static void RunChild(string path)
    { using var scene = ResourceLoader.Load<PackedScene>(path)!; using var node = (NavigationRegion)scene.Instantiate(); Check(node.NavigationPolygon!.GetPolygonCount() == 1 && node.NavigationPolygon.GetVertices()[0] == new Vector2(2, 3) && node.EnterCost == 2 && node.NavigationLayers == 3, "Fresh typed region/polygon reconstruction."); using var tree = new SceneTree(node); tree.PhysicsFrame(0); Check(NavigationServer.MapGetClosestPointOwner(node.GetNavigationMap(), new(3, 4)) == node.GetRID(), "Fresh loaded region supplies real navigation topology."); Console.WriteLine("Fresh navigation scene passed."); }
    private sealed class Walker : Entity
    {
        internal Vector2[] Path = []; internal bool Done; private int _next = 1;
        protected override void OnPhysicsProcess(double delta)
        {
            if (Path.Length == 0) { Path = NavigationServer.MapGetPath(GetWorld()!.NavigationMap, Position, new(44, 44), true); if (Path.Length < 2) return; }
            var remaining = (float)(delta * 200); while (_next < Path.Length && remaining > 0) { var difference = Path[_next] - Position; var distance = difference.Length(); if (distance <= remaining) { Position = Path[_next++]; remaining -= distance; } else { Position += difference / distance * remaining; remaining = 0; } }
            Done = _next == Path.Length;
        }
        protected override void OnDraw() => DrawRect(new(-2, -2, 4, 4), Colors.Yellow);
    }
    private sealed class Route : Entity
    {
        internal Walker? Walker;
        protected override void OnDraw() { DrawRect(new(4, 4, 16, 48), new(.2f, .2f, .2f)); DrawRect(new(20, 4, 16, 16), new(.2f, .2f, .2f)); DrawRect(new(36, 4, 16, 48), new(.2f, .2f, .2f)); if (Walker is { Path.Length: > 1 } w) DrawPolyline(w.Path, Colors.Green, 2); }
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        using var left = Rectangle(4, 4, 16, 48); using var top = Rectangle(20, 4, 16, 16); using var right = Rectangle(36, 4, 16, 48); var window = new Window { Size = new(64, 64) };
        foreach (var p in new[] { left, top, right }) window.AddChild(new NavigationRegion { Name = "Region" + window.GetChildCount(), NavigationPolygon = p }); var actor = new Walker { Position = new(12, 44), PhysicsProcessEnabled = true }; var route = new Route { Walker = actor }; window.AddChild(route); window.AddChild(actor);
        var frames = 0; RID map = default; window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend && RenderingServer.GetCurrentRenderingDriverName() != "software", "Requested hardware backend."); map = window.World!.NavigationMap; RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () => { frames++; if (actor.Path.Length > 1) route.QueueRedraw(); if (!actor.Done) { Check(frames < 180, "Path-following host finishes within bounded frames."); return; } using var image = window.GetTexture().GetImage()!; Check(image.GetPixel(44, 44).R > .9f && image.GetPixel(44, 44).G > .9f, "Navigation-driven actor reaches rendered goal pixel."); Check(actor.Path.SequenceEqual(new Vector2[] { new(12, 44), new(20, 20), new(36, 20), new(44, 44) }), "Rendered actor follows the optimized corridor around the hole."); if (Environment.GetEnvironmentVariable("ELECTRON2D_NAVIGATION_SNAPSHOT") is { } capture) image.SavePNG(capture); window.Tree!.Quit(); }; };
        Engine.Run(window); Check(window.IsDisposed && RenderingServer.Service is null, "Native scene/backend cleanup."); Reject<ArgumentException>(() => NavigationServer.MapGetRegions(map)); Console.WriteLine($"Navigation polygon corridor/path-following/pixels/cleanup passed: {backend}, {frames} frames.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
