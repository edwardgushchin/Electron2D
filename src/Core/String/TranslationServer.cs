using System.Globalization;
using System.Threading;

namespace Electron2D;

/// <summary>Stores and resolves process-wide in-memory translations for the selected UI culture.</summary>
/// <remarks>
/// Lookups use exact, case-sensitive domain, context, and source-message keys, then search the selected culture,
/// its parents, close resource locale matches, and a configured fallback. Direct entries precede borrowed resource
/// catalogs at each exact locale; later resource registrations win equal scores. This service does not load catalog
/// files or implement CLDR rules.
/// </remarks>
public static partial class TranslationServer
{
    private static readonly object Gate = new();
    private static readonly Dictionary<(string Culture, string Domain, string Context, string Message), string> Messages = new();
    private static readonly Dictionary<(string Culture, string Domain, string Context, string Singular, string Plural), Func<long, string>> Plurals = new();
    private static readonly Dictionary<string, TranslationDomain> Domains = new(StringComparer.Ordinal);

    private static CultureInfo _culture = CultureInfo.CurrentUICulture;
    private static CultureInfo? _fallbackCulture = CultureInfo.GetCultureInfo("en");
    private static int _enabled = 1;

    /// <summary>Gets or sets whether translation lookup is enabled globally.</summary>
    /// <value><see langword="true"/> by default. When false, lookup returns source text without consulting the catalogs.</value>
    internal static bool Enabled
    {
        get => Volatile.Read(ref _enabled) != 0;
        set => Volatile.Write(ref _enabled, value ? 1 : 0);
    }

    /// <summary>Gets or sets the culture used for subsequent translation lookups.</summary>
    /// <value>The process-wide lookup culture, initialized from <see cref="CultureInfo.CurrentUICulture"/> and sampled again at Engine startup unless a project test locale is configured.</value>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null"/>.</exception>
    public static CultureInfo Culture
    {
        get
        {
            lock (Gate)
                return _culture;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            lock (Gate)
                _culture = value;
        }
    }

    internal static CultureInfo? FallbackCulture
    {
        get { lock (Gate) return _fallbackCulture; }
        set { lock (Gate) _fallbackCulture = value; }
    }

    /// <summary>Scores how closely two supported locale names match.</summary>
    /// <param name="localeA">The requested locale.</param>
    /// <param name="localeB">The candidate locale.</param>
    /// <returns>Ten for the same normalized locale, zero for different languages, or a score around five adjusted for matching script, region, and variant.</returns>
    /// <exception cref="ArgumentNullException">A locale is null.</exception>
    /// <exception cref="CultureNotFoundException">A nonidentical locale is not supported by .NET globalization.</exception>
    public static int CompareLocales(string localeA, string localeB)
    {
        ArgumentNullException.ThrowIfNull(localeA);
        ArgumentNullException.ThrowIfNull(localeB);
        if (localeA == localeB) return 10;
        var a = ParseLocale(localeA);
        var b = ParseLocale(localeB);
        if (a == b) return 10;
        if (a.Language != b.Language) return 0;
        var score = 5;
        if (a.Script.Length != 0 && b.Script.Length != 0) score += a.Script == b.Script ? 1 : -1;
        if (a.Region.Length != 0 && b.Region.Length != 0) score += a.Region == b.Region ? 1 : -1;
        if (a.Variant.Length != 0 && b.Variant.Length != 0) score += a.Variant == b.Variant ? 1 : -1;
        return score;
    }

    /// <summary>Gets the best loaded main-domain catalog locale for the selected culture, or the configured fallback.</summary>
    /// <returns>The matching catalog locale, the current culture for an exact match, or the fallback locale.</returns>
    /// <remarks>Reuses the domain's internal catalog snapshot and compares already normalized locale names without allocating. Catalog locale changes are observed on every lookup.</remarks>
    public static string GetToolLocale()
    {
        var requested = Culture.Name;
        var bestScore = 0;
        string? best = null;
        foreach (var translation in GetOrAddDomain(string.Empty).GetLookupSnapshot())
        {
            try
            {
                var candidate = translation.Locale;
                var score = CompareNormalizedLocales(requested, candidate);
                if (score <= 0 || score < bestScore) continue;
                if (score == 10) return requested;
                best = candidate;
                bestScore = score;
            }
            catch (ObjectDisposedException) when (translation.IsDisposed) { }
        }
        return best ?? FallbackCulture?.Name ?? string.Empty;
    }

