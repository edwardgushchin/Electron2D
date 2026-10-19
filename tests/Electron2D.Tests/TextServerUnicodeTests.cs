using System.Globalization;
using Electron2D;

internal static class TextServerUnicodeTests
{
    internal static void Run()
    {
        var s = TextServerManager.GetPrimaryInterface();
        Check(s.NameToTag("weight") == 0x77676874 && s.TagToName(0x77676874) == "weight", "Variation tag names");
        Check(s.TagToName(s.NameToTag("italics")) == "italic", "Last registered canonical name for shared feature/axis tag");
        Check(s.NameToTag("ab") == 0x61622020 && s.NameToTag("abcdef") == 0x61626364 && s.NameToTag("ab\0cd") == 0x61622020, "Literal truncation/padding/zero");
        Check(s.NameToTag("abcustom_cd") == s.NameToTag("abcd") && s.NameToTag("é") == 0x20202020, "Custom marker and non-ASCII literal policy");
        Check(s.TagToName(0) == "custom_" && s.TagToName(0xffffffff) == "custom_ÿÿÿÿ" && s.TagToName(0x61006263) == "custom_a", "All tag bytes and zero terminator");
        for (var i = 1; i <= 99; i++) { var name = "character_variant_" + i.ToString("D2", CultureInfo.InvariantCulture); Check(s.TagToName(s.NameToTag(name)) == name, name); }
        for (var i = 1; i <= 20; i++) { var name = "stylistic_set_" + i.ToString("D2", CultureInfo.InvariantCulture); Check(s.TagToName(s.NameToTag(name)) == name, name); }
        Check(s.TagToName(s.NameToTag("ss21")) == "custom_ss21", "Numbered registration limit");
        foreach (var locale in new[] { "ar", "ar_EG", "he-IL", "dv", "fa_IR", "ff_Latn", "ku", "ur" }) Check(s.IsLocaleRightToLeft(locale), locale);
        foreach (var locale in new[] { "", "en", "arbitrary", "AR_EG", "yi" }) Check(!s.IsLocaleRightToLeft(locale), locale);
        Check(s.StringToLower("ΟΣ", "el") == "ος" && s.StringToLower("Iİ", "tr") == "ıi", "Context-sensitive lower case");
        Check(s.StringToTitle("HELLO WORLD", "en") == "Hello World" && s.StringToTitle("ijSSEL", "nl") == "IJssel", "Titlecase lowers word tails and expands Dutch IJ");
        Check(s.StringToUpper("Straße ﬃ", "en") == "STRASSE FFI" && s.StringToUpper("iı", "tr") == "İI", "Shared full uppercase");
        Check(s.StringToLower("\u1c89", "en") == "\u1c8a" && s.StringToUpper("\u1c8a", "en") == "\u1c89", "Pinned Unicode case data beyond old runtime tables");
        Check(s.StripDiacritics("é ﬃ ①") == "e ffi 1" && s.StripDiacritics("\u0301") == "", "NFKD and combining-class removal");
        Check(s.StripDiacritics("e\u034f\u0301") == "e\u034f", "Zero-class combining marks remain");
        Check(s.StringToLower("A\0B", "en") == "a\0b" && s.StringToLower("A\ud800B", "en") == "a\ud800b", "Explicit length and malformed-unit preservation");
        Check(s.StripDiacritics("É\0Ó\udfff") == "E\0O\udfff", "Normalization preserves zeroes and unpaired units");
        Check(s.StringToLower("", "tr") == "" && s.StringToTitle("", "nl") == "" && s.StripDiacritics("") == "", "Empty transforms");
        var culture = TranslationServer.Culture; var fallback = TranslationServer.FallbackCulture;
        try
        {
            TranslationServer.Culture = CultureInfo.GetCultureInfo("tr-TR"); TranslationServer.FallbackCulture = CultureInfo.GetCultureInfo("tr-TR");
            Check(s.StringToLower("I") == s.StringToLower("I", TranslationServer.GetToolLocale()) && s.StringToUpper("i") == s.StringToUpper("i", TranslationServer.GetToolLocale()), "Empty language captures tool locale");
        }
        finally { TranslationServer.Culture = culture; TranslationServer.FallbackCulture = fallback; }
        foreach (var text in new[] { "abc", "_a1", "é", "αБ", "ب\u200cا", "क्\u200cक", "क्\u200dक", "क्\u200d", "क\u093c्\u200cक" }) Check(s.IsValidIdentifier(text), "Valid identifier " + text);
        foreach (var text in new[] { "", "1abc", "e\u0301", "a b", "a+b", "ب\u200c", "क्\u200c", "a\u200db", "\U00010300", "a\0b", "a\ud800b", "क्\u200d\u093e", "\u037a" }) Check(!s.IsValidIdentifier(text), "Invalid identifier " + text);
        foreach (var scalar in new[] { (int)'A', 'é', 'א', 'क', 0x10400 }) Check(s.IsValidLetter(scalar), "Letter " + scalar);
        foreach (var scalar in new[] { -1, 0xd800, 0x110000, (int)'1', 0x216b, 0x0301, 0x1f600 }) Check(!s.IsValidLetter(scalar), "Nonletter " + scalar);
        Equal(s.StringGetCharacterBreaks("Test ❤️‍🔥 Test"), [1, 2, 3, 4, 5, 9, 10, 11, 12, 13, 14], "Composite scalar ends");
        Equal(s.StringGetCharacterBreaks("a\ud800b"), [1, 2, 3], "Malformed units retain scalar positions");
        Equal(s.StringGetCharacterBreaks(""), [], "Empty character boundaries");
        var snapshot = s.StringGetCharacterBreaks("abc"); snapshot[0] = 99; Equal(s.StringGetCharacterBreaks("abc"), [1, 2, 3], "Independent character snapshot");
        const string words = "The Solar Engine, 4";
        Equal(s.StringGetWordBreaks(words), [0, 3, 4, 9, 10, 16, 18, 19], "Word ranges");
        Equal(s.StringGetWordBreaks(words, "en", 5), [0, 3, 4, 9, 10, 15, 15, 19], "Five-scalar wrapping");
        Equal(s.StringGetWordBreaks(words, "en", 10), [0, 9, 10, 19], "Ten-scalar wrapping");
        Equal(s.StringGetWordBreaks("a\r\nb\nc", "en", 2), [0, 1, 3, 4, 5, 6], "Hard separators never leak into line ranges");
        Equal(s.StringGetWordBreaks("a\nb"), [0, 1, 2, 3], "Single-letter words after hard breaks");
        Equal(s.StringGetWordBreaks("日本語a", "ja"), [0, 3, 3, 4], "Penultimate dictionary boundary remains");
        Equal(s.StringGetWordBreaks("😀_a\u200bb"), [0, 1, 2, 3, 4, 5], "Supplementary scalar offsets, underscore and zero-width space");
        Equal(s.StringGetWordBreaks("  abc", "en", 1), [2, 3, 3, 4, 4, 5], "Leading-space wrapping makes progress");
        Equal(s.StringGetWordBreaks("  abc  ", "en", int.MaxValue), [0, 7], "Initial/final spaces in an unwrapped line");
        Equal(s.StringGetWordBreaks("a b", "en", int.MinValue), [0, 1, 2, 3], "Nonpositive width selects words");
        Equal(s.StringGetWordBreaks("\n\r\n", "en", 1), [], "No empty separator-only chunks");
        var wordsSnapshot = s.StringGetWordBreaks(words); wordsSnapshot[0] = 99; Check(s.StringGetWordBreaks(words)[0] == 0, "Independent word snapshot");
        Parallel.For(0, 32, i => Check(s.StringToLower("Iİ", "tr") == "ıi" && s.IsValidIdentifier("नमस्ते") && s.StringGetWordBreaks("a b").Length == 4, "Concurrent Unicode operation"));
        Reject<ArgumentNullException>(() => s.NameToTag(null!)); Reject<ArgumentNullException>(() => s.IsLocaleRightToLeft(null!)); Reject<ArgumentNullException>(() => s.IsValidIdentifier(null!));
        Reject<ArgumentNullException>(() => s.StringToUpper("", null!)); Reject<ArgumentNullException>(() => s.StringToLower(null!)); Reject<ArgumentNullException>(() => s.StringToTitle("", null!));
        Reject<ArgumentNullException>(() => s.StringGetCharacterBreaks("", null!)); Reject<ArgumentNullException>(() => s.StringGetWordBreaks(null!)); Reject<ArgumentNullException>(() => s.StripDiacritics(null!));
        Reject<ArgumentException>(() => s.StringToLower(new string('a', NativeTextBreak.MaximumStringUnits + 1)));
        Reject<ArgumentException>(() => s.StripDiacritics(new string('\ufdfa', NativeTextBreak.MaximumStringUnits / 18 + 1)));
        for (var i = 0; i < 128; i++) Values(s);
        var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) Values(s);
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "128 prepared scalar/known-name cycles allocate zero owner bytes");
        Console.WriteLine("TextServer Unicode: locale/full case/normalization, identifier contexts, scalar word/composite ranges, tags, concurrency, bounds and 128 zero-byte value cycles passed.");
    }
    private static void Values(TextServer s) { s.IsValidLetter('é'); s.IsValidIdentifier("identifier_1"); s.IsLocaleRightToLeft("ar_EG"); s.NameToTag("weight"); s.TagToName(0x77676874); }
    private static void Equal(int[] actual, int[] expected, string message) => Check(actual.AsSpan().SequenceEqual(expected), message + ": [" + string.Join(',', actual) + "]");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer Unicode: " + message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
