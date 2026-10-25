using System.Text;

namespace Electron2D;

public partial class ItemList
{
    private int _hovered = -1;
    private int _shiftAnchor = -1, _deferSelectSingle = -1;
    private bool _allowReselect, _allowRmbSelect;
    private bool _allowSearch = true, _handlingListInput;
    private char[] _searchBuffer = [];
    private int _searchLength;
    private long _lastSearchMsec;
    private uint _inputRevision;

    /// <summary>Occurs after GUI input selects the current item in Single mode.</summary>
    /// <remarks>Programmatic selection is silent. A pointer selection precedes ItemClicked and double-click activation.
    /// Handler exceptions stop the emission; model mutation or disposal stops the remaining input operation.</remarks>
    public event Action<int>? ItemSelected;
    /// <summary>Occurs for one item in an input-driven Multi or Toggle selection transition.</summary>
    /// <remarks>Range notifications run in index order. Programmatic selection is silent. A handler may change the model;
    /// the remaining operation then stops before using an old index. Keyboard ranges report requested selection even
    /// when an ineligible item retains its flag. Exceptions stop the ordinary multicast emission.</remarks>
    public event Action<int, bool>? MultiSelected;
    /// <summary>Occurs when a pointer button is pressed over an enabled item, or the menu action requests its context menu.</summary>
    /// <remarks>The position is local to the list. Pointer selection is delivered first; left double-click activation is last.
    /// A selected Multi item defers exclusive selection until release, without a click notification.</remarks>
    public event Action<int, Vector2, MouseButton>? ItemClicked;
    /// <summary>Occurs when an enabled item is activated by left double-click or the accept action.</summary>
    /// <remarks>Selectable state does not prevent activation. A double-click follows selection and ItemClicked.</remarks>
    public event Action<int>? ItemActivated;
    /// <summary>Occurs when a pointer button is pressed outside every item, including a wheel press before scrolling.</summary>
    /// <remarks>The position is local to the list. Subscribers may implement their own empty-space selection policy.</remarks>
    public event Action<Vector2, MouseButton>? EmptyClicked;

    /// <summary>Gets or sets whether clicking an already selected item repeats selection notification.</summary>
    /// <value>False initially.</value>
    public bool AllowReselect { get { ThrowIfDisposed(); return _allowReselect; } set { EnsureMutable(); if (_allowReselect == value) return; _allowReselect = value; _inputRevision++; } }

    /// <summary>Gets or sets whether right-click can select an item.</summary>
    /// <value>False initially.</value>
    public bool AllowRMBSelect { get { ThrowIfDisposed(); return _allowRmbSelect; } set { EnsureMutable(); if (_allowRmbSelect == value) return; _allowRmbSelect = value; _inputRevision++; } }

