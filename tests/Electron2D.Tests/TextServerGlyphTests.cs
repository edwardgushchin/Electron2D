using Electron2D;

internal static class TextServerGlyphTests
{
    private static readonly TextServer S = TextServerManager.GetPrimaryInterface();
    internal static void Run()
    {
        ResourceSources(); BorrowedLifetime(); VariationSources(); GlyphsAndRuns(); AutomaticSources(); AutomaticLinkedSources(); ParagraphLines(); ConcurrentSources(); CallbackFailure();
        Console.WriteLine("Resource font RIDs, borrowed cache mutation/lifetime, source projections, full visual/logical glyphs, runs, slices, objects, ellipsis and automatic fallback passed.");
    }
    private static void ResourceSources()
    {
        using var first = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        using var second = new FontFile { Data = FontTestFixtures.Hebrew, AllowSystemFallback = false };
        first.Fallbacks = [second, null, second]; var rid = first.GetRID();
        Check(first.GetRIDs().SequenceEqual(new[] { rid, second.GetRID(), second.GetRID() }), "Ordered explicit source identities preserve duplicates and skip nulls.");
        var ids = first.GetRIDs(); ids[0] = default; Check(first.GetRIDs()[0] == rid && S.Has(rid), "Copied RID arrays and live borrowed lookup.");
        Reject<InvalidOperationException>(() => S.FreeRID(rid));
        var projection = S.FontGetResource(rid); Check(projection.GetRID() == rid && ReferenceEquals(projection, S.FontGetResource(rid)) && projection is not FontFile, "Stable runtime borrowed projection.");
        var buffer = S.CreateShapedText(); var link = S.CreateFontLinkedVariation(rid); var sibling = S.CreateFontLinkedVariation(link);
        try
        {
            S.ShapedTextAddString(buffer, "AAA", [first], 24); var width = S.ShapedTextGetWidth(buffer);
            S.FontSetSpacing(rid, TextSpacingType.Glyph, 3);
            Near(S.ShapedTextGetWidth(buffer), width + 6, .01f, "Raw spacing reaches the ordinary resource consumer");
            S.FontSetName(rid, "source edit"); Check(first.GetFontName() == "source edit" && first.GetRID() == rid, "Raw metadata and source identity share ownership.");
            using var copy = (Font)projection.Duplicate(); var copyWidth = copy.GetStringSize("AAA", fontSize: 24).X;
            projection.Dispose(); Check(S.Has(rid) && !first.IsDisposed && S.FontGetResource(rid) != projection, "Disposing borrowed projection preserves source and recreates its runtime view.");
            S.FontSetData(rid, FontTestFixtures.OpenSans); Check(first.GetRID() == rid && S.FontGetName(link) == first.GetFontName(), "Data replacement preserves cache identity and linked source.");
            S.FreeRID(link); link = default; Check(S.FontGetName(sibling) == first.GetFontName(), "A link of a resource link retains the original source.");
            var retired = S.FontGetResource(rid); first.ClearCache();
            Check(!S.Has(rid) && first.GetRID() != rid && S.Has(sibling), "Removing primary cache retires borrowed identity but retains owned links.");
            Reject<InvalidOperationException>(() => retired.GetStringSize("retired")); Reject<InvalidOperationException>(() => S.FontGetName(sibling));
            Near(copy.GetStringSize("AAA", fontSize: 24).X, copyWidth, 0, "Duplicate owns an independent source snapshot");
            rid = first.GetRID(); first.RemoveCache(0); Check(!S.Has(rid) && first.GetRID() != rid, "Explicit primary cache removal retires its RID.");
            var resetRID = first.GetRID(); first.ResetState(); Check(!S.Has(resetRID) && first.GetRID() != resetRID, "Full resource reset retires its cleared cache identity.");
            var current = first.GetRID(); first.Dispose(); Check(!S.Has(current), "Resource disposal retires its actual source.");
        }
        finally { S.FreeRID(buffer); if (S.Has(link)) S.FreeRID(link); if (S.Has(sibling)) S.FreeRID(sibling); }
    }
    private static void BorrowedLifetime()
    {
        using var source = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        var (owner, view, rid) = Create(source);
        Collect(); Check(Alive(owner) && S.Has(rid) && view.GetStringSize("live").X > 0, "Live borrowed source view keeps its resource owner alive.");
        var link = S.CreateFontLinkedVariation(rid);
        view.Dispose(); Collect(); Check(Alive(owner) && S.Has(rid), "An owned resource-linked entry retains its original resource owner.");
        S.FreeRID(link); Collect(); Check(!Alive(owner) && !S.Has(rid), "Disposed views/links release their owner; weak RID registration does not retain it.");
        GC.KeepAlive(view);
        static void Collect() { for (var i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); } }
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static bool Alive(WeakReference<FontVariation> weak) => weak.TryGetTarget(out _);
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (WeakReference<FontVariation>, Font, RID) Create(Font source)
    { var owner = new FontVariation { BaseFont = source }; var rid = owner.GetRID(); return (new(owner), S.FontGetResource(rid), rid); }
    private static void VariationSources()
    {
        using var source = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        using var variation = new FontVariation { BaseFont = source };
        using var customFile = new CustomSpacingFile { Fallbacks = [source], AllowSystemFallback = false };
        using var customVariation = new CustomSpacingVariation { BaseFont = source };
        var plainWidth = source.GetStringSize("AA", fontSize: 24).X;
        Near(customFile.GetStringSize("AA", fontSize: 24).X, plainWidth + 3, .01f, "File fallback retains the public virtual spacing policy");
        Near(customVariation.GetStringSize("AA", fontSize: 24).X, plainWidth + 3, .01f, "Variation source retains the public virtual spacing policy");
        var rid = variation.GetRID(); var raw = S.FontGetResource(rid); var baseline = variation.GetStringSize("AAA", fontSize: 24).X;
        S.FontSetSpacing(rid, TextSpacingType.Glyph, 4);
        Near(variation.GetStringSize("AAA", fontSize: 24).X, baseline + 8, .01f, "Variation raw source spacing reaches actual shaping");
        Check(variation.GetSpacing(TextSpacingType.Glyph) == 0, "Runtime cache edits preserve authored variation preferences.");
        variation.SetSpacing(TextSpacingType.Glyph, 2); Check(variation.GetRID() == rid && S.FontGetSpacing(rid, TextSpacingType.Glyph) == 2, "Authored spacing mirrors into existing source cache.");
        Near(variation.GetStringSize("AAA", fontSize: 24).X, baseline + 4, .01f, "Authored spacing updates prepared consumers");
        variation.BaselineOffset = .25f; Check(!S.Has(rid) && variation.GetRID() != rid, "Native variation rebuild retires previous cache.");
        Reject<InvalidOperationException>(() => raw.GetRID());
        rid = variation.GetRID(); var link = S.CreateFontLinkedVariation(rid); var a = Buffer(variation, "A"); var b = Buffer(S.FontGetResource(link), "A");
        try
        {
            var original = S.ShapedTextGetGlyphs(a)[0]; var linked = S.ShapedTextGetGlyphs(b)[0];
            Check(original.FontRID == rid && linked.FontRID == link, "Glyph sources retain resource and linked projection identities.");
            Near(linked.Offset.Y, 0, .001f, "Zero linked baseline cancels the source native baseline");
            Check(original.Offset.Y > 0, "Ordinary variation retains native baseline path.");
            S.FontSetBaselineOffset(link, .5f); linked = S.ShapedTextGetGlyphs(b)[0];
            Near(linked.Offset.Y, (S.FontGetAscent(rid, 24) + S.FontGetDescent(rid, 24)) * .5f, .01f, "Independent linked baseline applies exactly once");
            var before = S.ShapedTextGetWidth(a); S.FontSetSpacing(rid, TextSpacingType.Glyph, 5); Check(S.ShapedTextGetWidth(a) == before, "A single last glyph has no trailing spacing.");
        }
        finally { S.FreeRID(a); S.FreeRID(b); S.FreeRID(link); }
        using var system = new SystemFont(); var systemRID = system.GetRID();
        Check(systemRID != default && S.Has(systemRID) && S.FontGetFaceCount(systemRID) > 0, "Resolved theme source has an actual service-readable native face.");
        var systemWidth = system.GetStringSize("AAA", fontSize: 24).X; S.FontSetSpacing(systemRID, TextSpacingType.Glyph, 3);
        Near(system.GetStringSize("AAA", fontSize: 24).X, systemWidth + 6, .01f, "System resource consumes its runtime source cache");
        system.FontWeight = 700; Check(!S.Has(systemRID) && system.GetRID() != systemRID, "System matching change retires previous native source.");
    }
    private static void GlyphsAndRuns()
    {
        using var latin = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        using var fallback = new FontFile { Data = FontTestFixtures.Arabic, AllowSystemFallback = false };
        using var arabic = new FontFile { Data = FontTestFixtures.Arabic, AllowSystemFallback = false };
        latin.Fallbacks = [fallback]; var id = S.CreateShapedText(); var owned = new List<RID> { id }; var key = new TextObjectKey();
        try
        {
            S.ShapedTextAddString(id, "ffi A\u0308 ", [latin], 24, language: "en");
            S.ShapedTextAddObject(id, key, new(13, 17), length: 2);
            S.ShapedTextAddString(id, "سلام", [latin], 30, language: "ar");
            var glyphs = S.ShapedTextGetGlyphs(id); Check(glyphs.Length == S.ShapedTextGetGlyphCount(id), "Full raw record count.");
            Check(glyphs.Any(g => g.End - g.Start > 1 && g.FontRID == latin.GetRID()), "Native ligature source and scalar range.");
            Check(glyphs.Any(g => g.FontRID == fallback.GetRID() && g.FontSize == 30 && (g.Flags & TextGraphemeFlags.RTL) != 0 && g.SpanIndex == 2), "Actual explicit fallback and bidi metadata: " + string.Join(";", glyphs.Select(g => $"{g.Start}:{g.End}/{g.FontRID}/{g.FontSize}/{g.Flags}/span{g.SpanIndex}")) + "; expected=" + fallback.GetRID());
            CheckClusters(glyphs);
            var objectGlyph = S.ShapedTextGetObjectGlyph(id, key); Check(objectGlyph >= 0 && glyphs[objectGlyph].Start == 7 && glyphs[objectGlyph].End == 9 && glyphs[objectGlyph].SpanIndex == 1 && (glyphs[objectGlyph].Flags & TextGraphemeFlags.EmbeddedObject) != 0, "Inline object visual glyph and authored interleaving.");
            Check(S.ShapedTextGetObjectGlyph(id, new()) == -1, "Missing object sentinel.");
            var logical = S.ShapedTextSortLogical(id); Check(logical.Select(g => g.Start).SequenceEqual(logical.Select(g => g.Start).Order()) && logical.Length == glyphs.Length && S.ShapedTextGetGlyphs(id).SequenceEqual(glyphs), "Logical sorting leaves visual records unchanged.");
            glyphs[0] = default; Check(S.ShapedTextGetGlyphs(id)[0] != default, "Caller snapshots are independent.");
            var runs = S.ShapedGetRunCount(id); Check(runs >= 3, "Span/source/size/direction divide visual runs.");
            var covered = 0;
            for (var i = 0; i < runs; i++)
            {
                var range = S.ShapedGetRunRange(id, i); var gl = S.ShapedGetRunGlyphRange(id, i); var all = S.ShapedTextGetGlyphs(id);
                Check(gl.X == covered && gl.Y >= gl.X && gl.Y < all.Length, "Inclusive run leader ranges partition visual glyphs."); covered = gl.Y + all[gl.Y].Count;
                Check(S.ShapedGetRunFontRID(id, i) == all[gl.X].FontRID && S.ShapedGetRunFontSize(id, i) == all[gl.X].FontSize, "Run font/size match actual glyph producer.");
                Check(S.ShapedGetRunText(id, i).EnumerateRunes().Count() == range.Y - range.X, "Run text uses scalar coordinates.");
                if (ReferenceEquals(S.ShapedGetRunObject(id, i), key)) Check(range == new Vector2i(7, 9) && S.ShapedGetRunLanguage(id, i) == "", "Object run keeps borrowed identity and no language.");
                else if (S.ShapedGetRunDirection(id, i) == TextDirection.RTL) Check(S.ShapedGetRunLanguage(id, i) == "ar", "RTL run language follows authored span.");
            }
            Check(covered == logical.Length, "Run ranges cover all raw glyph records.");
            Reject<ArgumentOutOfRangeException>(() => S.ShapedGetRunText(id, -1)); Reject<ArgumentOutOfRangeException>(() => S.ShapedGetRunRange(id, runs));
            var slice = S.ShapedTextSubstr(id, 9, 4); owned.Add(slice);
            Check(S.ShapedTextGetGlyphs(slice).All(g => g.Start >= 9 && g.End <= 13 && g.SpanIndex == 2) && S.ShapedGetRunText(slice, 0) == "سلام", "Substring preserves global scalar origin and original span identity.");
            var partial = S.ShapedTextSubstr(id, 8, 1); owned.Add(partial); Check(S.ShapedTextGetGlyphs(partial).Length == 0 && S.ShapedTextGetObjectGlyph(partial, key) == -1 && S.ShapedGetRunCount(partial) == 0, "Partial object has no copied glyph/run.");
            var trim = Buffer(latin, "A long line with several words and ligatures ffi"); owned.Add(trim);
            var before = S.ShapedTextGetGlyphs(trim); S.ShapedTextOverrunTrimToWidth(trim, 130, TextOverrunFlags.Trim | TextOverrunFlags.AddEllipsis | TextOverrunFlags.EnforceEllipsis);
            var ellipsis = S.ShapedTextGetEllipsisGlyphs(trim); Check(ellipsis.Length > 0 && ellipsis.Length == S.ShapedTextGetEllipsisGlyphCount(trim) && ellipsis.All(g => g.Start == -1 && g.End == -1 && g.SpanIndex == -1 && g.FontRID == latin.GetRID() && (g.Flags & TextGraphemeFlags.Virtual) != 0), "Independent actual ellipsis payload and sentinels.");
            Check(S.ShapedTextGetGlyphs(trim).SequenceEqual(before), "Trimming preserves complete raw glyph snapshots.");
            S.ShapedTextOverrunTrimToWidth(trim, 0); Check(S.ShapedTextGetEllipsisGlyphCount(trim) == 0, "Reset removes ellipsis payload.");
            var fit = Buffer(arabic, "سلام عالم"); owned.Add(fit); var natural = S.ShapedTextGetWidth(fit);
            S.ShapedTextFitToWidth(fit, natural + 100, TextJustificationFlags.Kashida | TextJustificationFlags.WordBound); var fitted = S.ShapedTextGetGlyphs(fit);
            Check(fitted.Any(g => g.Repeat > 1 && g.FontRID == arabic.GetRID()) && fitted.Any(g => (g.Flags & TextGraphemeFlags.Elongation) != 0), "Full snapshots retain repeated justification glyphs."); CheckClusters(fitted);
            var vertical = S.CreateShapedText(orientation: TextOrientation.Vertical); owned.Add(vertical); S.ShapedTextAddString(vertical, "Ab", [latin], 24);
            Check(S.ShapedTextGetGlyphs(vertical).All(g => g.FontSize == 24 && g.Advance > 0 && g.Offset.IsFinite()), "Vertical glyph advance, size and offsets.");
            var empty = S.CreateShapedText(); owned.Add(empty); Check(S.ShapedGetRunCount(empty) == 0 && S.ShapedTextGetGlyphs(empty).Length == 0 && S.ShapedTextGetEllipsisGlyphCount(empty) == 0, "Empty query family.");
            for (var i = 0; i < 100; i++) Query(); var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) Query(); allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Check(allocated == 0, "Prepared scalar run queries allocate no owner-thread bytes: " + allocated);
            void Query() { S.ShapedGetRunCount(id); S.ShapedGetRunRange(id, 0); S.ShapedGetRunFontRID(id, 0); S.ShapedGetRunFontSize(id, 0); S.ShapedGetRunLanguage(id, 0); S.ShapedGetRunDirection(id, 0); S.ShapedGetRunObject(id, 0); S.ShapedGetRunGlyphRange(id, 0); }
        }
        finally { foreach (var rid in owned) if (S.Has(rid)) S.FreeRID(rid); }
    }
    private static void AutomaticSources()
    {
        if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) return;
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = true }; var id = Buffer(font, "سلام");
        try
        {
            var glyph = S.ShapedTextGetGlyphs(id).First(g => g.FontRID != default); var rid = glyph.FontRID;
            Check(rid != font.GetRID() && S.Has(rid) && S.FontGetSupportedGlyphs(rid).Length > 0 && !font.GetRIDs().Contains(rid), "Automatic fallback is exposed through actual glyphs rather than authored source list.");
            var projection = S.FontGetResource(rid); var oldWidth = S.ShapedTextGetWidth(id);
            S.FontSetSpacing(rid, TextSpacingType.Glyph, 5); Check(S.ShapedTextGetWidth(id) > oldWidth && S.ShapedTextGetGlyphs(id).Any(g => g.FontRID == rid), "Automatic fallback runtime edits invalidate its parent and preserve source identity.");
            S.FontClearSystemFallbackCache(); Check(!S.Has(rid), "Global fallback clear retires dynamic source identity."); Reject<InvalidOperationException>(() => projection.GetRID());
            Check(S.ShapedTextGetWidth(id) > 0 && S.ShapedTextGetGlyphs(id).Any(g => g.FontRID != default && g.FontRID != rid), "Retired automatic source rebuilds through the same consumer.");
        }
        finally { S.FreeRID(id); }
    }
    private static void AutomaticLinkedSources()
    {
        if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) return;
        var root = S.CreateFont(); var link = S.CreateFontLinkedVariation(root); RID first = default, second = default;
        try
        {
            S.FontSetData(root, FontTestFixtures.OpenSans); S.FontSetAllowSystemFallback(root, true);
            S.FontSetEmbolden(root, .5f); S.FontSetFixedSize(root, 24); S.FontSetFixedSizeScaleMode(root, FixedSizeScaleMode.Enabled);
            S.FontSetSpacing(root, TextSpacingType.Glyph, 2); S.FontSetSpacing(link, TextSpacingType.Glyph, 6);
            S.FontSetBaselineOffset(root, .25f); S.FontSetBaselineOffset(link, -.25f);
            first = Buffer(S.FontGetResource(root), "سلام"); second = Buffer(S.FontGetResource(link), "سلام");
            var a = S.ShapedTextGetGlyphs(first).First(g => g.Repeat > 0); var b = S.ShapedTextGetGlyphs(second).First(g => g.Repeat > 0);
            Check(a.FontRID != b.FontRID && S.FontGetSpacing(a.FontRID, TextSpacingType.Glyph) == 2 && S.FontGetSpacing(b.FontRID, TextSpacingType.Glyph) == 6, "Shared native sources realize independent effective fallback spacing caches for root/linked consumers.");
            Check(S.FontGetBaselineOffset(a.FontRID) == .25f && S.FontGetBaselineOffset(b.FontRID) == -.25f && a.Offset.Y > 0 && b.Offset.Y < 0, "Fallback source metadata and native glyph offsets inherit the effective independent baseline.");
            Check(S.FontGetEmbolden(a.FontRID) == .5f && S.FontGetFixedSize(a.FontRID) == 24 && S.FontGetFixedSizeScaleMode(a.FontRID) == FixedSizeScaleMode.Enabled, "Automatic sources inherit actual synthetic/fixed-size instance preferences.");
            var old = a.FontRID; S.FontSetSpacing(root, TextSpacingType.Glyph, 4);
            Check(!S.Has(old) && S.ShapedTextGetGlyphs(first).Any(g => g.FontRID != default && S.FontGetSpacing(g.FontRID, TextSpacingType.Glyph) == 4), "Parent spacing invalidates derived caches and rebuilds the same consumer.");
            Check(S.ShapedTextGetGlyphs(second).Any(g => g.FontRID != default && S.FontGetSpacing(g.FontRID, TextSpacingType.Glyph) == 6), "Another linked consumer preserves its independent fallback policy after invalidation.");
        }
        finally { if (S.Has(first)) S.FreeRID(first); if (S.Has(second)) S.FreeRID(second); S.FreeRID(link); S.FreeRID(root); }
    }
    private static void ParagraphLines()
    {
        using var latin = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        using var arabic = new FontFile { Data = FontTestFixtures.Arabic, AllowSystemFallback = false };
        using var paragraph = new TextParagraph { Width = 400 };
        paragraph.AddString("Latin ", latin, 24, "en"); paragraph.AddString("سلام", arabic, 30, "ar");
        var line = paragraph.GetLineRID(0); var before = S.ShapedTextGetGlyphs(line);
        Check(before.Any(g => g.SpanIndex == 1 && g.FontRID == arabic.GetRID()) && S.ShapedGetRunCount(line) >= 2, "Borrowed paragraph line retains original span/source runs.");
        S.ShapedTextOverrunTrimToWidth(line, 70, TextOverrunFlags.Trim | TextOverrunFlags.AddEllipsis | TextOverrunFlags.EnforceEllipsis);
        Check(S.ShapedTextGetGlyphs(line).SequenceEqual(before) && S.ShapedTextGetEllipsisGlyphCount(line) > 0 && S.ShapedTextGetEllipsisGlyphs(line).All(g => g.Start == -1 && g.FontSize == 30), "Borrowed line formatting separates raw glyphs from actual display ellipsis.");
        paragraph.Width = 100; paragraph.GetSize(); Check(!S.Has(line), "Paragraph reflow retires borrowed metadata identity.");
        Reject<InvalidOperationException>(() => S.ShapedTextGetGlyphs(line));
    }
    private static void ConcurrentSources()
    {
        if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) return;
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = true };
        var id = Buffer(font, "سلام"); var rid = S.ShapedTextGetGlyphs(id).First(g => g.FontRID != default).FontRID; S.FreeRID(id);
        using var start = new ManualResetEventSlim();
        var writer = Task.Run(() => { start.Wait(); for (var i = 0; i < 48; i++) S.FontSetSpacing(rid, TextSpacingType.Glyph, i % 4); });
        var reader = Task.Run(() => { start.Wait(); for (var i = 0; i < 96; i++) Check(font.GetStringSize("سلام", fontSize: 24).X > 0, "Concurrent automatic source edit preserves a usable native read."); });
        start.Set(); Check(Task.WaitAll([writer, reader], TimeSpan.FromSeconds(15)), "Concurrent raw fallback edits/parent reads finish without lock inversion.");
        Check(S.Has(rid) && font.GetStringSize("سلام", fontSize: 24).X > 0, "Concurrent source identity remains current after completion.");
    }
    private static void CallbackFailure()
    {
        using var source = new FontFile { Data = FontTestFixtures.OpenSans, AllowSystemFallback = false };
        source.Changed += _ => throw new ApplicationException("source observer");
        using var variation = new FontVariation { BaseFont = source }; var old = variation.GetRID();
        var theme = ThemeDB.FallbackFont;
        try
        {
            ThemeDB.FallbackFont = source; using var system = new SystemFont(); var systemOld = system.GetRID();
            Reject<ApplicationException>(() => source.Data = FontTestFixtures.Variable);
            Check(variation.GetRID() != old && !S.Has(old) && system.GetRID() != systemOld && !S.Has(systemOld), "Source generation repairs native projections even when an earlier Changed observer throws.");
            Check(variation.GetStringSize("AA", fontSize: 24).X > 0 && system.GetStringSize("AA", fontSize: 24).X > 0, "Rebuilt source projections remain executable after observer failure.");
            var rid = variation.GetRID(); var view = S.FontGetResource(rid); var called = false;
            view.Changed += _ => { called = true; throw new ApplicationException("retirement observer"); };
            variation.PrimaryData!.SourceOwner!.TryGetTarget(out var retiredFile);
            Exception? failure = null; try { variation.VariationEmbolden = .5f; } catch (Exception error) { failure = error; }
            Check(failure is ApplicationException, $"Retirement failure delivered: called={called}, disposed={retiredFile!.IsDisposed}, gate={Monitor.IsEntered(variation.FontGate)}, native={FontData.NativeCleanupDepth}, viewDisposed={view.IsDisposed}, error={failure}");
            Check(called && !S.Has(rid) && variation.GetRID() != rid, "Committed retirement and new source survive a failing borrowed-view disposal observer.");
        }
        finally { ThemeDB.FallbackFont = theme; }
    }
    private sealed class CustomSpacingFile : FontFile
    { public override int GetSpacing(TextSpacingType spacing) => base.GetSpacing(spacing) + (spacing == TextSpacingType.Glyph ? 3 : 0); }
    private sealed class CustomSpacingVariation : FontVariation
    { public override int GetSpacing(TextSpacingType spacing) => base.GetSpacing(spacing) + (spacing == TextSpacingType.Glyph ? 3 : 0); }
    private static RID Buffer(Font font, string text)
    { var id = S.CreateShapedText(); try { S.ShapedTextAddString(id, text, [font], 24); return id; } catch { S.FreeRID(id); throw; } }
    private static void CheckClusters(TextGlyph[] glyphs)
    {
        for (var i = 0; i < glyphs.Length;)
        {
            var leader = glyphs[i]; Check(leader.Count > 0 && i + leader.Count <= glyphs.Length && leader.Repeat >= 0, "Cluster leader count/repeat bounds.");
            for (var j = 1; j < leader.Count; j++) Check(glyphs[i + j].Count == 0 && glyphs[i + j].Start == leader.Start && glyphs[i + j].End == leader.End, "Cluster continuation count and scalar range.");
            i += leader.Count;
        }
    }
    private static void Near(float actual, float expected, float tolerance, string message) => Check(Math.Abs(actual - expected) <= tolerance, $"{message}: {actual} vs {expected}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer glyphs: " + message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
