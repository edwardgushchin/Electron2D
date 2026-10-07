using Electron2D;
using IOPath = System.IO.Path;

internal static class GraphTests
{
    private sealed class InputProbeGraph : GraphEdit
    {
        internal bool MeasureInput; internal long InputBytes; internal int InputSamples;
        protected override void OnGUIInput(InputEvent inputEvent) { var before = GC.GetAllocatedBytesForCurrentThread(); base.OnGUIInput(inputEvent); if (MeasureInput && inputEvent is InputEventMouseMotion) { InputBytes += GC.GetAllocatedBytesForCurrentThread() - before; InputSamples++; } }
    }
    private sealed class Root : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 800, 600); }
    private static GraphNode Node(string name, Vector2 offset)
    {
        var node = new GraphNode { Name = name, Title = name, PositionOffset = offset, Size = new(160, 120) };
        node.AddChild(new Label { Name = "ValueRow", Text = "Value", CustomMinimumSize = new(60, 24), MouseFilter = MouseFilter.Ignore });
        node.AddChild(new Label { Name = "ResultRow", Text = "Result", CustomMinimumSize = new(60, 24), MouseFilter = MouseFilter.Ignore });
        node.SetSlot(0, true, 1, Colors.Cyan, false, 0, Colors.White); node.SetSlot(1, false, 0, Colors.White, true, 2, Colors.Orange); return node;
    }
    internal static void Run()
    {
        InputMap.LoadFromProjectSettings(); Model(); Interaction(); ResizeAndRequests(); KeyboardPorts(); GraphMenus(); Storage(); Console.WriteLine("Graph model, ports, frames, routed interaction and storage checks passed.");
    }
    private static void Model()
    {
        using var graph = new GraphEdit { Size = new(700, 500), ShowMenu = false, MinimapEnabled = false };
        var a = Node("A", new(40, 80)); var b = Node("B", new(320, 80)); var frame = new GraphFrame { Name = "Frame", Title = "Group" }; graph.AddChild(a); graph.AddChild(b); graph.AddChild(frame);
        Check(a.GetInputPortCount() == 1 && a.GetOutputPortCount() == 1 && a.GetInputPortSlot(0) == 0 && a.GetOutputPortSlot(0) == 1, "compressed ports");
        a.SetSlotMetadataLeft(0, "borrowed"); Check(a.GetSlotMetadataLeft(0)!.TryGet<string>(out var meta) && meta == "borrowed" && !a.GetSlotMetadataLeft(0)!.TryGet<object>(out _), "exact metadata");
        graph.ConnectNode("A", 0, "B", 0, true); graph.ConnectNode("A", 0, "B", 0); Check(graph.Connections.Length == 1 && graph.GetConnectionCount("A", 0) == 1 && graph.GetConnectionListFromNode("B").Length == 1, "idempotent connections");
        var snapshot = graph.Connections; snapshot[0] = new("invalid", 2, "other", 3); Check(graph.IsNodeConnected("A", 0, "B", 0), "snapshot isolation");
        graph.AttachGraphElementToFrame("A", "Frame"); Check(a.Parent == graph && graph.GetElementFrame("A") == frame && graph.GetAttachedNodesOfFrame("Frame").SequenceEqual(new[] { "A" }), "logical attachment");
        var frameFrom = frame.PositionOffset; var nodeFrom = a.PositionOffset; frame.PositionOffset += new Vector2(20, 30); Check(a.PositionOffset == nodeFrom + new Vector2(20, 30) && frame.PositionOffset == frameFrom + new Vector2(20, 30), "programmatic frame moves attachments");
        var outer = new GraphFrame { Name = "Outer" }; graph.AddChild(outer); graph.AttachGraphElementToFrame("Frame", "Outer"); Throws<ArgumentException>(() => graph.AttachGraphElementToFrame("Outer", "Frame"));
        a.Name = "Renamed"; Check(graph.GetElementFrame("Renamed") == frame && graph.Connections[0].FromNode == "Renamed", "rename remaps model");
        graph.DetachGraphElementFromFrame("Renamed"); Check(graph.GetElementFrame("Renamed") == null, "detach");
        graph.SetSelected(a); a.Selectable = false; a.Selected = true; Check(!a.Selected, "selection eligibility");
        Throws<ArgumentOutOfRangeException>(() => a.GetOutputPortType(1)); Throws<ArgumentOutOfRangeException>(() => a.PositionOffset = new(float.NaN, 0)); Throws<ArgumentOutOfRangeException>(() => graph.Zoom = float.NaN); Throws<ArgumentOutOfRangeException>(() => graph.SnappingDistance = 0);
        Throws<InvalidOperationException>(() => a.GetTitlebarHBox().Dispose()); Throws<InvalidOperationException>(() => graph.GetMenuHBox().Dispose());
        graph.ClearConnections(); Check(graph.Connections.Length == 0, "clear");
    }
    private static void Interaction()
    {
        var root = new Root(); var graph = new GraphEdit { Size = new(700, 500), ShowMenu = false, MinimapEnabled = false }; var a = Node("A", new(80, 80)); var b = Node("B", new(360, 80)); graph.AddChild(a); graph.AddChild(b); root.AddChild(graph); using var scene = new SceneTree(root); scene.FlushDeferred();
        Check(a.GetOutputPortPosition(0).Y > a.GetInputPortPosition(0).Y, "row layout");
        var started = 0; var ended = 0; var requested = 0; var empty = 0;
        graph.ConnectionDragStarted += (_, _, _) => started++; graph.ConnectionDragEnded += () => ended++; graph.ConnectionToEmpty += (_, _, _) => empty++;
        graph.ConnectionRequest += (from, fp, to, tp) => { requested++; graph.ConnectNode(from, fp, to, tp); };
        graph.AddValidConnectionType(2, 1);
        var from = a.Position + a.GetOutputPortPosition(0); var to = b.Position + b.GetInputPortPosition(0);
        Click(root, from, true); Move(root, to); Click(root, to, false); Check(started == 1 && ended == 1 && requested == 1 && graph.IsNodeConnected("A", 0, "B", 0), "routed wiring requests");
        var line = graph.GetConnectionLine(from, to); Check(line[0].IsEqualApprox(from) && line[^1].IsEqualApprox(to) && graph.GetClosestConnectionAtPoint(line[32], 1) != null && graph.GetConnectionsIntersectingWithRect(new(line[32] - new Vector2(2, 2), new(4, 4))).Length == 1, "geometry queries");
        Click(root, from, true); Move(root, new(600, 300)); Click(root, new(600, 300), false); Check(empty == 1 && ended == 2, "empty release");
        a.SetSlotTypeRight(1, -1); Click(root, from, true); Click(root, from, false); Check(started == 2, "negative port types do not start interactive wiring"); a.SetSlotTypeRight(1, 2);
        var begin = 0; var end = 0; graph.BeginNodeMove += () => begin++; graph.EndNodeMove += () => end++;
        var press = a.Position + new Vector2(60, 10); Click(root, press, true); Move(root, press + new Vector2(43, 27)); Click(root, press + new Vector2(43, 27), false);
        Check(a.PositionOffset == new Vector2(120, 100) && begin == 1 && end == 1, "snapped drag");
        graph.GrabFocus(); using (var key = new InputEventKey { Keycode = Key.Right, Pressed = true }) root.PushInput(key, true); Check(a.PositionOffset.X == 140, "keyboard movement");
        var scrollEvents = 0; graph.ScrollOffsetChanged += _ => scrollEvents++; graph.ScrollOffset = new(20, 30); Check(scrollEvents == 0, "programmatic scroll silent");
        graph.PanningScheme = GraphEdit.PanningSchemeMode.ScrollPans; using (var wheel = new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Pressed = true, Position = new(650, 450) }) root.PushInput(wheel, true); Check(scrollEvents == 1 && graph.ScrollOffset.Y == 94, "wheel pan");
        graph.SetSelected(null); graph.ArrangeNodes(); Check(b.PositionOffset.X > a.PositionOffset.X, "directed arrangement");
    }
    private static void ResizeAndRequests()
    {
        var root = new Root(); var graph = new GraphEdit { Size = new(700, 500), ShowMenu = false, MinimapEnabled = false }; var element = new GraphElement { Name = "Element", PositionOffset = new(80, 80), Size = new(140, 100), Resizable = true }; graph.AddChild(element); root.AddChild(graph); using var scene = new SceneTree(root); scene.FlushDeferred(); var requests = 0; var ends = 0; element.ResizeRequest += size => { requests++; element.Size = size; }; element.ResizeEnd += _ => ends++;
        var point = element.Position + element.Size - new Vector2(4, 4); Check(element.GetCursorShape(element.Size - new Vector2(4, 4)) == CursorShape.FDiagSize, "resize cursor"); Click(root, point, true); Move(root, point + new Vector2(20, 30)); Click(root, point + new Vector2(20, 30), false); Check(element.Size == new Vector2(160, 130) && requests == 1 && ends == 1, "request driven resize");
        graph.SetSelected(element); graph.GrabFocus(); var names = ""; graph.DeleteNodesRequest += memory => { names = string.Join(",", memory.ToArray()); }; element.DeleteRequest += () => { graph.RemoveChild(element); element.Dispose(); }; KeyPress(root, Key.Delete); Check(names == "Element" && element.IsDisposed, "deletion names survive immediate child removal");
    }
    private static void KeyboardPorts()
    {
        var root = new Root(); var graph = new GraphEdit { Size = new(700, 500), ShowMenu = false }; var a = Node("A", new(60, 80)); var b = Node("B", new(340, 80)); a.FocusMode = b.FocusMode = FocusMode.All; a.SlotsFocusMode = b.SlotsFocusMode = FocusMode.All; graph.AddChild(a); graph.AddChild(b); graph.AddValidConnectionType(2, 1); root.AddChild(graph); using var scene = new SceneTree(root); scene.FlushDeferred(); var requests = 0; graph.ConnectionRequest += (_, _, _, _) => requests++;
        a.GrabFocus(); KeyPress(root, Key.Down); KeyPress(root, Key.Down); KeyPress(root, Key.Right); b.GrabFocus(); KeyPress(root, Key.Down); KeyPress(root, Key.Left); Check(requests == 1, "keyboard port wiring");
        var duplicated = 0; graph.DuplicateNodesRequest += () => duplicated++; graph.GrabFocus(); InputMap.ActionEraseEvents("ui_graph_duplicate"); using var binding = new InputEventKey { Keycode = Key.F9 }; InputMap.ActionAddEvent("ui_graph_duplicate", binding); KeyPress(root, Key.F9); Check(duplicated == 1, "remapped graph action"); InputMap.LoadFromProjectSettings();
    }
    private static void KeyPress(Viewport root, Key key) { using var input = new InputEventKey { Keycode = key, Pressed = true }; root.PushInput(input, true); }
    private static void GraphMenus()
    {
        var root = new SubViewport { Size = new(800, 600), GUIEmbedSubwindows = true }; var graph = new GraphEdit { Size = new(700, 500), ShowMenu = false, ZoomMax = 3, Zoom = 2 }; var node = Node("A", new(100, 100)); node.ScalingMenus = true; var menu = new PopupMenu { Name = "Menu" }; menu.AddItem("Scaled item", 9); node.AddChild(menu); graph.AddChild(node); root.AddChild(graph); using var scene = new SceneTree(root); scene.FlushDeferred(); menu.PopupCentered(); var scaled = menu.Size;
        Check(menu.CanvasTransform.Scale == new Vector2(2, 2), "graph menu canvas scale"); var pressed = 0; menu.IDPressed += id => { if (id == 9) pressed++; };
        var content = menu.GetNode<Control>("_menu_scroll"); var point = (Vector2)menu.Position + (content.Position + new Vector2(24, 12)) * 2; Click(root, point, true); Click(root, point, false); Check(pressed == 1, "scaled popup hit coordinates");
        node.ScalingMenus = false; menu.PopupCentered(); Check(menu.CanvasTransform.Scale == Vector2.One && menu.Size.X < scaled.X && menu.Size.Y < scaled.Y, "unscaled popup policy"); menu.Hide();
    }
    private static void Storage()
    {
        using var graph = new GraphEdit { Name = "Graph", TypeNames = new() { [2] = "number" }, Zoom = 1.2f, ShowGrid = false };
        var a = Node("A", new(100, 100)); a.Owner = null; graph.AddChild(a); a.Owner = graph; for (var i = 0; i < a.GetChildCount(); i++) a.GetChild(i).Owner = graph; a.SetSlotTypeRight(1, 5); a.SetSlotColorLeft(0, Colors.Red); graph.ConnectNode("A", 0, "A", 0, true);
        using var packed = new PackedScene(); packed.Pack(graph); using var copy = (GraphEdit)packed.Instantiate(); var n = copy.GetNode<GraphNode>("A"); Check(n.GetSlotTypeRight(1) == 5 && n.GetSlotColorLeft(0) == Colors.Red && copy.TypeNames[2] == "number" && copy.Connections.Length == 1 && !copy.ShowGrid, "scene configuration");
        var path = IOPath.Combine(IOPath.GetTempPath(), "e2d-graph-" + Guid.NewGuid().ToString("N") + ".e2dscene"); try { ResourceSaver.Save(packed, path); using var loaded = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore); using var fileCopy = (GraphEdit)loaded.Instantiate(); Check(fileCopy.GetNode<GraphNode>("A").GetSlotTypeRight(1) == 5 && fileCopy.Connections[0].KeepAlive, "file codecs"); FreshProcess(path); } finally { System.IO.File.Delete(path); }
    }
    private static void FreshProcess(string path)
    {
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; if (IOPath.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(GraphTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_GRAPH"); start.Environment.Remove("ELECTRON2D_TEST_GRAPH_HOST"); start.Environment["ELECTRON2D_TEST_GRAPH_CHILD"] = path;
        using var process = System.Diagnostics.Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync(); if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("fresh graph scene"); }
        Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh graph scene passed"), error.GetAwaiter().GetResult());
    }
    internal static void RunChild(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); var graph = (GraphEdit)packed.Instantiate(); var node = graph.GetNode<GraphNode>("A"); Check(node.GetOutputPortCount() == 1 && node.GetOutputPortType(0) == 5 && graph.TypeNames[2] == "number" && graph.Connections[0].KeepAlive, "fresh registered factories and typed codecs"); var root = new Root(); root.AddChild(graph); using var scene = new SceneTree(root); scene.FlushDeferred(); Check(node.GetOutputPortPosition(0).Y > node.GetInputPortPosition(0).Y, "fresh executable layout"); Console.WriteLine("Fresh graph scene passed");
    }
    internal static void RunHost()
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_GRAPH_RENDERER") ?? "gpu";
        try { ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); NativeCapture(); Warm(); Warm(preview: true); } finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private static void NativeCapture()
    {
        var window = new Window { Size = new(720, 480) }; var graph = new GraphEdit { Size = new(720, 480), ShowZoomLabel = true }; var a = Node("Input", new(80, 100)); var b = Node("Output", new(380, 150)); var frame = new GraphFrame { Name = "Group", Title = "Processing", TintColorEnabled = true, TintColor = new(.15f, .25f, .4f, .55f) };
        graph.AddChild(frame); graph.AddChild(a); graph.AddChild(b); graph.AttachGraphElementToFrame("Input", "Group"); graph.ConnectNode("Input", 0, "Output", 0, true); window.AddChild(graph); var phase = 0;
        window.Ready += _ => RenderingServer.FramePostDraw += () =>
        {
            if (phase == 1) { using var pixels = RenderingServer.Service!.Readback(); if (Environment.GetEnvironmentVariable("ELECTRON2D_GRAPH_CAPTURE") is { } path) pixels.SavePNG(path); var cyan = 0; var orange = 0; for (var y = 60; y < 340; y++) for (var x = 30; x < 650; x++) { var color = pixels.GetPixel(x, y); if (color.R < .25f && color.G > .65f && color.B > .65f) cyan++; if (color.R > .65f && color.G > .25f && color.G < .8f && color.B < .25f) orange++; } Check(cyan > 50 && orange > 50, "native colored ports and gradient connection"); }
            if (++phase == 4) window.Tree!.Quit();
        };
        Check(Engine.Run(window) == 0 && phase == 4, "native graph lifecycle");
    }
    private static void Warm(bool preview = false)
    {
        var window = new Window { Size = new(720, 480) }; var graph = new InputProbeGraph { Size = new(720, 480) }; var a = Node("A", new(80, 100)); var b = Node("B", new(380, 140)); graph.AddChild(a); graph.AddChild(b); graph.ConnectNode("A", 0, "B", 0, true); window.AddChild(graph);
        var frame = 0; long before = 0, total = 0, nativeLoopBytes = 0; uint windowID = 0; var started = 0; graph.ConnectionDragStarted += (_, _, _) => started++;
        window.Ready += _ =>
        {
            if (preview) windowID = SDL3.SDL.GetWindowID(SDL3.SDL.GetWindows(out var nativeWindowCount)![0]);
            RenderingServer.FramePreDraw += () => { if (preview && frame >= 64) nativeLoopBytes += GC.GetAllocatedBytesForCurrentThread() - before; before = GC.GetAllocatedBytesForCurrentThread(); };
            RenderingServer.FramePostDraw += () =>
            {
                var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 64) total += after - before;
                if (preview)
                {
                    if (frame == 1) { var point = a.Position + a.GetOutputPortPosition(0); var button = new SDL3.SDL.Event { Button = new SDL3.SDL.MouseButtonEvent { Type = SDL3.SDL.EventType.MouseButtonDown, WindowID = windowID, Button = 1, Down = true, Clicks = 1, X = point.X, Y = point.Y } }; Check(SDL3.SDL.PushEvent(ref button), "queue port click"); }
                    if (frame >= 2) { var motion = new SDL3.SDL.Event { Motion = new SDL3.SDL.MouseMotionEvent { Type = SDL3.SDL.EventType.MouseMotion, WindowID = windowID, State = SDL3.SDL.MouseButtonFlags.Left, X = 300 + frame % 2 * 20, Y = 200, XRel = frame % 2 == 0 ? -20 : 20 } }; Check(SDL3.SDL.PushEvent(ref motion), "queue preview motion"); }
                    if (frame == 63) graph.MeasureInput = true;
                    before = GC.GetAllocatedBytesForCurrentThread();
                }
                else { a.PositionOffset = new(80 + frame % 2 * 20, 100); graph.SetConnectionActivity("A", 0, "B", 0, frame % 2); a.Selected = frame % 2 == 0; if (frame >= 64) total += GC.GetAllocatedBytesForCurrentThread() - after; }
                if (++frame == 128) window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && total == 0 && (!preview || started == 1 && graph.InputBytes == 0 && graph.InputSamples >= 64), "64 prepared graph " + (preview ? "pointer-preview" : "movement/activity/selection") + "/render intervals allocated " + total); if (preview) Console.WriteLine("Prepared native graph handlers: " + graph.InputSamples + " samples, " + graph.InputBytes + " managed bytes; shared native input/frame phase: " + nativeLoopBytes + " managed bytes."); Console.WriteLine("64 prepared graph " + (preview ? "pointer-preview" : "movement/activity/selection") + "/render intervals: " + total + " managed bytes.");
    }
    private static void Click(Viewport root, Vector2 point, bool pressed) { using var e = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = pressed }; root.PushInput(e, true); }
    private static void Move(Viewport root, Vector2 point) { using var e = new InputEventMouseMotion { Position = point, ButtonMask = MouseButtonMask.Left }; root.PushInput(e, true); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private static void Check(bool value, string message) { if (!value) throw new Exception("Graph: " + message); }
}
