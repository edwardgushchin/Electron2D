using Electron2D;

internal static class NavigationTopologyRenderingTests
{
    private sealed class Walker : Entity
    {
        internal bool Done;
        protected override void OnPhysicsProcess(double delta)
        {
            var path = NavigationServer.MapGetPath(GetWorld()!.NavigationMap, Position, new(44, 28), true);
            if (path.Length < 2) return;
            Position = Position.MoveToward(path[^1], 1); Done = Position.DistanceTo(new(44, 28)) < .01f;
        }
        protected override void OnDraw() => DrawRect(new(-1, -1, 3, 3), Colors.White);
    }
    private sealed class Surface : Entity
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 24, 48), new(.1f, .3f, .1f));
    }
    private sealed class FrontierWalker : Entity
    {
        internal NavigationAgent Agent { get; } = new() { PathDesiredDistance = .2f, TargetDesiredDistance = .2f };
        protected override void OnPhysicsProcess(double delta)
        {
            var next = Agent.GetNextPathPosition();
            Position = Position.MoveToward(next, 1);
        }
        protected override void OnDraw() => DrawRect(new(-1, -1, 3, 3), Colors.White);
    }

    internal static void RunFrontierHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 60;
        using var start = NavigationTests.Rectangle(0, 0, 10, 10);
        using var east = NavigationTests.Rectangle(10, 0, 40, 10);
        using var south = NavigationTests.Rectangle(0, 10, 10, 10);
        using var island = NavigationTests.Rectangle(35, 30, 10, 10);
        var root = new Window { Size = new(64, 64) };
        foreach (var polygon in new[] { start, east, south, island })
        {
            var region = new NavigationRegion { NavigationPolygon = polygon };
            NavigationServer.RegionSetUseAsyncIterations(region.GetRID(), false);
            root.AddChild(region);
        }
        var walker = new FrontierWalker { Position = new(8, 2), PhysicsProcessEnabled = true };
        walker.AddChild(walker.Agent);
        root.AddChild(walker);
        var frames = 0;
        var completions = 0;
        walker.Agent.NavigationFinished += () => completions++;
        root.Ready += _ =>
        {
            var map = root.World!.NavigationMap;
            NavigationServer.MapSetUseAsyncIterations(map, false);
            NavigationServer.MapSetUseEdgeConnections(map, false);
            RenderingServer.SetDefaultClearColor(Colors.Black);
            walker.Agent.TargetPosition = new(40, 35);
            RenderingServer.FramePostDraw += () =>
            {
                Check(++frames < 240, "The frontier walker finishes within 240 frames.");
                if (completions == 0)
                    return;
                Check(RenderingServer.GetCurrentRenderingMethod() == backend, "The requested renderer is active.");
                Check(completions == 1 && walker.Agent.GetFinalPosition() == new Vector2(10, 20) &&
                    !walker.Agent.IsTargetReachable() && !walker.Agent.IsTargetReached(), "The reached frontier completes without reaching the disconnected goal.");
                using var image = root.GetTexture().GetImage()!;
                Check(image.GetPixel(10, 20) == Colors.White && image.GetPixel(40, 35) == Colors.Black,
                    "The visible agent ends at the reached frontier.");
                root.Tree!.Quit();
            };
        };
        Check(Engine.Run(root) == 0 && completions == 1, "The frontier host terminates after one completion.");
        Console.WriteLine($"Navigation frontier agent and rendered endpoint passed: {backend}, {frames} frames.");
    }

    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        using var a = Rectangle(); using var b = Rectangle();
        var root = new Window { Size = new(64, 64) }; var left = new NavigationRegion { Name = "Left", Position = new(4, 4), NavigationPolygon = a }; var right = new NavigationRegion { Name = "Right", Position = new(28.04f, 4), NavigationPolygon = b };
        var walker = new Walker { Position = new(12, 28), PhysicsProcessEnabled = true }; root.AddChild(left); root.AddChild(right); root.AddChild(new Surface { Name = "LeftSurface", Position = left.Position }); root.AddChild(new Surface { Name = "RightSurface", Position = right.Position }); root.AddChild(walker); var frames = 0; RID map = default;
        root.Ready += _ =>
        {
            map = root.World!.NavigationMap; NavigationServer.MapSetCellSize(map, 1); NavigationServer.MapSetMergeRasterizerCellScale(map, .1f); NavigationServer.MapSetUseEdgeConnections(map, false); RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                frames++; if (!walker.Done) { Check(frames < 120, "Raster-connected walker completes."); return; }
                Check(RenderingServer.GetCurrentRenderingMethod() == backend && RenderingServer.GetCurrentRenderingDriverName() != "software", "Requested hardware backend.");
                Check(NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.PolygonCount) >= 2 && NavigationServer.GetProcessInfo(NavigationServer.ProcessInfo.EdgeConnectionCount) >= 1 && NavigationServer.RegionGetConnectionsCount(left.GetRID()) == 0, "Scene physics lane publishes real raster profiles independently of margin pathways.");
                using var image = root.GetTexture().GetImage()!; Check(image.GetPixel(44, 28).R > .9f, "Actor crosses the quantized gap and reaches actual goal pixels.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_NAVIGATION_SNAPSHOT") is { } capture) image.SavePNG(capture); root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(root.IsDisposed && RenderingServer.Service is null, "Native topology scene cleanup.");
        try { NavigationServer.MapGetCellSize(map); throw new InvalidOperationException("World map should be released."); } catch (ArgumentException) { }
        Console.WriteLine($"Navigation topology raster gap, scene publication/profiling, route-driven pixels and cleanup passed: {backend}, {frames} frames.");
    }
    private static NavigationPolygon Rectangle() { var p = new NavigationPolygon(); p.SetVertices([new(0, 0), new(24, 0), new(24, 48), new(0, 48)]); p.AddPolygon([0, 1, 2, 3]); return p; }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
