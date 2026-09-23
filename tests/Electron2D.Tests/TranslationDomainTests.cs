using System.Globalization;
using Electron2D;

internal static class TranslationDomainTests
{
    internal static void Run()
    {
        var oldCulture = TranslationServer.Culture;
        var oldEnabled = TranslationServer.Enabled;
        var main = TranslationServer.GetOrAddDomain("");
        var oldPseudo = main.PseudolocalizationEnabled;
        try
        {
            TranslationServer.Clear();
            TranslationServer.Enabled = true;
            TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-CA");
            using var catalog = new Translation { Locale = "fr" };
            catalog.AddMessage("Play", "Jouer");
            catalog.AddPluralMessage("pear", ["poire", "poires"]);
            catalog.PluralSelector = count => count > 1 ? 1 : 0;

            var domain = TranslationServer.GetOrAddDomain("domain-tests");
            Check(ReferenceEquals(domain, TranslationServer.GetOrAddDomain("domain-tests")) &&
                TranslationServer.HasDomain("domain-tests") && TranslationServer.HasDomain(""), "Domain registry identity.");
            domain.AddTranslation(catalog);
            domain.AddTranslation(catalog);
            Check(domain.GetTranslations().Length == 1 && domain.HasTranslation(catalog) &&
                domain.HasTranslationForLocale("fr-CA", false) && !domain.HasTranslationForLocale("fr-CA", true) &&
                ReferenceEquals(domain.GetTranslationObject("fr-CA"), catalog), "Catalog registration and locale lookup.");
            var snapshot = domain.GetTranslations();
            domain.RemoveTranslation(catalog);
            Check(snapshot.Length == 1 && ReferenceEquals(snapshot[0], catalog) &&
                !domain.HasTranslation(catalog) && !catalog.IsDisposed, "Removal preserves borrowed resources and prior snapshots.");
            domain.AddTranslation(catalog);
            domain.Clear();
            Check(domain.GetTranslations().Length == 0 && !catalog.IsDisposed, "Clearing a domain only removes its registrations.");
            domain.AddTranslation(catalog);
            Check(domain.Translate("Play") == "Jouer" && domain.TranslatePlural("pear", "pears", 2) == "poires",
                "Registered domain resolves resource entries through culture fallback.");
            TranslationServer.AddTranslation(CultureInfo.GetCultureInfo("fr"), "domain-tests", "Play", "Démarrer");
            Check(domain.Translate("Play") == TranslationServer.Translate("domain-tests", "Play") &&
                domain.Translate("Play") == "Démarrer", "Direct entries take priority through either domain entry point.");
            domain.LocaleOverride = "en-US";
            Check(domain.Translate("Play") == "Play", "Locale override replaces the selected culture.");
            domain.LocaleOverride = "";
            domain.Enabled = false;
            Check(domain.Translate("Play") == "Play" && TranslationServer.Translate("domain-tests", "Play") == "Play",
                "Domain disabling also disables direct entries.");
            domain.Enabled = true;

            Check(!domain.PseudolocalizationEnabled && domain.PseudolocalizationAccentsEnabled &&
                domain.PseudolocalizationSkipPlaceholdersEnabled && domain.PseudolocalizationPrefix == "[" &&
                domain.PseudolocalizationSuffix == "]", "Pseudolocalization defaults.");
            domain.PseudolocalizationEnabled = true;
            Check(domain.Translate("Play").StartsWith("[Ð", StringComparison.Ordinal) &&
                domain.Pseudolocalize("a") == "[á]" && domain.TranslatePlural("pear", "pears", 2) == "poires",
                "Singular lookup is pseudolocalized while plural lookup is not.");
            domain.PseudolocalizationAccentsEnabled = false;
            domain.PseudolocalizationOverrideEnabled = true;
            Check(domain.Pseudolocalize("a %s") == "[**%s]", "Placeholders survive override transforms.");
            domain.PseudolocalizationOverrideEnabled = false;
            domain.PseudolocalizationDoubleVowelsEnabled = true;
            domain.PseudolocalizationExpansionRatio = 0.5f;
            Check(domain.Pseudolocalize("abcd") == "[_aabcd_]", "Vowel doubling and expansion use the source length.");
            domain.PseudolocalizationFakeBIDIEnabled = true;
            Check(domain.Pseudolocalize("x").Contains('\u202e'), "Fake BIDI adds direction controls.");
            Reject<ArgumentOutOfRangeException>(() => domain.PseudolocalizationExpansionRatio = float.NaN);

            domain.PseudolocalizationEnabled = false;
            TranslationServer.RemoveDomain("domain-tests");
            Check(!TranslationServer.HasDomain("domain-tests") && domain.Translate("Play") == "Jouer" &&
                TranslationServer.Translate("domain-tests", "Play") == "Play", "Removed domain remains usable independently.");
            Reject<ArgumentException>(() => TranslationServer.RemoveDomain(""));
            domain.Dispose();
            var replacement = TranslationServer.GetOrAddDomain("domain-tests");
            Check(!ReferenceEquals(domain, replacement), "Removed domain can be recreated.");
            TranslationServer.AddTranslation(CultureInfo.GetCultureInfo("fr"), "domain-tests", "Play", "Stale");
            replacement.Dispose();
            Check(!TranslationServer.HasDomain("domain-tests") &&
                TranslationServer.Translate("domain-tests", "Play") == "Play",
                "Disposal unregisters a live domain and its direct entries.");
            using var disposedCatalog = new Translation();
            disposedCatalog.Dispose();
            Reject<ObjectDisposedException>(() => TranslationServer.AddTranslation(disposedCatalog, "invalid-domain"));
            Check(!TranslationServer.HasDomain("invalid-domain"), "A disposed catalog cannot create a domain.");

            main.PseudolocalizationEnabled = true;
            Check(TranslationServer.PseudolocalizationEnabled && TranslationServer.Pseudolocalize("a") == "[á]" &&
                TranslationServer.GetTranslations().Length == 0, "Main-domain wrappers use the same domain.");
            TranslationServer.AddTranslation(catalog);
            Check(TranslationServer.GetLoadedLocales().SequenceEqual(["fr"]) &&
                TranslationServer.HasTranslation(catalog) && TranslationServer.FindTranslations("fr-CA", false).Length == 1,
                "Main-domain catalog queries are live.");
            catalog.Dispose();
            Check(TranslationServer.GetLoadedLocales().Length == 0, "Disposed catalogs disappear from locale lists.");
        }
        finally
        {
            TranslationServer.Clear();
            TranslationServer.RemoveDomain("domain-tests");
            main.PseudolocalizationEnabled = oldPseudo;
            TranslationServer.Culture = oldCulture;
            TranslationServer.Enabled = oldEnabled;
        }
        Console.WriteLine("Translation domain registration, lookup and pseudolocalization passed.");
    }

    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
