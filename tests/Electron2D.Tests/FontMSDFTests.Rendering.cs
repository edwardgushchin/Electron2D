using Electron2D;

internal static partial class FontMSDFTests
{
    private sealed class MSDFPainter(FontFile font, Font variation, Color color) : Entity
    {
        protected override void OnDraw()
        {
            var raw = Glyph(font, font.MSDFSize); var factor = 24f / font.MSDFSize;
            for (var i = 0; i < 4; i++)
            {
                var at = new Vector2(20.375f, 60.25f + i * 60);
                if (i == 0) DrawChar(font, at, "A", 24, color);
                else if (i == 1) font.DrawChar(this, at, 'A', 24, color);
                else if (i == 2) DrawString(variation, at, "A", fontSize: 24, modulate: color);
                else DrawCharOutline(font, at, "A", 24, 2, color);
                Direct(raw, at + new Vector2(160, 0), factor, i == 3 ? 2 : 0);
            }
            var glyph = Glyph(font); var baseline = new Vector2(20.375f, 300.25f); var clip = new Rect2(20, 260, 8, 70);
            TextLayout.DrawGlyph(this, glyph, baseline, color, clipRect: clip);
            var rect = new Rect2(baseline + raw.Offset * factor, raw.Size * factor); var clipped = rect.Intersection(clip);
            var region = raw.Region ?? new Rect2(Vector2.Zero, raw.Texture!.GetSize()); var scale = region.Size / rect.Size;
            DrawMSDFTextureRectRegion(raw.Texture!, clipped with { Position = clipped.Position + new Vector2(160, 0) }, new(region.Position + (clipped.Position - rect.Position) * scale, clipped.Size * scale), color, pixelRange: font.MSDFPixelRange, scale: factor);
            Direct(raw, new(180, 340 + font.GetAscent(24)), factor, 0);
            void Direct(FontGlyph source, Vector2 position, float sourceScale, int outline) =>
                DrawMSDFTextureRectRegion(source.Texture!, new(position + source.Offset * sourceScale, source.Size * sourceScale), source.Region ?? new Rect2(Vector2.Zero, source.Texture!.GetSize()), color, outline, font.MSDFPixelRange, sourceScale);
        }
    }
    internal static void RunNative(string profile)
    {
        var backend = profile == "compatibility" ? "compatibility" : "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (profile == "gpu-msaa") ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, MultichannelSignedDistanceField = true, MSDFSize = 16, MSDFPixelRange = 8, AllowSystemFallback = false };
        using var variation = new FontVariation { BaseFont = font };
        var tint = new Color(.3f, .8f, .5f, .75f); var inherited = new Color(.7f, .6f, .9f, .7f);
        var window = new Window { Size = new(400, 410) }; var parent = new Entity { Modulate = inherited }; window.AddChild(parent);
        var painter = new MSDFPainter(font, variation, tint); parent.AddChild(painter);
        using var settings = new LabelSettings { Font = font, FontSize = 24, FontColor = tint, OutlineSize = 0, ShadowSize = 0 };
        parent.AddChild(new Label { Text = "A", Position = new(20, 340), Size = new(50, 60), LabelSettings = settings });
        using var target = window.GetTexture(); var phase = 0; var warm = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(new(.1f, .2f, .3f, 1));
            RenderingServer.FramePreDraw += () => { if (phase < 3) return; before = GC.GetAllocatedBytesForCurrentThread(); if (warm < 96) painter.QueueRedraw(); };
            RenderingServer.FramePostDraw += () =>
            {
                if (phase == 3)
                {
                    if (warm >= 32 && warm < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                    else if (warm >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (++warm == 160) { Check(active == 0 && idle == 0, $"MSDF warm managed bytes {active}/{idle}."); window.Tree!.Quit(); }
                    return;
                }
                using var pixels = target.GetImage()!; var visible = 0;
                for (var y = 0; y < 409; y++) for (var x = 0; x < 135; x++)
                    {
                        var actual = pixels.GetPixel(x, y); var expected = pixels.GetPixel(x + 160, y);
                        Check(MathF.Abs(actual.R - expected.R) < .012f && MathF.Abs(actual.G - expected.G) < .012f && MathF.Abs(actual.B - expected.B) < .012f && MathF.Abs(actual.A - expected.A) < .012f, $"MSDF font/direct pixels ({x},{y}), source size {font.MSDFSize}: {actual}/{expected}.");
                        if (MathF.Abs(actual.R - .1f) > .05f) visible++;
                    }
                Check(visible > 50, "Source mode produces visible native glyph coverage.");
                if (++phase == 1) font.MSDFSize = 48;
                if (phase == 2) font.MSDFSize = 96;
                painter.QueueRedraw();
            };
        };
        if (backend == "compatibility")
        {
            Reject<NotSupportedException>(() => Engine.Run(window)); Check(window.IsDisposed && !RenderingServer.IsAvailable, "Unsupported MSDF host cleans up.");
        }
        else Check(Engine.Run(window) == 0 && phase == 3 && warm == 160, "Native MSDF output completes every phase.");
        Console.WriteLine($"MSDF backend={backend}: scalar/string/variation/outline/clip/Label compare to explicit source sampling; 64 active/64 idle managed bytes {active}/{idle}.");
    }
}
