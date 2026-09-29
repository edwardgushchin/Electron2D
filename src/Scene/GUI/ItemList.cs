namespace Electron2D;

/// <summary>Displays an ordered, scrollable collection of selectable text and icon items.</summary>
/// <remarks>The two owned bars are internal scene children. Item metadata uses typed generic access; it is runtime
/// state and does not create an untyped public value boundary.</remarks>
public partial class ItemList : Control
{
    /// <summary>Places an item's icon above or beside its text.</summary>
    public enum IconMode
    {
        /// <summary>Draws the icon above the item text.</summary>
        Top = 0,
        /// <summary>Draws the icon beside the item text.</summary>
        Left = 1
    }

    /// <summary>Controls whether one, multiple or toggled items may be selected.</summary>
    public enum SelectMode
    {
        /// <summary>Only one item can be selected.</summary>
        Single = 0,
        /// <summary>Multiple items can be selected.</summary>
        Multi = 1,
        /// <summary>Activating an item toggles its selected state.</summary>
        Toggle = 2
    }

    /// <summary>Selects eligible directional hints for the list's vertical scroll range.</summary>
    public enum ScrollHintMode
    {
        /// <summary>Hides directional hints.</summary>
        Disabled = 0,
        /// <summary>Allows hints at both reachable vertical edges.</summary>
        Both = 1,
        /// <summary>Allows the top hint only.</summary>
        Top = 2,
        /// <summary>Allows the bottom hint only.</summary>
        Bottom = 3
    }

    private sealed class Item
    {
        internal string Text = string.Empty;
        internal Texture? Icon;
        internal bool Selectable = true, Selected, Disabled;
        internal string Language = string.Empty, Tooltip = string.Empty;
        internal TextDirection Direction = TextDirection.Auto;
        internal NodeAutoTranslateMode AutoTranslate = NodeAutoTranslateMode.Inherit;
        internal bool TooltipEnabled = true;
        internal bool IconTransposed;
        internal Rect2 IconRegion;
        internal Color IconModulate = Colors.White, CustomFG, CustomBG;
        internal readonly TextLayout Layout = new();
        internal Rect2 Rect;
        internal object? Metadata;
    }

    private readonly List<Item> _items = [];
    private readonly Dictionary<Texture, int> _watchedIcons = [];
    private readonly VScrollBar _vBar = new() { Name = "_v_scroll" };
    private readonly HScrollBar _hBar = new() { Name = "_h_scroll" };
    private SelectMode _selectMode;
    private int _current = -1;
    private bool _layoutDirty = true;
    private IconMode _iconMode = IconMode.Left;
    private Vector2i _fixedIconSize;
    private float _iconScale = 1;

    /// <summary>Creates a focused, clipped list with two owned internal scroll bars.</summary>
    public ItemList()
    {
        FocusMode = FocusMode.All;
        ClipContents = true;
        AddChild(_vBar, InternalMode.Front);
        AddChild(_hBar, InternalMode.Front);
        _vBar.ValueChanged += _ => QueueRedraw();
        _hBar.ValueChanged += _ => QueueRedraw();
    }

    /// <summary>Gets the owned vertical bar.</summary>
    /// <returns>The live borrowed internal bar.</returns>
    public VScrollBar GetVScrollBar() { ThrowIfDisposed(); return _vBar; }

    /// <summary>Gets the owned horizontal bar.</summary>
    /// <returns>The live borrowed internal bar.</returns>
    public HScrollBar GetHScrollBar() { ThrowIfDisposed(); return _hBar; }

