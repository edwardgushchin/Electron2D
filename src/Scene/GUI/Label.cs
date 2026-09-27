using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Electron2D;

/// <summary>Displays shaped, themed text with wrapping, bidirectional contexts and layered glyph effects.</summary>
/// <remarks>Text indices count Unicode scalars. Fonts and LabelSettings are borrowed. Attached mutations require
/// the scene owner thread and are rejected during scene capture. Resource updates are marshalled to that thread;
/// revision checks also recover updates hidden by earlier throwing resource observers.</remarks>
public partial class Label : Control
{
    private string _translatedText = string.Empty, _layoutLanguage = string.Empty;
    private string _text = string.Empty, _language = string.Empty, _paragraphSeparator = @"\n", _ellipsisChar = "…";
    private HorizontalAlignment _horizontalAlignment;
    private VerticalAlignment _verticalAlignment;
    private TextAutowrapMode _autowrapMode;
    private TextLineBreakFlags _autowrapTrimFlags = TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces;
    private TextJustificationFlags _justificationFlags = (TextJustificationFlags)163;
    private TextOverrunBehavior _textOverrunBehavior;
    private TextVisibleCharactersBehavior _visibleCharactersBehavior;
    private global::Electron2D.TextDirection _textDirection;
    private StructuredTextParser _structuredTextBIDIOverride;
    private string[] _structuredOptions = [];
    private ReadOnlyCollection<string> _structuredOptionsView = Array.AsReadOnly(Array.Empty<string>());
    private float[] _tabStops = [];
    private LabelSettings? _labelSettings;
    private bool _clipText, _uppercase, _textDirty = true, _dirty = true, _shaping;
    private int _visibleCharacters = -1, _linesSkipped, _maxLinesVisible = -1;
    private float _visibleRatio = 1;
    private readonly Action<Resource> _resourceChanged;
    private readonly Action<ElectronObject> _resourceDisposed;
    private readonly Action _maximumChanged;
    private SceneTree? _resourceTree;
    private Action? _resourceDispatch;
    private int _entryGeneration, _resourcePending;
    private Font? _font;
    private long _fontGeneration = -1, _settingsRevision = -1;
    private readonly List<Paragraph> _paragraphs = [];
    private readonly List<LabelLine> _lines = [];
    private readonly List<PreparedText> _textCache = [];
    private long _textCacheAccess;
    private readonly record struct PreparedKey(string Text, string Language, string Separator, bool Uppercase, int VisibleCharacters);
    private readonly record struct PreparedParagraph(string Text, int Start);
    private sealed class PreparedText
    {
        internal PreparedKey Key;
        internal long Access;
        internal readonly List<PreparedParagraph> Paragraphs = [];
    }
    private int _paragraphCount, _fontSize, _lineSpacing, _paragraphSpacing;
    private float _fontHeight, _naturalWidth, _minimumHeight;
    private StyleBox? _normalStyle;
    private sealed class Paragraph
    {
        internal string Text = string.Empty;
        internal int Start;
        internal readonly TextLayout Layout = new();
        internal readonly List<TextBIDIRange> Contexts = [];
    }
    private readonly record struct LabelLine(int Paragraph, int Local, float Height, float Ascent, float Width);

    /// <summary>Creates an empty label that ignores pointer input and centers within vertical container surplus.</summary>
    public Label() : this(string.Empty) { }
    /// <summary>Creates a label with the supplied text and default themed appearance.</summary>
    /// <param name="text">Initial text; may be empty.</param><exception cref="ArgumentNullException">Text is null.</exception>
    public Label(string text)
    {
        ArgumentNullException.ThrowIfNull(text); _text = text; _translatedText = Atr(text);
        _resourceChanged = ResourceChanged; _resourceDisposed = ResourceDisposed; _maximumChanged = MaximumChanged;
        MaximumSizeChanged += _maximumChanged; MouseFilter = MouseFilter.Ignore; SizeFlagsVertical = SizeFlags.ShrinkCenter;
    }

