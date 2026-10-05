using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyTextureProgress(string backend)
    {
        using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(110, 85) }; var bars = new List<TextureProgressBar>();
        for (var mode = 0; mode < 9; mode++)
        {
            var bar = new TextureProgressBar
            {
                Name = $"Bar{mode}",
                Position = new(3 + mode % 3 * 30, 3 + mode / 3 * 25),
                TextureProgress = texture,
                FillMode = (TextureProgressFillMode)mode,
                Value = 50,
                TextureFilter = TextureFilter.Nearest
            };
            bars.Add(bar); window.AddChild(bar);
        }
        var frame = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frame++;
                if (frame == 1) File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-texture-progress-{backend}.png"), pixels.SavePNGToBuffer());
                for (var mode = 0; mode < 9; mode++) for (var y = 0; y < 8; y++) for (var x = 0; x < 8; x++)
                        {
                            var filled = frame == 2 ? false : frame == 3 ? true : mode switch
                            {
                                0 => x < 4,
                                1 => x >= 4,
                                2 => y < 4,
                                3 => y >= 4,
                                4 => x >= 4,
                                5 => x < 4,
                                6 => x >= 2 && x < 6,
                                7 => y >= 2 && y < 6,
                                _ => y < 4
                            };
                            Pixel(pixels, 3 + mode % 3 * 30 + x, 3 + mode / 3 * 25 + y, filled ? Colors.White : Colors.Black);
                        }
                if (frame == 1) foreach (var bar in bars) bar.Value = 0;
                else if (frame == 2) foreach (var bar in bars) bar.Value = 100;
                else window.Tree!.Quit();
            };
        };
        Engine.Run(window); Released(window);
        VerifyProgressLayers(backend, texture);
        VerifyProgressNinePatch(backend, texture);
        VerifyProgressWarm(backend, texture);
        Console.WriteLine($"Texture progress nine fills, empty/full, nine-patch and warmed native frames passed: {backend}.");
    }
    private static void VerifyProgressLayers(string backend, Texture texture)
    {
        var window = new Window { Size = new(40, 30) }; var bar = new TextureProgressBar
        {
            Position = new(3, 3),
            TextureUnder = texture,
            TextureProgress = texture,
            TextureOver = texture,
            TintUnder = Colors.Red,
            TintProgress = Colors.Green,
            TintOver = new(0, 0, 1, .5f),
            TextureProgressOffset = new(2, 0),
            Value = 50,
            TextureFilter = TextureFilter.Nearest
        }; window.AddChild(bar); var frames = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback();
                if (++frames == 1) { Pixel(pixels, 3, 4, new(.5f, 0, .5f, 1)); Pixel(pixels, 5, 4, new(0, .5f, .5f, 1)); bar.TextureOver = null; bar.TintUnder = Colors.Blue; bar.TintProgress = Colors.White; }
                else { Pixel(pixels, 4, 4, Colors.Blue); Pixel(pixels, 6, 4, Colors.White); window.Tree!.Quit(); }
            };
        };
        Engine.Run(window); Released(window);
    }
    private static void VerifyProgressNinePatch(string backend, Texture texture)
    {
        var window = new Window { Size = new(110, 85) }; var bars = new List<TextureProgressBar>();
        foreach (var mode in new[] { 0, 1, 2, 3, 6, 7 })
        {
            var bar = new TextureProgressBar
            {
                Name = $"Bar{mode}",
                Position = new(3 + bars.Count % 3 * 30, 3 + bars.Count / 3 * 30),
                TextureProgress = texture,
                FillMode = (TextureProgressFillMode)mode,
                Value = 50,
                NinePatchStretch = true,
                Size = new(20, 16),
                StretchMarginLeft = 2,
                StretchMarginRight = 2,
                StretchMarginTop = 2,
                StretchMarginBottom = 2,
                TextureFilter = TextureFilter.Nearest
            };
            bars.Add(bar); window.AddChild(bar);
        }
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback();
                for (var index = 0; index < bars.Count; index++) for (var y = 0; y < 16; y++) for (var x = 0; x < 20; x++)
                        {
                            var filled = (int)bars[index].FillMode switch { 0 => x < 10, 1 => x >= 10, 2 => y < 8, 3 => y >= 8, 6 => x >= 5 && x < 15, _ => y >= 4 && y < 12 };
                            Pixel(pixels, 3 + index % 3 * 30 + x, 3 + index / 3 * 30 + y, filled ? Colors.White : Colors.Black);
                        }
                window.Tree!.Quit();
            };
        };
        Engine.Run(window); Released(window);
    }
    private static void VerifyProgressWarm(string backend, Texture texture)
    {
        var window = new Window { Size = new(110, 85) }; var bar = new TextureProgressBar
        {
            TextureProgress = texture,
            FillMode = TextureProgressFillMode.Clockwise,
            NinePatchStretch = true,
            Size = new(40, 40),
            Step = 0,
            TextureFilter = TextureFilter.Nearest
        }; window.AddChild(bar);
        var frames = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!;
            RenderingServer.FramePreDraw += () => { before = GC.GetAllocatedBytesForCurrentThread(); bar.Value = frames % 2 == 0 ? 25 : 75; };
            RenderingServer.FramePostDraw += () =>
            {
                if (++frames > 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frames == 128) window.Tree!.Quit();
            };
        };
        Engine.Run(window); Released(window); Check(allocated == 0, $"Warmed {backend} radial value/render path allocates {allocated} managed bytes.");
    }
}
