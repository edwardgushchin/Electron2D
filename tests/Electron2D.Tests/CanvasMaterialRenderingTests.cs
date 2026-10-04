using Electron2D;
using Blend = Electron2D.BlendMode;

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
                n.DrawRect(new Rect2(4, 4, 16, 16), new Color(.8f, .2f, .1f, .5f));
                n.DrawTextureRect(texture, new Rect2(28, 4, 16, 16), false);
            },
        };
        var frames = 0;
        node.ReadyAction = n =>
        {
            var server = RenderingServer.Service!;
            if (RenderingServer.GetCurrentRenderingDriverName() == "software")
            {
                modes = [Blend.Mix];
                expected = [expected[0]];
            }
            RenderingServer.SetDefaultClearColor(new Color(.2f, .4f, .6f, 1));
            RenderingServer.FramePostDraw += () =>
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
        Engine.Run(window);
        Released(window);
        Check(frames == modes.Length && node.Draws == 1, "Blend modes update retained geometry without re-recording.");
        if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") != "dummy")
            VerifyCanvasMaterialInheritanceFrame(backend);
        VerifyCanvasModulationFrame(backend);
        if (backend == "compatibility" && Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy")
        {
            using var unsupported = new CanvasItemMaterial { BlendMode = Blend.Add };
            var rejected = new Window { Size = new(32, 32) };
            rejected.AddChild(new CanvasNode { Material = unsupported, DrawAction = n => n.DrawRect(new Rect2(0, 0, 8, 8), Colors.Red) });
            Reject<NotSupportedException>(() => Engine.Run(rejected));
            Released(rejected);
        }
        Console.WriteLine($"Canvas material pixels passed: {backend}.");
    }

    private static void VerifyCanvasMaterialInheritanceFrame(string backend)
    {
        using var inherited = new CanvasItemMaterial { BlendMode = Blend.Add };
        using var own = new CanvasItemMaterial { BlendMode = Blend.Sub };
        var window = new Window { Size = new(32, 32) };
        var parent = new Entity { Material = inherited };
        var child = new CanvasNode
        {
            Name = "Inherited",
            Material = own,
            UseParentMaterial = true,
            DrawAction = n => n.DrawRect(new Rect2(4, 4, 16, 16), new Color(.8f, .2f, .1f, .5f)),
        };
        parent.AddChild(child);
        window.AddChild(parent);
        Color[] expected =
        [
            new Color(.6f, .5f, .65f, 1f),
            new Color(0f, .3f, .55f, .75f),
            new Color(.5f, .3f, .35f, 1f),
        ];
        var frames = 0;
        child.ReadyAction = n =>
        {
            var server = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(new Color(.2f, .4f, .6f, 1f));
            RenderingServer.FramePostDraw += () =>
            {
                using var frame = server.Readback();
                var actual = frame.GetPixel(12, 12);
                var target = expected[frames];
                Check(Math.Abs(actual.R - target.R) < .025f && Math.Abs(actual.G - target.G) < .025f &&
                    Math.Abs(actual.B - target.B) < .025f && Math.Abs(actual.A - target.A) < .025f,
                    $"{backend} inherited material frame {frames}: expected {target}, got {actual}.");
                frames++;
                if (frames == 1) n.UseParentMaterial = false;
                else if (frames == 2)
                {
                    Check(child.Draws == 1, "Changing material inheritance reuses retained geometry.");
                    n.UseParentMaterial = true;
                    n.TopLevel = true;
                }
                else n.Tree!.Quit();
            };
        };
        Engine.Run(window);
        Released(window);
        Check(frames == 3, "Direct-parent, local and TopLevel material paths render with the expected live policy.");
    }

    private static void VerifyCanvasModulationFrame(string backend)
    {
        var window = new Window { Size = new(64, 16) };
        var parent = new CanvasNode
        {
            Modulate = new Color(.5f, .75f, 1f, 1f),
            SelfModulate = new Color(1f, .5f, 1f, 1f),
            DrawAction = n => n.DrawRect(new Rect2(4, 4, 8, 8), Colors.White),
        };
        var child = new CanvasNode
        {
            Name = "TintChild",
            Position = new Vector2(20f, 0f),
            DrawAction = n => n.DrawRect(new Rect2(4, 4, 8, 8), Colors.White),
        };
        var neutral = new Node { Name = "Neutral" };
        var separate = new CanvasNode
        {
            Name = "Separate",
            Position = new Vector2(40f, 0f),
            DrawAction = n => n.DrawRect(new Rect2(4, 4, 8, 8), Colors.White),
        };
        parent.AddChild(child);
        parent.AddChild(neutral);
        neutral.AddChild(separate);
        window.AddChild(parent);
        var frames = 0;
        child.ReadyAction = n =>
        {
            var server = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var frame = server.Readback();
                var expectedOwn = frames == 0 ? new Color(.5f, .375f, 1f) : new Color(.25f, .25f, .75f);
                var expectedInherited = frames switch
                {
                    0 => new Color(.5f, .75f, 1f),
                    1 => new Color(.25f, .125f, .75f),
                    _ => new Color(1f, .25f, 1f),
                };
                Pixel(frame, 8, 8, expectedOwn);
                Pixel(frame, 28, 8, expectedInherited);
                Pixel(frame, 48, 8, Colors.White);
                frames++;
                if (frames == 1)
                {
                    parent.Modulate = new Color(.25f, .5f, .75f);
                    n.SelfModulate = new Color(1f, .25f, 1f);
                }
                else if (frames == 2)
                {
                    Check(parent.Draws == 1 && child.Draws == 1,
                        "Modulation changes reuse both retained command streams.");
                    n.TopLevel = true;
                }
                else n.Tree!.Quit();
            };
        };
        Engine.Run(window);
        Released(window);
        Check(frames == 3 && parent.Draws == 1 && separate.Draws == 1,
            "Live modulation and neutral/TopLevel boundaries retain unaffected recorded geometry.");
    }
}
