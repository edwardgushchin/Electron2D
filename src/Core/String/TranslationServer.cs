using System.Globalization;
using System.Threading;

namespace Electron2D;

/// <summary>Stores and resolves process-wide in-memory translations for the selected UI culture.</summary>
/// <remarks>
/// Lookups use exact, case-sensitive domain, context, and source-message keys, then walk from the selected culture
/// through its parents to the invariant culture. This intentionally small typed service does not load catalogs or
/// implement CLDR plural rules.
/// </remarks>
public static class TranslationServer
{
    private static readonly object Gate = new();
    private static readonly Dictionary<(string Culture, string Domain, string Context, string Message), string> Messages = new();
    private static readonly Dictionary<(string Culture, string Domain, string Context, string Singular, string Plural), Func<long, string>> Plurals = new();

    private static CultureInfo _culture = CultureInfo.CurrentUICulture;
    private static int _enabled = 1;

    /// <summary>Gets or sets whether translation lookup is enabled globally.</summary>
    /// <value><see langword="true"/> by default. When false, lookup returns source text without consulting the catalogs.</value>
    public static bool Enabled
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

    /// <summary>Resolves a singular message for the current culture and its parent cultures.</summary>
    /// <param name="domain">The case-sensitive translation domain.</param>
    /// <param name="message">The source message.</param>
    /// <param name="context">An optional disambiguation context. Null is equivalent to an empty string.</param>
    /// <returns>The first matching translation, or <paramref name="message"/> when none exists or translation is disabled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="domain"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    public static string Translate(string domain, string message, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(message);

        if (!Enabled)
            return message;

        lock (Gate)
        {
            foreach (var cultureName in GetCultureChain(_culture))
            {
                if (Messages.TryGetValue((cultureName, domain, context ?? string.Empty, message), out var translation))
                    return translation;
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
    /// <remarks>The selector executes synchronously while the catalog lock is held and should complete quickly.</remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="domain"/>, <paramref name="singular"/>, or <paramref name="plural"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">A matching plural selector returns <see langword="null"/>.</exception>
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

        lock (Gate)
        {
            foreach (var cultureName in GetCultureChain(_culture))
            {
                if (Plurals.TryGetValue((cultureName, domain, context ?? string.Empty, singular, plural), out var selector))
                    return selector(count) ?? throw new InvalidOperationException("A plural translation selector returned null.");
            }
        }

        return count == 1 ? singular : plural;
    }

    /// <summary>Removes all singular and plural translation registrations.</summary>
    /// <remarks>The selected <see cref="Culture"/> and <see cref="Enabled"/> state are not changed.</remarks>
    public static void Clear()
    {
        lock (Gate)
        {
            Messages.Clear();
            Plurals.Clear();
        }
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
