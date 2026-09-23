using System.Globalization;
using System.Text;

namespace Electron2D;

/// <summary>Owns the registration and lookup policy for a set of borrowed translation resources.</summary>
/// <remarks>Catalogs remain caller-owned. Their edits and locale changes are visible without re-registration;
/// disposal unregisters them. Resource callbacks execute outside the domain lock.</remarks>
public sealed class TranslationDomain : ElectronObject
{
    private readonly object _gate = new();
    private readonly List<Translation> _translations = [];
    private string _localeOverride = string.Empty;
    private bool _enabled = true;
    private string? _registeredName;
    private PseudoOptions _pseudo = new(true, false, false, false, false, true, 0, "[", "]");

    private readonly record struct PseudoOptions(
        bool Accents, bool DoubleVowels, bool FakeBIDI, bool Override, bool Enabled,
        bool SkipPlaceholders, float ExpansionRatio, string Prefix, string Suffix);

    /// <summary>Creates an empty independent domain.</summary>
    public TranslationDomain() { }

    /// <summary>Gets or sets whether lookup is enabled for this domain.</summary>
    public bool Enabled
    {
        get { lock (_gate) { ThrowIfDisposed(); return _enabled; } }
        set { lock (_gate) { ThrowIfDisposed(); _enabled = value; } }
    }

