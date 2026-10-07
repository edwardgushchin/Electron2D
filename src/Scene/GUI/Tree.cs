namespace Electron2D;

/// <summary>Displays and edits a retained hierarchy of typed multi-column TreeItems.</summary>
/// <remarks>TreeItems are owned non-node objects. Cell editors, scrollbars and popups are internal scene children;
/// prepared rows/layouts are reused by canvas recording and input. Structural authoring and first shaping are cold.</remarks>
public partial class Tree : Control
{
    /// <summary>Selects a single cell, complete row or multiple cells.</summary>
    public enum SelectMode
    {
        /// <summary>Selects a single cell.</summary>
        Single = 0,
        /// <summary>Selects an entire row.</summary>
        Row = 1,
        /// <summary>Allows multiple selected cells.</summary>
        Multi = 2
    }
    internal sealed class Column
    {
        internal string Title = "", Language = "", Tooltip = "";
        internal HorizontalAlignment Alignment = HorizontalAlignment.Center;
        internal TextDirection Direction = TextDirection.Inherited;
        internal int Minimum, Ratio = 1;
        internal bool Expand = true, Clip;
        internal float Width, X;
        internal readonly TextLayout Layout = new();
    }
    internal readonly record struct Row(TreeItem Item, int Depth, float Y, float Height);
    private readonly List<Column> _columns = [new()];
    private readonly List<Row> _rows = [];
    private readonly List<List<(TreeItem Item, int Column)>> _selectionChanges = [];
    private int _selectionDepth;
    private readonly List<TreeItem> _all = [];
    private readonly List<Resource> _resources = [];
    private readonly HashSet<Resource> _resourceSet = [];
    private readonly HashSet<Texture> _residentTextures = [];
    private readonly OwnedVScroll _vBar;
    private readonly OwnedHScroll _hBar;
    private readonly Action<Resource> _resourceChanged;
    private readonly Action<ElectronObject> _resourceDisposed;
    private TreeItem? _root, _selected, _edited, _hovered, _pressedItem, _unfoldItem;
    private int _selectedColumn = -1, _editedColumn = -1, _pressedButton = -1, _pressedButtonID = -1, _pressedColumn = -1, _resourcePending;
    private bool _dirty = true, _building, _drawing, _disposing, _texturesResident;
    private Rect2 _body, _customPopupRect;
    private float _contentWidth, _contentHeight, _titleHeight;
    private double _unfoldTime;
    private Vector2 _mouse;
    private string _search = "";
    private double _searchTime;
    private bool _allowReselect, _allowRMBSelect, _allowSearch = true, _autoTooltip = true, _titlesVisible, _dragUnfold = true, _recursiveFold = true, _hideFolding, _hideRoot, _scrollH = true, _scrollV = true, _tileHint;
    private VerticalScrollHintMode _hintMode;
    private SelectMode _selectMode;
    private TreeDropModeFlags _dropMode;
    /// <summary>Creates an empty clipped, focusable tree with required scrollbars and cell editors.</summary>
    public Tree()
    {
        FocusMode = FocusMode.All; ClipContents = true; _resourceChanged = ResourceChanged; _resourceDisposed = ResourceDisposed;
        _vBar = new(this) { Name = "_v_scroll" }; _hBar = new(this) { Name = "_h_scroll" }; AddChild(_vBar, InternalMode.Front); AddChild(_hBar, InternalMode.Front); _vBar.ValueChanged += _ => QueueRedraw(); _hBar.ValueChanged += _ => QueueRedraw(); CreateEditors();
    }
    internal void CheckTree() { if (!_disposing) ThrowIfDisposed(); base.Tree?.EnsureOwnerThread(); }
    internal void EnsureTreeMutable() { CheckTree(); if (_drawing || _building) throw new InvalidOperationException("An active Tree cannot mutate its hierarchy or cell layout."); if (!_disposing) EnsureMutable(); }
    internal void InvalidateTree() { if (IsDisposed || _disposing) return; _dirty = true; QueueRedraw(); UpdateMinimumSize(); }
    private void ResourceChanged(Resource _) { if (!IsDisposed) Interlocked.Exchange(ref _resourcePending, 1); }
    private void ResourceDisposed(ElectronObject resource) => ResourceChanged((Resource)resource);
    internal void ItemDetached(TreeItem item)
    {
        if (_root == item) _root = null; if (_selected == item) { _selected = null; _selectedColumn = -1; }
        if (_edited == item) { CancelEditor(); _edited = null; _editedColumn = -1; }
        if (_hovered == item) _hovered = null; if (_pressedItem == item) { _pressedItem = null; _pressedButton = -1; }
        if (_unfoldItem == item) _unfoldItem = null; foreach (var cell in item.Cells) cell.Selected = false;
    }
    /// <summary>Creates the root of an empty control or a child of the supplied/default root.</summary><param name="parent">Parent owned by this control, or null.</param><param name="index">Clamped insertion index; negative appends.</param><returns>New owned item.</returns>
    public TreeItem CreateItem(TreeItem? parent = null, int index = -1) { EnsureTreeMutable(); if (parent != null) { RequireItem(parent); return parent.CreateChild(index); } if (_root != null) return _root.CreateChild(index); _root = new(this, _columns.Count) { IsRoot = true }; InvalidateTree(); return _root; }
    /// <summary>Returns the stable borrowed root or null.</summary><returns>Root item.</returns>
    public TreeItem? GetRoot() { CheckTree(); return _root is { IsDisposed: false } ? _root : null; }
    private void RequireItem(TreeItem item) { ArgumentNullException.ThrowIfNull(item); item.CheckItem(); if (item.Owner != this) throw new ArgumentException("The item belongs to another control.", nameof(item)); }
    /// <summary>Disposes the entire owned item hierarchy and cancels editing.</summary>
    public void Clear() { EnsureTreeMutable(); CancelEditor(); var root = _root; _root = null; try { root?.Dispose(); } finally { _selected = _edited = _hovered = _pressedItem = null; _selectedColumn = -1; _editedColumn = _pressedButton = -1; InvalidateTree(); } }
    /// <summary>Returns the selection cursor item or null.</summary><returns>Borrowed cursor item.</returns>
    public TreeItem? GetSelected() { CheckTree(); return _selected is { IsDisposed: false } ? _selected : null; }
    /// <summary>Returns the cursor column.</summary><returns>Column index.</returns>
    public int GetSelectedColumn() { CheckTree(); return _selectedColumn; }
    /// <summary>Returns the last edited item or null.</summary><returns>Borrowed edited item.</returns>
    public TreeItem? GetEdited() { CheckTree(); return _edited is { IsDisposed: false } ? _edited : null; }
    /// <summary>Returns the last edited column.</summary><returns>Column index or -1.</returns>
    public int GetEditedColumn() { CheckTree(); return _editedColumn; }
    /// <summary>Returns the currently pressed cell-button index.</summary><returns>Index or -1.</returns>
    public int GetPressedButton() { CheckTree(); return _pressedButton; }
    /// <summary>Returns the next selected row in depth-first hierarchy order, regardless of folding.</summary><param name="from">Previous item or null to begin.</param><returns>Borrowed selected item or null.</returns>
    public TreeItem? GetNextSelected(TreeItem? from) { CheckTree(); if (from != null) RequireItem(from); for (var item = from == null ? _root : from.WalkNext(false, true); item != null; item = item.WalkNext(false, true)) foreach (var cell in item.Cells) if (cell.Selected) return item; return null; }
    /// <summary>Selects only the given cell or row under the current policy.</summary><param name="item">Owned item.</param><param name="column">Existing column.</param>
    public void SetSelected(TreeItem item, int column)
    {
        EnsureTreeMutable(); RequireItem(item); var cell = item.At(column); if (!cell.Selectable) return; CancelEditor();
        if (_selectMode != SelectMode.Multi) { SelectItemCell(item, column, true, true); return; }
        var notify = !cell.Selected || _allowReselect; var cursorChanged = _selected != item || _selectedColumn != column;
        var changes = BeginSelectionChanges();
        try
        {
            for (var other = _root; other != null; other = other.WalkNext(false, true))
                for (var c = 0; c < other.Cells.Count; c++)
                    if (other == item && c == column) { other.Cells[c].Selected = true; if (notify || cursorChanged) changes.Add((other, c)); }
                    else if (other.Cells[c].Selected) { other.Cells[c].Selected = false; if (MultiSelected != null) changes.Add((other, c)); }
            _selected = item; _selectedColumn = column; QueueRedraw(); List<Exception>? errors = null;
            foreach (var change in changes)
            {
                if (IsDisposed || change.Item.IsDisposed || change.Item.Owner != this) continue;
                var target = change.Item == item && change.Column == column;
                if (target) try { CellSelected?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
                if ((!target || notify) && !IsDisposed && !change.Item.IsDisposed && change.Item.Owner == this)
                    try { MultiSelected?.Invoke(change.Item, change.Column, target); } catch (Exception error) { CollectException(ref errors, error); }
            }
            ThrowCollected("Tree selection callbacks failed.", errors);
        }
        finally { changes.Clear(); _selectionDepth--; }
    }
    private List<(TreeItem Item, int Column)> BeginSelectionChanges()
    {
        var depth = _selectionDepth++;
        if (_selectionChanges.Count <= depth) _selectionChanges.Add([]);
        var changes = _selectionChanges[depth]; changes.Clear(); return changes;
    }
    /// <summary>Deselects every row/cell; multiple mode also clears the cursor.</summary>
    public void DeselectAll() { EnsureTreeMutable(); for (var item = _root; item != null; item = item.WalkNext(false, true)) foreach (var cell in item.Cells) cell.Selected = false; _selected = null; _selectedColumn = -1; QueueRedraw(); }
    internal void SelectItemCell(TreeItem item, int column, bool selected, bool cursor, bool notifyMulti = true)
    {
        EnsureTreeMutable(); RequireItem(item); var cell = item.At(column); if (selected && !cell.Selectable) return; var changed = cell.Selected != selected; if (!changed && !_allowReselect) { if (selected && cursor && (_selected != item || _selectedColumn != column)) { _selected = item; _selectedColumn = column; QueueRedraw(); if (_selectMode == SelectMode.Multi && notifyMulti) CellSelected?.Invoke(); } return; }
        if (selected && _selectMode != SelectMode.Multi) DeselectAll(); if (_selectMode == SelectMode.Row) foreach (var c in item.Cells) c.Selected = selected && c.Selectable; else cell.Selected = selected;
        if (cursor && selected) { _selected = item; _selectedColumn = column; } else if (!selected) { if (_selectMode != SelectMode.Multi && _selected == item && (_selectMode == SelectMode.Row || _selectedColumn == column)) { _selected = null; _selectedColumn = -1; } }
        QueueRedraw(); List<Exception>? errors = null;
        try { if (cursor && selected && _selectMode != SelectMode.Row && (_selectMode != SelectMode.Multi || notifyMulti)) CellSelected?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) { if (_selectMode == SelectMode.Multi) { if (notifyMulti) MultiSelected?.Invoke(item, column, selected); } else if (selected) ItemSelected?.Invoke(); } } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Tree selection callbacks failed.", errors);
    }
    internal void PublishCollapsed(TreeItem item) { if (!IsDisposed) ItemCollapsed?.Invoke(item); }
    internal void PublishCheck(TreeItem item, int column) { if (!IsDisposed) CheckPropagatedToItem?.Invoke(item, column); }
    /// <summary>Occurs when a selection cursor cell changes.</summary>
    public event Action? CellSelected;
    /// <summary>Occurs after a single selection.</summary>
    public event Action? ItemSelected;
    /// <summary>Reports a committed multiple cell selection change.</summary>
    public event Action<TreeItem, int, bool>? MultiSelected;
    /// <summary>Reports a changed folded branch.</summary>
    public event Action<TreeItem>? ItemCollapsed;
    /// <summary>Reports committed checkbox propagation.</summary>
    public event Action<TreeItem, int>? CheckPropagatedToItem;
    /// <summary>Occurs after user cell editing; GetEdited identifies its target.</summary>
    public event Action? ItemEdited;
    /// <summary>Occurs on item activation.</summary>
    public event Action? ItemActivated;
    /// <summary>Occurs on a double-clicked cell icon.</summary>
    public event Action? ItemIconDoubleClicked;
    /// <summary>Reports pointer selection.</summary>
    public event Action<Vector2, MouseButton>? ItemMouseSelected;
    /// <summary>Reports an activated cell button.</summary>
    public event Action<TreeItem, int, int, MouseButton>? ButtonClicked;
    /// <summary>Reports a clicked title.</summary>
    public event Action<int, MouseButton>? ColumnTitleClicked;
    /// <summary>Reports an empty-content click.</summary>
    public event Action<Vector2, MouseButton>? EmptyClicked;
    /// <summary>Occurs after clicking empty content in single/row mode.</summary>
    public event Action? NothingSelected;
    /// <summary>Reports custom-cell activation.</summary>
    public event Action<MouseButton>? CustomItemClicked;
    /// <summary>Requests the application's custom editor at GetCustomPopupRect.</summary>
    public event Action<bool>? CustomPopupEdited;
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Tree) ? CreateTree : base.CreateSceneInstanceFactory();
    private static Node CreateTree() => new Tree();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null; if (disposing) { _disposing = true; try { CancelEditor(); } catch (Exception error) { CollectException(ref errors, error); } try { _root?.Dispose(); } catch (Exception error) { CollectException(ref errors, error); } _root = null; ReleaseResources(); _rows.Clear(); _all.Clear(); CellSelected = ItemSelected = ItemEdited = ItemActivated = ItemIconDoubleClicked = NothingSelected = null; MultiSelected = null; ItemCollapsed = null; CheckPropagatedToItem = null; ItemMouseSelected = null; ButtonClicked = null; ColumnTitleClicked = null; EmptyClicked = null; CustomItemClicked = null; CustomPopupEdited = null; }
        try { base.Dispose(disposing); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Tree cleanup failed.", errors);
    }
    private void ValidateOwned() { if (!IsDisposed && !_disposing) throw new InvalidOperationException("Required tree controls belong to their owner."); }
    private sealed class OwnedVScroll(Tree owner) : VScrollBar { protected override void ValidateDisposal() { owner.ValidateOwned(); base.ValidateDisposal(); } }
    private sealed class OwnedHScroll(Tree owner) : HScrollBar { protected override void ValidateDisposal() { owner.ValidateOwned(); base.ValidateDisposal(); } }
}
/// <summary>Selects item and between-row drop presentation.</summary>
[Flags]
public enum TreeDropModeFlags
{
    /// <summary>Disables drop presentation.</summary>
    Disabled = 0,
    /// <summary>Allows dropping onto an item.</summary>
    OnItem = 1,
    /// <summary>Allows dropping above or below an item.</summary>
    Inbetween = 2
}
