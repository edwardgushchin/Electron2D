using Electron2D;
using SDL3;
using static TextureArrayTests;

internal static class CursorRefreshTests
{
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 96, 64); }
    private sealed class Probe : Control
    {
        internal int Motions, InternalMotions, Inputs, Unhandled, Enters, Exits;
        internal Action? DuringMotion;
        internal InputEvent? LastEvent;
        internal Vector2 LastPosition;
        internal Probe() { InputEnabled = UnhandledInputEnabled = true; MouseFilter = MouseFilter.Pass; MouseEntered += () => Enters++; MouseExited += () => Exits++; }
        protected override void OnInput(InputEvent input) => Inputs++;
        protected override void OnUnhandledInput(InputEvent input) => Unhandled++;
        protected override void OnGUIInput(InputEvent input)
        {
            if (input is not InputEventMouseMotion motion) return;
            Motions++; if (motion.Device == InputEvent.DeviceIdInternal) InternalMotions++;
            LastPosition = motion.Position; LastEvent = input; DuringMotion?.Invoke();
        }
    }
    private sealed class Indicator : Control
    {
        internal Color Color = Colors.Red;
        protected override void OnDraw() => DrawRect(new(Vector2.Zero, Size), Color);
        internal void Green() { Color = Colors.Green; QueueRedraw(); }
    }
    internal static void Run()
    {
        using var root = new TestViewport(); root.UpdateMouseCursorState();
        var probe = new Probe { Size = new(32, 32) }; root.AddChild(probe); using var tree = new SceneTree(root);
        root.UpdateMouseCursorState(); Throws<InvalidOperationException>(() => Task.Run(root.UpdateMouseCursorState).GetAwaiter().GetResult());
        using var motion = new InputEventMouseMotion { Device = InputEvent.DeviceIdInternal, Position = new(4, 5), GlobalPosition = new(4, 5) };
        root.PushInput(motion, true); root.PushInput(motion, true);
        Check(probe.Motions == 2 && probe.Enters == 1 && probe.Inputs == 0 && probe.Unhandled == 0 && !motion.IsDisposed, "Internal motion reaches GUI only and does not repeat enter.");
        motion.Device = InputEvent.DeviceIdMouse; root.PushInput(motion, true); Check(probe.Inputs == 1 && probe.Unhandled == 1, "Ordinary input still reaches Node callbacks.");
        using var inactive = new Window(); inactive.UpdateMouseCursorState(); Check(!inactive.IsInsideTree, "Detached windows have no containing native host to refresh.");
        using var dead = new SubViewport(); dead.Dispose(); Throws<ObjectDisposedException>(dead.UpdateMouseCursorState);
        using var parsed = new InputEventMouseMotion { Position = new(7, 9), GlobalPosition = new(7, 9), ButtonMask = MouseButtonMask.Left, Velocity = new(2, 3), ScreenVelocity = new(4, 5) };
        Input.ParseInputEvent(parsed); using var refresh = Input.Service.CreateCursorRefreshEvent();
        Check(refresh.Position == new Vector2(7, 9) && refresh.GlobalPosition == refresh.Position && refresh.Device == InputEvent.DeviceIdInternal && Input.MouseButtonMask == MouseButtonMask.Left && Input.LastMouseVelocity == Vector2.Zero, "Default refresh snapshots parsed pointer and preserves button state.");
        Input.ReleasePressedEvents();
        Console.WriteLine("Cursor refresh managed input isolation, polling, no-window, lifetime and owner checks passed.");
    }
    internal static void RunNative()
    {
        using (var display = DisplayServer.Open("Cursor polling without a scene", new Vector2i(96, 64)))
        {
            using var motion = new InputEventMouseMotion { Position = new(3, 7), Velocity = new(8, 9), ScreenVelocity = new(10, 11), ButtonMask = MouseButtonMask.Left };
            Input.ParseInputEvent(motion); Input.SetDefaultCursorShape(CursorShape.IBeam);
            Check(Input.LastMouseVelocity == Vector2.Zero && Input.LastMouseScreenVelocity == Vector2.Zero && Input.MouseButtonMask == MouseButtonMask.Left, "Default refresh without a scene preserves buttons and clears velocity.");
            Input.ParseInputEvent(motion); Input.SetDefaultCursorShape(CursorShape.IBeam);
            Check(Input.LastMouseVelocity == new Vector2(8, 9), "Same default does not synthesize or clear velocity.");
            Input.SetDefaultCursorShape(); Input.ReleasePressedEvents();
        }
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            foreach (var backend in new[] { "gpu", "compatibility" })
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
                var window = new Window { Size = new(96, 64) }; var probe = new Probe { Name = "Pointer", Size = new(32, 32), MouseFilter = MouseFilter.Stop, MouseDefaultCursorShape = CursorShape.Help };
                var indicator = new Indicator { Position = new(8, 8), Size = new(16, 16), MouseFilter = MouseFilter.Ignore };
                var container = new SubViewportContainer { Size = new(32, 32), Stretch = true, Visible = false };
                var nested = new SubViewport { Size = new(32, 32) }; var child = new Probe { Size = new(32, 32), MouseDefaultCursorShape = CursorShape.PointingHand };
                nested.AddChild(child); container.AddChild(nested); var overlay = new Probe { Name = "Overlay", Size = new(32, 32), MouseDefaultCursorShape = CursorShape.Cross, Visible = false, MouseFilter = MouseFilter.Stop };
                window.AddChild(probe); window.AddChild(container); window.AddChild(overlay); window.AddChild(indicator); window.AddChild(new Driver(window, probe, container, nested, child, overlay, indicator));
                window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend without fallback."); RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black); };
                try { Check(Engine.Run(window) == 0 && window.IsDisposed, "Native cursor host cleanup."); } finally { if (!window.IsDisposed) window.Dispose(); }
                Console.WriteLine("Cursor refresh " + backend + ": coalescing, pixels, nested overlap, default synthesis and cleanup passed.");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class Driver(Window window, Probe probe, SubViewportContainer container, SubViewport nested, Probe child, Probe overlay, Indicator indicator) : Node
    {
        private int _stage, _before, _enters;
        protected override void OnReady()
        {
            ProcessEnabled = true;
            var windows = SDL.GetWindows(out var count); Check(count == 1, "One native window.");
            var point = window.GetClientMousePosition();
            var motion = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = SDL.GetWindowID(windows![0]), X = point.X, Y = point.Y } };
            Check(SDL.PushEvent(ref motion), "Native input caches the real client pointer for default refresh.");
        }
        protected override void OnProcess(double delta)
        {
            if (_stage++ == 0)
            {
                var point = window.GetMousePosition(); probe.Position = container.Position = overlay.Position = point - new Vector2(4, 4);
                probe.DuringMotion = () => { indicator.Green(); window.UpdateMouseCursorState(); };
                _before = probe.InternalMotions; for (var i = 0; i < 20; i++) window.UpdateMouseCursorState();
                RenderingServer.ForceDraw(false); Pixel(Colors.Red); return;
            }
            if (_stage == 2)
            {
                Check(probe.InternalMotions == _before + 1 && probe.LastEvent!.IsDisposed && Input.GetCurrentCursorShape() == CursorShape.Help, "Coalesced internal motion and caller-independent lifetime.");
                RenderingServer.ForceDraw(false); Pixel(Colors.Green); _enters = probe.Enters;
                window.UpdateMouseCursorState(); return;
            }
            if (_stage == 3)
            {
                Check(probe.Enters == _enters, "Repeated refresh does not repeat enter.");
                probe.MouseFilter = MouseFilter.Ignore; container.Visible = true; overlay.Visible = true;
                _before = child.InternalMotions; nested.UpdateMouseCursorState(); return;
            }
            if (_stage == 4)
            {
                Check(child.InternalMotions == _before && overlay.InternalMotions > 0 && Input.GetCurrentCursorShape() == CursorShape.Cross, "Nested refresh starts in containing Window and honors overlay.");
                overlay.Visible = false; _before = child.InternalMotions; nested.UpdateMouseCursorState(); return;
            }
            if (_stage == 5)
            {
                Check(child.InternalMotions == _before + 1 && Input.GetCurrentCursorShape() == CursorShape.PointingHand, "Uncovered nested viewport gets localized motion.");
                container.Visible = false; probe.MouseFilter = MouseFilter.Stop; probe.DuringMotion = null;
                _before = probe.InternalMotions; Input.SetDefaultCursorShape(CursorShape.IBeam); Input.SetDefaultCursorShape(CursorShape.IBeam); return;
            }
            Check(probe.InternalMotions == _before + 1 && Input.GetCurrentCursorShape() == CursorShape.Help, "Changed default synthesizes once and preserves Control override.");
            Throws<InvalidOperationException>(() => Task.Run(window.UpdateMouseCursorState).GetAwaiter().GetResult());
            Throws<InvalidOperationException>(() => Task.Run(() => Input.SetDefaultCursorShape(CursorShape.IBeam)).GetAwaiter().GetResult());
            var bytes = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1024; i++) Input.SetDefaultCursorShape(CursorShape.IBeam);
            Check(GC.GetAllocatedBytesForCurrentThread() - bytes == 0, "Unchanged default cursor polling allocates zero bytes.");
            Input.SetDefaultCursorShape(); Tree!.Quit();
        }
        private void Pixel(Color expected)
        {
            using var image = window.GetTexture().GetImage(); var value = image!.GetPixel(12, 12);
            Check(Math.Abs(value.R - expected.R) < .02 && Math.Abs(value.G - expected.G) < .02 && Math.Abs(value.B - expected.B) < .02, "Refresh callback reaches real pixels.");
        }
    }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
