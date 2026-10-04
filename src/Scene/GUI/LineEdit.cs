using System.Text;
namespace Electron2D;

/// <summary>Edits one shaped line of Unicode text with selection, caret, clipboard and IME input.</summary>
/// <remarks>Columns count Unicode scalars. Fonts and icons are borrowed. Programmatic Text assignments and
/// InsertTextAtCaret do not emit TextChanged. Native commits, deletion and Clear do. Attached mutations require
/// the scene owner thread and are rejected during capture. Popup menus and platform virtual keyboards are separate hosts.</remarks>
public partial class LineEdit : Control
{
    private string _text = "", _placeholderText = "", _secretCharacter = "•", _language = "", _ime = "";
    private int _caret, _maxLength, _selectionFrom, _selectionTo, _selectionAnchor;
    private Vector2i _imeSelection;
    private float _scroll, _blinkTime;
    private double _caretBlinkInterval = .65;
    private bool _editing, _selecting, _pointerSelecting, _clearPressed, _caretVisible = true, _dirty = true;
    private readonly TextLayout _layout = new(), _sourceLayout = new();
    private readonly List<TextBIDIRange> _contexts = [];
    private readonly List<Vector2> _selectionRanges = [];
    private readonly Action<Resource> _resourceChanged;
    private readonly Action<ElectronObject> _resourceDisposed;
    private int _resourcePending;
    private readonly List<EditState> _history = [];
    private int _historyPosition;
    private readonly record struct EditState(string Text, int Caret, float Scroll);
    private Font? _font;
    private long _fontGeneration = -1;
    private int _fontSize;
    private TextDirection _inputDirection = TextDirection.LTR;
    private HorizontalAlignment _alignment;
    private global::Electron2D.TextDirection _textDirection;
    private StructuredTextParser _structuredTextBIDIOverride;
    private string[] _structuredOptions = [];
    private Texture? _rightIcon, _residentIcon;
    private bool _selectAllOnRelease;
    private LineEditIconExpandMode _iconExpandMode;
    private float _rightIconScale = 1;

