using System.Globalization;
using System.Text;
using Electron2D;

internal static class TextLayoutBreakTests
{
    // Independent ICU 78.3 C API oracle, UBRK_LINE with the listed locale and UBRK_WORD with root locale.
    private static readonly (string Text, string Locale, int[] Lines, int[] Words)[] DictionaryCases =
    [
        ("ภาษาไทยภาษาไทย", "th", [4, 7, 11, 14], [4, 7, 11, 14]),
        ("ພາສາລາວພາສາລາວ", "lo", [4, 7, 11, 14], [4, 7, 11, 14]),
        ("ភាសាខ្មែរភាសាខ្មែរ", "km", [9, 18], [9, 18]),
        ("မြန်မာဘာသာမြန်မာဘာသာ", "my", [10, 20], [10, 20]),
        ("中华人民共和国", "zh", [1, 2, 3, 4, 5, 6, 7], [2, 4, 7]),
        ("あぁい", "ja", [1, 2, 3], [2, 3]),
        ("あぁい", "ja@lb=strict", [2, 3], [2, 3])
    ];

    internal static void Run()
    {
        VerifyDictionaryBoundaries();
        VerifyLayoutConsumers();
        VerifyNeutralDirection();
        VerifyDefaultLocaleReuse();
        Console.WriteLine("Text layout verifies ICU dictionary and locale boundaries, CJK word expansion, neutral locale direction and warmed default-locale rebuilds.");
    }
    private static void VerifyDictionaryBoundaries()
    {
        Check(NativeTextBreak.IsNonprinting(0) && NativeTextBreak.IsNonprinting(0x1b) && NativeTextBreak.IsNonprinting(0x378) &&
            NativeTextBreak.IsNonprinting(0x10ffff) && !NativeTextBreak.IsNonprinting(0x20) && !NativeTextBreak.IsNonprinting(0x9) &&
            !NativeTextBreak.IsNonprinting(0x4e00) && !NativeTextBreak.IsNonprinting(0xe000) && !NativeTextBreak.IsNonprinting(0x10fffd),
            "Native nonprinting classification distinguishes controls and unassigned scalars from blanks, CJK and private-use graphic characters.");
        foreach (var item in DictionaryCases)
        {
            var offsets = ScalarOffsets(item.Text); var lines = new bool[offsets.Length]; var words = new bool[offsets.Length]; var ends = new bool[offsets.Length];
            NativeTextBreak.Fill(item.Text, item.Locale, offsets, lines, words, ends);
            Check(Positions(lines).SequenceEqual(item.Lines), $"Line boundaries for {item.Locale} must match the independent native oracle.");
            Check(Positions(ends).SequenceEqual(item.Words), $"Word/number boundaries for {item.Locale} must retain native dictionary rule status.");
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) NativeTextBreak.Fill(item.Text, item.Locale, offsets, lines, words, ends);
            Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Warmed native boundary fills must reuse iterator and managed output storage.");
        }
        var text = "A😀 B"; var scalarOffsets = ScalarOffsets(text); var lineFlags = new bool[scalarOffsets.Length]; var wordFlags = new bool[scalarOffsets.Length]; var wordEnds = new bool[scalarOffsets.Length];
        NativeTextBreak.Fill(text, "en", scalarOffsets, lineFlags, wordFlags, wordEnds);
        Check(scalarOffsets.Length == 5 && lineFlags[^1] && wordFlags[^1], "Native UTF-16 boundaries map back to whole Unicode scalars around supplementary characters.");
    }
    private static void VerifyLayoutConsumers()
    {
        using var empty = new FontFile(); var layout = new TextLayout();
        foreach (var item in DictionaryCases.Take(4))
        {
            var width = (item.Lines[0] + 1) * 12;
            layout.Build(empty, Key(item.Text, width), new TextLayoutOptions(Language: item.Locale, Overrun: 0, VisibleCharacters: -1));
            Check(layout.LineCount > 1 && layout.Lines[0].End == item.Lines[0],
                "Dictionary opportunities must control actual font layout, including when glyph fallback uses measurable hexadecimal boxes.");
        }
        layout.Build(empty, Key("あぁい", 13), new TextLayoutOptions(Language: "ja", Overrun: 0, VisibleCharacters: -1));
        Check(layout.Lines[0].End == 1, "Default Japanese line behavior retains its ordinary small-kana boundary.");
        layout.Build(empty, Key("あぁい", 13), new TextLayoutOptions(Language: "ja@lb=strict", Overrun: 0, VisibleCharacters: -1));
        Check(layout.Lines[0].End == 2, "Explicit strict Japanese tailoring reaches the actual width-breaking algorithm.");
        using var cjk = new FontFile { Data = FontTestFixtures.CJK, SubpixelPositioning = FontSubpixelPositioning.Quarter };
        const string chinese = "中华人民共和国";
        layout.Build(cjk, Key(chinese, -1) with { Alignment = HorizontalAlignment.Left }, new TextLayoutOptions(Language: "zh", Overrun: 0, VisibleCharacters: -1));
        var firstAdvance = layout.GetCharacterBounds(0).Size.X; var wordStart = layout.GetCharacterBounds(2).Position.X; var secondWordStart = layout.GetCharacterBounds(4).Position.X;
        var widthToFill = layout.Size.X + 28;
        layout.Build(cjk, Key(chinese, widthToFill) with { Alignment = HorizontalAlignment.Fill, Justification = TextJustificationFlags.WordBound }, new TextLayoutOptions(Language: "zh", Overrun: 0, VisibleCharacters: -1));
        Check(layout.Size.X == widthToFill && layout.GetCharacterBounds(1).Position.X == firstAdvance &&
            MathF.Abs(layout.GetCharacterBounds(2).Position.X - wordStart - 14) < .01f && MathF.Abs(layout.GetCharacterBounds(4).Position.X - secondWordStart - 28) < .01f,
            "CJK fill expands dictionary word boundaries rather than inserting spacing after every ideograph.");
    }
    private static void VerifyNeutralDirection()
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans }; var layout = new TextLayout();
        foreach (var language in new[] { "ar", "ff", "ku", "he-IL" })
        {
            layout.Build(font, Key("123", -1), new TextLayoutOptions(Language: language, Overrun: 0, VisibleCharacters: -1));
            Check(layout.Lines[0].ParagraphLevel == 1, "Neutral automatic direction uses the exact supported language RTL policy.");
        }
        layout.Build(font, Key("123", -1), new TextLayoutOptions(Language: "az-Arab", Overrun: 0, VisibleCharacters: -1));
        Check(layout.Lines[0].ParagraphLevel == 0, "Neutral direction uses the reference language policy instead of substituting a broader script-orientation heuristic.");
        layout.Build(font, Key("A123", -1), new TextLayoutOptions(Language: "ar", Overrun: 0, VisibleCharacters: -1));
        Check(layout.Lines[0].ParagraphLevel == 0, "A strong LTR character overrides the locale fallback.");
        using var settings = new LabelSettings { Font = font }; var root = new Node();
        var label = new Label("12345") { LabelSettings = settings, Language = "ar", TextOverrunBehavior = TextOverrunBehavior.TrimChar, ClipText = true, Size = new(20, 30), LayoutDirection = LayoutDirection.LTR };
        root.AddChild(label); using var tree = new SceneTree(root);
        Check(label.GetCharacterBounds(0) == default && label.GetCharacterBounds(4).Size.X > 0,
            "Neutral RTL paragraph direction changes which logical digits survive actual label trimming.");
    }
    private static void VerifyDefaultLocaleReuse()
    {
        var previous = TranslationServer.Culture;
        using var catalog = new Translation { Locale = "de-DE" };
        using var font = new FontFile { Data = FontTestFixtures.OpenSans };
        try
        {
            TranslationServer.Culture = CultureInfo.GetCultureInfo("de-AT"); TranslationServer.AddTranslation(catalog);
            Check(TranslationServer.GetToolLocale() == "de-DE", "Tool locale scoring preserves a closest nonidentical main-catalog locale.");
            for (var i = 0; i < 16; i++) { font.InvalidateFont(); font.GetStringSize("123 ffi"); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { font.InvalidateFont(); font.GetStringSize("123 ffi"); }
            Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Default-language font rebuilds with a real main catalog must not allocate locale snapshots or parsing iterators.");
            catalog.Locale = "ar"; TranslationServer.Culture = CultureInfo.GetCultureInfo("ar-EG"); font.InvalidateFont();
            var layout = font.GetLayout("123", HorizontalAlignment.Left, -1, 16, -1, TextLineBreakFlags.None, TextJustificationFlags.None, TextDirection.Auto, TextOrientation.Horizontal, false);
            Check(layout.Lines[0].ParagraphLevel == 1, "Empty language uses the current best main-catalog locale for neutral bidi fallback.");
            for (var i = 0; i < 16; i++) { font.InvalidateFont(); font.GetStringSize("123"); }
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { font.InvalidateFont(); font.GetStringSize("123"); }
            Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Repeated neutral RTL locale resolution stays allocation-free after warm-up.");
        }
        finally { TranslationServer.RemoveTranslation(catalog); TranslationServer.Culture = previous; }
    }
    private static TextLayoutKey Key(string text, float width) => new(text, 16, width, HorizontalAlignment.Left, -1,
        TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound, TextJustificationFlags.None, TextDirection.Auto, TextOrientation.Horizontal, true);
    private static int[] ScalarOffsets(string text)
    {
        var result = new List<int> { 0 }; var offset = 0;
        foreach (var rune in text.EnumerateRunes()) { offset += rune.Utf16SequenceLength; result.Add(offset); }
        return result.ToArray();
    }
    private static IEnumerable<int> Positions(bool[] flags) => Enumerable.Range(1, flags.Length - 1).Where(index => flags[index]);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
