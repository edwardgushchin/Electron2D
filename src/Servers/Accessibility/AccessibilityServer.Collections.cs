namespace Electron2D;

public sealed partial class AccessibilityServer
{
    internal sealed class CollectionState
    {
        internal int? Count, Index, Level, Rows, Columns, Row, Column, CellRow, CellColumn;
        internal int RowSpan = 1, ColumnSpan = 1;
        internal bool HasSpan;
        internal bool? Selected, Expanded, Vertical;
    }
    private enum CollectionNumber { Count, Index, Level, Rows, Columns, Row, Column }
    private enum CollectionFlag { Selected, Expanded, Vertical }
    private static ref int? CollectionNumberField(CollectionState state, CollectionNumber field)
    {
        switch (field)
        {
            case CollectionNumber.Count: return ref state.Count;
            case CollectionNumber.Index: return ref state.Index;
            case CollectionNumber.Level: return ref state.Level;
            case CollectionNumber.Rows: return ref state.Rows;
            case CollectionNumber.Columns: return ref state.Columns;
            case CollectionNumber.Row: return ref state.Row;
            default: return ref state.Column;
        }
    }
    private static void SetCollectionNumber(RID id, int value, CollectionNumber field)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        var e = Require().Get(id); var state = e.Collection ??= new(); ref var current = ref CollectionNumberField(state, field);
        if (current == value) return; current = value; Runtime._native!.CollectionChanged(e);
    }
    private static void SetCollectionFlag(RID id, bool value, CollectionFlag field)
    {
        var e = Require().Get(id); var state = e.Collection ??= new();
        if (field == CollectionFlag.Selected) { if (state.Selected == value) return; state.Selected = value; }
        else if (field == CollectionFlag.Expanded) { if (state.Expanded == value) return; state.Expanded = value; }
        else { if (state.Vertical == value) return; state.Vertical = value; }
        Runtime._native!.CollectionChanged(e);
    }
    /// <summary>Publishes the number of items in a collection.</summary>
    /// <param name="id">Live element.</param><param name="size">Nonnegative value.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is negative.</exception>
    public static void UpdateSetListItemCount(RID id, int size) => SetCollectionNumber(id, size, CollectionNumber.Count);
    /// <summary>Publishes the zero-based position of an item in its collection.</summary>
    /// <param name="id">Live element.</param><param name="index">Nonnegative value.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is negative.</exception>
    public static void UpdateSetListItemIndex(RID id, int index) => SetCollectionNumber(id, index, CollectionNumber.Index);
    /// <summary>Publishes the nonnegative hierarchical depth of an item.</summary>
    /// <param name="id">Live element.</param><param name="level">Nonnegative value.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is negative.</exception>
    public static void UpdateSetListItemLevel(RID id, int level) => SetCollectionNumber(id, level, CollectionNumber.Level);
    /// <summary>Publishes the number of logical table rows.</summary>
    /// <param name="id">Live element.</param><param name="count">Nonnegative value.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is negative.</exception>
    public static void UpdateSetTableRowCount(RID id, int count) => SetCollectionNumber(id, count, CollectionNumber.Rows);
    /// <summary>Publishes the number of logical table columns.</summary>
    /// <param name="id">Live element.</param><param name="count">Nonnegative value.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is negative.</exception>
    public static void UpdateSetTableColumnCount(RID id, int count) => SetCollectionNumber(id, count, CollectionNumber.Columns);
    /// <summary>Publishes the zero-based position of a table row or row header.</summary>
    /// <param name="id">Live element.</param><param name="index">Nonnegative value.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is negative.</exception>
    public static void UpdateSetTableRowIndex(RID id, int index) => SetCollectionNumber(id, index, CollectionNumber.Row);
    /// <summary>Publishes the zero-based position of a table column or column header.</summary>
    /// <param name="id">Live element.</param><param name="index">Nonnegative value.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Value is negative.</exception>
    public static void UpdateSetTableColumnIndex(RID id, int index) => SetCollectionNumber(id, index, CollectionNumber.Column);
    /// <summary>Publishes the selected state of an item.</summary>
    /// <param name="id">Live element.</param><param name="selected">Published state.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetListItemSelected(RID id, bool selected) => SetCollectionFlag(id, selected, CollectionFlag.Selected);
    /// <summary>Publishes the expanded state of a branch.</summary>
    /// <param name="id">Live element.</param><param name="expanded">Published state.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetListItemExpanded(RID id, bool expanded) => SetCollectionFlag(id, expanded, CollectionFlag.Expanded);
    /// <summary>Publishes vertical or horizontal collection orientation.</summary>
    /// <param name="id">Live element.</param><param name="vertical">Published state.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetListOrientation(RID id, bool vertical) => SetCollectionFlag(id, vertical, CollectionFlag.Vertical);
    /// <summary>Publishes a cell's zero-based logical position in its containing table.</summary>
    /// <param name="id">Live cell.</param><param name="rowIndex">Nonnegative row.</param><param name="columnIndex">Nonnegative column.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An index is negative.</exception>
    public static void UpdateSetTableCellPosition(RID id, int rowIndex, int columnIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex); ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);
        var e = Require().Get(id); var state = e.Collection ??= new();
        if (state.CellRow == rowIndex && state.CellColumn == columnIndex) return;
        state.CellRow = rowIndex; state.CellColumn = columnIndex; Runtime._native!.CollectionChanged(e);
    }
    /// <summary>Publishes the positive row and column spans of a cell.</summary>
    /// <param name="id">Live cell.</param><param name="rowSpan">Positive number of rows.</param><param name="columnSpan">Positive number of columns.</param>
    /// <remarks>Publishes producer state without changing the collection or invoking an action. Requires the display owner.</remarks>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent, foreign or retired.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A span is zero or negative.</exception>
    public static void UpdateSetTableCellSpan(RID id, int rowSpan, int columnSpan)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rowSpan, 1); ArgumentOutOfRangeException.ThrowIfLessThan(columnSpan, 1);
        var e = Require().Get(id); var state = e.Collection ??= new();
        if (state.HasSpan && state.RowSpan == rowSpan && state.ColumnSpan == columnSpan) return;
        state.HasSpan = true; state.RowSpan = rowSpan; state.ColumnSpan = columnSpan; Runtime._native!.CollectionChanged(e);
    }
}