    /// <summary>Gets or sets whether committed text or typed Unicode keys incrementally find items by source-text prefix.</summary>
    /// <value>True initially.</value>
    public bool AllowSearch { get { ThrowIfDisposed(); return _allowSearch; } set { EnsureMutable(); if (_allowSearch == value) return; _allowSearch = value; _inputRevision++; } }

    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(inputEvent);
        if (_handlingListInput) throw new InvalidOperationException("Item list input cannot reenter itself.");
        _handlingListInput = true;
        try { HandleListInput(inputEvent); }
        finally { _handlingListInput = false; }
    }

    /// <inheritdoc />
    protected override void OnTextInput(string text)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(text);
        if (!_allowSearch || text.Length == 0 || _items.Count == 0) return;
        if (_handlingListInput) throw new InvalidOperationException("Item list input cannot reenter itself.");
        _handlingListInput = true;
        try
        {
            foreach (var rune in text.EnumerateRunes())
                if (!Search(rune)) break;
        }
        finally { _handlingListInput = false; }
    }

    private void HandleListInput(InputEvent inputEvent)
    {
        var wheelMoved = false; var inputTree = Tree;
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
            if ((uint)deferred >= (uint)_items.Count) return;
            Select(deferred);
            EmitMultiSelected(deferred, true);
            return;
        }
        if (inputEvent is InputEventMouseButton { Pressed: true } button)
        {
            _searchLength = 0;
            var index = GetItemAtPosition(button.Position, exact: true);
            if (index >= 0 && (button.ButtonIndex == MouseButton.Left || button.ButtonIndex == MouseButton.Right && _allowRmbSelect))
            {
                HandleItemClick(index, button);
                return;
            }
            if (index >= 0)
            {
                if (!_items[index].Disabled && !EmitItemClicked(index, button.Position, button.ButtonIndex)) return;
            }
            else
            {
                var revision = _inputRevision; var tree = Tree;
                BeginEventEmission(EmptyClicked)?.Invoke(button.Position, button.ButtonIndex);
                if (IsDisposed || revision != _inputRevision || !ReferenceEquals(tree, Tree)) return;
            }
            if (button.ButtonIndex is >= MouseButton.WheelUp and <= MouseButton.WheelRight)
            {
                var vertical = button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown;
                var bar = vertical ? (button.ShiftPressed || !_vBar.Visible ? (ScrollBar)_hBar : _vBar)
                    : (button.ShiftPressed || !_hBar.Visible ? (ScrollBar)_vBar : _hBar);
                var before = bar.Value;
                var revision = _inputRevision;
                var sign = button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelLeft ? -1 : 1;
                bar.Scroll(sign * bar.Page / 8 * button.Factor);
                if (IsDisposed || revision != _inputRevision || !ReferenceEquals(inputTree, Tree)) return;
                wheelMoved = bar.Value != before;
            }
        }
        if (inputEvent is InputEventPanGesture pan)
        {
            var revision = _inputRevision; var tree = Tree;
            _vBar.Value += _vBar.Page * pan.Delta.Y / 8;
            if (!IsDisposed && revision == _inputRevision && ReferenceEquals(tree, Tree)) _hBar.Value += _hBar.Page * pan.Delta.X / 8;
            return;
        }
        HandleActionInput(inputEvent);
        if (wheelMoved && !IsDisposed && ReferenceEquals(inputTree, Tree)) AcceptEvent();
    }

    private void HandleActionInput(InputEvent inputEvent)
    {
        if (!inputEvent.IsPressed() || _items.Count == 0) return;
        RebuildListLayout();
        if (inputEvent.IsAction("ui_menu", true) && _allowRmbSelect && (uint)_current < (uint)_items.Count)
        {
            if (_items[_current].Disabled) return;
            if (EmitItemClicked(_current, GetItemRect(_current).Position, MouseButton.Right)) AcceptEvent();
            return;
        }
        var shift = _selectMode == SelectMode.Multi && inputEvent is InputEventKey { ShiftPressed: true };
        if (shift && inputEvent.IsAction("ui_up")) RangeSelect((int)Math.Max(0L, (long)_current - _maxColumns));
        else if (inputEvent.IsAction("ui_up", true)) NavigateVertical(-1);
        else if (shift && inputEvent.IsAction("ui_down")) RangeSelect((int)Math.Min(_items.Count - 1L, (long)_current + _maxColumns));
        else if (inputEvent.IsAction("ui_down", true)) NavigateVertical(1);
        else if (inputEvent.IsAction("ui_page_up", true)) NavigatePage(-1);
        else if (inputEvent.IsAction("ui_page_down", true)) NavigatePage(1);
        else if (shift && inputEvent.IsAction("ui_left")) RangeSelect(Math.Max(0, _current - 1));
        else if (inputEvent.IsAction("ui_left", true)) NavigateHorizontal(-1);
        else if (shift && inputEvent.IsAction("ui_right")) RangeSelect(Math.Min(_items.Count - 1, _current + 1));
        else if (inputEvent.IsAction("ui_right", true)) NavigateHorizontal(1);
        else if (inputEvent.IsAction("ui_cancel", true)) _searchLength = 0;
        else if (inputEvent.IsAction("ui_select", true) && _selectMode != SelectMode.Single && (uint)_current < (uint)_items.Count)
        {
            var index = _current;
            var item = _items[index];
            if (item.Selected) { Deselect(index); EmitMultiSelected(index, false); }
            else if (CanSelect(index)) { Select(index, single: false); EmitMultiSelected(index, true); }
        }
        else if (inputEvent.IsAction("ui_accept", true))
        {
            _searchLength = 0;
            if ((uint)_current < (uint)_items.Count && !_items[_current].Disabled) BeginEventEmission(ItemActivated)?.Invoke(_current);
        }
        else if (_allowSearch && inputEvent is InputEventKey { Unicode: > 0 } key) Search(new Rune(key.Unicode));
    }

    private void HandleItemClick(int index, InputEventMouseButton button)
    {
        var item = _items[index];
        if (item.Disabled) return;
        if (_selectMode == SelectMode.Multi && item.Selected && button.IsCommandOrControlPressed())
        {
            Deselect(index);
            if (!EmitMultiSelected(index, false)) return;
        }
        else if (_selectMode == SelectMode.Multi && button.ShiftPressed && (uint)_current < (uint)_items.Count && _current != index)
        {
            var start = Math.Min(_current, index);
            var end = Math.Max(_current, index);
            for (var i = start; i <= end; i++)
            {
                if (!CanSelect(i) || _items[i].Selected) continue;
                Select(i, single: false);
                if (!EmitMultiSelected(i, true)) return;
            }
        }
        else
        {
            if (_selectMode == SelectMode.Multi && !button.DoubleClick && !button.IsCommandOrControlPressed() && item.Selectable && item.Selected && button.ButtonIndex == MouseButton.Left)
            {
                _deferSelectSingle = index;
                return;
            }
            if (_selectMode == SelectMode.Toggle && item.Selectable)
            {
                if (item.Selected) Deselect(index);
                else Select(index, single: false);
                _current = index;
                if (!EmitMultiSelected(index, item.Selected)) return;
            }
            else if (_selectMode != SelectMode.Toggle && item.Selectable && (!item.Selected || _allowReselect))
            {
                Select(index, _selectMode == SelectMode.Single || !button.IsCommandOrControlPressed());
                if (_selectMode == SelectMode.Single ? !EmitItemSelected(index) : !EmitMultiSelected(index, true)) return;
            }
            if (!EmitItemClicked(index, button.Position, button.ButtonIndex)) return;
            if (button.ButtonIndex == MouseButton.Left && button.DoubleClick) BeginEventEmission(ItemActivated)?.Invoke(index);
            return;
        }
        EmitItemClicked(index, button.Position, button.ButtonIndex);
    }

    private bool CanSelect(int index) => _items[index].Selectable && !_items[index].Disabled;

    private void NavigateVertical(int direction)
    {
        var interval = ProjectSettings.GetWithOverride(ProjectSettings.IncrementalSearchMaxIntervalMsec);
        if (_searchLength > 0 && Environment.TickCount64 - _lastSearchMsec < (long)interval * 2)
        {
            for (var i = _current + direction; (uint)i < (uint)_items.Count; i += direction)
                if (CanSelect(i) && _items[i].Text.AsSpan().StartsWith(_searchBuffer.AsSpan(0, _searchLength), StringComparison.Ordinal))
                { NavigateTo(i); return; }
            AcceptEvent();
            return;
        }
        var next = (long)_current + (long)direction * _currentColumns;
        if (next < 0 || next >= _items.Count) return;
        while (next >= 0 && next < _items.Count && !CanSelect((int)next)) next += (long)direction * _currentColumns;
        if (next >= 0 && next < _items.Count) NavigateTo((int)next);
        else AcceptEvent();
    }

    private void NavigatePage(int direction)
    {
        _searchLength = 0;
        for (var count = 4; count > 0; count--)
        {
            var index = (long)_current + (long)direction * _currentColumns * count;
            if (index >= 0 && index < _items.Count && CanSelect((int)index)) { NavigateTo((int)index); return; }
        }
    }

    private void NavigateHorizontal(int direction)
    {
        _searchLength = 0;
        if (direction < 0 && _current % _currentColumns == 0 || direction > 0 && (_current % _currentColumns == _currentColumns - 1 || _current + 1 >= _items.Count)) return;
        var row = _current / _currentColumns;
        for (var index = _current + direction; (uint)index < (uint)_items.Count; index += direction)
        {
            if (!CanSelect(index)) continue;
            if (index / _currentColumns == row) NavigateTo(index);
            else AcceptEvent();
            return;
        }
        AcceptEvent();
    }

    private void NavigateTo(int index)
    {
        Current = index;
        EnsureCurrentIsVisible();
        if (_selectMode == SelectMode.Single && !EmitItemSelected(_current)) return;
        AcceptEvent();
    }

    private bool Search(Rune rune)
    {
        var now = Environment.TickCount64;
        var interval = ProjectSettings.GetWithOverride(ProjectSettings.IncrementalSearchMaxIntervalMsec);
        if (now - _lastSearchMsec > interval) _searchLength = 0;
        _lastSearchMsec = now;
        Span<char> scalar = stackalloc char[2];
        var length = rune.EncodeToUtf16(scalar);
        if (!_searchBuffer.AsSpan(0, _searchLength).SequenceEqual(scalar[..length]))
        {
            if (_searchBuffer.Length - _searchLength < length) Array.Resize(ref _searchBuffer, Math.Max(16, checked((_searchLength + length) * 2)));
            scalar[..length].CopyTo(_searchBuffer.AsSpan(_searchLength));
            _searchLength += length;
        }
        for (var offset = 1; offset <= _items.Count; offset++)
        {
            var index = (int)(((long)_current + offset) % _items.Count);
            if (index == _current) break;
            if (!_items[index].Text.AsSpan().StartsWith(_searchBuffer.AsSpan(0, _searchLength), StringComparison.OrdinalIgnoreCase)) continue;
            Current = index;
            EnsureCurrentIsVisible();
            return _selectMode != SelectMode.Single || EmitItemSelected(_current);
        }
        return true;
    }

    private void RangeSelect(int to)
    {
        if ((uint)_current >= (uint)_items.Count || (uint)to >= (uint)_items.Count) { AcceptEvent(); return; }
        if (_shiftAnchor < 0) _shiftAnchor = _current;
        var start = Math.Min(_shiftAnchor, to);
        var end = Math.Max(_shiftAnchor, to);
        for (var index = 0; index < _items.Count; index++)
        {
            var shouldSelect = index >= start && index <= end;
            if (_items[index].Selected == shouldSelect) continue;
            if (shouldSelect) Select(index, single: false);
            else Deselect(index);
            if (!EmitMultiSelected(index, shouldSelect)) return;
        }
        _current = to; _inputRevision++;
        QueueRedraw(); EnsureCurrentIsVisible(); AcceptEvent();
    }

    // Stop after callbacks replace the selection/model, before another index is consumed.
    private bool EmitItemSelected(int index)
    {
        var revision = _inputRevision; var tree = Tree;
        BeginEventEmission(ItemSelected)?.Invoke(index);
        return !IsDisposed && revision == _inputRevision && ReferenceEquals(tree, Tree);
    }
    private bool EmitMultiSelected(int index, bool selected)
    {
        var revision = _inputRevision; var tree = Tree;
        BeginEventEmission(MultiSelected)?.Invoke(index, selected);
        return !IsDisposed && revision == _inputRevision && ReferenceEquals(tree, Tree);
    }
    private bool EmitItemClicked(int index, Vector2 point, MouseButton button)
    {
        var revision = _inputRevision; var tree = Tree;
        BeginEventEmission(ItemClicked)?.Invoke(index, point, button);
        return !IsDisposed && revision == _inputRevision && ReferenceEquals(tree, Tree);
    }
}
