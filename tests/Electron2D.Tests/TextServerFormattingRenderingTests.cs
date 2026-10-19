using System.Diagnostics;
using Electron2D;

internal static class TextServerFormattingRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        using var restored = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_TEXT_FORMATTING_FONT") is { } path ? ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore) : null;
        var font = restored ?? ThemeDB.FallbackFont ?? throw new InvalidOperationException("Native formatting font.");
        var server = TextServerManager.GetPrimaryInterface(); var text = server.CreateShapedText(); var vertical = server.CreateShapedText(TextDirection.LTR, TextOrientation.Vertical);
        server.ShapedTextAddString(text, "alpha beta gamma delta epsilon office letters", [font], 28);
        server.ShapedTextAddString(vertical, "ABCD EFGH IJKL", [font], 25);
        server.ShapedTextSetSpacing(text, TextSpacingType.Top, 3); server.ShapedTextSetSpacing(text, TextSpacingType.Glyph, 1); server.ShapedTextSetCustomEllipsis(text, '#');
        using var paragraph = new TextParagraph { Width = 400 }; paragraph.AddString("first\nborrowed alpha beta gamma delta", font, 26); var line = paragraph.GetLineRID(1);
        var textWidth = server.ShapedTextGetWidth(text); var lineWidth = server.ShapedTextGetWidth(line); var verticalWidth = server.ShapedTextGetWidth(vertical);
        var flags = TextOverrunFlags.Trim | TextOverrunFlags.AddEllipsis | TextOverrunFlags.ShortStringEllipsis;
        server.ShapedTextFitToWidth(text, textWidth + 100); server.ShapedTextOverrunTrimToWidth(text, 200, flags);
        server.ShapedTextFitToWidth(line, lineWidth + 100); server.ShapedTextOverrunTrimToWidth(line, 160, flags);
        server.ShapedTextFitToWidth(vertical, verticalWidth + 100); server.ShapedTextOverrunTrimToWidth(vertical, 140, flags);
        var selection = server.ShapedTextGetSelection(text, 0, 5);
        using var window = new Window { Size = new(600, 300) }; var canvas = new Entity(); window.AddChild(canvas); using var target = window.GetTexture();
        var frames = 0; var draws = 0; long before = 0, active = 0, idle = 0;
        canvas.Draw += item =>
        {
            draws++;
            foreach (var interval in selection) item.DrawRect(new(20 + interval.X, 50 - server.ShapedTextGetAscent(text), interval.Y - interval.X, server.ShapedTextGetAscent(text) + server.ShapedTextGetDescent(text)), Colors.Blue);
            server.ShapedTextDraw(text, item, new(20, 50)); server.ShapedTextDrawOutline(text, item, new(20, 115), outlineSize: 2, color: Colors.Green);
            paragraph.DrawLine(item, new(20, 180), 1); server.ShapedTextDraw(vertical, item, new(500, 30));
        };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames < 135)
                {
                    var variation = frames % 2 * 40;
                    server.ShapedTextFitToWidth(text, textWidth + 100 + variation); server.ShapedTextOverrunTrimToWidth(text, 200 + variation, flags);
                    server.ShapedTextFitToWidth(line, lineWidth + 100 + variation); server.ShapedTextOverrunTrimToWidth(line, 160 + variation, flags);
                    server.ShapedTextFitToWidth(vertical, verticalWidth + 100 + variation); server.ShapedTextOverrunTrimToWidth(vertical, 140 + variation, flags);
                    canvas.QueueRedraw();
                }
                server.ShapedTextGetTrimPos(text); server.ShapedTextGetEllipsisPos(line); server.ShapedTextGetCarets(text, 3); server.ShapedTextGetSpacing(text, TextSpacingType.Glyph);
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
                using var image = target.GetImage() ?? throw new InvalidOperationException("Native formatting readback.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory) { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, "text-formatting-" + backend + ".png")); }
                Check(Count(image, new(10, 5, 300, 65), 0) > 100 && Count(image, new(10, 5, 300, 65), 1) > 100, "Actual fitted/trimmed glyphs, custom ellipsis and selection pixels.");
                Check(Count(image, new(10, 75, 300, 60), 3) > 50, "Trimmed outline reaches the canvas: " + Count(image, new(10, 75, 300, 60), 3) + ", green=" + Colors.Green);
                Check(Count(image, new(10, 155, 300, 70), 0) > 50, "Borrowed trimmed line reaches its Paragraph consumer.");
                Check(Count(image, new(460, 10, 90, 180), 0) > 50, "Vertical trimmed ellipsis pixels.");
                Check(server.ShapedTextGetWidth(text) <= 240 && server.ShapedTextGetEllipsisPos(text) >= 0 && paragraph.GetLineWidth(1) == server.ShapedTextGetWidth(line), "Live display metadata matches the rendered consumer.");

            }
            if (frames < 199) return;
            Check(draws >= 135 && active == 0 && idle == 0, "64 active + 64 idle complete frames owner bytes=" + active + "/" + idle);
            RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        try { Check(Engine.Run(window) == 0 && frames == 199, "Native formatting host completes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; server.FreeRID(text); server.FreeRID(vertical); }
        if (restored is null) Fresh(backend); else Console.WriteLine("{\"textFormatting\":true,\"freshProcess\":true,\"rendered\":true}");
        Console.WriteLine("TextServer formatting " + backend + ": actual fitted/trimmed horizontal, borrowed, vertical and outline pixels; 64 changing fit/trim + 64 idle frames=0/0 owner bytes passed.");
    }
    private static void Fresh(string backend)
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans };
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-formatting-font-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(font, path); var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(TextServerFormattingRenderingTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_TEXT_SERVER_FORMATTING_RENDER"] = backend; start.Environment["ELECTRON2D_TEST_TEXT_FORMATTING_FONT"] = path;
            using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh text formatting host."); }
            var output = stdout.GetAwaiter().GetResult(); var error = stderr.GetAwaiter().GetResult();
            Check(child.ExitCode == 0 && output.Contains("\"textFormatting\":true"), "Fresh public authoring/load/render: " + output + error); Console.WriteLine(output.Trim());
        }
        finally { File.Delete(path); }
    }
    private static int Count(Image image, Rect2i area, int kind)
    {
        var count = 0;
        for (var y = area.Position.Y; y < area.End.Y; y++) for (var x = area.Position.X; x < area.End.X; x++)
            {
                var c = image.GetPixel(x, y);
                if (kind switch { 0 => c.R > .6f && c.G > .6f && c.B > .6f, 1 => c.B > .6f && c.R < .2f && c.G < .2f, 2 => c.R > .6f && c.G < .2f && c.B < .2f, _ => c.G > .6f && c.R < .2f && c.B < .2f }) count++;
            }
        return count;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
