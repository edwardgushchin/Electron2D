namespace Electron2D;

public partial class TabBar
{
    private sealed class TabDrag(TabBar source, Tab tab) : DragPayload
    {
        internal readonly TabBar Source = source;
        internal readonly Tab Item = tab;
    }
    private Vector2 _pointer;
    private int _buttonHover = -1, _closeHover = -1, _arrowHover;
    private Tab? _pressedTab;
    private bool _pressedClose, _dropVisible;
    private float _dropX;
    private int _gamepadDirection;
    private double _repeatRemaining;
    private void ResetInteraction() { _hover = _buttonHover = _closeHover = -1; _pressedTab = null; _dropVisible = false; if (!_hoverTimer.IsDisposed) _hoverTimer.Stop(); QueueRedraw(); }
    private void FocusLost() { if (IsDisposed) return; _gamepadDirection = 0; SetInternalProcessing(false, false); QueueRedraw(); }
    private void PointerExited() { if (IsDisposed) return; ResetInteraction(); _arrowHover = 0; }
    private void UpdateHover(Vector2 point)
    {
        _pointer = point; var index = GetTabIdxAtPoint(point); var changed = _hover != index; _hover = index;
        _buttonHover = index >= 0 && _tabs[index].ButtonRect.HasPoint(point) ? index : -1;
        _closeHover = index >= 0 && !_tabs[index].Disabled && _tabs[index].CloseRect.HasPoint(point) ? index : -1;
        _arrowHover = _buttonsVisible ? _backRect.HasPoint(point) ? -1 : _nextRect.HasPoint(point) ? 1 : 0 : 0;
        if (changed) { _dirty = true; QueueRedraw(); if (index >= 0) TabHovered?.Invoke(index); } else QueueRedraw();
    }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        RebuildTabs(false);
        if (inputEvent is InputEventMouseMotion motion)
        {
            UpdateHover(motion.Position);
            if (!IsDisposed && GetViewport()?.GetGUIDragData() is { } payload) _ = OnCanDropData(motion.Position, payload);
            return;
        }
        if (inputEvent is InputEventMouseButton button)
        {
            UpdateHover(button.Position); if (IsDisposed) return;
            if (button.Pressed && button.ButtonIndex is >= MouseButton.WheelUp and <= MouseButton.WheelRight)
            {
                if (!_scrollingEnabled || !_buttonsVisible || button.IsCommandOrControlPressed()) return;
                var backward = button.ButtonIndex == MouseButton.WheelUp || button.ButtonIndex == (IsLayoutRTL() ? MouseButton.WheelRight : MouseButton.WheelLeft);
                ScrollPage(backward ? -1 : 1); return;
            }
            if (!button.Pressed)
            {
                if (button.ButtonIndex != MouseButton.Left) return;
                var pressed = _pressedTab; var close = _pressedClose; _pressedTab = null; QueueRedraw();
                var index = close ? _closeHover : _buttonHover;
                if (index >= 0 && ReferenceEquals(_tabs[index], pressed)) { if (close) TabClosePressed?.Invoke(index); else TabButtonPressed?.Invoke(index); }
                return;
            }
            if (button.ButtonIndex == MouseButton.Middle)
            {
                if (_closeWithMiddleMouse && _hover >= 0) TabClosePressed?.Invoke(_hover); return;
            }
            var selecting = button.ButtonIndex == MouseButton.Left || button.ButtonIndex == MouseButton.Right && _selectWithRMB;
            if (selecting && _buttonsVisible && _arrowHover != 0) { ScrollPage(_arrowHover); return; }
            var found = _hover; if (found < 0) return; var tab = _tabs[found];
            if (tab.ButtonRect.HasPoint(button.Position)) { if (selecting) { _pressedTab = tab; _pressedClose = false; QueueRedraw(); } return; }
            if (CloseVisible(found) && tab.CloseRect.HasPoint(button.Position)) { if (selecting && !tab.Disabled) { _pressedTab = tab; _pressedClose = true; QueueRedraw(); } return; }
            var revision = _selectionRevision;
            if (selecting && !tab.Disabled)
            {
                CurrentTab = _deselectEnabled && found == _current ? -1 : found;
                if (IsDisposed || !_tabs.Contains(tab)) return; revision = _selectionRevision; TabClicked?.Invoke(_tabs.IndexOf(tab));
            }
            if (!IsDisposed && revision == _selectionRevision && button.ButtonIndex == MouseButton.Right && _tabs.Contains(tab)) TabRMBClicked?.Invoke(_tabs.IndexOf(tab));
            return;
        }
        if (inputEvent.IsActionPressed("ui_left", allowEcho: true, exactMatch: true) || inputEvent.IsActionPressed("ui_right", allowEcho: true, exactMatch: true))
        {
            var right = inputEvent.IsActionPressed("ui_right", allowEcho: true, exactMatch: true); var direction = (right ^ IsLayoutRTL()) ? 1 : -1;
            var joypad = inputEvent is InputEventJoypadButton or InputEventJoypadMotion;
            if (joypad && !Input.IsActionJustPressedByEvent(right ? "ui_right" : "ui_left", inputEvent, exactMatch: true)) return;
            if (direction > 0 ? SelectNextAvailable() : SelectPreviousAvailable()) AcceptEvent();
            if (joypad) { _gamepadDirection = right ? 1 : -1; _repeatRemaining = .5; SetInternalProcessing(true, false); }
        }
    }
    private void ScrollPage(int direction)
    {
        var next = NextVisible(_offset, direction); if (direction > 0 && !_missingRight || next == -1) return;
        _offset = next; PlaceTabs(); QueueRedraw();
    }
    private void RepeatGamepad()
    {
        var action = _gamepadDirection > 0 ? "ui_right" : "ui_left";
        if (_gamepadDirection == 0 || !Input.IsActionPressed(action)) { _gamepadDirection = 0; SetInternalProcessing(false, false); return; }
        _repeatRemaining -= ProcessDeltaTime; if (_repeatRemaining > 0) return; _repeatRemaining = .05;
        var direction = IsLayoutRTL() ? -_gamepadDirection : _gamepadDirection;
        if (direction > 0) SelectNextAvailable(); else SelectPreviousAvailable();
    }
    private void HoverTimeout(Timer _)
    {
        if (IsDisposed || !_switchOnDragHover || GetViewport()?.GetGUIDragData() is null or TabDrag || (uint)_hover >= (uint)_tabs.Count || _tabs[_hover].Disabled || _tabs[_hover].Hidden) return;
        CurrentTab = _hover;
    }
    /// <inheritdoc />
    protected override DragPayload? OnGetDragData(Vector2 atPosition)
    {
        if (!_dragToRearrangeEnabled) return null; var index = GetTabIdxAtPoint(atPosition); if (index < 0) return null;
        if (GetViewport()?.IsGUIDragging() == true)
        {
            var preview = new Label { Text = Atr(_tabs[index].Title), MouseFilter = MouseFilter.Ignore };
            try { SetDragPreview(preview); } catch { if (!preview.IsDisposed) preview.Dispose(); throw; }
        }
        return new TabDrag(this, _tabs[index]);
    }
    /// <inheritdoc />
    protected override bool OnCanDropData(Vector2 atPosition, DragPayload payload)
    {
        UpdateHover(atPosition); if (IsDisposed) return false;
        if (_switchOnDragHover && payload is not TabDrag && _hover >= 0 && _hover != _current && !_tabs[_hover].Disabled)
        {
            if (_hoverTimer.IsStopped()) _hoverTimer.Start(Math.Max(.000001, GetThemeConstant("hover_switch_wait_msec") * .001));
        }
        else _hoverTimer.Stop();
        if (!_dragToRearrangeEnabled || payload is not TabDrag data || data.Source.IsDisposed || !ReferenceEquals(Tree, data.Source.Tree) || !data.Source._tabs.Contains(data.Item)) { _dropVisible = false; return false; }
        if (!ReferenceEquals(this, data.Source) && (_tabs.Count >= 65536 || _tabsRearrangeGroup == -1 || data.Source._tabsRearrangeGroup != _tabsRearrangeGroup)) { _dropVisible = false; return false; }
        RebuildTabs(false); var target = ClosestTab(atPosition); var before = target < 0 || IsLayoutRTL() ^ (atPosition.X <= _tabs[target].Rect.GetCenter().X);
        _dropX = target < 0 ? IsLayoutRTL() ? Size.X : 0 : before ? IsLayoutRTL() ? _tabs[target].Rect.End.X : _tabs[target].Rect.Position.X : IsLayoutRTL() ? _tabs[target].Rect.Position.X : _tabs[target].Rect.End.X;
        _dropVisible = true; QueueRedraw(); return true;
    }
    private int ClosestTab(Vector2 point)
    {
        var found = GetTabIdxAtPoint(point); if (found >= 0) return found; var distance = float.PositiveInfinity;
        for (var i = _offset; i <= _lastDrawn; i++) if (!_tabs[i].Hidden) { var delta = Math.Abs(_tabs[i].Rect.GetCenter().X - point.X); if (delta < distance) { found = i; distance = delta; } }
        return found;
    }
    internal int DropInsertionIndex(Vector2 point)
    { var target = ClosestTab(point); var before = target < 0 || IsLayoutRTL() ^ (point.X <= _tabs[target].Rect.GetCenter().X); return target < 0 ? 0 : target + (before ? 0 : 1); }
    /// <inheritdoc />
    protected override void OnDropData(Vector2 atPosition, DragPayload payload)
    {
        if (!OnCanDropData(atPosition, payload) || payload is not TabDrag data) return;
        // ponytail: an O(n) identity scan keeps drag payloads stable after edits; index identities if large strips become measured bottlenecks.
        var sourceIndex = data.Source._tabs.IndexOf(data.Item); var target = ClosestTab(atPosition); var before = target < 0 || IsLayoutRTL() ^ (atPosition.X <= _tabs[target].Rect.GetCenter().X);
        var destination = target < 0 ? 0 : target + (before ? 0 : 1); _dropVisible = false;
        if (ReferenceEquals(this, data.Source))
        {
            if (destination > sourceIndex) destination--; destination = Math.Clamp(destination, 0, _tabs.Count - 1);
            if (sourceIndex == destination) return; MoveTab(sourceIndex, destination);
            if (IsDisposed || !_tabs.Contains(data.Item) || data.Item.Disabled) return; destination = _tabs.IndexOf(data.Item); List<Exception>? errors = null;
            try { ActiveTabRearranged?.Invoke(destination); } catch (Exception error) { Node.CollectException(ref errors, error); }
            if (!IsDisposed && _tabs.Contains(data.Item)) try { CurrentTab = _tabs.IndexOf(data.Item); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Tab reorder observers failed.", errors);
        }
        else
        {
            data.Source.EnsureMutable(); _tabs.EnsureCapacity(_tabs.Count + 1);
            // Prepare target subscriptions before removing the source, then commit both collections before callbacks.
            WatchIcon(data.Item.Icon); try { WatchIcon(data.Item.ButtonIcon); } catch { UnwatchIcon(data.Item.Icon); throw; }
            data.Source.UnwatchIcon(data.Item.Icon); data.Source.UnwatchIcon(data.Item.ButtonIcon);
            var sourceSelected = data.Source._current == sourceIndex;
            data.Source._tabs.RemoveAt(sourceIndex); data.Source._selectionRevision++;
            if (data.Source._current >= sourceIndex && data.Source._current > 0) data.Source._current--;
            if (data.Source._previous >= sourceIndex && data.Source._previous > 0) data.Source._previous--;
            if (data.Source._tabs.Count == 0) data.Source._current = data.Source._previous = -1;
            else if (sourceSelected) { var next = data.Source.FindAvailable(data.Source._current - 1, 1); if (next < 0) next = data.Source.FindAvailable(data.Source._current, -1); data.Source._current = next; }
            data.Source._offset = Math.Min(data.Source._offset, Math.Max(0, data.Source._tabs.Count - 1));
            _tabs.Insert(destination, data.Item); if (_current >= destination) _current++; if (_previous >= destination) _previous++; _selectionRevision++; List<Exception>? errors = null;
            try { data.Source.ResetInteraction(); data.Source.InvalidateTabs(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            if (!IsDisposed) try { ResetInteraction(); InvalidateTabs(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            try { if (!data.Source.IsDisposed) data.Source.NotifyPropertyListChanged(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            try { if (!IsDisposed) NotifyPropertyListChanged(); } catch (Exception error) { Node.CollectException(ref errors, error); }
            if (sourceSelected && !data.Source.IsDisposed && data.Source.Tree is not null) try { data.Source.TabChanged?.Invoke(data.Source._current); } catch (Exception error) { Node.CollectException(ref errors, error); }
            if (!IsDisposed && _tabs.Contains(data.Item) && !data.Item.Disabled) try { CurrentTab = _tabs.IndexOf(data.Item); } catch (Exception error) { Node.CollectException(ref errors, error); }
            Node.ThrowCollected("Tab transfer observers failed.", errors);
        }
    }
}
