using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Electron2D;

/// <summary>Stores and resolves process-wide in-memory translations for the selected UI culture.</summary>
/// <remarks>
/// Static operations use the process-lifetime object registered with <see cref="Engine"/>.
/// The object supports typed property inspection and rejects caller disposal.
/// Lookups use exact, case-sensitive domain, context, and source-message keys. Default-aware locale scores rank
/// selected/parent direct entries and borrowed resource catalogs before the configured fallback. Direct entries win
/// score ties; later resource registrations win equal resource scores. Parsed locale identities are weakly retained
/// independently of live message results. Fixed locale metadata and integer plural rules support resource catalogs.
/// </remarks>
public sealed partial class TranslationServer : ElectronObject
{
    private sealed record LocaleParts(string Language, string Script, string Region, string Variant);
    private static readonly PropertyDescriptor[] TranslationProperties =
    [
        new PropertyDescriptor<TranslationServer, CultureInfo>(nameof(Culture),
            static _ => Culture, static (_, value) => Culture = value),
        new PropertyDescriptor<TranslationServer, bool>(nameof(PseudolocalizationEnabled),
            static _ => PseudolocalizationEnabled, static (_, value) => PseudolocalizationEnabled = value),
    ];

    internal static TranslationServer RegisteredService { get; } = new();

    // Engine registration and descriptor discovery can expose the object before the first state access.
    private static readonly Lazy<TranslationServer> InitializedService = new(static () =>
    {
        var service = RegisteredService;
        service._culture = CultureInfo.CurrentUICulture;
        return service;
    });
    private static TranslationServer Service => InitializedService.Value;

    private readonly object _gate = new();
    private readonly ConditionalWeakTable<string, LocaleParts> _localeIdentities = new();
    private readonly Dictionary<(string Culture, string Domain, string Context, string Message), string> _messages = new();
    private readonly Dictionary<(string Culture, string Domain, string Context, string Singular, string Plural), Func<long, string>> _plurals = new();
    private readonly Dictionary<string, TranslationDomain> _domains = new(StringComparer.Ordinal);

    private CultureInfo _culture = null!; // Set by the lazy initializer before service operations can read it.
    private CultureInfo? _fallbackCulture = CultureInfo.GetCultureInfo("en");
    private int _enabled = 1;

    private TranslationServer() { }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(TranslationProperties);

    /// <inheritdoc />
    /// <remarks>The process-wide translation service has process lifetime and cannot be disposed.</remarks>
    protected override void ValidateDisposal() =>
        throw new InvalidOperationException("The process-wide TranslationServer service cannot be disposed.");

    /// <summary>Gets or sets whether translation lookup is enabled globally.</summary>
    /// <value><see langword="true"/> by default. When false, lookup returns source text without consulting the catalogs.</value>
    internal static bool Enabled
    {
        get => Service.EnabledCore;
        set => Service.EnabledCore = value;
    }

    private bool EnabledCore
    {
        get => Volatile.Read(ref _enabled) != 0;
        set => Volatile.Write(ref _enabled, value ? 1 : 0);
    }

    /// <summary>Gets or sets the culture used for subsequent translation lookups.</summary>
    /// <value>The process-wide lookup culture. Before Engine startup, the first access to service state captures
    /// <see cref="CultureInfo.CurrentUICulture"/>. Engine startup samples it again unless a project test locale is configured.</value>
    /// <remarks>A different culture name reloads associated localized resources, then synchronously notifies
    /// <see cref="Engine.MainLoop"/> after commitment and release of the setter's catalog lock. Equal-name assignment
    /// retains the supplied managed culture without another reload or notification. Configured file reloads allocate
    /// and may block. An active SceneTree requires its owner; independent library trees are notified explicitly.
    /// Resource errors are collected after later resources are attempted and application notification still runs.
    /// A failure does not roll back the committed culture or completed resource copies.</remarks>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null"/>.</exception>
    /// <exception cref="Exception">The active application loop rejects notification or an observer fails after commitment.</exception>
    public static CultureInfo Culture
    {
        get => Service.CultureCore;
        set => Service.CultureCore = value;
    }

    private CultureInfo CultureCore
    {
        get
        {
            lock (_gate)
                return _culture;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            bool changed;
            lock (_gate)
            {
                changed = !string.Equals(_culture.Name, value.Name, StringComparison.Ordinal);
                _culture = value;
            }
            if (changed) NotifyTranslationChanged();
        }
    }

    internal static CultureInfo? FallbackCulture
    {
        get => Service.FallbackCultureCore;
        set => Service.FallbackCultureCore = value;
    }

