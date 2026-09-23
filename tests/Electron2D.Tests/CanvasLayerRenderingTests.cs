using Electron2D;
using SDL = SDL3.SDL;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasLayer(string backend, string? fixture = null)
    {
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        VerifyLayerOrdering(backend, material); VerifyLayerCoordinates(backend, material);
        Console.WriteLine($"Canvas layers native pixels, input and ordering passed: {backend}/{fixture ?? "default"}.");
    }

    private static CanvasNode LayerBox(string name, Vector2 position, Color color, Material? material, int z = 0) =>
        new() { Name = name, Position = position, ZIndex = z, Material = material, DrawAction = n => n.DrawRect(new(0, 0, 4, 4), color) };

    private static void VerifyLayerOrdering(string backend, Material? material)
    {
        var window = new Window { Size = new(96, 80), CanvasTransform = new(0, new(10, 0)) };
        window.AddChild(LayerBox("Default", new(5, 5), Colors.Red, material, 4096));
        var back = new CanvasLayer { Name = "Back", Layer = -1, Offset = new(10, 0) }; window.AddChild(back);
        back.AddChild(LayerBox("Green", new(5, 5), Colors.Green, material, 4096));
        var front = new CanvasLayer { Name = "Front", Offset = new(10, 0) }; window.AddChild(front);
        front.AddChild(LayerBox("Blue", new(5, 5), Colors.Blue, material, -4096));
        var neutral = new Node(); front.AddChild(neutral); neutral.AddChild(LayerBox("Neutral", new(40, 5), Colors.Cyan, material));
        var nested = new CanvasLayer { Name = "Nested" }; front.AddChild(nested); nested.AddChild(LayerBox("Yellow", new(60, 5), Colors.Yellow, material));
        var hidden = new Entity { Visible = false }; window.AddChild(hidden);
        var independent = new CanvasLayer { Name = "Independent", Layer = 2 }; hidden.AddChild(independent);
        independent.AddChild(LayerBox("White", new(70, 5), Colors.White, material));
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                try
                {
                    Pixel(pixels, 51, 6, Colors.Cyan); Pixel(pixels, 61, 6, Colors.Yellow); Pixel(pixels, 71, 6, Colors.White);
                    switch (frames)
                    {
                        case 1: Pixel(pixels, 16, 6, Colors.Blue); front.Layer = int.MinValue; break;
                        case 2: Pixel(pixels, 16, 6, Colors.Red); back.Layer = int.MaxValue; break;
                        case 3: Pixel(pixels, 16, 6, Colors.Green); back.Layer = front.Layer = 0; break;
                        case 4: Pixel(pixels, 16, 6, Colors.Blue); window.MoveChild(back, -1); break;
                        case 5: Pixel(pixels, 16, 6, Colors.Green); front.Hide(); back.Hide(); break;
                        default: Pixel(pixels, 16, 6, Colors.Red); window.Tree!.Quit(); break;
                    }
                }
                catch (Exception error) { throw new InvalidOperationException($"Canvas layer ordering {backend}, frame {frames}.", error); }
            };
        };
        Engine.Instance.Run(window); Released(window); Check(frames == 6, "Six layer ordering and visibility stages.");
    }

    private static void VerifyLayerCoordinates(string backend, Material? material)
    {
        var window = new Window { Size = new(256, 128), CanvasTransform = new(0, new(20, 10)), GlobalCanvasTransform = new(0, new(4, 2)) };
        var camera = new Camera { Enabled = false, LimitEnabled = false }; window.AddChild(camera);
        var snappedDefault = LayerBox("SnappedDefault", new(10, 10), Colors.Cyan, material); snappedDefault.Visible = false; window.AddChild(snappedDefault);
        var layer = new CanvasLayer { Offset = new(10, 5) }; window.AddChild(layer);
        var red = LayerBox("Red", new(1, 1), Colors.Red, material); layer.AddChild(red);
        var input = new CoordinateInputNode { Name = "Input", Position = new(1, 1), InputEnabled = true }; layer.AddChild(input);
        var frames = 0; var inputs = 0;
        input.InputAction = e =>
        {
            if (e is not InputEventMouseMotion motion || !motion.Position.IsEqualApprox(new(12, 7))) return;
            Check(input.MakeCanvasPositionLocal(motion.Position).IsEqualApprox(Vector2.One), "Native input removes the fixed layer transform instead of camera coordinates."); inputs++; window.SetInputAsHandled();
        };
        window.Ready += _ =>
        {
            var center = window.GetVisibleRect().Size * 0.5f;
            camera.Position = center + new Vector2(-20, -10);
            var native = SDL.GetWindows(out var count); Check(count == 1, "One layer window.");
            var density = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland" ? SDL.GetWindowPixelDensity(native![0]) : 1;
            var motion = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = SDL.GetWindowID(native![0]), Which = 987, X = 16 / density, Y = 9 / density } };
            Check(SDL.PushEvent(ref motion), "Inject layer pointer.");
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                try
                {
                    switch (frames)
                    {
                        case 1:
                            Check(inputs == 1, "Layer native input delivered."); Pixel(pixels, 16, 9, Colors.Red); Pixel(pixels, 36, 19, Colors.Black);
                            camera.Enabled = true; layer.FollowViewportEnabled = true; break;
                        case 2:
                            Pixel(pixels, 36, 19, Colors.Red); camera.Position = center + new Vector2(-10, -20); camera.ForceUpdateTransform(); layer.FollowViewportScale = 2;
                            Check(red.GetGlobalTransformWithCanvas().Origin.IsEqualApprox(new(32, 32)), "Logical follow matrix retains its separate query contract.");
                            red.Position = new(100, 50); break;
                        case 3: Pixel(pixels, 122, 92, Colors.Red); layer.FollowViewportScale = .5f; break;
                        case 4: Pixel(pixels, 127, 71, Colors.Red); layer.FollowViewportScale = 0; break;
                        case 5: Pixel(pixels, 128, 64, Colors.Black); layer.FollowViewportScale = -1; break;
                        case 6: Pixel(pixels, 130, 49, Colors.Red); red.Position = new(1, 1); layer.FollowViewportEnabled = false; layer.Transform = new(Mathf.Pi / 2, new(2, 1), 0, new(70, 10)); break;
                        case 7:
                            Pixel(pixels, 71, 16, Colors.Red); camera.Enabled = false; window.GlobalCanvasTransform = Transform.Identity;
                            window.CanvasTransform = new(0, new(.5f, -.5f)); layer.Transform = new(0, new(.5f, -.5f));
                            layer.FollowViewportEnabled = true; layer.FollowViewportScale = 1; window.SnapTransformsToPixel = true; snappedDefault.Show(); break;
                        default:
                            Pixel(pixels, 1, 0, Colors.Red); Pixel(pixels, 5, 0, Colors.Black); Pixel(pixels, 10, 9, Colors.Cyan); Pixel(pixels, 14, 9, Colors.Black);
                            Check(red.Draws == 1, "Layer and camera transforms retain draw commands."); window.Tree!.Quit(); break;
                    }
                }
                catch (Exception error) { throw new InvalidOperationException($"Canvas layer coordinates {backend}, frame {frames}.", error); }
            };
        };
        Engine.Instance.Run(window); Released(window); Check(frames == 8, "Eight layer transform and snapping stages.");
    }
}
