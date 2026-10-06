namespace Electron2D;

public partial class TextEdit
{
    private TextAutowrapMode _autowrapMode = TextAutowrapMode.WordSmart;
    private bool _backspaceDeletesCompositeCharacterEnabled = false;
    private bool _caretBlink = false;
    private double _caretBlinkInterval = 0.65;
    private bool _caretDrawWhenEditableDisabled = false;
    private bool _caretMidGrapheme = false;
    private bool _caretMoveOnRightClick = true;
    private bool _caretMultiple = true;
    private TextEditCaretType _caretType = default;
    private bool _contextMenuEnabled = true;
    private string _customWordSeparators = "";
    private bool _deselectOnFocusLossEnabled = true;
    private bool _dragAndDropSelectionEnabled = true;
    private bool _drawControlChars = false;
    private bool _drawSpaces = false;
    private bool _drawTabs = false;
    private bool _editable = true;
    private bool _emptySelectionClipboardEnabled = true;
    private bool _highlightAllOccurrences = false;
    private bool _highlightCurrentLine = false;
    private bool _indentWrappedLines = false;
    private string _language = "";
    private bool _middleMousePasteEnabled = true;
    private bool _minimapDraw = false;
    private int _minimapWidth = 80;
    private string _placeholderText = "";
    private bool _scrollFitContentHeight = false;
    private bool _scrollFitContentWidth = false;
    private int _scrollHorizontal = 0;
    private bool _scrollPastEndOfFile = false;
    private bool _scrollSmooth = false;
    private double _scrollVScrollSpeed = 80.0;
    private double _scrollVertical = 0.0;
    private bool _selectingEnabled = true;
    private bool _shortcutKeysEnabled = true;
    private StructuredTextParser _structuredTextBIDIOverride = StructuredTextParser.Default;
    private string[] _structuredTextBIDIOverrideOptions = [];
    private bool _tabInputMode = true;
    private TextDirection _textDirection = global::Electron2D.TextDirection.Auto;
    private bool _useCustomWordSeparators = false;
    private bool _useDefaultWordSeparators = true;
    private LineWrappingMode _wrapMode = default;
    /// <summary>Gets or sets autowrap mode for the multiline editor.</summary><value>TextAutowrapMode.WordSmart initially.</value>
    public TextAutowrapMode AutowrapMode { get { CheckTextEdit(); return _autowrapMode; } set { EnsureTextMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _autowrapMode = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets backspace deletes composite character enabled for the multiline editor.</summary><value>false initially.</value>
    public bool BackspaceDeletesCompositeCharacterEnabled { get { CheckTextEdit(); return _backspaceDeletesCompositeCharacterEnabled; } set { EnsureTextMutable(); _backspaceDeletesCompositeCharacterEnabled = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets caret blink for the multiline editor.</summary><value>false initially.</value>
    public bool CaretBlink { get { CheckTextEdit(); return _caretBlink; } set { EnsureTextMutable(); _caretBlink = value; ResetBlink(); } }
    /// <summary>Gets or sets caret blink interval for the multiline editor.</summary><value>0.65 initially.</value>
    public double CaretBlinkInterval { get { CheckTextEdit(); return _caretBlinkInterval; } set { EnsureTextMutable(); if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _caretBlinkInterval = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets caret draw when editable disabled for the multiline editor.</summary><value>false initially.</value>
    public bool CaretDrawWhenEditableDisabled { get { CheckTextEdit(); return _caretDrawWhenEditableDisabled; } set { EnsureTextMutable(); _caretDrawWhenEditableDisabled = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets caret mid grapheme for the multiline editor.</summary><value>false initially.</value>
    public bool CaretMidGrapheme { get { CheckTextEdit(); return _caretMidGrapheme; } set { EnsureTextMutable(); _caretMidGrapheme = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets caret move on right click for the multiline editor.</summary><value>true initially.</value>
    public bool CaretMoveOnRightClick { get { CheckTextEdit(); return _caretMoveOnRightClick; } set { EnsureTextMutable(); _caretMoveOnRightClick = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets caret multiple for the multiline editor.</summary><value>true initially.</value>
    public bool CaretMultiple { get { CheckTextEdit(); return _caretMultiple; } set { EnsureTextMutable(); _caretMultiple = value; InvalidateTextLayout(); if (!value) RemoveSecondaryCarets(); } }
    /// <summary>Gets or sets caret type for the multiline editor.</summary><value>default initially.</value>
    public TextEditCaretType CaretType { get { CheckTextEdit(); return _caretType; } set { EnsureTextMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _caretType = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets context menu enabled for the multiline editor.</summary><value>true initially.</value>
    public bool ContextMenuEnabled { get { CheckTextEdit(); return _contextMenuEnabled; } set { EnsureTextMutable(); _contextMenuEnabled = value; InvalidateTextLayout(); PrepareMenu(); } }
    /// <summary>Gets or sets custom word separators for the multiline editor.</summary><value>"" initially.</value>
    public string CustomWordSeparators { get { CheckTextEdit(); return _customWordSeparators; } set { EnsureTextMutable(); ArgumentNullException.ThrowIfNull(value); _customWordSeparators = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets deselect on focus loss enabled for the multiline editor.</summary><value>true initially.</value>
    public bool DeselectOnFocusLossEnabled { get { CheckTextEdit(); return _deselectOnFocusLossEnabled; } set { EnsureTextMutable(); _deselectOnFocusLossEnabled = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets drag and drop selection enabled for the multiline editor.</summary><value>true initially.</value>
    public bool DragAndDropSelectionEnabled { get { CheckTextEdit(); return _dragAndDropSelectionEnabled; } set { EnsureTextMutable(); _dragAndDropSelectionEnabled = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets draw control chars for the multiline editor.</summary><value>false initially.</value>
    public bool DrawControlChars { get { CheckTextEdit(); return _drawControlChars; } set { EnsureTextMutable(); _drawControlChars = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets draw spaces for the multiline editor.</summary><value>false initially.</value>
    public bool DrawSpaces { get { CheckTextEdit(); return _drawSpaces; } set { EnsureTextMutable(); _drawSpaces = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets draw tabs for the multiline editor.</summary><value>false initially.</value>
    public bool DrawTabs { get { CheckTextEdit(); return _drawTabs; } set { EnsureTextMutable(); _drawTabs = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets editable for the multiline editor.</summary><value>true initially.</value>
    public bool Editable { get { CheckTextEdit(); return _editable; } set { EnsureTextMutable(); _editable = value; if (_highlighter is { IsDisposed: false }) _highlighter.ClearHighlightingCache(); InvalidateTextLayout(); if (!value) CancelIME(); ActivateIME(value && HasFocus()); PrepareMenu(); } }
    /// <summary>Gets or sets empty selection clipboard enabled for the multiline editor.</summary><value>true initially.</value>
    public bool EmptySelectionClipboardEnabled { get { CheckTextEdit(); return _emptySelectionClipboardEnabled; } set { EnsureTextMutable(); _emptySelectionClipboardEnabled = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets highlight all occurrences for the multiline editor.</summary><value>false initially.</value>
    public bool HighlightAllOccurrences { get { CheckTextEdit(); return _highlightAllOccurrences; } set { EnsureTextMutable(); _highlightAllOccurrences = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets highlight current line for the multiline editor.</summary><value>false initially.</value>
    public bool HighlightCurrentLine { get { CheckTextEdit(); return _highlightCurrentLine; } set { EnsureTextMutable(); _highlightCurrentLine = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets indent wrapped lines for the multiline editor.</summary><value>false initially.</value>
    public bool IndentWrappedLines { get { CheckTextEdit(); return _indentWrappedLines; } set { EnsureTextMutable(); _indentWrappedLines = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets language for the multiline editor.</summary><value>"" initially.</value>
    public string Language { get { CheckTextEdit(); return _language; } set { EnsureTextMutable(); ArgumentNullException.ThrowIfNull(value); _language = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets middle mouse paste enabled for the multiline editor.</summary><value>true initially.</value>
    public bool MiddleMousePasteEnabled { get { CheckTextEdit(); return _middleMousePasteEnabled; } set { EnsureTextMutable(); _middleMousePasteEnabled = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets minimap draw for the multiline editor.</summary><value>false initially.</value>
    public bool MinimapDraw { get { CheckTextEdit(); return _minimapDraw; } set { EnsureTextMutable(); _minimapDraw = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets minimap width for the multiline editor.</summary><value>80 initially.</value>
    public int MinimapWidth { get { CheckTextEdit(); return _minimapWidth; } set { EnsureTextMutable(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _minimapWidth = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets placeholder text for the multiline editor.</summary><value>"" initially.</value>
    public string PlaceholderText { get { CheckTextEdit(); return _placeholderText; } set { EnsureTextMutable(); ArgumentNullException.ThrowIfNull(value); _placeholderText = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets scroll fit content height for the multiline editor.</summary><value>false initially.</value>
    public bool ScrollFitContentHeight { get { CheckTextEdit(); return _scrollFitContentHeight; } set { EnsureTextMutable(); _scrollFitContentHeight = value; InvalidateTextLayout(); UpdateMinimumSize(); } }
    /// <summary>Gets or sets scroll fit content width for the multiline editor.</summary><value>false initially.</value>
    public bool ScrollFitContentWidth { get { CheckTextEdit(); return _scrollFitContentWidth; } set { EnsureTextMutable(); _scrollFitContentWidth = value; InvalidateTextLayout(); UpdateMinimumSize(); } }
    /// <summary>Gets or sets scroll horizontal for the multiline editor.</summary><value>0 initially.</value>
    public int ScrollHorizontal { get { CheckTextEdit(); return _scrollHorizontal; } set { EnsureTextMutable(); RequestHorizontalScroll(value); } }
    /// <summary>Gets or sets scroll past end of file for the multiline editor.</summary><value>false initially.</value>
    public bool ScrollPastEndOfFile { get { CheckTextEdit(); return _scrollPastEndOfFile; } set { EnsureTextMutable(); _scrollPastEndOfFile = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets scroll smooth for the multiline editor.</summary><value>false initially.</value>
    public bool ScrollSmooth { get { CheckTextEdit(); return _scrollSmooth; } set { EnsureTextMutable(); _scrollSmooth = value; if (!value) { _scrollVertical = _verticalTarget; _vBar.SetValueNoSignal(_scrollVertical); } UpdateProcessing(); QueueRedraw(); } }
    /// <summary>Gets or sets scroll v scroll speed for the multiline editor.</summary><value>80.0 initially.</value>
    public double ScrollVScrollSpeed { get { CheckTextEdit(); return _scrollVScrollSpeed; } set { EnsureTextMutable(); if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _scrollVScrollSpeed = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets scroll vertical for the multiline editor.</summary><value>0.0 initially.</value>
    public double ScrollVertical { get { CheckTextEdit(); return _scrollVertical; } set { EnsureTextMutable(); if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); RequestVerticalScroll(value); } }
    /// <summary>Gets or sets selecting enabled for the multiline editor.</summary><value>true initially.</value>
    public bool SelectingEnabled { get { CheckTextEdit(); return _selectingEnabled; } set { EnsureTextMutable(); _selectingEnabled = value; if (!value) Deselect(); InvalidateTextLayout(); } }
    /// <summary>Gets or sets shortcut keys enabled for the multiline editor.</summary><value>true initially.</value>
    public bool ShortcutKeysEnabled { get { CheckTextEdit(); return _shortcutKeysEnabled; } set { EnsureTextMutable(); _shortcutKeysEnabled = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets structured text bidi override for the multiline editor.</summary><value>StructuredTextParser.Default initially.</value>
    public StructuredTextParser StructuredTextBIDIOverride { get { CheckTextEdit(); return _structuredTextBIDIOverride; } set { EnsureTextMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _structuredTextBIDIOverride = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets structured text bidi override options for the multiline editor.</summary><value>[] initially.</value>
    public string[] StructuredTextBIDIOverrideOptions { get { CheckTextEdit(); return (string[])_structuredTextBIDIOverrideOptions.Clone(); } set { EnsureTextMutable(); ArgumentNullException.ThrowIfNull(value); _structuredTextBIDIOverrideOptions = (string[])value.Clone(); InvalidateTextLayout(); } }
    /// <summary>Gets or sets tab input mode for the multiline editor.</summary><value>true initially.</value>
    public bool TabInputMode { get { CheckTextEdit(); return _tabInputMode; } set { EnsureTextMutable(); _tabInputMode = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets text direction for the multiline editor.</summary><value>global::Electron2D.TextDirection.Auto initially.</value>
    public TextDirection TextDirection { get { CheckTextEdit(); return _textDirection; } set { EnsureTextMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _textDirection = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets use custom word separators for the multiline editor.</summary><value>false initially.</value>
    public bool UseCustomWordSeparators { get { CheckTextEdit(); return _useCustomWordSeparators; } set { EnsureTextMutable(); _useCustomWordSeparators = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets use default word separators for the multiline editor.</summary><value>true initially.</value>
    public bool UseDefaultWordSeparators { get { CheckTextEdit(); return _useDefaultWordSeparators; } set { EnsureTextMutable(); _useDefaultWordSeparators = value; InvalidateTextLayout(); } }
    /// <summary>Gets or sets wrap mode for the multiline editor.</summary><value>default initially.</value>
    public LineWrappingMode WrapMode { get { CheckTextEdit(); return _wrapMode; } set { EnsureTextMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _wrapMode = value; InvalidateTextLayout(); } }
}
