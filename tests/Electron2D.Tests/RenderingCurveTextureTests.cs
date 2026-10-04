using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCurveTextureFrame(string fixture)
    {
        using var curve = CurveTextureTests.Constant(2); using var positive = CurveTextureTests.Constant(2);
        using var single = new CurveTexture { Curve = curve };
        using var xyz = new CurveXYZTexture { CurveX = positive, CurveY = curve };
        using var atlas = new AtlasTexture { Atlas = xyz };
        using var shader = LoadShader(fixture); using var material = new ShaderMaterial { Shader = shader };
        using var black = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); black.Fill(new Color(0, 0, 0, 0));
        using var detail = ImageTexture.CreateFromImage(black);
        shader.SetDefaultTextureParameter("colorMap", single); material.SetShaderParameter("detailMap", detail);
        material.SetShaderParameter("tint", new Color(.25f, .25f, .25f, 1));
        var window = new Window { Size = new Vector2i(96, 80) }; var stage = 0;
        var node = new CanvasNode { Material = material, DrawAction = n => n.DrawRect(new Rect2(0, 0, 64, 64), Colors.White) };
        node.ReadyAction = n =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                stage++; using var frame = server.Readback();
                var expected = stage switch
                {
                    1 or 2 => new Color(.5f, .5f, .5f, 1),
                    3 or 4 => new Color(.5f, 0, 0, 1),
                    5 => new Color(.5f, .5f, 0, 1),
                    6 or 7 => new Color(.25f, .5f, .25f, 1),
                    _ => new Color(.25f, 0, .25f, 1),
                };
                try { Pixel(frame, 16, 16, expected); Pixel(frame, 48, 48, expected); }
                catch (Exception e) { throw new InvalidOperationException($"Curve texture {fixture}, stage {stage}.", e); }
                Check(node.Draws == 1, "Curve edits and channel/storage changes reuse retained material geometry.");
                switch (stage)
                {
                    case 1: Task.Run(() => curve.SetPointValue(0, -2)).GetAwaiter().GetResult(); material.SetShaderParameter("tint", new Color(-.25f, -.25f, -.25f, 1)); break;
                    case 2: single.TextureMode = CurveTexture.TextureModeEnum.Red; break;
                    case 3: single.Width = 64; break;
                    case 4: shader.SetDefaultTextureParameter("colorMap", atlas); material.SetShaderParameter("tint", new Color(.25f, -.25f, .25f, 1)); break;
                    case 5: xyz.CurveZ = positive; Task.Run(() => positive.SetPointValue(0, 1)).GetAwaiter().GetResult(); break;
                    case 6: xyz.Width = 4096; break;
                    case 7: xyz.CurveY = null; break;
                    default: n.Tree!.Quit(); break;
                }
            };
        };
        window.AddChild(node); Engine.Run(window); Released(window);
        Check(stage == 8 && !curve.IsDisposed && !single.IsDisposed && !xyz.IsDisposed, "Eight frames, borrowed resources survive renderer cleanup.");
        Console.WriteLine($"Curve texture float/HDR channels and worker updates passed: {fixture}, {stage} stages.");
    }

    private static void VerifyCurveTextureCanvas(string backend)
    {
        using var curve = CurveTextureTests.Constant(2); using var single = new CurveTexture { Curve = curve };
        using var xyz = new CurveXYZTexture { CurveY = curve };
        var window = new Window { Size = new Vector2i(96, 80), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        window.AddChild(new CanvasNode
        {
            DrawAction = n => { n.DrawTextureRect(single, new Rect2(0, 0, 16, 16), false, new Color(.25f, .25f, .25f, 1)); n.DrawTextureRect(xyz, new Rect2(24, 0, 16, 16), false, new Color(.25f, .25f, .25f, 1)); },
            ReadyAction = n => RenderingServer.FramePostDraw += () =>
            {
                using var image = RenderingServer.Service!.Readback(); Pixel(image, 8, 8, new Color(.5f, .5f, .5f, 1)); Pixel(image, 32, 8, new Color(0, .5f, 0, 1)); n.Tree!.Quit();
            },
        });
        try { Engine.Run(window); Console.WriteLine($"Curve texture canvas precision passed: {backend}."); }
        catch (NotSupportedException e) when (backend == "compatibility" && e.Message.Contains("HDR texture precision"))
        { Console.WriteLine("Curve texture floats explicitly rejected by this compatibility driver."); }
        Released(window);
        using var uninitialized = new CurveTexture();
        window = new Window(); window.AddChild(new CanvasNode { DrawAction = n => n.DrawTexture(uninitialized, Vector2.Zero) });
        Reject<InvalidOperationException>(() => Engine.Run(window)); Released(window);
    }
}
