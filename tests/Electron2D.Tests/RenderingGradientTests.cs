using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyGradientCanvas(string backend)
    {
        using var g = new Gradient();
        using var ramp = new GradientRampTexture { Gradient = g, Width = 3 };
        using var fill = new GradientTexture { Gradient = g, Width = 3, Height = 3 };
        var window = new Window { Size = new Vector2i(96, 80), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var stage = 0; var changes = 0; ramp.Changed += _ => changes++; fill.Changed += _ => changes++;
        var node = new CanvasNode { DrawAction = n => { n.DrawTextureRect(ramp, new Rect2(0, 0, 16, 16), false); n.DrawTextureRect(fill, new Rect2(24, 0, 16, 16), false); } };
        node.ReadyAction = n => RenderingServer.Instance!.FramePostDraw += () =>
        {
            stage++; using var image = RenderingServer.Instance.Readback();
            if (stage == 1)
            {
                Pixel(image, 1, 8, Colors.Black); Pixel(image, 8, 8, new Color(.5f, .5f, .5f)); Pixel(image, 14, 8, Colors.White);
                Pixel(image, 25, 8, Colors.Black); Pixel(image, 32, 8, new Color(.5f, .5f, .5f)); Pixel(image, 38, 8, Colors.White);
                Task.Run(() => g.Colors = [Colors.Red]).GetAwaiter().GetResult();
                Check(changes == 0, "Source changes do not forward texture notifications.");
            }
            else
            {
                Pixel(image, 8, 8, Colors.Red); Pixel(image, 32, 8, Colors.Red);
                if (stage == 2) { ramp.Gradient = null; fill.Gradient = null; g.Colors = [Colors.Blue]; }
                else n.Tree!.Quit();
            }
            Check(node.Draws == 1, "Lazy gradient updates keep retained geometry.");
        };
        window.AddChild(node); Engine.Instance.Run(window); Released(window); Check(stage == 3, "Three gradient canvas stages.");
        Console.WriteLine($"Gradient canvas pattern and live updates passed: {backend}.");

        using var hdr = new Gradient { Colors = [new Color(2, 0, 0)] };
        using var texture = new GradientTexture { Gradient = hdr, UseHDR = true, Width = 1, Height = 1 };
        window = new Window { Size = new Vector2i(96, 80), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        window.AddChild(new CanvasNode
        {
            DrawAction = n => n.DrawTextureRect(texture, new Rect2(0, 0, 16, 16), false, new Color(.25f, .25f, .25f)),
            ReadyAction = n => RenderingServer.Instance!.FramePostDraw += () => { using var image = RenderingServer.Instance.Readback(); Pixel(image, 8, 8, new Color(.5f, 0, 0)); n.Tree!.Quit(); },
        });
        try { Engine.Instance.Run(window); Console.WriteLine($"Gradient HDR canvas precision passed: {backend}."); }
        catch (NotSupportedException e) when (backend == "compatibility" && e.Message.Contains("HDR texture precision"))
        { Console.WriteLine("Gradient HDR explicitly rejected by this compatibility driver."); }
        Released(window);
        foreach (Texture empty in new Texture[] { new GradientRampTexture(), new GradientTexture() })
        {
            using (empty)
            {
                window = new Window { CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; window.AddChild(new CanvasNode { DrawAction = n => n.DrawTexture(empty, Vector2.Zero) });
                Reject<InvalidOperationException>(() => Engine.Instance.Run(window)); Released(window);
            }
        }
    }

    private static void VerifyGradientMaterial(string fixture)
    {
        using var g = new Gradient { Colors = [new Color(2, 2, 2)] }; using var other = new Gradient { Colors = [new Color(2, 0, 2)] };
        using var ramp = new GradientRampTexture { Gradient = g, UseHDR = true, Width = 2 };
        using var fill = new GradientTexture { Gradient = other, UseHDR = true, Width = 2, Height = 2 };
        using var shader = LoadShader(fixture); using var material = new ShaderMaterial { Shader = shader };
        using var black = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); black.Fill(new Color(0, 0, 0, 0));
        using var detail = ImageTexture.CreateFromImage(black);
        shader.SetDefaultTextureParameter("colorMap", ramp); material.SetShaderParameter("detailMap", detail);
        material.SetShaderParameter("tint", new Color(.25f, .25f, .25f));
        var window = new Window { Size = new Vector2i(96, 80), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var stage = 0;
        var node = new CanvasNode { Material = material, DrawAction = n => n.DrawRect(new Rect2(0, 0, 64, 64), Colors.White) };
        node.ReadyAction = n => RenderingServer.Instance!.FramePostDraw += () =>
        {
            stage++; using var frame = RenderingServer.Instance.Readback();
            var expected = stage switch { 1 or 2 => new Color(.5f, .5f, .5f), 3 => new Color(.5f, 0, .5f), _ => new Color(0, .5f, 0) };
            try { Pixel(frame, 16, 16, expected); Pixel(frame, 48, 48, expected); }
            catch (Exception e) { throw new InvalidOperationException($"Gradient material {fixture}, stage {stage}.", e); }
            Check(node.Draws == 1, "Material gradient updates reuse retained geometry.");
            switch (stage)
            {
                case 1: Task.Run(() => g.Colors = [new Color(-2, -2, -2)]).GetAwaiter().GetResult(); material.SetShaderParameter("tint", new Color(-.25f, -.25f, -.25f)); break;
                case 2: shader.SetDefaultTextureParameter("colorMap", fill); material.SetShaderParameter("tint", new Color(.25f, .25f, .25f)); break;
                case 3: Task.Run(() => other.Colors = [new Color(0, 2, 0)]).GetAwaiter().GetResult(); break;
                case 4: fill.Gradient = null; fill.Width = 4; break;
                case 5: other.Colors = [new Color(2, 0, 0)]; break;
                default: n.Tree!.Quit(); break;
            }
        };
        window.AddChild(node); Engine.Instance.Run(window); Released(window);
        Check(stage == 6 && !g.IsDisposed && !ramp.IsDisposed && !fill.IsDisposed, "Six stages and borrowed ownership.");
        Console.WriteLine($"Gradient HDR material and worker updates passed: {fixture}, {stage} stages.");
    }
}
