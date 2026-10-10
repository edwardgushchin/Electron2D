namespace Electron2D;

/// <summary>A themed, scrollable popup of text, icon, check, state and submenu items.</summary>
/// <remarks>Items borrow icons and shortcuts. Check and state changes are explicit; activation only emits events.
/// Metadata uses exact generic types and remains runtime-only. The embedded host owns presentation and input.
/// A MenuBar parent uses this popup's child-bound scene fields for its header title, tooltip and eligibility.</remarks>
public partial class PopupMenu : Popup
{
    internal sealed class Item
    {
        internal string Text = "", Language = "", Tooltip = "";
        internal Texture? Icon;
        internal Shortcut? Shortcut;
        internal PopupMenu? Submenu;
        internal int ID, Checkable, IconMaxWidth, Indent, State, MaxStates;
        internal Key Accelerator;
        internal bool Checked, Disabled, Separator, ShortcutDisabled, Global, AllowEcho, Visible = true;
        internal Color IconModulate = Colors.White;
        internal TextDirection Direction = TextDirection.Auto;
        internal NodeAutoTranslateMode AutoTranslate = NodeAutoTranslateMode.Inherit;
        internal object? Metadata;
        internal readonly TextLayout TextLayout = new(), AcceleratorLayout = new();
        internal Rect2 Rect;
        internal Vector2 IconSize;
        internal string DisplayText = "", AcceleratorText = "";
    }
    internal string? MenuBarTitle;
    internal string MenuBarTooltip = "";
    internal bool MenuBarDisabled, MenuBarHidden;
    private readonly List<Item> _items = [];
    private readonly Dictionary<Resource, int> _resources = [];
    private readonly MenuItems _view;
    private readonly Panel _panel;
    private readonly ScrollContainer _scroll;
    private readonly LineEdit _search;
    private readonly Timer _submenuTimer;
    private int _focused = -1;
    private bool _dirty = true, _arranging, _searchVisible;
    private int _pendingSubmenu = -1;
    private PopupMenu? _activeSubmenu;
    private string _typeSearch = "";
    private double _typeRemaining;
    /// <summary>Creates a hidden transparent menu with owned internal panel, scroll, search and timer controls.</summary>
    public PopupMenu()
    {
        Transparent = true; TransparentBG = true; InputEnabled = true;
        CanvasItemDefaultTextureFilter = DefaultCanvasItemTextureFilter.ParentNode; CanvasItemDefaultTextureRepeat = DefaultCanvasItemTextureRepeat.ParentNode;
        _panel = new Panel { Name = "_menu_panel", MouseFilter = MouseFilter.Ignore };
        _scroll = new ScrollContainer { Name = "_menu_scroll", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _search = new LineEdit { Name = "_menu_search", Visible = false, ClearButtonEnabled = true, PlaceholderText = "Search", KeepEditingOnTextSubmit = true };
        _view = new(this) { Name = "_menu_items", FocusMode = FocusMode.All, MouseFilter = MouseFilter.Stop, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _scroll.AddChild(_view);
        AddChild(_panel, InternalMode.Front); AddChild(_search, InternalMode.Front); AddChild(_scroll, InternalMode.Front);
        _submenuTimer = new Timer { Name = "_submenu_timer", OneShot = true, WaitTime = .2 }; _submenuTimer.Timeout += _ => OpenPendingSubmenu(); AddChild(_submenuTimer, InternalMode.Front);
        _search.TextChanged += FilterChanged; _search.TextSubmitted += _ => ActivateFocused();
        SizeChanged += Arrange; VisibilityChanged += VisibilityUpdated;
    }
    /// <summary>Occurs after an eligible item's popup-hide policy completes, carrying its ID.</summary>
    public event Action<int>? IDPressed;
    /// <summary>Occurs after IDPressed, carrying the activated zero-based item index.</summary>
    public event Action<int>? IndexPressed;
    /// <summary>Occurs when keyboard or controller navigation focuses an eligible item.</summary>
    public event Action<int>? IDFocused;
    /// <summary>Occurs after an item mutation commits.</summary>
    public event Action? MenuChanged;
    /// <summary>Gets or sets the item count, preserving retained records and assigning new IDs from indices.</summary>
    /// <value>Zero initially; between zero and 65536.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside the supported range.</exception>
    public int ItemCount
    {
        get { CheckMenu(); return _items.Count; }
        set { EnsureMutable(); if ((uint)value > 65536) throw new ArgumentOutOfRangeException(nameof(value)); if (value == _items.Count) return; List<Exception>? errors = null; while (_items.Count > value) RemoveCore(_items.Count - 1, ref errors); while (_items.Count < value) _items.Add(new() { ID = _items.Count }); ChangedStructure(errors); }
    }
    private void CheckMenu() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private int Index(int index, bool negative = false) { if (negative && index < 0) index += _items.Count; if ((uint)index >= (uint)_items.Count) throw new ArgumentOutOfRangeException(nameof(index)); return index; }
    internal Item BorrowMenuItem(int index) => Get(index);
    internal int FindMenuItem(Item item) { CheckMenu(); return _items.IndexOf(item); }
    private Item Get(int index, bool negative = false) { CheckMenu(); return _items[Index(index, negative)]; }
    private static void Text(string text) { ArgumentNullException.ThrowIfNull(text); }
    private void Changed() { _dirty = true; _view?.QueueRedraw(); if (IsInsideTree) Arrange(); MenuChanged?.Invoke(); }
    private void ChangedStructure(List<Exception>? errors = null) { try { Changed(); } catch (Exception error) { CollectException(ref errors, error); } try { NotifyPropertyListChanged(); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("Menu structure callbacks failed.", errors); }
    private void Watch(Resource? resource)
    {
        if (resource is null) return; if (resource.IsDisposed) throw new ObjectDisposedException(resource.GetType().Name);
        if (_resources.TryGetValue(resource, out var count)) { _resources[resource] = count + 1; return; }
        _resources.Add(resource, 1); resource.Changed += ResourceChanged; resource.Disposed += ResourceDisposed;
    }
    private void Unwatch(Resource? resource)
    {
        if (resource is null) return; if (!_resources.TryGetValue(resource, out var count)) return; if (count > 1) { _resources[resource] = count - 1; return; }
        _resources.Remove(resource); resource.Changed -= ResourceChanged; resource.Disposed -= ResourceDisposed;
    }
    private void ResourceChanged(Resource _) { if (!IsDisposed) { if (Tree is { } tree) tree.Defer(() => { if (!IsDisposed) Changed(); }); else Changed(); } }
    private void ResourceDisposed(ElectronObject resource)
    {
        if (Tree is { } tree && !tree.IsOwnerThread) { if (!tree.IsClosing) tree.Defer(() => ResourceDisposedOnOwner(resource)); return; }
        ResourceDisposedOnOwner(resource);
    }
    private void ResourceDisposedOnOwner(ElectronObject resource)
    {
        if (IsDisposed) return; foreach (var item in _items) { if (item.Icon == resource) item.Icon = null; if (item.Shortcut == resource) item.Shortcut = null; }
        var typed = (Resource)resource; _resources.Remove(typed); typed.Changed -= ResourceChanged; typed.Disposed -= ResourceDisposed; Changed();
    }
    private void Add(string label, int id, Key accelerator, Texture? icon = null, int checkable = 0, int maxStates = 0, int state = 0, Shortcut? shortcut = null, bool global = false, bool allowEcho = false, bool separator = false, PopupMenu? submenu = null)
    {
        EnsureMutable(); Text(label); if (_items.Count == 65536) throw new InvalidOperationException("Menu item capacity exceeded."); if (icon?.IsDisposed == true || shortcut?.IsDisposed == true) throw new ObjectDisposedException(nameof(icon));
        var item = new Item { Text = label, ID = id == -1 ? _items.Count : id, Accelerator = accelerator, Icon = icon, Checkable = checkable, MaxStates = maxStates, State = state, Shortcut = shortcut, Global = global, AllowEcho = allowEcho, Separator = separator, Submenu = submenu };
        Watch(icon); Watch(shortcut); _items.Add(item); ChangedStructure();
    }
    /// <summary>Adds a text item; ID -1 derives from its new index.</summary>
    /// <param name="label">The typed item label value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="accelerator">The typed item accelerator value.</param>
    public void AddItem(string label, int id = -1, Key accelerator = Key.None) { Add(label, id, accelerator); }
    /// <summary>Adds a checkbox item; ID -1 derives from its new index.</summary>
    /// <param name="label">The typed item label value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="accelerator">The typed item accelerator value.</param>
    public void AddCheckItem(string label, int id = -1, Key accelerator = Key.None) { Add(label, id, accelerator, checkable: 1); }
    /// <summary>Adds a radio item; ID -1 derives from its new index.</summary>
    /// <param name="label">The typed item label value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="accelerator">The typed item accelerator value.</param>
    public void AddRadioCheckItem(string label, int id = -1, Key accelerator = Key.None) { Add(label, id, accelerator, checkable: 2); }
    /// <summary>Adds an icon and text item; ID -1 derives from its new index.</summary>
    /// <param name="icon">The typed item icon value.</param>
    /// <param name="label">The typed item label value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="accelerator">The typed item accelerator value.</param>
    public void AddIconItem(Texture? icon, string label, int id = -1, Key accelerator = Key.None) { Add(label, id, accelerator, icon); }
    /// <summary>Adds a checkbox item with an icon; ID -1 derives from its new index.</summary>
    /// <param name="icon">The typed item icon value.</param>
    /// <param name="label">The typed item label value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="accelerator">The typed item accelerator value.</param>
    public void AddIconCheckItem(Texture? icon, string label, int id = -1, Key accelerator = Key.None) { Add(label, id, accelerator, icon, 1); }
    /// <summary>Adds a radio item with an icon; ID -1 derives from its new index.</summary>
    /// <param name="icon">The typed item icon value.</param>
    /// <param name="label">The typed item label value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="accelerator">The typed item accelerator value.</param>
    public void AddIconRadioCheckItem(Texture? icon, string label, int id = -1, Key accelerator = Key.None) { Add(label, id, accelerator, icon, 2); }
    /// <summary>Adds an item with an explicit state range; ID -1 derives from its new index.</summary>
    /// <param name="label">The typed item label value.</param>
    /// <param name="maxStates">The typed item maxStates value.</param>
    /// <param name="defaultState">The typed item defaultState value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="accelerator">The typed item accelerator value.</param>
    public void AddMultistateItem(string label, int maxStates, int defaultState = 0, int id = -1, Key accelerator = Key.None) { Add(label, id, accelerator, maxStates: maxStates, state: defaultState); }
    /// <summary>Adds a separator with an optional title; ID -1 derives from its new index.</summary>
    /// <param name="label">The typed item label value.</param>
    /// <param name="id">The typed item id value.</param>
    public void AddSeparator(string label = "", int id = -1) { Add(label, id, Key.None, separator: true); }
    /// <summary>Adds an item titled from a borrowed shortcut; ID -1 derives from its new index.</summary>
    /// <param name="shortcut">The typed item shortcut value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="global">The typed item global value.</param>
    /// <param name="allowEcho">The typed item allowEcho value.</param>
    public void AddShortcut(Shortcut shortcut, int id = -1, bool global = false, bool allowEcho = false) { ArgumentNullException.ThrowIfNull(shortcut); Add(shortcut.ResourceName, id, Key.None, null, checkable: 0, shortcut: shortcut, global: global, allowEcho: allowEcho); }
    /// <summary>Adds an icon item titled from a borrowed shortcut; ID -1 derives from its new index.</summary>
    /// <param name="icon">The typed item icon value.</param>
    /// <param name="shortcut">The typed item shortcut value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="global">The typed item global value.</param>
    /// <param name="allowEcho">The typed item allowEcho value.</param>
    public void AddIconShortcut(Texture? icon, Shortcut shortcut, int id = -1, bool global = false, bool allowEcho = false) { ArgumentNullException.ThrowIfNull(shortcut); Add(shortcut.ResourceName, id, Key.None, icon, checkable: 0, shortcut: shortcut, global: global, allowEcho: allowEcho); }
    /// <summary>Adds a checkbox item titled from a borrowed shortcut; ID -1 derives from its new index.</summary>
    /// <param name="shortcut">The typed item shortcut value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="global">The typed item global value.</param>
    public void AddCheckShortcut(Shortcut shortcut, int id = -1, bool global = false) { ArgumentNullException.ThrowIfNull(shortcut); Add(shortcut.ResourceName, id, Key.None, null, checkable: 1, shortcut: shortcut, global: global, allowEcho: false); }
    /// <summary>Adds an icon checkbox item titled from a borrowed shortcut; ID -1 derives from its new index.</summary>
    /// <param name="icon">The typed item icon value.</param>
    /// <param name="shortcut">The typed item shortcut value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="global">The typed item global value.</param>
    public void AddIconCheckShortcut(Texture? icon, Shortcut shortcut, int id = -1, bool global = false) { ArgumentNullException.ThrowIfNull(shortcut); Add(shortcut.ResourceName, id, Key.None, icon, checkable: 1, shortcut: shortcut, global: global, allowEcho: false); }
    /// <summary>Adds a radio item titled from a borrowed shortcut; ID -1 derives from its new index.</summary>
    /// <param name="shortcut">The typed item shortcut value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="global">The typed item global value.</param>
    public void AddRadioCheckShortcut(Shortcut shortcut, int id = -1, bool global = false) { ArgumentNullException.ThrowIfNull(shortcut); Add(shortcut.ResourceName, id, Key.None, null, checkable: 2, shortcut: shortcut, global: global, allowEcho: false); }
    /// <summary>Adds an icon radio item titled from a borrowed shortcut; ID -1 derives from its new index.</summary>
    /// <param name="icon">The typed item icon value.</param>
    /// <param name="shortcut">The typed item shortcut value.</param>
    /// <param name="id">The typed item id value.</param>
    /// <param name="global">The typed item global value.</param>
    public void AddIconRadioCheckShortcut(Texture? icon, Shortcut shortcut, int id = -1, bool global = false) { ArgumentNullException.ThrowIfNull(shortcut); Add(shortcut.ResourceName, id, Key.None, icon, checkable: 2, shortcut: shortcut, global: global, allowEcho: false); }
    /// <summary>Gets an item's Text value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public string GetItemText(int index) => Get(index).Text;
    /// <summary>Sets an item's Text value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemText(int index, string value) { EnsureMutable(); var item = Get(index, true); Text(value); if (item.Text == value) return; item.Text = value; Changed(); }
    /// <summary>Gets an item's Language value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public string GetItemLanguage(int index) => Get(index).Language;
    /// <summary>Sets an item's Language value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemLanguage(int index, string value) { EnsureMutable(); var item = Get(index, true); Text(value); if (item.Language == value) return; item.Language = value; Changed(); }
    /// <summary>Gets an item's Tooltip value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public string GetItemTooltip(int index) => Get(index).Tooltip;
    /// <summary>Sets an item's Tooltip value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemTooltip(int index, string value) { EnsureMutable(); var item = Get(index, true); Text(value); if (item.Tooltip == value) return; item.Tooltip = value; Changed(); }
    /// <summary>Gets an item's IconMaxWidth value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public int GetItemIconMaxWidth(int index) => Get(index).IconMaxWidth;
    /// <summary>Sets an item's IconMaxWidth value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemIconMaxWidth(int index, int value) { EnsureMutable(); var item = Get(index, true); if (item.IconMaxWidth == value) return; item.IconMaxWidth = value; Changed(); }
    /// <summary>Gets an item's IconModulate value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public Color GetItemIconModulate(int index) => Get(index).IconModulate;
    /// <summary>Sets an item's IconModulate value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemIconModulate(int index, Color value) { EnsureMutable(); var item = Get(index, true); if (!value.IsFinite()) throw new ArgumentException("Color must be finite.", nameof(value)); if (item.IconModulate == value) return; item.IconModulate = value; Changed(); }
    /// <summary>Gets an item's ID value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public int GetItemID(int index) => Get(index).ID;
    /// <summary>Sets an item's ID value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemID(int index, int value) { EnsureMutable(); var item = Get(index, true); if (item.ID == value) return; item.ID = value; Changed(); }
    /// <summary>Gets an item's Indent value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public int GetItemIndent(int index) => Get(index).Indent;
    /// <summary>Sets an item's Indent value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemIndent(int index, int value) { EnsureMutable(); var item = Get(index, true); if (item.Indent == value) return; item.Indent = value; Changed(); }
    /// <summary>Gets an item's Accelerator value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public Key GetItemAccelerator(int index) => Get(index).Accelerator;
    /// <summary>Sets an item's Accelerator value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemAccelerator(int index, Key value) { EnsureMutable(); var item = Get(index, true); if (item.Accelerator == value) return; item.Accelerator = value; Changed(); }
    /// <summary>Gets an item's Multistate value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public int GetItemMultistate(int index) => Get(index).State;
    /// <summary>Sets an item's Multistate value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemMultistate(int index, int value) { EnsureMutable(); var item = Get(index, true); if (item.State == value) return; item.State = value; Changed(); }
    /// <summary>Gets an item's MultistateMax value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public int GetItemMultistateMax(int index) => Get(index).MaxStates;
    /// <summary>Sets an item's MultistateMax value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemMultistateMax(int index, int value) { EnsureMutable(); var item = Get(index, true); if (item.MaxStates == value) return; item.MaxStates = value; Changed(); }
    /// <summary>Gets an item's TextDirection value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public TextDirection GetItemTextDirection(int index) => Get(index).Direction;
    /// <summary>Sets an item's TextDirection value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemTextDirection(int index, TextDirection value) { EnsureMutable(); var item = Get(index, true); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (item.Direction == value) return; item.Direction = value; Changed(); }
    /// <summary>Gets an item's AutoTranslateMode value.</summary><param name="index">A nonnegative item index.</param><returns>The stored typed value.</returns>
    public NodeAutoTranslateMode GetItemAutoTranslateMode(int index) => Get(index).AutoTranslate;
    /// <summary>Sets an item's AutoTranslateMode value after validation; negative indices count from the end.</summary><param name="index">An item index.</param><param name="value">The new typed value.</param>
    public void SetItemAutoTranslateMode(int index, NodeAutoTranslateMode value) { EnsureMutable(); var item = Get(index, true); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (item.AutoTranslate == value) return; item.AutoTranslate = value; Changed(); }
    /// <summary>Reports an item's Checked state.</summary><param name="index">A nonnegative index.</param><returns>The stored flag.</returns>
    public bool IsItemChecked(int index) => Get(index).Checked;
    /// <summary>Changes an item's Checked flag.</summary><param name="index">An index, optionally negative.</param><param name="value">The desired state.</param>
    public void SetItemChecked(int index, bool value) { EnsureMutable(); var item = Get(index, true); if (item.Checked == value) return; item.Checked = value; Changed(); }
    /// <summary>Reports an item's Disabled state.</summary><param name="index">A nonnegative index.</param><returns>The stored flag.</returns>
    public bool IsItemDisabled(int index) => Get(index).Disabled;
    /// <summary>Changes an item's Disabled flag.</summary><param name="index">An index, optionally negative.</param><param name="value">The desired state.</param>
    public void SetItemDisabled(int index, bool value) { EnsureMutable(); var item = Get(index, true); if (item.Disabled == value) return; item.Disabled = value; Changed(); }
    /// <summary>Reports an item's Separator state.</summary><param name="index">A nonnegative index.</param><returns>The stored flag.</returns>
    public bool IsItemSeparator(int index) => Get(index).Separator;
    /// <summary>Changes an item's Separator flag.</summary><param name="index">An index, optionally negative.</param><param name="value">The desired state.</param>
    public void SetItemAsSeparator(int index, bool value) { EnsureMutable(); var item = Get(index, true); if (item.Separator == value) return; item.Separator = value; Changed(); }
    /// <summary>Reports an item's ShortcutDisabled state.</summary><param name="index">A nonnegative index.</param><returns>The stored flag.</returns>
    public bool IsItemShortcutDisabled(int index) => Get(index).ShortcutDisabled;
    /// <summary>Changes an item's ShortcutDisabled flag.</summary><param name="index">An index, optionally negative.</param><param name="value">The desired state.</param>
    public void SetItemShortcutDisabled(int index, bool value) { EnsureMutable(); var item = Get(index, true); if (item.ShortcutDisabled == value) return; item.ShortcutDisabled = value; Changed(); }
    /// <summary>Reports whether an item uses checkbox or radio decoration.</summary><param name="index">An index.</param><returns>True for either checkable role.</returns>
    public bool IsItemCheckable(int index) => Get(index).Checkable != 0;
    /// <summary>Reports whether an item uses radio decoration.</summary><param name="index">An index.</param><returns>True for the radio role.</returns>
    public bool IsItemRadioCheckable(int index) => Get(index).Checkable == 2;
    /// <summary>Sets checkbox decoration, or resets the item to plain text.</summary><param name="index">An index, optionally negative.</param><param name="value">Whether it is checkable.</param>
    public void SetItemAsCheckable(int index, bool value) { EnsureMutable(); var item = Get(index, true); var kind = value ? 1 : 0; if (item.Checkable == kind) return; item.Checkable = kind; Changed(); }
    /// <summary>Sets radio decoration, or resets the item to plain text.</summary><param name="index">An index, optionally negative.</param><param name="value">Whether it is radio checkable.</param>
    public void SetItemAsRadioCheckable(int index, bool value) { EnsureMutable(); var item = Get(index, true); var kind = value ? 2 : 0; if (item.Checkable == kind) return; item.Checkable = kind; Changed(); }
    /// <summary>Toggles the stored check state without activation.</summary><param name="index">A nonnegative index.</param>
    public void ToggleItemChecked(int index) { EnsureMutable(); var item = Get(index); item.Checked = !item.Checked; Changed(); }
    /// <summary>Advances a positive-range multistate item and wraps to zero.</summary><param name="index">A nonnegative index.</param>
    public void ToggleItemMultistate(int index) { EnsureMutable(); var item = Get(index); if (item.MaxStates <= 0) return; item.State = item.State >= item.MaxStates - 1 ? 0 : item.State + 1; Changed(); }
    /// <summary>Returns the first item index with an exact ID.</summary><param name="id">The signed ID.</param><returns>The index or minus one.</returns>
    public int GetItemIndex(int id) { CheckMenu(); return _items.FindIndex(item => item.ID == id); }
    /// <summary>Moves a record while preserving its ID and resource identities.</summary><param name="index">A source index.</param><param name="targetIndex">A destination index.</param>
    public void SetItemIndex(int index, int targetIndex) { EnsureMutable(); Index(index); Index(targetIndex); if (index == targetIndex) return; var item = _items[index]; _items.RemoveAt(index); _items.Insert(targetIndex, item); Changed(); }
    /// <summary>Gets the item's borrowed icon.</summary><param name="index">An index.</param><returns>The icon or null.</returns>
    public Texture? GetItemIcon(int index) => Get(index).Icon is { IsDisposed: false } icon ? icon : null;
    /// <summary>Replaces a borrowed icon and its invalidation subscription.</summary><param name="index">An index, optionally negative.</param><param name="icon">A live borrowed icon or null.</param>
    public void SetItemIcon(int index, Texture? icon) { EnsureMutable(); var item = Get(index, true); if (item.Icon == icon) return; Watch(icon); Unwatch(item.Icon); item.Icon = icon; Changed(); }
    /// <summary>Gets the item's borrowed shortcut.</summary><param name="index">An index.</param><returns>The shortcut or null.</returns>
    public Shortcut? GetItemShortcut(int index) => Get(index).Shortcut is { IsDisposed: false } shortcut ? shortcut : null;
    /// <summary>Replaces a borrowed shortcut and its global matching policy.</summary><param name="index">An index, optionally negative.</param><param name="shortcut">A live shortcut or null.</param><param name="global">Whether global-only requests may activate it.</param>
    public void SetItemShortcut(int index, Shortcut? shortcut, bool global = false) { EnsureMutable(); var item = Get(index, true); if (item.Shortcut == shortcut && item.Global == global) return; Watch(shortcut); Unwatch(item.Shortcut); item.Shortcut = shortcut; item.Global = global; Changed(); }
    private sealed record MetadataValue<T>(T Value);
    /// <summary>Sets runtime metadata with an exact generic type.</summary><typeparam name="T">The value type.</typeparam><param name="index">An index, optionally negative.</param><param name="value">The borrowed value, including typed null.</param>
    public void SetItemMetadata<T>(int index, T value) { EnsureMutable(); Get(index, true).Metadata = new MetadataValue<T>(value); }
    /// <summary>Gets metadata stored with the same exact generic type.</summary><typeparam name="T">The stored type.</typeparam><param name="index">A nonnegative index.</param><returns>The retained value.</returns><exception cref="KeyNotFoundException">No value of this exact type was stored.</exception>
    public T GetItemMetadata<T>(int index) => Get(index).Metadata is MetadataValue<T> typed ? typed.Value : throw new KeyNotFoundException("No metadata of the requested exact type.");
    /// <summary>Adds a submenu record, parenting a detached submenu when needed.</summary><param name="label">The title.</param><param name="submenu">A direct child or detached menu.</param><param name="id">An ID, or -1 for automatic.</param>
    public void AddSubmenuNodeItem(string label, PopupMenu submenu, int id = -1) { EnsureMutable(); ArgumentNullException.ThrowIfNull(submenu); ValidateSubmenu(submenu); Text(label); if (_items.Count == 65536) throw new InvalidOperationException("Menu capacity exceeded."); if (submenu.Parent is null) AddChild(submenu); Add(label, id, Key.None, submenu: submenu); }
    private void ValidateSubmenu(PopupMenu? submenu) { if (submenu is null) return; if (submenu.IsDisposed) throw new ObjectDisposedException(nameof(submenu)); if (submenu == this || submenu.IsAncestorOf(this)) throw new InvalidOperationException("Submenu cycle."); if (submenu.Parent is not null && submenu.Parent != this) throw new InvalidOperationException("Submenu belongs to another parent."); }
    /// <summary>Gets a live borrowed submenu.</summary><param name="index">An index.</param><returns>A direct submenu, or null.</returns>
    public PopupMenu? GetItemSubmenuNode(int index) { var child = Get(index).Submenu; return child is { IsDisposed: false } && child.Parent == this ? child : null; }
    /// <summary>Changes the submenu identity, parenting a detached child.</summary><param name="index">An index, optionally negative.</param><param name="submenu">A direct or detached menu, or null.</param>
    public void SetItemSubmenuNode(int index, PopupMenu? submenu) { EnsureMutable(); var item = Get(index, true); ValidateSubmenu(submenu); if (submenu?.Parent is null && submenu is not null) AddChild(submenu); if (item.Submenu == submenu) return; if (item.Submenu == _activeSubmenu) CloseSubmenu(); item.Submenu = submenu; Changed(); }
    /// <summary>Removes one record without freeing its submenu.</summary><param name="index">A nonnegative index.</param>
    public void RemoveItem(int index) { EnsureMutable(); Index(index); List<Exception>? errors = null; RemoveCore(index, ref errors); ChangedStructure(errors); }
    private void RemoveCore(int index, ref List<Exception>? errors) { var item = _items[index]; if (item.Submenu == _activeSubmenu) try { CloseSubmenu(); } catch (Exception error) { CollectException(ref errors, error); } Unwatch(item.Icon); Unwatch(item.Shortcut); _items.RemoveAt(index); if (_focused >= _items.Count) _focused = -1; }
    /// <summary>Clears items, optionally disposing their distinct submenu nodes.</summary><param name="freeSubmenus">Whether to detach and free submenu nodes.</param>
    public void Clear(bool freeSubmenus = false) { EnsureMutable(); var children = freeSubmenus ? _items.Where(i => i.Submenu is not null).Select(i => i.Submenu!).Distinct().ToArray() : []; List<Exception>? errors = null; while (_items.Count > 0) RemoveCore(_items.Count - 1, ref errors); _focused = -1; foreach (var child in children) if (!child.IsDisposed) { try { if (child.Parent == this) RemoveChild(child); } catch (Exception error) { CollectException(ref errors, error); } try { child.Dispose(); } catch (Exception error) { CollectException(ref errors, error); } } ChangedStructure(errors); }
    /// <summary>Gets the current focused index.</summary><returns>Minus one when no item is focused.</returns>
    public int GetFocusedItem() { CheckMenu(); return _focused; }
    /// <summary>Changes focus decoration without emitting IDFocused.</summary><param name="index">An index or -1 to clear.</param>
    public void SetFocusedItem(int index) { EnsureMutable(); if (index != -1) Index(index); if (_focused == index) return; _focused = index; if (index >= 0) ScrollToItem(index); _view.QueueRedraw(); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(PopupMenu) ? CreateMenu : base.CreateSceneInstanceFactory();
    private static Node CreateMenu() => new PopupMenu();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { foreach (var resource in _resources.Keys) { resource.Changed -= ResourceChanged; resource.Disposed -= ResourceDisposed; } _resources.Clear(); IDPressed = null; IndexPressed = null; IDFocused = null; MenuChanged = null; _items.Clear(); } base.Dispose(disposing); }
}
