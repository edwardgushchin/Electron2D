using Electron2D;
using static TextureArrayTests;

internal static class NavigationGlobalDebugTests
{
    private static NavigationDebugCanvas Canvas(Node source) => source.GetChildren(includeInternal: true).OfType<NavigationDebugCanvas>().Single();
    private static List<CanvasVertex> Draw(Node source)
    {
        var canvas = Canvas(source); canvas.PrepareCanvas(); var vertices = new List<CanvasVertex>(); canvas.AppendCanvas(vertices, [], Transform.Identity); return vertices;
    }
    internal static void Run()
    {
        using var synchronous = new NavigationIterationTests.SynchronousDefaults();
        ProjectSettings.Set(ProjectSettings.NavigationDebugGeometryFaceRandomColor, false);
        var debug = NavigationServer.GetDebugEnabled(); var editor = Engine.IsEditorHint;
        NavigationServer.SetDebugEnabled(false); Engine.IsEditorHint = false;
        using var polygon = NavigationTests.Rectangle(0, 0, 10, 10);
        using var second = NavigationTests.Rectangle(12, 0, 10, 10);
        using var root = new SubViewport { Size = new(200, 160) };
        var region = new NavigationRegion { NavigationPolygon = polygon, Position = new(20, 20) };
        var region2 = new NavigationRegion { NavigationPolygon = second, Position = new(20, 20) };
        var link = new NavigationLink { StartPosition = new(20, 80), EndPosition = new(80, 80) };
        var actor = new Entity { Position = new(100, 40) }; var agent = new NavigationAgent { AvoidanceEnabled = true, Radius = 5 }; actor.AddChild(agent);
        var obstacle = new NavigationObstacle { Position = new(100, 110), Radius = 8, Vertices = [new(-10, -10), new(10, -10), new(10, 10), new(-10, 10)] };
        root.AddChild(region); root.AddChild(region2); root.AddChild(link); root.AddChild(actor); root.AddChild(obstacle);
        using var tree = new SceneTree(root);
        try
        {
            Check(new Node[] { region, link, agent, obstacle }.All(n => n.GetChildCount() == 0 && n.GetChildCount(true) == 1), "Prepared global canvases stay internal.");
            NavigationServer.MapSetEdgeConnectionMargin(root.World!.NavigationMap, 4); NavigationServer.Synchronize();
            Check(Draw(region).Count == 0 && Draw(link).Count == 0 && Draw(agent).Count == 0 && Draw(obstacle).Count == 0, "Global diagnostics default off.");
            NavigationServer.SetDebugEnabled(true);
            var version = NavigationServer.MapGetIterationID(root.World.NavigationMap);
            var settings = NavigationServer.Service.GeometryDebugSettings;
            Check(NavigationServer.RegionGetConnectionsCount(region.GetRID()) > 0 && Draw(region).Any(v => v.Color == settings.EdgeConnectionColor), "Committed margin pathways have a drawing consumer.");
            region.UseEdgeConnections = false; Check(Draw(region).All(v => v.Color != settings.EdgeConnectionColor), "Region connection policy suppresses pathways without changing query state."); region.UseEdgeConnections = true;
            var vertices = Draw(region); Check(vertices.Any(v => v.Color == settings.GeometryFaceColor) && vertices.Any(v => v.Position == new Vector2(20, 20)), "Authored region world geometry and face color execute.");
            region.Enabled = false; Check(Draw(region).Any(v => v.Color == settings.GeometryFaceDisabledColor), "Disabled faces have their own color."); region.Enabled = true;
            var both = Draw(link).Count; link.Bidirectional = false; Check(Draw(link).Count < both, "The reverse arrow only draws for bidirectional links.");
            link.Enabled = false; Check(Draw(link).All(v => v.Color == settings.LinkConnectionDisabledColor), "Disabled link color."); link.Enabled = true;
            Check(Draw(agent).All(v => v.Color == settings.AgentsRadiusColor) && Draw(agent).Count > 0, "Scene avoidance-agent radius has a producer.");
            actor.Modulate = new(.1f, .2f, .3f, .2f); Check(Draw(agent).All(v => v.Color == settings.AgentsRadiusColor), "Global diagnostics retain their own tint."); actor.Modulate = Colors.White;
            var before = Draw(agent).Select(v => v.Position).ToArray(); actor.Position += new Vector2(15, 8); var after = Draw(agent).Select(v => v.Position).ToArray();
            Check(after.Zip(before).All(p => (p.First - p.Second).IsEqualApprox(new(15, 8))), "World-space radii follow the parent without a physics step.");
            actor.Visible = false; Check(Draw(agent).Count == 0, "Hidden agent parent retires commands."); actor.Visible = true;
            agent.AvoidanceEnabled = false; Check(Draw(agent).Count == 0, "Disabled agents do not draw a participating radius."); agent.AvoidanceEnabled = true;
            obstacle.Scale = new(-2, 3); vertices = Draw(obstacle);
            Check(vertices.Any(v => v.Color == settings.ObstaclesRadiusColor) && vertices.Any(v => v.Position.Y > 130), "Maximum-axis obstacle radius survives reflection and nonuniform scale.");
            var edge = !Geometry.IsPolygonClockwise(obstacle.DebugVertices) ? settings.StaticEdgePushOutColor : settings.StaticEdgePushInColor;
            Check(vertices.Any(v => v.Color == edge), "Oriented static obstacle edges draw.");
            obstacle.Vertices = obstacle.Vertices.Reverse().ToArray(); vertices = Draw(obstacle);
            var reversed = !Geometry.IsPolygonClockwise(obstacle.DebugVertices) ? settings.StaticEdgePushOutColor : settings.StaticEdgePushInColor;
            Check(edge != reversed && vertices.Any(v => v.Color == reversed), "Winding selects push-in versus push-out color.");
            obstacle.Scale = Vector2.Zero; Check(Draw(obstacle).Count > 0, "Singular source transforms require no inverse and keep the bounded radius producer."); obstacle.Scale = Vector2.One;
            NavigationServer.SetDebugEnabled(false); tree.DebugNavigationHint = true;
            Check(!NavigationServer.GetDebugEnabled() && Draw(region).Count > 0 && Draw(obstacle).Count > 0, "Tree hint independently enables all scene diagnostics.");
            tree.DebugNavigationHint = false; Check(Draw(region).Count == 0, "Tree hint disables retained commands."); Engine.IsEditorHint = true; Check(Draw(region).Count > 0, "Editor authoring has the same concrete consumer."); Engine.IsEditorHint = false;
            NavigationServer.SetDebugEnabled(true);
            var output = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); var canvas = Canvas(region);
            void Replay() { canvas.PrepareCanvas(); output.Clear(); batches.Clear(); canvas.AppendCanvas(output, batches, Transform.Identity); }
            for (var i = 0; i < 128; i++) { region.Enabled = (i & 1) == 0; Replay(); }
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) { region.Enabled = (i & 1) == 0; Replay(); }
            Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "128 changed face/edge replay intervals allocate zero owner bytes.");
            allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Replay(); Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Unchanged snapshots reuse commands.");
            Check(NavigationServer.MapGetIterationID(root.World.NavigationMap) == version, "Drawing cannot commit or query navigation topology.");
            var scenePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-nav-global-" + Guid.NewGuid().ToString("N") + ".e2dscene");
            try
            {
                using var authored = new Entity(); var stored = new NavigationRegion { NavigationPolygon = polygon }; authored.AddChild(stored); stored.Owner = authored;
                ResourceSaver.SaveScene(authored, scenePath); using var packed = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.Ignore); using var copy = packed.Instantiate();
                var restored = (NavigationRegion)copy.GetChild(0); Check(copy.GetChildCount() == 1 && restored.GetChildCount() == 0 && restored.GetChildCount(true) == 1, "Scene files reconstruct exactly one transient constructor-owned canvas.");
            }
            finally { File.Delete(scenePath); }
            root.RemoveChild(obstacle); Check(!Canvas(obstacle).IsInsideTree && Draw(obstacle).Count == 0, "Detached nodes retire diagnostic membership."); root.AddChild(obstacle);
            var owned = Canvas(obstacle); obstacle.Dispose(); Check(owned.IsDisposed, "Source disposal owns diagnostic canvas lifetime.");
        }
        finally { NavigationServer.SetDebugEnabled(debug); NavigationServer.Synchronize(); Engine.IsEditorHint = editor; }
        NavigationDebugTests.RunChild("ELECTRON2D_TEST_NAVIGATION_GLOBAL_SETTINGS_CHILD", "1");
        Console.WriteLine("Global navigation diagnostics: all scene producers, pathway/arrow/winding/scaling policies, visibility/tree/editor state, storage/lifetime and 128 active/idle zero-owner-byte intervals passed.");
    }
    internal static void SettingsChild()
    {
        using var synchronous = new NavigationIterationTests.SynchronousDefaults();
        ProjectSettings.Set(ProjectSettings.NavigationDebugEdgeConnections, false);
        ProjectSettings.Set(ProjectSettings.NavigationDebugEdgeLines, false);
        ProjectSettings.Set(ProjectSettings.NavigationDebugGeometryFaceRandomColor, false);
        ProjectSettings.Set(ProjectSettings.NavigationDebugLinkConnections, false);
        ProjectSettings.Set(ProjectSettings.AvoidanceDebugAgentsRadius, false);
        ProjectSettings.Set(ProjectSettings.AvoidanceDebugObstaclesRadius, false);
        ProjectSettings.Set(ProjectSettings.AvoidanceDebugObstaclesStatic, false);
        var color = new Color(.2f, .7f, .3f, .6f);
        ProjectSettings.Set(ProjectSettings.NavigationDebugGeometryFaceColor, Colors.Red);
        ProjectSettings.SetFeatureOverride(ProjectSettings.NavigationDebugGeometryFaceColor, "nav-global-test", color); ProjectSettings.AddCustomFeature("nav-global-test");
        NavigationServer.SetDebugEnabled(true);
        using var a = NavigationTests.Rectangle(0, 0, 10, 10); using var b = NavigationTests.Rectangle(12, 0, 10, 10);
        using var root = new SubViewport(); var region = new NavigationRegion { NavigationPolygon = a }; var second = new NavigationRegion { NavigationPolygon = b };
        var link = new NavigationLink { StartPosition = new(0, 20), EndPosition = new(20, 20) }; var actor = new Entity(); var agent = new NavigationAgent { AvoidanceEnabled = true, Radius = 10 }; actor.AddChild(agent);
        var obstacle = new NavigationObstacle { Radius = 10, Vertices = [new(0, 0), new(20, 0), new(0, 20)] };
        root.AddChild(region); root.AddChild(second); root.AddChild(link); root.AddChild(actor); root.AddChild(obstacle);
        using var tree = new SceneTree(root); NavigationServer.MapSetEdgeConnectionMargin(root.World!.NavigationMap, 4); NavigationServer.Synchronize();
        Check(NavigationServer.RegionGetConnectionsCount(region.GetRID()) > 0, "The disabled setting test has real margin connections.");
        var drawn = Draw(region); Check(drawn.Count == 6 && drawn.All(v => v.Color == color), "Face/random/edge/pathway settings all reach recording.");
        Check(Draw(link).Count == 0 && Draw(agent).Count == 0 && Draw(obstacle).Count == 0, "Link, agent-radius, obstacle-radius and static settings suppress their actual producers.");
        ProjectSettings.Set(ProjectSettings.NavigationDebugEdgeLines, true); ProjectSettings.Set(ProjectSettings.NavigationDebugGeometryFaceColor, Colors.Blue);
        Check(Draw(region).Count == 6 && Draw(region).All(v => v.Color == color), "Restart-bound settings retain the first configured snapshot.");
        Check(!RenderingServer.IsAvailable, "Pure diagnostic recording does not open a renderer.");
        Console.WriteLine("Fresh navigation diagnostics passed: all seven disabled producer switches, exact face color and immutable restart snapshot.");
    }
}
