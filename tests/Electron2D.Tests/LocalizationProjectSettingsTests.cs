using Electron2D;
using IOPath = System.IO.Path;

internal static class LocalizationProjectSettingsTests
{
    internal static void Run()
    {
        var root = IOPath.Combine(IOPath.GetTempPath(), "electron2d-localization-" + Guid.NewGuid().ToString("N"));
        var project = IOPath.Combine(root, "project");
        var user = IOPath.Combine(root, "user");
        Directory.CreateDirectory(project);
        Directory.CreateDirectory(user);
        try
        {
            using (var saved = new ProjectSettings(project, user))
            {
                Check(!saved.Get(ProjectSettings.PseudolocalizationEnabled) &&
                    saved.Get(ProjectSettings.RootNodeAutoTranslate) &&
                    saved.Get(ProjectSettings.PseudolocalizationReplaceWithAccents) &&
                    saved.Get(ProjectSettings.PseudolocalizationSkipPlaceholders) &&
                    saved.Get(ProjectSettings.PseudolocalizationPrefix) == "[" &&
                    saved.Get(ProjectSettings.PseudolocalizationSuffix) == "]", "Typed pseudolocalization defaults are registered.");
                saved.Set(ProjectSettings.PseudolocalizationEnabled, true);
                saved.Set(ProjectSettings.PseudolocalizationFakeBIDI, true);
                saved.Set(ProjectSettings.PseudolocalizationExpansionRatio, 0.5f);
                saved.Set(ProjectSettings.PseudolocalizationPrefix, "<");
                saved.Set(ProjectSettings.LocaleTest, "fr-CA");
                saved.Set(ProjectSettings.LocaleFallback, "de");
                saved.Set(ProjectSettings.RootNodeAutoTranslate, false);
                saved.Save();
            }
            using var loaded = new ProjectSettings(project, user);
            loaded.Load();
            Check(loaded.Get(ProjectSettings.PseudolocalizationEnabled) &&
                loaded.Get(ProjectSettings.PseudolocalizationFakeBIDI) &&
                loaded.Get(ProjectSettings.PseudolocalizationExpansionRatio) == 0.5f &&
                loaded.Get(ProjectSettings.PseudolocalizationPrefix) == "<" &&
                loaded.Get(ProjectSettings.LocaleTest) == "fr-CA" &&
                loaded.Get(ProjectSettings.LocaleFallback) == "de", "Typed localization settings survive a project-file round trip.");
            Check(!loaded.Get(ProjectSettings.RootNodeAutoTranslate), "The root translation mode setting survives a project-file round trip.");
            Reject<ArgumentOutOfRangeException>(() => loaded.Set(ProjectSettings.PseudolocalizationExpansionRatio, -0.1f));
            Reject<ArgumentOutOfRangeException>(() => loaded.Set(ProjectSettings.LocaleTest, "bad locale!"));
            Reject<ArgumentException>(() => loaded.Set(ProjectSettings.PseudolocalizationExpansionRatio, float.NaN));
            Reject<InvalidOperationException>(() => loaded.Unregister(ProjectSettings.PseudolocalizationEnabled));
            Reject<InvalidOperationException>(() => loaded.Unregister(ProjectSettings.LocaleFallback));
        }
        finally { Directory.Delete(root, recursive: true); }

        var settings = ProjectSettings.Instance;
        var main = TranslationServer.GetOrAddDomain("");
        var oldEnabled = TranslationServer.PseudolocalizationEnabled;
        var oldAccents = main.PseudolocalizationAccentsEnabled;
        var oldDoubleVowels = main.PseudolocalizationDoubleVowelsEnabled;
        var oldFakeBIDI = main.PseudolocalizationFakeBIDIEnabled;
        var oldOverride = main.PseudolocalizationOverrideEnabled;
        var oldExpansion = main.PseudolocalizationExpansionRatio;
        var oldPrefix = main.PseudolocalizationPrefix;
        var oldSuffix = main.PseudolocalizationSuffix;
        var oldSkipPlaceholders = main.PseudolocalizationSkipPlaceholdersEnabled;
        var settingEnabled = settings.Get(ProjectSettings.PseudolocalizationEnabled);
        var settingAccents = settings.Get(ProjectSettings.PseudolocalizationReplaceWithAccents);
        var settingDoubleVowels = settings.Get(ProjectSettings.PseudolocalizationDoubleVowels);
        var settingFakeBIDI = settings.Get(ProjectSettings.PseudolocalizationFakeBIDI);
        var settingOverride = settings.Get(ProjectSettings.PseudolocalizationOverride);
        var settingExpansion = settings.Get(ProjectSettings.PseudolocalizationExpansionRatio);
        var settingPrefix = settings.Get(ProjectSettings.PseudolocalizationPrefix);
        var settingSuffix = settings.Get(ProjectSettings.PseudolocalizationSuffix);
        var settingSkipPlaceholders = settings.Get(ProjectSettings.PseudolocalizationSkipPlaceholders);
        try
        {
            settings.Set(ProjectSettings.PseudolocalizationEnabled, true);
            settings.Set(ProjectSettings.PseudolocalizationReplaceWithAccents, false);
            settings.Set(ProjectSettings.PseudolocalizationDoubleVowels, false);
            settings.Set(ProjectSettings.PseudolocalizationFakeBIDI, false);
            settings.Set(ProjectSettings.PseudolocalizationOverride, true);
            settings.Set(ProjectSettings.PseudolocalizationExpansionRatio, 0f);
            settings.Set(ProjectSettings.PseudolocalizationPrefix, "<");
            settings.Set(ProjectSettings.PseudolocalizationSuffix, ">");
            settings.Set(ProjectSettings.PseudolocalizationSkipPlaceholders, true);

            using var tree = new SceneTree(new Node());
            Engine.Instance.Start(tree);
            try
            {
                Check(TranslationServer.PseudolocalizationEnabled &&
                    TranslationServer.Translate("", "a %s") == "<**%s>", "Engine startup applies project pseudolocalization before runtime work.");
                settings.Set(ProjectSettings.PseudolocalizationEnabled, false);
                settings.Set(ProjectSettings.PseudolocalizationOverride, false);
                settings.Set(ProjectSettings.PseudolocalizationDoubleVowels, true);
                settings.Set(ProjectSettings.PseudolocalizationPrefix, "{");
                settings.Set(ProjectSettings.PseudolocalizationSuffix, "}");
                settings.Set(ProjectSettings.PseudolocalizationFakeBIDI, true);
                settings.Set(ProjectSettings.PseudolocalizationExpansionRatio, 0.5f);
                settings.Set(ProjectSettings.PseudolocalizationSkipPlaceholders, false);
                TranslationServer.ReloadPseudolocalization();
                Check(TranslationServer.PseudolocalizationEnabled &&
                    main.PseudolocalizationDoubleVowelsEnabled && main.PseudolocalizationFakeBIDIEnabled &&
                    main.PseudolocalizationExpansionRatio == 0.5f && !main.PseudolocalizationSkipPlaceholdersEnabled &&
                    main.PseudolocalizationPrefix == "{" && main.PseudolocalizationSuffix == "}",
                    "Reload updates transform options but preserves the runtime enablement switch.");
                settings.Set(ProjectSettings.PseudolocalizationFakeBIDI, false);
                TranslationServer.ReloadPseudolocalization();
                Check(TranslationServer.Pseudolocalize("a") == "{aa}", "Reloaded settings change the actual text transform.");
            }
            finally { Engine.Instance.Stop(); }
        }
        finally
        {
            settings.Set(ProjectSettings.PseudolocalizationEnabled, settingEnabled);
            settings.Set(ProjectSettings.PseudolocalizationReplaceWithAccents, settingAccents);
            settings.Set(ProjectSettings.PseudolocalizationDoubleVowels, settingDoubleVowels);
            settings.Set(ProjectSettings.PseudolocalizationFakeBIDI, settingFakeBIDI);
            settings.Set(ProjectSettings.PseudolocalizationOverride, settingOverride);
            settings.Set(ProjectSettings.PseudolocalizationExpansionRatio, settingExpansion);
            settings.Set(ProjectSettings.PseudolocalizationPrefix, settingPrefix);
            settings.Set(ProjectSettings.PseudolocalizationSuffix, settingSuffix);
            settings.Set(ProjectSettings.PseudolocalizationSkipPlaceholders, settingSkipPlaceholders);
            main.PseudolocalizationAccentsEnabled = oldAccents;
            main.PseudolocalizationDoubleVowelsEnabled = oldDoubleVowels;
            main.PseudolocalizationFakeBIDIEnabled = oldFakeBIDI;
            main.PseudolocalizationOverrideEnabled = oldOverride;
            main.PseudolocalizationExpansionRatio = oldExpansion;
            main.PseudolocalizationPrefix = oldPrefix;
            main.PseudolocalizationSuffix = oldSuffix;
            main.PseudolocalizationSkipPlaceholdersEnabled = oldSkipPlaceholders;
            TranslationServer.PseudolocalizationEnabled = oldEnabled;
        }
        CheckLocaleSelection();
        Console.WriteLine("Typed localization project settings, locale selection and runtime reload passed.");
    }

