using System.Globalization;
using System.Text;

namespace Electron2D;

/// <summary>A multiline Unicode editor with multiple carets, selections, history, gutters and syntax colors.</summary>
/// <remarks>Coordinates use logical lines and Unicode scalar columns. Attached mutations belong to the scene
/// owner; editing, first-seen shaping, history and highlight parsing are cold operations. Fonts/highlighters
/// are borrowed; the scrollbars and context menu are required owned children. Prepared drawing reuses buffers.</remarks>
public partial class TextEdit : Control
{
    /// <summary>Identifies groups of edits merged into one undo action.</summary>
    public enum EditAction
    { /// <summary>No active edit group.</summary>
        None = 0, /// <summary>Consecutive insertion.</summary>
        Typing = 1, /// <summary>Backward deletion.</summary>
        Backspace = 2, /// <summary>Forward deletion.</summary>
        Delete = 3
    }
    /// <summary>Selects how a gutter item is drawn.</summary>
    public enum GutterType
    { /// <summary>Shaped text.</summary>
        String = 0, /// <summary>Borrowed texture.</summary>
        Icon = 1, /// <summary>Typed custom drawing callback.</summary>
        Custom = 2
    }
    /// <summary>Controls wrapping at the available content boundary.</summary>
    public enum LineWrappingMode
    { /// <summary>Retains logical lines without wrapping.</summary>
        None = 0, /// <summary>Wraps to the content width.</summary>
        Boundary = 1
    }
    /// <summary>Identifies the current selection interaction.</summary>
    public enum SelectionMode
    { /// <summary>No selection gesture.</summary>
        None = 0, /// <summary>Keyboard extension.</summary>
        Shift = 1, /// <summary>Pointer drag.</summary>
        Pointer = 2, /// <summary>Word selection.</summary>
        Word = 3, /// <summary>Whole-line selection.</summary>
        Line = 4
    }
    /// <summary>Selects text-search matching and traversal policies.</summary>
    [Flags]
    public enum SearchFlags
    { /// <summary>Ordinal case-sensitive matching.</summary>
        MatchCase = 1, /// <summary>Requires word boundaries.</summary>
        WholeWords = 2, /// <summary>Searches toward preceding positions.</summary>
        Backwards = 4
    }
    private sealed class Line
    {
        internal string Text = "";
        internal readonly TextLayout Layout = new();
        internal readonly List<TextBIDIRange> Contexts = [];
        internal readonly List<GutterCell> Gutters = [];
        internal Color Background = new(0, 0, 0, 0);
        internal bool Hidden = false;
        internal int Scalars, Index;
        internal float Y;
        internal string Shown = "";
        internal Color[] Colors = [];
        internal int[] Characters = [];
    }
    private sealed class Caret
    {
        internal Vector2i Position, Origin;
        internal int Wrap;
        internal bool Selected, Ignored;
        internal float DesiredX = -1;
        internal Caret Copy() => new() { Position = Position, Origin = Origin, Wrap = Wrap, Selected = Selected, Ignored = Ignored, DesiredX = DesiredX };
    }
    private readonly List<Line> _lines = [new()];
    private readonly List<Caret> _carets = [new()];
    private readonly List<Gutter> _gutters = [];
    private readonly List<History> _history = [];
    private readonly List<VisualLine> _visual = [];
    private readonly List<Vector2> _selectionRanges = [];
    private readonly OwnedVScroll _vBar;
    private readonly OwnedHScroll _hBar;
    private readonly OwnedMenu _menu;
    private readonly TextLayout _placeholderLayout = new();
    private int _resourcePending;
    private readonly HashSet<Texture> _textures = [];
    private bool _texturesResident;
    private bool _layoutDirty = true, _building, _disposing, _drawing;
    private readonly int _historyLimit;
    private int _tabSize = 4, _version, _nextVersion, _savedVersion, _historyPosition, _complex, _multiEdit;
    private EditAction _action;
    private History? _operationBefore;
    private SelectionMode _selectionMode;
    private bool _overtype, _draggingCursor, _draggingMinimap, _dragAttempt;
    private Vector2i _pointerOrigin;
    private Vector2 _mouse;
    private string _ime = "";
    private Vector2i _imeSelection;
    private bool _caretVisible = true;
    private double _blinkTime;
    private string _searchText = "";
    private SearchFlags _searchFlags;
    private Func<Vector2, string>? _tooltip;
    private Font? _font;
    private int _fontSize = 16;
    private long _fontGeneration = -1;
    private float _rowHeight = 20, _contentWidth, _contentHeight, _horizontalTarget;
    private double _verticalTarget;
    private SyntaxHighlighter? _highlighter, _storedHighlighter;
    private readonly Action<Resource> _resourceChanged;
    private readonly Action<ElectronObject> _resourceDisposed;
    private readonly Action _emitChanges;
    private bool _changeQueued, _pendingTextChanged, _pendingTextSet, _pendingCaret;
    private int _pendingFrom = int.MaxValue, _pendingTo = -1;
    private readonly record struct VisualLine(int Line, int Wrap, float Y, float Height);
    private sealed record History(string[] Lines, Caret[] Carets, int Version, (Gutter Identity, GutterCell Cell)[][] Gutters, Color[] Backgrounds);
    /// <summary>Creates an editable empty document with one caret, context menu and owned scrollbars.</summary>
    public TextEdit()
    {
        _historyLimit = ProjectSettings.Get(ProjectSettings.TextEditUndoStackMaxSize);
        FocusMode = FocusMode.All; MouseDefaultCursorShape = CursorShape.IBeam; ClipContents = true;
        _resourceChanged = ResourceChanged; _resourceDisposed = ResourceDisposed; _emitChanges = EmitChanges;
        _vBar = new OwnedVScroll(this) { Name = "_v_scroll" }; _hBar = new OwnedHScroll(this) { Name = "_h_scroll" }; _menu = new OwnedMenu(this) { Name = "_text_menu" };
        AddChild(_vBar, InternalMode.Front); AddChild(_hBar, InternalMode.Front); AddChild(_menu, InternalMode.Front);
        _vBar.ValueChanged += value => { if (!_building) { _scrollVertical = value; _verticalTarget = value; QueueRedraw(); } };
        _hBar.ValueChanged += value => { if (!_building) { _scrollHorizontal = (int)value; _horizontalTarget = (float)value; QueueRedraw(); } };
        _menu.IDPressed += value => MenuOption((TextMenuAction)value); PrepareMenu(); _history.Add(Snapshot());
    }
    /// <summary>Gets or replaces the document's LF-normalized text, clearing history and secondary carets.</summary>
    /// <value>Empty initially; the document always retains one logical line.</value>
    public string Text
    {
        get { CheckTextEdit(); return string.Join('\n', _lines.Select(l => l.Text)); }
        set
        {
            EnsureTextMutable(); ArgumentNullException.ThrowIfNull(value); var normalized = Normalize(value); if (normalized == Text) return;
            CancelIME(); _draggingCursor = _draggingMinimap = _dragAttempt = false; _operationBefore = null; _action = EditAction.None; _complex = _multiEdit = 0; SetLines(normalized.Split('\n')); _carets.Clear(); _carets.Add(new()); _version = ++_nextVersion; _savedVersion = 0; _scrollHorizontal = 0; _scrollVertical = _verticalTarget = 0; _horizontalTarget = 0; _history.Clear(); _historyPosition = 0; _history.Add(Snapshot()); _pendingTextSet = true; Changed(0, _lines.Count - 1, true);
        }
    }
    /// <summary>Gets or sets a borrowed highlighter dedicated to this editor.</summary><value>Null initially.</value>
    public SyntaxHighlighter? SyntaxHighlighter
    {
        get { CheckTextEdit(); return _highlighter is { IsDisposed: false } ? _highlighter : null; }
        set
        {
            EnsureTextMutable(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_highlighter, value)) return;
            if (value?.GetTextEdit() is { } owner && owner != this) throw new InvalidOperationException("The syntax highlighter is already bound to another editor.");
            if (_highlighter != null) { _highlighter.Disposed -= _resourceDisposed; if (!_highlighter.IsDisposed) _highlighter.Bind(null); }
            if (_storedHighlighter != null && !ReferenceEquals(_storedHighlighter, value)) { var owned = _storedHighlighter; _storedHighlighter = null; owned.Dispose(); }
            _highlighter = value; if (value != null) { value.Disposed += _resourceDisposed; value.Bind(this); }
            InvalidateTextLayout();
        }
    }
    /// <summary>Occurs after a coalesced change to document content.</summary>
    public event Action? TextChanged;
    /// <summary>Occurs after replacing the complete document through Text.</summary>
    public event Action? TextSet;
    /// <summary>Occurs after caret/selection positions change.</summary>
    public event Action? CaretChanged;
    /// <summary>Reports an inclusive affected logical line range.</summary>
    public event Action<int, int>? LinesEditedFrom;
    /// <summary>Occurs after a gutter is added.</summary>
    public event Action? GutterAdded;
    /// <summary>Occurs after a gutter is removed.</summary>
    public event Action? GutterRemoved;
    /// <summary>Reports a clicked logical line and gutter index.</summary>
    public event Action<int, int>? GutterClicked;
    private void EnsureTextMutable() { EnsureMutable(); if (_drawing || _building) throw new InvalidOperationException("An active text editor cannot mutate its document or layout."); }
    private void CheckTextEdit() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    internal void InvalidateTextLayout() { if (IsDisposed || _disposing) return; _layoutDirty = true; QueueRedraw(); if (_scrollFitContentHeight || _scrollFitContentWidth) UpdateMinimumSize(); }
    private void ResourceChanged(Resource _) { if (!IsDisposed) Interlocked.Exchange(ref _resourcePending, 1); }
    private void ResourceDisposed(ElectronObject resource) => ResourceChanged((Resource)resource);
    private void Changed(int from, int to, bool text)
    { InvalidateTextLayout(); if (text) { _pendingTextChanged = true; _pendingFrom = Math.Min(_pendingFrom, from); _pendingTo = Math.Max(_pendingTo, to); } _pendingCaret = true; QueueChanges(); }
    private void QueueChanges() { if (_complex > 0 || _multiEdit > 0 || _operationBefore != null || _changeQueued) return; _changeQueued = true; if (Tree is { IsClosing: false } tree) tree.Defer(_emitChanges); else if (Tree is null) EmitChanges(); }
    private void EmitChanges()
    {
        _changeQueued = false; if (IsDisposed || _disposing) return; var textSet = _pendingTextSet; var changed = _pendingTextChanged; var caret = _pendingCaret; var from = _pendingFrom; var to = _pendingTo;
        _pendingTextSet = _pendingTextChanged = _pendingCaret = false; _pendingFrom = int.MaxValue; _pendingTo = -1; List<Exception>? errors = null;
        try { if (from != int.MaxValue) LinesEditedFrom?.Invoke(from, to); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed && textSet) TextSet?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed && changed) TextChanged?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed && caret) CaretChanged?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("TextEdit change callbacks failed.", errors);
    }
    private static string Normalize(string text) => text.Replace("\r", "");
    private Line AtLine(int line) { CheckTextEdit(); if ((uint)line >= _lines.Count) throw new ArgumentOutOfRangeException(nameof(line)); return _lines[line]; }
    private Caret AtCaret(int caret) { CheckTextEdit(); if ((uint)caret >= _carets.Count) throw new ArgumentOutOfRangeException(nameof(caret)); return _carets[caret]; }
    private static int ScalarIndex(string text, int column) { var index = 0; foreach (var rune in text.EnumerateRunes()) { if (column-- <= 0) break; index += rune.Utf16SequenceLength; } return index; }
    private static string Slice(string text, int from, int to) { var begin = ScalarIndex(text, from); return text.Substring(begin, ScalarIndex(text, to) - begin); }
    private int Offset(Vector2i position) { var offset = 0; for (var i = 0; i < position.Y; i++) offset += _lines[i].Scalars + 1; return offset + position.X; }
    private Vector2i PositionAt(int offset) { for (var i = 0; i < _lines.Count; i++) { if (offset <= _lines[i].Scalars) return new(Math.Max(0, offset), i); offset -= _lines[i].Scalars + 1; } return new(_lines[^1].Scalars, _lines.Count - 1); }
    private Vector2i ClampPosition(Vector2i position) { var line = Math.Clamp(position.Y, 0, _lines.Count - 1); return new(Math.Clamp(position.X, 0, _lines[line].Scalars), line); }
    private void ValidatePosition(int line, int column) { var value = AtLine(line); if ((uint)column > value.Scalars) throw new ArgumentOutOfRangeException(nameof(column)); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(TextEdit) ? CreateTextEdit : base.CreateSceneInstanceFactory();
    private static Node CreateTextEdit() => new TextEdit();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null;
        if (disposing)
        {
            _disposing = true;
            try { ActivateIME(false); } catch (Exception error) { CollectException(ref errors, error); }
            ReleaseTextures(); if (_font != null) { _font.Changed -= _resourceChanged; _font.Disposed -= _resourceDisposed; }
            if (_highlighter != null) { _highlighter.Disposed -= _resourceDisposed; if (!_highlighter.IsDisposed) try { _highlighter.Bind(null); } catch (Exception error) { CollectException(ref errors, error); } }
            try { _storedHighlighter?.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
            _storedHighlighter = null;
            _lines.Clear(); _carets.Clear(); _history.Clear(); _visual.Clear(); TextChanged = TextSet = CaretChanged = GutterAdded = GutterRemoved = null; LinesEditedFrom = GutterClicked = null;
        }
        try { base.Dispose(disposing); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("TextEdit disposal callbacks failed.", errors);
    }
}

/// <summary>Selects the shape of a multiline editor's caret.</summary>
public enum TextEditCaretType
{ /// <summary>A thin insertion line.</summary>
    Line = 0, /// <summary>A character-width overtype block.</summary>
    Block = 1
}
