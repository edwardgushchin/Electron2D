using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyCanvasPolygons(string backend, string? fixture = null)
    {
        using var image = Image.CreateFromData(2, 2, false, Image.Format.Rgba8,
            new byte[] { 255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 0, 255 });
        using var texture = ImageTexture.CreateFromImage(image);
        using var atlas = new AtlasTexture { Atlas = texture, Region = new(1, 0, 1, 1) };
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(144, 104), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        Vector2[] contour = [new(0, 0), new(28, 0), new(28, 8), new(8, 8), new(8, 28), new(0, 28)];
        Vector2[] quad = [new(0, 0), new(24, 0), new(24, 24), new(0, 24)];
        Vector2[] uvs = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
        var frames = 0; var draws = 0;
        var node = new CanvasNode
        {
            Material = material,
            DrawAction = n =>
        {
            draws++;
            n.DrawSetTransform(new(4, 4)); n.DrawColoredPolygon(contour, Colors.Red);
            n.DrawSetTransform(new(40, 4)); n.DrawColoredPolygon(contour.Reverse().ToArray(), Colors.Blue);
            n.DrawSetTransform(new(80, 4)); n.DrawPolygon(quad, [Colors.Red, Colors.Green, Colors.Blue, Colors.White]);
            n.DrawSetTransform(new(4, 44)); n.DrawPolygon(quad, [], uvs, texture);
            n.DrawSetTransform(new(40, 44)); n.DrawPolygon(quad, [], uvs, atlas);
            n.DrawSetTransform(new(76, 44)); n.DrawPrimitive(quad, [], uvs, atlas);
            n.DrawSetTransform(new(112.5f, 8.5f), scale: new(2, 2));
            n.DrawPrimitive([Vector2.Zero], [Colors.Yellow], []);
            n.DrawPrimitive([new(2, 0), new(10, 0)], [Colors.Cyan], []);
            n.DrawSetTransform(new(112, 44)); n.DrawPrimitive([Vector2.Zero, new(24, 0), new(0, 24)], [Colors.Magenta], []);
            n.DrawSetTransform(new(4, 80)); n.DrawPolygon(quad, [], [new(0.25f, 0.25f), new(1.25f, 0.25f), new(1.25f, 1.25f), new(0.25f, 1.25f)], texture);
        }
        };
        window.AddChild(node);
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var frame = server.Readback(); frames++;
                var factor = frames == 5 ? 0.5f : 1;
                Color Scaled(Color color) => new(color.R * factor, color.G * factor, color.B * factor, 1);
                Pixel(frame, 6, 6, Scaled(Colors.Red)); Pixel(frame, 6, 28, Scaled(Colors.Red)); Pixel(frame, 26, 6, Scaled(Colors.Red)); Pixel(frame, 20, 20, Colors.Black);
                Pixel(frame, 42, 6, Scaled(Colors.Blue)); Pixel(frame, 42, 28, Scaled(Colors.Blue)); Pixel(frame, 56, 20, Colors.Black);
                var topLeft = frame.GetPixel(82, 6); var topRight = frame.GetPixel(101, 6);
                Check(topLeft.R > 0.7f * factor && topRight.G > 0.7f * factor, "Polygon vertex colors interpolate through native rasterization.");
                var updated = frames >= 4;
                Pixel(frame, 6, 46, Scaled(updated ? Colors.Cyan : Colors.Red));
                Pixel(frame, 24, 46, Scaled(updated ? Colors.Cyan : Colors.Green));
                Pixel(frame, 6, 64, Scaled(updated ? Colors.Cyan : Colors.Blue));
                Pixel(frame, 24, 64, Scaled(updated ? Colors.Cyan : Colors.Yellow));
                Pixel(frame, 26, 82, Scaled(updated ? Colors.Cyan : Colors.Green));
                Pixel(frame, 26, 101, Scaled(updated ? Colors.Cyan : Colors.Yellow));
                Pixel(frame, 42, 46, Scaled(updated ? Colors.Cyan : frames <= 2 ? Colors.Green : Colors.Blue));
                Pixel(frame, 78, 46, Scaled(updated ? Colors.Cyan : Colors.Red));
                Pixel(frame, 96, 64, Scaled(updated ? Colors.Cyan : Colors.Yellow));
                Pixel(frame, 112, 8, Scaled(Colors.Yellow)); Pixel(frame, 112, 9, Colors.Black);
                Pixel(frame, 122, 8, Scaled(Colors.Cyan)); Pixel(frame, 122, 9, Colors.Black);
                Pixel(frame, 115, 47, Scaled(Colors.Magenta)); Pixel(frame, 132, 64, Colors.Black);
                if (frames == 1) atlas.Region = new(0, 1, 1, 1);
                if (frames == 2) node.QueueRedraw();
                if (frames == 3) { image.Fill(Colors.Cyan); texture.Update(image); }
                if (frames == 4) node.Modulate = new(0.5f, 0.5f, 0.5f, 1);
                if (frames == 5) { Check(draws == 2, "Atlas metadata needs redraw; pixels and modulation reuse commands."); window.Tree!.Quit(); }
            };
        };
        Engine.Run(window); Released(window); Check(frames == 5, "Five polygon frames completed.");
        Console.WriteLine($"Canvas polygon native pixels passed: {backend}/{fixture ?? "default"}.");
    }
}
