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
            using (var saved = new ProjectSettingsRegistry(project, user))
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
            using var loaded = new ProjectSettingsRegistry(project, user);
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

        var settings = ProjectSettings.Service;
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
        var settingEnabled = ProjectSettings.Get(ProjectSettings.PseudolocalizationEnabled);
        var settingAccents = ProjectSettings.Get(ProjectSettings.PseudolocalizationReplaceWithAccents);
        var settingDoubleVowels = ProjectSettings.Get(ProjectSettings.PseudolocalizationDoubleVowels);
        var settingFakeBIDI = ProjectSettings.Get(ProjectSettings.PseudolocalizationFakeBIDI);
        var settingOverride = ProjectSettings.Get(ProjectSettings.PseudolocalizationOverride);
        var settingExpansion = ProjectSettings.Get(ProjectSettings.PseudolocalizationExpansionRatio);
        var settingPrefix = ProjectSettings.Get(ProjectSettings.PseudolocalizationPrefix);
        var settingSuffix = ProjectSettings.Get(ProjectSettings.PseudolocalizationSuffix);
        var settingSkipPlaceholders = ProjectSettings.Get(ProjectSettings.PseudolocalizationSkipPlaceholders);
        try
        {
            ProjectSettings.Set(ProjectSettings.PseudolocalizationEnabled, true);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationReplaceWithAccents, false);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationDoubleVowels, false);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationFakeBIDI, false);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationOverride, true);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationExpansionRatio, 0f);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationPrefix, "<");
            ProjectSettings.Set(ProjectSettings.PseudolocalizationSuffix, ">");
            ProjectSettings.Set(ProjectSettings.PseudolocalizationSkipPlaceholders, true);

            using var tree = new SceneTree(new Node());
            Engine.Start(tree);
            try
            {
                Check(TranslationServer.PseudolocalizationEnabled &&
                    TranslationServer.Translate("", "a %s") == "<**%s>", "Engine startup applies project pseudolocalization before runtime work.");
                ProjectSettings.Set(ProjectSettings.PseudolocalizationEnabled, false);
                ProjectSettings.Set(ProjectSettings.PseudolocalizationOverride, false);
                ProjectSettings.Set(ProjectSettings.PseudolocalizationDoubleVowels, true);
                ProjectSettings.Set(ProjectSettings.PseudolocalizationPrefix, "{");
                ProjectSettings.Set(ProjectSettings.PseudolocalizationSuffix, "}");
                ProjectSettings.Set(ProjectSettings.PseudolocalizationFakeBIDI, true);
                ProjectSettings.Set(ProjectSettings.PseudolocalizationExpansionRatio, 0.5f);
                ProjectSettings.Set(ProjectSettings.PseudolocalizationSkipPlaceholders, false);
                TranslationServer.ReloadPseudolocalization();
                Check(TranslationServer.PseudolocalizationEnabled &&
                    main.PseudolocalizationDoubleVowelsEnabled && main.PseudolocalizationFakeBIDIEnabled &&
                    main.PseudolocalizationExpansionRatio == 0.5f && !main.PseudolocalizationSkipPlaceholdersEnabled &&
                    main.PseudolocalizationPrefix == "{" && main.PseudolocalizationSuffix == "}",
                    "Reload updates transform options but preserves the runtime enablement switch.");
                ProjectSettings.Set(ProjectSettings.PseudolocalizationFakeBIDI, false);
                TranslationServer.ReloadPseudolocalization();
                Check(TranslationServer.Pseudolocalize("a") == "{aa}", "Reloaded settings change the actual text transform.");
            }
            finally { Engine.Stop(); }
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.PseudolocalizationEnabled, settingEnabled);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationReplaceWithAccents, settingAccents);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationDoubleVowels, settingDoubleVowels);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationFakeBIDI, settingFakeBIDI);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationOverride, settingOverride);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationExpansionRatio, settingExpansion);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationPrefix, settingPrefix);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationSuffix, settingSuffix);
            ProjectSettings.Set(ProjectSettings.PseudolocalizationSkipPlaceholders, settingSkipPlaceholders);
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
        var settings = ProjectSettings.Service;
        var oldTest = ProjectSettings.Get(ProjectSettings.LocaleTest);
        var oldFallbackSetting = ProjectSettings.Get(ProjectSettings.LocaleFallback);
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
            ProjectSettings.Set(ProjectSettings.LocaleTest, "fr-CA");
            ProjectSettings.Set(ProjectSettings.LocaleFallback, "en");
            using (var tree = new SceneTree(new Node()))
            {
                Engine.Start(tree);
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
                    ProjectSettings.Set(ProjectSettings.LocaleTest, "de-DE");
                    Check(TranslationServer.Culture.Name == "fr-CA", "Changing a startup locale setting does not switch the current run.");
                }
                finally { Engine.Stop(); }
            }
            ProjectSettings.Set(ProjectSettings.LocaleFallback, string.Empty);
            using (var tree = new SceneTree(new Node()))
            {
                Engine.Start(tree);
                try
                {
                    Check(TranslationServer.Culture.Name == "de-DE" && TranslationServer.FallbackCulture is null &&
                        TranslationServer.Translate("", "Only English") == "Only English",
                        "The next run applies the changed test locale and disabled fallback.");
                }
                finally { Engine.Stop(); }
            }
        }
        finally
        {
            main.RemoveTranslation(regional);
            main.RemoveTranslation(english);
            ProjectSettings.Set(ProjectSettings.LocaleTest, oldTest);
            ProjectSettings.Set(ProjectSettings.LocaleFallback, oldFallbackSetting);
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
