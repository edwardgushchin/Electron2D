using Electron2D;

internal static partial class FontSupportTests
{
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_FONT_SUPPORT_RENDERER") ?? "gpu";
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            using var root = Bitmap('A', 6, Colors.White); using var first = Bitmap('B', 7, Colors.Red); using var second = Bitmap('B', 11, Colors.Green);
            root.Fallbacks = [first, second]; using var variation = new FontVariation { BaseFont = root };
            var window = new Window { Size = new(180, 130) }; var canvas = new SupportCanvas { Font = root }; window.AddChild(canvas);
            var label = new Label { Text = "B", Position = new(40, 14), Size = new(60, 24), Language = "en" }; label.AddThemeFontOverride("font", variation); label.AddThemeFontSizeOverride("font_size", 8); window.AddChild(label);
            var rich = new RichTextLabel { Text = "B", Position = new(80, 14), Size = new(60, 24), Language = "en" }; rich.AddThemeFontOverride("normal_font", root); rich.AddThemeFontSizeOverride("normal_font_size", 8); window.AddChild(rich);
            var frame = 0; long before = 0, active = 0, idle = 0;
            void Pre() => before = GC.GetAllocatedBytesForCurrentThread();
            void Post()
            {
                var after = GC.GetAllocatedBytesForCurrentThread(); if (frame >= 32 && frame < 96) active += after - before; else if (frame >= 96) idle += after - before;
                if (frame is 2 or 5)
                {
                    using var image = RenderingServer.Service!.Readback();
                    var pixel = image.GetPixel(17, 25); Check(frame == 2 ? pixel.R > .8f && pixel.G < .2f : pixel.G > .4f && pixel.R < .2f, "Fallback override changes actual canvas pixels.");
                    Check(HasColor(image, 40, 14, 60, 24, frame == 2) && HasColor(image, 80, 14, 60, 24, frame == 2), "Shared Label, variation and rich rendering follows source policy.");
                    if (frame == 5 && Environment.GetEnvironmentVariable("ELECTRON2D_FONT_SUPPORT_CAPTURE") is { } path) image.SavePNG(path);
                }
                if (frame == 3) first.SetScriptSupportOverride("Latn", false);
                if (frame < 96) { canvas.QueueRedraw(); label.QueueRedraw(); rich.QueueRedraw(); }
                if (frame >= 32 && frame < 96) active += GC.GetAllocatedBytesForCurrentThread() - after; else if (frame >= 96) idle += GC.GetAllocatedBytesForCurrentThread() - after;
                if (++frame == 160) window.Tree!.Quit();
            }
            window.Ready += _ => { RenderingServer.SetDefaultClearColor(new(.08f, .09f, .12f)); RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post; };
            try { Check(Engine.Run(window) == 0 && frame == 160 && active == 0 && idle == 0, "Prepared fallback rendering: active=" + active + ", idle=" + idle); }
            finally { if (RenderingServer.IsAvailable) { RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; } }
            Console.WriteLine("64 active and 64 idle fallback intervals: " + active + "/" + idle + " managed bytes; backend=" + backend);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private static bool HasColor(Image image, int x, int y, int width, int height, bool red)
    {
        for (var j = y; j < y + height; j++) for (var i = x; i < x + width; i++) { var color = image.GetPixel(i, j); if (red ? color.R > .8f && color.G < .2f : color.G > .4f && color.R < .2f) return true; }
        return false;
    }
    private sealed class SupportCanvas : Control
    {
        internal required FontFile Font;
        protected override void OnDraw() => DrawString(Font, new(16, 30), "B", fontSize: 8);
    }
}
