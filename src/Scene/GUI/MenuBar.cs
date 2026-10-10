namespace Electron2D;

/// <summary>Displays a themed horizontal menu strip for its direct ordinary PopupMenu children.</summary>
/// <remarks>Menus retain child identity across reordering. Titles follow each popup's Title or Name unless
/// explicitly overridden. Header scene fields follow their popup when packing prunes siblings. Popup nodes retain
/// normal scene ownership. Presentation uses the embedding viewport;
/// system global menus require a native menu backend. Attached access requires the scene owner thread.</remarks>
public partial class MenuBar : Control
{
    private sealed class Menu(PopupMenu popup, MenuBar owner)
    {
        internal readonly PopupMenu Popup = popup;
        internal readonly TextLayout Layout = new();
        internal string? Title { get => Popup.MenuBarTitle; set => Popup.MenuBarTitle = value; }
        internal string Tooltip { get => Popup.MenuBarTooltip; set => Popup.MenuBarTooltip = value; }
        internal bool Disabled { get => Popup.MenuBarDisabled; set => Popup.MenuBarDisabled = value; }
        internal bool Hidden { get => Popup.MenuBarHidden; set => Popup.MenuBarHidden = value; }
        internal Rect2 Rect;
        internal readonly Action Changed = owner.InvalidateMenus;
        internal readonly Action<Node> Renamed = _ => owner.InvalidateMenus();
        internal readonly Action Visibility = () => owner.PopupVisibility(popup);
    }
    private readonly List<Menu> _menus = [];
    private Menu? _active;
    private int _hover = -1, _selected = -1;
    private bool _flat, _switchOnHover = true, _disableShortcuts, _syncing, _syncAgain, _disposing, _opening;
    private string _language = "";
    private TextDirection _textDirection;
    /// <summary>Creates an empty menu strip with accessibility focus and shortcut input enabled.</summary>
    public MenuBar() { FocusMode = FocusMode.Accessibility; ShortcutInputEnabled = true; }
    /// <summary>Gets or sets suppression of item decorations while retaining text and layout margins.</summary>
    /// <value>False initially.</value>
    public bool Flat { get { CheckMenus(); return _flat; } set { EnsureMutable(); if (_flat == value) return; _flat = value; QueueRedraw(); } }
    /// <summary>Gets or sets the shaping language; empty uses the current translation locale.</summary>
    /// <value>Empty initially. NUL is rejected.</value>
    public string Language
    {
        get { CheckMenus(); return _language; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (value.Contains('\0')) throw new ArgumentException("Language must not contain NUL.", nameof(value)); if (_language == value) return; _language = value; InvalidateMenus(); }
    }
    /// <summary>Gets or sets the base writing direction independently of visual layout direction.</summary>
    /// <value>Auto initially; Inherited follows layout direction.</value>
    public TextDirection TextDirection
    {
        get { CheckMenus(); return _textDirection; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_textDirection == value) return; _textDirection = value; InvalidateMenus(); }
    }
    /// <summary>Gets or sets hover switching while a menu in this strip is open.</summary>
    /// <value>True initially. Hidden, disabled and obscured targets are skipped.</value>
    public bool SwitchOnHover { get { CheckMenus(); return _switchOnHover; } set { EnsureMutable(); _switchOnHover = value; } }
    private void CheckMenus() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private Menu At(int menu) { CheckMenus(); if ((uint)menu >= (uint)_menus.Count) throw new ArgumentOutOfRangeException(nameof(menu)); return _menus[menu]; }
    private static string DefaultTitle(Menu menu) => menu.Popup.Title.Length == 0 ? menu.Popup.Name : menu.Popup.Title;
    /// <summary>Returns the number of direct ordinary PopupMenu children, including hidden and disabled menus.</summary>
    /// <returns>The current menu count.</returns>
    public int GetMenuCount() { CheckMenus(); return _menus.Count; }
    /// <summary>Returns a borrowed popup without changing its parent or lifetime.</summary>
    /// <param name="menu">A valid zero-based menu index.</param><returns>The corresponding child.</returns>
    public PopupMenu GetMenuPopup(int menu) => At(menu).Popup;
    /// <summary>Returns the explicit title or the child's current Title/Name.</summary>
    /// <param name="menu">A valid menu index.</param><returns>The untranslated title.</returns>
    public string GetMenuTitle(int menu) { var item = At(menu); return item.Title ?? DefaultTitle(item); }
    /// <summary>Overrides one menu's title; matching the child's current Title/Name restores automatic naming.</summary>
    /// <param name="menu">A valid menu index.</param><param name="title">Nonnull source text.</param>
    public void SetMenuTitle(int menu, string title) { EnsureMutable(); ArgumentNullException.ThrowIfNull(title); var item = At(menu); item.Title = title == DefaultTitle(item) ? null : title; InvalidateMenus(); }
    /// <summary>Returns one menu's tooltip.</summary><param name="menu">A valid menu index.</param><returns>Empty initially.</returns>
    public string GetMenuTooltip(int menu) => At(menu).Tooltip;
    /// <summary>Changes one menu's tooltip.</summary><param name="menu">A valid menu index.</param><param name="tooltip">Nonnull source text.</param>
    public void SetMenuTooltip(int menu, string tooltip) { EnsureMutable(); ArgumentNullException.ThrowIfNull(tooltip); At(menu).Tooltip = tooltip; }
    /// <summary>Returns whether user opening and shortcuts are disabled for one menu.</summary><param name="menu">A valid menu index.</param><returns>False initially.</returns>
    public bool IsMenuDisabled(int menu) => At(menu).Disabled;
    /// <summary>Changes the disabled flag and closes this menu if necessary.</summary><param name="menu">A valid menu index.</param><param name="disabled">Whether interaction is disabled.</param>
    public void SetMenuDisabled(int menu, bool disabled) { EnsureMutable(); var item = At(menu); if (item.Disabled == disabled) return; item.Disabled = disabled; MenuEligibilityChanged(item); }
    /// <summary>Returns whether one menu is omitted from the strip and shortcuts.</summary><param name="menu">A valid menu index.</param><returns>False initially.</returns>
    public bool IsMenuHidden(int menu) => At(menu).Hidden;
    /// <summary>Changes header visibility without changing child membership.</summary><param name="menu">A valid menu index.</param><param name="hidden">Whether the header is hidden.</param>
    public void SetMenuHidden(int menu, bool hidden) { EnsureMutable(); var item = At(menu); if (item.Hidden == hidden) return; item.Hidden = hidden; MenuEligibilityChanged(item); }
    /// <summary>Reports whether this strip currently uses a system global menu.</summary>
    /// <returns>False for the current embedded menu backend.</returns>
    public bool IsNativeMenu() { CheckMenus(); return false; }
    /// <summary>Disables the owner shortcut stage without changing individual popup item records.</summary>
    /// <param name="disabled">Whether shortcuts are disabled.</param>
    public void SetDisableShortcuts(bool disabled) { EnsureMutable(); _disableShortcuts = disabled; }
    internal void ValidatePopupHeaderMutation() => EnsureMutable();
    internal void PopupHeaderChanged(PopupMenu popup) { var index = Find(popup); if (index >= 0) MenuEligibilityChanged(_menus[index]); }
    private void MenuEligibilityChanged(Menu item)
    {
        List<Exception>? errors = null;
        try { if ((item.Disabled || item.Hidden) && item.Popup.Visible) item.Popup.Hide(); } catch (Exception e) { CollectException(ref errors, e); }
        try { InvalidateMenus(); } catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Menu eligibility callbacks failed.", errors);
    }
    private int Find(PopupMenu popup) { for (var i = 0; i < _menus.Count; i++) if (_menus[i].Popup == popup) return i; return -1; }
    private void SynchronizeMenus()
    {
        if (_disposing) return; if (_syncing) { _syncAgain = true; return; }
        _syncing = true; List<Exception>? errors = null;
        try
        {
            for (var pass = 0; pass < 64 && !IsDisposed; pass++)
            {
                _syncAgain = false;
                for (var i = _menus.Count - 1; i >= 0; i--)
                {
                    var item = _menus[i]; if (!item.Popup.IsDisposed && item.Popup.Parent == this && !item.Popup.IsInternalChild) continue;
                    _menus.RemoveAt(i); Unwatch(item); if (_active == item) _active = null;
                    try { if (!item.Popup.IsDisposed && item.Popup.Visible) item.Popup.Hide(); } catch (Exception e) { CollectException(ref errors, e); }
                    if (IsDisposed) break;
                }
                if (IsDisposed) break;
                var desired = 0;
                for (var i = 0; i < Children.Count; i++)
                {
                    if (Children[i] is not PopupMenu popup || popup.IsDisposed || popup.IsInternalChild) continue;
                    var index = Find(popup);
                    if (index < 0)
                    {
                        var item = new Menu(popup, this); _menus.Insert(desired, item);
                        popup.TitleChanged += item.Changed; popup.Renamed += item.Renamed; popup.VisibilityChanged += item.Visibility;
                    }
                    else if (index != desired) { var item = _menus[index]; _menus.RemoveAt(index); _menus.Insert(desired, item); }
                    desired++;
                }
                _hover = -1; _selected = _active is null ? -1 : _menus.IndexOf(_active); SetInternalProcessing(_active is not null, false);
                try { InvalidateMenus(); } catch (Exception e) { CollectException(ref errors, e); }
                try { NotifyPropertyListChanged(); } catch (Exception e) { CollectException(ref errors, e); }
                if (!_syncAgain) break;
                if (pass == 63) CollectException(ref errors, new InvalidOperationException("Menu child synchronization did not settle."));
            }
        }
        finally { _syncing = false; }
        ThrowCollected("Menu child synchronization failed.", errors);
    }
    private static void Unwatch(Menu item) { item.Popup.TitleChanged -= item.Changed; item.Popup.Renamed -= item.Renamed; item.Popup.VisibilityChanged -= item.Visibility; }
    private void PopupVisibility(PopupMenu popup)
    {
        if (IsDisposed || _disposing) return;
        var index = Find(popup); if (index < 0) return;
        List<Exception>? errors = null;
        if (popup.Visible)
        {
            var previous = _active; _active = _menus[index]; _selected = index;
            try { if (previous != _active && previous?.Popup is { IsDisposed: false, Visible: true } old) old.Hide(); } catch (Exception e) { CollectException(ref errors, e); }
        }
        else if (_active?.Popup == popup) { _active = null; _hover = -1; }
        try { if (!IsDisposed) { SetInternalProcessing(_active is not null, false); QueueRedraw(); } } catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Menu visibility callbacks failed.", errors);
    }
    private void CloseMenus()
    {
        _active = null; _hover = -1; _selected = -1; SetInternalProcessing(false, false); List<Exception>? errors = null;
        // Callbacks can remove or reorder menus; retain a structural snapshot for this lifecycle boundary.
        foreach (var item in _menus.ToArray()) try { if (!item.Popup.IsDisposed && item.Popup.Visible) item.Popup.Hide(); } catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Menu close callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(MenuBar) ? CreateMenuBar : base.CreateSceneInstanceFactory();
    private static Node CreateMenuBar() => new MenuBar();
    /// <inheritdoc />
    protected override void ValidateMutation() { if (_building) throw new InvalidOperationException("Menu shaping callbacks cannot edit their strip."); base.ValidateMutation(); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (_building) throw new InvalidOperationException("Menu shaping callbacks cannot dispose their strip."); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _disposing = true; foreach (var item in _menus) { Unwatch(item); } _menus.Clear(); _active = null; }
        base.Dispose(disposing);
    }
}
