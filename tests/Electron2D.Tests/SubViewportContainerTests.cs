using Electron2D;
using SDL = SDL3.SDL;

internal static class SubViewportContainerTests
{
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 256, 192); }
    private sealed class Filter : SubViewportContainer
    {
        internal bool Allow = true;
        internal int Calls;
        protected override bool PropagateInputEvent(InputEvent input) { Calls++; return Allow; }
    }
    private sealed class Probe : Control
    {
        internal int Presses, Keys, Inputs, Enters, Exits, Drops;
        internal Vector2 Last, DropPosition;
        internal bool Accept, Drag;
        internal Probe() { FocusMode = FocusMode.All; MouseFilter = MouseFilter.Stop; InputEnabled = true; }
        protected override void OnInput(InputEvent input) { Inputs++; }
        protected override void OnDraw() { DrawRect(new(Vector2.Zero, Size), HasFocus(ignoreHiddenFocus: true) ? Colors.Lime : Colors.Red); }
        protected override void OnGUIInput(InputEvent input)
        {
            if (input is InputEventMouseButton { Pressed: true } button) { Presses++; Last = button.Position; }
            if (input is InputEventKey) Keys++;
            if (Accept) AcceptEvent();
        }
        protected override DragPayload? OnGetDragData(Vector2 position) => Drag ? new DragPayload<int>(9) : null;
        protected override bool OnCanDropData(Vector2 position, DragPayload payload) => payload is DragPayload<int>;
        protected override void OnDropData(Vector2 position, DragPayload payload) { Drops++; DropPosition = position; }
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationMouseEnter) Enters++; if (what == NotificationMouseExit) Exits++; }
    }
    internal static void Run()
    {
        Authoring(); Input(); Drag(); NestedAndFailures();
        Console.WriteLine("SubViewportContainer sizing, packing, forwarding, viewport focus/hover and cross-viewport drag passed.");
    }
    private static void Authoring()
    {
        using var container = new SubViewportContainer(); Check(!container.Stretch && container.StretchShrink == 1 && !container.MouseTarget && container.FocusMode == FocusMode.Click && container.UnhandledInputEnabled, "Container defaults."); Check(container.GetConfigurationWarnings().Length == 1, "Missing viewport warning.");
        Reject<ArgumentOutOfRangeException>(() => container.StretchShrink = 0);
        var a = new SubViewport { Name = "a", Size = new(32, 16) }; var b = new SubViewport { Name = "b", Size = new(16, 48) }; container.AddChild(a); container.AddChild(b);
        Check(container.GetMinimumSize() == new Vector2(32, 48) && container.GetConfigurationWarnings().Length == 0, "Nonstretched max child size.");
        container.Stretch = true; container.Size = new(64, 48); container.StretchShrink = 2;
        Check(a.Size == new Vector2i(32, 24) && b.Size == a.Size && container.GetMinimumSize() == Vector2.Zero, "Stretch resolution."); Reject<InvalidOperationException>(() => a.Size = new(16, 16));
        container.StretchShrink = int.MaxValue; Check(a.Size == new Vector2i(2, 2), "Minimum native resolution."); container.StretchShrink = 2;
        container.MouseTarget = true; container.Owner = null; a.Owner = container; b.Owner = container;
        using var packed = new PackedScene(); packed.Pack(container); using var restored = (SubViewportContainer)packed.Instantiate(); Check(restored.Stretch && restored.MouseTarget && restored.StretchShrink == 2 && restored.GetNode<SubViewport>("a").Size == a.Size, "Typed container reconstruction.");
        container.Stretch = false; a.Size = new(20, 30); Check(container.GetMinimumSize() == new Vector2(32, 30), "Manual sizing resumes.");
    }
    private static void Input()
    {
        var root = new TestViewport(); var outer = new Probe { Name = "outer", Position = new(180, 0), Size = new(32, 32) }; var container = new Filter { Name = "container", Stretch = true, StretchShrink = 2, Position = new(20, 20), Size = new(128, 96) }; var view = new SubViewport { Name = "view" }; var probe = new Probe { Name = "probe", Position = new(4, 4), Size = new(24, 24) }; view.AddChild(probe); container.AddChild(view); root.AddChild(container); root.AddChild(outer);
        using var tree = new SceneTree(root); tree.FlushDeferred(); Check(view.Size == new Vector2i(64, 48) && view.RenderTargetUpdateMode == ViewportUpdateMode.Always && !view.HandleInputLocally, "Entry policies.");
        Click(root, new(36, 36), true); Check(probe.Presses == 1 && probe.Last.IsEqualApprox(new(4, 4)) && ReferenceEquals(view.GetGUIFocusOwner(), probe) && ReferenceEquals(root.GetGUIFocusOwner(), container), "Scaled child pointer and section focus."); Click(root, new(36, 36), false);
        using (var key = new InputEventKey { Keycode = Key.A, Pressed = true }) root.PushInput(key, true);
        Check(probe.Keys == 1, "Focused container forwards nonpositional input before root GUI.");
        outer.GrabFocus(); Check(ReferenceEquals(root.GetGUIFocusOwner(), outer) && ReferenceEquals(view.GetGUIFocusOwner(), probe), "Independent viewport focus persists.");
        Move(root, new(36, 36)); Check(ReferenceEquals(root.GUIGetHoveredControl(), container) && ReferenceEquals(view.GUIGetHoveredControl(), probe) && probe.Enters > 0, "Independent hover targets."); Move(root, new(230, 160)); Check(view.GUIGetHoveredControl() is null && probe.Exits > 0, "Container exit clears child hover.");
        var before = probe.Presses; container.Allow = false; Click(root, new(36, 36), true); Click(root, new(36, 36), false); Check(probe.Presses == before, "Filter rejects forwarding."); container.Allow = true;
        view.GUIDisableInput = true; Click(root, new(36, 36), true); Click(root, new(36, 36), false); Check(probe.Presses == before && view.GUIGetHoveredControl() is null, "Disabled viewport input."); view.GUIDisableInput = false;
        probe.Accept = true; var parentUnhandled = 0; root.UnhandledInputEnabled = true; var extra = new InputProbe(() => parentUnhandled++); root.AddChild(extra); Click(root, new(36, 36), true); Check(parentUnhandled == 0, "Child handling consumes parent input."); Click(root, new(36, 36), false);
        Task.Run(() => { Reject<InvalidOperationException>(() => container.StretchShrink = 3); Reject<InvalidOperationException>(() => view.GetGUIFocusOwner()); }).GetAwaiter().GetResult();
        view.ReleaseGUIFocus(); Check(view.GetGUIFocusOwner() is null && root.GetGUIFocusOwner() is not null, "Focus release is local.");
        using var eventForReentry = new InputEventKey(); probe.GUIInput += _ => Reject<InvalidOperationException>(() => root.PushInput(eventForReentry, true)); probe.GrabFocus(); using (var key = new InputEventKey { Pressed = true, Keycode = Key.A }) view.PushInput(key, true);
    }
    private sealed class InputProbe(Action received) : Node { internal InputProbe() : this(() => { }) { } protected override void OnUnhandledInput(InputEvent input) => received(); protected override void OnReady() => UnhandledInputEnabled = true; }
    private static void Drag()
    {
        var root = new TestViewport(); var source = new Probe { Name = "source", Size = new(16, 16) }; var container = new SubViewportContainer { Name = "container", Position = new(32, 16), Size = new(64, 64), Stretch = true, StretchShrink = 2 }; var view = new SubViewport { Name = "view" }; var target = new Probe { Name = "target", Size = new(32, 32) }; view.AddChild(target); container.AddChild(view); root.AddChild(source); root.AddChild(container); using var tree = new SceneTree(root); tree.FlushDeferred();
        source.ForceDrag(new DragPayload<int>(42)); Move(root, new(48, 32)); Click(root, new(48, 32), false); Check(target.Drops == 1 && target.DropPosition.IsEqualApprox(new(8, 8)) && root.IsGUIDragSuccessful() && view.IsGUIDragSuccessful(), "Root-to-child drop uses child-local coordinates.");
        container.MouseTarget = true; source.ForceDrag(new DragPayload<int>(43)); Move(root, new(48, 32)); Click(root, new(48, 32), false); Check(target.Drops == 1 && !root.IsGUIDragSuccessful(), "MouseTarget selects container for drag targeting.");
        container.MouseTarget = false; target.Drag = true;
        Click(root, new(48, 32), true); using (var motion = new InputEventMouseMotion { Position = new(70, 32), Relative = new(22, 0), ButtonMask = MouseButtonMask.Left }) root.PushInput(motion, true);
        Check(root.IsGUIDragging() && view.GetGUIDragData() is DragPayload<int> { Value: 9 }, "Automatic child drag has local threshold state."); Move(root, new(8, 8)); Click(root, new(8, 8), false); Check(source.Drops == 1 && source.DropPosition.IsEqualApprox(new(8, 8)), "Automatic child drag drops on a root control.");
        root.SetGUIDragDescription("section"); Check(view.GetGUIDragDescription() == "section", "Child reads section description.");
        target.ForceDrag(new DragPayload<int>(44)); Check(ReferenceEquals(root.GetGUIDragData(), view.GetGUIDragData()), "Embedded source shares section payload."); view.CancelGUIDrag(); Check(!root.IsGUIDragging(), "Child cancels section drag.");
    }
    private static void NestedAndFailures()
    {
        var root = new TestViewport(); var outer = new SubViewportContainer { Name = "outer", Position = new(8, 8), Size = new(128, 128), Stretch = true, StretchShrink = 2 }; var a = new SubViewport { Name = "a" }; var inner = new SubViewportContainer { Name = "inner", Position = new(4, 4), Size = new(32, 32), Stretch = true, StretchShrink = 2 }; var b = new SubViewport { Name = "b" }; var leaf = new Probe { Name = "leaf", Position = new(2, 2), Size = new(8, 8) }; b.AddChild(leaf); inner.AddChild(b); a.AddChild(inner); outer.AddChild(a); root.AddChild(outer); using var tree = new SceneTree(root); tree.FlushDeferred();
        Click(root, new(28, 28), true); Click(root, new(28, 28), false); Check(leaf.Presses == 1 && leaf.Last.IsEqualApprox(Vector2.One) && ReferenceEquals(b.GetGUIFocusOwner(), leaf) && ReferenceEquals(a.GetGUIFocusOwner(), inner), "Two nested shrink transforms and focus contexts.");
        var text = ""; leaf.TextInput += value => text = value; tree.DispatchCommittedText(root, "nested"); Check(text == "nested", "Committed text reaches deepest focused embedded control.");
        leaf.TooltipText = "inner tip"; Move(root, new(28, 28)); tree.ProcessFrame(1); Check(tree.ViewportTooltipPanel(b) is { } panel && ReferenceEquals(panel.GetViewport(), b), "Tooltip belongs to child viewport."); Move(root, new(240, 160)); tree.FlushDeferred(); Check(tree.ViewportTooltipPanel(b) is null, "Nested exit cleans tooltip.");
        var extra = new SubViewport { Name = "extra" }; var seen = 0; extra.AddChild(new FaultInput(() => seen++)); outer.AddChild(extra); a.AddChild(new FaultInput(() => throw new ApplicationException("forward fixture")));
        outer.GrabFocus(); using var key = new InputEventKey { Keycode = Key.A, Pressed = true }; Reject<AggregateException>(() => root.PushInput(key, true)); Check(seen == 1, "Failure in one child continues later viewport dispatch.");
        outer.RemoveChild(extra); extra.Dispose(); Check(root.GetGUIFocusOwner() is not null, "Child detachment preserves parent focus.");
        var disposableView = new SubViewport { Name = "disposable" }; var disposedControl = new Probe { Name = "control", Size = new(16, 16) }; disposableView.AddChild(disposedControl); outer.AddChild(disposableView); disposedControl.GrabFocus(); disposedControl.GUIInput += _ => disposableView.Dispose();
        disposableView.PushInput(key, true); Check(disposableView.IsDisposed && ReferenceEquals(root.GetGUIFocusOwner(), outer), "Viewport disposal during input restores parent context.");
        using var direct = new InputEventKey { Keycode = Key.B, Pressed = true }; var directView = new SubViewport { Name = "direct", HandleInputLocally = false }; var directProbe = new Probe { Name = "direct_probe", Size = new(16, 16), Accept = true }; directView.AddChild(directProbe); root.AddChild(directView); directProbe.GrabFocus(); directView.PushInput(direct, true); directView.PushInput(direct, true); Check(directProbe.Keys == 2, "Nonlocal direct input clears handled ownership between events.");
    }
    private sealed class FaultInput(Action received) : Node
    {
        protected override void OnReady() => InputEnabled = true;
        protected override void OnInput(InputEvent input) { if (input is InputEventKey) received(); }
    }
    private sealed class Paint : Entity
    {
        internal Color Fill = Colors.Red;
        protected override void OnDraw() => DrawRect(new(0, 0, 16, 12), Fill);
    }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_CONTAINER_RENDERER") ?? "gpu"; var settings = ProjectSettings.Service; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try { Pixels(backend, null); NativeInput(backend); Warm(backend); if (backend == "gpu") foreach (var artifact in new[] { "CanvasHLSL", "CanvasGLSL" }) { using var shader = ShaderFixture(artifact); using var material = new ShaderMaterial { Shader = shader }; Pixels(backend, material); } }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private static void NativeInput(string backend)
    {
        var window = new Window { Size = new(96, 64) }; var container = new SubViewportContainer { Name = "container", Stretch = true, StretchShrink = 2, Position = new(8, 8), Size = new(64, 48), TextureFilter = TextureFilter.Nearest }; var view = new SubViewport { Name = "view" }; var probe = new Probe { Name = "probe", Size = new(24, 20), MouseDefaultCursorShape = CursorShape.PointingHand }; view.AddChild(probe); container.AddChild(view); window.AddChild(container); var driver = new NativeDriver(container, view, probe); window.AddChild(driver);
        Check(Engine.Run(window) == 0 && driver.Frames == 2, "Native embedded input lifecycle."); Console.WriteLine($"Native embedded cursor, MouseTarget, click/shrink and keyboard focus pixels passed ({backend}).");
    }
    private sealed class NativeDriver(SubViewportContainer container, SubViewport view, Probe probe) : Node
    {
        internal int Frames;
        private uint _windowID;
        protected override void OnReady()
        {
            ProcessEnabled = true; var windows = SDL.GetWindows(out var count); Check(count == 1, "One native host."); _windowID = SDL.GetWindowID(windows![0]);
            var motion = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = _windowID, X = 16, Y = 16 } }; Check(SDL.PushEvent(ref motion), "Queue native motion.");
            var button = new SDL.Event { Button = new SDL.MouseButtonEvent { Type = SDL.EventType.MouseButtonDown, WindowID = _windowID, Button = SDL.ButtonLeft, Down = true, X = 16, Y = 16 } }; Check(SDL.PushEvent(ref button), "Queue native click."); button.Button.Type = SDL.EventType.MouseButtonUp; button.Button.Down = false; Check(SDL.PushEvent(ref button), "Queue native release.");
            RenderingServer.FramePostDraw += () =>
            {
                using var image = RenderingServer.Service!.Readback(); Pixel(image, 12, 12, Frames == 0 ? Colors.Red : Colors.Lime);
                if (++Frames == 2) Tree!.Quit();
            };
        }
        protected override void OnProcess(double delta)
        {
            Check(probe.Presses == 1 && probe.Last.IsEqualApprox(new(4, 4)) && ReferenceEquals(view.GetGUIFocusOwner(), probe), "Native click reaches child-local coordinates and focus.");
            Check(Electron2D.Input.GetCurrentCursorShape() == CursorShape.PointingHand, "Embedded cursor delegates to child."); container.MouseTarget = true; Check(Electron2D.Input.GetCurrentCursorShape() == CursorShape.Arrow, "MouseTarget selects container cursor."); container.MouseTarget = false;
            if (Frames == 0)
            {
                var key = new SDL.Event { Key = new SDL.KeyboardEvent { Type = SDL.EventType.KeyDown, WindowID = _windowID, Key = SDL.Keycode.Tab, Scancode = SDL.Scancode.Tab, Down = true } }; Check(SDL.PushEvent(ref key), "Queue native Tab.");
            }
            else Check(probe.Keys == 1 && probe.HasFocus(ignoreHiddenFocus: true), "Native keyboard reveals child focus.");
        }
    }
    private static Shader ShaderFixture(string artifact)
    {
        using var stream = typeof(SubViewportContainerTests).Assembly.GetManifestResourceStream("TestShaders." + artifact + ".spv")!; using var bytes = new MemoryStream(); stream.CopyTo(bytes); return Shader.CreateFromSPIRV(bytes.ToArray());
    }
    private static void Pixels(string backend, ShaderMaterial? material)
    {
        var window = new Window { Size = new(128, 96) }; var container = new SubViewportContainer { Name = "container", Position = new(8, 8), Size = new(64, 48), Stretch = true, StretchShrink = 2, TextureFilter = TextureFilter.Nearest, Material = material }; var view = new SubViewport { Name = "view" }; var paint = new Paint { Name = "paint" }; view.AddChild(paint); container.AddChild(view); window.AddChild(container); var phase = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
            {
                using var image = renderer.Readback();
                if (phase == 0) { Pixel(image, 12, 12, Colors.Red); Pixel(image, 44, 12, Colors.Black); Check(view.Size == new Vector2i(32, 24), "Native shrink dimensions."); container.StretchShrink = 1; }
                else if (phase == 1) { Pixel(image, 12, 12, Colors.Red); Pixel(image, 28, 12, Colors.Black); Check(view.Size == new Vector2i(64, 48), "Native resolution resize."); container.Modulate = new(1, 1, 1, .5f); }
                else if (phase == 2) { Pixel(image, 12, 12, new(.5f, 0, 0)); container.Visible = false; }
                else if (phase == 3) { Pixel(image, 12, 12, Colors.Black); Check(view.RenderTargetUpdateMode == ViewportUpdateMode.Disabled, "Hidden container disables updates."); container.Visible = true; container.Modulate = Colors.White; container.Position = new(64, 8); container.Rotation = Mathf.Pi / 2; }
                else if (phase == 4) { Pixel(image, 60, 12, Colors.Red); Pixel(image, 12, 12, Colors.Black); container.Rotation = 0; container.Position = new(8, 8); container.Stretch = false; }
                else if (phase == 5) { Pixel(image, 12, 12, Colors.Red); Pixel(image, 28, 12, Colors.Black); var overlay = new SubViewport { Name = "overlay", Size = new(64, 48), TransparentBG = true }; overlay.AddChild(new Paint { Fill = Colors.Blue }); container.AddChild(overlay); }
                else if (phase == 6) { Pixel(image, 12, 12, Colors.Blue); var overlay = container.GetNode<SubViewport>("overlay"); container.RemoveChild(overlay); overlay.Dispose(); }
                else if (phase == 7) { Pixel(image, 12, 12, Colors.Red); window.Tree!.Quit(); }
                phase++;
            };
        };
        Check(Engine.Run(window) == 0 && phase == 8 && window.IsDisposed, "Container native lifecycle."); Console.WriteLine($"Embedded viewport composition/shrink/resize/modulation/visibility/rotation/sibling lifetime passed ({backend}, shader={material is not null}).");
    }
    private static void Warm(string backend)
    {
        var window = new Window { Size = new(128, 96) }; var container = new SubViewportContainer { Name = "container", Size = new(64, 48), Stretch = true }; var view = new SubViewport { Name = "view" }; var paint = new Paint(); view.AddChild(paint); container.AddChild(view); window.AddChild(container); var frame = 0; long active = 0, idle = 0; long before = 0; var dimensions = Vector2i.Zero;
        window.Ready += _ =>
        {
            RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePostDraw += () =>
            {
                var size = RenderingServer.Service!.ViewportDimensions(window); var now = GC.GetAllocatedBytesForCurrentThread();
                if (dimensions != size) { dimensions = size; frame = 0; active = idle = 0; }
                if (frame is >= 32 and < 96) active += now - before; else if (frame >= 96) idle += now - before;
                if (frame < 96) { container.Position = new((frame & 1) * 4, 0); container.MouseTarget = (frame & 1) == 0; container.Modulate = new(1, 1, 1, (frame & 1) == 0 ? .5f : 1); }
                var mutations = GC.GetAllocatedBytesForCurrentThread() - now; if (frame is >= 32 and < 96) active += mutations;
                if (++frame == 160) window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && active == 0 && idle == 0, $"Container warm allocation {active}/{idle}."); Console.WriteLine($"64 active + 64 idle embedded render/mutation intervals: {active}/{idle} managed bytes ({backend}).");
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    {
        var actual = image.GetPixel(x, y); Check(Mathf.Abs(actual.R - expected.R) < .025f && Mathf.Abs(actual.G - expected.G) < .025f && Mathf.Abs(actual.B - expected.B) < .025f && Mathf.Abs(actual.A - expected.A) < .025f, $"Container pixel ({x},{y}) {actual}, expected {expected}.");
    }
    private static void Click(Viewport root, Vector2 point, bool pressed) { using var input = new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = pressed }; root.PushInput(input, true); }
    private static void Move(Viewport root, Vector2 point) { using var input = new InputEventMouseMotion { Position = point, GlobalPosition = point }; root.PushInput(input, true); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
