using Script = Electron2D.TextFormatting.Unicode.Script;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Electron2D;
using Electron2D.TextFormatting.Unicode;
using Electron2D.TextFormatting.Utilities;

internal static class UnicodeTextTests
{
    internal static void Run(string? fixturePath = null)
    {
        using var stream = fixturePath is null
            ? typeof(UnicodeTextTests).Assembly.GetManifestResourceStream("TestFixtures.UnicodeTextConformance.json") ?? throw new InvalidOperationException("Missing Unicode conformance fixture.")
            : File.OpenRead(fixturePath);
        using var fixture = JsonDocument.Parse(stream);
        var root = fixture.RootElement;
        Check(root.GetProperty("unicodeVersion").GetString() == UnicodeDataSource.Version && UnicodeDataSource.Version == "17.0.0" &&
            root.GetProperty("upstreamCommit").GetString() == "8eeda4f6f546165b3f72e63c9f42247abb306905", "Unicode fixture and imported algorithm/data revisions agree.");
        foreach (var source in root.GetProperty("sources").EnumerateArray())
        {
            var expected = source.GetProperty("name").GetString() switch
            {
                "GraphemeBreakTest.txt" => "e2d134d2c52919bace503ebb6a551c1855fe1a1faec18478c78fff254a1793ec",
                "LineBreakTest.txt" => "e69884e0dde6a8724873f885d68c52dc14518abf9ae4ca9e2283b8773db3b752",
                "WordBreakTest.txt" => "1de23a75f37904abc7d206239ee8d34f8fdf0fb4ab32a7174dfbabbde25419b2",
                "BidiTest.txt" => "888bdfc8090652272d1f859cdb00ae659e2dc6c26740be61ef1d03998a687620",
                "BidiCharacterTest.txt" => "a3e6e905ab5afbe318a96df5401d0372a04cd73ef139ab5e3cf0ae241c255488",
                _ => throw new InvalidOperationException("Unexpected conformance source.")
            };
            Check(source.GetProperty("upstreamSHA256").GetString() == expected, "Official conformance data retain their pinned hashes.");
        }
        var graphemes = VerifyBreaks(root.GetProperty("grapheme"), grapheme: true);
        var lines = VerifyBreaks(root.GetProperty("lineBreak"), grapheme: false);
        var words = VerifyBreaks(root.GetProperty("wordBreak"), grapheme: false, word: true);
        var bidi = new BidiAlgorithm(); var adapter = new TextBIDI(); var characterCases = 0; var classCases = 0;
        int[] regressionLines = [85, 86, 87, 88, 89, 90, 104, 110, 115]; var seenRegressions = new HashSet<int>();
        foreach (var test in root.GetProperty("bidiCharacters").EnumerateArray())
        {
            var parts = test.GetProperty("data").GetString()!.Split(';');
            adapter.Resolve(FromCodepoints(parts[0]), sbyte.Parse(parts[1], CultureInfo.InvariantCulture));
            Check(adapter.ParagraphLevel == int.Parse(parts[2], CultureInfo.InvariantCulture), "Bidi paragraph direction at official line " + test.GetProperty("line"));
            VerifyLevels(adapter.Levels, Numbers(parts[3]), "BidiCharacterTest", test.GetProperty("line").GetInt32());
            VerifyReorder(adapter.Levels, Numbers(parts[3]), Numbers(parts[4]), "BidiCharacterTest", test.GetProperty("line").GetInt32());
            if (regressionLines.Contains(test.GetProperty("line").GetInt32())) seenRegressions.Add(test.GetProperty("line").GetInt32());
            characterCases++;
        }
        foreach (var test in root.GetProperty("bidiClasses").EnumerateArray())
        {
            var parts = test.GetProperty("data").GetString()!.Split(';'); var classes = Words(parts[0]).Select(Class).ToArray();
            var mask = int.Parse(parts[1].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture); var levels = Numbers(test.GetProperty("levels").GetString()!);
            for (var flag = 1; flag <= 4; flag <<= 1)
            {
                if ((mask & flag) == 0) continue;
                bidi.Process(classes, ArraySlice<BidiPairedBracketType>.Empty, ArraySlice<int>.Empty, (sbyte)(flag == 1 ? 2 : flag == 2 ? 0 : 1), false, null, null, null);
                VerifyLevels(bidi.ResolvedLevels.Span, levels, "BidiTest", test.GetProperty("line").GetInt32());
                VerifyReorder(bidi.ResolvedLevels.Span, levels, Numbers(test.GetProperty("order").GetString()!), "BidiTest", test.GetProperty("line").GetInt32());
                classCases++;
            }
        }
        Check(seenRegressions.Count == 9, "The permanent fixture includes all nine raw N0/NSM counterexamples without changing their official expectations.");
        VerifyAdapterScope();
        VerifyScripts();
        VerifyBoundariesAndWarmReuse();
        Check(!typeof(Codepoint).Assembly.GetExportedTypes().Any(type => type.Namespace?.StartsWith("Electron2D.TextFormatting", StringComparison.Ordinal) == true), "The Unicode dependency has no public exported types.");
        Console.WriteLine($"Unicode {UnicodeDataSource.Version} conformance passed: grapheme={graphemes}, word-break={words}, line-break={lines}, bidi-character={characterCases}, bidi-class/direction={classCases}; warmed reusable algorithms=0 B.");
    }

