using Electron2D;

internal static class TextServerTests
{
    internal static void Run()
    {
        var ts = TextServerManager.GetPrimaryInterface();
        Check(ReferenceEquals(ts, TextServerManager.GetInterface(0)) && ReferenceEquals(ts, TextServerManager.FindInterface(ts.GetName())) && TextServerManager.GetInterfaceCount() == 1 && TextServerManager.GetInterfaces().SequenceEqual([ts]), "Retained provider discovery.");
        Check(Engine.GetSingleton<TextServerManager>(nameof(TextServerManager)) is not null && TextServerManager.FindInterface("missing") is null, "Named service identity.");
        Reject<InvalidOperationException>(() => ts.Dispose()); Reject<InvalidOperationException>(() => Engine.GetSingleton<TextServerManager>(nameof(TextServerManager)).Dispose());
        Reject<ArgumentOutOfRangeException>(() => TextServerManager.GetInterface(1)); Reject<ArgumentNullException>(() => TextServerManager.FindInterface(null!));
        Reject<ArgumentOutOfRangeException>(() => ts.CreateShapedText((TextDirection)99)); Reject<ArgumentOutOfRangeException>(() => ts.CreateShapedText(TextDirection.Inherited));
        using var font = new FontFile { Data = FontTestFixtures.Variable, AllowSystemFallback = false };
        using var arabic = new FontFile { Data = FontTestFixtures.Arabic, AllowSystemFallback = false };
        var rid = ts.CreateShapedText(); var key = new TextObjectKey();
        try
        {
            Check(ts.Has(rid) && !ts.Has(default) && ts.Has(font.GetRID()) && !ts.ShapedTextIsReady(rid), "Live ownership and initial preparation state.");
            Check(ts.ShapedTextGetRange(rid) == default && ts.ShapedGetText(rid) == "" && !ts.ShapedTextIsReady(rid), "Authoring queries do not prepare native layout.");
            Check(ts.ShapedTextGetRange(rid) == default && ts.ShapedTextGetSize(rid) == default && ts.ShapedTextHitTestPosition(rid, 1e30f) == 0, "Empty buffer geometry.");
            Check(ts.ShapedTextIsReady(rid) && ts.ShapedTextShape(rid), "Queries prepare the native buffer.");
            ts.ShapedTextAddString(rid, "A😀e\u0301 ", [font], 23, null, "en", 42);
            Check(!ts.ShapedTextIsReady(rid), "Authoring invalidates shape readiness.");
            Check(ts.ShapedTextGetRange(rid) == new Vector2i(0, 5) && ts.ShapedGetText(rid) == "A😀e\u0301 ", "Unicode scalar authoring.");
            Check(ts.ShapedTextGetWidth(rid) > 0 && ts.ShapedTextGetAscent(rid) > 0 && ts.ShapedTextGetDescent(rid) >= 0 && ts.ShapedTextGetUnderlineThickness(rid) == font.GetUnderlineThickness(23), "Actual native metrics: " + ts.ShapedTextGetWidth(rid) + "/" + ts.ShapedTextGetAscent(rid) + "/" + ts.ShapedTextGetDescent(rid) + "/" + ts.ShapedTextGetUnderlineThickness(rid) + " font=" + font.GetAscent(23) + "/" + font.GetDescent(23) + "/" + font.GetUnderlineThickness(23));
            Check(ts.ShapedTextHitTestPosition(rid, float.MaxValue) == 5 && ts.ShapedTextHitTestPosition(rid, -float.MaxValue) == 0, "Extreme hit boundaries.");
            Check(ts.ShapedTextAddObject(rid, key, new(25, 30), length: 3) && !ts.ShapedTextAddObject(rid, key, new(2, 2)), "Unique inline object.");
            Check(ts.ShapedGetSpanCount(rid) == 2 && ts.ShapedGetSpanText(rid, 0) == "A😀e\u0301 " && ts.ShapedGetSpanObject(rid, 1) == key && ts.ShapedGetSpanEmbeddedObject(rid, 0) is null && ts.TryGetSpanMetadata<int>(rid, 0, out var meta) && meta == 42 && !ts.TryGetSpanMetadata<string>(rid, 0, out _), "Text/object span indexing and exact metadata.");
            Check(ts.ShapedTextGetObjectRange(rid, key) == new Vector2i(5, 8) && ts.ShapedTextHasObject(rid, key) && ts.ShapedTextGetObjects(rid).SequenceEqual([key]), "Scalar object range and order.");
            Reject<KeyNotFoundException>(() => ts.ShapedTextGetObjectRange(rid, new())); Reject<KeyNotFoundException>(() => ts.ShapedTextGetObjectRect(rid, new())); Check(!ts.ShapedTextHasObject(rid, new()), "Missing object presence is false.");
            Check(ts.ShapedTextGetObjectRect(rid, key).Size == new Vector2(25, 30) && ts.ShapedTextGetObjectRect(rid, key).Position.Y < 0, "Baseline object geometry.");
            Check(ts.ShapedTextResizeObject(rid, key, new(35, 30)) && !ts.ShapedTextResizeObject(rid, new(), new(1, 1)), "Object mutation.");
            var copy = ts.ShapedTextDuplicate(rid);
            ts.FreeRID(rid); rid = default;
            try { Check(ts.ShapedTextGetWidth(copy) > 35 && ts.ShapedTextGetObjectRect(copy, key).Size.X == 35, "Duplicate retains independent font-wrapper ownership after original release."); }
            finally { ts.FreeRID(copy); }
            Check(!ts.Has(copy), "Released identity is retired."); Reject<InvalidOperationException>(() => ts.FreeRID(copy));
            rid = ts.CreateShapedText(); ts.ShapedTextAddString(rid, "مرحبا", [font, arabic], 25, language: "ar");
            Check(ts.ShapedTextGetInferredDirection(rid) == TextDirection.RTL && ts.ShapedTextGetWidth(rid) > 0, "Ordered font list shapes Arabic.");
            Check(ts.ShapedTextHitTestPosition(rid, -float.MaxValue) == 5 && ts.ShapedTextHitTestPosition(rid, float.MaxValue) == 0, "RTL visual edge hits preserve logical scalar positions.");
            ts.ShapedTextSetOrientation(rid, TextOrientation.Vertical); var vertical = ts.ShapedTextGetSize(rid);
            Check(vertical.Y == ts.ShapedTextGetWidth(rid) && ts.ShapedTextGetOrientation(rid) == TextOrientation.Vertical, "Vertical scalar buffer.");
            ts.ShapedTextSetPreserveControl(rid, true); ts.ShapedTextSetPreserveInvalid(rid, false); ts.ShapedTextClear(rid);
            Check(ts.ShapedTextGetPreserveControl(rid) && !ts.ShapedTextGetPreserveInvalid(rid) && ts.ShapedGetText(rid) == "", "Clear retains policies.");
            Reject<ArgumentException>(() => ts.ShapedTextAddString(rid, "x", [], 20));
            Reject<ArgumentOutOfRangeException>(() => ts.ShapedTextSetOrientation(rid, (TextOrientation)3));
            Reject<ArgumentOutOfRangeException>(() => ts.ShapedTextHitTestPosition(rid, float.NaN));
            var captured = rid; Task.Run(() => Reject<InvalidOperationException>(() => ts.ShapedTextGetWidth(captured))).GetAwaiter().GetResult();
        }
        finally { if (rid.IsValid()) ts.FreeRID(rid); }
        using var line = new TextLine(); line.AddString("Borrowed", font, 20); var borrowed = line.GetRID();
        Check(ts.Has(borrowed) && ts.ShapedTextGetRange(borrowed) == new Vector2i(0, 8), "TextLine shares actual buffer owner.");
        line.Width = 30; line.TextOverrunBehavior = TextOverrunBehavior.TrimEllipsisForce;
        Check(ts.ShapedTextGetWidth(borrowed) == line.GetLineWidth() && ts.ShapedTextGetSize(borrowed) == line.GetSize(), "Borrowed IDs retain the actual trimmed display policy and ceiled size contract.");
        line.Width = -1;
        Reject<InvalidOperationException>(() => ts.FreeRID(borrowed)); ts.ShapedTextAddString(borrowed, "!", [font], 20);
        Check(line.HitTest(1e30f) == 9, "Borrowed authoring updates original line."); line.Dispose(); Check(!ts.Has(borrowed), "TextLine disposal retires borrowed ID.");
        using var paragraph = new TextParagraph { Width = 65 }; paragraph.AddString("First second third", font, 20);
        var lineRID = paragraph.GetLineRID(1);
        Check(ts.ShapedTextGetRange(lineRID) == paragraph.GetLineRange(1), "Paragraph line range interoperates.");
        Check(MathF.Ceiling(ts.ShapedTextGetWidth(lineRID)) == paragraph.GetLineWidth(1), "Paragraph line fractional metrics.");
        var before = paragraph.GetSize(); ts.ShapedTextAddString(paragraph.GetRID(), " fourth", [font], 30);
        Check(paragraph.GetRange().Y == 25 && paragraph.GetSize() != before && !ts.Has(lineRID), "Source authoring reflows paragraph and retires previous line handles.");
        PreparedUnderline(ts); Features(ts, font); MutationAliases(ts, font); BIDIViews(ts, font, arabic); Reentry(ts, font); Warm(ts, font);
        Console.WriteLine("TextServer: native buffers, ownership, source aliases, scalar metrics, ordered fonts, copies, failures and warm queries passed.");
    }
    private static void Features(TextServer ts, Font primary)
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        using var variation = new FontVariation { BaseFont = font, SpacingGlyph = 2 };
        var rid = ts.CreateShapedText();
        try
        {
            var features = new Dictionary<string, int> { ["liga"] = 0 }; Font[] fonts = [font];
            ts.ShapedTextAddString(rid, "ffi", fonts, 30, features); features["liga"] = 1; fonts[0] = primary;
            Check(ts.ShapedTextGetGlyphCount(rid) == 3, "Copied fonts/features control actual native ligature shaping.");
            ts.ShapedTextClear(rid); ts.ShapedTextAddString(rid, "ffi", [font], 30);
            Check(ts.ShapedTextGetGlyphCount(rid) < 3, "Default native ligature remains active.");
            Check(ts.ShapedTextHitTestPosition(rid, ts.ShapedTextGetWidth(rid) / 3) == 1, "Low-level scalar hit testing interpolates inside a native ligature.");
            ts.ShapedTextClear(rid); ts.ShapedTextAddString(rid, "ABC", [variation], 20);
            Check(MathF.Ceiling(ts.ShapedTextGetWidth(rid)) == variation.GetStringSize("ABC", fontSize: 20).X, "Borrowed font spacing survives the source-list adapter.");
            ts.ShapedTextClear(rid); using var empty = new FontFile { AllowSystemFallback = false };
            ts.ShapedTextAddString(rid, "ABC", [empty, variation], 20);
            Check(ts.ShapedTextGetWidth(rid) == variation.GetStringSize("ABC", fontSize: 20).X, "Selected fallback keeps its own spacing policy.");
            var width = ts.ShapedTextGetWidth(rid); variation.SpacingGlyph = 5;
            Check(!ts.ShapedTextIsReady(rid) && ts.ShapedTextGetWidth(rid) > width, "Borrowed generation changes invalidate prepared buffers.");
            ts.ShapedTextClear(rid); ts.ShapedTextAddString(rid, "A", [primary, font], 20);
            Check(ts.ShapedTextGetAscent(rid) == primary.GetAscent(20) && ts.ShapedTextGetUnderlineThickness(rid) == primary.GetUnderlineThickness(20), "Unused later sources cannot inflate selected font metrics.");
            ts.ShapedTextClear(rid); ts.ShapedTextAddString(rid, "ABC", [variation], 20);
            var previous = ts.ShapedGetText(rid); Reject<ArgumentException>(() => ts.ShapedTextAddString(rid, new string('A', 1048577), [font], 20));
            Check(ts.ShapedGetText(rid) == previous, "Oversize authoring fails before publication.");
            variation.Dispose(); Reject<ObjectDisposedException>(() => ts.ShapedTextGetWidth(rid));
        }
        finally { ts.FreeRID(rid); }
    }
    private static void MutationAliases(TextServer ts, Font font)
    {
        using var p = new TextParagraph { Width = 70 }; p.AddString("First second third", font, 20);
        var rid = p.GetLineRID(1); var originalText = ts.ShapedGetText(p.GetRID()); var originalWidth = p.GetLineWidth(1);
        Check(ts.ShapedGetText(rid) == originalText && ts.ShapedGetSpanText(rid, 0) == originalText, "Line views expose the underlying authored source and local span index.");
        Reject<ArgumentOutOfRangeException>(() => ts.ShapedTextSetOrientation(rid, (TextOrientation)99));
        ts.ShapedTextSetOrientation(rid, TextOrientation.Horizontal); ts.ShapedTextAddString(rid, "", [font], 20);
        Check(p.GetLineWidth(1) == originalWidth && ts.ShapedTextGetRange(rid) == p.GetLineRange(1), "Failed first line edit preserves its original live view.");
        var sourceRange = ts.ShapedTextGetRange(rid); ts.ShapedTextAddString(rid, "AA", [font], 36);
        Check(ts.ShapedTextGetRange(rid).X == sourceRange.X && ts.ShapedGetText(p.GetRID()) == originalText && p.GetLineWidth(1) > originalWidth, "Line copy-on-write is independent and updates the real paragraph consumer.");
        var currentRange = ts.ShapedTextGetRange(rid); ts.ShapedTextSetBIDIOverride(rid, [new(currentRange.X, currentRange.Y, TextDirection.LTR)]);
        ts.ShapedTextSetBIDIOverride(rid, []); var key = new TextObjectKey(); ts.ShapedTextAddObject(rid, key, new(80, 40));
        Check(p.GetLineObjects(1).SequenceEqual([key]) && p.GetLineObjectRect(1, key).Size == new Vector2(80, 40) && p.GetSize().X >= 80, "Detached line objects reach paragraph metrics and geometry.");
        ts.ShapedTextSetOrientation(rid, TextOrientation.Vertical);
        Check(p.GetLineSize(1) == ts.ShapedTextGetSize(rid).Ceil(), "Detached orientation reaches actual paragraph line dimensions.");
        ts.ShapedTextClear(rid); Check(ts.ShapedTextGetRange(rid) == default && p.GetLineSize(1) == default, "Clear resets detached buffer range and geometry.");
        p.Width = 90; p.GetSize(); Check(!ts.Has(rid), "A later source reflow retires detached line identity.");
    }
    private static void BIDIViews(TextServer ts, Font latin, Font arabic)
    {
        using var p = new TextParagraph { Width = 75 }; p.AddString("مرحبا ", arabic, 20); p.AddString("ABC DEF GHI", latin, 20);
        var id = p.GetLineRID(1); var direction = ts.ShapedTextGetInferredDirection(id);
        Check(direction == TextDirection.RTL, "A wrapped Latin line retains its Arabic paragraph direction.");
        ts.ShapedTextSetPreserveInvalid(id, false);
        Check(ts.ShapedTextGetInferredDirection(id) == direction, "Detached line shaping keeps the surrounding source BIDI context.");
        var full = ts.ShapedGetText(id); var range = ts.ShapedTextGetRange(id); var slice = string.Concat(full.EnumerateRunes().Skip(range.X).Take(range.Y - range.X).Select(r => r.ToString()));
        Check(slice.Length > 0 && full.StartsWith("مرحبا "), "Detached underlying source retains global scalar coordinates.");
    }
    private sealed class HookFont : FontFile
    {
        internal Action? Once;
        internal Action? TopOnce;
        public override int GetSpacing(TextSpacingType spacing) { if (spacing == TextSpacingType.Glyph) Interlocked.Exchange(ref Once, null)?.Invoke(); if (spacing == TextSpacingType.Top) Interlocked.Exchange(ref TopOnce, null)?.Invoke(); return base.GetSpacing(spacing); }
    }
    private static void PreparedUnderline(TextServer ts)
    {
        using var first = new HookFont { Data = FontTestFixtures.Variable, AllowSystemFallback = false };
        using var fallback = new FontFile { Data = FontTestFixtures.Arabic, AllowSystemFallback = false };
        var rid = ts.CreateShapedText();
        try
        {
            ts.ShapedTextAddString(rid, "مرحبا", [first, fallback], 20); ts.ShapedTextShape(rid);
            var expected = ts.ShapedTextGetUnderlineThickness(rid); Check(expected > 0, "The selected native fallback supplies a real underline metric.");
            first.TopOnce = () => fallback.Data = (byte[])FontTestFixtures.Arabic.Clone();
            Check(ts.ShapedTextGetUnderlineThickness(rid) == expected && first.TopOnce is not null, "Prepared underline snapshots do not reenter fonts or race retired source faces.");
            first.TopOnce = null;
        }
        finally { ts.FreeRID(rid); }
    }
    private static void Reentry(TextServer ts, Font font)
    {
        using var hook = new HookFont { Fallbacks = [font] }; var rid = ts.CreateShapedText();
        try
        {
            ts.ShapedTextAddString(rid, "ABC", [hook], 20);
            hook.Once = () => { Reject<InvalidOperationException>(() => ts.ShapedTextClear(rid)); Reject<InvalidOperationException>(() => ts.FreeRID(rid)); Reject<InvalidOperationException>(() => ts.ShapedTextShape(rid)); Reject<InvalidOperationException>(() => ts.ShapedGetText(rid)); };
            Check(ts.ShapedTextGetWidth(rid) > 0 && ts.Has(rid), "Active native work rejects reentry/mutation/free without poisoning ownership.");
            using var p = new TextParagraph(); p.AddString("ABC", hook, 20); var source = p.GetRID();
            hook.Once = () => Reject<InvalidOperationException>(() => ts.ShapedTextAddString(source, "x", [font], 20));
            Check(p.GetSize().X > 0 && ts.ShapedGetText(source) == "ABC", "A paragraph's service alias rejects active-owner authoring before mutation.");
        }
        finally { ts.FreeRID(rid); }
    }
    private static void Warm(TextServer ts, Font font)
    {
        var rid = ts.CreateShapedText();
        try
        {
            ts.ShapedTextAddString(rid, "Prepared text", [font], 20);
            for (var i = 0; i < 128; i++) Queries(ts, rid);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Queries(ts, rid);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "128 prepared query cycles allocate zero owner bytes.");
        }
        finally { ts.FreeRID(rid); }
    }
    private static void Queries(TextServer ts, RID rid)
    { ts.ShapedTextGetWidth(rid); ts.ShapedTextGetAscent(rid); ts.ShapedTextGetDescent(rid); ts.ShapedTextGetSize(rid); ts.ShapedTextGetRange(rid); ts.ShapedTextHitTestPosition(rid, 30); ts.ShapedTextIsReady(rid); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
