using Electron2D;

internal static class TextServerSpanTests
{
    internal static void Run()
    {
        using var first = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        using var other = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        var s = TextServerManager.GetPrimaryInterface(); var owned = new List<RID>();
        RID Buffer(string text, int size = 24, string language = "en")
        { var id = s.CreateShapedText(); owned.Add(id); s.ShapedTextAddString(id, text, [first], size, language: language); return id; }
        try
        {
            var line = s.CreateShapedText(); owned.Add(line); var metadata = new[] { 5, 7 };
            s.ShapedTextAddString(line, "ffi", [first], 24, new Dictionary<string, int> { ["liga"] = 1 }, "en", metadata);
            var width = s.ShapedTextGetWidth(line); var glyphs = s.ShapedTextGetGlyphCount(line);
            Font[] fonts = [other]; var features = new Dictionary<string, int> { ["liga"] = 0 };
            s.ShapedSetSpanUpdateFont(line, 0, fonts, 36, features); fonts[0] = first; features["liga"] = 1;
            Check(!s.ShapedTextIsReady(line), "Font update invalidates prepared geometry");
            Check(s.ShapedTextGetWidth(line) > width && s.ShapedTextGetGlyphCount(line) > glyphs, "Copied font size/features change actual shaping");
            Check(s.ShapedGetText(line) == "ffi" && s.TryGetSpanMetadata<int[]>(line, 0, out var actual) && ReferenceEquals(actual, metadata), "Text and exact metadata remain");
            s.ShapedSetSpanUpdateFont(line, 0, [other], 24); Check(s.ShapedTextGetGlyphCount(line) == glyphs, "Null features remove prior override");
            var duplicate = s.ShapedTextDuplicate(line); owned.Add(duplicate);
            s.ShapedSetSpanUpdateFont(line, 0, [other], 48); Check(s.ShapedTextGetWidth(duplicate) < s.ShapedTextGetWidth(line), "Duplicate owns separate authoring");
            var parent = Buffer("A ffi X"); var child = s.ShapedTextSubstr(parent, 2, 3); owned.Add(child);
            Reject<ArgumentException>(() => s.ShapedSetSpanUpdateFont(child, 0, [], 24)); Check(s.ShapedTextGetParent(child) == parent, "Failed update retains substring parent");
            s.ShapedSetSpanUpdateFont(child, 0, [other], 40);
            Check(s.ShapedTextGetParent(child) == default && s.ShapedTextGetRange(child) == new Vector2i(2, 5) && s.ShapedGetText(child).Contains("ffi"), "Successful update detaches with global origin");
            var beforeChild = s.ShapedTextGetWidth(child); s.FreeRID(parent); owned.Remove(parent); Check(s.ShapedTextGetWidth(child) == beforeChild, "Changed child survives source retirement");
            var mixed = Buffer("AB"); var key = new TextObjectKey(); s.ShapedTextAddObject(mixed, key, new(12, 8), length: 3); s.ShapedTextAddString(mixed, "CD", [first], 24);
            s.ShapedSetSpanUpdateFont(mixed, 2, [other], 40); Check(s.ShapedGetSpanText(mixed, 2) == "CD" && s.ShapedGetSpanObject(mixed, 1) == key, "Text/object interleaved span indexes");
            var objectRect = s.ShapedTextGetObjectRect(mixed, key); s.ShapedSetSpanUpdateFont(mixed, 1, [other], 18);
            Check(s.ShapedTextGetObjectRect(mixed, key) == objectRect && s.ShapedGetSpanCount(mixed) == 3, "Explicit inline geometry has no font dependency");
            using (var paragraph = new TextParagraph { Width = 75 })
            {
                paragraph.AddString("first second third", first, 24); var borrowed = paragraph.GetLineRID(1); var range = s.ShapedTextGetRange(borrowed); var sourceText = s.ShapedGetText(paragraph.GetRID());
                s.ShapedSetSpanUpdateFont(borrowed, 0, [other], 36);
                Check(s.ShapedTextGetRange(borrowed) == range && s.ShapedTextGetWidth(borrowed) == paragraph.GetLineWidth(1), "Borrowed line font override reaches paragraph consumer");
                Check(s.ShapedGetText(paragraph.GetRID()) == sourceText, "Line override preserves source authoring");
                paragraph.Width = 130; _ = paragraph.GetLineCount(); Check(!s.Has(borrowed), "Reflow retires borrowed override");
            }
            var recovery = Buffer("retired source"); _ = s.ShapedTextGetWidth(recovery); first.Dispose();
            s.ShapedSetSpanUpdateFont(recovery, 0, [other], 28); Check(s.ShapedTextGetWidth(recovery) > 0, "Replacement removes retired font dependency before reshaping");
            void Words(string text, int[] expected, TextGraphemeFlags delimiters = TextGraphemeFlags.Space | TextGraphemeFlags.Punctuation, TextGraphemeFlags skip = TextGraphemeFlags.Virtual, string language = "en")
            { var id = s.CreateShapedText(); owned.Add(id); s.ShapedTextAddString(id, text, [other], 24, language: language); Equal(s.ShapedTextGetWordBreaks(id, delimiters, skip), expected, text); }
            Words("one,two three_4", [0, 3, 4, 7, 8, 15]);
            Words("a_b", [0, 1, 2, 3], TextGraphemeFlags.Underscore);
            Words("a,b", [0, 3], TextGraphemeFlags.None);
            Words("a b", [0, 3], TextGraphemeFlags.Space, TextGraphemeFlags.Space);
            Words("a\r\nb\nc", [0, 1, 3, 4, 5, 6]);
            Words("a\u200bb", [0, 1, 2, 3]);
            Words("a\tb\vc", [0, 1, 2, 3, 4, 5], TextGraphemeFlags.Tab);
            Words("a\u00adb", [0, 1, 2, 3], TextGraphemeFlags.SoftHyphen);
            Words("a b", [], TextGraphemeFlags.Valid);
            Words("日本語a", [0, 4], language: "ja");
            Words("日本語a", [0, 3, 3, 4], skip: TextGraphemeFlags.None, language: "ja");
            Words("AאבB", [0, 1, 3, 4], TextGraphemeFlags.RTL);
            Words("a\r\nb", [0, 1, 3, 4], TextGraphemeFlags.BreakHard);
            Words("a b", [0, 1, 2, 3], TextGraphemeFlags.BreakSoft);
            Words("", []);
            var embeddedWords = s.CreateShapedText(); owned.Add(embeddedWords); s.ShapedTextAddString(embeddedWords, "AB", [other], 24);
            s.ShapedTextAddObject(embeddedWords, new TextObjectKey(), new(8, 8), length: 2); s.ShapedTextAddString(embeddedWords, "CD", [other], 24);
            Equal(s.ShapedTextGetWordBreaks(embeddedWords, TextGraphemeFlags.EmbeddedObject), [0, 2, 4, 6], "Selected inline delimiters exclude a complete multi-scalar object");
            Equal(s.ShapedTextGetWordBreaks(embeddedWords, skipGraphemeFlags: TextGraphemeFlags.None), [0, 2, 2, 6], "A virtual boundary before an inline object retains its source and word-space role");
            var custom = s.CreateShapedText(); owned.Add(custom); s.ShapedTextAddString(custom, "ab_cd,ef", [other], 24); s.ShapedTextSetCustomPunctuation(custom, "_");
            Equal(s.ShapedTextGetWordBreaks(custom), [0, 2, 3, 8], "Custom punctuation replaces defaults");
            var composite = s.CreateShapedText(); owned.Add(composite); s.ShapedTextAddString(composite, "ffi e\u0301 😀", [other], 24);
            Equal(s.ShapedTextGetWordBreaks(composite), [0, 3, 4, 6, 7, 8], "Ligature/composite/supplementary scalar ranges");
            var snapshot = s.ShapedTextGetWordBreaks(composite); snapshot[0] = 99; Check(s.ShapedTextGetWordBreaks(composite)[0] == 0, "Independent word snapshot");
            var sliced = s.ShapedTextSubstr(composite, 4, 4); owned.Add(sliced); Equal(s.ShapedTextGetWordBreaks(sliced), [4, 6, 7, 8], "Substring global scalar ranges");
            var complete = s.CreateShapedText(); owned.Add(complete); s.ShapedTextAddString(complete, "ffi", [other], 24);
            var cut = s.ShapedTextSubstr(complete, 1, 1); owned.Add(cut); Equal(s.ShapedTextGetWordBreaks(cut), [], "An unshaped partial ligature has no word records");
            s.ShapedSetSpanUpdateFont(cut, 0, [other], 24); Equal(s.ShapedTextGetWordBreaks(cut), [1, 2], "Authoring detaches and shapes the partial source with its global origin");
            var vertical = s.CreateShapedText(orientation: TextOrientation.Vertical); owned.Add(vertical); s.ShapedTextAddString(vertical, "AB CD", [other], 24);
            var verticalWidth = s.ShapedTextGetWidth(vertical); s.ShapedSetSpanUpdateFont(vertical, 0, [other], 36); Check(s.ShapedTextGetWidth(vertical) > verticalWidth, "Vertical replacement uses the actual advance axis");
            Equal(s.ShapedTextGetWordBreaks(vertical), [0, 2, 3, 5], "Vertical words retain source scalar units");
            var neutral = s.CreateShapedText(); owned.Add(neutral); s.ShapedTextAddString(neutral, "123", [other], 24, language: "he"); s.ShapedSetSpanUpdateFont(neutral, 0, [other], 30);
            Check(s.ShapedTextGetInferredDirection(neutral) == TextDirection.RTL, "Font replacement preserves the span language");
            using (var callback = new CallbackFont { Fallbacks = [other] })
            {
                s.ShapedSetSpanUpdateFont(line, 0, [callback], 24);
                callback.DuringGlyph = () => { Reject<InvalidOperationException>(() => s.ShapedSetSpanUpdateFont(line, 0, [other], 30)); Reject<InvalidOperationException>(() => s.ShapedTextGetWordBreaks(line)); throw new CallbackFailure(); };
                Reject<CallbackFailure>(() => s.ShapedTextGetWordBreaks(line)); callback.DuringGlyph = null;
                s.ShapedSetSpanUpdateFont(line, 0, [other], 24); Equal(s.ShapedTextGetWordBreaks(line), [0, 3], "Failed native callback releases guards and replacement can recover");
            }
            var excessive = Enumerable.Range(0, 4097).ToDictionary(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture), _ => 0);
            Reject<ArgumentException>(() => s.ShapedSetSpanUpdateFont(line, 0, [other], 24, excessive));
            Reject<ArgumentOutOfRangeException>(() => s.ShapedSetSpanUpdateFont(line, -1, [other], 24)); Reject<ArgumentOutOfRangeException>(() => s.ShapedSetSpanUpdateFont(line, 1, [other], 24));
            Reject<ArgumentOutOfRangeException>(() => s.ShapedSetSpanUpdateFont(line, 0, [other], 0)); Reject<ArgumentException>(() => s.ShapedSetSpanUpdateFont(line, 0, new Font[65], 24));
            Reject<ArgumentNullException>(() => s.ShapedSetSpanUpdateFont(line, 0, [null!], 24)); Reject<ObjectDisposedException>(() => s.ShapedSetSpanUpdateFont(line, 0, [first], 24));
            Reject<ArgumentOutOfRangeException>(() => s.ShapedTextGetWordBreaks(line, (TextGraphemeFlags)16384)); Reject<ArgumentOutOfRangeException>(() => s.ShapedTextGetWordBreaks(line, skipGraphemeFlags: (TextGraphemeFlags)(-1)));
            Task.Run(() => Reject<InvalidOperationException>(() => s.ShapedTextGetWordBreaks(line))).GetAwaiter().GetResult();
            for (var i = 0; i < 128; i++) s.ShapedTextGetWidth(line);
            var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) s.ShapedTextGetWidth(line);
            Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared values after font replacement remain zero-byte");
            Console.WriteLine("TextServer spans: actual font/size/features, exact metadata, ownership/retirement, paragraph consumers, logical flag-delimited word ranges and guards passed.");
        }
        finally { for (var i = owned.Count - 1; i >= 0; i--) s.FreeRID(owned[i]); }
    }
    private sealed class CallbackFailure : Exception { }
    private sealed class CallbackFont : Font
    {
        internal override FontData? PrimaryData => null;
        internal Action? DuringGlyph;
        public override int GetSpacing(TextSpacingType spacing) { if (spacing == TextSpacingType.Glyph) DuringGlyph?.Invoke(); return 0; }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer spans: " + message); }
    private static void Equal(int[] actual, int[] expected, string message) => Check(actual.AsSpan().SequenceEqual(expected), message + ": " + string.Join(',', actual));
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
