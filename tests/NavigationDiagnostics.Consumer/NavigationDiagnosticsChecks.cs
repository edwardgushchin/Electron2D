using Electron2D;

internal static class NavigationDiagnosticsChecks
{
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static NavigationPolygon Rectangle(float x, float y, float width, float height)
    {
        var polygon = new NavigationPolygon();
        polygon.SetVertices([new(x, y), new(x + width, y), new(x + width, y + height), new(x, y + height)]); polygon.AddPolygon([0, 1, 2, 3]);
        return polygon;
    }
    internal static void Run(string profile)
    {
        Managed();
        if (profile != "managed") Native(profile);
    }
    private static void Managed()
    {
        var original = NavigationServer.GetDebugEnabled(); var events = new List<string>();
        void Navigation() { events.Add("navigation"); Reject<InvalidOperationException>(NavigationServer.Synchronize); }
        void Avoidance() => events.Add("avoidance");
        NavigationServer.NavigationDebugChanged += Navigation; NavigationServer.AvoidanceDebugChanged += Avoidance;
        try
        {
            NavigationServer.SetDebugEnabled(!original); NavigationServer.SetDebugEnabled(original); NavigationServer.SetDebugEnabled(!original);
            Check(events.Count == 0 && NavigationServer.GetDebugEnabled() != original, "Debug state publishes immediately; events are deferred.");
            NavigationServer.Synchronize(); Check(events.SequenceEqual(new[] { "navigation", "avoidance" }), "Changes coalesce, ordered events and reentry guard.");
            events.Clear(); NavigationServer.SetDebugEnabled(!original); NavigationServer.Synchronize(); Check(events.Count == 0, "Equal assignments do not notify.");
            var requeued = 0;
            void Requeue() { if (++requeued == 1) NavigationServer.SetDebugEnabled(!NavigationServer.GetDebugEnabled()); }
            NavigationServer.NavigationDebugChanged += Requeue;
            try
            {
                NavigationServer.SetDebugEnabled(original); NavigationServer.Synchronize();
                Check(requeued == 1, "A callback edit cannot recursively deliver another debug notification.");
                NavigationServer.Synchronize(); Check(requeued == 2, "A callback edit remains queued for the next boundary.");
            }
            finally { NavigationServer.NavigationDebugChanged -= Requeue; }
            events.Clear();
            var later = 0;
            void Bad() { throw new IOException("debug observer"); }
            void Good() { later++; }
            NavigationServer.NavigationDebugChanged += Bad; NavigationServer.NavigationDebugChanged += Good;
            try
            {
                NavigationServer.SetDebugEnabled(original); Reject<AggregateException>(NavigationServer.Synchronize);
                Check(later == 1 && events.SequenceEqual(new[] { "navigation", "avoidance" }), "Failure cannot skip later listeners or the other event.");
            }
            finally { NavigationServer.NavigationDebugChanged -= Bad; NavigationServer.NavigationDebugChanged -= Good; }
            events.Clear();
            var sender = Engine.GetSingleton<NavigationServer>("NavigationServer");
            sender.SetBlockSignals(true);
            try { NavigationServer.SetDebugEnabled(!original); NavigationServer.Synchronize(); Check(events.Count == 0, "Inherited signal blocking suppresses both events."); }
            finally { sender.SetBlockSignals(false); }
            NavigationServer.SetDebugEnabled(original); NavigationServer.Synchronize(); events.Clear();
            using var tree = new SceneTree(new Node());
            Task.Run(() => NavigationServer.SetDebugEnabled(!original)).GetAwaiter().GetResult();
            Check(events.Count == 0, "Worker does not invoke scene callbacks."); tree.ProcessFrame(0);
            Check(events.SequenceEqual(new[] { "navigation", "avoidance" }), "Scene process boundary pumps events without a physics tick.");
        }
        finally
        {
            NavigationServer.NavigationDebugChanged -= Navigation; NavigationServer.AvoidanceDebugChanged -= Avoidance;
            NavigationServer.SetDebugEnabled(original); NavigationServer.Synchronize();
        }
        var map = NavigationServer.MapCreate(); var agent = NavigationServer.AgentCreate(); var obstacle = NavigationServer.ObstacleCreate();
        try
        {
            NavigationServer.MapSetActive(map, true); NavigationServer.AgentSetMap(agent, map); NavigationServer.AgentSetAvoidanceEnabled(agent, true);
            NavigationServer.AgentSetVelocity(agent, new(3, 0)); NavigationServer.ObstacleSetMap(obstacle, map);
            var ids = NavigationServer.MapGetObstacles(map); Check(ids.SequenceEqual(new[] { obstacle }), "Obstacle membership is queryable before sync."); ids[0] = default;
            NavigationServer.ObstacleSetPaused(obstacle, true); Check(NavigationServer.MapGetObstacles(map)[0] == obstacle, "Copied membership includes paused obstacles.");
            var calls = 0; NavigationServer.AgentSetAvoidanceCallback(agent, _ => calls++);
            NavigationServer.SetActive(false); NavigationServer.Step(.1); Check(calls == 0, "Global inactivity suppresses avoidance callbacks.");
            var version = NavigationServer.MapGetIterationID(map); NavigationServer.SetActive(true); NavigationServer.Step(.1);
            Check(calls == 1 && NavigationServer.MapGetIterationID(map) == version && NavigationServer.MapIsActive(map), "Reactivation resumes preserved map/velocity state.");
            Reject<ArgumentOutOfRangeException>(() => NavigationServer.Step(double.NaN));
            NavigationServer.ObstacleSetMap(obstacle, default); Check(NavigationServer.MapGetObstacles(map).Length == 0, "Detachment is reflected immediately.");
            Reject<ArgumentException>(() => NavigationServer.MapGetObstacles(obstacle)); Reject<ArgumentException>(() => NavigationServer.MapGetObstacles(default));
        }
        finally { NavigationServer.SetActive(true); NavigationServer.FreeRID(agent); NavigationServer.FreeRID(obstacle); NavigationServer.FreeRID(map); }
        Reject<ArgumentException>(() => NavigationServer.MapGetObstacles(map));
        Console.WriteLine("Navigation controls: deferred/coalesced events, failure/reentry/worker delivery, copied membership and global suspend/resume passed.");
    }
    private static void Native(string renderer)
    {
        Engine.IsEditorHint = false; Engine.MaxFPS = 120;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.NavigationDebugGeometryFaceRandomColor, false);
        ProjectSettings.Set(ProjectSettings.NavigationDebugGeometryFaceColor, new Color(0, 1, 0));
        ProjectSettings.Set(ProjectSettings.NavigationDebugGeometryFaceDisabledColor, Colors.White);
        ProjectSettings.Set(ProjectSettings.NavigationDebugGeometryEdgeColor, new Color(0, 1, 0));
        ProjectSettings.Set(ProjectSettings.AvoidanceDebugAgentsRadiusColor, new Color(0, 0, 1));
        ProjectSettings.Set(ProjectSettings.AvoidanceDebugObstaclesRadiusColor, new Color(1, 0, 0));
        ProjectSettings.Set(ProjectSettings.AvoidanceDebugStaticFacePushInColor, new Color(1, 1, 0));
        ProjectSettings.Set(ProjectSettings.AvoidanceDebugStaticFacePushOutColor, new Color(1, 1, 0));
        using var polygon = Rectangle(10, 10, 65, 55);
        using var world = new World(PhysicsServer.Backend.CPU);
        using var window = new Window { Size = new(220, 140), World = world, Title = "Electron2D navigation diagnostics" };
        var region = new NavigationRegion { NavigationPolygon = polygon, Modulate = new(0, 0, 0, .2f) };
        var link = new NavigationLink { StartPosition = new(20, 100), EndPosition = new(75, 100), Bidirectional = true };
        var actor = new Entity { Position = new(130, 35), Modulate = new(.1f, .2f, .3f, .2f) }; var agent = new NavigationAgent { AvoidanceEnabled = true, Radius = 10 };
        actor.AddChild(agent);
        var obstacle = new NavigationObstacle { Position = new(130, 100), Radius = 10 };
        var contour = new NavigationObstacle { Position = new(185, 100), Vertices = [new(-15, -10), new(15, -10), new(0, 15)] };
        window.AddChild(region); window.AddChild(link); window.AddChild(actor); window.AddChild(obstacle); window.AddChild(contour);
        var original = NavigationServer.GetDebugEnabled(); NavigationServer.SetDebugEnabled(false); NavigationServer.SetActive(false);
        var frame = 0; var complete = false; Exception? failure = null; long startBytes = 0; CanvasLayer? layer = null;
        window.Ready += _ => { NavigationServer.MapSetUseAsyncIterations(world.NavigationMap, false); NavigationServer.RegionSetUseAsyncIterations(region.GetRID(), false); RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Drawn; };
        try { var exit = Engine.Run(window); if (failure is not null) throw failure; Check(exit == 0 && complete, "Native navigation sequence completes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Drawn; NavigationServer.SetActive(true); NavigationServer.SetDebugEnabled(original); Engine.IsEditorHint = false; }
        void Drawn()
        {
            try
            {
                frame++;
                if (frame is 1 or 3 or 5 or 7 or 9 or 12 or 15 or 19 or 23)
                {
                    using var image = window.GetTexture().GetImage()!;
                    if (frame is 19 or 23) Check(Is(image, 110, 30, new(0, 1, 0)) && Is(image, 30, 30, Colors.Black), "World geometry follows the viewport/canvas-layer basis exactly once.");
                    else if (frame is 1 or 9) Check(Is(image, 30, 30, Colors.Black) && Is(image, 130, 100, Colors.Black), "Disabled drawing is absent.");
                    else
                    {
                        Check(Is(image, 30, 30, frame is 5 or 7 ? Colors.White : new(0, 1, 0)), "Region faces and disabled color.");
                        Check(Is(image, 130, 35, frame == 7 ? Colors.Black : new(0, 0, 1)), "Avoidance radius and parent visibility.");
                        Check(Is(image, 130, 100, new(1, 0, 0)) && Is(image, 185, 100, new(1, 1, 0)), "Obstacle disc and oriented static fill.");
                        Check(Has(image, 40, 100, new(1, .5f, 1)), "Directional link connection draws on the real target.");
                    }
                    var output = Environment.GetEnvironmentVariable("E2D_NAVIGATION_OUTPUT");
                    if (output is not null) { Directory.CreateDirectory(output); image.SavePNG(System.IO.Path.Combine(output, renderer + "-" + frame + ".png")); }
                }
                if (frame == 1) NavigationServer.SetDebugEnabled(true);
                if (frame == 3) region.Enabled = false;
                if (frame == 5) actor.Visible = false;
                if (frame == 7) { actor.Visible = true; region.Enabled = true; NavigationServer.SetDebugEnabled(false); }
                if (frame == 9) window.Tree!.DebugNavigationHint = true;
                if (frame == 12) { Check(!NavigationServer.GetDebugEnabled(), "Tree-local hint leaves the global flag unchanged."); window.Tree!.DebugNavigationHint = false; Engine.IsEditorHint = true; }
                if (frame == 15) { Engine.IsEditorHint = false; NavigationServer.SetDebugEnabled(true); }
                if (frame == 17) window.CanvasTransform = Transform.Identity.Translated(new(80, 0));
                if (frame == 19)
                {
                    window.CanvasTransform = Transform.Identity;
                    layer = new CanvasLayer { Offset = new(80, 0) }; window.AddChild(layer); window.RemoveChild(region); layer.AddChild(region);
                }
                if (frame == 23) { layer!.RemoveChild(region); window.AddChild(region); layer.Dispose(); layer = null; }
                if (frame == 32) startBytes = GC.GetAllocatedBytesForCurrentThread();
                if (frame == 96) { var bytes = GC.GetAllocatedBytesForCurrentThread() - startBytes; Check(bytes == 0, "Prepared navigation owner frames allocated " + bytes); complete = true; window.Tree!.Quit(); }
            }
            catch (Exception error) { failure = error; window.Tree!.Quit(1); }
        }
        Console.WriteLine("Navigation " + renderer + ": real region/link/agent/obstacle pixels, global/tree/editor changes and 64 zero-owner-byte prepared frames passed.");
    }
    private static bool Is(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y);
        return Math.Abs(actual.R - expected.R) < .03f && Math.Abs(actual.G - expected.G) < .03f && Math.Abs(actual.B - expected.B) < .03f;
    }
    private static bool Has(Image image, int x, int y, Color color)
    {
        for (var iy = y - 2; iy <= y + 2; iy++) for (var ix = x - 2; ix <= x + 2; ix++) if (Is(image, ix, iy, color)) return true;
        return false;
    }
}
