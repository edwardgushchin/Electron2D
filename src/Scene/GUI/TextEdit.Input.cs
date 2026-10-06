using System.Text;
namespace Electron2D;

public partial class TextEdit
{
    private DragPayload<string>? _dragPayload;
    private bool _dragHandled;
    private long _lastDoubleClick;
    private int _doubleLine = -1;
    private Vector2i _dragFrom, _dragTo;
    private string _dragOriginal = "", _copiedLine = "";
    private bool CopyDrag => Input.IsKeyPressed(OperatingSystem.IsMacOS() ? Key.Meta : Key.Control);
    /// <summary>Deletes text preceding one or all carets through the typed hook.</summary><param name="caretIndex">Caret index or -1 for all.</param>
    public void Backspace(int caretIndex = -1) { EnsureTextMutable(); if (_editable) OnBackspace(caretIndex); }
    /// <summary>Returns the first non-space/non-tab scalar column.</summary><param name="line">Existing line.</param><returns>Scalar column, or line length.</returns>
    public int GetFirstNonWhitespaceColumn(int line) { var text = AtLine(line).Text; var column = 0; foreach (var rune in text.EnumerateRunes()) { if (rune.Value is not (' ' or '\t')) break; column++; } return column; }
    /// <summary>Returns indentation width in scalar spaces using configured tab stops.</summary><param name="line">Existing line.</param><returns>Indentation columns.</returns>
    public int GetIndentLevel(int line) { var text = AtLine(line).Text; var column = 0; foreach (var rune in text.EnumerateRunes()) { if (rune.Value == ' ') column++; else if (rune.Value == '\t') column += _tabSize - column % _tabSize; else break; } return column; }
    /// <summary>Copies the selections, or the current logical line when empty-selection copying is enabled.</summary><param name="caretIndex">Caret index or -1 for all.</param>
    public void Copy(int caretIndex = -1) { EnsureTextMutable(); OnCopy(caretIndex); }
    /// <summary>Cuts selected text or whole lines when empty-selection copying is enabled.</summary><param name="caretIndex">Caret index or -1 for all.</param>
    public void Cut(int caretIndex = -1) { EnsureTextMutable(); if (_editable) OnCut(caretIndex); }
    /// <summary>Pastes the platform clipboard at the selected carets.</summary><param name="caretIndex">Caret index or -1 for all.</param>
    public void Paste(int caretIndex = -1) { EnsureTextMutable(); if (_editable) OnPaste(caretIndex); }
    /// <summary>Pastes the platform primary selection.</summary><param name="caretIndex">Caret index or -1 for all.</param>
    public void PastePrimaryClipboard(int caretIndex = -1) { EnsureTextMutable(); if (_editable) OnPastePrimaryClipboard(caretIndex); }
    /// <summary>Handles an overridable backward deletion.</summary><param name="caretIndex">Caret index or -1 for all.</param>
    protected virtual void OnBackspace(int caretIndex) => DeleteAtCarets(true, false, false, caretIndex);
    /// <summary>Handles overridable clipboard copying.</summary><param name="caretIndex">Caret index or -1 for all.</param>
    protected virtual void OnCopy(int caretIndex)
    {
        var indices = SortedCaretIndices(caretIndex); if (HasSelection(caretIndex)) { _copiedLine = ""; DisplayServer.Service?.ClipboardSetCore(GetSelectedText(caretIndex)); return; }
        if (!_emptySelectionClipboardEnabled) return; var text = string.Join('\n', indices.Select(i => _carets[i].Position.Y).Distinct().Order().Select(line => _lines[line].Text)) + "\n";
        _copiedLine = _carets.Count == 1 ? text : ""; DisplayServer.Service?.ClipboardSetCore(text);
    }
    /// <summary>Handles overridable clipboard cutting.</summary><param name="caretIndex">Caret index or -1 for all.</param>
    protected virtual void OnCut(int caretIndex)
    {
        if (!_editable) return; OnCopy(caretIndex); if (HasSelection(caretIndex)) { DeleteSelection(caretIndex); return; }
        if (!_emptySelectionClipboardEnabled) return;
        BeginComplexOperation(); try { foreach (var line in SortedCaretIndices(caretIndex).Select(i => _carets[i].Position.Y).Distinct().OrderDescending()) RemoveLineAt(line); } finally { EndComplexOperation(); }
    }
    /// <summary>Handles overridable platform clipboard insertion.</summary><param name="caretIndex">Caret index or -1 for all.</param>
    protected virtual void OnPaste(int caretIndex)
    {
        if (!_editable) return; var text = Normalize(DisplayServer.Service?.ClipboardGetCore() ?? ""); if (text.Length == 0) return;
        if (_carets.Count == 1 && !_carets[0].Selected && _copiedLine.Length > 0 && _copiedLine == text) { InsertText(text, _carets[0].Position.Y, 0); AdjustViewportToCaret(); return; }
        var lines = text.Split('\n'); if (caretIndex != -1 || _carets.Count <= 1 || lines.Length != _carets.Count) { InsertTextAtCaret(text, caretIndex); return; }
        var order = GetSortedCarets(); BeginComplexOperation(); BeginMulticaretEdit(); try { for (var i = order.Length - 1; i >= 0; i--) InsertTextAtCaret(lines[i], order[i]); } finally { EndMulticaretEdit(); EndComplexOperation(); }
    }
    /// <summary>Handles overridable primary clipboard insertion.</summary><param name="caretIndex">Caret index or -1 for all.</param>
    protected virtual void OnPastePrimaryClipboard(int caretIndex) { if (_editable) InsertTextAtCaret(DisplayServer.Service?.ClipboardGetPrimaryCore() ?? "", caretIndex); }
    /// <summary>Handles one typed Unicode scalar insertion.</summary><param name="unicodeChar">Valid scalar.</param><param name="caretIndex">Caret index or -1 for all.</param>
    protected virtual void OnHandleUnicodeInput(int unicodeChar, int caretIndex) { if (!Rune.IsValid(unicodeChar)) throw new ArgumentOutOfRangeException(nameof(unicodeChar)); if (_editable) InsertTextAtCaret(new Rune(unicodeChar).ToString(), caretIndex); }
    /// <summary>Returns whether an IME composition is active.</summary><returns>Composition presence.</returns>
    public bool HasIMEText() { CheckTextEdit(); return _ime.Length > 0; }
    /// <summary>Returns a logical line with each caret's uncommitted composition inserted.</summary><param name="line">Existing line.</param><returns>Composed display text.</returns>
    public string GetLineWithIME(int line)
    {
        var text = AtLine(line).Text; if (_ime.Length == 0) return text;
        foreach (var index in SortedCaretIndices()) if (_carets[index].Position.Y == line) text = text.Insert(ScalarIndex(text, _carets[index].Position.X), _ime); return text;
    }
    private int DisplayColumn(int line, int column)
    {
        if (_ime.Length == 0) return column; var length = CountScalars(_ime.AsSpan()); var before = 0; foreach (var caret in _carets) if (!caret.Ignored && caret.Position.Y == line && caret.Position.X < column) before++; return column + before * length;
    }
    private int SourceColumn(int line, int column)
    {
        if (_ime.Length == 0) return column; var length = CountScalars(_ime.AsSpan()); var removed = 0; foreach (var index in GetSortedCarets()) { var caret = _carets[index]; if (caret.Position.Y != line) continue; var start = caret.Position.X + removed; if (column <= start) break; if (column <= start + length) return caret.Position.X; removed += length; }
        return Math.Clamp(column - removed, 0, _lines[line].Scalars);
    }
    /// <summary>Commits current composition into the document.</summary>
    public void ApplyIME() { EnsureTextMutable(); var text = _ime; CancelIME(); if (_editable && text.Length > 0) InsertTextAtCaret(text); }
    /// <summary>Discards uncommitted composition.</summary>
    public void CancelIME() { EnsureTextMutable(); if (_ime.Length > 0) { _ime = ""; _imeSelection = default; if (_highlighter is { IsDisposed: false }) _highlighter.ClearHighlightingCache(); InvalidateTextLayout(); } ActivateIME(false); }
    private void ActivateIME(bool active)
    {
        if (DisplayServer.Service is not { } display || !display.HasFeatureCore(DisplayServer.Feature.Ime)) return; var window = IsInsideTree ? GetWindow() : null; while (window?.Embedder is { } host) window = host.GetWindow(); if (window is null || window.GetWindowID() == DisplayServer.InvalidWindowId) return;
        display.WindowSetIMEActiveCore(active, window.GetWindowID()); if (active) { var local = (Vector2)GetPosAtLineColumn(_carets[0].Position.Y, _carets[0].Position.X) + new Vector2(0, _rowHeight); var position = GetViewport()!.GetScreenTransform() * GetGlobalTransformWithCanvas() * local; display.WindowSetIMEPositionCore((Vector2i)position, window.GetWindowID()); }
    }
    /// <inheritdoc />
    protected override void OnTextInput(string text) { if (!_editable || !HasFocus()) return; CancelIME(); InsertTextAtCaret(text); }
    /// <inheritdoc />
    protected override void OnIMECompositionChanged(string text, Vector2i selection)
    {
        if (!_editable || !HasFocus()) return; if (text.Length > 0 && HasSelection()) DeleteSelection(); _ime = Normalize(text); var count = CountScalars(_ime.AsSpan()); _imeSelection = new(Math.Clamp(selection.X, 0, count), Math.Clamp(selection.Y, 0, count - Math.Clamp(selection.X, 0, count))); if (_highlighter is { IsDisposed: false }) _highlighter.ClearHighlightingCache(); InvalidateTextLayout(); AdjustViewportToCaret(); ResetBlink();
    }
    private bool Action(InputEvent input, string action) => InputMap.HasAction(action) && input.IsActionPressed(action, allowEcho: true, exactMatch: true);
    private void MoveCaretTo(int index, Vector2i target, bool extend)
    {
        var caret = AtCaret(index); target = ClampPosition(target); if (extend && _selectingEnabled) { if (!caret.Selected) caret.Origin = caret.Position; caret.Selected = caret.Origin != target; _selectionMode = SelectionMode.Shift; } else { caret.Selected = false; caret.Origin = target; }
        caret.Position = target; caret.Wrap = GetLineWrapIndexAtColumn(target.Y, target.X); CaretsChanged(); AdjustViewportToCaret(index);
    }
    private void DeleteAtCarets(bool backwards, bool word, bool toEdge, int caretIndex = -1)
    {
        if (!_editable) return; EnsureTextLayout(); BeginComplexOperation(); BeginMulticaretEdit(); try
        {
            foreach (var index in SortedCaretIndices(caretIndex))
            {
                var caret = AtCaret(index); if (caret.Ignored) continue; if (caret.Selected) { var a = SelectionStart(caret); var b = SelectionEnd(caret); Replace(a, b, "", index); caret.Position = caret.Origin = a; caret.Selected = false; continue; }
                var from = caret.Position; var to = from; var line = _lines[from.Y];
                if (backwards) { if (from.X > 0) from.X = toEdge ? 0 : word ? WordRange(new(from.X, from.Y)).From : _backspaceDeletesCompositeCharacterEnabled && !_caretMidGrapheme ? line.Layout.PreviousGrapheme(from.X) : from.X - 1; else if (from.Y > 0) { from.Y--; from.X = _lines[from.Y].Scalars; } }
                else if (to.X < line.Scalars) to.X = toEdge ? line.Scalars : word ? WordRange(to).To : _caretMidGrapheme ? to.X + 1 : line.Layout.NextGrapheme(to.X); else if (to.Y + 1 < _lines.Count) { to.Y++; to.X = 0; }
                if (from == to && word) { if (backwards && from.X > 0) from.X--; else if (!backwards && to.X < line.Scalars) to.X++; }
                Replace(from, to, "", index); caret.Position = caret.Origin = from;
            }
        }
        finally { EndMulticaretEdit(); EndComplexOperation(); }
        CaretsChanged(); AdjustViewportToCaret();
    }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion motion)
        {
            _mouse = motion.Position; if (_draggingMinimap) { SetLineAsCenterVisible(GetMinimapLineAtPos((Vector2i)motion.Position)); AcceptEvent(); }
            else if (_draggingCursor)
            {
                var point = GetLineColumnAtPos((Vector2i)motion.Position);
                if (_selectionMode == SelectionMode.Word) { var word = WordRange(point); Select(_pointerOrigin.Y, _pointerOrigin.X, point.Y, Offset(point) >= Offset(_pointerOrigin) ? word.To : word.From); }
                else if (_selectionMode == SelectionMode.Line) { var from = Math.Min(_pointerOrigin.Y, point.Y); var to = Math.Max(_pointerOrigin.Y, point.Y); Select(from, 0, to + 1 < _lines.Count ? to + 1 : to, to + 1 < _lines.Count ? 0 : _lines[to].Scalars); }
                else MoveCaretTo(0, point, true); AcceptEvent();
            }
            return;
        }
        if (inputEvent is InputEventMouseButton mouse)
        {
            _mouse = mouse.Position; if (mouse.Pressed && mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown) { RequestVerticalScroll(_verticalTarget + (mouse.ButtonIndex == MouseButton.WheelUp ? -3 : 3) * mouse.Factor); AcceptEvent(); return; }
            if (mouse.Pressed && mouse.ButtonIndex is MouseButton.WheelLeft or MouseButton.WheelRight) { RequestHorizontalScroll(_scrollHorizontal + (mouse.ButtonIndex == MouseButton.WheelLeft ? -40 : 40)); AcceptEvent(); return; }
            if (mouse.ButtonIndex == MouseButton.Right && mouse.Pressed) { if (_caretMoveOnRightClick && !IsMouseOverSelection()) MoveCaretTo(0, GetLineColumnAtPos((Vector2i)mouse.Position), false); if (_contextMenuEnabled) { PrepareMenu(); var at = GetViewport()!.GetScreenTransform() * GetGlobalTransformWithCanvas() * mouse.Position; _menu.Popup(new((Vector2i)at, Vector2i.Zero)); AcceptEvent(); } return; }
            if (mouse.ButtonIndex == MouseButton.Middle && mouse.Pressed && _middleMousePasteEnabled && _editable) { MoveCaretTo(0, GetLineColumnAtPos((Vector2i)mouse.Position), false); PastePrimaryClipboard(0); AcceptEvent(); return; }
            if (mouse.ButtonIndex != MouseButton.Left) return;
            if (!mouse.Pressed) { _draggingCursor = _draggingMinimap = false; if (_dragAttempt && _dragPayload == null) { _dragAttempt = false; MoveCaretTo(0, GetLineColumnAtPos((Vector2i)mouse.Position), false); } if (HasSelection() && DisplayServer.Service is { } display && display.HasFeatureCore(DisplayServer.Feature.ClipboardPrimary)) display.ClipboardSetPrimaryCore(GetSelectedText()); AcceptEvent(); return; }
            EnsureTextLayout(); if (_minimapDraw && MinimapRect().HasPoint(mouse.Position)) { _draggingMinimap = true; SetLineAsCenterVisible(GetMinimapLineAtPos((Vector2i)mouse.Position)); AcceptEvent(); return; }
            var point = GetLineColumnAtPos((Vector2i)mouse.Position); var gutter = HitGutter(mouse.Position); if (gutter >= 0) { if (IsLineGutterClickable(point.Y, gutter)) GutterClicked?.Invoke(point.Y, gutter); AcceptEvent(); return; }
            if (mouse.AltPressed) { AddCaret(point.Y, point.X); AcceptEvent(); return; }
            RemoveSecondaryCarets();
            if (_selectingEnabled && point.Y == _doubleLine && Environment.TickCount64 - _lastDoubleClick < 500) { _lastDoubleClick = 0; _pointerOrigin = new(0, point.Y); Select(point.Y, 0, point.Y + 1 < _lines.Count ? point.Y + 1 : point.Y, point.Y + 1 < _lines.Count ? 0 : _lines[point.Y].Scalars); _selectionMode = SelectionMode.Line; _draggingCursor = true; }
            else if (mouse.DoubleClick && _selectingEnabled) { var word = WordRange(point); Select(point.Y, word.From, point.Y, word.To); _pointerOrigin = new(word.From, point.Y); _selectionMode = SelectionMode.Word; _draggingCursor = true; _lastDoubleClick = Environment.TickCount64; _doubleLine = point.Y; }
            else if (_dragAndDropSelectionEnabled && !mouse.ShiftPressed && GetSelectionAtLineColumn(point.Y, point.X, false) >= 0) { _dragAttempt = true; _draggingCursor = false; }
            else { MoveCaretTo(0, point, mouse.ShiftPressed); _pointerOrigin = _carets[0].Origin; _draggingCursor = _selectingEnabled; _selectionMode = SelectionMode.Pointer; }
            AcceptEvent(); return;
        }
        if (inputEvent is not InputEventKey { Pressed: true } key) return;
        if (_shortcutKeysEnabled)
        {
            if (Action(key, "ui_text_select_all")) { SelectAll(); AcceptEvent(); return; }
            if (Action(key, "ui_copy")) { Copy(); AcceptEvent(); return; }
            if (Action(key, "ui_cut")) { Cut(); AcceptEvent(); return; }
            if (Action(key, "ui_paste")) { Paste(); AcceptEvent(); return; }
            if (Action(key, "ui_undo")) { if (_editable) Undo(); AcceptEvent(); return; }
            if (Action(key, "ui_redo")) { if (_editable) Redo(); AcceptEvent(); return; }
        }
        if (_ime.Length > 0) return;
        if (_editable && (Action(key, "ui_text_backspace") || Action(key, "ui_text_backspace_word") || Action(key, "ui_text_backspace_all_to_left"))) { if (Action(key, "ui_text_backspace")) OnBackspace(-1); else DeleteAtCarets(true, Action(key, "ui_text_backspace_word"), Action(key, "ui_text_backspace_all_to_left")); AcceptEvent(); return; }
        if (_editable && (Action(key, "ui_text_delete") || Action(key, "ui_text_delete_word") || Action(key, "ui_text_delete_all_to_right"))) { DeleteAtCarets(false, Action(key, "ui_text_delete_word"), Action(key, "ui_text_delete_all_to_right")); AcceptEvent(); return; }
        if (_editable && Action(key, "ui_text_newline")) { InsertTextAtCaret("\n"); AcceptEvent(); return; }
        if (_editable && _tabInputMode && Action(key, "ui_focus_next")) { InsertTextAtCaret("\t"); AcceptEvent(); return; }
        using var movement = new InputEventKey { Keycode = key.Keycode, PhysicalKeycode = key.PhysicalKeycode, KeyLabel = key.KeyLabel, Pressed = true, ControlPressed = key.ControlPressed, AltPressed = key.AltPressed, MetaPressed = key.MetaPressed }; EnsureTextLayout(); var moved = false;
        for (var i = 0; i < _carets.Count; i++)
        {
            var caret = _carets[i]; var target = caret.Position; var row = VisualIndex(target.Y, GetCaretWrapIndex(i)); var vertical = 0;
            if (Action(movement, "ui_text_caret_left")) { if (target.X > 0) target.X = _caretMidGrapheme ? target.X - 1 : _lines[target.Y].Layout.PreviousGrapheme(target.X); else if (target.Y > 0) { target.Y--; target.X = _lines[target.Y].Scalars; } }
            else if (Action(movement, "ui_text_caret_right")) { if (target.X < _lines[target.Y].Scalars) target.X = _caretMidGrapheme ? target.X + 1 : _lines[target.Y].Layout.NextGrapheme(target.X); else if (target.Y + 1 < _lines.Count) { target.Y++; target.X = 0; } }
            else if (Action(movement, "ui_text_caret_word_left")) { target = PositionAt(Math.Max(0, Offset(target) - 1)); target.X = WordRange(target).From; }
            else if (Action(movement, "ui_text_caret_word_right")) { target = PositionAt(Math.Min(Offset(new(_lines[^1].Scalars, _lines.Count - 1)), Offset(target) + 1)); target.X = WordRange(target).To; }
            else if (Action(movement, "ui_text_caret_line_start")) target.X = 0;
            else if (Action(movement, "ui_text_caret_line_end")) target.X = _lines[target.Y].Scalars;
            else if (Action(movement, "ui_text_caret_document_start")) target = default;
            else if (Action(movement, "ui_text_caret_document_end")) target = new(_lines[^1].Scalars, _lines.Count - 1);
            else if (Action(movement, "ui_text_caret_up")) vertical = -1; else if (Action(movement, "ui_text_caret_down")) vertical = 1; else if (Action(movement, "ui_text_caret_page_up")) vertical = -GetVisibleLineCount(); else if (Action(movement, "ui_text_caret_page_down")) vertical = GetVisibleLineCount(); else continue;
            if (vertical != 0) { var next = _visual[Math.Clamp(row + vertical, 0, _visual.Count - 1)]; var x = caret.DesiredX >= 0 ? caret.DesiredX : CaretX(_lines[target.Y], target.X, GetCaretWrapIndex(i)); target = GetLineColumnAtPos(new((int)(_contentRect.Position.X + x - _scrollHorizontal), (int)(_contentRect.Position.Y + (next.Y / _rowHeight - _scrollVertical + .5) * _rowHeight))); caret.DesiredX = x; } else caret.DesiredX = -1;
            MoveCaretTo(i, target, key.ShiftPressed); moved = true;
        }
        if (moved) { MergeOverlappingCarets(); AcceptEvent(); return; }
        if (Action(key, "ui_text_toggle_insert_mode")) { SetOvertypeModeEnabled(!_overtype); AcceptEvent(); return; }
        if (_editable && key.Unicode >= 32 && Rune.IsValid(key.Unicode) && !key.ControlPressed && !key.AltPressed && !key.MetaPressed) { OnHandleUnicodeInput(key.Unicode, -1); AcceptEvent(); }
    }
    private int HitGutter(Vector2 point) { var x = IsLayoutRTL() ? _contentRect.End.X : _contentRect.Position.X - GetTotalGutterWidth(); for (var i = 0; i < _gutters.Count; i++) { var gutter = _gutters[i]; if (!gutter.Draw) continue; if (point.X >= x && point.X < x + gutter.Width) return i; x += gutter.Width; } return -1; }
    /// <summary>Reports whether the current pointer lies inside any nonempty selection.</summary><param name="edges">Includes selection endpoints.</param><param name="caretIndex">Specific caret or -1 for all.</param><returns>Selection hit.</returns>
    public bool IsMouseOverSelection(bool edges = true, int caretIndex = -1) { CheckTextEdit(); var point = GetLineColumnAtPos((Vector2i)_mouse); var index = GetSelectionAtLineColumn(point.Y, point.X, edges); return index >= 0 && (caretIndex == -1 || index == caretIndex); }
    /// <summary>Reports whether the pointer is extending a selection.</summary><returns>Pointer selection state.</returns>
    public bool IsDraggingCursor() { CheckTextEdit(); return _draggingCursor; }
    /// <summary>Sets a typed local-position tooltip provider.</summary><param name="callback">Provider, or null to restore TooltipText.</param>
    public void SetTooltipRequestFunc(Func<Vector2, string>? callback) { EnsureTextMutable(); _tooltip = callback; }
    /// <inheritdoc />
    protected override string OnGetTooltip(Vector2 atPosition) => _tooltip?.Invoke(atPosition) ?? base.OnGetTooltip(atPosition);
    /// <inheritdoc />
    protected override DragPayload? OnGetDragData(Vector2 atPosition)
    {
        if (!_dragAndDropSelectionEnabled || !_dragAttempt || !IsMouseOverSelection()) return null; _draggingCursor = false; _dragFrom = SelectionStart(_carets[0]); _dragTo = SelectionEnd(_carets[0]); _dragOriginal = Text; var text = GetSelectedText(); SetDragPreview(new Label(text) { AutoTranslateMode = NodeAutoTranslateMode.Disabled }); _dragHandled = false; return _dragPayload = new DragPayload<string>(text);
    }
    /// <inheritdoc />
    protected override bool OnCanDropData(Vector2 atPosition, DragPayload payload) => _editable && payload is DragPayload<string>;
    /// <inheritdoc />
    protected override void OnDropData(Vector2 atPosition, DragPayload payload)
    {
        if (!OnCanDropData(atPosition, payload)) return; var target = GetLineColumnAtPos((Vector2i)atPosition); var offset = Offset(target); BeginComplexOperation(); try { if (ReferenceEquals(payload, _dragPayload)) { _dragHandled = true; var start = Offset(_dragFrom); var end = Offset(_dragTo); if (offset >= start && offset <= end) return; if (!CopyDrag) { Replace(_dragFrom, _dragTo, ""); if (offset > end) offset -= end - start; target = PositionAt(offset); } } RemoveSecondaryCarets(); MoveCaretTo(0, target, false); InsertTextAtCaret(((DragPayload<string>)payload).Value, 0); } finally { EndComplexOperation(); }
    }
    private void FinishTextDrag() { var payload = _dragPayload; _dragPayload = null; _dragAttempt = false; if (payload != null && !_dragHandled && _editable && IsDragSuccessful() && !CopyDrag && Text == _dragOriginal) DeleteSelection(); _dragHandled = false; _dragOriginal = ""; }
}
