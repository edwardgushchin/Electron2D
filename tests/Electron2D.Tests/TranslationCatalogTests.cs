using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Electron2D;
using IOPath = System.IO.Path;

internal static class TranslationCatalogTests
{
    internal const string Header = "Language: ru\nContent-Type: text/plain; charset=UTF-8\nPlural-Forms: nplurals=3; plural=n==1?2:n==2?0:1;\n";
    internal static string PO => "msgid \"\"\nmsgstr \"\"\n" + Quote(Header) + "\n\nmsgctxt \"menu\"\nmsgid \"Play\"\nmsgstr \"Играть\"\n\nmsgid \"apple\"\nmsgid_plural \"apples\"\nmsgstr[0] \"few\"\nmsgstr[1] \"many\"\nmsgstr[2] \"one\"\n";
    internal static void Run()
    {
        var directory = IOPath.Combine(IOPath.GetTempPath(), "e2d-gettext-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            var po = IOPath.Combine(directory, "catalog.po"); var mo = IOPath.Combine(directory, "catalog.mo"); var archive = IOPath.Combine(directory, "catalog.e2dres");
            File.WriteAllText(po, PO + "\n#, fuzzy\nmsgid \"fuzzy\"\nmsgstr \"skip\"\n\n#~ msgid \"obsolete\"\n#~ msgstr \"skip\"\n\nmsgid \"lines\"\nmsgstr \"first\\n\"\n\"second\\t\\\"quoted\\\"\\\\\"\n", new UTF8Encoding(false));
            using var catalog = ResourceLoader.Load<Translation>(po); Verify(catalog);
            Check(catalog.GetMessage("fuzzy") == "" && catalog.GetMessage("obsolete") == "" && catalog.GetMessage("lines") == "first\nsecond\t\"quoted\"\\", "PO comments and escaped continuation.");
            Check(ResourceLoader.GetRecognizedExtensionsForType<Translation>().Contains("po") && ResourceLoader.GetClassesUsed(po).Contains(typeof(Translation)), "Catalog loader discovery.");
            Check(ReferenceEquals(catalog, ResourceLoader.Load<Translation>(po)), "Reuse cache retains identity.");
            using (var copy = (Translation)catalog.Duplicate()) { Verify(copy); copy.PluralRulesOverride = ""; Check(copy.GetPluralMessage("apple", "apples", 1) == "few", "Clearing imported rule restores locale rule independently."); }
            catalog.PluralSelector = _ => 1; Check(catalog.GetPluralMessage("apple", "apples", 1) == "many", "Explicit caller selector wins."); catalog.PluralSelector = null;
            ResourceSaver.Save(catalog, archive); using (var restored = ResourceLoader.Load<Translation>(archive, ResourceLoader.CacheMode.Ignore)) Verify(restored);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
            if (IOPath.GetFileNameWithoutExtension(Environment.ProcessPath!).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(TranslationCatalogTests).Assembly.Location);
            start.ArgumentList.Add("--gettext-child"); start.ArgumentList.Add(archive);
            using (var child = Process.Start(start)!) { if (!child.WaitForExit(15000)) { child.Kill(true); throw new InvalidOperationException("Catalog child timeout."); } Check(child.ExitCode == 0, "Fresh process imported rule persistence."); }
            for (var endian = 0; endian < 2; endian++)
            {
                File.WriteAllBytes(mo, MO(endian == 1)); using var binary = ResourceLoader.Load<Translation>(mo, ResourceLoader.CacheMode.Ignore); Verify(binary);
            }
            ResourceLoader.LoadThreadedRequest<Translation>(mo, cacheMode: ResourceLoader.CacheMode.Ignore);
            using (var threaded = ResourceLoader.LoadThreadedGet<Translation>(mo)) Verify(threaded);
            for (var i = 0; i < 128; i++) catalog.GetPluralMessage("apple", "apples", i);
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) catalog.GetPluralMessage("apple", "apples", i);
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Imported warm plural lookup allocates zero bytes.");
            Rules();
            var valid = File.ReadAllBytes(mo);
            var revision = (byte[])valid.Clone(); BinaryPrimitives.WriteUInt32BigEndian(revision.AsSpan(4, 4), 0x00010000); File.WriteAllBytes(mo, revision); using (var majorOne = ResourceLoader.Load<Translation>(mo, ResourceLoader.CacheMode.Ignore)) Verify(majorOne);
            var terminated = (byte[])valid.Clone(); terminated[^1] = 1; File.WriteAllBytes(mo, terminated); Reject(() => ResourceLoader.Load<Translation>(mo, ResourceLoader.CacheMode.Ignore));
            File.WriteAllBytes(mo, valid[..27]); Reject(() => ResourceLoader.Load<Translation>(mo, ResourceLoader.CacheMode.Ignore));
            foreach (var offset in new[] { 0, 4, 8, 12, 16, 20, 28, 32 })
            {
                var bad = (byte[])valid.Clone(); BinaryPrimitives.WriteUInt32BigEndian(bad.AsSpan(offset, 4), uint.MaxValue); File.WriteAllBytes(mo, bad); Reject(() => ResourceLoader.Load<Translation>(mo, ResourceLoader.CacheMode.Ignore));
            }
            var malformed = new[] { "", "msgid \"unfinished\"\n", PO.Replace("msgstr[1]", "msgstr[8]"), PO.Replace("nplurals=3", "nplurals=64"), PO.Replace("plural=n==1?2:n==2?0:1", "plural=broken"), PO + "\nmsgctxt \"menu\"\nmsgid \"Play\"\nmsgstr \"duplicate\"\n", PO.Replace("charset=UTF-8", "charset=ISO-8859-1"), PO.Replace("msgstr \"Играть\"", "msgstr \"bad\\q\"") };
            foreach (var bad in malformed) { File.WriteAllText(po, bad); Reject(() => ResourceLoader.Load<Translation>(po, ResourceLoader.CacheMode.Ignore)); }
            Reject(() => ResourceLoader.Load<Translation>(po, ResourceLoader.CacheMode.Replace));
            Check(catalog.GetMessage("Play", "menu") == "Играть", "Failed replacement import preserves cached catalog.");
            File.WriteAllText(po, PO.Replace("Играть", "Старт"));
            Check(ReferenceEquals(catalog, ResourceLoader.Load<Translation>(po, ResourceLoader.CacheMode.Replace)) && catalog.GetMessage("Play", "menu") == "Старт", "Successful replacement updates the retained catalog identity.");
            File.WriteAllBytes(po, [0xff, 0xfe]); Reject(() => ResourceLoader.Load<Translation>(po, ResourceLoader.CacheMode.Ignore));
            using (var large = File.Create(po)) large.SetLength(16 * 1024 * 1024 + 1); Reject(() => ResourceLoader.Load<Translation>(po, ResourceLoader.CacheMode.Ignore));
            Console.WriteLine("PO/MO both byte orders, discovery/cache/threaded loads, copied and fresh-process archive rules, malformed input and 1024 zero-byte lookups passed.");
        }
        finally { Directory.Delete(directory, true); }
    }
    internal static void Verify(Translation catalog)
    {
        Check(catalog.Locale == "ru" && catalog.GetMessage("Play", "menu") == "Играть", "Context and header locale.");
        Check(catalog.PluralSelector is null && catalog.PluralRulesOverride.Contains("n==1?2", StringComparison.Ordinal) && catalog.GetPluralMessage("apple", "apples", 1) == "one" && catalog.GetPluralMessage("apple", "apples", 2) == "few" && catalog.GetPluralMessage("apple", "apples", 5) == "many", "Imported expression overrides automatic Russian form order.");
    }
    internal static void ReadArchive(string path) { using var catalog = ResourceLoader.Load<Translation>(path, ResourceLoader.CacheMode.Ignore); Verify(catalog); }
    private static void Rules()
    {
        using var catalog = new Translation(); catalog.AddPluralMessage("x", ["0", "1", "2", "3"]);
        foreach (var pair in new (string Rule, long N, string Expected)[] { ("nplurals=4; plural=1+2*3==7?3:0;", 1, "3"), ("nplurals=4; plural=n==0?0:12/n%4;", 0, "0"), ("nplurals=4; plural=!(n>=2)&&n<=1 || n==9;", 9, "1"), ("nplurals=4; plural=n!=1 && (n<5 || n>10);", 7, "0"), ("nplurals=4; plural=n==0 || 1/n;", 0, "1"), ("nplurals=4; plural=n!=0 && 1/n;", 0, "0"), ("nplurals=4; plural=n-2;", 3, "1"), ("nplurals=4; plural=n==1?0:n==2?1:2;", long.MaxValue, "2") })
        { catalog.PluralRulesOverride = pair.Rule; Check(catalog.GetPluralMessage("x", "xs", pair.N) == pair.Expected, "Plural operator semantics."); }
        catalog.PluralRulesOverride = "nplurals=4; plural=4;"; Reject(() => catalog.GetPluralMessage("x", "xs", 1));
        catalog.PluralRulesOverride = "nplurals=4; plural=1/0;"; Reject(() => catalog.GetPluralMessage("x", "xs", 1));
        foreach (var bad in new[] { "bad", new string('x', 4097), "nplurals=4; plural=" + string.Join("+", Enumerable.Repeat("1", 130)) + ";", "nplurals=0; plural=0;", "nplurals=4; plural=-1;", "nplurals=4; plural=" + new string('(', 65) + "n" + new string(')', 65) + ";" })
        { catalog.PluralRulesOverride = bad; Check(catalog.PluralRulesOverride == bad && catalog.GetPluralMessage("x", "xs", 1) == "0", "Invalid override retained with automatic fallback."); }
        var expressionCases = 0;
        foreach (var language in TranslationServer.GetAllLanguages())
        {
            var definition = TranslationServer.GetPluralRules(language); if (definition.Length == 0) continue;
            var countEnd = definition.IndexOf(';'); var forms = int.Parse(definition.AsSpan(definition.IndexOf('=') + 1, countEnd - definition.IndexOf('=') - 1), CultureInfo.InvariantCulture);
            using var automatic = new Translation { Locale = language }; automatic.AddPluralMessage("x", Enumerable.Range(0, forms).Select(index => index.ToString(CultureInfo.InvariantCulture)).ToArray());
            using var expression = new Translation { Locale = language, PluralRulesOverride = definition }; expression.AddPluralMessage("x", Enumerable.Range(0, forms).Select(index => index.ToString(CultureInfo.InvariantCulture)).ToArray());
            for (long n = 0; n < 256; n++) { Check(automatic.GetPluralMessage("x", "xs", n) == expression.GetPluralMessage("x", "xs", n), "Prepared expression matches fixed locale arithmetic."); expressionCases++; }
            foreach (var n in new[] { 1_000_000L, 1_000_001L, long.MaxValue }) { Check(automatic.GetPluralMessage("x", "xs", n) == expression.GetPluralMessage("x", "xs", n), "Prepared expression boundary matches fixed locale arithmetic."); expressionCases++; }
        }
        Console.WriteLine(expressionCases + " locale-expression comparison cases passed.");
        using var optimized = new OptimizedTranslation(); Check(optimized.Generate(catalog) && optimized.GetPluralMessage("x", "xs", 22) == "0", "Optimized sibling retains first-form contract.");
    }
    internal static void VerifyFiles(string directory)
    {
        foreach (var path in Directory.GetFiles(directory, "*.mo")) { using var catalog = ResourceLoader.Load<Translation>(path, ResourceLoader.CacheMode.Ignore); Verify(catalog); }
        Console.WriteLine("Independently generated msgfmt catalogs passed.");
    }
    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
    internal static byte[] MO(bool bigEndian)
    {
        var ids = new[] { "", "apple\0apples", "menu\u0004Play" }; var translated = new[] { Header, "few\0many\0one", "Играть" };
        var bytes = new byte[28 + ids.Length * 16 + ids.Concat(translated).Sum(value => Encoding.UTF8.GetByteCount(value) + 1)];
        void Word(int offset, uint value) { if (bigEndian) BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(offset, 4), value); else BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), value); }
        Word(0, 0x950412de); Word(8, 3); Word(12, 28); Word(16, 52); var position = 76;
        for (var table = 0; table < 2; table++) for (var i = 0; i < 3; i++)
            { var value = Encoding.UTF8.GetBytes((table == 0 ? ids : translated)[i]); Word(28 + table * 24 + i * 8, (uint)value.Length); Word(32 + table * 24 + i * 8, (uint)position); value.CopyTo(bytes, position); position += value.Length + 1; }
        return bytes;
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject(Action action) { try { action(); } catch (Exception error) when (error is InvalidDataException or InvalidOperationException or AggregateException or NotSupportedException or DecoderFallbackException) { return; } throw new InvalidOperationException("Malformed catalog or rule was accepted."); }
}
