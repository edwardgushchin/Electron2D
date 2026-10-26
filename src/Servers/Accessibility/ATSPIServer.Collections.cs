using static Electron2D.GIONative;
using Element = Electron2D.AccessibilityServer.Element;
using State = Electron2D.AccessibilityServer.CollectionState;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using Flag = Electron2D.AccessibilityServer.AccessibilityFlags;
namespace Electron2D;

internal sealed unsafe partial class ATSPIServer
{
    private const string SelectionInterface = "org.a11y.atspi.Selection", TableInterface = "org.a11y.atspi.Table", TableCellInterface = "org.a11y.atspi.TableCell";
    private void EnableSelection(Element e)
    {
        if (e.SelectionRegistered) return; EnableInterface(e, SelectionInterface); e.SelectionRegistered = true; InterfacesChanged(e);
    }
    internal void CollectionChanged(Element e)
    {
        var c = e.Collection!;
        if (c.Count is not null) EnableSelection(e);
        if (c.Selected is not null && e.Parent is { } owner) EnableSelection(owner);
        if ((c.Rows is not null || c.Columns is not null) && !e.TableRegistered) { EnableInterface(e, TableInterface); e.TableRegistered = true; EnableSelection(e); InterfacesChanged(e); if (e.Role == Role.Tree) RoleChanged(e); }
        if ((c.CellRow is not null || c.HasSpan) && !e.TableCellRegistered) { EnableInterface(e, TableCellInterface); e.TableCellRegistered = true; InterfacesChanged(e); }
        if (c.Selected is { } selected) { StateChanged(e, "selected", selected); StateChanged(e, "selectable", e.CollectionSelectable); }
        StateChanged(e, "expandable", c.Expanded is not null); StateChanged(e, "expanded", c.Expanded == true);
        if (c.Vertical is { } vertical) { StateChanged(e, "vertical", vertical); StateChanged(e, "horizontal", !vertical); }
        Emit(e.Path, "org.a11y.atspi.Event.Object", "AttributesChanged", Event("", 0, 0, I(0), e));
        var table = e.TableRegistered ? e : TableOf(e);
        if (table is not null) Emit(table.Path, "org.a11y.atspi.Event.Object", "ModelChanged", Event("", 0, 0, I(0), table));
        if (e.Parent is { SelectionRegistered: true } parent) Emit(parent.Path, "org.a11y.atspi.Event.Object", "SelectionChanged", Event("", 0, 0, I(0), parent));
    }
    private static void AddCollectionAttributes(nint builder, Element e)
    {
        if (e.Collection is not { } c) return;
        void Add(string name, long value) => g_variant_builder_add_value(builder, g_variant_new_dict_entry(S(name), S(value.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        if ((c.Count ?? (c.Index is not null ? e.Parent?.Collection?.Count : null)) is { } count) Add("setsize", count);
        if (c.Index is { } index) Add("posinset", (long)index + 1);
        if (c.Level is { } level) Add("level", (long)level + 1);
        if (c.Row is { } row) Add("rowindex", row);
        if (c.Column is { } column) Add("colindex", column);
    }
    private static Element SelectionChild(Element e, int index)
    {
        if ((uint)index >= (uint)e.Children.Count) throw new InvalidRequestException("The child index is outside the collection.");
        return e.Children[index];
    }
    private static List<Element> SelectedChildren(Element e)
    {
        var result = new List<Element>(); foreach (var child in e.Children) if (!child.Retired && child.Collection?.Selected == true) result.Add(child); return result;
    }
    private static bool SelectChild(Element owner, Element child, bool selected, bool direct = true)
    {
        if (!CanInvoke(owner) || owner.Flag(Flag.ReadOnly) || !CanInvoke(child) || (direct ? child.Parent != owner : TableOf(child) != owner) || child.Collection?.Selected is null || selected && !child.CollectionSelectable) return false;
        try { return child.CollectionSource is not null ? AccessibilityServer.SelectCollectionItem(child, selected) : child.Collection.Selected == selected || Invoke(child, selected ? AccessibilityServer.AccessibilityAction.Focus : AccessibilityServer.AccessibilityAction.Blur); } catch (Exception error) when (error is not CallbackException) { throw new CallbackException(error); }
    }
    private static bool SelectAllOrClear(Element e, bool all)
    {
        if (!CanInvoke(e) || e.Flag(Flag.ReadOnly)) return false;
        try
        {
            if (e.Source is ItemList or Tree or TabBar) return AccessibilityServer.SelectCollection(e, all);
            var targets = e.Children.Where(child => child.Collection?.Selected is not null && (all ? child.CollectionSelectable && CanInvoke(child) : child.Collection.Selected == true)).ToArray();
            if (all && targets.Length > 1 && !e.Flag(Flag.Multiselectable)) return false;
            foreach (var child in targets) { if (e.Retired) return true; if (!SelectChild(e, child, all)) return false; }
            return true;
        }
        catch (Exception error) when (error is not CallbackException) { throw new CallbackException(error); }
    }
    private nint SelectionCall(Element e, string method, nint args)
    {
        if (method == "SelectAll") return Tuple([B(SelectAllOrClear(e, true))]);
        if (method == "ClearSelection") return Tuple([B(SelectAllOrClear(e, false))]);
        var index = IntArg(args, 0);
        if (method is "GetSelectedChild" or "DeselectSelectedChild")
        {
            var selected = SelectedChildren(e); if ((uint)index >= (uint)selected.Count) throw new InvalidRequestException("The selected-child index is outside the current selection.");
            return method == "GetSelectedChild" ? Tuple([ObjectRef(selected[index])]) : Tuple([B(SelectChild(e, selected[index], false))]);
        }
        var child = SelectionChild(e, index);
        return method switch
        {
            "IsChildSelected" => Tuple([B(child.Collection?.Selected == true)]),
            "SelectChild" => Tuple([B(SelectChild(e, child, true))]),
            "DeselectChild" => Tuple([B(SelectChild(e, child, false))]),
            _ => throw new InvalidRequestException("Unknown selection method."),
        };
    }
    private static Element? TableOf(Element cell)
    { for (var p = cell.Parent; p is not null; p = p.Parent) if (p.TableRegistered) return p; return null; }
    private static List<Element> TableCells(Element e)
    {
        var cells = new List<Element>(); var todo = new Stack<Element>();
        todo.Push(e);
        while (todo.Count != 0)
        {
            var child = todo.Pop(); if (child.Retired || child != e && child.TableRegistered) continue;
            if (child != e && child.Collection is { CellRow: not null, CellColumn: not null }) cells.Add(child);
            for (var index = child.Children.Count - 1; index >= 0; index--) todo.Push(child.Children[index]);
        }
        return cells;
    }
    private static bool InTable(Element e, int row, int column) => (uint)row < (uint)(e.Collection?.Rows ?? 0) && (uint)column < (uint)(e.Collection?.Columns ?? 0);
    private static bool Covers(State c, int row, int column) => c.CellRow is { } r && c.CellColumn is { } col && row >= r && column >= col && (long)row < (long)r + c.RowSpan && (long)column < (long)col + c.ColumnSpan;
    private static Element? CellAt(Element e, int row, int column)
    {
        if (!InTable(e, row, column)) return null;
        foreach (var cell in TableCells(e)) if (Covers(cell.Collection!, row, column)) return cell;
        return null;
    }
    private static Element? CellAtChildIndex(Element e, int index) => (uint)index < (uint)e.Children.Count && e.Children[index].Collection?.CellRow is not null ? e.Children[index] : null;
    private static bool AxisSelected(Element e, List<Element> cells, int index, bool row)
    {
        var count = row ? e.Collection?.Rows ?? 0 : e.Collection?.Columns ?? 0;
        var other = row ? e.Collection?.Columns ?? 0 : e.Collection?.Rows ?? 0;
        if ((uint)index >= (uint)count || other == 0) return false;
        var intervals = new List<(int Start, long End)>(); var any = false;
        foreach (var cell in cells)
        {
            var c = cell.Collection!; var from = row ? c.CellRow!.Value : c.CellColumn!.Value; var span = row ? c.RowSpan : c.ColumnSpan;
            if (index < from || (long)index >= (long)from + span) continue;
            any |= c.Selected == true;
            if (c.Selected != true && cell.CollectionSelectable && c.Selected is not null) continue;
            var start = row ? c.CellColumn!.Value : c.CellRow!.Value; var extent = row ? c.ColumnSpan : c.RowSpan;
            if (start < other) intervals.Add((start, Math.Min(other, (long)start + extent)));
        }
        if (!any) return false;
        intervals.Sort(static (a, b) => a.Start.CompareTo(b.Start)); long end = 0;
        foreach (var interval in intervals) { if (interval.Start > end) return false; end = Math.Max(end, interval.End); }
        return end >= other;
    }
    private static List<int> SelectedAxes(Element e, bool row)
    {
        var cells = TableCells(e); var candidates = new SortedSet<int>(); var limit = row ? e.Collection?.Rows ?? 0 : e.Collection?.Columns ?? 0;
        foreach (var cell in cells)
        {
            var c = cell.Collection!; if (c.Selected != true) continue;
            var from = row ? c.CellRow!.Value : c.CellColumn!.Value; var span = row ? c.RowSpan : c.ColumnSpan;
            var end = Math.Min(limit, (long)from + span);
            if (end - from > 1_000_000) throw new InvalidRequestException("The selected-index response exceeds native message capacity.");
            for (var i = (long)from; i < end; i++) { candidates.Add((int)i); if (candidates.Count > 1_000_000) throw new InvalidRequestException("The selected-index response exceeds native message capacity."); }
        }
        var result = new List<int>(); foreach (var index in candidates) if (AxisSelected(e, cells, index, row)) result.Add(index); return result;
    }
    private nint IndexArray(List<int> values)
    {
        var builder = Builder("ai"); try { foreach (var value in values) g_variant_builder_add_value(builder, I(value)); return g_variant_builder_end(builder); } finally { g_variant_builder_unref(builder); }
    }
    private static Element? HeaderAt(Element e, int index, bool row)
    {
        foreach (var child in e.Children)
            if (child.Role == (row ? Role.RowHeader : Role.ColumnHeader) && (row ? child.Collection?.Row : child.Collection?.Column) == index) return child;
        return null;
    }
    private nint HeaderCells(Element cell, bool row)
    {
        var builder = Builder("a(so)");
        try
        {
            if (TableOf(cell) is { } table && cell.Collection is { } c)
            {
                var start = row ? c.CellRow : c.CellColumn; var span = row ? c.RowSpan : c.ColumnSpan;
                foreach (var header in table.Children)
                {
                    var index = row ? header.Collection?.Row : header.Collection?.Column;
                    if (header.Role == (row ? Role.RowHeader : Role.ColumnHeader) && index is { } at && start is { } from && at >= from && (long)at < (long)from + span) g_variant_builder_add_value(builder, ObjectRef(header));
                }
            }
            return g_variant_builder_end(builder);
        }
        finally { g_variant_builder_unref(builder); }
    }
    private nint CollectionProperty(Element e, string iface, string name)
    {
        var c = e.Collection;
        if (iface == SelectionInterface && name == "NSelectedChildren") return I(SelectedChildren(e).Count);
        if (iface == TableCellInterface) return name switch
        {
            "ColumnSpan" => I(c?.ColumnSpan ?? 1),
            "RowSpan" => I(c?.RowSpan ?? 1),
            "Position" => Tuple([I(c?.CellRow ?? -1), I(c?.CellColumn ?? -1)]),
            "Table" => ObjectRef(TableOf(e)),
            _ => throw new InvalidRequestException("Unknown table-cell property."),
        };
        if (iface == TableInterface) return name switch
        {
            "NRows" => I(c?.Rows ?? 0),
            "NColumns" => I(c?.Columns ?? 0),
            "Caption" or "Summary" => ObjectRef(null),
            "NSelectedRows" => I(SelectedAxes(e, true).Count),
            "NSelectedColumns" => I(SelectedAxes(e, false).Count),
            _ => throw new InvalidRequestException("Unknown table property."),
        };
        throw new InvalidRequestException("Unknown collection property.");
    }
    private nint TableCellCall(Element e, string method) => method switch
    {
        "GetRowColumnSpan" => Tuple([B(TableOf(e) is not null && e.Collection?.CellRow is not null), I(e.Collection?.CellRow ?? -1), I(e.Collection?.CellColumn ?? -1), I(e.Collection?.RowSpan ?? 1), I(e.Collection?.ColumnSpan ?? 1)]),
        "GetColumnHeaderCells" => Tuple([HeaderCells(e, false)]),
        "GetRowHeaderCells" => Tuple([HeaderCells(e, true)]),
        _ => throw new InvalidRequestException("Unknown table-cell method."),
    };
    private static bool SelectAxis(Element e, int index, bool row, bool selected)
    {
        var count = row ? e.Collection?.Rows ?? 0 : e.Collection?.Columns ?? 0;
        if ((uint)index >= (uint)count || !CanInvoke(e) || e.Flag(Flag.ReadOnly)) return false;
        try
        {
            if (e.Source is Tree) return AccessibilityServer.SelectCollectionAxis(e, index, row, selected);
            var targets = TableCells(e).Where(cell => cell.Collection?.Selected is not null && (selected ? cell.CollectionSelectable : cell.Collection!.Selected == true) && (row ? index >= cell.Collection!.CellRow && (long)index < (long)cell.Collection.CellRow + cell.Collection.RowSpan : index >= cell.Collection!.CellColumn && (long)index < (long)cell.Collection.CellColumn + cell.Collection.ColumnSpan)).ToArray();
            if (selected && targets.Length > 1 && !e.Flag(Flag.Multiselectable) && targets.Any(cell => cell.Collection!.Selected != true)) return false;
            foreach (var child in targets) { if (e.Retired) return true; if (!SelectChild(e, child, selected, false)) return false; }
            return true;
        }
        catch (Exception error) when (error is not CallbackException) { throw new CallbackException(error); }
    }
    private nint TableCall(Element e, string method, nint args)
    {
        if (method == "GetSelectedRows") return Tuple([IndexArray(SelectedAxes(e, true))]);
        if (method == "GetSelectedColumns") return Tuple([IndexArray(SelectedAxes(e, false))]);
        var index = IntArg(args, 0);
        if (method == "GetAccessibleAt") return Tuple([ObjectRef(CellAt(e, index, IntArg(args, 1)))]);
        if (method == "GetIndexAt") { var cell = CellAt(e, index, IntArg(args, 1)); return Tuple([I(cell is null ? -1 : e.Children.IndexOf(cell))]); }
        if (method is "GetRowAtIndex" or "GetColumnAtIndex") { var c = CellAtChildIndex(e, index)?.Collection; return Tuple([I((method == "GetRowAtIndex" ? c?.CellRow : c?.CellColumn) ?? -1)]); }
        if (method is "GetRowExtentAt" or "GetColumnExtentAt") { var c = CellAt(e, index, IntArg(args, 1))?.Collection; return Tuple([I(c is null ? 0 : method == "GetRowExtentAt" ? c.RowSpan : c.ColumnSpan)]); }
        if (method is "GetRowDescription" or "GetColumnDescription") { var row = method == "GetRowDescription"; return Tuple([S(HeaderAt(e, index, row)?.Name ?? (row ? CellAt(e, index, 0)?.Name : null) ?? "")]); }
        if (method is "GetRowHeader" or "GetColumnHeader") return Tuple([ObjectRef(HeaderAt(e, index, method == "GetRowHeader"))]);
        if (method == "IsSelected") return Tuple([B(CellAt(e, index, IntArg(args, 1))?.Collection?.Selected == true)]);
        if (method is "IsRowSelected" or "IsColumnSelected") return Tuple([B(AxisSelected(e, TableCells(e), index, method == "IsRowSelected"))]);
        if (method is "AddRowSelection" or "RemoveRowSelection" or "AddColumnSelection" or "RemoveColumnSelection") return Tuple([B(SelectAxis(e, index, method.Contains("Row", StringComparison.Ordinal), method.StartsWith("Add", StringComparison.Ordinal)))]);
        if (method == "GetRowColumnExtentsAtIndex") { var c = CellAtChildIndex(e, index)?.Collection; return Tuple([B(c?.CellRow is not null), I(c?.CellRow ?? -1), I(c?.CellColumn ?? -1), I(c?.RowSpan ?? 1), I(c?.ColumnSpan ?? 1), B(c?.Selected == true)]); }
        throw new InvalidRequestException("Unknown table method.");
    }
}
