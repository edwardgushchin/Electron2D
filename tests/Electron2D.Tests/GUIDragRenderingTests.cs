using Electron2D;
using SDL = SDL3.SDL;

internal static partial class RenderingRuntimeTests
{
    private sealed class NativeDropPanel : Panel
    {
        internal int Drops;
        protected override bool OnCanDropData(Vector2 atPosition, DragPayload payload) =>
            payload is DragPayload<int> { Value: 42 };
        protected override void OnDropData(Vector2 atPosition, DragPayload payload) => Drops++;
    }

    private static void VerifyGUIDragRendering(string backend)
    {
        using var red = new StyleBoxFlat { BGColor = Colors.Red, AntiAliasing = false };
        using var blue = new StyleBoxFlat { BGColor = Colors.Blue, AntiAliasing = false };
        var window = new Window { Size = new(160, 100) };
        var source = new Control { Name = "DragSource", Size = new(20, 20) };
        var target = new NativeDropPanel { Name = "DropTarget", Position = new(80, 10), Size = new(40, 40) };
        target.AddThemeStyleBoxOverride("panel", blue);
        var preview = new Panel { Name = "DragPreview", Size = new(12, 12) };
        preview.AddThemeStyleBoxOverride("panel", red);
        window.AddChild(source); window.AddChild(target);
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!;
            server.SetDefaultClearColor(Colors.Black);
            source.ForceDrag(new DragPayload<int>(42), preview);
            var windows = SDL.GetWindows(out var count);
            Check(count == 1 && windows is { Length: 1 }, "The drag preview test uses one native root window.");
            var nativeWindow = windows![0]; var windowID = SDL.GetWindowID(nativeWindow);
            var scale = SDL.GetCurrentVideoDriver() == "wayland" ? SDL.GetWindowPixelDensity(nativeWindow) : 1f;
            var motion = new SDL.Event
            {
                Motion = new SDL.MouseMotionEvent
                { Type = SDL.EventType.MouseMotion, WindowID = windowID, Which = 987, X = 90 / scale, Y = 20 / scale }
            };
            Check(SDL.PushEvent(ref motion), "Queue drag motion for the next native event pump.");
            server.FramePostDraw += () =>
            {
                using var image = server.Readback();
                var pixel = image.GetPixel(95, 25);
                if (++frames == 1)
                {
                    Check(pixel.R > .8f && pixel.B < .2f && window.IsGUIDragging() &&
                          DisplayServer.Instance!.CursorGetShape() == DisplayServer.CursorShape.CanDrop,
                        $"The {backend} drag preview overlays the accepting target and exposes the drop cursor; pixel={pixel}.");
                    var release = new SDL.Event
                    {
                        Button = new SDL.MouseButtonEvent
                        {
                            Type = SDL.EventType.MouseButtonUp,
                            WindowID = windowID,
                            Which = 987,
                            Button = 1,
                            Down = false,
                            X = 90 / scale,
                            Y = 20 / scale
                        }
                    };
                    Check(SDL.PushEvent(ref release), "Queue release for the next native event pump.");
                }
                else
                {
                    Check(pixel.B > .8f && pixel.R < .2f && !window.IsGUIDragging() &&
                          window.IsGUIDragSuccessful() && target.Drops == 1 && preview.IsDisposed,
                        $"The {backend} completed drop removes the preview and reveals the target; pixel={pixel}.");
                    window.Tree!.Quit();
                }
            };
        };
        Engine.Instance.Run(window);
        Released(window);
        Check(frames == 2, $"The {backend} GUI drag rendered both phases.");
        Console.WriteLine($"Typed GUI drag preview and accepted drop rendered on {backend}.");
    }
}
