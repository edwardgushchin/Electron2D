namespace Electron2D;

/// <summary>Draws a themed action button with shaped text and an optional icon.</summary>
/// <remarks>Text is translated through the node's domain. Fonts, icons and styles remain borrowed.
/// Attached mutations require the scene owner thread and are forbidden during scene capture.
/// Resource changes update minimum size and recorded drawing; prepared text and glyph buffers are reused.
/// Known icon and textured-style dependencies retain renderer residency only while attached. Shared aliases
/// are counted once per button; exit, replacement and disposal release retention without disposing resources.</remarks>
public partial class Button : BaseButton
{
    private string _text = string.Empty, _translatedText = string.Empty, _language = string.Empty;
    private Texture? _icon;
    private readonly List<ButtonTextureState> _textureStates = [];
    private readonly List<Texture> _residentTextures = [];
    private bool _residencyAttached;
    private readonly record struct ButtonTextureState(Texture Texture, long Revision, bool Disposed);
    private bool _flat, _clipText, _expandIcon;
    private HorizontalAlignment _alignment = HorizontalAlignment.Center, _iconAlignment = HorizontalAlignment.Left;
    private VerticalAlignment _verticalIconAlignment = VerticalAlignment.Center;
    private global::Electron2D.TextDirection _textDirection = global::Electron2D.TextDirection.Auto;
    private TextAutowrapMode _autowrapMode;
    private TextLineBreakFlags _autowrapTrimFlags = TextLineBreakFlags.TrimEndEdgeSpaces;
    private TextOverrunBehavior _textOverrunBehavior;
    private readonly TextLayout _layout = new();
    private Font? _font;
    private long _fontGeneration = -1;
    private bool _dirty = true, _themeDirty = true, _shaping, _minimumPending;
    private float _layoutWidth = -1;
    private HorizontalAlignment _layoutAlignment = HorizontalAlignment.Left;
    private int _revision, _entryGeneration, _resourcePending;
    private SceneTree? _resourceTree;
    private Action? _resourceDispatch;
    private readonly Action<Resource> _resourceChanged;
    private readonly Action<ElectronObject> _resourceDisposed;
    private readonly StyleBox?[] _styles = new StyleBox?[6];
    private Vector2 _largestStyleSize;
    private float _marginLeft, _marginRight, _marginTop, _marginBottom, _internalLeft, _internalRight;
    private int _fontSize, _lineSpacing, _separation, _iconMaxWidth, _outlineSize;
    private bool _alignToLargestStyleBox;

    /// <summary>Creates an empty button with centered text and pointer input enabled.</summary>
    public Button() : this(string.Empty) { }
    /// <summary>Creates a button with untranslated source text.</summary>
    /// <param name="text">Initial text; may be empty.</param>
    /// <exception cref="ArgumentNullException">The text is null.</exception>
    public Button(string text)
    {
        _resourceChanged = ResourceChanged; _resourceDisposed = ResourceDisposed;
        MouseFilter = MouseFilter.Stop;
        Text = text;
    }

