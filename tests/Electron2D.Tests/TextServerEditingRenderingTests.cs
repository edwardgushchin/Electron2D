using System.Diagnostics;
using Electron2D;

internal static class TextServerEditingRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        using var restored = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_TEXT_EDITING_FONT") is { } path ? ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore) : null;
        var font = restored ?? ThemeDB.FallbackFont ?? throw new InvalidOperationException("Native editing font.");
        var server = TextServerManager.GetPrimaryInterface(); var text = server.CreateShapedText(); var vertical = server.CreateShapedText(TextDirection.RTL, TextOrientation.Vertical);
        server.ShapedTextAddString(text, "office אב editor", [font], 28); server.ShapedTextAddString(vertical, "אבABC", [font], 28);
        using var paragraph = new TextParagraph(); paragraph.AddString("first\nborrowed editor", font, 26); var line = paragraph.GetLineRID(1);
        var range = server.ShapedTextGetRange(text); var selected = server.ShapedTextGetSelection(text, 0, 5); var verticalSelected = server.ShapedTextGetSelection(vertical, 2, 5);
        using var window = new Window { Size = new(480, 300) }; var canvas = new Entity(); window.AddChild(canvas); using var target = window.GetTexture();
        var frames = 0; var draws = 0; var position = 0; long before = 0, active = 0, idle = 0;
        var lineSelection = server.ShapedTextGetSelection(line, 6, 14);
        canvas.Draw += item =>
        {
            draws++;
            foreach (var interval in selected) item.DrawRect(new(20 + interval.X, 40 - server.ShapedTextGetAscent(text), interval.Y - interval.X, server.ShapedTextGetAscent(text) + server.ShapedTextGetDescent(text)), Colors.Blue);
            server.ShapedTextDraw(text, item, new(20, 40)); DrawCarets(item, server.ShapedTextGetCarets(text, position), new(20, 40), false);
            foreach (var interval in lineSelection) item.DrawRect(new(20 + interval.X, 145 - server.ShapedTextGetAscent(line), interval.Y - interval.X, server.ShapedTextGetAscent(line) + server.ShapedTextGetDescent(line)), Colors.Blue);
            server.ShapedTextDraw(line, item, new(20, 145)); DrawCarets(item, server.ShapedTextGetCarets(line, 9), new(20, 145), false);
            foreach (var interval in verticalSelected) item.DrawRect(new(420 - server.ShapedTextGetAscent(vertical), 40 + interval.X, server.ShapedTextGetAscent(vertical) + server.ShapedTextGetDescent(vertical), interval.Y - interval.X), Colors.Blue);
            server.ShapedTextDraw(vertical, item, new(420, 40)); DrawCarets(item, server.ShapedTextGetCarets(vertical, 2), new(420, 40), true);
        };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames < 135)
                {
                    position = server.ShapedTextNextCharacterPos(text, position); if (position == range.Y) position = range.X;
                    canvas.QueueRedraw();
                }
                server.ShapedTextGetCarets(text, position); server.ShapedTextGetGraphemeBounds(text, position); server.ShapedTextHitTestGrapheme(text, 30);
                server.ShapedTextClosestCharacterPos(text, position); server.ShapedTextPrevCharacterPos(text, position); server.ShapedTextNextGraphemePos(text, position);
                server.ShapedTextPrevGraphemePos(text, position); server.ShapedTextHasVisibleChars(text); server.ShapedTextGetDominantDirectionInRange(text, 0, range.Y);
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
                using var image = target.GetImage() ?? throw new InvalidOperationException("Native editing readback.");
                Check(Count(image, new(10, 5, 360, 55), 0) > 100 && Count(image, new(10, 5, 360, 55), 1) > 100, "Actual text and selection pixels.");
                Check(Count(image, new(10, 100, 340, 55), 0) > 100 && Count(image, new(10, 100, 340, 55), 1) > 100, "Borrowed line selection is line-local.");
                Check(Count(image, new(380, 20, 80, 200), 1) > 100 && Count(image, new(380, 20, 80, 200), 0) > 20, "Vertical text and advance-axis selection pixels.");
                Check(Count(image, new(0, 0, 480, 300), 2) > 5 && Count(image, new(0, 0, 480, 300), 3) > 5, "Leading and trailing caret shapes reach the canvas.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory) { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, "text-editing-" + backend + ".png")); }
            }
            if (frames == 3) server.ShapedTextAddString(line, " +", [font], 26);
            if (frames == 4)
            {
                var retired = line; paragraph.Width = 400; paragraph.GetLineCount(); line = paragraph.GetLineRID(1);
                Check(!server.Has(retired) && server.ShapedTextGetCarets(line, 9).LeadingRect.Size.Y > 0, "Native paragraph reflow replaces detached font read sets and line identities.");
                lineSelection = server.ShapedTextGetSelection(line, 6, 14);
            }
            if (frames < 199) return;
            Check(draws >= 135 && active == 0 && idle == 0, "64 active + 64 idle full-frame owner bytes=" + active + "/" + idle);
            RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        try { Check(Engine.Run(window) == 0 && frames == 199, "Native editing host completes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; server.FreeRID(text); server.FreeRID(vertical); }
        if (restored is null) Fresh(backend); else Console.WriteLine("{\"textEditing\":true,\"freshProcess\":true,\"rendered\":true}");
        Console.WriteLine("TextServer editing " + backend + ": native caret/selection/text pixels and 64 active + 64 idle frames=0/0 owner bytes passed.");
    }
    private static void DrawCarets(CanvasItem item, TextCaretInfo carets, Vector2 baseline, bool vertical)
    {
        Draw(carets.LeadingRect, Colors.Red); Draw(carets.TrailingRect, Colors.Green);
        void Draw(Rect2 rect, Color color)
        {
            if (rect == default) return;
            item.DrawRect(new(baseline + rect.Position, vertical ? new(rect.Size.X, 1) : new(1, rect.Size.Y)), color);
        }
    }
    private static void Fresh(string backend)
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans };
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-editing-font-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(font, path); var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(TextServerEditingRenderingTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_TEXT_SERVER_EDITING_RENDER"] = backend; start.Environment["ELECTRON2D_TEST_TEXT_EDITING_FONT"] = path;
            using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh text editing host."); }
            var output = stdout.GetAwaiter().GetResult(); var error = stderr.GetAwaiter().GetResult();
            Check(child.ExitCode == 0 && output.Contains("\"textEditing\":true"), "Fresh public authoring/load/render: " + output + error); Console.WriteLine(output.Trim());
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
