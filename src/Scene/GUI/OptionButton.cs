namespace Electron2D;

/// <summary>Displays one selected item and opens an owned themed dropdown menu.</summary>
/// <remarks>The internal popup owns item records and borrows icons/shortcuts. IDs are independent of indices;
/// metadata uses exact generic types and remains runtime-only. Programmatic selection emits no ItemSelected.
/// An attached embedded window host is required for dropdown presentation. Mutations follow scene ownership.</remarks>
public partial class OptionButton : Button
{
    private readonly PopupMenu _popup;
    private PopupMenu.Item? _selectedItem;
    private int _selected = -1, _queuedSelection = -2, _selectionRevision, _changing;
    private bool _initialized, _allowReselect, _fitLongest = true, _disableShortcuts, _disposing;
    /// <summary>Creates an empty toggle button with leading alignment, press-edge activation and an internal menu.</summary>
    public OptionButton() : this("") { }
    /// <summary>Creates an empty choice button with initial source text.</summary><param name="text">Nonnull text; later selection owns the caption.</param>
    public OptionButton(string text) : base(text)
    {
        ToggleMode = true; Alignment = HorizontalAlignment.Left; ActionMode = ButtonActionMode.ButtonPress; ShortcutInputEnabled = true;
        _popup = new PopupMenu { Name = "_option_popup", ShrinkWidth = false }; AddChild(_popup, InternalMode.Front);
        _popup.IndexPressed += SelectedByUser; _popup.IDFocused += id => ItemFocused?.Invoke(_popup.GetItemIndex(id)); _popup.PopupHide += PopupHidden; _popup.MenuChanged += MenuChanged;
    }
    /// <summary>Occurs after a user activation commits the selected index and caption.</summary>
    public event Action<int>? ItemSelected;
    /// <summary>Occurs after keyboard/controller navigation focuses a menu item, carrying its first matching ID index.</summary>
    public event Action<int>? ItemFocused;
    private void CheckOption() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    /// <summary>Gets or sets whether activating the current item reports selection again.</summary><value>False initially.</value>
    public bool AllowReselect { get { CheckOption(); return _allowReselect; } set { EnsureMutable(); _allowReselect = value; } }
    /// <summary>Gets or sets whether the longest item and popup width contribute to the button minimum.</summary><value>True initially.</value>
    public bool FitToLongestItem { get { CheckOption(); return _fitLongest; } set { EnsureMutable(); if (_fitLongest == value) return; _fitLongest = value; UpdateMinimumSize(); } }
    /// <summary>Gets or sets the selected index without a user-selection event; an initial out-of-range assignment queues.</summary><value>-1 initially. Existing disabled/separator records may be selected programmatically.</value>
    public int Selected { get { CheckOption(); return _selected; } set { EnsureMutable(); if (value >= ItemCount && !_initialized) { if (value >= 65536) throw new ArgumentOutOfRangeException(nameof(value)); _queuedSelection = value; return; } Select(value); } }
    /// <summary>Gets or sets the item count, retaining records and selected identity.</summary><value>Zero through 65536; new slots use radio decoration and automatic IDs.</value>
    public int ItemCount
    {
        get { CheckOption(); return _popup.ItemCount; }
        set
        {
            EnsureMutable(); if ((uint)value > 65536) throw new ArgumentOutOfRangeException(nameof(value)); var old = _popup.ItemCount; if (old == value) return;
            _changing++; List<Exception>? errors = null;
            try { try { _popup.ItemCount = value; } catch (Exception e) { CollectException(ref errors, e); } for (var i = old; i < _popup.ItemCount; i++) try { _popup.SetItemAsRadioCheckable(i, true); } catch (Exception e) { CollectException(ref errors, e); } }
            finally { _changing--; }
            Reconcile(); if (!_initialized) { _initialized = true; var queued = _queuedSelection; _queuedSelection = -2; if (queued >= -1) try { Select(queued); } catch (Exception e) { CollectException(ref errors, e); } }
            if (_selected < 0 && value < old && value > 0) Select(value - 1); UpdateMinimumSize(); NotifyPropertyListChanged(); ThrowCollected("Option item count callbacks failed.", errors);
        }
    }
    /// <summary>Gets or sets the owned menu's SearchBarEnabled policy.</summary><value>Uses the PopupMenu default and validation.</value>
    public bool SearchBarEnabled { get { CheckOption(); return _popup.SearchBarEnabled; } set { EnsureMutable(); _popup.SearchBarEnabled = value; UpdateMinimumSize(); } }
    /// <summary>Gets or sets the owned menu's SearchBarFuzzySearchEnabled policy.</summary><value>Uses the PopupMenu default and validation.</value>
    public bool SearchBarFuzzySearchEnabled { get { CheckOption(); return _popup.SearchBarFuzzySearchEnabled; } set { EnsureMutable(); _popup.SearchBarFuzzySearchEnabled = value; UpdateMinimumSize(); } }
    /// <summary>Gets or sets the owned menu's SearchBarFuzzySearchMaxMisses policy.</summary><value>Uses the PopupMenu default and validation.</value>
    public int SearchBarFuzzySearchMaxMisses { get { CheckOption(); return _popup.SearchBarFuzzySearchMaxMisses; } set { EnsureMutable(); _popup.SearchBarFuzzySearchMaxMisses = value; UpdateMinimumSize(); } }
    /// <summary>Gets or sets the owned menu's SearchBarMinItemCount policy.</summary><value>Uses the PopupMenu default and validation.</value>
    public int SearchBarMinItemCount { get { CheckOption(); return _popup.SearchBarMinItemCount; } set { EnsureMutable(); _popup.SearchBarMinItemCount = value; UpdateMinimumSize(); } }
    /// <summary>Gets the stable borrowed internal popup. Its lifetime remains owned by this button.</summary><returns>The owned menu.</returns>
    public PopupMenu GetPopup() { CheckOption(); return _popup; }
    /// <summary>Adds a radio item, selecting it if it is the first selectable record.</summary><param name="label">Nonnull source title.</param><param name="id">Signed ID; -1 derives from the new index.</param>
    public void AddItem(string label, int id = -1) { EnsureMutable(); var first = !HasSelectableItems(); var count = ItemCount; var revision = _selectionRevision; MutateItems(() => _popup.AddRadioCheckItem(label, id), () => { if (first && ItemCount > count && revision == _selectionRevision) Select(ItemCount - 1); }); }
    /// <summary>Adds a radio item with a borrowed icon.</summary><param name="icon">A live borrowed icon or null.</param><param name="label">Nonnull source title.</param><param name="id">Signed ID or -1 for automatic.</param>
    public void AddIconItem(Texture? icon, string label, int id = -1) { EnsureMutable(); var first = !HasSelectableItems(); var count = ItemCount; var revision = _selectionRevision; MutateItems(() => _popup.AddIconRadioCheckItem(icon, label, id), () => { if (first && ItemCount > count && revision == _selectionRevision) Select(ItemCount - 1); }); }
    /// <summary>Adds a nonselectable separator with an optional source title.</summary><param name="text">Nonnull source title.</param>
    public void AddSeparator(string text = "") { EnsureMutable(); MutateItems(() => _popup.AddSeparator(text)); }
    /// <summary>Selects an existing record or clears selection, without ItemSelected.</summary><param name="index">A nonnegative existing index or -1.</param>
    public void Select(int index) { EnsureMutable(); SelectCore(index, false); }
    private void SelectCore(int index, bool emit)
    {
        if (index < -1 || index >= _popup.ItemCount) throw new ArgumentOutOfRangeException(nameof(index)); var item = index < 0 ? null : _popup.BorrowMenuItem(index);
        if (item == _selectedItem && index == _selected && !_allowReselect) return; _selected = index; _selectedItem = item; var revision = ++_selectionRevision; List<Exception>? errors = null; _changing++;
        try { for (var i = 0; i < _popup.ItemCount; i++) { try { _popup.SetItemChecked(i, _popup.BorrowMenuItem(i) == item); } catch (Exception e) { CollectException(ref errors, e); } if (revision != _selectionRevision || IsDisposed) break; } }
        finally { _changing--; }
        if (!IsDisposed && revision == _selectionRevision) { try { Reconcile(); } catch (Exception e) { CollectException(ref errors, e); } if (emit && IsInsideTree && revision == _selectionRevision) try { ItemSelected?.Invoke(_selected); } catch (Exception e) { CollectException(ref errors, e); } }
        ThrowCollected("Option selection callbacks failed.", errors);
    }
    private void MutateItems(Action mutation, Action? complete = null)
    { List<Exception>? errors = null; try { mutation(); } catch (Exception e) { CollectException(ref errors, e); } if (!IsDisposed) { try { complete?.Invoke(); } catch (Exception e) { CollectException(ref errors, e); } try { Reconcile(); NotifyPropertyListChanged(); } catch (Exception e) { CollectException(ref errors, e); } } ThrowCollected("Option item mutation callbacks failed.", errors); }
    private void SelectedByUser(int index) { if (!IsDisposed) SelectCore(index, true); }
    private void PopupHidden() { if (!IsDisposed) ButtonPressed = false; }
    private void MenuChanged() { if (_disposing || IsDisposed) return; if (_changing == 0) Reconcile(); UpdateMinimumSize(); QueueRedraw(); }
    private void Reconcile()
    {
        if (_disposing || IsDisposed) return; var index = _selectedItem is null ? -1 : _popup.FindMenuItem(_selectedItem); if (index != _selected) { _selected = index; _selectionRevision++; }
        if (index < 0) _selectedItem = null;
        Text = index < 0 ? "" : _popup.GetItemText(index); Icon = index < 0 ? null : _popup.GetItemIcon(index);
    }
    /// <summary>Removes one record, retaining selection by identity or clearing a removed selection.</summary><param name="index">A nonnegative index.</param>
    public void RemoveItem(int index) { EnsureMutable(); MutateItems(() => _popup.RemoveItem(index)); }
    /// <summary>Clears item records, selection and caption without ItemSelected.</summary>
    public void Clear() { EnsureMutable(); MutateItems(() => _popup.Clear()); }
    /// <summary>Reports whether any record is enabled and not a separator.</summary><returns>True when user selection is possible.</returns>
    public bool HasSelectableItems() => GetSelectableItem() != -1;
    /// <summary>Finds the first or last enabled nonseparator record.</summary><param name="fromLast">Searches backward when true.</param><returns>The index or -1.</returns>
    public int GetSelectableItem(bool fromLast = false) { CheckOption(); for (var n = 0; n < _popup.ItemCount; n++) { var i = fromLast ? _popup.ItemCount - 1 - n : n; if (!_popup.IsItemDisabled(i) && !_popup.IsItemSeparator(i)) return i; } return -1; }
    /// <summary>Gets the current signed ID, or -1 without selection.</summary><returns>The selected ID.</returns>
    public int GetSelectedID() { CheckOption(); return _selected < 0 ? -1 : _popup.GetItemID(_selected); }
    /// <summary>Gets selected exact generic metadata.</summary><typeparam name="T">The stored exact type.</typeparam><returns>The borrowed value.</returns><exception cref="KeyNotFoundException">Selection or matching metadata is absent.</exception>
    public T GetSelectedMetadata<T>() { CheckOption(); if (_selected < 0) throw new KeyNotFoundException("No selected item metadata."); return _popup.GetItemMetadata<T>(_selected); }
    /// <summary>Gets the first record index with an exact ID.</summary><param name="id">The signed ID.</param><returns>The first index or -1.</returns>
    public int GetItemIndex(int id) { CheckOption(); return _popup.GetItemIndex(id); }
    /// <summary>Gets runtime metadata with its exact generic type.</summary><typeparam name="T">The stored exact type.</typeparam><param name="index">A nonnegative index.</param><returns>The borrowed value.</returns>
    public T GetItemMetadata<T>(int index) { CheckOption(); return _popup.GetItemMetadata<T>(index); }
    /// <summary>Sets exact generic runtime metadata.</summary><typeparam name="T">The stored type.</typeparam><param name="index">An index, optionally counted from the end.</param><param name="value">The borrowed value, including typed null.</param>
    public void SetItemMetadata<T>(int index, T value) { EnsureMutable(); _popup.SetItemMetadata(index, value); }
    /// <summary>Gets an item's Text value.</summary><param name="index">A nonnegative index.</param><returns>The stored value; resources remain borrowed.</returns>
    public string GetItemText(int index) { CheckOption(); return _popup.GetItemText(index); }
    /// <summary>Sets an item's Text value and refreshes a selected caption/icon when applicable.</summary><param name="index">An index, optionally counted from the end.</param><param name="value">The new typed value.</param>
    public void SetItemText(int index, string value) { EnsureMutable(); _popup.SetItemText(index, value); }
    /// <summary>Gets an item's Icon value.</summary><param name="index">A nonnegative index.</param><returns>The stored value; resources remain borrowed.</returns>
    public Texture? GetItemIcon(int index) { CheckOption(); return _popup.GetItemIcon(index); }
    /// <summary>Sets an item's Icon value and refreshes a selected caption/icon when applicable.</summary><param name="index">An index, optionally counted from the end.</param><param name="value">The new typed value.</param>
    public void SetItemIcon(int index, Texture? value) { EnsureMutable(); _popup.SetItemIcon(index, value); }
    /// <summary>Gets an item's ID value.</summary><param name="index">A nonnegative index; -1 returns -1.</param><returns>The stored value; resources remain borrowed.</returns>
    public int GetItemID(int index) { CheckOption(); if (index == -1) return -1; return _popup.GetItemID(index); }
    /// <summary>Sets an item's ID value and refreshes a selected caption/icon when applicable.</summary><param name="index">An index, optionally counted from the end.</param><param name="value">The new typed value.</param>
    public void SetItemID(int index, int value) { EnsureMutable(); _popup.SetItemID(index, value); }
    /// <summary>Gets an item's Tooltip value.</summary><param name="index">A nonnegative index.</param><returns>The stored value; resources remain borrowed.</returns>
    public string GetItemTooltip(int index) { CheckOption(); return _popup.GetItemTooltip(index); }
    /// <summary>Sets an item's Tooltip value and refreshes a selected caption/icon when applicable.</summary><param name="index">An index, optionally counted from the end.</param><param name="value">The new typed value.</param>
    public void SetItemTooltip(int index, string value) { EnsureMutable(); _popup.SetItemTooltip(index, value); }
    /// <summary>Gets an item's AutoTranslateMode value.</summary><param name="index">A nonnegative index.</param><returns>The stored value; resources remain borrowed.</returns>
    public NodeAutoTranslateMode GetItemAutoTranslateMode(int index) { CheckOption(); return _popup.GetItemAutoTranslateMode(index); }
    /// <summary>Sets an item's AutoTranslateMode value and refreshes a selected caption/icon when applicable.</summary><param name="index">An index, optionally counted from the end.</param><param name="value">The new typed value.</param>
    public void SetItemAutoTranslateMode(int index, NodeAutoTranslateMode value) { EnsureMutable(); _popup.SetItemAutoTranslateMode(index, value); }
    /// <summary>Reports an item's Disabled flag.</summary><param name="index">A nonnegative index.</param><returns>The stored flag.</returns>
    public bool IsItemDisabled(int index) { CheckOption(); return _popup.IsItemDisabled(index); }
    /// <summary>Reports an item's Separator flag.</summary><param name="index">A nonnegative index.</param><returns>The stored flag.</returns>
    public bool IsItemSeparator(int index) { CheckOption(); return _popup.IsItemSeparator(index); }
    /// <summary>Changes an enabled/disabled flag without changing programmatic selection.</summary><param name="index">An index, optionally negative.</param><param name="disabled">The flag.</param>
    public void SetItemDisabled(int index, bool disabled) { EnsureMutable(); _popup.SetItemDisabled(index, disabled); }
    /// <summary>Disables this button's shortcut input stage, including popup item shortcuts.</summary><param name="disabled">Whether shortcut handling is disabled.</param>
    public void SetDisableShortcuts(bool disabled) { EnsureMutable(); _disableShortcuts = disabled; }
    /// <inheritdoc />
    protected override void OnPressed() { if (_popup.Visible) { _popup.Hide(); return; } try { ShowPopup(); } catch { if (!IsDisposed) SetPressedNoSignal(false); throw; } }
    /// <summary>Opens the owned menu beneath the button; keyboard/programmatic opening focuses selection or the first selectable item.</summary>
    /// <remarks>Detached buttons return without opening. Attached presentation requires an embedded window host.</remarks>
    public void ShowPopup()
    {
        EnsureMutable(); if (GetViewport() is null) return; var rect = GetGlobalRect(); var at = rect.Position + new Vector2(0, rect.Size.Y);
        if (GetViewport() is Window { Embedder: not null } window) at += (Vector2)window.Position;
        _popup.LayoutDirection = LayoutDirection; _popup.MinSize = Vector2i.Zero; _popup.Popup(new((Vector2i)at, new(Math.Max(1, (int)MathF.Ceiling(rect.Size.X)), 1)));
        var focus = _selected >= 0 && !_popup.IsItemDisabled(_selected) && !_popup.IsItemSeparator(_selected) ? _selected : GetSelectableItem();
        if (focus >= 0) { if (!WasPressedByMouse) _popup.SetFocusedItem(focus); _popup.ScrollToItem(focus); }
    }
    /// <inheritdoc />
    protected override void OnShortcutInput(InputEvent inputEvent)
    {
        if (_disableShortcuts) return; if (!Disabled && IsVisibleInTree && inputEvent.IsPressed() && !inputEvent.IsEcho() && _popup.ActivateItemByEvent(inputEvent)) { Tree!.SetInputAsHandled(); return; }
        base.OnShortcutInput(inputEvent);
    }
    internal override string TranslateButtonText(string text)
    { if (_popup is null || _selected < 0 || _selected >= _popup.ItemCount) return base.TranslateButtonText(text); return TranslateItem(_selected, text); }
    private string TranslateItem(int index, string text) => _popup.GetItemAutoTranslateMode(index) switch { NodeAutoTranslateMode.Disabled => text, NodeAutoTranslateMode.Always => Tr(text), _ => Atr(text) };
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(OptionButton) ? CreateOptionButton : base.CreateSceneInstanceFactory();
    private static Node CreateOptionButton() => new OptionButton();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _disposing = true; ItemSelected = null; ItemFocused = null; _selectedItem = null; } base.Dispose(disposing); }
}
