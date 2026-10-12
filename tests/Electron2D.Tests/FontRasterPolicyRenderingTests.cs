using Electron2D;

internal static partial class FontRasterPolicyTests
{
    private static FontFile Checker(bool mipmaps)
    {
        var font = new FontFile { AllowSystemFallback = false, FixedSize = 32, FixedSizeScaleMode = FixedSizeScaleMode.Enabled };
        using var image = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
        for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) image.SetPixel(x, y, ((x ^ y) & 1) == 0 ? Colors.White : Colors.Black);
        font.SetTextureImage(0, new(32, 0), 0, image); font.SetGlyphOffset(0, new(32, 0), 'A', new(0, -32)); font.SetGlyphSize(0, new(32, 0), 'A', new(32, 32));
        font.SetGlyphUVRect(0, new(32, 0), 'A', new(0, 0, 32, 32)); font.SetGlyphTextureIndex(0, new(32, 0), 'A', 0); font.SetGlyphAdvance(0, 32, 'A', new(32, 32));
        font.SetCacheAscent(0, 32, 32); font.GenerateMIPMaps = mipmaps; return font;
    }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_FONT_RASTER_RENDERER") ?? "gpu"; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            using var native = new FontFile { Data = FontTestFixtures.Strike, SubpixelPositioning = FontSubpixelPositioning.Disabled, GenerateMIPMaps = true, AllowSystemFallback = false };
            using var variation = new FontVariation { BaseFont = native }; using var mipmaps = Checker(true); using var plain = Checker(false); using var system = new SystemFont { GenerateMIPMaps = true, ForceAutohinter = true };
            var window = new Window { Size = new(240, 130) }; var canvas = new RasterCanvas { Native = native, System = system }; window.AddChild(canvas);
            var sampling = new CheckerCanvas { Mipmaps = mipmaps, Plain = plain, TextureFilter = backend == "gpu" ? TextureFilter.NearestWithMipmaps : TextureFilter.Nearest }; window.AddChild(sampling);
            var label = new Label { Text = "A", Position = new(40, 14), Size = new(60, 30) }; label.AddThemeFontOverride("font", variation); label.AddThemeFontSizeOverride("font_size", 16); window.AddChild(label);
            var rich = new RichTextLabel { Text = "A", Position = new(100, 14), Size = new(60, 30) }; rich.AddThemeFontOverride("normal_font", native); rich.AddThemeFontSizeOverride("normal_font_size", 16); window.AddChild(rich);
            var frame = 0; long before = 0, active = 0, idle = 0;
            void Pre() => before = GC.GetAllocatedBytesForCurrentThread();
            void Post()
            {
                var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 32 && frame < 96) active += after - before; else if (frame >= 96) idle += after - before;
                if (frame is 2 or 5)
                {
                    using var image = RenderingServer.Service!.Readback(); var changed = image.GetPixel(24, 22); Check(frame == 2 ? changed.R > .8f : changed.R < .2f, "Native strike policy changes actual canvas pixels.");
                    if (frame == 5)
                    {
                        Check(image.GetPixel(16, 25).R > .8f, "Selected embedded bitmap is rendered.");
                        Check(Count(image, 40, 14, 60, 30) > 0 && Count(image, 100, 14, 60, 30) > 0, "Label/rich/variation consume raster policy.");
                        if (backend == "gpu") { var filtered = image.GetPixel(18, 82).R; var unfiltered = image.GetPixel(30, 82).R; Check(filtered is > .4f and < .6f && (unfiltered < .2f || unfiltered > .8f), "GPU samples generated native glyph mip levels under minification."); }
                        if (Environment.GetEnvironmentVariable("ELECTRON2D_FONT_RASTER_CAPTURE") is { } path) image.SavePNG(path);
                    }
                }
                if (frame == 3) native.DisableEmbeddedBitmaps = false;
                if (frame < 96) { canvas.QueueRedraw(); sampling.QueueRedraw(); label.QueueRedraw(); rich.QueueRedraw(); }
                if (frame >= 32 && frame < 96) active += GC.GetAllocatedBytesForCurrentThread() - after; else if (frame >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - after;
                if (++frame == 160) window.Tree!.Quit();
            }
            window.Ready += _ => { RenderingServer.SetDefaultClearColor(new(.08f, .09f, .12f)); RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post; };
            try { Check(Engine.Run(window) == 0 && frame == 160 && active == 0 && idle == 0, "Prepared native policies: active=" + active + ", idle=" + idle); }
            finally { if (RenderingServer.IsAvailable) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; } }
            Console.WriteLine("64 active and 64 idle raster-policy intervals: " + active + "/" + idle + " managed bytes; backend=" + backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private static int Count(Image image, int x, int y, int width, int height)
    { var result = 0; for (var j = y; j < y + height; j++) for (var i = x; i < x + width; i++) if (image.GetPixel(i, j).R > .8f) result++; return result; }
    private sealed class RasterCanvas : Control
    {
        internal required FontFile Native; internal required SystemFont System;
        protected override void OnDraw() { DrawChar(Native, new(16, 30), "A", fontSize: 16); DrawString(System, new(16, 115), "MIP raster", fontSize: 16); }
    }
    private sealed class CheckerCanvas : Control
    {
        internal required FontFile Mipmaps, Plain;
        protected override void OnDraw() { DrawChar(Mipmaps, new(16, 85), "A", fontSize: 5); DrawChar(Plain, new(28, 85), "A", fontSize: 5); }
    }
}
