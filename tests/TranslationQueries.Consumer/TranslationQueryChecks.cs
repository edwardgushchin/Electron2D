using System.Globalization;
using Electron2D;

internal static class TranslationQueryChecks
{
    // Expected memberships are fixed independently of the runtime matching helper.
    private static readonly string[] Locales = ["fr", "fr-CA", "sr-Cyrl-RS", "sr-Latn-ME", "he-IL", "id-ID", "zh-CN", "zh-Hant-TW", "fr-CA", "", "yi", "en-GB", "ro-MD"];
    private static readonly (string Locale, int[] Exact, int[] Related)[] Cases =
    [
        ("fr", [0], [0, 1, 8]), ("fr-CA", [1, 8], [0, 1, 8]),
        ("fr_CA", [], [0, 1, 8]), ("fr-ca", [], [0, 1, 8]), ("FR-CA", [], []),
        ("fr-BE", [], [0, 1, 8]), ("sr", [], [2, 3]), ("sr_RS@latin", [], [2, 3]),
        ("sr-Cyrl-RS", [2], [2, 3]), ("sr-Latn-ME", [3], [2, 3]),
        ("he-IL", [4], [4]), ("iw_IL", [], [4]), ("iw", [], [4]),
        ("id-ID", [5], [5]), ("in_ID", [], [5]), ("ji", [], []), ("yi", [10], [10]),
        ("zh", [], [6, 7]), ("zh-HK", [], [6, 7]), ("zh_Hans_CN", [], [6, 7]),
        ("zh-Hant-TW", [7], [6, 7]), ("en_UK", [], [11]), ("mo", [], []),
        ("", [9], [9]), ("unknown_Abcd_US", [], []), ("de-DE", [], [])
    ];

