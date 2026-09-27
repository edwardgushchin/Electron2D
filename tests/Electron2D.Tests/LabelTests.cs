using System.Globalization;
using Electron2D;

internal static class LabelTests
{
    internal static void Run(byte[] fontData)
    {
        VerifyDefaultsAndStorage(fontData);
        VerifyLinesAndAlignment(fontData);
        VerifyWrappingAndVisibility(fontData);
        VerifyStructuredText(fontData);
        VerifyEffectsAndResources(fontData);
        VerifyTranslation(fontData);
        VerifyWarmFrames(fontData, "de");
        VerifyWarmFrames(fontData, "ar");
        Console.WriteLine("Labels verify themed glyph drawing, paragraph and scalar layout, wrapping, visibility, structured bidi, effects, storage, lifetime and warm frame reuse.");
    }
    private static FontFile Font(byte[] bytes) => new() { Data = bytes, SubpixelPositioning = FontSubpixelPositioning.Quarter };

    private static void VerifyDefaultsAndStorage(byte[] data)
    {
        using var plain = new Label();
        Check(plain.Text == "" && plain.LabelSettings is null && plain.HorizontalAlignment == HorizontalAlignment.Left && plain.VerticalAlignment == VerticalAlignment.Top &&
            plain.AutowrapMode == TextAutowrapMode.Off && plain.AutowrapTrimFlags == (TextLineBreakFlags)192 && plain.JustificationFlags == (TextJustificationFlags)163 &&
            plain.TextDirection == TextDirection.Auto && plain.TextOverrunBehavior == TextOverrunBehavior.NoTrimming && plain.StructuredTextBIDIOverride == StructuredTextParser.Default &&
            plain.StructuredTextBIDIOverrideOptions.Length == 0 && plain.TabStops.Length == 0 && plain.Language == "" && plain.ParagraphSeparator == @"\n" && plain.EllipsisChar == "…" &&
            plain.VisibleCharacters == -1 && plain.VisibleRatio == 1 && plain.VisibleCharactersBehavior == TextVisibleCharactersBehavior.CharsBeforeShaping &&
            plain.LinesSkipped == 0 && plain.MaxLinesVisible == -1 && !plain.Uppercase && !plain.ClipText && plain.MouseFilter == MouseFilter.Ignore && plain.SizeFlagsVertical == Control.SizeFlags.ShrinkCenter,
            "Label defaults preserve all source text, visibility, input and sizing values.");
        plain.Text = "A\nB"; Check(plain.GetLineCount() == 1 && plain.GetTotalCharacterCount() == 3, "Detached line count remains one and the source character count includes newline scalars.");
        Reject<ArgumentNullException>(() => plain.Text = null!); Reject<ArgumentOutOfRangeException>(() => plain.LinesSkipped = -1);
        Reject<ArgumentOutOfRangeException>(() => plain.HorizontalAlignment = (HorizontalAlignment)4); Reject<ArgumentOutOfRangeException>(() => plain.VerticalAlignment = (VerticalAlignment)4);
        Reject<ArgumentOutOfRangeException>(() => plain.TextDirection = (TextDirection)4); Reject<ArgumentOutOfRangeException>(() => plain.VisibleRatio = float.NaN);
        Reject<ArgumentException>(() => plain.TabStops = [0]); Reject<ArgumentException>(() => plain.TabStops = [float.PositiveInfinity]);
        plain.AutowrapMode = (TextAutowrapMode)99; plain.TextOverrunBehavior = (TextOverrunBehavior)99; plain.VisibleCharactersBehavior = (TextVisibleCharactersBehavior)99; plain.StructuredTextBIDIOverride = (StructuredTextParser)99;
        Check((int)plain.AutowrapMode == 99 && (int)plain.TextOverrunBehavior == 99 && (int)plain.VisibleCharactersBehavior == 99 && (int)plain.StructuredTextBIDIOverride == 99, "Unguarded source enum setters retain unknown numeric storage values.");
        Reject<NotSupportedException>(() => plain.StructuredTextBIDIOverride = (StructuredTextParser)5);
        plain.TabStops = [-1, 3]; Check(plain.TabStops.SequenceEqual(new[] { -1f, 3f }), "Signed tab increments remain valid when their repeated cycle advances.");
        plain.AutowrapTrimFlags = (TextLineBreakFlags)255; Check(plain.AutowrapTrimFlags == (TextLineBreakFlags)224, "Autowrap trim storage retains indent/start/end flags and masks unrelated or deprecated edge flags.");
        plain.EllipsisChar = "\U0001F600rest"; Check(plain.EllipsisChar == "\U0001F600", "Ellipsis truncation retains one scalar rather than one UTF-16 code unit.");
        plain.VisibleCharacters = -2; Check(plain.VisibleCharacters == -2 && plain.VisibleRatio < 0, "Signed character storage preserves the source ratio relationship for negative counts other than minus one.");
        plain.VisibleRatio = 2; Check(plain.VisibleCharacters == -1 && plain.VisibleRatio == 1, "A ratio at or above one restores unlimited characters.");
        plain.VisibleRatio = -1; Check(plain.VisibleCharacters == 0 && plain.VisibleRatio == 0, "A negative ratio clamps both count and ratio to zero.");
        using var font = Font(data); using var settings = new LabelSettings { Font = font, FontSize = 20, ResourceLocalToScene = true };
        using var source = new Label("AV")
        {
            LabelSettings = settings,
            Uppercase = true,
            Language = "en",
            ParagraphSeparator = "|",
            TabStops = [12, 24],
            ClipText = true,
            TextDirection = TextDirection.RTL,
            StructuredTextBIDIOverride = StructuredTextParser.List,
            StructuredTextBIDIOverrideOptions = ["|"],
            VisibleRatio = .5f
        };
        using var scene = new PackedScene(); scene.Pack(source); using var copy = (Label)scene.Instantiate();
        Check(copy.GetType() == typeof(Label) && copy.Text == "AV" && copy.Uppercase && copy.Language == "en" && copy.ParagraphSeparator == "|" && copy.ClipText && copy.TextDirection == TextDirection.RTL &&
            copy.StructuredTextBIDIOverride == StructuredTextParser.List && copy.StructuredTextBIDIOverrideOptions.SequenceEqual(new[] { "|" }) && copy.TabStops.SequenceEqual(new[] { 12f, 24f }) &&
            copy.VisibleCharacters == 1 && copy.VisibleRatio == .5f && copy.LabelSettings is not null && copy.LabelSettings != settings && copy.LabelSettings.Font != font,
            "Packing restores exact label type, coupled visibility, arrays and independently owned local resource graphs.");
        var tabs = copy.TabStops; tabs[0] = 99; Check(copy.TabStops[0] == 12, "Array getters cannot mutate stored text configuration.");
        var descriptors = plain.GetPropertyList();
        var mouse = descriptors.Single(property => property.Name == nameof(Label.MouseFilter));
        Check(mouse is PropertyDescriptor<Label, MouseFilter> && mouse.ValueType == typeof(MouseFilter), "Derived input defaults have typed label storage descriptors.");
    }

