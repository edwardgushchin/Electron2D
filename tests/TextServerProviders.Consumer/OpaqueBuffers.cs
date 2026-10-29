using System.Runtime.InteropServices;
using System.Text;
using Electron2D;

internal static class OpaqueBuffers
{
    internal static void Run()
    {
        var builtin = TextServerManager.GetPrimaryInterface(); using var provider = new SyntheticProvider();
        var rid = provider.CreateFont(); var font = provider.FontGetResource(rid);
        try
        {
            TextServerManager.SetPrimaryInterface(provider);
            Check(font.GetStringSize("AB", fontSize: 18) == new Vector2(12, 12), "Opaque font measurement uses the shared producer without a native face.");
            using var line = new TextLine(); line.AddString("AB", font, 18, "en", 73);
            Check(line.GetSize() == new Vector2(12, 12) && provider.LastMetadata == 73, "Opaque TextLine preserves source geometry and exact typed metadata.");
            var glyphs = provider.ShapedTextGetGlyphs(line.GetRID());
            Check(glyphs.Length == 2 && glyphs[0].FontRID == rid && glyphs[0].Start == 0 && glyphs[1].End == 2, "Canonical snapshots retain the opaque source and scalar ranges.");
            provider.CustomEditing = true;
            Check(provider.ShapedTextGetCarets(line.GetRID(), 1).TrailingRect.Position.X == 26, "Canonical caret metadata preserves the producer callback result.");
            Check(line.HitTest(1) == 2, "Typed line hits preserve the producer's valid position policy.");
            Check(provider.ShapedTextGetCharacterBreaks(line.GetRID()).SequenceEqual(new[] { 2 }) && provider.ShapedTextNextCharacterPos(line.GetRID(), 0) == 2 && provider.ShapedTextPrevCharacterPos(line.GetRID(), 2) == 0 && provider.ShapedTextClosestCharacterPos(line.GetRID(), 1) == 0, "Canonical character navigation uses producer boundary policy.");
            Check(provider.ShapedTextNextGraphemePos(line.GetRID(), 0) == 2 && provider.ShapedTextPrevGraphemePos(line.GetRID(), 2) == 0 && provider.ShapedTextGetGraphemeBounds(line.GetRID(), 1) == new Vector2(2, 9), "Canonical shaping-cluster navigation and bounds use producer metadata.");
            Check(provider.ShapedTextHitTestGrapheme(line.GetRID(), 1) == 1 && !provider.ShapedTextHasVisibleChars(line.GetRID()) && provider.ShapedTextGetDominantDirectionInRange(line.GetRID(), 0, 2) == TextDirection.RTL, "Canonical glyph hits, visibility and dominant direction preserve custom producer policy.");
            Check(provider.ShapedTextGetWordBreaks(line.GetRID()).SequenceEqual(new[] { 0, 1 }) && provider.ShapedTextGetLineBreaksAdv(line.GetRID(), [50], once: false).SequenceEqual(new[] { 0, 1, 1, 2 }), "Canonical word ranges and line breaks preserve producer policy.");
            Check(provider.ShapedGetRunCount(line.GetRID()) == 2 && provider.ShapedGetRunRange(line.GetRID(), 1) == new Vector2i(1, 2) && provider.ShapedGetRunGlyphRange(line.GetRID(), 1) == new Vector2i(1, 1) && provider.ShapedGetRunText(line.GetRID(), 1) == "B" && provider.ShapedGetRunLanguage(line.GetRID(), 1) == "producer" && provider.ShapedGetRunFontRID(line.GetRID(), 1) == rid && provider.ShapedGetRunFontSize(line.GetRID(), 1) == 18 && provider.ShapedGetRunObject(line.GetRID(), 1) is null, "Canonical visual run metadata preserves the producer's partition, text, language and font identity.");
            for (var i = 0; i < 128; i++) _ = provider.ShapedGetRunCount(line.GetRID()); var runsBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) _ = provider.ShapedGetRunCount(line.GetRID());
            var runBytes = GC.GetAllocatedBytesForCurrentThread() - runsBefore;
            Check(runBytes == 0, "Prepared provider run-count queries allocate no managed bytes: " + runBytes);
            var selection = provider.ShapedTextGetSelection(line.GetRID(), 0, 1);
            Check(selection.SequenceEqual(new Vector2[] { new(2, 9) }), "Canonical selection preserves producer intervals."); selection[0] = default;
            Check(provider.ShapedTextGetSelection(line.GetRID(), 0, 1)[0] == new Vector2(2, 9), "Public selection copies do not expose producer storage.");
            provider.BadSelection = true;
            try { provider.ShapedTextGetSelection(line.GetRID(), 0, 1); throw new InvalidOperationException("Nonfinite producer selection was accepted."); }
            catch (InvalidOperationException error) when (error.Message.Contains("nonfinite")) { }
            finally { provider.BadSelection = false; }
            provider.CustomEditing = false;
            using (var theme = new Theme { DefaultFont = font })
            using (var edit = new TextEdit { Theme = theme, Text = "AB", Size = new(200, 100) })
            {
                var nativeCaret = edit.GetPosAtLineColumn(0, 1); provider.CustomEditing = true;
                Check(edit.GetPosAtLineColumn(0, 1).X == nativeCaret.X + 20, "TextEdit caret placement uses producer caret metadata.");
                Check(edit.GetLineColumnAtPos(new(nativeCaret.X, nativeCaret.Y)).X == 2, "TextEdit pointer hits use the producer's position callback.");
                for (var i = 0; i < 128; i++) _ = edit.GetPosAtLineColumn(0, 1); var caretBefore = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 128; i++) _ = edit.GetPosAtLineColumn(0, 1);
                Check(GC.GetAllocatedBytesForCurrentThread() == caretBefore, "Prepared provider GUI caret queries allocate no managed bytes.");
                provider.CustomEditing = false;
            }
            var copies = provider.CopyCalls; var copied = provider.ShapedTextDuplicate(line.GetRID());
            try
            {
                Check(provider.ShapedTextGetWidth(copied) == 12, "A canonical opaque copy retains its producer width exactly once.");
                Check(provider.CopyCalls > copies, "Canonical duplication copies the producer's shaped buffer independently.");
            }
            finally { provider.FreeRID(copied); }
            var isolatedBefore = provider.IsolatedBShapes;
            var sliced = provider.ShapedTextSubstr(line.GetRID(), 1, 1);
            try { Check(provider.ShapedTextGetWidth(sliced) == 6 && provider.ShapedTextGetGlyphs(sliced).Single().Start == 1, "Canonical opaque slices retain shaped cluster data in original scalar coordinates."); }
            finally { provider.FreeRID(sliced); }
            Check(provider.IsolatedBShapes == isolatedBefore, "A substring retains its complete original shaping context without reshaping an isolated fragment.");
            CheckFormatting(provider, font);
            using (var failed = new TextLine())
            {
                failed.AddString("Failed preparation", font, 18); provider.FailShape = true; provider.DuringBufferFree = () => throw new ApplicationException("Controlled failed-layout retirement.");
                try
                {
                    Exception? observed = null; try { failed.GetSize(); } catch (Exception error) { observed = error; }
                    Check(observed is AggregateException errors && errors.Flatten().InnerExceptions.Any(error => error is InvalidDataException) && errors.Flatten().InnerExceptions.Any(error => error is ApplicationException), "A failed shape and its failed buffer retirement both survive preparation cleanup: " + observed);
                }
                finally { provider.FailShape = false; provider.DuringBufferFree = null; }
            }
            using (var ligature = new TextLine())
            {
                ligature.AddString("fiX", font, 18);
                Check(provider.ShapedTextGetGlyphs(ligature.GetRID()).SequenceEqual(SyntheticProvider.LigatureGlyphs(rid)), "Canonical snapshots preserve all eleven producer glyph fields, including a continuation, repeated negative advance and zero-repeat record.");
                var partial = provider.ShapedTextSubstr(ligature.GetRID(), 1, 1);
                try
                {
                    ligature.Dispose();
                    Check(provider.ShapedTextGetGlyphs(partial).Length == 0 && provider.ShapedTextGetWidth(partial) == 0, "A partial ligature slice has no copied glyph and survives its parent owner.");
                }
                finally { provider.FreeRID(partial); }
            }
            using (var parent = new TextLine())
            {
                parent.AddString("AB", font, 18); var independent = provider.ShapedTextDuplicate(parent.GetRID()); var child = provider.ShapedTextSubstr(parent.GetRID(), 1, 1);
                try
                {
                    parent.Dispose();
                    Check(provider.ShapedTextGetWidth(independent) == 12 && provider.ShapedTextGetWidth(child) == 6, "Owned opaque copies and contextual slices survive source owner disposal.");
                    Check(provider.ShapedTextGetCarets(child, 2).TrailingRect.Position.X == 6, "A contextual slice translates global scalar queries into its retained producer range.");
                }
                finally { provider.FreeRID(child); provider.FreeRID(independent); }
            }
            var nativeBuffer = provider.CreateShapedText();
            try
            {
                provider.ShapedTextAddString(nativeBuffer, "AB", [font], 18); provider.BadGlyph = true;
                try { provider.ShapedTextGetGlyphs(nativeBuffer); throw new InvalidOperationException("Invalid producer glyph was accepted."); }
                catch (InvalidOperationException error) when (error.Message.Contains("invalid shaped glyph")) { }
                finally { provider.BadGlyph = false; }
                Check(provider.ShapedTextGetGlyphs(nativeBuffer).Length == 2, "A rejected glyph callback releases its admission and preserves the live source.");
            }
            finally { provider.FreeRID(nativeBuffer); }
            using var vertical = new TextLine { Orientation = TextOrientation.Vertical, Direction = TextDirection.RTL }; vertical.AddString("AB", font, 18);
            Check(vertical.GetSize() == new Vector2(12, 12) && provider.ShapedTextGetGlyphs(vertical.GetRID())[0].Start == 1, "Vertical RTL consumers retain producer visual order.");
            using var paragraph = new TextParagraph { Width = 13, BreakFlags = TextLineBreakFlags.GraphemeBound }; paragraph.AddString("ABABAB", font, 18);
            Check(paragraph.GetLineCount() == 3 && paragraph.GetSize().Y == 36, "Opaque paragraph wrapping uses producer substrings and cross-axis metrics.");
            using var objects = new TextLine(); var key = new TextObjectKey(); objects.AddString("A", font, 18); objects.AddObject(key, new(8, 6)); objects.AddString("B", font, 18);
            Check(objects.GetLineWidth() == 20 && provider.LastObject == key && objects.GetObjectRect(key).Size == new Vector2(8, 6), "Opaque inline authoring preserves the original token and geometry.");
            provider.CustomObjectPosition = true;
            Check(provider.ShapedTextGetObjectRect(objects.GetRID(), key).Position == new Vector2(17, -4) && objects.GetObjectRect(key).Position == new Vector2(17, 5), "Canonical baseline and typed top-left inline geometry preserve producer object placement."); provider.CustomObjectPosition = false;
            using (var parentObjects = new TextLine())
            {
                var omitted = new TextObjectKey(); var retained = new TextObjectKey(); parentObjects.AddString("A", font, 18); parentObjects.AddObject(omitted, new(3, 2)); parentObjects.AddString("B", font, 18); parentObjects.AddObject(retained, new(9, 4));
                var child = provider.ShapedTextSubstr(parentObjects.GetRID(), 3, 1);
                try { parentObjects.Dispose(); Check(provider.ShapedTextGetObjectRect(child, retained).Size == new Vector2(9, 4), "A contextual object slice maps its local object index to the retained original token after parent disposal."); }
                finally { provider.FreeRID(child); }
            }
            using var variation = font.FindVariation(spacingTop: 2); using var varied = new TextLine(); varied.AddString("AB", variation, 18);
            Check(varied.GetLineAscent() == 11, "Opaque variations reach producer line metrics through their independent source.");
            for (var i = 0; i < 128; i++) _ = line.GetSize(); var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) _ = line.GetSize();
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared opaque line metrics allocate no managed bytes.");
            Check(provider.LiveBuffers > 0, "Prepared opaque consumers own actual producer buffers before source invalidation.");
            provider.ChangeNameDirect(rid, "Changed opaque source");
            Check(provider.LiveAuthoredBuffers == 0, "A source change explicitly retires dependent prepared producer buffers: " + provider.BufferSummary);
            Check(line.GetSize() == new Vector2(12, 12), "A live consumer rebuilds from the changed source after dependent buffer retirement.");
            line.Dispose(); vertical.Dispose(); paragraph.Dispose(); objects.Dispose(); varied.Dispose(); variation.Dispose(); font.SetCacheCapacity(0, 0);
            Check(provider.LiveBuffers == 0, "Opaque consumers release every producer buffer before primary replacement.");
            Console.WriteLine("Opaque font/buffer producer drives Font, typed TextLine, vertical RTL, wrapping, inline objects and independent variations; 128 prepared line reads 0 B.");
        }
        finally { TextServerManager.SetPrimaryInterface(builtin); provider.FreeRID(rid); }
    }
    private static void CheckFormatting(SyntheticProvider provider, Font font)
    {
        using var line = new TextLine(); line.AddString("A A A", font, 18);
        var rid = line.GetRID(); var fits = provider.FitCalls;
        Check(provider.ShapedTextFitToWidth(rid, 60, TextJustificationFlags.WordBound) == 60 && provider.FitCalls > fits, "Canonical fitting uses the producer's justification opportunities.");
        var trim = TextOverrunFlags.Trim | TextOverrunFlags.AddEllipsis | TextOverrunFlags.ShortStringEllipsis;
        provider.ShapedTextOverrunTrimToWidth(rid, 48, trim);
        Check(line.GetLineWidth() == 39 && provider.ShapedTextGetGlyphs(rid).Length == 5 && provider.ShapedTextGetEllipsisGlyphs(rid).Length == 1, "Trimming preserves raw clusters and the producer's independent ellipsis display.");
        var duplicate = provider.ShapedTextDuplicate(rid);
        try
        {
            Check(provider.ShapedTextGetWidth(duplicate) == 39, "Duplication preserves producer formatting.");
            Check(provider.ShapedTextFitToWidth(rid, 40, TextJustificationFlags.WordBound | TextJustificationFlags.ConstrainEllipsis) == 40 && line.GetLineWidth() == 40, "Constrained fitting changes only the producer's visible range.");
            Check(provider.ShapedTextGetWidth(duplicate) == 39, "Formatting a source does not mutate its independent duplicate.");
            provider.ShapedTextOverrunTrimToWidth(rid);
            Check(line.GetLineWidth() == 61, "Resetting trim exposes the complete fitted producer advances.");
            using var paragraph = new TextParagraph { Width = -1 }; paragraph.AddString("A A A", font, 18);
            provider.ShapedTextFitToWidth(paragraph.GetRID(), 60, TextJustificationFlags.WordBound);
            Check(paragraph.GetLineWidth(0) == 60, "Paragraph wrapping retains explicitly fitted producer advances.");
        }
        finally { provider.FreeRID(duplicate); }
        var active = provider.LiveBuffers; line.Clear();
        Check(provider.LiveBuffers < active && line.GetLineWidth() == 0, "Clearing a formatted line retires its provider workspaces.");
        using var signed = new TextLine(); signed.AddString("A", font, 18); provider.ShapedTextSetSpacing(signed.GetRID(), TextSpacingType.Top, -20);
        Check(signed.GetLineAscent() == -11, "Signed buffer spacing preserves finite producer metrics below zero.");
        var opaque = provider.CreateShapedText();
        try
        {
            fits = provider.FitCalls;
            try { provider.ShapedTextFitToWidth(opaque, float.NaN); throw new InvalidOperationException("Nonfinite fit width was accepted."); } catch (ArgumentOutOfRangeException) { }
            Check(provider.FitCalls == fits, "Invalid formatting inputs are rejected before entering a custom callback.");
        }
        finally { provider.FreeRID(opaque); }
        var cleanup = new TextLine(); cleanup.AddString("A A", font, 18); provider.ShapedTextFitToWidth(cleanup.GetRID(), 30);
        var beforeCleanup = provider.LiveBuffers; var freed = 0; provider.DuringBufferFree = () =>
        {
            freed++;
            try { cleanup.Clear(); throw new InvalidOperationException("Cleanup reentered its active owner."); } catch (InvalidOperationException error) when (error.Message.Contains("active text buffer", StringComparison.OrdinalIgnoreCase)) { }
            if (freed == 1) throw new InvalidOperationException("Fixture cleanup failure.");
        };
        try
        {
            try { cleanup.Clear(); throw new InvalidOperationException("Cleanup failure was lost."); } catch (AggregateException error) when (error.ToString().Contains("Fixture cleanup failure")) { }
            Check(freed > 1 && provider.LiveBuffers == beforeCleanup - freed, "A failing retirement still releases every formatted workspace exactly once.");
        }
        finally { provider.DuringBufferFree = null; cleanup.Dispose(); }
    }
    internal static void Render(string backend, string output)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var builtin = TextServerManager.GetPrimaryInterface(); using var provider = new SyntheticProvider(); var rid = provider.CreateFont(); var font = provider.FontGetResource(rid);
        TextServerManager.SetPrimaryInterface(provider);
        try
        {
            using var line = new TextLine(); line.AddString("ABAB", font, 18);
            using var vertical = new TextLine { Orientation = TextOrientation.Vertical, Direction = TextDirection.RTL }; vertical.AddString("ABAB", font, 18);
            using var paragraph = new TextParagraph { Width = 13, BreakFlags = TextLineBreakFlags.GraphemeBound }; paragraph.AddString("ABABAB", font, 18);
            using var root = new Window { Size = new(220, 180), OversamplingOverride = 2 }; var canvas = new OpaqueCanvas(font, line, vertical, paragraph); root.AddChild(canvas);
            var label = new Label { Position = new(100, 10), Size = new(100, 30), Text = "ABAB" }; label.AddThemeFontOverride("font", font); root.AddChild(label);
            var rich = new RichTextLabel { Position = new(100, 50), Size = new(100, 60), Text = "ABAB", Threaded = false }; rich.AddThemeFontOverride("normal_font", font); root.AddChild(rich);
            var checkedCallback = false;
            provider.DuringScale = () =>
            {
                var registry = Task.Run(TextServerManager.GetInterfaces);
                Check(registry.Wait(TimeSpan.FromSeconds(3)), "Raster reference callbacks hold no registry lock.");
                try { TextServerManager.SetPrimaryInterface(builtin); throw new InvalidOperationException("Raster notification replaced its active implementation."); } catch (InvalidOperationException error) when (error.Message.Contains("active consumer read")) { }
            };
            provider.DuringGlyph = () => { if (checkedCallback) return; checkedCallback = true; Check(provider.ScaleReferences.GetValueOrDefault(2) > 0, "Viewport scale ownership reaches the producer before glyph rasterization."); var metadata = Task.Run(font.GetFontName); Check(metadata.Wait(TimeSpan.FromSeconds(3)) && metadata.Result == "Opaque", "Opaque raster callbacks hold no font monitor across metadata work."); };
            var frames = 0;
            Action rendered = () =>
            {
                using var pixels = root.GetTexture().GetImage() ?? throw new InvalidOperationException("Opaque rendered readback is unavailable.");
                foreach (var region in new[] { new Rect2i(5, 5, 40, 23), new(5, 32, 40, 23), new(55, 32, 20, 40), new(5, 68, 30, 50), new(95, 5, 100, 35), new(95, 45, 100, 65) })
                { var ink = 0; for (var y = region.Position.Y; y < region.End.Y; y++) for (var x = region.Position.X; x < region.End.X; x++) if (pixels.GetPixel(x, y).R > .8f) ink++; Check(ink > 8, "An opaque rendered consumer produced no visible glyph pixels: " + region); }
                if (++frames < 2) { root.OversamplingOverride = 3; canvas.QueueRedraw(); label.QueueRedraw(); rich.QueueRedraw(); return; }
                Check(provider.ScaleReferences.GetValueOrDefault(2) == 0 && provider.ScaleReferences.GetValueOrDefault(3) > 0, "Scale changes retire the previous producer cache level.");
                using (var replacement = new SyntheticProvider())
                {
                    TextServerManager.SetPrimaryInterface(replacement);
                    Check(replacement.ScaleReferences.GetValueOrDefault(3) > 0, "A selected replacement receives existing viewport references.");
                    TextServerManager.SetPrimaryInterface(builtin); replacement.Dispose();
                    Check(replacement.ScaleReferences.Values.All(count => count == 0), "Disposal balances a detached implementation's retained raster references.");
                }
                File.WriteAllBytes(output, pixels.SavePNGToBuffer()); root.Tree!.Quit();
            };
            root.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += rendered; };
            try { Engine.Run(root); } finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= rendered; }
            Check(checkedCallback && provider.GlyphDraws > 0, "Opaque buffers and fonts reached actual canvas raster callbacks.");
            root.Dispose(); line.Dispose(); vertical.Dispose(); paragraph.Dispose(); font.SetCacheCapacity(0, 0);
            Check(provider.ScaleReferences.Values.All(count => count == 0), "Viewport disposal releases the original opaque creator's raster references after selection changes.");
            Check(provider.LiveBuffers == 0, "Opaque rendered consumers retire their source buffers before provider replacement.");
            Console.WriteLine($"Opaque producer rendered Font, TextLine, vertical RTL, paragraph, Label and RichText; glyphs={provider.GlyphDraws}, backend={backend}.");
        }
        finally { TextServerManager.SetPrimaryInterface(builtin); provider.FreeRID(rid); }
    }
    private sealed class OpaqueCanvas(Font font, TextLine line, TextLine vertical, TextParagraph paragraph) : Entity
    {
        protected override void OnDraw() { DrawString(font, new(10, 20), "ABAB", fontSize: 18); line.Draw(this, new(10, 36)); vertical.Draw(this, new(60, 36)); paragraph.Draw(this, new(10, 70)); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

internal sealed class SyntheticProvider : OpaqueProbe
{
    private sealed record Metadata<T>(T Value);
    private sealed record Span(int Start, int End, Font Font, int Size, object? Metadata);
    private sealed record Inline(TextObjectKey Key, int Start, int Length, Vector2 Size);
    private sealed class Buffer
    {
        internal string Text = "";
        internal int Start, End;
        internal TextDirection Direction;
        internal TextOrientation Orientation;
        internal bool PreserveControl, PreserveInvalid = true, Ready;
        internal string Punctuation = "";
        internal int Ellipsis = 0x2026;
        internal int[] Spacing = new int[4];
        internal TextBIDIRange[] Contexts = [];
        internal float[] Tabs = [];
        internal readonly List<Span> Spans = [];
        internal readonly List<Inline> Objects = [];
        internal readonly List<TextGlyph> Glyphs = [];
        internal readonly List<TextGlyph> Ending = [];
        internal int Trim = -1, EllipsisPosition = -1;
        internal readonly Vector2[] Selection = new Vector2[1];
        internal float RawWidth => Glyphs.Sum(glyph => glyph.Advance * glyph.Repeat);
        internal bool RTL => Direction == TextDirection.RTL;
        internal bool Visible(int index) => Trim < 0 || (RTL ? index >= Trim : index < Trim);
        internal float Width => Glyphs.Where((_, index) => Visible(index)).Sum(glyph => glyph.Advance * glyph.Repeat) + Ending.Sum(glyph => glyph.Advance * glyph.Repeat);
        internal Buffer Copy(int start, int end)
        {
            var copy = new Buffer { Text = Text, Start = start, End = end, Direction = Direction, Orientation = Orientation, PreserveControl = PreserveControl, PreserveInvalid = PreserveInvalid, Punctuation = Punctuation, Ellipsis = Ellipsis, Spacing = (int[])Spacing.Clone(), Contexts = (TextBIDIRange[])Contexts.Clone(), Tabs = (float[])Tabs.Clone(), Ready = true };
            copy.Spans.AddRange(Spans); copy.Objects.AddRange(Objects.Where(item => item.Start >= start && item.Start + item.Length <= end)); copy.Glyphs.AddRange(Glyphs.Where(glyph => glyph.Start >= start && glyph.End <= end)); return copy;
        }
    }
    private readonly Dictionary<RID, Buffer> _buffers = [];
    internal int LiveBuffers => _buffers.Count;
    internal int LiveAuthoredBuffers => _buffers.Values.Count(buffer => buffer.Spans.Count > 0);
    internal string BufferSummary => string.Join("; ", _buffers.Values.Select(buffer => $"{buffer.Text}:{buffer.Start}-{buffer.End}:{buffer.Spans.FirstOrDefault()?.Font.GetType().Name}"));
    internal int LastMetadata;
    internal TextObjectKey? LastObject;
    internal int GlyphDraws, CopyCalls, IsolatedBShapes, FitCalls, TrimCalls;
    internal bool CustomEditing, BadSelection, BadGlyph, FailShape, CustomObjectPosition;
    private readonly TextGlyph[] _invalidGlyph = [new(0, 1, 1, 2, TextGraphemeFlags.Valid, default, 6, default, 18, 65, 0)];
    internal Action? DuringGlyph, DuringBufferFree, DuringScale;
    internal readonly Dictionary<float, int> ScaleReferences = [];
    protected override void OnReferenceOversamplingLevel(float oversampling)
    { ScaleReferences[oversampling] = ScaleReferences.GetValueOrDefault(oversampling) + 1; DuringScale?.Invoke(); }
    protected override void OnUnreferenceOversamplingLevel(float oversampling)
    { ScaleReferences[oversampling] = ScaleReferences.GetValueOrDefault(oversampling) - 1; if (ScaleReferences[oversampling] < 0) throw new InvalidOperationException("A raster scale reference retired more than once."); DuringScale?.Invoke(); }
    protected override string OnGetName() => "Synthetic opaque producer";
    protected override Dictionary<string, int> OnFontGetOpenTypeFeatureOverrides(RID fontRID) => [];
    protected override bool OnFontHasChar(RID fontRID, int character) => character >= 32;
    protected override int OnFontGetGlyphIndex(RID fontRID, int size, int character, int variationSelector) => character >= 32 ? character : 0;
    protected override RID OnCreateShapedText(TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)
    { var rid = AllocateShapedTextRID(); _buffers.Add(rid, new() { Direction = direction, Orientation = orientation }); return rid; }
    protected override void OnFreeRID(RID rid)
    { var buffer = _buffers.Remove(rid); if (!buffer && !IsProbeFont(rid)) throw new InvalidOperationException("An opaque cleanup callback received a managed facade identity."); base.OnFreeRID(rid); if (buffer) DuringBufferFree?.Invoke(); }
    protected override void OnShapedTextClear(RID shaped)
    { var old = _buffers[shaped]; _buffers[shaped] = new() { Direction = old.Direction, Orientation = old.Orientation }; }
    protected override void OnShapedTextSetDirection(RID shaped, TextDirection direction) { var b = _buffers[shaped]; b.Direction = direction; b.Ready = false; }
    protected override TextDirection OnShapedTextGetDirection(RID shaped) => _buffers[shaped].Direction;
    protected override TextDirection OnShapedTextGetInferredDirection(RID shaped) => _buffers[shaped].Direction == TextDirection.RTL ? TextDirection.RTL : TextDirection.LTR;
    protected override void OnShapedTextSetOrientation(RID shaped, TextOrientation orientation) => _buffers[shaped].Orientation = orientation;
    protected override TextOrientation OnShapedTextGetOrientation(RID shaped) => _buffers[shaped].Orientation;
    protected override void OnShapedTextSetPreserveControl(RID shaped, bool enabled) => _buffers[shaped].PreserveControl = enabled;
    protected override bool OnShapedTextGetPreserveControl(RID shaped) => _buffers[shaped].PreserveControl;
    protected override void OnShapedTextSetPreserveInvalid(RID shaped, bool enabled) => _buffers[shaped].PreserveInvalid = enabled;
    protected override bool OnShapedTextGetPreserveInvalid(RID shaped) => _buffers[shaped].PreserveInvalid;
    protected override void OnShapedTextSetCustomPunctuation(RID shaped, string punctuation) => _buffers[shaped].Punctuation = punctuation;
    protected override string OnShapedTextGetCustomPunctuation(RID shaped) => _buffers[shaped].Punctuation;
    protected override void OnShapedTextSetCustomEllipsis(RID shaped, int character) => _buffers[shaped].Ellipsis = character;
    protected override int OnShapedTextGetCustomEllipsis(RID shaped) => _buffers[shaped].Ellipsis;
    protected override void OnShapedTextSetSpacing(RID shaped, TextSpacingType spacing, int value) => _buffers[shaped].Spacing[(int)spacing] = value;
    protected override int OnShapedTextGetSpacing(RID shaped, TextSpacingType spacing) => _buffers[shaped].Spacing[(int)spacing];
    protected override void OnShapedTextSetBIDIOverride(RID shaped, ReadOnlySpan<TextBIDIRange> ranges) => _buffers[shaped].Contexts = ranges.ToArray();
    protected override float OnShapedTextTabAlign(RID shaped, ReadOnlySpan<float> tabStops) { _buffers[shaped].Tabs = tabStops.ToArray(); return OnShapedTextGetWidth(shaped); }
    protected override bool OnShapedTextAddString(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures = null, string language = "") => Add(shaped, text, fonts, size, null);
    protected override bool OnShapedTextAddString<T>(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures, string language, T metadata)
    { if (metadata is int value) LastMetadata = value; return Add(shaped, text, fonts, size, new Metadata<T>(metadata)); }
    private bool Add(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, object? metadata)
    { var b = _buffers[shaped]; var count = text.EnumerateRunes().Count(); b.Spans.Add(new(b.End, b.End + count, fonts[0], size, metadata)); b.Text += text; b.End += count; b.Ready = false; return true; }
    protected override bool OnShapedTextAddObject(RID shaped, TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, int length = 1, float baseline = 0)
    { var b = _buffers[shaped]; if (b.Objects.Any(item => ReferenceEquals(item.Key, key))) return false; LastObject = key; b.Objects.Add(new(key, b.End, length, size)); b.Text += new string('\ufffc', length); b.End += length; b.Ready = false; return true; }
    protected override bool OnShapedTextShape(RID shaped)
    {
        if (FailShape) throw new InvalidDataException("Controlled opaque shape failure.");
        var buffer = _buffers[shaped]; return buffer.Ready || Shape(buffer);
    }
    private bool Shape(Buffer b)
    {
        // Captured authoring locals belong to the cold path, so ready callbacks stay allocation-free.
        if (b.Text == "B") IsolatedBShapes++; b.Glyphs.Clear(); var at = 0; var runes = b.Text.EnumerateRunes().ToArray();
        if (b.Text == "fiX") { b.Glyphs.AddRange(LigatureGlyphs(b.Spans[0].Font.GetRID())); b.Ready = true; return true; }
        while (at < runes.Length)
        {
            var item = b.Objects.FirstOrDefault(value => value.Start == at);
            if (item is not null) { b.Glyphs.Add(new(at, at + item.Length, 1, 1, TextGraphemeFlags.EmbeddedObject | TextGraphemeFlags.Valid, default, b.Orientation == TextOrientation.Horizontal ? item.Size.X : item.Size.Y, default, 18, 0, -1)); at += item.Length; continue; }
            var spanIndex = b.Spans.FindIndex(span => at >= span.Start && at < span.End); var span = b.Spans[spanIndex]; var flags = TextGraphemeFlags.Valid;
            if (b.Direction == TextDirection.RTL) flags |= TextGraphemeFlags.RTL;
            if (runes[at].Value == ' ') flags |= TextGraphemeFlags.Space;
            var nonprinting = runes[at].Value == 0x200b;
            b.Glyphs.Add(new(at, at + 1, 1, 1, nonprinting ? flags | TextGraphemeFlags.Virtual : flags, default, nonprinting ? 0 : 6 + b.Spacing[(int)TextSpacingType.Glyph], span.Font.GetRID(), span.Size, nonprinting ? 0 : runes[at].Value, spanIndex)); at++;
        }
        if (b.Direction == TextDirection.RTL) b.Glyphs.Reverse(); b.Ready = true; return true;
    }
    internal static TextGlyph[] LigatureGlyphs(RID font) =>
    [
        new(0, 2, 1, 2, TextGraphemeFlags.Valid | TextGraphemeFlags.Connected, new(1, 2), 7, font, 18, 65, 0),
        new(0, 2, 2, 0, TextGraphemeFlags.SafeToInsertTatweel, new(-2, 1), -1, font, 18, 66, 0),
        new(2, 3, 0, 1, TextGraphemeFlags.Valid, new(4, -2), 11, default, 0, 67, 0)
    ];
    protected override bool OnShapedTextIsReady(RID shaped) => _buffers[shaped].Ready;
    protected override int OnShapedTextGetGlyphCount(RID shaped) { OnShapedTextShape(shaped); return _buffers[shaped].Glyphs.Count; }
    protected override ReadOnlySpan<TextGlyph> OnShapedTextGetGlyphs(RID shaped) { OnShapedTextShape(shaped); return BadGlyph ? _invalidGlyph : CollectionsMarshal.AsSpan(_buffers[shaped].Glyphs); }
    protected override ReadOnlySpan<TextGlyph> OnShapedTextGetEllipsisGlyphs(RID shaped) => CollectionsMarshal.AsSpan(_buffers[shaped].Ending);
    protected override int OnShapedTextGetTrimPos(RID shaped) => _buffers[shaped].Trim;
    protected override int OnShapedTextGetEllipsisPos(RID shaped) => _buffers[shaped].EllipsisPosition;
    protected override float OnShapedTextGetWidth(RID shaped) { OnShapedTextShape(shaped); return _buffers[shaped].Width; }
    protected override float OnShapedTextGetAscent(RID shaped) { var b = _buffers[shaped]; return (b.Spans.FirstOrDefault()?.Font.GetAscent(18) ?? 9) + b.Spacing[(int)TextSpacingType.Top]; }
    protected override float OnShapedTextGetDescent(RID shaped) => 3 + _buffers[shaped].Spacing[(int)TextSpacingType.Bottom];
    protected override float OnShapedTextGetUnderlinePosition(RID shaped) => 2;
    protected override float OnShapedTextGetUnderlineThickness(RID shaped) => 1;
    protected override Vector2i OnShapedTextGetRange(RID shaped) { var b = _buffers[shaped]; return new(b.Start, b.End); }
    protected override Rect2 OnShapedTextGetObjectRect(RID shaped, TextObjectKey key)
    { var b = _buffers[shaped]; var item = b.Objects.First(value => ReferenceEquals(value.Key, key)); var position = CustomObjectPosition ? new Vector2(17, -4) : b.Orientation == TextOrientation.Horizontal ? new Vector2((item.Start - b.Start) * 6, -item.Size.Y) : new Vector2(-item.Size.X, (item.Start - b.Start) * 6); return new(position, item.Size); }
    protected override int OnShapedGetRunCount(RID shaped)
    { OnShapedTextShape(shaped); var b = _buffers[shaped]; return b.Glyphs.Count == 0 ? 0 : CustomEditing ? b.Glyphs.Count : 1; }
    protected override Vector2i OnShapedGetRunRange(RID shaped, int index)
    { var b = _buffers[shaped]; return CustomEditing ? new(b.Glyphs[index].Start, b.Glyphs[index].End) : new(b.Start, b.End); }
    protected override Vector2i OnShapedGetRunGlyphRange(RID shaped, int index)
    { var b = _buffers[shaped]; return CustomEditing ? new(index, index) : new(0, b.Glyphs.Count - 1); }
    protected override RID OnShapedGetRunFontRID(RID shaped, int index) => _buffers[shaped].Spans[0].Font.GetRID();
    protected override int OnShapedGetRunFontSize(RID shaped, int index) => _buffers[shaped].Spans[0].Size;
    protected override TextDirection OnShapedGetRunDirection(RID shaped, int index) => _buffers[shaped].RTL ? TextDirection.RTL : TextDirection.LTR;
    protected override string OnShapedGetRunLanguage(RID shaped, int index) => CustomEditing ? "producer" : "";
    protected override TextObjectKey? OnShapedGetRunObject(RID shaped, int index) => null;
    protected override string OnShapedGetRunText(RID shaped, int index)
    { var b = _buffers[shaped]; var range = OnShapedGetRunRange(shaped, index); return string.Concat(b.Text.EnumerateRunes().Skip(range.X).Take(range.Y - range.X).Select(rune => rune.ToString())); }
    protected override RID OnShapedTextDuplicate(RID shaped)
    { var b = _buffers[shaped]; var rid = Copy(b, b.Start, b.End); var copy = _buffers[rid]; copy.Trim = b.Trim; copy.EllipsisPosition = b.EllipsisPosition; copy.Ending.AddRange(b.Ending); return rid; }
    protected override RID OnShapedTextSubstr(RID shaped, int start, int length) => Copy(_buffers[shaped], start, start + length);
    private RID Copy(Buffer source, int start, int end)
    { CopyCalls++; var rid = AllocateShapedTextRID(); _buffers.Add(rid, source.Copy(start, end)); return rid; }
    protected override int[] OnShapedTextGetLineBreaksAdv(RID shaped, ReadOnlySpan<float> width, int start = 0, bool once = true, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound)
    {
        var b = _buffers[shaped]; var step = CustomEditing ? 1 : width[0] <= 0 ? Math.Max(1, b.End - start) : Math.Max(1, (int)(width[0] / 6)); var ranges = new List<int>();
        for (var at = Math.Max(start, b.Start); at < b.End; at += step) { ranges.Add(at); ranges.Add(Math.Min(b.End, at + step)); if (once) break; }
        return ranges.ToArray();
    }
    protected override int[] OnShapedTextGetWordBreaks(RID shaped, TextGraphemeFlags graphemeFlags = TextGraphemeFlags.Space | TextGraphemeFlags.Punctuation, TextGraphemeFlags skipGraphemeFlags = TextGraphemeFlags.Virtual)
    { var b = _buffers[shaped]; return b.Start == b.End ? [] : CustomEditing ? [b.Start, Math.Min(b.Start + 1, b.End)] : [b.Start, b.End]; }
    protected override void OnShapedTextOverrunTrimToWidth(RID shaped, float width = 0, TextOverrunFlags overrunTrimFlags = TextOverrunFlags.NoTrim)
    {
        TrimCalls++; OnShapedTextShape(shaped); var b = _buffers[shaped]; b.Trim = b.EllipsisPosition = -1; b.Ending.Clear();
        if (!overrunTrimFlags.HasFlag(TextOverrunFlags.Trim) || width <= 0 || b.RawWidth <= width && !overrunTrimFlags.HasFlag(TextOverrunFlags.EnforceEllipsis)) return;
        var add = overrunTrimFlags.HasFlag(TextOverrunFlags.AddEllipsis); var available = width - (add ? 6 : 0); var take = 0; var advance = 0f;
        while (take < b.Glyphs.Count)
        { var glyph = b.Glyphs[b.RTL ? b.Glyphs.Count - take - 1 : take]; if (advance + glyph.Advance * glyph.Repeat > available) break; advance += glyph.Advance * glyph.Repeat; take++; }
        b.Trim = b.RTL ? b.Glyphs.Count - take : take;
        if (add && b.Glyphs.Count > 0) { b.EllipsisPosition = b.Trim; b.Ending.Add(b.Glyphs[0] with { Start = -1, End = -1, Advance = 6, Index = b.Ellipsis, Flags = TextGraphemeFlags.Valid }); }
    }
    protected override float OnShapedTextFitToWidth(RID shaped, float width, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound)
    {
        FitCalls++; OnShapedTextShape(shaped); var b = _buffers[shaped]; var constrained = justificationFlags.HasFlag(TextJustificationFlags.ConstrainEllipsis);
        var spaces = b.Glyphs.Where((glyph, index) => glyph.Flags.HasFlag(TextGraphemeFlags.Space) && (!constrained || b.Visible(index))).Count();
        if (spaces > 0 && justificationFlags.HasFlag(TextJustificationFlags.WordBound))
        { var delta = (width - (constrained ? b.Width : b.RawWidth)) / spaces; for (var i = 0; i < b.Glyphs.Count; i++) if (b.Glyphs[i].Flags.HasFlag(TextGraphemeFlags.Space) && (!constrained || b.Visible(i))) b.Glyphs[i] = b.Glyphs[i] with { Advance = b.Glyphs[i].Advance + delta }; }
        return constrained ? b.Width : b.RawWidth;
    }
    protected override TextCaretInfo OnShapedTextGetCarets(RID shaped, int position)
    {
        var b = _buffers[shaped]; var x = (position - b.Start) * 6 + (CustomEditing ? 20 : 0);
        return new TextCaretInfo(new(x, -9, -6, 12), TextDirection.LTR, new(x, -9, 6, 12), TextDirection.LTR);
    }
    protected override ReadOnlySpan<Vector2> OnShapedTextGetSelection(RID shaped, int start, int end)
    {
        var b = _buffers[shaped]; if (start == end) return [];
        b.Selection[0] = BadSelection ? new(float.NaN, 9) : CustomEditing ? new(2, 9) : new((Math.Min(start, end) - b.Start) * 6, (Math.Max(start, end) - b.Start) * 6);
        return b.Selection;
    }
    protected override int OnShapedTextHitTestPosition(RID shaped, float coords)
    { var b = _buffers[shaped]; return CustomEditing ? b.End : Math.Clamp(b.Start + (int)MathF.Round(coords / 6), b.Start, b.End); }
    protected override int[] OnShapedTextGetCharacterBreaks(RID shaped)
    { var b = _buffers[shaped]; return CustomEditing ? [b.End] : Enumerable.Range(b.Start + 1, b.End - b.Start).ToArray(); }
    protected override int OnShapedTextClosestCharacterPos(RID shaped, int position)
    { var b = _buffers[shaped]; return CustomEditing ? (long)position - b.Start <= (long)b.End - position ? b.Start : b.End : Math.Clamp(position, b.Start, b.End); }
    protected override int OnShapedTextNextCharacterPos(RID shaped, int position)
    { var b = _buffers[shaped]; return CustomEditing ? b.End : (int)Math.Clamp((long)position + 1, b.Start, b.End); }
    protected override int OnShapedTextPrevCharacterPos(RID shaped, int position)
    { var b = _buffers[shaped]; return CustomEditing ? b.Start : (int)Math.Clamp((long)position - 1, b.Start, b.End); }
    protected override int OnShapedTextNextGraphemePos(RID shaped, int position)
    { if (CustomEditing) return _buffers[shaped].End; OnShapedTextShape(shaped); return _buffers[shaped].Glyphs.FirstOrDefault(glyph => position >= glyph.Start && position < glyph.End) is { End: > 0 } found ? found.End : position; }
    protected override int OnShapedTextPrevGraphemePos(RID shaped, int position)
    { if (CustomEditing) return _buffers[shaped].Start; OnShapedTextShape(shaped); return _buffers[shaped].Glyphs.FirstOrDefault(glyph => position > glyph.Start && position <= glyph.End) is { End: > 0 } found ? found.Start : position; }
    protected override Vector2 OnShapedTextGetGraphemeBounds(RID shaped, int position)
    { var b = _buffers[shaped]; return CustomEditing ? new(2, 9) : new((position - b.Start) * 6, (position - b.Start + 1) * 6); }
    protected override int OnShapedTextHitTestGrapheme(RID shaped, float coords)
    {
        OnShapedTextShape(shaped); var b = _buffers[shaped]; if (CustomEditing) return b.Glyphs.Count >= 2 ? 1 : -1; var pen = 0f;
        for (var i = 0; i < b.Glyphs.Count; i++) { var advance = b.Glyphs[i].Advance * b.Glyphs[i].Repeat; if (coords >= pen && coords < pen + advance) return i; pen += advance; }
        return -1;
    }
    protected override bool OnShapedTextHasVisibleChars(RID shaped)
    { OnShapedTextShape(shaped); return !CustomEditing && _buffers[shaped].Glyphs.Any(glyph => glyph.Index != 0 && !glyph.Flags.HasFlag(TextGraphemeFlags.Virtual)); }
    protected override TextDirection OnShapedTextGetDominantDirectionInRange(RID shaped, int start, int end)
    { var b = _buffers[shaped]; return CustomEditing || b.RTL ? TextDirection.RTL : TextDirection.LTR; }
    protected override void OnFontDrawGlyph(RID fontRID, CanvasItem canvas, int size, Vector2 position, int index, Color? color = null, float oversampling = 0)
    { GlyphDraws++; DuringGlyph?.Invoke(); canvas.DrawRect(new(position + new Vector2(0, -8), new(4, 8)), color ?? Colors.White); }
}