    internal virtual string TranslateButtonText(string text) => Atr(text);
    /// <summary>Gets or sets untranslated button text.</summary>
    /// <value>Empty initially. Equal writes refresh a changed translation but otherwise remain silent.</value>
    /// <exception cref="ArgumentNullException">The text is null.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public string Text
    {
        get { CheckButton(); return _text; }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value); var translated = TranslateButtonText(value);
            if (_text == value && _translatedText == translated) return;
            _text = value; _translatedText = translated; Invalidate();
        }
    }
    /// <summary>Gets or sets the borrowed icon override.</summary>
    /// <value>Null initially, using the optional theme icon. Equal identities are silent. Disposed borrowed icons read as null and are cleared during owner processing.</value>
    /// <exception cref="ObjectDisposedException">The button or assigned icon is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    public Texture? Icon
    {
        get { CheckButton(); return _icon is { IsDisposed: false } ? _icon : null; }
        set
        {
            EnsureMutable(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
            if (ReferenceEquals(_icon, value)) return;
            ReleaseButtonTextureResidency();
            _textureStates.Clear();
            if (_icon is not null) { _icon.Changed -= _resourceChanged; _icon.Disposed -= _resourceDisposed; }
            _icon = value;
            if (_icon is not null) { _icon.Changed += _resourceChanged; _icon.Disposed += _resourceDisposed; }
            Invalidate();
        }
    }
    /// <summary>Gets or sets whether the state background is omitted.</summary>
    /// <value>False initially. Focus decoration and minimum-size margins remain active.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public bool Flat { get { CheckButton(); return _flat; } set { EnsureMutable(); Set(ref _flat, value, minimum: false, shape: false); } }
    /// <summary>Gets or sets text alignment along the horizontal axis.</summary>
    /// <value>Center initially. Raw numeric values are retained; Fill uses the paragraph's justification behavior.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public HorizontalAlignment Alignment { get { CheckButton(); return _alignment; } set { EnsureMutable(); Set(ref _alignment, value, minimum: false, shape: false); } }
    /// <summary>Gets or sets width-dependent wrapping.</summary>
    /// <value>Off initially. Raw numeric values are retained.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public TextAutowrapMode AutowrapMode { get { CheckButton(); return _autowrapMode; } set { EnsureMutable(); Set(ref _autowrapMode, value); } }
    /// <summary>Gets or sets trimming of spaces around line breaks.</summary>
    /// <value>TrimEndEdgeSpaces initially. Only TrimIndent, TrimStartEdgeSpaces and TrimEndEdgeSpaces are retained.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public TextLineBreakFlags AutowrapTrimFlags { get { CheckButton(); return _autowrapTrimFlags; } set { EnsureMutable(); Set(ref _autowrapTrimFlags, value & (TextLineBreakFlags.TrimIndent | TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces)); } }
    /// <summary>Gets or sets whether text may shrink horizontally below its natural width.</summary>
    /// <value>False initially. Drawing clips whole glyph advances to the available paragraph width.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public bool ClipText { get { CheckButton(); return _clipText; } set { EnsureMutable(); Set(ref _clipText, value, shape: false); } }
    /// <summary>Gets or sets whether the icon fits the available content rectangle.</summary>
    /// <value>False initially. Expanded icons do not contribute their intrinsic size to minimum size.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public bool ExpandIcon { get { CheckButton(); return _expandIcon; } set { EnsureMutable(); Set(ref _expandIcon, value, shape: false); } }
    /// <summary>Gets or sets horizontal icon placement.</summary>
    /// <value>Left initially; left and right swap under RTL layout. Center permits text and icon to overlap.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public HorizontalAlignment IconAlignment { get { CheckButton(); return _iconAlignment; } set { EnsureMutable(); Set(ref _iconAlignment, value, shape: false); } }
    /// <summary>Gets or sets vertical icon placement.</summary>
    /// <value>Center initially. Other values reserve icon height in addition to the text height.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public VerticalAlignment VerticalIconAlignment { get { CheckButton(); return _verticalIconAlignment; } set { EnsureMutable(); Set(ref _verticalIconAlignment, value, shape: false); } }
    /// <summary>Gets or sets the shaping language.</summary>
    /// <value>Empty initially; uses the translation-domain locale, current culture, then tool locale.</value>
    /// <exception cref="ArgumentNullException">The language is null.</exception>
    /// <exception cref="ArgumentException">The language contains NUL.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public string Language
    {
        get { CheckButton(); return _language; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (value.Contains('\0')) throw new ArgumentException("Language must not contain NUL.", nameof(value)); Set(ref _language, value, minimum: false); }
    }
    /// <summary>Gets or sets paragraph direction.</summary>
    /// <value>Auto initially. Inherited follows the control's layout direction; legacy negative one also selects Auto.</value>
    /// <exception cref="ArgumentOutOfRangeException">The direction is outside negative one through three.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public global::Electron2D.TextDirection TextDirection
    {
        get { CheckButton(); return _textDirection; }
        set { EnsureMutable(); if ((int)value is < -1 or > 3) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _textDirection, value, minimum: false); }
    }
    /// <summary>Gets or sets character/word trimming and ellipsis behavior.</summary>
    /// <value>NoTrimming initially. Unknown numeric values are retained without selecting an ellipsis policy.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    public TextOverrunBehavior TextOverrunBehavior { get { CheckButton(); return _textOverrunBehavior; } set { EnsureMutable(); Set(ref _textOverrunBehavior, value); } }

    private void CheckButton() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void Set<T>(ref T field, T value, bool minimum = true, bool shape = true)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; Invalidate(minimum, shape);
    }
    private void Invalidate(bool minimum = true, bool shape = true)
    {
        _revision++; _dirty |= shape; QueueRedraw(); if (minimum) UpdateMinimumSize();
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Button) ? CreateButton : base.CreateSceneInstanceFactory();
    private static Node CreateButton() => new Button();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        foreach (var property in ButtonProperties) yield return property;
    }
    private static readonly PropertyDescriptor[] ButtonProperties =
    [
        new PropertyDescriptor<Button, string>(nameof(Text), b => b.Text, (b, v) => b.Text = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<Button, Texture?>(nameof(Icon), b => b.Icon, (b, v) => b.Icon = v, _ => null, stored: true),
        new PropertyDescriptor<Button, bool>(nameof(Flat), b => b.Flat, (b, v) => b.Flat = v, _ => false, stored: true),
        new PropertyDescriptor<Button, HorizontalAlignment>(nameof(Alignment), b => b.Alignment, (b, v) => b.Alignment = v, _ => HorizontalAlignment.Center, stored: true),
        new PropertyDescriptor<Button, TextAutowrapMode>(nameof(AutowrapMode), b => b.AutowrapMode, (b, v) => b.AutowrapMode = v, _ => TextAutowrapMode.Off, stored: true),
        new PropertyDescriptor<Button, TextLineBreakFlags>(nameof(AutowrapTrimFlags), b => b.AutowrapTrimFlags, (b, v) => b.AutowrapTrimFlags = v, _ => TextLineBreakFlags.TrimEndEdgeSpaces, stored: true),
        new PropertyDescriptor<Button, bool>(nameof(ClipText), b => b.ClipText, (b, v) => b.ClipText = v, _ => false, stored: true),
        new PropertyDescriptor<Button, bool>(nameof(ExpandIcon), b => b.ExpandIcon, (b, v) => b.ExpandIcon = v, _ => false, stored: true),
        new PropertyDescriptor<Button, HorizontalAlignment>(nameof(IconAlignment), b => b.IconAlignment, (b, v) => b.IconAlignment = v, _ => HorizontalAlignment.Left, stored: true),
        new PropertyDescriptor<Button, VerticalAlignment>(nameof(VerticalIconAlignment), b => b.VerticalIconAlignment, (b, v) => b.VerticalIconAlignment = v, _ => VerticalAlignment.Center, stored: true),
        new PropertyDescriptor<Button, string>(nameof(Language), b => b.Language, (b, v) => b.Language = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<Button, global::Electron2D.TextDirection>(nameof(TextDirection), b => b.TextDirection, (b, v) => b.TextDirection = v, _ => global::Electron2D.TextDirection.Auto, stored: true),
        new PropertyDescriptor<Button, TextOverrunBehavior>(nameof(TextOverrunBehavior), b => b.TextOverrunBehavior, (b, v) => b.TextOverrunBehavior = v, _ => global::Electron2D.TextOverrunBehavior.NoTrimming, stored: true)
    ];
}
