using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyFlatStyles(string backend)
    {
        using var rounded = new StyleBoxFlat { BGColor = Colors.Red, BorderColor = Colors.Blue, AntiAliasing = false };
        rounded.SetBorderWidthAll(4); rounded.SetCornerRadiusAll(10);
        using var blended = new StyleBoxFlat { BGColor = Colors.Blue, BorderColor = Colors.Red, BorderBlend = true, AntiAliasing = false };
        blended.SetBorderWidthAll(8);
        using var shadow = new StyleBoxFlat { BGColor = Colors.White, ShadowSize = 8, ShadowOffset = new(8, 5), ShadowColor = new(0, 1, 0, .8f), AntiAliasing = false };
        using var skewed = new StyleBoxFlat { BGColor = Colors.Green, BorderColor = Colors.White, Skew = new(.5f, 0), AntiAliasing = true, AntiAliasingSize = 2 };
        skewed.SetBorderWidthAll(2);
        using var expanded = new StyleBoxFlat { BGColor = Colors.Yellow, ExpandMarginLeft = 3.25f, ExpandMarginTop = 2.75f, ExpandMarginRight = 1.25f, ExpandMarginBottom = .25f };
        var window = new Window { Size = new(224, 140) }; var frames = 0;
        var node = new CanvasNode
        {
            DrawAction = canvas =>
            {
                canvas.DrawStyleBox(rounded, new(10, 10, 40, 30)); canvas.DrawStyleBox(blended, new(70, 10, 40, 30));
                canvas.DrawStyleBox(shadow, new(140, 10, 30, 30)); canvas.DrawStyleBox(skewed, new(10, 70, 40, 30));
                canvas.DrawStyleBox(expanded, new(80, 70, 20, 20));
            }
        }; window.AddChild(node);
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                if (frames == 1) File.WriteAllBytes($"/tmp/electron2d-flat-styles-{backend}.png", pixels.SavePNGToBuffer());
                Pixel(pixels, 9, 25, Colors.Black); Pixel(pixels, 30, 11, Colors.Blue); Pixel(pixels, 11, 25, Colors.Blue);
                Pixel(pixels, 10, 10, frames == 3 ? Colors.Blue : Colors.Black);
                Pixel(pixels, 30, 25, frames == 2 ? Colors.Black : Colors.Red);
                Pixel(pixels, 90, 25, Colors.Blue);
                FlatGradientPixel(pixels, 72, 25, frames == 3 ? Colors.Red : new(.6875f, 0, .3125f, 1));
                FlatGradientPixel(pixels, 76, 25, frames == 3 ? Colors.Red : new(.1875f, 0, .8125f, 1));
                Pixel(pixels, 135, 30, Colors.Black); Pixel(pixels, 188, 30, Colors.Black);
                Pixel(pixels, 160, 25, frames == 2 ? Colors.Black : Colors.White);
                FlatGradientPixel(pixels, 175, 30, frames == 2 ? Colors.Black : new(0, .8f, 0, 1));
                FlatGradientPixel(pixels, 182, 30, new(0, .35f, 0, 1));
                Pixel(pixels, 30, 85, Colors.Green);
                Pixel(pixels, 12, 73, frames == 3 ? Colors.Green : Colors.Black);
                Pixel(pixels, 52, 73, frames == 3 ? Colors.Black : Colors.Green);
                FlatGradientPixel(pixels, 10, 84, frames == 1 ? new(.625f, .625f, .625f, 1) : Colors.White);
                FlatGradientPixel(pixels, 9, 84, frames == 1 ? new(.125f, .125f, .125f, 1) : Colors.Black);
                Pixel(pixels, 76, 68, Colors.Black); Pixel(pixels, 101, 89, Colors.Black);
                Pixel(pixels, 77, 68, frames == 2 ? Colors.Black : Colors.Yellow); Pixel(pixels, 100, 89, frames == 2 ? Colors.Black : Colors.Yellow);
                if (frames == 1)
                {
                    rounded.DrawCenter = false; shadow.DrawCenter = false; expanded.DrawCenter = false; skewed.AntiAliasing = false; node.QueueRedraw();
                }
                else if (frames == 2)
                {
                    rounded.DrawCenter = true; rounded.SetCornerRadiusAll(0); shadow.DrawCenter = true; expanded.DrawCenter = true;
                    blended.BorderBlend = false; skewed.Skew = new(-.5f, 0); node.QueueRedraw();
                }
                else window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window); Check(node.Draws == 3, "Flat style mutations rerecord the three visible states.");
        VerifyFlatStyleWarm(backend);
        Console.WriteLine($"Flat-style native rounded corners, borders, center suppression, border blend, offset shadow, skew, AA, expansion and warmed mutation/render passed: {backend}.");
    }

    private static void FlatGradientPixel(Image frame, int x, int y, Color expected)
    {
        var actual = frame.GetPixel(x, y);
        Check(MathF.Abs(actual.R - expected.R) < .025f && MathF.Abs(actual.G - expected.G) < .025f && MathF.Abs(actual.B - expected.B) < .025f && MathF.Abs(actual.A - expected.A) < .025f,
            $"Flat gradient pixel ({x},{y}): expected {expected}, got {actual}.");
    }

    private static void VerifyFlatStyleWarm(string backend)
    {
        using var style = new StyleBoxFlat { BGColor = Colors.Blue, BorderColor = Colors.White, BorderBlend = true, ShadowSize = 4, ShadowOffset = new(3, 2), AntiAliasing = true, AntiAliasingSize = 2 };
        style.SetBorderWidthAll(3); style.SetCornerRadiusAll(8);
        var window = new Window { Size = new(160, 120) };
        var node = new CanvasNode { DrawAction = canvas => canvas.DrawStyleBox(style, new(20, 20, 60, 40)) }; window.AddChild(node);
        var frames = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            var tree = window.Tree!;
            tree.ProcessFrameStarted += frameTree =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                style.BGColor = frames % 2 == 0 ? Colors.Red : Colors.Green;
                style.Skew = frames % 2 == 0 ? new(.125f, .05f) : new(.25f, .05f);
                style.SetExpandMarginAll(frames % 2 == 0 ? 1.25f : 2.25f);
                style.ShadowSize = frames % 2 == 0 ? 4 : 5; node.QueueRedraw();
            };
            RenderingServer.Instance!.FramePostDraw += () =>
            {
                if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames == 128) tree.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(frames == 128 && node.Draws == 128 && allocated == 0, $"Warmed {backend} flat style mutation/recording/render allocated {allocated} bytes over 64 ProcessFrameStarted-to-FramePostDraw frames; recordings={node.Draws}.");
    }
}
