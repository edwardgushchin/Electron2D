using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyScrollRendering(string backend)
    {
        var window = new Window { Size = new(160, 150) };
        var scroll = new ScrollContainer { Position = new(10, 10), Size = new(100, 100) };
        var content = new Control { CustomMinimumSize = new(200, 200) };
        content.Draw += _ =>
        {
            content.DrawRect(new(0, 0, 200, 200), Colors.Red);
            content.DrawRect(new(50, 50, 50, 50), Colors.Blue);
        };
        scroll.AddChild(content);
        window.AddChild(scroll);
        var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = server.Readback();
                frames++;
                if (frames == 1)
                {
                    Check(image.GetPixel(20, 20).R > .8f && image.GetPixel(20, 20).B < .2f &&
                          image.GetPixel(120, 20).R < .2f,
                        $"Initial {backend} scroll frame draws clipped red content and leaves the outside black.");
                    scroll.ScrollHorizontal = 50;
                    scroll.ScrollVertical = 50;
                }
                else if (frames == 2)
                {
                    Check(image.GetPixel(20, 20).B > .8f && image.GetPixel(20, 20).R < .2f &&
                          image.GetPixel(120, 20).B < .2f,
                        $"Scrolled {backend} frame moves the blue region into view without leaking outside the clip.");
                    scroll.FocusMode = FocusMode.All;
                    scroll.DrawFocusBorder = true;
                    scroll.GrabFocus();
                }
                else
                {
                    Check(((Control)scroll.GetChild(5, includeInternal: true)).Visible,
                        $"Focused {backend} scroll frame enables its separate border panel.");
                    Check(image.GetPixel(11, 11).G > .6f && image.GetPixel(20, 20).B > .8f,
                        $"Focused {backend} scroll frame draws a visible light border without covering the content center.");
                    File.WriteAllBytes($"/tmp/electron2d-scroll-focus-{backend}.png", image.SavePNGToBuffer());
                    window.Tree!.Quit();
                }
            };
        };
        Engine.Run(window);
        Released(window);
        Check(frames == 3, $"All three {backend} scroll and focus frames completed.");
        VerifyScrollHints(backend);
        VerifyScrollWarm(backend);
        Console.WriteLine($"Scroll container clipping and content offset rendered on {backend}.");
    }

    private static void VerifyScrollHints(string backend)
    {
        var window = new Window { Size = new(150, 140) };
        var scroll = new ScrollContainer { Position = new(10, 10), Size = new(100, 100) };
        var content = new Control { CustomMinimumSize = new(80, 200) };
        content.Draw += _ => content.DrawRect(new(0, 0, 80, 200), Colors.Red);
        scroll.AddChild(content);
        window.AddChild(scroll);
        var frames = 0;
        var baseline = 0f;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = server.Readback();
                frames++;
                if (frames == 1)
                {
                    baseline = image.GetPixel(20, 95).R;
                    Check(baseline > .9f, $"Unhinted {backend} content is solid red.");
                    scroll.HintMode = ScrollContainer.ScrollHintMode.All;
                }
                else if (frames == 2)
                {
                    File.WriteAllBytes($"/tmp/electron2d-scroll-hint-{backend}.png", image.SavePNGToBuffer());
                    var hint = (Control)scroll.GetChild(2, includeInternal: true);
                    Check(image.GetPixel(20, 95).R < baseline - .04f &&
                          hint.Visible,
                        $"The {backend} trailing gradient darkens overflow content; baseline={baseline}, hinted={image.GetPixel(20, 95).R}, visible={hint.Visible}, position={hint.Position}, size={hint.Size}.");
                    scroll.TileScrollHint = true;
                }
                else
                {
                    Check(image.GetPixel(20, 95).R < baseline - .04f,
                        $"The {backend} tiled hint keeps a visible gradient.");
                    window.Tree!.Quit();
                }
            };
        };
        Engine.Run(window);
        Released(window);
        Check(frames == 3, $"All three {backend} hint frames completed.");
    }

    private static void VerifyScrollWarm(string backend)
    {
        var window = new Window { Size = new(160, 150) };
        var scroll = new ScrollContainer { Position = new(10, 10), Size = new(100, 100) };
        var content = new Control { CustomMinimumSize = new(200, 200) };
        content.Draw += _ => content.DrawRect(new(0, 0, 200, 200), Colors.Red);
        scroll.AddChild(content);
        window.AddChild(scroll);
        var frames = 0;
        long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            var tree = window.Tree!;
            tree.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                scroll.ScrollHorizontal = frames % 2 == 0 ? 0 : 40;
                scroll.ScrollVertical = frames % 2 == 0 ? 40 : 0;
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames == 128) tree.Quit();
            };
        };
        Engine.Run(window);
        Released(window);
        Check(frames == 128 && allocated == 0,
            $"Warmed {backend} scroll offset, layout, recording and render allocated {allocated} managed bytes over 64 frames.");
    }
}
