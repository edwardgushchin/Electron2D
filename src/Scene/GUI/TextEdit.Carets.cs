namespace Electron2D;

public partial class TextEdit
{
    private Vector2i SelectionStart(Caret caret) => Offset(caret.Origin) <= Offset(caret.Position) ? caret.Origin : caret.Position;
    private Vector2i SelectionEnd(Caret caret) => Offset(caret.Origin) >= Offset(caret.Position) ? caret.Origin : caret.Position;
    private void CaretsChanged() { _pendingCaret = true; QueueChanges(); QueueRedraw(); ResetBlink(); }
    /// <summary>Returns the current number of carets.</summary><returns>At least one.</returns>
    public int GetCaretCount() { CheckTextEdit(); return _carets.Count; }
    /// <summary>Adds a distinct caret at a logical/scalar position.</summary><param name="line">Existing line.</param><param name="column">Scalar column.</param><returns>The index, or -1 when disabled/duplicate.</returns>
    public int AddCaret(int line, int column) { EnsureTextMutable(); ValidatePosition(line, column); if (!_caretMultiple || _carets.Any(c => c.Position == new Vector2i(column, line))) return -1; _carets.Add(new() { Position = new(column, line), Origin = new(column, line) }); CaretsChanged(); return _carets.Count - 1; }
    /// <summary>Adds a caret on the adjacent visible row for each current caret.</summary><param name="below">Chooses following rows when true.</param>
    public void AddCaretAtCarets(bool below) { EnsureTextMutable(); EnsureTextLayout(); foreach (var caret in _carets.ToArray()) { var at = VisualIndex(caret.Position.Y, GetLineWrapIndexAtColumn(caret.Position.Y, caret.Position.X)); var target = Math.Clamp(at + (below ? 1 : -1), 0, _visual.Count - 1); if (target == at) continue; var row = _visual[target]; var x = CaretX(_lines[caret.Position.Y], caret.Position.X, GetLineWrapIndexAtColumn(caret.Position.Y, caret.Position.X)); var point = GetLineColumnAtPos(new((int)(_contentRect.Position.X + x - _scrollHorizontal), (int)(_contentRect.Position.Y + (target - _scrollVertical + .5) * _rowHeight))); AddCaret(point.Y, point.X); } }
    /// <summary>Removes one caret, retaining a primary caret.</summary><param name="caret">Existing index; the sole caret cannot be removed.</param>
    public void RemoveCaret(int caret) { EnsureTextMutable(); AtCaret(caret); if (_carets.Count == 1) return; _carets.RemoveAt(caret); CaretsChanged(); }
    /// <summary>Removes all carets except the first.</summary>
    public void RemoveSecondaryCarets() { EnsureTextMutable(); if (_carets.Count > 1) { _carets.RemoveRange(1, _carets.Count - 1); CaretsChanged(); } }
    /// <summary>Returns a caret's logical line.</summary><param name="caretIndex">Existing index.</param><returns>Logical line index.</returns>
    public int GetCaretLine(int caretIndex = 0) => AtCaret(caretIndex).Position.Y;
    /// <summary>Returns a caret's scalar column.</summary><param name="caretIndex">Existing index.</param><returns>Logical scalar column.</returns>
    public int GetCaretColumn(int caretIndex = 0) => AtCaret(caretIndex).Position.X;
    /// <summary>Moves a caret to a clamped logical line and wrap row.</summary><param name="line">Requested line.</param><param name="adjustViewport">Keeps the caret in view.</param><param name="canBeHidden">Whether hidden lines are eligible.</param><param name="wrapIndex">Preferred wrap row.</param><param name="caretIndex">Existing index.</param>
    public void SetCaretLine(int line, bool adjustViewport = true, bool canBeHidden = true, int wrapIndex = 0, int caretIndex = 0)
    {
        EnsureTextMutable(); var caret = AtCaret(caretIndex); line = Math.Clamp(line, 0, _lines.Count - 1); if (!canBeHidden) { while (line < _lines.Count - 1 && _lines[line].Hidden) line++; while (line > 0 && _lines[line].Hidden) line--; }
        EnsureTextLayout(); var desired = CaretX(_lines[caret.Position.Y], caret.Position.X, GetCaretWrapIndex(caretIndex)); var wrap = Math.Clamp(wrapIndex, 0, GetLineWrapCount(line)); var visual = VisualIndex(line, wrap);
        var point = GetLineColumnAtPos(new((int)(_contentRect.Position.X + desired - _scrollHorizontal), (int)(_contentRect.Position.Y + (visual - _scrollVertical + .5) * _rowHeight))); caret.Position = point; caret.Wrap = wrap; CaretsChanged(); if (adjustViewport) AdjustViewportToCaret(caretIndex);
    }
    /// <summary>Moves a caret to a clamped scalar column, respecting grapheme policy.</summary><param name="column">Requested column.</param><param name="adjustViewport">Keeps the caret in view.</param><param name="caretIndex">Existing index.</param>
    public void SetCaretColumn(int column, bool adjustViewport = true, int caretIndex = 0)
    { EnsureTextMutable(); var caret = AtCaret(caretIndex); var line = caret.Position.Y; var value = Math.Clamp(column, 0, _lines[line].Scalars); EnsureTextLayout(); if (!_caretMidGrapheme) value = _lines[line].Layout.ClosestGrapheme(value); caret.Position.X = value; caret.Wrap = GetLineWrapIndexAtColumn(line, value); CaretsChanged(); if (adjustViewport) AdjustViewportToCaret(caretIndex); }
    /// <summary>Returns the caret's current wrap row.</summary><param name="caretIndex">Existing index.</param><returns>Wrap row index.</returns>
    public int GetCaretWrapIndex(int caretIndex = 0) { var caret = AtCaret(caretIndex); return GetLineWrapIndexAtColumn(caret.Position.Y, caret.Position.X); }
    /// <summary>Returns whether a caret is eligible for drawing inside the viewport.</summary><param name="caretIndex">Existing index.</param><returns>Whether its logical/wrapped row is visible.</returns>
    public bool IsCaretVisible(int caretIndex = 0) { var caret = AtCaret(caretIndex); return !_lines[caret.Position.Y].Hidden && IsLineInViewport(caret.Position.Y); }
    /// <summary>Returns caret indices in ascending selection-start/document order.</summary><param name="includeIgnoredCarets">Includes suppressed carets from a multicaret edit.</param><returns>Independent index array.</returns>
    public int[] GetSortedCarets(bool includeIgnoredCarets = false) { CheckTextEdit(); return Enumerable.Range(0, _carets.Count).Where(i => includeIgnoredCarets || !_carets[i].Ignored).OrderBy(i => Offset(_carets[i].Selected ? SelectionStart(_carets[i]) : _carets[i].Position)).ToArray(); }
    /// <summary>Starts a nested multicaret operation, deferring overlap merging.</summary>
    public void BeginMulticaretEdit() { EnsureTextMutable(); if (_multiEdit++ == 0) foreach (var caret in _carets) caret.Ignored = false; }
    /// <summary>Ends a multicaret operation and merges overlapping selections at the outer boundary.</summary>
    public void EndMulticaretEdit() { EnsureTextMutable(); if (_multiEdit == 0) throw new InvalidOperationException("No multicaret edit is active."); if (--_multiEdit == 0) { MergeOverlappingCarets(); FinishEdit(); } }
    /// <summary>Reports whether a grouped multicaret edit is active.</summary><returns>Whether nested edit depth is nonzero.</returns>
    public bool IsInMulticaretEdit() { CheckTextEdit(); return _multiEdit != 0; }
    /// <summary>Reports whether an overlapping caret is suppressed in the current grouped edit.</summary><param name="caretIndex">Existing index.</param><returns>Suppression state.</returns>
    public bool MulticaretEditIgnoreCaret(int caretIndex) => AtCaret(caretIndex).Ignored;
    /// <summary>Merges carets whose selection intervals overlap or whose positions coincide.</summary>
    public void MergeOverlappingCarets()
    {
        EnsureTextMutable();
        for (var i = 0; i < _carets.Count; i++) for (var j = _carets.Count - 1; j > i; j--)
            {
                var a = _carets[i]; var b = _carets[j]; var a0 = Offset(a.Selected ? SelectionStart(a) : a.Position); var a1 = Offset(a.Selected ? SelectionEnd(a) : a.Position); var b0 = Offset(b.Selected ? SelectionStart(b) : b.Position); var b1 = Offset(b.Selected ? SelectionEnd(b) : b.Position);
                if (Math.Max(a0, b0) > Math.Min(a1, b1)) continue; a.Origin = PositionAt(Math.Min(a0, b0)); a.Position = PositionAt(Math.Max(a1, b1)); a.Selected = a.Origin != a.Position;
                if (_multiEdit > 0) b.Ignored = true; else _carets.RemoveAt(j);
            }
        CaretsChanged();
    }
    /// <summary>Moves carets within an ordered interval to its start, retaining affected selection anchors.</summary><param name="fromLine">Start line.</param><param name="fromColumn">Start column.</param><param name="toLine">End line.</param><param name="toColumn">End column.</param><param name="inclusive">Includes caret positions on interval boundaries.</param>
    public void CollapseCarets(int fromLine, int fromColumn, int toLine, int toColumn, bool inclusive = false)
    { EnsureTextMutable(); ValidatePosition(fromLine, fromColumn); ValidatePosition(toLine, toColumn); var from = Offset(new(fromColumn, fromLine)); var to = Offset(new(toColumn, toLine)); if (to < from) throw new ArgumentException("Caret interval is reversed."); foreach (var caret in _carets) { var at = Offset(caret.Position); if (inclusive ? at >= from && at <= to : at > from && at < to) caret.Position = new(fromColumn, fromLine); } MergeOverlappingCarets(); }
    /// <summary>Selects between an origin and caret endpoint.</summary><param name="originLine">Origin line.</param><param name="originColumn">Origin scalar column.</param><param name="caretLine">Endpoint line.</param><param name="caretColumn">Endpoint scalar column.</param><param name="caretIndex">Existing caret.</param>
    public void Select(int originLine, int originColumn, int caretLine, int caretColumn, int caretIndex = 0)
    { EnsureTextMutable(); if (!_selectingEnabled) return; var caret = AtCaret(caretIndex); caret.Origin = ClampPosition(new(originColumn, originLine)); caret.Position = ClampPosition(new(caretColumn, caretLine)); caret.Selected = caret.Origin != caret.Position; CaretsChanged(); }
    /// <summary>Selects the complete document with the primary caret.</summary>
    public void SelectAll() { EnsureTextMutable(); if (!_selectingEnabled) return; RemoveSecondaryCarets(); Select(0, 0, _lines.Count - 1, _lines[^1].Scalars); }
    /// <summary>Clears one or all selections without moving carets.</summary><param name="caretIndex">Existing index or -1 for all.</param>
    public void Deselect(int caretIndex = -1) { EnsureTextMutable(); foreach (var index in Indices(caretIndex)) { var caret = _carets[index]; caret.Selected = false; caret.Origin = caret.Position; } _selectionMode = SelectionMode.None; CaretsChanged(); }
    /// <summary>Reports whether one or any caret owns selected text.</summary><param name="caretIndex">Existing index or -1 for any.</param><returns>Whether a nonempty selection exists.</returns>
    public bool HasSelection(int caretIndex = -1) { CheckTextEdit(); return Indices(caretIndex).Any(i => _carets[i].Selected); }
    private IEnumerable<int> Indices(int index) { if (index == -1) return Enumerable.Range(0, _carets.Count); AtCaret(index); return [index]; }
    private Caret SelectionCaret(int index) { if (index >= 0) return AtCaret(index); if (index != -1) throw new ArgumentOutOfRangeException(nameof(index)); return _carets.FirstOrDefault(c => c.Selected) ?? _carets[0]; }
    /// <summary>Returns one selection or all selections joined by newline in document order.</summary><param name="caretIndex">Existing index or -1 for all.</param><returns>Copied selected text.</returns>
    public string GetSelectedText(int caretIndex = -1)
    {
        CheckTextEdit(); if (caretIndex < -1) throw new ArgumentOutOfRangeException(nameof(caretIndex)); string TextOf(Caret caret) { if (!caret.Selected) return ""; var from = SelectionStart(caret); var to = SelectionEnd(caret); if (from.Y == to.Y) return Slice(_lines[from.Y].Text, from.X, to.X); var result = new List<string> { Slice(_lines[from.Y].Text, from.X, _lines[from.Y].Scalars) }; for (var i = from.Y + 1; i < to.Y; i++) result.Add(_lines[i].Text); result.Add(Slice(_lines[to.Y].Text, 0, to.X)); return string.Join('\n', result); }
        return caretIndex >= 0 ? TextOf(AtCaret(caretIndex)) : string.Join('\n', _carets.Where(c => c.Selected).OrderBy(c => Offset(SelectionStart(c))).Select(TextOf));
    }
    /// <summary>Deletes one or all selection intervals as one undo step.</summary><param name="caretIndex">Existing index or -1 for all.</param>
    public void DeleteSelection(int caretIndex = -1)
    { EnsureTextMutable(); BeginComplexOperation(); try { foreach (var index in SortedCaretIndices(caretIndex)) { var caret = _carets[index]; if (!caret.Selected) continue; var from = SelectionStart(caret); Replace(from, SelectionEnd(caret), "", index); caret.Position = caret.Origin = from; caret.Selected = false; } } finally { EndComplexOperation(); } MergeOverlappingCarets(); }
    /// <summary>Returns whether a caret endpoint follows its selection origin.</summary><param name="caretIndex">Existing index.</param><returns>Document-order relationship.</returns>
    public bool IsCaretAfterSelectionOrigin(int caretIndex = 0) { var caret = AtCaret(caretIndex); return !caret.Selected || Offset(caret.Position) >= Offset(caret.Origin); }
    /// <summary>Returns the selection origin's line.</summary><param name="caretIndex">Existing index.</param><returns>Logical line.</returns>
    public int GetSelectionOriginLine(int caretIndex = 0) => AtCaret(caretIndex).Origin.Y;
    /// <summary>Returns the selection origin's scalar column.</summary><param name="caretIndex">Existing index.</param><returns>Scalar column.</returns>
    public int GetSelectionOriginColumn(int caretIndex = 0) => AtCaret(caretIndex).Origin.X;
    /// <summary>Returns the ordered selection start line.</summary><param name="caretIndex">Existing index.</param><returns>Logical line.</returns>
    public int GetSelectionFromLine(int caretIndex = 0) => SelectionStart(AtCaret(caretIndex)).Y;
    /// <summary>Returns the ordered selection start column.</summary><param name="caretIndex">Existing index.</param><returns>Scalar column.</returns>
    public int GetSelectionFromColumn(int caretIndex = 0) => SelectionStart(AtCaret(caretIndex)).X;
    /// <summary>Returns the ordered selection end line.</summary><param name="caretIndex">Existing index.</param><returns>Logical line.</returns>
    public int GetSelectionToLine(int caretIndex = 0) => SelectionEnd(AtCaret(caretIndex)).Y;
    /// <summary>Returns the ordered selection end column.</summary><param name="caretIndex">Existing index.</param><returns>Scalar column.</returns>
    public int GetSelectionToColumn(int caretIndex = 0) => SelectionEnd(AtCaret(caretIndex)).X;
    /// <summary>Sets a selection origin's line while retaining its column.</summary><param name="line">Requested line.</param><param name="canBeHidden">Whether hidden lines are eligible.</param><param name="wrapIndex">Requested wrap index or -1.</param><param name="caretIndex">Existing index.</param>
    public void SetSelectionOriginLine(int line, bool canBeHidden = true, int wrapIndex = -1, int caretIndex = 0)
    {
        EnsureTextMutable(); var caret = AtCaret(caretIndex); line = Math.Clamp(line, 0, _lines.Count - 1); if (!canBeHidden) { while (line < _lines.Count - 1 && _lines[line].Hidden) line++; while (line > 0 && _lines[line].Hidden) line--; }
        caret.Origin = ClampPosition(new(caret.Origin.X, line)); if (wrapIndex >= 0) { EnsureTextLayout(); if (_lines[line].Layout.LineCount > 0) { var row = _lines[line].Layout.Lines[Math.Min(wrapIndex, GetLineWrapCount(line))]; caret.Origin.X = Math.Clamp(caret.Origin.X, row.Start, row.End); } }
        caret.Selected = caret.Position != caret.Origin; CaretsChanged();
    }
    /// <summary>Sets a selection origin's scalar column.</summary><param name="column">Requested scalar column.</param><param name="caretIndex">Existing index.</param>
    public void SetSelectionOriginColumn(int column, int caretIndex = 0) { EnsureTextMutable(); var caret = AtCaret(caretIndex); caret.Origin = ClampPosition(new(column, caret.Origin.Y)); caret.Selected = caret.Position != caret.Origin; CaretsChanged(); }
    /// <summary>Returns the active selection interaction.</summary><returns>Current typed selection mode.</returns>
    public SelectionMode GetSelectionMode() { CheckTextEdit(); return _selectionMode; }
    /// <summary>Sets the selection interaction policy.</summary><param name="mode">A defined interaction mode.</param>
    public void SetSelectionMode(SelectionMode mode) { EnsureTextMutable(); if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode)); _selectionMode = mode; }
    /// <summary>Returns the selection containing a scalar position.</summary><param name="line">Logical line.</param><param name="column">Scalar column.</param><param name="includeEdges">Includes endpoints.</param><param name="onlySelections">Ignores empty carets.</param><returns>Caret index or -1.</returns>
    public int GetSelectionAtLineColumn(int line, int column, bool includeEdges = true, bool onlySelections = true)
    { CheckTextEdit(); ValidatePosition(line, column); var at = Offset(new(column, line)); for (var i = 0; i < _carets.Count; i++) { var caret = _carets[i]; if (onlySelections && !caret.Selected) continue; var from = Offset(caret.Selected ? SelectionStart(caret) : caret.Position); var to = Offset(caret.Selected ? SelectionEnd(caret) : caret.Position); if (includeEdges ? at >= from && at <= to : at > from && at < to) return i; } return -1; }
    /// <summary>Returns selected/caret logical-line intervals in ascending order.</summary><param name="onlySelections">Omits unselected carets.</param><param name="mergeAdjacent">Combines overlapping/adjacent line intervals.</param><returns>Inclusive line-start/line-end pairs.</returns>
    public Vector2i[] GetLineRangesFromCarets(bool onlySelections = false, bool mergeAdjacent = true)
    { CheckTextEdit(); var result = new List<Vector2i>(); foreach (var caret in _carets.Where(c => !onlySelections || c.Selected).OrderBy(c => (c.Selected ? SelectionStart(c) : c.Position).Y)) { var from = caret.Selected ? SelectionStart(caret) : caret.Position; var to = caret.Selected ? SelectionEnd(caret) : caret.Position; if (caret.Selected && to.X == 0 && to.Y > from.Y) to.Y--; var range = new Vector2i(from.Y, to.Y); if (result.Count > 0 && range.X <= result[^1].Y + (mergeAdjacent ? 1 : 0)) result[^1] = new(result[^1].X, Math.Max(range.Y, result[^1].Y)); else result.Add(range); } return result.ToArray(); }
    private bool WordSeparator(int scalar)
    {
        if (scalar <= 32 || _useDefaultWordSeparators && scalar is >= 33 and <= 47 or >= 58 and <= 64 or >= 91 and <= 94 or 96 or >= 123 and <= 126) return true;
        if (_useCustomWordSeparators) foreach (var rune in _customWordSeparators.EnumerateRunes()) if (rune.Value == scalar) return true; return false;
    }
    private (int From, int To) WordRange(Vector2i position)
    { if (_useDefaultWordSeparators && !_useCustomWordSeparators) { EnsureTextLayout(); return _lines[position.Y].Layout.WordAt(position.X); } var values = _lines[position.Y].Text.EnumerateRunes().Select(r => r.Value).ToArray(); var from = Math.Min(position.X, values.Length); var to = from; while (from > 0 && !WordSeparator(values[from - 1])) from--; while (to < values.Length && !WordSeparator(values[to])) to++; return (from, to); }
    private bool IsWordBoundary(int line, int column)
    {
        var text = _lines[line].Text; var index = ScalarIndex(text, column); if (index <= 0 || index >= text.Length) return true;
        System.Text.Rune.DecodeLastFromUtf16(text.AsSpan(0, index), out var previous, out _); System.Text.Rune.DecodeFromUtf16(text.AsSpan(index), out var next, out _); return WordSeparator(previous.Value) || WordSeparator(next.Value);
    }
    /// <summary>Returns one caret's word or the primary caret's word.</summary><param name="caretIndex">Index or -1 for primary.</param><returns>Word text.</returns>
    public string GetWordUnderCaret(int caretIndex = -1) { var caret = AtCaret(caretIndex < 0 ? 0 : caretIndex); var range = WordRange(caret.Position); return Slice(_lines[caret.Position.Y].Text, range.From, range.To); }
    /// <summary>Selects the word at one or all carets.</summary><param name="caretIndex">Index or -1 for all.</param>
    public void SelectWordUnderCaret(int caretIndex = -1) { EnsureTextMutable(); foreach (var i in Indices(caretIndex)) { var caret = _carets[i]; var range = WordRange(caret.Position); Select(caret.Position.Y, range.From, caret.Position.Y, range.To, i); } }
    /// <summary>Adds a selection for the next exact occurrence of the current selected text or word.</summary>
    public void AddSelectionForNextOccurrence() { EnsureTextMutable(); if (!HasSelection()) SelectWordUnderCaret(0); var word = GetSelectedText(0); if (word.Length == 0) return; var caret = _carets[^1]; var found = Search(word, SearchFlags.MatchCase, caret.Position.Y, caret.Position.X); if (found.X < 0) return; var index = AddCaret(found.Y, found.X + CountScalars(word.AsSpan())); if (index >= 0) Select(found.Y, found.X, found.Y, found.X + CountScalars(word.AsSpan()), index); }
    /// <summary>Moves the latest selection to the next exact occurrence.</summary>
    public void SkipSelectionForNextOccurrence() { EnsureTextMutable(); var word = GetSelectedText(_carets.Count - 1); if (word.Length == 0) return; var caret = _carets[^1]; var found = Search(word, SearchFlags.MatchCase, caret.Position.Y, caret.Position.X); if (found.X >= 0) Select(found.Y, found.X, found.Y, found.X + CountScalars(word.AsSpan()), _carets.Count - 1); }
    /// <summary>Gets whether user typing replaces characters.</summary><returns>Overtype policy.</returns>
    public bool IsOvertypeModeEnabled() { CheckTextEdit(); return _overtype; }
    /// <summary>Sets whether user typing replaces characters.</summary><param name="enabled">Overtype policy.</param>
    public void SetOvertypeModeEnabled(bool enabled) { EnsureTextMutable(); _overtype = enabled; QueueRedraw(); }
}
