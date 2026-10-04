using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;

internal static partial class RenderingRuntimeTests
{
    // Independent C FreeType 2.13.3 LIGHT/NO_BITMAP raster of the pinned Open Sans SemiBold:
    // FT_Load_Char/FT_Render_Glyph; outline uses FT_Glyph_Stroke with radius64, butt caps, round joins.
    private static readonly byte[] FontOracleA16 = Convert.FromBase64String("AAAAA+n/gAAAAAAAAABI//jbAAAAAAAAAKP9j/82AAAAAAAK88cu/5AAAAAAAFj/dADZ6AMAAAAAs/8gAIT/RgAAABP6ygAAL/+gAAAAaP////////IJAADD/6uoqKjR/1YAHv7PAAAAAED/sAB4/3oAAAAAAuj5EdP/JgAAAAAAlv9m");
    private static readonly byte[] FontOracleA16Quarter = Convert.FromBase64String("AAAAAK7/wAAAAAAAAAAP+Pj+HQAAAAAAAGP/jP92AAAAAAAAvvkQ6tAAAAAAABr9tACa/ysAAAAAc/9fAET/hgAAAADO+hAAA+vfAQAAKP////////87AACD/8GoqKi7/5YAAN37FAAAAAr16wU4/7oAAAAAAKv/S5P/ZgAAAAAAVv+m");
    private static readonly byte[] FontOracleA32 = Convert.FromBase64String("AAAAAAAAAACw////1gAAAAAAAAAAAAAAAAAAAAAT+v////81AAAAAAAAAAAAAAAAAAAAbf//////kwAAAAAAAAAAAAAAAAAAAMv//7///+wFAAAAAAAAAAAAAAAAACr///8y9v//UAAAAAAAAAAAAAAAAACJ///bAK7//68AAAAAAAAAAAAAAAAC5P//jQBa///5EwAAAAAAAAAAAAAARv///zYADfb//2wAAAAAAAAAAAAAAKT//9oAAACo///KAAAAAAAAAAAAAA31//9+AAAATP///ykAAAAAAAAAAABi////IwAAAAXr//+HAAAAAAAAAAAAwP//xgAAAAAAlf//4wIAAAAAAAAAH/7//2oAAAAAADr///9EAAAAAAAAAH3/////////////////ogAAAAAAAADb//////////////////QMAAAAAAA7////////////////////XwAAAAAAmf///lVISEhISEhISvb//70AAAAAB+///8cAAAAAAAAAAACs///+HgAAAFb///9yAAAAAAAAAAAAV////3oAAAC1////HgAAAAAAAAAAAAv2///YAAAX+///yAAAAAAAAAAAAAAArf///zcAcv///3MAAAAAAAAAAAAAAFj///+WANH///8fAAAAAAAAAAAAAAAM9v//7QY=");
    private static readonly byte[] FontOracleA16Outline = Convert.FromBase64String("AAAAAACB/f/rJgAAAAAAAAAACfL///+OAAAAAAAAAABW/////+YCAAAAAAAAALH//////0QAAAAAAAAR+f//////ngAAAAAAAGb////////xCAAAAAAAwf///7v///9UAAAAAB3+/////////64AAAAAdv/s////////+BAAAADR////////////ZAAAK////+qoqLn///++AACG////iAAAB/H///0bANr///80AAAApf///20Aqv//uwAAAAA68v/0SQ==");

