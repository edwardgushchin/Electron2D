using Electron2D;

internal static class TextServerFormattingTests
{
    internal static void Run()
    {
        var s = TextServerManager.GetPrimaryInterface(); using var font = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        var buffers = new List<RID>();
        RID Buffer(string text, int size = 28) { var id = s.CreateShapedText(); buffers.Add(id); s.ShapedTextAddString(id, text, [font], size); return id; }
        try
        {
            var empty = Buffer(""); Check(s.ShapedTextGetTrimPos(empty) == -1 && s.ShapedTextGetEllipsisPos(empty) == -1 && s.ShapedTextGetCustomEllipsis(empty) == 0x2026, "Initial metadata/default scalar");
            s.ShapedTextSetSpacing(empty, TextSpacingType.Top, 5); s.ShapedTextSetSpacing(empty, TextSpacingType.Bottom, -2);
            Check(s.ShapedTextGetAscent(empty) == 5 && s.ShapedTextGetDescent(empty) == -2 && s.ShapedTextGetSize(empty) == new Vector2(0, 3), "Empty signed cross spacing");
            var text = Buffer("alpha beta gamma delta epsilon"); var natural = s.ShapedTextGetWidth(text); var rawCount = s.ShapedTextGetGlyphCount(text);
            var fitted = s.ShapedTextFitToWidth(text, natural + 100, TextJustificationFlags.WordBound);
            Near(fitted, natural + 100, 1, "Word fitting"); Check(s.ShapedTextGetWidth(text) == fitted && s.ShapedTextGetTrimPos(text) == -1, "Fitted metrics");
            var beforeCaret = s.ShapedTextGetCarets(text, 6).LeadingRect.Position.X;
            s.ShapedTextFitToWidth(text, natural + 40); Check(s.ShapedTextGetCarets(text, 6).LeadingRect.Position.X < beforeCaret, "Repeated fit updates actual raw geometry");
            var trimFlags = TextOverrunFlags.Trim | TextOverrunFlags.AddEllipsis | TextOverrunFlags.ShortStringEllipsis;
            s.ShapedTextOverrunTrimToWidth(text, 150, trimFlags); var cut = s.ShapedTextGetTrimPos(text); var ellipse = s.ShapedTextGetEllipsisPos(text);
            Check(cut >= 0 && ellipse >= 0 && cut < s.ShapedTextGetGlyphCount(text) && s.ShapedTextGetWidth(text) <= 150, "Trim indices and displayed width: " + cut + "/" + ellipse + "/" + s.ShapedTextGetWidth(text));
            Check(s.ShapedTextGetGlyphCount(text) >= rawCount && s.ShapedTextGetRange(text).Y == 30, "Original raw clusters/source range survive trimming");
            var selection = s.ShapedTextGetSelection(text, 0, 30); Check(selection.Length > 0 && selection[^1].Y > s.ShapedTextGetWidth(text), "Raw selection retains trimmed tail");
            s.ShapedTextOverrunTrimToWidth(text); Check(s.ShapedTextGetTrimPos(text) == -1 && s.ShapedTextGetEllipsisPos(text) == -1 && s.ShapedTextGetWidth(text) > 150, "NoTrim restores full fitted geometry");
            s.ShapedTextSetCustomEllipsis(text, '#'); Check(s.ShapedTextGetCustomEllipsis(text) == '#', "Custom scalar roundtrip");
            s.ShapedTextOverrunTrimToWidth(text, 100, trimFlags); Check(s.ShapedTextGetEllipsisPos(text) >= 0 && s.ShapedTextGetWidth(text) <= 100, "Custom actual ellipsis");
            s.ShapedTextSetCustomEllipsis(text, 0x10ffff); s.ShapedTextOverrunTrimToWidth(text, 100, trimFlags); Check(s.ShapedTextGetEllipsisPos(text) >= 0, "Unsupported scalar uses period fallback");
            var shortText = Buffer("ABC"); s.ShapedTextOverrunTrimToWidth(shortText, 25, TextOverrunFlags.Trim | TextOverrunFlags.AddEllipsis); Check(s.ShapedTextGetEllipsisPos(shortText) == -1, "Six-record short-string rule");
            s.ShapedTextOverrunTrimToWidth(shortText, 300, trimFlags | TextOverrunFlags.EnforceEllipsis); Check(s.ShapedTextGetEllipsisPos(shortText) >= 0 && s.ShapedTextGetTrimPos(shortText) == -1, "Forced fitting ellipsis");
            var wordOnly = Buffer("ABCDEFGHIJKLMNO"); var wordLimit = s.ShapedTextGetGraphemeBounds(wordOnly, 3).Y;
            s.ShapedTextOverrunTrimToWidth(wordOnly, wordLimit, TextOverrunFlags.Trim | TextOverrunFlags.TrimWordOnly);
            Check(s.ShapedTextGetWidth(wordOnly) > 0 && s.ShapedTextGetWidth(wordOnly) <= MathF.Ceiling(wordLimit), "Word-only keeps the source short-result cluster fallback");
            var noSpace = Buffer("ABCDEFGHIJKLMNO"); var noSpaceNatural = s.ShapedTextGetWidth(noSpace);
            s.ShapedTextOverrunTrimToWidth(noSpace, 50, trimFlags | TextOverrunFlags.JustificationAware); Check(s.ShapedTextGetTrimPos(noSpace) == -1, "Aware trim waits for minimum result");
            Check(s.ShapedTextFitToWidth(noSpace, 50) == noSpaceNatural, "Unshrinkable buffer returns actual width"); s.ShapedTextOverrunTrimToWidth(noSpace, 50, trimFlags | TextOverrunFlags.JustificationAware); Check(s.ShapedTextGetTrimPos(noSpace) >= 0, "Aware trim after failed fit");
            s.ShapedTextOverrunTrimToWidth(text, 160, trimFlags); var constrained = s.ShapedTextFitToWidth(text, 200, TextJustificationFlags.WordBound | TextJustificationFlags.ConstrainEllipsis); Check(s.ShapedTextGetWidth(text) == constrained && s.ShapedTextGetEllipsisPos(text) >= 0, "Constrained fitting retains ellipsis view");
            var spaced = Buffer("A A"); var width = s.ShapedTextGetWidth(spaced); var ascent = s.ShapedTextGetAscent(spaced); var descent = s.ShapedTextGetDescent(spaced);
            s.ShapedTextSetSpacing(spaced, TextSpacingType.Glyph, 3); Near(s.ShapedTextGetWidth(spaced), width + 6, 1, "Glyph spacing between advances");
            s.ShapedTextSetSpacing(spaced, TextSpacingType.Space, 7); Near(s.ShapedTextGetWidth(spaced), width + 10, 1, "Space spacing overrides glyph spacing at spaces");
            s.ShapedTextSetSpacing(spaced, TextSpacingType.Top, 4); s.ShapedTextSetSpacing(spaced, TextSpacingType.Bottom, -2); Near(s.ShapedTextGetAscent(spaced), ascent + 4, .001f, "Top spacing"); Near(s.ShapedTextGetDescent(spaced), descent - 2, .001f, "Bottom spacing");
            var duplicate = s.ShapedTextDuplicate(spaced); buffers.Add(duplicate); Check(s.ShapedTextGetSpacing(duplicate, TextSpacingType.Glyph) == 3 && s.ShapedTextGetWidth(duplicate) == s.ShapedTextGetWidth(spaced), "Duplicate spacing");
            s.ShapedTextClear(spaced); Check(s.ShapedTextGetSpacing(spaced, TextSpacingType.Space) == 7, "Clear preserves spacing");
            var parent = Buffer("alpha beta gamma delta"); var child = s.ShapedTextSubstr(parent, 0, 10); buffers.Add(child); s.ShapedTextFitToWidth(child, 200);
            Check(s.ShapedTextGetParent(child) == parent, "Fit preserves parent"); s.ShapedTextOverrunTrimToWidth(child, 80, trimFlags); Check(s.ShapedTextGetParent(child) == parent, "Trim preserves parent");
            s.ShapedTextSetCustomEllipsis(child, '?'); Check(s.ShapedTextGetParent(child) == parent, "Ellipsis configuration preserves parent");
            s.ShapedTextSetSpacing(child, TextSpacingType.Glyph, 0); Check(s.ShapedTextGetParent(child) == parent, "No-op spacing preserves parent"); s.ShapedTextSetSpacing(child, TextSpacingType.Glyph, 2); Check(s.ShapedTextGetParent(child) == default, "Changed spacing detaches authoring");
            using var customParagraph = new TextParagraph { Width = 180, BreakFlags = TextLineBreakFlags.Mandatory, TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce, EllipsisChar = "#" };
            customParagraph.AddString("borrowed alpha beta gamma delta", font, 28); var customLine = customParagraph.GetLineRID(0);
            Check(s.ShapedTextGetCustomEllipsis(customLine) == '#' && s.ShapedTextGetCustomEllipsis(customParagraph.GetRID()) == 0x2026, "Borrowed line exposes its paragraph ellipsis independently of full source");
            s.ShapedTextFitToWidth(customLine, 450); Check(s.ShapedTextGetCustomEllipsis(customLine) == '#', "First formatting override retains the configured paragraph ellipsis");
            s.ShapedTextSetCustomEllipsis(customLine, 0x2026); Check(s.ShapedTextGetCustomEllipsis(customLine) == 0x2026 && customParagraph.EllipsisChar == "#", "Changing inherited line ellipsis to default is not a source-value no-op");
            s.ShapedTextOverrunTrimToWidth(customLine, 100, trimFlags); Check(s.ShapedTextGetEllipsisPos(customLine) >= 0 && customParagraph.GetLineWidth(0) == s.ShapedTextGetWidth(customLine), "Changed line ellipsis reaches actual paragraph display");
            using var paragraph = new TextParagraph { Width = 200 }; paragraph.AddString("first\nalpha beta gamma", font, 24); var line = paragraph.GetLineRID(1);
            s.ShapedTextFitToWidth(line, 220); Check(s.ShapedTextGetParent(line) == paragraph.GetRID() && paragraph.GetLineWidth(1) == s.ShapedTextGetWidth(line), "Borrowed fitted line updates real consumer and parent");
            s.ShapedTextOverrunTrimToWidth(line, 100, trimFlags); Check(paragraph.GetLineWidth(1) == s.ShapedTextGetWidth(line) && s.ShapedTextGetEllipsisPos(line) >= 0, "Borrowed line trim metadata and consumer: " + paragraph.GetLineWidth(1) + "/" + s.ShapedTextGetWidth(line) + "/" + s.ShapedTextGetEllipsisPos(line));
            paragraph.CustomPunctuation = "_"; Check(s.ShapedTextGetCustomPunctuation(paragraph.GetRID()) == "_", "Paragraph punctuation shares source owner"); s.ShapedTextSetCustomPunctuation(paragraph.GetRID(), ":"); Check(paragraph.CustomPunctuation == ":", "Source punctuation reaches paragraph settings");
            var punctuation = Buffer("A B C"); var punctuationWidth = s.ShapedTextGetWidth(punctuation); s.ShapedTextSetCustomPunctuation(punctuation, " ");
            Check(s.ShapedTextFitToWidth(punctuation, 500, TextJustificationFlags.WordBound) == punctuationWidth, "Custom space punctuation excludes real word opportunities");
            var edges = Buffer("  A A  "); var untrimmedEdges = s.ShapedTextGetWidth(edges); var trimmedEdges = s.ShapedTextFitToWidth(edges, 0, TextJustificationFlags.TrimEdgeSpaces);
            Check(trimmedEdges < untrimmedEdges && s.ShapedTextGetGlyphCount(edges) == 7, "Edge fitting zeros advances without deleting original glyphs");
            using var hebrew = new FontFile { Data = FontTestFixtures.Hebrew, AllowSystemFallback = false };
            var rtl = s.CreateShapedText(TextDirection.Auto); buffers.Add(rtl); s.ShapedTextAddString(rtl, "אבגד הוזח טיכל מנסע", [hebrew], 28);
            Check(!hebrew.HasChar('…') && !hebrew.HasChar('.'), "Hebrew fixture intentionally lacks ellipsis/period glyphs");
            s.ShapedTextOverrunTrimToWidth(rtl, 140, trimFlags); Check(s.ShapedTextGetEllipsisPos(rtl) == -1, "Missing ellipsis and period glyphs keep the absent-payload sentinel"); s.ShapedTextSetCustomEllipsis(rtl, 'א');
            s.ShapedTextFitToWidth(rtl, 400); s.ShapedTextOverrunTrimToWidth(rtl, 140, trimFlags); Check(s.ShapedTextGetTrimPos(rtl) > 0 && s.ShapedTextGetEllipsisPos(rtl) == s.ShapedTextGetTrimPos(rtl) && s.ShapedTextGetWidth(rtl) <= 140, "Auto RTL uses inferred leading trim/ellipsis boundary: " + s.ShapedTextGetTrimPos(rtl) + "/" + s.ShapedTextGetEllipsisPos(rtl) + "/" + s.ShapedTextGetWidth(rtl) + "/" + s.ShapedTextGetInferredDirection(rtl));
            var neutral = s.CreateShapedText(); buffers.Add(neutral); s.ShapedTextAddString(neutral, "12345 67890", [font], 28, language: "ar");
            Check(s.ShapedTextGetInferredDirection(neutral) == TextDirection.RTL, "Neutral buffer uses its first authored span language");
            var neutralChild = s.ShapedTextSubstr(neutral, 2, 5); buffers.Add(neutralChild); s.ShapedTextSetSpacing(neutralChild, TextSpacingType.Glyph, 2);
            Check(s.ShapedTextGetInferredDirection(neutralChild) == TextDirection.RTL, "Detached neutral slice retains authored language context");
            var vertical = Buffer("ABCD EFGH IJKL"); s.ShapedTextSetOrientation(vertical, TextOrientation.Vertical); s.ShapedTextFitToWidth(vertical, 400); s.ShapedTextOverrunTrimToWidth(vertical, 150, trimFlags);
            Check(s.ShapedTextGetSize(vertical).Y <= 150 && s.ShapedTextGetEllipsisPos(vertical) >= 0, "Vertical ellipsis uses the advance axis");
            using var arabic = new FontFile { Data = FontTestFixtures.Arabic, AllowSystemFallback = false }; var elongation = s.CreateShapedText(); buffers.Add(elongation); s.ShapedTextAddString(elongation, "سلام عالم", [arabic], 34);
            var firstArabic = s.ShapedTextFitToWidth(elongation, 500); Check(firstArabic >= 499 && firstArabic <= 501 && s.ShapedTextFitToWidth(elongation, 500) == firstArabic, "Repeated mixed-size Kashida fitting is stable");
            var fittedChild = s.ShapedTextSubstr(elongation, 0, 9); buffers.Add(fittedChild); Check(s.ShapedTextGetWidth(fittedChild) == s.ShapedTextGetWidth(elongation), "Fitted parent snapshot retains actual virtual/repeated advances");
            var retainedFitted = Buffer("alpha beta gamma delta epsilon"); s.ShapedTextFitToWidth(retainedFitted, 600);
            var retainedChild = s.ShapedTextSubstr(retainedFitted, 0, 10); buffers.Add(retainedChild); var retainedWidth = s.ShapedTextGetWidth(retainedChild); var retainedDirection = s.ShapedTextGetInferredDirection(retainedChild);
            s.ShapedTextClear(retainedFitted); s.ShapedTextAddString(retainedFitted, "😀😀😀😀😀😀😀😀😀😀", [font], 12, language: "ar"); s.ShapedTextShape(retainedFitted);
            s.FreeRID(retainedFitted); buffers.Remove(retainedFitted); var nestedFitted = s.ShapedTextSubstr(retainedChild, 0, 10); buffers.Add(nestedFitted);
            Check(s.ShapedTextGetWidth(nestedFitted) == retainedWidth && s.ShapedTextGetWidth(retainedChild) == retainedWidth && s.ShapedTextGetInferredDirection(retainedChild) == retainedDirection, "Fitted snapshot semantic arrays survive parent source replacement");
            var tabs = Buffer("A A\tB B"); s.ShapedTextTabAlign(tabs, [100]); var prefix = s.ShapedTextGetCarets(tabs, 3).LeadingRect; s.ShapedTextFitToWidth(tabs, 400, TextJustificationFlags.WordBound | TextJustificationFlags.AfterLastTab); Check(s.ShapedTextGetCarets(tabs, 3).LeadingRect == prefix, "LTR fitting after last tab preserves preceding advances");
            using var cjk = new FontFile { Data = FontTestFixtures.CJK, AllowSystemFallback = false };
            using var constrainedLine = new TextLine { Width = 160, Alignment = HorizontalAlignment.Fill, Flags = TextJustificationFlags.WordBound | TextJustificationFlags.ConstrainEllipsis, TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce };
            constrainedLine.AddString("天地玄黄宇宙洪荒日月盈昃辰宿列张", cjk, 24, "zh"); constrainedLine.GetSize(); var constrainedRID = constrainedLine.GetRID();
            var cjkFitted = s.ShapedTextFitToWidth(constrainedRID, 220, TextJustificationFlags.WordBound | TextJustificationFlags.ConstrainEllipsis);
            Near(s.ShapedTextGetWidth(constrainedRID), cjkFitted, .001f, "High-level constrained CJK source and display remain coherent");
            using var callback = new CallbackFont { Fallbacks = [font] }; var transactional = s.CreateShapedText(); buffers.Add(transactional); s.ShapedTextAddString(transactional, "callback alpha beta gamma", [callback], 28); s.ShapedTextOverrunTrimToWidth(transactional, 150, trimFlags);
            var savedWidth = s.ShapedTextGetWidth(transactional); var savedCut = s.ShapedTextGetTrimPos(transactional); var savedEllipse = s.ShapedTextGetEllipsisPos(transactional);
            callback.DuringGlyph = () => { Reject<InvalidOperationException>(() => s.ShapedTextClear(transactional)); throw new CallbackFailure(); };
            Reject<CallbackFailure>(() => s.ShapedTextOverrunTrimToWidth(transactional, 100, trimFlags)); callback.DuringGlyph = null;
            Check(s.ShapedTextGetWidth(transactional) == savedWidth && s.ShapedTextGetTrimPos(transactional) == savedCut && s.ShapedTextGetEllipsisPos(transactional) == savedEllipse, "Failed native/virtual font work publishes no partial geometry");
            using var callbackParagraph = new TextParagraph { Width = 300 }; callbackParagraph.AddString("borrowed callback alpha beta gamma", callback, 28); var callbackLine = callbackParagraph.GetLineRID(0);
            s.ShapedTextFitToWidth(callbackLine, 450); var callbackWidth = s.ShapedTextGetWidth(callbackLine);
            callback.DuringGlyph = () => { Reject<InvalidOperationException>(() => s.ShapedTextGetCustomEllipsis(callbackLine)); Reject<InvalidOperationException>(() => s.ShapedTextGetTrimPos(callbackLine)); Reject<InvalidOperationException>(() => s.ShapedTextGetSpacing(callbackLine, TextSpacingType.Glyph)); throw new CallbackFailure(); };
            Reject<CallbackFailure>(() => s.ShapedTextOverrunTrimToWidth(callbackLine, 100, trimFlags)); callback.DuringGlyph = null;
            Check(s.ShapedTextGetWidth(callbackLine) == callbackWidth, "Borrowed formatting queries reject active line reentry and failed trims publish nothing");
            Reject<ArgumentOutOfRangeException>(() => s.ShapedTextSetSpacing(text, TextSpacingType.Max, 1)); Reject<ArgumentOutOfRangeException>(() => s.ShapedTextSetCustomEllipsis(text, 0xd800)); Reject<ArgumentOutOfRangeException>(() => s.ShapedTextSetCustomEllipsis(text, -1));
            Reject<ArgumentOutOfRangeException>(() => s.ShapedTextFitToWidth(text, float.NaN)); Reject<ArgumentOutOfRangeException>(() => s.ShapedTextOverrunTrimToWidth(text, float.PositiveInfinity)); Reject<ArgumentOutOfRangeException>(() => s.ShapedTextOverrunTrimToWidth(text, 1, (TextOverrunFlags)64));
            Task.Run(() => Reject<InvalidOperationException>(() => s.ShapedTextFitToWidth(text, 100))).GetAwaiter().GetResult();
            for (var i = 0; i < 32; i++) Cycle(s, text, trimFlags, i);
            var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Cycle(s, text, trimFlags, i);
            Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "128 prepared alternating fit/trim cycles allocate zero owner bytes: " + (GC.GetAllocatedBytesForCurrentThread() - allocated));
        }
        finally { foreach (var buffer in buffers) s.FreeRID(buffer); }
        Console.WriteLine("TextServer formatting: fitting/trim/scalar/spacing, raw/display separation, metadata, parent/consumer integration and 128 warm zero-byte cycles passed.");
    }
    private sealed class CallbackFailure : Exception { }
    private sealed class CallbackFont : Font
    {
        internal override FontData? PrimaryData => null;
        internal Action? DuringGlyph;
        public override int GetSpacing(TextSpacingType spacing) { if (spacing == TextSpacingType.Glyph) DuringGlyph?.Invoke(); return 0; }
    }
    private static void Cycle(TextServer s, RID id, TextOverrunFlags flags, int i) { s.ShapedTextFitToWidth(id, 350 + i % 2 * 10); s.ShapedTextOverrunTrimToWidth(id, 140 + i % 2 * 10, flags); s.ShapedTextGetTrimPos(id); s.ShapedTextGetEllipsisPos(id); s.ShapedTextGetCarets(id, 3); }
    private static void Near(float actual, float expected, float tolerance, string message) => Check(Math.Abs(actual - expected) <= tolerance, message + ": " + actual + "/" + expected);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer formatting: " + message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
