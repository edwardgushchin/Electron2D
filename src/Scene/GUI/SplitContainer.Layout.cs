namespace Electron2D;

public partial class SplitContainer
{
    private readonly List<(Control Child, int Size)> _preserved = [];
    private readonly List<float> _finalSizes = [];
    private void ChildVisibility(CanvasItem _) => Synchronize();
    private void Synchronize(bool preparing = false)
    {
        if (!_ready || _syncing || IsDisposed) return;
        if (_arranging && !preparing) { _again = true; QueueSort(); return; }
        _syncing = true;
        try
        {
            _nextChildren.Clear();
            for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Control child && Sortable(child, true)) _nextChildren.Add(child);
            var same = _children.Count == _nextChildren.Count;
            if (same) for (var i = 0; i < _children.Count; i++) if (!ReferenceEquals(_children[i], _nextChildren[i])) { same = false; break; }
            if (same) return;
            var removed = false; foreach (var old in _children) if (!_nextChildren.Contains(old)) { removed = true; break; }
            var preserve = !_offsetPending && _children.Count >= 2 && _offsets.Count == _defaults.Count && _defaults.Count == _children.Count - 1 &&
                (removed && _children.Count > 2 || _nextChildren.Count == _children.Count && _children.Count > 2 || _canPreserve && _nextChildren.Count > _children.Count);
            _preserved.Clear();
            if (preserve)
            {
                var start = 0; var sep = Separation;
                for (var i = 0; i < _children.Count; i++)
                {
                    var end = i == _defaults.Count ? AxisSize : checked(_defaults[i] + _offsets[i]);
                    _preserved.Add((_children[i], checked(end - start))); start = checked(end + sep);
                }
            }
            foreach (var child in _children) child.VisibilityChanged -= ChildVisibility;
            _children.Clear(); _children.AddRange(_nextChildren); _nextChildren.Clear();
            foreach (var child in _children) child.VisibilityChanged += ChildVisibility;
            _canPreserve |= removed; _generation++; _again |= _arranging;
            if (_offsetPending && _offsets.Count == _children.Count - 1) _offsetPending = false;
            PrepareSlots(); DefaultPositions(); EnsureDraggers();
            if (preserve && _children.Count >= 2)
            {
                _desired.Clear(); var priority = -1;
                // ponytail: Structural child reconciliation scans retained sizes quadratically; index by identity if large-panel edits become a measured bottleneck.
                foreach (var child in _children)
                {
                    var size = Axis(child.Size); var found = false;
                    foreach (var previous in _preserved) if (ReferenceEquals(previous.Child, child)) { size = previous.Size; found = true; break; }
                    if (!found) priority = _desired.Count;
                    _desired.Add(size);
                }
                if (!removed && priority < 0) for (var i = 0; i < _children.Count; i++) if (!ReferenceEquals(_children[i], _preserved[i].Child)) { priority = i; break; }
                SetDesiredSizes(priority);
            }
            UpdateMinimumSize(); QueueSort();
        }
        finally { _syncing = false; _nextChildren.Clear(); _preserved.Clear(); }
    }
    private void ValidateDraggers()
    {
        foreach (var dragger in _draggers)
        {
            ObjectDisposedException.ThrowIf(dragger.IsDisposed, dragger);
            if (!ReferenceEquals(dragger.Parent, this)) throw new InvalidOperationException("A required split drag area was removed.");
        }
    }
    private void EnsureDraggers()
    {
        ValidateDraggers(); var count = Math.Max(1, _children.Count - 1); var syncing = _syncing; _syncing = true;
        try
        {
            while (_draggers.Count < count)
            {
                var dragger = new Dragger(this, _draggers.Count) { Name = "_split_drag_" + _draggers.Count };
                _draggers.Add(dragger); AddChild(dragger, InternalMode.Back); dragger.UpdateTouch();
            }
            while (_draggers.Count > count)
            {
                var index = _draggers.Count - 1; var dragger = _draggers[index]; _draggers.RemoveAt(index);
                List<Exception>? errors = null;
                try { dragger.Stop(); } catch (Exception error) { CollectException(ref errors, error); }
                try { RemoveChild(dragger); } catch (Exception error) { CollectException(ref errors, error); }
                try { dragger.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
                ThrowCollected("Split drag area removal failed.", errors);
            }
        }
        finally { _syncing = syncing; }
    }
    private void PrepareSlots()
    {
        _slots.Clear();
        foreach (var child in _children)
        {
            child.ContainerMaximum = null;
            var min = child.GetBoundMinimumSize(); var max = child.GetCombinedMaximumSize();
            var ratio = child.SizeFlagsStretchRatio;
            var expand = ((_vertical ? child.SizeFlagsVertical : child.SizeFlagsHorizontal) & SizeFlags.Expand) != 0 && ratio > 0;
            _slots.Add(new() { Child = child, Min = _vertical ? min.Y : min.X, Max = _vertical ? max.Y : max.X, Ratio = expand ? ratio : 0, Expand = expand });
        }
    }
    private void DefaultPositions()
    {
        _defaults.Clear(); if (_slots.Count < 2) return;
        var sep = Separation; var size = AxisSize; var space = (float)checked(size - sep * (_slots.Count - 1)); var total = 0f; var expands = 0;
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i]; slot.Final = Pixel(slot.Min); slot.Active = slot.Expand;
            if (slot.Expand) { total += slot.Ratio; expands++; } else space -= slot.Final;
            _slots[i] = slot;
        }
        if (!float.IsFinite(total)) throw new InvalidOperationException("Split stretch weights overflowed.");
        if (_slots.Count == 2 && expands == 2) { _defaults.Add(Pixel(size * (_slots[0].Ratio / total) - sep * .5f)); return; }
        while (total > 0 && space > 0)
        {
            var retry = false; var error = 0f;
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i]; if (!slot.Active) continue;
                var amount = slot.Ratio / total * space; var min = Pixel(slot.Min); var max = Pixel(slot.Max);
                error += amount - Pixel(amount);
                if (amount < min || max >= 0 && amount > max)
                {
                    slot.Final = amount < min ? min : max; slot.Active = false; total -= slot.Ratio; space -= slot.Final; retry = true; _slots[i] = slot; break;
                }
                slot.Final = Pixel(amount); if (error >= 1) { slot.Final++; error--; }
                _slots[i] = slot;
            }
            if (!retry) break;
        }
        var pos = 0; var seen = 0;
        for (var i = 0; i < _slots.Count - 1; i++)
        {
            pos = checked(pos + Pixel(_slots[i].Final)); if (_slots[i].Expand) seen++;
            _defaults.Add(seen == 0 ? 0 : seen >= expands ? checked(size - sep) : pos); pos = checked(pos + sep);
        }
    }
    private (int Min, int Max) ValidRange(int index)
    {
        var sep = Separation; var min = checked(sep * index); var max = checked(AxisSize - sep * (_slots.Count - 1 - index));
        var maxLeft = min; var maxRight = checked(sep * (_slots.Count - 1 - index)); var left = true; var right = true;
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i]; var minimum = Pixel(slot.Min); var maximum = Pixel(slot.Max);
            if (i <= index) { min = checked(min + minimum); if (left) { if (maximum < 0) left = false; else maxLeft = checked(maxLeft + maximum); } }
            else { max = checked(max - minimum); if (right) { if (maximum < 0) right = false; else maxRight = checked(maxRight + maximum); } }
        }
        if (left) max = Math.Min(max, maxLeft); if (right) min = Math.Max(min, checked(AxisSize - maxRight));
        return (min, max);
    }
    private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    private void Positions(int priority = -1)
    {
        _positions.Clear(); if (priority >= _defaults.Count) throw new ArgumentOutOfRangeException(nameof(priority));
        while (_offsets.Count < Math.Max(1, _defaults.Count)) _offsets.Add(0);
        for (var i = 0; i < _defaults.Count; i++) { var range = ValidRange(i); _positions.Add(Clamp(checked(_defaults[i] + (_collapsed ? 0 : _offsets[i])), range.Min, range.Max)); }
        var sep = Separation;
        if (!_collapsed)
        {
            if (priority < 0)
            {
                for (var i = 0; i < _positions.Count - 1; i++)
                {
                    var push = checked(_positions[i] + sep + Pixel(_slots[i + 1].Min));
                    if (_positions[i + 1] >= push) continue;
                    _positions[i + 1] = push; var range = ValidRange(i); _positions[i] = Clamp(_positions[i], range.Min, range.Max);
                }
            }
            else
            {
                for (var i = priority - 1; i >= 0; i--)
                {
                    _positions[i] = Math.Min(_positions[i], checked(_positions[i + 1] - sep - Pixel(_slots[i + 1].Min)));
                    if (_slots[i + 1].Max >= 0) _positions[i] = Math.Max(_positions[i], checked(_positions[i + 1] - sep - Pixel(_slots[i + 1].Max)));
                }
                for (var i = priority + 1; i < _positions.Count; i++)
                {
                    _positions[i] = Math.Max(_positions[i], checked(_positions[i - 1] + sep + Pixel(_slots[i].Min)));
                    if (_slots[i].Max >= 0) _positions[i] = Math.Min(_positions[i], checked(_positions[i - 1] + sep + Pixel(_slots[i].Max)));
                }
            }
        }
        if (priority >= 0) for (var i = 0; i < _positions.Count; i++) _offsets[i] = checked(_positions[i] - _defaults[i]);
        if (!_vertical && IsLayoutRTL()) for (var i = 0; i < _positions.Count; i++) _positions[i] = checked(AxisSize - _positions[i] - sep);
    }
    private void SetDesiredSizes(int priority)
    {
        var sep = Separation; var size = (float)AxisSize; var max = GetCombinedMaximumSize(); var maximum = _vertical ? max.Y : max.X;
        var propagating = PropagateMaximumSize && maximum >= 0;
        var total = (float)checked(sep * (_slots.Count - 1)); var ratios = 0f;
        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i]; slot.Final = MathF.Max(slot.Min, _desired[i]); slot.Priority = i == priority;
            if (slot.Priority) slot.Ratio = 0; ratios += slot.Ratio; total += slot.Final; _slots[i] = slot;
        }
        if (!float.IsFinite(ratios) || !float.IsFinite(total)) throw new InvalidOperationException("Split desired geometry overflowed.");
        var available = (propagating && ratios > 0 ? maximum : size) - total;
        for (var pass = 0; pass < _slots.Count + 1 && available > 0; pass++)
        {
            var ratio = 0f; foreach (var s in _slots) if (s.Ratio > 0 && (s.Max < 0 || s.Final < s.Max)) ratio += s.Ratio;
            if (ratio <= 0) break;
            var amount = available / ratio; var before = available;
            for (var i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i]; if (s.Ratio <= 0 || s.Max >= 0 && s.Final >= s.Max) continue;
                var old = s.Final; s.Final = s.Max >= 0 ? MathF.Min(s.Final + amount * s.Ratio, s.Max) : s.Final + amount * s.Ratio; available -= s.Final - old; _slots[i] = s;
            }
            if (Mathf.IsZeroApprox(available) || Mathf.IsEqualApprox(before, available)) break;
        }
        for (var pass = 0; pass < _slots.Count + 1 && available < 0; pass++)
        {
            var ratio = 0f; var capacity = 0f; foreach (var s in _slots) if (s.Ratio > 0 && s.Final > s.Min) { ratio += s.Ratio; capacity += s.Final - s.Min; }
            if (ratio <= 0) break; var amount = MathF.Min(-available, capacity) / ratio; if (Mathf.IsZeroApprox(amount)) break; var before = available;
            for (var i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i]; if (s.Ratio <= 0 || s.Final <= s.Min) continue;
                var old = s.Final; s.Final = Math.Clamp(s.Final - amount * s.Ratio, s.Min, s.Final); available += old - s.Final; _slots[i] = s;
            }
            if (Mathf.IsEqualApprox(before, available)) break;
        }
        var skip = true;
        for (var pass = 0; pass < 2 * _slots.Count + 2 && available < 0; pass++)
        {
            var largest = 0f; var target = 0f; var count = 0;
            foreach (var s in _slots)
            {
                if (s.Final <= s.Min || skip && s.Priority) continue;
                if (s.Final > largest) { target = largest; largest = s.Final; count = 1; } else if (s.Final == largest) count++; else if (s.Final > target) target = s.Final;
            }
            if (largest <= 0) { if (skip) { skip = false; continue; } break; }
            target = MathF.Max(target, largest + available / count); var before = available;
            for (var i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i]; if (s.Final <= s.Min || skip && s.Priority || s.Final != largest) continue;
                s.Final = Math.Clamp(target, s.Min, s.Final); available += largest - s.Final; _slots[i] = s;
            }
            if (Mathf.IsZeroApprox(available) || Mathf.IsEqualApprox(before, available)) break;
        }
        var desiredSize = (float)checked(sep * (_slots.Count - 1)); _finalSizes.Clear(); foreach (var s in _slots) { desiredSize += s.Final; _finalSizes.Add(s.Final); }
        if (ratios > 0 && (desiredSize > size || propagating && desiredSize < size))
        {
            var newSize = Size; if (_vertical) newSize.Y = desiredSize; else newSize.X = desiredSize; Size = newSize; DefaultPositions();
        }
        _offsets.Clear(); var pos = 0;
        for (var i = 0; i < _defaults.Count; i++) { pos = checked(pos + Pixel(_finalSizes[i])); _offsets.Add(checked(pos - _defaults[i])); pos = checked(pos + sep); }
        if (_offsets.Count == 0) _offsets.Add(0);
    }
    private void Arrange()
    {
        if (_arranging) { _again = true; QueueSort(); return; }
        if (!IsInsideTree || !IsVisibleInTree) return;
        _arranging = true;
        try
        {
            for (var pass = 0; pass < 64; pass++) { _again = false; Synchronize(true); ArrangeOnce(); if (!_again || IsDisposed || !IsInsideTree) return; }
            throw new InvalidOperationException("Split layout callbacks did not settle after 64 passes.");
        }
        finally { _arranging = false; _again = false; _slots.Clear(); }
    }
    private void ArrangeOnce()
    {
        var tree = Tree; PrepareSlots(); EnsureDraggers();
        if (_children.Count < 2)
        {
            foreach (var dragger in _draggers) dragger.Visible = false;
            if (_children.Count == 1) FitChildInRect(_children[0], new(Vector2.Zero, Size));
            return;
        }
        DefaultPositions(); Positions(); var size = Size; var sep = Separation; var rtl = IsLayoutRTL(); var axis = AxisSize;
        for (var i = 0; i < _slots.Count; i++)
        {
            var start = !_vertical && rtl ? i == _positions.Count ? 0 : checked(_positions[i] + sep) : i == 0 ? 0 : checked(_positions[i - 1] + sep);
            var end = !_vertical && rtl ? i == 0 ? axis : _positions[i - 1] : i == _positions.Count ? axis : _positions[i];
            var s = _slots[i]; s.Rect = AxisRect(start, checked(end - start), _vertical ? size.X : size.Y);
            if (!s.Rect.IsFinite() || s.Rect.Size.X < 0 || s.Rect.Size.Y < 0) throw new InvalidOperationException("Split constraints produce an invalid panel allocation."); _slots[i] = s;
        }
        List<Exception>? errors = null;
        for (var i = 0; i < _slots.Count; i++)
        {
            var s = _slots[i]; if (IsDisposed || !ReferenceEquals(Tree, tree)) break;
            if (!s.Child.IsDisposed && ReferenceEquals(s.Child.Parent, this) && Sortable(s.Child)) try { FitChildInRect(s.Child, s.Rect); } catch (Exception error) { CollectException(ref errors, error); }
        }
        if (!IsDisposed && ReferenceEquals(Tree, tree)) for (var i = 0; i < _draggers.Count; i++)
            {
                var d = _draggers[i]; if (d.IsDisposed) continue;
                try
                {
                    var thickness = Math.Max(sep, GetThemeConstant("minimum_grab_thickness")); var shift = (thickness - sep) * .5f; var cross = (_vertical ? size.X : size.Y) - _marginBegin - _marginEnd;
                    var bar = _vertical ? new Rect2(rtl ? _marginEnd : _marginBegin, _positions[i], cross, sep) : new Rect2(_positions[i], _marginBegin, sep, cross);
                    var rect = _vertical ? new Rect2(bar.Position.X, bar.Position.Y - shift + _dragOffset, cross, thickness) : new Rect2(bar.Position.X - shift + _dragOffset * (rtl ? -1 : 1), bar.Position.Y, thickness, cross);
                    d.Bar = _vertical ? new Rect2(0, Pixel(shift) - _dragOffset, cross, sep) : new Rect2(Pixel(shift) - _dragOffset * (rtl ? -1 : 1), 0, sep, cross);
                    d.SetContainerRect(rect with { Size = rect.Size.Max(Vector2.Zero) }); d.MouseFilter = _enabled ? MouseFilter.Stop : MouseFilter.Ignore; d.Visible = !_collapsed; d.UpdateTouch(); d.QueueRedraw();
                }
                catch (Exception error) { CollectException(ref errors, error); }
            }
        ThrowCollected("Split layout callbacks failed.", errors);
    }
}
