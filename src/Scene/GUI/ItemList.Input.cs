namespace Electron2D;

public partial class ItemList
{
    private int _hovered = -1;
    private int _shiftAnchor = -1, _deferSelectSingle = -1;
    private bool _allowReselect, _allowRmbSelect;
    private bool _allowSearch = true;
    private string _searchString = string.Empty;
    private long _lastSearchMsec;

    /// <summary>Occurs when GUI input selects an item in Single mode.</summary>
    public event Action<int>? ItemSelected;
    /// <summary>Occurs when GUI input changes one item in Multi or Toggle mode.</summary>
    public event Action<int, bool>? MultiSelected;
    /// <summary>Occurs when a pointer button is pressed over an enabled item.</summary>
    public event Action<int, Vector2, MouseButton>? ItemClicked;
    /// <summary>Occurs when an item is activated by double-click or the accept action.</summary>
    public event Action<int>? ItemActivated;
    /// <summary>Occurs when a pointer button is pressed outside every item.</summary>
    public event Action<Vector2, MouseButton>? EmptyClicked;

    /// <summary>Gets or sets whether clicking an already selected item repeats selection notification.</summary>
    /// <value>False initially.</value>
    public bool AllowReselect { get { ThrowIfDisposed(); return _allowReselect; } set { EnsureMutable(); _allowReselect = value; } }

    /// <summary>Gets or sets whether right-click can select an item.</summary>
    /// <value>False initially.</value>
    public bool AllowRMBSelect { get { ThrowIfDisposed(); return _allowRmbSelect; } set { EnsureMutable(); _allowRmbSelect = value; } }

    /// <summary>Gets or sets whether typed Unicode keys incrementally find items by text prefix.</summary>
    /// <value>True initially.</value>
    public bool AllowSearch { get { ThrowIfDisposed(); return _allowSearch; } set { EnsureMutable(); _allowSearch = value; } }

    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(inputEvent);
        if (inputEvent is InputEventMouseMotion motion)
        {
            if (_deferSelectSingle >= 0) { _deferSelectSingle = -1; return; }
            var hovered = GetItemAtPosition(motion.Position, exact: true);
            if (hovered != _hovered) { _hovered = hovered; QueueRedraw(); }
            return;
        }

