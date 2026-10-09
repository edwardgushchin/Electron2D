using Electron2D;

internal static class PhysicsPickingTests
{
    internal static void Run()
    {
        DefaultsAndPacking(); ProjectDefaults();
        Verify(PhysicsServer.Backend.CPU);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_PICKING_CPU_ONLY") == "1")
        {
            Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "CPU picking needs no display or renderer"); return;
        }
        using var device = new GPUPhysicsBodyStore();
        Verify(PhysicsServer.Backend.GPU);
    }
    private static void Verify(PhysicsServer.Backend backend)
    {
        Bindings(backend); DeliveryAndHover(backend); OrderingAndConsumption(backend); CoordinatesAndWorlds(backend);
        DisposalFailures(backend); LifetimeAndErrors(backend); Capacity(backend); NestedAndLayerPolicy(backend); Measure(backend);
        Console.WriteLine($"Public physics picking passed on {backend}: input/hover order, logical shapes, policies, canvas/world routing, lifecycle and warmed allocation.");
    }
    private static void DefaultsAndPacking()
    {
        using var area = new Area(); using var body = new StaticBody();
        using var root = new SubViewport();
        Check(area.InputPickable && !body.InputPickable && !root.PhysicsObjectPicking && !root.PhysicsObjectPickingSort && !root.PhysicsObjectPickingFirstOnly, "Distinct inherited picking defaults");
        root.PhysicsObjectPicking = root.PhysicsObjectPickingSort = root.PhysicsObjectPickingFirstOnly = true;
        body.InputPickable = true; body.Name = "Body"; root.AddChild(body); body.Owner = root;
        area.InputPickable = false; area.Name = "Area"; root.AddChild(area); area.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root); using var copy = (SubViewport)packed.Instantiate();
        Check(copy.PhysicsObjectPicking && copy.PhysicsObjectPickingSort && copy.PhysicsObjectPickingFirstOnly &&
            ((StaticBody)copy.GetChild(0)).InputPickable && !((Area)copy.GetChild(1)).InputPickable, "Stored picking policies round-trip");
    }
    private static void ProjectDefaults()
    {
        var saved = ProjectSettings.Get(ProjectSettings.PhysicsObjectPicking);
        try
        {
            Check(ProjectSettings.PhysicsObjectPicking.DefaultValue, "Project picking defaults enabled");
            ProjectSettings.Set(ProjectSettings.PhysicsObjectPicking, false);
            using var first = new SubViewport(); using var firstTree = new SceneTree(first);
            Check(!first.PhysicsObjectPicking, "Root samples disabled project setting");
            ProjectSettings.Set(ProjectSettings.PhysicsObjectPicking, true);
            using var second = new SubViewport(); var child = new SubViewport { Name = "Child" }; second.AddChild(child);
            using var secondTree = new SceneTree(second);
            Check(second.PhysicsObjectPicking && !child.PhysicsObjectPicking && !first.PhysicsObjectPicking, "Only new roots sample the project default");
            using var authored = new SubViewport { PhysicsObjectPicking = false }; using var authoredTree = new SceneTree(authored);
            Check(!authored.PhysicsObjectPicking, "An explicit caller choice overrides project initialization");
        }
        finally { ProjectSettings.Set(ProjectSettings.PhysicsObjectPicking, saved); }
    }
    private static void Bindings(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend); var source = f.Body(new(40, 40));
        var first = new Probe { Name = "ProxyA" }; var second = new Probe { Name = "ProxyB" };
        f.View.AddChild(first); f.View.AddChild(second);
        PhysicsServer.BodyAttachObject(source.GetRID(), first);
        f.Mouse(new(40, 40)); f.Step(); Check(source.Inputs == 0 && first.Inputs == 1, "Pointer input follows the assigned collision object, not the physical convenience owner");
        using var plain = new Node(); PhysicsServer.BodyAttachObject(source.GetRID(), plain);
        f.Mouse(new(40, 40)); f.Step(); Check(first.Inputs == 1 && source.Inputs == 0 && first.Log.Contains("exit"), "Non-collider association is not a pointer receiver");
        PhysicsServer.BodyAttachObject(source.GetRID(), null); f.Mouse(new(40, 40)); f.Step(); Check(source.Inputs == 0, "Cleared object identity suppresses pointer delivery");
        PhysicsServer.BodyAttachObject(source.GetRID(), first);
        var editor = f.Body(new(40, 40)); editor.ZIndex = 100; f.View.PhysicsObjectPickingSort = true;
        editor.Receive = (_, _, _) => PhysicsServer.BodyAttachObject(source.GetRID(), second);
        f.Mouse(new(40, 40)); f.Step(); Check(first.Inputs == 2 && second.Inputs == 0, "Rebinding during dispatch cannot retarget already sampled hits");
        editor.Receive = null; f.Mouse(new(40, 40)); f.Step(); Check(second.Inputs == 1, "Later queries observe the new association");
        var duplicate = f.Body(new(40, 40)); PhysicsServer.BodyAttachObject(duplicate.GetRID(), second);
        second.Log.Clear(); f.Mouse(new(40, 40)); f.Step();
        Check(second.Inputs == 3 && !second.Log.Contains("enter") && !second.Log.Contains("shape-enter:0"), "Shared assigned identity deduplicates hover but keeps physical hit input");
        editor.Receive = (_, _, _) => second.Dispose(); f.Mouse(new(40, 40)); f.Step(); Check(second.Inputs == 3, "Disposed assigned target is never invoked");
    }
    private static void DeliveryAndHover(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend); var body = f.Body(new(40, 40));
        body.AddChild(new CollisionShape { Name = "Second", Shape = f.Shape, Position = new(40, 0) });
        body.PhysicsProcessEnabled = true;
        f.Tree.PhysicsFrameStarted += _ => body.Log.Add("frame");
        using (var input = new InputEventMouseMotion { Position = new(40, 40), GlobalPosition = new(40, 40), ShiftPressed = true })
        { f.View.PushInput(input, true); Check(body.Log.Count == 0, "Picking waits for the physics tick"); }
        f.Step();
        Check(body.Log.SequenceEqual(["frame", "enter", "enter-signal", "shape-enter:0", "shape-enter-signal:0", "input:0", "input-signal:0", "physics"]), "Virtual/signal and physics ordering: " + string.Join(',', body.Log));
        Check(body.Event is { IsDisposed: true } && body.Shift && body.Pointer == new Vector2(40, 40), "Queued copy owns its lifetime and preserves local modifiers/position");
        body.PhysicsProcessEnabled = false; body.Log.Clear(); f.Step();
        Check(body.Log.SequenceEqual(["frame"]), "Stationary hover does not repeat input");
        body.Log.Clear(); f.Mouse(new(80, 40)); f.Step();
        Check(body.Log.Contains("shape-enter:1") && body.Log.Contains("shape-exit:0") && !body.Log.Contains("enter") && !body.Log.Contains("exit"), "Moving within one object changes shape hover only");
        body.Log.Clear(); f.Mouse(new(150, 150)); f.Step();
        Check(body.Log.Contains("exit") && body.Log.Contains("shape-exit:1"), "Empty-space movement leaves object and shape");
        body.Log.Clear(); body.Position = new(150, 150); f.Step();
        Check(body.Log.Contains("enter") && body.Log.Contains("input:0") && body.Device == InputEvent.DeviceIdInternal, "Moving into a stationary pointer creates one passive input");
        body.Log.Clear(); body.InputPickable = false; f.Step(); Check(body.Log.Contains("exit"), "Disabling pickability exits hover");
        body.InputPickable = true; body.CollisionLayer = 0; body.Log.Clear(); f.Step(); Check(!body.Log.Contains("enter"), "Zero layer cannot be picked");
        body.CollisionLayer = 1u << 31; body.CollisionMask = 0; f.Step(); Check(body.Log.Contains("enter"), "Layer 32 works independently of collision mask");
        body.Log.Clear(); body.Visible = false; f.Step(); Check(body.Log.Contains("exit"), "Hidden object exits hover");
        body.Visible = true; f.Step(); body.Log.Clear(); f.Tree.Paused = true; f.Step(); Check(body.Log.Contains("exit"), "Paused body exits hover");
        f.Tree.Paused = false; f.Step();
        f.View.NotifyMouseEntered(); body.Log.Clear(); f.View.NotifyMouseExited(); Check(body.Log.Contains("exit"), "Viewport exit clears hover synchronously");
        body.Log.Clear(); f.Step(); Check(!body.Log.Contains("enter"), "No passive hover outside viewport");
    }
    private static void OrderingAndConsumption(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend); var a = f.Body(new(40, 40)); var b = f.Body(new(40, 40));
        a.ZIndex = 10; b.ZIndex = 20; f.View.PhysicsObjectPickingSort = true; f.View.PhysicsObjectPickingFirstOnly = true;
        f.Mouse(new(40, 40)); f.Step(); Check(b.Inputs == 1 && a.Inputs == 0, "First-only chooses highest Z");
        a.ZIndex = b.ZIndex; f.Mouse(new(40, 40)); f.Step(); Check(b.Inputs == 2 && a.Inputs == 0, "Later tree node breaks Z ties");
        f.View.MoveChild(a, 1); f.Mouse(new(40, 40)); f.Step(); Check(a.Inputs == 1, "Sibling reordering changes picking priority");
        f.View.PhysicsObjectPickingFirstOnly = false;
        a.Receive = (v, _, _) => v.SetInputAsHandled(); var before = b.Inputs;
        f.Mouse(new(40, 40)); f.Step(); Check(b.Inputs == before, "Handled physics input stops lower hits"); a.Receive = null;
        f.View.PhysicsObjectPicking = false; f.Mouse(new(40, 40)); var resumed = a.Inputs;
        f.View.PhysicsObjectPicking = true; f.Step(); Check(a.Inputs == resumed + 1, "Enabling picking under a stationary known pointer updates hover");
        var control = new Control { Name = "Overlay", Position = new(20, 20), Size = new(40, 40), MouseFilter = MouseFilter.Stop };
        f.View.AddChild(control); before = a.Inputs + b.Inputs;
        f.Mouse(new(40, 40)); f.Step(); Check(a.Inputs + b.Inputs == before && a.Log.Contains("exit"), "GUI Stop blocks picking and clears previous hover");
        control.Visible = false; f.Step(); Check(a.Inputs + b.Inputs == before + 2, "Removing GUI obstruction restores passive hover without mouse movement");
        control.Visible = true; f.Mouse(new(40, 40)); f.Step(); before = a.Inputs + b.Inputs;
        control.MouseFilter = MouseFilter.Ignore; f.Mouse(new(40, 40)); f.Step(); Check(a.Inputs + b.Inputs == before + 2, "GUI Ignore permits both objects");
        using var touch = new InputEventScreenTouch { Index = 2, Position = new(40, 40), Pressed = true };
        a.Log.Clear(); f.View.PushInput(touch, true); f.Step(); Check(a.Log.Contains("input:0") && !a.Log.Contains("enter"), "Touch delivers shape input without mouse transitions");
        f.View.PhysicsObjectPicking = false; Check(a.Log.Contains("exit"), "Disabling picking clears hover");
        before = a.Inputs; f.Mouse(new(40, 40)); f.Step(); Check(a.Inputs == before, "Disabled viewport does not pick");
        f.View.PhysicsObjectPicking = true;
        a.Recording = b.Recording = false; f.Mouse(new(40, 40)); f.Step();
        for (var i = 0; i < 96; i++) f.Step();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) f.Step();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
        Check(owner == 0 && total == 0, $"Warmed sorted passive picking allocates {owner}/{total} managed bytes");
    }
    private static void CoordinatesAndWorlds(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend); var a = f.Body(new(40, 40));
        f.View.CanvasTransform = new(0, new Vector2(2, 2), 0, new(100, 20));
        f.Mouse(new(180, 100)); f.Step(); Check(a.Inputs == 1 && a.Pointer == new Vector2(180, 100), "Canvas inverse selects body but event stays viewport-local");
        var layer = new CanvasLayer { Name = "Layer", Offset = new(280, 20), Scale = new(2, 2) }; f.View.AddChild(layer);
        var b = f.Body(new(20, 20), layer); f.Mouse(new(320, 60)); f.Step(); Check(b.Inputs == 1, "Independent transformed CanvasLayer");
        using var independent = new World(backend);
        var child = new SubViewport { Name = "Child", World = independent, Size = new(512, 300), PhysicsObjectPicking = true }; f.View.AddChild(child);
        var c = f.Body(new(20, 20), child);
        using (var input = new InputEventMouseMotion { Position = new(20, 20) }) child.PushInput(input, true);
        f.Step(); Check(c.Inputs == 1 && c.Receiver == child && b.Inputs == 1, "Independent viewport world and receiver");
        layer.CustomViewport = child;
        using (var input = new InputEventMouseMotion { Position = new(320, 60) }) child.PushInput(input, true);
        f.Step(); Check(b.Inputs == 1, "Custom canvas rendering does not migrate bodies into an unrelated physics world");
        child.World = f.World;
        using (var input = new InputEventMouseMotion { Position = new(320, 60) }) child.PushInput(input, true);
        f.Step(); Check(b.Inputs == 2 && b.Receiver == child, "Custom canvas viewport picks when worlds are shared");
        f.View.CanvasTransform = Transform.Identity;
        f.Mouse(new(20, 20)); f.Step(); Check(c.Receiver == f.View && c.Inputs == 2, "Shared-world collider receives the selecting viewport");
    }
    private static void DisposalFailures(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend); var body = f.Body(new(40, 40)); body.PhysicsProcessEnabled = true;
        body.Receive = (_, input, _) => input.Disposed += _ => throw new InvalidOperationException("dispose observer");
        f.Mouse(new(40, 40)); f.Mouse(new(40, 40)); Reject<AggregateException>(f.Step);
        Check(body.Inputs == 2 && body.Signals == 2 && body.Log.Contains("physics"), "Event disposal failure does not strand queued input or skip physics callbacks");
        body.Receive = null;
        using var source = new DisposingMouse { Position = new(40, 40) };
        f.View.PushInput(source, true); f.View.PushInput(source, true);
        Reject<AggregateException>(() => f.View.PhysicsObjectPicking = false);
        Check(source.Copies.Count == 2 && source.Copies.All(e => e.IsDisposed), "Queue clearing releases every copy despite disposal observers");
        f.View.PhysicsObjectPicking = true; f.Step();
    }
    private sealed class DisposingMouse : InputEventMouse
    {
        internal readonly List<InputEvent> Copies = [];
        public override string AsText() => "Disposal observer mouse";
        protected override InputEvent CreateEventInstance()
        {
            var copy = new DisposingMouse(); Copies.Add(copy);
            copy.Disposed += _ => throw new InvalidOperationException("queued dispose observer"); return copy;
        }
    }
    private static void LifetimeAndErrors(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend); var a = f.Body(new(40, 40)); var b = f.Body(new(40, 40));
        a.ZIndex = 10; f.View.PhysicsObjectPickingSort = true;
        a.Receive = (_, _, _) => throw new InvalidOperationException("observer");
        f.Mouse(new(40, 40)); Reject<AggregateException>(f.Step);
        Check(a.Signals == 1 && b.Inputs == 1, "Failure in virtual callback still delivers signal and other objects");
        a.Receive = (_, _, _) => b.RemoveChild(b.GetChild(0));
        f.Mouse(new(40, 40)); f.Step(); Check(b.Inputs == 1, "Removing a pending shape suppresses its stale hit");
        a.Receive = (_, _, _) => f.View.PhysicsObjectPicking = false;
        f.Mouse(new(40, 40)); f.Step(); Check(a.Log.Contains("exit"), "Reentrant disable finishes hover cleanup");
        f.View.PhysicsObjectPicking = true; a.Receive = (_, _, _) => a.Dispose();
        f.Mouse(new(40, 40)); f.Step(); f.Step(); Check(a.IsDisposed, "Disposal during delivery is safe");
        var c = f.Body(new(120, 40)); f.Mouse(new(120, 40)); f.View.PhysicsObjectPicking = false; f.Step(); Check(c.Inputs == 0, "Disable discards queued event copies");
        Reject<InvalidOperationException>(() => Task.Run(() => f.View.PhysicsObjectPicking = true).GetAwaiter().GetResult());
    }
    private static void Capacity(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend); var bodies = new Probe[70];
        for (var i = 0; i < 70; i++) { var ignored = f.Body(new(40, 40)); ignored.InputPickable = false; }
        for (var i = 0; i < bodies.Length; i++) bodies[i] = f.Body(new(40, 40));
        f.Mouse(new(40, 40)); f.Step(); Check(bodies.Sum(b => b.Inputs) == 64, "Cap applies to eligible logical shape hits");
        var front = f.Body(new(200, 40)); var back = f.Body(new(200, 40));
        f.View.RemoveChild(front); f.View.RemoveChild(back);
        f.View.AddChild(front, Node.InternalMode.Front); f.View.AddChild(back, Node.InternalMode.Back);
        f.View.PhysicsObjectPickingFirstOnly = f.View.PhysicsObjectPickingSort = true;
        f.Mouse(new(200, 40)); f.Step(); Check(back.Inputs == 1 && front.Inputs == 0, "Picking tree order includes internal children");
        Check(back.IsGreaterThan(front) && !front.IsGreaterThan(back) && back.IsGreaterThan(f.View) && !f.View.IsGreaterThan(back), "Allocation-free tree comparison handles ancestors/internal groups");
        var area = new Area { Name = "Sensor", Position = new(120, 40) }; area.AddChild(new CollisionShape { Shape = f.Shape }); f.View.AddChild(area);
        var inputs = 0; area.InputEvent += (_, _, _) => inputs++;
        f.Mouse(new(120, 40)); f.Step(); Check(inputs == 1, "Area default pickability works on the same path");
    }
    private static void NestedAndLayerPolicy(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend); var body = f.Body(new(40, 40));
        var layer = new CanvasLayer { Name = "Layer" }; f.View.AddChild(layer); var overlay = f.Body(new(40, 40), layer);
        f.View.PhysicsObjectPickingFirstOnly = f.View.PhysicsObjectPickingSort = true;
        f.Mouse(new(40, 40)); f.Step(); Check(body.Inputs == 1 && overlay.Inputs == 1, "Each canvas applies its own first-only selection");
        body.Receive = (v, _, _) => v.SetInputAsHandled(); f.Mouse(new(40, 40)); f.Step();
        Check(body.Inputs == 2 && overlay.Inputs == 1, "Handled input stops later canvases"); body.Receive = null;
        using (var custom = new CustomMouse { Position = new(40, 40) }) f.View.PushInput(custom, true);
        f.Step(); Check(body.Inputs == 3, "Custom positional events are copied even without a transform override");
        var container = new SubViewportContainer { Name = "Container", Position = new(160, 40), Size = new(100, 100), Stretch = true };
        var child = new SubViewport { Name = "Embedded", Size = new(100, 100), World = f.World, PhysicsObjectPicking = true };
        container.AddChild(child); f.View.AddChild(container); var inner = f.Body(new(20, 20), child);
        f.Mouse(new(180, 60)); f.Step(); Check(inner.Inputs == 1 && inner.Receiver == child, "Embedded container forwards localized unhandled input");
        var blocker = new InputBlocker { Name = "Blocker", InputEnabled = true }; child.AddChild(blocker);
        var before = inner.Inputs; f.Mouse(new(180, 60)); f.Step(); Check(inner.Inputs == before, "OnInput consumption prevents physics picking");
        blocker.InputEnabled = false; blocker.UnhandledInputEnabled = true;
        f.Mouse(new(180, 60)); f.Step(); Check(inner.Inputs == before, "Unhandled input consumption also prevents picking");
        blocker.UnhandledInputEnabled = false; f.Mouse(new(180, 60)); child.GUIDisableInput = true; f.Step();
        Check(inner.Inputs == before, "GUI disabling discards queued picking"); child.GUIDisableInput = false;
        using var drag = new InputEventScreenDrag { Index = 3, Position = new(20, 20) }; child.PushInput(drag, true); f.Step();
        Check(inner.Inputs == before + 1, "Screen drag participates without pointer capture");
        f.Mouse(new(180, 60)); container.RemoveChild(child); f.Step(); Check(inner.Inputs == before + 1, "Detached viewport drops pending input"); child.Dispose();
    }
    private static void Measure(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend); f.View.Size = new(1200, 1200); f.View.PhysicsObjectPickingSort = true;
        for (var i = 0; i < 1024; i++) f.Body(new(20 + i % 32 * 32, 20 + i / 32 * 32)).Recording = false;
        f.Mouse(new(20, 20)); for (var i = 0; i < 96; i++) f.Step();
        var gpu = PhysicsServer.Service.GetSceneSpace(f.World.Space).GPUStore;
        var up = gpu?.UploadBytes ?? 0; var down = gpu?.ReadbackBytes ?? 0; var wait = gpu?.WaitMS ?? 0;
        var times = new double[64]; var owner = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < times.Length; i++)
        {
            var start = System.Diagnostics.Stopwatch.GetTimestamp(); f.Step();
            times[i] = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
        Check(owner == 0 && total == 0, $"Warmed 1024-body picking frames allocate {owner}/{total} bytes");
        Array.Sort(times);
        Console.WriteLine($"Picking {backend}: 1024 static circles, one hovered shape, sorted passive point query + complete physics frame, 96 warmup/64 samples; p50/p95/p99={times[32]:F4}/{times[60]:F4}/{times[63]:F4} ms, {owner}/{total} owner/all-thread managed B, GPU up/down={((gpu?.UploadBytes ?? 0) - up) / 64}/{((gpu?.ReadbackBytes ?? 0) - down) / 64} B, wait={((gpu?.WaitMS ?? 0) - wait) / 64:F4} ms.");
    }
    private sealed class InputBlocker : Node
    {
        protected override void OnInput(InputEvent inputEvent) => GetViewport()!.SetInputAsHandled();
        protected override void OnUnhandledInput(InputEvent inputEvent) => GetViewport()!.SetInputAsHandled();
    }
    private sealed class CustomMouse : InputEventMouse
    {
        public override string AsText() => "Custom mouse";
        protected override InputEvent CreateEventInstance() => new CustomMouse();
    }
    private sealed class Fixture : IDisposable
    {
        internal readonly World World; internal readonly SubViewport View; internal readonly SceneTree Tree;
        internal readonly CircleShape Shape = new() { Radius = 12 };
        private int _serial;
        internal Fixture(PhysicsServer.Backend backend)
        {
            World = new(backend); View = new() { Name = "View", World = World, Size = new(512, 300), PhysicsObjectPicking = true }; Tree = new(View);
        }
        internal Probe Body(Vector2 point, Node? parent = null)
        {
            var body = new Probe { Name = "Body" + _serial++, Position = point, InputPickable = true };
            body.AddChild(new CollisionShape { Name = "Shape", Shape = Shape }); (parent ?? View).AddChild(body); return body;
        }
        internal void Mouse(Vector2 point) { using var input = new InputEventMouseMotion { Position = point, GlobalPosition = point }; View.PushInput(input, true); }
        internal void Step() => Tree.PhysicsFrame(1d / 60);
        public void Dispose() { Tree.Dispose(); View.Dispose(); World.Dispose(); Shape.Dispose(); }
    }
    private sealed class Probe : StaticBody
    {
        internal readonly List<string> Log = [];
        internal bool Recording = true, Shift;
        internal int Inputs, Signals, Device;
        internal InputEvent? Event;
        internal Vector2 Pointer;
        internal Viewport? Receiver;
        internal Action<Viewport, InputEvent, int>? Receive;
        internal Probe()
        {
            MouseEntered += () => Add("enter-signal"); MouseExited += () => Add("exit-signal");
            MouseShapeEntered += i => Add("shape-enter-signal:" + i); MouseShapeExited += i => Add("shape-exit-signal:" + i);
            InputEvent += (_, _, i) => { Signals++; Add("input-signal:" + i); };
        }
        private void Add(string item) { if (Recording) Log.Add(item); }
        protected override void OnInputEvent(Viewport viewport, InputEvent inputEvent, int shapeIndex)
        {
            Inputs++; Event = inputEvent; Device = inputEvent.Device; Receiver = viewport; Add("input:" + shapeIndex);
            if (inputEvent is InputEventMouse mouse) { Pointer = mouse.Position; Shift = mouse.ShiftPressed; }
            Receive?.Invoke(viewport, inputEvent, shapeIndex);
        }
        protected override void OnMouseEnter() => Add("enter");
        protected override void OnMouseExit() => Add("exit");
        protected override void OnMouseShapeEnter(int shapeIndex) => Add("shape-enter:" + shapeIndex);
        protected override void OnMouseShapeExit(int shapeIndex) => Add("shape-exit:" + shapeIndex);
        protected override void OnPhysicsProcess(double delta) => Add("physics");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
