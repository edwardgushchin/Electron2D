namespace Electron2D;

public partial class MenuBar
{
    private bool Eligible(int index) => (uint)index < (uint)_menus.Count && !_menus[index].Hidden && !_menus[index].Disabled;
    private void OpenMenu(int index, bool keyboard, bool toggle = true)
    {
        if (!Eligible(index) || !IsInsideTree || GetViewport() is not { } viewport) return;
        if (_opening) throw new InvalidOperationException("Menu opening cannot reenter popup callbacks.");
        var item = _menus[index]; if (toggle && item.Popup.Visible) { item.Popup.Hide(); return; }
        _opening = true; List<Exception>? errors = null;
        try
        {
            var previous = _active; _active = null;
            try { if (previous != item && previous?.Popup is { IsDisposed: false, Visible: true } old) old.Hide(); } catch (Exception e) { CollectException(ref errors, e); }
            if (!IsDisposed && !item.Popup.IsDisposed && item.Popup.Parent == this && !item.Hidden && !item.Disabled && IsInsideTree)
            {
                MeasureMenus(); var transform = GetGlobalTransformWithCanvas(); var at = transform * item.Rect.Position; var size = transform.Scale * item.Rect.Size;
                if (viewport is Window { Embedder: not null } window && item.Popup.Embedder != viewport) at += (Vector2)window.Position;
                item.Popup.LayoutDirection = LayoutDirection;
                var popupSize = item.Popup.GetContentsMinimumSize().Max(new Vector2(Math.Abs(size.X), 0)).Ceil();
                at.Y += size.Y; if (IsLayoutRTL()) at.X += size.X - popupSize.X;
                _selected = _menus.IndexOf(item); _active = item;
                item.Popup.Popup(new((Vector2i)at, (Vector2i)popupSize));
                if (!IsDisposed && !item.Popup.IsDisposed && item.Popup.Visible && item.Popup.Parent == this)
                {
                    var first = -1;
                    if (keyboard) for (var i = 0; i < item.Popup.ItemCount; i++) if (!item.Popup.IsItemDisabled(i) && !item.Popup.IsItemSeparator(i)) { first = i; break; }
                    item.Popup.SetFocusedItem(first);
                }
            }
        }
        catch (Exception e) { CollectException(ref errors, e); }
        finally
        {
            _opening = false;
            try { if (!IsDisposed) { if (item.Popup.IsDisposed || !item.Popup.Visible || item.Popup.Parent != this) { if (_active == item) _active = null; } SetInternalProcessing(_active is not null, false); QueueRedraw(); } }
            catch (Exception e) { CollectException(ref errors, e); }
        }
        ThrowCollected("Menu opening callbacks failed.", errors);
    }
    private bool NavigateMenu(int direction)
    {
        if (_menus.Count == 0) return false;
        var start = _active is null ? _selected : _menus.IndexOf(_active);
        if (start < 0) start = direction > 0 ? -1 : 0;
        for (var n = 1; n <= _menus.Count; n++)
        {
            var candidate = (start + direction * n + _menus.Count * 2) % _menus.Count;
            if (!Eligible(candidate)) continue;
            _selected = candidate; _hover = candidate; OpenMenu(candidate, true, toggle: false); return true;
        }
        return false;
    }
    internal bool SwitchFromPopup(PopupMenu popup, InputEvent input)
    {
        if (_active?.Popup != popup) return false;
        if (input.IsActionPressed("ui_left", true, true)) return NavigateMenu(-1);
        if (input.IsActionPressed("ui_right", true, true)) return NavigateMenu(1);
        return false;
    }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent input)
    {
        base.OnGUIInput(input);
        if (input.IsActionPressed("ui_left", true, true)) { if (NavigateMenu(-1) && !IsDisposed) AcceptEvent(); }
        else if (input.IsActionPressed("ui_right", true, true)) { if (NavigateMenu(1) && !IsDisposed) AcceptEvent(); }
        else if (input.IsActionPressed("ui_accept", true, true) || input.IsActionPressed("ui_down", true, true))
        { if (!Eligible(_hover)) { _selected = -1; if (NavigateMenu(1) && !IsDisposed) AcceptEvent(); } else { OpenMenu(_hover, true); if (!IsDisposed) AcceptEvent(); } }
        else if (input is InputEventMouseMotion motion) { var index = MenuAt(motion.Position); if (_hover != index) { _hover = index; if (index >= 0) _selected = index; QueueRedraw(); } }
        else if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left or MouseButton.Right } button)
        { var index = MenuAt(button.Position); if (index >= 0) { _hover = index; _selected = index; OpenMenu(index, false); if (!IsDisposed) AcceptEvent(); } }
    }
    /// <inheritdoc />
    protected override void OnShortcutInput(InputEvent input)
    {
        if (_disableShortcuts || !IsVisibleInTree || !input.IsPressed()) return;
        var tree = Tree;
        // Activation callbacks may remove the current child; finish immediately after a matching command.
        for (var i = 0; i < _menus.Count; i++) if (Eligible(i) && _menus[i].Popup.ActivateItemByEvent(input)) { tree?.SetInputAsHandled(); return; }
    }
    private void HoverMenu()
    {
        if (!_switchOnHover || _active is null || !IsVisibleInTree || GetViewport()?.GUIGetHoveredControl() != this) return;
        var index = MenuAt(MakeCanvasPositionLocal(Tree!.GetGUIHoverPosition(GetViewport()!))); if (index < 0 || _menus[index] == _active) return;
        _hover = index; _selected = index; OpenMenu(index, false, toggle: false);
    }
}
