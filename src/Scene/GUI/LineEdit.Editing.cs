namespace Electron2D;

public partial class LineEdit
{
    private void AssignText(string text)
    {
        _deletionVersion++; _deletionPending = false; _text = ""; _caret = 0; _scroll = 0; _ime = ""; Deselect(); _history.Clear(); _history.Add(new("", 0, 0)); _historyPosition = 0;
        InsertTextAtCaret(text); if (_text.Length > 0) Remember(); _caret = 0; _scroll = 0; Invalidate();
    }
    /// <summary>Begins editing an attached editable field, acquiring focus when needed.</summary>
    /// <param name="hideFocus">Whether focus decoration is hidden when focus is acquired.</param>
    public void Edit(bool hideFocus = false)
    {
        EnsureMutable(); if (!IsInsideTree || !_editable || _editing) return;
        if (!HasFocus()) { GrabFocus(hideFocus); return; }
        _editing = true; _selectAllOnRelease = _selectAllOnFocus && Input.IsMouseButtonPressed(MouseButton.Left); if (_selectAllOnFocus && !_selectAllOnRelease) SelectAll(); ResetBlink(); ActivateIME(true);
    }
    /// <summary>Ends editing, commits composition and applies the focus-loss selection policy.</summary>
    public void Unedit()
    {
        EnsureMutable(); if (!_editing) return; _editing = false;
        List<Exception>? errors = null;
        try { ApplyIME(); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (_deselectOnFocusLossEnabled && !_dragAttempt) Deselect(); ResetBlink(); ActivateIME(false); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("LineEdit end-edit callbacks failed.", errors);
    }
    /// <summary>Returns whether editing is active.</summary><returns>True while this field is editing.</returns>
    public bool IsEditing() { CheckLineEdit(); return _editing; }
    /// <summary>Removes text, selection, composition and history; emits TextChanged only for a nonempty previous text.</summary>
    public void Clear() { EnsureMutable(); var changed = _text.Length > 0; AssignText(""); if (changed) TextChanged?.Invoke(_text); }
    /// <summary>Inserts text at the scalar caret without deleting selection or emitting TextChanged.</summary>
    /// <param name="text">The text to insert.</param>
    /// <remarks>MaxLength rejection is reported after committing the accepted prefix, so observers see consistent state.</remarks>
    public void InsertTextAtCaret(string text)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(text); var count = ScalarCount(text);
        var accepted = _maxLength == 0 ? count : Math.Min(count, Math.Max(0, _maxLength - ScalarCount(_text)));
        var prefix = ScalarSlice(text, 0, accepted); var rejected = ScalarSlice(text, accepted, count);
        var index = UTF16Index(_text, _caret); _text = _text.Insert(index, prefix); var start = _caret; _caret += accepted;
        Invalidate(); EnsureLayout(); var direction = _sourceLayout.DominantDirection(start, _caret); if (direction != TextDirection.Auto) _inputDirection = direction;
        FitCaret(); ResetBlink(); if (rejected.Length > 0) TextChangeRejected?.Invoke(rejected);
    }
    /// <summary>Deletes the scalar interval and adjusts the caret; equal endpoints are accepted.</summary>
    /// <param name="fromColumn">Inclusive scalar start.</param><param name="toColumn">Exclusive scalar end.</param>
    /// <exception cref="ArgumentOutOfRangeException">The ordered interval lies outside Text.</exception>
    /// <remarks>Attached changes emit one deferred TextChanged and add one history entry. Detached deletion changes text without that notification.</remarks>
    public void DeleteText(int fromColumn, int toColumn)
    {
        EnsureMutable(); var count = ScalarCount(_text); if (fromColumn < 0 || toColumn < fromColumn || toColumn > count) throw new ArgumentOutOfRangeException(nameof(fromColumn));
        if (fromColumn == toColumn) return; DeleteRange(fromColumn, toColumn);
        if (IsInsideTree) QueueDeletedChange();
    }
    private bool _deletionPending;
    private int _deletionVersion;
    private void QueueDeletedChange()
    {
        if (_deletionPending) return; _deletionPending = true; var version = _deletionVersion; var tree = Tree!;
        tree.Defer(() => { if (version != _deletionVersion) return; _deletionPending = false; if (!IsDisposed && ReferenceEquals(Tree, tree)) ChangedByUser(); });
    }
    private void DeleteRange(int from, int to)
    {
        _text = _text.Remove(UTF16Index(_text, from), UTF16Index(_text, to) - UTF16Index(_text, from));
        _caret -= Math.Clamp(_caret - from, 0, to - from); Deselect(); Invalidate(); FitCaret(); ResetBlink();
    }
    /// <summary>Deletes the scalar or configured grapheme immediately before the caret and emits TextChanged.</summary>
    public void DeleteCharAtCaret()
    {
        EnsureMutable(); if (_caret == 0) return; EnsureLayout();
        var from = !_caretMidGrapheme && _backspaceDeletesCompositeCharacterEnabled ? _sourceLayout.PreviousGrapheme(_caret) : _caret - 1;
        DeleteRange(from, _caret); ChangedByUser();
    }
    /// <summary>Selects a scalar interval when selection is enabled.</summary>
    /// <param name="fromColumn">Inclusive start, clamped to the text.</param><param name="toColumn">Exclusive end; negative means text length.</param>
    /// <remarks>An empty (0,0) clears selection; another reversed or empty interval leaves selection unchanged.</remarks>
    public void Select(int fromColumn = 0, int toColumn = -1)
    {
        EnsureMutable(); if (!_selectingEnabled) return; if (fromColumn == 0 && toColumn == 0) { Deselect(); return; }
        var count = ScalarCount(_text); fromColumn = Math.Clamp(fromColumn, 0, count); toColumn = toColumn < 0 ? count : Math.Min(toColumn, count);
        if (fromColumn >= toColumn) return; _selectionFrom = fromColumn; _selectionTo = toColumn; _selectionAnchor = fromColumn; _selecting = true; QueueRedraw();
    }
    /// <summary>Selects the whole text without moving a nonempty line's caret.</summary>
    public void SelectAll() { EnsureMutable(); if (!_selectingEnabled) return; if (_text.Length == 0) { _caret = 0; return; } Select(); }
    /// <summary>Clears selection.</summary>
    public void Deselect() { EnsureMutable(); _selecting = false; _selectionFrom = _selectionTo = 0; QueueRedraw(); }
    /// <summary>Returns whether a nonempty interval is selected.</summary><returns>True for an active selection.</returns>
    public bool HasSelection() { CheckLineEdit(); return _selecting; }
    /// <summary>Returns the selected source text, including password text when called programmatically.</summary><returns>The selected substring or empty.</returns>
    public string GetSelectedText() { CheckLineEdit(); return _selecting ? ScalarSlice(_text, _selectionFrom, _selectionTo) : ""; }
    /// <summary>Returns the selection start or minus one when absent.</summary><returns>The inclusive scalar start or minus one.</returns>
    public int GetSelectionFromColumn() { CheckLineEdit(); return _selecting ? _selectionFrom : -1; }
    /// <summary>Returns the selection end or minus one when absent.</summary><returns>The exclusive scalar end or minus one.</returns>
    public int GetSelectionToColumn() { CheckLineEdit(); return _selecting ? _selectionTo : -1; }
    /// <summary>Returns the preceding shaped-grapheme boundary.</summary><param name="column">Scalar column, clamped to Text.</param><returns>A scalar boundary.</returns>
    public int GetPreviousCompositeCharacterColumn(int column) { CheckLineEdit(); var count = ScalarCount(_text); column = Math.Clamp(column, 0, count); if (column == 0) return 0; EnsureLayout(); return _sourceLayout.PreviousGrapheme(column); }
    /// <summary>Returns the following shaped-grapheme boundary.</summary><param name="column">Scalar column, clamped to Text.</param><returns>A scalar boundary.</returns>
    public int GetNextCompositeCharacterColumn(int column) { CheckLineEdit(); var count = ScalarCount(_text); column = Math.Clamp(column, 0, count); if (column == count) return count; EnsureLayout(); return _sourceLayout.NextGrapheme(column); }
    /// <summary>Returns the horizontal text offset in pixels; scrolling is negative.</summary><returns>The current shaped-line offset.</returns>
    public float GetScrollOffset() { CheckLineEdit(); FitCaret(); return _scroll; }
    /// <summary>Returns whether undo has an earlier text state.</summary><returns>True when an earlier history entry exists.</returns>
    public bool HasUndo() { CheckLineEdit(); return _historyPosition > 0; }
    /// <summary>Returns whether redo has a later text state.</summary><returns>True when a later history entry exists.</returns>
    public bool HasRedo() { CheckLineEdit(); return _historyPosition + 1 < _history.Count; }
    private void Remember()
    {
        if (_historyPosition + 1 < _history.Count) _history.RemoveRange(_historyPosition + 1, _history.Count - _historyPosition - 1);
        if (_history.Count > 0 && _history[_historyPosition].Text == _text) return;
        _history.Add(new(_text, _caret, _scroll)); _historyPosition = _history.Count - 1;
    }
    private void ChangedByUser() { if (_deletionPending) { _deletionPending = false; _deletionVersion++; } Remember(); TextChanged?.Invoke(_text); }
    private void RestoreHistory(int step)
    {
        if (!_editable) return; var position = _historyPosition + step; if (position < 0 || position >= _history.Count) return;
        _historyPosition = position; var state = _history[position]; _text = state.Text; _caret = state.Caret; _scroll = state.Scroll;
        Deselect(); Invalidate(); ResetBlink(); TextChanged?.Invoke(_text);
    }
    /// <summary>Returns whether uncommitted IME text is present.</summary><returns>True for a nonempty composition.</returns>
    public bool HasIMEText() { CheckLineEdit(); return _ime.Length > 0; }
    /// <summary>Commits the current composition through InsertTextAtCaret, then closes the native IME session.</summary>
    public void ApplyIME() { EnsureMutable(); var text = _ime; CancelIME(); if (text.Length > 0) InsertTextAtCaret(text); }
    /// <summary>Discards composition and closes the native IME session.</summary>
    public void CancelIME() { EnsureMutable(); _ime = ""; _imeSelection = default; Invalidate(); ActivateIME(false); }
    private void ActivateIME(bool active)
    {
        var window = IsInsideTree ? GetWindow() : null;
        if (window is not null && window.GetWindowID() != DisplayServer.InvalidWindowId && DisplayServer.Service is { } display && display.HasFeatureCore(DisplayServer.Feature.Ime))
            display.WindowSetIMEActiveCore(active, window.GetWindowID());
    }
}
