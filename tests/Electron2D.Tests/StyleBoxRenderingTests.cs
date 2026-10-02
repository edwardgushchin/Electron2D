using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyStyleBoxes(string backend)
    {
        using var image = Image.CreateEmpty(5, 5, false, Image.Format.Rgba8);
        for (var y = 0; y < 5; y++) for (var x = 0; x < 5; x++) image.SetPixel(x, y, new Color((x + 1) / 6f, (y + 1) / 6f, (x + y + 1) / 10f, 1));
        using var texture = ImageTexture.CreateFromImage(image);
        var styles = new StyleBoxTexture[9];
        for (var v = 0; v < 3; v++) for (var h = 0; h < 3; h++)
            {
                var style = new StyleBoxTexture { Texture = texture, AxisStretchHorizontal = (AxisStretchMode)h, AxisStretchVertical = (AxisStretchMode)v };
                style.SetTextureMarginAll(1.25f); styles[v * 3 + h] = style;
            }
        using var expanded = new StyleBoxTexture { Texture = texture, ExpandMarginLeft = 1.25f, ExpandMarginTop = .75f, ExpandMarginRight = .75f, ExpandMarginBottom = 1.25f, ModulateColor = new(1, .5f, 1, 1) };
        using var atlas = new AtlasTexture { Atlas = texture, Region = new(1, 1, 3, 3), Margin = new(1, 1, 2, 2) };
        using var atlasStyle = new StyleBoxTexture { Texture = atlas, RegionRect = new(0, 0, 5, 5) }; atlasStyle.SetExpandMarginAll(2);
        using var horizontal = new StyleBoxLine { Color = Colors.Red, Thickness = 3, GrowBegin = 1.75f, GrowEnd = 2.5f };
        using var vertical = new StyleBoxLine { Color = Colors.Blue, Vertical = true, Thickness = 2, GrowBegin = -1.5f, GrowEnd = 3.25f };
        using var empty = new StyleBoxEmpty();
        var window = new Window { Size = new(160, 120) }; var frames = 0;
        var node = new CanvasNode
        {
            TextureFilter = TextureFilter.Nearest,
            DrawAction = canvas =>
            {
                for (var v = 0; v < 3; v++) for (var h = 0; h < 3; h++) canvas.DrawStyleBox(styles[v * 3 + h], new(3 + h * 20, 3 + v * 20, 11, 9));
                expanded.Draw(canvas, new(80, 10, 11, 9)); canvas.DrawStyleBox(atlasStyle, new(80, 30, 15, 15));
                horizontal.Draw(canvas, new(8.9f, 74.9f, 14.9f, 10.9f)); canvas.DrawStyleBox(vertical, new(45.9f, 74.9f, 10.9f, 14.9f));
                canvas.DrawRect(new(80, 70, 20, 20), Colors.Green); canvas.DrawStyleBox(empty, new(80, 70, 20, 20));
            }
        }; window.AddChild(node);
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                if (frames == 1) File.WriteAllBytes($"/tmp/electron2d-style-box-{backend}.png", pixels.SavePNGToBuffer());
                var sourceSize = frames == 3 ? 3 : 5; var margin = frames == 3 ? .5f : 1.25f;
                for (var v = 0; v < 3; v++) for (var h = 0; h < 3; h++) for (var y = 0; y < 9; y++) for (var x = 0; x < 11; x++)
                            {
                                if (frames == 2 && x + .5f >= margin && x + .5f < 11 - margin && y + .5f >= margin && y + .5f < 9 - margin)
                                { Pixel(pixels, 3 + h * 20 + x, 3 + v * 20 + y, Colors.Black); continue; }
                                var tx = NinePatchTests.MapAxis(x + .5f, 11, sourceSize, margin, margin, h);
                                var ty = NinePatchTests.MapAxis(y + .5f, 9, sourceSize, margin, margin, v);
                                // Exact texel boundaries are backend-sensitive; compare interior nearest samples.
                                if (MathF.Abs(tx - MathF.Round(tx)) < .0001f || MathF.Abs(ty - MathF.Round(ty)) < .0001f) continue;
                                var offset = frames == 3 ? 1 : 0;
                                var expected = image.GetPixel(offset + Math.Clamp((int)MathF.Floor(tx), 0, sourceSize - 1), offset + Math.Clamp((int)MathF.Floor(ty), 0, sourceSize - 1));
                                if (frames == 3) expected *= new Color(.5f, 1, .5f, 1);
                                Pixel(pixels, 3 + h * 20 + x, 3 + v * 20 + y, expected);
                            }
                Pixel(pixels, 78, 10, Colors.Black); Pixel(pixels, 79, 10, image.GetPixel(0, 0) * expanded.ModulateColor); Pixel(pixels, 80, 9, image.GetPixel(0, 0) * expanded.ModulateColor);
                Pixel(pixels, 91, 19, image.GetPixel(4, 4) * expanded.ModulateColor); Pixel(pixels, 92, 19, Colors.Black);
                if (frames == 1)
                {
                    Pixel(pixels, 80, 31, Colors.Black); Pixel(pixels, 81, 31, image.GetPixel(1, 1));
                    Pixel(pixels, 93, 43, image.GetPixel(3, 3)); Pixel(pixels, 94, 43, Colors.Black);
                }
                else
                {
                    Pixel(pixels, 82, 33, Colors.Black); Pixel(pixels, 83, 33, image.GetPixel(1, 1));
                    Pixel(pixels, 96, 46, image.GetPixel(2, 2)); Pixel(pixels, 97, 46, Colors.Black);
                }
                Pixel(pixels, 5, 74, Colors.Black); Pixel(pixels, 6, 74, Colors.Red); Pixel(pixels, 23, 76, Colors.Red); Pixel(pixels, 24, 76, Colors.Black); Pixel(pixels, 10, 77, Colors.Black);
                Pixel(pixels, 45, 74, Colors.Black); Pixel(pixels, 45, 75, Colors.Blue); Pixel(pixels, 46, 89, Colors.Blue); Pixel(pixels, 47, 89, Colors.Black); Pixel(pixels, 45, 90, Colors.Black);
                Pixel(pixels, 80, 70, Colors.Green); Pixel(pixels, 90, 80, Colors.Green); Pixel(pixels, 99, 89, Colors.Green); Pixel(pixels, 100, 89, Colors.Black);
                if (frames == 1)
                {
                    foreach (var style in styles) style.DrawCenter = false;
                    atlasStyle.RegionRect = default; node.QueueRedraw();
                }
                else if (frames == 2)
                {
                    foreach (var style in styles) { style.DrawCenter = true; style.RegionRect = new(1, 1, 3, 3); style.SetTextureMarginAll(.5f); style.ModulateColor = new(.5f, 1, .5f, 1); }
                    node.QueueRedraw();
                }
                else window.Tree!.Quit();
            };
        };
        try { Engine.Instance.Run(window); Released(window); }
        finally { foreach (var style in styles) style.Dispose(); }
        Check(!texture.IsDisposed && !atlas.IsDisposed, "Style drawing and disposal preserve borrowed textures.");
        VerifyStyleBoxWarm(backend, texture);
        Console.WriteLine($"Style-box nine axis combinations, float margins/expansion, center suppression, region, modulation, atlas ordering, lines, empty and warmed native frames passed: {backend}.");
    }

    private static void VerifyStyleBoxWarm(string backend, Texture texture)
    {
        using var style = new StyleBoxTexture { Texture = texture, AxisStretchHorizontal = AxisStretchMode.Tile, AxisStretchVertical = AxisStretchMode.TileFit };
        style.SetTextureMarginAll(1.25f);
        using var line = new StyleBoxLine { Color = Colors.Red, Thickness = 2 };
        using var empty = new StyleBoxEmpty();
        var window = new Window { Size = new(160, 120) };
        var node = new CanvasNode
        {
            TextureFilter = TextureFilter.Nearest,
            DrawAction = canvas => { canvas.DrawStyleBox(style, new(10, 10, 31, 23)); line.Draw(canvas, new(10, 50, 31, 6)); empty.Draw(canvas, new(60, 10, 20, 20)); }
        }; window.AddChild(node);
        var frames = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            var tree = window.Tree!;
            tree.ProcessFrameStarted += frameTree =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                style.ModulateColor = frames % 2 == 0 ? Colors.White : Colors.Green;
                style.SetExpandMarginAll(frames % 2 == 0 ? 1.25f : 2.25f);
                line.Thickness = frames % 2 == 0 ? 2 : 3; node.QueueRedraw();
            };
            RenderingServer.Instance!.FramePostDraw += () =>
            {
                if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames == 128) tree.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(frames == 128 && node.Draws == 128 && allocated == 0, $"Warmed {backend} style mutation/recording/render allocated {allocated} bytes over 64 ProcessFrameStarted-to-FramePostDraw frames; recordings={node.Draws}.");
    }
}
