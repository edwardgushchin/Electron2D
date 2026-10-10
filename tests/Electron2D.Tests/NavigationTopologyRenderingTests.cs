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
