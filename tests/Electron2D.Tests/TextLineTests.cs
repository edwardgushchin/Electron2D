using Electron2D;

internal static class TextLineTests
{
    internal static void Run()
    {
        AllocationWarmupCheck.Verify(Collect);
        using var font = new FontFile { Data = FontTestFixtures.Variable };
        using var arabic = new FontFile { Data = FontTestFixtures.Arabic };
        using var line = new TextLine(); var rid = line.GetRID();
        Check(rid.IsValid() && line.Width == -1 && line.Alignment == HorizontalAlignment.Left && line.Direction == TextDirection.Auto && line.Orientation == TextOrientation.Horizontal, "Default identity/policies.");
        Check(line.PreserveInvalid && !line.PreserveControl && line.EllipsisChar == "…" && line.Flags == (TextJustificationFlags.Kashida | TextJustificationFlags.WordBound) && line.TextOverrunBehavior == TextOverrunBehavior.TrimEllipsis, "Remaining defaults.");
        Check(line.GetSize() == default && line.GetLineWidth() == 0 && line.GetLineAscent() == 0 && line.GetLineDescent() == 0 && line.HitTest(200) == 0, "Empty shape and caret.");
        Check(line.AddString("", font, 18) && line.SpanCount == 0, "Empty addition succeeds without a span.");
        Check(line.AddString("abcd", font, 18, "en", 47) && line.SpanCount == 1 && line.TryGetSpanMetadata<int>(0, out var metadata) && metadata == 47 && !line.TryGetSpanMetadata<string>(0, out _), "Exact typed span metadata.");
        Check(line.GetSize() == font.GetStringSize("abcd", fontSize: 18), "Single span uses the real font shaping backend.");
        Check(line.HitTest(float.MaxValue) == 4 && line.HitTest(-float.MaxValue) == 0, "Extreme-coordinate caret clamps to the visible edge.");
        Check(line.GetLineAscent() >= font.GetAscent(18) && line.GetLineDescent() >= font.GetDescent(18) && line.GetLineUnderlinePosition() == font.GetUnderlinePosition(18), "Fractional real metrics: " + line.GetLineAscent() + "/" + font.GetAscent(18) + " descent=" + line.GetLineDescent() + "/" + font.GetDescent(18) + " underline=" + line.GetLineUnderlinePosition() + "/" + font.GetUnderlinePosition(18));
        line.Orientation = TextOrientation.Vertical;
        Check(line.GetSize().Y == line.GetLineWidth() && line.HitTest(line.GetLineWidth() + 100) == 4, "Vertical advance-axis caret.");
        line.Orientation = TextOrientation.Horizontal; line.Clear(); Check(line.GetRID() == rid && line.SpanCount == 0 && line.GetSize() == default, "Clear retains identity and removes authored source.");
        var key = new TextObjectKey(); Check(line.AddObject(key, new(30, 20), length: 3) && !line.AddObject(key, new(1, 1)), "Inline-only length and duplicate token.");
        Check(line.GetLineWidth() == 30 && line.GetSize() == new Vector2(30, 20) && line.GetObjectRect(key) == new Rect2(0, 0, 30, 20), "One multi-scalar object has one advance and neutral-font geometry.");
        Check(line.HitTest(1000) == 3 && line.HasObject(key) && line.GetObjects().SequenceEqual([key]), "Object source length and authored keys.");
        Check(!line.ResizeObject(new TextObjectKey(), new(2, 2)) && line.ResizeObject(key, new(40, 24)), "Missing/present resize.");
        line.Orientation = TextOrientation.Vertical; Check(line.GetSize() == new Vector2(40, 24) && line.GetLineWidth() == 24 && line.GetObjectRect(key).Size == new Vector2(40, 24), "Vertical object dimensions are not swapped in its rectangle.");
        line.Orientation = TextOrientation.Horizontal; line.Width = 100; line.Alignment = HorizontalAlignment.Center; Check(line.GetObjectRect(key).Position.X == 30, "Centered object alignment.");
        line.Alignment = HorizontalAlignment.Right; Check(line.GetObjectRect(key).Position.X == 60, "Right object alignment.");
        line.Clear(); line.Width = -1; line.Alignment = HorizontalAlignment.Left; line.AddString("A", font, 16); line.AddObject(key, new(12, 50), (InlineAlignment)11, 2, -3); line.AddString("B", font, 32);
        Check(line.GetLineAscent() >= font.GetAscent(32) && line.GetLineDescent() >= 50, "Global mixed-font alignment and signed baseline.");
        var fullRect = line.GetObjectRect(key); line.TextOverrunBehavior = TextOverrunBehavior.TrimChar; line.Width = 1;
        Check(line.HasObject(key) && line.GetObjectRect(key) == fullRect && line.HitTest(10000) == 4, "Trimmed display retains complete inline and caret geometry.");
        using var duplicate = line.Duplicate(); Check(duplicate.GetRID() != rid && duplicate.Width == line.Width && duplicate.HasObject(key) && duplicate.GetObjectRect(key) == line.GetObjectRect(key), "Independent copied buffer and shared token.");
        duplicate.Clear(); Check(line.HasObject(key), "Duplicate clearing is independent.");
        line.Clear(); line.Width = -1; line.AddString("A\tB", font, 18); line.TabAlign([60]); Check(line.GetLineWidth() > 60, "Repeating authored tab intervals.");
        line.Clear(); line.AddString("مرحبا", arabic, 22); Check(line.GetInferredDirection() == TextDirection.RTL, "Native Arabic inferred direction.");
        line.Clear(); line.AddString("A😀B", font, 18); line.SetBIDIOverride([new(0, 3, TextDirection.LTR)]);
        Reject<InvalidOperationException>(() => line.AddString("x", font, 18)); line.SetBIDIOverride([]); Check(line.HitTest(10000) == 3, "Scalar BIDI range and supplementary caret indexing.");
        Reject<ArgumentException>(() => line.SetBIDIOverride([new(0, 2, TextDirection.LTR)])); Reject<ArgumentException>(() => line.SetBIDIOverride([new(1, 3, TextDirection.LTR)]));
        using var missing = new FontFile { AllowSystemFallback = false }; line.Clear(); line.AddString("\uEFFF", missing, 32);
        Check(line.GetSize() == missing.GetStringSize("\uEFFF", fontSize: 32), "Missing box uses its authored span size.");
        var missingWidth = line.GetLineWidth(); line.PreserveInvalid = false; Check(missingWidth > 0 && line.GetLineWidth() == 0, "Missing character policy actually suppresses boxes.");
        line.Clear(); line.AddString("\u0001", missing, 32); line.PreserveControl = true; Check(line.GetLineWidth() > 0, "Control display works independently of invalid display.");
        Reject<ArgumentOutOfRangeException>(() => line.Width = float.NaN); Reject<ArgumentOutOfRangeException>(() => line.Orientation = (TextOrientation)99); Reject<ArgumentOutOfRangeException>(() => line.Flags = (TextJustificationFlags)256);
        Reject<ArgumentException>(() => line.AddObject(key, new(float.NaN, 0))); Reject<ArgumentOutOfRangeException>(() => line.AddObject(key, default, length: 0)); Reject<ArgumentOutOfRangeException>(() => line.TabAlign([0]));
        Reject<ArgumentOutOfRangeException>(() => line.HitTest(float.NaN)); Reject<ArgumentOutOfRangeException>(() => line.TryGetSpanMetadata<int>(99, out _));
        Task.Run(() => { Reject<InvalidOperationException>(() => line.GetRID()); Reject<InvalidOperationException>(() => line.Dispose()); }).GetAwaiter().GetResult(); Check(!line.IsDisposed, "Off-owner disposal cannot publish.");
        using var variation = new FontVariation { BaseFont = font };
        line.Clear(); line.PreserveInvalid = true; line.PreserveControl = false; line.TextOverrunBehavior = TextOverrunBehavior.NoTrimming; line.AddString("abcd", variation, 18);
        var oldWidth = line.GetLineWidth(); variation.SpacingGlyph = 2; Check(line.GetLineWidth() > oldWidth, "Borrowed font generation invalidates buffers.");
        Collect();
        for (var i = 0; i < 128; i++) Queries(line);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Queries(line);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"128 prepared TextLine metric/caret cycles allocate zero bytes; observed {allocated} bytes.");
        using var ellipsisLine = new TextLine(); var defaultFont = ThemeDB.FallbackFont!; ellipsisLine.AddString("A long line for trimming", defaultFont, 24); ellipsisLine.Width = 1; ellipsisLine.TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce;
        Check(ellipsisLine.GetLineWidth() == defaultFont.GetStringSize("…", fontSize: 24).X, "Ellipsis uses the last authored span font/size, not the neutral owner.");
        var dead = new TextLine(); dead.AddString("x", font, 18); dead.Dispose(); Check(font.GetStringSize("x").X > 0, "Line cleanup never disposes borrowed fonts."); Reject<ObjectDisposedException>(() => dead.GetRID());
        var disposedFont = new FontFile { Data = FontTestFixtures.Variable }; using var dependent = new TextLine(); dependent.AddString("x", disposedFont, 18); dependent.GetSize(); disposedFont.Dispose(); Reject<ObjectDisposedException>(() => dependent.GetSize());
        Console.WriteLine("TextLine: typed spans/metadata/keys, native shaping, both axes, inline length/baseline, complete geometry versus trim, BIDI/tabs/control/missing, duplication/owner/lifetime/font invalidation and 128 warmed metric/caret cycles=0 managed bytes passed.");
    }
    private static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers(); }
    private static void Queries(TextLine line) { line.GetSize(); line.GetLineAscent(); line.GetLineDescent(); line.GetLineWidth(); line.GetLineUnderlinePosition(); line.GetLineUnderlineThickness(); line.GetInferredDirection(); line.HitTest(20); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
