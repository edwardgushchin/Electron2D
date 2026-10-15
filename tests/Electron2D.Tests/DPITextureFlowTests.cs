using Electron2D;
using Path = System.IO.Path;

internal static class DPITextureFlowTests
{
    private sealed class InputObserver : Node
    {
        internal int Count;
        protected override void OnInput(InputEvent input) => Count++;
    }
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var dpi = DPITexture.CreateFromString(DPITextureTests.Source, colorMap: new() { [Colors.Red] = Colors.Lime, [Colors.Blue] = Colors.Yellow });
        using var hold = new SubViewport { OversamplingOverride = 2 };
        using var window = new Window { Size = new(240, 96), OversamplingOverride = 2 };
        var canvas = new Entity(); window.AddChild(canvas); var draws = 0; RID current = default, scale2 = default, scale3 = default, quantized = default;
        canvas.Draw += item => { draws++; current = dpi.GetScaledRID(); item.DrawTextureRect(dpi, new(10, 10, 32, 16), false); item.DrawString(ThemeDB.FallbackFont!, new(90.25f, 42.25f), "S", fontSize: 24); };
        var input = new InputObserver { InputEnabled = true }; window.AddChild(input); var transitions = 0;
        window.FocusEntered += () => transitions++; window.FocusExited += () => transitions++; window.MouseEntered += () => transitions++; window.MouseExited += () => transitions++; window.SizeChanged += () => transitions++;
        var samples = 0; var previousTransitions = -1;
        using var target = window.GetTexture(); var frame = 0; long begin = 0, allocated = -1; var expected = Colors.Lime;
        ulong scaledGlyph = 0;
        static ulong GlyphHash(Image image)
        {
            ulong hash = 14695981039346656037UL; var lit = 0;
            for (var y = 10; y < 48; y++) for (var x = 88; x < 120; x++)
                {
                    var pixel = image.GetPixel(x, y); if (pixel.R > .1f) lit++;
                    hash = unchecked((hash ^ pixel.ToRGBA32()) * 1099511628211UL);
                }
            DPITextureTests.Check(lit > 20, "The viewport rendered actual font glyph pixels."); return hash;
        }
        static void Retired(RID rid)
        {
            try { using var image = RenderingServer.Texture2DGet(rid); if (image is null) return; }
            catch (ArgumentException) { return; }
            throw new InvalidOperationException("A retired raster identity is still live.");
        }
        void Capture()
        {
            frame++;
            if (frame <= 5)
            {
                using var image = target.GetImage()!; DPITextureTests.Pixel(image, 15, 15, expected);
            }
            if (frame == 1)
            {
                scale2 = current; DPITextureTests.Check(scale2 != dpi.GetRID(), "Scale-two identity differs from base.");
                using var image = RenderingServer.Texture2DGet(scale2)!; DPITextureTests.Check(image.Width == 32 && image.Height == 16, "Scale-two native raster.");
                window.OversamplingOverride = 3;
            }
            else if (frame == 2)
            {
                using (var glyphs = target.GetImage()!) scaledGlyph = GlyphHash(glyphs);
                scale3 = current; DPITextureTests.Check(scale3 != scale2 && scale3 != dpi.GetRID(), "Distinct simultaneous raster scale identities.");
                using var old = RenderingServer.Texture2DGet(scale2)!; using var image = RenderingServer.Texture2DGet(scale3)!;
                DPITextureTests.Check(old.Width == 32 && image.Width == 48, "Previous scale remains borrowed while another viewport retains it.");
                hold.Dispose(); Retired(scale2);
            }
            else if (frame == 3)
            {
                DPITextureTests.Check(draws == 2, "Retained drawing records only on viewport scale changes.");
                dpi.ColorMap = new() { [Colors.Red] = Colors.Yellow, [Colors.Blue] = Colors.Lime }; expected = Colors.Yellow; Retired(scale3);
            }
            else if (frame == 4)
            {
                DPITextureTests.Check(draws == 2, "Live paint edit refreshes retained replay without a new custom draw.");
                window.Oversampling = false;
            }
            else if (frame == 5)
            {
                using (var glyphs = target.GetImage()!) DPITextureTests.Check(GlyphHash(glyphs) != scaledGlyph, "Viewport oversampling changes the actual font raster as well as SVG variants.");
                DPITextureTests.Check(current == dpi.GetRID(), "Disabled oversampling uses base identity.");
                window.Oversampling = true;
            }
            if (frame == 6) dpi.SetSource("");
            if (frame == 7)
            {
                using var image = target.GetImage()!; DPITextureTests.Pixel(image, 15, 15, Colors.Black);
                dpi.SetSource(DPITextureTests.Source);
            }
            if (frame == 8) { using var image = target.GetImage()!; DPITextureTests.Pixel(image, 15, 15, Colors.Yellow); }
            if (frame == 9) window.OversamplingOverride = 2.01f;
            if (frame == 10) { quantized = current; window.OversamplingOverride = 2.015f; }
            if (frame == 11) { DPITextureTests.Check(current == quantized, "Values in one 1/64 scale bucket retain the same raster identity."); window.OversamplingOverride = 3; Retired(quantized); }
            if (frame > 512) throw new InvalidOperationException("The host did not provide 64 consecutive stable frames.");
            if (frame < 12) return;
            var observed = transitions + input.Count;
            if (observed != previousTransitions)
            {
                previousTransitions = observed; samples = 0; begin = GC.GetAllocatedBytesForCurrentThread(); return;
            }
            if (++samples == 64)
            {
                allocated = GC.GetAllocatedBytesForCurrentThread() - begin;
                RenderingServer.FramePostDraw -= Capture;
                using var image = target.GetImage()!; DPITextureTests.Pixel(image, 15, 15, Colors.Yellow);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(Path.Combine(folder, "dpi-texture-" + backend + ".png")); }
                window.Tree!.Quit();
            }
        }
        window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Capture; };
        try { Engine.Run(window); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
        DPITextureTests.Check(samples == 64 && allocated == 0, "Scalable SVG warmed full-frame managed bytes=" + allocated);
        Console.WriteLine($"DPI texture {backend}: actual pixels, two/three/unit scales, shared level lifetime, paint edits without rerecording and 64 warmed full frames ({allocated} B) passed.");
    }
}
