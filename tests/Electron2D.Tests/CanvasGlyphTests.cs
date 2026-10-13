using Electron2D;

internal static partial class CanvasGlyphTests
{
    private sealed class Painter(Texture lcd, Texture msdf) : Entity
    {
        protected override void OnDraw()
        {
            DrawLCDTextureRectRegion(lcd, new(8, 8, 64, 16), new(0, 0, 2, 1), new(.2f, .6f, .8f, .5f));
            DrawMSDFTextureRectRegion(msdf, new(8, 40, 128, 16), new(0, 0, 4, 1), new(.8f, .3f, .1f, .5f));
        }
    }
    internal static void RunManaged()
    {
        Reject<InvalidOperationException>(() => RenderingServer.CanvasItemAddLCDTextureRectRegion(default, default, default, default, Colors.White));
        Reject<ArgumentOutOfRangeException>(() => new CanvasGlyph(CanvasGlyphMode.MSDF, 0, 0, 1).Validate());
        Reject<ArgumentException>(() => new CanvasGlyph(CanvasGlyphMode.MSDF, float.MaxValue, 1, float.Epsilon).Validate());
        using var node = new Entity(); using var image = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        Reject<InvalidOperationException>(() => node.DrawLCDTextureRectRegion(texture, default, default));
        Reject<InvalidOperationException>(() => node.DrawMSDFTextureRectRegion(texture, default, default));
        Recording();
        Console.WriteLine("Glyph parameter, recording, service and scene-storage guards passed.");
    }
    internal static void RunNative(string profile)
    {
        if (profile.StartsWith("material-", StringComparison.Ordinal)) { RunMaterial(profile); return; }
        if (profile == "groups") { RunGroups(); return; }
        if (profile.Contains("msaa", StringComparison.Ordinal)) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        var backend = profile == "compatibility" ? "compatibility" : "gpu"; ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var lcdImage = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [64, 128, 191, 255, 128, 128, 128, 128]);
        using var msdfImage = Image.CreateFromData(4, 1, false, Image.Format.Rgba8, [64, 64, 191, 230, 191, 64, 191, 230, 128, 128, 128, 230, 64, 191, 64, 255]);
        using var lcd = ImageTexture.CreateFromImage(lcdImage); using var msdf = ImageTexture.CreateFromImage(msdfImage);
        var parentColor = new Color(.5f, .8f, .5f, .8f); var transparent = profile.Contains("transparent", StringComparison.Ordinal);
        var clear = transparent ? default : new Color(.6f, .2f, .4f, 1);
        var window = new Window { Size = new(240, 128), TransparentBG = transparent }; var parent = new Entity { Modulate = parentColor }; window.AddChild(parent);
        var painter = new Painter(lcd, msdf) { TextureFilter = TextureFilter.Nearest }; parent.AddChild(painter);
        using var target = window.GetTexture(); RID item = default; var frame = 0; var warm = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(clear); var canvas = window.FindWorld()!.Canvas; item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, canvas);
            RenderingServer.CanvasItemSetModulate(item, parentColor); RenderingServer.CanvasItemSetDefaultTextureFilter(item, TextureFilter.Nearest);
            RenderingServer.CanvasItemAddLCDTextureRectRegion(item, new(80, 8, 64, 16), lcd.GetRID(), new(0, 0, 2, 1), new(.2f, .6f, .8f, .5f));
            RenderingServer.CanvasItemAddMSDFTextureRectRegion(item, new(8, 64, 128, 16), msdf.GetRID(), new(0, 0, 4, 1), new(.8f, .3f, .1f, .5f), pixelRange: 4);
            Reject<ArgumentException>(() => RenderingServer.CanvasItemAddLCDTextureRectRegion(item, default, window.GetViewportRID(), default, Colors.White));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.CanvasItemAddMSDFTextureRectRegion(item, default, msdf.GetRID(), default, pixelRange: 0));
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.CanvasItemAddLCDTextureRectRegion(item, default, lcd.GetRID(), default, Colors.White))).GetAwaiter().GetResult();
            RenderingServer.FramePreDraw += () =>
            {
                if (frame < 4) return; before = GC.GetAllocatedBytesForCurrentThread();
                if (warm < 96) RenderingServer.CanvasItemSetModulate(item, (warm & 1) == 0 ? parentColor : Colors.White);
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame == 4)
                {
                    if (warm >= 32 && warm < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                    else if (warm >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++warm == 160)
                    {
                        Check(active == 0 && idle == 0, $"64 active/64 idle glyph intervals: {active}/{idle} bytes.");
                        RenderingServer.FreeRID(item); Reject<ArgumentException>(() => RenderingServer.CanvasItemAddLCDTextureRectRegion(item, default, lcd.GetRID(), default, Colors.White)); window.Tree!.Quit();
                    }
                    return;
                }
                using var pixels = target.GetImage()!;
                var tint = new Color(.2f, .6f, .8f, .5f) * parentColor; var coverage = new Color(64 / 255f, 128 / 255f, 191 / 255f, 1);
                var lcdExpected = new Color(coverage.R * tint.A * tint.R + clear.R * (1 - coverage.R * tint.A), coverage.G * tint.A * tint.G + clear.G * (1 - coverage.G * tint.A), coverage.B * tint.A * tint.B + clear.B * (1 - coverage.B * tint.A), tint.A + clear.A * (1 - tint.A));
                Pixel(pixels, 20, 12, lcdExpected); Pixel(pixels, 92, 12, lcdExpected); Pixel(pixels, 60, 12, clear); Pixel(pixels, 132, 12, clear);
                var color = new Color(.8f, .3f, .1f, .5f) * parentColor;
                Pixel(pixels, 20, 44, clear); Pixel(pixels, 52, 44, Blend(color, clear)); Pixel(pixels, 52, 68, Blend(color, clear));
                var alpha = Math.Clamp((128 / 255f - .5f) * 96 + .5f, 0, 1); Pixel(pixels, 84, 44, Blend(color with { A = color.A * alpha }, clear));
                ++frame;
            };
        };
        if (backend == "compatibility") { Reject<NotSupportedException>(() => Engine.Run(window)); Check(window.IsDisposed, "Compatibility capability cleanup."); return; }
        Check(Engine.Run(window) == 0 && frame == 4 && warm == 160 && painter.ServerState is null, "Native glyph host and session cleanup complete.");
        Console.WriteLine($"Scene/server LCD/MSDF pixels and 64 active/64 idle intervals passed: {active}/{idle} bytes.");
    }
    private static Color Blend(Color source, Color dest) => new(source.R * source.A + dest.R * (1 - source.A), source.G * source.A + dest.G * (1 - source.A), source.B * source.A + dest.B * (1 - source.A), source.A + dest.A * (1 - source.A));
    private static void Pixel(Image image, int x, int y, Color expected) { var got = image.GetPixel(x, y); Check(MathF.Abs(got.R - expected.R) < .01f && MathF.Abs(got.G - expected.G) < .01f && MathF.Abs(got.B - expected.B) < .01f && MathF.Abs(got.A - expected.A) < .01f, $"Pixel {x},{y}: {got}, expected {expected}."); }
    private static void Reject<T>(Action call) where T : Exception { try { call(); } catch (T) { return; } throw new Exception($"Expected {typeof(T).Name}."); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
