using System.Globalization;
using Electron2D;
using IOPath = System.IO.Path;

internal static partial class TranslationQueryChecks
{
    private sealed class CatalogOverrides : Translation
    {
        internal Func<string, string, string?>? Singular;
        internal Func<string, string, long, string, string?>? Plural;

        protected override string? OnGetMessage(string source, string context) => Singular?.Invoke(source, context);
        protected override string? OnGetPluralMessage(string source, string plural, long count, string context) =>
            Plural?.Invoke(source, plural, count, context);
    }

    private static void CheckCatalog()
    {
        using var catalog = new CatalogOverrides();
        Check(catalog.Locale == "en", "A new resource starts in English.");
        catalog.Locale = "en_us";
        Check(catalog.Locale == "en-US", "Catalog locale uses managed normalization.");
        catalog.Locale = "";
        Check(catalog.Locale == "", "The invariant locale is accepted.");
        catalog.Locale = "fr_ca";
        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-CA");
        using var lower = new Translation { Locale = "fr-CA" };
        lower.AddPluralMessage("box", ["lower-one", "lower-many"], "menu");
        TranslationServer.AddTranslation(lower);
        TranslationServer.AddTranslation(catalog);
        var forms = new[] { "one", "many" };
        try
        {
            catalog.AddPluralMessage("box", forms, "menu");
            forms[1] = "caller edit";
            catalog.AddMessage("box", "other", "Menu");
            catalog.AddMessage("", "empty source");
            catalog.AddMessage("x\u0004y", "literal EOT");
            Check(catalog.GetMessage("box", "menu") == "one" && catalog.GetMessage("box", "Menu") == "other" &&
                catalog.GetMessage("box") == "" && catalog.GetMessage("Box", "menu") == "" &&
                catalog.GetMessage("") == "empty source" && catalog.GetMessage("x\u0004y") == "literal EOT",
                "Keys preserve ordinal source/context identity including empty source and literal EOT.");
            Check(catalog.GetPluralMessage("box", "unused", long.MaxValue, "menu") == "many" &&
                TranslationServer.TranslatePlural("", "box", "boxes", 2, "menu") == "many", "Copied forms reach local and live server lookup.");
            catalog.Singular = (source, context) => (source, context) == ("box", "menu") ? "" : null;
            catalog.Plural = (source, plural, count, context) => (source, plural, count, context) switch
            {
                ("box", "boxes", long.MinValue, "menu") => "negative override",
                ("box", "boxes", long.MaxValue, "menu") => "positive override",
                ("box", "boxes", 2, "menu") => "",
                _ => null
            };
            Check(catalog.GetMessage("box", "menu") == "" && TranslationServer.Translate("", "box", "menu") == "lower-one" &&
                catalog.GetMessage("box", "Menu") == "other", "Empty override resolves locally and falls through between registered catalogs.");
            Check(catalog.GetPluralMessage("box", "boxes", long.MinValue, "menu") == "negative override" &&
                TranslationServer.TranslatePlural("", "box", "boxes", long.MaxValue, "menu") == "positive override" &&
                catalog.GetPluralMessage("box", "boxes", 2, "menu") == "" &&
                TranslationServer.TranslatePlural("", "box", "boxes", 2, "menu") == "lower-many",
                "Plural overrides preserve full signed counts, resolve before storage and keep server empty-result fallthrough.");
            catalog.Singular = null;
            catalog.Plural = null;
            Check(catalog.GetPluralMessage("box", "boxes", -1, "menu") == "", "Local plural lookup rejects negative counts as missing.");

            var notifications = 0;
            catalog.Changed += _ => notifications++;
            Reject<ArgumentNullException>(() => catalog.Locale = null!);
            Reject<CultureNotFoundException>(() => catalog.Locale = "en/invalid");
            Reject<ArgumentNullException>(() => catalog.GetMessage(null!));
            Reject<ArgumentNullException>(() => catalog.GetPluralMessage("box", null!, 2, "menu"));
            Reject<ArgumentNullException>(() => catalog.EraseMessage("box", null!));
            Reject<ArgumentException>(() => catalog.AddPluralMessage("box", [], "menu"));
            Reject<ArgumentException>(() => catalog.AddPluralMessage("box", ["valid", null!], "menu"));
            catalog.EraseMessage("missing");
            Check(notifications == 0 && catalog.Locale == "fr-CA" && catalog.GetPluralMessage("box", "boxes", 2, "menu") == "many",
                "Rejected inputs and missing erase preserve the previous complete entry without a notification.");
            catalog.EraseMessage("box", "menu");
            Check(notifications == 1 && catalog.GetMessage("box", "Menu") == "other" &&
                TranslationServer.TranslatePlural("", "box", "boxes", 2, "menu") == "lower-many",
                "Erase retires every form in just the matching context and publishes once.");
            catalog.AddMessage("box", "", "menu");
            Check(catalog.GetMessage("box", "menu") == "" && TranslationServer.Translate("", "box", "menu") == "lower-one",
                "An empty stored translation also falls through on the server.");
            catalog.Locale = "de-DE";
            catalog.AddMessage("box", "de-one", "menu");
            Check(TranslationServer.Translate("", "box", "menu") == "lower-one", "Locale edits immediately change server eligibility.");
        }
        finally
        {
            TranslationServer.RemoveTranslation(catalog);
            TranslationServer.RemoveTranslation(lower);
        }
        using var disposed = new CatalogOverrides { Singular = (_, _) => throw new InvalidOperationException("Disposed catalog called an override.") };
        disposed.Dispose();
        Reject<ObjectDisposedException>(() => disposed.GetMessage("box"));
        CheckArchive();
        Console.WriteLine("Translation catalog: contextual keys, override/fallthrough, counts, copied forms, errors, live edits and archives passed.");
    }

