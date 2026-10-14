using Electron2D;

internal static partial class Type1FontTests
{
    private sealed class Painter(FontFile font, Font variation) : Entity
    {
        protected override void OnDraw()
        {
            var at = new Vector2(20, 50);
            DrawChar(font, at, "A", 24, Colors.White); Direct(font, at + new Vector2(160, 0), 0);
            DrawString(variation, new(20, 100), "A", fontSize: 24); Direct(font, new(180, 100), 0);
            DrawCharOutline(font, new(20, 150), "A", 24, 2, Colors.White); Direct(font, new(180, 150), 2);
        }
        private void Direct(Font font, Vector2 at, int outline)
        {
            var glyph = Glyph(font, 24, outline); var rect = new Rect2(at + glyph.Offset, glyph.Size); var region = glyph.Region ?? new Rect2(Vector2.Zero, glyph.Texture!.GetSize());
            if (glyph.MSDFPixelRange != 0) DrawMSDFTextureRectRegion(glyph.Texture!, rect, region, Colors.White, outline, glyph.MSDFPixelRange, glyph.MSDFScale);
            else if (glyph.LCD) DrawLCDTextureRectRegion(glyph.Texture!, rect, region);
            else DrawTextureRectRegion(glyph.Texture!, rect, region);
        }
    }
    internal static void RunNative(string profile)
    {
        var compatibility = profile.StartsWith("compatibility", StringComparison.Ordinal); var lcd = profile.EndsWith("lcd", StringComparison.Ordinal); var msdf = profile.EndsWith("msdf", StringComparison.Ordinal);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ProjectSettings.Set(ProjectSettings.LCDSubpixelLayout, FontLCDSubpixelLayout.HorizontalRGB);
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-type1-render-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            Write(directory, "pfb"); Write(directory, "pfm"); using var font = new FontFile(); font.LoadDynamicFont(System.IO.Path.Combine(directory, "type1.pfm"));
            font.AllowSystemFallback = false; font.Antialiasing = lcd ? FontAntialiasing.LCD : FontAntialiasing.Gray; font.MultichannelSignedDistanceField = msdf;
            using var variation = new FontVariation { BaseFont = font }; var window = new Window { Size = new(320, 230) }; var painter = new Painter(font, variation); window.AddChild(painter);
            using var settings = new LabelSettings { Font = font, FontSize = 24, FontColor = Colors.White, OutlineSize = 0, ShadowSize = 0 };
            window.AddChild(new Label { Text = "AV", Position = new(20, 180), Size = new(100, 50), LabelSettings = settings });
            using var target = window.GetTexture(); var frames = 0; long before = 0, active = 0, idle = 0;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePreDraw += () => { before = GC.GetAllocatedBytesForCurrentThread(); if (frames < 96) painter.QueueRedraw(); };
                RenderingServer.FramePostDraw += () =>
                {
                    if (frames >= 32 && frames < 96) active += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (frames >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                    if (frames++ != 160) return;
                    Check(active == 0 && idle == 0, $"Prepared Type 1 managed allocation {active}/{idle}."); using var image = target.GetImage()!; var visible = 0; var label = 0;
                    for (var y = 0; y < 230; y++) for (var x = 0; x < 130; x++)
                        {
                            var actual = image.GetPixel(x, y); if (y < 170)
                            { var expected = image.GetPixel(x + 160, y); Check(MathF.Abs(actual.R - expected.R) < .012f && MathF.Abs(actual.G - expected.G) < .012f && MathF.Abs(actual.B - expected.B) < .012f, "Native font/direct Type 1 pixels."); if (actual.R > .1f) visible++; }
                            else if (actual.R > .1f) label++;
                        }
                    Check(visible > 50 && label > 20, "Type 1 fill/variation/stroke/Label produce real pixels."); window.Tree!.Quit();
                };
            };
            if (compatibility && (lcd || msdf)) { Reject<NotSupportedException>(() => Engine.Run(window)); Check(window.IsDisposed && !RenderingServer.IsAvailable, "Unsupported encoded font cleans up host."); }
            else Check(Engine.Run(window) == 0 && frames == 161, "Type 1 native host completes.");
            Console.WriteLine($"Type 1 {profile}: real scalar/string/variation/stroke/Label output, 64 active/64 idle managed bytes {active}/{idle}.");
        }
        finally { Directory.Delete(directory, true); }
    }
}
