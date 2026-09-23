using Electron2D;

internal static class RegExTests
{
    internal static void Run()
    {
        using var expression = RegEx.CreateFromString(@"(?<name>[a-z]+)(?:-(\d+))?");
        Check(expression.IsValid && expression.Pattern.Length > 0 && expression.GroupCount == 2 && expression.GetNames().SequenceEqual(["name"]), "Compiled expression reports groups and names.");

        using var first = expression.Search("!abc-12 xyz", 1)!;
        Check(first.Subject == "!abc-12 xyz" && first.GetStart() == 1 && first.GetEnd() == 7 && first.GetString() == "abc-12", "Search retains original coordinates and subject.");
        Check(first.GetString("name") == "abc" && first.GetStart("name") == 1 && first.GetEnd("name") == 4 && first.Names.ContainsKey("name"), "Named groups expose text and bounds.");
        Check(first.GroupCount == 2 && first.GetString(99) == "" && first.GetStart("absent") == -1, "Missing groups return empty text or -1.");
        var strings = first.Strings;
        strings[0] = "changed";
        Check(first.GetString() == "abc-12", "Returned group strings are an independent copy.");

        var all = expression.SearchAll("abc-12 xyz", 0, 10);
        Check(all.Length == 2 && all[0].GetString() == "abc-12" && all[1].GetString() == "xyz" && all[1].GetStart(1) == -1,
            "SearchAll finds nonoverlapping matches and preserves unmatched groups.");
        foreach (var match in all) match.Dispose();

        using var anchored = RegEx.CreateFromString("^abc");
        using var anchoredMatch = anchored.Search("abcx", 0, 3);
        Check(anchored.Search("xabc", 1) is null && anchoredMatch?.GetString() == "abc" && anchoredMatch.Subject == "abcx",
            "An offset does not move the start anchor; end limits matching but retains the original subject.");
        Check(expression.Sub("abc-12 xyz!", "$0+", all: true, end: 10) == "abc-12+ xyz+!", "Replacement preserves the untouched suffix.");

        using var empty = RegEx.CreateFromString(@"(?=a)");
        var zeroWidth = empty.SearchAll("aa");
        Check(zeroWidth.Length == 2 && zeroWidth[0].GetStart() == 0 && zeroWidth[1].GetStart() == 1, "Zero-width matches advance.");
        foreach (var match in zeroWidth) match.Dispose();

        using var emptyResult = new RegExMatch();
        Check(emptyResult.Subject == "" && emptyResult.GroupCount == 0 && emptyResult.Strings.Length == 0 && emptyResult.GetStart() == -1,
            "An explicitly constructed result is empty.");

        Reject<ArgumentOutOfRangeException>(() => expression.Search("a", -1));
        Reject<ArgumentNullException>(() => first.GetString((string)null!));
        Reject<ArgumentException>(() => expression.Compile("("));
        Check(!expression.IsValid && expression.Pattern == "(", "Failed compilation clears the old compiled program but preserves attempted text.");
        Check(first.GetString("name") == "abc", "A match remains valid after its expression is replaced.");
        expression.Clear();
        Check(!expression.IsValid && expression.Pattern == "", "Clear resets the expression.");
        Reject<InvalidOperationException>(() => expression.Search("abc"));
        first.Dispose();
        Reject<ObjectDisposedException>(() => first.GetString());
        Console.WriteLine("RegEx managed search, replacement, group and lifetime checks passed.");
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