    private static void CheckArchive()
    {
        using var source = new Translation { Locale = "fr-CA", PluralRulesOverride = "nplurals=2; plural=n==1;" };
        source.AddPluralMessage("box", ["many", "one"], "menu");
        source.AddPluralMessage("compress", ["first", "second"]);
        using var optimized = new OptimizedTranslation();
        Check(optimized.Generate(source), "Generate optimized catalog from a public Translation resource.");
        using var copy = (Translation)source.Duplicate();
        source.AddMessage("box", "replacement", "menu");
        optimized.AddMessage("local-only", "hidden");
        Check(copy.GetMessage("box", "menu") == "many" && optimized.GetMessage("local-only") == "" &&
            optimized.GetMessage("box", "menu") == "", "Copies keep prior forms and optimized lookup ignores inherited storage and skips contextual keys.");
        var directory = Directory.CreateTempSubdirectory("e2d-translation-");
        try
        {
            var ordinaryPath = IOPath.Combine(directory.FullName, "ordinary.e2dres");
            var optimizedPath = IOPath.Combine(directory.FullName, "optimized.e2dres");
            ResourceSaver.Save(copy, ordinaryPath);
            ResourceSaver.Save(optimized, optimizedPath);
            using var restored = ResourceLoader.Load<Translation>(ordinaryPath, ResourceLoader.CacheMode.Ignore);
            using var compressed = ResourceLoader.Load<Translation>(optimizedPath, ResourceLoader.CacheMode.Ignore);
            Check(restored.Locale == "fr-CA" && restored.PluralRulesOverride == copy.PluralRulesOverride &&
                restored.GetPluralMessage("box", "boxes", 1, "menu") == "one" &&
                restored.GetPluralMessage("box", "boxes", 2, "menu") == "many", "Ordinary archive restores context and rule priority.");
            Check(compressed is OptimizedTranslation && compressed.Locale == "fr-CA" &&
                compressed.GetPluralMessage("compress", "ignored", 2, "ignored") == "first" &&
                compressed.GetMessage("local-only") == "", "Optimized archive restores its type and first-form lookup through the base API.");
            TranslationServer.AddTranslation(restored);
            TranslationServer.AddTranslation(compressed);
            try
            {
                Check(TranslationServer.TranslatePlural("", "box", "boxes", 1, "menu") == "one" &&
                    TranslationServer.Translate("", "compress", "ignored") == "first", "Reloaded ordinary and optimized catalogs serve the live server.");
            }
            finally
            {
                TranslationServer.RemoveTranslation(compressed);
                TranslationServer.RemoveTranslation(restored);
            }
        }
        finally { directory.Delete(recursive: true); }
    }