    private static void CheckLocaleSelection()
    {
        var settings = ProjectSettings.Instance;
        var oldTest = settings.Get(ProjectSettings.LocaleTest);
        var oldFallbackSetting = settings.Get(ProjectSettings.LocaleFallback);
        var oldCulture = TranslationServer.Culture;
        var oldFallbackCulture = TranslationServer.FallbackCulture;
        using var regional = new Translation { Locale = "fr-FR" };
        using var english = new Translation { Locale = "en" };
        regional.AddMessage("Color", "Couleur");
        english.AddMessage("Only English", "English value");
        english.AddPluralMessage("pear", ["one pear", "many pears"]);
        var main = TranslationServer.GetOrAddDomain("");
        try
        {
            Check(TranslationServer.CompareLocales("fr-CA", "fr-CA") == 10 &&
                TranslationServer.CompareLocales("fr-CA", "fr") == 5 &&
                TranslationServer.CompareLocales("fr-CA", "fr-FR") == 4 &&
                TranslationServer.CompareLocales("fr", "de") == 0 &&
                TranslationServer.CompareLocales("sr-Latn-RS", "sr-Cyrl-RS") == 5,
                "Locale score distinguishes exact, language, region, script and unrelated languages.");
            main.AddTranslation(regional);
            main.AddTranslation(english);
            settings.Set(ProjectSettings.LocaleTest, "fr-CA");
            settings.Set(ProjectSettings.LocaleFallback, "en");
            using (var tree = new SceneTree(new Node()))
            {
                Engine.Instance.Start(tree);
                try
                {
                    Check(TranslationServer.Culture.Name == "fr-CA" && TranslationServer.FallbackCulture?.Name == "en" &&
                        TranslationServer.GetToolLocale() == "fr-FR" &&
                        ReferenceEquals(TranslationServer.GetTranslationObject("fr-CA"), regional),
                        "Startup uses the test locale, fallback and scored catalog selection.");
                    Check(TranslationServer.Translate("", "Color") == "Couleur" &&
                        TranslationServer.Translate("", "Only English") == "English value" &&
                        TranslationServer.TranslatePlural("", "pear", "pears", 2) == "many pears",
                        "Close regional catalogs and fallback catalogs resolve singular and plural messages.");
                    using var standalone = new TranslationDomain { LocaleOverride = "de-DE" };
                    standalone.AddTranslation(english);
                    Check(standalone.Translate("Only English") == "English value" &&
                        standalone.TranslatePlural("pear", "pears", 2) == "many pears",
                        "Standalone domains use the same fallback for singular and plural lookup.");
                    settings.Set(ProjectSettings.LocaleTest, "de-DE");
                    Check(TranslationServer.Culture.Name == "fr-CA", "Changing a startup locale setting does not switch the current run.");
                }
                finally { Engine.Instance.Stop(); }
            }
            settings.Set(ProjectSettings.LocaleFallback, string.Empty);
            using (var tree = new SceneTree(new Node()))
            {
                Engine.Instance.Start(tree);
                try
                {
                    Check(TranslationServer.Culture.Name == "de-DE" && TranslationServer.FallbackCulture is null &&
                        TranslationServer.Translate("", "Only English") == "Only English",
                        "The next run applies the changed test locale and disabled fallback.");
                }
                finally { Engine.Instance.Stop(); }
            }
        }
        finally
        {
            main.RemoveTranslation(regional);
            main.RemoveTranslation(english);
            settings.Set(ProjectSettings.LocaleTest, oldTest);
            settings.Set(ProjectSettings.LocaleFallback, oldFallbackSetting);
            TranslationServer.Culture = oldCulture;
            TranslationServer.FallbackCulture = oldFallbackCulture;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
