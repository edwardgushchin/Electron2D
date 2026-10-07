namespace Electron2D;

public partial class CodeEdit
{
    /// <summary>Gets or sets automatic paired insertion, closing-key skip and paired backspace.</summary><value>False initially.</value>
    public bool AutoBraceCompletionEnabled { get { CheckCode(); return _autoPairs; } set { MutableCode(); if (_autoPairs == value) return; _autoPairs = value; DirtyCode(); } }
    /// <summary>Gets or sets matching and mismatched brace underlines.</summary><value>False initially.</value>
    public bool AutoBraceCompletionHighlightMatching { get { CheckCode(); return _highlightPairs; } set { MutableCode(); if (_highlightPairs == value) return; _highlightPairs = value; DirtyCode(); } }
    /// <summary>Gets or sets automatic completion requests after committed text.</summary><value>False initially.</value>
    public bool CodeCompletionEnabled { get { CheckCode(); return _completionEnabled; } set { MutableCode(); if (_completionEnabled == value) return; _completionEnabled = value; DirtyCode(); } }
    /// <summary>Gets or sets bookmark icons in the main gutter.</summary><value>False initially.</value>
    public bool GuttersDrawBookmarks { get { CheckCode(); return _drawBookmarks; } set { MutableCode(); if (_drawBookmarks == value) return; _drawBookmarks = value; DirtyCode(); } }
    /// <summary>Gets or sets breakpoint icons and pointer toggling in the main gutter.</summary><value>False initially.</value>
    public bool GuttersDrawBreakpointsGutter { get { CheckCode(); return _drawBreakpoints; } set { MutableCode(); if (_drawBreakpoints == value) return; _drawBreakpoints = value; DirtyCode(); } }
    /// <summary>Gets or sets execution markers in the main gutter.</summary><value>False initially.</value>
    public bool GuttersDrawExecutingLines { get { CheckCode(); return _drawExecuting; } set { MutableCode(); if (_drawExecuting == value) return; _drawExecuting = value; DirtyCode(); } }
    /// <summary>Gets or sets folding icons and pointer toggling.</summary><value>False initially.</value>
    public bool GuttersDrawFoldGutter { get { CheckCode(); return _drawFold; } set { MutableCode(); if (_drawFold == value) return; _drawFold = value; DirtyCode(); } }
    /// <summary>Gets or sets logical line-number presentation.</summary><value>False initially.</value>
    public bool GuttersDrawLineNumbers { get { CheckCode(); return _drawNumbers; } set { MutableCode(); if (_drawNumbers == value) return; _drawNumbers = value; DirtyCode(); } }
    /// <summary>Gets or sets zero padding instead of space padding in line numbers.</summary><value>False initially.</value>
    public bool GuttersZeroPadLineNumbers { get { CheckCode(); return _zeroPad; } set { MutableCode(); if (_zeroPad == value) return; _zeroPad = value; DirtyCode(); } }
    /// <summary>Gets or sets indentation inheritance and configured prefix expansion on newlines.</summary><value>False initially.</value>
    public bool IndentAutomatic { get { CheckCode(); return _autoIndent; } set { MutableCode(); if (_autoIndent == value) return; _autoIndent = value; DirtyCode(); } }
    /// <summary>Gets or sets space indentation instead of tab indentation.</summary><value>False initially.</value>
    public bool IndentUseSpaces { get { CheckCode(); return _useSpaces; } set { MutableCode(); if (_useSpaces == value) return; _useSpaces = value; DirtyCode(); } }
    /// <summary>Gets or sets code-region, delimiter-block and indentation folding.</summary><value>False initially.</value>
    public bool LineFolding { get { CheckCode(); return _lineFolding; } set { MutableCode(); if (_lineFolding == value) return; _lineFolding = value; if (!value) UnfoldAllLines(); DirtyCode(); } }
    /// <summary>Gets or sets validated symbol lookup on command-modified pointer clicks.</summary><value>False initially.</value>
    public bool SymbolLookupOnClick { get { CheckCode(); return _symbolLookup; } set { MutableCode(); if (_symbolLookup == value) return; _symbolLookup = value; SetSymbolLookupWordAsValid(false); DirtyCode(); } }
    /// <summary>Gets or sets symbol hover requests after the project tooltip delay.</summary><value>False initially.</value>
    public bool SymbolTooltipOnHover { get { CheckCode(); return _symbolTooltip; } set { MutableCode(); if (_symbolTooltip == value) return; _symbolTooltip = value; if (!value) _symbolTimer.Stop(); DirtyCode(); } }
    /// <summary>Gets or sets the positive indentation/tab width.</summary><value>Four initially.</value>
    public int IndentSize { get => GetTabSize(); set { MutableCode(); SetTabSize(value); DirtyCode(); } }
    /// <summary>Gets or sets the minimum number of line-number digits.</summary><value>Three initially.</value>
    public int GuttersLineNumbersMinDigits { get { CheckCode(); return _minDigits; } set { MutableCode(); _minDigits = value; DirtyCode(); } }
    /// <summary>Gets or sets copied single-scalar automatic indentation prefixes.</summary><value>Colon and opening braces initially.</value>
    public string[] IndentAutomaticPrefixes { get { CheckCode(); return (string[])_indentPrefixes.Clone(); } set { MutableCode(); var copy = CopyKeys(value, nameof(value)); if (copy.Any(s => CountScalars(s.AsSpan()) != 1)) throw new ArgumentException("Prefixes must be single scalars.", nameof(value)); _indentPrefixes = copy; } }
    /// <summary>Gets or sets copied single-scalar completion prefixes.</summary><value>Empty initially.</value>
    public string[] CodeCompletionPrefixes { get { CheckCode(); return (string[])_completionPrefixes.Clone(); } set { MutableCode(); var copy = CopyKeys(value, nameof(value)); if (copy.Any(s => CountScalars(s.AsSpan()) != 1)) throw new ArgumentException("Prefixes must be single scalars.", nameof(value)); _completionPrefixes = copy; } }
    /// <summary>Gets or sets independent brace-key mappings.</summary><value>Parentheses, brackets, braces and quotes initially.</value>
    public IReadOnlyDictionary<string, string> AutoBraceCompletionPairs { get { CheckCode(); return new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(new Dictionary<string, string>(_bracePairs, StringComparer.Ordinal)); } set { MutableCode(); ArgumentNullException.ThrowIfNull(value); var pairs = new Dictionary<string, string>(value, StringComparer.Ordinal); foreach (var pair in pairs) { RequireSymbols(pair.Key, false); RequireSymbols(pair.Value, false); } _bracePairs.Clear(); foreach (var pair in pairs) _bracePairs.Add(pair.Key, pair.Value); DirtyCode(); } }
    /// <summary>Gets or sets copied string delimiter descriptions encoded as start and optional end keys.</summary><value>Single and double quotes initially.</value>
    public string[] DelimiterStrings { get => DelimiterDescriptions(false); set => SetDelimiterDescriptions(value, false); }
    /// <summary>Gets or sets copied comment delimiter descriptions.</summary><value>Empty initially.</value>
    public string[] DelimiterComments { get => DelimiterDescriptions(true); set => SetDelimiterDescriptions(value, true); }
    /// <summary>Gets or sets copied nonnegative scalar guideline columns.</summary><value>Empty initially.</value>
    public int[] LineLengthGuidelines { get { CheckCode(); return (int[])_guidelines.Clone(); } set { MutableCode(); ArgumentNullException.ThrowIfNull(value); if (value.Any(v => v < 0)) throw new ArgumentOutOfRangeException(nameof(value)); _guidelines = (int[])value.Clone(); DirtyCode(); } }
}
