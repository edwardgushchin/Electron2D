using Electron2D;

internal static class PolygonPathRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var authored = new Window { Size = new(220, 140) };
        using var source = new PolygonPathFinder(); source.Setup([new(10, 10), new(210, 10), new(210, 130), new(10, 130), new(85, 40), new(135, 40), new(135, 100), new(85, 100)], PolygonPathFinderTests.Edges);
        var holder = new ResourcePreloader { Name = "Routes" }; authored.AddChild(holder); holder.Owner = authored; holder.AddResource("area", source);
        var obstacle = new ColorRect { Name = "Hole", Position = new(85, 40), Size = new(50, 60), Color = Colors.Red }; authored.AddChild(obstacle); obstacle.Owner = authored;
        var line = new Line { Name = "Path", Width = 4, DefaultColor = Colors.Cyan }; authored.AddChild(line); line.Owner = authored;
        var mover = new ColorRect { Name = "Agent", Size = new(6, 6), Color = Colors.Yellow }; authored.AddChild(mover); mover.Owner = authored;
        using var scene = new PackedScene(); scene.Pack(authored);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-polygon-native-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var file = System.IO.Path.Combine(folder, "route.e2dscene"); ResourceSaver.Save(scene, file, SaverFlags.Compress | SaverFlags.SaveBigEndian);
            using var loaded = ResourceLoader.Load<PackedScene>(file, ResourceLoader.CacheMode.Ignore); using var window = (Window)loaded.Instantiate();
            var polygon = (PolygonPathFinder)((ResourcePreloader)window.GetNode("Routes")).GetResource("area"); var painted = (Line)window.GetNode("Path"); var agent = (ColorRect)window.GetNode("Agent");
            var route = polygon.FindPath(new(25, 70), new(195, 70)); painted.Points = route; agent.Position = route[0] - new Vector2(3, 3);
            Check(route.Length == 4 && route[1].Y == 40, "Loaded route starts above hole.");
            using var target = window.GetTexture(); var frames = 0; long before = 0, allocated = 0;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                window.Tree!.ProcessFrameStarted += _ =>
                {
                    before = GC.GetAllocatedBytesForCurrentThread();
                    agent.Position = Along(route, Math.Clamp((frames - 3) / 120f, 0, 1)) - new Vector2(3, 3);
                    Check(polygon.IsPointInside(new(25, 70)) && !polygon.IsPointInside(new(110, 70)), "Rendered scene retains executable area.");
                    polygon.GetClosestPoint(new(110, 70)); polygon.GetBounds(); polygon.GetPointPenalty(4);
                };
                RenderingServer.FramePostDraw += Capture;
            };
            void Capture()
            {
                if (frames >= 71) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                frames++;
                if (frames is 1 or 2 or 135)
                {
                    using var image = target.GetImage() ?? throw new InvalidOperationException("Polygon route readback.");
                    Pixel(image, 110, frames == 1 ? 40 : 100, Colors.Cyan); Pixel(image, 110, 70, Colors.Red);
                    if (frames == 135) Pixel(image, 195, 70, Colors.Yellow);
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } capture) { Directory.CreateDirectory(capture); image.SavePNG(System.IO.Path.Combine(capture, "polygon-" + backend + "-" + frames + ".png")); }
                }
                if (frames == 1) { polygon.SetPointPenalty(4, 400); route = polygon.FindPath(new(25, 70), new(195, 70)); painted.Points = route; Check(route[1].Y == 100, "Penalty replans below visible hole."); }
                if (frames < 135) return;
                Check(allocated == 0, "64 warm full polygon movement/query frame bytes=" + allocated); RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
            try { Check(Engine.Run(window) == 0 && frames == 135, "Polygon navigation host finished."); }
            finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
            Console.WriteLine("PolygonPathFinder " + backend + ": loaded compressed big-endian scene, visible hole/penalty replanning, agent traversal and 64 warm full movement/query frames=0 managed bytes passed.");
        }
        finally { Directory.Delete(folder, true); }
    }
    private static Vector2 Along(Vector2[] route, float progress)
    {
        float total = 0; for (var i = 1; i < route.Length; i++) total += route[i - 1].DistanceTo(route[i]); var left = total * progress;
        for (var i = 1; i < route.Length; i++) { var length = route[i - 1].DistanceTo(route[i]); if (left <= length) return route[i - 1].Lerp(route[i], length > 0 ? left / length : 0); left -= length; }
        return route[^1];
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var c = image.GetPixel(x, y); Check(Math.Abs(c.R - expected.R) < .06f && Math.Abs(c.G - expected.G) < .06f && Math.Abs(c.B - expected.B) < .06f, "Polygon pixel " + x + "," + y + "=" + c); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
