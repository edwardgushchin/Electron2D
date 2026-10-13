namespace Electron2D;

public static partial class TranslationServer
{
    /// <summary>Gets all supported language codes in catalog order.</summary>
    /// <returns>A caller-owned ordered array.</returns>
    public static string[] GetAllLanguages() => LocaleCatalog.Languages.Keys.ToArray();
    /// <summary>Gets all supported writing-system codes in catalog order.</summary>
    /// <returns>A caller-owned ordered array.</returns>
    public static string[] GetAllScripts() => LocaleCatalog.Scripts.Keys.ToArray();
    /// <summary>Gets all supported country codes in catalog order.</summary>
    /// <returns>A caller-owned ordered array.</returns>
    public static string[] GetAllCountries() => LocaleCatalog.Countries.Keys.ToArray();
    /// <summary>Gets the catalog's English language name, or the supplied unknown code.</summary>
    /// <param name="language">Ordinal language code.</param><returns>The name or unchanged unknown code.</returns>
    /// <exception cref="ArgumentNullException">The code is null.</exception>
    public static string GetLanguageName(string language) { ArgumentNullException.ThrowIfNull(language); return LocaleCatalog.Languages.GetValueOrDefault(language, language); }
    /// <summary>Gets the catalog's English writing-system name, or the supplied unknown code.</summary>
    /// <param name="script">Ordinal writing-system code.</param><returns>The name or unchanged unknown code.</returns>
    /// <exception cref="ArgumentNullException">The code is null.</exception>
    public static string GetScriptName(string script) { ArgumentNullException.ThrowIfNull(script); return LocaleCatalog.Scripts.GetValueOrDefault(script, script); }
    /// <summary>Gets the catalog's English country name, or the supplied unknown code.</summary>
    /// <param name="country">Ordinal country code.</param><returns>The name or unchanged unknown code.</returns>
    /// <exception cref="ArgumentNullException">The code is null.</exception>
    public static string GetCountryName(string country) { ArgumentNullException.ThrowIfNull(country); return LocaleCatalog.Countries.GetValueOrDefault(country, country); }
    /// <summary>Combines English language, optional writing-system and country names for a locale.</summary>
    /// <param name="locale">A locale identifier.</param><returns>Language, optional parenthesized script, and optional comma-separated country.</returns>
    /// <exception cref="ArgumentNullException">The locale is null.</exception>
    public static string GetLocaleName(string locale)
    {
        var parsed = ParseCatalogLocale(locale, false); var name = GetLanguageName(parsed.Language);
        if (parsed.Script.Length != 0) name += " (" + GetScriptName(parsed.Script) + ")";
        if (parsed.Region.Length != 0) name += ", " + GetCountryName(parsed.Region);
        return name;
    }
    /// <summary>Normalizes locale aliases, separators, supported script and regional variants.</summary>
    /// <param name="locale">Locale text; empty remains empty.</param>
    /// <param name="addDefaults">Adds catalog script/country defaults for ambiguous languages.</param>
    /// <returns>A language[_Script][_COUNTRY][_variant] identifier.</returns>
    /// <remarks>Catalog casing is significant. This source normalization does not require a platform globalization entry.</remarks>
    /// <exception cref="ArgumentNullException">The locale is null.</exception>
    public static string StandardizeLocale(string locale, bool addDefaults = false)
    {
        var parsed = ParseCatalogLocale(locale, addDefaults); var result = parsed.Language;
        if (parsed.Script.Length != 0) result += "_" + parsed.Script;
        if (parsed.Region.Length != 0) result += "_" + parsed.Region;
        if (parsed.Variant.Length != 0) result += "_" + parsed.Variant;
        return result;
    }
    /// <summary>Gets the locale's fixed gettext plural definition.</summary>
    /// <param name="locale">A locale identifier, including supported aliases and region overrides.</param>
    /// <returns>The nplurals/selector definition, or empty for an unknown language.</returns>
    /// <exception cref="ArgumentNullException">The locale is null.</exception>
    public static string GetPluralRules(string locale) => FindPluralRule(locale).Definition ?? "";

    internal static (string Definition, int Forms, Func<long, int>? Select) FindPluralRule(string locale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        if (LocaleCatalog.Plurals.TryGetValue(locale, out var rule)) return rule;
        var parsed = ParseCatalogLocale(locale, false);
        if (parsed.Region.Length != 0 && LocaleCatalog.Plurals.TryGetValue(parsed.Language + "_" + parsed.Region, out rule)) return rule;
        return LocaleCatalog.Plurals.GetValueOrDefault(parsed.Language);
    }
    private static (string Language, string Script, string Region, string Variant) ParseCatalogLocale(string locale, bool defaults)
    {
        ArgumentNullException.ThrowIfNull(locale);
        var extra = locale.Replace('-', '_').Split('@'); var parts = extra[0].Split('_');
        var language = parts[0]; var script = ""; var region = ""; var variant = "";
        static bool Country(string text) => text.Length == 2 && char.IsAsciiLetterUpper(text[0]) && char.IsAsciiLetterUpper(text[1]);
        static bool Script(string text) => text.Length == 4 && char.IsAsciiLetterUpper(text[0]) && text.AsSpan(1).IndexOfAnyExceptInRange('a', 'z') == -1;
        bool Variant(string text) => LocaleCatalog.Variants.GetValueOrDefault(text.ToLowerInvariant()) == language;
        if (parts.Length >= 2) { if (Script(parts[1])) script = parts[1]; if (Country(parts[1])) region = parts[1]; }
        if (parts.Length >= 3) { if (Country(parts[2])) region = parts[2]; else if (Variant(parts[2])) variant = parts[2].ToLowerInvariant(); }
        if (parts.Length >= 4 && Variant(parts[3])) variant = parts[3].ToLowerInvariant();
        if (extra.Length >= 2) foreach (var item in extra[1].Split(';'))
            {
                var value = item.ToLowerInvariant();
                if (value is "cyrillic" or "latin" or "devanagari") { script = value == "cyrillic" ? "Cyrl" : value == "latin" ? "Latn" : "Deva"; break; }
                if (Variant(value)) variant = value;
            }
        language = LocaleCatalog.LanguageRenames.GetValueOrDefault(language, language);
        region = LocaleCatalog.CountryRenames.GetValueOrDefault(region, region);
        if (!LocaleCatalog.Scripts.ContainsKey(script)) script = "";
        if (defaults)
        {
            if (script.Length == 0) foreach (var entry in LocaleCatalog.Defaults)
                    if (entry.Language == language && (region.Length == 0 || entry.Countries.Split(',').Contains(region, StringComparer.Ordinal))) { script = entry.Script; break; }
            if (script.Length != 0 && region.Length == 0) foreach (var entry in LocaleCatalog.Defaults)
                    if (entry.Language == language && entry.Script == script) { region = entry.DefaultCountry; break; }
        }
        return (language, script, region, variant);
    }
}
