using Electron2D;

internal static class FontTests
{
    internal static void Run(byte[] openSansData, byte[] arabicData)
    {
        TextLayoutBreakTests.Run();
        VerifyDefaultsAndGuards();
        VerifyMeasurement(openSansData);
        VerifyLayout(openSansData, arabicData);
        VerifyFallbacksAndCopies(openSansData, arabicData);
        VerifyDrawing(openSansData);
        VerifyReuse(openSansData);
        Console.WriteLine("Font resources verify measured shaping, fallback lifetimes, bidi, line formatting, all six drawing entrypoints and warmed layout reuse.");
    }

    private static FontFile Load(byte[] data) => new() { Data = data, SubpixelPositioning = FontSubpixelPositioning.Quarter };

    private static void VerifyDefaultsAndGuards()
    {
        using var empty = new FontFile { AllowSystemFallback = false };
        Check(empty.Fallbacks.Length == 0 && empty.GetHeight() == 0 && empty.GetAscent() == 0 && empty.GetDescent() == 0 &&
            empty.GetFontName() == string.Empty && empty.GetFontStyleName() == string.Empty && empty.GetFontWeight() == 400 && empty.GetFontStretch() == 100 &&
            empty.GetFontStyle() == FontStyle.None && empty.GetFaceCount() == 0 && empty.GetSupportedChars() == string.Empty && !empty.HasChar('A') && empty.GetCharSize('A', 16) == Vector2.Zero,
            "An empty font has no native face, characters or metrics and preserves intrinsic metadata defaults.");
        Check(empty.GetStringSize("") == Vector2.Zero && empty.GetMultilineStringSize("") == Vector2.Zero,
            "Empty text has zero shaped extent rather than reserving a line-height estimate.");
        Check(empty.GetStringSize("\U0010FFFD") == new Vector2(16, 15), "Missing characters use the source hexadecimal-box advance and height even without a loaded face.");
        Reject<ArgumentNullException>(() => empty.Fallbacks = null!);
        Reject<ArgumentException>(() => empty.Fallbacks = [empty]);
        Reject<ArgumentOutOfRangeException>(() => empty.HasChar(0xD800));
        Reject<ArgumentOutOfRangeException>(() => empty.HasChar(0x110000));
        Reject<ArgumentOutOfRangeException>(() => empty.GetHeight(0));
        Reject<ArgumentOutOfRangeException>(() => empty.GetSpacing(TextSpacingType.Max));
        Reject<ArgumentNullException>(() => empty.GetStringSize(null!));
        Reject<ArgumentOutOfRangeException>(() => empty.GetStringSize("a", width: float.NaN));
        Reject<ArgumentOutOfRangeException>(() => empty.GetStringSize("a", alignment: (HorizontalAlignment)99));
        Reject<ArgumentOutOfRangeException>(() => empty.GetStringSize("a", direction: (TextDirection)99));
        Reject<ArgumentOutOfRangeException>(() => empty.GetStringSize("a", orientation: (TextOrientation)99));
        Reject<ArgumentOutOfRangeException>(() => empty.SetCacheCapacity(-1, 0));
        empty.Dispose(); Reject<ObjectDisposedException>(() => empty.GetStringSize("")); Reject<ObjectDisposedException>(() => empty.GetFontName());
    }

