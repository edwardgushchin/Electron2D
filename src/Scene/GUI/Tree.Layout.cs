using System.Globalization;
namespace Electron2D;

public partial class Tree
{
    private Font? _font, _titleFont;
    private long _fontGeneration = -1, _titleGeneration = -1;
    private void ReleaseResources()
    {
        foreach (var resource in _resources) { resource.Changed -= _resourceChanged; resource.Disposed -= _resourceDisposed; }
        if (_texturesResident) foreach (var texture in _residentTextures) texture.ReleaseRendererCacheResidency(); _resources.Clear(); _resourceSet.Clear(); _residentTextures.Clear(); _texturesResident = false;
    }
    private void Watch(Resource? resource) { if (resource is not { IsDisposed: false } || !_resourceSet.Add(resource)) return; _resources.Add(resource); resource.Changed += _resourceChanged; resource.Disposed += _resourceDisposed; if (resource is Texture texture) { _residentTextures.Add(texture); if (IsInsideTree) texture.AcquireRendererCacheResidency(); } }
    private static Vector2 LiveSize(Texture? texture) => texture is { IsDisposed: false } ? texture.GetSize() : Vector2.Zero;
    private Vector2 IconSize(TreeItem.Cell cell)
    {
        var size = cell.IconRegion.HasArea() ? cell.IconRegion.Size : LiveSize(cell.Icon); var max = GetThemeConstant("icon_max_width"); if (cell.IconMaxWidth > 0) max = max > 0 ? Math.Min(max, cell.IconMaxWidth) : cell.IconMaxWidth; if (max > 0 && size.X > max) size *= max / size.X; return size;
    }
    private TextDirection ResolveDirection(TextDirection direction) => direction == TextDirection.Inherited ? IsLayoutRTL() ? TextDirection.RTL : TextDirection.LTR : direction;
    private string Locale(string language) => language.Length > 0 ? language : TranslationServer.Culture.Name;
    private string CellText(TreeItem item, TreeItem.Cell cell)
    {
        var text = cell.Mode == TreeItem.TreeCellMode.Range ? cell.Text.Length == 0 ? cell.Value.ToString("G", CultureInfo.InvariantCulture) + (cell.Suffix.Length > 0 ? " " + cell.Suffix : "") : ChoiceText(cell, cell.Value) : cell.Text;
        if (cell.Editable && cell.Mode == TreeItem.TreeCellMode.String) return text; return cell.AutoTranslate switch { NodeAutoTranslateMode.Disabled => text, NodeAutoTranslateMode.Always => Tr(text), _ => Atr(text) };
    }
    private static string ChoiceText(TreeItem.Cell cell, double value)
    { var choices = cell.Text.Split(','); for (var i = 0; i < choices.Length; i++) { var split = choices[i].LastIndexOf(':'); var id = split >= 0 && int.TryParse(choices[i].AsSpan(split + 1), out var parsed) ? parsed : i; if (id == value) return split >= 0 ? choices[i][..split] : choices[i]; } return ""; }
    private void Shape(TreeItem item, TreeItem.Cell cell, float width)
    {
        var font = cell.Font is { IsDisposed: false } ? cell.Font : _font!; var size = cell.FontSize > 0 ? cell.FontSize : GetThemeFontSize("font_size"); cell.ResolvedFont = font; cell.Size = size; cell.Display = CellText(item, cell); ParseStructuredText(cell.Parser, cell.ParserOptions, cell.Display, cell.Contexts);
        var breaks = TextLineBreakFlags.Mandatory | cell.Trim | (cell.Autowrap switch { TextAutowrapMode.Arbitrary => TextLineBreakFlags.GraphemeBound, TextAutowrapMode.Word => TextLineBreakFlags.WordBound, TextAutowrapMode.WordSmart => TextLineBreakFlags.WordBound | TextLineBreakFlags.Adaptive, _ => TextLineBreakFlags.None });
        cell.Layout.Build(font, new(cell.Display, size, width, cell.Alignment, -1, breaks, TextJustificationFlags.None, ResolveDirection(cell.Direction), TextOrientation.Horizontal, true), new(LineSpacing: 0, Language: Locale(cell.Language), Overrun: (int)cell.Overrun, BIDIOverride: cell.Contexts));
    }
    private void EnsureTreeLayout()
    {
        CheckTree(); var font = GetThemeFont("font") ?? throw new InvalidOperationException("Tree requires a live font."); var titleFont = GetThemeFont("title_button_font") ?? font;
        if (!_dirty && ReferenceEquals(font, _font) && ReferenceEquals(titleFont, _titleFont) && font.GetContentGeneration() == _fontGeneration && titleFont.GetContentGeneration() == _titleGeneration) return;
        if (_building) throw new InvalidOperationException("Tree layout cannot reenter."); _building = true;
        try
        {
            _font = font; _titleFont = titleFont; ReleaseResources(); Watch(font); Watch(titleFont); _all.Clear(); _rows.Clear();
            // ponytail: cold traversal uses linear sibling lookup; cache sibling indices if very wide authoring trees need faster rebuilds.
            for (var item = _root; item != null; item = item.WalkNext(false, true)) { item.LayoutDepth = item.ParentItem == null ? 0 : item.ParentItem.LayoutDepth + 1; item.LayoutVisible = item.Visible && (item.ParentItem?.LayoutVisible ?? true); item.LayoutUnfolded = item.ParentItem == null || item.ParentItem.LayoutUnfolded && !item.ParentItem.Collapsed; _all.Add(item); }
            var panel = GetThemeStyleBox("panel"); Watch(panel); var offset = panel?.GetOffset() ?? Vector2.Zero; var margins = panel?.GetMinimumSize() ?? Vector2.Zero;
            var hSep = GetThemeConstant("h_separation"); var indent = GetThemeConstant("item_margin"); var titleSize = GetThemeFontSize("title_button_font_size"); _titleHeight = _titlesVisible ? titleFont.GetHeight(titleSize) + (GetThemeStyleBox("title_button_normal")?.GetMinimumSize().Y ?? 0) : 0;
            for (var columnIndex = 0; columnIndex < _columns.Count; columnIndex++)
            {
                var column = _columns[columnIndex]; column.Layout.Build(titleFont, new(Atr(column.Title), titleSize, 0, column.Alignment, -1, 0, 0, ResolveDirection(column.Direction), TextOrientation.Horizontal), new(Language: Locale(column.Language)));
                var minimum = (float)column.Minimum; if (_titlesVisible) minimum = Math.Max(minimum, column.Layout.Size.X + hSep * 2);
                foreach (var item in _all)
                {
                    var cell = item.Cells[columnIndex]; Watch(cell.Font); Watch(cell.Style); Watch(cell.Icon); Watch(cell.Overlay); Shape(item, cell, 0); var icon = IconSize(cell); var buttons = 0f; foreach (var button in cell.Buttons) { Watch(button.Texture); buttons += LiveSize(button.Texture).X + GetThemeConstant("button_margin"); }
                    var depth = item.LayoutDepth; if (_hideRoot) depth = Math.Max(0, depth - 1);
                    var extra = buttons + icon.X + (icon.X > 0 ? GetThemeConstant("icon_h_separation") : 0) + hSep * 2;
                    if (columnIndex == 0) extra += depth * indent + (!_hideFolding && !item.DisableFolding ? indent : 0);
                    if (cell.Mode == TreeItem.TreeCellMode.Check) extra += LiveSize(GetThemeIcon("checked")).X + GetThemeConstant("check_h_separation");
                    if (cell.Mode == TreeItem.TreeCellMode.Range) extra += LiveSize(GetThemeIcon(cell.Text.Length > 0 ? "select_arrow" : "updown")).X;
                    minimum = Math.Max(minimum, extra + (column.Clip ? 0 : cell.Layout.UnwrappedWidth) + (cell.Style?.GetMinimumSize().X ?? 0));
                }
                column.Width = MathF.Ceiling(minimum);
            }
            var natural = 0f; var ratio = 0L; foreach (var column in _columns) { natural += column.Width; if (column.Expand) ratio += column.Ratio; }
            var vWidth = _scrollV ? _vBar.GetBoundMinimumSize().X + GetThemeConstant("scrollbar_h_separation") : 0; var hHeight = _scrollH ? _hBar.GetBoundMinimumSize().Y + GetThemeConstant("scrollbar_v_separation") : 0; var available = Math.Max(0, Size.X - margins.X - vWidth); var extraWidth = Math.Max(0, available - natural);
            var x = 0f; foreach (var column in _columns) { if (column.Expand && ratio > 0) column.Width += extraWidth * column.Ratio / ratio; column.X = x; x += column.Width; }
            _contentWidth = x;
            foreach (var column in _columns) column.Layout.Build(titleFont, new(Atr(column.Title), titleSize, Math.Max(1, column.Width - hSep * 2), column.Alignment, -1, 0, 0, ResolveDirection(column.Direction), TextOrientation.Horizontal), new(Language: Locale(column.Language)));
            _body = new(offset + new Vector2(IsLayoutRTL() ? vWidth : 0, _titleHeight), new(Math.Max(1, Size.X - margins.X - vWidth), Math.Max(1, Size.Y - margins.Y - _titleHeight - hHeight)));
            _contentHeight = 0;
            foreach (var item in _all)
            {
                if (!item.LayoutVisible || !item.LayoutUnfolded || _hideRoot && item == _root) continue; var depth = item.LayoutDepth; if (_hideRoot) depth = Math.Max(0, depth - 1);
                var height = Math.Max(item.CustomMinimumHeight, font.GetHeight(GetThemeFontSize("font_size")) + GetThemeConstant("v_separation"));
                for (var i = 0; i < _columns.Count; i++)
                {
                    var cell = item.Cells[i]; var column = _columns[i]; var span = 0; var cellWidth = column.Width; if (cell.ExpandRight) for (var next = i + 1; next < _columns.Count && !item.Cells[next].Editable && item.Cells[next].Mode == TreeItem.TreeCellMode.String && item.Cells[next].Display.Length == 0 && item.Cells[next].Icon == null; next++) { cellWidth += _columns[next].Width; span++; }
                    var icon = IconSize(cell); var buttons = 0f; foreach (var button in cell.Buttons) buttons += LiveSize(button.Texture).X + GetThemeConstant("button_margin"); var left = hSep + GetThemeConstant("inner_item_margin_left") + (i == 0 ? depth * indent + (!_hideFolding && !item.DisableFolding ? indent : 0) : 0);
                    var mark = cell.Mode == TreeItem.TreeCellMode.Check ? LiveSize(GetThemeIcon("checked")).X + GetThemeConstant("check_h_separation") : 0; var rangeMark = cell.Mode == TreeItem.TreeCellMode.Range ? LiveSize(GetThemeIcon(cell.Text.Length > 0 ? "select_arrow" : "updown")).X : cell.Mode == TreeItem.TreeCellMode.Custom && cell.Editable && !cell.CustomButton ? LiveSize(GetThemeIcon("select_arrow")).X : 0;
                    var style = cell.Style?.GetMinimumSize() ?? Vector2.Zero; var contentWidth = cellWidth - left - hSep - GetThemeConstant("inner_item_margin_right") - buttons - icon.X - (icon.X > 0 ? GetThemeConstant("icon_h_separation") : 0) - mark - rangeMark - style.X;
                    Shape(item, cell, Math.Max(1, contentWidth)); height = Math.Max(height, Math.Max(cell.Layout.Size.Y + style.Y + GetThemeConstant("v_separation"), icon.Y)); foreach (var button in cell.Buttons) height = Math.Max(height, LiveSize(button.Texture).Y + GetThemeConstant("v_separation"));
                    cell.Rect = new(new(column.X, _contentHeight), new(cellWidth, height)); cell.IconRect = new(new(column.X + left + mark, _contentHeight), icon); cell.TextRect = new(new(column.X + left + mark + icon.X + (icon.X > 0 ? GetThemeConstant("icon_h_separation") : 0), _contentHeight), new(Math.Max(1, contentWidth), height));
                    var buttonX = column.X + cellWidth - hSep; for (var b = cell.Buttons.Count - 1; b >= 0; b--) { var button = cell.Buttons[b]; var size = LiveSize(button.Texture); buttonX -= size.X; button.Rect = new(new(buttonX, _contentHeight), size); buttonX -= GetThemeConstant("button_margin"); }
                    cell.Merged = false; for (var merged = 1; merged <= span; merged++) { item.Cells[i + merged].Merged = true; item.Cells[i + merged].Rect = default; }
                    i += span;
                }
                height = MathF.Ceiling(height) + GetThemeConstant("inner_item_margin_top") + GetThemeConstant("inner_item_margin_bottom");
                foreach (var cell in item.Cells) { if (cell.Merged) continue; cell.Rect = new(cell.Rect.Position, new(cell.Rect.Size.X, height)); cell.TextRect = new(cell.TextRect.Position, new(cell.TextRect.Size.X, height)); cell.IconRect = cell.Mode == TreeItem.TreeCellMode.Icon ? new(cell.Rect.Position + (cell.Rect.Size - cell.IconRect.Size) / 2, cell.IconRect.Size) : new(cell.IconRect.Position + new Vector2(0, (height - cell.IconRect.Size.Y) / 2), cell.IconRect.Size); foreach (var button in cell.Buttons) button.Rect = new(button.Rect.Position + new Vector2(0, (height - button.Rect.Size.Y) / 2), button.Rect.Size); }
                _rows.Add(new(item, depth, _contentHeight, height)); _contentHeight += height;
            }
            var barTop = GetThemeConstant("scrollbar_margin_top"); var barBottom = GetThemeConstant("scrollbar_margin_bottom"); var barLeft = GetThemeConstant("scrollbar_margin_left"); var barRight = GetThemeConstant("scrollbar_margin_right"); var right = barRight < 0 ? panel?.GetContentMargin(Side.Right) ?? 0 : barRight; var leftMargin = barLeft < 0 ? offset.X : barLeft; var topMargin = barTop < 0 ? _body.Position.Y : barTop; var bottom = barBottom < 0 ? panel?.GetContentMargin(Side.Bottom) ?? 0 : barBottom;
            _vBar.Position = new(IsLayoutRTL() ? leftMargin : Size.X - _vBar.GetBoundMinimumSize().X - right, topMargin); _vBar.Size = new(_vBar.GetBoundMinimumSize().X, Math.Max(1, Size.Y - topMargin - bottom - hHeight)); _vBar.MinValue = 0; _vBar.Step = 1; _vBar.MaxValue = Math.Max(_contentHeight, _body.Size.Y); _vBar.Page = _body.Size.Y; _vBar.Visible = _scrollV && _contentHeight > _body.Size.Y;
            _hBar.Position = new(leftMargin + (IsLayoutRTL() ? vWidth : 0), Size.Y - hHeight - bottom); _hBar.Size = new(Math.Max(1, Size.X - leftMargin - right - vWidth), hHeight); _hBar.MinValue = 0; _hBar.Step = 1; _hBar.MaxValue = Math.Max(_contentWidth, _body.Size.X); _hBar.Page = _body.Size.X; _hBar.Visible = _scrollH && _contentWidth > _body.Size.X;
            foreach (var name in new[] { "arrow", "arrow_collapsed", "arrow_collapsed_mirrored", "checked", "checked_disabled", "indeterminate", "indeterminate_disabled", "unchecked", "unchecked_disabled", "select_arrow", "updown", "scroll_hint" }) Watch(GetThemeIcon(name));
            _texturesResident = IsInsideTree; _fontGeneration = font.GetContentGeneration(); _titleGeneration = titleFont.GetContentGeneration(); _dirty = false;
        }
        finally { _building = false; }
    }
    private Rect2 LocalRect(Rect2 content)
    {
        var x = content.Position.X - (float)_hBar.Value; var position = _body.Position + new Vector2(IsLayoutRTL() ? _body.Size.X - x - content.Size.X : x, content.Position.Y - (float)_vBar.Value); return new(position, content.Size);
    }
    private int RowAt(Vector2 position) { EnsureTreeLayout(); if (!_body.HasPoint(position)) return -1; var y = position.Y - _body.Position.Y + (float)_vBar.Value; for (var i = 0; i < _rows.Count; i++) if (y >= _rows[i].Y && y < _rows[i].Y + _rows[i].Height) return i; return -1; }
    /// <summary>Returns a presented item at a local control coordinate.</summary><param name="position">Finite local point.</param><returns>Borrowed row or null.</returns>
    public TreeItem? GetItemAtPosition(Vector2 position) { CheckTree(); if (!position.IsFinite()) throw new ArgumentException("Point must be finite.", nameof(position)); var row = RowAt(position); return row < 0 ? null : _rows[row].Item; }
    /// <summary>Returns the cell column under a presented row.</summary><param name="position">Local point.</param><returns>Column or -1.</returns>
    public int GetColumnAtPosition(Vector2 position) { var item = GetItemAtPosition(position); if (item == null) return -1; for (var i = 0; i < item.Cells.Count; i++) if (LocalRect(item.Cells[i].Rect).HasPoint(position)) return i; return -1; }
    private int ButtonIndexAt(TreeItem item, int column, Vector2 position) { for (var index = 0; index < item.Cells[column].Buttons.Count; index++) if (LocalRect(item.Cells[column].Buttons[index].Rect).HasPoint(position)) return index; return -1; }
    /// <summary>Returns a cell-button signal ID under a local point.</summary><param name="position">Local point.</param><returns>Signal ID or -1.</returns>
    public int GetButtonIDAtPosition(Vector2 position) { var item = GetItemAtPosition(position); var column = GetColumnAtPosition(position); if (item == null || column < 0) return -1; foreach (var button in item.Cells[column].Buttons) if (LocalRect(button.Rect).HasPoint(position)) return button.ID; return -1; }
    /// <summary>Returns a presented row, cell or button rectangle in local control coordinates.</summary><param name="item">Owned item.</param><param name="column">Column or -1 for complete row.</param><param name="buttonIndex">Button index or -1 for the cell.</param><returns>Local rectangle, empty for folded/hidden rows.</returns>
    public Rect2 GetItemAreaRect(TreeItem item, int column = -1, int buttonIndex = -1) { RequireItem(item); EnsureTreeLayout(); foreach (var row in _rows) if (row.Item == item) { if (column == -1) return new(new(0, LocalRect(new(new(0, row.Y), new(_contentWidth, row.Height))).Position.Y), new(Size.X, row.Height)); var cell = item.At(column); if (cell.Merged) return default; if (buttonIndex == -1) return LocalRect(cell.Rect); if ((uint)buttonIndex >= cell.Buttons.Count) throw new ArgumentOutOfRangeException(nameof(buttonIndex)); return LocalRect(cell.Buttons[buttonIndex].Rect); } return default; }
    /// <summary>Returns the current pixel scroll offsets.</summary><returns>Horizontal/vertical offsets.</returns>
    public Vector2 GetScroll() { CheckTree(); EnsureTreeLayout(); return new((float)_hBar.Value, (float)_vBar.Value); }
    /// <summary>Unfolds ancestors and scrolls to a row.</summary><param name="item">Owned item.</param><param name="centerOnItem">Centers the row vertically.</param>
    public void ScrollToItem(TreeItem item, bool centerOnItem = false) { EnsureTreeMutable(); RequireItem(item); item.UncollapseTree(); EnsureTreeLayout(); foreach (var row in _rows) if (row.Item == item) { if (centerOnItem) _vBar.Value = row.Y + row.Height / 2 - _body.Size.Y / 2; else if (row.Y < _vBar.Value) _vBar.Value = row.Y; else if (row.Y + row.Height > _vBar.Value + _body.Size.Y) _vBar.Value = row.Y + row.Height - _body.Size.Y; break; } }
    /// <summary>Scrolls the current selection cursor into view on both axes.</summary>
    public void EnsureCursorIsVisible() { EnsureTreeMutable(); if (_selected == null) return; ScrollToItem(_selected); var rect = _selected.At(_selectedColumn).Rect; if (rect.Position.X < _hBar.Value) _hBar.Value = rect.Position.X; else if (rect.End.X > _hBar.Value + _body.Size.X) _hBar.Value = rect.End.X - _body.Size.X; }
    /// <summary>Returns the typed borrowed canvas used by cell custom drawing.</summary><returns>This control; Draw operations require its active custom-draw scope.</returns>
    public CanvasItem GetCustomDrawingCanvasItem() { CheckTree(); return this; }
    /// <summary>Returns the most recent custom-editor rectangle in global canvas coordinates.</summary><returns>Custom popup area.</returns>
    public Rect2 GetCustomPopupRect() { CheckTree(); return _customPopupRect; }
    /// <summary>Returns enabled row drop presentation at a local point.</summary><param name="position">Local point.</param><returns>-1 above, 0 on, 1 below, or -100 when unavailable.</returns>
    public int GetDropSectionAtPosition(Vector2 position) { var index = RowAt(position); if (index < 0 || _dropMode == TreeDropModeFlags.Disabled) return -100; var row = _rows[index]; var fraction = (position.Y - LocalRect(new(new(0, row.Y), new(_contentWidth, row.Height))).Position.Y) / row.Height; if ((_dropMode & TreeDropModeFlags.OnItem) == 0) return fraction < .5 ? -1 : 1; if ((_dropMode & TreeDropModeFlags.Inbetween) == 0) return row.Item.IsAcceptingChildren() ? 0 : -100; return fraction < .25 ? -1 : fraction > .75 ? 1 : row.Item.IsAcceptingChildren() ? 0 : -100; }
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() { if (_vBar == null) return Vector2.Zero; var panel = GetThemeStyleBox("panel"); return (panel?.GetMinimumSize() ?? Vector2.Zero) + new Vector2(1, 1); }
}
