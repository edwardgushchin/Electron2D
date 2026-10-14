using Electron2D;

internal static class UndoRedoRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(180, 90) }; var color = new ColorRect { Name = "Paint", Position = new(12, 12), Size = new(60, 60), Color = Colors.Red }; window.AddChild(color);
        using var history = new UndoRedo(); var fill = new PropertyDescriptor<ColorRect, Color>("Color", c => c.Color, (c, v) => c.Color = v);
        var added = new ColorRect { Name = "Added", Position = new(100, 12), Size = new(60, 60), Color = Colors.Green };
        using var target = window.GetTexture(); var frames = 0; long before = 0, allocated = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); if (frames >= 7) { history.Undo(); history.Redo(); } };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            if (frames >= 71) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
            frames++;
            if (frames > 7)
            {
                if (frames < 135) return;
                Check(allocated == 0, "64 warm rendered history frame bytes=" + allocated); RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit(); return;
            }
            using var image = target.GetImage() ?? throw new InvalidOperationException("Undo readback.");
            Pixel(image, 20, 20, frames is 1 or 3 ? Colors.Red : frames == 7 ? Colors.Yellow : Colors.Blue);
            switch (frames)
            {
                case 1:
                    history.CreateAction("Paint"); history.AddDoProperty(color, fill, Colors.Blue); history.AddUndoProperty(color, fill, Colors.Red); history.CommitAction(); break;
                case 2: history.Undo(); break;
                case 3: history.Redo(); break;
                case 4:
                    history.ClearHistory(false); history.CreateAction("Add"); history.AddDoMethod(() => window.AddChild(added)); history.AddUndoMethod(() => window.RemoveChild(added)); history.AddDoReference(added); history.CommitAction(); break;
                case 5: Pixel(image, 110, 20, Colors.Green); history.Undo(); break;
                case 6:
                    Pixel(image, 110, 20, Colors.Black); history.CreateAction("Branch"); Check(added.IsDisposed, "New branch disposes undone authored node."); history.AddDoProperty(color, fill, Colors.Yellow); history.AddUndoProperty(color, fill, Colors.Blue); history.CommitAction(); break;
                case 7: Check(history.GetHistoryCount() == 1 && history.GetVersion() == 3, "Native authoring history metadata."); break;
            }
            if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, "undo-" + backend + "-" + frames + ".png")); }
        }
        try { Check(Engine.Run(window) == 0 && frames == 135, "Undo native host completed."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; added.Dispose(); }
        Console.WriteLine("UndoRedo " + backend + ": actual scene paint/undo/redo, node addition/removal/branch retirement and 64 warm rendered playback frames=0 managed bytes passed.");
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var c = image.GetPixel(x, y); Check(Math.Abs(c.R - expected.R) < .06f && Math.Abs(c.G - expected.G) < .06f && Math.Abs(c.B - expected.B) < .06f, "Undo pixel " + x + "," + y + " expected " + expected + " actual " + c); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
