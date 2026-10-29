using Electron2D;

internal static class ConcreteRendering
{
    internal static void Run(string backend, string output)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var original = TextServerManager.GetPrimaryInterface();
        var fallback = TextServerManager.FindInterface("Fallback (Built-in)")!; var dummy = TextServerManager.FindInterface("Dummy")!;
        using var font = new FontFile(); font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets/Regular.ttf"));
        TextServerManager.SetPrimaryInterface(fallback);
        try
        {
            using var line = new TextLine(); line.AddString("ffi AV scalar text", font, 24);
            using var vertical = new TextLine { Orientation = TextOrientation.Vertical }; vertical.AddString("VERT", font, 20);
            using var paragraph = new TextParagraph { Width = 150 }; paragraph.AddString("Simple paragraph wraps into real lines", font, 20);
            using var root = new Window { Size = new(640, 360) };
            var canvas = new ProfileCanvas(font, line, vertical, paragraph); root.AddChild(canvas);
            var label = new Label { Position = new(210, 10), Size = new(180, 40), Text = "Label ffi profile" }; label.AddThemeFontOverride("font", font); root.AddChild(label);
            var rich = new RichTextLabel { Position = new(210, 65), Size = new(180, 90), Text = "Rich text ffi profile", Threaded = false }; rich.AddThemeFontOverride("normal_font", font); root.AddChild(rich);
            var entry = new LineEdit { Position = new(405, 10), Size = new(225, 40), Text = "Single line ffi" }; entry.AddThemeFontOverride("font", font); root.AddChild(entry);
            var edit = new TextEdit { Position = new(405, 65), Size = new(225, 100), Text = "Editable ffi\nSecond line" }; edit.AddThemeFontOverride("font", font); root.AddChild(edit);
            var code = new CodeEdit { Position = new(405, 175), Size = new(225, 100), Text = "Code ffi\nSecond line", GuttersDrawLineNumbers = true }; code.AddThemeFontOverride("font", font); root.AddChild(code);
            var button = new Button { Position = new(405, 295), Size = new(225, 40), Text = "Button ffi" }; button.AddThemeFontOverride("font", font); root.AddChild(button);
            var phase = 0; var frames = 0;
            Action rendered = () =>
            {
                if (++frames < 3) return;
                using var pixels = root.GetTexture().GetImage() ?? throw new InvalidOperationException("No rendered profile pixels.");
                var regions = new Rect2i[] { new(5, 5, 190, 35), new(5, 45, 165, 35), new(5, 85, 165, 110), new(172, 45, 30, 150), new(205, 5, 190, 45), new(205, 60, 190, 100), new(410, 15, 210, 30), new(410, 70, 210, 85), new(410, 180, 210, 85), new(410, 300, 210, 30) };
                foreach (var region in regions)
                {
                    var ink = Ink(pixels, region);
                    if (phase == 1 ? ink != 0 : ink < 10) throw new InvalidOperationException($"Text profile phase {phase} produced invalid ink {ink} in {region}.");
                }
                if (pixels.GetPixel(15, 220).R < .9f) throw new InvalidOperationException("Text selection broke ordinary canvas rendering.");
                File.WriteAllBytes(output + $"-{phase}.png", pixels.SavePNGToBuffer());
                if (phase == 2) { root.Tree!.Quit(); return; }
                if (phase == 0) Measure(font, line, paragraph);
                else
                {
                    entry.InsertTextAtCaret("X"); edit.InsertTextAtCaret("X"); code.InsertTextAtCaret("X");
                    if (!entry.Text.Contains('X') || !edit.Text.Contains('X') || !code.Text.Contains('X') || !edit.GetCaretDrawPos().IsFinite() || !code.GetCaretDrawPos().IsFinite())
                        throw new InvalidOperationException("Dummy broke logical editing or finite caret geometry.");
                }
                phase++; frames = 0; TextServerManager.SetPrimaryInterface(phase == 1 ? dummy : original);
                canvas.QueueRedraw(); label.QueueRedraw(); rich.QueueRedraw(); entry.QueueRedraw(); edit.QueueRedraw(); code.QueueRedraw(); button.QueueRedraw();
            };
            root.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += rendered; };
            try { Engine.Run(root); }
            finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= rendered; }
            Console.WriteLine($"Concrete profiles rendered ten text roles, Dummy suppression/editing and Advanced restoration on {backend}.");
        }
        finally { TextServerManager.SetPrimaryInterface(original); }
    }
    private static void Measure(Font font, TextLine line, TextParagraph paragraph)
    {
        for (var i = 0; i < 128; i++) { _ = font.GetStringSize("warm ffi"); _ = line.GetSize(); _ = paragraph.GetSize(); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 256; i++) { _ = font.GetStringSize("warm ffi"); _ = line.GetSize(); _ = paragraph.GetSize(); }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        if (bytes != 0) throw new InvalidOperationException($"Prepared simple consumers allocated {bytes} managed owner bytes.");
        Console.WriteLine("Prepared simple font/line/paragraph: 256 iterations, 0 managed owner bytes.");
    }
    private static int Ink(Image image, Rect2i region)
    { var count = 0; for (var y = region.Position.Y; y < region.End.Y; y++) for (var x = region.Position.X; x < region.End.X; x++) { var c = image.GetPixel(x, y); if (c.R + c.G + c.B > 1.2f) count++; } return count; }
    private sealed class ProfileCanvas(Font font, TextLine line, TextLine vertical, TextParagraph paragraph) : Entity
    {
        protected override void OnDraw()
        {
            DrawString(font, new(10, 28), "Font ffi profile", fontSize: 24); line.Draw(this, new(10, 48)); paragraph.Draw(this, new(10, 90)); vertical.Draw(this, new(175, 48));
            DrawRect(new(10, 215, 20, 15), Colors.Red);
        }
    }
}
