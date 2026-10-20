using Electron2D;

internal static class TextServerGlyphRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        using var latin = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        using var fallback = new FontFile { Data = FontTestFixtures.Arabic, AllowSystemFallback = false };
        latin.Fallbacks = [fallback]; using var font = new FontVariation { BaseFont = latin, BaselineOffset = .2f };
        var s = TextServerManager.GetPrimaryInterface(); var buffer = s.CreateShapedText();
        s.ShapedTextAddString(buffer, "ffi A\u0308 سلام", [font], 28); var glyphs = s.ShapedTextGetGlyphs(buffer); var source = font.GetRID();
        using var window = new Window { Size = new(420, 180) }; var canvas = new Entity(); window.AddChild(canvas); using var target = window.GetTexture();
        var frame = 0; var draws = 0; long before = 0, active = 0, idle = 0;
        canvas.Draw += item =>
        {
            draws++; s.ShapedTextDraw(buffer, item, new(20, 60)); var pen = new Vector2(20, 130);
            foreach (var glyph in glyphs)
                for (var repeat = 0; repeat < glyph.Repeat; repeat++)
                { s.FontDrawGlyph(glyph.FontRID, item, glyph.FontSize, pen + glyph.Offset, glyph.Index); pen.X += glyph.Advance; }
        };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frame == 25)
                {
                    s.FontSetSpacing(source, TextSpacingType.Glyph, 3); s.FontSetEmbolden(source, .6f);
                    glyphs = s.ShapedTextGetGlyphs(buffer);
                    Check(font.GetRID() == source && glyphs.Any(g => g.FontRID == source) && glyphs.Any(g => g.FontRID == fallback.GetRID()), "Source cache mutation preserves actual resource/fallback identities.");
                }
                if (frame < 135) canvas.QueueRedraw();
                s.ShapedGetRunCount(buffer); s.ShapedGetRunFontRID(buffer, 0); s.ShapedGetRunGlyphRange(buffer, 0);
            };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            if (frame is >= 71 and < 135) active += delta;
            if (frame is >= 135 and < 199) idle += delta;
            frame++;
            if (frame is 2 or 50)
            {
                using var image = target.GetImage() ?? throw new InvalidOperationException("Glyph snapshot readback."); var pixels = 0;
                for (var y = 15; y < 80; y++) for (var x = 10; x < 400; x++)
                    { var original = image.GetPixel(x, y); var custom = image.GetPixel(x, y + 70); Check(original == custom, "Source RID/index/offset/advance reconstruct identical glyph pixels."); if (original.R > .5f) pixels++; }
                Check(pixels > 100, "Native ligature/mark/bidi/variation draw has real output.");
            }
            if (frame == 199)
            {
                Check(draws >= 130 && active == 0 && idle == 0, $"Prepared custom glyph draws/queries: {draws} recordings, {active}/{idle} owner bytes.");
                RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
        }
        try { Engine.Run(window); }
        finally { s.FreeRID(buffer); }
        Console.WriteLine($"{backend}: copied glyph source/offset/advance reproduces native ligature/combining/bidi/variation pixels before/after borrowed cache edits; 64 active + 64 idle frames=0/0 owner bytes.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer glyph rendering: " + message); }
}
