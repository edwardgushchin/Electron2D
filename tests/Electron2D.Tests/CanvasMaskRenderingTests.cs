using Electron2D;
using SDL = SDL3.SDL;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasMasks(string backend, string? fixture = null)
    {
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(128, 80) };
        var parent = LayerBox("Parent", new(4, 4), Colors.Red, material); window.AddChild(parent);
        var child = LayerBox("Child", new(8, 0), Colors.Green, material); child.VisibilityLayer = 2; parent.AddChild(child);
        var top = LayerBox("Top", new(40, 4), Colors.Blue, material); top.VisibilityLayer = 2; top.TopLevel = true; parent.AddChild(top);
        var neutral = new Node(); parent.AddChild(neutral);
        var independent = LayerBox("Independent", new(52, 4), Colors.White, material); independent.VisibilityLayer = 2; neutral.AddChild(independent);
        var layer = new CanvasLayer(); parent.AddChild(layer);
        var layered = LayerBox("Layered", new(64, 4), Colors.Yellow, material); layered.VisibilityLayer = 2; layer.AddChild(layered);
        var high = LayerBox("High", new(80, 4), Colors.Blue, material); high.VisibilityLayer = 1u << 31; window.AddChild(high);
        var zero = LayerBox("Zero", new(92, 4), Colors.Red, material); zero.VisibilityLayer = 0; window.AddChild(zero);
        var sorted = new Entity { Position = new(4, 20), YSortEnabled = true }; window.AddChild(sorted);
        var middle = new Entity { Position = new(8, 0), YSortEnabled = true, VisibilityLayer = 2 }; sorted.AddChild(middle);
        var leaf = LayerBox("Leaf", new(8, 0), Colors.Blue, material); leaf.VisibilityLayer = 4; middle.AddChild(leaf);
        var sibling = LayerBox("Sibling", new(32, 0), Colors.Cyan, material); sibling.VisibilityLayer = 4; sorted.AddChild(sibling);
        var mutator = new CanvasNode { Position = new(92, 20), VisibilityLayer = 0, Material = material, DrawAction = n => { n.VisibilityLayer = 1u << 31; n.DrawRect(new(0, 0, 4, 4), Colors.Magenta); } }; window.AddChild(mutator);
        var probe = new MaskProbe { VisibilityLayer = 0, InputEnabled = true, ProcessEnabled = true }; window.AddChild(probe);
        var draws = new[] { parent, child, top, independent, layered, high, zero, leaf, sibling, mutator };
        var visibilityEvents = 0; foreach (var item in draws) { item.VisibilityChanged += _ => visibilityEvents++; item.Hidden += _ => visibilityEvents++; }
        var frames = 0;
        window.Ready += _ =>
        {
            visibilityEvents = 0;
            var native = SDL.GetWindows(out var count); Check(count == 1, "One mask window.");
            var density = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland" ? SDL.GetWindowPixelDensity(native![0]) : 1;
            var motion = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = SDL.GetWindowID(native![0]), Which = 987, X = 3 / density, Y = 75 / density } };
            Check(SDL.PushEvent(ref motion), "Inject input for a culled node.");
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                try
                {
                    // Expected pixels are explicit so the test does not repeat the traversal algorithm.
                    var parentShown = frames is 1 or 2 or 4 or 5 or 6 or 7;
                    var childShown = frames is 1 or 4 or 5 or 7;
                    var independentShown = frames is 1 or 3 or 4 or 5 or 9 or 10;
                    var highShown = frames is 1 or 9;
                    Pixel(pixels, 5, 5, parentShown ? Colors.Red : Colors.Black);
                    Pixel(pixels, 13, 5, childShown ? Colors.Green : Colors.Black);
                    Pixel(pixels, 41, 5, independentShown ? Colors.Blue : Colors.Black);
                    Pixel(pixels, 53, 5, independentShown ? Colors.White : Colors.Black);
                    Pixel(pixels, 65, 5, independentShown ? Colors.Yellow : Colors.Black);
                    Pixel(pixels, 81, 5, highShown ? Colors.Blue : Colors.Black);
                    Pixel(pixels, 93, 21, highShown ? Colors.Magenta : Colors.Black);
                    Pixel(pixels, 93, 5, Colors.Black);
                    Pixel(pixels, 21, 21, frames is 1 or 5 ? Colors.Blue : Colors.Black);
                    Pixel(pixels, 37, 21, frames is 1 or 5 or 6 or 7 or 8 ? Colors.Cyan : Colors.Black);
                    Check(visibilityEvents == 0 && draws.All(n => n.Visible && n.IsVisibleInTree && n.Draws == 1), "Masks retain commands and logical visibility, including zero-mask OnDraw mutation.");
                    switch (frames)
                    {
                        case 1: window.CanvasCullMask = 1; break;
                        case 2: window.CanvasCullMask = 2; break;
                        case 3: window.CanvasCullMask = 3; break;
                        case 4: window.CanvasCullMask = 7; break;
                        case 5: window.CanvasCullMask = 5; break;
                        case 6: child.VisibilityLayer = 4; break;
                        case 7: parent.VisibilityLayer = 0; break;
                        case 8: window.CanvasCullMask = 0x80000002; break;
                        case 9: window.SetCanvasCullMaskBit(31, false); break;
                        case 10: window.CanvasCullMask = 0; break;
                        default: Check(probe.Inputs == 1 && probe.Frames > 0, "A culled node still processes frames and native input."); window.Tree!.Quit(); break;
                    }
                }
                catch (Exception error) { throw new InvalidOperationException($"Canvas masks {backend}/{fixture}, frame {frames}.", error); }
            };
        };
        Engine.Instance.Run(window); Released(window); Check(frames == 11, "Eleven mask and retained submission stages.");
        Console.WriteLine($"Canvas masks native pixels, input and retention passed: {backend}/{fixture ?? "default"}.");
    }

    private static void VerifyCulledShader()
    {
        using var shader = LoadShader("CanvasHLSL"); using var material = new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(96, 80), CanvasCullMask = 0 };
        var item = LayerBox("Shader", Vector2.Zero, Colors.Red, material); window.AddChild(item);
        var frames = 0;
        window.Ready += _ => RenderingServer.Instance!.FramePostDraw += () =>
        {
            Check(item.Draws == 1, "Culled shader commands are recorded."); frames++; window.CanvasCullMask = uint.MaxValue;
        };
        Reject<NotSupportedException>(() => Engine.Instance.Run(window)); Released(window);
        Check(frames == 1, "Compatibility accepts a culled shader and rejects it only once submitted.");
    }

    private sealed class MaskProbe : Entity
    {
        internal int Inputs, Frames;
        protected override void OnProcess(double delta) => Frames++;
        protected override void OnInput(InputEvent input) { if (input is InputEventMouseMotion motion && motion.Position.IsEqualApprox(new(3, 75))) Inputs++; }
    }
}
