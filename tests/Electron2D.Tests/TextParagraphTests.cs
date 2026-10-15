using Electron2D;

internal static class TextParagraphTests
{
    internal static void Run()
    {
        using var font = new FontFile { Data = FontTestFixtures.Variable };
        using var arabic = new FontFile { Data = FontTestFixtures.Arabic };
        using var p = new TextParagraph(); var rid = p.GetRID(); var capRID = p.GetDropcapRID();
        Check(rid.IsValid() && capRID.IsValid() && rid != capRID, "Distinct stable full/dropcap identities.");
        Check(p.Width == -1 && p.MaxLinesVisible == -1 && p.LineSpacing == 0 && p.Alignment == HorizontalAlignment.Left && p.Direction == TextDirection.Auto && p.Orientation == TextOrientation.Horizontal, "Default paragraph policies.");
        Check(p.BreakFlags == (TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound) && (int)p.JustificationFlags == 163 && p.TextOverrunBehavior == TextOverrunBehavior.NoTrimming && p.PreserveInvalid && !p.PreserveControl && p.CustomPunctuation == "" && p.EllipsisChar == "…", "All remaining defaults.");
        Check(p.GetLineCount() == 1 && p.GetRange() == default && p.GetSize() == default && p.GetDropcapLines() == 0 && p.GetDropcapSize() == default && p.HitTest(new(10, 10)) == 0, "Empty paragraph.");
        Reject<ArgumentOutOfRangeException>(() => p.GetLineRID(1)); Reject<ArgumentOutOfRangeException>(() => p.GetLineSize(-1));
        p.AddString("AB\r\nC😀D\n\nE", font, 20, "en", 47);
        Check(p.SpanCount == 1 && p.TryGetSpanMetadata<int>(0, out var meta) && meta == 47 && !p.TryGetSpanMetadata<string>(0, out _), "Exact typed metadata.");
        Check(p.GetRange() == new Vector2i(0, 10) && p.GetLineCount() == 4, "Scalar range and CRLF/empty mandatory lines.");
        Check(p.GetLineRange(0) == new Vector2i(0, 4) && p.GetLineRange(1) == new Vector2i(4, 8) && p.GetLineRange(2) == new Vector2i(8, 9) && p.GetLineRange(3) == new Vector2i(9, 10), "Line ranges retain mandatory separators.");
        Check(p.GetLineAscent(2) > 0 && p.GetLineSize(2).Y > 0, "Empty mandatory line retains font metrics.");
        var fullSize = p.GetSize(); p.MaxLinesVisible = 1; Check(p.GetLineCount() == 4 && p.GetSize().Y < fullSize.Y && p.GetLineRID(3).IsValid(), "Visibility never narrows line identities/query family.");
        p.MaxLinesVisible = 0; Check(p.GetSize() == default && p.GetLineCount() == 4, "Zero visible lines."); p.MaxLinesVisible = -2;
        p.LineSpacing = 7; Check(p.GetSize().Y == fullSize.Y + 21, "Three signed inter-line spacings.");
        p.LineSpacing = -1; Check(p.GetSize().Y == fullSize.Y - 3, "Negative spacing is a real policy."); p.LineSpacing = 0;
        Check(p.HitTest(new(float.MaxValue, 1)) == 2 && p.HitTest(new(float.MinValue, 1)) == 0 && p.HitTest(new(1, -1)) == 0, "Extreme advance and negative cross carets.");
        var lineID = p.GetLineRID(0); p.Width = p.Width; Check(p.GetLineRID(0) == lineID, "Equal settings keep prepared identities.");
        p.Width = 40; Check(p.GetLineRID(0) != lineID, "Reflow retires prior line identities.");
        p.Clear(); p.Width = 100; p.MaxLinesVisible = -1; p.AddString("hello hello hello hello hello hello", font, 20);
        Check(p.GetLineCount() > 1 && p.GetNonWrappedSize().X > p.GetSize().X, "Actual word wrapping and full unwrapped metrics.");
        var total = p.GetLineCount(); p.MaxLinesVisible = 1; p.TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce; Check(p.GetLineCount() == total && p.GetLineWidth(0) <= p.Width, "Wrapped visible-tail ellipsis keeps all line data.");
        p.Clear(); p.MaxLinesVisible = -1; p.Width = -1; var key = new TextObjectKey(); p.AddString("A", font, 18); p.AddObject(key, new(13, 24), length: 3); p.AddString("B\nC", font, 30);
        Check(p.GetRange().Y == 7 && p.HasObject(key) && p.GetLineObjects(0).SequenceEqual([key]) && p.GetLineObjects(1).Length == 0, "Multi-scalar inline remains one object on one line.");
        var rect = p.GetLineObjectRect(0, key); p.Alignment = HorizontalAlignment.Right; p.Width = 160; Check(p.GetLineObjectRect(0, key).Position.X > rect.Position.X && p.GetLineObjectRect(1, key) == default, "Full inline geometry follows paragraph alignment.");
        Check(!p.AddObject(key, default) && !p.ResizeObject(new TextObjectKey(), default), "Duplicate and missing keys.");
        p.ResizeObject(key, new(27, 35), InlineAlignment.Top, -2); Check(p.GetLineObjectRect(0, key).Size == new Vector2(27, 35), "Retained inline resize/baseline.");
        Check(p.GetLineUnderlinePosition(0) == Math.Max(font.GetUnderlinePosition(18), font.GetUnderlinePosition(30)) && p.GetLineUnderlineThickness(0) == Math.Max(font.GetUnderlineThickness(18), font.GetUnderlineThickness(30)), "Mixed-span underline metrics.");
        p.Alignment = HorizontalAlignment.Left; p.SetDropcap("D", font, 50, new(new(3, 4), new(5, 6)));
        Check(p.GetDropcapRID() == capRID && p.GetDropcapSize().X > 8 && p.GetSize().Y >= p.GetDropcapSize().Y, "Independent cap reservation and stable identity.");
        Check(p.GetLineObjectRect(0, key).Position.X >= p.GetDropcapSize().X, "Flow advances beyond LTR dropcap.");
        var marginsSize = p.GetDropcapSize(); using var copy = p.Duplicate(); Check(copy.GetRID() != rid && copy.GetDropcapRID() != capRID && copy.GetDropcapSize() == marginsSize && copy.HasObject(key), "Duplicate copies every policy and borrows keys/fonts.");
        copy.ClearDropcap(); Check(copy.GetDropcapSize() == default && p.GetDropcapSize() == marginsSize, "Duplicate cap mutation is independent.");
        p.Orientation = TextOrientation.Vertical; Check(p.GetLineSize(0).X >= p.GetLineAscent(0) && p.GetLineSize(0).Y == MathF.Ceiling(p.GetLineWidth(0)), "Vertical metrics use advance/cross axes.");
        p.ClearDropcap(); p.Clear(); p.Width = -1; p.AddString("AB\nCD", font, 20); var extent = p.GetLineSize(0).X;
        Check(p.HitTest(new(extent + 1, float.MaxValue)) == 5 && p.HitTest(new(-1, 3)) == 0, "Vertical hit testing advances the actual cross axis.");
        p.Clear(); p.Orientation = TextOrientation.Horizontal; p.AddString("مرحبا", arabic, 22); Check(p.GetInferredDirection() == TextDirection.RTL, "Native inferred Arabic direction.");
        p.SetBIDIOverride([new(0, 5, TextDirection.RTL)]); Reject<InvalidOperationException>(() => p.AddString("x", font, 20)); p.SetBIDIOverride([]); p.TabAlign([60]);
        using var richPolicy = new RichTextLabel(); Reject<ArgumentOutOfRangeException>(() => richPolicy.AutowrapTrimFlags = (TextLineBreakFlags)16);
        using var treePolicy = new Tree(); var cell = treePolicy.CreateItem(); Reject<ArgumentOutOfRangeException>(() => cell.SetAutowrapTrimFlags(0, (TextLineBreakFlags)16));
        Reject<ArgumentOutOfRangeException>(() => p.BreakFlags = (TextLineBreakFlags)16); Reject<ArgumentOutOfRangeException>(() => p.BreakFlags = (TextLineBreakFlags)256);
        Reject<ArgumentOutOfRangeException>(() => p.Width = float.NaN); Reject<ArgumentOutOfRangeException>(() => p.LineSpacing = float.PositiveInfinity); Reject<ArgumentOutOfRangeException>(() => p.HitTest(new(float.NaN, 0)));
        Reject<ArgumentException>(() => p.SetDropcap("x", font, 20, new(new(float.NaN, 0), default)));
        Task.Run(() => { Reject<InvalidOperationException>(() => p.GetRID()); Reject<InvalidOperationException>(() => p.Dispose()); }).GetAwaiter().GetResult(); Check(!p.IsDisposed, "Wrong-thread disposal cannot publish.");
        using var variation = new FontVariation { BaseFont = font }; p.Clear(); p.AddString("abcd efgh", variation, 18); var oldWidth = p.GetLineWidth(0); variation.SpacingGlyph = 2; Check(p.GetLineWidth(0) > oldWidth, "Font generation invalidates paragraph buffers.");
        for (var i = 0; i < 128; i++) Queries(p);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Queries(p); Check(GC.GetAllocatedBytesForCurrentThread() == before, "128 prepared paragraph scalar-query cycles allocate zero bytes.");
        using var hook = new HookFont { Fallbacks = [font] }; using var reentrant = new TextParagraph(); reentrant.AddString("ABC", hook, 20);
        hook.Once = () => { Reject<InvalidOperationException>(() => reentrant.Clear()); Reject<InvalidOperationException>(() => reentrant.GetSize()); Reject<InvalidOperationException>(() => reentrant.Dispose()); };
        Check(reentrant.GetSize().X > 0 && !reentrant.IsDisposed, "Native preparation rejects mutation/query/disposal reentry without poisoning the owner.");
        hook.Invalidate(); hook.Once = () => throw new ApplicationException("expected paragraph font failure"); Reject<ApplicationException>(() => reentrant.GetSize());
        Check(reentrant.GetSize().X > 0, "A failed font hook releases every parent/child read scope and permits retry.");
        using var changed = new FontFile { Data = FontTestFixtures.Variable }; using var trigger = new HookFont { Fallbacks = [font] }; using var coherent = new TextParagraph();
        var token = new TextObjectKey(); coherent.AddString("AA", changed, 20); coherent.AddObject(token, new(10, 10)); coherent.AddString("B", trigger, 20); coherent.GetNonWrappedSize();
        trigger.Once = () => changed.Data = FontTestFixtures.OpenSans; var coherentRect = coherent.GetLineObjectRect(0, token);
        var expectedAdvance = changed.GetStringSize("AA", fontSize: 20).X; Check(Math.Abs(coherentRect.Position.X - expectedAdvance) < 1, "A cross-font callback cannot publish old inline geometry with a new display layout: actual=" + coherentRect.Position.X + " expected=" + expectedAdvance);
        trigger.Invalidate(); trigger.Repeat = () => changed.Data = FontTestFixtures.OpenSans; Reject<InvalidOperationException>(() => coherent.GetSize()); trigger.Repeat = null; Check(coherent.GetSize().X > 0, "Unsettled cross-font replacement is bounded and the owner recovers.");
        using var cjk = new FontFile { Data = FontTestFixtures.CJK, AllowSystemFallback = false }; using var punctuation = new TextParagraph { Width = 120, Alignment = HorizontalAlignment.Fill };
        punctuation.AddString("漢A", cjk, 20); var justified = punctuation.GetLineWidth(0); punctuation.CustomPunctuation = "A"; var unexpanded = punctuation.GetLineWidth(0);
        Check(justified == 120 && unexpanded < justified, "Custom punctuation changes actual word-justification opportunities: " + justified + " -> " + unexpanded);
        using var overflow = new TextParagraph { MaxLinesVisible = 1, LineSpacing = float.MaxValue }; overflow.AddString("A\nB\nC", font, 20); Reject<OverflowException>(() => overflow.GetSize()); overflow.LineSpacing = 0; Check(overflow.GetLineCount() == 3, "Hidden derived-coordinate overflow fails explicitly and can recover.");
        using var manyLines = new TextParagraph { MaxLinesVisible = 1 }; manyLines.AddString(string.Join("\n", Enumerable.Repeat("A", 4096)), font, 16);
        Check(manyLines.GetLineCount() == 4096 && manyLines.GetLineRange(4095).Y == 8191 && manyLines.GetLineRID(4095).IsValid(), "Thousands of hidden lines retain real scalar ranges and independent identities.");
        using var deadFont = new FontFile { Data = FontTestFixtures.Variable }; using var dependent = new TextParagraph(); dependent.AddString("x", deadFont, 20); dependent.GetSize(); deadFont.Dispose(); Reject<ObjectDisposedException>(() => dependent.GetSize());
        p.Dispose(); Check(!font.IsDisposed && !arabic.IsDisposed, "Paragraph never disposes borrowed fonts."); Reject<ObjectDisposedException>(() => p.GetRID());
        Console.WriteLine("TextParagraph: full typed family, scalar line ranges, wrapping/visibility/spacing, inline/dropcap/duplicate, both axes/BIDI, font invalidation, lifetime and 128 warmed query cycles=0 managed bytes passed.");
    }
    private sealed class HookFont : FontFile
    {
        internal Action? Once;
        internal Action? Repeat;
        internal void Invalidate() => InvalidateFont();
        public override int GetSpacing(TextSpacingType spacing) { if (spacing == TextSpacingType.Glyph) { Interlocked.Exchange(ref Once, null)?.Invoke(); Repeat?.Invoke(); } return base.GetSpacing(spacing); }
    }
    private static void Queries(TextParagraph p) { p.GetSize(); p.GetRange(); p.GetLineCount(); p.GetLineRID(0); p.GetLineRange(0); p.GetLineSize(0); p.GetLineAscent(0); p.GetLineDescent(0); p.GetLineWidth(0); p.GetLineUnderlinePosition(0); p.GetLineUnderlineThickness(0); p.GetDropcapLines(); p.GetDropcapSize(); p.GetInferredDirection(); p.HitTest(new(20, 3)); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
