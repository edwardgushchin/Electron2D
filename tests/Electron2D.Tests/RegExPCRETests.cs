using Electron2D;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

internal static class RegExPCRETests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static T Error<T>(Action action) where T : Exception { try { action(); } catch (T error) { return error; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run()
    {
        using var mixed = new RegEx("(?<named>a)(b)(?<last>c)"); using var capture = mixed.Search("abc")!;
        Check(capture.GetString(1) == "a" && capture.GetString(2) == "b" && capture.Names["last"] == 3 && mixed.GetNames().SequenceEqual(["last", "named"]), "Source-order captures and sorted native names");
        using var duplicate = new RegEx("(?:(?<item>a)|(?<item>b))"); using var matched = duplicate.Search("b")!;
        Check(duplicate.GroupCount == 2 && duplicate.GetNames().SequenceEqual(["item"]) && matched.Names["item"] == 2 && matched.GetString("item") == "b", "First participating duplicate name");
        using var recursive = new RegEx(@"(?<pair>\((?:[^()]++|(?&pair))*\))"); using var pair = recursive.Search("x(a(b)c)!"); Check(pair?.GetString() == "(a(b)c)", "Recursive PCRE2 subroutine executes");
        using var branch = new RegEx("(?|(a)|(b))"); using var branchMatch = branch.Search("b"); Check(branch.GroupCount == 1 && branchMatch?.GetString(1) == "b", "Branch-reset groups execute");
        using var lookbehind = new RegEx("(?<=a{1,3})b"); using var behind = lookbehind.Search("aaab"); Check(behind?.GetStart() == 3, "Bounded variable lookbehind executes");
        using var reset = new RegEx(@"a\Kb"); using var resetMatch = reset.Search("ab"); Check(resetMatch?.GetString() == "b" && resetMatch.GetStart() == 1, "Match-start reset executes");
        using var unicode = new RegEx("."); using var point = unicode.Search("😀x😀", 1, 2); Check(point?.GetString() == "x" && point.GetStart() == 1 && point.GetEnd() == 2, "Code-point regions map to original UTF-16 text");
        using var malformed = unicode.Search("\uD800x"); Check(malformed?.GetString() == "\uD800", "Non-UTF profile preserves unmatched UTF-16 units");
        using var strict = new RegEx("(*UTF)."); Error<ArgumentException>(() => strict.Search("\uD800x"));
        using var embedded = new RegEx("a\0b"); using var nul = embedded.Search("!a\0b!"); Check(nul?.GetString() == "a\0b", "Explicit lengths preserve embedded NUL");
        using var empty = new RegEx(""); var empties = empty.SearchAll("😀x"); Check(empties.Select(x => x.GetStart()).SequenceEqual([0, 1, 2]), "Empty matches advance by code point including final position"); foreach (var item in empties) item.Dispose();
        Check(empty.Sub("ab", "-", all: true) == "-a-b-", "Native global empty substitution");
        Check(mixed.Sub("abc", "${named}/${absent}/$$") == "a//$", "Unknown and unset replacements follow native grammar"); Check(mixed.Sub("abc", "$&") == "abc", "Native whole-match replacement alias"); Error<ArgumentException>(() => mixed.Sub("abc", "${"));
        Check(unicode.Sub("😀xSUFFIX", "$0+", true, 1, 2) == "😀x+SUFFIX", "Replacement preserves prefix and suffix in code-point regions");
        Error<ArgumentOutOfRangeException>(() => unicode.Sub("a", "x", offset: 2)); Check(unicode.Search("a", 2) is null, "Search beyond region has no match");
        using var budget = new RegEx("(*LIMIT_MATCH=10)(a+)+$"); Error<InvalidOperationException>(() => budget.Search(new string('a', 500) + "!")); using var reused = budget.Search("a"); Check(reused?.GetString() == "a", "Native budget failure does not invalidate program");
        var large = new string('x', 16_777_217); Error<ArgumentException>(() => unicode.Search(large)); Error<ArgumentException>(() => unicode.Sub("a", large)); large = string.Empty;
        using var expansion = new RegEx("a"); Error<InvalidOperationException>(() => expansion.Sub(new string('a', 1024), new string('x', 32768), all: true));
        Error<InvalidOperationException>(() => expansion.Sub(new string('a', 1024) + "SUFFIX", new string('x', 16384), all: true, end: 1024));
        Parallel.For(0, 4, worker => { for (var i = 0; i < 100; i++) { using var match = mixed.Search("abc"); Check(match?.GetString(2) == "b", "Serialized shared-owner native search"); } });
        Parallel.For(0, 4, worker => { using var expression = new RegEx(@"(*UCP)\w+"); for (var i = 0; i < 100; i++) { using var match = expression.Search("ёж42"); Check(match?.GetString() == "ёж42", "Independent concurrent native programs"); } });
        using (var capturesOnly = new RegEx(string.Concat(Enumerable.Repeat("(?=(.{1023}))", 64))))
        {
            var subject = new string('a', 1024);
            for (var i = 0; i < 16; i++) { using var warm = capturesOnly.Search(subject); }
            var before = GC.GetAllocatedBytesForCurrentThread(); using var compact = capturesOnly.Search(subject)!;
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(bytes < 32768 && compact.GroupCount == 64 && compact.GetString(64).Length == 1023, "Capture bounds do not eagerly duplicate text: " + bytes);
            Console.WriteLine("64 overlapping captures: " + bytes + " managed bytes before materializing text.");
        }
        using (var changing = new RegEx("a"))
            Parallel.Invoke(() => { for (var i = 0; i < 200; i++) { using var match = changing.Search("a"); Check(match?.GetString() == "a", "Program replacement never exposes a partial program"); } },
                () => { for (var i = 0; i < 200; i++) changing.Compile((i & 1) == 0 ? "a" : "(?<value>a)"); });
        var retiring = new RegEx("a"); using var entered = new ManualResetEventSlim();
        var searches = Task.Run(() =>
        {
            var completed = 0;
            while (true)
            {
                try { using var match = retiring.Search("a"); Check(match?.GetString() == "a", "Retirement preserves a completed native search"); completed++; entered.Set(); }
                catch (ObjectDisposedException) { return completed; }
            }
        });
        Check(entered.Wait(TimeSpan.FromSeconds(5)), "Concurrent search entered"); retiring.Dispose();
        Check(searches.Wait(TimeSpan.FromSeconds(5)) && searches.Result > 0, "Concurrent disposal retires the native owner safely");
        mixed.Compile("replacement"); mixed.Clear(); Check(mixed.Pattern == "replacement" && !mixed.IsValid && capture.GetString("named") == "a", "Clear retains text and captured values survive program retirement");
        var forgotten = Forget(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); Check(!forgotten.IsAlive, "Forgotten owner and its SafeHandle can retire");
        Console.WriteLine("PCRE2 dialect, Unicode, regions, budgets, concurrency and lifecycle passed.");
    }
    [MethodImpl(MethodImplOptions.NoInlining)] private static WeakReference Forget() => new(new RegEx("a"));
    private static string Read(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String) return value.GetString()!;
        var result = new StringBuilder(); foreach (var item in value.EnumerateArray()) { var cp = item.GetInt32(); if (cp <= 0xFFFF) result.Append((char)cp); else result.Append(char.ConvertFromUtf32(cp)); }
        return result.ToString();
    }
    internal static void Oracle(string path)
    {
        using var document = JsonDocument.Parse(System.IO.File.ReadAllText(path)); var count = 0;
        foreach (var test in document.RootElement.EnumerateArray())
        {
            var number = count++; var pattern = Read(test.GetProperty("pattern")); using var expression = new RegEx();
            if (test.TryGetProperty("compileError", out _)) { Error<ArgumentException>(() => expression.Compile(pattern)); continue; }
            expression.Compile(pattern); Check(expression.GroupCount == test.GetProperty("groups").GetInt32(), "Oracle group count " + number);
            Check(expression.GetNames().SequenceEqual(test.GetProperty("names").EnumerateArray().Select(Read)), "Oracle names " + number);
            var subject = Read(test.GetProperty("subject")); var offset = test.GetProperty("offset").GetInt32(); var end = test.GetProperty("end").GetInt32();
            if (test.TryGetProperty("matchError", out _)) Error<ArgumentException>(() => expression.SearchAll(subject, offset, end));
            else
            {
                var actual = expression.SearchAll(subject, offset, end);
                try
                {
                    var expected = test.GetProperty("matches"); Check(actual.Length == expected.GetArrayLength(), "Oracle match count " + number);
                    for (var i = 0; i < actual.Length; i++)
                    {
                        var captures = expected[i].GetProperty("captures"); Check(actual[i].GroupCount + 1 == captures.GetArrayLength(), "Oracle capture count " + number);
                        for (var j = 0; j < captures.GetArrayLength(); j++) Check(actual[i].GetStart(j) == captures[j][0].GetInt32() && actual[i].GetEnd(j) == captures[j][1].GetInt32() && actual[i].GetString(j) == Read(captures[j][2]), "Oracle capture " + number + "/" + j);
                        var names = expected[i].GetProperty("names"); Check(actual[i].Names.Count == names.EnumerateObject().Count(), "Oracle matched names " + number);
                        foreach (var name in names.EnumerateObject()) Check(actual[i].Names[name.Name] == name.Value.GetInt32(), "Oracle named capture " + number);
                    }
                }
                finally { foreach (var match in actual) match.Dispose(); }
            }
            var replacement = Read(test.GetProperty("replacement")); var all = test.GetProperty("all").GetBoolean();
            if (test.TryGetProperty("subError", out _)) Error<ArgumentException>(() => expression.Sub(subject, replacement, all, offset, end));
            else Check(expression.Sub(subject, replacement, all, offset, end) == Read(test.GetProperty("sub")), "Oracle substitution " + number);
        }
        Console.WriteLine("Independent PCRE2-32 oracle: " + count + " vectors, zero mismatches.");
    }
}
