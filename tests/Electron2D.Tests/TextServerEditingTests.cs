using Electron2D;

internal static class TextServerEditingTests
{
    private static float _sink;
    internal static void Run()
    {
        var server = TextServerManager.GetPrimaryInterface();
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        var buffer = server.CreateShapedText();
        try
        {
            Check(server.ShapedTextGetCharacterBreaks(buffer).Length == 0 && server.ShapedTextGetSelection(buffer, -1, 1).Length == 0, "Empty snapshots.");
            Check(server.ShapedTextGetCarets(buffer, 0).LeadingRect == default && server.ShapedTextGetCarets(buffer, 0).TrailingDirection == TextDirection.Auto, "Empty caret.");
            Check(!server.ShapedTextHasVisibleChars(buffer) && server.ShapedTextHitTestGrapheme(buffer, 0) == -1 && server.ShapedTextGetGraphemeBounds(buffer, 0) == default, "Empty glyph queries.");
            Check(server.ShapedTextGetDominantDirectionInRange(buffer, int.MinValue, int.MaxValue) == TextDirection.Auto, "Empty direction.");
            Check(server.ShapedTextClosestCharacterPos(buffer, int.MaxValue) == 0 && server.ShapedTextPrevCharacterPos(buffer, int.MinValue) == 0 && server.ShapedTextNextCharacterPos(buffer, 0) == 0, "Empty character navigation.");
            Check(server.ShapedTextNextGraphemePos(buffer, int.MinValue) == int.MinValue && server.ShapedTextPrevGraphemePos(buffer, int.MaxValue) == int.MaxValue, "Outside cluster positions remain unchanged.");
            server.ShapedTextAddString(buffer, "ffi A😀e\u0301", [font], 28, language: "en");
            Check(server.ShapedTextGetCharacterBreaks(buffer).SequenceEqual([1, 2, 3, 4, 5, 6, 8]), "Scalar composite-character boundaries include the end.");
            Check(server.ShapedTextNextCharacterPos(buffer, 0) == 1 && server.ShapedTextNextGraphemePos(buffer, 0) == 3 && server.ShapedTextPrevGraphemePos(buffer, 2) == 0, "Unicode characters differ from the native ffi cluster: next=" + server.ShapedTextNextGraphemePos(buffer, 0) + ", prev=" + server.ShapedTextPrevGraphemePos(buffer, 2) + ", glyphs=" + server.ShapedTextGetGlyphCount(buffer));
            Check(server.ShapedTextClosestCharacterPos(buffer, 7) == 6 && server.ShapedTextNextCharacterPos(buffer, 6) == 8 && server.ShapedTextPrevCharacterPos(buffer, 8) == 6, "Combining boundary ties choose the preceding character.");
            Check(server.ShapedTextClosestCharacterPos(buffer, int.MinValue) == 0 && server.ShapedTextClosestCharacterPos(buffer, int.MaxValue) == 8 && server.ShapedTextNextCharacterPos(buffer, int.MinValue) == 1, "Extreme scalar coordinates do not overflow.");
            var snapshot = server.ShapedTextGetCharacterBreaks(buffer); snapshot[0] = 999; Check(server.ShapedTextGetCharacterBreaks(buffer)[0] == 1, "Boundary arrays are independent.");
            var bounds = server.ShapedTextGetGraphemeBounds(buffer, 0); var caret = server.ShapedTextGetCarets(buffer, 1);
            Near(caret.LeadingRect.Position.X, bounds.Y / 3, "Interpolated native ligature caret.");
            Check(caret.LeadingRect == caret.TrailingRect && caret.LeadingDirection == TextDirection.Auto && caret.TrailingDirection == TextDirection.Auto, "Inside-cluster caret has coincident shapes and Auto directions.");
            Near(caret.LeadingRect.Size.Y, server.ShapedTextGetAscent(buffer) + server.ShapedTextGetDescent(buffer), "Full inside-cluster height.");
            var selection = server.ShapedTextGetSelection(buffer, 0, 1); Check(selection.Length == 1, "Partial ligature selection."); Near(selection[0].Y, bounds.Y / 3, "Partial native cluster interval.");
            Check(selection.SequenceEqual(server.ShapedTextGetSelection(buffer, 1, 0)) && server.ShapedTextGetGraphemeBounds(buffer, 3) == bounds, "Reversed selection and inclusive glyph boundary.");
            Check(server.ShapedTextHitTestGrapheme(buffer, bounds.Y * .5f) == 0 && server.ShapedTextHitTestGrapheme(buffer, -1) == -1 && server.ShapedTextHitTestGrapheme(buffer, float.MaxValue) == -1, "Glyph index hit and half-open outside bounds.");
            Check(server.ShapedTextHasVisibleChars(buffer) && server.ShapedTextGetDominantDirectionInRange(buffer, int.MaxValue, int.MinValue) == TextDirection.LTR, "Actual visible glyphs and reversed dominant range.");
            Check(server.ShapedTextGetCarets(buffer, int.MinValue).LeadingRect == default && server.ShapedTextGetCarets(buffer, int.MaxValue).TrailingRect == default, "Outside caret shapes are empty.");
            Reject<ArgumentOutOfRangeException>(() => server.ShapedTextHitTestGrapheme(buffer, float.NaN));
            Task.Run(() => Reject<InvalidOperationException>(() => server.ShapedTextGetCarets(buffer, 1))).GetAwaiter().GetResult();
            for (var i = 0; i < 16; i++) Queries(server, buffer);
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Queries(server, buffer);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "128 prepared scalar/value query cycles allocate zero owner bytes.");
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "A", [font], 24, language: "en"); server.ShapedTextAddString(buffer, "\u0301", [font], 24, language: "fr");
            Check(server.ShapedTextGetCharacterBreaks(buffer).SequenceEqual([1, 2]), "Language-span boundaries retain separate character contexts.");
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "A", [font], 24, language: "en"); server.ShapedTextAddString(buffer, "\u0301", [font], 24, language: "en");
            Check(server.ShapedTextGetCharacterBreaks(buffer).SequenceEqual([2]), "Equal-language spans share composite characters after rebuild.");
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "😀", [font], 24, language: "en"); server.ShapedTextAddString(buffer, "e\u0301", [font], 24, language: "fr");
            Check(server.ShapedTextGetCharacterBreaks(buffer).SequenceEqual([1, 3]) && server.ShapedTextNextCharacterPos(buffer, 1) == 3, "Mixed-language contexts after a supplementary prefix use actual UTF-16 span endpoints.");
            server.ShapedTextClear(buffer); var key = new TextObjectKey(); server.ShapedTextAddObject(buffer, key, new(30, 20), length: 3);
            Check(server.ShapedTextGetCharacterBreaks(buffer).SequenceEqual([1, 2, 3]) && server.ShapedTextNextGraphemePos(buffer, 0) == 3, "Multi-scalar object has one shaped cluster.");
            Near(server.ShapedTextGetCarets(buffer, 1).TrailingRect.Position.X, 10, "Inline scalar caret advance.");
            Check(server.ShapedTextGetSelection(buffer, 0, 3).Length == 0 && !server.ShapedTextHasVisibleChars(buffer) && server.ShapedTextGetGraphemeBounds(buffer, 1) == default, "Inline geometry is distinct from actual glyph visibility and eligible selection.");
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "A", [font], 24); server.ShapedTextAddObject(buffer, new(), new(30, 20), length: 3); server.ShapedTextAddString(buffer, "B", [font], 24);
            var objectGaps = server.ShapedTextGetSelection(buffer, int.MinValue, int.MaxValue);
            Check(objectGaps.Length == 2 && objectGaps[1].X - objectGaps[0].Y >= 29.99f, "Inline objects retain physical gaps between selected glyphs.");
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "Aאב", [font], 24);
            var split = server.ShapedTextGetCarets(buffer, 1);
            Check(split.LeadingDirection == TextDirection.LTR && split.TrailingDirection == TextDirection.RTL && split.TrailingRect.Position.X > split.LeadingRect.Position.X, "BIDI boundary retains two visual carets.");
            Near(split.LeadingRect.Size.Y, split.TrailingRect.Size.Y, "Split caret halves.");
            Check(server.ShapedTextGetDominantDirectionInRange(buffer, 0, 3) == TextDirection.RTL && server.ShapedTextGetDominantDirectionInRange(buffer, 0, 2) == TextDirection.Auto, "Direction counts shaped clusters and preserves ties.");
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "ffiאב", [font], 24);
            Check(server.ShapedTextGetDominantDirectionInRange(buffer, 0, 5) == TextDirection.RTL, "Dominance counts one native ligature rather than its three scalar characters.");
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "A\U0001F600B", [font], 24);
            var gaps = server.ShapedTextGetSelection(buffer, int.MinValue, int.MaxValue);
            Check(gaps.Length == 1 && !font.HasChar(0x1F600), "Nonzero hexadecimal placeholder indices participate in selection.");
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "\U0001F600", [font], 24);
            Check(server.ShapedTextGetWidth(buffer) > 0 && server.ShapedTextHasVisibleChars(buffer), "A nonzero hexadecimal placeholder index counts as visible.");
            server.ShapedTextSetPreserveInvalid(buffer, false);
            Check(server.ShapedTextGetWidth(buffer) == 0 && !server.ShapedTextHasVisibleChars(buffer), "Hidden invalid source has no visible glyph record.");
            server.ShapedTextSetPreserveInvalid(buffer, true);
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, " ", [font], 24);
            Check(server.ShapedTextHasVisibleChars(buffer) && server.ShapedTextGetSelection(buffer, 0, 1).Length == 1, "A valid space glyph participates even without ink.");
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "אב", [font], 24); server.ShapedTextSetDirection(buffer, TextDirection.RTL); server.ShapedTextSetOrientation(buffer, TextOrientation.Vertical);
            var vertical = server.ShapedTextGetCarets(buffer, 2);
            Near(vertical.LeadingRect.Position.X, -server.ShapedTextGetAscent(buffer), "Vertical RTL edge caret must not drift on its cross axis.");
            Near(vertical.LeadingRect.Position.Y, 0, "Vertical RTL leading edge advances along Y.");
            Check(vertical.LeadingDirection == TextDirection.RTL && vertical.LeadingRect.Size.Y > 0, "Signed vertical RTL caret geometry.");
            Paragraph(server, font); RepeatedAdvances(server); Adjacency(server, font); WrappedWhitespace(server, font);
        }
        finally { server.FreeRID(buffer); }
        Reject<InvalidOperationException>(() => server.ShapedTextGetCarets(buffer, 0));
        using var disposedFont = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false }; var borrowed = server.CreateShapedText();
        try { server.ShapedTextAddString(borrowed, "x", [disposedFont], 20); server.ShapedTextGetCharacterBreaks(borrowed); disposedFont.Dispose(); Reject<ObjectDisposedException>(() => server.ShapedTextGetCarets(borrowed, 0)); }
        finally { server.FreeRID(borrowed); }
        Console.WriteLine("TextServer editing: Unicode/shape boundaries, BIDI/ligature/inline/vertical carets, selections, owner failures and 128 zero-byte query cycles passed.");
    }
    private static void Paragraph(TextServer server, Font font)
    {
        using var paragraph = new TextParagraph(); paragraph.AddString("first\nffi e\u0301", font, 24); var line = paragraph.GetLineRID(1); var range = server.ShapedTextGetRange(line);
        Check(range.X == 6 && server.ShapedTextGetCharacterBreaks(line).SequenceEqual([7, 8, 9, 10, 12]), "Paragraph character offsets are global and line-filtered.");
        Check(server.ShapedTextNextGraphemePos(line, 6) == 9 && server.ShapedTextPrevCharacterPos(line, int.MinValue) == 6, "Borrowed line navigation retains origin.");
        Check(server.ShapedTextHitTestGrapheme(line, 1) == 0 && server.ShapedTextGetSelection(line, 6, 7)[0].X == 0, "Line-local glyph indices and selection geometry.");
        server.ShapedTextAddString(line, "X", [font], 24); var copied = server.ShapedTextDuplicate(line);
        try { Check(server.ShapedTextGetRange(copied).X == 6 && server.ShapedTextGetCharacterBreaks(copied)[^1] == 13, "Detached and duplicated line snapshots retain the scalar origin."); }
        finally { server.FreeRID(copied); }
        paragraph.Width = 80; paragraph.GetLineCount(); Check(!server.Has(line), "Reflow retires edited line identities.");
        Reject<InvalidOperationException>(() => server.ShapedTextGetSelection(line, 0, 1));
        paragraph.GetSize(); paragraph.GetSize();
        using var variation = new FontVariation { BaseFont = font };
        using var changedFont = new TextParagraph(); changedFont.AddString("first\nabc", variation, 24); var edited = changedFont.GetLineRID(1);
        server.ShapedTextAddString(edited, "X", [variation], 24); variation.SpacingGlyph = 2;
        changedFont.GetLineCount(); Check(!server.Has(edited) && changedFont.GetSize().X > 0, "Font-driven reflow rebuilds the read set after retiring detached font adapters.");
    }
    private static void RepeatedAdvances(TextServer server)
    {
        using var arabic = new FontFile { Data = FontTestFixtures.Arabic, AllowSystemFallback = false };
        using var line = new TextLine { Alignment = HorizontalAlignment.Fill, Width = 300, Flags = TextJustificationFlags.Kashida };
        line.AddString("سلام سلام", arabic, 28, "ar"); var buffer = line.GetRID(); var selection = server.ShapedTextGetSelection(buffer, int.MinValue, int.MaxValue);
        var selected = selection.Sum(x => x.Y - x.X); var width = server.ShapedTextGetWidth(buffer);
        Check(selected > 250 && Math.Abs(selected - width) < 1.01f, "Selection covers repeated native elongation advances: " + selected + "/" + width);
        Check(server.ShapedTextHitTestGrapheme(buffer, 250) >= 0 && server.ShapedTextHitTestGrapheme(buffer, 250) < server.ShapedTextGetGlyphCount(buffer), "Repeated advances retain their visual record index.");
    }
    private static void Adjacency(TextServer server, Font font)
    {
        using var line = new TextLine(); line.AddString("\tBאב", font, 24); line.TabAlign([20000000]);
        var selected = server.ShapedTextGetSelection(line.GetRID(), 1, 3);
        var editorRanges = new List<Vector2>(); line.ServerLayout.SelectionRanges(1, 3, editorRanges);
        Check(selected.Length == 2 && editorRanges.Count == 2, "Public/editor large-coordinate selection intervals must remain separate: " + selected.Length + "/" + editorRanges.Count);
        using var tiny = new TextLine(); tiny.AddString("A", font, 24); tiny.AddObject(new(), new(.0005f, 20)); tiny.AddString("B", font, 24);
        Check(server.ShapedTextGetSelection(tiny.GetRID(), 0, 3).Length == 1, "Subpixel adjacency uses the fixed .001 logical-pixel tolerance.");
    }
    private static void WrappedWhitespace(TextServer server, Font font)
    {
        using var prefix = new TextLine(); prefix.AddString("A אב ", font, 24);
        using var paragraph = new TextParagraph { Width = prefix.GetLineWidth() }; paragraph.AddString("A אב אב", font, 24);
        var buffer = paragraph.GetLineRID(0); var range = server.ShapedTextGetRange(buffer);
        Check(range.Y == 5, "The first soft-wrapped line retains its trailing space: " + range);
        var caret = server.ShapedTextGetCarets(buffer, 5); var bounds = server.ShapedTextGetGraphemeBounds(buffer, 5);
        Check(caret.LeadingDirection == TextDirection.LTR && caret.LeadingRect.Size.X < 0, "Trailing line whitespace retains its actual LTR shaping direction: " + caret.LeadingDirection);
        Near(caret.LeadingRect.Position.X, bounds.Y, "Trailing whitespace caret uses the physical line end.");
        Check(server.ShapedTextGetDominantDirectionInRange(buffer, 4, 5) == TextDirection.LTR && server.ShapedTextHitTestPosition(buffer, bounds.Y) == 5, "Direction and scalar hits agree with the actual whitespace run.");
        server.ShapedTextSetBIDIOverride(paragraph.GetRID(), [new(0, 7, TextDirection.LTR)]); paragraph.GetLineCount(); buffer = paragraph.GetLineRID(0);
        Check(server.ShapedTextGetCarets(buffer, 5).LeadingDirection == TextDirection.LTR, "Later wrapped-line context resolution cannot overwrite earlier glyph directions.");
    }
    private static void Queries(TextServer s, RID b)
    {
        var c = s.ShapedTextGetCarets(b, 1); var v = s.ShapedTextGetGraphemeBounds(b, 1);
        _sink = c.LeadingRect.Position.X + v.Y + s.ShapedTextClosestCharacterPos(b, 7) + s.ShapedTextNextCharacterPos(b, 6) + s.ShapedTextPrevCharacterPos(b, 8)
            + s.ShapedTextNextGraphemePos(b, 0) + s.ShapedTextPrevGraphemePos(b, 2) + s.ShapedTextHitTestGrapheme(b, 1) + (int)s.ShapedTextGetDominantDirectionInRange(b, 0, 3) + (s.ShapedTextHasVisibleChars(b) ? 1 : 0);
    }
    private static void Near(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < .001f, message + ": " + actual + " != " + expected);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