    private static void VerifyMeasurement(byte[] data)
    {
        using var font = Load(data); using var oracle = new NativeFontPrecision(data);
        Check(font.GetFontName() == "Open Sans" && font.GetFontStyleName() == "SemiBold" && font.GetFaceCount() == 1 && font.GetFontWeight() == 600,
            "The public font reports native SFNT identity and weight rather than inferred display-name flags.");
        Check(font.GetAscent() == 18 && font.GetDescent() == 5 && font.GetHeight() == 23 && font.GetUnderlinePosition() == 63 / 64f && font.GetUnderlineThickness() == 25 / 64f,
            "Public metric queries retain the independent pinned FT oracle values.");
        Check(font.GetCharSize('A', 16) == new Vector2(677 / 64f, 23) && font.GetCharSize(' ', 16) == new Vector2(266 / 64f, 23),
            "Character measurement uses fractional unkerned advance and face height.");
        var withControls = font.GetStringSize("A\0\u001BV").X; var withoutControls = font.GetStringSize("AV").X;
        var onlyControls = font.GetStringSize("\0\u001B\u0378\U0010FFFF").X;
        Check(withControls == withoutControls && onlyControls == 0,
            $"NUL, ESC and unassigned non-graphic scalars have no invented .notdef advance or hexadecimal fallback under default control handling: with={withControls}, without={withoutControls}, only={onlyControls}.");
        Check(font.GetMultilineStringSize("A\vV").Y == 46 && font.GetMultilineStringSize("A\fV").Y == 46,
            "Unicode mandatory vertical-tab and form-feed separators split paragraphs when mandatory breaks are enabled.");
        Check(font.GetStringSize("ffi") == new Vector2(17, 23), "The independent 1026/64 ffi ligature advance is rounded only at the final bounding extent.");
        var shaped = oracle.Shape(['A', 'V'], NativeTextDirection.LTR); var expected = 0f;
        foreach (var glyph in shaped) expected += glyph.XAdvance / 64f;
        Check(font.GetStringSize("AV").X == MathF.Ceiling(expected),
            "String measurement retains the native run advances before final bounding-box rounding.");
        Check(font.GetStringSize("a\u0301") == font.GetStringSize("á"), "Canonical combining text shapes with its base rather than becoming an independent character width.");
        var vertical = oracle.Shape(['A', 'V'], NativeTextDirection.TTB); var verticalAdvance = 0f;
        foreach (var glyph in vertical) verticalAdvance -= glyph.YAdvance / 64f;
        var verticalSize = font.GetStringSize("AV", orientation: TextOrientation.Vertical);
        Check(verticalSize.Y == MathF.Ceiling(verticalAdvance) && verticalSize.X >= font.GetHeight(), "Vertical text uses vertical advances and remains a two-dimensional measured layout.");
        Check(font.GetStringSize("abcdef", width: 0) == font.GetStringSize("abcdef") && font.GetMultilineStringSize("abcdef", width: 0) == font.GetStringSize("abcdef"),
            "A zero width disables source trimming and wrapping rather than discarding text or generating empty lines.");
        Check(font.GetMultilineStringSize("A\nV") == new Vector2(11, 46) && font.GetMultilineStringSize("A\n") == new Vector2(11, 23),
            "Mandatory separators create lines without inventing a trailing empty paragraph in the font convenience API.");
        Check(font.GetMultilineStringSize("A\nV", maxLines: 0) == Vector2.Zero && font.GetMultilineStringSize("A\nV", maxLines: 1) == new Vector2(11, 23),
            "Visible-line limits affect both extents and drawing while preserving zero and unlimited cases.");
        var before = font.GetStringSize("ffi"); font.OpenTypeFeatureOverrides = new() { ["liga"] = 0 };
        Check(font.GetOpenTypeFeatures().Count == 0, "Font span overrides remain distinct from file-level OpenType defaults.");
        Check(Layout(font, "ffi").GlyphCount == 3, "Changing a shaping feature invalidates cached layout and replaces one ligature with three glyphs, even when rounded extents coincide.");
        font.OpenTypeFeatureOverrides = [];
        Check(font.GetStringSize("ffi") == before && Layout(font, "ffi").GlyphCount == 1, "Removing feature overrides restores the original shaped result.");
        var supported = font.GetSupportedChars(); Check(supported.Contains('A') && supported.Contains('é') && font.HasChar('é') && !font.HasChar(0x10FFFF), "Font character introspection reads the actual native character map.");
    }

    private static TextLayout Layout(Font font, string text, float width = -1, TextLineBreakFlags breaks = TextLineBreakFlags.None,
        HorizontalAlignment alignment = HorizontalAlignment.Left, TextDirection direction = TextDirection.Auto, TextJustificationFlags justification = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound) =>
        font.GetLayout(text, alignment, width, 16, -1, breaks, justification, direction, TextOrientation.Horizontal, breaks != TextLineBreakFlags.None);

