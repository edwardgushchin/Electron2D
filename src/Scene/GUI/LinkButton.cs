using System.Text;

namespace Electron2D;

/// <summary>Displays a shaped text link and opens its configured URI when activated.</summary>
/// <remarks>Text is translated through the control's domain. Fonts and focus styles remain borrowed.
/// Pointer, keyboard, touch, shortcut and toggle behavior are inherited from BaseButton.
/// Attached access requires the scene owner thread; mutation is forbidden during scene capture.
/// Text preparation allocates; prepared unchanged layout and drawing reuse retained buffers.</remarks>
public partial class LinkButton : BaseButton
{
    /// <summary>Selects when the text underline is visible.</summary>
    public enum UnderlineMode
    {
        /// <summary>Underline normal, active and disabled text.</summary>
        Always = 0,
        /// <summary>Underline hovered or pressed text, including shortcut feedback.</summary>
        OnHover = 1,
        /// <summary>Omit the underline in every state.</summary>
        Never = 2
    }
    internal string AccessibleLinkText => _translatedText;
    private string _text = string.Empty, _translatedText = string.Empty, _language = string.Empty, _uri = string.Empty, _ellipsis = "…";
    private global::Electron2D.TextDirection _textDirection;
    private TextOverrunBehavior _overrun;
    private UnderlineMode _underline;
    private StructuredTextParser _parser;
    private string[] _options = [];
    private IReadOnlyList<string> _optionsView = Array.AsReadOnly(Array.Empty<string>());

