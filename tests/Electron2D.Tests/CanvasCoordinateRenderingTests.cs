using Electron2D;
using SDL = SDL3.SDL;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasCoordinates(string backend, string? fixture = null)
    {
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window
        {
            Size = new(96, 80),
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
            CanvasTransform = new(new(2, 0), new(0, 2), new(10, 6)),
            GlobalCanvasTransform = new(0, new(5, 4))
        };
        var parent = new Entity { Position = new(2, 3) }; window.AddChild(parent);
        var red = new CanvasNode { Name = "Red", Position = new(1, 1), Material = material, DrawAction = n => n.DrawTextureRect(texture, new(0, 0, 2, 2), false, Colors.Red) }; parent.AddChild(red);
        var neutral = new Node(); parent.AddChild(neutral);
        neutral.AddChild(new CanvasNode { Position = new(10, 6), Material = material, DrawAction = n => n.DrawRect(new(0, 0, 2, 2), Colors.Green) });
        parent.AddChild(new CanvasNode { Name = "Blue", TopLevel = true, Position = new(20, 6), Material = material, DrawAction = n => n.DrawRect(new(0, 0, 2, 2), Colors.Blue) });
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                try
                {
                    switch (frames)
                    {
                        case 1:
                            Pixel(pixels, 22, 19, Colors.Red); Pixel(pixels, 36, 23, Colors.Green); Pixel(pixels, 56, 23, Colors.Blue); Pixel(pixels, 3, 4, Colors.Black);
                            window.CanvasTransform = new(new(0, 1), new(-1, 0), new(45, 10)); break;
                        case 2:
                            Pixel(pixels, 45, 18, Colors.Red); Pixel(pixels, 43, 25, Colors.Green); Pixel(pixels, 43, 35, Colors.Blue);
                            window.CanvasTransform = Transform.Identity; window.GlobalCanvasTransform = new(new(2, 0), new(0, 2), new(5, 4)); break;
                        case 3:
                            Pixel(pixels, 12, 13, Colors.Red); Pixel(pixels, 26, 17, Colors.Green); Pixel(pixels, 46, 17, Colors.Blue);
                            window.GlobalCanvasTransform = Transform.Identity; window.SnapTransformsToPixel = true; window.SnapVerticesToPixel = true;
                            parent.Position = new(2.2f, 3.2f); red.Position = new(1.4f, 1.4f); break;
                        default:
                            Pixel(pixels, 3, 4, Colors.Red); Pixel(pixels, 10, 6, Colors.Green); Pixel(pixels, 20, 6, Colors.Blue);
                            Check(red.Draws == 1, "Canvas transforms, rotation and snapping reuse retained texture commands."); window.Tree!.Quit(); break;
                    }
                }
                catch (Exception e) { throw new InvalidOperationException($"Canvas coordinates {backend}/{fixture}, frame {frames}.", e); }
            };
        };
        Engine.Instance.Run(window); Released(window); Check(frames == 4, "Four canvas coordinate stages.");
        Console.WriteLine($"Canvas transforms native pixels passed: {backend}/{fixture ?? "default"}.");
    }

    private static void VerifyViewportCoordinateInput(string backend)
    {
        var window = new Window { Size = new(96, 200), CanvasTransform = new(new(2, 0), new(0, 3), new(10, 20)), GlobalCanvasTransform = new(new(2, 0), new(0, 4), new(5, 6)) };
        var probe = new CoordinateInputNode { InputEnabled = true, Position = new(1, 2) }; window.AddChild(probe);
        InputEvent? received = null; var inputs = 0;
        probe.InputAction = e =>
        {
            if (e is not InputEventMouseMotion motion || !motion.Position.IsEqualApprox(new(16, 35))) return;
            inputs++; received = e;
            Check(motion.GlobalPosition.IsEqualApprox(motion.Position) && motion.Relative.IsEqualApprox(new(4, 3)) && motion.ScreenRelative.IsEqualApprox(new(8, 12)), "Host localization transforms local vectors and preserves screen motion.");
            using var local = (InputEventMouseMotion)probe.MakeInputLocal(motion);
            Check(local.Position.IsEqualApprox(new(2, 3)) && local.Relative.IsEqualApprox(new(2, 1)), "Native host event reaches the exact drawn item coordinates.");
            window.SetInputAsHandled();
        };
        window.Ready += _ =>
        {
            var native = SDL.GetWindows(out var count); Check(count == 1, "Single native window.");
            var scale = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland" ? SDL.GetWindowPixelDensity(native![0]) : 1;
            var motion = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = SDL.GetWindowID(native![0]), Which = 987, X = 37 / scale, Y = 146 / scale, XRel = 8 / scale, YRel = 12 / scale } };
            Check(SDL.PushEvent(ref motion), "Inject native pointer coordinates.");
            RenderingServer.Instance!.FramePostDraw += () =>
            {
                Check(inputs == 1 && received!.IsDisposed, "Host dispatch disposes its positional projection after callbacks.");
                var raw = window.GetClientMousePosition(); var expected = window.GetFinalTransform().AffineInverse() * raw;
                Check(window.GetMousePosition().IsEqualApprox(expected), "Native pointer query removes the final transform.");
                Check(probe.GetGlobalMousePosition().IsEqualApprox(window.CanvasTransform.AffineInverse() * expected), "Canvas pointer query removes the canvas transform.");
                Check(probe.GetLocalMousePosition().IsEqualApprox(probe.GetGlobalTransform().AffineInverse() * probe.GetGlobalMousePosition()), "Local pointer query removes the node transform.");
                if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland") Reject<NotSupportedException>(() => probe.GetScreenTransform());
                if (!DisplayServer.Instance!.HasFeature(DisplayServer.Feature.MouseWarp)) Reject<NotSupportedException>(() => window.WarpMouse(Vector2.Zero));
                window.GlobalCanvasTransform = new(Vector2.Zero, Vector2.Zero, Vector2.Zero);
                Check(window.GetMousePosition() == Vector2.Zero, "Singular final transform reports zero pointer position.");
                window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Console.WriteLine($"Viewport native input and pointer coordinates passed: {backend}.");
    }

    private sealed class CoordinateInputNode : Entity
    {
        internal Action<InputEvent>? InputAction;
        protected override void OnInput(InputEvent e) => InputAction?.Invoke(e);
    }
}