    private static void VerifyLayout(byte[] data, byte[] arabicData)
    {
        using var font = Load(data); using var arabic = Load(arabicData); font.Fallbacks = [arabic];
        var ligature = Layout(font, "ffi"); var first = ligature.GetCharacterBounds(0);
        Check(ligature.GlyphCount == 1 && first == ligature.GetCharacterBounds(1) && first == ligature.GetCharacterBounds(2) && first.Size.X > 0,
            "Every scalar in a ligature resolves to its shared glyph cluster bounds.");
        var rtl = Layout(font, "אבג", direction: TextDirection.RTL);
        Check(rtl.GetCharacterBounds(0).Position.X > rtl.GetCharacterBounds(1).Position.X && rtl.GetCharacterBounds(1).Position.X > rtl.GetCharacterBounds(2).Position.X,
            "Visual glyph order reverses RTL scalars while public character indices remain logical.");
        var verticalRTL = arabic.GetLayout("بب", HorizontalAlignment.Left, -1, 16, -1, TextLineBreakFlags.None,
            TextJustificationFlags.None, TextDirection.RTL, TextOrientation.Vertical, false);
        Check(verticalRTL.GetCharacterBounds(0).Position.Y > verticalRTL.GetCharacterBounds(1).Position.Y,
            "Vertical RTL runs use bottom-to-top shaping rather than reusing top-to-bottom glyph order.");
        var mixed = Layout(font, "A אב B");
        Check(mixed.GetCharacterBounds(2).Position.X > mixed.GetCharacterBounds(3).Position.X && mixed.GetCharacterBounds(0).Position.X < mixed.GetCharacterBounds(5).Position.X,
            "A mixed-direction paragraph reorders only the embedded RTL run.");
        var graphemes = Layout(font, "a\u0301a\u0301", 10, TextLineBreakFlags.GraphemeBound);
        Check(graphemes.LineCount == 2 && graphemes.Lines[0].End == 2 && graphemes.Lines[1].Start == 2, "Grapheme wrapping cannot separate a combining sequence.");
        var longWord = Layout(font, "abcd", 12, TextLineBreakFlags.WordBound);
        Check(longWord.LineCount == 1 && longWord.Size.X > 12, "Word wrapping preserves a word that cannot fit when adaptive fallback is disabled.");
        var adaptive = Layout(font, "abcd", 12, TextLineBreakFlags.WordBound | TextLineBreakFlags.Adaptive);
        Check(adaptive.LineCount > 1, "Adaptive word wrapping falls back to available glyph-cluster boundaries.");
        var fill = Layout(font, "A V", 80, alignment: HorizontalAlignment.Fill, justification: TextJustificationFlags.WordBound);
        Check(fill.Size.X == 80 && fill.GetCharacterBounds(2).Position.X > 60, "Word justification expands actual spacing and changes character positions.");
        var plain = Layout(font, "A V"); var noFill = Layout(font, "A V", 80, alignment: HorizontalAlignment.Fill, justification: TextJustificationFlags.None);
        Check(noFill.Size == plain.Size, "Fill without permitted opportunities preserves the measured line width.");
        var centered = Layout(font, "A", 100, alignment: HorizontalAlignment.Center);
        Check(centered.GetCharacterBounds(0).Position.X == MathF.Floor((100 - 677 / 64f) / 2), "Center alignment floors the fractional remaining half-width before raster positioning.");
        var trimmed = Layout(font, "abc", 12); Check(trimmed.GetCharacterBounds(2) == default && trimmed.Size.X <= 12, "Single-line trimming removes whole clusters and their bounds.");
        Check(font.GetStringSize("A\tA").X == MathF.Ceiling(2 * font.GetCharSize('A', 16).X), "A tab has no invented default advance until explicit tab stops are supplied.");
        var custom = new TextLayout(); custom.Build(font, new("A\tV", 16, -1, HorizontalAlignment.Left, -1, TextLineBreakFlags.None,
            TextJustificationFlags.None, TextDirection.Auto, TextOrientation.Horizontal), new TextLayoutOptions(TabStops: [30], Overrun: 0, VisibleCharacters: -1));
        Check(custom.GetCharacterBounds(2).Position.X == 30, "Custom tab stops move the following glyph to the configured logical stop.");
    }

