using Electron2D;

internal static class ProviderRendering
{
    internal static void Run(string backend, string output, bool retireSource = false, bool failDuringRetirement = false)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var builtin = TextServerManager.GetPrimaryInterface();
        using var provider = new DrawingProbe(); using var font = new FontFile();
        font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets/Regular.ttf"));
        using var variation = font.FindVariation();
        Font drawingFont = retireSource ? variation : font;
        TextServerManager.AddInterface(provider); TextServerManager.SetPrimaryInterface(provider);
        try
        {
            using var line = new TextLine(); line.AddString("TextLine provider", drawingFont, 18);
            using var paragraph = new TextParagraph { Width = 150 }; paragraph.AddString("Paragraph wraps through provider callbacks", drawingFont, 18);
            using var root = new Window { Size = new(400, 240) };
            var canvas = new TextCanvas(drawingFont, line, paragraph); root.AddChild(canvas);
            var label = new Label { Position = new(210, 10), Size = new(180, 40), Text = "Label provider" };
            label.AddThemeFontOverride("font", drawingFont); root.AddChild(label);
            var rich = new RichTextLabel { Position = new(210, 60), Size = new(180, 100), Text = "RichText provider", Threaded = false };
            rich.AddThemeFontOverride("normal_font", drawingFont); root.AddChild(rich);
            var checkedCallback = false;
            provider.DuringGlyph = () =>
            {
                if (checkedCallback) return; checkedCallback = true;
                if (retireSource)
                {
                    var captured = provider.GlyphSource;
                    variation.VariationEmbolden = .1f;
                    Reject(() => Task.Run(() => provider.FontGetName(captured)).GetAwaiter().GetResult());
                }
                var metadata = Task.Run(drawingFont.GetFontName);
                if (!metadata.Wait(TimeSpan.FromSeconds(3)) || string.IsNullOrEmpty(metadata.Result)) throw new InvalidOperationException("Provider glyph callback holds no font monitor across metadata work.");
                if (provider.DrawingBuffer != default)
                {
                    Reject(() => provider.FreeRID(provider.DrawingBuffer));
                    Reject(() => provider.ShapedTextClear(provider.DrawingBuffer));
                }
                if (failDuringRetirement)
                {
                    provider.FailBufferFree = true;
                    throw new InvalidDataException("Controlled glyph drawing failure.");
                }
            };
            var frames = 0;
            Action rendered = () =>
            {
                using var pixels = root.GetTexture().GetImage() ?? throw new InvalidOperationException("Rendered viewport readback is unavailable.");
                foreach (var region in new[] { new Rect2i(5, 5, 190, 30), new(5, 48, 190, 30), new(5, 88, 190, 110), new(205, 5, 190, 45), new(205, 55, 190, 100) })
                    if (Ink(pixels, region) < 10) throw new InvalidOperationException("A provider-backed text consumer produced no visible pixels: " + region);
                if (++frames < 2) { canvas.QueueRedraw(); label.QueueRedraw(); rich.QueueRedraw(); return; }
                File.WriteAllBytes(output, pixels.SavePNGToBuffer()); root.Tree!.Quit();
            };
            root.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += rendered; };
            Exception? observed = null;
            try { Engine.Run(root); }
            catch (Exception error) { observed = error; }
            finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= rendered; }
            if (failDuringRetirement)
            {
                var failures = observed is AggregateException combined ? combined.Flatten().InnerExceptions.ToArray() : observed is null ? [] : new[] { observed };
                if (!failures.Any(error => error is InvalidDataException) || !failures.Any(error => error is ApplicationException))
                    throw new InvalidOperationException("Drawing and deferred buffer retirement errors must both survive.", observed);
                root.Dispose(); line.Dispose(); paragraph.Dispose(); drawingFont.SetCacheCapacity(0, 0);
                if (provider.LiveBuffers.Count != 0) throw new InvalidOperationException("Failed drawing retained provider buffers.");
                Console.WriteLine("Provider drawing and deferred source retirement preserve both failures on " + backend + ".");
                return;
            }
            if (observed is not null) throw observed;
            if (!checkedCallback || provider.GlyphDraws == 0 || provider.ShapedDraws == 0 || provider.ParserCalls == 0) throw new InvalidOperationException("Real drawing did not invoke the external glyph and shaped-buffer callbacks.");
            root.Dispose(); line.Dispose(); paragraph.Dispose(); drawingFont.SetCacheCapacity(0, 0); font.SetCacheCapacity(0, 0);
            if (provider.LiveBuffers.Count != 0) throw new InvalidOperationException($"Consumer disposal retained {provider.LiveBuffers.Count} provider buffers before primary replacement.");
            Console.WriteLine($"Provider rendered Font, TextLine, wrapped TextParagraph, Label and RichText; glyphs={provider.GlyphDraws}, shaped draws={provider.ShapedDraws}, backend={backend}.");
        }
        finally { TextServerManager.SetPrimaryInterface(builtin); TextServerManager.RemoveInterface(provider); }
    }
    private static int Ink(Image pixels, Rect2i region)
    { var count = 0; for (var y = region.Position.Y; y < region.End.Y; y++) for (var x = region.Position.X; x < region.End.X; x++) { var color = pixels.GetPixel(x, y); if (color.R + color.G + color.B > 1.2f) count++; } return count; }
    private static void Reject(Action operation)
    { try { operation(); } catch (InvalidOperationException) { return; } throw new InvalidOperationException("An active provider buffer accepted mutation or retirement."); }
    private sealed class TextCanvas(Font font, TextLine line, TextParagraph paragraph) : Entity
    {
        protected override void OnDraw()
        { DrawString(font, new(10, 25), "Font provider", fontSize: 18); line.Draw(this, new(10, 50)); paragraph.Draw(this, new(10, 90)); }
    }
    private sealed class DrawingProbe : Probe
    {
        internal int GlyphDraws, ShapedDraws;
        internal bool FailBufferFree;
        internal readonly HashSet<RID> LiveBuffers = [];
        protected override RID OnCreateShapedText(TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)
        { var rid = base.OnCreateShapedText(direction, orientation); LiveBuffers.Add(rid); return rid; }
        protected override RID OnShapedTextSubstr(RID shaped, int start, int length)
        { var rid = base.OnShapedTextSubstr(shaped, start, length); LiveBuffers.Add(rid); return rid; }
        protected override RID OnShapedTextDuplicate(RID shaped)
        { var rid = base.OnShapedTextDuplicate(shaped); LiveBuffers.Add(rid); return rid; }
        protected override void OnFreeRID(RID rid) { base.OnFreeRID(rid); LiveBuffers.Remove(rid); if (FailBufferFree) { FailBufferFree = false; throw new ApplicationException("Controlled buffer retirement failure."); } }
        internal RID DrawingBuffer, GlyphSource;
        internal Action? DuringGlyph;
        protected override void OnShapedTextDraw(RID shaped, CanvasItem canvas, Vector2 position, float clipL = -1, float clipR = -1, Color? color = null, float oversampling = 0)
        { ShapedDraws++; DrawingBuffer = shaped; try { base.OnShapedTextDraw(shaped, canvas, position, clipL, clipR, color, oversampling); } finally { DrawingBuffer = default; } }
        protected override void OnFontDrawGlyph(RID fontRID, CanvasItem canvas, int size, Vector2 position, int index, Color? color = null, float oversampling = 0)
        { if (index == 0) throw new InvalidOperationException("A nonprinting glyph reached raster drawing."); GlyphDraws++; GlyphSource = fontRID; DuringGlyph?.Invoke(); base.OnFontDrawGlyph(fontRID, canvas, size, position, index, color, oversampling); }
    }
}
