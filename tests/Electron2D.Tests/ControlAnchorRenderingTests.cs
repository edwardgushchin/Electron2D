using Electron2D;

internal static class ControlAnchorRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(160, 96), Borderless = true };
        var panel = new ColorRect
        {
            AnchorRight = .75f,
            AnchorBottom = .75f,
            AnchorLeft = .25f,
            AnchorTop = .25f,
            OffsetLeft = 8,
            OffsetTop = 12,
            OffsetRight = -16,
            OffsetBottom = -20,
            Color = Colors.Red
        };
        window.AddChild(panel);
        using var target = window.GetTexture();
        Vector2i[] sizes = [new(160, 96), new(200, 120), new(200, 120), new(160, 96)];
        Rect2[] rectangles = [new(48, 36, 56, 16), new(58, 42, 76, 28), new(66, 42, 76, 28), new(56, 36, 56, 16)];
        var phase = 0;
        var frames = 0;
        var driver = string.Empty;
        var adapter = string.Empty;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Expected exact rendering method.");
            driver = RenderingServer.GetCurrentRenderingDriverName();
            adapter = RenderingServer.GetVideoAdapterName();
            Check(driver.Length > 0 && driver != "software", "Expected a native rendering driver.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_EXPECTED_DRIVER") is { } expected)
                Check(driver == expected, $"Expected driver {expected}, got {driver}.");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += Capture;
        };

        try { Check(Engine.Run(window) == 0 && phase == sizes.Length, "Adaptive UI completed."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
        Check(window.IsDisposed && panel.IsDisposed && !RenderingServer.IsAvailable, "Scene and renderer were released.");
        Check(target.GetImage() is null, "Borrowed window image is released on teardown.");
        Console.WriteLine($"Control anchors native: {backend}/{driver}; adapter={adapter}; four resized or mirrored rectangles and pixel bounds; scene/renderer/target released.");
        return;

        void Capture()
        {
            if (++frames > 120) throw new InvalidOperationException("The window did not reach the requested render size.");
            using var image = target.GetImage();
            if (image is null || image.Size != sizes[phase] || window.GetVisibleRect().Size != (Vector2)sizes[phase]) return;
            var rect = rectangles[phase];
            Check(panel.Position == rect.Position && panel.Size == rect.Size, $"Phase {phase} expected {rect}, got {panel.GetRect()}.");
            var left = (int)rect.Position.X;
            var top = (int)rect.Position.Y;
            var right = (int)rect.End.X;
            var bottom = (int)rect.End.Y;
            Pixel(image, left, top, red: true);
            Pixel(image, right - 1, bottom - 1, red: true);
            Pixel(image, left - 1, top, red: false);
            Pixel(image, right, bottom - 1, red: false);
            Pixel(image, left, top - 1, red: false);
            Pixel(image, right - 1, bottom, red: false);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory)
                image.SavePNG(System.IO.Path.Combine(directory, $"anchors-{backend}-{phase}.png"));

            phase++;
            switch (phase)
            {
                case 1: window.Size = sizes[phase]; break;
                case 2: panel.LayoutDirection = LayoutDirection.RTL; break;
                case 3: window.Size = sizes[phase]; break;
                default:
                    RenderingServer.FramePostDraw -= Capture;
                    window.Tree!.Quit();
                    break;
            }
        }
    }

    private static void Pixel(Image image, int x, int y, bool red)
    {
        var pixel = image.GetPixel(x, y);
        Check((red ? pixel.R > .9f : pixel.R < .1f) && pixel.G < .1f && pixel.B < .1f,
            $"Unexpected pixel {pixel} at ({x}, {y}).");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
