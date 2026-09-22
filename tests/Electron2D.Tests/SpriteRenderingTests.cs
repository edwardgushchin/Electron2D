using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifySprite(string backend, string? fixture = null)
    {
        using var image = Image.CreateFromData(2, 2, false, Image.Format.Rgba8,
            new byte[] { 255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 0, 255 });
        using var texture = ImageTexture.CreateFromImage(image);
        using var replacementImage = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        replacementImage.Fill(Colors.Magenta);
        using var replacement = ImageTexture.CreateFromImage(replacementImage);
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var sprite = new Sprite { Texture = texture, Centered = false, Position = new(16, 16), Scale = new(8, 8), Material = material };
        var window = new Window { Size = new(96, 96) };
        var observer = new CanvasNode();
        window.AddChild(sprite); window.AddChild(observer);
        var frames = 0;
        observer.ReadyAction = n =>
        {
            var server = RenderingServer.Instance!;
            server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                frames++;
                using var pixels = server.Readback();
                switch (frames)
                {
                    case 1:
                        Pixel(pixels, 18, 18, Colors.Red); Pixel(pixels, 28, 18, Colors.Green);
                        Pixel(pixels, 18, 28, Colors.Blue); Pixel(pixels, 28, 28, Colors.Yellow);
                        sprite.FlipH = true;
                        break;
                    case 2:
                        Pixel(pixels, 18, 18, Colors.Green); Pixel(pixels, 28, 18, Colors.Red);
                        sprite.FlipV = true;
                        break;
                    case 3:
                        Pixel(pixels, 18, 18, Colors.Yellow); Pixel(pixels, 28, 28, Colors.Red);
                        sprite.HFrames = 2; sprite.VFrames = 2; sprite.FrameCoords = new(1, 1);
                        break;
                    case 4:
                        Pixel(pixels, 18, 18, Colors.Yellow); Pixel(pixels, 26, 18, Colors.Black);
                        sprite.HFrames = 1; sprite.VFrames = 1; sprite.FlipH = sprite.FlipV = false;
                        sprite.RegionEnabled = true; sprite.RegionRect = new(0, 1, 2, 1); sprite.RegionFilterClipEnabled = true;
                        break;
                    case 5:
                        Pixel(pixels, 18, 18, Colors.Blue); Pixel(pixels, 28, 18, Colors.Yellow); Pixel(pixels, 18, 26, Colors.Black);
                        sprite.RegionRect = new(1, 0, 1, 1);
                        break;
                    case 6:
                        Pixel(pixels, 18, 18, Colors.Green); Pixel(pixels, 26, 18, Colors.Black);
                        sprite.RegionEnabled = false; sprite.Centered = true; sprite.Offset = new(1, 1);
                        Task.Run(() => texture.SetSizeOverride(new(4, 4))).GetAwaiter().GetResult();
                        break;
                    case 7:
                        Pixel(pixels, 10, 10, Colors.Red); Pixel(pixels, 34, 10, Colors.Green);
                        Pixel(pixels, 10, 34, Colors.Blue); Pixel(pixels, 34, 34, Colors.Yellow); Pixel(pixels, 42, 34, Colors.Black);
                        Task.Run(() => { image.Fill(Colors.Cyan); texture.Update(image); }).GetAwaiter().GetResult();
                        break;
                    case 8:
                        Pixel(pixels, 10, 10, Colors.Cyan); Pixel(pixels, 34, 34, Colors.Cyan);
                        sprite.Texture = replacement;
                        break;
                    case 9:
                        Pixel(pixels, 18, 18, Colors.Magenta); Pixel(pixels, 10, 10, Colors.Black);
                        sprite.Visible = false;
                        break;
                    case 10:
                        Pixel(pixels, 18, 18, Colors.Black);
                        sprite.RegionEnabled = true; sprite.RegionRect = new(0, 0, 1, 1); sprite.Offset = Vector2.Zero;
                        sprite.Centered = false; sprite.Visible = true;
                        break;
                    case 11:
                        Pixel(pixels, 18, 18, Colors.Magenta); Pixel(pixels, 26, 18, Colors.Black);
                        sprite.Texture = null;
                        break;
                    case 12:
                        Pixel(pixels, 18, 18, Colors.Black);
                        n.Tree!.Quit();
                        break;
                }
            };
        };
        Engine.Instance.Run(window);
        Check(frames == 12 && sprite.IsDisposed && !texture.IsDisposed && !replacement.IsDisposed, "Sprite frame progression and borrowed lifetime.");
        Released(window);
        Console.WriteLine($"Sprite pixel checks passed: {backend}/{fixture ?? "default"}.");
    }
}
