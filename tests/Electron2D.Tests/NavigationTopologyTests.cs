using Electron2D;
using System.Text.Json;

internal static class NavigationTopologyTests
{
    internal static void Run()
    {
        ReferenceCases(); Publication(); Sampling(); ResourceOwnerDelivery();
        Console.WriteLine("Navigation raster ownership, reference counters/pathways/routes, deferred rollback/versions, filtered area sampling and warmed reads passed.");
    }
    private static int[] Counts() => Enumerable.Range(0, 10).Select(i => NavigationServer.GetProcessInfo((NavigationServer.ProcessInfo)i)).ToArray();
    private static Vector2 Point(JsonElement value) => new(value[0].GetSingle(), value[1].GetSingle());
    private static bool Flag(JsonElement value, string name, bool fallback) => value.TryGetProperty(name, out var member) ? member.GetBoolean() : fallback;
    private static float Number(JsonElement value, string name, float fallback) => value.TryGetProperty(name, out var member) ? member.GetSingle() : fallback;
    private static uint Layers(JsonElement value) => value.TryGetProperty("layers", out var member) ? member.GetUInt32() : 1;
    private static void ReferenceCases()
    {
        using var input = typeof(NavigationTopologyTests).Assembly.GetManifestResourceStream("TestNavigation.topology-cases.json")!;
        using var expected = typeof(NavigationTopologyTests).Assembly.GetManifestResourceStream("TestNavigation.topology-reference.json")!;
        using var cases = JsonDocument.Parse(input); using var reference = JsonDocument.Parse(expected);
        Check(reference.RootElement.GetProperty("commit").GetString() == "ed1daf0bf001b61586d9930840f2f1394092c079", "Reference pin.");
        var index = 0;
        foreach (var test in cases.RootElement.EnumerateArray())
        {
            NavigationServer.Synchronize(); var before = Counts(); var map = NavigationServer.MapCreate(); var regions = new List<RID>(); var resources = new List<NavigationPolygon>();
            try
            {
                NavigationServer.MapSetActive(map, Flag(test, "active", true)); NavigationServer.MapSetCellSize(map, Number(test, "cell_size", 1)); NavigationServer.MapSetMergeRasterizerCellScale(map, Number(test, "scale", .1f)); NavigationServer.MapSetUseEdgeConnections(map, Flag(test, "connect", false)); NavigationServer.MapSetEdgeConnectionMargin(map, Number(test, "margin", 1));
                foreach (var source in test.GetProperty("regions").EnumerateArray())
                {
                    var polygon = new NavigationPolygon(); resources.Add(polygon); var vertices = new List<Vector2>(); var cells = new List<int[]>();
                    foreach (var cell in source.GetProperty("cells").EnumerateArray()) { var indexes = new List<int>(); foreach (var p in cell.EnumerateArray()) { indexes.Add(vertices.Count); vertices.Add(Point(p)); } cells.Add(indexes.ToArray()); }
                    polygon.SetVertices(vertices.ToArray()); foreach (var cell in cells) polygon.AddPolygon(cell);
                    var region = NavigationServer.RegionCreate(); regions.Add(region); NavigationServer.RegionSetMap(region, map); NavigationServer.RegionSetNavigationPolygon(region, polygon); NavigationServer.RegionSetEnabled(region, Flag(source, "enabled", true)); NavigationServer.RegionSetUseEdgeConnections(region, Flag(source, "connect", true)); NavigationServer.RegionSetNavigationLayers(region, Layers(source));
                }
                NavigationServer.Synchronize(); var result = reference.RootElement.GetProperty("cases")[index++]; var name = test.GetProperty("name").GetString()!;
                var counts = Counts(); var actual = counts.Select((v, i) => v - before[i]).ToArray(); var wanted = result.GetProperty("counts").EnumerateArray().Select(v => v.GetInt32()).ToArray(); Check(actual.SequenceEqual(wanted), name + " counters: " + string.Join(',', actual) + " expected " + string.Join(',', wanted));
                for (var r = 0; r < regions.Count; r++)
                {
                    var paths = result.GetProperty("pathways")[r]; Check(NavigationServer.RegionGetConnectionsCount(regions[r]) == paths.GetArrayLength(), name + " connection count.");
                    for (var c = 0; c < paths.GetArrayLength(); c++) { Equal(NavigationServer.RegionGetConnectionPathwayStart(regions[r], c), Point(paths[c][0]), name + " pathway start"); Equal(NavigationServer.RegionGetConnectionPathwayEnd(regions[r], c), Point(paths[c][1]), name + " pathway end"); }
                }
                foreach (var optimize in new[] { false, true })
                {
                    var path = NavigationServer.MapGetPath(map, Point(test.GetProperty("start")), Point(test.GetProperty("end")), optimize, Layers(test)); var wantedPath = result.GetProperty(optimize ? "optimized_path" : "raw_path").EnumerateArray().Select(Point).ToArray();
                    Check(path.Length == wantedPath.Length, name + " path count: " + string.Join(';', path) + " expected " + string.Join(';', wantedPath)); for (var p = 0; p < path.Length; p++) Equal(path[p], wantedPath[p], name + " path");
                }
            }
            finally { foreach (var region in regions) NavigationServer.FreeRID(region); NavigationServer.FreeRID(map); foreach (var resource in resources) resource.Dispose(); NavigationServer.Synchronize(); }
        }
    }
    private static NavigationPolygon Rectangle(float x, float y, float width, float height)
    { var value = new NavigationPolygon(); value.SetVertices([new(x, y), new(x + width, y), new(x + width, y + height), new(x, y + height)]); value.AddPolygon([0, 1, 2, 3]); return value; }
    private static void Publication()
    {
        using var a = Rectangle(0, 0, 10, 10); using var b = Rectangle(10.4f, 2, 10, 6); var map = NavigationServer.MapCreate(); var ra = NavigationServer.RegionCreate(); var rb = NavigationServer.RegionCreate(); var agent = NavigationServer.AgentCreate(); var obstacle = NavigationServer.ObstacleCreate(); var link = NavigationServer.LinkCreate();
        try
        {
            Check(NavigationServer.MapGetCellSize(map) == 1 && NavigationServer.MapGetMergeRasterizerCellScale(map) == .1f && NavigationServer.RegionGetIterationID(ra) == 0, "Raster/version defaults.");
            Reject<ArgumentOutOfRangeException>(() => NavigationServer.MapSetCellSize(map, float.NaN)); Reject<ArgumentOutOfRangeException>(() => NavigationServer.MapSetMergeRasterizerCellScale(map, float.PositiveInfinity)); Reject<ArgumentOutOfRangeException>(() => NavigationServer.GetProcessInfo((NavigationServer.ProcessInfo)10));
            NavigationServer.MapSetCellSize(map, -1); Check(NavigationServer.MapGetCellSize(map) == .0001f, "Cell minimum clamp."); NavigationServer.MapSetCellSize(map, 1); NavigationServer.MapSetMergeRasterizerCellScale(map, 20); Check(NavigationServer.MapGetMergeRasterizerCellScale(map) == .1f, "Raster maximum clamp.");
            NavigationServer.MapSetActive(map, true); NavigationServer.MapSetEdgeConnectionMargin(map, .5f); NavigationServer.RegionSetMap(ra, map); NavigationServer.RegionSetMap(rb, map); NavigationServer.RegionSetNavigationPolygon(ra, a); NavigationServer.RegionSetNavigationPolygon(rb, b); NavigationServer.AgentSetMap(agent, map); NavigationServer.ObstacleSetMap(obstacle, map); NavigationServer.LinkSetMap(link, map); NavigationServer.Synchronize();
            Check(NavigationServer.RegionGetConnectionsCount(ra) == 1 && NavigationServer.RegionGetConnectionsCount(rb) == 1, "Published margin pathways."); var snapshot = Counts(); var version = NavigationServer.RegionGetIterationID(rb); var pathway = NavigationServer.RegionGetConnectionPathwayStart(ra, 0);
            NavigationServer.RegionSetTransform(rb, new(new Vector2(float.MaxValue, 0), new Vector2(0, 1), Vector2.Zero)); Reject<ArgumentException>(NavigationServer.Synchronize); Check(Counts().SequenceEqual(snapshot) && NavigationServer.RegionGetIterationID(rb) == version && NavigationServer.RegionGetConnectionPathwayStart(ra, 0) == pathway, "Failed build rolls back every public snapshot.");
            NavigationServer.RegionSetTransform(rb, Transform.Identity); NavigationServer.MapSetUseEdgeConnections(map, false); Check(NavigationServer.RegionGetConnectionsCount(ra) == 1, "Pathways retain published state before synchronization."); NavigationServer.Synchronize(); Check(NavigationServer.RegionGetConnectionsCount(ra) == 0 && NavigationServer.RegionGetIterationID(rb) > version, "Valid retry atomically publishes topology and version."); Reject<ArgumentOutOfRangeException>(() => NavigationServer.RegionGetConnectionPathwayStart(ra, 0));
            NavigationServer.MapSetMergeRasterizerCellScale(map, .02f); NavigationServer.Synchronize(); Check(NavigationServer.RegionGetIterationID(ra) > 1, "Map raster edits rebuild attached region versions.");
            var regionsField = typeof(NavigationServer).GetField("_regions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var bank = (Dictionary<RID, NavigationRegionState>)regionsField.GetValue(NavigationServer.Service)!; bank[ra].IterationID = uint.MaxValue; NavigationServer.RegionSetTravelCost(ra, 2); NavigationServer.Synchronize(); Check(NavigationServer.RegionGetIterationID(ra) == 1, "Region version skips zero at uint wrap.");
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) { NavigationServer.Synchronize(); _ = NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.PolygonCount); _ = NavigationServer.RegionGetIterationID(ra); }
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before; Check(bytes == 0, "Unchanged synchronized topology/counters allocated " + bytes + " bytes.");
            NavigationServer.AgentSetMap(agent, default); NavigationServer.ObstacleSetMap(obstacle, default); NavigationServer.LinkSetMap(link, default); NavigationServer.Synchronize(); Check(NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.AgentCount) == snapshot[2] - 1 && NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.ObstacleCount) == snapshot[9] - 1, "Member-only edits refresh profiling.");
        }
        finally { NavigationServer.FreeRID(agent); NavigationServer.FreeRID(obstacle); NavigationServer.FreeRID(link); NavigationServer.FreeRID(ra); NavigationServer.FreeRID(rb); NavigationServer.FreeRID(map); NavigationServer.Synchronize(); }
        Reject<ArgumentException>(() => NavigationServer.MapGetCellSize(map)); Reject<ArgumentException>(() => NavigationServer.RegionGetConnectionsCount(ra));
    }
    private static void Sampling()
    {
        using var small = Rectangle(0, 0, 10, 10); using var large = Rectangle(20, 0, 20, 20); var map = NavigationServer.MapCreate(); var a = NavigationServer.RegionCreate(); var b = NavigationServer.RegionCreate();
        try
        {
            NavigationServer.MapSetActive(map, true); NavigationServer.RegionSetMap(a, map); NavigationServer.RegionSetMap(b, map); NavigationServer.RegionSetNavigationPolygon(a, small); NavigationServer.RegionSetNavigationPolygon(b, large); NavigationServer.RegionSetNavigationLayers(b, 2); NavigationServer.Synchronize();
            var big = 0; var flatBig = 0;
            for (var i = 0; i < 10000; i++) { var p = NavigationServer.MapGetRandomPoint(map, 3, true); Check(p.IsFinite() && (p.X is >= 0 and <= 10 && p.Y is >= 0 and <= 10 || p.X is >= 20 and <= 40 && p.Y is >= 0 and <= 20), "Uniform sample lies on geometry."); if (p.X >= 20) big++; if (NavigationServer.MapGetRandomPoint(map, 3, false).X >= 20) flatBig++; Check(NavigationServer.MapGetRandomPoint(map, 2, true).X >= 20 && NavigationServer.MapGetRandomPoint(map, 2, false).X >= 20, "Filtered region index belongs to the requested layer."); }
            Check(big is > 7700 and < 8300 && flatBig is > 4700 and < 5300, "Area versus hierarchical uniform distribution: " + big + "/" + flatBig);
            Check(NavigationServer.MapGetRandomPoint(map, 0, true) == Vector2.Zero && NavigationServer.RegionGetRandomPoint(a, 2, true) == Vector2.Zero, "No eligible layer returns zero.");
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 2000; i++) { _ = NavigationServer.MapGetRandomPoint(map, 3, true); _ = NavigationServer.RegionGetRandomPoint(b, 2, false); }
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared sampling allocates no managed bytes.");
            NavigationServer.RegionSetEnabled(b, false); NavigationServer.Synchronize(); Check(NavigationServer.RegionGetRandomPoint(b, 2, true) == Vector2.Zero && NavigationServer.MapGetRandomPoint(map, 2, true) == Vector2.Zero, "Disabled surfaces do not sample.");
            NavigationServer.RegionSetMap(a, default); NavigationServer.Synchronize(); Check(NavigationServer.RegionGetRandomPoint(a, 1, true).X is >= 0 and <= 10, "Detached geometry remains sampleable.");
            using var huge = Rectangle(0, 0, 1e20f, 1e20f); NavigationServer.RegionSetNavigationPolygon(a, huge); NavigationServer.Synchronize(); var pHuge = NavigationServer.RegionGetRandomPoint(a, 1, true); Check(pHuge.IsFinite() && pHuge.X is >= 0 and <= 1e20f && pHuge.Y is >= 0 and <= 1e20f, "Double areas preserve finite large-coordinate sampling.");
        }
        finally { NavigationServer.FreeRID(a); NavigationServer.FreeRID(b); NavigationServer.FreeRID(map); NavigationServer.Synchronize(); }
    }
    private static void ResourceOwnerDelivery()
    {
        using var polygon = Rectangle(0, 0, 10, 10); var root = new SubViewport(); var region = new NavigationRegion { NavigationPolygon = polygon }; root.AddChild(region); using var tree = new SceneTree(root); tree.PhysicsFrame(0); var delivered = 0; var version = NavigationServer.RegionGetIterationID(region.GetRID());
        region.NavigationPolygonChanged += () => { Check(tree.IsOwnerThread, "Resource change signal returns to the scene owner."); delivered++; };
        Task.Run(() => polygon.SetVertices([new(1, 0), new(11, 0), new(11, 10), new(1, 10)])).GetAwaiter().GetResult(); Check(delivered == 0 && NavigationServer.RegionGetIterationID(region.GetRID()) == version, "Worker edits stage geometry without publishing versions or invoking scene listeners."); tree.ProcessFrame(.01); tree.PhysicsFrame(0); Check(delivered == 1 && NavigationServer.RegionGetIterationID(region.GetRID()) > version, "Owner signal and next physics publication execute.");
    }
    private static void Equal(Vector2 actual, Vector2 expected, string name) => Check(actual.DistanceTo(expected) <= .0001f, name + ": " + actual + " expected " + expected);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
