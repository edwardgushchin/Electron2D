namespace Electron2D;

/// <summary>Displays a themed horizontal strip of selectable, scrollable and rearrangeable tabs.</summary>
/// <remarks>Tabs own shaped text and borrow icons and typed runtime metadata. CurrentTab assignments report
/// selection independently from change. Hidden/disabled tabs retain their data and current selection. Structural
/// operations allocate; warmed drawing and input reuse prepared state. Attached mutations require the scene owner.</remarks>
public partial class TabBar : Control
{
    /// <summary>Selects alignment within the available tab strip.</summary>
    public enum AlignmentMode
    {
        /// <summary>Align to the logical leading edge.</summary>
        Left = 0,
        /// <summary>Center the visible tabs.</summary>
        Center = 1,
        /// <summary>Align to the logical trailing edge.</summary>
        Right = 2,
        /// <summary>Exclusive upper bound.</summary>
        Max = 3
    }
    /// <summary>Selects which tabs display a close-request button.</summary>
    public enum CloseButtonDisplayPolicy
    {
        /// <summary>Show no close buttons.</summary>
        ShowNever = 0,
        /// <summary>Show a close button on the current tab.</summary>
        ShowActiveOnly = 1,
        /// <summary>Show a close button on every visible tab.</summary>
        ShowAlways = 2,
        /// <summary>Exclusive upper bound.</summary>
        Max = 3
    }
    internal sealed class Tab
    {
        internal string Title = "", Tooltip = "", Language = "";
        internal Texture? Icon, ButtonIcon;
        internal bool Disabled, Hidden;
        internal int IconMaxWidth;
        internal TextDirection Direction = TextDirection.Inherited;
        internal object? Metadata;
        internal readonly TextLayout Layout = new(), Intrinsic = new();
        internal float Width, TextWidth, Offset;
        internal Vector2 IconSize;
        internal Rect2 Rect, ButtonRect, CloseRect;
    }
    private sealed class MetadataValue<T>(T value) { internal readonly T Value = value; }
    private readonly List<Tab> _tabs = [];
    private readonly Dictionary<Texture, int> _icons = [];
    private int _current = -1, _previous = -1, _offset, _lastDrawn = -1, _hover = -1;
    private int _queuedCurrent = -2, _selectionRevision;
    private bool _initialized;
    private bool _clipTabs = true, _scrollToSelected = true, _scrollingEnabled = true, _switchOnDragHover = true, _closeWithMiddleMouse = true;
    private bool _dragToRearrangeEnabled, _deselectEnabled, _selectWithRMB;
    private int _maxTabWidth, _tabsRearrangeGroup = -1;
    private AlignmentMode _tabAlignment;
    private CloseButtonDisplayPolicy _closePolicy;
    private readonly Timer _hoverTimer = new() { Name = "_hover_switch", OneShot = true };
    /// <summary>Creates an empty strip with All focus and one owned internal hover timer.</summary>
    public TabBar()
    {
        FocusMode = FocusMode.All; AddChild(_hoverTimer, InternalMode.Front);
        _hoverTimer.Timeout += HoverTimeout; MouseExited += PointerExited;
        FocusEntered += QueueRedraw; FocusExited += FocusLost;
    }
    /// <summary>Gets or sets the number of tabs, retaining the common prefix.</summary>
    /// <value>Zero initially; zero through 65,536. New slots have empty titles, inherited text direction and no icons.</value>
    /// <remarks>Count edits clamp selection without selection events and notify PropertyListChanged. The first count
    /// assignment resolves an earlier queued CurrentTab. Zero resets current/previous/scroll state.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Count is outside the bounded storage capacity.</exception>
    public int TabCount
    {
        get { ThrowIfDisposed(); return _tabs.Count; }
        set
        {
            EnsureMutable(); if ((uint)value > 65536) throw new ArgumentOutOfRangeException(nameof(value)); if (value == _tabs.Count) return;
            _tabs.EnsureCapacity(value);
            while (_tabs.Count > value) { var tab = _tabs[^1]; ReleaseTab(tab); _tabs.RemoveAt(_tabs.Count - 1); }
            while (_tabs.Count < value) _tabs.Add(new());
            _selectionRevision++; ResetInteraction();
            if (value == 0) { _offset = 0; _current = _previous = -1; }
            else { _offset = Math.Min(_offset, value - 1); _current = Math.Min(_current, value - 1); if (_current == -1 && !CanDeselect()) _current = 0; }
            InvalidateTabs();
            if (!_initialized) { _initialized = true; var queued = _queuedCurrent; _queuedCurrent = -2; if (queued != -2 && queued != _current) CurrentTab = queued; }
            NotifyPropertyListChanged();
        }
    }
    /// <summary>Gets or sets the selected index, including programmatic hidden or disabled selections.</summary>
    /// <value>Minus one initially. Minus one is valid when deselection is enabled or every tab is unavailable.</value>
    /// <remarks>A nonnegative out-of-range value before initial count/tree entry is queued. Every valid assignment
    /// raises TabSelected; a changed index then raises TabChanged. Nested selection/structural callbacks suppress
    /// an obsolete outer change event. Observer failure occurs after state/layout commitment.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is invalid after initialization.</exception>
    /// <exception cref="InvalidOperationException">Deselection is forbidden with an available tab.</exception>
    public int CurrentTab
    {
        get { ThrowIfDisposed(); return _current; }
        set
        {
            EnsureMutable(); if (value == -1) { if (!CanDeselect()) throw new InvalidOperationException("Deselection is disabled while a tab is available."); }
            else { if (value < -1) throw new ArgumentOutOfRangeException(nameof(value)); if (!_initialized && value >= _tabs.Count) { _queuedCurrent = value; return; } Index(value); }
            _previous = _current; _current = value; var revision = ++_selectionRevision;
            var changed = _previous != _current; if (changed) InvalidateTabs();
            List<Exception>? errors = null;
            try { TabSelected?.Invoke(value); } catch (Exception error) { Node.CollectException(ref errors, error); }
            if (changed && !IsDisposed && revision == _selectionRevision) try { TabChanged?.Invoke(value); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Tab selection observers failed.", errors);
        }
    }
    /// <summary>Gets or sets whether overflow tabs are hidden and scroll arrows are displayed.</summary>
    /// <value>true initially.</value>
    public bool ClipTabs { get { ThrowIfDisposed(); return _clipTabs; } set { EnsureMutable(); if (_clipTabs == value) return; _clipTabs = value; if (!value) _offset = 0; InvalidateTabs(); } }
    /// <summary>Gets or sets whether a middle-button press requests closing the hovered tab.</summary>
    /// <value>true initially.</value>
    public bool CloseWithMiddleMouse { get { ThrowIfDisposed(); return _closeWithMiddleMouse; } set { EnsureMutable(); if (_closeWithMiddleMouse == value) return; _closeWithMiddleMouse = value; } }
    /// <summary>Gets or sets whether typed tab drags can rearrange or transfer tabs.</summary>
    /// <value>false initially.</value>
    public bool DragToRearrangeEnabled { get { ThrowIfDisposed(); return _dragToRearrangeEnabled; } set { EnsureMutable(); if (_dragToRearrangeEnabled == value) return; _dragToRearrangeEnabled = value; } }
    /// <summary>Gets or sets automatic scrolling after selection or layout edits.</summary>
    /// <value>true initially.</value>
    public bool ScrollToSelected { get { ThrowIfDisposed(); return _scrollToSelected; } set { EnsureMutable(); if (_scrollToSelected == value) return; _scrollToSelected = value; if (value) EnsureTabVisible(_current); } }
    /// <summary>Gets or sets wheel scrolling; arrow buttons remain usable.</summary>
    /// <value>true initially.</value>
    public bool ScrollingEnabled { get { ThrowIfDisposed(); return _scrollingEnabled; } set { EnsureMutable(); if (_scrollingEnabled == value) return; _scrollingEnabled = value; } }
    /// <summary>Gets or sets whether a right-button press also selects its tab.</summary>
    /// <value>false initially.</value>
    public bool SelectWithRMB { get { ThrowIfDisposed(); return _selectWithRMB; } set { EnsureMutable(); if (_selectWithRMB == value) return; _selectWithRMB = value; } }
    /// <summary>Gets or sets delayed selection while a foreign drag hovers a tab.</summary>
    /// <value>true initially.</value>
    public bool SwitchOnDragHover { get { ThrowIfDisposed(); return _switchOnDragHover; } set { EnsureMutable(); if (_switchOnDragHover == value) return; _switchOnDragHover = value; if (!value) _hoverTimer.Stop(); } }
    /// <summary>Gets or sets the transfer group; minus one limits drops to this strip.</summary>
    /// <value>-1 initially.</value>
    public int TabsRearrangeGroup { get { ThrowIfDisposed(); return _tabsRearrangeGroup; } set { EnsureMutable(); if (_tabsRearrangeGroup == value) return; _tabsRearrangeGroup = value; } }
    /// <summary>Gets or sets whether an available selection may be cleared.</summary>
    /// <value>False initially. Disabling it selects the next available tab if currently deselected.</value>
    public bool DeselectEnabled { get { ThrowIfDisposed(); return _deselectEnabled; } set { EnsureMutable(); if (_deselectEnabled == value) return; _deselectEnabled = value; if (!value && _current == -1) SelectNextAvailable(); } }
    /// <summary>Gets or sets the measured tab width cap, preserving non-text content.</summary>
    /// <value>Zero initially, meaning no explicit cap; nonnegative.</value>
    /// <exception cref="ArgumentOutOfRangeException">The requested cap is negative.</exception>
    public int MaxTabWidth { get { ThrowIfDisposed(); return _maxTabWidth; } set { EnsureMutable(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_maxTabWidth == value) return; _maxTabWidth = value; InvalidateTabs(); } }
    /// <summary>Gets or sets logical leading, centered or trailing alignment.</summary>
    /// <value>Left initially; RTL mirrors the logical leading edge.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is Max or undefined.</exception>
    public AlignmentMode TabAlignment { get { ThrowIfDisposed(); return _tabAlignment; } set { EnsureMutable(); if (value is < AlignmentMode.Left or >= AlignmentMode.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_tabAlignment == value) return; _tabAlignment = value; InvalidateTabs(); } }
    /// <summary>Gets or sets which visible tabs display close buttons.</summary>
    /// <value>ShowNever initially.</value>
    /// <remarks>Close buttons raise requests; they do not remove tabs automatically.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is Max or undefined.</exception>
    public CloseButtonDisplayPolicy TabCloseDisplayPolicy { get { ThrowIfDisposed(); return _closePolicy; } set { EnsureMutable(); if (value is < CloseButtonDisplayPolicy.ShowNever or >= CloseButtonDisplayPolicy.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_closePolicy == value) return; _closePolicy = value; InvalidateTabs(); } }
    /// <summary>Occurs after a same-strip drop reorders an available tab, before its selection.</summary>
    public event Action<int>? ActiveTabRearranged;
    /// <summary>Occurs when the auxiliary icon receives a completed left-button click.</summary>
    public event Action<int>? TabButtonPressed;
    /// <summary>Occurs after the selected index changes; removal of the selected attached tab also reports its replacement.</summary>
    public event Action<int>? TabChanged;
    /// <summary>Occurs after a left or enabled right-button tab selection.</summary>
    public event Action<int>? TabClicked;
    /// <summary>Requests closing a tab after its close button or a middle-button press.</summary>
    public event Action<int>? TabClosePressed;
    /// <summary>Occurs when pointer motion enters a different visible tab; exit emits no index.</summary>
    public event Action<int>? TabHovered;
    /// <summary>Occurs when a right-button press hits a tab, independently of right-button selection.</summary>
    public event Action<int>? TabRMBClicked;
    /// <summary>Occurs for every valid CurrentTab assignment, including equal assignments.</summary>
    public event Action<int>? TabSelected;
    internal Tab BorrowTabRecord(int index) => Item(index);
    internal void ReplaceTabRecord(int index, Tab record)
    {
        EnsureMutable(); var previous = Item(index); if (record.Icon?.IsDisposed == true) record.Icon = null; if (record.ButtonIcon?.IsDisposed == true) record.ButtonIcon = null;
        WatchIcon(record.Icon); try { WatchIcon(record.ButtonIcon); } catch { UnwatchIcon(record.Icon); throw; }
        UnwatchIcon(previous.Icon); UnwatchIcon(previous.ButtonIcon); _tabs[index] = record; InvalidateTabs();
    }
    private Tab Item(int index) { Index(index); return _tabs[index]; }
    private void Index(int index) { if ((uint)index >= (uint)_tabs.Count) throw new ArgumentOutOfRangeException(nameof(index)); }
    private bool CanDeselect() { if (_deselectEnabled) return true; foreach (var tab in _tabs) if (!tab.Disabled && !tab.Hidden) return false; return true; }
    /// <summary>Appends a tab, selecting the first one unless deselection is enabled.</summary>
    /// <param name="title">Nonnull source title; empty is valid.</param>
    /// <param name="icon">Borrowed optional live texture.</param>
    public void AddTab(string title = "", Texture? icon = null)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(title); if (_tabs.Count == 65536) throw new InvalidOperationException("Tab capacity exceeded."); WatchIcon(icon);
        _tabs.Add(new() { Title = title, Icon = icon }); _selectionRevision++; InvalidateTabs();
        if (!_deselectEnabled && _tabs.Count == 1) { if (Tree is not null) CurrentTab = 0; else { _current = 0; _previous = -1; InvalidateTabs(); } }
    }
    /// <summary>Removes every tab, resetting current/previous/scroll state without selection events.</summary>
    public void ClearTabs() { EnsureMutable(); if (_tabs.Count == 0) return; foreach (var tab in _tabs) ReleaseTab(tab); _tabs.Clear(); _current = _previous = -1; _offset = 0; _selectionRevision++; ResetInteraction(); InvalidateTabs(); NotifyPropertyListChanged(); }
    /// <summary>Removes a tab and searches forward, then backward, for an available replacement selection.</summary>
    /// <param name="tabIndex">A valid zero-based index.</param>
    /// <remarks>Attached removal of the selected tab raises TabChanged, without TabSelected.</remarks>
    public void RemoveTab(int tabIndex) { EnsureMutable(); Index(tabIndex); RemoveCore(tabIndex, true); }
    private bool RemoveCore(int index, bool release)
    {
        var selected = _current == index; var tab = _tabs[index]; if (release) ReleaseTab(tab); _tabs.RemoveAt(index);
        if (_current >= index && _current > 0) _current--; if (_previous >= index && _previous > 0) _previous--;
        if (_tabs.Count == 0) { _current = _previous = -1; _offset = 0; }
        else
        {
            if (_current != -1 && (_tabs[_current].Disabled || _tabs[_current].Hidden))
            {
                var replacement = FindAvailable(_current - 1, 1); if (replacement == -1) replacement = FindAvailable(_current, -1); _current = replacement;
            }
            _offset = Math.Min(_offset, _tabs.Count - 1);
        }
        _selectionRevision++; ResetInteraction(); InvalidateTabs(); NotifyPropertyListChanged();
        if (selected && Tree is not null && !IsDisposed) TabChanged?.Invoke(_current); return selected;
    }
    /// <summary>Moves a tab to another existing index while preserving selected and previous tab identities.</summary>
    /// <param name="from">Source index.</param><param name="to">Destination index after removal/reinsertion.</param>
    /// <remarks>No selection signal is emitted; PropertyListChanged reports the new indexed schema.</remarks>
    public void MoveTab(int from, int to)
    {
        EnsureMutable(); Index(from); Index(to); if (from == to) return; var tab = _tabs[from]; _tabs.RemoveAt(from); _tabs.Insert(to, tab);
        _current = MovedIndex(_current, from, to); _previous = MovedIndex(_previous, from, to); _selectionRevision++; ResetInteraction(); InvalidateTabs(); NotifyPropertyListChanged();
    }
    private static int MovedIndex(int index, int from, int to) => index == from ? to : index > from && index <= to ? index - 1 : index < from && index >= to ? index + 1 : index;
    /// <summary>Gets the selected index immediately before the last CurrentTab assignment.</summary>
    /// <returns>Minus one initially; structural operations retain the preceding tab's identity where possible.</returns>
    public int GetPreviousTab() { ThrowIfDisposed(); return _previous; }
    private int FindAvailable(int from, int direction) { for (var i = from + direction; (uint)i < (uint)_tabs.Count; i += direction) if (!_tabs[i].Disabled && !_tabs[i].Hidden) return i; return -1; }
    /// <summary>Selects the next available tab without wrapping.</summary><returns>True when an enabled visible candidate exists.</returns>
    public bool SelectNextAvailable() { EnsureMutable(); var next = FindAvailable(_current, 1); if (next == -1) return false; CurrentTab = next; return true; }
    /// <summary>Selects the previous available tab without wrapping.</summary><returns>True when an enabled visible candidate exists.</returns>
    public bool SelectPreviousAvailable() { EnsureMutable(); var next = FindAvailable(_current, -1); if (next == -1) return false; CurrentTab = next; return true; }
    /// <summary>Gets one tab's Title configuration.</summary><param name="tabIndex">A valid zero-based index.</param><returns>"" initially.</returns>
    public string GetTabTitle(int tabIndex) { ThrowIfDisposed(); return Item(tabIndex).Title; }
    /// <summary>Changes one tab's Title configuration.</summary><param name="tabIndex">A valid zero-based index.</param><param name="value">The nonnull source title.</param>
    public void SetTabTitle(int tabIndex, string value)
    {
        EnsureMutable(); var tab = Item(tabIndex);
        ArgumentNullException.ThrowIfNull(value);
        if (tab.Title == value) return;
        tab.Title = value; InvalidateTabs();
    }
    /// <summary>Gets one tab's Tooltip configuration.</summary><param name="tabIndex">A valid zero-based index.</param><returns>"" initially.</returns>
    public string GetTabTooltip(int tabIndex) { ThrowIfDisposed(); return Item(tabIndex).Tooltip; }
    /// <summary>Changes one tab's Tooltip configuration.</summary><param name="tabIndex">A valid zero-based index.</param><param name="value">The nonnull tooltip; empty uses the translated title when truncated.</param>
    public void SetTabTooltip(int tabIndex, string value)
    {
        EnsureMutable(); var tab = Item(tabIndex);
        ArgumentNullException.ThrowIfNull(value);
        if (tab.Tooltip == value) return;
        tab.Tooltip = value; InvalidateTabs();
    }
    /// <summary>Gets one tab's Language configuration.</summary><param name="tabIndex">A valid zero-based index.</param><returns>"" initially.</returns>
    public string GetTabLanguage(int tabIndex) { ThrowIfDisposed(); return Item(tabIndex).Language; }
    /// <summary>Changes one tab's Language configuration.</summary><param name="tabIndex">A valid zero-based index.</param><param name="value">Nonnull shaping language; empty uses the current locale.</param>
    public void SetTabLanguage(int tabIndex, string value)
    {
        EnsureMutable(); var tab = Item(tabIndex);
        ArgumentNullException.ThrowIfNull(value);
        if (tab.Language == value) return;
        tab.Language = value; InvalidateTabs();
    }
    /// <summary>Gets one tab's TextDirection configuration.</summary><param name="tabIndex">A valid zero-based index.</param><returns>TextDirection.Inherited initially.</returns>
    public TextDirection GetTabTextDirection(int tabIndex) { ThrowIfDisposed(); return Item(tabIndex).Direction; }
    /// <summary>Changes one tab's TextDirection configuration.</summary><param name="tabIndex">A valid zero-based index.</param><param name="value">A defined text direction; Inherited follows layout direction.</param>
    public void SetTabTextDirection(int tabIndex, TextDirection value)
    {
        EnsureMutable(); var tab = Item(tabIndex);
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (tab.Direction == value) return;
        tab.Direction = value; InvalidateTabs();
    }
    /// <summary>Gets one tab's IconMaxWidth configuration.</summary><param name="tabIndex">A valid zero-based index.</param><returns>0 initially.</returns>
    public int GetTabIconMaxWidth(int tabIndex) { ThrowIfDisposed(); return Item(tabIndex).IconMaxWidth; }
    /// <summary>Changes one tab's IconMaxWidth configuration.</summary><param name="tabIndex">A valid zero-based index.</param><param name="value">Nonnegative per-tab icon cap; zero uses the theme cap.</param>
    public void SetTabIconMaxWidth(int tabIndex, int value)
    {
        EnsureMutable(); var tab = Item(tabIndex);
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (tab.IconMaxWidth == value) return;
        tab.IconMaxWidth = value; InvalidateTabs();
    }
    /// <summary>Gets one tab's Icon configuration.</summary><param name="tabIndex">A valid zero-based index.</param><returns>null initially.</returns>
    public Texture? GetTabIcon(int tabIndex) { ThrowIfDisposed(); return Item(tabIndex).Icon; }
    /// <summary>Changes one tab's Icon configuration.</summary><param name="tabIndex">A valid zero-based index.</param><param name="value">Borrowed optional live title icon.</param>
    public void SetTabIcon(int tabIndex, Texture? value)
    {
        EnsureMutable(); var tab = Item(tabIndex);
        if (ReferenceEquals(tab.Icon, value)) return; WatchIcon(value); UnwatchIcon(tab.Icon);
        tab.Icon = value; InvalidateTabs();
    }
    /// <summary>Gets one tab's ButtonIcon configuration.</summary><param name="tabIndex">A valid zero-based index.</param><returns>null initially.</returns>
    public Texture? GetTabButtonIcon(int tabIndex) { ThrowIfDisposed(); return Item(tabIndex).ButtonIcon; }
    /// <summary>Changes one tab's ButtonIcon configuration.</summary><param name="tabIndex">A valid zero-based index.</param><param name="value">Borrowed optional live auxiliary button texture.</param>
    public void SetTabButtonIcon(int tabIndex, Texture? value)
    {
        EnsureMutable(); var tab = Item(tabIndex);
        if (ReferenceEquals(tab.ButtonIcon, value)) return; WatchIcon(value); UnwatchIcon(tab.ButtonIcon);
        tab.ButtonIcon = value; InvalidateTabs();
    }
    /// <summary>Reports whether the tab is disabled.</summary><param name="tabIndex">A valid zero-based index.</param><returns>False initially.</returns>
    public bool IsTabDisabled(int tabIndex) { ThrowIfDisposed(); return Item(tabIndex).Disabled; }
    /// <summary>Changes a tab's disabled state without changing the current index.</summary><param name="tabIndex">A valid zero-based index.</param><param name="value">The new state.</param>
    public void SetTabDisabled(int tabIndex, bool value) { EnsureMutable(); var tab = Item(tabIndex); if (tab.Disabled == value) return; tab.Disabled = value; ResetInteraction(); InvalidateTabs(); }
    /// <summary>Reports whether the tab is hidden.</summary><param name="tabIndex">A valid zero-based index.</param><returns>False initially.</returns>
    public bool IsTabHidden(int tabIndex) { ThrowIfDisposed(); return Item(tabIndex).Hidden; }
    /// <summary>Changes a tab's hidden state without changing the current index.</summary><param name="tabIndex">A valid zero-based index.</param><param name="value">The new state.</param>
    public void SetTabHidden(int tabIndex, bool value) { EnsureMutable(); var tab = Item(tabIndex); if (tab.Hidden == value) return; tab.Hidden = value; ResetInteraction(); InvalidateTabs(); }
    /// <summary>Stores runtime-only metadata with an exact compile-time type.</summary><typeparam name="T">The exact value type reused when reading.</typeparam><param name="tabIndex">A valid zero-based index.</param><param name="value">Borrowed value; never copied or disposed.</param>
    public void SetTabMetadata<T>(int tabIndex, T value) { EnsureMutable(); Item(tabIndex).Metadata = new MetadataValue<T>(value); }
    /// <summary>Gets the exact typed runtime metadata stored on a tab.</summary><typeparam name="T">The exact stored type.</typeparam><param name="tabIndex">A valid zero-based index.</param><returns>The stored value, including typed null.</returns><exception cref="KeyNotFoundException">The requested metadata type is absent.</exception>
    public T GetTabMetadata<T>(int tabIndex) { ThrowIfDisposed(); return Item(tabIndex).Metadata is MetadataValue<T> typed ? typed.Value : throw new KeyNotFoundException("Tab metadata type is absent."); }
    private void WatchIcon(Texture? icon)
    {
        if (icon is null) return; ObjectDisposedException.ThrowIf(icon.IsDisposed, icon);
        if (_icons.TryGetValue(icon, out var uses)) { _icons[icon] = uses + 1; return; }
        _icons.Add(icon, 1); icon.Changed += IconChanged; icon.Disposed += IconDisposed;
    }
    private void UnwatchIcon(Texture? icon)
    {
        if (icon is null || !_icons.TryGetValue(icon, out var uses)) return;
        if (uses > 1) { _icons[icon] = uses - 1; return; }
        _icons.Remove(icon); icon.Changed -= IconChanged; icon.Disposed -= IconDisposed;
    }
    private void IconChanged(Resource _) { if (!IsDisposed) InvalidateTabs(); }
    private void IconDisposed(ElectronObject resource)
    {
        var icon = (Texture)resource; _icons.Remove(icon); icon.Changed -= IconChanged; icon.Disposed -= IconDisposed;
        foreach (var tab in _tabs) { if (ReferenceEquals(tab.Icon, icon)) tab.Icon = null; if (ReferenceEquals(tab.ButtonIcon, icon)) tab.ButtonIcon = null; }
        if (!IsDisposed) InvalidateTabs();
    }
    private void ReleaseTab(Tab tab) { UnwatchIcon(tab.Icon); UnwatchIcon(tab.ButtonIcon); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(TabBar) ? CreateTabBar : base.CreateSceneInstanceFactory();
    private static Node CreateTabBar() => new TabBar();
    /// <inheritdoc />
    protected override void ValidateMutation() { if (_building) throw new InvalidOperationException("Tab shaping callbacks cannot edit their strip."); base.ValidateMutation(); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (_building) throw new InvalidOperationException("Tab shaping callbacks cannot dispose their strip."); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { foreach (var tab in _tabs) ReleaseTab(tab); _tabs.Clear(); ActiveTabRearranged = TabButtonPressed = TabChanged = TabClicked = TabClosePressed = TabHovered = TabRMBClicked = TabSelected = null; }
        base.Dispose(disposing);
    }
}
