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
            body.AddChild(new CollisionShape { Shape = shape }); inner.AddChild(body); inner.AddChild(control);
            outer.AddChild(inner); nested.AddChild(outer); container.AddChild(nested); root.AddChild(container);
            using var click = new InputEventMouseButton { Position = new(42, 52), GlobalPosition = new(42, 52), ButtonIndex = MouseButton.Left, Pressed = true };
            if (native)
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
                var driver = new NativeDriver((Window)root, nested, outer, inner, body, control); root.AddChild(driver);
                Check(Engine.Run((Window)root) == 0 && driver.Complete, "Native coordinate fixture finishes and releases its window");
                Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Native coordinate services are released");
            }
            else
            {
                using var tree = new SceneTree(root);
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
                Reject<InvalidOperationException>(() => inner.GetMousePosition());
                Reject<InvalidOperationException>(() => body.GetGlobalMousePosition());
                Reject<InvalidOperationException>(() => body.GetLocalMousePosition());
                Reject<InvalidOperationException>(() => control.WarpMouse(Vector2.Zero));
            }
            Console.WriteLine($"Canvas viewport coordinates {label}/{threaded}/{native}: stretch, shrink, nested windows, localized physics input and {(native ? "pixels/native pointer capability" : "128 full frames 0/0 B")} passed.");
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

    private sealed class NativeDriver(Window root, SubViewport nested, Window outer, Window inner, Probe body, Control control) : Node
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
            var raw = root.GetFinalTransform() * root.GetMousePosition();
            Check(inner.GetMousePosition().IsEqualApprox(inner.GetScreenTransform().AffineInverse() * raw) &&
                body.GetLocalMousePosition().IsEqualApprox(body.GetGlobalTransformWithCanvas().AffineInverse() * inner.GetMousePosition()), "Nested pointer reads use the same native root and inverse chain");
            if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland") Reject<NotSupportedException>(() => body.GetScreenTransform());
            else Check(body.GetScreenTransform().Origin.IsEqualApprox((Vector2)root.Position + new Vector2(42, 52)), "Item screen coordinates add only the native root desktop origin");
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!;
                var pixel = image.GetPixel(42, 52); Check(pixel.R > .9f && pixel.G < .1f && pixel.B < .1f, "Nested physics target is red at the projected client coordinate");
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
