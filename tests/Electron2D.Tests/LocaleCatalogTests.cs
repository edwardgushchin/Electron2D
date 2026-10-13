using System.Globalization;
using System.Diagnostics;
using Electron2D;
using IOPath = System.IO.Path;

internal static partial class LocaleCatalogTests
{
    internal static void Run()
    {
        Check(TranslationServer.GetLanguageName("ru") == "Russian" && TranslationServer.GetScriptName("Cyrl") == "Cyrillic" && TranslationServer.GetCountryName("US") == "United States of America", "English catalog labels.");
        foreach (var getter in new Func<string, string>[] { TranslationServer.GetLanguageName, TranslationServer.GetScriptName, TranslationServer.GetCountryName }) { Check(getter("Unknown") == "Unknown", "Unknown code remains literal."); Reject<ArgumentNullException>(() => getter(null!)); }
        Check(TranslationServer.StandardizeLocale("zh-HK", true) == "zh_Hant_HK" && TranslationServer.StandardizeLocale("zh", true) == "zh_Hans_CN", "Ambiguous script/country defaults.");
        Check(TranslationServer.StandardizeLocale("sr_RS@latin") == "sr_Latn_RS" && TranslationServer.StandardizeLocale("iw_US") == "he_US", "Script modifier and language alias.");
        Check(TranslationServer.StandardizeLocale("en_us") == "en" && TranslationServer.StandardizeLocale("") == "", "Pinned casing and empty input.");
        Check(TranslationServer.GetLocaleName("sr-Latn-RS") == "Serbian (Latin), Serbia", "User-facing locale label.");
        Check(TranslationServer.CompareLocales("zh", "zh-CN") == 10 && TranslationServer.CompareLocales("sr", "sr-ME") == 4, "Default script participates in catalog scores.");
        var languages = TranslationServer.GetAllLanguages(); languages[0] = "altered"; Check(TranslationServer.GetAllLanguages()[0] != "altered", "Catalog arrays are copied.");
        Reject<ArgumentNullException>(() => TranslationServer.StandardizeLocale(null!)); Reject<ArgumentNullException>(() => TranslationServer.GetLocaleName(null!)); Reject<ArgumentNullException>(() => TranslationServer.GetPluralRules(null!));
        Check(TranslationServer.GetPluralRules("unknown") == "" && TranslationServer.GetPluralRules("pt-BR").Contains("n > 1", StringComparison.Ordinal), "Unknown rules and regional override.");
        using var russian = new Translation { Locale = "ru" }; russian.AddMessage("greeting", "hello", "context"); russian.AddPluralMessage("apple", ["one", "few", "many"]);
        foreach (var (n, value) in new (long, string)[] { (0, "many"), (1, "one"), (2, "few"), (5, "many"), (11, "many"), (21, "one"), (22, "few"), (long.MaxValue, "many") }) Check(russian.GetPluralMessage("apple", "apples", n) == value, "Russian integer forms.");
        Check(russian.GetPluralMessage("apple", "apples", -1) == "", "Negative source count retains missing-form policy.");
        using var copied = (Translation)russian.Duplicate(); copied.Locale = "en"; Check(copied.PluralSelector is null && copied.GetPluralMessage("apple", "apples", 2) == "few", "Copy keeps automatic selector bound to new locale.");
        russian.PluralSelector = _ => 1; Check(russian.GetPluralMessage("apple", "apples", 1) == "few", "Caller selector wins."); russian.PluralSelector = null;
        for (var i = 0; i < 128; i++) _ = russian.GetPluralMessage("apple", "apples", 22);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) _ = russian.GetPluralMessage("apple", "apples", 22); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed automatic lookup allocates no managed bytes.");
        var stored = (PropertyDescriptor<Translation, string[]>)russian.GetPropertyList().Single(property => property.Name == "Messages");
        foreach (var malformed in new string[][] { ["context"], ["", "item", "0"], ["", "item", "2", "one"], ["", "item", "1", null!], ["", "item", "1", "one", "", "item", "1", "other"] })
            Reject<InvalidDataException>(() => stored.SetValue(russian, malformed));
        Check(russian.GetPluralMessage("apple", "apples", 22) == "few", "Invalid stored records preserve prior catalog.");
        var path = IOPath.Combine(IOPath.GetTempPath(), "locale-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(russian, path); using var restored = ResourceLoader.Load<Translation>(path, ResourceLoader.CacheMode.Ignore); Check(restored.PluralSelector is null && restored.GetMessage("greeting", "context") == "hello" && restored.GetPluralMessage("apple", "apples", 22) == "few", "Persisted catalog restores automatic rules.");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
            if (IOPath.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(LocaleCatalogTests).Assembly.Location);
            start.ArgumentList.Add("--locale-archive-child"); start.ArgumentList.Add(path);
            using var child = Process.Start(start)!; if (!child.WaitForExit(15000)) { child.Kill(true); throw new InvalidOperationException("Fresh locale reader timed out."); }
            Check(child.ExitCode == 0, "Fresh process restores resource catalog and automatic plural selection.");
            var previous = File.ReadAllBytes(path); russian.PluralSelector = _ => 0;
            Reject<AggregateException>(() => ResourceSaver.Save(russian, path)); Check(File.ReadAllBytes(path).SequenceEqual(previous), "Unserializable selector cannot replace a valid catalog."); russian.PluralSelector = null;
        }
        finally { File.Delete(path); }
        using var optimized = new OptimizedTranslation(); Check(optimized.Generate(russian) && optimized.GetPluralMessage("apple", "apples", 22) == "one", "Optimized sibling preserves first-form policy.");
        Console.WriteLine("Locale labels/defaults/aliases, scoring, automatic plural copy/archive/override and 1024 zero-byte lookups passed.");
    }
    internal static void ReadArchive(string path)
    {
        using var catalog = ResourceLoader.Load<Translation>(path, ResourceLoader.CacheMode.Ignore);
        Check(catalog.PluralSelector is null && catalog.GetMessage("greeting", "context") == "hello" && catalog.GetPluralMessage("apple", "apples", 22) == "few", "Fresh archive data and rules.");
    }
    internal static void Oracle(string file)
    {
        var catalogs = new Dictionary<string, Translation>(StringComparer.Ordinal); var tables = new Dictionary<string, List<string>> { ["L"] = [], ["S"] = [], ["C"] = [] }; var cases = 0; var normalizations = 0;
        try
        {
            foreach (var line in File.ReadLines(file))
            {
                var row = line.Split('\t');
                if (row[0] == "N") { Check(TranslationServer.StandardizeLocale(row[1], row[2] == "1") == row[3], $"Independent normalization {row[1]}, defaults={row[2]}."); normalizations++; continue; }
                if (row[0] != "P") { var actual = row[0] == "L" ? TranslationServer.GetLanguageName(row[1]) : row[0] == "S" ? TranslationServer.GetScriptName(row[1]) : TranslationServer.GetCountryName(row[1]); Check(actual == row[2], "Independent table label."); tables[row[0]].Add(row[1]); continue; }
                if (!catalogs.TryGetValue(row[1], out var catalog)) { catalog = new Translation { Locale = row[1] }; catalog.AddPluralMessage("item", Enumerable.Range(0, int.Parse(row[4], CultureInfo.InvariantCulture)).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray()); catalogs.Add(row[1], catalog); }
                Check(catalog.GetPluralMessage("item", "items", long.Parse(row[2], CultureInfo.InvariantCulture)) == row[3], $"Independent rule {row[1]}, n={row[2]}."); cases++;
            }
            Check(TranslationServer.GetAllLanguages().SequenceEqual(tables["L"]) && TranslationServer.GetAllScripts().SequenceEqual(tables["S"]) && TranslationServer.GetAllCountries().SequenceEqual(tables["C"]), "Complete ordered catalog census.");
            Console.WriteLine($"Independent locale tables, {normalizations} normalization cases and {cases} plural cases across {catalogs.Count} locale keys passed.");
        }
        finally { foreach (var catalog in catalogs.Values) catalog.Dispose(); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
