using Electron2D;

internal static partial class FontAntialiasingTests
{
    private sealed class Painter(Font font, Font variation, Color color) : Entity
    {
        protected override void OnDraw()
        {
            DrawChar(font, new(20, 40), "A", 24, color);
            font.DrawChar(this, new(80, 40), 'A', 24, color);
            DrawString(variation, new(140, 40), "A", fontSize: 24, modulate: color);
            DrawCharOutline(font, new(310, 40), "A", 24, 1, color);
            var data = font.PrimaryData!; var glyph = data.GetGlyph(data.GetGlyphIndex('A'), 24, 0, 1);
            TextLayout.DrawGlyph(this, glyph, new(260, 40), color, clipRect: new(260, 0, 8, 80));
        }
    }
    internal static void RunNative(string profile)
    {
        var layout = (FontLCDSubpixelLayout)int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_FONT_AA_LAYOUT") ?? "1");
        ProjectSettings.Set(ProjectSettings.LCDSubpixelLayout, layout);
        var backend = profile.StartsWith("compatibility", StringComparison.Ordinal) ? "compatibility" : "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (profile.Contains("msaa", StringComparison.Ordinal)) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, SubpixelPositioning = FontSubpixelPositioning.Disabled, AllowSystemFallback = false };
        using var variation = new FontVariation { BaseFont = font };
        var tint = new Color(.25f, .65f, .8f, .5f); var inherited = new Color(.8f, .6f, .9f, .7f); var clear = new Color(.7f, .2f, .4f, 1);
        var window = new Window { Size = new(360, 100), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var parent = new Entity { Modulate = inherited }; window.AddChild(parent); var painter = new Painter(font, variation, tint); parent.AddChild(painter);
        using var settings = new LabelSettings { Font = font, FontSize = 24, FontColor = tint, OutlineSize = 0, ShadowSize = 0 };
        var label = new Label { Text = "A", Position = new(200, 10), Size = new(40, 60), LabelSettings = settings }; parent.AddChild(label);
        using var target = window.GetTexture(); var phase = 0; var warm = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(clear);
            RenderingServer.FramePreDraw += () => { if (phase < 3) return; before = GC.GetAllocatedBytesForCurrentThread(); if (warm < 96) painter.QueueRedraw(); };
            RenderingServer.FramePostDraw += () =>
            {
                if (phase == 3)
                {
                    if (warm >= 32 && warm < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                    else if (warm >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++warm == 160) { Check(active == 0 && idle == 0, $"Font AA 64 active/64 idle: {active}/{idle} bytes."); window.Tree!.Quit(); }
                    return;
                }
                using var pixels = target.GetImage()!; var glyph = Glyph(font); var color = tint * inherited;
                foreach (var baseline in new[] { new Vector2(20, 40), new(80, 40), new(140, 40), new(200, 10 + MathF.Floor(font.GetAscent(24))), new(260, 40) })
                    CompareGlyph(pixels, glyph, baseline, color, clear, clipped: baseline.X == 260);
                CompareGlyph(pixels, Glyph(font, outline: 1), new(310, 40), color, clear, clipped: false);
                if (++phase == 1) font.Antialiasing = FontAntialiasing.None;
                if (phase == 2)
                {
                    if (backend == "compatibility" && layout != FontLCDSubpixelLayout.None) { window.Tree!.Quit(); return; }
                    font.Antialiasing = FontAntialiasing.LCD;
                }
                painter.QueueRedraw();
            };
        };
        Check(Engine.Run(window) == 0 && (phase == 3 && warm == 160 || backend == "compatibility" && phase == 2), "Native coverage host completes.");
        if (backend == "compatibility" && layout != FontLCDSubpixelLayout.None)
        {
            var rejected = new Window { Size = new(160, 96) }; font.Antialiasing = FontAntialiasing.LCD; rejected.AddChild(new Painter(font, variation, tint));
            Reject<NotSupportedException>(() => Engine.Run(rejected)); Check(rejected.IsDisposed && !RenderingServer.IsAvailable, "Unsupported LCD font submission cleans up.");
        }
        Console.WriteLine($"Font AA backend={backend}, layout={layout}: native pixels, scalar/String/variation/Label/clip consumers; warm bytes={active}/{idle}.");
    }
    private static void CompareGlyph(Image pixels, FontGlyph glyph, Vector2 baseline, Color color, Color clear, bool clipped)
    {
        using var texture = glyph.Texture!.GetImage()!; var region = glyph.Region ?? new(0, 0, texture.Width, texture.Height);
        var origin = (baseline + glyph.Offset).Floor(); var compared = 0; var componentPixels = 0;
        for (var y = 0; y < (int)glyph.Size.Y; y++) for (var x = 0; x < (int)glyph.Size.X; x++)
            {
                var sample = texture.GetPixel((int)region.Position.X + x, (int)region.Position.Y + y); Color expected;
                if (clipped && (origin.X + x < 260 || origin.X + x >= 268)) expected = clear;
                else if (glyph.LCD)
                {
                    if (sample.R != sample.G || sample.G != sample.B) componentPixels++;
                    expected = new(sample.R * color.A * color.R + clear.R * (1 - sample.R * color.A), sample.G * color.A * color.G + clear.G * (1 - sample.G * color.A), sample.B * color.A * color.B + clear.B * (1 - sample.B * color.A), 1);
                }
                else
                {
                    var alpha = sample.A * color.A;
                    expected = new(sample.R * color.R * alpha + clear.R * (1 - alpha), sample.G * color.G * alpha + clear.G * (1 - alpha), sample.B * color.B * alpha + clear.B * (1 - alpha), 1);
                }
                var actual = pixels.GetPixel((int)origin.X + x, (int)origin.Y + y);
                Check(MathF.Abs(actual.R - expected.R) < .015f && MathF.Abs(actual.G - expected.G) < .015f && MathF.Abs(actual.B - expected.B) < .015f, $"Font pixel {origin + new Vector2(x, y)}: {actual} vs {expected}, LCD={glyph.LCD}."); compared++;
            }
        Check(compared > 0 && (!glyph.LCD || clipped || componentPixels > 0), "The consumer exercises actual per-component glyph coverage.");
    }
}