    /// <summary>Gets or sets a culture name used instead of the process-wide UI culture; empty clears the override.</summary>
    public string LocaleOverride
    {
        get { lock (_gate) { ThrowIfDisposed(); return _localeOverride; } }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            var normalized = value.Length == 0 ? string.Empty : CultureInfo.GetCultureInfo(value.Replace('_', '-')).Name;
            lock (_gate) { ThrowIfDisposed(); _localeOverride = normalized; }
        }
    }

    /// <summary>Gets or sets whether singular results are pseudolocalized.</summary>
    public bool PseudolocalizationEnabled
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pseudo.Enabled; } }
        set { lock (_gate) { ThrowIfDisposed(); _pseudo = _pseudo with { Enabled = value }; } }
    }

    /// <summary>Gets or sets whether Latin letters receive accented substitutes.</summary>
    public bool PseudolocalizationAccentsEnabled
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pseudo.Accents; } }
        set { lock (_gate) { ThrowIfDisposed(); _pseudo = _pseudo with { Accents = value }; } }
    }

    /// <summary>Gets or sets whether unprotected ASCII vowels are doubled.</summary>
    public bool PseudolocalizationDoubleVowelsEnabled
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pseudo.DoubleVowels; } }
        set { lock (_gate) { ThrowIfDisposed(); _pseudo = _pseudo with { DoubleVowels = value }; } }
    }

    /// <summary>Gets or sets whether right-to-left override marks surround the result.</summary>
    public bool PseudolocalizationFakeBIDIEnabled
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pseudo.FakeBIDI; } }
        set { lock (_gate) { ThrowIfDisposed(); _pseudo = _pseudo with { FakeBIDI = value }; } }
    }

    /// <summary>Gets or sets whether unprotected characters are replaced with asterisks.</summary>
    public bool PseudolocalizationOverrideEnabled
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pseudo.Override; } }
        set { lock (_gate) { ThrowIfDisposed(); _pseudo = _pseudo with { Override = value }; } }
    }

    /// <summary>Gets or sets whether common percent placeholders are preserved by transformations.</summary>
    public bool PseudolocalizationSkipPlaceholdersEnabled
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pseudo.SkipPlaceholders; } }
        set { lock (_gate) { ThrowIfDisposed(); _pseudo = _pseudo with { SkipPlaceholders = value }; } }
    }

    /// <summary>Gets or sets the fraction of source length to add as underscore padding.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The ratio is negative or not finite.</exception>
    public float PseudolocalizationExpansionRatio
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pseudo.ExpansionRatio; } }
        set
        {
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate) { ThrowIfDisposed(); _pseudo = _pseudo with { ExpansionRatio = value }; }
        }
    }

    /// <summary>Gets or sets the prefix of pseudolocalized text.</summary>
    public string PseudolocalizationPrefix
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pseudo.Prefix; } }
        set { ArgumentNullException.ThrowIfNull(value); lock (_gate) { ThrowIfDisposed(); _pseudo = _pseudo with { Prefix = value }; } }
    }

    /// <summary>Gets or sets the suffix of pseudolocalized text.</summary>
    public string PseudolocalizationSuffix
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pseudo.Suffix; } }
        set { ArgumentNullException.ThrowIfNull(value); lock (_gate) { ThrowIfDisposed(); _pseudo = _pseudo with { Suffix = value }; } }
    }

    /// <summary>Registers a live catalog by identity; repeated registrations do nothing.</summary>
    /// <param name="translation">A caller-owned resource.</param>
    public void AddTranslation(Translation translation)
    {
        ArgumentNullException.ThrowIfNull(translation);
        lock (_gate)
        {
            ThrowIfDisposed();
            if (translation.IsDisposed) throw new ObjectDisposedException(nameof(translation));
            if (_translations.Any(item => ReferenceEquals(item, translation))) return;
            translation.Disposed += OnTranslationDisposed;
            _translations.Add(translation);
            if (translation.IsDisposed)
            {
                RemoveUnderLock(translation);
                throw new ObjectDisposedException(nameof(translation));
            }
        }
    }

    /// <summary>Unregisters a catalog without disposing it.</summary>
    /// <param name="translation">The resource to remove.</param>
    public void RemoveTranslation(Translation translation)
    {
        ArgumentNullException.ThrowIfNull(translation);
        lock (_gate) { ThrowIfDisposed(); RemoveUnderLock(translation); }
    }

    /// <summary>Unregisters all catalogs without disposing them.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (var translation in _translations) translation.Disposed -= OnTranslationDisposed;
            _translations.Clear();
        }
    }

    /// <summary>Returns a snapshot of registered catalogs in registration order.</summary>
    public Translation[] GetTranslations() { lock (_gate) { ThrowIfDisposed(); return [.. _translations]; } }

    /// <summary>Returns a snapshot of catalogs matching a locale exactly or by language.</summary>
    /// <param name="locale">The requested locale.</param>
    /// <param name="exact">Whether a normalized exact match is required.</param>
    public Translation[] FindTranslations(string locale, bool exact)
    {
        var normalized = NormalizeLocale(locale);
        var translations = GetTranslations();
        return translations.Where(item => MatchesLocale(normalized, item, exact)).ToArray();
    }

    /// <summary>Reports whether a resource is registered by identity.</summary>
    /// <param name="translation">The resource to inspect.</param>
    public bool HasTranslation(Translation translation)
    {
        ArgumentNullException.ThrowIfNull(translation);
        lock (_gate) { ThrowIfDisposed(); return _translations.Any(item => ReferenceEquals(item, translation)); }
    }

    /// <summary>Reports whether any catalog matches a locale.</summary>
    /// <param name="locale">The requested locale.</param>
    /// <param name="exact">Whether a normalized exact match is required.</param>
    public bool HasTranslationForLocale(string locale, bool exact) => FindTranslations(locale, exact).Length != 0;

    /// <summary>Gets the closest matching catalog, or null when none matches.</summary>
    /// <param name="locale">The requested locale.</param>
    public Translation? GetTranslationObject(string locale)
    {
        var normalized = NormalizeLocale(locale);
        var translations = GetTranslations();
        for (var i = translations.Length - 1; i >= 0; i--)
            if (MatchesLocale(normalized, translations[i], exact: true)) return translations[i];
        for (var i = translations.Length - 1; i >= 0; i--)
            if (MatchesLocale(normalized, translations[i], exact: false)) return translations[i];
        return null;
    }

    /// <summary>Resolves a singular message using this domain's catalogs and effective culture.</summary>
    /// <param name="message">The source message.</param>
    /// <param name="context">A case-sensitive context.</param>
    public string Translate(string message, string context = "")
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(context);
        string? registeredName;
        lock (_gate) { ThrowIfDisposed(); registeredName = _registeredName; }
        if (registeredName is not null) return TranslationServer.Translate(registeredName, message, context);
        if (!Enabled || !TranslationServer.Enabled) return message;
        var culture = EffectiveCulture;
        foreach (var locale in CultureChain(culture))
        {
            var translated = FindMessage(locale, message, context);
            if (translated.Length != 0) return ApplyPseudo(translated);
        }
        return ApplyPseudo(message);
    }

    /// <summary>Resolves a plural message using this domain's catalogs and effective culture.</summary>
    /// <param name="singular">The source singular form.</param>
    /// <param name="plural">The source plural form.</param>
    /// <param name="count">The quantity.</param>
    /// <param name="context">A case-sensitive context.</param>
    public string TranslatePlural(string singular, string plural, long count, string context = "")
    {
        ArgumentNullException.ThrowIfNull(singular);
        ArgumentNullException.ThrowIfNull(plural);
        ArgumentNullException.ThrowIfNull(context);
        string? registeredName;
        lock (_gate) { ThrowIfDisposed(); registeredName = _registeredName; }
        if (registeredName is not null) return TranslationServer.TranslatePlural(registeredName, singular, plural, count, context);
        if (Enabled && TranslationServer.Enabled)
        {
            foreach (var locale in CultureChain(EffectiveCulture))
            {
                var translated = FindPluralMessage(locale, singular, plural, count, context);
                if (translated.Length != 0) return translated;
            }
        }
        return count == 1 ? singular : plural;
    }

    /// <summary>Applies this domain's configured pseudolocalization transforms to a string.</summary>
    /// <param name="message">The string to transform.</param>
    public string Pseudolocalize(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        PseudoOptions options;
        lock (_gate) { ThrowIfDisposed(); options = _pseudo; }
        if (message.Length == 0) return message;
        var transformed = options.Override ? Transform(message, options, PseudoTransform.Override) : message;
        if (options.DoubleVowels) transformed = Transform(transformed, options, PseudoTransform.DoubleVowels);
        if (options.Accents) transformed = Transform(transformed, options, PseudoTransform.Accents);
        if (options.FakeBIDI) transformed = Transform(transformed, options, PseudoTransform.FakeBIDI);
        var padding = new string('_', checked((int)(message.Length * options.ExpansionRatio / 2)));
        return string.Concat(options.Prefix, padding, transformed, padding, options.Suffix);
    }

    internal CultureInfo EffectiveCulture
    {
        get
        {
            string locale;
            lock (_gate) { ThrowIfDisposed(); locale = _localeOverride; }
            return locale.Length == 0 ? TranslationServer.Culture : CultureInfo.GetCultureInfo(locale);
        }
    }

    internal string FindMessage(string locale, string message, string context)
    {
        var translations = GetTranslations();
        for (var i = translations.Length - 1; i >= 0; i--)
        {
            var translation = translations[i];
            try
            {
                if (translation.Locale != locale) continue;
                var value = translation.GetMessage(message, context);
                if (value.Length != 0) return value;
            }
            catch (ObjectDisposedException) when (translation.IsDisposed) { }
        }
        return string.Empty;
    }

    internal string FindPluralMessage(string locale, string singular, string plural, long count, string context)
    {
        var translations = GetTranslations();
        for (var i = translations.Length - 1; i >= 0; i--)
        {
            var translation = translations[i];
            try
            {
                if (translation.Locale != locale) continue;
                var value = translation.GetPluralMessage(singular, plural, count, context);
                if (value.Length != 0) return value;
            }
            catch (ObjectDisposedException) when (translation.IsDisposed) { }
        }
        return string.Empty;
    }

    internal string ApplyPseudo(string message) => PseudolocalizationEnabled ? Pseudolocalize(message) : message;

    internal void SetRegisteredName(string? name)
    {
        lock (_gate) { ThrowIfDisposed(); _registeredName = name; }
    }

    internal static IEnumerable<string> CultureChain(CultureInfo culture)
    {
        while (true)
        {
            yield return culture.Name;
            if (culture.Equals(CultureInfo.InvariantCulture)) yield break;
            culture = culture.Parent;
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) Clear();
        base.Dispose(disposing);
    }

    private void OnTranslationDisposed(ElectronObject resource)
    {
        lock (_gate) _translations.RemoveAll(item => ReferenceEquals(item, resource));
    }

    private void RemoveUnderLock(Translation translation)
    {
        if (_translations.Remove(translation)) translation.Disposed -= OnTranslationDisposed;
    }

    private static string NormalizeLocale(string locale)
    {
        ArgumentNullException.ThrowIfNull(locale);
        return CultureInfo.GetCultureInfo(locale.Replace('_', '-')).Name;
    }

    private static bool MatchesLocale(string normalized, Translation translation, bool exact)
    {
        try
        {
            var candidate = translation.Locale;
            if (candidate == normalized) return true;
            return !exact && CultureInfo.GetCultureInfo(candidate).TwoLetterISOLanguageName ==
                CultureInfo.GetCultureInfo(normalized).TwoLetterISOLanguageName;
        }
        catch (ObjectDisposedException) when (translation.IsDisposed) { return false; }
    }

    private enum PseudoTransform { Override, DoubleVowels, Accents, FakeBIDI }

    private static string Transform(string message, PseudoOptions options, PseudoTransform transform)
    {
        var result = new StringBuilder(message.Length);
        if (transform == PseudoTransform.FakeBIDI) result.Append('\u202e');
        for (var i = 0; i < message.Length; i++)
        {
            if (transform == PseudoTransform.FakeBIDI && message[i] == '\n')
            {
                result.Append('\u202c').Append('\n').Append('\u202e');
                continue;
            }
            if (options.SkipPlaceholders && i + 1 < message.Length && message[i] == '%' &&
                "scdoxXf".Contains(message[i + 1]))
            {
                if (transform == PseudoTransform.FakeBIDI) result.Append('\u202c');
                result.Append(message[i]).Append(message[++i]);
                if (transform == PseudoTransform.FakeBIDI) result.Append('\u202e');
                continue;
            }
            var letter = message[i];
            switch (transform)
            {
                case PseudoTransform.Override: result.Append('*'); break;
                case PseudoTransform.DoubleVowels:
                    result.Append(letter);
                    if ("aeiouAEIOU".Contains(letter)) result.Append(letter);
                    break;
                case PseudoTransform.Accents: result.Append(Accent(letter)); break;
                default: result.Append(letter); break;
            }
        }
        if (transform == PseudoTransform.FakeBIDI) result.Append('\u202c');
        return result.ToString();
    }

    private static string Accent(char letter) => letter switch
    {
        'A' => "Å",
        'B' => "ß",
        'C' => "Ç",
        'D' => "Ð",
        'E' => "É",
        'F' => "F́",
        'G' => "Ĝ",
        'H' => "Ĥ",
        'I' => "Ĩ",
        'J' => "Ĵ",
        'K' => "ĸ",
        'L' => "Ł",
        'M' => "Ḿ",
        'N' => "й",
        'O' => "Ö",
        'P' => "Ṕ",
        'Q' => "Q́",
        'R' => "Ř",
        'S' => "Ŝ",
        'T' => "Ŧ",
        'U' => "Ũ",
        'V' => "Ṽ",
        'W' => "Ŵ",
        'X' => "X́",
        'Y' => "Ÿ",
        'Z' => "Ž",
        'a' => "á",
        'b' => "ḅ",
        'c' => "ć",
        'd' => "d́",
        'e' => "é",
        'f' => "f́",
        'g' => "ǵ",
        'h' => "h̀",
        'i' => "í",
        'j' => "ǰ",
        'k' => "ḱ",
        'l' => "ł",
        'm' => "m̀",
        'n' => "ή",
        'o' => "ô",
        'p' => "ṕ",
        'q' => "q́",
        'r' => "ŕ",
        's' => "š",
        't' => "ŧ",
        'u' => "ü",
        'v' => "ṽ",
        'w' => "ŵ",
        'x' => "x́",
        'y' => "ý",
        'z' => "ź",
        _ => letter.ToString(),
    };
}
