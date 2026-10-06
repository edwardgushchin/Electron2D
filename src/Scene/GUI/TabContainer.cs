namespace Electron2D;

/// <summary>Hosts direct child control pages beneath a themed selectable tab strip.</summary>
/// <remarks>Ordinary non-top-level controls define the tab order. Internal strip/button controls are owned;
/// icons, metadata and the weak popup binding are borrowed. Only the selected page is locally visible.
/// Attached access follows the scene owner thread; indexed scene settings precede child construction.</remarks>
public partial class TabContainer : Container
{
    /// <summary>Places the tab header above or below page content.</summary>
    public enum TabPosition
    {
        /// <summary>Header above content.</summary>
        Top = 0,
        /// <summary>Header below content.</summary>
        Bottom = 1,
        /// <summary>Exclusive upper bound.</summary>
        Max = 2
    }
    private sealed class Page(Control control) { internal readonly Control Control = control; internal string? Title; }
    private sealed class PendingTab { internal string? Title; internal Texture? Icon; internal bool Disabled, Hidden; }
    private readonly List<Page> _pages = [];
    private readonly List<PendingTab> _pending = [];
    private readonly List<Control> _paintPages = [];
    private readonly ContainerTabBar _bar;
    private readonly Button _popupButton;
    private WeakReference<Popup>? _popup;
    private bool _syncing, _painting, _paintAgain, _visibility, _disposing, _tabsVisible = true, _useHidden;
    private int _setupCurrent = -2;
    private TabPosition _position;
    /// <summary>Creates an empty page owner with internal tab strip and popup button.</summary>
    public TabContainer()
    {
        _bar = new(this) { Name = "_tabs", CloseWithMiddleMouse = false, UseParentMaterial = true };
        _popupButton = new Button { Name = "_popup", Visible = false, Flat = true, ActionMode = ButtonActionMode.ButtonPress, UseParentMaterial = true };
        AddChild(_bar, InternalMode.Front); AddChild(_popupButton, InternalMode.Front);
        _bar.TabSelected += Selected; _bar.TabChanged += Changed; _bar.TabClicked += i => TabClicked?.Invoke(i); _bar.TabHovered += i => TabHovered?.Invoke(i); _bar.TabButtonPressed += i => TabButtonPressed?.Invoke(i);
        _bar.MinimumSizeChanged += LayoutChanged; _popupButton.Pressed += OpenPopup; _popupButton.MouseEntered += () => PopupHover(true); _popupButton.MouseExited += () => PopupHover(false);
        ChildAdded += ChildEntered; ChildRemoved += ChildLeft; ChildOrderChanged += OrderChanged; Resized += LayoutChanged;
    }
    private void CheckTabs() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    /// <summary>Gets or sets the selected page index; detached assignments are applied on tree entry.</summary>
    /// <value>-1 with no pages; first page otherwise. -1 requires deselection or no available tab.</value>
    public int CurrentTab { get { CheckTabs(); return _bar.CurrentTab; } set { EnsureMutable(); if (!IsInsideTree) { if (value < -1 || value >= 65536) throw new ArgumentOutOfRangeException(nameof(value)); _setupCurrent = value; return; } _bar.CurrentTab = value; } }
    /// <summary>Gets or sets the header position.</summary><value>Top initially; Max is invalid.</value>
    public TabPosition TabsPosition { get { CheckTabs(); return _position; } set { EnsureMutable(); if (value is < TabPosition.Top or >= TabPosition.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_position == value) return; _position = value; LayoutChanged(); } }
    /// <summary>Gets or sets whether the header is shown.</summary><value>True initially; hidden headers leave the page panel.</value>
    public bool TabsVisible { get { CheckTabs(); return _tabsVisible; } set { EnsureMutable(); if (_tabsVisible == value) return; _tabsVisible = value; _bar.Visible = value; _popupButton.Visible = value && GetPopup() != null; LayoutChanged(); } }
    /// <summary>Gets or sets whether locally hidden pages contribute to the minimum size.</summary><value>False initially.</value>
    public bool UseHiddenTabsForMinSize { get { CheckTabs(); return _useHidden; } set { EnsureMutable(); if (_useHidden == value) return; _useHidden = value; LayoutChanged(); } }
    /// <summary>Gets the obsolete front-order flag; assigning it has no effect because tabs always draw in front.</summary><value>Always false.</value>
    public bool AllTabsInFront { get { CheckTabs(); return false; } set { EnsureMutable(); } }
    /// <summary>Gets or sets the internal strip ClipTabs policy.</summary><value>true initially.</value>
    public bool ClipTabs { get { CheckTabs(); return _bar.ClipTabs; } set { EnsureMutable(); _bar.ClipTabs = value; LayoutChanged(); } }
    /// <summary>Gets or sets the internal strip DeselectEnabled policy.</summary><value>false initially.</value>
    public bool DeselectEnabled { get { CheckTabs(); return _bar.DeselectEnabled; } set { EnsureMutable(); _bar.DeselectEnabled = value; LayoutChanged(); } }
    /// <summary>Gets or sets the internal strip DragToRearrangeEnabled policy.</summary><value>false initially.</value>
    public bool DragToRearrangeEnabled { get { CheckTabs(); return _bar.DragToRearrangeEnabled; } set { EnsureMutable(); _bar.DragToRearrangeEnabled = value; LayoutChanged(); } }
    /// <summary>Gets or sets the internal strip SwitchOnDragHover policy.</summary><value>true initially.</value>
    public bool SwitchOnDragHover { get { CheckTabs(); return _bar.SwitchOnDragHover; } set { EnsureMutable(); _bar.SwitchOnDragHover = value; LayoutChanged(); } }
    /// <summary>Gets or sets the internal strip TabsRearrangeGroup policy.</summary><value>-1 initially.</value>
    public int TabsRearrangeGroup { get { CheckTabs(); return _bar.TabsRearrangeGroup; } set { EnsureMutable(); _bar.TabsRearrangeGroup = value; LayoutChanged(); } }
    /// <summary>Gets or sets the internal strip TabAlignment policy.</summary><value>Left initially.</value>
    public TabBar.AlignmentMode TabAlignment { get { CheckTabs(); return _bar.TabAlignment; } set { EnsureMutable(); _bar.TabAlignment = value; LayoutChanged(); } }
    /// <summary>Gets or sets the internal strip TabFocusMode policy.</summary><value>All initially.</value>
    public FocusMode TabFocusMode { get { CheckTabs(); return _bar.FocusMode; } set { EnsureMutable(); _bar.FocusMode = value; LayoutChanged(); } }
    /// <summary>Occurs when a same-container drop rearranges its selected page.</summary>
    public event Action<int>? ActiveTabRearranged;
    /// <summary>Occurs when a tab auxiliary button is activated.</summary>
    public event Action<int>? TabButtonPressed;
    /// <summary>Occurs when selection changes after page visibility commits.</summary>
    public event Action<int>? TabChanged;
    /// <summary>Occurs when a tab is clicked.</summary>
    public event Action<int>? TabClicked;
    /// <summary>Occurs when pointer enters a tab.</summary>
    public event Action<int>? TabHovered;
    /// <summary>Occurs when a valid assignment selects a tab, including an equal assignment.</summary>
    public event Action<int>? TabSelected;
    /// <summary>Occurs immediately before the configured popup opens.</summary>
    public event Action? PrePopupPressed;
    /// <summary>Gets the borrowed internal tab strip. Page count and ordering are owned by this container.</summary><returns>The stable internal strip.</returns>
    public TabBar GetTabBar() { CheckTabs(); return _bar; }
    /// <summary>Gets the number of ordinary non-top-level child pages.</summary><returns>The page count.</returns>
    public int GetTabCount() { CheckTabs(); SyncPages(); return _pages.Count; }
    /// <summary>Gets a borrowed page by index.</summary><param name="tabIndex">An index; negative or unavailable indices return null.</param><returns>The direct child or null.</returns>
    public Control? GetTabControl(int tabIndex) { CheckTabs(); SyncPages(); return (uint)tabIndex < (uint)_pages.Count ? _pages[tabIndex].Control : null; }
    /// <summary>Gets the selected borrowed page.</summary><returns>The selected control or null.</returns>
    public Control? GetCurrentTabControl() => GetTabControl(CurrentTab);
    /// <summary>Gets the selection preceding the latest assignment.</summary><returns>The previous index or -1.</returns>
    public int GetPreviousTab() { CheckTabs(); return _bar.GetPreviousTab(); }
    /// <summary>Finds the index of a direct page.</summary><param name="control">A live borrowed control.</param><returns>The page index or -1.</returns>
    public int GetTabIdxFromControl(Control control) { CheckTabs(); ArgumentNullException.ThrowIfNull(control); SyncPages(); return FindPage(control); }
    /// <summary>Finds a tab from a point in the strip's local coordinates.</summary><param name="point">Finite strip-local coordinates.</param><returns>The visible tab index or -1.</returns>
    public int GetTabIdxAtPoint(Vector2 point) { CheckTabs(); return _bar.GetTabIdxAtPoint(point); }
    /// <summary>Selects the next enabled visible tab without wrapping.</summary><returns>Whether selection succeeded.</returns>
    public bool SelectNextAvailable() { EnsureMutable(); return _bar.SelectNextAvailable(); }
    /// <summary>Selects the preceding enabled visible tab without wrapping.</summary><returns>Whether selection succeeded.</returns>
    public bool SelectPreviousAvailable() { EnsureMutable(); return _bar.SelectPreviousAvailable(); }
    /// <summary>Gets a tab's Title setting.</summary><param name="tabIndex">A valid nonnegative index.</param><returns>The stored value; resources are borrowed.</returns>
    public string GetTabTitle(int tabIndex) { CheckTabs(); return _bar.GetTabTitle(tabIndex); }
    /// <summary>Gets a tab's Tooltip setting.</summary><param name="tabIndex">A valid nonnegative index.</param><returns>The stored value; resources are borrowed.</returns>
    public string GetTabTooltip(int tabIndex) { CheckTabs(); return _bar.GetTabTooltip(tabIndex); }
    /// <summary>Sets a tab's Tooltip setting.</summary><param name="tabIndex">A valid nonnegative index.</param><param name="value">The new value; resources remain borrowed.</param>
    public void SetTabTooltip(int tabIndex, string value) { EnsureMutable(); _bar.SetTabTooltip(tabIndex, value); LayoutChanged(); }
    /// <summary>Gets a tab's Icon setting.</summary><param name="tabIndex">A valid nonnegative index.</param><returns>The stored value; resources are borrowed.</returns>
    public Texture? GetTabIcon(int tabIndex) { CheckTabs(); return _bar.GetTabIcon(tabIndex); }
    /// <summary>Gets a tab's IconMaxWidth setting.</summary><param name="tabIndex">A valid nonnegative index.</param><returns>The stored value; resources are borrowed.</returns>
    public int GetTabIconMaxWidth(int tabIndex) { CheckTabs(); return _bar.GetTabIconMaxWidth(tabIndex); }
    /// <summary>Sets a tab's IconMaxWidth setting.</summary><param name="tabIndex">A valid nonnegative index.</param><param name="value">The new value; resources remain borrowed.</param>
    public void SetTabIconMaxWidth(int tabIndex, int value) { EnsureMutable(); _bar.SetTabIconMaxWidth(tabIndex, value); LayoutChanged(); }
    /// <summary>Gets a tab's ButtonIcon setting.</summary><param name="tabIndex">A valid nonnegative index.</param><returns>The stored value; resources are borrowed.</returns>
    public Texture? GetTabButtonIcon(int tabIndex) { CheckTabs(); return _bar.GetTabButtonIcon(tabIndex); }
    /// <summary>Sets a tab's ButtonIcon setting.</summary><param name="tabIndex">A valid nonnegative index.</param><param name="value">The new value; resources remain borrowed.</param>
    public void SetTabButtonIcon(int tabIndex, Texture? value) { EnsureMutable(); _bar.SetTabButtonIcon(tabIndex, value); LayoutChanged(); }
    /// <summary>Reports a tab's Disabled flag.</summary><param name="tabIndex">A valid index.</param><returns>The stored flag.</returns>
    public bool IsTabDisabled(int tabIndex) { CheckTabs(); return _bar.IsTabDisabled(tabIndex); }
    /// <summary>Reports a tab's Hidden flag.</summary><param name="tabIndex">A valid index.</param><returns>The stored flag.</returns>
    public bool IsTabHidden(int tabIndex) { CheckTabs(); return _bar.IsTabHidden(tabIndex); }
    /// <summary>Sets a source title, retaining an override until it equals the child name.</summary><param name="tabIndex">A nonnegative index; may be pending before Ready.</param><param name="title">Nonnull title.</param>
    public void SetTabTitle(int tabIndex, string title) { EnsureMutable(); ArgumentNullException.ThrowIfNull(title); if ((uint)tabIndex >= (uint)_pages.Count) { Pending(tabIndex).Title = title; return; } _pages[tabIndex].Title = title == _pages[tabIndex].Control.Name ? null : title; _bar.SetTabTitle(tabIndex, title); LayoutChanged(); }
    /// <summary>Sets a borrowed icon, including pending indexed scene configuration.</summary><param name="tabIndex">A nonnegative index.</param><param name="icon">A live borrowed texture or null.</param>
    public void SetTabIcon(int tabIndex, Texture? icon) { EnsureMutable(); if (icon?.IsDisposed == true) throw new ObjectDisposedException(nameof(icon)); if ((uint)tabIndex >= (uint)_pages.Count) { Pending(tabIndex).Icon = icon; return; } _bar.SetTabIcon(tabIndex, icon); LayoutChanged(); }
    /// <summary>Sets a disabled flag; programmatic selection remains permitted.</summary><param name="tabIndex">A nonnegative index.</param><param name="disabled">Whether user selection is disabled.</param>
    public void SetTabDisabled(int tabIndex, bool disabled) { EnsureMutable(); if ((uint)tabIndex >= (uint)_pages.Count) { Pending(tabIndex).Disabled = disabled; return; } _bar.SetTabDisabled(tabIndex, disabled); LayoutChanged(); }
    /// <summary>Sets header visibility, preserving the page record and applying selection/visibility policy.</summary><param name="tabIndex">A nonnegative index.</param><param name="hidden">Whether the tab is hidden.</param>
    public void SetTabHidden(int tabIndex, bool hidden) { EnsureMutable(); if ((uint)tabIndex >= (uint)_pages.Count) { Pending(tabIndex).Hidden = hidden; return; } _bar.SetTabHidden(tabIndex, hidden); _pages[tabIndex].Control.Hide(); Paint(); LayoutChanged(); }
    /// <summary>Sets exact generic runtime metadata.</summary><typeparam name="T">The stored value type.</typeparam><param name="tabIndex">A valid index.</param><param name="value">The borrowed value, including typed null.</param>
    public void SetTabMetadata<T>(int tabIndex, T value) { EnsureMutable(); _bar.SetTabMetadata(tabIndex, value); }
    /// <summary>Gets metadata stored with the same exact generic type.</summary><typeparam name="T">The stored value type.</typeparam><param name="tabIndex">A valid index.</param><returns>The borrowed value.</returns><exception cref="KeyNotFoundException">No metadata of the exact type is stored.</exception>
    public T GetTabMetadata<T>(int tabIndex) { CheckTabs(); return _bar.GetTabMetadata<T>(tabIndex); }
    private PendingTab Pending(int index) { if (index < 0 || index >= 65536 || IsNodeReady) throw new ArgumentOutOfRangeException(nameof(index)); while (_pending.Count <= index) _pending.Add(new()); return _pending[index]; }
    // ponytail: a linear identity scan survives scene edits; add an index only for measured large-page workloads.
    private int FindPage(Control control) { for (var i = 0; i < _pages.Count; i++) if (_pages[i].Control == control) return i; return -1; }
    private void ChildEntered(Node owner, Node child) => SyncPages();
    private void ChildLeft(Node owner, Node child) => SyncPages();
    private void OrderChanged(Node owner) => SyncPages();
    private void RenamedPage(Node node) { if (node is Control control) { var i = FindPage(control); if (i >= 0) { _bar.SetTabTitle(i, _pages[i].Title ?? control.Name); LayoutChanged(); } } }
    private bool _syncAgain;
    private void SyncPages()
    {
        if (_disposing || _bar is null) return; if (_syncing) { _syncAgain = true; return; }
        _syncing = true; List<Exception>? errors = null;
        try { for (var pass = 0; pass < 64; pass++) { _syncAgain = false; try { SyncPagesCore(); } catch (Exception e) { CollectException(ref errors, e); } if (!_syncAgain) break; if (pass == 63) CollectException(ref errors, new InvalidOperationException("Tab page synchronization did not settle.")); } }
        finally { _syncing = false; }
        ThrowCollected("Tab page synchronization failed.", errors);
    }
    private void SyncPagesCore()
    {
        var changed = false; List<Exception>? errors = null;
        {
            for (var i = _pages.Count - 1; i >= 0; i--) { var c = _pages[i].Control; if (c.Parent == this && !c.TopLevel && !c.IsInternalChild) continue; c.Renamed -= RenamedPage; c.VisibilityChanged -= PageVisibility; _pages.RemoveAt(i); changed = true; try { _bar.RemoveTab(i); } catch (Exception e) { CollectException(ref errors, e); } }
            var desired = 0;
            for (var i = 0; i < Children.Count; i++)
            {
                if (Children[i] is not Control c || c.TopLevel) continue; var index = FindPage(c);
                if (index < 0)
                {
                    var page = new Page(c); _pages.Add(page); changed = true; c.Renamed += RenamedPage; c.VisibilityChanged += PageVisibility;
                    _visibility = true; try { c.Hide(); } catch (Exception e) { CollectException(ref errors, e); } finally { _visibility = false; }
                    try { _bar.AddTab(c.Name); } catch (Exception e) { CollectException(ref errors, e); }
                    index = _pages.Count - 1;
                    if (index < _pending.Count) { var pending = _pending[index]; try { if (pending.Title is not null) SetTabTitle(index, pending.Title); SetTabIcon(index, pending.Icon is { IsDisposed: false } ? pending.Icon : null); SetTabDisabled(index, pending.Disabled); _bar.SetTabHidden(index, pending.Hidden); } catch (Exception e) { CollectException(ref errors, e); } }
                }
                if (index != desired) { changed = true; var page = _pages[index]; _pages.RemoveAt(index); _pages.Insert(desired, page); try { _bar.MoveTab(index, desired); } catch (Exception e) { CollectException(ref errors, e); } }
                desired++;
            }
            if (_pending.Count != 0 && _pages.Count >= _pending.Count) _pending.Clear();
            try { if (changed) { Paint(); LayoutChanged(); NotifyPropertyListChanged(); } } catch (Exception e) { CollectException(ref errors, e); }
        }
        ThrowCollected("Tab page synchronization failed.", errors);
    }
    private void Selected(int index) { List<Exception>? errors = null; try { Paint(); } catch (Exception e) { CollectException(ref errors, e); } try { TabSelected?.Invoke(index); } catch (Exception e) { CollectException(ref errors, e); } ThrowCollected("Tab selection callbacks failed.", errors); }
    private void Changed(int index) { List<Exception>? errors = null; try { Paint(); } catch (Exception e) { CollectException(ref errors, e); } try { TabChanged?.Invoke(index); } catch (Exception e) { CollectException(ref errors, e); } ThrowCollected("Tab change callbacks failed.", errors); }
    private void PageVisibility(CanvasItem item)
    {
        if (_visibility || _disposing || item is not Control control) return; var index = FindPage(control); if (index < 0) return; _visibility = true;
        try { if (control.Visible && _bar.CurrentTab != index) _bar.CurrentTab = index; else if (!control.Visible && _bar.CurrentTab == index) { if (_bar.DeselectEnabled) _bar.CurrentTab = -1; else if (_pages.Count == 1) control.Show(); else if (!_bar.SelectNextAvailable() && !_bar.SelectPreviousAvailable()) _bar.CurrentTab = -1; } }
        finally { _visibility = false; }
        Paint();
    }
    private void LayoutChanged() { if (_disposing || IsDisposed || _bar is null || _themeUpdating) return; UpdateMinimumSize(); QueueSort(); QueueRedraw(); }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(TabContainer) ? CreateTabContainer : base.CreateSceneInstanceFactory();
    private static Node CreateTabContainer() => new TabContainer();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _disposing = true; foreach (var page in _pages) { page.Control.Renamed -= RenamedPage; page.Control.VisibilityChanged -= PageVisibility; } if (GetPopup() is { } popup) popup.Disposed -= PopupDisposed; _pages.Clear(); _pending.Clear(); ActiveTabRearranged = null; TabChanged = null; TabSelected = null; TabClicked = null; TabHovered = null; TabButtonPressed = null; PrePopupPressed = null; } base.Dispose(disposing); }
}
