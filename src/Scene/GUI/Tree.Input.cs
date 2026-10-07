using System.Text;
namespace Electron2D;

public partial class Tree
{
    private TreeItem? _selectionAnchor;
    private int _pressedHeader = -1;
    private bool Action(InputEvent input, string name) => InputMap.HasAction(name) && input.IsActionPressed(name, allowEcho: true, exactMatch: false);
    private int RowIndex(TreeItem? item) { for (var i = 0; i < _rows.Count; i++) if (_rows[i].Item == item) return i; return -1; }
    private int HeaderAt(Vector2 point) { EnsureTreeLayout(); if (!_titlesVisible || point.Y < _body.Position.Y - _titleHeight || point.Y >= _body.Position.Y) return -1; for (var i = 0; i < _columns.Count; i++) { var rect = LocalRect(new(new(_columns[i].X, 0), new(_columns[i].Width, 1))); if (point.X >= rect.Position.X && point.X < rect.End.X) return i; } return -1; }
    private void SelectRange(TreeItem target, int column)
    {
        if (_selectionAnchor == null || _selectionAnchor.Owner != this) { SelectItemCell(target, column, true, true); _selectionAnchor = target; return; }
        EnsureTreeLayout(); var a = RowIndex(_selectionAnchor); var b = RowIndex(target); if (a < 0 || b < 0) return;
        var changes = BeginSelectionChanges();
        try
        {
            for (var index = Math.Min(a, b); index <= Math.Max(a, b); index++)
                for (var c = 0; c < _columns.Count; c++)
                {
                    var item = _rows[index].Item; var cell = item.Cells[c];
                    if (!cell.Selectable || cell.Selected) continue; cell.Selected = true; changes.Add((item, c));
                }
            _selected = target; _selectedColumn = column; QueueRedraw(); List<Exception>? errors = null;
            try { CellSelected?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
            foreach (var change in changes)
                if (!IsDisposed && !change.Item.IsDisposed && change.Item.Owner == this)
                    try { MultiSelected?.Invoke(change.Item, change.Column, true); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Tree range-selection callbacks failed.", errors);
        }
        finally { changes.Clear(); _selectionDepth--; }
    }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion motion) { _mouse = motion.Position; _hovered = GetItemAtPosition(_mouse); _hoverColumn = GetColumnAtPosition(_mouse); _hoverHeader = HeaderAt(_mouse); QueueRedraw(); return; }
        if (inputEvent is InputEventMouseButton mouse)
        {
            _mouse = mouse.Position; if (mouse.Pressed && mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown) { if (mouse.ShiftPressed && _scrollH) _hBar.Value += (mouse.ButtonIndex == MouseButton.WheelUp ? -1 : 1) * GetThemeConstant("scroll_speed") * mouse.Factor; else if (_scrollV) _vBar.Value += (mouse.ButtonIndex == MouseButton.WheelUp ? -1 : 1) * GetThemeConstant("scroll_speed") * mouse.Factor; AcceptEvent(); return; }
            if (!mouse.Pressed)
            {
                _pressedHeader = -1;
                try
                {
                    if (_pressedItem is { IsDisposed: false } pressed && pressed.Owner == this && _pressedButton != -1)
                    {
                        var column = GetColumnAtPosition(_mouse); var id = GetButtonIDAtPosition(_mouse);
                        if (GetItemAtPosition(_mouse) == pressed && column == _pressedColumn && id == _pressedButtonID && ButtonIndexAt(pressed, column, _mouse) == _pressedButton) ButtonClicked?.Invoke(pressed, column, id, mouse.ButtonIndex);
                    }
                }
                finally { _pressedItem = null; _pressedButton = -1; if (!IsDisposed) QueueRedraw(); }
                return;
            }
            var header = HeaderAt(_mouse); if (header >= 0) { _pressedHeader = header; QueueRedraw(); ColumnTitleClicked?.Invoke(header, mouse.ButtonIndex); if (!IsDisposed) AcceptEvent(); return; }
            var item = GetItemAtPosition(_mouse); var col = GetColumnAtPosition(_mouse); if (item == null || col < 0) { if (mouse.ButtonIndex == MouseButton.Left) { DeselectAll(); _selected = null; NothingSelected?.Invoke(); } if (!IsDisposed) EmptyClicked?.Invoke(_mouse, mouse.ButtonIndex); AcceptEvent(); return; }
            if (mouse.ButtonIndex != MouseButton.Left && !(mouse.ButtonIndex == MouseButton.Right && _allowRMBSelect)) { if (item.Cells[col].Mode == TreeItem.TreeCellMode.Custom && item.Cells[col].CustomButton) CustomItemClicked?.Invoke(mouse.ButtonIndex); return; }
            var button = GetButtonIDAtPosition(_mouse); if (button != -1) { var at = ButtonIndexAt(item, col, _mouse); if (!item.IsButtonDisabled(col, at)) { _pressedItem = item; _pressedColumn = col; _pressedButton = at; _pressedButtonID = button; QueueRedraw(); } AcceptEvent(); return; }
            var rowIndex = RowAt(_mouse); var row = _rows[rowIndex]; if (col == 0 && !_hideFolding && !item.DisableFolding && item.Children.Count > 0 && FoldRect(row).HasPoint(_mouse)) { if (_recursiveFold && (mouse.ShiftPressed || mouse.ControlPressed || mouse.MetaPressed)) item.SetCollapsedRecursive(!item.Collapsed); else item.Collapsed = !item.Collapsed; if (!IsDisposed) AcceptEvent(); return; }
            if (_selectMode == SelectMode.Multi && mouse.ShiftPressed) SelectRange(item, col);
            else if (_selectMode == SelectMode.Multi && (mouse.ControlPressed || mouse.MetaPressed)) SelectItemCell(item, col, !item.IsSelected(col), true);
            else { SetSelected(item, col); _selectionAnchor = item; }
            if (IsDisposed || item.IsDisposed || item.Owner != this || col >= item.Cells.Count) return; ItemMouseSelected?.Invoke(_mouse, mouse.ButtonIndex);
            if (IsDisposed || item.IsDisposed || item.Owner != this || col >= item.Cells.Count) return;
            if (mouse.DoubleClick) { if (LocalRect(item.Cells[col].IconRect).HasPoint(_mouse)) ItemIconDoubleClicked?.Invoke(); if (!IsDisposed) ItemActivated?.Invoke(); if (!IsDisposed && item.Cells[col].Editable) EditSelected(); }
            else if (item.Cells[col].Editable && item.Cells[col].Mode is TreeItem.TreeCellMode.Check or TreeItem.TreeCellMode.Custom) { if (item.Cells[col].Mode == TreeItem.TreeCellMode.Custom) { if (item.Cells[col].CustomButton) { _pressedItem = item; _pressedColumn = col; _pressedButton = -1; QueueRedraw(); } CustomItemClicked?.Invoke(mouse.ButtonIndex); } if (!IsDisposed && !item.IsDisposed && item.Owner == this) EditSelected(); }
            if (!IsDisposed) { EnsureCursorIsVisible(); AcceptEvent(); }
            return;
        }
        if (inputEvent is not InputEventKey { Pressed: true } key) return; EnsureTreeLayout();
        if (Action(key, "ui_accept")) { if (_selected != null) { if (!EditSelected()) ItemActivated?.Invoke(); if (!IsDisposed) AcceptEvent(); } return; }
        if (Action(key, "ui_cancel")) { CancelEditor(); AcceptEvent(); return; }
        if (_selected == null && _rows.Count > 0) { var first = _rows[0].Item; SelectItemCell(first, 0, true, true); if (IsDisposed) return; }
        if (_selected != null)
        {
            var index = RowIndex(_selected); var target = index; var column = _selectedColumn;
            if (Action(key, "ui_up")) target--;
            else if (Action(key, "ui_down")) target++;
            else if (Action(key, "ui_home")) target = 0;
            else if (Action(key, "ui_end")) target = _rows.Count - 1;
            else if (Action(key, "ui_page_up")) target -= Math.Max(1, (int)(_body.Size.Y / Math.Max(1, _rows[Math.Max(0, index)].Height)));
            else if (Action(key, "ui_page_down")) target += Math.Max(1, (int)(_body.Size.Y / Math.Max(1, _rows[Math.Max(0, index)].Height)));
            else if (Action(key, "ui_left")) { if (_selected.Children.Count > 0 && !_selected.Collapsed) { if (_recursiveFold && key.ShiftPressed) _selected.SetCollapsedRecursive(true); else _selected.Collapsed = true; AcceptEvent(); return; } if (column > 0) column--; else if (_selected.ParentItem != null) target = RowIndex(_selected.ParentItem); }
            else if (Action(key, "ui_right")) { if (_selected.Collapsed && _selected.Children.Count > 0) { if (_recursiveFold && key.ShiftPressed) _selected.SetCollapsedRecursive(false); else _selected.Collapsed = false; AcceptEvent(); return; } if (column + 1 < _columns.Count) column++; else if (_selected.Children.Count > 0) target = index + 1; }
            else { SearchKey(key); return; }
            if (_rows.Count > 0) { target = Math.Clamp(target, 0, _rows.Count - 1); var item = _rows[target].Item; if (_selectMode == SelectMode.Multi && key.ShiftPressed) SelectRange(item, column); else { SetSelected(item, column); _selectionAnchor = item; } if (!IsDisposed) { EnsureCursorIsVisible(); AcceptEvent(); } }
            return;
        }
        SearchKey(key);
    }
    private void SearchKey(InputEventKey key)
    {
        if (!_allowSearch || key.Unicode < 32 || !Rune.IsValid(key.Unicode) || key.ControlPressed || key.MetaPressed || key.AltPressed) return; var now = Environment.TickCount64 / 1000.0; if ((now - _searchTime) * 1000 > ProjectSettings.Get(ProjectSettings.IncrementalSearchMaxIntervalMsec)) _search = ""; _searchTime = now; var rune = new Rune(key.Unicode).ToString(); if (!string.Equals(_search, rune, StringComparison.OrdinalIgnoreCase)) _search += rune;
        var from = _selected == null ? -1 : RowIndex(_selected); for (var n = 0; n < _rows.Count; n++) { var item = _rows[(from + 1 + n) % _rows.Count].Item; for (var column = 0; column < _columns.Count; column++) if (item.Cells[column].Selectable && item.Cells[column].Display.StartsWith(_search, StringComparison.OrdinalIgnoreCase)) { SetSelected(item, column); if (!IsDisposed) { EnsureCursorIsVisible(); AcceptEvent(); } return; } }
    }
    /// <inheritdoc />
    protected override string OnGetTooltip(Vector2 atPosition)
    {
        var header = HeaderAt(atPosition); if (header >= 0) return _columns[header].Tooltip; var item = GetItemAtPosition(atPosition); var column = GetColumnAtPosition(atPosition); if (item == null || column < 0) return base.OnGetTooltip(atPosition); var cell = item.Cells[column]; foreach (var button in cell.Buttons) if (LocalRect(button.Rect).HasPoint(atPosition)) return button.Tooltip; return cell.Tooltip.Length > 0 ? cell.Tooltip : _autoTooltip ? cell.Display : "";
    }
}