    private CultureInfo? FallbackCultureCore
    {
        get { lock (_gate) return _fallbackCulture; }
        set { lock (_gate) _fallbackCulture = value; }
    }

    /// <summary>Scores how closely two default-aware catalog locale identities match.</summary>
    /// <param name="localeA">The requested locale.</param>
    /// <param name="localeB">The candidate locale.</param>
    /// <returns>Ten for the same normalized locale, zero for different languages, or a score around five adjusted for matching script, region, and variant.</returns>
    /// <exception cref="ArgumentNullException">A locale is null.</exception>
    public static int CompareLocales(string localeA, string localeB)
    {
        ArgumentNullException.ThrowIfNull(localeA);
        ArgumentNullException.ThrowIfNull(localeB);
        if (localeA == localeB) return 10;
        var service = Service;
        var a = service.ParseLocale(localeA);
        var b = service.ParseLocale(localeB);
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
    /// <remarks>Reuses the domain's internal catalog snapshot and compares prepared default-aware locale identities without allocating after preparation. Catalog locale changes are observed on every lookup.</remarks>
    public static string GetToolLocale() =>
        Service.GetToolLocaleCore();

    private string GetToolLocaleCore()
    {
        var requested = CultureCore.Name;
        var bestScore = 0;
        string? best = null;
        foreach (var translation in GetOrAddDomainCore(string.Empty).GetLookupSnapshot())
        {
            try
            {
                var candidate = translation.Locale;
                var score = CompareLocales(requested, candidate);
                if (score <= 0 || score < bestScore) continue;
                if (score == 10) return requested;
                best = candidate;
                bestScore = score;
            }
            catch (ObjectDisposedException) when (translation.IsDisposed) { }
        }
        return best ?? FallbackCultureCore?.Name ?? string.Empty;
    }

    private LocaleParts ParseLocale(string locale) => _localeIdentities.GetValue(locale, static key =>
    {
        var parsed = ParseCatalogLocale(key, true);
        return new(parsed.Language, parsed.Script, parsed.Region, parsed.Variant);
    });

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
        string? context = null) =>
        Service.AddTranslationCore(culture, domain, message, translation, context);

    private void AddTranslationCore(CultureInfo culture, string domain, string message, string translation, string? context)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(translation);

        lock (_gate)
        {
            EnsureDomainUnderLock(domain);
            _messages[(culture.Name, domain, context ?? string.Empty, message)] = translation;
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
        string? context = null) =>
        Service.AddPluralTranslationCore(culture, domain, singular, plural, selector, context);

    private void AddPluralTranslationCore(CultureInfo culture, string domain, string singular, string plural, Func<long, string> selector, string? context)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(singular);
        ArgumentNullException.ThrowIfNull(plural);
        ArgumentNullException.ThrowIfNull(selector);

