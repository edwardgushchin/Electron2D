namespace Electron2D;

public partial class SplitContainer
{
    private int _forceGrabber = -1;
    private bool _leaving;
    private sealed class Dragger : Control
    {
        internal readonly SplitContainer Splitter;
        internal readonly int Index;
        internal Rect2 Bar;
        internal bool Dragging;
        private bool _hover;
        private int _from, _start;
        private TextureRect? _touchControl;
        private readonly bool _multi;
        internal Dragger(SplitContainer owner, int index, bool multi = false)
        {
            Splitter = owner; Index = index; _multi = multi; FocusMode = multi ? FocusMode.None : FocusMode.Accessibility; MouseFilter = multi ? MouseFilter.Pass : MouseFilter.Stop;
            MouseEntered += () => { _hover = true; QueueRedraw(); if (_multi && !Dragging && !Input.Instance.IsMouseButtonPressed(MouseButton.Left)) Splitter.ShowGrabber(Index); };
            MouseExited += () => { _hover = false; QueueRedraw(); if (_multi && !Dragging && !Input.Instance.IsMouseButtonPressed(MouseButton.Left)) Splitter.ShowGrabber(-1); };
        }
        private bool Available => !Splitter.IsDisposed && !Splitter._leaving && Splitter.IsInsideTree && Splitter.IsVisibleInTree && !Splitter._collapsed && Splitter._enabled && Splitter._children.Count >= 2 &&
            (!_multi || Parent is Dragger parent && parent.Available);
        internal void UpdateTouch()
        {
            if (_multi || IsDisposed) return;
            if (Splitter._touch && _touchControl is null)
            {
                _touchControl = new TextureRect { Name = "_split_touch", Modulate = Splitter.GetThemeColor("touch_dragger_color") };
                _touchControl.GUIInput += TouchInput;
                _touchControl.MouseExited += () => { if (!Dragging && _touchControl is { IsDisposed: false }) _touchControl.Modulate = Splitter.GetThemeColor("touch_dragger_color"); };
                AddChild(_touchControl, InternalMode.Front);
            }
            else if (!Splitter._touch && _touchControl is { } old)
            {
                _touchControl = null; RemoveChild(old); old.Dispose();
            }
            if (_touchControl is not { } touch) return;
            touch.Texture = Splitter.TouchIcon; touch.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
            touch.MouseDefaultCursorShape = Splitter._vertical ? CursorShape.VSplit : CursorShape.HSplit;
            touch.Visible = Splitter._enabled;
        }
        private void TouchInput(InputEvent input)
        {
            if (_touchControl is not { } touch) return;
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } button) touch.Modulate = Splitter.GetThemeColor(button.Pressed ? "touch_dragger_pressed_color" : "touch_dragger_color");
            else if (input is InputEventMouseMotion && !Dragging) touch.Modulate = Splitter.GetThemeColor("touch_dragger_hover_color");
        }
        internal void Stop()
        {
            if (!Dragging) return; Dragging = false; List<Exception>? errors = null;
            if (!IsDisposed) QueueRedraw();
            if (_multi && !Splitter.IsDisposed) try { Splitter.ShowGrabber(-1); } catch (Exception error) { CollectException(ref errors, error); }
            try { Splitter.DragEnded?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
            for (var i = 0; !IsDisposed && i < GetChildCount(includeInternal: true); i++) if (GetChild(i, includeInternal: true) is Dragger { _multi: true } child)
                    try { child.Stop(); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Split release callbacks failed.", errors);
        }
        protected override CursorShape OnGetCursorShape(Vector2 atPosition) => Available ? _multi ? CursorShape.Drag : Splitter._vertical ? CursorShape.VSplit : CursorShape.HSplit : base.OnGetCursorShape(atPosition);
        protected override void OnGUIInput(InputEvent input)
        {
            if (!Available || Index >= Splitter._offsets.Count) return;
            if (!_multi)
            {
                var rtl = !Splitter._vertical && IsLayoutRTL(); var decrease = Splitter._vertical ? "ui_up" : rtl ? "ui_right" : "ui_left"; var increase = Splitter._vertical ? "ui_down" : rtl ? "ui_left" : "ui_right";
                var minus = InputMap.Instance.HasAction(decrease) && input.IsActionPressed(decrease, true); var plus = InputMap.Instance.HasAction(increase) && input.IsActionPressed(increase, true);
                if (minus || plus)
                {
                    Splitter.SetOffset(Index, Pixel(Splitter.Offset(Index) + Splitter.AxisSize * (minus ? -.1f : .1f))); Splitter.ClampSplitOffset(Index); AcceptEvent(); return;
                }
            }
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
            {
                if (button.Pressed)
                {
                    Splitter.ClampSplitOffset(); Dragging = true; QueueRedraw(); if (_multi) Splitter.ShowGrabber(Index);
                    List<Exception>? errors = null;
                    try { Splitter.DragStarted?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
                    if (Dragging && Available && Index < Splitter._offsets.Count) { _start = Splitter.Offset(Index); _from = Splitter.Axis(GetTransform() * button.Position); }
                    ThrowCollected("Split start callbacks failed.", errors);
                }
                else Stop();
                if (!_multi) AcceptEvent();
            }
            else if (input is InputEventMouseMotion motion && Dragging)
            {
                var current = Splitter.Axis(GetTransform() * motion.Position); var delta = checked(current - _from);
                Splitter.SetOffset(Index, checked(_start + (!Splitter._vertical && IsLayoutRTL() ? -delta : delta))); Splitter.ClampSplitOffset(Index);
                Splitter.Dragged?.Invoke(Splitter.Offset(Index));
            }
        }
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationFocusExit || what == NotificationExitTree || what == NotificationVisibilityChanged && !IsVisibleInTree) Stop();
            else if (what == NotificationThemeChanged) { UpdateTouch(); if (_touchControl is { } touch) touch.Modulate = Splitter.GetThemeColor("touch_dragger_color"); }
            else if (what == NotificationDraw && !_multi)
            {
                var style = Splitter.GetThemeStyleBox("split_bar_background") ?? throw new InvalidOperationException("A split bar style is required.");
                DrawStyleBox(style, Bar);
                if (!Splitter._touch && Splitter._visibility == DraggerVisibility.Visible && (Dragging || _hover || Splitter.GetThemeConstant("autohide") == 0 || Splitter._forceGrabber == Index))
                {
                    var icon = Splitter.Grabber; var natural = icon.GetSize(); var cross = Splitter._vertical ? Splitter.Size.X - natural.X : Splitter.Size.Y - natural.Y;
                    if (cross - Splitter._marginBegin - Splitter._marginEnd > 0) DrawTexture(icon, Bar.Position + (Bar.Size - natural) / 2);
                }
            }
        }
    }
    private void ShowGrabber(int index)
    {
        if (_forceGrabber == index) return; _forceGrabber = index; foreach (var dragger in _draggers) if (!dragger.IsDisposed) dragger.QueueRedraw();
    }
    private readonly List<(Dragger Control, SplitContainer Target, int ParentIndex, int TargetIndex)> _intersections = [];
    private readonly List<(SplitContainer Target, int ParentIndex, int TargetIndex)> _wanted = [];
    private int _intersectionID;
    private bool CanIntersect => !IsDisposed && !_leaving && IsInsideTree && IsVisibleInTree && _nested && _enabled && !_collapsed && _children.Count >= 2;
    private void ClearIntersections()
    {
        List<Exception>? errors = null;
        for (var i = _intersections.Count - 1; i >= 0; i--)
        {
            var control = _intersections[i].Control;
            try { control.Stop(); } catch (Exception error) { CollectException(ref errors, error); }
            try { if (control.Parent is { IsDisposed: false } parent) parent.RemoveChild(control); } catch (Exception error) { CollectException(ref errors, error); }
            try { control.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        _intersections.Clear(); _wanted.Clear(); ThrowCollected("Split intersection removal failed.", errors);
    }
    private bool Edited(Node node) => Tree?.EditedSceneRoot is { } root && (ReferenceEquals(root, node) || root.IsAncestorOf(node));
    private void FindIntersections(Control root, Control? first = null)
    {
        for (var i = 0; i < root.GetChildCount(); i++)
        {
            if (root.GetChild(i) is not Control child || child.TopLevel || Edited(child) != Edited(this)) continue;
            var direct = first ?? child;
            if (child is SplitContainer target && target.CanIntersect && target._vertical != _vertical)
            {
                var index = _children.IndexOf(direct);
                if (index >= 0)
                {
                    var outer = direct.GetGlobalRect(); var inner = target.GetGlobalRect(); var tolerance = GetThemeConstant("minimum_grab_thickness");
                    var start = MathF.Abs((_vertical ? outer.Position.Y : outer.Position.X) - (_vertical ? inner.Position.Y : inner.Position.X)) <= tolerance;
                    var end = MathF.Abs((_vertical ? outer.End.Y : outer.End.X) - (_vertical ? inner.End.Y : inner.End.X)) <= tolerance;
                    if (!_vertical && IsLayoutRTL()) (start, end) = (end, start);
                    for (var t = 0; t < target._children.Count - 1; t++)
                    {
                        if (start && index > 0) _wanted.Add((target, index - 1, t));
                        if (end && index < _draggers.Count && index < _children.Count - 1) _wanted.Add((target, index, t));
                    }
                }
            }
            FindIntersections(child, direct);
        }
    }
    private void UpdateIntersections()
    {
        if (!CanIntersect) { if (_intersections.Count != 0) ClearIntersections(); return; }
        _wanted.Clear(); FindIntersections(this);
        List<Exception>? errors = null;
        for (var i = _intersections.Count - 1; i >= 0; i--)
        {
            var current = _intersections[i]; var keep = false;
            foreach (var wanted in _wanted) if (ReferenceEquals(current.Target, wanted.Target) && current.ParentIndex == wanted.ParentIndex && current.TargetIndex == wanted.TargetIndex &&
                !current.Control.IsDisposed && current.ParentIndex < _draggers.Count && ReferenceEquals(current.Control.Parent, _draggers[current.ParentIndex])) { keep = true; break; }
            if (keep) continue;
            _intersections.RemoveAt(i);
            try { current.Control.Stop(); } catch (Exception error) { CollectException(ref errors, error); }
            try { if (current.Control.Parent is { IsDisposed: false } parent) parent.RemoveChild(current.Control); } catch (Exception error) { CollectException(ref errors, error); }
            try { current.Control.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        foreach (var wanted in _wanted)
        {
            Dragger? control = null;
            foreach (var current in _intersections) if (ReferenceEquals(current.Target, wanted.Target) && current.ParentIndex == wanted.ParentIndex && current.TargetIndex == wanted.TargetIndex) { control = current.Control; break; }
            if (control is null)
            {
                control = new Dragger(wanted.Target, wanted.TargetIndex, true) { Name = "_split_intersection_" + checked(++_intersectionID) };
                _intersections.Add((control, wanted.Target, wanted.ParentIndex, wanted.TargetIndex)); _draggers[wanted.ParentIndex].AddChild(control, InternalMode.Front);
            }
            if (control.Dragging || wanted.TargetIndex >= wanted.Target._draggers.Count) continue;
            try
            {
                var thickness = Math.Max(Separation, GetThemeConstant("minimum_grab_thickness")); control.Size = new(thickness, thickness);
                var parentPosition = _draggers[wanted.ParentIndex].GlobalPosition; var targetPosition = wanted.Target._draggers[wanted.TargetIndex].GlobalPosition;
                control.SetGlobalPosition(_vertical ? new(targetPosition.X, parentPosition.Y) : new(parentPosition.X, targetPosition.Y));
            }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        _wanted.Clear(); ThrowCollected("Split intersection callbacks failed.", errors);
    }
    private readonly record struct SourceState(Resource Resource, long Revision, bool Disposed);
    private readonly List<SourceState> _resources = [];
    private readonly List<Texture> _resident = [];
    private void ReleaseResidency() { foreach (var texture in _resident) texture.ReleaseRendererCacheResidency(); _resident.Clear(); }
    private void PollResources()
    {
        var count = 0; var changed = false;
        for (var family = 0; family < 3; family++)
        {
            Resource? resource = family == 0 ? Grabber : family == 1 ? TouchIcon : GetThemeStyleBox("split_bar_background");
            var chain = count;
            while (resource is not null)
            {
                var repeated = false; for (var i = chain; i < count; i++) if (ReferenceEquals(_resources[i].Resource, resource)) { repeated = true; break; }
                if (repeated) break;
                var next = new SourceState(resource, resource.ChangeRevision, resource.IsDisposed);
                if (count < _resources.Count) { changed |= _resources[count] != next; _resources[count] = next; } else { _resources.Add(next); changed = true; }
                count++; if (next.Disposed) break;
                resource = resource is AtlasTexture atlas ? atlas.Atlas : resource is StyleBoxTexture style ? style.Texture : null;
            }
        }
        if (count < _resources.Count) { _resources.RemoveRange(count, _resources.Count - count); changed = true; }
        for (var i = _resident.Count - 1; i >= 0; i--)
        {
            var keep = false; foreach (var state in _resources) if (ReferenceEquals(state.Resource, _resident[i]) && !state.Disposed) { keep = true; break; }
            if (!keep) { _resident[i].ReleaseRendererCacheResidency(); _resident.RemoveAt(i); }
        }
        foreach (var state in _resources) if (!state.Disposed && state.Resource is Texture texture && !_resident.Contains(texture)) { texture.AcquireRendererCacheResidency(); _resident.Add(texture); }
        if (!changed) return;
        UpdateMinimumSize(); QueueSort(); foreach (var dragger in _draggers) { dragger.UpdateTouch(); dragger.InvalidateCanvas(); }
    }
}