    private static void VerifyFallbacksAndCopies(byte[] data, byte[] arabicData)
    {
        using var font = Load(data); using var fallback = Load(arabicData); var changes = 0; font.Changed += _ => changes++;
        Font?[] supplied = [fallback, null, fallback]; font.Fallbacks = supplied; supplied[0] = null;
        Check(font.Fallbacks[0] == fallback && changes == 1, "Fallback assignments copy their input and retain repeated/null identities.");
        var snapshot = font.Fallbacks; snapshot[0] = null; Check(font.Fallbacks[0] == fallback, "Fallback getters return independent snapshots.");
        fallback.EmitChanged(); Check(changes == 2, "Repeated fallback aliases share one forwarding subscription.");
        Reject<ArgumentException>(() => fallback.Fallbacks = [font]); Check(fallback.Fallbacks.Length == 0, "Cyclic replacement fails before changing the fallback graph.");
        Check(font.HasChar('ب') && font.GetCharSize('ب', 16).X == fallback.GetCharSize('ب', 16).X, "Fallback selection uses the first face containing a missing scalar.");
        var both = font.GetSupportedChars(); Check(both.Count(character => character == 'A') == 1 && both.Contains('ب'), "Combined character introspection removes overlapping coverage without dropping fallback characters.");
        using var shallow = (FontFile)font.Duplicate(); using var deep = (FontFile)font.Duplicate(true);
        Check(shallow.Fallbacks[0] == fallback && deep.Fallbacks[0] is FontFile && deep.Fallbacks[0] != fallback && deep.Fallbacks[0] == deep.Fallbacks[2],
            "Shallow copies borrow fallbacks while deep copies preserve aliases in an independent resource graph.");
        var copiedFallback = deep.Fallbacks[0]!; copiedFallback.Dispose();
        var prior = font.GetStringSize("ffi"); fallback.Dispose();
        Check(font.Fallbacks[0] == fallback, "Disposal notifications preserve borrowed fallback identity.");
        Reject<ObjectDisposedException>(() => font.GetStringSize("ffi"));
        font.Fallbacks = []; Check(font.GetStringSize("ffi") == prior, "Removing a disposed fallback restores a clean source graph and layout.");
        using var replacement = Load(arabicData); using var observer = Load(data);
        replacement.Changed += _ => throw new ApplicationException("expected early subscriber failure");
        observer.Fallbacks = [replacement]; var measured = observer.GetStringSize("بب");
        Reject<ApplicationException>(() => replacement.Data = arabicData);
        Check(observer.GetStringSize("بب") == measured, "Generation polling refreshes parent layout even when an earlier throwing subscriber prevents its Changed callback and disposes the old face.");
        using var target = new FontFile(); Reject<ObjectDisposedException>(() => target.Fallbacks = [fallback]);
        Check(!font.IsDisposed, "Fonts never own borrowed fallback resources.");
    }

