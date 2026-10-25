using Electron2D;

internal static class CanvasViewportCoordinateTests
{
    internal static void Run(Func<World> create, string label, bool threaded, bool native)
    {
        var savedThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var savedRendering = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var savedFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            using var world = create(); using var shape = new CircleShape { Radius = .5f };
            using Viewport root = native
                ? new Window { Size = new(160, 120), ContentScaleSize = new(80, 60), ContentScaleMode = ContentScaleMode.CanvasItems, World = world }
                : new SubViewport { Size = new(160, 120), Size2DOverride = new(80, 60), Size2DOverrideStretch = true, World = world };
            root.CanvasTransform = new(0, new(1, 2));
            var container = new SubViewportContainer { Position = new(4, 6), Size = new(64, 48), Stretch = true, StretchShrink = 2 };
            var nested = new SubViewport { World = world, GUIEmbedSubwindows = true, RenderTargetUpdateMode = ViewportUpdateMode.Always };
            var outer = new Window { World = world, Position = new(2, 3), Size = new(20, 16), ContentScaleSize = new(10, 8), ContentScaleMode = ContentScaleMode.CanvasItems, GUIEmbedSubwindows = true, Borderless = true };
            var inner = new Window { World = world, Position = new(1, 1), Size = new(8, 8), ContentScaleSize = new(4, 4), ContentScaleMode = ContentScaleMode.CanvasItems, PhysicsObjectPicking = true, Borderless = true };
            foreach (var viewport in new[] { root, nested, outer, inner }) viewport.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest;
            var body = new Probe { Position = new(1, 1), InputPickable = true };
            var control = new Control { Position = new(1, 1), Size = Vector2.One, MouseFilter = MouseFilter.Ignore };
            var rootControl = new Control { Position = new(2, 3), MouseFilter = MouseFilter.Ignore };
            var popup = new Popup { World = world, Size = new(2, 2), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
            var popupLayer = new CanvasLayer(); popupLayer.AddChild(new BluePopup { Size = new(2, 2), MouseFilter = MouseFilter.Ignore }); popup.AddChild(popupLayer); body.AddChild(popup);
            body.AddChild(new CollisionShape { Shape = shape }); inner.AddChild(body); inner.AddChild(control);
            outer.AddChild(inner); nested.AddChild(outer); container.AddChild(nested); root.AddChild(container); root.AddChild(rootControl);
            using var click = new InputEventMouseButton { Position = new(42, 52), GlobalPosition = new(42, 52), ButtonIndex = MouseButton.Left, Pressed = true };
            if (native)
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
                var isolated = new SubViewport { World = world }; var isolatedItem = new Control { Position = new(7, 9) }; isolated.AddChild(isolatedItem); root.AddChild(isolated);
                var driver = new NativeDriver((Window)root, nested, outer, inner, body, control, isolated, isolatedItem, rootControl, popup); root.AddChild(driver);
                Check(Engine.Run((Window)root) == 0 && driver.Complete, "Native coordinate fixture finishes and releases its window");
                Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native coordinate services are released");
            }
            else
            {
                using var tree = new SceneTree(root);
                PopupScreenQueries(root, world, rootControl, body, control, popup);
                OffscreenMouseQueries(root, world, tree, create);
                Verify(nested, outer, inner, body);
                root.PushInput(click); tree.PhysicsFrame(.01);
                Check(body.Clicks == 1 && body.LastInput is { IsDisposed: true }, "Container and both embedded windows deliver one owned physical click");
                for (var i = 0; i < 128; i++) { Verify(nested, outer, inner, body); tree.PhysicsFrame(.01); }
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
                for (var i = 0; i < 128; i++) { Verify(nested, outer, inner, body); tree.PhysicsFrame(.01); }
                var tick = PhysicsServer.SpaceGetTick(world.Space);
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) { Verify(nested, outer, inner, body); tree.PhysicsFrame(.01); }
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128, $"Nested coordinate queries and 128 full physics frames: {owner}/{all} B");
                Reject<InvalidOperationException>(() => Task.Run(() => inner.WarpMouse(Vector2.Zero)).GetAwaiter().GetResult());
                Reject<ArgumentException>(() => inner.WarpMouse(new(float.NaN, 0)));
                Reject<ArgumentException>(() => inner.WarpMouse(new(float.MaxValue, 0)));
                Reject<InvalidOperationException>(() => inner.WarpMouse(Vector2.Zero));
                Check(inner.GetMousePosition().IsFinite() && body.GetGlobalMousePosition().IsFinite() && body.GetLocalMousePosition().IsFinite(), "Headless root sections can poll emulated input without a display");
                Reject<InvalidOperationException>(() => control.WarpMouse(Vector2.Zero));
            }
            Console.WriteLine($"Canvas viewport coordinates {label}/{threaded}/{native}: stretch, shrink, nested windows and {(native ? "pixels/native pointer capability" : "isolated mouse/picking lifecycle and two 128 full-frame intervals 0/0 B")} passed.");
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, savedThread);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, savedRendering); ProjectSettings.Set(ProjectSettings.RenderingFallback, savedFallback);
        }
    }

    private static void Verify(SubViewport nested, Window outer, Window inner, Probe body)
    {
        Check((nested.GetScreenTransform() * new Vector2(8, 9)).IsEqualApprox(new(42, 52)) &&
            (outer.GetScreenTransform() * new Vector2(3, 3)).IsEqualApprox(new(42, 52)) &&
            (inner.GetScreenTransform() * new Vector2(1, 1)).IsEqualApprox(new(42, 52)), "Screen chain composes each presentation scale and placement exactly once");
        Check(body.GetCanvasTransform() == Transform.Identity && body.GetGlobalTransformWithCanvas().Origin == new Vector2(1, 1) &&
            (body.GetViewportTransform() * body.GetGlobalTransform()).Origin == new Vector2(2, 2) && body.GetViewportRect().Size == new Vector2(4, 4),
            "Item viewport queries stop at the owning viewport rather than including ancestor placements");
        Check(body.MakeCanvasPositionLocal(new(1, 1)) == Vector2.Zero, "Canvas position localization stays independent of the screen chain");
        Check(body.GetScreenTransform().Origin == new Vector2(3, 3), "Item screen coordinates stop at the outer embedding window rather than the native root");
    }

    private static void PopupScreenQueries(Viewport root, World world, Control rootControl, Probe body, Control control, Popup popup)
    {
        Check(rootControl.GetScreenPosition() == new Vector2(6, 10), "Standalone root item includes the root final transform");
        root.GUIEmbedSubwindows = true;
        Check(rootControl.GetScreenPosition() == new Vector2(3, 5), "An embedding root excludes its own final presentation transform"); root.GUIEmbedSubwindows = false;
        Check(control.GetScreenPosition() == new Vector2(3, 3), "Control screen position uses the same popup host as CanvasItem");
        popup.Popup(new((Vector2i)(body.GetScreenTransform().Origin + new Vector2(0, 2)), new(2, 2)));
        Check(popup.Position == new Vector2i(3, 5) && popup.Visible, "Public item screen coordinates place an embedded popup in the correct host"); popup.Hide();
        using var standalone = new SubViewport { World = world, GlobalCanvasTransform = new(new(2, 0), new(0, 3), new(11, 13)), CanvasTransform = new(0, new(3, 4)) };
        var item = new Control { Position = new(2, 3), Scale = new(2, 3) }; standalone.AddChild(item); root.AddChild(standalone);
        Check(item.GetScreenPosition() == new Vector2(21, 34), "A standalone offscreen screen query includes only its own final/canvas transforms");
        standalone.GUIEmbedSubwindows = true;
        Check(item.GetScreenPosition() == new Vector2(5, 7), "An offscreen embedding boundary stops before its own final transform"); standalone.GUIEmbedSubwindows = false;
        using var container = new SubViewportContainer { Position = new(5, 6), Size = new(64, 48), Stretch = true, StretchShrink = 2 };
        root.AddChild(container); standalone.Reparent(container, false);
        root.GUIEmbedSubwindows = true;
        Check(item.GetScreenPosition() == new Vector2(48, 76), "Container popup space includes its item transform, shrink and child final transform in order");
        Check((item.GetScreenTransform() * new Vector2(1, 1)).IsEqualApprox(new(56, 94)), "Popup projection preserves the complete nonuniform basis, not just its origin");
        root.GUIEmbedSubwindows = false;
        Check(item.GetScreenPosition() == new Vector2(96, 152), "Without a root embedding boundary the container inherits the root final transform");
        standalone.GUIEmbedSubwindows = true;
        Check(item.GetScreenPosition() == new Vector2(5, 7), "A nearer embedding boundary removes the entire ancestor popup chain");
        Reject<InvalidOperationException>(() => Task.Run(() => body.GetScreenTransform()).GetAwaiter().GetResult());
    }

    private sealed class BluePopup : Control
    {
        protected override void OnDraw() => DrawRect(new(Vector2.Zero, Size), Colors.Blue);
    }

    private static void OffscreenMouseQueries(Viewport root, World world, SceneTree tree, Func<World> create)
    {
        using var view = new SubViewport
        {
            World = world,
            Size = new(100, 80),
            CanvasTransform = new(new(2, 0), new(0, 3), new(10, 20)),
            GlobalCanvasTransform = new(new(2, 0), new(0, 4), new(100, 200))
        };
        var listener = new MouseQueries { InputEnabled = true, Position = new(4, 5) };
        var container = new SubViewportContainer { Position = new(2, 3), Size = new(64, 64), Stretch = true, StretchShrink = 2 };
        var child = new SubViewport { World = world };
        var embedded = new Window { World = world, Position = new(10, 10), Size = new(20, 20), ContentScaleSize = new(10, 10), ContentScaleMode = ContentScaleMode.CanvasItems, Borderless = true };
        view.GUIEmbedSubwindows = true; view.AddChild(embedded);
        container.AddChild(child); view.AddChild(container); view.AddChild(listener); root.AddChild(view);
        Check(view.GetMousePosition() == Vector2.Zero && child.GetMousePosition() == Vector2.Zero, "Unobserved isolated viewport mouse position starts at zero");
        var previousGlobal = root.GetScreenTransform() * root.GetMousePosition();
        using (var input = new InputEventMouseMotion { Position = new(50, 80) }) Input.ParseInputEvent(input);
        Check(root.GetMousePosition().IsEqualApprox(root.GetScreenTransform().AffineInverse() * new Vector2(50, 80)), "Headless root sections poll global emulated input rather than their local PushInput cache");
        var calls = 0;
        listener.Receive = input =>
        {
            if (input is not InputEventMouse) return;
            calls++;
            Check(view.GetMousePosition().IsEqualApprox(new(20, 41)) && listener.GetGlobalMousePosition().IsEqualApprox(new(5, 7)) && listener.GetLocalMousePosition().IsEqualApprox(new(1, 2)), "Fresh isolated mouse coordinates are visible before Node input callbacks");
            Check(child.GetMousePosition().IsEqualApprox(new(1.5f, 2)), "Container hover updates its viewport even when parent input consumes the event");
            view.SetInputAsHandled();
        };
        using (var input = new InputEventMouseMotion { Position = new(140, 364), GlobalPosition = new(140, 364) }) view.PushInput(input);
        Check(calls == 1, "A standalone viewport localizes one borrowed input"); listener.Receive = null;
        using (var input = new InputEventMouseButton { Position = new(91, 72) }) child.PushInput(input, true);
        Check(child.GetMousePosition().IsEqualApprox(new(1.5f, 2)), "Direct input into an attached viewport does not replace its section's projected pointer");
        using (var input = new InputEventScreenTouch { Position = new(90, 70), Pressed = true }) view.PushInput(input, true);
        using (var input = new InputEventKey { Keycode = Key.A, Pressed = true }) view.PushInput(input, true);
        Check(view.GetMousePosition().IsEqualApprox(new(20, 41)), "Touch and keyboard events preserve the last mouse position");
        view.GUIDisableInput = true;
        using (var input = new InputEventMouseMotion { Position = new(90, 70) }) view.PushInput(input, true);
        Check(view.GetMousePosition().IsEqualApprox(new(20, 41)), "Disabled input does not publish a mouse position"); view.GUIDisableInput = false;
        using (var input = new InputEventMouseButton { Position = new(12, 14) }) view.PushInput(input, true);
        Check(view.GetMousePosition() == new Vector2(12, 14) && embedded.GetMousePosition() == new Vector2(1, 2), "Embedded windows in an isolated section retain their own localized pointer");
        Reject<InvalidOperationException>(() => embedded.WarpMouse(Vector2.Zero));
        container.Visible = false;
        using var shape = new CircleShape { Radius = .5f };
        var body = new StaticBody { Position = new(5, 7), InputPickable = true }; body.AddChild(new CollisionShape { Shape = shape }); view.AddChild(body);
        view.PhysicsObjectPicking = true;
        var picks = 0; InputEvent? received = null;
        body.InputEvent += (viewport, input, index) =>
        {
            if (input is not InputEventMouseButton { Pressed: true }) return;
            Check(ReferenceEquals(viewport, view) && index == 0 && view.GetMousePosition() == new Vector2(20, 41) && body.GetLocalMousePosition().IsEqualApprox(Vector2.Zero), "Isolated mouse queries stay current in the later physical shape callback");
            received = input; picks++;
        };
        using (var input = new InputEventMouseButton { Position = new(20, 41), ButtonIndex = MouseButton.Left, Pressed = true }) view.PushInput(input, true);
        tree.PhysicsFrame(.01);
        Check(picks == 1 && received is { IsDisposed: true }, "Isolated picking queries the selected world once and releases its input copy");
        view.PhysicsObjectPicking = false;
        using var gui = new MouseGUIQueries { Position = new(4, 6), Size = new(2, 2) }; view.AddChild(gui);
        using (var input = new InputEventMouseButton { Position = new(20, 41), ButtonIndex = MouseButton.Left, Pressed = true }) view.PushInput(input, true);
        Check(gui.Calls == 1, "A GUI callback sees the same fresh viewport and item pointer"); gui.Visible = false;
        listener.Receive = _ => throw new InvalidOperationException("pointer observer");
        using (var input = new InputEventMouseMotion { Position = new(18, 38) }) Reject<AggregateException>(() => view.PushInput(input, true));
        Check(view.GetMousePosition() == new Vector2(18, 38), "Observer failure does not roll back the committed pointer"); listener.Receive = null;
        using (var input = new InputEventMouseButton { Position = new(12, 14) }) view.PushInput(input, true);
        using (var packed = new PackedScene())
        {
            packed.Pack(view); using var copy = (SubViewport)packed.Instantiate(); copy.World = world; root.AddChild(copy);
            Check(copy.GetMousePosition() == Vector2.Zero, "Scene reconstruction starts with fresh runtime pointer state");
        }
        view.CanvasTransform = Transform.Identity; view.GlobalCanvasTransform = new(Vector2.Zero, Vector2.Zero, Vector2.Zero);
        Check(view.GetMousePosition() == new Vector2(12, 14), "A disconnected section returns the retained viewport point without applying a later singular screen inverse");
        container.RemoveChild(child); view.AddChild(child);
        Check(child.GetMousePosition().IsEqualApprox(new(1.5f, 2)), "Changing section membership preserves the viewport's retained mouse point");
        view.RemoveChild(child); Reject<InvalidOperationException>(() => child.GetMousePosition()); view.AddChild(child);
        Check(child.GetMousePosition().IsEqualApprox(new(1.5f, 2)), "Mouse state survives detached/reattached viewport membership");
        view.RemoveChild(child);
        using (var secondWorld = create())
        using (var secondRoot = new SubViewport { World = secondWorld })
        {
            child.World = secondWorld;
            secondRoot.AddChild(child); using var secondTree = new SceneTree(secondRoot);
            Check(ReferenceEquals(child.World, secondWorld) && child.GetMousePosition().IsEqualApprox(new(1.5f, 2)), "A retained viewport point follows the object into another scene tree with its independently owned world"); secondRoot.RemoveChild(child);
            child.World = world;
        }
        view.AddChild(child);
        Reject<InvalidOperationException>(() => Task.Run(() => view.GetMousePosition()).GetAwaiter().GetResult());
        for (var i = 0; i < 128; i++) { _ = view.GetMousePosition(); _ = listener.GetLocalMousePosition(); tree.PhysicsFrame(.01); }
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) { _ = view.GetMousePosition(); _ = listener.GetLocalMousePosition(); tree.PhysicsFrame(.01); }
        var tick = PhysicsServer.SpaceGetTick(world.Space);
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) { _ = view.GetMousePosition(); _ = listener.GetLocalMousePosition(); tree.PhysicsFrame(.01); }
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128, $"128 complete isolated-pointer physics frames retain strict 0/0 B: {owner}/{all}");
        view.Dispose(); Reject<ObjectDisposedException>(() => view.GetMousePosition());
        using (var input = new InputEventMouseMotion { Position = previousGlobal }) Input.ParseInputEvent(input);
    }

    private sealed class MouseQueries : Entity
    {
        internal Action<InputEvent>? Receive;
        protected override void OnInput(InputEvent input) => Receive?.Invoke(input);
    }

    private sealed class MouseGUIQueries : Control
    {
        internal int Calls;
        protected override void OnGUIInput(InputEvent input)
        {
            if (input is not InputEventMouseButton { Pressed: true }) return;
            Check(GetViewport()!.GetMousePosition() == new Vector2(20, 41) && GetLocalMousePosition().IsEqualApprox(Vector2.One), "GUI localization uses the current cached mouse point"); Calls++;
        }
    }

    private sealed class Probe : StaticBody
    {
        internal int Clicks;
        internal InputEvent? LastInput;
        protected override void OnDraw() => DrawRect(new(-.5f, -.5f, 1, 1), Colors.Red);
        protected override void OnInputEvent(Viewport viewport, InputEvent input, int shape)
        {
            if (input is not InputEventMouseButton { Pressed: true } button) return;
            Check(ReferenceEquals(viewport, GetViewport()) && shape == 0 && button.Position.IsEqualApprox(new(1, 1)), "Physics picking receives owning viewport coordinates and exact logical shape");
            using var local = (InputEventMouseButton)MakeInputLocal(input);
            Check(local.Position.IsEqualApprox(Vector2.Zero), "Physics event converts to the drawn body origin");
            LastInput = input; Clicks++;
        }
    }

    private sealed class NativeDriver(Window root, SubViewport nested, Window outer, Window inner, Probe body, Control control, SubViewport isolated, Control isolatedItem, Control rootControl, Popup popup) : Node
    {
        internal bool Complete;
        private bool _pixels;
        private int _frames;
        protected override void OnReady()
        {
            ProcessEnabled = true; RenderingServer.SetDefaultClearColor(Colors.Black);
            Verify(nested, outer, inner, body);
            foreach (var viewport in new Viewport[] { nested, outer, inner })
            {
                var point = viewport.GetScreenTransform().AffineInverse() * new Vector2(42, 52);
                if (DisplayServer.HasFeature(DisplayServer.Feature.MouseWarp))
                {
                    viewport.WarpMouse(point);
                    Check((root.GetFinalTransform() * root.GetMousePosition()).DistanceTo(new(42, 52)) <= 1, "Nested viewport warp reaches the native root without applying transforms twice");
                }
                else Reject<NotSupportedException>(() => viewport.WarpMouse(point));
            }
            if (DisplayServer.HasFeature(DisplayServer.Feature.MouseWarp)) control.WarpMouse(Vector2.Zero);
            else Reject<NotSupportedException>(() => control.WarpMouse(Vector2.Zero));
            Check(isolated.GetMousePosition() == Vector2.Zero, "A disconnected native-host section retains its own pointer rather than polling the outer window");
            var raw = root.GetFinalTransform() * root.GetMousePosition();
            Check(inner.GetMousePosition().IsEqualApprox(inner.GetScreenTransform().AffineInverse() * raw) &&
                body.GetLocalMousePosition().IsEqualApprox(body.GetGlobalTransformWithCanvas().AffineInverse() * inner.GetMousePosition()), "Nested pointer reads use the same native root and inverse chain");
            Check(body.GetScreenTransform().Origin == new Vector2(3, 3) && control.GetScreenPosition() == new Vector2(3, 3), "Embedded item screen queries stop at the popup host without a desktop origin");
            Check(isolatedItem.GetScreenPosition() == new Vector2(7, 9), "Disconnected native-host viewports have their own popup space without desktop lookup");
            if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland") Reject<NotSupportedException>(() => rootControl.GetScreenTransform());
            else Check(rootControl.GetScreenPosition().IsEqualApprox((Vector2)root.Position + new Vector2(6, 10)), "A native root still includes actual desktop placement when required");
            root.GUIEmbedSubwindows = true; Check(rootControl.GetScreenPosition() == new Vector2(3, 5), "A native embedding root uses logical popup coordinates on every backend"); root.GUIEmbedSubwindows = false;
            popup.Popup(new((Vector2i)(body.GetScreenTransform().Origin + new Vector2(0, 2)), new(2, 2)));
            Check(popup.Position == new Vector2i(3, 5), "The native fixture presents a popup using the public item screen projection");
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!;
                if (Environment.GetEnvironmentVariable("ELECTRON2D_COORDINATE_CAPTURE") is { } path) image.SavePNG(path);
                var pixel = image.GetPixel(42, 52); Check(pixel.R > .9f && pixel.G < .1f && pixel.B < .1f, $"Nested physics target is red at the projected client coordinate: {pixel}; popup={popup.Position}/{popup.Size}");
                pixel = image.GetPixel(46, 72); Check(pixel.B > .9f && pixel.R < .1f && pixel.G < .1f, "A popup positioned with item screen coordinates is blue at the independent native-client pixel");
                _pixels = true;
            };
        }
        protected override void OnProcess(double delta)
        {
            Check(++_frames < 600, "Native coordinate fixture exceeds its frame budget");
            if (!_pixels || PhysicsServer.SpaceGetTick(root.World!.Space) == 0) return;
            Complete = true; Tree!.Quit();
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
