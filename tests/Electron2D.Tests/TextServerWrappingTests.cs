using Electron2D;

internal static class TextServerWrappingTests
{
    internal static void Run()
    {
        var s = TextServerManager.GetPrimaryInterface();
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        var owned = new List<RID>();
        RID Buffer(string text) { var id = s.CreateShapedText(); owned.Add(id); s.ShapedTextAddString(id, text, [font], 28); return id; }
        RID Slice(RID id, int start, int length) { var result = s.ShapedTextSubstr(id, start, length); owned.Add(result); return result; }
        try
        {
            var empty = Buffer(""); Equal(s.ShapedTextGetLineBreaks(empty, 10), [0, 0], "Empty line");
            Check(s.ShapedTextGetParent(empty) == default, "Independent parent");
            var text = Buffer("AAAA"); var a = s.ShapedTextGetGraphemeBounds(text, 0).Y;
            Equal(s.ShapedTextGetLineBreaks(text, a + .1f, breakFlags: TextLineBreakFlags.GraphemeBound), [0, 1, 1, 2, 2, 3, 3, 4], "Cluster wrap");
            Equal(s.ShapedTextGetLineBreaks(text, a + .1f), [0, 4], "Words may overflow");
            var adaptive = TextLineBreakFlags.WordBound | TextLineBreakFlags.Adaptive;
            Equal(s.ShapedTextGetLineBreaks(text, a + .1f, breakFlags: adaptive), [0, 1, 1, 2, 2, 3, 3, 4], "Adaptive fallback");
            Equal(s.ShapedTextGetLineBreaksAdv(text, [a + .1f, 2 * a + .1f], breakFlags: adaptive), [0, 1, 1, 3], "One column cycle");
            Equal(s.ShapedTextGetLineBreaksAdv(text, [a + .1f, 2 * a + .1f], once: false, breakFlags: adaptive), [0, 1, 1, 3, 3, 4], "Repeated column widths");
            Equal(s.ShapedTextGetLineBreaksAdv(text, [a + .1f], once: false, breakFlags: adaptive), s.ShapedTextGetLineBreaks(text, a + .1f, breakFlags: adaptive), "Single and advanced flag parity");
            Equal(s.ShapedTextGetLineBreaks(text, -1), [0, 4], "Disabled width");
            Equal(s.ShapedTextGetLineBreaks(text, 0, int.MaxValue), [4, 4], "Clamped exhausted start");
            Equal(s.ShapedTextGetLineBreaks(text, 0, int.MinValue), [0, 4], "Clamped negative start");
            var breaks = s.ShapedTextGetLineBreaks(text, -1); breaks[0] = 99; Equal(s.ShapedTextGetLineBreaks(text, -1), [0, 4], "Snapshot independence");
            var hard = Buffer("A\r\nB\nC");
            Equal(s.ShapedTextGetLineBreaks(hard, -1), [0, 3, 3, 5, 5, 6], "CRLF and mandatory ranges");
            Equal(s.ShapedTextGetLineBreaksAdv(hard, [1000, 1000]), [0, 3], "Mandatory stops once");
            Equal(s.ShapedTextGetLineBreaks(hard, -1, breakFlags: TextLineBreakFlags.None), [0, 6], "Mandatory opt out");
            var blank = Buffer("\n\nA");
            Equal(s.ShapedTextGetLineBreaks(blank, -1, breakFlags: TextLineBreakFlags.Mandatory | TextLineBreakFlags.TrimEndEdgeSpaces), [0, 0, 1, 1, 2, 3], "Trimmed mandatory blank lines retain empty ranges");
            var trim = Buffer("  A  B  C  "); var firstWidth = s.ShapedTextGetGraphemeBounds(trim, 5).Y + .01f;
            var flags = TextLineBreakFlags.WordBound | TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces;
            var trimmed = s.ShapedTextGetLineBreaks(trim, firstWidth, breakFlags: flags);
            Check(trimmed.Length >= 4 && trimmed[0] == 0 && trimmed[1] == 3 && trimmed[2] == 5 && trimmed[^1] == 11, "Initial leading/final trailing spaces retained: " + string.Join(',', trimmed));
            var indented = s.ShapedTextGetLineBreaks(trim, firstWidth, breakFlags: flags | TextLineBreakFlags.TrimIndent);
            Check(indented.Length >= trimmed.Length, "Indent reduces subsequent available widths");
            var soft = Buffer("ab\u00adcd"); var softSlice = Slice(soft, 0, 3);
            Check(s.ShapedTextGetWidth(softSlice) > MathF.Ceiling(s.ShapedTextGetGraphemeBounds(soft, 2).Y), "A final soft hyphen becomes visible");
            var dictionary = s.CreateShapedText(); owned.Add(dictionary); s.ShapedTextAddString(dictionary, "ภาษาไทยภาษาไทย", [font], 28, language: "th");
            var dictionaryBreaks = s.ShapedTextGetLineBreaks(dictionary, 1);
            Check(dictionaryBreaks.Length > 2 && dictionaryBreaks[1] > 1 && dictionaryBreaks[^1] == 14, "Native Thai dictionary opportunities survive shaping: " + string.Join(',', dictionaryBreaks));
            Equal(s.ShapedTextGetLineBreaks(dictionary, 1, breakFlags: TextLineBreakFlags.WordBound | TextLineBreakFlags.TrimEndEdgeSpaces | TextLineBreakFlags.TrimStartEdgeSpaces), dictionaryBreaks, "Trimming never removes visible dictionary glyphs at virtual break opportunities");
            var languages = s.CreateShapedText(); owned.Add(languages); s.ShapedTextAddString(languages, "A", [font], 28, language: "en"); s.ShapedTextAddString(languages, "B", [font], 28, language: "fr");
            Equal(s.ShapedTextGetLineBreaks(languages, 1), [0, 1, 1, 2], "Language-context boundary remains a native break opportunity");
            var ffi = Buffer("ffi A😀e\u0301 אב");
            Check(s.ShapedTextNextGraphemePos(ffi, 0) == 3, "Fixture has ffi shaping");
            var whole = Slice(ffi, 0, 3); var partial = Slice(ffi, 1, 1);
            Check(s.ShapedTextGetGlyphCount(whole) == 1 && s.ShapedTextGetWidth(whole) == MathF.Ceiling(s.ShapedTextGetGraphemeBounds(ffi, 0).Y), "Complete ligature copied");
            Check(s.ShapedTextGetGlyphCount(partial) == 0 && s.ShapedTextGetSize(partial) == default, "Partial ligature is not reshaped");
            Check(s.ShapedTextGetRange(partial) == new Vector2i(1, 2) && s.ShapedGetText(partial) == s.ShapedGetText(ffi), "Global source context");
            Equal(s.ShapedTextGetLineBreaks(partial, 1), [1, 2], "Glyphless nonempty substring range");
            var nested = Slice(partial, 4, 4); Check(s.ShapedTextGetParent(nested) == ffi && s.ShapedTextGetRange(nested) == new Vector2i(4, 8), "Nested slice addresses original source");
            Equal(s.ShapedTextGetCharacterBreaks(nested), [5, 6, 8], "Supplementary and combining scalar origin");
            var zero = Slice(ffi, 6, 0); Equal(s.ShapedTextGetLineBreaks(zero, 1), [6, 6], "Empty substring retains origin");
            Reject<ArgumentOutOfRangeException>(() => s.ShapedTextSubstr(ffi, -1, 1)); Reject<ArgumentOutOfRangeException>(() => s.ShapedTextSubstr(ffi, 0, int.MaxValue));
            Reject<ArgumentException>(() => s.ShapedTextGetLineBreaksAdv(text, [])); Reject<ArgumentException>(() => s.ShapedTextGetLineBreaksAdv(text, new float[4097]));
            Reject<ArgumentOutOfRangeException>(() => s.ShapedTextGetLineBreaks(text, float.NaN)); Reject<ArgumentOutOfRangeException>(() => s.ShapedTextGetLineBreaksAdv(text, [1, float.PositiveInfinity]));
            Reject<ArgumentOutOfRangeException>(() => s.ShapedTextGetLineBreaks(text, 1, breakFlags: (TextLineBreakFlags)16));
            Reject<ArgumentOutOfRangeException>(() => s.ShapedTextSetOrientation(nested, (TextOrientation)99)); Check(s.ShapedTextGetParent(nested) == ffi, "Failed mutation retains parent");
            s.ShapedTextAddString(nested, "", [font], 28); Check(s.ShapedTextGetParent(nested) == ffi, "Empty append retains parent");
            s.ShapedTextAddString(nested, "!", [font], 28); Check(s.ShapedTextGetParent(nested) == default && s.ShapedTextGetRange(nested) == new Vector2i(4, 9), "Successful mutation detaches and preserves origin");
            s.ShapedTextShape(nested); s.ShapedTextClear(nested); Check(s.ShapedTextGetRange(nested) == default && s.ShapedTextGetParent(nested) == default, "Clear resets detached origin");
            var key = new TextObjectKey(); var objects = Buffer("A"); s.ShapedTextAddObject(objects, key, new(30, 20), length: 3); s.ShapedTextAddString(objects, "B", [font], 28);
            var objectSlice = Slice(objects, 1, 3); Check(s.ShapedTextGetObjects(objectSlice).SequenceEqual([key]) && s.ShapedTextGetObjectRange(objectSlice, key) == new Vector2i(1, 4) && s.ShapedTextGetObjectRect(objectSlice, key).Size == new Vector2(30, 20), "Whole inline token/range/geometry");
            var objectPartial = Slice(objects, 2, 1); Check(s.ShapedTextGetObjects(objectPartial).Length == 0 && s.ShapedTextGetGlyphCount(objectPartial) == 0, "Partial object remains an unshaped source range");
            Equal(s.ShapedTextGetLineBreaks(objectSlice, 1, breakFlags: TextLineBreakFlags.GraphemeBound), [1, 4], "Multi-scalar object cannot split");
            var partialCopy = s.ShapedTextDuplicate(objectPartial); owned.Add(partialCopy); s.FreeRID(objectPartial); owned.Remove(objectPartial);
            Check(s.ShapedTextGetParent(partialCopy) == default && s.ShapedTextGetRange(partialCopy) == new Vector2i(2, 3), "Duplicate partial-object authored data is independent");
            s.ShapedTextShape(partialCopy);
            var meta = s.CreateShapedText(); owned.Add(meta); var token = new object(); s.ShapedTextAddString(meta, "prefix", [font], 24, null, "en", token); var metaSlice = Slice(meta, 2, 3);
            Check(s.TryGetSpanMetadata<object>(metaSlice, 0, out var found) && ReferenceEquals(found, token), "Exact metadata survives slicing");
            var parent = Buffer("A אב אב"); var bidi = Slice(parent, 2, 3); Check(s.ShapedTextGetCarets(bidi, 5).LeadingDirection == TextDirection.LTR, "Line trailing space resets direction");
            s.FreeRID(parent); owned.Remove(parent); Check(!s.Has(parent) && s.ShapedTextGetParent(bidi) == parent && s.ShapedTextGetWidth(bidi) > 0, "Source leases survive parent free");
            var afterFree = Slice(bidi, 0, 1); Check(s.ShapedTextGetParent(afterFree) == parent && s.ShapedTextGetGlyphCount(afterFree) == 1, "Nested source snapshot survives free");
            font.FixedSize = 30; Check(s.ShapedTextGetWidth(bidi) > 0 && s.ShapedTextGetWidth(metaSlice) > 0, "Font-generation rebuild retains source shaping");
            Task.Run(() => Reject<InvalidOperationException>(() => s.ShapedTextGetLineBreaks(bidi, 100))).GetAwaiter().GetResult();
            using var paragraph = new TextParagraph(); paragraph.AddString("first\nsecond", font, 26); var line = paragraph.GetLineRID(1); var independent = Slice(line, 6, 6); paragraph.Width = 5; paragraph.GetLineCount();
            Check(!s.Has(line) && s.ShapedTextGetParent(independent) == paragraph.GetRID() && s.ShapedTextGetGlyphCount(independent) > 0, "Paragraph reflow retires borrowed parent without invalidating owned substring");
            s.ShapedTextSetOrientation(independent, TextOrientation.Vertical); Check(s.ShapedTextGetParent(independent) == default && s.ShapedTextGetSize(independent).Y > 0, "Axis mutation detaches and runs");
            using var arabic = new FontFile { Data = FontTestFixtures.Arabic, AllowSystemFallback = false };
            using var justified = new TextLine { Alignment = HorizontalAlignment.Fill, Width = 500, TextOverrunBehavior = TextOverrunBehavior.NoTrimming };
            justified.AddString("سلام عالم", arabic, 28, "ar"); var justifiedSlice = Slice(justified.GetRID(), 0, 9);
            Check(s.ShapedTextGetWidth(justifiedSlice) == s.ShapedTextGetWidth(justified.GetRID()), "Copied virtual/repeated justification advances remain complete: " + s.ShapedTextGetWidth(justifiedSlice) + "/" + s.ShapedTextGetWidth(justified.GetRID()));
            var large = Buffer(new string('A', 32768)); var warmSlice = Slice(large, 0, 1); s.ShapedTextGetWidth(warmSlice);
            owned.EnsureCapacity(owned.Count + 32);
            var authoringBefore = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 32; i++) Slice(large, 100 + i, 1);
            var authoringBytes = GC.GetAllocatedBytesForCurrentThread() - authoringBefore;
            Check(authoringBytes < 2 * 1024 * 1024, "Sibling source/context leases avoid copying the whole 32K source per child: " + authoringBytes);
            for (var i = 0; i < 16; i++) Values(s, objectSlice);
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Values(s, objectSlice);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared substring value queries allocate zero owner bytes");
        }
        finally { foreach (var id in owned) s.FreeRID(id); }
        Console.WriteLine("TextServer wrapping: columns, flags, clusters, Unicode/BIDI, objects, metadata, parent lifetime/reflow and 128 value cycles=0 owner bytes passed.");
    }
    private static void Values(TextServer s, RID id) { s.ShapedTextGetParent(id); s.ShapedTextGetRange(id); s.ShapedTextGetSize(id); s.ShapedTextGetCarets(id, 2); s.ShapedTextHitTestPosition(id, 10); }
    private static void Equal(int[] actual, int[] expected, string message) => Check(actual.SequenceEqual(expected), message + ": " + string.Join(',', actual));
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer wrapping: " + message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