    /// <summary>Creates an empty editable field with keyboard focus and an I-beam cursor.</summary>
    public LineEdit() { _resourceChanged = ResourceChanged; _resourceDisposed = ResourceDisposed; FocusMode = FocusMode.All; MouseDefaultCursorShape = CursorShape.IBeam; _history.Add(new("", 0, 0)); }
    /// <summary>Occurs when user editing starts or ends through focus, submission or Editable changes.</summary>
    public event Action<bool>? EditingToggled;
    /// <summary>Occurs after a user edit, deletion or clear has committed its new value.</summary>
    public event Action<string>? TextChanged;
    /// <summary>Occurs when submission is requested while editing.</summary>
    public event Action<string>? TextSubmitted;
    /// <summary>Occurs when MaxLength rejects a suffix of an insertion.</summary>
    public event Action<string>? TextChangeRejected;
    /// <summary>Gets or replaces the text, resetting selection, scroll, caret and history without TextChanged.</summary>
    /// <value>Empty initially. MaxLength limits Unicode scalars; zero means unlimited.</value>
    public string Text { get { CheckLineEdit(); return _text; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); AssignText(value); } }
    /// <summary>Gets or sets the caret's scalar column, clamped to the current text length.</summary>
    /// <value>Zero initially. Direct assignments may address the interior of a grapheme.</value>
    public int CaretColumn { get { CheckLineEdit(); return _caret; } set { EnsureMutable(); _caret = Math.Clamp(value, 0, ScalarCount(_text)); ResetBlink(); FitCaret(); } }
    /// <summary>Gets or sets the scalar length limit and resets the current text through that limit.</summary>
    /// <value>Zero initially; zero is unlimited.</value>
    /// <exception cref="ArgumentOutOfRangeException">The limit is negative.</exception>
    public int MaxLength { get { CheckLineEdit(); return _maxLength; } set { EnsureMutable(); ArgumentOutOfRangeException.ThrowIfNegative(value); _maxLength = value; AssignText(_text); } }
    /// <summary>Gets or sets whether native edits are allowed. Programmatic assignments remain available.</summary>
    /// <value>True initially.</value>
    public bool Editable { get { CheckLineEdit(); return _editable; } set { EnsureMutable(); if (_editable == value) return; _editable = value; var was = _editing; if (!value) Unedit(); else if (IsInsideTree && HasFocus()) Edit(); Invalidate(); if (was != _editing) EditingToggled?.Invoke(_editing); } }
    private bool _editable = true;
    /// <summary>Gets or sets whether pointer and keyboard selection are allowed.</summary>
    /// <value>True initially. Disabling clears an existing selection.</value>
    public bool SelectingEnabled { get { CheckLineEdit(); return _selectingEnabled; } set { EnsureMutable(); if (_selectingEnabled == value) return; _selectingEnabled = value; if (!value) Deselect(); QueueRedraw(); } }
    private bool _selectingEnabled = true;
    /// <summary>Gets or sets a single-scalar password mask. An empty string displays the default bullet.</summary>
    /// <value>A bullet initially. Only the first scalar of an assigned value is retained.</value>
    public string SecretCharacter { get { CheckLineEdit(); return _secretCharacter; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); Set(ref _secretCharacter, ScalarSlice(value, 0, Math.Min(1, ScalarCount(value)))); } }
    /// <summary>Gets or sets paragraph writing direction.</summary>
    /// <value>Auto initially; Inherited follows layout direction.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside minus one through three.</exception>
    public global::Electron2D.TextDirection TextDirection { get { CheckLineEdit(); return _textDirection; } set { EnsureMutable(); if ((int)value is < -1 or > 3) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _textDirection, value); } }
    /// <summary>Gets or sets structured bidirectional context parsing.</summary>
    /// <value>Default initially. Unknown numeric values use the default parser.</value>
    /// <exception cref="NotSupportedException">The language-specific parser is selected.</exception>
    public StructuredTextParser StructuredTextBIDIOverride { get { CheckLineEdit(); return _structuredTextBIDIOverride; } set { EnsureMutable(); if ((int)value == 5) throw new NotSupportedException("Language-specific parsing is outside this text profile."); Set(ref _structuredTextBIDIOverride, value); } }
    /// <summary>Gets a copied read-only view of parser options, or replaces them with a defensive copy.</summary>
    /// <value>No options initially.</value>
    public string[] StructuredTextBIDIOverrideOptions { get { CheckLineEdit(); return (string[])_structuredOptions.Clone(); } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); var copy = value.ToArray(); foreach (var item in copy) ArgumentNullException.ThrowIfNull(item); _structuredOptions = copy; Invalidate(); } }
    /// <summary>Gets or sets the shaping language. Empty follows the current translation locale.</summary>
    /// <value>Empty initially.</value>
    /// <exception cref="ArgumentException">The language contains NUL.</exception>
    public string Language { get { CheckLineEdit(); return _language; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (value.Contains('\0')) throw new ArgumentException("Language contains NUL.", nameof(value)); Set(ref _language, value); } }
    /// <summary>Gets or sets the positive finite blink interval in seconds.</summary>
    /// <value>0.65 initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The interval is nonpositive or not finite.</exception>
    public double CaretBlinkInterval { get { CheckLineEdit(); return _caretBlinkInterval; } set { EnsureMutable(); if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _caretBlinkInterval = value; ResetBlink(); } }
    /// <summary>Gets or sets the borrowed trailing icon.</summary>
    /// <value>Null initially. A visible clear button takes precedence over this icon.</value>
    public Texture? RightIcon { get { CheckLineEdit(); return _rightIcon; } set { EnsureMutable(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_rightIcon, value)) return; if (_rightIcon is not null) { _rightIcon.Changed -= _resourceChanged; _rightIcon.Disposed -= _resourceDisposed; } _rightIcon = value; if (value is not null) { value.Changed += _resourceChanged; value.Disposed += _resourceDisposed; } Invalidate(); } }
    /// <summary>Gets or sets how the trailing icon fits the field.</summary>
    /// <value>OriginalSize initially. Unknown values use original size.</value>
    public LineEditIconExpandMode IconExpandMode { get { CheckLineEdit(); return _iconExpandMode; } set { EnsureMutable(); Set(ref _iconExpandMode, value); } }
    /// <summary>Gets or sets the finite multiplier for the trailing icon.</summary>
    /// <value>One initially.</value>
    public float RightIconScale { get { CheckLineEdit(); return _rightIconScale; } set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _rightIconScale, value); } }
    /// <summary>Gets or sets horizontal text placement, mirrored under RTL layout.</summary>
    /// <value>HorizontalAlignment.Left initially.</value>
    public HorizontalAlignment Alignment { get { CheckLineEdit(); return _alignment; } set { EnsureMutable(); if (value is < HorizontalAlignment.Left or > HorizontalAlignment.Fill) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _alignment, value); } }
    /// <summary>Gets or sets translated text displayed while the field and IME composition are empty.</summary>
    /// <value>Empty initially.</value>
    public string PlaceholderText { get { CheckLineEdit(); return _placeholderText; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); Set(ref _placeholderText, value); } }
    private bool _secret = false;
    /// <summary>Gets or sets whether displayed text uses SecretCharacter and clipboard copying is suppressed.</summary>
    /// <value>false initially.</value>
    public bool Secret { get { CheckLineEdit(); return _secret; } set { EnsureMutable(); Set(ref _secret, value); } }
    private bool _caretBlink = false;
    /// <summary>Gets or sets whether the caret blinks while editing.</summary>
    /// <value>false initially.</value>
    public bool CaretBlink { get { CheckLineEdit(); return _caretBlink; } set { EnsureMutable(); Set(ref _caretBlink, value); ResetBlink(); } }
    private bool _caretForceDisplayed = false;
    /// <summary>Gets or sets whether an editable caret is drawn while the field is not editing.</summary>
    /// <value>false initially.</value>
    public bool CaretForceDisplayed { get { CheckLineEdit(); return _caretForceDisplayed; } set { EnsureMutable(); Set(ref _caretForceDisplayed, value); } }
    private bool _caretMidGrapheme = false;
    /// <summary>Gets or sets whether pointer and arrow movement can address interior grapheme scalars.</summary>
    /// <value>false initially.</value>
    public bool CaretMidGrapheme { get { CheckLineEdit(); return _caretMidGrapheme; } set { EnsureMutable(); Set(ref _caretMidGrapheme, value); } }
    private bool _backspaceDeletesCompositeCharacterEnabled = false;
    /// <summary>Gets or sets whether backspace removes the preceding complete grapheme when CaretMidGrapheme is false.</summary>
    /// <value>false initially.</value>
    public bool BackspaceDeletesCompositeCharacterEnabled { get { CheckLineEdit(); return _backspaceDeletesCompositeCharacterEnabled; } set { EnsureMutable(); Set(ref _backspaceDeletesCompositeCharacterEnabled, value); } }
    private bool _clearButtonEnabled = false;
    /// <summary>Gets or sets whether a nonempty editable field displays a clear button.</summary>
    /// <value>false initially.</value>
    public bool ClearButtonEnabled { get { CheckLineEdit(); return _clearButtonEnabled; } set { EnsureMutable(); Set(ref _clearButtonEnabled, value); } }
    private bool _deselectOnFocusLossEnabled = true;
    /// <summary>Gets or sets whether ending editing clears selection.</summary>
    /// <value>true initially.</value>
    public bool DeselectOnFocusLossEnabled { get { CheckLineEdit(); return _deselectOnFocusLossEnabled; } set { EnsureMutable(); Set(ref _deselectOnFocusLossEnabled, value); if (value && (!IsInsideTree || !HasFocus())) Deselect(); } }
    private bool _dragAndDropSelectionEnabled = true;
    /// <summary>Gets or sets whether selected text can be dragged and strings dropped into this editable field.</summary>
    /// <value>true initially.</value>
    public bool DragAndDropSelectionEnabled { get { CheckLineEdit(); return _dragAndDropSelectionEnabled; } set { EnsureMutable(); Set(ref _dragAndDropSelectionEnabled, value); } }
    private bool _expandToTextLength = false;
    /// <summary>Gets or sets whether natural text width contributes to minimum width.</summary>
    /// <value>false initially.</value>
    public bool ExpandToTextLength { get { CheckLineEdit(); return _expandToTextLength; } set { EnsureMutable(); Set(ref _expandToTextLength, value); } }
    private bool _flat = false;
    /// <summary>Gets or sets whether the normal or read-only background is omitted.</summary>
    /// <value>false initially.</value>
    public bool Flat { get { CheckLineEdit(); return _flat; } set { EnsureMutable(); Set(ref _flat, value); } }
    private bool _keepEditingOnTextSubmit = false;
    /// <summary>Gets or sets whether submission leaves editing active.</summary>
    /// <value>false initially.</value>
    public bool KeepEditingOnTextSubmit { get { CheckLineEdit(); return _keepEditingOnTextSubmit; } set { EnsureMutable(); Set(ref _keepEditingOnTextSubmit, value); } }
    private bool _middleMousePasteEnabled = true;
    /// <summary>Gets or sets whether the middle button pastes the primary clipboard.</summary>
    /// <value>true initially.</value>
    public bool MiddleMousePasteEnabled { get { CheckLineEdit(); return _middleMousePasteEnabled; } set { EnsureMutable(); Set(ref _middleMousePasteEnabled, value); } }
    private bool _selectAllOnFocus = false;
    /// <summary>Gets or sets whether entering editing through focus selects the full line.</summary>
    /// <value>false initially.</value>
    public bool SelectAllOnFocus { get { CheckLineEdit(); return _selectAllOnFocus; } set { EnsureMutable(); Set(ref _selectAllOnFocus, value); } }
    private bool _shortcutKeysEnabled = true;
    /// <summary>Gets or sets whether selection, clipboard and undo keyboard shortcuts are active.</summary>
    /// <value>true initially.</value>
    public bool ShortcutKeysEnabled { get { CheckLineEdit(); return _shortcutKeysEnabled; } set { EnsureMutable(); Set(ref _shortcutKeysEnabled, value); } }

    private bool _drawControlChars;
    /// <summary>Gets or sets whether nonprinting control scalars display hexadecimal placeholder glyphs.</summary>
    /// <value>False initially.</value>
    public bool DrawControlChars { get { CheckLineEdit(); return _drawControlChars; } set { EnsureMutable(); Set(ref _drawControlChars, value); } }
    private void ResourceChanged(Resource resource)
    {
        if (IsDisposed) return; Interlocked.Exchange(ref _resourcePending, 1);
    }
    private void ResourceDisposed(ElectronObject resource) => ResourceChanged((Resource)resource);
    private void CheckLineEdit() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void Set<T>(ref T field, T value) { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; Invalidate(); }
    private void Invalidate() { _dirty = true; QueueRedraw(); if (Tree?.IsClosing != true) UpdateMinimumSize(); }
    private void ResetBlink() { _blinkTime = 0; _caretVisible = true; QueueRedraw(); }
    private static int ScalarCount(string text) { var count = 0; foreach (var rune in text.EnumerateRunes()) count++; return count; }
    private static int UTF16Index(string text, int column) { var index = 0; foreach (var rune in text.EnumerateRunes()) { if (column-- <= 0) break; index += rune.Utf16SequenceLength; } return index; }
    private static string ScalarSlice(string text, int from, int to) => text[UTF16Index(text, from)..UTF16Index(text, to)];
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(LineEdit) ? CreateLineEdit : base.CreateSceneInstanceFactory();
    private static Node CreateLineEdit() => new LineEdit();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { ReleaseIconResidency(); if (_font is not null) { _font.Changed -= _resourceChanged; _font.Disposed -= _resourceDisposed; } if (_rightIcon is not null) { _rightIcon.Changed -= _resourceChanged; _rightIcon.Disposed -= _resourceDisposed; } EditingToggled = null; TextChanged = null; TextSubmitted = null; TextChangeRejected = null; _history.Clear(); } base.Dispose(disposing); }
}

/// <summary>Controls how a LineEdit trailing icon fits its available height.</summary>
public enum LineEditIconExpandMode
{
    /// <summary>Uses the icon's natural dimensions.</summary>
    OriginalSize = 0,
    /// <summary>Uses a font-height square.</summary>
    FitToText = 1,
    /// <summary>Fits the field bounds while preserving aspect ratio, then applies RightIconScale.</summary>
    FitToLineEdit = 2
}