    private static void VerifyLinesAndAlignment(byte[] data)
    {
        using var font = Font(data); using var settings = new LabelSettings { Font = font };
        var root = new Node(); var label = new Label("A\nV") { LabelSettings = settings, Size = new(100, 100) }; root.AddChild(label); using var tree = new SceneTree(root);
        Check(label.GetLineCount() == 2 && label.GetLineHeight() == 23 && label.GetVisibleLineCount() == 2 && label.GetMinimumSize() == new Vector2(11, 49),
            "Paragraph lines use exact glyph advances, minimum font height and the default three-pixel line gap.");
        Check(label.GetCharacterBounds(0) == new Rect2(0, 0, 677 / 64f, 23) && label.GetCharacterBounds(2).Position.Y == 26 && label.GetCharacterBounds(1) == default,
            "Character bounds keep full-text scalar indices across paragraph separators.");
        label.VerticalAlignment = VerticalAlignment.Fill; Check(label.GetCharacterBounds(2).Position.Y == 77, "Vertical fill distributes spare height between visible lines.");
        label.MaxLinesVisible = 1; label.VerticalAlignment = VerticalAlignment.Center;
        Check(label.GetVisibleLineCount() == 1 && label.GetCharacterBounds(0).Position.Y == 38 && label.GetCharacterBounds(2) == default,
            "Vertical centering uses the actual visible line count and integer source offsets.");
        label.LinesSkipped = 1; label.VerticalAlignment = VerticalAlignment.Top;
        Check(label.GetCharacterBounds(0) == default && label.GetCharacterBounds(2).Position.Y == 0, "Skipped lines do not reserve vertical space.");
        label.LinesSkipped = 0; label.Text = "A"; label.HorizontalAlignment = HorizontalAlignment.Center;
        Check(label.GetCharacterBounds(0).Position.X == 44, "Label centering uses its rounded line box before integer division.");
        label.HorizontalAlignment = HorizontalAlignment.Left; label.LayoutDirection = LayoutDirection.RTL;
        Check(label.GetCharacterBounds(0).Position.X == 89, "Inherited RTL layout mirrors logical left alignment independently of the text's character direction.");
        label.HorizontalAlignment = HorizontalAlignment.Right; Check(label.GetCharacterBounds(0).Position.X == 0, "Logical right alignment mirrors with RTL layout.");
        label.Text = "A\n"; label.MaxLinesVisible = -1; Check(label.GetLineCount() == 2, "Labels preserve trailing empty paragraphs using an invisible shaping sentinel.");
        label.Text = ""; Check(label.GetLineCount() == 1 && label.GetMinimumSize() == new Vector2(1, 23), "An empty label retains one line and the one-pixel minimum width.");
        Check(Task.Run(() => Capture(() => label.Text = "off-thread")).Result is InvalidOperationException, "Label mutations preserve scene owner-thread guards.");
    }