    /// <summary>Gets or replaces the untranslated source text.</summary><value>Empty initially; equal writes are silent.</value>
    /// <exception cref="ArgumentNullException">The text is null.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public string Text
    {
        get { CheckLabel(); return _text; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (_text == value) return; _text = value; _translatedText = Atr(value); if (_visibleRatio < 1) _visibleCharacters = RatioCount(_visibleRatio); Invalidate(true); }
    }
    /// <summary>Gets or sets horizontal text alignment.</summary><value>Left initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its supported range.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public HorizontalAlignment HorizontalAlignment
    {
        get { CheckLabel(); return _horizontalAlignment; }
        set { EnsureMutable(); ValidateAlignment(value); Set(ref _horizontalAlignment, value, false); }
    }
    /// <summary>Gets or sets vertical alignment of visible lines.</summary><value>Top initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its supported range.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public VerticalAlignment VerticalAlignment
    {
        get { CheckLabel(); return _verticalAlignment; }
        set { EnsureMutable(); if (value is < VerticalAlignment.Top or > VerticalAlignment.Fill) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _verticalAlignment, value, false); }
    }
    /// <summary>Gets or sets width-dependent line wrapping.</summary><value>Off initially. Unknown numeric values are retained.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public TextAutowrapMode AutowrapMode
    {
        get { CheckLabel(); return _autowrapMode; }
        set { EnsureMutable(); Set(ref _autowrapMode, value, false); }
    }
    /// <summary>Gets or sets permitted fill-alignment operations.</summary><value>Kashida, word expansion, skip-last-line and do-not-skip-single-line initially.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public TextJustificationFlags JustificationFlags
    {
        get { CheckLabel(); return _justificationFlags; }
        set { EnsureMutable(); Set(ref _justificationFlags, value, false); }
    }
    /// <summary>Gets or sets trimming and ellipsis behavior.</summary><value>NoTrimming initially. Unknown numeric values are retained.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public TextOverrunBehavior TextOverrunBehavior
    {
        get { CheckLabel(); return _textOverrunBehavior; }
        set { EnsureMutable(); Set(ref _textOverrunBehavior, value, false); }
    }
    /// <summary>Gets or sets clipping of this label and its canvas descendants to the control rectangle.</summary><value>False initially.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public bool ClipText
    {
        get { CheckLabel(); return _clipText; }
        set { EnsureMutable(); Set(ref _clipText, value, false); }
    }
    /// <summary>Gets or sets locale-aware uppercase display without replacing Text.</summary><value>False initially.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public bool Uppercase
    {
        get { CheckLabel(); return _uppercase; }
        set { EnsureMutable(); Set(ref _uppercase, value, true); }
    }
    /// <summary>Gets or sets the paragraph writing direction.</summary><value>Auto initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its supported range.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public global::Electron2D.TextDirection TextDirection
    {
        get { CheckLabel(); return _textDirection; }
        set { EnsureMutable(); if ((int)value < -1 || (int)value > 3) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _textDirection, value, true); }
    }
    /// <summary>Gets or sets independent bidi contexts for structured text.</summary><value>Default initially. Unknown numeric values are retained.</value>
    /// <exception cref="NotSupportedException">The excluded language-specific parser is selected.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public StructuredTextParser StructuredTextBIDIOverride
    {
        get { CheckLabel(); return _structuredTextBIDIOverride; }
        set { EnsureMutable(); if ((int)value == 5) throw new NotSupportedException("Language-specific structured parsing is outside this text profile."); Set(ref _structuredTextBIDIOverride, value, true); }
    }
    /// <summary>Gets or sets the number of initial shaped lines omitted from display.</summary><value>Zero initially; must be nonnegative.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside its supported range.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public int LinesSkipped
    {
        get { CheckLabel(); return _linesSkipped; }
        set { EnsureMutable(); ArgumentOutOfRangeException.ThrowIfNegative(value); Set(ref _linesSkipped, value, false); }
    }
    /// <summary>Gets or sets the maximum number of visible lines.</summary><value>Minus one initially. A negative value means unlimited; zero displays no lines.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public int MaxLinesVisible
    {
        get { CheckLabel(); return _maxLinesVisible; }
        set { EnsureMutable(); Set(ref _maxLinesVisible, value, false); }
    }
    /// <summary>Gets or sets whether visibility limits characters before or after shaping, or visual glyphs.</summary><value>CharsBeforeShaping initially. Unknown numeric values are retained.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public TextVisibleCharactersBehavior VisibleCharactersBehavior
    {
        get { CheckLabel(); return _visibleCharactersBehavior; }
        set { EnsureMutable(); Set(ref _visibleCharactersBehavior, value, value == TextVisibleCharactersBehavior.CharsBeforeShaping || _visibleCharactersBehavior == TextVisibleCharactersBehavior.CharsBeforeShaping); }
    }
    /// <summary>Gets or sets the line-edge whitespace trimming flags.</summary>
    /// <value>Start and end trimming initially. Only TrimIndent, TrimStartEdgeSpaces and TrimEndEdgeSpaces are retained.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public TextLineBreakFlags AutowrapTrimFlags
    {
        get { CheckLabel(); return _autowrapTrimFlags; }
        set { EnsureMutable(); Set(ref _autowrapTrimFlags, value & (TextLineBreakFlags.TrimIndent | TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces)); }
    }
    /// <summary>Gets or sets an optional borrowed font and effects resource.</summary><value>Null initially; equal identities are silent.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public LabelSettings? LabelSettings
    {
        get { CheckLabel(); return _labelSettings; }
        set
        {
            EnsureMutable(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
            if (ReferenceEquals(_labelSettings, value)) return;
            if (_labelSettings is not null) { _labelSettings.Changed -= _resourceChanged; _labelSettings.Disposed -= _resourceDisposed; }
            _labelSettings = value;
            if (value is not null) { value.Changed += _resourceChanged; value.Disposed += _resourceDisposed; }
            Invalidate();
        }
    }
    /// <summary>Gets or sets the language used for shaping and casing.</summary>
    /// <value>Empty initially. An empty value uses the translation domain's locale override, then the current culture, then the tool locale.</value>
    /// <exception cref="ArgumentException">The language is null or contains NUL.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public string Language
    {
        get { CheckLabel(); return _language; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (value.Contains('\0')) throw new ArgumentException("Language must not contain NUL.", nameof(value)); Set(ref _language, value, true); }
    }
    /// <summary>Gets or sets the escaped separator used to split paragraphs.</summary><value>The two-character escape \n initially; empty keeps one paragraph.</value>
    /// <exception cref="ArgumentNullException">The separator is null.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public string ParagraphSeparator
    {
        get { CheckLabel(); return _paragraphSeparator; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); Set(ref _paragraphSeparator, value, true); }
    }
    /// <summary>Gets or sets the ellipsis scalar.</summary><value>An ellipsis initially. Only the first scalar is retained; empty selects the default ellipsis.</value>
    /// <exception cref="ArgumentNullException">The supplied string is null.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public string EllipsisChar
    {
        get { CheckLabel(); return _ellipsisChar; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); Set(ref _ellipsisChar, PrefixScalars(value, 1)); }
    }
    /// <summary>Gets or replaces the typed custom/list-parser options.</summary><value>An independent empty array initially; List uses exactly one delimiter string.</value>
    /// <exception cref="ArgumentNullException">The array or an option is null.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public string[] StructuredTextBIDIOverrideOptions
    {
        get { CheckLabel(); return (string[])_structuredOptions.Clone(); }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value); foreach (var option in value) ArgumentNullException.ThrowIfNull(option);
            if (_structuredOptions.AsSpan().SequenceEqual(value)) return;
            _structuredOptions = (string[])value.Clone(); _structuredOptionsView = Array.AsReadOnly(_structuredOptions); Invalidate(true);
        }
    }
    /// <summary>Gets or replaces repeating tab-stop increments in logical pixels.</summary><value>An independent empty array initially.</value>
    /// <exception cref="ArgumentException">The array is null, an increment is nonfinite, or its complete cycle is not positive.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public float[] TabStops
    {
        get { CheckLabel(); return (float[])_tabStops.Clone(); }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value);
            double cycle = 0;
            foreach (var stop in value) { if (!float.IsFinite(stop)) throw new ArgumentException("Tab increments must be finite.", nameof(value)); cycle += stop; }
            if (value.Length > 0 && !(cycle > 0)) throw new ArgumentException("The complete tab cycle must advance by a positive amount.", nameof(value));
            if (_tabStops.AsSpan().SequenceEqual(value)) return; _tabStops = (float[])value.Clone(); Invalidate();
        }
    }
    /// <summary>Gets or sets the stored visible-character limit.</summary><value>Minus one initially. Negative limits display all text; the stored ratio follows the supplied count.</value>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public int VisibleCharacters
    {
        get { CheckLabel(); return _visibleCharacters; }
        set { EnsureMutable(); if (_visibleCharacters == value) return; _visibleCharacters = value; var total = GetTotalCharacterCount(); _visibleRatio = value == -1 || total == 0 ? 1 : (float)value / total; Invalidate(_visibleCharactersBehavior == TextVisibleCharactersBehavior.CharsBeforeShaping); }
    }
    /// <summary>Gets or sets the visible fraction of characters or glyphs.</summary><value>One initially. Assignments clamp to zero through one and update VisibleCharacters.</value>
    /// <exception cref="ArgumentOutOfRangeException">The ratio is not finite.</exception>
    /// <exception cref="InvalidOperationException">An attached mutation is off its owner thread or occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The label is disposed.</exception>
    public float VisibleRatio
    {
        get { CheckLabel(); return _visibleRatio; }
        set
        {
            EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            var ratio = Math.Clamp(value, 0, 1); if (_visibleRatio == ratio) return;
            _visibleRatio = ratio; _visibleCharacters = ratio >= 1 ? -1 : RatioCount(ratio); Invalidate(_visibleCharactersBehavior == TextVisibleCharactersBehavior.CharsBeforeShaping);
        }
    }

    /// <summary>Returns the number of logical Unicode scalars in the translated text, including whitespace.</summary><returns>The complete source character count.</returns>
    /// <exception cref="InvalidOperationException">An attached query is off the owner thread.</exception><exception cref="ObjectDisposedException">The label is disposed.</exception>
    public int GetTotalCharacterCount() { CheckLabel(); return CountScalars(_translatedText.AsSpan()); }
    /// <summary>Returns the shaped line count.</summary><returns>One while detached; the complete line count while in a tree.</returns>
    /// <exception cref="InvalidOperationException">An attached query is off the owner thread or a required theme font is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The label or a borrowed resource is disposed.</exception>
    public int GetLineCount() { CheckLabel(); if (!IsInsideTree) return 1; EnsureShaped(); return _lines.Count; }
    /// <summary>Returns the height of one line, or the maximum line height when the index is outside the line range.</summary>
    /// <param name="line">Line index, or minus one for the maximum.</param><returns>The pixel height, truncated to an integer.</returns>
    /// <exception cref="InvalidOperationException">An attached query is off the owner thread or a required theme font is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The label or a borrowed resource is disposed.</exception>
    public int GetLineHeight(int line = -1)
    {
        CheckLabel(); EnsureShaped(); var height = _fontHeight;
        if ((uint)line < (uint)_lines.Count) height = _lines[line].Height; else foreach (var item in _lines) height = Math.Max(height, item.Height);
        return checked((int)height);
    }
    /// <summary>Returns the number of complete lines fitting in the control after skipping and line limits.</summary><returns>The visible line count.</returns>
    /// <exception cref="InvalidOperationException">An attached query is off the owner thread or a required theme font is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The label or a borrowed resource is disposed.</exception>
    public int GetVisibleLineCount() { CheckLabel(); EnsureShaped(); return VisibleLineCount(); }
    /// <summary>Returns the local rectangle occupied by a logical character's shaped cluster.</summary>
    /// <param name="position">The zero-based Unicode scalar index.</param><returns>The cluster rectangle, or an empty rectangle outside the laid-out visible lines.</returns>
    /// <exception cref="InvalidOperationException">An attached query is off the owner thread or a required theme font is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The label or a borrowed resource is disposed.</exception>
    public Rect2 GetCharacterBounds(int position) => CharacterBounds(position);

    private void CheckLabel() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static void ValidateAlignment(HorizontalAlignment value) { if (value is < HorizontalAlignment.Left or > HorizontalAlignment.Fill) throw new ArgumentOutOfRangeException(nameof(value)); }
    private void Set<T>(ref T field, T value, bool text = false) { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; Invalidate(text); }
    private int RatioCount(float ratio) => (int)Math.Clamp(Math.Truncate(GetTotalCharacterCount() * (double)ratio), int.MinValue, int.MaxValue);
    private static string PrefixScalars(string text, int count)
    {
        if (count < 0) return text; var length = 0; var index = 0;
        foreach (var rune in text.EnumerateRunes()) { if (index++ == count) break; length += rune.Utf16SequenceLength; }
        return length == text.Length ? text : text[..length];
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Label) ? CreateLabel : base.CreateSceneInstanceFactory();
    private static Node CreateLabel() => new Label();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _entryGeneration++; _resourceTree = null; _resourceDispatch = null; _resourcePending = 0;
            if (_labelSettings is not null) { _labelSettings.Changed -= _resourceChanged; _labelSettings.Disposed -= _resourceDisposed; }
            if (_font is not null) { _font.Changed -= _resourceChanged; _font.Disposed -= _resourceDisposed; }
            MaximumSizeChanged -= _maximumChanged; _paragraphs.Clear(); _lines.Clear(); _textCache.Clear();
        }
        base.Dispose(disposing);
    }
}
