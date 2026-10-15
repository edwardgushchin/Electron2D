using Electron2D;

internal static class TextLineRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(360, 240) }; var canvas = new Entity(); window.AddChild(canvas);
        var font = ThemeDB.FallbackFont ?? throw new InvalidOperationException("Default font.");
        using var line = new TextLine(); var key = new TextObjectKey();
        line.AddString("Hello ", font, 22, "en", 23); line.AddObject(key, new(18, 20), length: 3); line.AddString(" text buffer", font, 30);
        using var vertical = new TextLine { Orientation = TextOrientation.Vertical }; vertical.AddString("ABC", font, 24);
        using var outline = new TextLine(); outline.AddString("Outline", font, 26);
        using var noTrim = new TextLine { Width = font.GetStringSize("H", fontSize: 24).X, TextOverrunBehavior = TextOverrunBehavior.NoTrimming }; noTrim.AddString("HHH", font, 24);
        var drawing = 0; var frames = 0; long before = 0, allocated = 0;
        canvas.Draw += item =>
        {
            drawing++; line.Draw(item, new(12, 14), Colors.White); var rect = line.GetObjectRect(key); rect.Position += new Vector2(12, 14); item.DrawRect(rect, Colors.Green);
            outline.DrawOutline(item, new(12, 100), 2, Colors.Blue, 1.25f); outline.Draw(item, new(12, 100), Colors.Yellow, 1.25f);
            vertical.Draw(item, new(310, 20), Colors.Cyan); noTrim.Draw(item, new(220, 160), Colors.Magenta);
            if (frames >= 4)
            {
                line.Alignment = line.Alignment; line.Width = line.Width; line.Direction = line.Direction;
                line.GetSize(); line.GetLineAscent(); line.GetLineDescent(); line.GetLineWidth(); line.HitTest(40); line.GetInferredDirection();
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
                using var image = target.GetImage() ?? throw new InvalidOperationException("TextLine readback.");
                Check(Colored(image, new(12, 14, 290, 65), 0) > 20, "Real text pixels.");
                Check(Colored(image, new(300, 20, 50, 110), 1) > 10, "Vertical text pixels.");
                Check(Colored(image, new(12, 100, 180, 50), 2) > 20, "Outlined text pixels.");
                Check(Colored(image, new(220, 150, 15, 60), 3) > 5 && Colored(image, new(240, 150, 55, 60), 3) == 0, "Advance-axis clipping skips overflow glyphs.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, "text-line-" + backend + "-" + frames + ".png")); }
            }
            if (frames == 1) { line.Width = 125; line.TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce; line.Alignment = HorizontalAlignment.Right; }
            if (frames == 2) { line.Width = -1; line.Alignment = HorizontalAlignment.Left; line.ResizeObject(key, new(28, 28), InlineAlignment.Top); }
            if (frames == 3) { line.Direction = TextDirection.RTL; line.Alignment = HorizontalAlignment.Center; line.Width = 290; }
            if (frames < 135) return;
            Check(drawing >= 135 && allocated == 0, "64 warmed full TextLine redraw frame bytes=" + allocated); RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        try { Check(Engine.Run(window) == 0 && frames == 135, "TextLine host finished."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
        Console.WriteLine("TextLine " + backend + ": real mixed-font text, inline length/resize, ellipsis/alignment/direction, vertical text, outline/oversampling/clip and 64 warmed full redraw/query frames=0 managed bytes passed.");
    }
    private static int Colored(Image image, Rect2i rect, int kind)
    {
        var count = 0; for (var y = rect.Position.Y; y < rect.End.Y; y++) for (var x = rect.Position.X; x < rect.End.X; x++)
            { var c = image.GetPixel(x, y); if (kind switch { 0 => c.R > .5f && c.G > .5f && c.B > .5f, 1 => c.R < .2f && c.G > .3f && c.B > .3f, 2 => c.R > .3f && c.G > .3f && c.B < .3f, _ => c.R > .3f && c.G < .2f && c.B > .3f }) count++; }
        return count;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
