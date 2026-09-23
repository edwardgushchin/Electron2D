using System.Globalization;
using System.Threading;

namespace Electron2D;

/// <summary>Stores and resolves process-wide in-memory translations for the selected UI culture.</summary>
/// <remarks>
/// Lookups use exact, case-sensitive domain, context, and source-message keys, then walk from the selected culture
/// through its parents to the invariant culture. Direct entries precede borrowed resource catalogs within a locale;
/// later resource registrations are queried first. This service does not load catalog files or implement CLDR rules.
/// </remarks>
public static class TranslationServer
{
    private static readonly object Gate = new();
    private static readonly Dictionary<(string Culture, string Domain, string Context, string Message), string> Messages = new();
    private static readonly Dictionary<(string Culture, string Domain, string Context, string Singular, string Plural), Func<long, string>> Plurals = new();
    private static readonly Dictionary<string, TranslationDomain> Domains = new(StringComparer.Ordinal);

    private static CultureInfo _culture = CultureInfo.CurrentUICulture;
    private static int _enabled = 1;

    /// <summary>Gets or sets whether translation lookup is enabled globally.</summary>
    /// <value><see langword="true"/> by default. When false, lookup returns source text without consulting the catalogs.</value>
    internal static bool Enabled
    {
        get => Volatile.Read(ref _enabled) != 0;
        set => Volatile.Write(ref _enabled, value ? 1 : 0);
    }

    /// <summary>Gets or sets the culture used for subsequent translation lookups.</summary>
    /// <value>The process-wide lookup culture, initialized from <see cref="CultureInfo.CurrentUICulture"/>.</value>
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

    /// <summary>Resolves a singular message for the current culture and its parent cultures.</summary>
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
        foreach (var cultureName in TranslationDomain.CultureChain(culture))
        {
            string? direct;
            lock (Gate)
                Messages.TryGetValue((cultureName, domain, context ?? string.Empty, message), out direct);
            if (direct is not null)
            {
                try { return catalogDomain is null ? direct : catalogDomain.ApplyPseudo(direct); }
                catch (ObjectDisposedException) when (catalogDomain?.IsDisposed == true) { return direct; }
            }
            if (catalogDomain is not null)
            {
                try
                {
                    var translation = catalogDomain.FindMessage(cultureName, message, context ?? string.Empty);
                    if (translation.Length != 0) return catalogDomain.ApplyPseudo(translation);
                }
                catch (ObjectDisposedException) when (catalogDomain.IsDisposed) { catalogDomain = null; }
            }
        }

        try { return catalogDomain is null ? message : catalogDomain.ApplyPseudo(message); }
        catch (ObjectDisposedException) when (catalogDomain?.IsDisposed == true) { return message; }
    }

    /// <summary>Resolves a plural message for the current culture and its parent cultures.</summary>
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
        foreach (var cultureName in TranslationDomain.CultureChain(culture))
        {
            lock (Gate)
            {
                if (Plurals.TryGetValue((cultureName, domain, context ?? string.Empty, singular, plural), out var selector))
                    return selector(count) ?? throw new InvalidOperationException("A plural translation selector returned null.");
            }
            if (catalogDomain is not null)
            {
                try
                {
                    var translation = catalogDomain.FindPluralMessage(cultureName, singular, plural, count, context ?? string.Empty);
                    if (translation.Length != 0) return translation;
                }
                catch (ObjectDisposedException) when (catalogDomain.IsDisposed) { catalogDomain = null; }
            }
        }

        return count == 1 ? singular : plural;
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
