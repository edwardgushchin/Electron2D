using System.Security.Cryptography;
using Electron2D;
using Path = System.IO.Path;

internal static class TextServerSecurityTests
{
    internal static void Run()
    {
        var s = TextServerManager.GetPrimaryInterface();
        Check(s.LoadSupportData("") && s.LoadSupportData("missing-file.dat"), "Embedded initialization ignores external paths");
        Check(s.GetSupportDataFilename() == "TextBreak78.dat" && s.GetSupportDataInfo().Contains("Unicode 17"), "Export identity");
        var first = s.GetSupportData(); var copy = s.GetSupportData();
        Check(first.Length == 4811936 && Convert.ToHexStringLower(SHA256.HashData(first)) == "632f80e9268e60542a9189d3575b53a4338a752ac8a4f1ebd89465ff73eca329", "Exact pinned data bytes");
        first[0] ^= 255; Check(!first.AsSpan().SequenceEqual(copy) && s.GetSupportData().AsSpan().SequenceEqual(copy), "Independent data snapshots");
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-text-security-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, s.GetSupportDataFilename());
            Check(s.SaveSupportData(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(copy), "Complete flushed export");
            File.WriteAllText(path, "old output"); Check(s.SaveSupportData(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(copy), "Truncated previous output");
            Check(!s.SaveSupportData(directory) && !s.SaveSupportData(Path.Combine(directory, "missing", "data.dat")) && !s.SaveSupportData(""), "I/O failure returns false");
        }
        finally { Directory.Delete(directory, true); }
        foreach (var locale in new[] { "my", "zh_CN", "ja-JP", "ko", "km_KH", "lo", "th" }) Check(s.IsLocaleUsingSupportData(locale), locale);
        foreach (var locale in new[] { "", "en", "zhang", "TH", "thailand" }) Check(!s.IsLocaleUsingSupportData(locale), locale);
        Check(s.IsConfusable("paypal", ["other", "раураl", "paypal"]) == 1, "Cyrillic/Latin skeleton, first match");
        Check(s.IsConfusable("m", ["rn"]) == 0 && s.IsConfusable("é", ["e\u0301"]) == 0, "Multicharacter skeleton and canonical decomposition");
        Check(s.IsConfusable("ABC", ["abc"]) == -1 && s.IsConfusable("a", []) == -1, "Case-sensitive skeleton and empty dictionary");
        Check(s.IsConfusable("", ["a", ""]) == 1 && s.IsConfusable("a\0b", ["a", "a\0b"]) == 1, "Empty and embedded-zero comparisons");
        Check(s.IsConfusable("x\ud800", ["x", "x\ud800"]) == 1 && s.IsConfusable("\U00010400", ["\U00010400"]) == 0, "Malformed and supplementary units");
        foreach (var name in new[] { "hello", "école", "日本語", "a日本語", "", "abc123" }) Check(!s.SpoofCheck(name), "Allowed name " + name);
        foreach (var name in new[] { "pаypal", "a\u0301\u0301", "abc1١", "a\0b", "x\ud800", "a b", "αБ" }) Check(s.SpoofCheck(name), "Suspicious name " + name);
        Check(s.IsValidIdentifier("αБ") && s.SpoofCheck("αБ"), "Syntax and security are separate");
        Parallel.For(0, 32, i => Check(s.SpoofCheck("pаypal") && s.IsConfusable("paypal", ["раураl"]) == 0 && s.LoadSupportData(""), "Concurrent serialized security/data use"));
        Reject<ArgumentNullException>(() => s.IsConfusable(null!, [])); Reject<ArgumentNullException>(() => s.IsConfusable("a", ["a", null!]));
        Reject<ArgumentNullException>(() => s.SpoofCheck(null!)); Reject<ArgumentNullException>(() => s.IsLocaleUsingSupportData(null!));
        Reject<ArgumentNullException>(() => s.LoadSupportData(null!)); Reject<ArgumentNullException>(() => s.SaveSupportData(null!));
        Reject<ArgumentException>(() => s.IsConfusable("", new string[1048577]));
        var large = new string('a', NativeTextBreak.MaximumStringUnits / 2 + 1);
        Reject<ArgumentException>(() => s.IsConfusable(large, [large]));
        Reject<ArgumentException>(() => s.SpoofCheck(new string('a', NativeTextBreak.MaximumStringUnits + 1)));
        // A confusable skeleton expands each U+FDFA to eighteen UTF-16 units.
        Reject<ArgumentException>(() => s.IsConfusable(new string('\ufdfa', NativeTextBreak.MaximumStringUnits / 18 + 1), []));
        for (var i = 0; i < 128; i++) { s.SpoofCheck("hello"); s.IsLocaleUsingSupportData("th-TH"); s.LoadSupportData(""); }
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) { s.SpoofCheck("hello"); s.IsLocaleUsingSupportData("th-TH"); s.LoadSupportData(""); }
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "128 warm checks/initializations allocate zero managed bytes");
        Console.WriteLine("TextServer security: Unicode 17 confusable/spoof checks, exact database export, embedded initialization, bounds, concurrency and 128 zero-byte cycles passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer security: " + message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
