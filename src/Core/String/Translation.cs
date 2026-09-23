using System.Globalization;

namespace Electron2D;

/// <summary>Stores contextual translated messages for one locale.</summary>
/// <remarks>Messages are copied into this resource and can be registered with <see cref="TranslationServer"/>.
/// Reads and writes are serialized; change handlers and plural selectors run after the state lock is released.</remarks>
public class Translation : Resource
{
    private static readonly IReadOnlyList<PropertyDescriptor> TranslationProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<Translation, string>(nameof(Locale), translation => translation.Locale,
            (translation, value) => translation.Locale = value, _ => "en", stored: true),
    ]);

    private readonly object _gate = new();
    private readonly Dictionary<(string Context, string Source), string[]> _messages = new();
    private string _locale = "en";
    private Func<long, int>? _pluralSelector;

    /// <summary>Creates an empty English translation catalog.</summary>
    public Translation() { }

    /// <summary>Gets or sets the locale of this catalog.</summary>
    /// <value>A normalized culture name; the default is <c>en</c>.</value>
    /// <exception cref="ArgumentException">The locale is not recognized by the managed globalization runtime.</exception>
    /// <exception cref="ArgumentNullException">The locale is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public string Locale
    {
        get { lock (_gate) { ThrowIfDisposed(); return _locale; } }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            var normalized = CultureInfo.GetCultureInfo(value.Replace('_', '-')).Name;
            lock (_gate) { ThrowIfDisposed(); _locale = normalized; }
            EmitChanged();
        }
    }

    /// <summary>Gets or sets the locale-specific plural-form index selector.</summary>
    /// <value>A selector returning an index into registered forms; null uses English rules only for English locales.</value>
    /// <remarks>Supply a selector before resolving plural messages in other locales. The delegate is borrowed and copied by reference.</remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public Func<long, int>? PluralSelector
    {
        get { lock (_gate) { ThrowIfDisposed(); return _pluralSelector; } }
        set { lock (_gate) { ThrowIfDisposed(); _pluralSelector = value; } EmitChanged(); }
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
        if (translations.Count == 0) throw new ArgumentException("At least one translated form is required.", nameof(translations));
        var copy = new string[translations.Count];
        for (var i = 0; i < copy.Length; i++)
            copy[i] = translations[i] ?? throw new ArgumentException("Translated forms cannot be null.", nameof(translations));
        lock (_gate) { ThrowIfDisposed(); _messages[(context, source)] = copy; }
        EmitChanged();
    }

    /// <summary>Gets a translated message, or an empty string when no entry exists.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="context">An optional case-sensitive context.</param>
    /// <returns>The first registered form or an empty string.</returns>
    /// <exception cref="ArgumentNullException">A supplied string is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public string GetMessage(string source, string context = "")
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(context);
        ThrowIfDisposed();
        var custom = OnGetMessage(source, context);
        if (custom is not null) return custom;
        lock (_gate) { ThrowIfDisposed(); return _messages.TryGetValue((context, source), out var forms) ? forms[0] : string.Empty; }
    }

    /// <summary>Gets a translated plural form, or an empty string when no entry exists.</summary>
    /// <param name="source">The source singular text.</param>
    /// <param name="plural">The source plural text, available to an override.</param>
    /// <param name="count">The quantity used to select a form.</param>
    /// <param name="context">An optional case-sensitive context.</param>
    /// <returns>The selected translated form, or an empty string when absent or the count is negative.</returns>
    /// <exception cref="InvalidOperationException">A non-English catalog has multiple forms but no plural selector, or a selector returns an invalid index.</exception>
    /// <exception cref="ArgumentNullException">A supplied string is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public string GetPluralMessage(string source, string plural, long count, string context = "")
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(plural);
        ArgumentNullException.ThrowIfNull(context);
        ThrowIfDisposed();
        var custom = OnGetPluralMessage(source, plural, count, context);
        if (custom is not null) return custom;
        if (count < 0) return string.Empty;

        string[]? forms;
        Func<long, int>? selector;
        string locale;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_messages.TryGetValue((context, source), out forms)) return string.Empty;
            selector = _pluralSelector;
            locale = _locale;
        }

        var index = selector is not null ? selector(count) : forms.Length == 1 ? 0 :
            locale is "en" || locale.StartsWith("en-", StringComparison.OrdinalIgnoreCase) ? count == 1 ? 0 : 1 :
            throw new InvalidOperationException("A plural selector is required for this locale.");
        if ((uint)index >= (uint)forms.Length)
            throw new InvalidOperationException("The plural selector returned an index outside the registered forms.");
        return forms[index];
    }

    /// <summary>Erases one contextual message and all of its plural forms.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="context">An optional case-sensitive context.</param>
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
    public int GetMessageCount() { lock (_gate) { ThrowIfDisposed(); return _messages.Count; } }

    /// <summary>Gets source messages with context separated by the EOT character.</summary>
    /// <returns>An independent array of source-message keys.</returns>
    public string[] GetMessageList()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _messages.Keys.Select(key => key.Context.Length == 0 ? key.Source : string.Concat(key.Context, "\u0004", key.Source)).ToArray();
        }
    }

    /// <summary>Gets every translated form stored by this catalog.</summary>
    /// <returns>An independent array including all plural forms.</returns>
    public string[] GetTranslatedMessageList()
    {
        lock (_gate) { ThrowIfDisposed(); return _messages.Values.SelectMany(forms => forms).ToArray(); }
    }

    /// <summary>Allows a derived catalog to resolve a singular message before local storage is queried.</summary>
    /// <param name="source">The source text.</param>
    /// <param name="context">The context.</param>
    /// <returns>A translation, or null to query local storage.</returns>
    protected virtual string? OnGetMessage(string source, string context) => null;

    /// <summary>Allows a derived catalog to resolve a plural message before local storage is queried.</summary>
    /// <param name="source">The source singular text.</param>
    /// <param name="plural">The source plural text.</param>
    /// <param name="count">The quantity.</param>
    /// <param name="context">The context.</param>
    /// <returns>A translation, or null to query local storage.</returns>
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
    {
        string locale;
        Func<long, int>? selector;
        KeyValuePair<(string Context, string Source), string[]>[] entries;
        lock (_gate)
        {
            ThrowIfDisposed();
            locale = _locale;
            selector = _pluralSelector;
            entries = _messages.Select(entry => new KeyValuePair<(string, string), string[]>(entry.Key, (string[])entry.Value.Clone())).ToArray();
        }
        var copy = (Translation)target;
        lock (copy._gate)
        {
            copy.ThrowIfDisposed();
            copy._locale = locale;
            copy._pluralSelector = selector;
            copy._messages.Clear();
            foreach (var entry in entries) copy._messages.Add(entry.Key, entry.Value);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(TranslationProperties);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) _messages.Clear();
        base.Dispose(disposing);
    }
}