    /// <summary>Gets or resizes the number of entries, retaining existing entries in order.</summary>
    /// <value>Zero initially; newly added entries use empty text and remain selectable.</value>
    /// <exception cref="ArgumentOutOfRangeException">The requested count is negative.</exception>
    public int ItemCount
    {
        get { ThrowIfDisposed(); return _items.Count; }
        set
        {
            EnsureMutable();
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == _items.Count) return;
            if (value < _items.Count)
            {
                for (var index = value; index < _items.Count; index++) UnwatchIcon(_items[index].Icon);
                _items.RemoveRange(value, _items.Count - value);
                if (_current >= value) _current = -1;
            }
            else while (_items.Count < value) _items.Add(new Item());
            InvalidateListLayout();
        }
    }

    /// <summary>Gets or sets the selection policy.</summary>
    /// <value>Single initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    public SelectMode SelectionMode
    {
        get { ThrowIfDisposed(); return _selectMode; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _selectMode = value; QueueRedraw(); }
    }

    /// <summary>Gets or sets the current keyboard-navigation item.</summary>
    /// <value>Minus one initially. A Single-mode assignment selects the item.</value>
    /// <exception cref="ArgumentOutOfRangeException">The requested item is outside the list.</exception>
    public int Current
    {
        get { ThrowIfDisposed(); return _current; }
        set
        {
            EnsureMutable(); ValidateIndex(value);
            if (_current == value) return;
            if (_selectMode == SelectMode.Single) Select(value);
            else { _current = value; QueueRedraw(); }
        }
    }

    /// <summary>Adds an item and returns its zero-based index.</summary>
    /// <param name="text">The required source text.</param>
    /// <param name="icon">A borrowed optional icon.</param>
    /// <param name="selectable">Whether the item may be newly selected.</param>
    /// <returns>The added index.</returns>
    public int AddItem(string text, Texture? icon = null, bool selectable = true)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(text);
        if (icon?.IsDisposed == true) throw new ObjectDisposedException(nameof(icon));
        _items.Add(new Item { Text = text, Icon = icon, Selectable = selectable });
        WatchIcon(icon);
        InvalidateListLayout(); return _items.Count - 1;
    }

    /// <summary>Adds an icon-only item and returns its zero-based index.</summary>
    /// <param name="icon">The borrowed icon, or null for an empty icon cell.</param>
    /// <param name="selectable">Whether the item may be newly selected.</param>
    /// <returns>The added index.</returns>
    public int AddIconItem(Texture? icon, bool selectable = true) => AddItem(string.Empty, icon, selectable);

    /// <summary>Gets an item's source text.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>The stored text.</returns>
    public string GetItemText(int index) { ThrowIfDisposed(); return GetItem(index).Text; }

    /// <summary>Changes an item's source text.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="text">The required new text.</param>
    public void SetItemText(int index, string text)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(text); var item = GetItem(index, negative: true);
        if (item.Text == text) return; item.Text = text; InvalidateListLayout();
    }

    /// <summary>Returns whether an item may be newly selected.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>The stored selection policy.</returns>
    public bool IsItemSelectable(int index) { ThrowIfDisposed(); return GetItem(index).Selectable; }

    /// <summary>Changes whether an item may be newly selected.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="selectable">The new policy; existing selection is retained.</param>
    public void SetItemSelectable(int index, bool selectable)
    {
        EnsureMutable(); GetItem(index, negative: true).Selectable = selectable;
    }

    /// <summary>Returns whether an item is disabled for new selection and activation.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>The stored disabled flag.</returns>
    public bool IsItemDisabled(int index) { ThrowIfDisposed(); return GetItem(index).Disabled; }

    /// <summary>Changes an item's disabled flag without clearing an existing selection.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="disabled">The new disabled flag.</param>
    public void SetItemDisabled(int index, bool disabled)
    {
        EnsureMutable(); var item = GetItem(index, negative: true);
        if (item.Disabled == disabled) return; item.Disabled = disabled; QueueRedraw();
    }

    /// <summary>Selects an eligible item without emitting a GUI-selection event.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <param name="single">Clear other selections, or force single selection in Single mode.</param>
    public void Select(int index, bool single = true)
    {
        EnsureMutable(); var item = GetItem(index);
        if (single || _selectMode == SelectMode.Single)
        {
            if (!item.Selectable || item.Disabled) return;
            for (var i = 0; i < _items.Count; i++) _items[i].Selected = i == index;
            _current = index;
        }
        else if (item.Selectable && !item.Disabled) item.Selected = true;
        QueueRedraw();
    }

    /// <summary>Clears one item's selected state.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    public void Deselect(int index)
    {
        EnsureMutable(); GetItem(index).Selected = false;
        if (_selectMode == SelectMode.Single) _current = -1;
        QueueRedraw();
    }

    /// <summary>Clears every selected item and the current navigation index.</summary>
    public void DeselectAll()
    {
        EnsureMutable();
        if (_items.Count == 0) return;
        foreach (var item in _items) item.Selected = false;
        _current = -1; QueueRedraw();
    }

    /// <summary>Returns whether the requested item is selected.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>The stored selection flag.</returns>
    public bool IsSelected(int index) { ThrowIfDisposed(); return GetItem(index).Selected; }

    /// <summary>Returns the selected indices in display order.</summary>
    /// <returns>A caller-owned array; Single mode returns at most one item.</returns>
    public int[] GetSelectedItems()
    {
        ThrowIfDisposed(); var result = new List<int>();
        for (var i = 0; i < _items.Count; i++) if (_items[i].Selected) { result.Add(i); if (_selectMode == SelectMode.Single) break; }
        return [.. result];
    }

    /// <summary>Returns whether any item is selected.</summary>
    /// <returns>True when at least one entry has its selection flag set.</returns>
    public bool IsAnythingSelected() { ThrowIfDisposed(); return _items.Exists(static item => item.Selected); }

    /// <summary>Moves one entry to another index, preserving the entry's state.</summary>
    /// <param name="fromIndex">A valid source index.</param>
    /// <param name="toIndex">A valid destination index.</param>
    public void MoveItem(int fromIndex, int toIndex)
    {
        EnsureMutable(); ValidateIndex(fromIndex); ValidateIndex(toIndex);
        if (IsAnythingSelected() && GetSelectedItems()[0] == fromIndex) _current = toIndex;
        var item = _items[fromIndex]; _items.RemoveAt(fromIndex); _items.Insert(toIndex, item); InvalidateListLayout();
    }

    /// <summary>Removes one entry without disposing its borrowed resources.</summary>
    /// <param name="index">A valid zero-based index.</param>
    public void RemoveItem(int index)
    {
        EnsureMutable(); ValidateIndex(index); UnwatchIcon(_items[index].Icon); _items.RemoveAt(index);
        if (_current == index) _current = -1;
        else if (_current > index) _current--;
        InvalidateListLayout();
    }

    /// <summary>Removes all entries without disposing borrowed resources.</summary>
    public void Clear()
    {
        EnsureMutable();
        foreach (var icon in _watchedIcons.Keys) { icon.Changed -= IconChanged; icon.Disposed -= IconDisposed; }
        _watchedIcons.Clear(); _items.Clear(); _current = -1; InvalidateListLayout();
    }

    /// <summary>Sorts items by source text using ordinal string order, carrying their state with them.</summary>
    public void SortItemsByText()
    {
        EnsureMutable();
        _items.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Text, right.Text));
        if (_selectMode == SelectMode.Single)
            for (var index = 0; index < _items.Count; index++)
                if (_items[index].Selected) { Select(index); break; }
        InvalidateListLayout();
    }

    private Item GetItem(int index, bool negative = false)
    {
        if (negative && index < 0) index += _items.Count;
        ValidateIndex(index); return _items[index];
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)_items.Count) throw new ArgumentOutOfRangeException(nameof(index));
    }
}
