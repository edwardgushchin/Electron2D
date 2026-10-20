using Electron2D;

internal static class TextServerFontTests
{
    internal static void Run()
    {
        Ownership(); Dynamic(); Bitmap(); VariationsAndRaster(); ConcurrentRetirement();
        Console.WriteLine("TextServer font ownership, shared linked source/config/cache, typed shaping, native contours, source mutation, cache authoring, scaling and retirement passed.");
    }
    private static void Ownership()
    {
        var s = TextServerManager.GetPrimaryInterface(); var root = s.CreateFont(); var a = s.CreateFontLinkedVariation(root); var b = s.CreateFontLinkedVariation(a);
        try
        {
            var view = s.FontGetResource(root); var linked = s.FontGetResource(a);
            Check(ReferenceEquals(view, s.FontGetResource(root)) && view.GetRID() == root && linked.GetRID() == a, "Stable live typed projection identity.");
            Check(s.Has(root) && s.Has(a) && s.Has(b) && !s.Has(default), "Shared owner lookup.");
            Check(s.FontGetFaceCount(root) == 0 && s.FontGetMSDFPixelRange(root) == 14 && s.FontGetMSDFSize(root) == 48, "Empty native font defaults.");
            s.FontSetWeight(a, 650); s.FontSetName(b, "shared");
            Check(s.FontGetWeight(root) == 650 && s.FontGetWeight(b) == 650 && s.FontGetName(a) == "shared", "Links alias original root configuration.");
            s.FontSetSpacing(root, TextSpacingType.Glyph, 2); s.FontSetSpacing(a, TextSpacingType.Glyph, -3); s.FontSetBaselineOffset(root, .5f); s.FontSetBaselineOffset(a, .25f);
            Check(s.FontGetSpacing(b, TextSpacingType.Glyph) == 0 && s.FontGetBaselineOffset(b) == 0 && s.FontGetSpacing(root, TextSpacingType.Glyph) == 2 && s.FontGetSpacing(a, TextSpacingType.Glyph) == -3, "Every link starts with independent zero spacing/baseline.");
            s.FontSetData(root, FontTestFixtures.OpenSans); using var duplicate = (Font)view.Duplicate(); var width = duplicate.GetStringSize("copy", fontSize: 28).X;
            var oldRoot = root; s.FreeRID(root); root = default;
            Check(!s.Has(oldRoot) && view.IsDisposed && s.Has(a) && s.Has(b), "Base source retirement preserves independent linked entries.");
            Check(s.FontGetSpacing(a, TextSpacingType.Glyph) == -3 && s.FontGetBaselineOffset(a) == .25f, "Independent link state survives source retirement.");
            Reject<InvalidOperationException>(() => s.FontGetName(a)); Reject<InvalidOperationException>(() => s.CreateFontLinkedVariation(b));
            Reject<ObjectDisposedException>(() => linked.GetStringSize("retired"));
            Check(duplicate.GetStringSize("copy", fontSize: 28).X == width, "Duplicate source snapshot survives service retirement.");
        }
        finally { if (root != default && s.Has(root)) s.FreeRID(root); if (s.Has(a)) s.FreeRID(a); if (s.Has(b)) s.FreeRID(b); }
        var disposed = s.CreateFont(); s.FontGetResource(disposed).Dispose(); Check(!s.Has(disposed), "Projection disposal frees its owned identity.");
        Reject<InvalidOperationException>(() => s.FreeRID(disposed)); Reject<InvalidOperationException>(() => s.FontGetResource(default));
        var failingRoot = s.CreateFont(); var failingLink = s.CreateFontLinkedVariation(failingRoot); var childView = s.FontGetResource(failingLink);
        Action<Resource> fail = _ => { Check(!s.Has(failingRoot), "Committed root disposal closes lookup before link callbacks."); throw new CallbackFailure(); };
        childView.Changed += fail;
        try { Reject<CallbackFailure>(() => s.FreeRID(failingRoot)); Check(!s.Has(failingRoot) && s.Has(failingLink), "Callback failure does not leak root ownership."); }
        finally { childView.Changed -= fail; if (s.Has(failingLink)) s.FreeRID(failingLink); }
    }
    private static void Dynamic()
    {
        var s = TextServerManager.GetPrimaryInterface(); var root = s.CreateFont(); var link = s.CreateFontLinkedVariation(root); var buffer = s.CreateShapedText();
        try
        {
            s.FontSetAllowSystemFallback(root, false); var data = (byte[])FontTestFixtures.OpenSans.Clone(); s.FontSetData(root, data); data[0] ^= 255;
            var view = s.FontGetResource(root); var linked = s.FontGetResource(link); var a = s.FontGetGlyphIndex(root, 28, 'A', 0);
            Check(a > 0 && s.FontHasChar(link, 'A') && s.FontGetFaceCount(root) == 1 && s.FontGetSupportedChars(link).Contains('A') && s.FontGetSupportedGlyphs(root).Contains(a), "Copied native source and real glyph/character enumeration.");
            Check(s.FontGetCharFromGlyphIndex(root, 28, a) == 'A' && s.FontGetName(root).Length > 0 && s.FontGetStyleName(root).Length > 0, "Native reverse mapping and names.");
            Check(s.FontGetOTNameStrings(root).Count > 0 && s.FontSupportedFeatureList(root).Count > 0, "Native SFNT metadata is exposed through the shared owner.");
            s.FontSetHinting(root, FontHinting.None); s.FontSetSubpixelPositioning(root, FontSubpixelPositioning.Quarter); s.FontSetKeepRoundingRemainders(root, true);
            s.FontSetGenerateMIPMaps(root, true); s.FontSetDisableEmbeddedBitmaps(root, true); s.FontSetForceAutohinter(root, true); s.FontSetAntialiasing(root, FontAntialiasing.Gray); s.FontSetOversampling(root, 1.5f); s.FontSetModulateColorGlyphs(root, true);
            Check(s.FontGetHinting(link) == FontHinting.None && s.FontGetSubpixelPositioning(link) == FontSubpixelPositioning.Quarter && s.FontGetKeepRoundingRemainders(link) && s.FontGetGenerateMIPMaps(link) && s.FontGetDisableEmbeddedBitmaps(link) && s.FontIsForceAutohinter(link) && s.FontGetOversampling(link) == 1.5f && s.FontIsModulateColorGlyphs(link), "Raster configuration aliases across links.");
            s.FontSetOversampling(root, 0); s.FontSetGenerateMIPMaps(root, false); s.FontSetForceAutohinter(root, false);
            var features = new Dictionary<string, int> { ["kern"] = 0 }; s.FontSetOpenTypeFeatureOverrides(link, features); features["kern"] = 1;
            Check(s.FontGetOpenTypeFeatureOverrides(root)["kern"] == 0, "Feature input is copied."); var returned = s.FontGetOpenTypeFeatureOverrides(root); returned.Clear(); Check(s.FontGetOpenTypeFeatureOverrides(root).Count == 1, "Feature output is independent.");
            s.FontSetLanguageSupportOverride(root, "en", false); s.FontSetScriptSupportOverride(link, "Latn", false);
            Check(!s.FontIsLanguageSupported(link, "en") && !s.FontIsScriptSupported(root, "Latn") && s.FontGetLanguageSupportOverrides(root).Contains("en") && s.FontGetScriptSupportOverrides(link).Contains("Latn"), "Shared language and script policy.");
            s.FontRemoveLanguageSupportOverride(root, "en"); s.FontRemoveScriptSupportOverride(root, "Latn");
            s.ShapedTextAddString(buffer, "AA", [view], 28); var initial = s.ShapedTextGetWidth(buffer);
            s.FontSetSpacing(root, TextSpacingType.Glyph, 3); Check(s.ShapedTextGetWidth(buffer) == initial + 3, "Existing typed buffer consumes service spacing changes.");
            s.ShapedSetSpanUpdateFont(buffer, 0, [linked], 28); Check(s.ShapedTextGetWidth(buffer) == initial, "Linked spacing remains independent on the real consumer.");
            var contours = s.FontGetGlyphContours(link, 28, a);
            Check(contours.Points.Length > 0 && contours.Contours.Length > 0 && contours.Contours[^1] == contours.Points.Length - 1 && contours.Points.All(point => point.Position.IsFinite()), "Copied native Bézier outline topology and finite points.");
            contours.Points[0] = default; Check(s.FontGetGlyphContours(root, 28, a).Points[0] != default, "Outline snapshots are independent.");
            s.FontRenderRange(root, new(28, 0), 'A', 'C'); s.FontRenderGlyph(link, new(28, 1), a);
            Check(s.FontGetGlyphList(root, new(28, 0)).Contains(a) && s.FontGetSizeCacheList(root).Contains(new(28, 1)) && s.FontGetTextureCount(root, new(28, 0)) > 0, "Actual generated glyph/outline atlas ownership.");
            var textureIndex = s.FontGetGlyphTextureIndex(root, new(28, 0), a); using var image = s.FontGetTextureImage(link, new(28, 0), textureIndex)!;
            Check(image.Width > 0 && s.FontGetGlyphTextureRID(root, new(28, 0), a) != default && s.FontGetGlyphTextureSize(root, new(28, 0), a) == new Vector2(image.Width, image.Height), "Real borrowed renderer atlas and independent CPU image.");
            Check(s.FontGetGlyphSize(root, new(28, 0), a).X > 0 && s.FontGetGlyphUVRect(root, new(28, 0), a).HasArea() && s.FontGetSizeCacheInfo(root).Any(info => info.TextureBytes > 0 && info.Glyphs > 0), "Live geometry, UV and cache telemetry.");
            var before = s.FontGetGlyphAdvance(root, 28, a); s.FontSetFixedSize(root, 28); s.FontSetFixedSizeScaleMode(root, FixedSizeScaleMode.Enabled);
            Near(s.FontGetGlyphAdvance(link, 56, a).X, before.X * 2, .05f, "Native fixed-size scaled advance");
            Near(s.FontGetScale(root, 56), 2, .0001f, "Native fixed-size scale query");
            var baseGeometry = s.FontGetGlyphSize(root, new(28, 0), a); Check(s.FontGetGlyphSize(root, new(56, 0), a) == baseGeometry * 2, "Fixed source pixels scale without a new nominal cache.");
            s.FontSetFixedSizeScaleMode(root, FixedSizeScaleMode.Disable); Near(s.FontGetGlyphAdvance(root, 56, a).X, before.X, .05f, "Disabled fixed-size scaling retains source advance");
            s.FontSetFixedSize(root, 0); s.FontSetBaselineOffset(link, .25f); s.FontSetAscent(root, 28, 30); s.FontSetDescent(root, 28, 10);
            Near(s.FontGetAscent(link, 28), 30, 0, "Authored metrics share with links");
            var saved = s.FontGetGlyphIndex(root, 28, 'A', 0); Reject<InvalidDataException>(() => s.FontSetData(root, [1, 2, 3])); Check(s.FontGetGlyphIndex(link, 28, 'A', 0) == saved, "Invalid source preserves owned identity and native data.");
            Reject<ArgumentOutOfRangeException>(() => s.FontSetSpacing(root, TextSpacingType.Max, 1)); Reject<ArgumentOutOfRangeException>(() => s.FontSetBaselineOffset(root, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => s.FontGetGlyphIndex(root, 28, 0xD800, 0)); Reject<ArgumentOutOfRangeException>(() => s.FontRenderRange(root, new(28, 0), 10, 2));
            Reject<ArgumentOutOfRangeException>(() => s.FontGetGlyphContours(root, 0, a));
            s.FontClearSystemFallbackCache(); Check(s.ShapedTextGetWidth(buffer) > 0, "Global cache invalidation preserves executable typed buffers.");
            s.FontClearSizeCache(link); Check(s.FontGetSizeCacheList(root).Length == 0, "Linked clear removes actual shared caches.");
            s.FontRenderGlyph(root, new(28, 0), a); s.FontRemoveGlyph(link, new(28, 0), a); Check(!s.FontGetGlyphList(root, new(28, 0)).Contains(a), "Individual generated glyph removal.");
            s.FontRemoveSizeCache(link, new(28, 0)); Check(s.FontGetSizeCacheList(root).Length == 0, "Individual shared size removal.");
        }
        finally { s.FreeRID(buffer); if (s.Has(link)) s.FreeRID(link); if (s.Has(root)) s.FreeRID(root); }
    }
    private static void Bitmap()
    {
        var s = TextServerManager.GetPrimaryInterface(); var root = s.CreateFont(); var link = s.CreateFontLinkedVariation(root);
        try
        {
            s.FontSetAllowSystemFallback(root, false); using var page = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); page.Fill(Colors.Cyan);
            var key = new Vector2i(16, 0); s.FontSetTextureImage(root, key, 0, page);
            s.FontSetGlyphOffset(root, key, 'A', new(1, -10)); s.FontSetGlyphSize(root, key, 'A', new(5, 9)); s.FontSetGlyphUVRect(root, key, 'A', new(2, 3, 5, 9)); s.FontSetGlyphTextureIndex(root, key, 'A', 0); s.FontSetGlyphAdvance(root, 16, 'A', new(8, 16));
            s.FontSetAscent(root, 16, 12); s.FontSetDescent(root, 16, 4); s.FontSetUnderlinePosition(root, 16, 2); s.FontSetUnderlineThickness(root, 16, 1); s.FontSetScale(root, 16, 1);
            s.FontSetKerning(root, 16, new('A', 'A'), new(-1, 0));
            Check(s.FontGetGlyphOffset(link, key, 'A') == new Vector2(1, -10) && s.FontGetGlyphSize(link, key, 'A') == new Vector2(5, 9) && s.FontGetKerning(link, 16, new('A', 'A')).X == -1 && s.FontGetKerningList(root, 16).Length == 1, "Executable authored glyph and kerning records.");
            Check(s.FontGetResource(root).GetStringSize("AA", fontSize: 16).X == 15, "Service bitmap cache shapes real text.");
            s.FontSetFixedSize(root, 16); s.FontSetFixedSizeScaleMode(root, FixedSizeScaleMode.Enabled);
            Check(s.FontGetGlyphSize(link, new(8, 0), 'A') == new Vector2(2.5f, 4.5f) && s.FontGetGlyphAdvance(root, 8, 'A') == new Vector2(4, 8), "Authored fixed-size geometry and advance scale together.");
            Check(s.FontGetGlyphContours(root, 16, 'A').Points.Length == 0, "Bitmap sources have no invented outlines.");
            s.FontRemoveKerning(root, 16, new('A', 'A')); Check(s.FontGetKerningList(root, 16).Length == 0, "Remove authored kerning.");
            s.FontSetKerning(root, 16, new('A', 'A'), new(-1, 0)); s.FontClearKerningMap(link, 16); Check(s.FontGetKerningList(root, 16).Length == 0, "Clear shared kerning map.");
            s.FontClearGlyphs(link, key); Check(s.FontGetGlyphList(root, key).Length == 0, "Clear authored glyphs.");
            s.FontRemoveTexture(link, key, 0); Check(s.FontGetTextureCount(root, key) == 0, "Remove actual authored atlas page.");
            s.FontSetTextureImage(root, key, 0, page); s.FontClearTextures(root, key); Check(s.FontGetTextureCount(link, key) == 0, "Clear actual authored atlas pages.");
        }
        finally { s.FreeRID(link); s.FreeRID(root); }
    }
    private sealed class CallbackFailure : Exception { }
    private static void ConcurrentRetirement()
    {
        var s = TextServerManager.GetPrimaryInterface();
        for (var i = 0; i < 32; i++)
        {
            var root = s.CreateFont(); RID link = default; using var start = new Barrier(2);
            var create = Task.Run(() => { start.SignalAndWait(); try { link = s.CreateFontLinkedVariation(root); } catch (InvalidOperationException) { } });
            start.SignalAndWait(); s.FreeRID(root); create.GetAwaiter().GetResult();
            Check(!s.Has(root), "Concurrent root retirement closes the root identity.");
            if (link != default)
            {
                Check(s.Has(link) && s.FontGetSpacing(link, TextSpacingType.Glyph) == 0, "A concurrently created link retains only its independent entry state.");
                Reject<InvalidOperationException>(() => s.FontGetName(link)); s.FreeRID(link);
            }
        }
    }
    private static void VariationsAndRaster()
    {
        var s = TextServerManager.GetPrimaryInterface(); var root = s.CreateFont(); var link = s.CreateFontLinkedVariation(root);
        try
        {
            s.FontSetAllowSystemFallback(root, false); s.FontSetData(root, FontTestFixtures.Variable); const uint weight = 0x77676874;
            Check(s.FontSupportedVariationList(root)[weight] == new FontVariationAxis(100, 900, 100), "Native variable axis metadata.");
            var width = s.FontGetResource(root).GetStringSize("A", fontSize: 20).X; var coordinates = new Dictionary<uint, float> { [weight] = 900 };
            s.FontSetVariationCoordinates(link, coordinates); coordinates.Clear();
            Check(s.FontGetVariationCoordinates(root)[weight] == 900 && s.FontGetResource(link).GetStringSize("A", fontSize: 20).X > width, "Copied shared variation coordinates execute actual native shaping.");
            s.FontSetTransform(root, new(new(1.25f, 0), new(0, 1), Vector2.Zero)); Check(s.FontGetTransform(link).X.X == 1.25f, "Shared native outline transform.");
            s.FontSetData(root, FontTestFixtures.Collection); s.FontSetFaceIndex(link, 1);
            Check(s.FontGetFaceCount(root) == 2 && s.FontGetFaceIndex(root) == 1 && s.FontGetResource(link).GetFontStyleName() == "Heavy", "Shared collection face selection.");
            s.FontSetFaceIndex(root, 0); s.FontSetTransform(root, Transform.Identity); s.FontSetVariationCoordinates(root, []);
            s.FontSetData(root, FontTestFixtures.Palette);
            Check(s.FontGetPaletteCount(root) == 2 && s.FontGetPaletteName(root, 1) == "Alternate" && s.FontGetPaletteColors(root, 0)[0] == Colors.Red, "Native palette metadata.");
            s.FontSetUsedPalette(link, 1); Check(s.FontGetUsedPalette(root) == 1 && s.FontGetPaletteColors(root, 1)[0] == Colors.Lime, "Shared selected native palette.");
            var colors = new[] { Colors.Blue }; s.FontSetPaletteCustomColors(root, colors); colors[0] = Colors.Red;
            Check(s.FontGetPaletteCustomColors(link)[0] == Colors.Blue, "Custom palette input copied.");
            var returned = s.FontGetPaletteCustomColors(root); returned[0] = Colors.Red; Check(s.FontGetPaletteCustomColors(root)[0] == Colors.Blue, "Custom palette output copied.");
            var glyph = s.FontGetGlyphIndex(root, 32, 'A', 0); s.FontRenderGlyph(root, new(32, 0), glyph);
            var uv = s.FontGetGlyphUVRect(root, new(32, 0), glyph); using (var image = s.FontGetTextureImage(root, new(32, 0), s.FontGetGlyphTextureIndex(root, new(32, 0), glyph))!)
                Check(image.GetPixel((int)uv.Position.X + 5, (int)uv.Position.Y + 10).B > .9f, "Actual native palette override pixels.");
            s.FontSetData(root, FontTestFixtures.OpenSans); s.FontSetSubpixelPositioning(root, FontSubpixelPositioning.Quarter); glyph = s.FontGetGlyphIndex(root, 24, 'A', 0);
            var shifted = glyph | (1 << 27); s.FontRenderGlyph(root, new(24, 0), shifted);
            Check(s.FontGetGlyphList(link, new(24, 0)).Contains(shifted) && s.FontGetGlyphSize(root, new(24, 0), shifted).X > 0 && s.FontGetGlyphTextureIndex(root, new(24, 0), shifted) >= 0, "Packed fractional-phase glyphs have real shared atlas entries.");
            Check(s.FontGetGlyphAdvance(root, 24, shifted) == s.FontGetGlyphAdvance(root, 24, glyph), "Packed phase does not change nominal glyph advance.");
            s.FontSetOversampling(root, 2); s.FontRenderGlyph(root, new(24, 0), glyph);
            var high = s.FontGetSizeCacheInfo(root); Check(high.Any(info => info.RasterScales.Contains(2)), "Actual oversampling is represented by size-cache telemetry.");
            s.FontSetOversampling(root, 0); s.FontSetMultichannelSignedDistanceField(root, true); s.FontSetMSDFSize(root, 48); s.FontSetMSDFPixelRange(root, 8);
            Check(s.FontIsMultichannelSignedDistanceField(link) && s.FontGetMSDFSize(link) == 48 && s.FontGetMSDFPixelRange(link) == 8, "Shared MSDF configuration.");
            s.FontRenderGlyph(root, new(24, 0), glyph); var small = s.FontGetGlyphSize(root, new(24, 0), glyph); var large = s.FontGetGlyphSize(root, new(48, 0), glyph);
            Check(small.X > 0 && small.Y > 0 && large == small * 2 && s.FontGetSizeCacheList(root).Contains(new Vector2i(48, 0)) &&
                s.FontGetGlyphTextureRID(root, new(24, 0), glyph) == s.FontGetGlyphTextureRID(root, new(48, 0), glyph), "MSDF geometry shares source pixels while following requested logical size: " + small + "/" + large);
            var outline = s.FontGetGlyphContours(root, 48, s.FontGetGlyphIndex(root, 48, 'O', 0)); Check(outline.Points.Any(point => point.Tag == TextServer.ContourPointTag.OffConic), "Quadratic native control points retain their tags.");
            s.FontSetMultichannelSignedDistanceField(root, false);
            s.FontSetStyle(root, FontStyle.Bold | FontStyle.Italic); s.FontSetStyleName(root, "authored"); s.FontSetStretch(root, 125);
            Check(s.FontGetStyle(link) == (FontStyle.Bold | FontStyle.Italic) && s.FontGetStyleName(link) == "authored" && s.FontGetStretch(link) == 125, "Shared descriptive style metadata.");
            using var canvas = new Entity(); Reject<InvalidOperationException>(() => s.FontDrawGlyph(root, canvas, 24, Vector2.Zero, glyph));
            s.FontSetAntialiasing(root, (FontAntialiasing)99); Check((int)s.FontGetAntialiasing(link) == 99, "Unknown stored antialiasing values preserve the documented grayscale fallback policy.");
            Reject<ArgumentException>(() => s.FontSetPaletteCustomColors(root, [new(float.NaN, 0, 0)]));
        }
        finally { s.FreeRID(link); s.FreeRID(root); }
        var veto = new VetoFont();
        try
        {
            for (var i = 0; i < 100; i++) { using var temporary = new FontFile(); }
            veto.Dispose(); Check(!veto.IsDisposed, "Font disposal veto remains live after a global cache sweep.");
            var changes = 0; veto.Changed += _ => changes++; s.FontClearSystemFallbackCache();
            Check(changes == 1, "Global font cache registry retains a vetoed resource.");
        }
        finally { veto.Veto = false; veto.Dispose(); }
    }
    private sealed class VetoFont : FontFile
    {
        internal bool Veto = true;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationPreDelete && Veto) { TextServerManager.GetPrimaryInterface().FontClearSystemFallbackCache(); CancelDisposal(); }
        }
    }
    private static void Near(float a, float b, float tolerance, string message) => Check(Math.Abs(a - b) <= tolerance, message + ": " + a + "/" + b);
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer fonts: " + message); }
}
