using Electron2D;
using SDL3;

internal static partial class ControlPointerTests
{
    private sealed class Painted : Control
    {
        internal Color Tint = Colors.Red;
        protected override void OnDraw() => DrawRect(new(Vector2.Zero, Size), Tint);
        protected override void OnGUIInput(InputEvent input)
        {
            if (input is InputEventMouseButton { Pressed: true, Device: InputEvent.DeviceIdInternal }) { Tint = Colors.Green; QueueRedraw(); }
        }
    }
    internal static void RunNative()
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            foreach (var backend in Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy" ? new[] { "compatibility" } : new[] { "gpu", "compatibility" })
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
                var window = new Window { Size = new(96, 80), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var first = new Control { Position = new(4, 4), Size = new(20, 20) };
                var second = new Painted { Position = new(40, 4), Size = new(20, 20) };
                var container = new SubViewportContainer { Position = new(8, 40), Size = new(48, 32), Stretch = true, StretchShrink = 2 };
                var nested = new SubViewport { CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest, Size = new(24, 16), RenderTargetUpdateMode = ViewportUpdateMode.Always }; var child = new Control { Position = new(2, 2), Size = new(8, 8) };
                nested.AddChild(child); container.AddChild(nested); window.AddChild(first); window.AddChild(second); window.AddChild(container);
                if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy") window.Position = new(100, 200);
                var driver = new NativeDriver(window, second, child); window.AddChild(driver);
                window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend without fallback."); RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black); };
                try { Check(Engine.Run(window) == 0 && driver.Complete, "Native capture host cleanup."); } finally { if (!window.IsDisposed) window.Dispose(); }
                Console.WriteLine("Control pointer " + backend + ": pixels, capture and native pointer capability/coordinate checks passed.");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); }
    }
    private sealed class NativeDriver(Window window, Painted second, Control child) : Node
    {
        private int _stage;
        internal bool Complete;
        protected override void OnReady()
        {
            ProcessEnabled = true;
            var windows = SDL.GetWindows(out var count); Check(count == 1, "One native window.");
            var input = new SDL.Event { Button = new SDL.MouseButtonEvent { Type = SDL.EventType.MouseButtonDown, WindowID = SDL.GetWindowID(windows![0]), Button = 1, Down = true, X = 8, Y = 8 } };
            Check(SDL.PushEvent(ref input), "Native pointer press is queued before processing.");
        }
        protected override void OnProcess(double delta)
        {
            if (_stage++ == 0)
            {
                second.GrabClickFocus(); return;
            }
            if (_stage == 2) return;
            RenderingServer.ForceDraw(false);
            using (var image = window.GetTexture().GetImage()) { var pixel = image!.GetPixel(44, 8); Check(pixel.G > .9 && pixel.R < .1, "Transferred press changes actual canvas pixels."); }

            var expected = child.GetViewport()!.GetScreenTransform() * child.GetGlobalTransformWithCanvas() * new Vector2(1, 1);
            if (DisplayServer.HasFeature(DisplayServer.Feature.MouseWarp))
            {
                child.WarpMouse(new(1, 1)); var native = window.GetClientMousePosition();
                Check(native.DistanceTo(new((int)expected.X, (int)expected.Y)) <= 1, "Nested control warp reaches client coordinates.");
            }
            else Throws<NotSupportedException>(() => child.WarpMouse(new(1, 1)));
            try { Check(second.GetScreenPosition() == second.GetScreenTransform().Origin, "Native desktop origin."); Check(child.GetScreenPosition() == (Vector2)window.Position + child.GetViewport()!.GetScreenTransform() * child.GetGlobalTransformWithCanvas().Origin, "Embedded screen coordinates include outer native desktop origin."); }
            catch (NotSupportedException) { Check(Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland", "Only the known Wayland desktop-position limitation is accepted."); }
            Complete = true; Tree!.Quit();
        }
    }
}
