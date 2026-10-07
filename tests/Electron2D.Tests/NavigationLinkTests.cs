using Electron2D;

internal static class NavigationLinkTests
{
    internal static void Run()
    {
        using var left = NavigationTests.Rectangle(0, 0, 10, 20); using var right = NavigationTests.Rectangle(30, 0, 10, 20);
        var map = NavigationServer.MapCreate(); var a = NavigationServer.RegionCreate(); var b = NavigationServer.RegionCreate(); var link = NavigationServer.LinkCreate();
        NavigationServer.MapSetActive(map, true); Bind(a, left); Bind(b, right); NavigationServer.Synchronize();
        var origin = new Vector2(5, 15); var destination = new Vector2(35, 15);
        Check(Path(origin, destination)[^1].X <= 10, "Separated region topology cannot reach the destination before link attachment.");
        Check(NavigationServer.LinkGetEnabled(link) && NavigationServer.LinkIsBidirectional(link) && NavigationServer.LinkGetMap(link) == default && NavigationServer.LinkGetNavigationLayers(link) == 1 && NavigationServer.LinkGetEnterCost(link) == 0 && NavigationServer.LinkGetTravelCost(link) == 1 && NavigationServer.LinkGetIterationID(link) == 0 && NavigationServer.MapGetLinkConnectionRadius(map) == 4, "Caller link and attachment defaults.");
        NavigationServer.LinkSetMap(link, map); NavigationServer.LinkSetStartPosition(link, new(8, 5)); NavigationServer.LinkSetEndPosition(link, new(32, 5)); NavigationServer.LinkSetOwnerID(link, 123);
        var membership = NavigationServer.MapGetLinks(map); membership[0] = default; Check(NavigationServer.MapGetLinks(map).SequenceEqual(new[] { link }) && NavigationServer.LinkGetOwnerID(link) == 123, "Copied staged membership and logical owner identity.");
        Check(Path(origin, destination)[^1].X <= 10 && NavigationServer.LinkGetIterationID(link) == 0, "Link geometry remains deferred before synchronization."); NavigationServer.Synchronize();
        var route = Path(origin, destination); Console.WriteLine("Linked path: " + string.Join(";", route));
        Check(route.SequenceEqual(new Vector2[] { origin, new(8, 5), new(32, 5), destination }), "Optimized corridor crosses both link portals around off-surface gap.");
        Check(NavigationServer.MapGetPath(map, origin, destination, false).SequenceEqual(route), "Raw corridor retains point portals.");
        Check(NavigationServer.MapGetClosestPoint(map, new(20, 5)) == new Vector2(10, 5) && NavigationServer.MapGetClosestPointOwner(map, new(20, 5)) == a, "Off-surface link is never a walkable surface or closest-point owner.");
        Check(Path(destination, origin)[^1] == origin, "Bidirectional default allows reverse traversal.");
        var version = NavigationServer.LinkGetIterationID(link); NavigationServer.LinkSetBidirectional(link, false); Check(Path(destination, origin)[^1] == origin, "Direction edits preserve the published path until sync."); NavigationServer.Synchronize(); Check(Path(destination, origin)[^1].X >= 30 && Path(origin, destination)[^1] == destination && NavigationServer.LinkGetIterationID(link) > version, "Directed link only permits start-to-end traversal.");
        NavigationServer.LinkSetNavigationLayers(link, 2); NavigationServer.Synchronize(); Check(Path(origin, destination)[^1].X <= 10 && NavigationServer.MapGetPath(map, origin, destination, true, 3)[^1] == destination, "Link layer filtering is additional to region layers.");
        NavigationServer.LinkSetNavigationLayers(link, 1); NavigationServer.LinkSetStartPosition(link, new(12, 5)); NavigationServer.MapSetLinkConnectionRadius(map, 2); NavigationServer.Synchronize(); Check(Path(origin, destination)[^1].X <= 10, "Endpoint exactly on the radius boundary does not attach.");
        NavigationServer.MapSetLinkConnectionRadius(map, 2.1f); NavigationServer.Synchronize(); Check(Path(origin, destination).Contains(new Vector2(10, 5)) && Path(origin, destination)[^1] == destination, "Endpoint inside the radius projects onto its nearest surface.");
        version = NavigationServer.LinkGetIterationID(link); NavigationServer.MapSetLinkConnectionRadius(map, 3); NavigationServer.Synchronize(); Check(NavigationServer.LinkGetIterationID(link) == version, "Map rebuild does not increment unchanged link version.");
        NavigationServer.MapSetLinkConnectionRadius(map, 0); NavigationServer.Synchronize(); Check(Path(origin, destination)[^1].X <= 10, "Zero radius prevents every endpoint attachment.");
        NavigationServer.MapSetLinkConnectionRadius(map, 4); NavigationServer.LinkSetStartPosition(link, new(8, 5)); NavigationServer.LinkSetEnabled(link, false); NavigationServer.Synchronize(); Check(Path(origin, destination)[^1].X <= 10, "Disabled links do not connect regions.");
        NavigationServer.LinkSetEnabled(link, true); NavigationServer.Synchronize(); Check(Path(origin, destination)[^1] == destination, "Reenabled link restores traversal.");
        Reject<ArgumentException>(() => NavigationServer.LinkSetMap(link, a)); Reject<ArgumentException>(() => NavigationServer.LinkGetEnabled(a)); Reject<ArgumentException>(() => NavigationServer.LinkSetEndPosition(link, new(float.NaN, 0))); Reject<ArgumentOutOfRangeException>(() => NavigationServer.LinkSetEnterCost(link, -1)); Reject<ArgumentOutOfRangeException>(() => NavigationServer.LinkSetTravelCost(link, float.PositiveInfinity)); Reject<ArgumentOutOfRangeException>(() => NavigationServer.MapSetLinkConnectionRadius(map, float.NaN));
        version = NavigationServer.LinkGetIterationID(link); NavigationServer.LinkSetEnterCost(link, 7); NavigationServer.RegionSetTransform(a, new(new Vector2(float.MaxValue, 0), new Vector2(0, 1), Vector2.Zero));
        Reject<ArgumentException>(NavigationServer.Synchronize); Check(NavigationServer.LinkGetIterationID(link) == version && Path(origin, destination)[^1] == destination, "Failed map build preserves published link version and route."); NavigationServer.RegionSetTransform(a, Transform.Identity); NavigationServer.Synchronize(); Check(NavigationServer.LinkGetIterationID(link) > version, "Valid retry publishes the retained dirty link.");
        var states = (Dictionary<RID, NavigationLinkState>)typeof(NavigationServer).GetField("_links", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(NavigationServer.Service)!;
        states[link].IterationID = uint.MaxValue; NavigationServer.LinkSetTravelCost(link, 2); NavigationServer.Synchronize(); Check(NavigationServer.LinkGetIterationID(link) == 1, "Link version wraps at the pinned 32-bit limit.");
        for (var i = 0; i < 64; i++) { _ = NavigationServer.LinkGetStartPosition(link); NavigationServer.LinkSetStartPosition(link, new(8, 5)); NavigationServer.Synchronize(); }
        var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 2000; i++) { _ = NavigationServer.LinkGetStartPosition(link); NavigationServer.LinkSetStartPosition(link, new(8, 5)); NavigationServer.Synchronize(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "2000 unchanged link identity/setting/sync cycles allocate zero bytes.");
        NavigationServer.FreeRID(map); Check(NavigationServer.LinkGetMap(link) == default, "Map free detaches surviving caller-owned links."); NavigationServer.FreeRID(link); Reject<ArgumentException>(() => NavigationServer.LinkGetIterationID(link)); NavigationServer.FreeRID(a); NavigationServer.FreeRID(b);
        WeightedRoutes(); SameRegion(); SceneAndStorage(); WeakLifetime();
        Console.WriteLine("Navigation links, direction/layers/costs/radius, surface ownership, scene transforms/storage and zero-allocation idle cycles passed.");
        void Bind(RID region, NavigationPolygon polygon) { NavigationServer.RegionSetMap(region, map); NavigationServer.RegionSetNavigationPolygon(region, polygon); }
        Vector2[] Path(Vector2 start, Vector2 end) => NavigationServer.MapGetPath(map, start, end, true);
    }
    private static void WeightedRoutes()
    {
        using var left = NavigationTests.Rectangle(0, 0, 10, 20); using var right = NavigationTests.Rectangle(30, 0, 10, 20); var map = NavigationServer.MapCreate(); NavigationServer.MapSetActive(map, true); var owned = new List<RID>();
        foreach (var p in new[] { left, right }) { var r = NavigationServer.RegionCreate(); owned.Add(r); NavigationServer.RegionSetMap(r, map); NavigationServer.RegionSetNavigationPolygon(r, p); }
        var upper = NavigationServer.LinkCreate(); var lower = NavigationServer.LinkCreate(); owned.Add(upper); owned.Add(lower);
        foreach (var (r, y) in new[] { (upper, 4f), (lower, 16f) }) { NavigationServer.LinkSetMap(r, map); NavigationServer.LinkSetStartPosition(r, new(8, y)); NavigationServer.LinkSetEndPosition(r, new(32, y)); }
        NavigationServer.LinkSetEnterCost(upper, 100); NavigationServer.Synchronize(); Check(Path().Any(p => p.Y == 16), "Entry cost selects the lower off-surface corridor.");
        NavigationServer.LinkSetEnterCost(upper, 0); NavigationServer.LinkSetTravelCost(lower, 10); NavigationServer.Synchronize(); Check(Path().Any(p => p.Y == 4), "Travel multiplier selects the upper off-surface corridor.");
        NavigationServer.LinkSetTravelCost(lower, 0); NavigationServer.Synchronize(); Check(Path().Any(p => p.Y == 16), "Zero link travel cost remains a meaningful weighted route.");
        NavigationServer.FreeRID(lower); owned.Remove(lower); Check(Path().Any(p => p.Y == 16), "Freed link topology remains a committed snapshot until sync."); NavigationServer.Synchronize(); Check(Path().Any(p => p.Y == 4), "Link free rebuilds the surviving alternative route.");
        foreach (var rid in owned) NavigationServer.FreeRID(rid); NavigationServer.FreeRID(map);
        Vector2[] Path() => NavigationServer.MapGetPath(map, new(5, 10), new(35, 10), true);
    }
    private static void SameRegion()
    {
        using var polygon = new NavigationPolygon(); polygon.SetVertices([new(0, 0), new(10, 0), new(10, 10), new(0, 10), new(30, 0), new(40, 0), new(40, 10), new(30, 10)]); polygon.AddPolygon([0, 1, 2, 3]); polygon.AddPolygon([4, 5, 6, 7]);
        var map = NavigationServer.MapCreate(); var region = NavigationServer.RegionCreate(); var link = NavigationServer.LinkCreate(); NavigationServer.MapSetActive(map, true); NavigationServer.RegionSetMap(region, map); NavigationServer.RegionSetNavigationPolygon(region, polygon); NavigationServer.LinkSetMap(link, map); NavigationServer.LinkSetStartPosition(link, new(8, 5)); NavigationServer.LinkSetEndPosition(link, new(32, 5)); NavigationServer.Synchronize();
        var path = NavigationServer.MapGetPath(map, new(2, 5), new(38, 5), false); Check(path.SequenceEqual(new Vector2[] { new(2, 5), new(8, 5), new(32, 5), new(38, 5) }), "A link also connects separate polygons owned by the same region, retaining raw endpoint portals.");
        NavigationServer.FreeRID(link); NavigationServer.FreeRID(region); NavigationServer.FreeRID(map);
    }
    private static void SceneAndStorage()
    {
        using var detached = new NavigationLink { Position = new(100, 0), StartPosition = new(2, 3), EndPosition = new(8, 9) }; Check(detached.GetGlobalStartPosition() == new Vector2(2, 3), "Detached global endpoint API retains the source local role."); detached.SetGlobalEndPosition(new(4, 5)); Check(detached.EndPosition == new Vector2(4, 5), "Detached global setter stores a local point.");
        detached.SetNavigationLayerValue(32, true); Check(detached.GetNavigationLayerValue(32), "High layer bit round-trips."); Reject<ArgumentOutOfRangeException>(() => detached.GetNavigationLayerValue(0)); Reject<ArgumentOutOfRangeException>(() => detached.SetNavigationLayerValue(33, true));
        using var zero = new NavigationLink(); Check(zero.GetConfigurationWarnings().Length == 1, "Coincident endpoint warning uses the existing Node tooling API."); zero.EndPosition = Vector2.One; Check(zero.GetConfigurationWarnings().Length == 0, "Distinct endpoints clear the warning.");
        var root = new SubViewport(); var link = new NavigationLink { StartPosition = new(2, 3), EndPosition = new(8, 9), Position = new(10, 20) }; root.AddChild(link); using var tree = new SceneTree(root); var rid = link.GetRID(); Reject<InvalidOperationException>(() => NavigationServer.FreeRID(rid)); tree.PhysicsFrame(0);
        Check(link.GetGlobalStartPosition() == new Vector2(12, 23) && NavigationServer.LinkGetStartPosition(rid) == new Vector2(12, 23) && NavigationServer.LinkGetOwnerID(rid) == link.InstanceID, "Scene entry publishes global endpoints and logical owner.");
        link.SetGlobalStartPosition(new(14, 25)); Check(link.StartPosition == new Vector2(4, 5), "Attached global setters invert the parent transform.");
        link.Position = new(20, 30); tree.PhysicsFrame(0); Check(NavigationServer.LinkGetStartPosition(rid) == new Vector2(24, 35), "Global transform notification updates both endpoints before map synchronization.");
        tree.EditedSceneRoot = root; Action<SceneTree, Node> failWarning = (_, _) => throw new InvalidOperationException("Warning observer"); tree.NodeConfigurationWarningChanged += failWarning;
        Reject<InvalidOperationException>(() => link.EndPosition = new(11, 12)); tree.NodeConfigurationWarningChanged -= failWarning;
        Check(link.EndPosition == new Vector2(11, 12) && NavigationServer.LinkGetEndPosition(rid) == new Vector2(31, 42), "Warning observer failure preserves committed source and staged endpoint data.");
        for (var i = 0; i < 64; i++) { link.EndPosition = new(11 + (i & 1), 12); _ = link.GetGlobalStartPosition(); }
        var allocation = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 2000; i++) { link.EndPosition = new(11 + (i & 1), 12); _ = link.GetGlobalStartPosition(); }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocation, "2000 changing scene endpoint staging/global lookup cycles allocate zero bytes, excluding topology builds.");
        using var world = new World(); root.World = world; tree.PhysicsFrame(0); Check(link.GetRID() == rid && link.GetNavigationMap() == world.NavigationMap && NavigationServer.LinkGetMap(rid) == world.NavigationMap, "World replacement transfers stable link identity.");
        var explicitMap = NavigationServer.MapCreate(); link.SetNavigationMap(explicitMap); Check(link.GetNavigationMap() == explicitMap, "Explicit link map override."); link.SetNavigationMap(default); Check(link.GetNavigationMap() == world.NavigationMap, "Empty override restores selected World map."); NavigationServer.FreeRID(explicitMap);
        NavigationServer.LinkSetTravelCost(rid, 7); Check(link.TravelCost == 1, "Direct server edits preserve cached node source properties."); root.RemoveChild(link); Check(NavigationServer.LinkGetMap(rid) == default && link.GetGlobalStartPosition() == link.StartPosition, "Scene exit detaches membership and restores local global-endpoint role."); link.Dispose(); Reject<ArgumentException>(() => NavigationServer.LinkGetEnabled(rid));
        using var authored = new NavigationLink { Name = "Link", Enabled = false, Bidirectional = false, NavigationLayers = 3, StartPosition = new(2, 3), EndPosition = new(12, 13), EnterCost = 5, TravelCost = 2 }; using var packed = new PackedScene(); packed.Pack(authored); var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-link-" + Guid.NewGuid() + ".e2dscene");
        try { ResourceSaver.Save(packed, file); RunChild(file); var start = new System.Diagnostics.ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(NavigationLinkTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_NAVIGATION_LINKS"); start.Environment["ELECTRON2D_TEST_NAVIGATION_LINK_CHILD"] = file; using var child = System.Diagnostics.Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh link scene."); } Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh navigation link scene passed"), error.GetAwaiter().GetResult()); }
        finally { System.IO.File.Delete(file); }
    }
    internal static void RunChild(string file)
    { using var packed = ResourceLoader.Load<PackedScene>(file)!; using var node = (NavigationLink)packed.Instantiate(); Check(!node.Enabled && !node.Bidirectional && node.NavigationLayers == 3 && node.StartPosition == new Vector2(2, 3) && node.EndPosition == new Vector2(12, 13) && node.EnterCost == 5 && node.TravelCost == 2, "Fresh typed link reconstruction."); using var tree = new SceneTree(node); tree.PhysicsFrame(0); Check(node.GetNavigationMap().IsValid() && NavigationServer.MapGetLinks(node.GetNavigationMap()).Contains(node.GetRID()), "Fresh scene uses real World link storage."); Console.WriteLine("Fresh navigation link scene passed."); }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (WeakReference<NavigationLink> Weak, RID RID) Abandon() { var node = new NavigationLink(); return (new(node), node.GetRID()); }
    private static void WeakLifetime() { var value = Abandon(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); Check(!value.Weak.TryGetTarget(out _), "Link registry retains detached nodes weakly."); NavigationServer.Synchronize(); Reject<ArgumentException>(() => NavigationServer.LinkGetMap(value.RID)); }
    private sealed class Traveler : Entity
    {
        internal NavigationLink Link = null!; internal Vector2[] Path = []; internal bool Done; private int _phase, _next = 1;
        protected override void OnPhysicsProcess(double delta)
        {
            var map = GetWorld()!.NavigationMap;
            if (_phase == 0) { Check(NavigationServer.MapGetPath(map, Position, new(44, 44), true)[^1].X <= 20, "Disabled scene link cannot bridge the native gap."); Link.Enabled = true; Check(NavigationServer.MapGetPath(map, Position, new(44, 44), true)[^1].X <= 20, "Native source enablement waits for the next topology iteration."); _phase = 1; return; }
            if (_phase == 1) { Path = NavigationServer.MapGetPath(map, Position, new(44, 44), true); Check(Path.SequenceEqual(new Vector2[] { new(12, 44), new(18, 12), new(38, 12), new(44, 44) }), "Native actor obtains a linked corridor after publication."); Check(NavigationServer.MapGetPath(map, new(44, 44), new(12, 44), true)[^1].X >= 36, "Native directed link blocks reverse navigation."); _phase = 2; }
            var remaining = (float)(delta * 200); while (_next < Path.Length && remaining > 0) { var offset = Path[_next] - Position; var length = offset.Length(); if (length <= remaining) { Position = Path[_next++]; remaining -= length; } else { Position += offset / length * remaining; remaining = 0; } }
            Done = _next == Path.Length;
        }
        protected override void OnDraw() => DrawRect(new(-2, -2, 4, 4), Colors.Yellow);
    }
    private sealed class Journey : Entity
    {
        internal Traveler Traveler = null!; internal NavigationLink Link = null!;
        protected override void OnDraw() { DrawRect(new(4, 4, 16, 48), new(.2f, .2f, .2f)); DrawRect(new(36, 4, 16, 48), new(.2f, .2f, .2f)); DrawLine(Link.GetGlobalStartPosition(), Link.GetGlobalEndPosition(), Colors.Cyan, 2); if (Traveler.Path.Length > 1) DrawPolyline(Traveler.Path, Colors.Green, 2); }
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        using var left = NavigationTests.Rectangle(4, 4, 16, 48); using var right = NavigationTests.Rectangle(36, 4, 16, 48); var window = new Window { Size = new(64, 64) };
        window.AddChild(new NavigationRegion { Name = "Left", NavigationPolygon = left }); window.AddChild(new NavigationRegion { Name = "Right", NavigationPolygon = right }); var link = new NavigationLink { Name = "Crossing", StartPosition = new(18, 12), EndPosition = new(38, 12), Bidirectional = false, Enabled = false }; window.AddChild(link);
        var actor = new Traveler { Link = link, Position = new(12, 44), PhysicsProcessEnabled = true }; var journey = new Journey { Traveler = actor, Link = link }; window.AddChild(journey); window.AddChild(actor); var frames = 0; RID map = default, linkRID = default;
        window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend && RenderingServer.GetCurrentRenderingDriverName() != "software", "Requested hardware backend."); map = window.World!.NavigationMap; linkRID = link.GetRID(); RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () => { frames++; journey.QueueRedraw(); if (!actor.Done) { Check(frames < 180, "Bounded linked path-following host."); return; } using var pixels = window.GetTexture().GetImage()!; Check(pixels.GetPixel(44, 44).R > .9f && pixels.GetPixel(44, 44).G > .9f, "Link-guided actor reaches the real rendered goal pixel."); if (Environment.GetEnvironmentVariable("ELECTRON2D_NAVIGATION_LINK_SNAPSHOT") is { } capture) pixels.SavePNG(capture); window.Tree!.Quit(); }; };
        Engine.Run(window); Check(window.IsDisposed && RenderingServer.Service is null, "Linked native scene teardown."); Reject<ArgumentException>(() => NavigationServer.MapGetLinks(map)); Reject<ArgumentException>(() => NavigationServer.LinkGetMap(linkRID)); Console.WriteLine($"Navigation linked off-surface route, enablement/direction/pixels/cleanup passed: {backend}, {frames} frames.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
