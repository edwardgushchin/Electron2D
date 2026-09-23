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
                    saved.Get(ProjectSettings.PseudolocalizationReplaceWithAccents) &&
                    saved.Get(ProjectSettings.PseudolocalizationSkipPlaceholders) &&
                    saved.Get(ProjectSettings.PseudolocalizationPrefix) == "[" &&
                    saved.Get(ProjectSettings.PseudolocalizationSuffix) == "]", "Typed pseudolocalization defaults are registered.");
                saved.Set(ProjectSettings.PseudolocalizationEnabled, true);
                saved.Set(ProjectSettings.PseudolocalizationFakeBIDI, true);
                saved.Set(ProjectSettings.PseudolocalizationExpansionRatio, 0.5f);
                saved.Set(ProjectSettings.PseudolocalizationPrefix, "<");
                saved.Save();
            }
            using var loaded = new ProjectSettings(project, user);
            loaded.Load();
            Check(loaded.Get(ProjectSettings.PseudolocalizationEnabled) &&
                loaded.Get(ProjectSettings.PseudolocalizationFakeBIDI) &&
                loaded.Get(ProjectSettings.PseudolocalizationExpansionRatio) == 0.5f &&
                loaded.Get(ProjectSettings.PseudolocalizationPrefix) == "<", "Typed pseudolocalization settings survive a project-file round trip.");
            Reject<ArgumentOutOfRangeException>(() => loaded.Set(ProjectSettings.PseudolocalizationExpansionRatio, -0.1f));
            Reject<ArgumentException>(() => loaded.Set(ProjectSettings.PseudolocalizationExpansionRatio, float.NaN));
            Reject<InvalidOperationException>(() => loaded.Unregister(ProjectSettings.PseudolocalizationEnabled));
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
        Console.WriteLine("Typed pseudolocalization project settings and runtime reload passed.");
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
