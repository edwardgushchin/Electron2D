using System.Text;
namespace Electron2D;

public partial class LineEdit
{
    private bool _dragAttempt, _dragHandled;
    private int _dragFrom, _dragTo;
    private DragPayload<string>? _dragPayload;
    private bool CopyDrag => Input.Instance.IsKeyPressed(OperatingSystem.IsMacOS() ? Key.Meta : Key.Control);
    /// <inheritdoc />
    protected override void OnTextInput(string text)
    {
        if (!_editing || !_editable) return;
        _ime = ""; _imeSelection = default; UserInsert(text);
    }
    private void UserInsert(string text)
    {
        text = text.Replace("\r", "").Replace("\n", "").Replace("\t", "");
        if (text.Length == 0) return; var before = _text;
        if (_selecting) DeleteRange(_selectionFrom, _selectionTo);
        List<Exception>? errors = null;
        try { InsertTextAtCaret(text); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed && _text != before) ChangedByUser(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("LineEdit insertion callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override void OnIMECompositionChanged(string text, Vector2i selection)
    {
        if (!_editing || !_editable) return;
        var changed = text.Length > 0 && _selecting; if (changed) DeleteRange(_selectionFrom, _selectionTo);
        _ime = text; var count = ScalarCount(text); _imeSelection = new(Math.Clamp(selection.X, 0, count), Math.Clamp(selection.Y, 0, count - Math.Clamp(selection.X, 0, count)));
        Invalidate(); FitCaret(); ResetBlink(); if (changed) ChangedByUser();
    }
    private bool Action(InputEvent input, string action) => InputMap.Instance.HasAction(action) && input.IsActionPressed(action, allowEcho: true, exactMatch: true);
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion motion)
        {
            if (_pointerSelecting) { MoveCaret(HitColumn(motion.Position), true); AcceptEvent(); }
            return;
        }
        if (inputEvent is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.Middle && mouse.Pressed && _editable && _middleMousePasteEnabled)
            { Edit(); CaretColumn = HitColumn(mouse.Position); Deselect(); UserInsert(DisplayServer.Instance?.ClipboardGetPrimary() ?? ""); AcceptEvent(); return; }
            if (mouse.ButtonIndex != MouseButton.Left) return;
            if (!mouse.Pressed)
            {
                if (_selectAllOnRelease) { _selectAllOnRelease = false; _pointerSelecting = false; _dragAttempt = false; SelectAll(); AcceptEvent(); return; }
                _pointerSelecting = false; if (_dragAttempt && _dragPayload is null) { _dragAttempt = false; MoveCaret(HitColumn(mouse.Position), false); }
                var clear = _clearPressed; _clearPressed = false;
                if (clear && IconRect().HasPoint(mouse.Position) && _editable) Clear();
                if (_selecting && !_secret && DisplayServer.Instance is { } display && display.HasFeature(DisplayServer.Feature.ClipboardPrimary)) display.ClipboardSetPrimary(GetSelectedText());
                QueueRedraw(); AcceptEvent(); return;
            }
            Edit(); EnsureLayout();
            if (IsClearIcon && IconRect().HasPoint(mouse.Position)) { _clearPressed = true; QueueRedraw(); AcceptEvent(); return; }
            if (_selectAllOnRelease) { AcceptEvent(); return; }
            var column = HitColumn(mouse.Position);
            if (mouse.DoubleClick && _selectingEnabled) { var from = _sourceLayout.PreviousWord(Math.Min(column + 1, ScalarCount(_text))); var to = _sourceLayout.NextWord(column); Select(from, to); _caret = to; }
            else if (_selecting && _dragAndDropSelectionEnabled && !mouse.ShiftPressed && column >= _selectionFrom && column <= _selectionTo) { _dragAttempt = true; _pointerSelecting = false; }
            else { _dragAttempt = false; MoveCaret(column, mouse.ShiftPressed); _selectionAnchor = mouse.ShiftPressed ? _selectionAnchor : _caret; _pointerSelecting = _selectingEnabled; }
            AcceptEvent(); return;
        }
        if (inputEvent is not InputEventKey { Pressed: true } key) return;
        if (!_editing)
        {
            if (_editable && Action(key, "ui_text_submit")) { Edit(); EditingToggled?.Invoke(_editing); AcceptEvent(); }
            return;
        }
        if (Action(key, "ui_text_submit"))
        {
            AcceptEvent(); List<Exception>? errors = null;
            try { TextSubmitted?.Invoke(_text); } catch (Exception error) { CollectException(ref errors, error); }
            try { if (!IsDisposed && _editing && !_keepEditingOnTextSubmit) { Unedit(); EditingToggled?.Invoke(false); } } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("LineEdit submission callbacks failed.", errors); return;
        }
        if (Action(key, "ui_cancel")) { AcceptEvent(); Unedit(); EditingToggled?.Invoke(false); return; }
        if (_shortcutKeysEnabled)
        {
            if (Action(key, "ui_text_select_all")) { SelectAll(); AcceptEvent(); return; }
            if (Action(key, "ui_copy")) { CopySelection(); AcceptEvent(); return; }
            if (Action(key, "ui_cut")) { if (_editable && !_secret && _selecting) { CopySelection(); DeleteRange(_selectionFrom, _selectionTo); ChangedByUser(); } AcceptEvent(); return; }
            if (Action(key, "ui_paste")) { if (_editable) UserInsert(DisplayServer.Instance?.ClipboardGet() ?? ""); AcceptEvent(); return; }
            if (Action(key, "ui_undo")) { RestoreHistory(-1); AcceptEvent(); return; }
            if (Action(key, "ui_redo")) { RestoreHistory(1); AcceptEvent(); return; }
        }
        if (_ime.Length > 0) return;
        if (_editable && (Action(key, "ui_text_backspace") || Action(key, "ui_text_backspace_word") || Action(key, "ui_text_backspace_all_to_left")))
        {
            EnsureLayout(); if (_selecting) { DeleteRange(_selectionFrom, _selectionTo); ChangedByUser(); }
            else if (_caret > 0) { var from = Action(key, "ui_text_backspace_all_to_left") ? 0 : Action(key, "ui_text_backspace_word") ? _sourceLayout.PreviousWord(_caret) : !_caretMidGrapheme && _backspaceDeletesCompositeCharacterEnabled ? _sourceLayout.PreviousGrapheme(_caret) : _caret - 1; DeleteRange(from, _caret); ChangedByUser(); }
            AcceptEvent(); return;
        }
        if (_editable && (Action(key, "ui_text_delete") || Action(key, "ui_text_delete_word") || Action(key, "ui_text_delete_all_to_right")))
        {
            EnsureLayout(); var count = ScalarCount(_text); if (_selecting) { DeleteRange(_selectionFrom, _selectionTo); ChangedByUser(); }
            else if (_caret < count) { var to = Action(key, "ui_text_delete_all_to_right") ? count : Action(key, "ui_text_delete_word") ? _sourceLayout.NextWord(_caret) : _caretMidGrapheme ? _caret + 1 : _sourceLayout.NextGrapheme(_caret); DeleteRange(_caret, to); ChangedByUser(); }
            AcceptEvent(); return;
        }
        // Movement ignores Shift for action matching; the modifier extends selection instead.
        using var movement = new InputEventKey { Keycode = key.Keycode, PhysicalKeycode = key.PhysicalKeycode, KeyLabel = key.KeyLabel, Pressed = true, ControlPressed = key.ControlPressed, AltPressed = key.AltPressed, MetaPressed = key.MetaPressed };
        EnsureLayout(); var target = _caret; var moved = true;
        if (Action(movement, "ui_text_caret_word_left")) target = _sourceLayout.PreviousWord(_caret);
        else if (Action(movement, "ui_text_caret_word_right")) target = _sourceLayout.NextWord(_caret);
        else if (Action(movement, "ui_text_caret_left")) target = _selecting && !key.ShiftPressed ? _selectionFrom : _caretMidGrapheme ? Math.Max(0, _caret - 1) : _sourceLayout.PreviousGrapheme(_caret);
        else if (Action(movement, "ui_text_caret_right")) target = _selecting && !key.ShiftPressed ? _selectionTo : _caretMidGrapheme ? Math.Min(ScalarCount(_text), _caret + 1) : _sourceLayout.NextGrapheme(_caret);
        else if (Action(movement, "ui_text_caret_line_start") || Action(movement, "ui_text_caret_up") || Action(movement, "ui_text_caret_page_up")) target = 0;
        else if (Action(movement, "ui_text_caret_line_end") || Action(movement, "ui_text_caret_down") || Action(movement, "ui_text_caret_page_down")) target = ScalarCount(_text);
        else moved = false;
        if (moved) { MoveCaret(target, key.ShiftPressed); AcceptEvent(); return; }
        if (Action(key, "ui_swap_input_direction")) { _inputDirection = _inputDirection == TextDirection.LTR ? TextDirection.RTL : TextDirection.LTR; FitCaret(); AcceptEvent(); return; }
        if (_editable && key.Unicode >= 32 && Rune.IsValid(key.Unicode) && !key.ControlPressed && !key.AltPressed && !key.MetaPressed) { UserInsert(new Rune(key.Unicode).ToString()); AcceptEvent(); }
    }
    private void FinishTextDrag()
    {
        var payload = _dragPayload; _dragPayload = null; _dragAttempt = false;
        if (payload is not null && !_dragHandled && _editable && IsDragSuccessful() && !CopyDrag && _dragTo <= ScalarCount(_text) && ScalarSlice(_text, _dragFrom, _dragTo) == payload.Value) { DeleteRange(_dragFrom, _dragTo); ChangedByUser(); }
        _dragHandled = false;
    }
    private void CopySelection() { if (!_secret && _selecting) DisplayServer.Instance?.ClipboardSet(GetSelectedText()); }
    private void MoveCaret(int column, bool extend)
    {
        var previous = _caret; if (extend && _selectingEnabled) { if (!_selecting) _selectionAnchor = previous; _selectionFrom = Math.Min(_selectionAnchor, column); _selectionTo = Math.Max(_selectionAnchor, column); _selecting = _selectionFrom != _selectionTo; }
        else Deselect(); CaretColumn = column;
    }
    /// <inheritdoc />
    protected override DragPayload? OnGetDragData(Vector2 atPosition)
    {
        if (!_dragAndDropSelectionEnabled || !_selecting || _secret || !_dragAttempt) return null;
        var column = HitColumn(atPosition); if (column < _selectionFrom || column > _selectionTo) return null;
        _pointerSelecting = false; var text = GetSelectedText(); _dragFrom = _selectionFrom; _dragTo = _selectionTo; _dragHandled = false;
        var preview = new Label(text) { AutoTranslateMode = NodeAutoTranslateMode.Disabled }; SetDragPreview(preview); return _dragPayload = new DragPayload<string>(text);
    }
    /// <inheritdoc />
    protected override bool OnCanDropData(Vector2 atPosition, DragPayload payload) => _editable && payload is DragPayload<string>;
    /// <inheritdoc />
    protected override void OnDropData(Vector2 atPosition, DragPayload payload)
    {
        if (!OnCanDropData(atPosition, payload)) return; ApplyIME(); Edit(); var column = HitColumn(atPosition); var text = ((DragPayload<string>)payload).Value;
        var self = ReferenceEquals(payload, _dragPayload);
        if (self)
        {
            _dragHandled = true;
            if (column >= _dragFrom && column <= _dragTo && (!CopyDrag || column > _dragFrom && column < _dragTo)) return;
            if (!CopyDrag && _text.Length > 0) { if (column > _dragTo) column -= _dragTo - _dragFrom; DeleteRange(_dragFrom, _dragTo); }
        }
        else if (_selecting && column >= _selectionFrom && column <= _selectionTo) { column = _selectionFrom; DeleteRange(_selectionFrom, _selectionTo); }
        Deselect(); CaretColumn = column; var before = _text;
        List<Exception>? errors = null; try { InsertTextAtCaret(text); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) { Select(column, _caret); if (before != _text || self && !CopyDrag) ChangedByUser(); } } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("LineEdit drop callbacks failed.", errors);
    }
}
