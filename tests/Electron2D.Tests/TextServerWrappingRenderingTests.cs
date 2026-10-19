using System.Diagnostics;
using Electron2D;

internal static class TextServerWrappingRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        using var restored = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_TEXT_WRAPPING_FONT") is { } path ? ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore) : null;
        var font = restored ?? ThemeDB.FallbackFont ?? throw new InvalidOperationException("Native wrapping font.");
        var server = TextServerManager.GetPrimaryInterface(); var source = server.CreateShapedText(); var verticalSource = server.CreateShapedText(TextDirection.LTR, TextOrientation.Vertical);
        server.ShapedTextAddString(source, "office alpha beta gamma אב delta\nsecond line", [font], 25);
        server.ShapedTextAddString(verticalSource, "ABCD EFGH", [font], 25);
        var flags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound | TextLineBreakFlags.Adaptive | TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces;
        var ranges = server.ShapedTextGetLineBreaksAdv(source, [110, 170], once: false, breakFlags: flags);
        var verticalRanges = server.ShapedTextGetLineBreaks(verticalSource, 120, breakFlags: flags);
        var lines = new RID[ranges.Length / 2]; var columns = new RID[verticalRanges.Length / 2];
        var selections = new Vector2[lines.Length][];
        for (var i = 0; i < lines.Length; i++) { lines[i] = server.ShapedTextSubstr(source, ranges[i * 2], ranges[i * 2 + 1] - ranges[i * 2]); selections[i] = server.ShapedTextGetSelection(lines[i], ranges[i * 2], ranges[i * 2 + 1]); }
        for (var i = 0; i < columns.Length; i++) columns[i] = server.ShapedTextSubstr(verticalSource, verticalRanges[i * 2], verticalRanges[i * 2 + 1] - verticalRanges[i * 2]);
        Check(lines.Length >= 3 && columns.Length >= 2, "Custom multiline and column layout.");
        server.FreeRID(source); server.FreeRID(verticalSource);
        using var window = new Window { Size = new(600, 320) }; var canvas = new Entity(); window.AddChild(canvas); using var target = window.GetTexture();
        var frames = 0; var draws = 0; long before = 0, active = 0, idle = 0;
        canvas.Draw += item =>
        {
            draws++;
            for (var i = 0; i < lines.Length; i++)
            {
                var baseline = new Vector2(20, 35 + i * 40);
                foreach (var interval in selections[i]) item.DrawRect(new(baseline.X + interval.X, baseline.Y - server.ShapedTextGetAscent(lines[i]), interval.Y - interval.X, server.ShapedTextGetAscent(lines[i]) + server.ShapedTextGetDescent(lines[i])), Colors.Blue);
                server.ShapedTextDraw(lines[i], item, baseline);
            }
            for (var i = 0; i < columns.Length; i++) server.ShapedTextDraw(columns[i], item, new(350 + i * 40, 25));
        };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames < 135) canvas.QueueRedraw();
                foreach (var line in lines) { server.ShapedTextGetParent(line); server.ShapedTextGetCarets(line, server.ShapedTextGetRange(line).X); server.ShapedTextGetWidth(line); }
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
                using var image = target.GetImage() ?? throw new InvalidOperationException("Native wrapping readback.");
                Check(Count(image, new(10, 5, 260, 280), 0) > 100 && Count(image, new(10, 5, 260, 280), 1) > 100, "Actual multiline glyph and selection pixels.");
                Check(Count(image, new(320, 10, 260, 200), 0) > 100, "Vertical substring column pixels.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory) { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, "text-wrapping-" + backend + ".png")); }
            }
            if (frames == 3) { server.ShapedTextAddString(lines[0], "!", [font], 25); Check(server.ShapedTextGetParent(lines[0]) == default, "Live substring mutation detaches."); }
            if (frames < 199) return;
            Check(draws >= 135 && active == 0 && idle == 0, "64 active + 64 idle full-frame owner bytes=" + active + "/" + idle);
            RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        try { Check(Engine.Run(window) == 0 && frames == 199, "Native wrapping host completes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; foreach (var line in lines) server.FreeRID(line); foreach (var column in columns) server.FreeRID(column); }
        if (restored is null) Fresh(backend); else Console.WriteLine("{\"textWrapping\":true,\"freshProcess\":true,\"rendered\":true}");
        Console.WriteLine("TextServer wrapping " + backend + ": multiline/vertical substring pixels, parent-free lifetime, live detach and 64 active + 64 idle frames=0/0 owner bytes passed.");
    }
    private static void Fresh(string backend)
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans };
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-wrapping-font-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(font, path); var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(TextServerWrappingRenderingTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_TEXT_SERVER_WRAPPING_RENDER"] = backend; start.Environment["ELECTRON2D_TEST_TEXT_WRAPPING_FONT"] = path;
            using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh text wrapping host."); }
            var output = stdout.GetAwaiter().GetResult(); var error = stderr.GetAwaiter().GetResult();
            Check(child.ExitCode == 0 && output.Contains("\"textWrapping\":true"), "Fresh public authoring/load/render: " + output + error); Console.WriteLine(output.Trim());
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
