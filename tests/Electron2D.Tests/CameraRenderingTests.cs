using Electron2D;
using SDL = SDL3.SDL;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCamera(string backend, string? fixture = null)
    {
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(100, 80) };
        var first = new Camera { Name = "First", Position = new(10, 20), LimitEnabled = false };
        var second = new Camera { Name = "Second", Position = new(20, 20), LimitEnabled = false, Enabled = false };
        var red = new CanvasNode { Position = new(10, 20), Material = material, DrawAction = n => n.DrawRect(new(-2, -2, 4, 4), Colors.Red) };
        var input = new CoordinateInputNode { Position = new(10, 20), InputEnabled = true };
        window.AddChild(first); window.AddChild(second); window.AddChild(red); window.AddChild(input);
        var frames = 0; var inputCount = 0;
        input.InputAction = e =>
        {
            if (e is not InputEventMouseMotion motion || !motion.Position.IsEqualApprox(new(50, 40))) return;
            Check(input.MakeCanvasPositionLocal(motion.Position).IsEqualApprox(Vector2.Zero), "Viewport input maps to the item centered by the camera.");
            inputCount++; window.SetInputAsHandled();
        };
        window.Ready += _ =>
        {
            Check(window.GetCamera() == first, "Camera registered before the ready callback.");
            var native = SDL.GetWindows(out var count); Check(count == 1, "One camera window.");
            var scale = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland" ? SDL.GetWindowPixelDensity(native![0]) : 1;
            var motion = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = SDL.GetWindowID(native![0]), Which = 987, X = 50 / scale, Y = 40 / scale } };
            Check(SDL.PushEvent(ref motion), "Inject a pointer at the camera center.");
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                try
                {
                    switch (frames)
                    {
                        case 1:
                            Check(inputCount == 1, "Native pointer reached camera-local coordinates."); Pixel(pixels, 50, 40, Colors.Red); Pixel(pixels, 10, 20, Colors.Black);
                            first.Zoom = new(2, 2); first.Position = new(12, 20); break;
                        case 2:
                            Pixel(pixels, 46, 40, Colors.Red); Pixel(pixels, 60, 40, Colors.Black);
                            first.IgnoreRotation = false; first.Rotation = Mathf.Pi / 2; break;
                        case 3:
                            Pixel(pixels, 50, 44, Colors.Red); second.Enabled = true; second.MakeCurrent(); break;
                        case 4:
                            Pixel(pixels, 40, 40, Colors.Red); second.Enabled = false; Check(window.GetCamera() == first, "Disabling hands off to the first enabled camera."); break;
                        case 5:
                            Pixel(pixels, 50, 44, Colors.Red); first.Enabled = false; Check(window.GetCamera() is null, "All cameras disabled."); break;
                        default:
                            Pixel(pixels, 10, 20, Colors.Red); Check(red.Draws == 1, "Camera changes retain draw commands."); window.Tree!.Quit(); break;
                    }
                }
                catch (Exception e) { throw new InvalidOperationException($"Camera pixels {backend}/{fixture}, frame {frames}.", e); }
            };
        };
        Engine.Instance.Run(window); Released(window); Check(frames == 6, "All six camera stages ran.");
        Console.WriteLine($"Camera native pixels, input and switching passed: {backend}/{fixture ?? "default"}.");
    }
}
