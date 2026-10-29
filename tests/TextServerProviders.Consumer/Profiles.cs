using Electron2D;

internal static class ConcreteProfiles
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run()
    {
        using var advanced = new TextServerAdvanced(); using var fallback = new TextServerFallback();
        using var font = new FontFile(); font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets/Regular.ttf"));
        Check(advanced.HasFeature(TextServer.Feature.Shaping) && advanced.HasFeature(TextServer.Feature.BIDILayout), "Advanced exposes its contextual and bidi execution.");
        Check(!fallback.HasFeature(TextServer.Feature.Shaping) && !fallback.HasFeature(TextServer.Feature.BIDILayout) && fallback.HasFeature(TextServer.Feature.FontMSDF), "Fallback supplies simple layout with the shared actual raster features.");
        var rich = advanced.CreateShapedText(); var simple = fallback.CreateShapedText(); RID slice = default;
        try
        {
            advanced.ShapedTextAddString(rich, "ffi", [font], 32); fallback.ShapedTextAddString(simple, "ffi", [font], 32);
            Check(advanced.ShapedTextGetGlyphs(rich).Length < 3, "Advanced executes the fixture ligature.");
            var glyphs = fallback.ShapedTextGetGlyphs(simple);
            Check(glyphs.Length == 3 && glyphs.Select(g => g.Start).SequenceEqual([0, 1, 2]) && glyphs.All(g => g.End == g.Start + 1 && g.Count == 1), "Fallback keeps one logical scalar per nominal glyph.");
            Check(fallback.ShapedTextGetSize(simple).X > 0 && fallback.ShapedTextGetDirection(simple) == TextDirection.LTR, "Fallback prepares real metrics with LTR direction.");
            slice = fallback.ShapedTextSubstr(simple, 1, 2); fallback.FreeRID(simple); simple = default;
            Check(fallback.ShapedTextGetGlyphs(slice).Length == 2, "Fallback substring preserves its simple context after parent retirement.");
            Reject<NotSupportedException>(() => fallback.CreateShapedText(TextDirection.RTL));
            fallback.FreeRID(slice); slice = default;
            simple = fallback.CreateShapedText(); fallback.ShapedTextAddString(simple, "a\u0301", [font], 32);
            Check(fallback.ShapedTextGetGlyphs(simple).Length == 2 && fallback.ShapedTextGetCharacterBreaks(simple).SequenceEqual([1, 2]), "Fallback has scalar rather than contextual combining clusters.");
            var source = simple;
            Task.Run(() => Reject<InvalidOperationException>(() => fallback.ShapedTextGetSize(source))).GetAwaiter().GetResult();
        }
        finally
        {
            if (advanced.Has(rich)) advanced.FreeRID(rich);
            if (simple.IsValid() && fallback.Has(simple)) fallback.FreeRID(simple);
            if (slice.IsValid() && fallback.Has(slice)) fallback.FreeRID(slice);
        }
        Check(fallback.StringToUpper("ßi\0\ud800", "tr") == "ßI\0\ud800" && fallback.StringToLower("IİΣ", "tr") == "iiσ", "Simple case mapping has no expansion or locale tailoring and preserves malformed input.");
        Check(fallback.StringToTitle("myHTTP2_value-test") == "My Http 2 Value Test", "Simple title mapping separates compound identifiers.");
        Check(fallback.StringToTitle("one\u2010two\u2011three") == "One Two Three", "Simple title mapping recognizes both Unicode hyphen separators.");
        Check(fallback.StringGetCharacterBreaks("a\u0301😀").SequenceEqual([1, 2, 3]) && fallback.StringGetWordBreaks("abc_def 中文").SequenceEqual([0, 3, 4, 7, 8, 10]), "Simple utility boundaries preserve scalar coordinates without dictionary splitting.");
        Check(!fallback.IsLocaleRightToLeft("ar") && !fallback.IsLocaleUsingSupportData("th") && fallback.GetSupportData().Length == 0 && !fallback.LoadSupportData("unused"), "Simple profile has no ICU support-data lifecycle.");
        fallback.FontSetScriptSupportOverride(font.GetRID(), "*", false); fallback.FontSetScriptSupportOverride(font.GetRID(), "Arab", true);
        Check(fallback.FontIsScriptSupported(font.GetRID(), "Arab") && !fallback.FontIsScriptSupported(font.GetRID(), "Latn"), "Simple script policy uses exact and wildcard overrides.");
        fallback.FontRemoveScriptSupportOverride(font.GetRID(), "*"); fallback.FontRemoveScriptSupportOverride(font.GetRID(), "Arab");
        Check(fallback.FontIsScriptSupported(font.GetRID(), "Unkn") && fallback.FontSupportedFeatureList(font.GetRID()).Count == 0, "Simple script support defaults true without contextual feature enumeration.");
        FallbackBuffers(fallback, font);
        using var dummy = new TextServerDummy();
        Check(dummy.GetFeatures() == 0 && dummy.CreateFont() == default && dummy.CreateShapedText() == default && dummy.CreateFontLinkedVariation(font.GetRID()) == default, "Dummy allocates no resources.");
        dummy.FontSetWeight(font.GetRID(), 999); dummy.FreeRID(font.GetRID()); dummy.ShapedTextClear(default);
        Check(font.FontWeight != 999 && font.GetRID().IsValid() && !dummy.Has(font.GetRID()) && !dummy.ShapedTextShape(default) && !dummy.ShapedTextAddString(default, "ignored", [font], 24), "Dummy ignores live foreign identities without editing or freeing them.");
        Check(dummy.ShapedTextGetGlyphs(default).Length == 0 && dummy.ShapedTextGetSize(default) == default && dummy.ShapedTextGetTrimPos(default) == -1 && dummy.ShapedTextGetEllipsisPos(default) == -1 && dummy.ShapedTextGetObjectGlyph(default, new()) == -1, "Dummy supplies empty geometry and missing-position sentinels.");
        Check(dummy.FontGetTransform(default) == Transform.Identity && dummy.FontGetGlyphContours(default, 24, 1).Points.Length == 0 && dummy.FontGetTextureImage(default, new(24, 0), 0) is null, "Dummy supplies typed empty cache values.");
        Check(dummy.StringToUpper("abc") == "abc" && dummy.StringGetCharacterBreaks("a\u0301").SequenceEqual([1, 2]) && dummy.StringGetWordBreaks("words").Length == 0, "Dummy preserves its common utility defaults.");
        Reject<NotSupportedException>(() => dummy.FontGetResource(default));
        var original = TextServerManager.GetPrimaryInterface();
        try
        {
            TextServerManager.SetPrimaryInterface(fallback);
            VerifyConfigurationAllocations();
            using var line = new TextLine(); line.AddString("ffi", font, 32);
            Check(line.GetSize().X > 0 && fallback.ShapedTextGetGlyphCount(line.GetRID()) == 3, "Ordinary TextLine uses the selected simple profile.");
            TextServerManager.SetPrimaryInterface(dummy);
            Check(font.GetStringSize("visible before").X == 0, "Dummy disables shared font measurement.");
            Check(line.GetSize() == default, "Dummy retires retained line geometry.");
            Check(font.GetAscent(24) == 0, "Dummy disables shared font metrics.");
            TextServerManager.SetPrimaryInterface(advanced);
            Check(font.GetStringSize("ffi").X > 0, "Advanced resumes shared text after Dummy.");
        }
        finally { TextServerManager.SetPrimaryInterface(original); }
        var savedDriver = ProjectSettings.Get(ProjectSettings.TextDriver);
        try
        {
            Check(ProjectSettings.IsRestartRequired(ProjectSettings.TextDriver) && ProjectSettings.HasSetting(ProjectSettings.TextDriver), "Text driver is a permanent restart setting.");
            Reject<ArgumentException>(() => ProjectSettings.Set(ProjectSettings.TextDriver, "bad\0driver"));
            ProjectSettings.Set(ProjectSettings.TextDriver, fallback.GetName()); TextServerManager.AddInterface(fallback);
            using (var loop = new StartupProbe(TextServerManager.FindInterface(fallback.GetName())!))
            {
                Engine.Start(loop);
                try { Check(loop.InitializedWithDriver && !loop.NotifiedBeforeInitialization, "Driver selection precedes initialization without sending a runtime-change notification to an uninitialized loop."); }
                finally { Engine.Stop(); }
            }
            using (var tree = new SceneTree(new Node()))
            {
                Engine.Start(tree);
                try { Check(ReferenceEquals(TextServerManager.GetPrimaryInterface(), TextServerManager.FindInterface(fallback.GetName())), "Startup resolves the first exact registered driver name."); }
                finally { Engine.Stop(); }
            }
            ProjectSettings.Set(ProjectSettings.TextDriver, "missing-driver");
            using (var tree = new SceneTree(new Node())) Reject<NotSupportedException>(() => Engine.Start(tree));
            ProjectSettings.Set(ProjectSettings.TextDriver, ""); TextServerManager.SetPrimaryInterface(dummy);
            using (var tree = new SceneTree(new Node())) { Engine.Start(tree); Check(ReferenceEquals(TextServerManager.GetPrimaryInterface(), dummy), "Empty driver preserves explicit application selection after failed startup."); Engine.Stop(); }
        }
        finally
        {
            if (Engine.MainLoop is not null) Engine.Stop(); ProjectSettings.Set(ProjectSettings.TextDriver, savedDriver); TextServerManager.SetPrimaryInterface(original);
            if (TextServerManager.GetInterfaces().Contains(fallback)) TextServerManager.RemoveInterface(fallback);
        }
        TextServerManager.AddInterface(dummy); TextServerManager.RemoveInterface(dummy);
        Check(!TextServerManager.GetInterfaces().Contains(dummy), "Dummy has the ordinary registry removal lifetime.");
        ProviderSubstringOrigin(font);
        Console.WriteLine($"PASS {_checks} concrete text profile contracts.");
    }
    private static void ProviderSubstringOrigin(Font font)
    {
        var previous = TextServerManager.GetPrimaryInterface(); using var provider = new OriginProbe();
        TextServerManager.SetPrimaryInterface(provider);
        try
        {
            using var paragraph = new TextParagraph(); paragraph.AddString("first\nsecond", font, 22);
            var line = paragraph.GetLineRID(1); var slice = provider.ShapedTextSubstr(line, 6, 6);
            VerifyConfigurationAllocations();
            try
            {
                paragraph.Width = 5; _ = paragraph.GetLineCount();
                provider.ShapedTextSetOrientation(slice, TextOrientation.Vertical);
                Check(provider.ShapedTextGetSize(slice).Y > 0 && provider.ShapedTextGetRange(slice) == new Vector2i(6, 12) && provider.ShapedTextGetGlyphs(slice).All(g => g.Start >= 6 && g.End <= 12), "Provider substring reauthoring resets its private origin while preserving public scalar coordinates after parent reflow.");
            }
            finally { provider.FreeRID(slice); }
        }
        finally { TextServerManager.SetPrimaryInterface(previous); }
    }
    private sealed class OriginProbe : TextServerExtension
    { protected override string OnGetName() => "Substring origin probe"; }
    private static void VerifyConfigurationAllocations()
    {
        using var font = new FontFile();
        for (var i = 0; i < 64; i++) Toggle(font, i);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) Toggle(font, i);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(bytes == 0, "Warmed native configuration and unrelated provider-cache invalidation allocate no owner bytes: " + bytes);
        static void Toggle(FontFile font, int value)
        { font.Hinting = (value & 1) == 0 ? FontHinting.Light : FontHinting.None; font.SubpixelPositioning = (value & 1) == 0 ? FontSubpixelPositioning.Auto : FontSubpixelPositioning.Disabled; }
    }
    private sealed class StartupProbe(TextServer expected) : MainLoop
    {
        internal bool InitializedWithDriver, NotifiedBeforeInitialization;
        protected override void OnInitialize() => InitializedWithDriver = ReferenceEquals(TextServerManager.GetPrimaryInterface(), expected);
        protected override void OnNotification(int what)
        { base.OnNotification(what); if (what == NotificationTextServerChanged && !InitializedWithDriver) NotifiedBeforeInitialization = true; }
    }
    private static void FallbackBuffers(TextServerFallback server, FontFile fixture)
    {
        var fontRID = server.CreateFont(); var buffer = server.CreateShapedText(); RID copy = default;
        try
        {
            server.FontSetData(fontRID, fixture.Data); var font = server.FontGetResource(fontRID);
            var a = server.FontGetGlyphIndex(fontRID, 32, 'A', 0); var v = server.FontGetGlyphIndex(fontRID, 32, 'V', 0);
            server.FontSetGlyphAdvance(fontRID, 32, a, new(20, 30)); server.FontSetGlyphAdvance(fontRID, 32, v, new(22, 31));
            server.FontSetKerning(fontRID, 32, new(a, v), new(-5, 0));
            server.ShapedTextAddString(buffer, "AV", [font], 32);
            Check(server.ShapedTextGetWidth(buffer) == 37, "Simple glyph advances apply authored kerning exactly once.");
            server.ShapedTextSetBIDIOverride(buffer, [new(0, 2, TextDirection.Auto)]);
            Check(server.ShapedTextGetWidth(buffer) == 37, "Automatic structured contexts remain valid for simple consumers.");
            Reject<NotSupportedException>(() => server.ShapedTextSetBIDIOverride(buffer, [new(0, 2, TextDirection.RTL)]));
            server.ShapedTextSetBIDIOverride(buffer, []);
            server.ShapedTextSetSpacing(buffer, TextSpacingType.Glyph, 2);
            Check(server.ShapedTextGetWidth(buffer) == 39, "Simple glyph spacing affects real metrics.");
            server.ShapedTextSetSpacing(buffer, TextSpacingType.Glyph, 0);
            server.FontSetFixedSize(fontRID, 32); server.FontSetFixedSizeScaleMode(fontRID, FixedSizeScaleMode.Enabled);
            var scaled = server.CreateShapedText();
            try
            {
                server.ShapedTextAddString(scaled, "AV", [font], 48);
                Check(server.ShapedTextGetWidth(scaled) == 56, "Simple fixed-size scaling retains one authored kerning application.");
            }
            finally { server.FreeRID(scaled); }
            server.FontSetFixedSize(fontRID, 0);

            server.ShapedTextSetOrientation(buffer, TextOrientation.Vertical);
            Check(server.ShapedTextGetSize(buffer).Y > 0 && server.ShapedTextGetGlyphs(buffer).All(g => g.Offset.IsFinite() && g.Advance > 0), "Simple vertical layout uses finite nominal metrics without contextual substitutions.");
            server.ShapedTextSetOrientation(buffer, TextOrientation.Horizontal);
            copy = server.ShapedTextDuplicate(buffer); server.FreeRID(buffer); buffer = server.CreateShapedText();
            Check(server.ShapedTextGetWidth(copy) == 37, "Simple duplicates preserve independent authored configuration.");
            server.ShapedTextAddString(buffer, "one two\tthree\nlast", [font], 18);
            server.ShapedTextTabAlign(buffer, [50]);
            var breaks = server.ShapedTextGetLineBreaks(buffer, 65);
            Check(breaks.Length >= 4 && breaks[0] == 0 && breaks[^1] == 18, "Simple tabs and mandatory/word wrapping produce complete scalar ranges.");
            var key = new TextObjectKey(); server.ShapedTextAddObject(buffer, key, new(20, 15), length: 2);
            Check(server.ShapedTextGetObjectRange(buffer, key) == new Vector2i(18, 20) && server.ShapedTextGetObjectRect(buffer, key).Size == new Vector2(20, 15), "Simple inline objects retain typed ranges and actual geometry.");
            server.ShapedTextOverrunTrimToWidth(buffer, 45, TextOverrunFlags.Trim | TextOverrunFlags.AddEllipsis);
            Check(server.ShapedTextGetGlyphs(buffer).Length > 0 && server.ShapedTextGetSelection(buffer, 0, 3).Length > 0 && server.ShapedTextHitTestPosition(buffer, -100) == 0, "Simple trimming keeps raw glyph/editing geometry.");
            Reject<NotSupportedException>(() => server.ShapedTextSetDirection(buffer, TextDirection.RTL));
            Check(server.ShapedTextGetDirection(buffer) == TextDirection.LTR, "Rejected RTL edits preserve simple direction.");
            Reject<ArgumentOutOfRangeException>(() => server.ShapedTextSubstr(buffer, -1, 1));
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "A\u00a0A-A", [font], 18);
            Check(server.ShapedTextGetLineBreaks(buffer, 1).SequenceEqual([0, 5]), "Simple word wrapping preserves nonbreaking spaces and does not invent punctuation breaks.");
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "A\r\nA", [font], 18);
            var crlf = server.ShapedTextGetLineBreaks(buffer, 100);
            Check(crlf.SequenceEqual([0, 3, 3, 4]), "Simple mandatory wrapping treats CRLF as one break: " + string.Join(',', crlf) + " glyphs=" + string.Join(';', server.ShapedTextGetGlyphs(buffer).Select(g => $"{g.Start}:{g.End}:{g.Flags}")));
            server.ShapedTextClear(buffer); server.ShapedTextAddString(buffer, "سلام", [font], 18);
            server.ShapedTextFitToWidth(buffer, 200);
            Check(server.ShapedTextGetGlyphs(buffer) is { Length: 4 } arabic && arabic.All(g => (g.Flags & (TextGraphemeFlags.Virtual | TextGraphemeFlags.Elongation)) == 0) && server.ShapedTextGetInferredDirection(buffer) == TextDirection.LTR, "Simple Arabic input stays nominal/logical and does not synthesize contextual elongation.");
            server.FontSetMultichannelSignedDistanceField(fontRID, true); server.FontSetMSDFSize(fontRID, 32); server.FontSetMSDFPixelRange(fontRID, 4);
            server.FontRenderGlyph(fontRID, new(32, 0), a);
            using var atlas = server.FontGetTextureImage(fontRID, new(32, 0), 0);
            Check(server.FontGetTextureCount(fontRID, new(32, 0)) > 0 && atlas is not null && !atlas.IsEmpty, "Simple profile realizes an actual shared MSDF glyph atlas.");
        }
        finally
        {
            if (server.Has(buffer)) server.FreeRID(buffer);
            if (server.Has(copy)) server.FreeRID(copy);
            server.FreeRID(fontRID);
        }
        var bitmap = server.CreateFont(); var shaped = server.CreateShapedText();
        try
        {
            server.FontSetAllowSystemFallback(bitmap, false);
            using var page = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); page.Fill(Colors.Cyan); var size = new Vector2i(16, 0);
            server.FontSetTextureImage(bitmap, size, 0, page); server.FontSetGlyphOffset(bitmap, size, 'A', new(1, -10)); server.FontSetGlyphSize(bitmap, size, 'A', new(5, 9));
            server.FontSetGlyphUVRect(bitmap, size, 'A', new(2, 3, 5, 9)); server.FontSetGlyphTextureIndex(bitmap, size, 'A', 0); server.FontSetGlyphAdvance(bitmap, 16, 'A', new(8, 16));
            server.FontSetAscent(bitmap, 16, 12); server.FontSetDescent(bitmap, 16, 4); server.FontSetKerning(bitmap, 16, new('A', 'A'), new(-1, 0));
            server.ShapedTextAddString(shaped, "AA", [server.FontGetResource(bitmap)], 16);
            Check(server.ShapedTextGetWidth(shaped) == 15 && server.ShapedTextGetGlyphs(shaped).All(g => g.Index == 'A'), "Simple profile shapes authored bitmap glyphs and kerning without native outlines.");
        }
        finally { server.FreeRID(shaped); server.FreeRID(bitmap); }
    }
}