    private static void VerifyFonts(string backend)
    {
        using var arabic = new FontFile { Data = FontTestFixtures.Arabic };
        using var hebrew = new FontFile { Data = FontTestFixtures.Hebrew };
        using var cjk = new FontFile { Data = FontTestFixtures.CJK };
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, SubpixelPositioning = FontSubpixelPositioning.Disabled, Fallbacks = [arabic, hebrew, cjk] };
        using var fallbackFont = new FontFile { Fallbacks = [arabic, hebrew, cjk] };
        using var quarter = new FontFile { Data = FontTestFixtures.OpenSans, SubpixelPositioning = FontSubpixelPositioning.Quarter };
        var window = new Window { Size = new(512, 256) }; var frames = 0;
        var node = new CanvasNode
        {
            Name = "Text",
            TextureFilter = TextureFilter.Nearest,
            DrawAction = canvas =>
            {
                canvas.DrawChar(font, new(10, 24), "A");
                canvas.DrawChar(quarter, new(30.25f, 24), "A");
                canvas.DrawCharOutline(font, new(50, 24), "A", size: 4, modulate: Colors.Green);
                canvas.DrawString(font, new(90, 24), "ffi AV");
                canvas.DrawStringOutline(font, new(170, 24), "ffi AV", size: 1, modulate: Colors.Blue);
                canvas.DrawMultilineString(font, new(260, 24), "A\nV", modulate: Colors.Yellow);
                canvas.DrawMultilineStringOutline(font, new(330, 24), "A\nV", size: 1, modulate: Colors.Cyan);
                canvas.DrawString(font, new(450, 24), "\U0010FFFD", modulate: Colors.Magenta);
                canvas.DrawChar(font, new(10.25f, 76.375f), "A", oversampling: 2);
                canvas.DrawString(fallbackFont, new(10, 140), "سلام", fontSize: 20);
                canvas.DrawString(fallbackFont, new(90, 140), "שלום", fontSize: 20);
                canvas.DrawString(fallbackFont, new(180, 140), "漢字", fontSize: 20);
                canvas.DrawString(arabic, new(10, 180), "سلام", fontSize: 20);
                canvas.DrawString(hebrew, new(90, 180), "שלום", fontSize: 20);
                canvas.DrawString(cjk, new(180, 180), "漢字", fontSize: 20);
                canvas.DrawString(font, new(10, 220), "Latin ffi שלום العربية 漢字", fontSize: 24);
            }
        }; window.AddChild(node);
        var clip = new Control { Position = new(12, 90), Size = new(7, 9), ClipContents = true }; window.AddChild(clip);
        var clipped = new CanvasNode { TextureFilter = TextureFilter.Nearest, DrawAction = canvas => canvas.DrawChar(font, new(0, 12), "A") }; clip.AddChild(clipped);
        var transformed = new CanvasNode
        {
            Name = "Transformed",
            Position = new(190, 85),
            Scale = new(2, 2),
            Modulate = new(.2f, 1, .6f, .5f),
            TextureFilter = TextureFilter.Nearest,
            DrawAction = canvas => canvas.DrawChar(font, new(0, 12), "A")
        }; window.AddChild(transformed);
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                if (frames == 1) File.WriteAllBytes($"/tmp/electron2d-fonts-{backend}.png", pixels.SavePNGToBuffer());
                var offset = frames == 1 ? Vector2.Zero : new Vector2(1, 2);
                var tint = frames == 1 ? Colors.White : new Color(1, .5f, .25f, 1);
                FontRasterPixels(pixels, FontOracleA16, 11, 12, new Vector2(10, 12) + offset, 1, tint);
                FontRasterPixels(pixels, FontOracleA16Quarter, 11, 12, new Vector2(30, 12) + offset, 1, tint);
                FontRasterPixels(pixels, FontOracleA16Outline, 14, 14, new Vector2(48, 11) + offset, 1, Colors.Green * tint);
                FontRasterPixels(pixels, FontOracleA32, 22, 23, new Vector2(10.25f, 64.875f) + offset, 2, tint);
                FontRasterPixels(pixels, FontOracleA16, 11, 12, new(12, 90), 1, Colors.White, new Rect2i(12, 90, 7, 9));
                FontRasterPixels(pixels, FontOracleA16, 11, 12, new(190, 85), .5f, new(.2f, 1, .6f, .5f));
                var dx = (int)offset.X; var dy = (int)offset.Y;
                Check(FontInk(pixels, new(90 + dx, 10 + dy, 60, 18)) > 80, "The shaped string records a visible ligature and kerning pair.");
                Check(FontInk(pixels, new(168 + dx, 9 + dy, 62, 20)) > 80, "The shaped string outline draws visible expanded glyphs.");
                Check(FontInk(pixels, new(260 + dx, 11 + dy, 20, 15)) > 20 && FontInk(pixels, new(260 + dx, 34 + dy, 20, 15)) > 20,
                    "Multiline fill preserves separate baselines.");
                Check(FontInk(pixels, new(328 + dx, 10 + dy, 24, 17)) > 20 && FontInk(pixels, new(328 + dx, 33 + dy, 24, 17)) > 20,
                    "Multiline outline preserves separate baselines.");
                Pixel(pixels, 450 + dx, 12 + dy, Colors.Magenta * tint); Pixel(pixels, 465 + dx, 12 + dy, Colors.Black);
                FontEqualCells(pixels, new(8 + dx, 115 + dy, 72, 33), new(8 + dx, 155 + dy));
                FontEqualCells(pixels, new(88 + dx, 115 + dy, 72, 33), new(88 + dx, 155 + dy));
                FontEqualCells(pixels, new(178 + dx, 115 + dy, 60, 33), new(178 + dx, 155 + dy));
                Check(FontInk(pixels, new(10 + dx, 194 + dy, 380, 29)) > 400, "The mixed Latin, Hebrew, Arabic and CJK line reaches the real canvas.");
                if (frames == 1) { node.Position = new(1, 2); node.SelfModulate = new(1, .5f, .25f, 1); node.QueueRedraw(); }
                else window.Tree!.Quit();
            };
        };
        Engine.Run(window); Released(window);
        Check(!font.IsDisposed && !arabic.IsDisposed && !hebrew.IsDisposed && !cjk.IsDisposed, "Closing text consumers preserves their borrowed font graph.");
        VerifyFontWarm(backend, font, quarter);
        VerifyFontRetirement(backend);
        Console.WriteLine($"Native font six canvas entrypoints, independent raster pixels, fractional phase/oversampling, fallback scripts, clipping, transform, modulation and warm frames passed: {backend}.");
    }

    private static void FontRasterPixels(Image pixels, byte[] alpha, int width, int height, Vector2 origin, float sampleScale, Color color, Rect2i? clip = null)
    {
        var end = origin + new Vector2(width, height) / sampleScale;
        for (var y = (int)MathF.Floor(origin.Y) - 1; y <= (int)MathF.Ceiling(end.Y); y++)
            for (var x = (int)MathF.Floor(origin.X) - 1; x <= (int)MathF.Ceiling(end.X); x++)
            {
                var point = new Vector2(x + .5f, y + .5f); var source = (point - origin) * sampleScale;
                var visible = source.X >= 0 && source.Y >= 0 && source.X < width && source.Y < height &&
                    (clip is null || x >= clip.Value.Position.X && y >= clip.Value.Position.Y && x < clip.Value.End.X && y < clip.Value.End.Y);
                var amount = visible ? alpha[(int)source.Y * width + (int)source.X] / 255f * color.A : 0;
                Pixel(pixels, x, y, new(color.R * amount, color.G * amount, color.B * amount, 1));
            }
    }

    private static int FontInk(Image pixels, Rect2i region)
    {
        var count = 0;
        for (var y = region.Position.Y; y < region.End.Y; y++) for (var x = region.Position.X; x < region.End.X; x++)
            { var value = pixels.GetPixel(x, y); if (value.R > .02f || value.G > .02f || value.B > .02f) count++; }
        return count;
    }
    private static void FontEqualCells(Image pixels, Rect2i source, Vector2i target)
    {
        Check(FontInk(pixels, source) > 30, "Each fallback script produces real glyph pixels.");
        for (var y = 0; y < source.Size.Y; y++) for (var x = 0; x < source.Size.X; x++)
                Pixel(pixels, target.X + x, target.Y + y, pixels.GetPixel(source.Position.X + x, source.Position.Y + y));
    }

    private static void VerifyFontWarm(string backend, Font font, Font quarter)
    {
        var window = new Window { Size = new(360, 160) }; var frames = 0; long before = 0, allocated = 0;
        var node = new CanvasNode
        {
            TextureFilter = TextureFilter.Nearest,
            DrawAction = canvas =>
            {
                var even = (frames & 1) == 0;
                canvas.DrawChar(quarter, new(even ? 10.25f : 10.5f, 24), "A", oversampling: even ? 1 : 2);
                canvas.DrawCharOutline(font, new(50, 24), "A", size: even ? 1 : 2);
                canvas.DrawString(font, new(90, 24), even ? "ffi AV" : "AV ffi");
                canvas.DrawStringOutline(font, new(190, 24), "ffi AV", size: even ? 1 : 2);
                canvas.DrawMultilineString(font, new(10, 65), even ? "שלום\nسلام" : "سلام\nשלום", width: 110);
                canvas.DrawMultilineStringOutline(font, new(150, 65), "漢字\nffi", width: 110, size: 1);
                canvas.DrawString(font, new(10, 140), even ? "Latin שלום العربية 漢字" : "Latin العربية שלום 漢字");
            }
        }; window.AddChild(node);
        window.Ready += _ =>
        {
            var tree = window.Tree!;
            tree.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread(); node.Position = new(frames & 1, 0);
                node.SelfModulate = (frames & 1) == 0 ? Colors.White : new(1, .75f, .5f, 1); node.QueueRedraw();
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frames >= 64) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames == 128) tree.Quit();
            };
        };
        Engine.Run(window); Released(window);
        Check(frames == 128 && node.Draws == 128 && allocated == 0,
            $"Warmed {backend} active text/phase/outline/transform/modulation mutation and record/render allocated {allocated} bytes in64 ProcessFrameStarted-to-FramePostDraw frames; recordings={node.Draws}.");
    }

    private static void VerifyFontRetirement(string backend)
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, SubpixelPositioning = FontSubpixelPositioning.Disabled };
        var window = new Window { Size = new(64, 64) }; var draw = true; var frame = 0;
        var node = new CanvasNode { TextureFilter = TextureFilter.Nearest, DrawAction = canvas => { if (draw) canvas.DrawChar(font, new(10, 24), "A"); } }; window.AddChild(node);
        IDictionary? cache = null; Texture? glyph = null; SafeHandle[] handles = [];
        window.Ready += _ =>
        {
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            // Backend-only test inspection verifies native SafeHandle release without exposing diagnostics publicly.
            var renderer = typeof(RenderingServer).GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(server)!;
            cache = (IDictionary)renderer.GetType().GetField("_textures", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(renderer)!;
            RenderingServer.FramePostDraw += () =>
            {
                frame++;
                if (frame == 1)
                {
                    Check(cache.Count == 1, "One glyph creates one resident renderer texture.");
                    foreach (DictionaryEntry entry in cache)
                    {
                        glyph = (Texture)entry.Key; var payload = entry.Value!;
                        handles = backend == "gpu"
                            ? [(SafeHandle)payload.GetType().GetField("_texture", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(payload)!,
                               (SafeHandle)payload.GetType().GetField("_transfer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(payload)!]
                            : [(SafeHandle)payload.GetType().GetField("Item1")!.GetValue(payload)!];
                    }
                    draw = false; node.QueueRedraw();
                }
                else if (frame == 2)
                {
                    Check(cache.Contains(glyph!) && handles.All(handle => !handle.IsClosed), "An unused live glyph keeps its renderer payload for later frames.");
                    draw = true; node.QueueRedraw();
                }
                else if (frame == 3)
                {
                    font.Dispose();
                    Check(!glyph!.IsDisposed && !glyph.RetainRendererCache, "Font disposal retires residency while preserving immutable recorded glyph pixels.");
                }
                else if (frame == 4)
                {
                    using var pixels = server.Readback();
                    FontRasterPixels(pixels, FontOracleA16, 11, 12, new(10, 12), 1, Colors.White);
                    Check(node.Draws == 3 && cache.Contains(glyph!) && handles.All(handle => !handle.IsClosed),
                        "Previously recorded commands draw the same glyph after the native font is disposed, without invoking the disposed font again.");
                    draw = false; node.QueueRedraw();
                }
                else
                {
                    Check(!cache.Contains(glyph!) && handles.All(handle => handle.IsClosed) && ReferenceEquals(RenderingServer.Service, server) && !window.IsDisposed,
                        "The next frame releases retired glyph native handles before renderer shutdown.");
                    window.Tree!.Quit();
                }
            };
        };
        Engine.Run(window); Released(window); glyph?.Dispose();
        Check(frame == 5, "Glyph retirement and immutable command snapshots execute while the native renderer remains alive.");
    }
}
