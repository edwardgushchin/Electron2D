namespace Electron2D;

public partial class Tree
{
    private int _hoverColumn = -1, _hoverHeader = -1;
    private void ClipRect(Rect2 rect, Color color, bool outline = false) { rect = rect.Intersection(_body); if (rect.HasArea() && color.A > 0) DrawRect(rect, color, !outline); }
    private Rect2 FoldRect(Row row)
    { var width = GetThemeConstant("item_margin"); return LocalRect(new(new(row.Depth * width, row.Y), new(width, row.Height))); }
    private void DrawIcon(Texture? texture, Rect2 rect, Color color, Rect2? region = null)
    {
        if (texture is not { IsDisposed: false } || !rect.HasArea()) return; var clipped = rect.Intersection(_body); if (!clipped.HasArea()) return; var source = region is { } area && area.HasArea() ? area : new Rect2(Vector2.Zero, texture.GetSize()); var uv = new Rect2(source.Position + (clipped.Position - rect.Position) / rect.Size * source.Size, clipped.Size / rect.Size * source.Size); DrawTextureRectRegion(texture, clipped, uv, color);
    }
    private void DrawTree()
    {
        EnsureTreeLayout(); if (_drawing) throw new InvalidOperationException("Tree custom drawing cannot reenter."); _drawing = true;
        try
        {
            var bounds = new Rect2(Vector2.Zero, Size); GetThemeStyleBox("panel")?.Draw(this, bounds); if (HasFocus()) GetThemeStyleBox("focus")?.Draw(this, bounds);
            if (_titlesVisible) for (var i = 0; i < _columns.Count; i++)
                {
                    var column = _columns[i]; var rect = LocalRect(new(new(column.X, -_titleHeight), new(column.Width, _titleHeight))); rect.Position = new(rect.Position.X, _body.Position.Y - _titleHeight);
                    GetThemeStyleBox(_pressedHeader == i ? "title_button_pressed" : _hoverHeader == i ? "title_button_hover" : "title_button_normal")?.Draw(this, rect); var baseline = rect.Position + new Vector2(GetThemeConstant("h_separation"), (_titleHeight - column.Layout.Size.Y) / 2 + column.Layout.FirstAscent); var outline = GetThemeConstant("outline_size"); if (outline > 0) column.Layout.Draw(this, baseline, GetThemeColor("font_outline_color"), outline, outlinePass: true, clipRect: rect); column.Layout.Draw(this, baseline, GetThemeColor("title_button_color"), clipRect: rect);
                }
            foreach (var row in _rows)
            {
                var rowRect = LocalRect(new(new(0, row.Y), new(_contentWidth, row.Height))); if (!rowRect.Intersects(_body)) continue;
                if (!_hideFolding && !row.Item.DisableFolding && row.Item.Children.Count > 0) { var fold = FoldRect(row); var icon = GetThemeIcon(row.Item.Collapsed ? IsLayoutRTL() ? "arrow_collapsed_mirrored" : "arrow_collapsed" : "arrow"); var size = LiveSize(icon); DrawIcon(icon, new(fold.Position + (fold.Size - size) / 2, size), Colors.White); }
                if (GetThemeConstant("draw_guides") != 0) { var y = rowRect.End.Y - .5f; ClipRect(new(new(_body.Position.X, y), new(_body.Size.X, 1)), GetThemeColor("guide_color")); }
                if (GetThemeConstant("draw_relationship_lines") != 0 && row.Depth > 0) { var x = FoldRect(row).Position.X; ClipRect(new(new(x, rowRect.Position.Y), new(Math.Max(1, GetThemeConstant("relationship_line_width")), rowRect.Size.Y)), GetThemeColor("relationship_line_color")); DrawRelationshipHighlight(row, rowRect, x); }
                for (var column = 0; column < _columns.Count; column++)
                {
                    var cell = row.Item.Cells[column]; if (cell.Merged) continue; var rect = LocalRect(cell.Rect); var clip = rect.Intersection(_body); if (!clip.HasArea()) continue; var hovered = _hovered == row.Item && (_selectMode == SelectMode.Row || column == _hoverColumn);
                    if (cell.Selected) GetThemeStyleBox(HasFocus() ? "selected_focus" : "selected")?.Draw(this, clip);
                    if (hovered) GetThemeStyleBox(cell.Selected ? HasFocus() ? "hovered_selected_focus" : "hovered_selected" : cell.Selectable ? "hovered" : "hovered_dimmed")?.Draw(this, clip);
                    if (_selected == row.Item && _selectedColumn == column) GetThemeStyleBox(HasFocus() ? "cursor" : "cursor_unfocused")?.Draw(this, clip);
                    if (cell.HasBackground) ClipRect(rect, cell.Background, cell.BackgroundOutline); cell.Style?.Draw(this, clip);
                    var color = cell.HasColor ? cell.Color : GetThemeColor(!cell.Selectable ? hovered ? "font_hovered_dimmed_color" : "font_disabled_color" : cell.Selected ? hovered ? "font_hovered_selected_color" : "font_selected_color" : hovered ? "font_hovered_color" : "font_color");
                    if (cell.Mode == TreeItem.TreeCellMode.Custom && cell.CustomButton) GetThemeStyleBox(_pressedItem == row.Item && _pressedColumn == column ? "custom_button_pressed" : hovered ? "custom_button_hover" : "custom_button")?.Draw(this, clip);
                    if (cell.Mode == TreeItem.TreeCellMode.Check)
                    { var icon = GetThemeIcon(cell.Indeterminate ? cell.Editable ? "indeterminate" : "indeterminate_disabled" : cell.Checked ? cell.Editable ? "checked" : "checked_disabled" : cell.Editable ? "unchecked" : "unchecked_disabled"); var size = LiveSize(icon); var mark = LocalRect(new(new(cell.IconRect.Position.X - size.X - GetThemeConstant("check_h_separation"), row.Y + (row.Height - size.Y) / 2), size)); DrawIcon(icon, mark, Colors.White); }
                    if (cell.Icon is { IsDisposed: false })
                    { var iconRect = LocalRect(cell.IconRect); DrawIcon(cell.Icon, iconRect, cell.IconModulate, cell.IconRegion); if (cell.Overlay is { IsDisposed: false }) { var size = LiveSize(cell.Overlay); DrawIcon(cell.Overlay, new(iconRect.End - size, size), cell.IconModulate); } }
                    if (cell.Mode != TreeItem.TreeCellMode.Icon)
                    {
                        if (cell.Mode == TreeItem.TreeCellMode.Custom && cell.CustomButton && hovered) color = GetThemeColor("custom_button_font_highlight");
                        var text = LocalRect(cell.TextRect); var baseline = text.Position + new Vector2(0, Math.Max(0, (row.Height - cell.Layout.Size.Y) / 2) + cell.Layout.FirstAscent); var outline = GetThemeConstant("outline_size"); if (outline > 0) cell.Layout.Draw(this, baseline, GetThemeColor("font_outline_color"), outline, outlinePass: true, clipRect: _columns[column].Clip ? text.Intersection(_body) : _body); cell.Layout.Draw(this, baseline, color, clipRect: _columns[column].Clip ? text.Intersection(_body) : _body);
                    }
                    if (cell.Mode == TreeItem.TreeCellMode.Range || cell.Mode == TreeItem.TreeCellMode.Custom && cell.Editable && !cell.CustomButton)
                    { var icon = GetThemeIcon(cell.Mode == TreeItem.TreeCellMode.Custom || cell.Text.Length > 0 ? "select_arrow" : "updown"); var size = LiveSize(icon); DrawIcon(icon, new(new(IsLayoutRTL() ? rect.Position.X : rect.End.X - size.X, rect.Position.Y + (rect.Size.Y - size.Y) / 2), size), Colors.White); }
                    for (var buttonIndex = 0; buttonIndex < cell.Buttons.Count; buttonIndex++)
                    { var button = cell.Buttons[buttonIndex]; var buttonRect = LocalRect(button.Rect); if (!button.Disabled && buttonRect.HasPoint(_mouse)) GetThemeStyleBox(_pressedItem == row.Item && _pressedColumn == column && _pressedButton == buttonIndex ? "button_pressed" : "button_hover")?.Draw(this, buttonRect.Intersection(_body)); DrawIcon(button.Texture, buttonRect, button.Color * new Color(1, 1, 1, button.Disabled ? .5f : 1)); }
                    if (cell.Mode == TreeItem.TreeCellMode.Custom && cell.CustomDraw != null) { cell.CustomDraw(row.Item, rect); if (IsDisposed) return; }
                }
            }
            DrawDropFeedback(); DrawScrollHints();
        }
        finally { _drawing = false; }
    }
    private void DrawRelationshipHighlight(Row row, Rect2 rect, float x)
    {
        if (_selected == null) return; var ancestor = false; for (var item = _selected.ParentItem; item != null; item = item.ParentItem) if (item == row.Item) { ancestor = true; break; }
        var descendant = false; for (var item = row.Item.ParentItem; item != null; item = item.ParentItem) if (item == _selected) { descendant = true; break; }
        if (ancestor || descendant) { var width = GetThemeConstant(ancestor ? "parent_hl_line_width" : "children_hl_line_width"); var margin = ancestor ? GetThemeConstant("parent_hl_line_margin") : 0; ClipRect(new(new(x + margin, rect.Position.Y), new(Math.Max(1, width), rect.Size.Y)), GetThemeColor(ancestor ? "parent_hl_line_color" : "children_hl_line_color")); }
    }
    private void DrawScrollHints()
    {
        if (_hintMode == VerticalScrollHintMode.Disabled) return; var icon = GetThemeIcon("scroll_hint"); var size = LiveSize(icon); if (size.Y <= 0) return; var color = GetThemeColor("scroll_hint_color");
        if (_vBar.Value > 0 && _hintMode is VerticalScrollHintMode.Both or VerticalScrollHintMode.Top) DrawTextureRect(icon!, new(_body.Position, new(_body.Size.X, size.Y)), _tileHint, color);
        if (_vBar.Value < _vBar.MaxValue - _vBar.Page && _hintMode is VerticalScrollHintMode.Both or VerticalScrollHintMode.Bottom) DrawTextureRect(icon!, new(new(_body.Position.X, _body.End.Y), new(_body.Size.X, -size.Y)), _tileHint, color);
    }
    private void DrawDropFeedback()
    {
        if (GetViewport()?.IsGUIDragging() != true || _dropMode == TreeDropModeFlags.Disabled) return; var row = RowAt(_mouse); if (row < 0) return; var section = GetDropSectionAtPosition(_mouse); var rect = GetItemAreaRect(_rows[row].Item);
        if (section == 0) ClipRect(rect, GetThemeColor("drop_on_item_color"), true); else if (section is -1 or 1) ClipRect(new(new(rect.Position.X, section < 0 ? rect.Position.Y : rect.End.Y - 1), new(rect.Size.X, 1)), GetThemeColor("drop_position_color"));
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { base.OnNotification(what); DrawTree(); return; }
        base.OnNotification(what); if (IsDisposed) return;
        switch (what)
        {
            case NotificationEnterTree: InvalidateTree(); SetInternalProcessing(true, false); break;
            case NotificationExitTree: CancelEditor(); ReleaseResources(); _unfoldItem = null; _pressedItem = null; SetInternalProcessing(false, false); break;
            case NotificationResized: case NotificationThemeChanged: case NotificationTranslationChanged: case NotificationLayoutDirectionChanged: InvalidateTree(); break;
            case NotificationFocusEnter: case NotificationFocusExit: QueueRedraw(); break;
            case NotificationInternalProcess:
                if (Interlocked.Exchange(ref _resourcePending, 0) != 0) InvalidateTree();
                if (GetViewport()?.IsGUIDragging() == true) { if (GetWindow() is { } window && window.GetWindowID() != DisplayServer.InvalidWindowId) _mouse = GetLocalMousePosition(); var item = GetItemAtPosition(_mouse); if (_unfoldItem != item) { _unfoldItem = item; _unfoldTime = 0; } if (_dragUnfold && item is { Collapsed: true } && item.IsAcceptingChildren() && GetDropSectionAtPosition(_mouse) == 0) { _unfoldTime += ProcessDeltaTime; if (_unfoldTime * 1000 >= GetThemeConstant("dragging_unfold_wait_msec")) item.Collapsed = false; } var border = GetThemeConstant("scroll_border"); var speed = GetThemeConstant("scroll_speed") * ProcessDeltaTime; if (_mouse.Y < _body.Position.Y + border) _vBar.Value -= speed; else if (_mouse.Y > _body.End.Y - border) _vBar.Value += speed; QueueRedraw(); } else { _unfoldItem = null; _unfoldTime = 0; }
                break;
        }
    }
}
