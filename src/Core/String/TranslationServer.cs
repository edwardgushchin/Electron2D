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
    private static readonly List<(string Domain, Translation Catalog)> Catalogs = [];

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
            Messages[(culture.Name, domain, context ?? string.Empty, message)] = translation;
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
            Plurals[(culture.Name, domain, context ?? string.Empty, singular, plural)] = selector;
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
        lock (Gate)
        {
            if (translation.IsDisposed) throw new ObjectDisposedException(nameof(translation));
            if (Catalogs.Any(entry => entry.Domain == domain && ReferenceEquals(entry.Catalog, translation))) return;
            if (!Catalogs.Any(entry => ReferenceEquals(entry.Catalog, translation)))
                translation.Disposed += OnCatalogDisposed;
            Catalogs.Add((domain, translation));
            if (translation.IsDisposed)
            {
                RemoveCatalogUnderLock(translation, domain);
                throw new ObjectDisposedException(nameof(translation));
            }
        }
    }

    /// <summary>Unregisters a translation resource from a domain without disposing it.</summary>
    /// <param name="translation">The resource to remove.</param>
    /// <param name="domain">The registration domain.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static void RemoveTranslation(Translation translation, string domain = "")
    {
        ArgumentNullException.ThrowIfNull(translation);
        ArgumentNullException.ThrowIfNull(domain);
        lock (Gate) RemoveCatalogUnderLock(translation, domain);
    }

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
        Translation[] catalogs;
        lock (Gate)
        {
            culture = _culture;
            catalogs = Catalogs.Where(entry => entry.Domain == domain).Select(entry => entry.Catalog).ToArray();
        }
        foreach (var cultureName in GetCultureChain(culture))
        {
            lock (Gate)
            {
                if (Messages.TryGetValue((cultureName, domain, context ?? string.Empty, message), out var translation))
                    return translation;
            }
            for (var i = catalogs.Length - 1; i >= 0; i--)
            {
                try
                {
                    var catalog = catalogs[i];
                    if (catalog.Locale != cultureName) continue;
                    var translation = catalog.GetMessage(message, context ?? string.Empty);
                    if (translation.Length != 0) return translation;
                }
                catch (ObjectDisposedException) when (catalogs[i].IsDisposed) { }
            }
        }

        return message;
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
        Translation[] catalogs;
        lock (Gate)
        {
            culture = _culture;
            catalogs = Catalogs.Where(entry => entry.Domain == domain).Select(entry => entry.Catalog).ToArray();
        }
        foreach (var cultureName in GetCultureChain(culture))
        {
            lock (Gate)
            {
                if (Plurals.TryGetValue((cultureName, domain, context ?? string.Empty, singular, plural), out var selector))
                    return selector(count) ?? throw new InvalidOperationException("A plural translation selector returned null.");
            }
            for (var i = catalogs.Length - 1; i >= 0; i--)
            {
                try
                {
                    var catalog = catalogs[i];
                    if (catalog.Locale != cultureName) continue;
                    var translation = catalog.GetPluralMessage(singular, plural, count, context ?? string.Empty);
                    if (translation.Length != 0) return translation;
                }
                catch (ObjectDisposedException) when (catalogs[i].IsDisposed) { }
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
            foreach (var catalog in Catalogs.Select(entry => entry.Catalog).Distinct())
                catalog.Disposed -= OnCatalogDisposed;
            Catalogs.Clear();
        }
    }

    private static void OnCatalogDisposed(ElectronObject resource)
    {
        lock (Gate) Catalogs.RemoveAll(entry => ReferenceEquals(entry.Catalog, resource));
    }

    private static void RemoveCatalogUnderLock(Translation translation, string domain)
    {
        Catalogs.RemoveAll(entry => entry.Domain == domain && ReferenceEquals(entry.Catalog, translation));
        if (!Catalogs.Any(entry => ReferenceEquals(entry.Catalog, translation)))
            translation.Disposed -= OnCatalogDisposed;
    }

    private static IEnumerable<string> GetCultureChain(CultureInfo culture)
    {
        while (true)
        {
            yield return culture.Name;

            if (culture.Equals(CultureInfo.InvariantCulture))
                yield break;

            culture = culture.Parent;
        }
    }
}
