namespace Electron2D;

public partial class PopupMenu
{
    private int _gamepadDirection;
    private double _repeatRemaining;
    private Item? _pressedItem;
    private double _suspendRemaining;
    private Vector2 _lastPointer;
    private MouseButtonMask _initialButtons;
    private double _openedElapsed;
    private bool _grabbedOpen;
    private int ItemAt(float y) { Measure(); for (var i = 0; i < _items.Count; i++) { var item = _items[i]; if (item.Visible && y >= item.Rect.Position.Y && y < item.Rect.End.Y) return i; } return -1; }
    private bool Eligible(int index) => (uint)index < (uint)_items.Count && _items[index].Visible && !_items[index].Disabled && !_items[index].Separator;
    private void FocusItem(int index, bool signal)
    { if (!Eligible(index) || _focused == index) return; _focused = index; _view.QueueRedraw(); ScrollToItem(index); if (signal) IDFocused?.Invoke(_items[index].ID); }
    private void Navigate(int direction)
    { if (_items.Count == 0) return; for (var n = 1; n <= _items.Count; n++) { var candidate = (_focused + direction * n + _items.Count * 2) % _items.Count; if (Eligible(candidate)) { FocusItem(candidate, true); _view.GrabFocus(); return; } } }
    private void NavigateEvent(InputEvent input, int direction)
    { var joypad = input is InputEventJoypadButton or InputEventJoypadMotion; if (joypad && !Input.IsActionJustPressedByEvent(direction > 0 ? "ui_down" : "ui_up", input, true)) return; Navigate(direction); if (joypad) { _gamepadDirection = direction; _repeatRemaining = .5; } SetInputAsHandled(); }
    private void ActivateFocused() { if (Eligible(_focused)) { if (_items[_focused].Submenu is not null) OpenSubmenu(_focused, true); else Activate(_focused); } }
    /// <summary>Activates the first matching shortcut or accelerator, recursively searching submenus.</summary>
    /// <param name="inputEvent">A live borrowed event.</param><param name="forGlobalOnly">Restricts shortcut matching to global shortcuts.</param>
    /// <returns>True when an enabled item is activated.</returns>
    public bool ActivateItemByEvent(InputEvent inputEvent, bool forGlobalOnly = false)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(inputEvent); inputEvent.EnsureUsable(); if (!inputEvent.IsPressed()) return false;
        var code = inputEvent is InputEventKey key ? (key.Keycode == Key.None ? key.GetPhysicalKeycodeWithModifiers() : key.GetKeycodeWithModifiers()) : Key.None;
        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i]; if (item.Disabled || item.ShortcutDisabled || !item.AllowEcho && inputEvent.IsEcho()) continue;
            if (item.Shortcut is { IsDisposed: false } shortcut && (item.Global || !forGlobalOnly) && shortcut.MatchesEvent(inputEvent) || code != Key.None && item.Accelerator == code) { if (item.Separator) return false; Activate(i); return true; }
            if (item.Submenu is { IsDisposed: false } child && child.ActivateItemByEvent(inputEvent, forGlobalOnly)) return true;
        }
        return false;
    }
    private bool HidePolicy(Item item) => item.Checkable != 0 ? _hideChecks : item.MaxStates > 0 ? _hideStates : _hideItems;
    private void Activate(int index)
    {
        var item = Get(index); if (item.Separator || item.Disabled) return; var id = item.ID < 0 ? index : item.ID; List<Exception>? errors = null;
        if (HidePolicy(item))
        {
            for (var parent = Parent as PopupMenu; parent is not null && parent.HidePolicy(item); parent = parent.Parent as PopupMenu)
                try { parent.Hide(); } catch (Exception error) { CollectException(ref errors, error); }
            try { Hide(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        try { IDPressed?.Invoke(id); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) IndexPressed?.Invoke(index); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Menu activation callbacks failed.", errors);
    }
    private void ItemsInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion)
        {
            if (motion.Relative == Vector2.Zero) return; var index = ItemAt(motion.Position.Y); _lastPointer = motion.Position; if (_parentMenu is { } owner) owner._suspendRemaining = 0;
            if (!Eligible(index)) { _focused = -1; _pendingSubmenu = -1; _submenuTimer.Stop(); if (_activeSubmenu is not null) StartSubmenu(-1, motion.Relative); _view.QueueRedraw(); return; }
            FocusItem(index, false); _typeSearch = "";
            if (_activeSubmenu is not null && _activeSubmenu == _items[index].Submenu) { _suspendRemaining = 0; _pendingSubmenu = -1; _submenuTimer.Stop(); }
            else if (_items[index].Submenu is not null || _activeSubmenu is not null) StartSubmenu(index, motion.Relative);
        }
        else if (input is InputEventMouseButton button && (button.ButtonIndex == MouseButton.Left || (_initialButtons & Mask(button.ButtonIndex)) != 0))
        {
            var index = ItemAt(button.Position.Y);
            if (button.Pressed) { _grabbedOpen = false; _pressedItem = Eligible(index) ? _items[index] : null; FocusItem(index, false); }
            else { var grabbed = _grabbedOpen; _grabbedOpen = false; _initialButtons = 0; if (grabbed && _openedElapsed < .4) return; if (!Eligible(index)) return; if (!grabbed && _pressedItem is not null && _pressedItem != _items[index]) return; _pressedItem = null; if (_items[index].Submenu is not null) OpenSubmenu(index, false); else Activate(index); }
        }
        else if (input is InputEventScreenTouch touch)
        {
            var index = ItemAt(touch.Position.Y); if (touch.Pressed) { _pressedItem = Eligible(index) ? _items[index] : null; FocusItem(index, false); } else if (Eligible(index) && _pressedItem == _items[index] && !touch.Canceled) { _pressedItem = null; if (_items[index].Submenu is not null) OpenSubmenu(index, false); else Activate(index); }
        }
    }
    private static MouseButtonMask Mask(MouseButton button) => (MouseButtonMask)(1u << ((int)button - 1));
    /// <inheritdoc />
    protected override void OnInput(InputEvent inputEvent)
    {
        base.OnInput(inputEvent); if (!Visible) return;
        if (inputEvent.IsActionPressed("ui_down", true, true)) { NavigateEvent(inputEvent, 1); }
        else if (inputEvent.IsActionPressed("ui_up", true, true)) { NavigateEvent(inputEvent, -1); }
        else if (inputEvent.IsActionPressed("ui_left", true, true) && Parent is PopupMenu) { Hide(); SetInputAsHandled(); }
        else if (inputEvent.IsActionPressed("ui_right", true, true) && Eligible(_focused) && _items[_focused].Submenu is not null) { OpenSubmenu(_focused, true); SetInputAsHandled(); }
        else if (inputEvent.IsActionPressed("ui_accept", true, true)) { ActivateFocused(); SetInputAsHandled(); }
        else if (!(_searchVisible && _search.HasFocus()) && ActivateItemByEvent(inputEvent)) { SetInputAsHandled(); }
        else if (_allowSearch && !_searchVisible && inputEvent is InputEventKey { Pressed: true, Unicode: > 0 } key)
        {
            var text = char.ConvertFromUtf32((int)key.Unicode); _typeSearch = _typeRemaining > 0 && _typeSearch != text ? _typeSearch + text : text; _typeRemaining = 2;
            for (var n = 1; n <= _items.Count; n++) { var index = (_focused + n + _items.Count) % _items.Count; if (Eligible(index) && _items[index].DisplayText.StartsWith(_typeSearch, StringComparison.OrdinalIgnoreCase)) { FocusItem(index, true); SetInputAsHandled(); break; } }
        }
    }
    private void StartSubmenu(int index, Vector2 relative)
    {
        _pendingSubmenu = index; var delay = Parent is PopupMenu parent ? parent._submenuDelay : _submenuDelay;
        if (_activeSubmenu is { Visible: true } child)
        {
            var pointer = (Vector2)Position + (_scroll.Position + _view.Position + _lastPointer) * _graphScale; var left = child.Position.X < Position.X; var x = left ? child.Position.X + child.Size.X : child.Position.X; var top = (new Vector2(x, child.Position.Y) - pointer).Rotated(left ? -MathF.PI / 2 : MathF.PI / 2); var bottom = (new Vector2(x, child.Position.Y + child.Size.Y) - pointer).Rotated(left ? MathF.PI / 2 : -MathF.PI / 2); var toward = top.Dot(relative) > 0 && bottom.Dot(relative) > 0;
            if (toward) { if (_suspendRemaining <= 0) _suspendRemaining = .5; _submenuTimer.Stop(); return; }
            CloseSubmenu();
        }
        if (!Eligible(index) || _items[index].Submenu is null) { _pendingSubmenu = -1; return; }
        if (delay <= 0) OpenPendingSubmenu(); else { _submenuTimer.WaitTime = delay; _submenuTimer.Start(); }
    }
    private void OpenPendingSubmenu() { var pending = _pendingSubmenu; _pendingSubmenu = -1; if (Eligible(pending) && _focused == pending && _items[pending].Submenu is not null) OpenSubmenu(pending, false); }
    private void OpenSubmenu(int index, bool keyboard)
    {
        var item = Get(index); var child = GetItemSubmenuNode(index); if (child is null) throw new InvalidOperationException("Submenu is unavailable."); if (child.Visible) return;
        CloseSubmenu(); Measure(); child.Exclusive = false; child.SubmenuPopupDelay = _submenuDelay; child.ApplyGraphScale(_graphScale); child.Measure(); var childSize = (child._minimum * _graphScale).Ceil(); var area = GetUsableParentRect();
        var x = IsLayoutRTL() ? Position.X - (int)childSize.X : Position.X + Size.X; if (x + childSize.X > area.End.X) x = Position.X - (int)childSize.X; if (x < area.Position.X) x = Position.X + Size.X;
        var y = Position.Y + (int)((_scroll.Position.Y + item.Rect.Position.Y - _scroll.ScrollVertical - child.GetThemeStyleBox("panel")!.GetMargin(Side.Top)) * _graphScale);
        _activeSubmenu = child; child._parentMenu = this; child.Popup(new(new(x, y), (Vector2i)childSize)); if (keyboard) { child._focused = -1; child.Navigate(1); }
        child._view.QueueRedraw();
    }
    private PopupMenu? _parentMenu;
    private void CloseSubmenu() { _submenuTimer?.Stop(); _suspendRemaining = 0; var child = _activeSubmenu; _activeSubmenu = null; if (child is { IsDisposed: false, Visible: true }) child.Hide(); }
    private void ProcessMenu() { var delta = ProcessDeltaTime; _openedElapsed += delta; if (_gamepadDirection != 0) { if (!Input.IsActionPressed(_gamepadDirection > 0 ? "ui_down" : "ui_up")) _gamepadDirection = 0; else { _repeatRemaining -= delta; if (_repeatRemaining <= 0) { _repeatRemaining += .05; Navigate(_gamepadDirection); } } } if (_typeRemaining > 0) _typeRemaining -= delta; if (_suspendRemaining > 0) { _suspendRemaining -= delta; if (_suspendRemaining <= 0) { CloseSubmenu(); StartSubmenu(_pendingSubmenu, Vector2.Zero); } } }
    internal override bool AcceptEmbeddedPointer(Vector2 point, InputEvent input) => _parentMenu is not { IsDisposed: false, Visible: true } parent || EmbeddedHitRect.HasPoint(point) || !parent.EmbeddedHitRect.HasPoint(point);
}
