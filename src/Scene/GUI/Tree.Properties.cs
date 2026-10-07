namespace Electron2D;

public partial class Tree
{
    /// <summary>Gets or resizes the positive number of cell columns.</summary><value>One initially.</value>
    public int Columns { get { CheckTree(); return _columns.Count; } set { EnsureTreeMutable(); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); if (value == _columns.Count) return; if (_columns.Count > value) _columns.RemoveRange(value, _columns.Count - value); while (_columns.Count < value) _columns.Add(new()); for (var item = _root; item != null; item = item.WalkNext(false, true)) item.ResizeCells(value); if (_selectedColumn >= value) _selectedColumn = value - 1; if (_editedColumn >= value) CancelEditor(); InvalidateTree(); } }
    /// <summary>Gets or sets the cell/row/multiple selection policy.</summary><value>Single initially.</value>
    public SelectMode SelectionMode { get { CheckTree(); return _selectMode; } set { EnsureTreeMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_selectMode == value) return; _selectMode = value; DeselectAll(); _selected = null; QueueRedraw(); } }
    /// <summary>Gets or sets whether selecting an already selected cell publishes selection again.</summary><value>False initially.</value>
    public bool AllowReselect { get { CheckTree(); return _allowReselect; } set { EnsureTreeMutable(); _allowReselect = value; } }
    /// <summary>Gets or sets right-button selection before context actions.</summary><value>False initially.</value>
    public bool AllowRMBSelect { get { CheckTree(); return _allowRMBSelect; } set { EnsureTreeMutable(); _allowRMBSelect = value; } }
    /// <summary>Gets or sets timed incremental text search.</summary><value>True initially.</value>
    public bool AllowSearch { get { CheckTree(); return _allowSearch; } set { EnsureTreeMutable(); _allowSearch = value; _search = ""; } }
    /// <summary>Gets or sets source-text fallback for empty explicit tooltips.</summary><value>True initially.</value>
    public bool AutoTooltip { get { CheckTree(); return _autoTooltip; } set { EnsureTreeMutable(); _autoTooltip = value; } }
    /// <summary>Gets or sets column header presentation.</summary><value>False initially.</value>
    public bool ColumnTitlesVisible { get { CheckTree(); return _titlesVisible; } set { EnsureTreeMutable(); _titlesVisible = value; InvalidateTree(); } }
    /// <summary>Gets or sets drop feedback flags.</summary><value>Disabled initially.</value>
    public TreeDropModeFlags DropModeFlags { get { CheckTree(); return _dropMode; } set { EnsureTreeMutable(); if ((value & ~(TreeDropModeFlags.OnItem | TreeDropModeFlags.Inbetween)) != 0) throw new ArgumentOutOfRangeException(nameof(value)); _dropMode = value; QueueRedraw(); } }
    /// <summary>Gets or sets timed unfolding while a drop hovers a folded branch.</summary><value>True initially.</value>
    public bool EnableDragUnfolding { get { CheckTree(); return _dragUnfold; } set { EnsureTreeMutable(); _dragUnfold = value; _unfoldItem = null; } }
    /// <summary>Gets or sets recursive folding with modified pointer/keyboard input.</summary><value>True initially.</value>
    public bool EnableRecursiveFolding { get { CheckTree(); return _recursiveFold; } set { EnsureTreeMutable(); _recursiveFold = value; } }
    /// <summary>Gets or sets whether all fold affordances are hidden.</summary><value>False initially.</value>
    public bool HideFolding { get { CheckTree(); return _hideFolding; } set { EnsureTreeMutable(); _hideFolding = value; InvalidateTree(); } }
    /// <summary>Gets or sets whether the root row is omitted while its visible children are presented.</summary><value>False initially.</value>
    public bool HideRoot { get { CheckTree(); return _hideRoot; } set { EnsureTreeMutable(); _hideRoot = value; InvalidateTree(); } }
    /// <summary>Gets or sets eligible vertical scroll hints.</summary><value>Disabled initially.</value>
    public VerticalScrollHintMode HintMode { get { CheckTree(); return _hintMode; } set { EnsureTreeMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _hintMode = value; QueueRedraw(); } }
    /// <summary>Gets or sets horizontal scrollbar operation.</summary><value>True initially.</value>
    public bool ScrollHorizontalEnabled { get { CheckTree(); return _scrollH; } set { EnsureTreeMutable(); _scrollH = value; if (!value) _hBar.SetValueNoSignal(0); InvalidateTree(); } }
    /// <summary>Gets or sets vertical scrollbar operation.</summary><value>True initially.</value>
    public bool ScrollVerticalEnabled { get { CheckTree(); return _scrollV; } set { EnsureTreeMutable(); _scrollV = value; if (!value) _vBar.SetValueNoSignal(0); InvalidateTree(); } }
    /// <summary>Gets or sets repeated scroll-hint texture presentation.</summary><value>False initially.</value>
    public bool TileScrollHint { get { CheckTree(); return _tileHint; } set { EnsureTreeMutable(); _tileHint = value; QueueRedraw(); } }
    private Column AtColumn(int column) { CheckTree(); if ((uint)column >= _columns.Count) throw new ArgumentOutOfRangeException(nameof(column)); return _columns[column]; }
    /// <summary>Sets a nonnegative explicit column minimum.</summary><param name="column">Existing column.</param><param name="minWidth">Pixel width.</param>
    public void SetColumnCustomMinimumWidth(int column, int minWidth) { EnsureTreeMutable(); if (minWidth < 0) throw new ArgumentOutOfRangeException(nameof(minWidth)); AtColumn(column).Minimum = minWidth; InvalidateTree(); }
    /// <summary>Returns the current fitted pixel column width, saturating at Int32.MaxValue.</summary><param name="column">Existing column.</param><returns>Pixel width.</returns>
    public int GetColumnWidth(int column) { AtColumn(column); EnsureTreeLayout(); return (int)Math.Min((double)_columns[column].Width, int.MaxValue); }
    /// <summary>Sets whether spare width expands a column.</summary><param name="column">Existing column.</param><param name="expand">Expansion policy.</param>
    public void SetColumnExpand(int column, bool expand) { EnsureTreeMutable(); AtColumn(column).Expand = expand; InvalidateTree(); }
    /// <summary>Reports column expansion.</summary><param name="column">Existing column.</param><returns>True initially.</returns>
    public bool IsColumnExpanding(int column) => AtColumn(column).Expand;
    /// <summary>Sets a positive column expansion ratio.</summary><param name="column">Existing column.</param><param name="ratio">Positive weight.</param>
    public void SetColumnExpandRatio(int column, int ratio) { EnsureTreeMutable(); if (ratio <= 0) throw new ArgumentOutOfRangeException(nameof(ratio)); AtColumn(column).Ratio = ratio; InvalidateTree(); }
    /// <summary>Returns a column expansion ratio.</summary><param name="column">Existing column.</param><returns>One initially.</returns>
    public int GetColumnExpandRatio(int column) => AtColumn(column).Ratio;
    /// <summary>Sets whether intrinsic text width contributes to a column minimum.</summary><param name="column">Existing column.</param><param name="enable">Clips overflowing cell content.</param>
    public void SetColumnClipContent(int column, bool enable) { EnsureTreeMutable(); AtColumn(column).Clip = enable; InvalidateTree(); }
    /// <summary>Reports column content clipping.</summary><param name="column">Existing column.</param><returns>False initially.</returns>
    public bool IsColumnClippingContent(int column) => AtColumn(column).Clip;
    /// <summary>Sets a column's source title.</summary><param name="column">Existing column.</param><param name="title">Nonnull source text.</param>
    public void SetColumnTitle(int column, string title) { EnsureTreeMutable(); ArgumentNullException.ThrowIfNull(title); AtColumn(column).Title = title; InvalidateTree(); }
    /// <summary>Returns a source column title.</summary><param name="column">Existing column.</param><returns>Title.</returns>
    public string GetColumnTitle(int column) => AtColumn(column).Title;
    /// <summary>Sets a title's source tooltip.</summary><param name="column">Existing column.</param><param name="tooltipText">Nonnull tooltip.</param>
    public void SetColumnTitleTooltipText(int column, string tooltipText) { EnsureTreeMutable(); ArgumentNullException.ThrowIfNull(tooltipText); AtColumn(column).Tooltip = tooltipText; }
    /// <summary>Returns a source title tooltip.</summary><param name="column">Existing column.</param><returns>Tooltip.</returns>
    public string GetColumnTitleTooltipText(int column) => AtColumn(column).Tooltip;
    /// <summary>Sets title alignment.</summary><param name="column">Existing column.</param><param name="titleAlignment">Defined alignment.</param>
    public void SetColumnTitleAlignment(int column, HorizontalAlignment titleAlignment) { EnsureTreeMutable(); if (!Enum.IsDefined(titleAlignment)) throw new ArgumentOutOfRangeException(nameof(titleAlignment)); AtColumn(column).Alignment = titleAlignment; InvalidateTree(); }
    /// <summary>Returns title alignment.</summary><param name="column">Existing column.</param><returns>Alignment.</returns>
    public HorizontalAlignment GetColumnTitleAlignment(int column) => AtColumn(column).Alignment;
    /// <summary>Sets title text direction.</summary><param name="column">Existing column.</param><param name="direction">Defined direction.</param>
    public void SetColumnTitleDirection(int column, TextDirection direction) { EnsureTreeMutable(); if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction)); AtColumn(column).Direction = direction; InvalidateTree(); }
    /// <summary>Returns title text direction.</summary><param name="column">Existing column.</param><returns>Direction.</returns>
    public TextDirection GetColumnTitleDirection(int column) => AtColumn(column).Direction;
    /// <summary>Sets title shaping language.</summary><param name="column">Existing column.</param><param name="language">Language tag or empty.</param>
    public void SetColumnTitleLanguage(int column, string language) { EnsureTreeMutable(); ArgumentNullException.ThrowIfNull(language); AtColumn(column).Language = language; InvalidateTree(); }
    /// <summary>Returns title shaping language.</summary><param name="column">Existing column.</param><returns>Language tag.</returns>
    public string GetColumnTitleLanguage(int column) => AtColumn(column).Language;
}
