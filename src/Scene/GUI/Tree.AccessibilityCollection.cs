using Element = Electron2D.AccessibilityServer.Element;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
namespace Electron2D;

public partial class Tree
{
    private long _accessibilityCollectionVersion;
    internal void AccessibilitySyncCollection(Element owner)
    {
        EnsureTreeLayout(); var version = _accessibilityCollectionVersion;
        if (!AccessibilityServer.BeginCollection(owner, version)) return;
        AccessibilityServer.CollectionList(owner, _rows.Count, true); AccessibilityServer.CollectionTable(owner, _rows.Count, _columns.Count);
        for (var column = 0; column < _columns.Count; column++)
        {
            var header = AccessibilityServer.CollectionElement(owner, _columns[column], Role.ColumnHeader, column, column);
            AccessibilityServer.CollectionIdentity(header, Atr(_columns[column].Title), !_titlesVisible, false, false);
            AccessibilityServer.CollectionColumnHeader(header, column);
        }
        Element? active = null; var row = 0;
        foreach (var item in _all)
        {
            var visible = item.LayoutVisible && item.LayoutUnfolded && !(_hideRoot && item == _root); Element? rowElement = null;
            for (var column = 0; column < item.Cells.Count; column++)
            {
                var cell = item.Cells[column]; var e = AccessibilityServer.CollectionElement(owner, cell, column == 0 ? Role.TreeItem : Role.Cell, row, column); e.CollectionTreeItem = item;
                var span = 1; if (!cell.Merged) while (column + span < item.Cells.Count && item.Cells[column + span].Merged) span++;
                AccessibilityServer.CollectionIdentity(e, visible ? cell.Display : cell.Text, !visible || cell.Merged, false, cell.Selectable && !cell.Merged);
                AccessibilityServer.CollectionEntry(e, row, Math.Max(0, item.LayoutDepth - (_hideRoot ? 1 : 0)), cell.Selected, column == 0 && item.Children.Count != 0 ? !item.Collapsed : null, visible && !cell.Merged ? row : -1, column, span);
                if (column == 0) { rowElement = e; AccessibilityServer.CollectionBranch(e, item); }
                if (visible && !cell.Merged && _selected == item && _selectedColumn == column) active = e;
            }
            if (rowElement is not null)
            {
                Element? parent = null; if (item.ParentItem is { Cells.Count: > 0 } p && !(_hideRoot && p == _root) && owner.SourceCollectionElements!.TryGetValue(p.Cells[0], out var parentElement)) parent = parentElement;
                AccessibilityServer.CollectionHierarchy(rowElement, parent);
            }
            if (visible) row++;
        }
        AccessibilityServer.EndCollection(owner, version, 0, active);
    }
    private bool AccessibilityCollectionCell(Element e, out TreeItem item, out TreeItem.Cell cell)
    {
        item = e.CollectionTreeItem!; cell = null!;
        if (item is null || item.IsDisposed || item.Owner != this || (uint)e.CollectionSourceColumn >= (uint)item.Cells.Count) return false;
        cell = item.Cells[e.CollectionSourceColumn]; return ReferenceEquals(cell, e.CollectionItem);
    }
    internal bool AccessibilityCollectionLive(Element e) => e.CollectionItem is Column column ? _columns.Contains(column) : AccessibilityCollectionCell(e, out _, out _);
    internal Rect2 AccessibilityCollectionRect(Element e)
    {
        EnsureTreeLayout();
        if (e.CollectionItem is Column header)
        {
            if (!_titlesVisible || !_columns.Contains(header)) return default;
            return AccessibilityCollectionHeaderView() with { Position = new Vector2(LocalRect(new(header.X, 0, header.Width, 1)).Position.X, _body.Position.Y - _titleHeight), Size = new(header.Width, _titleHeight) };
        }
        return AccessibilityCollectionCell(e, out var item, out var cell) && item.LayoutVisible && item.LayoutUnfolded && !(_hideRoot && item == _root) && !cell.Merged ? LocalRect(cell.Rect) : default;
    }
    internal Rect2 AccessibilityCollectionHeaderView() => new(_body.Position - new Vector2(0, _titleHeight), new(_body.Size.X, _titleHeight));
    internal bool AccessibilitySelectCollection(Element e, bool selected)
    {
        if (IsDisposed || !IsInsideTree || !AccessibilityCollectionCell(e, out var item, out var cell)) return false;
        if (!item.LayoutVisible || !item.LayoutUnfolded || cell.Merged || selected && !cell.Selectable) return false;
        if (selected) item.Select(e.CollectionSourceColumn); else item.Deselect(e.CollectionSourceColumn); return true;
    }
    internal bool AccessibilitySelectCollection(bool all)
    {
        if (!all) { DeselectAll(); return true; }
        EnsureTreeLayout(); var count = 0; foreach (var row in _rows) foreach (var cell in row.Item.Cells) if (cell.Selectable && !cell.Merged) count++;
        var selectableRows = 0; foreach (var row in _rows) if (row.Item.Cells.Any(cell => cell.Selectable && !cell.Merged)) selectableRows++;
        if (_selectMode == SelectMode.Single && count > 1 || _selectMode == SelectMode.Row && selectableRows > 1) return false;
        var cells = new List<(TreeItem Item, int Column)>(); foreach (var row in _rows) for (var col = 0; col < row.Item.Cells.Count; col++) if (row.Item.Cells[col].Selectable && !row.Item.Cells[col].Merged) cells.Add((row.Item, col));
        foreach (var target in cells) { if (IsDisposed || !IsInsideTree) break; if (!target.Item.IsDisposed && target.Item.Owner == this && (uint)target.Column < (uint)target.Item.Cells.Count) target.Item.Select(target.Column); }
        return true;
    }
    internal bool AccessibilitySelectCollectionAxis(int index, bool row, bool selected)
    {
        EnsureTreeLayout(); if (row && (uint)index >= (uint)_rows.Count || !row && (uint)index >= (uint)_columns.Count) return false;
        var cells = new List<(TreeItem Item, int Column)>();
        for (var rowIndex = 0; rowIndex < _rows.Count; rowIndex++)
        {
            if (row && rowIndex != index) continue; var item = _rows[rowIndex].Item;
            for (var col = 0; col < item.Cells.Count; col++)
            {
                var cell = item.Cells[col]; if (cell.Merged || (selected ? !cell.Selectable : !cell.Selected)) continue;
                var span = 1; while (col + span < item.Cells.Count && item.Cells[col + span].Merged) span++;
                if (row || index >= col && index < col + span) cells.Add((item, col));
            }
        }
        if (selected && (_selectMode == SelectMode.Single && cells.Count > 1 || _selectMode == SelectMode.Row && cells.Select(cell => cell.Item).Distinct().Take(2).Count() > 1)) return false;
        foreach (var target in cells)
        {
            if (IsDisposed || !IsInsideTree) break;
            if (target.Item.IsDisposed || target.Item.Owner != this || (uint)target.Column >= (uint)target.Item.Cells.Count) continue;
            if (selected) target.Item.Select(target.Column); else target.Item.Deselect(target.Column);
        }
        return true;
    }
}