    private static void VerifyWrappingAndVisibility(byte[] data)
    {
        using var font = Font(data); using var settings = new LabelSettings { Font = font };
        var root = new Node(); var label = new Label("a\u0301a\u0301") { LabelSettings = settings, AutowrapMode = TextAutowrapMode.Arbitrary, ClipText = true, Size = new(10, 100) };
        root.AddChild(label); using var tree = new SceneTree(root);
        Check(label.GetLineCount() == 2 && label.GetMinimumSize() == Vector2.One, "Clipped wrapping has a one-pixel minimum while preserving grapheme clusters.");
        label.Text = "abcd"; label.AutowrapMode = TextAutowrapMode.Word; Check(label.GetLineCount() == 1, "Word mode preserves an oversized word.");
        label.AutowrapMode = TextAutowrapMode.WordSmart; Check(label.GetLineCount() > 1, "Smart word mode enables grapheme fallback.");
        label.AutowrapMode = TextAutowrapMode.Off; label.Size = new(100, 100); label.Text = "A\nV"; label.VisibleRatio = .5f;
        Check(label.VisibleCharacters == 1 && label.GetLineCount() == 1, "Before-shaping visibility changes paragraph and line layout.");
        label.VisibleCharactersBehavior = TextVisibleCharactersBehavior.CharsAfterShaping;
        Check(label.GetLineCount() == 2, "After-shaping visibility preserves the complete paragraph layout.");
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); Record(label, vertices, batches);
        Check(vertices.Count == 6, "After-shaping visibility records only clusters within the logical character limit.");
        label.VisibleCharacters = -1; label.ParagraphSeparator = "::"; label.Text = "A::V::"; Check(label.GetLineCount() == 3, "Multi-scalar paragraph separators preserve trailing empty fields.");
        label.ParagraphSeparator = string.Empty; Check(label.GetLineCount() == 1, "An empty paragraph separator keeps the text in one paragraph.");
        label.Text = "straße"; label.Uppercase = true; label.Language = "de";
        Check(label.GetTotalCharacterCount() == 6 && label.GetCharacterBounds(6).Size.X > 0, "Full uppercase expands display scalars without changing the source character count.");
        label.Uppercase = false; label.Text = "A\tV\tA"; label.TabStops = [30, 60]; label.ClipText = false;
        Check(label.GetCharacterBounds(2).Position.X == 30 && label.GetCharacterBounds(4).Position.X == 90, "Repeating tab increments position successive columns without introducing hardcoded tab widths.");
        label.ClipText = true; Check(label.CanvasClipRect == new Rect2(Vector2.Zero, label.Size), "Clipping exposes the exact label rectangle to retained canvas traversal.");
        label.ClipText = false; Check(label.CanvasClipRect is null, "Disabling clipping removes the retained clip instead of leaving stale native state.");
    }

    private static void VerifyStructuredText(byte[] data)
    {
        using var font = Font(data); using var settings = new LabelSettings { Font = font };
        using var control = new ParserLabel(); var contexts = new List<TextBIDIRange>();
        control.ParseStructuredText(StructuredTextParser.URI, Array.Empty<string>(), "a/אב?q=x", contexts);
        Check(contexts.Contains(new(1, 2, TextDirection.LTR)) && contexts.Contains(new(2, 4, TextDirection.Auto)) && contexts.Contains(new(4, 5, TextDirection.LTR)), "URI parsing isolates separators and directional components using scalar offsets.");
        control.ParseStructuredText(StructuredTextParser.Email, Array.Empty<string>(), "@ab.cd", contexts);
        Check(contexts[0] == new TextBIDIRange(0, 0, TextDirection.Auto) && contexts.Any(item => item == new TextBIDIRange(3, 4, TextDirection.LTR)), "Email parsing retains an empty local context and separately resolves the dotted domain.");
        control.ParseStructuredText(StructuredTextParser.List, new[] { "::" }, "a::ב::", contexts);
        Check(contexts.SequenceEqual(new[] { new TextBIDIRange(0, 1, TextDirection.Inherited), new(1, 3, TextDirection.Inherited), new(3, 4, TextDirection.Inherited), new(4, 6, TextDirection.Inherited) }),
            "List parsing covers its first field and complete multi-scalar separators without producing a phantom range beyond the text.");
        var root = new Node(); var label = new ParserLabel { Text = "אב", LabelSettings = settings, Size = new(100, 100) }; root.AddChild(label); using var tree = new SceneTree(root);
        Check(label.GetCharacterBounds(0).Position.X > label.GetCharacterBounds(1).Position.X, "The ordinary bidi paragraph reverses the RTL run.");
        label.StructuredTextBIDIOverride = StructuredTextParser.Custom;
        Check(label.GetCharacterBounds(0).Position.X < label.GetCharacterBounds(1).Position.X && label.Calls > 0,
            "The typed custom parser is consumed by layout and preserves independently resolved contexts in supplied order.");
        label.Invalid = true; label.Text = "בא"; Reject<InvalidOperationException>(() => label.GetCharacterBounds(0));
        label.Invalid = false; label.Text = "אב"; Check(label.GetCharacterBounds(0).Size.X > 0, "A failed custom parser does not poison subsequent layout rebuilds.");
    }

    private static void VerifyEffectsAndResources(byte[] data)
    {
        using var font = Font(data); using var settings = new LabelSettings { Font = font, FontColor = Colors.White, ShadowColor = Colors.Red, ShadowSize = 1 };
        settings.AddStackedOutline(); settings.SetStackedOutlineSize(0, 1); settings.SetStackedOutlineColor(0, Colors.Blue);
        settings.AddStackedShadow(); settings.SetStackedShadowColor(0, Colors.Green); settings.SetStackedShadowOutlineSize(0, 1);
        var root = new Node(); var label = new Label("A") { LabelSettings = settings, Size = new(100, 100) }; root.AddChild(label); using var tree = new SceneTree(root);
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); Record(label, vertices, batches);
        Check(vertices.Count == 36 && vertices[0].Color == Colors.Red && vertices[12].Color == Colors.Green && vertices[24].Color == Colors.Blue && vertices[30].Color == Colors.White,
            "Effects record complete shadow-outline, shadow, stacked shadow, cumulative outline and final text passes in source order.");
        var oldHeight = label.GetLineHeight(); Task.Run(() => settings.FontSize = 32).GetAwaiter().GetResult(); tree.ProcessFrame(0);
        Check(label.GetLineHeight() > oldHeight, "Background settings changes marshal layout invalidation onto the scene owner.");
        root.RemoveChild(label); Task.Run(() => settings.FontSize = 16).GetAwaiter().GetResult(); root.AddChild(label); tree.ProcessFrame(0);
        Check(label.GetLineHeight() == 23, "Reentry observes current resource content and ignores deferred callbacks from the prior membership.");
        using var missedFont = Font(data); using var missedSettings = new LabelSettings { Font = missedFont };
        missedSettings.Changed += _ => throw new ApplicationException("expected earlier settings observer");
        label.LabelSettings = missedSettings; Record(label, vertices, batches);
        Reject<ApplicationException>(() => missedSettings.FontSize = 24); tree.ProcessFrame(0);
        Check(label.GetLineHeight() > 23, "Revision polling recovers a settings update hidden by an earlier throwing Changed subscriber.");
        var borrowed = label.LabelSettings; label.LabelSettings = null; borrowed!.Dispose();
        Check(!font.IsDisposed && !label.IsDisposed, "Labels detach borrowed settings subscriptions without assuming ownership.");
    }

    private static void VerifyTranslation(byte[] data)
    {
        var oldCulture = TranslationServer.Culture;
        using var font = Font(data); using var settings = new LabelSettings { Font = font };
        using var catalog = new Translation { Locale = "de" }; catalog.AddMessage("label.test.source", "straße\nA");
        using var domain = TranslationServer.GetOrAddDomain("label.test.domain"); domain.LocaleOverride = "de"; domain.AddTranslation(catalog);
        try
        {
            TranslationServer.Culture = CultureInfo.GetCultureInfo("de"); TranslationServer.AddTranslation(catalog);
            var root = new Node { TranslationDomain = "label.test.domain" };
            var translated = new Label("label.test.source") { LabelSettings = settings, Uppercase = true, Size = new(200, 100) }; root.AddChild(translated);
            var noTranslate = new Node { AutoTranslateMode = NodeAutoTranslateMode.Disabled }; root.AddChild(noTranslate);
            var raw = new Label("label.test.source") { LabelSettings = settings, Size = new(200, 100) }; noTranslate.AddChild(raw);
            using var tree = new SceneTree(root);
            Check(translated.Text == "label.test.source" && translated.GetTotalCharacterCount() == 8 && translated.GetLineCount() == 2,
                "A label translates through its inherited domain, then performs display casing without changing its source text.");
            Check(raw.GetTotalCharacterCount() == "label.test.source".Length && raw.GetLineCount() == 1,
                "Tree entry refreshes a constructed label under an inherited disabled-translation boundary.");
            catalog.AddMessage("label.test.source", "AV"); root.PropagateNotification(Node.NotificationTranslationChanged);
            Check(translated.GetTotalCharacterCount() == 2 && translated.GetLineCount() == 1, "Translation notifications refresh live catalog edits and reshape cached paragraphs.");
        }
        finally { TranslationServer.RemoveTranslation(catalog); TranslationServer.Culture = oldCulture; }
    }

    private static void VerifyWarmFrames(byte[] data, string locale)
    {
        using var font = Font(data); using var settings = new LabelSettings { Font = font };
        using var catalog = new Translation { Locale = locale }; catalog.AddMessage("label.one", "straße"); catalog.AddMessage("label.two", "ﬃ AV");
        using var domain = TranslationServer.GetOrAddDomain("label.warm.domain"); domain.LocaleOverride = locale; domain.AddTranslation(catalog);
        var root = new Node { TranslationDomain = "label.warm.domain" };
        var label = new Label("label.one") { LabelSettings = settings, Uppercase = true, Language = "de", Size = new(200, 100) }; root.AddChild(label); using var tree = new SceneTree(root);
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        for (var i = 0; i < 32; i++) { label.Text = (i & 1) == 0 ? "label.one" : "label.two"; tree.ProcessFrame(0); Record(label, vertices, batches); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        long writes = 0, frames = 0, records = 0;
        for (var i = 0; i < 64; i++)
        {
            var phase = GC.GetAllocatedBytesForCurrentThread(); label.Text = (i & 1) == 0 ? "label.one" : "label.two"; writes += GC.GetAllocatedBytesForCurrentThread() - phase;
            phase = GC.GetAllocatedBytesForCurrentThread(); tree.ProcessFrame(0); frames += GC.GetAllocatedBytesForCurrentThread() - phase;
            phase = GC.GetAllocatedBytesForCurrentThread(); Record(label, vertices, batches); records += GC.GetAllocatedBytesForCurrentThread() - phase;
        }
        var active = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(active == 0, $"Warmed active uppercase text changes and glyph recording must reuse storage; allocated {active} bytes (writes={writes}, frames={frames}, records={records}).");
        before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) { tree.ProcessFrame(0); label.GetLineCount(); label.GetCharacterBounds(0); }
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Idle label revision checks and geometry queries must not allocate.");
    }
    private sealed class ParserLabel : Label
    {
        internal int Calls;
        internal bool Invalid;
        protected override TextBIDIRange[] OnStructuredTextParser(IReadOnlyList<string> options, string text)
        {
            Calls++; return Invalid ? [new(0, 9999, TextDirection.LTR)] : [new(0, 1, TextDirection.Auto), new(1, 2, TextDirection.Auto), new(2, CountScalars(text.AsSpan()), TextDirection.Auto)];
        }
    }
    private static void Record(Label label, List<CanvasVertex> vertices, List<CanvasBatch> batches)
    {
        label.InvalidateCanvas(); label.PrepareCanvas(); vertices.Clear(); batches.Clear(); label.AppendCanvas(vertices, batches, Transform.Identity);
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
