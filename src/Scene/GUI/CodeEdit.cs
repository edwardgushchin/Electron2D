using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Electron2D;

/// <summary>Edits source text with indentation, delimiters, folding, markers and typed completion.</summary>
/// <remarks>Logical lines and Unicode scalar columns share the TextEdit document/history. Fonts and option
/// icons are borrowed; parsing, authoring and first shaping are cold. Required gutters and hover timer belong
/// to this control. Completion providers are application callbacks rather than a built-in language parser.</remarks>
public partial class CodeEdit : TextEdit
{
    /// <summary>Identifies a submitted completion candidate.</summary>
    public enum CodeCompletionKind
    {
        /// <summary>A type.</summary>
        Class = 0,
        /// <summary>A function.</summary>
        Function = 1,
        /// <summary>A signal.</summary>
        Signal = 2,
        /// <summary>A variable.</summary>
        Variable = 3,
        /// <summary>A member.</summary>
        Member = 4,
        /// <summary>An enumeration.</summary>
        Enum = 5,
        /// <summary>A constant.</summary>
        Constant = 6,
        /// <summary>A node path.</summary>
        NodePath = 7,
        /// <summary>A file path.</summary>
        FilePath = 8,
        /// <summary>Plain text.</summary>
        PlainText = 9,
        /// <summary>A keyword.</summary>
        Keyword = 10
    }
    /// <summary>Defines completion-location sentinels; ancestor distance occupies values 1 through 256.</summary>
    public enum CodeCompletionLocation
    {
        /// <summary>The query's local scope.</summary>
        Local = 0,
        /// <summary>Maximum encoded ancestor distance.</summary>
        ParentMask = 256,
        /// <summary>Another user-code scope.</summary>
        OtherUserCode = 512,
        /// <summary>An external scope.</summary>
        Other = 1024
    }
    [Flags] private enum Marker { Breakpoint = 1, Bookmark = 2, Executing = 4, Folded = 8 }
    private sealed record Delimiter(string Start, string End, bool LineOnly, bool Comment);
    private sealed class Region(int index, Vector2i start)
    { internal readonly int Index = index; internal readonly Vector2i Start = start; internal Vector2i End = new(-1, -1); internal readonly List<(int Line, int From, int To)> Segments = []; }
    private readonly List<Delimiter> _delimiters = [];
    private readonly List<Region> _regions = [];
    private readonly Dictionary<string, string> _bracePairs = new(StringComparer.Ordinal) { ["("] = ")", ["["] = "]", ["{"] = "}", ["\""] = "\"", ["'"] = "'" };
    private readonly HashSet<int> _folded = [];
    private readonly HashSet<int> _breakpoints = [];
    private readonly List<TextLayout> _numberLayouts = [];
    private int _mainGutter = -1, _numberGutter = -1, _foldGutter = -1, _parsedVersion = -1, _preparedVersion = -1;
    private bool _codeDirty = true, _codeDisposing, _refreshing;
    private readonly OwnedSymbolTimer _symbolTimer;
    private bool _autoPairs, _highlightPairs, _completionEnabled, _drawBookmarks, _drawBreakpoints, _drawExecuting, _drawFold, _drawNumbers, _zeroPad, _autoIndent, _useSpaces, _lineFolding, _symbolLookup, _symbolTooltip;
    private int _minDigits = 3;
    private string[] _indentPrefixes = [":", "{", "[", "("], _completionPrefixes = [];
    private int[] _guidelines = [];
    private string _regionStart = "region", _regionEnd = "endregion", _hoverWord = "";
    private Vector2i _symbolPosition, _hoverPosition;
    private bool _symbolValid;
    private readonly bool _customCompletionFilter;
    /// <summary>Creates an LTR empty code document with three required gutters and an owned hover timer.</summary>
    public CodeEdit()
    {
        _customCompletionFilter = GetType().GetMethod(nameof(OnFilterCodeCompletionCandidates), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.DeclaringType != typeof(CodeEdit);
        LayoutDirection = LayoutDirection.LTR; TextDirection = TextDirection.LTR;
        AddStringDelimiter("\"", "\""); AddStringDelimiter("'", "'");
        AddCodeGutter("main_gutter", DrawMainGutter, true); AddCodeGutter("line_numbers", DrawLineNumber, false); AddCodeGutter("fold_gutter", DrawFoldGutter, false); RefreshGutterIndices();
        _symbolTimer = new(this) { Name = "_symbol_tooltip", WaitTime = .5, OneShot = true }; _symbolTimer.Timeout += _ => { if (_symbolTooltip && _hoverWord.Length > 0) SymbolHovered?.Invoke(_hoverWord, _hoverPosition.Y, _hoverPosition.X); }; AddChild(_symbolTimer, InternalMode.Front);
        GutterAdded += RefreshGutterIndices; GutterRemoved += RefreshGutterIndices; GutterClicked += GutterClick; TextChanged += CodeTextChanged; TextSet += CodeTextSet; CaretChanged += CodeCaretChanged;
    }
    private void CheckCode() => CheckTextEdit();
    private void MutableCode() => EnsureTextMutable();
    private void DirtyCode() { _codeDirty = true; _parsedVersion = -1; QueueRedraw(); }
    private void RequireLine(int line) => GetLine(line);
    private int LengthOf(int line) => CountScalars(GetLine(line).AsSpan());
    private void RequirePosition(int line, int column) { RequireLine(line); if ((uint)column > LengthOf(line)) throw new ArgumentOutOfRangeException(nameof(column)); }
    private static string[] CopyKeys(string[] values, string name) { ArgumentNullException.ThrowIfNull(values, name); if (values.Any(v => v == null)) throw new ArgumentException("Keys cannot contain null.", name); return (string[])values.Clone(); }
    private void AddCodeGutter(string name, Action<CanvasItem, int, int, Rect2> draw, bool overwrite)
    { var at = GetGutterCount(); AddGutter(); SetGutterName(at, name); SetGutterType(at, GutterType.Custom); SetGutterDraw(at, false); SetGutterOverwritable(at, overwrite); SetGutterCustomDraw(at, draw); }
    private void RefreshGutterIndices() { _mainGutter = _numberGutter = _foldGutter = -1; for (var i = 0; i < GetGutterCount(); i++) switch (GetGutterName(i)) { case "main_gutter": _mainGutter = i; break; case "line_numbers": _numberGutter = i; break; case "fold_gutter": _foldGutter = i; break; } _codeDirty = true; }
    private void CodeTextSet() { UnfoldAllLines(); ClearBookmarkedLines(); ClearExecutingLines(); CancelCodeCompletion(); _codeDirty = true; ClearBreakpointedLines(); }
    private void CodeTextChanged() { _parsedVersion = -1; _codeDirty = true; List<Exception>? errors = null; try { RefreshBreakpoints(); _folded.Clear(); foreach (var line in MarkedLines(Marker.Folded)) _folded.Add(line); if (_folded.Count > 0) RebuildHidden(); } catch (Exception error) { CollectException(ref errors, error); } try { if (_completionActive) FilterCompletion(); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("CodeEdit text observers failed.", errors); }
    private void CodeCaretChanged() { QueueRedraw(); if (_completionActive) FilterCompletion(); }
    /// <summary>Occurs when a breakpoint is set, cleared or shifted to another line.</summary>
    public event Action<int>? BreakpointToggled;
    /// <summary>Requests that the application submit completion candidates.</summary>
    public event Action? CodeCompletionRequested;
    /// <summary>Reports a symbol after the configured hover delay.</summary>
    public event Action<string, int, int>? SymbolHovered;
    /// <summary>Requests application lookup of a symbol at a logical line/scalar column.</summary>
    public event Action<string, int, int>? SymbolLookup;
    /// <summary>Requests validation of the symbol under the pointer.</summary>
    public event Action<string>? SymbolValidate;
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(CodeEdit) ? CreateCodeEdit : base.CreateSceneInstanceFactory();
    private static Node CreateCodeEdit() => new CodeEdit();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    { if (disposing) { _codeDisposing = true; BreakpointToggled = null; CodeCompletionRequested = null; SymbolHovered = SymbolLookup = null; SymbolValidate = null; _regions.Clear(); _folded.Clear(); _numberLayouts.Clear(); _submitted.Clear(); _options.Clear(); } base.Dispose(disposing); }
    private sealed class OwnedSymbolTimer(CodeEdit owner) : Timer
    { protected override void ValidateDisposal() { if (!owner.IsDisposed && !owner._codeDisposing) throw new InvalidOperationException("The hover timer belongs to CodeEdit."); base.ValidateDisposal(); } }
}