    private static int VerifyBreaks(JsonElement cases, bool grapheme, bool word = false)
    {
        var actual = new List<int>(); var expected = new List<int>(); var text = new StringBuilder(); var count = 0;
        foreach (var test in cases.EnumerateArray())
        {
            actual.Clear(); expected.Clear(); text.Clear();
            foreach (var token in Words(test.GetProperty("data").GetString()!))
            {
                if (token == "÷") { if (text.Length != 0) expected.Add(text.Length); }
                else if (token != "×") text.Append(char.ConvertFromUtf32(int.Parse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
            }
            var value = text.ToString();
            if (word)
            {
                var iterator = new WordBreakEnumerator(value);
                while (iterator.MoveNext(out var segment))
                {
                    actual.Add(segment.Offset + segment.Length);
                    Check(CountScalars(value.AsSpan(0, segment.Offset)) == segment.CodepointOffset &&
                        CountScalars(value.AsSpan(segment.Offset, segment.Length)) == segment.CodepointLength,
                        "Word segments expose scalar indices independently from UTF-16 indices.");
                }
            }
            else if (grapheme)
            {
                var iterator = new GraphemeEnumerator(value);
                while (iterator.MoveNext(out var cluster)) actual.Add(cluster.Offset + cluster.Length);
            }
            else
            {
                var iterator = new LineBreakEnumerator(value);
                while (iterator.MoveNext(out var point)) actual.Add(point.PositionWrap);
            }
            if (!actual.SequenceEqual(expected)) throw new InvalidOperationException($"{(word ? "WordBreakTest" : grapheme ? "GraphemeBreakTest" : "LineBreakTest")} line {test.GetProperty("line")}: expected [{string.Join(',', expected)}], actual [{string.Join(',', actual)}].");
            count++;
        }
        return count;
    }

    private static void VerifyLevels(ReadOnlySpan<sbyte> actual, int[] expected, string source, int line)
    {
        Check(actual.Length == expected.Length, $"{source} line {line}: level count differs.");
        for (var index = 0; index < expected.Length; index++)
            if (expected[index] >= 0 && actual[index] != expected[index])
                throw new InvalidOperationException($"{source} line {line}, index {index}: expected level {expected[index]}, actual {actual[index]}.");
    }

    private static void VerifyReorder(ReadOnlySpan<sbyte> levels, int[] expectedLevels, int[] expectedOrder, string source, int line)
    {
        // Conformance-only UAX9 L2 projection; the imported algorithm exposes resolved levels.
        var order = Enumerable.Range(0, levels.Length).Where(index => expectedLevels[index] >= 0).ToArray();
        var highest = 0; var lowestOdd = int.MaxValue;
        foreach (var index in order) { highest = Math.Max(highest, levels[index]); if ((levels[index] & 1) != 0) lowestOdd = Math.Min(lowestOdd, levels[index]); }
        for (var level = highest; level >= lowestOdd; level--)
            for (var start = 0; start < order.Length;)
            {
                if (levels[order[start]] < level) { start++; continue; }
                var end = start + 1; while (end < order.Length && levels[order[end]] >= level) end++;
                Array.Reverse(order, start, end - start); start = end;
            }
        if (!order.SequenceEqual(expectedOrder)) throw new InvalidOperationException($"{source} line {line}: resolved levels disagree with official visual order.");
    }

    private static void VerifyAdapterScope()
    {
        var adapter = new TextBIDI(); var core = new BidiAlgorithm(); var data = new BidiData();
        foreach (var text in new[] { "a(b)\u0331", "a(b)\u202A\u202C\u0331", "a(b)\u202B\u0331\u202C", "A\u2067(א)\u0331\u2069 B", "(\u202B\u0331\u202C", "A\uD800B" })
        {
            data.Reset(); data.Append(text); core.Process(data.Classes, data.PairedBracketTypes, data.PairedBracketValues, 1, null, null, null, null);
            adapter.Resolve(text, 1); var iterator = new CodepointEnumerator(text); var index = 0;
            while (iterator.MoveNext(out var codepoint))
            {
                var annotated = codepoint.BiDiClass == BidiClass.NonspacingMark && data.Classes[index] is BidiClass.LeftToRight or BidiClass.RightToLeft;
                if (!annotated) Check(adapter.Levels[index] == core.ResolvedLevels[index], "The engine adapter preserves every class and NSM outside the core's N0 annotations.");
                index++;
            }
        }
        adapter.Resolve(ReadOnlySpan<char>.Empty, 1); Check(adapter.Levels.IsEmpty && adapter.ParagraphLevel == 1, "An empty RTL paragraph retains its explicit direction and no previous levels.");
        adapter.Resolve(ReadOnlySpan<char>.Empty); Check(adapter.ParagraphLevel == 0, "An empty automatic paragraph defaults to LTR.");
        try { adapter.Resolve("x", 3); throw new InvalidOperationException("Invalid paragraph direction was accepted."); } catch (ArgumentOutOfRangeException) { }
    }

    private static void VerifyBoundariesAndWarmReuse()
    {
        var empty = new GraphemeEnumerator(ReadOnlySpan<char>.Empty); Check(!empty.MoveNext(out _), "An empty span has no grapheme.");
        var lines = new LineBreakEnumerator("a\r\nb"); Check(lines.MoveNext(out var first) && first.Required && first.PositionMeasure == 1 && first.PositionWrap == 3, "CRLF is one mandatory line break with separate measure/wrap offsets.");
        var scalars = new CodepointEnumerator("A\U0001F600B"); var scalarCount = 0; while (scalars.MoveNext(out _)) scalarCount++;
        Check(scalarCount == 3, "Supplementary scalars do not become two bidi characters.");
        const string value = "abc (אבג) 123 العربية a\u0301 👩‍👩‍👧‍👦\r\n第二行";
        var bidi = new TextBIDI(); var observed = 0;
        void Cycle()
        {
            bidi.Resolve(value); observed += bidi.Levels.Length;
            bidi.Resolve("a(b)\u0331", 1); observed += bidi.Levels[4];
            var graphemes = new GraphemeEnumerator(value); while (graphemes.MoveNext(out var cluster)) observed += cluster.Length;
            var breaker = new LineBreakEnumerator(value); while (breaker.MoveNext(out var point)) observed += point.PositionWrap;
            var words = new WordBreakEnumerator(value); while (words.MoveNext(out var segment)) observed += segment.CodepointLength;
        }
        for (var pass = 0; pass < 64; pass++) Cycle();
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) Cycle(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0 && observed > 0, $"Warmed bidi/grapheme/line-break reuse allocated {allocated} bytes.");
    }

    private static void VerifyScripts()
    {
        Check(TextScript.ToTag(Script.Latin) == 0x4C61746Eu && TextScript.ToTag(Script.Arabic) == 0x41726162u &&
            TextScript.ToTag(Script.Inherited) == 0x5A696E68u && TextScript.ToTag(Script.ZanabazarSquare) == 0x5A616E62u,
            "Scripts map to the shaping backend's big-endian ISO 15924 tags.");
        var scripts = Enum.GetValues<Script>(); var seen = new HashSet<uint>();
        foreach (var script in scripts) Check(seen.Add(TextScript.ToTag(script)), "Every Unicode script has its own tag.");
        Check(scripts.Length == 176 && !TextScript.IsStrong(Script.Unknown) && !TextScript.IsStrong(Script.Inherited) &&
            !TextScript.IsStrong(Script.Common) && TextScript.IsStrong(Script.Latin), "The complete pinned script set separates contextual scripts.");
        try { TextScript.ToTag((Script)(-1)); throw new InvalidOperationException("Invalid script was accepted."); }
        catch (ArgumentOutOfRangeException) { }
        Check(TextScript.IsCompatible('A', Script.Latin) && !TextScript.IsCompatible('A', Script.Arabic), "Strong scripts restrict run adoption.");
        Check(TextScript.IsCompatible('(', Script.Latin) && TextScript.IsCompatible('(', Script.Arabic) &&
            TextScript.IsCompatible(0x200D, Script.Devanagari), "Unrestricted punctuation and inherited joiners adopt the surrounding script.");
        Check(TextScript.IsCompatible(0x30FC, Script.Hiragana) && TextScript.IsCompatible(0x30FC, Script.Katakana) &&
            !TextScript.IsCompatible(0x30FC, Script.Latin), "Common prolonged sound mark obeys explicit Japanese Script_Extensions.");
        Check(TextScript.IsCompatible(0x0363, Script.Latin) && !TextScript.IsCompatible(0x0363, Script.Arabic),
            "Inherited combining Latin small letter obeys explicit Script_Extensions.");
        uint observed = 0;
        for (var pass = 0; pass < 128; pass++) { observed ^= TextScript.ToTag(Script.Arabic); observed += TextScript.IsCompatible(0x30FC, Script.Katakana) ? 1u : 0u; }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var pass = 0; pass < 128; pass++) { observed ^= TextScript.ToTag(Script.Arabic); observed += TextScript.IsCompatible(0x30FC, Script.Katakana) ? 1u : 0u; }
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0 && observed != 0, "Warmed script lookup allocates no managed bytes.");
    }

    private static string[] Words(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    private static int CountScalars(ReadOnlySpan<char> text) { var count = 0; foreach (var _ in text.EnumerateRunes()) count++; return count; }
    private static int[] Numbers(string text) => Words(text).Select(value => value == "x" ? -1 : int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
    private static string FromCodepoints(string text) => string.Concat(Words(text).Select(value => char.ConvertFromUtf32(int.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture))));
    private static BidiClass Class(string value) => value switch
    {
        "L" => BidiClass.LeftToRight,
        "R" => BidiClass.RightToLeft,
        "AL" => BidiClass.ArabicLetter,
        "EN" => BidiClass.EuropeanNumber,
        "ES" => BidiClass.EuropeanSeparator,
        "ET" => BidiClass.EuropeanTerminator,
        "AN" => BidiClass.ArabicNumber,
        "CS" => BidiClass.CommonSeparator,
        "NSM" => BidiClass.NonspacingMark,
        "BN" => BidiClass.BoundaryNeutral,
        "B" => BidiClass.ParagraphSeparator,
        "S" => BidiClass.SegmentSeparator,
        "WS" => BidiClass.WhiteSpace,
        "ON" => BidiClass.OtherNeutral,
        "LRE" => BidiClass.LeftToRightEmbedding,
        "LRO" => BidiClass.LeftToRightOverride,
        "RLE" => BidiClass.RightToLeftEmbedding,
        "RLO" => BidiClass.RightToLeftOverride,
        "PDF" => BidiClass.PopDirectionalFormat,
        "LRI" => BidiClass.LeftToRightIsolate,
        "RLI" => BidiClass.RightToLeftIsolate,
        "FSI" => BidiClass.FirstStrongIsolate,
        "PDI" => BidiClass.PopDirectionalIsolate,
        _ => throw new InvalidOperationException("Unknown official bidi class: " + value)
    };
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
