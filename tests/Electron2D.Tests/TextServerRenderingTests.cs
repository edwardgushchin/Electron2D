using System.Diagnostics;
using Electron2D;

internal static class TextServerRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 120;
        using var restored = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_TEXT_SERVER_FONT") is { } sourcePath ? ResourceLoader.Load<FontFile>(sourcePath, ResourceLoader.CacheMode.Ignore) : null;
        var ts = TextServerManager.GetPrimaryInterface(); var font = restored ?? ThemeDB.FallbackFont ?? throw new InvalidOperationException("Default font.");
        var text = ts.CreateShapedText(); var vertical = ts.CreateShapedText(orientation: TextOrientation.Vertical);
        ts.ShapedTextAddString(text, "HHHHH", [font], 24); ts.ShapedTextAddString(vertical, "ABC", [font], 24);
        using var paragraph = new TextParagraph { Width = 80 }; paragraph.AddString("Hello second line", font, 22); var paragraphLine = paragraph.GetLineRID(1);
        using var borrowed = new TextLine(); borrowed.AddString("Shared buffer", font, 20);
        using var window = new Window { Size = new(460, 280) }; var canvas = new Entity(); window.AddChild(canvas);
        using var target = window.GetTexture();
        var frames = 0; var draws = 0; long before = 0; long active = 0; long idle = 0;
        Console.WriteLine("Font=" + font.GetFontName() + " H=" + font.HasChar(72) + " width=" + ts.ShapedTextGetWidth(text));
        var advance = font.GetCharSize('H', 24).X;
        canvas.Draw += item =>
        {
            draws++;
            if (frames < 4)
            {
                var ephemeral = ts.CreateShapedText(); ts.ShapedTextAddString(ephemeral, "HHHHH", [font], 24);
                ts.ShapedTextDraw(ephemeral, item, new(350, 235)); ts.FreeRID(ephemeral);
            }
            ts.ShapedTextDraw(text, item, new(10, 40));
            ts.ShapedTextDrawOutline(text, item, new(10, 90), outlineSize: 2, color: Colors.Blue, oversampling: 1.25f);
            ts.ShapedTextDraw(text, item, new(10, 90), color: Colors.Yellow, oversampling: 1.25f);
            ts.ShapedTextDraw(text, item, new(10, 140), clipR: advance + .01f, color: Colors.Magenta);
            ts.ShapedTextDraw(text, item, new(160, 140), clipL: advance + .01f, color: Colors.Red);
            ts.ShapedTextDraw(text, item, new(10, 190), clipR: 0, color: Colors.Cyan);
            ts.ShapedTextDrawOutline(text, item, new(10, 235), outlineSize: 2, color: Colors.Blue, oversampling: 1.25f);
            font.DrawStringOutline(item, new(160, 235), "HHHHH", fontSize: 24, size: 2, modulate: Colors.Blue, oversampling: 1.25f);
            ts.ShapedTextDraw(vertical, item, new(420, 30), clipL: 0, clipR: 80, color: Colors.Green);
            borrowed.Draw(item, new(230, 10));
            ts.ShapedTextDraw(borrowed.GetRID(), item, new(230, 60 + borrowed.GetLineAscent()));
            ts.ShapedTextDraw(paragraphLine, item, new(230, 145), color: Colors.Cyan);
            paragraph.DrawLine(item, new(230, 185), 1, Colors.Cyan);
        };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames < 135) canvas.QueueRedraw();
                ts.ShapedTextGetRange(text); ts.ShapedTextGetSize(text); ts.ShapedTextGetWidth(text); ts.ShapedTextHitTestPosition(text, 25);
                ts.ShapedTextGetWidth(paragraphLine); ts.ShapedTextIsReady(text);
            };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            if (frames is >= 71 and < 135) active += delta;
            if (frames is >= 135 and < 199) idle += delta;
            frames++;
            if (frames == 2)
            {
                using var image = target.GetImage() ?? throw new InvalidOperationException("TextServer native presentation readback.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } diagnostics) { Directory.CreateDirectory(diagnostics); image.SavePNG(System.IO.Path.Combine(diagnostics, "text-server-diagnostic-" + backend + ".png")); }
                Check(Count(image, new(10, 10, 130, 35), 0) > 60, "Raw text reaches the renderer.");
                Check(Count(image, new(350, 200, 100, 40), 0) > 60, "Retired buffer commands retain recorded native glyph output.");
                Check(Count(image, new(10, 200, 130, 40), 1) > 10, "Outline/raster scale reaches the renderer.");
                Check(Count(image, new(10, 110, 20, 35), 2) > 10 && Count(image, new(35, 110, 95, 35), 2) == 0, "Independent trailing glyph clipping.");
                Check(Count(image, new(160, 110, 15, 35), 3) == 0 && Count(image, new(195, 110, 100, 35), 3) > 10, "Independent leading glyph clipping.");
                Check(Count(image, new(10, 155, 130, 40), 4) == 0, "Zero trailing bound clips all glyphs.");
                Check(Count(image, new(400, 20, 40, 100), 5) > 10, "Vertical raw native glyphs.");
                Check(Count(image, new(230, 110, 150, 40), 4) > 10 && Count(image, new(230, 185, 150, 45), 4) > 10, "Raw paragraph line and ordinary line rendering.");
                for (var y = 210; y < 245; y++) for (var x = 0; x < 130; x++) Check(image.GetPixel(10 + x, y) == image.GetPixel(160 + x, y), "Raw outline/raster pixels exactly match the public Font consumer.");
                for (var y = 0; y < 40; y++) for (var x = 0; x < 170; x++) Check(image.GetPixel(230 + x, 10 + y) == image.GetPixel(230 + x, 60 + y), "Borrowed full buffer uses the same exact native glyph output.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory) { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, "text-server-" + backend + ".png")); }
            }
            if (frames == 3) { ts.ShapedTextAddString(paragraphLine, " AA", [font], 30); canvas.QueueRedraw(); }
            if (frames == 4)
            {
                using var changed = target.GetImage() ?? throw new InvalidOperationException("Detached line readback.");
                Check(Count(changed, new(315, 100, 90, 50), 4) > 10 && Count(changed, new(315, 180, 90, 60), 4) > 10, "Detached line authoring updates both native RID and paragraph consumers.");
            }
            if (frames < 199) return;
            Check(draws >= 135 && active == 0 && idle == 0, "64 active and 64 idle full-frame owner bytes=" + active + "/" + idle);
            RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        try { Check(Engine.Run(window) == 0 && frames == 199, "Native text buffer host completes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; ts.FreeRID(text); ts.FreeRID(vertical); }
        if (restored is null) Authoring(backend);
        else Console.WriteLine("{\"textServer\":true,\"freshProcess\":true,\"rendered\":true}");
        Console.WriteLine("TextServer " + backend + ": native owned/borrowed/paragraph glyph pixels, baseline/outline/clip/both axes and 64 active + 64 idle frames=0/0 owner bytes passed.");
    }
    private static void Authoring(string backend)
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans };
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-shaped-font-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(font, path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(TextServerRenderingTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_TEXT_SERVER_RENDER"] = backend; start.Environment["ELECTRON2D_TEST_TEXT_SERVER_FONT"] = path;
            using var process = Process.Start(start)!; var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh shaped-buffer authoring host."); }
            var text = stdout.GetAwaiter().GetResult(); var errors = stderr.GetAwaiter().GetResult();
            Check(process.ExitCode == 0 && text.Contains("\"textServer\":true"), "Public fresh font-resource/buffer authoring: " + text + errors); Console.WriteLine(text.Trim());
        }
        finally { File.Delete(path); }
    }
    private static int Count(Image image, Rect2i area, int color)
    {
        var count = 0;
        for (var y = area.Position.Y; y < area.End.Y; y++) for (var x = area.Position.X; x < area.End.X; x++)
            {
                var c = image.GetPixel(x, y);
                if (color switch { 0 => c.R > .5f && c.G > .5f && c.B > .5f, 1 => c.B > .5f && c.R < .3f, 2 => c.R > .5f && c.B > .5f && c.G < .3f, 3 => c.R > .5f && c.G < .3f && c.B < .3f, 4 => c.G > .5f && c.B > .5f && c.R < .3f, _ => c.G > .3f && c.R < .3f && c.B < .3f }) count++;
            }
        return count;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
