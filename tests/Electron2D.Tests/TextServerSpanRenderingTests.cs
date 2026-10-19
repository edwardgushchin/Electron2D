using System.Diagnostics;
using Electron2D;

internal static class TextServerSpanRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        using var restored = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_TEXT_SPAN_FONT") is { } path ? ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore) : null;
        using var local = restored is null ? new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false } : null;
        var font = restored ?? local!; using var replacement = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        var server = TextServerManager.GetPrimaryInterface(); var buffers = new RID[4]; var metadata = new[] { 3, 5 };
        buffers[0] = server.CreateShapedText();
        server.ShapedTextAddString(buffers[0], "ffi first ", [font], 24, new Dictionary<string, int> { ["liga"] = 1 }, "en", metadata);
        server.ShapedTextAddString(buffers[0], "SECOND", [font], 24);
        var width = server.ShapedTextGetWidth(buffers[0]); var glyphs = server.ShapedTextGetGlyphCount(buffers[0]);
        Color[] colors = [Colors.White, Colors.Red, Colors.Green, Colors.Blue];
        Vector2[] positions = [new(20, 45), new(20, 120), new(160, 120), new(340, 120)];
        void Author(bool changed)
        {
            if (changed)
            {
                server.ShapedSetSpanUpdateFont(buffers[0], 0, [replacement], 38, new Dictionary<string, int> { ["liga"] = 0 });
                server.ShapedSetSpanUpdateFont(buffers[0], 1, [replacement], 24);
                Check(server.ShapedTextGetWidth(buffers[0]) > width && server.ShapedTextGetGlyphCount(buffers[0]) > glyphs, "Live span font/size/features change geometry.");
                Check(server.TryGetSpanMetadata<int[]>(buffers[0], 0, out var value) && ReferenceEquals(value, metadata), "Live metadata identity.");
            }
            var words = server.ShapedTextGetWordBreaks(buffers[0]); Check(words.AsSpan().SequenceEqual([0, 3, 4, 9, 10, 16]), "Three shaped words drive actual source ranges.");
            for (var i = 1; i < buffers.Length; i++)
            {
                if (buffers[i] != default) server.FreeRID(buffers[i]);
                buffers[i] = server.ShapedTextSubstr(buffers[0], words[(i - 1) * 2], words[(i - 1) * 2 + 1] - words[(i - 1) * 2]);
            }
            if (changed) { font.Dispose(); Check(server.ShapedTextGetWidth(buffers[0]) > 0, "Retired old font is no longer a dependency."); }
        }
        Author(false);
        using var window = new Window { Size = new(600, 240) }; var canvas = new Entity(); window.AddChild(canvas); using var target = window.GetTexture();
        var frames = 0; var draws = 0; var initialInk = 0; long before = 0, active = 0, idle = 0;
        canvas.Draw += item => { draws++; for (var i = 0; i < buffers.Length; i++) server.ShapedTextDraw(buffers[i], item, positions[i], color: colors[i]); };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 25) Author(true); // Text transformations and returned snapshots are cold authoring.
                if (frames < 135) canvas.QueueRedraw();
                server.ShapedTextGetWidth(buffers[0]); server.ShapedTextGetGlyphCount(buffers[0]);
            };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            if (frames is >= 71 and < 135) active += delta;
            if (frames is >= 135 and < 199) idle += delta;
            frames++;
            if (frames is 2 or 50)
            {
                using var image = target.GetImage() ?? throw new InvalidOperationException("Span render readback.");
                var ink = Count(image, new(10, 5, 580, 60), 0); Check(ink > 200, "Complete source glyph pixels.");
                if (frames == 2) initialInk = ink; else Check(ink > initialInk * 1.2, "Live larger replacement produces more visible glyph ink.");
                Check(Count(image, new(10, 65, 130, 75), 1) > 40 && Count(image, new(150, 65, 170, 75), 2) > 100 && Count(image, new(330, 65, 250, 75), 3) > 100, "Independent shaped word ranges render with real span fonts.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory)
                { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, "text-span-" + backend + "-" + frames + ".png")); }
            }
            if (frames < 199) return;
            Check(draws >= 135 && active == 0 && idle == 0, "64 active + 64 idle complete frames owner bytes=" + active + "/" + idle);
            RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        try { Check(Engine.Run(window) == 0 && frames == 199, "Span rendering host completes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; foreach (var id in buffers) if (id != default) server.FreeRID(id); }
        if (restored is null) Fresh(backend, replacement); else Console.WriteLine("{\"spanText\":true,\"freshProcess\":true,\"rendered\":true}");
        Console.WriteLine("TextServer Span " + backend + ": initial/live span font/size/features, old-font retirement and shaped word-range consumers; 64 active + 64 idle frames=0/0 owner bytes passed.");
    }
    private static void Fresh(string backend, FontFile font)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-span-font-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(font, path); var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(TextServerSpanRenderingTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_TEXT_SERVER_SPANS_RENDER"] = backend; start.Environment["ELECTRON2D_TEST_TEXT_SPAN_FONT"] = path;
            using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh Span text host."); }
            var output = stdout.GetAwaiter().GetResult(); var error = stderr.GetAwaiter().GetResult();
            Check(child.ExitCode == 0 && output.Contains("\"spanText\":true"), "Fresh font load and text authoring/render: " + output + error); Console.WriteLine(output.Trim());
        }
        finally { File.Delete(path); }
    }
    private static int Count(Image image, Rect2i area, int kind)
    {
        var count = 0;
        for (var y = area.Position.Y; y < area.End.Y; y++) for (var x = area.Position.X; x < area.End.X; x++)
            {
                var c = image.GetPixel(x, y);
                if (kind switch
                {
                    0 => c.R > .6f && c.G > .6f && c.B > .6f,
                    1 => c.R > .6f && c.G < .2f && c.B < .2f,
                    2 => c.G > .6f && c.R < .2f && c.B < .2f,
                    3 => c.B > .6f && c.R < .2f && c.G < .2f,
                    _ => c.B > .6f && c.R < .2f && c.G < .2f
                }) count++;
            }
        return count;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
