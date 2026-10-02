using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyNinePatch(string backend)
    {
        using var image = Image.CreateEmpty(5, 5, false, Image.Format.Rgba8);
        for (var y = 0; y < 5; y++) for (var x = 0; x < 5; x++) image.SetPixel(x, y, new Color((x + 1) / 6f, (y + 1) / 6f, (x + y + 1) / 10f, 1));
        using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(100, 90) }; var panels = new List<NinePatchRect>();
        for (var v = 0; v < 3; v++) for (var h = 0; h < 3; h++)
            {
                var panel = new NinePatchRect
                {
                    Name = $"Patch{h}{v}",
                    Position = new(3 + h * 20, 3 + v * 20),
                    Size = new(11, 9),
                    Texture = texture,
                    PatchMarginLeft = 1,
                    PatchMarginRight = 1,
                    PatchMarginTop = 1,
                    PatchMarginBottom = 1,
                    AxisStretchHorizontal = (AxisStretchMode)h,
                    AxisStretchVertical = (AxisStretchMode)v,
                    TextureFilter = TextureFilter.Nearest
                };
                window.AddChild(panel); panels.Add(panel);
            }
        var frame = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frame++;
                if (frame == 1) File.WriteAllBytes($"/tmp/electron2d-nine-patch-{backend}.png", pixels.SavePNGToBuffer());
                for (var v = 0; v < 3; v++) for (var h = 0; h < 3; h++) for (var y = 0; y < 9; y++) for (var x = 0; x < 11; x++)
                            {
                                var inCenter = x >= 1 && x < 10 && y >= 1 && y < 8;
                                var tx = (int)MathF.Floor(NinePatchTests.MapAxis(x + .5f, 11, 5, 1, 1, h));
                                var ty = (int)MathF.Floor(NinePatchTests.MapAxis(y + .5f, 9, 5, 1, 1, v));
                                Pixel(pixels, 3 + h * 20 + x, 3 + v * 20 + y, frame == 2 && inCenter ? Colors.Black : image.GetPixel(Math.Clamp(frame == 3 ? 4 - tx : tx, 0, 4), Math.Clamp(ty, 0, 4)));
                            }
                if (frame == 1) foreach (var panel in panels) panel.DrawCenter = false;
                else if (frame == 2) foreach (var panel in panels) { panel.DrawCenter = true; panel.RegionRect = new(0, 0, -5, 5); }
                else window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        VerifyNinePatchAtlas(backend, texture);
        VerifyNinePatchWarm(backend, texture);
        Console.WriteLine($"Nine-patch nine axis combinations, center suppression, atlas and warmed native frames passed: {backend}.");
    }

    private static void VerifyNinePatchAtlas(string backend, Texture texture)
    {
        using var atlas = new AtlasTexture { Atlas = texture, Region = new(1, 1, 3, 3), Margin = new(1, 1, 2, 2) };
        var window = new Window { Size = new(100, 90) };
        var panel = new NinePatchRect
        {
            Name = "AtlasPatch",
            Position = new(10, 10),
            Size = new(15, 15),
            Texture = atlas,
            TextureFilter = TextureFilter.Nearest,
            PatchMarginLeft = 1,
            PatchMarginTop = 1,
            PatchMarginRight = 1,
            PatchMarginBottom = 1
        }; window.AddChild(panel);
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback();
                Pixel(pixels, 10, 10, Colors.Black); Pixel(pixels, 16, 16, new(.5f, .5f, .5f, 1));
                window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
    }

    private static void VerifyNinePatchWarm(string backend, Texture texture)
    {
        var window = new Window { Size = new(100, 90) }; var panel = new NinePatchRect
        {
            Texture = texture,
            Size = new(30, 20),
            PatchMarginLeft = 1,
            PatchMarginTop = 1,
            PatchMarginRight = 1,
            PatchMarginBottom = 1,
            AxisStretchHorizontal = AxisStretchMode.Tile,
            AxisStretchVertical = AxisStretchMode.TileFit
        };
        window.AddChild(panel); var frames = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!;
            server.FramePreDraw += () => { before = GC.GetAllocatedBytesForCurrentThread(); panel.Size = frames % 2 == 0 ? new(30, 20) : new(32, 22); };
            server.FramePostDraw += () =>
            {
                if (++frames > 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frames == 128) window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(frames == 128 && allocated == 0, $"Warmed resized {backend} nine-patch frames allocate {allocated} bytes.");
    }
}