    /// <summary>Creates an empty link with accessibility focus, a pointing cursor and an always-visible underline.</summary>
    public LinkButton() : this(string.Empty) { }
    /// <summary>Creates a link with untranslated source text.</summary>
    /// <param name="text">Initial source text, possibly empty.</param>
    /// <exception cref="ArgumentNullException">The text is null.</exception>
    public LinkButton(string text)
    {
        _resourceChanged = ResourceChanged; _resourceDisposed = ResourceDisposed;
        FocusMode = FocusMode.Accessibility; MouseDefaultCursorShape = CursorShape.PointingHand;
        MouseFilter = MouseFilter.Stop; Text = text;
    }
    /// <summary>Gets or sets the untranslated source caption.</summary>
    /// <value>Empty initially. Equal writes can refresh changed translation.</value>
    /// <exception cref="ArgumentNullException">The text is null.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The link is disposed.</exception>
    public string Text
    {
        get { CheckLink(); return _text; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); var translated = Atr(value); if (_text == value && _translatedText == translated) return; _text = value; _translatedText = translated; InvalidateLink(); if (!IsDisposed) UpdateConfigurationWarnings(); }
    }
    /// <summary>Gets or sets the URI or path passed to the platform resource opener on activation.</summary>
    /// <value>Empty initially, suppressing platform opening while retaining Pressed notification.</value>
    /// <remarks>Opening runs before Pressed, on the activating thread. OS.ShellOpen normalizes paths and
    /// validates the identifier at activation. Its platform errors follow inherited callback aggregation.
    /// A changed URI queues native semantic updates, including dependent contextual help. Setting this property does not launch an application.</remarks>
    /// <exception cref="ArgumentNullException">The identifier is null.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The link is disposed.</exception>
    public string URI { get { CheckLink(); return _uri; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (_uri == value) return; _uri = value; QueueAccessibilityUpdate(); } }
    /// <summary>Gets or sets the text underline policy.</summary>
    /// <value>Always initially. Unknown numeric values retain the active-state underline policy.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The link is disposed.</exception>
    public UnderlineMode Underline { get { CheckLink(); return _underline; } set { EnsureMutable(); if (_underline == value) return; _underline = value; QueueRedraw(); } }
    /// <summary>Gets or sets the language used for shaping.</summary>
    /// <value>Empty selects the translation domain, culture, then tool locale.</value>
    /// <exception cref="ArgumentException">The language is null or contains NUL.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The link is disposed.</exception>
    public string Language
    {
        get { CheckLink(); return _language; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (value.Contains('\0')) throw new ArgumentException("Language must not contain NUL.", nameof(value)); if (_language == value) return; _language = value; InvalidateLink(); }
    }
    /// <summary>Gets or sets the paragraph writing direction.</summary>
    /// <value>Auto initially. Inherited follows the control's layout direction.</value>
    /// <exception cref="ArgumentOutOfRangeException">The direction is outside -1 through 3.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The link is disposed.</exception>
    public global::Electron2D.TextDirection TextDirection
    {
        get { CheckLink(); return _textDirection; }
        set { EnsureMutable(); if ((int)value is < -1 or > 3) throw new ArgumentOutOfRangeException(nameof(value)); if (_textDirection == value) return; _textDirection = value; InvalidateLink(); }
    }
    /// <summary>Gets or sets character/word clipping and ellipsis behavior.</summary>
    /// <value>NoTrimming initially. Any other value releases minimum width; unknown values use ordinary clipping.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The link is disposed.</exception>
    public TextOverrunBehavior TextOverrunBehavior { get { CheckLink(); return _overrun; } set { EnsureMutable(); if (_overrun == value) return; _overrun = value; InvalidateLink(); } }
    /// <summary>Gets or sets the ellipsis scalar used for trimming.</summary>
    /// <value>An ellipsis initially. Only the first Unicode scalar is retained; empty uses the default ellipsis.</value>
    /// <exception cref="ArgumentException">The value is null or starts with malformed UTF-16.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The link is disposed.</exception>
    public string EllipsisChar
    {
        get { CheckLink(); return _ellipsis; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); var prefix = value.Length == 0 ? string.Empty : value[..Rune.GetRuneAt(value, 0).Utf16SequenceLength]; if (_ellipsis == prefix) return; _ellipsis = prefix; InvalidateLink(); }
    }
    /// <summary>Gets or sets the typed structured-text bidirectional parser.</summary>
    /// <value>Default initially. Custom calls the inherited OnStructuredTextParser hook.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The link is disposed.</exception>
    public StructuredTextParser StructuredTextBIDIOverride { get { CheckLink(); return _parser; } set { EnsureMutable(); if (_parser == value) return; _parser = value; InvalidateLink(); } }
    /// <summary>Gets or replaces independent typed parser options.</summary>
    /// <value>An empty array initially. List expects one delimiter; Custom receives a read-only snapshot.</value>
    /// <exception cref="ArgumentNullException">The array or an option is null.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The link is disposed.</exception>
    public string[] StructuredTextBIDIOverrideOptions
    {
        get { CheckLink(); return (string[])_options.Clone(); }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); foreach (var option in value) ArgumentNullException.ThrowIfNull(option); if (_options.AsSpan().SequenceEqual(value)) return; _options = (string[])value.Clone(); _optionsView = Array.AsReadOnly(_options); InvalidateLink(); }
    }
    private void CheckLink() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void InvalidateLink() { _revision++; _dirty = true; QueueRedraw(); UpdateMinimumSize(); }
    /// <inheritdoc />
    protected override void OnPressed() { EnsureMutable(); if (_uri.Length != 0) OS.ShellOpen(_uri); }
    /// <inheritdoc />
    protected override CursorShape OnGetCursorShape(Vector2 atPosition) => Disabled ? CursorShape.Arrow : base.OnGetCursorShape(atPosition);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(LinkButton) ? CreateLink : base.CreateSceneInstanceFactory();
    private static Node CreateLink() => new LinkButton();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
            if (property.Name != nameof(FocusMode) && property.Name != nameof(MouseDefaultCursorShape)) yield return property;
        foreach (var property in LinkProperties) yield return property;
    }
    private static readonly PropertyDescriptor[] LinkProperties =
    [
        new PropertyDescriptor<LinkButton, string>(nameof(Text), b => b.Text, (b,v) => b.Text=v, _ => string.Empty, stored:true),
        new PropertyDescriptor<LinkButton, string>(nameof(URI), b => b.URI, (b,v) => b.URI=v, _ => string.Empty, stored:true),
        new PropertyDescriptor<LinkButton, UnderlineMode>(nameof(Underline), b => b.Underline, (b,v) => b.Underline=v, _ => UnderlineMode.Always, stored:true),
        new PropertyDescriptor<LinkButton, string>(nameof(Language), b => b.Language, (b,v) => b.Language=v, _ => string.Empty, stored:true),
        new PropertyDescriptor<LinkButton, global::Electron2D.TextDirection>(nameof(TextDirection), b => b.TextDirection, (b,v) => b.TextDirection=v, _ => global::Electron2D.TextDirection.Auto, stored:true),
        new PropertyDescriptor<LinkButton, TextOverrunBehavior>(nameof(TextOverrunBehavior), b => b.TextOverrunBehavior, (b,v) => b.TextOverrunBehavior=v, _ => global::Electron2D.TextOverrunBehavior.NoTrimming, stored:true),
        new PropertyDescriptor<LinkButton, string>(nameof(EllipsisChar), b => b.EllipsisChar, (b,v) => b.EllipsisChar=v, _ => "…", stored:true),
        new PropertyDescriptor<LinkButton, StructuredTextParser>(nameof(StructuredTextBIDIOverride), b => b.StructuredTextBIDIOverride, (b,v) => b.StructuredTextBIDIOverride=v, _ => StructuredTextParser.Default, stored:true),
        new PropertyDescriptor<LinkButton, string[]>(nameof(StructuredTextBIDIOverrideOptions), b => b.StructuredTextBIDIOverrideOptions, (b,v) => b.StructuredTextBIDIOverrideOptions=v, _ => [], stored:true),
        new PropertyDescriptor<LinkButton, FocusMode>(nameof(FocusMode), b => b.FocusMode, (b,v) => b.FocusMode=v, _ => FocusMode.Accessibility, stored:true),
        new PropertyDescriptor<LinkButton, CursorShape>(nameof(MouseDefaultCursorShape), b => b.MouseDefaultCursorShape, (b,v) => b.MouseDefaultCursorShape=v, _ => CursorShape.PointingHand, stored:true)
    ];
}