    private static int CompareNormalizedLocales(string first, string second)
    {
        if (first == second) return 10;
        var a = first.AsSpan(); var b = second.AsSpan(); var aSeparator = a.IndexOf('-'); var bSeparator = b.IndexOf('-');
        var aLanguage = a[..(aSeparator < 0 ? a.Length : aSeparator)]; var bLanguage = b[..(bSeparator < 0 ? b.Length : bSeparator)];
        if (!aLanguage.SequenceEqual(bLanguage)) return 0;
        ParseNormalizedParts(aSeparator < 0 ? [] : a[(aSeparator + 1)..], out var aScript, out var aRegion, out var aVariant);
        ParseNormalizedParts(bSeparator < 0 ? [] : b[(bSeparator + 1)..], out var bScript, out var bRegion, out var bVariant);
        if (aScript.SequenceEqual(bScript) && aRegion.SequenceEqual(bRegion) && aVariant.SequenceEqual(bVariant)) return 10;
        var score = 5;
        if (!aScript.IsEmpty && !bScript.IsEmpty) score += aScript.SequenceEqual(bScript) ? 1 : -1;
        if (!aRegion.IsEmpty && !bRegion.IsEmpty) score += aRegion.SequenceEqual(bRegion) ? 1 : -1;
        if (!aVariant.IsEmpty && !bVariant.IsEmpty) score += aVariant.SequenceEqual(bVariant) ? 1 : -1;
        return score;
    }

    private static void ParseNormalizedParts(ReadOnlySpan<char> parts, out ReadOnlySpan<char> script,
        out ReadOnlySpan<char> region, out ReadOnlySpan<char> variant)
    {
        script = region = variant = [];
        foreach (var range in parts.Split('-'))
        {
            var part = parts[range];
            if (part.Length == 4 && script.IsEmpty) script = part;
            else if (part.Length is 2 or 3 && region.IsEmpty) region = part;
            else if (variant.IsEmpty) variant = part;
        }
    }

    private static (string Language, string Script, string Region, string Variant) ParseLocale(string locale)
    {
        if (locale.Length == 0) return (string.Empty, string.Empty, string.Empty, string.Empty);
        var parts = CultureInfo.GetCultureInfo(locale.Replace('_', '-')).Name.Split('-');
        var script = string.Empty;
        var region = string.Empty;
        var variant = string.Empty;
        foreach (var part in parts.Skip(1))
        {
            if (part.Length == 4 && script.Length == 0) script = part;
            else if ((part.Length == 2 || part.Length == 3) && region.Length == 0) region = part;
            else if (variant.Length == 0) variant = part;
        }
        return (parts[0], script, region, variant);
    }

    /// <summary>Adds or replaces one singular translation.</summary>
    /// <param name="culture">The culture whose name forms part of the lookup key.</param>
    /// <param name="domain">The case-sensitive translation domain.</param>
    /// <param name="message">The source message.</param>
    /// <param name="translation">The translated message.</param>
    /// <param name="context">An optional disambiguation context. Null is normalized to an empty string.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="culture"/>, <paramref name="domain"/>, <paramref name="message"/>, or
    /// <paramref name="translation"/> is <see langword="null"/>.
    /// </exception>
    public static void AddTranslation(
        CultureInfo culture,
        string domain,
        string message,
        string translation,
        string? context = null)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(translation);

