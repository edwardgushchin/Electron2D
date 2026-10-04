using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyAnimatedSprite(string backend, string? fixture = null)
    {
        using var image = Image.CreateFromData(2, 2, false, Image.Format.Rgba8,
            new byte[] { 255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 0, 255 });
        using var texture = ImageTexture.CreateFromImage(image);
        using var atlas = new AtlasTexture { Atlas = texture, Region = new(1, 0, 1, 1), FilterClip = true };
        using var frames = new SpriteFrames();
        frames.AddFrame("default", texture); frames.AddFrame("default", atlas); frames.AddFrame("default", null); frames.AddFrame("default", texture);
        frames.SetAnimationSpeed("default", 20); frames.SetAnimationLoopMode("default", SpriteFrames.LoopMode.None);
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var sprite = new AnimatedSprite { SpriteFrames = frames, Centered = false, Position = new(16, 16), Scale = new(8, 8), Material = material };
        var window = new Window { Size = new(96, 96), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var observer = new CanvasNode(); window.AddChild(sprite); window.AddChild(observer);
        var draws = 0; var finished = 0; var stage = 0;
        sprite.AnimationFinished += () => finished++;
        observer.ReadyAction = n =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                Check(++draws <= 120, "Timed animation must finish in the native host.");
                using var pixels = server.Readback();
                switch (stage)
                {
                    case 0:
                        Pixel(pixels, 18, 18, Colors.Red); Pixel(pixels, 28, 18, Colors.Green);
                        sprite.FlipH = true; stage++; break;
                    case 1:
                        Pixel(pixels, 18, 18, Colors.Green); Pixel(pixels, 28, 18, Colors.Red);
                        sprite.FlipV = true; stage++; break;
                    case 2:
                        Pixel(pixels, 18, 18, Colors.Yellow); Pixel(pixels, 28, 28, Colors.Red);
                        sprite.FlipH = sprite.FlipV = false; sprite.Frame = 1; stage++; break;
                    case 3:
                        Pixel(pixels, 18, 18, Colors.Green); Pixel(pixels, 26, 18, Colors.Black);
                        sprite.Frame = 2; stage++; break;
                    case 4:
                        Pixel(pixels, 18, 18, Colors.Black);
                        Task.Run(() => frames.SetFrame("default", 2, texture)).GetAwaiter().GetResult(); stage++; break;
                    case 5:
                        Pixel(pixels, 18, 18, Colors.Red);
                        sprite.Centered = true; sprite.Offset = new(1, 1); sprite.Play(); stage++; break;
                    case 6:
                        if (finished == 0) break;
                        Check(!sprite.IsPlaying() && sprite.Frame == 3, "Native playback reaches the last frame and pauses.");
                        Pixel(pixels, 18, 18, Colors.Red); Pixel(pixels, 28, 28, Colors.Yellow);
                        Task.Run(() => { image.Fill(Colors.Cyan); texture.Update(image); }).GetAwaiter().GetResult(); stage++; break;
                    case 7:
                        Pixel(pixels, 18, 18, Colors.Cyan); Pixel(pixels, 28, 28, Colors.Cyan);
                        sprite.Visible = false; stage++; break;
                    case 8:
                        Pixel(pixels, 18, 18, Colors.Black); sprite.SpriteFrames = null; stage++; break;
                    default:
                        n.Tree!.Quit(); break;
                }
            };
        };
        Check(Engine.Run(window) == 0 && finished == 1 && stage == 9, "Animated canvas run completed.");
        Released(window); Check(!frames.IsDisposed && !texture.IsDisposed && !atlas.IsDisposed, "Runtime borrows animation resources.");
        Console.WriteLine($"AnimatedSprite {backend}/{fixture ?? "builtin"} readback and timed playback passed.");
    }

    private static void VerifyAnimatedSpriteFailure()
    {
        using var frames = new SpriteFrames(); frames.AddFrame("default", null); frames.SetAnimationSpeed("default", 1000);
        var sprite = new AnimatedSprite { SpriteFrames = frames, Autoplay = "default" };
        var window = new Window { Size = new(96, 96) }; window.AddChild(sprite);
        var threw = false; sprite.AnimationLooped += () => { threw = true; throw new ApplicationException("animation callback"); };
        Reject<AggregateException>(() => Engine.Run(window));
        Check(threw && !frames.IsDisposed, "Animation callback failure exits the host while retaining borrowed resources."); Released(window);
    }
}