    internal static void Run()
    {
        var originalCulture = TranslationServer.Culture;
        var errors = new List<string>();
        try
        {
            TranslationServer.Clear();
            using var independent = new TranslationDomain();
            var registered = TranslationServer.GetOrAddDomain("query-checks");
            try
            {
                CheckDomain(independent, false, errors);
                CheckDomain(registered, false, errors);
                CheckDomain(TranslationServer.GetOrAddDomain(""), true, errors);
            }
            finally { TranslationServer.RemoveDomain("query-checks"); registered.Dispose(); }
            using var retired = new TranslationDomain();
            using var borrowed = new Translation { Locale = "fr" };
            retired.AddTranslation(borrowed);
            retired.Dispose();
            Check(!borrowed.IsDisposed && borrowed.Locale == "fr", "Domain disposal preserves borrowed catalogs.");
            Reject<ObjectDisposedException>(() => retired.FindTranslations("fr", true));
            Reject<ObjectDisposedException>(() => retired.HasTranslationForLocale("fr", false));
            CheckConcurrentRetirement();
        }
        finally { TranslationServer.Clear(); TranslationServer.Culture = originalCulture; }
        if (errors.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        Console.WriteLine($"Translation query matrix: {Cases.Length * 2 * 3} cases, independent memberships/order, snapshots, live locale edits, retirement and ranking passed.");
    }

    private static void CheckDomain(TranslationDomain domain, bool server, List<string> errors)
    {
        var catalogs = Locales.Select(locale => new Translation { Locale = locale }).ToArray();
        Translation[] Find(string locale, bool exact) => server ? TranslationServer.FindTranslations(locale, exact) : domain.FindTranslations(locale, exact);
        bool Has(string locale, bool exact) => server ? TranslationServer.HasTranslationForLocale(locale, exact) : domain.HasTranslationForLocale(locale, exact);
        try
        {
            foreach (var catalog in catalogs) { domain.AddTranslation(catalog); domain.AddTranslation(catalog); }
            TranslationServer.AddTranslation(CultureInfo.GetCultureInfo("de"), "", "direct-only", "direct");
            domain.Enabled = false; domain.LocaleOverride = "en";
            foreach (var row in Cases)
                foreach (var exact in new[] { true, false })
                {
                    var expected = (exact ? row.Exact : row.Related).Select(index => catalogs[index]).ToArray();
                    try
                    {
                        if (!Find(row.Locale, exact).SequenceEqual(expected)) errors.Add($"{server}/{row.Locale}/{exact}: incorrect ordered membership.");
                        if (Has(row.Locale, exact) != (expected.Length != 0)) errors.Add($"{server}/{row.Locale}/{exact}: incorrect availability.");
                    }
                    catch (ArgumentException error) { errors.Add($"{server}/{row.Locale}/{exact}: query rejected text: {error.GetType().Name}."); }
                }
            Reject<ArgumentNullException>(() => Find(null!, true));
            Reject<ArgumentNullException>(() => Has(null!, false));
            domain.Enabled = true; domain.LocaleOverride = "";

            var snapshot = Find("fr", false);
            snapshot[0] = catalogs[4];
            Check(ReferenceEquals(Find("fr", false)[0], catalogs[0]), "A caller cannot mutate the registration snapshot.");
            var retained = Find("fr-CA", true);
            var loaded = server ? TranslationServer.GetLoadedLocales() : [];
            if (server) Check(loaded.SequenceEqual(["fr", "fr-CA", "sr-Cyrl-RS", "sr-Latn-ME", "he-IL", "id-ID", "zh-CN", "zh-Hant-TW", "", "yi", "en-GB", "ro-MD"]), "Loaded locales use first registration order, deduplicate and exclude direct entries.");
            catalogs[1].Locale = "de-DE";
            Check(Find("fr-CA", true).SequenceEqual([catalogs[8]]) && Find("de-DE", true).SequenceEqual([catalogs[1]]) && Has("de-DE", true), "Locale mutation is visible without registration changes.");
            if (server)
            {
                Check(loaded[1] == "fr-CA" && TranslationServer.GetLoadedLocales()[1] == "de-DE", "Locale strings are snapshots.");
                loaded[0] = "altered";
                Check(TranslationServer.GetLoadedLocales()[0] == "fr", "Loaded locale arrays are independent.");
            }
            domain.RemoveTranslation(catalogs[8]);
            Check(!Has("fr-CA", true) && Find("fr-CA", true).Length == 0 && !catalogs[8].IsDisposed, "Removal only retires registration.");
            domain.AddTranslation(catalogs[8]);
            Check(ReferenceEquals(Find("fr", false)[^1], catalogs[8]), "Re-registration appends a catalog.");
            catalogs[8].Dispose();
            Check(!Has("fr-CA", true) && Find("fr-CA", true).Length == 0 && retained.Length == 2 && ReferenceEquals(retained[0], catalogs[1]) && retained[1].IsDisposed, "Old resource arrays remain live references after edits/removal/disposal.");
            if (server) Check(!TranslationServer.GetLoadedLocales().Contains("fr-CA"), "A retired final duplicate leaves loaded locales.");

            domain.Clear();
            Check(!catalogs[0].IsDisposed && Find("fr", false).Length == 0 && !Has("fr", false), "Clear preserves borrowed resources and empties both queries.");
            if (server) Check(TranslationServer.GetLoadedLocales().Length == 0, "Direct entries do not populate loaded locales.");

            // Registration order of query results must not replace message-score ranking.
            catalogs[0].AddMessage("rank", "parent"); catalogs[0].AddPluralMessage("rank-n", ["parent-one", "parent-many"]);
            catalogs[1].Locale = "fr-CA"; catalogs[1].AddMessage("rank", "regional"); catalogs[1].AddPluralMessage("rank-n", ["regional-one", "regional-many"]);
            catalogs[1].PluralSelector = count => count == 1 ? 0 : 1;
            domain.AddTranslation(catalogs[1]); domain.AddTranslation(catalogs[0]);
            TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-CA");
            Check(Find("fr", false).SequenceEqual([catalogs[1], catalogs[0]]) && domain.Translate("rank") == "regional" && domain.TranslatePlural("rank-n", "rank-ns", 2) == "regional-many", "Queries preserve registration order while messages preserve best-score ranking.");
            for (var i = 0; i < 128; i++) { _ = Has("iw_IL", false); _ = Has("fr-CA", false); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1024; i++) { _ = Has("iw_IL", false); _ = Has("fr-CA", false); }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            if (allocated != 0) errors.Add($"Prepared availability allocated {allocated} managed bytes per 1024 calls.");
        }
        finally
        {
            domain.Clear(); domain.Enabled = true; domain.LocaleOverride = "";
            foreach (var catalog in catalogs) catalog.Dispose();
        }
    }

    private static void CheckConcurrentRetirement()
    {
        using var domain = new TranslationDomain();
        var catalogs = Enumerable.Range(0, 64).Select(_ => new Translation { Locale = "he-IL" }).ToArray();
        foreach (var catalog in catalogs) domain.AddTranslation(catalog);
        using var started = new ManualResetEventSlim();
        var reader = Task.Run(() =>
        {
            started.Set();
            for (var i = 0; i < 2048; i++)
            {
                var found = domain.FindTranslations("iw_IL", false);
                Check(found.All(catalogs.Contains) && found.Distinct().Count() == found.Length, "Concurrent query retains only registered identities without duplicates.");
                _ = domain.HasTranslationForLocale("iw_IL", false);
            }
        });
        try
        {
            Check(started.Wait(TimeSpan.FromSeconds(5)), "Concurrent reader starts within its deadline.");
            foreach (var catalog in catalogs) catalog.Dispose();
            Check(reader.Wait(TimeSpan.FromSeconds(10)), "Concurrent reader finishes within its deadline.");
            Check(domain.FindTranslations("iw_IL", false).Length == 0 && !domain.HasTranslationForLocale("iw_IL", false), "Concurrent retirement drains both queries.");
        }
        finally { foreach (var catalog in catalogs) catalog.Dispose(); }
    }

    private sealed class Indicator : Entity
    {
        internal string Value = "";
        protected override void OnDraw()
        {
            Value = Atr("Choice");
            DrawRect(new(8, 40, 16, 12), Value == "French" ? Colors.Red : Value == "Hebrew" ? Colors.Blue : Colors.Black);
        }
    }

    internal static void RunNative(string backend, string? output)
    {
        var originalCulture = TranslationServer.Culture;
        var originalBackend = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        TranslationServer.Clear();
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        using var french = new Translation { Locale = "fr-CA" };
        using var hebrew = new Translation { Locale = "he-IL" };
        french.AddMessage("Choice", "French"); hebrew.AddMessage("Choice", "Hebrew");
        TranslationServer.AddTranslation(french); TranslationServer.AddTranslation(hebrew);
        var locales = TranslationServer.GetLoadedLocales();
        var menu = new OptionButton { Position = new(32, 4), Size = new(170, 28) };
        foreach (var locale in locales) menu.AddItem(TranslationServer.GetLocaleName(locale));
        var indicator = new Indicator();
        var window = new Window { Size = new(220, 80) };
        window.AddChild(indicator); window.AddChild(menu);
        var frames = 0;
        void Select(int index)
        {
            menu.Select(index);
            var locale = locales[menu.Selected];
            Check(TranslationServer.HasTranslationForLocale(locale, true) && TranslationServer.FindTranslations(locale, true).Length == 1, "The language menu selects an available exact catalog.");
            if (index == 1) Check(TranslationServer.HasTranslationForLocale("iw_IL", false) && TranslationServer.FindTranslations("iw_IL", false).SequenceEqual([hebrew]), "Alias availability and retrieval agree in the language consumer.");
            TranslationServer.Culture = CultureInfo.GetCultureInfo(locale);
            indicator.QueueRedraw();
        }
        Action? capture = null;
        window.Ready += _ =>
        {
            Select(0);
            RenderingServer.SetDefaultClearColor(Colors.Black);
            capture = () =>
            {
                using var image = window.GetTexture().GetImage() ?? throw new InvalidOperationException("No scene framebuffer.");
                var pixel = image.GetPixel(12, 44);
                Check(frames == 0 ? indicator.Value == "French" && pixel.R > .95f && pixel.B < .05f : indicator.Value == "Hebrew" && pixel.B > .95f && pixel.R < .05f, "Language selection reaches the expected native pixels.");
                if (output is not null) { Directory.CreateDirectory(output); image.SavePNG(System.IO.Path.Combine(output, $"{backend}-{frames}.png")); }
                if (++frames == 1) Select(1); else window.Tree!.Quit();
            };
            RenderingServer.FramePostDraw += capture;
        };
        try { Check(Engine.Run(window) == 0 && frames == 2 && window.IsDisposed && !RenderingServer.IsAvailable, "Native language consumer teardown."); }
        finally
        {
            if (capture is not null && RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= capture;
            TranslationServer.Clear(); TranslationServer.Culture = originalCulture;
            ProjectSettings.Set(ProjectSettings.RenderingMethod, originalBackend);
            if (!window.IsDisposed) window.Dispose();
        }
        Console.WriteLine($"Translation query language menu: {backend}, French/Hebrew pixels and teardown passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
