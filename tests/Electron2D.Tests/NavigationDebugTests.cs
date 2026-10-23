using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static class NavigationDebugTests
{
    private static NavigationAgent Agent() => new() { Name = "Agent", DebugEnabled = true, PathDesiredDistance = 1, TargetDesiredDistance = 1, TargetPosition = new(100, 30) };
    private static NavigationAgentDebugCanvas Canvas(NavigationAgent agent) => (NavigationAgentDebugCanvas)agent.GetChild(0, includeInternal: true);
    private static List<CanvasVertex> Draw(NavigationAgent agent)
    {
        var canvas = Canvas(agent); canvas.PrepareCanvas(); var vertices = new List<CanvasVertex>(); canvas.AppendCanvas(vertices, [], Transform.Identity); return vertices;
    }
    internal static void Run()
    {
        using var synchronous = new NavigationIterationTests.SynchronousDefaults();
        using var defaults = new NavigationAgent();
        Check(!defaults.DebugEnabled && !defaults.DebugUseCustom && defaults.DebugPathCustomColor == Colors.White && defaults.DebugPathCustomLineWidth == -1 && defaults.DebugPathCustomPointSize == 4 && defaults.GetChildCount(includeInternal: true) == 0, "All five defaults and lazy canvas ownership.");
        defaults.DebugPathCustomPointSize = -5; Check(defaults.DebugPathCustomPointSize == 0, "Negative point sizes clamp.");
        Reject<ArgumentException>(() => defaults.DebugPathCustomColor = new(float.NaN, 0, 0)); Reject<ArgumentOutOfRangeException>(() => defaults.DebugPathCustomLineWidth = float.NaN); Reject<ArgumentOutOfRangeException>(() => defaults.DebugPathCustomPointSize = float.PositiveInfinity);
        using var polygon = NavigationTests.Rectangle(0, 0, 180, 100); using var root = new SubViewport { Size = new(180, 100) }; var region = new NavigationRegion { NavigationPolygon = polygon }; var actor = new Entity { Position = new(20, 30) }; var agent = Agent(); actor.AddChild(agent); root.AddChild(region); root.AddChild(actor); using var tree = new SceneTree(root);
        NavigationServer.Synchronize(); agent.GetNextPathPosition(); var index = agent.GetCurrentNavigationPathIndex(); var mapVersion = NavigationServer.MapGetIterationID(root.World!.NavigationMap);
        var vertices = Draw(agent); Check(vertices.Count > 0 && vertices.All(v => v.Color == Colors.Red) && agent.GetCurrentNavigationPathIndex() == index && NavigationServer.MapGetIterationID(root.World.NavigationMap) == mapVersion, "Debug recording reads the path without stepping navigation.");
        Check(agent.GetChildCount() == 0 && Canvas(agent).ZIndex == CanvasItem.MaximumZIndex - 1 && !Canvas(agent).ZAsRelative && Canvas(agent).CanvasUsesWorldCoordinates, "The helper is internal and uses the canvas overlay/world role.");
        agent.DebugUseCustom = true; agent.DebugPathCustomColor = Colors.Blue; agent.DebugPathCustomLineWidth = 3; agent.DebugPathCustomPointSize = 8;
        vertices = Draw(agent); Check(vertices.All(v => v.Color == Colors.Blue) && vertices.Any(v => v.Position == new Vector2(104, 34)), "Custom color, line and square point geometry execute.");
        actor.Position = new(30, 70); Check(Draw(agent).Select(v => v.Position).SequenceEqual(vertices.Select(v => v.Position)), "Parent movement does not move the retained world path.");
        actor.Visible = false; Check(Draw(agent).Count == 0, "Parent hide clears path commands."); actor.Visible = true; Check(Draw(agent).Count > 0, "Parent show restores current path.");
        agent.DebugEnabled = false; Check(Draw(agent).Count == 0, "Disabled diagnostics retire commands."); agent.DebugEnabled = true;
        agent.GetCurrentNavigationResult().Path = [new(10, 60), new(90, 60)]; Check(Draw(agent).All(v => v.Position.Y >= 56), "A replaced current result is observed without a path query.");
        agent.DebugPathCustomPointSize = 0; agent.DebugUseCustom = false; vertices = Draw(agent); Check(vertices.Count > 0 && vertices.All(v => v.Position.Y < 62), "Custom zero point size suppresses default waypoints while preserving a default-colored line.");
        agent.DebugPathCustomLineWidth = 0; Check(Draw(agent).Count == 0, "Zero width plus zero points emits no geometry."); agent.DebugPathCustomLineWidth = -1; agent.DebugPathCustomPointSize = 4;
        var canvas = Canvas(agent); var output = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        void Replay() { canvas.PrepareCanvas(); output.Clear(); batches.Clear(); canvas.AppendCanvas(output, batches, Transform.Identity); }
        for (var i = 0; i < 128; i++) { agent.DebugUseCustom = (i & 1) != 0; Replay(); }
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) { agent.DebugUseCustom = (i & 1) != 0; Replay(); }
        var activeOwner = GC.GetAllocatedBytesForCurrentThread() - owner; var activeAll = GC.GetTotalAllocatedBytes(true) - all;
        Check(activeOwner == 0 && activeAll == 0, $"128 active path redraw/replay intervals: {activeOwner}/{activeAll} owner/all-thread managed bytes.");
        owner = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Replay(); Check(GC.GetAllocatedBytesForCurrentThread() == owner, "Idle path replay allocates zero owner bytes.");
        // ThreadPool startup belongs outside the all-thread allocation interval.
        Task.Run(() => Reject<InvalidOperationException>(() => agent.DebugEnabled = true)).GetAwaiter().GetResult();
        var capture = root.BeginSceneCapture(); try { Reject<InvalidOperationException>(() => agent.DebugEnabled = false); Reject<InvalidOperationException>(() => agent.DebugPathCustomColor = Colors.White); } finally { Node.EndSceneCapture(capture); }
        agent.GetCurrentNavigationResult().Path = [new(10, 60)]; Check(Draw(agent).Count == 0, "A single-point path produces no debug drawing.");
        Fresh(); FreshSettings(); actor.RemoveChild(agent); Check(!canvas.IsInsideTree, "Detached agents retire the canvas membership."); actor.AddChild(agent); NavigationServer.Synchronize(); agent.GetNextPathPosition(); Check(Draw(agent).Count > 0, "Reentry recreates only current navigation drawing.");
        agent.Dispose(); Check(canvas.IsDisposed, "Agent disposal owns canvas retirement."); Reject<ObjectDisposedException>(() => agent.DebugEnabled = true);
        Teardown(); CreationFailure();
        Console.WriteLine("Navigation agent diagnostics: defaults, world path/visibility, custom geometry, edited results, ownership, scene archives and warm active/idle zero-byte replay passed.");
    }
    private static void CreationFailure()
    {
        using var root = new SubViewport(); var actor = new Entity(); var agent = new NavigationAgent(); root.AddChild(actor); actor.AddChild(agent); using var tree = new SceneTree(root);
        void Added(SceneTree _, Node node) { if (node is NavigationAgentDebugCanvas) throw new InvalidOperationException("add observer"); }
        void Removed(SceneTree _, Node node) { if (node is NavigationAgentDebugCanvas) throw new InvalidOperationException("remove observer"); }
        tree.NodeAdded += Added; tree.NodeRemoved += Removed;
        try { agent.DebugEnabled = true; throw new InvalidOperationException("Expected attachment failure."); }
        catch (AggregateException error) { var messages = error.Flatten().InnerExceptions.Select(e => e.Message).ToArray(); Check(messages.Contains("add observer") && messages.Contains("remove observer"), "Failed creation preserves both attachment and cleanup failures."); }
        tree.NodeAdded -= Added; tree.NodeRemoved -= Removed;
        Check(!agent.DebugEnabled && agent.GetChildCount(includeInternal: true) == 0, "Failed creation leaves no retained helper or enabled flag.");
        agent.DebugEnabled = true; Check(agent.GetChildCount(includeInternal: true) == 1, "A later enable recovers after failed creation.");
    }
    private static void Teardown()
    {
        using var root = new SubViewport(); using var tree = new SceneTree(root);
        var actor = new Entity(); root.AddChild(actor);
        foreach (var kind in new[] { 0, 1, 2, 3 })
        {
            Node node = kind switch { 0 => new NavigationAgent { DebugEnabled = true }, 1 => new NavigationRegion(), 2 => new NavigationLink(), _ => new NavigationObstacle() };
            (kind == 0 ? (Node)actor : root).AddChild(node);
            var rid = kind switch { 0 => ((NavigationAgent)node).GetRID(), 1 => ((NavigationRegion)node).GetRID(), 2 => ((NavigationLink)node).GetRID(), _ => ((NavigationObstacle)node).GetRID() };
            RID Map() => kind switch { 0 => NavigationServer.AgentGetMap(rid), 1 => NavigationServer.RegionGetMap(rid), 2 => NavigationServer.LinkGetMap(rid), _ => NavigationServer.ObstacleGetMap(rid) };
            var exits = 0;
            node.TreeExiting += _ => { Check(!Map().IsValid(), "Exit unbinds the map before TreeExiting without retiring the identity."); NavigationServer.Synchronize(); Map(); Task.Run(() => Reject<ArgumentException>(() => Map())).GetAwaiter().GetResult(); Map(); exits++; throw new InvalidOperationException("exit observer"); };
            try { node.Dispose(); throw new InvalidOperationException("Expected exit observer failure."); }
            catch (AggregateException error) { Check(error.Flatten().InnerExceptions.Count == 1 && error.Flatten().InnerExceptions[0].Message == "exit observer", "Teardown preserves the observer error without a stale-RID error."); }
            Check(exits == 1 && node.IsDisposed && node.Parent is null, "Failed observers do not retain node membership."); Reject<ArgumentException>(() => Map());
        }
    }
    private static void Fresh()
    {
        using var actor = new Entity { Name = "Actor" }; var agent = Agent(); agent.DebugUseCustom = true; agent.DebugPathCustomColor = Colors.Green; agent.DebugPathCustomLineWidth = 5; agent.DebugPathCustomPointSize = 7; actor.AddChild(agent); agent.Owner = actor;
        using var scene = new PackedScene(); scene.Pack(actor); var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-nav-debug-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            ResourceSaver.Save(scene, path); RunChild("ELECTRON2D_TEST_NAVIGATION_DEBUG_CHILD", path);
        }
        finally { File.Delete(path); }
    }
    private static void RunChild(string flag, string path)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(NavigationDebugTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_NAVIGATION_DEBUG"); start.Environment.Remove("ELECTRON2D_TEST_NAVIGATION_DEBUG_NATIVE"); start.Environment[flag] = path;
        using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Navigation debug child."); }
        Check(child.ExitCode == 0 && stdout.GetAwaiter().GetResult().Contains("Fresh navigation diagnostics passed"), stderr.GetAwaiter().GetResult());
    }
    private static void FreshSettings()
    {
        using var registry = new ProjectSettingsRegistry(System.IO.Path.GetTempPath(), System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-nav-debug-user"));
        Check(registry.IsRestartRequired(ProjectSettings.NavigationDebugAgentPaths) && registry.IsRestartRequired(ProjectSettings.NavigationDebugAgentPathColor) && registry.IsRestartRequired(ProjectSettings.NavigationDebugAgentPathPointSize) && !registry.IsBasic(ProjectSettings.NavigationDebugAgentPaths), "Startup diagnostic settings expose restart metadata and nonbasic discovery.");
        Reject<System.Text.Json.JsonException>(() => registry.Set(ProjectSettings.NavigationDebugAgentPathColor, new Color(float.NaN, 0, 0)));
        Reject<ArgumentOutOfRangeException>(() => registry.Set(ProjectSettings.NavigationDebugAgentPathPointSize, -1));
        Reject<ArgumentException>(() => registry.Set(ProjectSettings.NavigationDebugAgentPathPointSize, float.PositiveInfinity));
        registry.Set(ProjectSettings.NavigationDebugAgentPathColor, Colors.Blue); registry.Set(ProjectSettings.NavigationDebugAgentPathPointSize, 2);
        registry.SetFeatureOverride(ProjectSettings.NavigationDebugAgentPathColor, "nav-debug-test", Colors.Green); registry.SetFeatureOverride(ProjectSettings.NavigationDebugAgentPathPointSize, "nav-debug-test", 8);
        foreach (var enabled in new[] { false, true })
        {
            registry.SetFeatureOverride(ProjectSettings.NavigationDebugAgentPaths, "nav-debug-test", enabled);
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-nav-settings-" + Guid.NewGuid().ToString("N") + ".json");
            try { registry.SaveCustom(path); RunChild("ELECTRON2D_TEST_NAVIGATION_DEBUG_SETTINGS", path); } finally { File.Delete(path); }
        }
    }
    internal static void ChildSettings(string path)
    {
        ProjectSettings.LoadCustom(path); ProjectSettings.AddCustomFeature("nav-debug-test"); var enabled = ProjectSettings.GetWithOverride(ProjectSettings.NavigationDebugAgentPaths);
        using var root = new SubViewport(); var actor = new Entity(); var agent = Agent(); root.AddChild(actor); actor.AddChild(agent); using var tree = new SceneTree(root);
        agent.GetCurrentNavigationResult().Path = [new(20, 30), new(100, 30)]; var vertices = Draw(agent);
        Check(enabled ? vertices.Count > 0 && vertices.All(v => v.Color == Colors.Green) && vertices.Any(v => v.Position == new Vector2(104, 34)) : vertices.Count == 0, "Loaded feature overrides drive default enablement, color and point geometry.");
        ProjectSettings.SetFeatureOverride(ProjectSettings.NavigationDebugAgentPaths, "nav-debug-test", !enabled); ProjectSettings.SetFeatureOverride(ProjectSettings.NavigationDebugAgentPathColor, "nav-debug-test", Colors.Blue); agent.DebugUseCustom = false;
        var after = Draw(agent); Check(after.Select(v => (v.Position, v.Color)).SequenceEqual(vertices.Select(v => (v.Position, v.Color))), "Retained startup defaults do not resample after project edits.");
        Console.WriteLine("Fresh navigation diagnostics passed.");
    }
    internal static void Child(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var actor = scene.Instantiate(); var agent = (NavigationAgent)actor.GetNode("Agent");
        Check(agent.DebugEnabled && agent.DebugUseCustom && agent.DebugPathCustomColor == Colors.Green && agent.DebugPathCustomLineWidth == 5 && agent.DebugPathCustomPointSize == 7 && agent.GetChildCount() == 0 && agent.GetChildCount(includeInternal: true) == 1, "Fresh scene restores all properties and constructs one fresh transient canvas.");
        Console.WriteLine("Fresh navigation diagnostics passed.");
    }
    internal static void Native(string backend)
    {
        using var synchronous = new NavigationIterationTests.SynchronousDefaults();
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var polygon = NavigationTests.Rectangle(0, 0, 180, 100); using var window = new Window { Size = new(180, 100) }; var region = new NavigationRegion { NavigationPolygon = polygon }; var actor = new Entity { Position = new(20, 30) }; var agent = Agent(); actor.AddChild(agent); window.AddChild(region); window.AddChild(actor);
        var frame = 0; long before = 0, active = 0, idle = 0; var index = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Native diagnostics use requested backend without fallback."); NavigationServer.Synchronize(); agent.GetNextPathPosition(); index = agent.GetCurrentNavigationPathIndex();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frame == 2) { agent.DebugUseCustom = true; agent.DebugPathCustomColor = Colors.Blue; agent.DebugPathCustomLineWidth = 4; agent.DebugPathCustomPointSize = 6; }
                if (frame == 4) actor.Position = new(30, 70);
                if (frame == 6) actor.Visible = false;
                if (frame == 8) actor.Visible = true;
                if (frame == 10) agent.GetCurrentNavigationResult().Path = [new(20, 50), new(100, 50)];
                if (frame == 12) agent.DebugPathCustomPointSize = 0;
                if (frame == 14) window.CanvasTransform = new Transform(0, new Vector2(7, 5));
                if (frame == 16) { window.CanvasTransform = Transform.Identity; actor.Modulate = new Color(.2f, .2f, .2f, .1f); }
                if (frame >= 24 && frame < 88) agent.DebugPathCustomColor = (frame & 1) == 0 ? Colors.Blue : Colors.Green;
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (frame >= 24 && frame < 88) active += bytes; else if (frame >= 88) idle += bytes;
                if (frame is 0 or 2 or 4 or 6 or 8 or 10 or 12 or 14 or 16)
                {
                    using var image = window.GetTexture().GetImage()!;
                    var color = frame == 0 ? Colors.Red : frame == 6 ? Colors.Black : Colors.Blue;
                    if (frame == 0) { var a = image.GetPixel(60, 29); var b = image.GetPixel(60, 30); Check((a.R > .9f) != (b.R > .9f), "A negative-width path covers one framebuffer row."); Pixel(image, 100, 30, Colors.Red); }
                    else if (frame == 14) { Pixel(image, 67, 55, Colors.Blue); Pixel(image, 60, 50, Colors.Black); }
                    else Pixel(image, 60, frame < 10 ? 30 : 50, color);
                    if (frame == 2) Pixel(image, 102, 32, Colors.Blue);
                    if (frame == 4) Pixel(image, 70, 70, Colors.Black);
                    if (frame == 10) Pixel(image, 60, 30, Colors.Black);
                    if (frame == 12) Pixel(image, 102, 52, Colors.Black);
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } captures) { Directory.CreateDirectory(captures); image.SavePNG(System.IO.Path.Combine(captures, $"navigation-debug-{backend}-{frame}.png")); }
                }
                Check(agent.GetCurrentNavigationPathIndex() == index, "Rendered diagnostics do not advance the agent.");
                if (++frame == 152) window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && frame == 152 && active == 0 && idle == 0, $"64 active/64 idle full owner frames: {active}/{idle} bytes.");
        Console.WriteLine($"Native navigation diagnostics {backend}: actual defaults/custom/world/visibility/result pixels and 64 active/64 idle complete owner frames 0 B passed.");
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var c = image.GetPixel(x, y); Check(Math.Abs(c.R - expected.R) < .08f && Math.Abs(c.G - expected.G) < .08f && Math.Abs(c.B - expected.B) < .08f, $"Navigation pixel {x}/{y}: {c}, expected {expected}."); }
}
