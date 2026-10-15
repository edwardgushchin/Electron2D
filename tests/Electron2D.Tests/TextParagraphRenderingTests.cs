using Electron2D;

internal static class TextParagraphRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(520, 360) }; var canvas = new Entity(); window.AddChild(canvas);
        var font = ThemeDB.FallbackFont ?? throw new InvalidOperationException("Default font.");
        using var p = new TextParagraph { Width = 220, LineSpacing = 3 };
        var key = new TextObjectKey(); p.AddString("Mixed ", font, 20, "en", 47); p.AddObject(key, new(16, 24), length: 3); p.AddString("text wraps around a dropcap and spans several real lines.", font, 24);
        p.SetDropcap("D", font, 48, new(new(2, 0), new(8, 2)));
        using var hidden = new TextParagraph { MaxLinesVisible = 1, Alignment = HorizontalAlignment.Right, Width = 200 };
        hidden.AddString("First\nHidden line", font, 23);
        using var vertical = new TextParagraph { Orientation = TextOrientation.Vertical, Width = 85 }; vertical.AddString("AB\nCD", font, 24);
        using var clip = new TextParagraph { BreakFlags = TextLineBreakFlags.None, Width = font.GetStringSize("H", fontSize: 24).X }; clip.AddString("HHH", font, 24);
        var frames = 0; var drawings = 0; long before = 0, allocated = 0;
        canvas.Draw += item =>
        {
            drawings++; p.DrawOutline(item, new(12, 12), 1, Colors.Blue, Colors.Blue, 1.25f); p.Draw(item, new(12, 12), Colors.White, Colors.Red, 1.25f);
            for (var i = 0; i < p.GetLineCount(); i++) if (frames < 4 && p.GetLineObjects(i).Length > 0) { var rect = p.GetLineObjectRect(i, key); rect.Position += new Vector2(12, 12); item.DrawRect(rect, Colors.Green); }
            hidden.DrawLineOutline(item, new(280, 20), 1, 2, Colors.Blue); hidden.DrawLine(item, new(280, 20), 1, Colors.Yellow);
            vertical.Draw(item, new(410, 180), Colors.Cyan); p.DrawDropcapOutline(item, new(280, 250), 1, Colors.Blue); p.DrawDropcap(item, new(280, 250), Colors.Yellow);
            clip.Draw(item, new(320, 120), Colors.Magenta);
            if (frames >= 4)
            {
                p.Width = p.Width; p.Alignment = p.Alignment; p.GetSize(); p.GetLineCount(); p.GetLineRID(0); p.GetLineRange(0); p.GetLineWidth(0); p.GetLineAscent(0); p.GetLineObjectRect(0, key); p.HitTest(new(20, 10));
            }
        };
        using var target = window.GetTexture();
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); canvas.QueueRedraw(); };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            if (frames >= 71) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            frames++;
            if (frames <= 4 || frames == 135)
            {
                using var image = target.GetImage() ?? throw new InvalidOperationException("Paragraph readback.");
                Check(Colored(image, new(12, 12, 250, 200), 0) > 20, "Real wrapped text pixels.");
                Check(Colored(image, new(280, 20, 200, 60), 1) > 20, "Hidden indexed line draws at its own origin.");
                Check(Colored(image, new(280, 250, 240, 90), 1) > 20, "Independent dropcap pixels.");
                Check(Colored(image, new(400, 170, 100, 130), 2) > 10, "Vertical multiline pixels.");
                Check(Colored(image, new(320, 110, 20, 60), 3) > 5 && Colored(image, new(342, 110, 60, 60), 3) == 0, "Paragraph clipping skips overflow glyphs.");
                if (frames == 1) Check(Colored(image, new(12, 12, 70, 90), 4) > 10 && p.GetLineCount() > 2, "Distinct dropcap and actual wrapping.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, "text-paragraph-" + backend + "-" + frames + ".png")); }
            }
            if (frames == 1) { p.MaxLinesVisible = 2; p.TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce; p.Alignment = HorizontalAlignment.Right; }
            if (frames == 2) { p.MaxLinesVisible = -1; p.ResizeObject(key, new(26, 26), InlineAlignment.Top); p.Width = 230; }
            if (frames == 3) { p.Direction = TextDirection.RTL; p.Alignment = HorizontalAlignment.Center; }
            if (frames < 135) return;
            Check(drawings >= 135 && allocated == 0, "64 warmed paragraph redraw/query frame bytes=" + allocated); RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        try { Check(Engine.Run(window) == 0 && frames == 135, "Paragraph host finished."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
        Console.WriteLine("TextParagraph " + backend + ": real wrap/dropcap/hidden-line/outline/vertical/clip, policy reflow and 64 warmed full redraw/query frames=0 managed bytes passed.");
    }
    private static int Colored(Image image, Rect2i rect, int kind)
    {
        var result = 0; for (var y = rect.Position.Y; y < rect.End.Y; y++) for (var x = rect.Position.X; x < rect.End.X; x++)
            { var c = image.GetPixel(x, y); if (kind switch { 0 => c.R > .5f && c.G > .5f && c.B > .5f, 1 => c.R > .3f && c.G > .3f && c.B < .3f, 2 => c.R < .2f && c.G > .3f && c.B > .3f, 3 => c.R > .3f && c.G < .2f && c.B > .3f, _ => c.R > .3f && c.G < .2f && c.B < .2f }) result++; }
        return result;
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
