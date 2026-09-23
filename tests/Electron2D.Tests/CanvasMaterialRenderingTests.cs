using Electron2D;
using Blend = Electron2D.CanvasItemMaterial.BlendModeEnum;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasMaterialState()
    {
        using var material = new CanvasItemMaterial();
        Check(material.BlendMode == Blend.Mix, "Canvas blend defaults to Mix.");
        var changes = 0;
        material.Changed += _ => changes++;
        material.BlendMode = Blend.Sub;
        Check(changes == 1 && material.BlendMode == Blend.Sub, "Blend changes commit before notification.");
        using var duplicate = (CanvasItemMaterial)material.Duplicate();
        Check(duplicate.BlendMode == Blend.Sub && !ReferenceEquals(duplicate, material), "Duplication copies blend mode.");
        duplicate.BlendMode = Blend.Add;
        Check(material.BlendMode == Blend.Sub, "Duplicate blend state is independent.");
        using var copy = new CanvasItemMaterial();
        copy.CopyFromResource(material);
        Check(copy.BlendMode == Blend.Sub, "Resource copy preserves blend mode.");
        Reject<ArgumentOutOfRangeException>(() => material.BlendMode = (Blend)99);
        Check(material.BlendMode == Blend.Sub && changes == 1, "Invalid modes preserve state and do not notify.");
    }

    private static void VerifyCanvasMaterialFrame(string backend)
    {
        Blend[] modes = [Blend.Mix, Blend.Add, Blend.Sub, Blend.Mul, Blend.PremultAlpha];
        Color[] expected =
        [
            new Color(.5f, .3f, .35f, 1),
            new Color(.6f, .5f, .65f, 1),
            new Color(0, .3f, .55f, .75f),
            new Color(.16f, .08f, .06f, .5f),
            new Color(.9f, .4f, .4f, 1),
        ];
        using var material = new CanvasItemMaterial();
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        image.Fill(new Color(.8f, .2f, .1f, .5f));
        using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(64, 32), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var node = new CanvasNode
        {
            Material = material,
            DrawAction = n =>
            {
                n.DrawRect(new Rect(4, 4, 16, 16), new Color(.8f, .2f, .1f, .5f));
                n.DrawTextureRect(texture, new Rect(28, 4, 16, 16), false);
            },
        };
        var frames = 0;
        node.ReadyAction = n =>
        {
            var server = RenderingServer.Instance!;
            if (server.GetCurrentRenderingDriverName() == "software")
            {
                modes = [Blend.Mix];
                expected = [expected[0]];
            }
            server.SetDefaultClearColor(new Color(.2f, .4f, .6f, 1));
            server.FramePostDraw += () =>
            {
                using var frame = server.Readback();
                var target = expected[frames];
                foreach (var x in new[] { 12, 36 })
                {
                    var actual = frame.GetPixel(x, 12);
                    Check(Math.Abs(actual.R - target.R) < .025f && Math.Abs(actual.G - target.G) < .025f &&
                        Math.Abs(actual.B - target.B) < .025f && Math.Abs(actual.A - target.A) < .025f,
                        $"{backend} {modes[frames]} pixel {x}: expected {target}, got {actual}.");
                }
                frames++;
                if (frames == modes.Length) n.Tree!.Quit();
                else material.BlendMode = modes[frames];
            };
        };
        window.AddChild(node);
        Engine.Instance.Run(window);
        Released(window);
        Check(frames == modes.Length && node.Draws == 1, "Blend modes update retained geometry without re-recording.");
        if (backend == "compatibility" && Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy")
        {
            using var unsupported = new CanvasItemMaterial { BlendMode = Blend.Add };
            var rejected = new Window { Size = new(32, 32) };
            rejected.AddChild(new CanvasNode { Material = unsupported, DrawAction = n => n.DrawRect(new Rect(0, 0, 8, 8), Colors.Red) });
            Reject<NotSupportedException>(() => Engine.Instance.Run(rejected));
            Released(rejected);
        }
        Console.WriteLine($"Canvas material pixels passed: {backend}.");
    }
}
