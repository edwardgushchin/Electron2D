namespace Electron2D;

/// <summary>A command button owning a themed popup menu and optional hover switching between related menu buttons.</summary>
/// <remarks>The internal popup is borrowed through GetPopup and remains owned by this button. Attach below an
/// embedding viewport for presentation. Commands use PopupMenu events; shortcuts and metadata follow that menu's contracts.</remarks>
public partial class MenuButton : Button
{
    private readonly PopupMenu _popup;
    private bool _switchOnHover, _disableShortcuts, _opening;
    /// <summary>Creates a flat, press-edge toggle button with accessibility focus and an owned popup.</summary>
    public MenuButton() : this("") { }
    /// <summary>Creates a command menu button with source text.</summary>
    /// <param name="text">Nonnull caption text.</param>
    public MenuButton(string text) : base(text)
    {
        Flat = true; ToggleMode = true; ActionMode = ButtonActionMode.ButtonPress; FocusMode = FocusMode.Accessibility; ShortcutInputEnabled = true;
        _popup = new PopupMenu { Name = "_menu_popup" }; AddChild(_popup, InternalMode.Front);
        _popup.VisibilityChanged += SyncPopupState; _popup.PopupHide += SyncPopupState;
    }
    /// <summary>Gets or sets switching to an enabled related menu button hovered while this menu is open.</summary>
    /// <value>False initially; both buttons must opt in.</value>
    public bool SwitchOnHover { get { CheckMenuButton(); return _switchOnHover; } set { EnsureMutable(); _switchOnHover = value; } }
    /// <summary>Gets or sets the owned popup's item count, retaining existing records.</summary>
    /// <value>Zero initially; the popup limit is 65536. New records are plain items with automatic IDs.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is outside the supported range.</exception>
    public int ItemCount
    {
        get { CheckMenuButton(); return _popup.ItemCount; }
        set
        {
            EnsureMutable(); if ((uint)value > 65536) throw new ArgumentOutOfRangeException(nameof(value)); if (_popup.ItemCount == value) return;
            List<Exception>? errors = null; try { _popup.ItemCount = value; } catch (Exception e) { CollectException(ref errors, e); }
            try { if (!IsDisposed) NotifyPropertyListChanged(); } catch (Exception e) { CollectException(ref errors, e); }
            ThrowCollected("Menu item count callbacks failed.", errors);
        }
    }
    /// <summary>Occurs before popup geometry and visibility change; handlers may populate the owned menu.</summary>
    public event Action? AboutToPopup;
    /// <summary>Returns the stable borrowed internal popup; preserve its ownership and lifetime.</summary>
    /// <returns>The command menu.</returns>
    public PopupMenu GetPopup() { CheckMenuButton(); return _popup; }
    /// <summary>Disables item shortcuts and the inherited button shortcut input stage.</summary>
    /// <param name="disabled">Whether shortcut input is disabled.</param>
    public void SetDisableShortcuts(bool disabled) { EnsureMutable(); _disableShortcuts = disabled; }
    private void CheckMenuButton() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void SyncPopupState() { if (!IsDisposed && !_popup.IsDisposed) ButtonPressed = _popup.Visible; }
    /// <summary>Shows the command menu below the button and focuses its first enabled nonseparator item for keyboard opening.</summary>
    /// <remarks>Detached buttons return without opening. The popup fits the embedding viewport and uses its own
    /// minimum width; RTL aligns its trailing edge. Pointer opening retains the popup's grabbed-click policy.</remarks>
    /// <exception cref="InvalidOperationException">Presentation reenters AboutToPopup.</exception>
    /// <exception cref="AggregateException">A presentation or state callback fails after required cleanup.</exception>
    public void ShowPopup()
    {
        EnsureMutable(); if (!IsInsideTree || GetViewport() is not { } viewport) return;
        if (_opening) throw new InvalidOperationException("Menu presentation cannot reenter AboutToPopup.");
        _opening = true; List<Exception>? errors = null;
        try
        {
            AboutToPopup?.Invoke(); if (IsDisposed || !IsInsideTree || _popup.IsDisposed) return;
            var transform = GetGlobalTransformWithCanvas(); var rect = new Rect2(transform.Origin, transform.Scale * Size); var at = rect.Position + new Vector2(0, rect.Size.Y);
            if (viewport is Window { Embedder: not null } window && _popup.Embedder != viewport) at += (Vector2)window.Position;
            _popup.LayoutDirection = LayoutDirection;
            var area = _popup.Embedder?.GetVisibleRect() ?? viewport.GetVisibleRect(); var available = Math.Max(0, (int)MathF.Floor(area.End.Y - at.Y));
            _popup.MaxSize = available >= 4 * rect.Size.Y ? new(0, Math.Max(available, _popup.MinSize.Y)) : Vector2i.Zero;
            if (IsLayoutRTL()) { var width = _popup.ShrinkWidth ? _popup.GetContentsMinimumSize().X : _popup.Size.X; at.X += rect.Size.X - width; }
            _popup.Position = (Vector2i)at; _popup.Popup();
            if (!_popup.IsDisposed && _popup.Visible && !WasPressedByMouse)
            {
                var first = -1; for (var i = 0; i < _popup.ItemCount; i++) if (!_popup.IsItemDisabled(i) && !_popup.IsItemSeparator(i)) { first = i; break; }
                _popup.SetFocusedItem(first);
            }
        }
        catch (Exception e) { CollectException(ref errors, e); }
        finally { _opening = false; try { SyncPopupState(); } catch (Exception e) { CollectException(ref errors, e); } }
        ThrowCollected("Menu presentation callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override void OnPressed() { if (_popup.Visible) _popup.Hide(); else ShowPopup(); }
    /// <inheritdoc />
    protected override void OnShortcutInput(InputEvent input)
    {
        if (_disableShortcuts) return;
        var tree = Tree;
        if (!Disabled && IsVisibleInTree && input.IsPressed() && _popup.ActivateItemByEvent(input)) { tree?.SetInputAsHandled(); return; }
        base.OnShortcutInput(input);
    }
    internal bool CanSwitchTo(MenuButton other) => !IsDisposed && !other.IsDisposed && other != this && other.Tree == Tree && other.IsVisibleInTree && other.CanProcess() && other.SwitchOnHover && !other.Disabled && Parent != null && other.Parent != null && (Parent.IsAncestorOf(other) || other.Parent.IsAncestorOf(_popup));
    private void SwitchMenu()
    {
        if (!_switchOnHover || !_popup.Visible || GetViewport()?.GUIGetHoveredControl() is not MenuButton other || !CanSwitchTo(other)) return;
        List<Exception>? errors = null; try { _popup.Hide(); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!other.IsDisposed && other.IsInsideTree) other.OnPressed(); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!other.IsDisposed && !other._popup.IsDisposed) other._popup.SetFocusedItem(-1); } catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Menu hover switch callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null; try { base.OnNotification(what); } catch (Exception e) { CollectException(ref errors, e); }
        if (!IsDisposed && _popup != null && !_popup.IsDisposed)
            try
            {
                if (what is NotificationLayoutDirectionChanged or NotificationTranslationChanged) _popup.LayoutDirection = LayoutDirection;
                else if (what == NotificationVisibilityChanged && !IsVisibleInTree || what == NotificationExitTree && _popup.Visible) _popup.Hide();
                else if (what == NotificationInternalProcess) SwitchMenu();
            }
            catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Menu button notification callbacks failed.", errors);
    }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings() { CheckMenuButton(); var own = base.GetConfigurationWarnings(); var popup = _popup.GetConfigurationWarnings(); return own.Length == 0 ? popup : popup.Length == 0 ? own : [.. own, .. popup]; }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(MenuButton) ? CreateMenuButton : base.CreateSceneInstanceFactory();
    private static Node CreateMenuButton() => new MenuButton();
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { AboutToPopup = null; _popup.VisibilityChanged -= SyncPopupState; _popup.PopupHide -= SyncPopupState; } base.Dispose(disposing); }
}