        lock (Gate)
        {
            EnsureDomainUnderLock(domain);
            Messages[(culture.Name, domain, context ?? string.Empty, message)] = translation;
        }
    }

    /// <summary>Adds or replaces one plural translation selector.</summary>
    /// <param name="culture">The culture whose name forms part of the lookup key.</param>
    /// <param name="domain">The case-sensitive translation domain.</param>
    /// <param name="singular">The source singular form.</param>
    /// <param name="plural">The source plural form.</param>
    /// <param name="selector">A function that maps a quantity to translated text.</param>
    /// <param name="context">An optional disambiguation context. Null is normalized to an empty string.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="culture"/>, <paramref name="domain"/>, <paramref name="singular"/>,
    /// <paramref name="plural"/>, or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    public static void AddPluralTranslation(
        CultureInfo culture,
        string domain,
        string singular,
        string plural,
        Func<long, string> selector,
        string? context = null)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(singular);
        ArgumentNullException.ThrowIfNull(plural);
        ArgumentNullException.ThrowIfNull(selector);

        lock (Gate)
        {
            EnsureDomainUnderLock(domain);
            Plurals[(culture.Name, domain, context ?? string.Empty, singular, plural)] = selector;
        }
    }

    /// <summary>Registers a live translation resource for a domain.</summary>
    /// <param name="translation">The borrowed resource; its subsequent edits are visible to lookups.</param>
    /// <param name="domain">The case-sensitive domain, empty by default.</param>
    /// <remarks>The most recently registered resource wins within one locale. Disposing the resource removes it.</remarks>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public static void AddTranslation(Translation translation, string domain = "")
    {
        ArgumentNullException.ThrowIfNull(translation);
        ArgumentNullException.ThrowIfNull(domain);
        if (translation.IsDisposed) throw new ObjectDisposedException(nameof(translation));
        GetOrAddDomain(domain).AddTranslation(translation);
    }

    /// <summary>Unregisters a translation resource from a domain without disposing it.</summary>
    /// <param name="translation">The resource to remove.</param>
    /// <param name="domain">The registration domain.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static void RemoveTranslation(Translation translation, string domain = "")
    {
        ArgumentNullException.ThrowIfNull(translation);
        ArgumentNullException.ThrowIfNull(domain);
        TranslationDomain? registered;
        lock (Gate) Domains.TryGetValue(domain, out registered);
        if (registered is not null)
        {
            try { registered.RemoveTranslation(translation); }
            catch (ObjectDisposedException) when (registered.IsDisposed) { }
        }
    }

    /// <summary>Returns a registered translation domain, creating it if necessary.</summary>
    /// <param name="name">The exact case-sensitive domain name; empty selects the main domain.</param>
    /// <returns>A caller-visible borrowed domain; disposing it removes its registration.</returns>
    public static TranslationDomain GetOrAddDomain(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (Gate) return EnsureDomainUnderLock(name);
    }

    /// <summary>Reports whether a named domain is registered.</summary>
    /// <param name="name">The exact domain name.</param>
    /// <returns>True for the main domain or a live custom domain.</returns>
    public static bool HasDomain(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (Gate) return name.Length == 0 || Domains.TryGetValue(name, out var domain) && !domain.IsDisposed;
    }

    /// <summary>Removes a custom domain and its direct entries without disposing the domain or its catalogs.</summary>
    /// <param name="name">A nonempty exact domain name.</param>
    public static void RemoveDomain(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0) throw new ArgumentException("The main translation domain cannot be removed.", nameof(name));
        lock (Gate)
        {
            if (Domains.Remove(name, out var removed))
            {
                removed.Disposed -= OnDomainDisposed;
                if (!removed.IsDisposed) removed.SetRegisteredName(null);
            }
            RemoveDirectEntriesUnderLock(name);
        }
    }

    /// <summary>Gets a snapshot of resources in the main domain.</summary>
    /// <returns>Registered resources in registration order.</returns>
    public static Translation[] GetTranslations() => GetOrAddDomain(string.Empty).GetTranslations();

    /// <summary>Gets matching resources from the main domain.</summary>
    /// <param name="locale">The requested culture name.</param>
    /// <param name="exact">Whether to require an exact normalized match.</param>
    /// <returns>An independent array of borrowed resources.</returns>
    public static Translation[] FindTranslations(string locale, bool exact) =>
        GetOrAddDomain(string.Empty).FindTranslations(locale, exact);

    /// <summary>Gets the closest matching resource in the main domain.</summary>
    /// <param name="locale">The requested culture name.</param>
    /// <returns>A borrowed resource, or null.</returns>
    public static Translation? GetTranslationObject(string locale) =>
        GetOrAddDomain(string.Empty).GetTranslationObject(locale);

    /// <summary>Reports whether the main domain contains a resource by identity.</summary>
    /// <param name="translation">The resource to inspect.</param>
    /// <returns>True when registered.</returns>
    public static bool HasTranslation(Translation translation) => GetOrAddDomain(string.Empty).HasTranslation(translation);

    /// <summary>Reports whether the main domain has a catalog matching a locale.</summary>
    /// <param name="locale">The requested culture name.</param>
    /// <param name="exact">Whether to require an exact normalized match.</param>
    /// <returns>True when a matching resource exists.</returns>
    public static bool HasTranslationForLocale(string locale, bool exact) =>
        GetOrAddDomain(string.Empty).HasTranslationForLocale(locale, exact);

    /// <summary>Gets distinct locale names of live resources in the main domain.</summary>
    /// <returns>An independent array of locale names.</returns>
    public static string[] GetLoadedLocales()
    {
        var locales = new HashSet<string>(StringComparer.Ordinal);
        foreach (var translation in GetTranslations())
        {
            try { locales.Add(translation.Locale); }
            catch (ObjectDisposedException) when (translation.IsDisposed) { }
        }
        return [.. locales];
    }

    /// <summary>Gets or sets pseudolocalization for the main domain.</summary>
    public static bool PseudolocalizationEnabled
    {
        get => GetOrAddDomain(string.Empty).PseudolocalizationEnabled;
        set => GetOrAddDomain(string.Empty).PseudolocalizationEnabled = value;
    }

    /// <summary>Pseudolocalizes text using the main domain's options.</summary>
    /// <param name="message">The source text.</param>
    /// <returns>The transformed text.</returns>
    public static string Pseudolocalize(string message) => GetOrAddDomain(string.Empty).Pseudolocalize(message);

    /// <summary>Reloads the main domain's pseudolocalization transforms from active project settings.</summary>
    /// <remarks>The runtime enablement switch is unchanged; the project switch is sampled when an Engine run starts.
    /// This managed reload does not reload asset remaps or notify an active scene of a translation change.</remarks>
    public static void ReloadPseudolocalization()
    {
        var settings = ProjectSettings.Service;
        var domain = GetOrAddDomain(string.Empty);
        domain.PseudolocalizationAccentsEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationReplaceWithAccents);
        domain.PseudolocalizationDoubleVowelsEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationDoubleVowels);
        domain.PseudolocalizationFakeBIDIEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationFakeBIDI);
        domain.PseudolocalizationOverrideEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationOverride);
        domain.PseudolocalizationExpansionRatio = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationExpansionRatio);
        domain.PseudolocalizationPrefix = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationPrefix);
        domain.PseudolocalizationSuffix = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationSuffix);
        domain.PseudolocalizationSkipPlaceholdersEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationSkipPlaceholders);
    }

    internal static void LoadProjectLocalization()
    {
        var settings = ProjectSettings.Service;
        var test = settings.GetWithOverrideCore(ProjectSettings.LocaleTest).Trim();
        var fallback = settings.GetWithOverrideCore(ProjectSettings.LocaleFallback).Trim();
        var culture = test.Length == 0 ? CultureInfo.CurrentUICulture : CultureInfo.GetCultureInfo(test.Replace('_', '-'));
        var fallbackCulture = fallback.Length == 0 ? null : CultureInfo.GetCultureInfo(fallback.Replace('_', '-'));
        ReloadPseudolocalization();
        lock (Gate)
        {
            _culture = culture;
            _fallbackCulture = fallbackCulture;
        }
        PseudolocalizationEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationEnabled);
    }

    /// <summary>Resolves a singular message for the selected culture, nearby catalog locales and the project fallback.</summary>
    /// <param name="domain">The case-sensitive translation domain.</param>
    /// <param name="message">The source message.</param>
    /// <param name="context">An optional disambiguation context. Null is equivalent to an empty string.</param>
    /// <returns>The first matching direct or resource translation, or <paramref name="message"/> when none exists or translation is disabled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="domain"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    public static string Translate(string domain, string message, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(message);

        if (!Enabled)
            return message;

        CultureInfo culture;
        TranslationDomain? catalogDomain;
        lock (Gate)
        {
            culture = _culture;
            Domains.TryGetValue(domain, out catalogDomain);
        }
        if (catalogDomain is not null)
        {
            try
            {
                if (!catalogDomain.Enabled) return message;
                culture = catalogDomain.EffectiveCulture;
            }
            catch (ObjectDisposedException) when (catalogDomain.IsDisposed) { catalogDomain = null; }
        }
        var translated = FindSingular(culture, domain, message, context ?? string.Empty, catalogDomain);
        if (translated is null && FallbackCulture is { } fallback)
            translated = FindSingular(fallback, domain, message, context ?? string.Empty, catalogDomain);
        try { return catalogDomain is null ? translated ?? message : catalogDomain.ApplyPseudo(translated ?? message); }
        catch (ObjectDisposedException) when (catalogDomain?.IsDisposed == true) { return translated ?? message; }
    }

    /// <summary>Resolves a plural message for the selected culture, nearby catalog locales and the project fallback.</summary>
    /// <param name="domain">The case-sensitive translation domain.</param>
    /// <param name="singular">The source singular form.</param>
    /// <param name="plural">The source plural form.</param>
    /// <param name="count">The quantity passed to the matching selector.</param>
    /// <param name="context">An optional disambiguation context. Null is equivalent to an empty string.</param>
    /// <returns>
    /// The selected translation. Without a matching entry, <paramref name="singular"/> is returned only when
    /// <paramref name="count"/> equals <c>1</c>; otherwise <paramref name="plural"/> is returned.
    /// </returns>
    /// <remarks>Direct selectors execute under the server lock; resource selectors and overrides execute outside it.</remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="domain"/>, <paramref name="singular"/>, or <paramref name="plural"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">A direct selector returns null, or a resource cannot select a valid plural form.</exception>
    /// <exception cref="Exception">A matching plural selector throws.</exception>
    public static string TranslatePlural(
        string domain,
        string singular,
        string plural,
        long count,
        string? context = null)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(singular);
        ArgumentNullException.ThrowIfNull(plural);

        if (!Enabled)
            return count == 1 ? singular : plural;

        CultureInfo culture;
        TranslationDomain? catalogDomain;
        lock (Gate)
        {
            culture = _culture;
            Domains.TryGetValue(domain, out catalogDomain);
        }
        if (catalogDomain is not null)
        {
            try
            {
                if (!catalogDomain.Enabled) return count == 1 ? singular : plural;
                culture = catalogDomain.EffectiveCulture;
            }
            catch (ObjectDisposedException) when (catalogDomain.IsDisposed) { catalogDomain = null; }
        }
        var translated = FindPlural(culture, domain, singular, plural, count, context ?? string.Empty, catalogDomain);
        if (translated is null && FallbackCulture is { } fallback)
            translated = FindPlural(fallback, domain, singular, plural, count, context ?? string.Empty, catalogDomain);
        return translated ?? (count == 1 ? singular : plural);
    }

    private static string? FindSingular(CultureInfo culture, string domain, string message, string context, TranslationDomain? catalogDomain)
    {
        foreach (var cultureName in TranslationDomain.CultureChain(culture))
        {
            string? direct;
            lock (Gate)
                Messages.TryGetValue((cultureName, domain, context, message), out direct);
            if (direct is not null) return direct;
            if (catalogDomain is not null)
            {
                try
                {
                    var translation = catalogDomain.FindMessage(cultureName, message, context, exact: true);
                    if (translation.Length != 0) return translation;
                }
                catch (ObjectDisposedException) when (catalogDomain.IsDisposed) { return null; }
            }
        }
        if (catalogDomain is not null)
        {
            try
            {
                var translation = catalogDomain.FindMessage(culture.Name, message, context);
                if (translation.Length != 0) return translation;
            }
            catch (ObjectDisposedException) when (catalogDomain.IsDisposed) { }
        }
        return null;
    }

    private static string? FindPlural(CultureInfo culture, string domain, string singular, string plural, long count, string context, TranslationDomain? catalogDomain)
    {
        foreach (var cultureName in TranslationDomain.CultureChain(culture))
        {
            lock (Gate)
                if (Plurals.TryGetValue((cultureName, domain, context, singular, plural), out var selector))
                    return selector(count) ?? throw new InvalidOperationException("A plural translation selector returned null.");
            if (catalogDomain is not null)
            {
                try
                {
                    var translation = catalogDomain.FindPluralMessage(cultureName, singular, plural, count, context, exact: true);
                    if (translation.Length != 0) return translation;
                }
                catch (ObjectDisposedException) when (catalogDomain.IsDisposed) { return null; }
            }
        }
        if (catalogDomain is not null)
        {
            try
            {
                var translation = catalogDomain.FindPluralMessage(culture.Name, singular, plural, count, context);
                if (translation.Length != 0) return translation;
            }
            catch (ObjectDisposedException) when (catalogDomain.IsDisposed) { }
        }
        return null;
    }

    /// <summary>Removes all direct translations and borrowed resource registrations without disposing resources.</summary>
    /// <remarks>The selected <see cref="Culture"/> and <see cref="Enabled"/> state are not changed.</remarks>
    public static void Clear()
    {
        lock (Gate)
        {
            Messages.Clear();
            Plurals.Clear();
            foreach (var domain in Domains.Values)
            {
                try { domain.Clear(); }
                catch (ObjectDisposedException) when (domain.IsDisposed) { }
            }
        }
    }

    private static TranslationDomain EnsureDomainUnderLock(string name)
    {
        if (Domains.TryGetValue(name, out var domain) && !domain.IsDisposed) return domain;
        domain = new TranslationDomain();
        domain.SetRegisteredName(name);
        domain.Disposed += OnDomainDisposed;
        Domains[name] = domain;
        return domain;
    }

    private static void OnDomainDisposed(ElectronObject resource)
    {
        lock (Gate)
        {
            foreach (var name in Domains.Where(pair => ReferenceEquals(pair.Value, resource)).Select(pair => pair.Key).ToArray())
            {
                Domains.Remove(name);
                RemoveDirectEntriesUnderLock(name);
            }
        }
    }

    private static void RemoveDirectEntriesUnderLock(string name)
    {
        foreach (var key in Messages.Keys.Where(key => key.Domain == name).ToArray()) Messages.Remove(key);
        foreach (var key in Plurals.Keys.Where(key => key.Domain == name).ToArray()) Plurals.Remove(key);
    }
}