    private static void CheckConcurrentCatalogLookup()
    {
        using var catalog = new CatalogOverrides();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var deadline = TimeSpan.FromSeconds(10);
        catalog.AddPluralMessage("box", ["old-one", "old-many"]);
        catalog.PluralSelector = _ =>
        {
            entered.Set();
            Check(release.Wait(deadline), "The selector must be able to wait while another thread edits the catalog.");
            return 1;
        };
        var read = Task.Run(() => catalog.GetPluralMessage("box", "boxes", 2));
        Task? write = null;
        try
        {
            Check(entered.Wait(deadline), "Reader reached the selector with its original forms.");
            write = Task.Run(() =>
            {
                catalog.PluralSelector = _ => 0;
                catalog.Changed += _ =>
                {
                    var observe = Task.Run(() => (catalog.GetMessage("box"), catalog.GetPluralMessage("box", "boxes", 2)));
                    Check(observe.WaitAsync(deadline).GetAwaiter().GetResult() == ("new", "new"),
                        "Another thread sees the whole committed entry inside Changed delivery.");
                };
                catalog.AddMessage("box", "new");
            });
            write.WaitAsync(deadline).GetAwaiter().GetResult();
        }
        finally
        {
            release.Set();
            if (write is not null) write.WaitAsync(deadline).GetAwaiter().GetResult();
        }
        Check(read.WaitAsync(deadline).GetAwaiter().GetResult() == "old-many" &&
            catalog.GetPluralMessage("box", "boxes", 2) == "new", "The in-flight lookup uses its original forms and selector together.");

        catalog.Singular = (_, _) => Task.Run(() => catalog.GetMessageCount()).WaitAsync(deadline).GetAwaiter().GetResult() == 1 ? null : "unexpected";
        catalog.Plural = (_, _, _, _) => Task.Run(() => catalog.GetMessageCount()).WaitAsync(deadline).GetAwaiter().GetResult() == 1 ? null : "unexpected";
        Check(catalog.GetMessage("box") == "new" && catalog.GetPluralMessage("box", "boxes", 2) == "new",
            "Both overrides allow another thread to acquire the catalog lock before falling through.");
        Console.WriteLine("Translation catalog concurrency: captured forms and selector, unlocked overrides and committed Changed delivery passed.");
    }

    private static void CheckServerLocaleChange()
    {
        using var fallback = new Translation { Locale = "fr" };
        using var catalog = new CatalogOverrides { Locale = "fr-CA" };
        catalog.AddPluralMessage("item", ["regional-one", "regional-many"], "menu");
        fallback.AddPluralMessage("item", ["fallback-one", "fallback-many"], "menu");
        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-CA");
        TranslationServer.AddTranslation(fallback);
        TranslationServer.AddTranslation(catalog);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var deadline = TimeSpan.FromSeconds(10);
        string Lookup() => TranslationServer.TranslatePlural("", "item", "items", 0, "menu");
        try
        {
            Check(Lookup() == "regional-one", "The exact French catalog selects its first form for zero.");
            catalog.Plural = (_, _, _, _) =>
            {
                entered.Set();
                Check(release.Wait(deadline), "The plural override waited for a locale change.");
                return null;
            };
            var read = Task.Run(Lookup);
            try
            {
                Check(entered.Wait(deadline), "The server selected the regional catalog and entered its override.");
                Task.Run(() => catalog.Locale = "en").WaitAsync(deadline).GetAwaiter().GetResult();
            }
            finally { release.Set(); }
            var actual = read.WaitAsync(deadline).GetAwaiter().GetResult();
            Check(actual == "fallback-one", $"Expected the French fallback after the locale edit, got '{actual}'.");
            Check(Lookup() == "fallback-one" && catalog.GetPluralMessage("item", "items", 0, "menu") == "regional-many",
                "Subsequent server lookup skips the edited catalog, which uses the English rule for direct lookup.");

            catalog.Locale = "fr-CA";
            catalog.Singular = (_, _) =>
            {
                catalog.Locale = "en";
                return "stale override";
            };
            Check(TranslationServer.Translate("", "item", "menu") == "fallback-one",
                "Singular lookup also discards the result of an override whose catalog changed locale.");
        }
        finally
        {
            TranslationServer.RemoveTranslation(catalog);
            TranslationServer.RemoveTranslation(fallback);
        }
        Console.WriteLine("Translation server: locale edits during singular and plural overrides discard stale rankings.");
    }
}
