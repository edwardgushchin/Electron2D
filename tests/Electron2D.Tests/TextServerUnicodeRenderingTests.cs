using System.Diagnostics;
using Electron2D;

internal static class TextServerUnicodeRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        using var restored = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_TEXT_UNICODE_FONT") is { } path ? ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore) : null;
        using var local = restored is null ? new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false } : null;
        var font = restored ?? local!; var server = TextServerManager.GetPrimaryInterface(); var buffers = new RID[5];
        var features = new Dictionary<string, int> { [server.TagToName(server.NameToTag("kerning"))] = 1 };
        Color[] colors = [Colors.White, Colors.Green, Colors.Yellow, Colors.Red, Colors.Blue];
        Vector2[] positions = [new(20, 45), new(20, 100), new(20, 155), new(300, 45), new(300, 100)];
        void Author(bool changed)
        {
            string[] text = [server.StringToUpper(changed ? "changed ß" : "Straße ﬃ", "en"),
                server.StringToLower(changed ? "IİSTANBUL" : "IİSTAN", "tr"), server.StripDiacritics(changed ? "Crème ﬃ" : "café ﬃ ①")];
            var title = server.StringToTitle(changed ? "REBUILT SOLAR" : "STRAßE IJSSEL", changed ? "en" : "nl");
            var words = server.StringGetWordBreaks(title, changed ? "en" : "nl"); Check(words.Length == 4, "Authoring supplies two real word ranges.");
            var ends = server.StringGetCharacterBreaks(title); Check(ends[^1] == title.Length, "Authoring character boundaries reach the source end.");
            for (var i = 0; i < buffers.Length; i++) { if (buffers[i] != default) server.FreeRID(buffers[i]); buffers[i] = default; }
            for (var i = 0; i < 3; i++) { buffers[i] = server.CreateShapedText(); server.ShapedTextAddString(buffers[i], text[i], [font], 28, features, "en"); }
            var parent = server.CreateShapedText();
            try
            {
                server.ShapedTextAddString(parent, title, [font], 28, features, "nl");
                for (var i = 0; i < 2; i++) buffers[3 + i] = server.ShapedTextSubstr(parent, words[i * 2], words[i * 2 + 1] - words[i * 2]);
            }
            finally { server.FreeRID(parent); }
        }
        Author(false);
        using var window = new Window { Size = new(600, 240) }; var canvas = new Entity(); window.AddChild(canvas); using var target = window.GetTexture();
        var frames = 0; var draws = 0; long before = 0, active = 0, idle = 0;
        canvas.Draw += item => { draws++; for (var i = 0; i < buffers.Length; i++) server.ShapedTextDraw(buffers[i], item, positions[i], color: colors[i]); };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 25) Author(true); // Text transformations and returned snapshots are cold authoring.
                if (frames < 135) canvas.QueueRedraw();
                server.IsValidIdentifier("field_1"); server.IsValidLetter('İ'); server.IsLocaleRightToLeft("he_IL");
                server.NameToTag("kerning"); server.TagToName(0x6b65726e); server.ShapedTextGetWidth(buffers[0]);
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
                using var image = target.GetImage() ?? throw new InvalidOperationException("Unicode render readback.");
                Check(Count(image, new(10, 5, 275, 55), 0) > 100, "Converted full uppercase glyph pixels.");
                Check(Count(image, new(10, 65, 275, 55), 1) > 40, "Locale lowercased glyph pixels.");
                Check(Count(image, new(10, 120, 275, 65), 2) > 100, "NFKD/stripped glyph pixels.");
                Check(Count(image, new(290, 5, 295, 55), 3) > 100 && Count(image, new(290, 65, 295, 55), 4) > 100, "Titlecase word ranges drive independent shaped draws after parent retirement.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory)
                { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, "text-unicode-" + backend + "-" + frames + ".png")); }
            }
            if (frames < 199) return;
            Check(draws >= 135 && active == 0 && idle == 0, "64 active + 64 idle complete frames owner bytes=" + active + "/" + idle);
            RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        try { Check(Engine.Run(window) == 0 && frames == 199, "Unicode rendering host completes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; foreach (var id in buffers) if (id != default) server.FreeRID(id); }
        if (restored is null) Fresh(backend, font); else Console.WriteLine("{\"unicodeText\":true,\"freshProcess\":true,\"rendered\":true}");
        Console.WriteLine("TextServer Unicode " + backend + ": initial/live converted glyphs and scalar word-range consumers; 64 active + 64 idle frames=0/0 owner bytes passed.");
    }
    private static void Fresh(string backend, FontFile font)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-unicode-font-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(font, path); var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(TextServerUnicodeRenderingTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_TEXT_SERVER_UNICODE_RENDER"] = backend; start.Environment["ELECTRON2D_TEST_TEXT_UNICODE_FONT"] = path;
            using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh Unicode text host."); }
            var output = stdout.GetAwaiter().GetResult(); var error = stderr.GetAwaiter().GetResult();
            Check(child.ExitCode == 0 && output.Contains("\"unicodeText\":true"), "Fresh font load and text authoring/render: " + output + error); Console.WriteLine(output.Trim());
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
                    1 => c.G > .6f && c.R < .2f && c.B < .2f,
                    2 => c.R > .6f && c.G > .6f && c.B < .2f,
                    3 => c.R > .6f && c.G < .2f && c.B < .2f,
                    _ => c.B > .6f && c.R < .2f && c.G < .2f
                }) count++;
            }
        return count;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