    private static void VerifyDrawing(byte[] data)
    {
        using var font = Load(data); var root = new Node(); var painter = new Painter(); root.AddChild(painter); using var tree = new SceneTree(root);
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        painter.Paint = node => font.DrawString(node, new(0, 20), "ffi", modulate: Colors.Red);
        Record(painter, vertices, batches);
        Check(vertices.Count == 6 && batches.Count == 1 && vertices.All(vertex => vertex.Color == Colors.Red), "A ligature draws one real textured glyph command with modulation.");
        painter.Paint = node => font.DrawString(node, new(0, 20), "A", HorizontalAlignment.Center, 100); Record(painter, vertices, batches);
        Check(Bounds(vertices).Position.X == 44, "The center alignment floor precedes quarter-pixel raster phase selection, avoiding a fractional three-quarter-pixel drift.");
        painter.Paint = node => font.DrawString(node, new(0, 20), "ffi"); Record(painter, vertices, batches);
        var first = Bounds(vertices); painter.Paint = node => font.DrawStringOutline(node, new(0, 20), "ffi", size: 2); Record(painter, vertices, batches);
        Check(Bounds(vertices).Size.X > first.Size.X && Bounds(vertices).Size.Y > first.Size.Y, "String outline rasterization expands the glyph's retained geometry.");
        painter.Paint = node => { var advance = font.DrawChar(node, new(0, 20), 'A', 16); Check(advance == 677 / 64f, "DrawChar returns the unshaped character advance."); };
        Record(painter, vertices, batches); first = Bounds(vertices);
        painter.Paint = node => font.DrawCharOutline(node, new(0, 20), 'A', 16, oversampling: -2); Record(painter, vertices, batches);
        Check(Bounds(vertices) == first, "The default negative character outline draws the normal glyph and negative oversampling selects automatic scale.");
        painter.Paint = node => font.DrawMultilineString(node, new(0, 20), "A\nV"); Record(painter, vertices, batches);
        Check(vertices.Count == 12 && Bounds(vertices).Size.Y > 23, "Multiline fill records both baseline-separated glyphs.");
        painter.Paint = node => font.DrawMultilineStringOutline(node, new(0, 20), "A\nV", maxLines: 1, size: 1); Record(painter, vertices, batches);
        Check(vertices.Count == 6, "Multiline outlines respect the same visible-line limit as measurement.");
        painter.Paint = node => font.DrawString(node, new(0, 20), "A\0\u001BV"); Record(painter, vertices, batches);
        Check(vertices.Count == 12, "Nonprinting controls retain scalar indices while recording no glyph-zero raster or hexadecimal boxes.");
        painter.Paint = node => font.DrawString(node, new(0, 20), "\U0010FFFD"); Record(painter, vertices, batches);
        Check(vertices.Count > 24, "A missing shaped scalar records a hexadecimal placeholder rather than a .notdef texture.");
        painter.Paint = node => font.DrawStringOutline(node, new(0, 20), "\U0010FFFD"); Record(painter, vertices, batches);
        Check(vertices.Count == 0, "Missing-glyph boxes have no fabricated outline rendering.");
        Reject<InvalidOperationException>(() => font.DrawString(painter, Vector2.Zero, ""));
        painter.Paint = node => { font.DrawString(node, Vector2.Zero, "A"); font.DrawString(node, Vector2.Zero, "V", oversampling: float.NaN); };
        painter.InvalidateCanvas(); Reject<ArgumentOutOfRangeException>(painter.PrepareCanvas);
        vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 0, "Failed text recording discards all partially recorded glyph commands.");
        painter.Paint = node => font.DrawString(node, Vector2.Zero, "A"); Record(painter, vertices, batches);
        Check(Task.Run(() => Capture(painter.PrepareCanvas)).Result is InvalidOperationException, "Text drawing preserves canvas owner-thread guards.");
    }

    private static void VerifyReuse(byte[] data)
    {
        using var font = Load(data); font.SetCacheCapacity(2, 1);
        var baseline = font.GetStringSize("AV ffi a\u0301");
        Check(Task.Run(() => font.GetStringSize("AV ffi a\u0301")).Result == baseline, "Synchronized font measurements can use the native text worker from another managed thread.");
        var root = new Node(); var painter = new Painter(); root.AddChild(painter); using var tree = new SceneTree(root);
        painter.Paint = node => font.DrawString(node, new(0, 20), "AV ffi a\u0301");
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        for (var i = 0; i < 16; i++) { font.InvalidateFont(); font.GetStringSize("AV ffi a\u0301"); Record(painter, vertices, batches); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            font.InvalidateFont(); font.GetStringSize("AV ffi a\u0301"); Record(painter, vertices, batches);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Warmed layout rebuilds, cache hits and glyph recording must reuse managed storage; allocated {allocated} bytes.");
        font.SetCacheCapacity(0, 0); Check(font.GetStringSize("AV ffi a\u0301") == baseline, "Disabling LRU retention preserves shaped output through scratch storage.");
        font.SetCacheCapacity(64, 16);
        Check(font.GetStringSize("AV ffi a\u0301") == baseline, "Restoring source cache capacities preserves output.");
    }

    private sealed class Painter : Entity
    {
        internal Action<Painter>? Paint;
        protected override void OnDraw() => Paint?.Invoke(this);
    }
    private static void Record(Painter painter, List<CanvasVertex> vertices, List<CanvasBatch> batches)
    {
        painter.InvalidateCanvas(); painter.PrepareCanvas(); vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity);
    }
    private static Rect2 Bounds(List<CanvasVertex> vertices)
    {
        Check(vertices.Count > 0, "Expected retained glyph geometry."); var minimum = vertices[0].Position; var maximum = minimum;
        foreach (var vertex in vertices) { minimum = minimum.Min(vertex.Position); maximum = maximum.Max(vertex.Position); }
        return new(minimum, maximum - minimum);
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