        lock (_gate)
        {
            EnsureDomainUnderLock(domain);
            _plurals[(culture.Name, domain, context ?? string.Empty, singular, plural)] = selector;
        }
    }

    /// <summary>Registers a live translation resource for a domain.</summary>
    /// <param name="translation">The borrowed resource; its subsequent edits are visible to lookups.</param>
    /// <param name="domain">The case-sensitive domain, empty by default.</param>
    /// <remarks>Registration compares references. Repeating a registration keeps its position; removing and adding
    /// appends it. The latest registration wins equal locale scores. Disposing the borrowed resource removes it.</remarks>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public static void AddTranslation(Translation translation, string domain = "") =>
        Service.AddTranslationCore(translation, domain);

    private void AddTranslationCore(Translation translation, string domain)
    {
        ArgumentNullException.ThrowIfNull(translation);
        ArgumentNullException.ThrowIfNull(domain);
        if (translation.IsDisposed) throw new ObjectDisposedException(nameof(translation));
        GetOrAddDomainCore(domain).AddTranslation(translation);
    }

    /// <summary>Unregisters the exact translation resource from a domain without disposing it.</summary>
    /// <param name="translation">The resource to remove.</param>
    /// <param name="domain">The registration domain.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static void RemoveTranslation(Translation translation, string domain = "") =>
        Service.RemoveTranslationCore(translation, domain);

    private void RemoveTranslationCore(Translation translation, string domain)
    {
        ArgumentNullException.ThrowIfNull(translation);
        ArgumentNullException.ThrowIfNull(domain);
        TranslationDomain? registered;
        lock (_gate) _domains.TryGetValue(domain, out registered);
        if (registered is not null)
        {
            try { registered.RemoveTranslation(translation); }
            catch (ObjectDisposedException) when (registered.IsDisposed) { }
        }
    }

    /// <summary>Returns a registered translation domain, creating it if necessary.</summary>
    /// <param name="name">The exact case-sensitive domain name; empty selects the main domain.</param>
    /// <returns>A caller-visible borrowed domain; disposing it removes its registration.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    public static TranslationDomain GetOrAddDomain(string name) =>
        Service.GetOrAddDomainCore(name);

    private TranslationDomain GetOrAddDomainCore(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (_gate) return EnsureDomainUnderLock(name);
    }

    /// <summary>Reports whether a named domain is registered.</summary>
    /// <param name="name">The exact domain name.</param>
    /// <returns>True for the main domain or a live custom domain.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    public static bool HasDomain(string name) =>
        Service.HasDomainCore(name);

    private bool HasDomainCore(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (_gate) return name.Length == 0 || _domains.TryGetValue(name, out var domain) && !domain.IsDisposed;
    }

    /// <summary>Removes a custom domain and its direct entries without disposing the domain or its catalogs.</summary>
    /// <param name="name">A nonempty exact domain name.</param>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">The name selects the main domain.</exception>
    public static void RemoveDomain(string name) =>
        Service.RemoveDomainCore(name);

    private void RemoveDomainCore(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0) throw new ArgumentException("The main translation domain cannot be removed.", nameof(name));
        lock (_gate)
        {
            if (_domains.Remove(name, out var removed))
            {
                removed.Disposed -= OnDomainDisposed;
                if (!removed.IsDisposed) removed.SetRegisteredName(null);
            }
            RemoveDirectEntriesUnderLock(name);
        }
    }

    /// <summary>Gets a snapshot of resources in the main domain.</summary>
    /// <returns>An independent array of borrowed resources in registration order, excluding direct entries.</returns>
    public static Translation[] GetTranslations() => GetOrAddDomain(string.Empty).GetTranslations();

    /// <summary>Gets matching resources from the main domain.</summary>
    /// <param name="locale">Locale text; exact queries use the stored <see cref="Translation.Locale"/> spelling.</param>
    /// <param name="exact">True requires ordinal equality; false accepts a positive <see cref="CompareLocales"/> score.</param>
    /// <returns>An independent array of borrowed resources in registration order.</returns>
    /// <remarks>Shares the live predicate of <see cref="HasTranslationForLocale"/>. Nonexact queries apply fixed aliases
    /// and script/region defaults. Direct entries, fallback, enablement and locale override do not affect membership.</remarks>
    /// <exception cref="ArgumentNullException">The locale is null.</exception>
    public static Translation[] FindTranslations(string locale, bool exact) =>
        GetOrAddDomain(string.Empty).FindTranslations(locale, exact);

    /// <summary>Reports whether the main domain contains a resource by identity.</summary>
    /// <param name="translation">The resource to inspect.</param>
    /// <returns>True when registered.</returns>
    /// <exception cref="ArgumentNullException">The resource is null.</exception>
    public static bool HasTranslation(Translation translation) => GetOrAddDomain(string.Empty).HasTranslation(translation);

    /// <summary>Reports whether the main domain has a catalog matching a locale.</summary>
    /// <param name="locale">Locale text; exact queries use the stored <see cref="Translation.Locale"/> spelling.</param>
    /// <param name="exact">True requires ordinal equality; false accepts a positive <see cref="CompareLocales"/> score.</param>
    /// <returns>True when <see cref="FindTranslations"/> would return at least one resource.</returns>
    /// <exception cref="ArgumentNullException">The locale is null.</exception>
    public static bool HasTranslationForLocale(string locale, bool exact) =>
        GetOrAddDomain(string.Empty).HasTranslationForLocale(locale, exact);

    /// <summary>Gets distinct locale names of live resources in the main domain.</summary>
    /// <returns>An independent array of stored locale names, in first registration order with ordinal duplicates removed.</returns>
    /// <remarks>Reflects live locale changes and catalog retirement. Direct entries and other domains are excluded.</remarks>
    public static string[] GetLoadedLocales()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var locales = new List<string>();
        foreach (var translation in GetTranslations())
        {
            try { var locale = translation.Locale; if (seen.Add(locale)) locales.Add(locale); }
            catch (ObjectDisposedException) when (translation.IsDisposed) { }
        }
        return [.. locales];
    }

    /// <summary>Gets or sets pseudolocalization for the main domain.</summary>
    /// <value>The main domain's runtime transformation switch.</value>
    /// <remarks>Every assignment, including an equal value, reloads associated localized resources and synchronously
    /// notifies the attached application loop after commitment. File reloads allocate and may block. Active scene
    /// delivery requires its owner thread.</remarks>
    /// <exception cref="Exception">Resource refresh or application notification fails after the switch is committed.</exception>
    public static bool PseudolocalizationEnabled
    {
        get => Service.PseudolocalizationEnabledCore;
        set => Service.PseudolocalizationEnabledCore = value;
    }

    private bool PseudolocalizationEnabledCore
    {
        get => GetOrAddDomainCore(string.Empty).PseudolocalizationEnabled;
        set { GetOrAddDomainCore(string.Empty).PseudolocalizationEnabled = value; NotifyTranslationChanged(); }
    }

    /// <summary>Pseudolocalizes text using the main domain's options.</summary>
    /// <param name="message">The source text.</param>
    /// <returns>The transformed text.</returns>
    public static string Pseudolocalize(string message) => GetOrAddDomain(string.Empty).Pseudolocalize(message);

    /// <summary>Reloads the main domain's pseudolocalization transforms from active project settings.</summary>
    /// <remarks>The runtime enablement switch is unchanged; the project switch is sampled when an Engine run starts.
    /// After applying every option, the attached application loop receives a synchronous translation notification.
    /// Associated localized resources reload before that notification. Active scene delivery requires its owner
    /// thread. Configured file reloads allocate and may block; errors preserve completed changes.</remarks>
    /// <exception cref="Exception">Option preparation or application notification fails; committed options remain visible.</exception>
    public static void ReloadPseudolocalization() =>
        Service.ReloadPseudolocalizationCore();

    private void ReloadPseudolocalizationCore()
    {
        ApplyProjectPseudolocalization();
        NotifyTranslationChanged();
    }

    private static void NotifyTranslationChanged()
    {
        var loop = Engine.MainLoop;
        if (loop is SceneTree { IsOwnerThread: false }) { loop.Notify(MainLoop.NotificationTranslationChanged); return; }
        Exception? failure = null;
        try { ResourceLoader.ReloadTranslationRemaps(); } catch (Exception error) { failure = error; }
        try { Engine.MainLoop?.Notify(MainLoop.NotificationTranslationChanged); }
        catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void ApplyProjectPseudolocalization()
    {
        var settings = ProjectSettings.Service;
        var domain = GetOrAddDomainCore(string.Empty);
        domain.PseudolocalizationAccentsEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationReplaceWithAccents);
        domain.PseudolocalizationDoubleVowelsEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationDoubleVowels);
        domain.PseudolocalizationFakeBIDIEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationFakeBIDI);
        domain.PseudolocalizationOverrideEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationOverride);
        domain.PseudolocalizationExpansionRatio = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationExpansionRatio);
        domain.PseudolocalizationPrefix = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationPrefix);
        domain.PseudolocalizationSuffix = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationSuffix);
        domain.PseudolocalizationSkipPlaceholdersEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationSkipPlaceholders);
    }

    internal static void LoadProjectLocalization() =>
        Service.LoadProjectLocalizationCore();

    private void LoadProjectLocalizationCore()
    {
        var settings = ProjectSettings.Service;
        var test = settings.GetWithOverrideCore(ProjectSettings.LocaleTest).Trim();
        var fallback = settings.GetWithOverrideCore(ProjectSettings.LocaleFallback).Trim();
        var culture = test.Length == 0 ? CultureInfo.CurrentUICulture : GetRuntimeLocale(test);
        var fallbackCulture = fallback.Length == 0 ? null : GetRuntimeLocale(fallback);
        ApplyProjectPseudolocalization();
        lock (_gate)
        {
            _culture = culture;
            _fallbackCulture = fallbackCulture;
        }
        GetOrAddDomainCore(string.Empty).PseudolocalizationEnabled = settings.GetWithOverrideCore(ProjectSettings.PseudolocalizationEnabled);
        NotifyTranslationChanged();
    }

    /// <summary>Resolves a singular message for the selected culture, nearby catalog locales and the project fallback.</summary>
    /// <param name="domain">The case-sensitive translation domain.</param>
    /// <param name="message">The source message.</param>
    /// <param name="context">An optional disambiguation context. Null is equivalent to an empty string.</param>
    /// <returns>The highest-ranked direct or resource translation, or <paramref name="message"/> when none exists or translation is disabled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="domain"/> or <paramref name="message"/> is <see langword="null"/>.</exception>
    public static string Translate(string domain, string message, string? context = null) =>
        Service.TranslateCore(domain, message, context);

    private string TranslateCore(string domain, string message, string? context)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(message);

        if (!EnabledCore)
            return message;

        CultureInfo culture;
        TranslationDomain? catalogDomain;
        lock (_gate)
        {
            culture = _culture;
            _domains.TryGetValue(domain, out catalogDomain);
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
        if (translated is null && FallbackCultureCore is { } fallback)
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
        string? context = null) =>
        Service.TranslatePluralCore(domain, singular, plural, count, context);

    private string TranslatePluralCore(string domain, string singular, string plural, long count, string? context)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(singular);
        ArgumentNullException.ThrowIfNull(plural);

        if (!EnabledCore)
            return count == 1 ? singular : plural;

        CultureInfo culture;
        TranslationDomain? catalogDomain;
        lock (_gate)
        {
            culture = _culture;
            _domains.TryGetValue(domain, out catalogDomain);
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
        if (translated is null && FallbackCultureCore is { } fallback)
            translated = FindPlural(fallback, domain, singular, plural, count, context ?? string.Empty, catalogDomain);
        return translated ?? (count == 1 ? singular : plural);
    }

    private string? FindSingular(CultureInfo culture, string domain, string message, string context, TranslationDomain? catalogDomain)
    {
        string? direct = null;
        var bestScore = 0;
        foreach (var cultureName in global::Electron2D.TranslationDomain.CultureChain(culture))
        {
            string? candidate;
            lock (_gate) _messages.TryGetValue((cultureName, domain, context, message), out candidate);
            if (candidate is null) continue;
            var score = cultureName.Length == 0 && culture.Name.Length != 0 ? 1 : CompareLocales(culture.Name, cultureName);
            if (score <= bestScore) continue;
            direct = candidate; bestScore = score;
            if (score == 10) break;
        }
        if (catalogDomain is not null)
        {
            try
            {
                var translation = catalogDomain.FindMessage(culture.Name, message, context, bestScore);
                if (translation.Length != 0) return translation;
            }
            catch (ObjectDisposedException) when (catalogDomain.IsDisposed) { }
        }
        return direct;
    }

    private string? FindPlural(CultureInfo culture, string domain, string singular, string plural, long count, string context, TranslationDomain? catalogDomain)
    {
        Func<long, string>? selector = null;
        var bestScore = 0;
        foreach (var cultureName in global::Electron2D.TranslationDomain.CultureChain(culture))
        {
            Func<long, string>? candidate;
            lock (_gate) _plurals.TryGetValue((cultureName, domain, context, singular, plural), out candidate);
            if (candidate is null) continue;
            var score = cultureName.Length == 0 && culture.Name.Length != 0 ? 1 : CompareLocales(culture.Name, cultureName);
            if (score <= bestScore) continue;
            selector = candidate; bestScore = score;
            if (score == 10) break;
        }
        if (catalogDomain is not null)
        {
            try
            {
                var translation = catalogDomain.FindPluralMessage(culture.Name, singular, plural, count, context, bestScore);
                if (translation.Length != 0) return translation;
            }
            catch (ObjectDisposedException) when (catalogDomain.IsDisposed) { }
        }
        if (selector is null) return null;
        lock (_gate) return selector(count) ?? throw new InvalidOperationException("A plural translation selector returned null.");
    }

    /// <summary>Removes all direct translations and borrowed resource registrations without disposing resources.</summary>
    /// <remarks>The selected <see cref="Culture"/>, enablement, domain identities and their settings are preserved.
    /// Detached domains are independent and are not cleared.</remarks>
    public static void Clear() =>
        Service.ClearCore();

    private void ClearCore()
    {
        lock (_gate)
        {
            _messages.Clear();
            _plurals.Clear();
            foreach (var domain in _domains.Values)
            {
                try { domain.Clear(); }
                catch (ObjectDisposedException) when (domain.IsDisposed) { }
            }
        }
    }

    private TranslationDomain EnsureDomainUnderLock(string name)
    {
        if (_domains.TryGetValue(name, out var domain) && !domain.IsDisposed) return domain;
        domain = new TranslationDomain();
        domain.SetRegisteredName(name);
        domain.Disposed += OnDomainDisposed;
        _domains[name] = domain;
        return domain;
    }

    private void OnDomainDisposed(ElectronObject resource)
    {
        lock (_gate)
        {
            foreach (var name in _domains.Where(pair => ReferenceEquals(pair.Value, resource)).Select(pair => pair.Key).ToArray())
            {
                _domains.Remove(name);
                RemoveDirectEntriesUnderLock(name);
            }
        }
    }

    private void RemoveDirectEntriesUnderLock(string name)
    {
        foreach (var key in _messages.Keys.Where(key => key.Domain == name).ToArray()) _messages.Remove(key);
        foreach (var key in _plurals.Keys.Where(key => key.Domain == name).ToArray()) _plurals.Remove(key);
    }
}
