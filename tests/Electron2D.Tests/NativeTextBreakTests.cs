using System.Text;
using Electron2D;

internal static class NativeTextBreakTests
{
    internal static void Run()
    {
        CheckBoundaries("ภาษาไทยภาษาไทย", "th", [4, 7, 11, 14]);
        CheckBoundaries("ພາສາລາວພາສາລາວ", "lo", [4, 7, 11, 14]);
        CheckBoundaries("ភាសាខ្មែរភាសាខ្មែរ", "km", [9, 18]);
        CheckBoundaries("မြန်မာဘာသာမြန်မာဘာသာ", "my", [10, 20]);
        CheckBoundaries("中华人民共和国", "zh", [1, 2, 3, 4, 5, 6, 7], [2, 4, 7]);
        CheckBoundaries("あぁい", "ja", [1, 2, 3]);
        CheckBoundaries("あぁい", "ja@lb=strict", [2, 3]);
        CheckBoundaries("a\nb", "en", [2, 3]);
        var text = "\U0001F600 A\U0001F600";
        var offsets = ScalarOffsets(text); var lines = new bool[offsets.Length]; var words = new bool[offsets.Length]; var ends = new bool[offsets.Length];
        NativeTextBreak.Fill(text, "en", offsets, lines, words, ends);
        Check(lines[^1] && words[^1] && offsets[^1] == text.Length, "Supplementary scalars retain complete UTF-16 input while boundary outputs address scalars.");
        for (var i = 0; i < 64; i++) NativeTextBreak.Fill(text, "en", offsets, lines, words, ends);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) NativeTextBreak.Fill(text, "en", offsets, lines, words, ends);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed native boundary calls reuse managed buffers and locale iterators without allocation.");
        for (var i = 0; i < 70; i++) NativeTextBreak.Fill("ab", "en@x=" + i, new[] { 0, 1, 2 }, new bool[3], new bool[3], new bool[3]);
        CheckBoundaries("ภาษาไทยภาษาไทย", "th", [4, 7, 11, 14]);
        Parallel.For(0, 8, _ => CheckBoundaries("中华人民共和国", "zh", [1, 2, 3, 4, 5, 6, 7], [2, 4, 7]));
        Check(NativeTextBreak.IsLocaleRTL("ar") && NativeTextBreak.IsLocaleRTL("ar-SA") && NativeTextBreak.IsLocaleRTL("ff_Latn") &&
            NativeTextBreak.IsLocaleRTL("ku") && !NativeTextBreak.IsLocaleRTL("az-Arab") && !NativeTextBreak.IsLocaleRTL("en"),
            "Neutral paragraph fallback preserves the specified language policy and adapts the managed culture-tag separator.");
        Console.WriteLine("Private ICU boundaries verify dictionary scripts, CJK words, locale tailoring, scalar positions, cache eviction, concurrent use and warmed zero managed allocations.");
    }
    private static void CheckBoundaries(string text, string locale, int[] expectedLines, int[]? expectedWords = null)
    {
        var offsets = ScalarOffsets(text); var lines = new bool[offsets.Length]; var words = new bool[offsets.Length]; var ends = new bool[offsets.Length];
        NativeTextBreak.Fill(text, locale, offsets, lines, words, ends);
        Check(Enumerable.Range(1, lines.Length - 1).Where(i => lines[i]).SequenceEqual(expectedLines), "Line boundaries differ from the independent ICU 78.3 oracle for " + locale + ".");
        if (expectedWords is not null)
            Check(Enumerable.Range(1, ends.Length - 1).Where(i => ends[i]).SequenceEqual(expectedWords), "Word boundaries differ from the independent ICU 78.3 oracle for " + locale + ".");
    }
    private static int[] ScalarOffsets(string text)
    {
        var result = new List<int> { 0 }; var offset = 0;
        foreach (var scalar in text.EnumerateRunes()) { offset += scalar.Utf16SequenceLength; result.Add(offset); }
        return result.ToArray();
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
