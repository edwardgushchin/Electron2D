using System.Globalization;

namespace Electron2D;

/// <summary>Stores contextual translated messages for one locale.</summary>
/// <remarks>Messages are copied into this resource and can be registered with <see cref="TranslationServer"/>.
/// Catalog state is synchronized. Overrides, caller plural selectors, and change handlers run outside the catalog lock.</remarks>
public partial class Translation : Resource
{
    private static readonly IReadOnlyList<PropertyDescriptor> TranslationProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<Translation, string>(nameof(Locale), translation => translation.Locale,
            (translation, value) => translation.Locale = value, _ => "en", stored: true),
        new PropertyDescriptor<Translation, string>(nameof(PluralRulesOverride), translation => translation.PluralRulesOverride, (translation, value) => translation.PluralRulesOverride = value, _ => "", stored: true),
        new PropertyDescriptor<Translation, string[]>("Messages", translation => translation.CaptureMessages(), (translation, value) => translation.RestoreMessages(value), _ => [], stored: true),
    ]);
    private static readonly IReadOnlyList<PropertyDescriptor> OptimizedProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<Translation, string>(nameof(Locale), translation => translation.Locale,
            (translation, value) => translation.Locale = value, _ => "en"),
        TranslationProperties[1],
        new PropertyDescriptor<OptimizedTranslation, byte[]>(OptimizedTranslation.CatalogPropertyName,
            translation => translation.SaveCatalog(), (translation, data) => translation.LoadCatalog(data), stored: true),
    ]);

    private readonly object _gate = new();
    private readonly Dictionary<(string Context, string Source), string[]> _messages = new();
    private string _locale = "en";
    private long _localeRevision;
    private Func<long, int>? _pluralSelector;
    private string _pluralRulesOverride = "";
    private CatalogPluralRule? _importedRule;
    private Func<long, int>? _defaultPluralSelector = TranslationServer.FindPluralRule("en").Select;
    internal object CatalogGate => _gate;

    /// <summary>Creates an empty English translation catalog.</summary>
    public Translation() { }

    /// <summary>Gets or sets the locale of this catalog.</summary>
    /// <value>A culture name normalized through <see cref="CultureInfo"/> and the catalog alias/script rules.
    /// The default is <c>en</c>; an empty name is the invariant culture.</value>
    /// <remarks>Assignment prepares the automatic plural rule before publishing the locale and raising <see cref="Resource.Changed"/>.</remarks>
    /// <exception cref="ArgumentException">The locale is not recognized by the managed globalization runtime.</exception>
    /// <exception cref="ArgumentNullException">The locale is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public string Locale
    {
        get { lock (_gate) { ThrowIfDisposed(); return _locale; } }
        set
        {
            SetCatalogLocale(value);
            EmitChanged();
        }
    }

    internal void SetCatalogLocale(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var normalized = TranslationServer.GetRuntimeLocale(value).Name;
        var defaultSelector = TranslationServer.FindPluralRule(normalized).Select;
        lock (_gate)
        {
            ThrowIfDisposed();
            UpdateLocaleUnderLock(normalized);
            _defaultPluralSelector = defaultSelector;
        }
    }

    private void UpdateLocaleUnderLock(string locale)
    {
        if (!string.Equals(_locale, locale, StringComparison.Ordinal)) _localeRevision++;
        _locale = locale;
    }

    internal (string Locale, long Revision) CaptureLocale()
    {
        lock (_gate) { ThrowIfDisposed(); return (_locale, _localeRevision); }
    }

    /// <summary>Gets or sets the locale-specific plural-form index selector.</summary>
    /// <value>A selector returning an index into registered forms; null uses an imported definition or the fixed locale-specific catalog rule.</value>
    /// <remarks>Supply a selector to override known rules or resolve an unknown locale. The delegate is borrowed and copied by reference.</remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Func<long, int>? PluralSelector
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pluralSelector; } }
        set { lock (_gate) { ThrowIfDisposed(); _pluralSelector = value; } EmitChanged(); }
    }

    /// <summary>Gets or sets a gettext plural definition overriding the locale rule.</summary>
    /// <value>A definition such as <c>nplurals=2; plural=n!=1;</c>; initially empty.</value>
    /// <remarks>Empty or invalid definitions use the automatic locale rule. A caller PluralSelector takes priority.
    /// The definition is prepared on assignment and retained in copies and catalog archives.</remarks>
    /// <exception cref="ArgumentNullException">The definition is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public string PluralRulesOverride
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pluralRulesOverride; } }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CatalogPluralRule? rule = null;
            if (value.Length != 0) { try { rule = CatalogPluralRule.Parse(value); } catch (InvalidDataException) { } }
            lock (_gate) { ThrowIfDisposed(); _pluralRulesOverride = value; _importedRule = rule; }
            EmitChanged();
        }
    }

    /// <summary>Adds or replaces one translated message in a context.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="translation">The translated text.</param>
    /// <param name="context">An optional case-sensitive context.</param>
    /// <exception cref="ArgumentNullException">A supplied string is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void AddMessage(string source, string translation, string context = "")
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(translation);
        ArgumentNullException.ThrowIfNull(context);
        lock (_gate) { ThrowIfDisposed(); _messages[(context, source)] = [translation]; }
        EmitChanged();
    }

    /// <summary>Adds or replaces the translated plural forms of one message.</summary>
    /// <param name="source">The source singular text.</param>
    /// <param name="translations">One or more translated forms in plural-index order; copied on assignment.</param>
    /// <param name="context">An optional case-sensitive context.</param>
    /// <exception cref="ArgumentException">No translated forms were supplied, or a form is null.</exception>
    /// <exception cref="ArgumentNullException">A supplied argument is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void AddPluralMessage(string source, IReadOnlyList<string> translations, string context = "")
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(translations);
        ArgumentNullException.ThrowIfNull(context);
        var count = translations.Count;
        if (count == 0) throw new ArgumentException("At least one translated form is required.", nameof(translations));
        var copy = new string[count];
        for (var i = 0; i < copy.Length; i++)
            copy[i] = translations[i] ?? throw new ArgumentException("Translated forms cannot be null.", nameof(translations));
        lock (_gate) { ThrowIfDisposed(); _messages[(context, source)] = copy; }
        EmitChanged();
    }

    /// <summary>Gets a translated message, or an empty string when no entry exists.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="context">An optional case-sensitive context.</param>
    /// <returns>The override result, the first registered form, or an empty string for a missing entry.</returns>
    /// <exception cref="ArgumentNullException">A supplied string is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public string GetMessage(string source, string context = "")
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        return ResolveMessage(source, context);
    }

    internal string ResolveMessage(string source, string context, long? localeRevision = null)
    {
        ThrowIfDisposed();
        var custom = OnGetMessage(source, context);
        if (custom is not null && localeRevision is null) return custom;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (localeRevision is { } expected && expected != _localeRevision) return string.Empty;
            return custom ?? (_messages.TryGetValue((context, source), out var forms) ? forms[0] : string.Empty);
        }
    }

    /// <summary>Gets a translated plural form, or an empty string when no entry exists.</summary>
    /// <param name="source">The source singular text.</param>
    /// <param name="plural">The source plural text, available to an override.</param>
    /// <param name="count">The quantity used to select a form.</param>
    /// <param name="context">An optional case-sensitive context.</param>
    /// <returns>The override result, the selected registered form, or an empty string when local lookup has no entry or the count is negative.</returns>
    /// <remarks>Local lookup captures the forms and their rule together before calling a selector.</remarks>
    /// <exception cref="InvalidOperationException">The catalog has no applicable plural rule, an expression fails arithmetic, or a selector returns an invalid index.</exception>
    /// <exception cref="ArgumentNullException">A supplied string is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public string GetPluralMessage(string source, string plural, long count, string context = "")
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(plural);
        ArgumentNullException.ThrowIfNull(context);
        return ResolvePluralMessage(source, plural, count, context);
    }

    internal string ResolvePluralMessage(string source, string plural, long count, string context, long? localeRevision = null)
    {
        ThrowIfDisposed();
        var custom = OnGetPluralMessage(source, plural, count, context);
        if (localeRevision is null && (custom is not null || count < 0)) return custom ?? string.Empty;

        string[]? forms;
        Func<long, int>? selector;
        CatalogPluralRule? imported;
        lock (_gate)
        {
            ThrowIfDisposed();
            // Ranking happens before the unlocked override. Validate it where we take the forms and rule.
            if (localeRevision is { } expected && expected != _localeRevision) return string.Empty;
            if (custom is not null) return custom;
            if (count < 0 || !_messages.TryGetValue((context, source), out forms)) return string.Empty;
            selector = _pluralSelector;
            imported = selector is null ? _importedRule : null;
            selector ??= _defaultPluralSelector;
        }

        var index = imported is not null ? imported.Select(count) : selector is not null ? selector(count) : forms.Length == 1 ? 0 :
            throw new InvalidOperationException("A plural selector is required for an unknown locale rule.");
        if ((uint)index >= (uint)forms.Length)
            throw new InvalidOperationException("The plural selector returned an index outside the registered forms.");
        return forms[index];
    }

    /// <summary>Erases one contextual message and all of its plural forms.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="context">An optional case-sensitive context.</param>
    /// <remarks>Raises <see cref="Resource.Changed"/> only if the entry existed.</remarks>
    /// <exception cref="ArgumentNullException">A supplied string is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public void EraseMessage(string source, string context = "")
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        bool removed;
        lock (_gate) { ThrowIfDisposed(); removed = _messages.Remove((context, source)); }
        if (removed) EmitChanged();
    }

    /// <summary>Gets the number of contextual source-message entries.</summary>
    /// <returns>The entry count; plural forms count as one entry.</returns>
    public virtual int GetMessageCount() { lock (_gate) { ThrowIfDisposed(); return _messages.Count; } }

    /// <summary>Gets source messages with context separated by the EOT character.</summary>
    /// <returns>An independent array of source-message keys.</returns>
    public virtual string[] GetMessageList()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _messages.Keys.Select(key => key.Context.Length == 0 ? key.Source : string.Concat(key.Context, "\u0004", key.Source)).ToArray();
        }
    }

    /// <summary>Gets every translated form stored by this catalog.</summary>
    /// <returns>An independent array including all plural forms.</returns>
    public virtual string[] GetTranslatedMessageList()
    {
        lock (_gate) { ThrowIfDisposed(); return _messages.Values.SelectMany(forms => forms).ToArray(); }
    }

    /// <summary>Allows a derived catalog to resolve a singular message before local storage is queried.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="context">The context.</param>
    /// <returns>A translation, including an empty string, or null to query local storage.</returns>
    protected virtual string? OnGetMessage(string source, string context) => null;

    /// <summary>Allows a derived catalog to resolve a plural message before local storage is queried.</summary>
    /// <param name="source">The source singular text.</param>
    /// <param name="plural">The source plural text.</param>
    /// <param name="count">The quantity.</param>
    /// <param name="context">The context.</param>
    /// <returns>A translation, including an empty string, or null to query local storage.</returns>
    /// <remarks>The override receives the original signed count before local lookup checks for negative counts.</remarks>
    protected virtual string? OnGetPluralMessage(string source, string plural, long count, string context) => null;

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance()
    {
        if (GetType() != typeof(Translation))
            throw new NotSupportedException($"{GetType().Name} must override {nameof(CreateDuplicateInstance)} to support duplication.");
        return new Translation();
    }

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
        => ((Translation)target).RestoreCatalogCopy(CaptureCatalogCopy());

    internal sealed record CatalogCopy(string Locale, string RuleDefinition, CatalogPluralRule? ImportedRule,
        Func<long, int>? Selector, Func<long, int>? DefaultSelector,
        KeyValuePair<(string Context, string Source), string[]>[] Entries);

    internal CatalogCopy CaptureCatalogCopy()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var entries = _messages.Select(entry => new KeyValuePair<(string, string), string[]>(entry.Key, (string[])entry.Value.Clone())).ToArray();
            return new CatalogCopy(_locale, _pluralRulesOverride, _importedRule, _pluralSelector, _defaultPluralSelector, entries);
        }
    }

    internal void RestoreCatalogCopy(CatalogCopy snapshot)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            UpdateLocaleUnderLock(snapshot.Locale);
            _pluralSelector = snapshot.Selector;
            _pluralRulesOverride = snapshot.RuleDefinition;
            _importedRule = snapshot.ImportedRule;
            _defaultPluralSelector = snapshot.DefaultSelector;
            _messages.Clear();
            foreach (var entry in snapshot.Entries) _messages.Add(entry.Key, entry.Value);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(this is OptimizedTranslation ? OptimizedProperties : TranslationProperties);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) _messages.Clear();
        base.Dispose(disposing);
    }
}
