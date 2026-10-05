using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyTextureRect(string backend)
    {
        using var image = Image.CreateEmpty(4, 2, false, Image.Format.Rgba8);
        image.SetPixel(0, 0, Colors.Red); image.SetPixel(1, 0, Colors.Red); image.SetPixel(2, 0, Colors.Green); image.SetPixel(3, 0, Colors.Green);
        image.SetPixel(0, 1, Colors.Blue); image.SetPixel(1, 1, Colors.Blue); image.SetPixel(2, 1, Colors.White); image.SetPixel(3, 1, Colors.White);
        using var texture = ImageTexture.CreateFromImage(image); using var view = new AtlasTexture { Atlas = texture, Region = new(1, 0, 2, 2) };
        using var nested = new AtlasTexture { Atlas = view, Region = new(0, 0, 2, 2) };
        var window = new Window { Size = new(96, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var host = new Control(); window.AddChild(host);
        var node = new TextureRect { Texture = texture, ExpandMode = TextureRectExpandMode.IgnoreSize, Position = new(4, 4), Size = new(16, 16) }; host.AddChild(node);
        var frames = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            var software = RenderingServer.GetCurrentRenderingDriverName() == "software";
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = renderer.Readback();
                // The software input truncates half-texel crop boundaries before interpolation.
                if (software && frames == 6)
                { Pixel(pixels, 5, 5, Colors.Red); Pixel(pixels, 13, 5, Colors.Red); Pixel(pixels, 5, 13, Colors.Red); Pixel(pixels, 13, 13, Colors.Red); Pixel(pixels, 18, 5, Colors.Green); Pixel(pixels, 5, 18, Colors.Blue); Pixel(pixels, 18, 18, Colors.White); }
                else if (frames == 0 || frames == 6)
                { Pixel(pixels, 5, 5, Colors.Red); Pixel(pixels, 13, 5, Colors.Green); Pixel(pixels, 5, 13, Colors.Blue); Pixel(pixels, 13, 13, Colors.White); }
                else if (frames == 1)
                { Pixel(pixels, 4, 4, Colors.Red); Pixel(pixels, 6, 4, Colors.Green); Pixel(pixels, 4, 5, Colors.Blue); Pixel(pixels, 8, 4, Colors.Red); }
                else if (frames == 2)
                { Pixel(pixels, 4, 4, Colors.Red); Pixel(pixels, 6, 5, Colors.White); Pixel(pixels, 8, 4, Colors.Black); }
                else if (frames == 3)
                { Pixel(pixels, 10, 11, Colors.Red); Pixel(pixels, 12, 12, Colors.White); Pixel(pixels, 6, 6, Colors.Black); }
                else if (frames == 4)
                { Pixel(pixels, 5, 5, Colors.Red); Pixel(pixels, 13, 9, Colors.White); Pixel(pixels, 5, 13, Colors.Black); }
                else if (frames == 5)
                { Pixel(pixels, 5, 5, Colors.Black); Pixel(pixels, 5, 9, Colors.Red); Pixel(pixels, 13, 13, Colors.White); }
                else if (frames == 7)
                { Pixel(pixels, 5, 5, Colors.White); Pixel(pixels, 13, 5, Colors.Blue); Pixel(pixels, 5, 13, Colors.Green); Pixel(pixels, 13, 13, Colors.Red); }
                else if (frames == 8)
                { Pixel(pixels, 4, 4, Colors.Red); Pixel(pixels, 5, 4, Colors.Green); Pixel(pixels, 6, 4, Colors.Red); Pixel(pixels, 4, 5, Colors.Blue); }
                else if (frames == 9)
                { Pixel(pixels, 4, 4, Colors.White); Pixel(pixels, 5, 4, Colors.Blue); Pixel(pixels, 4, 5, Colors.Green); }
                else if (frames == 10)
                { Pixel(pixels, 4, 4, Colors.Green); Pixel(pixels, 5, 4, Colors.Red); Pixel(pixels, 4, 5, Colors.White); }
                else if (frames == 11)
                { Pixel(pixels, 4, 4, Colors.Blue); Pixel(pixels, 5, 4, Colors.White); Pixel(pixels, 4, 5, Colors.Red); }
                else
                { Pixel(pixels, 5, 5, Colors.Black); window.Tree!.Quit(); }
                var profile = Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy" ? $"{backend}-dummy" : backend;
                pixels.SavePNG(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"e2d-texture-rect-{profile}-{frames}.png"));
                frames++;
                if (frames <= 6) node.StretchMode = (TextureStretchMode)frames;
                else if (frames == 7) { node.FlipH = true; node.FlipV = true; }
                else if (frames == 8) { node.Texture = nested; node.StretchMode = TextureStretchMode.Tile; node.FlipH = false; node.FlipV = false; }
                else if (frames == 9) { node.FlipH = true; node.FlipV = true; }
                else if (frames == 10) node.FlipV = false;
                else if (frames == 11) { node.FlipH = false; node.FlipV = true; }
                else if (frames == 12) node.Texture = null;
            };
        };
        Engine.Run(window); Released(window); Check(frames == 13, "All texture rectangle visible states completed.");
        VerifyTextureRectWarm(backend, texture);
        Console.WriteLine($"Texture rectangle seven stretches/reflections/nested-atlas tile/null and warmed frame checks passed ({backend}).");
    }
    private static void VerifyTextureRectWarm(string backend, Texture texture)
    {
        var window = new Window { Size = new(96, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var node = new TextureRect { Texture = texture, ExpandMode = TextureRectExpandMode.IgnoreSize, StretchMode = TextureStretchMode.KeepAspectCentered, Size = new(16, 16) }; window.AddChild(node);
        var frames = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames < 84) { node.FlipH = frames % 2 == 0; node.Size = new(frames % 2 == 0 ? 16 : 18, 16); }
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                if (frames is >= 20 and < 84) active += bytes;
                if (frames >= 84) idle += bytes;
                if (++frames == 148) window.Tree!.Quit();
            };
        };
        Engine.Run(window); Released(window);
        Check(active == 0 && idle == 0, $"Texture rectangle warm process/minimum/record/render allocated {active}/{idle} managed bytes ({backend}).");
    }
}
