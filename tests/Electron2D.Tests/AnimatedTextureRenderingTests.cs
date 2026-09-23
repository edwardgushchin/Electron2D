using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyAnimatedTexture(string backend)
    {
        using var redImage = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        using var greenImage = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        redImage.Fill(Colors.Red); greenImage.Fill(Colors.Green);
        using var red = ImageTexture.CreateFromImage(redImage);
        using var green = ImageTexture.CreateFromImage(greenImage);
        using var animation = new AnimatedTexture { Frames = 2 };
        animation.SetFrameTexture(0, red); animation.SetFrameTexture(1, green);
        animation.SetFrameDuration(0, .05f); animation.SetFrameDuration(1, .05f);
        var reentered = false;
        animation.Changed += _ =>
        {
            if (reentered || RenderingServer.Instance is null) return;
            using var transient = new AnimatedTexture();
            transient.SetFrameTexture(0, red);
            reentered = true;
        };
        var sprite = new Sprite { Texture = animation, Centered = false, Position = new(30, 30) };
        var window = new Window { Size = new(256, 128), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; window.AddChild(sprite);
        var frames = 0; var stage = 0; var pausedFrames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback();
                Check(++frames <= 120, "Animated texture must advance during the native run.");
                Pixel(pixels, 31, 31, animation.CurrentFrame == 0 ? Colors.Red : Colors.Green);
                Pixel(pixels, 33, 31, Colors.Black);
                switch (stage)
                {
                    case 0:
                        if (animation.CurrentFrame == 1) { animation.Pause = true; stage = 1; }
                        break;
                    case 1:
                        Check(animation.CurrentFrame == 1, "Pause holds the selected frame in the renderer.");
                        if (++pausedFrames == 3) { animation.SpeedScale = -1; animation.Pause = false; stage = 2; }
                        break;
                    default:
                        if (animation.CurrentFrame == 0) { stage = 3; window.Tree!.Quit(); }
                        break;
                }
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(stage == 3 && frames >= 5 && reentered && !red.IsDisposed && !green.IsDisposed && !animation.IsDisposed,
            "Renderer changed frames, paused, reversed and retained borrowed resources.");
        Console.WriteLine($"AnimatedTexture timed native pixels passed: {backend}.");
    }

    private static void VerifyAnimatedTextureFailure()
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        using var source = ImageTexture.CreateFromImage(image);
        using var animation = new AnimatedTexture { Frames = 2 };
        animation.SetFrameTexture(0, source); animation.SetFrameTexture(1, source);
        animation.SetFrameDuration(0, .001f); animation.SetFrameDuration(1, .001f);
        var window = new Window { Size = new(96, 96), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        window.AddChild(new Sprite { Texture = animation });
        var threw = false;
        animation.Changed += _ => { threw = true; throw new ApplicationException("animated texture callback"); };
        Reject<ApplicationException>(() => Engine.Instance.Run(window));
        Check(threw && !source.IsDisposed && !animation.IsDisposed, "Animated texture callback failure cleans up the host and retains borrowed resources.");
        Released(window);
    }
}