        if (inputEvent is InputEventKey { Keycode: Key.Shift, Pressed: false }) _shiftAnchor = -1;

        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } && _deferSelectSingle >= 0)
        {
            var deferred = _deferSelectSingle;
            _deferSelectSingle = -1;
            Select(deferred);
            MultiSelected?.Invoke(deferred, true);
            return;
        }

        if (inputEvent is InputEventMouseButton button && button.Pressed)
        {
            _searchString = string.Empty;
            if (button.ButtonIndex is >= MouseButton.WheelUp and <= MouseButton.WheelRight)
            {
                var vertical = button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown;
                var bar = vertical && !button.ShiftPressed && _vBar.Visible || !vertical && button.ShiftPressed ? (ScrollBar)_vBar : _hBar;
                var before = bar.Value;
                var sign = button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelLeft ? -1 : 1;
                bar.Scroll(sign * bar.Page / 8 * button.Factor);
                if (bar.Value != before) AcceptEvent();
                return;
            }

            var index = GetItemAtPosition(button.Position, exact: true);
            if (index < 0) { EmptyClicked?.Invoke(button.Position, button.ButtonIndex); return; }
            var item = GetItem(index);
            if (item.Disabled) return;
            if (button.ButtonIndex == MouseButton.Left || button.ButtonIndex == MouseButton.Right && _allowRmbSelect)
            {
                if (_selectMode == SelectMode.Multi && item.Selected && button.IsCommandOrControlPressed())
                {
                    Deselect(index);
                    MultiSelected?.Invoke(index, false);
                }
                else if (_selectMode == SelectMode.Multi && button.ShiftPressed && (uint)_current < (uint)_items.Count && _current != index)
                {
                    var start = Math.Min(_current, index);
                    var end = Math.Max(_current, index);
                    for (var selectedIndex = start; selectedIndex <= end; selectedIndex++)
                    {
                        var candidate = _items[selectedIndex];
                        if (candidate.Selected || !candidate.Selectable || candidate.Disabled) continue;
                        Select(selectedIndex, single: false);
                        MultiSelected?.Invoke(selectedIndex, true);
                    }
                }
                else if (_selectMode == SelectMode.Toggle)
                {
                    if (item.Selectable)
                    {
                        if (item.Selected) Deselect(index);
                        else Select(index, single: false);
                        _current = index;
                        MultiSelected?.Invoke(index, item.Selected);
                    }
                }
                else if (_selectMode == SelectMode.Multi && !button.DoubleClick &&
                    !button.IsCommandOrControlPressed() && item.Selectable && item.Selected && button.ButtonIndex == MouseButton.Left)
                {
                    _deferSelectSingle = index;
                    return;
                }
                else if (item.Selectable && (!item.Selected || _allowReselect))
                {
                    Select(index, _selectMode == SelectMode.Single || !button.IsCommandOrControlPressed());
                    if (_selectMode == SelectMode.Single) ItemSelected?.Invoke(index);
                    else MultiSelected?.Invoke(index, true);
                }
                ItemClicked?.Invoke(index, button.Position, button.ButtonIndex);
                if (button.ButtonIndex == MouseButton.Left && button.DoubleClick) ItemActivated?.Invoke(index);
            }
            else ItemClicked?.Invoke(index, button.Position, button.ButtonIndex);
            return;
        }

        if (inputEvent is InputEventPanGesture pan)
        {
            var beforeV = _vBar.Value; var beforeH = _hBar.Value;
            _vBar.Value += _vBar.Page * pan.Delta.Y / 8;
            _hBar.Value += _hBar.Page * pan.Delta.X / 8;
            if (beforeV != _vBar.Value || beforeH != _hBar.Value) AcceptEvent();
            return;
        }
        if (!inputEvent.IsPressed() || _items.Count == 0) return;
        if (_selectMode == SelectMode.Multi && inputEvent is InputEventKey { ShiftPressed: true } &&
            inputEvent.IsAction("ui_up", false)) RangeSelect(Math.Max(0, _current - _currentColumns));
        else if (_selectMode == SelectMode.Multi && inputEvent is InputEventKey { ShiftPressed: true } &&
            inputEvent.IsAction("ui_down", false)) RangeSelect(Math.Min(_items.Count - 1, _current + _currentColumns));
        else if (_selectMode == SelectMode.Multi && inputEvent is InputEventKey { ShiftPressed: true } &&
            inputEvent.IsAction("ui_left", false)) RangeSelect(Math.Max(0, _current - 1));
        else if (_selectMode == SelectMode.Multi && inputEvent is InputEventKey { ShiftPressed: true } &&
            inputEvent.IsAction("ui_right", false)) RangeSelect(Math.Min(_items.Count - 1, _current + 1));
        else if (inputEvent.IsAction("ui_up", true)) NavigateVertical(-1);
        else if (inputEvent.IsAction("ui_down", true)) NavigateVertical(1);
        else if (inputEvent.IsAction("ui_left", true)) NavigateHorizontal(-1);
        else if (inputEvent.IsAction("ui_right", true)) NavigateHorizontal(1);
        else if (inputEvent.IsAction("ui_page_up", true)) NavigateVertical(-4);
        else if (inputEvent.IsAction("ui_page_down", true)) NavigateVertical(4);
        else if (inputEvent.IsAction("ui_select", true) && _selectMode != SelectMode.Single && (uint)_current < (uint)_items.Count)
        {
            var item = _items[_current];
            if (!item.Selectable || item.Disabled) return;
            if (item.Selected) Deselect(_current);
            else Select(_current, single: false);
            MultiSelected?.Invoke(_current, item.Selected);
            AcceptEvent();
        }
        else if (inputEvent.IsAction("ui_menu", true) && _allowRmbSelect && (uint)_current < (uint)_items.Count && !_items[_current].Disabled)
        {
            ItemClicked?.Invoke(_current, GetItemRect(_current).Position, MouseButton.Right);
            AcceptEvent();
        }
        else if (inputEvent.IsAction("ui_accept", true) && (uint)_current < (uint)_items.Count && !_items[_current].Disabled)
        {
            _searchString = string.Empty;
            ItemActivated?.Invoke(_current);
            AcceptEvent();
        }
        else if (inputEvent.IsAction("ui_cancel", true)) _searchString = string.Empty;
        else if (_allowSearch && inputEvent is InputEventKey { Unicode: > 0 } key) Search(key.Unicode);
    }

    private void NavigateVertical(int rows)
    {
        RebuildListLayout();
        var step = Math.Sign(rows) * _currentColumns;
        for (var count = Math.Abs(rows); count > 0; count--)
        {
            var index = _current + step * count;
            while ((uint)index < (uint)_items.Count && (!_items[index].Selectable || _items[index].Disabled)) index += step;
            if ((uint)index >= (uint)_items.Count) continue;
            Current = index;
            EnsureCurrentIsVisible();
            if (_selectMode == SelectMode.Single) ItemSelected?.Invoke(index);
            AcceptEvent();
            return;
        }
    }

    private void NavigateHorizontal(int direction)
    {
        _searchString = string.Empty;
        RebuildListLayout();
        if (_current < 0 || direction < 0 && _current % _currentColumns == 0 ||
            direction > 0 && _current % _currentColumns == _currentColumns - 1) return;
        var row = _current / _currentColumns;
        for (var index = _current + direction; (uint)index < (uint)_items.Count && index / _currentColumns == row; index += direction)
        {
            if (!_items[index].Selectable || _items[index].Disabled) continue;
            Current = index;
            EnsureCurrentIsVisible();
            if (_selectMode == SelectMode.Single) ItemSelected?.Invoke(index);
            AcceptEvent();
            return;
        }
    }

    private void Search(int scalar)
    {
        var now = Environment.TickCount64;
        var interval = ProjectSettings.GetWithOverride(ProjectSettings.IncrementalSearchMaxIntervalMsec);
        if (now - _lastSearchMsec > interval) _searchString = string.Empty;
        _lastSearchMsec = now;
        var next = char.ConvertFromUtf32(scalar);
        if (_searchString != next) _searchString += next;
        for (var offset = 1; offset <= _items.Count; offset++)
        {
            var index = (_current + offset + _items.Count) % _items.Count;
            var item = _items[index];
            if (!item.Selectable || item.Disabled || !item.Text.StartsWith(_searchString, StringComparison.OrdinalIgnoreCase)) continue;
            Current = index;
            EnsureCurrentIsVisible();
            if (_selectMode == SelectMode.Single) ItemSelected?.Invoke(index);
            break;
        }
    }

    private void RangeSelect(int to)
    {
        if ((uint)_current >= (uint)_items.Count || (uint)to >= (uint)_items.Count) return;
        if (_shiftAnchor < 0) _shiftAnchor = _current;
        var start = Math.Min(_shiftAnchor, to);
        var end = Math.Max(_shiftAnchor, to);
        for (var index = 0; index < _items.Count; index++)
        {
            var item = _items[index];
            var shouldSelect = index >= start && index <= end && item.Selectable && !item.Disabled;
            if (item.Selected == shouldSelect) continue;
            if (shouldSelect) Select(index, single: false);
            else Deselect(index);
            MultiSelected?.Invoke(index, shouldSelect);
        }
        _current = to;
        EnsureCurrentIsVisible();
        AcceptEvent();
    }
}
