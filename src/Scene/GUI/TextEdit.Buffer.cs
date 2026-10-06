namespace Electron2D;

public partial class TextEdit
{
    // ponytail: cold undo snapshots cost O(document size); edit deltas replace them when large-document editing needs a tighter ceiling.
    private History Snapshot() => new(_lines.Select(l => l.Text).ToArray(), _carets.Select(c => c.Copy()).ToArray(), _version, _lines.Select(l => l.Gutters.Select((cell, index) => (_gutters[index], cell.Copy())).ToArray()).ToArray(), _lines.Select(l => l.Background).ToArray());
    private void SetLines(string[] text)
    {
        while (_lines.Count > text.Length) _lines.RemoveAt(_lines.Count - 1);
        for (var i = 0; i < Math.Max(1, text.Length); i++) { if (i == _lines.Count) _lines.Add(NewLine("")); _lines[i].Text = text.Length == 0 ? "" : text[i]; _lines[i].Scalars = CountScalars(_lines[i].Text.AsSpan()); }
        InvalidateTextLayout();
    }
    private Line NewLine(string text) { var line = new Line { Text = text, Scalars = CountScalars(text.AsSpan()) }; for (var i = 0; i < _gutters.Count; i++) line.Gutters.Add(new()); return line; }
    private void StartEdit() { if (_operationBefore == null) _operationBefore = Snapshot(); }
    private void FinishEdit()
    {
        if (_complex > 0 || _multiEdit > 0 || _action != EditAction.None || _operationBefore == null) return;
        var before = _operationBefore; _operationBefore = null;
        if (before.Lines.SequenceEqual(_lines.Select(l => l.Text))) { QueueChanges(); return; }
        if (_historyPosition + 1 < _history.Count) _history.RemoveRange(_historyPosition + 1, _history.Count - _historyPosition - 1);
        _history[_historyPosition] = before; _history.Add(Snapshot()); if (_history.Count > _historyLimit + 1) _history.RemoveRange(0, _history.Count - _historyLimit - 1); _historyPosition = _history.Count - 1; QueueChanges();
    }
    private void Replace(Vector2i from, Vector2i to, string value, int excludedCaret = -1, bool beforeBegin = true, bool beforeEnd = false)
    {
        ValidatePosition(from.Y, from.X); ValidatePosition(to.Y, to.X); var start = Offset(from); var end = Offset(to); if (end < start) throw new ArgumentException("The text interval is reversed.");
        value = Normalize(value); var inserted = CountScalars(value.AsSpan()); if (start == end && inserted == 0) return;
        var caretOffsets = _carets.Select(c => (Position: Offset(c.Position), Origin: Offset(c.Origin))).ToArray(); StartEdit();
        var prefix = Slice(_lines[from.Y].Text, 0, from.X); var suffix = Slice(_lines[to.Y].Text, to.X, _lines[to.Y].Scalars); var pieces = (prefix + value + suffix).Split('\n'); var first = _lines[from.Y];
        if (to.Y > from.Y) { if (from.X == 0 && to.X == 0) { first.Gutters.Clear(); first.Gutters.AddRange(_lines[to.Y].Gutters.Select(c => c.Copy())); first.Background = _lines[to.Y].Background; } else MergeGutters(to.Y, from.Y); }
        var carryGutters = from.X == 0 && from.Y == to.Y && pieces.Length > 1 ? first.Gutters.Select(c => c.Copy()).ToArray() : null; var carryBackground = first.Background;
        _lines.RemoveRange(from.Y + 1, to.Y - from.Y); first.Text = pieces[0]; first.Scalars = CountScalars(first.Text.AsSpan());
        for (var i = 1; i < pieces.Length; i++) _lines.Insert(from.Y + i, NewLine(pieces[i]));
        if (carryGutters != null) { var last = _lines[from.Y + pieces.Length - 1]; last.Gutters.Clear(); last.Gutters.AddRange(carryGutters); last.Background = carryBackground; first.Gutters.Clear(); for (var g = 0; g < _gutters.Count; g++) first.Gutters.Add(new()); first.Background = new(0, 0, 0, 0); }
        int Shift(int position, bool before) => position < start || position == start && before ? position : position <= end ? start + inserted : position + inserted - (end - start);
        for (var i = 0; i < _carets.Count; i++)
        {
            if (i == excludedCaret) continue;
            var caret = _carets[i]; caret.Position = PositionAt(Shift(caretOffsets[i].Position, beforeEnd)); caret.Origin = PositionAt(Shift(caretOffsets[i].Origin, beforeBegin)); if (caret.Position == caret.Origin) caret.Selected = false;
        }
        _version = ++_nextVersion; Changed(from.Y, Math.Max(to.Y, from.Y + pieces.Length - 1), true); FinishEdit();
    }
    /// <summary>Returns the number of logical lines, including an empty final line.</summary><returns>At least one.</returns>
    public int GetLineCount() { CheckTextEdit(); return _lines.Count; }
    /// <summary>Returns one logical line without a newline.</summary><param name="line">Existing index.</param><returns>Stored line text.</returns>
    public string GetLine(int line) => AtLine(line).Text;
    /// <summary>Replaces one logical line, adjusting affected carets and preserving its gutters.</summary><param name="line">Existing index.</param><param name="newText">Nonnull replacement; may contain newlines.</param>
    public void SetLine(int line, string newText) { EnsureTextMutable(); ArgumentNullException.ThrowIfNull(newText); Replace(new(0, line), new(AtLine(line).Scalars, line), newText); }
    /// <summary>Inserts a logical line before an existing index or at the document end.</summary><param name="line">Index from zero through line count.</param><param name="text">Nonnull inserted text.</param>
    public void InsertLineAt(int line, string text)
    { EnsureTextMutable(); ArgumentNullException.ThrowIfNull(text); if ((uint)line > _lines.Count) throw new ArgumentOutOfRangeException(nameof(line)); if (line == _lines.Count) Replace(new(_lines[^1].Scalars, line - 1), new(_lines[^1].Scalars, line - 1), "\n" + text); else Replace(new(0, line), new(0, line), text + "\n"); }
    /// <summary>Removes a logical line while retaining at least one line.</summary><param name="line">Existing index.</param><param name="moveCaretsDown">Chooses the following line rather than its predecessor when available.</param>
    public void RemoveLineAt(int line, bool moveCaretsDown = true)
    {
        EnsureTextMutable(); AtLine(line); if (_lines.Count == 1) { SetLine(0, ""); return; }
        BeginComplexOperation();
        try
        {
            var tracked = _carets.Select(c => c.Position.Y == line).ToArray();
            if (line + 1 < _lines.Count) Replace(new(0, line), new(0, line + 1), ""); else Replace(new(_lines[line - 1].Scalars, line - 1), new(_lines[line].Scalars, line), "");
            if (!moveCaretsDown) for (var i = 0; i < tracked.Length; i++) if (tracked[i]) { _carets[i].Position = ClampPosition(new(_carets[i].Position.X, Math.Max(0, line - 1))); _carets[i].Origin = _carets[i].Position; _carets[i].Selected = false; }
        }
        finally { EndComplexOperation(); }
    }
    /// <summary>Exchanges complete logical lines and their gutter/background state.</summary><param name="fromLine">Existing index.</param><param name="toLine">Existing index.</param>
    public void SwapLines(int fromLine, int toLine)
    { EnsureTextMutable(); AtLine(fromLine); AtLine(toLine); if (fromLine == toLine) return; StartEdit(); (_lines[fromLine], _lines[toLine]) = (_lines[toLine], _lines[fromLine]); foreach (var caret in _carets) { if (caret.Position.Y == fromLine) caret.Position.Y = toLine; else if (caret.Position.Y == toLine) caret.Position.Y = fromLine; if (caret.Origin.Y == fromLine) caret.Origin.Y = toLine; else if (caret.Origin.Y == toLine) caret.Origin.Y = fromLine; } _version = ++_nextVersion; Changed(Math.Min(fromLine, toLine), Math.Max(fromLine, toLine), true); FinishEdit(); }
    /// <summary>Inserts text at an explicit logical position and adjusts all carets.</summary><param name="text">Nonnull text.</param><param name="line">Existing line.</param><param name="column">Scalar column.</param><param name="beforeSelectionBegin">Keeps origins before insertion at equal positions.</param><param name="beforeSelectionEnd">Keeps caret ends before insertion at equal positions.</param>
    public void InsertText(string text, int line, int column, bool beforeSelectionBegin = true, bool beforeSelectionEnd = false)
    { EnsureTextMutable(); ArgumentNullException.ThrowIfNull(text); Replace(new(column, line), new(column, line), text, beforeBegin: beforeSelectionBegin, beforeEnd: beforeSelectionEnd); }
    /// <summary>Removes an ordered logical/scalar text interval.</summary><param name="fromLine">Start line.</param><param name="fromColumn">Start scalar column.</param><param name="toLine">End line.</param><param name="toColumn">End scalar column.</param>
    public void RemoveText(int fromLine, int fromColumn, int toLine, int toColumn) { EnsureTextMutable(); Replace(new(fromColumn, fromLine), new(toColumn, toLine), ""); }
    /// <summary>Inserts at one caret or all carets, replacing their selections in reverse document order.</summary><param name="text">Nonnull text.</param><param name="caretIndex">Existing index or -1 for all.</param>
    public void InsertTextAtCaret(string text, int caretIndex = -1)
    {
        EnsureTextMutable(); ArgumentNullException.ThrowIfNull(text); text = Normalize(text); BeginComplexOperation(); BeginMulticaretEdit();
        try
        {
            var carets = SortedCaretIndices(caretIndex);
            foreach (var index in carets)
            {
                var caret = AtCaret(index); if (caret.Ignored) continue; var from = caret.Selected ? SelectionStart(caret) : caret.Position; var to = caret.Selected ? SelectionEnd(caret) : caret.Position;
                if (_overtype && !caret.Selected && !text.Contains('\n')) to.X = Math.Min(_lines[to.Y].Scalars, to.X + CountScalars(text.AsSpan()));
                var start = Offset(from); Replace(from, to, text, index); caret.Position = PositionAt(start + CountScalars(text.AsSpan())); caret.Origin = caret.Position; caret.Selected = false; caret.Wrap = 0;
            }
        }
        finally { EndMulticaretEdit(); EndComplexOperation(); }
        AdjustViewportToCaret(); ResetBlink();
    }
    /// <summary>Clears text, secondary carets and undo history.</summary>
    public void Clear() { EnsureTextMutable(); Text = ""; RemoveSecondaryCarets(); ClearUndoHistory(); }
    /// <summary>Starts a nested group of edits represented by one undo step.</summary>
    public void BeginComplexOperation() { EnsureTextMutable(); if (_complex++ == 0) StartEdit(); }
    /// <summary>Ends one nested undo group.</summary>
    public void EndComplexOperation() { EnsureTextMutable(); if (_complex == 0) throw new InvalidOperationException("No complex edit is active."); _complex--; FinishEdit(); }
    /// <summary>Starts grouping consecutive edits of a named interaction.</summary><param name="action">Typed edit action.</param>
    public void StartAction(EditAction action) { EnsureTextMutable(); if (!Enum.IsDefined(action)) throw new ArgumentOutOfRangeException(nameof(action)); if (_action == action) return; EndAction(); _action = action; if (action != EditAction.None) StartEdit(); }
    /// <summary>Ends a consecutive edit action and commits its undo step.</summary>
    public void EndAction() { EnsureTextMutable(); _action = EditAction.None; FinishEdit(); }
    /// <summary>Returns whether a prior state is available.</summary><returns>Whether Undo can change content.</returns>
    public bool HasUndo() { CheckTextEdit(); return _historyPosition > 0 || _operationBefore != null && !_operationBefore.Lines.SequenceEqual(_lines.Select(l => l.Text)); }
    /// <summary>Returns whether an undone state is available.</summary><returns>Whether Redo can change content.</returns>
    public bool HasRedo() { CheckTextEdit(); return _historyPosition + 1 < _history.Count; }
    /// <summary>Restores the previous committed content/caret state.</summary>
    public void Undo() { EnsureTextMutable(); ApplyIME(); EndAction(); if (_historyPosition == 0) return; Restore(_history[--_historyPosition]); }
    /// <summary>Restores the next undone content/caret state.</summary>
    public void Redo() { EnsureTextMutable(); EndAction(); if (_historyPosition + 1 == _history.Count) return; Restore(_history[++_historyPosition]); }
    private void Restore(History history)
    {
        SetLines(history.Lines); _carets.Clear(); _carets.AddRange(history.Carets.Select(c => c.Copy())); _version = history.Version;
        for (var line = 0; line < _lines.Count; line++) { var target = _lines[line]; target.Background = history.Backgrounds[line]; target.Gutters.Clear(); foreach (var gutter in _gutters) { var found = Array.Find(history.Gutters[line], entry => ReferenceEquals(entry.Identity, gutter)); target.Gutters.Add(found.Cell?.Copy() ?? new()); } }
        Changed(0, _lines.Count - 1, true);
    }
    /// <summary>Clears undo/redo history while retaining current text.</summary>
    public void ClearUndoHistory() { EnsureTextMutable(); _savedVersion = 0; _history.Clear(); _historyPosition = 0; _operationBefore = null; _complex = _multiEdit = 0; _action = EditAction.None; _history.Add(Snapshot()); }
    /// <summary>Returns the current content version.</summary><returns>The undo-restorable edit version.</returns>
    public int GetVersion() { CheckTextEdit(); return _version; }
    /// <summary>Marks the current edit version as saved.</summary>
    public void TagSavedVersion() { EnsureTextMutable(); _savedVersion = _version; }
    /// <summary>Returns the last explicitly saved version.</summary><returns>The saved edit version.</returns>
    public int GetSavedVersion() { CheckTextEdit(); return _savedVersion; }
    /// <summary>Gets the scalar-column tab stop size.</summary><returns>Four initially.</returns>
    public int GetTabSize() { CheckTextEdit(); return _tabSize; }
    /// <summary>Sets a positive tab stop size used by layout and indentation input.</summary><param name="size">Positive scalar tab size.</param>
    public void SetTabSize(int size) { EnsureTextMutable(); if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size)); _tabSize = size; InvalidateTextLayout(); }
    private int[] SortedCaretIndices(int index = -1)
    { if (index >= 0) { AtCaret(index); return [index]; } if (index != -1) throw new ArgumentOutOfRangeException(nameof(index)); return Enumerable.Range(0, _carets.Count).Where(i => !_carets[i].Ignored).OrderByDescending(i => Offset(_carets[i].Position)).ToArray(); }
    /// <summary>Searches logical lines once, wrapping around document boundaries.</summary><param name="text">Nonempty search text.</param><param name="flags">Matching/direction flags.</param><param name="fromLine">Starting logical line.</param><param name="fromColumn">Starting scalar column.</param><returns>Column/line or (-1,-1).</returns>
    public Vector2i Search(string text, SearchFlags flags, int fromLine, int fromColumn)
    {
        CheckTextEdit(); ArgumentNullException.ThrowIfNull(text); ValidatePosition(fromLine, fromColumn); if (text.Length == 0) return new(-1, -1); var comparison = (flags & SearchFlags.MatchCase) != 0 ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase; var backwards = (flags & SearchFlags.Backwards) != 0;
        for (var pass = 0; pass <= _lines.Count; pass++)
        {
            var line = Mathf.PosMod(fromLine + (backwards ? -pass : pass), _lines.Count); var value = _lines[line].Text; var begin = pass == 0 ? ScalarIndex(value, fromColumn) : backwards ? value.Length : 0; var end = pass == _lines.Count ? ScalarIndex(value, fromColumn) : backwards ? 0 : value.Length;
            while (backwards ? begin >= end : begin <= end)
            {
                var at = backwards ? value.Length == 0 || begin < 0 ? -1 : value.LastIndexOf(text, Math.Min(begin + text.Length - 1, value.Length - 1), comparison) : value.IndexOf(text, begin, comparison); if (at < 0 || backwards && at < end || !backwards && at > end) break;
                var column = CountScalars(value.AsSpan(0, at)); var length = CountScalars(text.AsSpan()); if ((flags & SearchFlags.WholeWords) == 0 || IsWordBoundary(line, column) && IsWordBoundary(line, column + length)) return new(column, line);
                begin = backwards ? at - 1 : at + Math.Max(1, text.Length);
            }
        }
        return new(-1, -1);
    }
    /// <summary>Sets the highlighted search text without moving carets.</summary><param name="searchText">Nonnull text.</param>
    public void SetSearchText(string searchText) { EnsureTextMutable(); ArgumentNullException.ThrowIfNull(searchText); _searchText = searchText; QueueRedraw(); }
    /// <summary>Sets flags for highlighted search matches.</summary><param name="flags">Typed matching flags.</param>
    public void SetSearchFlags(SearchFlags flags) { EnsureTextMutable(); _searchFlags = flags; QueueRedraw(); }
}
