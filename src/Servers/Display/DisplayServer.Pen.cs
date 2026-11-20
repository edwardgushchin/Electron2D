using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private Dictionary<uint, PenContact>? _pens;
    private (DisplayServer Window, uint ID, ulong Timestamp)? _pendingPenMotion;

    private sealed class PenContact
    {
        internal float Pressure;
        internal Vector2 Tilt;
        internal bool Eraser;
        internal Vector2? Position;
        internal Vector2? LastMotionPosition;
        internal ulong LastMotionTimestamp;
        internal int? Index;
        internal MouseButtonMask Buttons;
    }

    private static bool IsEmulatedPenEvent(SDL.EventType type, SDL.Event value) => type switch
    {
        SDL.EventType.MouseMotion => value.Motion.Which == unchecked((uint)SDL.PenMouseID),
        SDL.EventType.MouseButtonDown or SDL.EventType.MouseButtonUp =>
            value.Button.Which == unchecked((uint)SDL.PenMouseID),
        SDL.EventType.FingerDown or SDL.EventType.FingerUp or SDL.EventType.FingerMotion or SDL.EventType.FingerCanceled =>
            value.TFinger.TouchID == unchecked((ulong)SDL.PenTouchID),
        _ => false,
    };

    private void DispatchPenEvent(SDL.EventType type, SDL.Event source, ref List<Exception>? failures)
    {
        var (windowID, id, timestamp) = type switch
        {
            SDL.EventType.PenMotion => (source.PMotion.WindowID, source.PMotion.Which, source.PMotion.Timestamp),
            SDL.EventType.PenAxis => (source.PAxis.WindowID, source.PAxis.Which, source.PAxis.Timestamp),
            SDL.EventType.PenDown or SDL.EventType.PenUp =>
                (source.PTouch.WindowID, source.PTouch.Which, source.PTouch.Timestamp),
            SDL.EventType.PenButtonDown or SDL.EventType.PenButtonUp =>
                (source.PButton.WindowID, source.PButton.Which, source.PButton.Timestamp),
            SDL.EventType.PenProximityIn or SDL.EventType.PenProximityOut =>
                (source.PProximity.WindowID, source.PProximity.Which, source.PProximity.Timestamp),
            _ => (0u, 0u, 0ul),
        };
        var window = windowID == _sdlWindowId ? this : _nativeChildren.GetValueOrDefault(windowID);
        // SDL can enqueue motion before the axes of the same hardware sample.
        if (type is not (SDL.EventType.PenMotion or SDL.EventType.PenAxis) ||
            _pendingPenMotion != (window, id, timestamp))
        {
            FlushPenMotion(ref failures);
        }

        if (type == SDL.EventType.PenProximityOut && windowID == 0)
        {
            List<Exception>? errors = null;
            foreach (var candidate in _childWindows.Values.Prepend(this).ToArray())
            {
                try { candidate.RemovePen(id); }
                catch (Exception error) { Node.CollectException(ref errors, error); }
            }

            Node.ThrowCollected("Pen proximity cancellation failed.", errors);
            return;
        }
        if (window is not { IsDisposed: false })
            return;

        switch (type)
        {
            case SDL.EventType.PenMotion:
                {
                    var position = window.PenPosition(source.PMotion.X, source.PMotion.Y);
                    var pen = window.GetPen(id);
                    pen.Position = position;
                    pen.Eraser = (source.PMotion.PenState & SDL.PenInputFlags.EraserTip) != 0;
                    _pendingPenMotion = (window, id, timestamp);
                    break;
                }
            case SDL.EventType.PenAxis:
                {
                    var axis = source.PAxis.Axis;
                    var value = source.PAxis.Value;
                    if (!float.IsFinite(value) || axis is not (SDL.PenAxis.Pressure or SDL.PenAxis.XTilt or SDL.PenAxis.YTilt))
                        return;
                    var pen = window.GetPen(id);
                    if (axis == SDL.PenAxis.Pressure)
                        pen.Pressure = Math.Clamp(value, 0f, 1f);
                    else
                    {
                        var tilt = MathF.Sin(Math.Clamp(value, -90f, 90f) * (MathF.PI / 180f));
                        pen.Tilt = axis == SDL.PenAxis.XTilt ? new Vector2(tilt, pen.Tilt.Y) : new Vector2(pen.Tilt.X, tilt);
                    }
                    if (pen.Index is not null)
                    {
                        pen.Eraser = (source.PAxis.PenState & SDL.PenInputFlags.EraserTip) != 0;
                        _pendingPenMotion = (window, id, timestamp);
                    }
                    break;
                }
            case SDL.EventType.PenDown:
            case SDL.EventType.PenUp:
                window.DispatchPenTouch(source.PTouch, type == SDL.EventType.PenDown);
                break;
            case SDL.EventType.PenButtonDown:
            case SDL.EventType.PenButtonUp:
                window.DispatchPenButton(source.PButton, type == SDL.EventType.PenButtonDown);
                break;
            case SDL.EventType.PenProximityOut:
                window.RemovePen(id);
                break;
        }
    }

    private PenContact GetPen(uint id)
    {
        _pens ??= new Dictionary<uint, PenContact>();
        if (!_pens.TryGetValue(id, out var pen))
        {
            pen = new PenContact();
            _pens.Add(id, pen);
        }
        return pen;
    }

    private Vector2 PenPosition(float x, float y)
    {
        var scale = GetMousePixelScale();
        var position = new Vector2(x * scale, y * scale);
        if (!position.IsFinite())
            throw new ArgumentOutOfRangeException(nameof(x), "Native pen coordinates must be finite.");
        return position;
    }

    private void FlushPenMotion(ref List<Exception>? failures)
    {
        if (_pendingPenMotion is not { } sample)
            return;
        _pendingPenMotion = null;
        var (window, id, timestamp) = sample;
        if (!window.IsDisposed && window._pens?.GetValueOrDefault(id) is { Position: { } position } pen)
        {
            try { window.PublishPenMotion(id, timestamp, pen, position); }
            catch (Exception error) { Node.CollectException(ref failures, error); }
        }
    }

    private void PublishPenMotion(uint id, ulong timestamp, PenContact pen, Vector2 position)
    {
        var relative = pen.LastMotionPosition is { } last ? position - last : Vector2.Zero;
        var dt = timestamp > pen.LastMotionTimestamp ? (timestamp - pen.LastMotionTimestamp) / 1_000_000_000f : 0f;
        var root = _rootDisplay ?? this;
        var velocity = dt > 0f && (pen.Index is not null || root._mouseMode != MouseMode.Captured)
            ? relative / dt : Vector2.Zero;
        if (!relative.IsFinite() || !velocity.IsFinite())
            throw new ArgumentOutOfRangeException(nameof(position), "Native pen movement and velocity must be finite.");
        pen.LastMotionPosition = position;
        pen.LastMotionTimestamp = timestamp;

        if (pen.Index is int index)
        {
            using var drag = new InputEventScreenDrag
            {
                WindowID = WindowID,
                Device = unchecked((int)id),
                Index = index,
                Position = position,
                Relative = relative,
                ScreenRelative = relative,
                Velocity = velocity,
                ScreenVelocity = velocity,
                Pressure = pen.Pressure,
                Tilt = pen.Tilt,
                PenInverted = pen.Eraser,
            };
            ParsePenInput(drag);
            return;
        }

        _lastClientMousePosition = position;
        using var motion = new InputEventMouseMotion
        {
            WindowID = WindowID,
            Device = unchecked((int)id),
            Position = position,
            GlobalPosition = position,
            ButtonMask = GetPenButtons(id),
            Relative = relative,
            ScreenRelative = relative,
            Velocity = velocity,
            ScreenVelocity = velocity,
            Pressure = pen.Pressure,
            Tilt = pen.Tilt,
            PenInverted = pen.Eraser,
        };
        SetPointerModifiers(motion);
        ParsePenInput(motion);
    }

    private void DispatchPenButton(SDL.PenButtonEvent source, bool pressed)
    {
        // Match SDL's mouse-compatible assignments for the four mapped barrel buttons.
        var button = source.Button switch
        {
            1 => MouseButton.Right,
            2 => MouseButton.Middle,
            3 => MouseButton.XButton1,
            4 => MouseButton.XButton2,
            _ => MouseButton.None,
        };
        if (button == MouseButton.None)
            return;

        var mask = ButtonMask(button);
        var held = FindPenButtonHold(source.Which, mask);
        if (pressed == (held is not null))
            return;

        var position = PenPosition(source.X, source.Y);
        var pen = held ?? GetPen(source.Which);
        if (pressed || ReferenceEquals(pen, _pens?.GetValueOrDefault(source.Which)))
            pen.Position = position;

        UpdatePenButton(source.Which, pen, button, position, pressed, canceled: false, retiring: false);
    }

    private PenContact? FindPenButtonHold(uint id, MouseButtonMask button)
    {
        var root = _rootDisplay ?? this;
        if (root._pens?.GetValueOrDefault(id) is { } main && (main.Buttons & button) != 0)
            return main;

        foreach (var child in root._childWindows.Values)
        {
            if (child._pens?.GetValueOrDefault(id) is { } pen && (pen.Buttons & button) != 0)
                return pen;
        }

        return null;
    }

    private MouseButtonMask GetPenButtons(uint id)
    {
        var root = _rootDisplay ?? this;
        var buttons = root._pens?.GetValueOrDefault(id)?.Buttons ?? MouseButtonMask.None;
        foreach (var child in root._childWindows.Values)
            buttons |= child._pens?.GetValueOrDefault(id)?.Buttons ?? MouseButtonMask.None;

        return buttons;
    }

    private void UpdatePenButton(uint id, PenContact pen, MouseButton button, Vector2 position, bool pressed, bool canceled, bool retiring)
    {
        var mask = ButtonMask(button);
        pen.Buttons = pressed ? pen.Buttons | mask : pen.Buttons & ~mask;
        using var input = new InputEventMouseButton
        {
            WindowID = WindowID,
            Device = unchecked((int)id),
            ButtonIndex = button,
            ButtonMask = GetPenButtons(id) | pen.Buttons,
            Position = position,
            GlobalPosition = position,
            Pressed = pressed,
            Canceled = canceled,
        };
        SetPointerModifiers(input);
        if (retiring)
            Input.Service.RetirePointerRelease(input);
        else
            ParsePenInput(input);
    }

    private void DispatchPenTouch(SDL.PenTouchEvent source, bool pressed)
    {
        var pen = _pens?.GetValueOrDefault(source.Which);
        if (pressed == (pen?.Index is not null))
            return;

        var position = PenPosition(source.X, source.Y);
        pen ??= GetPen(source.Which);
        pen.Position = position;
        pen.LastMotionPosition = position;
        pen.LastMotionTimestamp = source.Timestamp;
        pen.Eraser = source.Eraser || (source.PenState & SDL.PenInputFlags.EraserTip) != 0;
        if (pressed)
        {
            var root = _rootDisplay ?? this;
            pen.Index = root._freeTouchIndexes.TryPop(out var index) ? index : root._nextTouchIndex++;
            PublishPenTouch(source.Which, pen.Index.Value, position, pressed: true, canceled: false);
        }
        else
        {
            pen.Pressure = 0f;
            ReleasePenTouch(source.Which, pen, canceled: false);
        }
    }

    private void ReleasePenTouch(uint id, PenContact pen, bool canceled, bool retiring = false)
    {
        if (pen.Index is not int index)
            return;
        pen.Index = null;
        try { PublishPenTouch(id, index, pen.Position!.Value, pressed: false, canceled, retiring); }
        finally { (_rootDisplay ?? this)._freeTouchIndexes.Push(index); }
    }

    private void PublishPenTouch(uint id, int index, Vector2 position, bool pressed, bool canceled, bool retiring = false)
    {
        using var touch = new InputEventScreenTouch
        {
            WindowID = WindowID,
            Device = unchecked((int)id),
            Index = index,
            Position = position,
            Pressed = pressed,
            Canceled = canceled,
        };
        if (retiring)
            Input.Service.RetirePointerRelease(touch);
        else
            ParsePenInput(touch);
    }

    private void ParsePenInput(InputEvent input)
    {
        var root = _rootDisplay ?? this;
        var previous = root._eventInputWindow;
        root._eventInputWindow = this;
        try { Input.ParseInputEvent(input); }
        finally { root._eventInputWindow = previous; }
    }

    private void RemovePen(uint id)
    {
        if (_pens is null || !_pens.Remove(id, out var pen))
            return;

        List<Exception>? errors = null;
        ReleasePen(id, pen, retiring: false, ref errors);
        Node.ThrowCollected("Pen cancellation failed.", errors);
    }

    private void ReleasePen(uint id, PenContact pen, bool retiring, ref List<Exception>? errors)
    {
        try { ReleasePenTouch(id, pen, canceled: true, retiring); }
        catch (Exception error) { Node.CollectException(ref errors, error); }

        ReadOnlySpan<MouseButton> buttons = [MouseButton.Right, MouseButton.Middle, MouseButton.XButton1, MouseButton.XButton2];
        foreach (var button in buttons)
        {
            if ((pen.Buttons & ButtonMask(button)) == 0)
                continue;

            try { UpdatePenButton(id, pen, button, pen.Position!.Value, pressed: false, canceled: true, retiring); }
            catch (Exception error) { Node.CollectException(ref errors, error); }
        }
    }

    private void CancelPens(bool retiring = false)
    {
        if (_pens is not { Count: > 0 } pens)
            return;
        _pens = null;
        List<Exception>? errors = null;
        foreach (var (id, pen) in pens)
            ReleasePen(id, pen, retiring, ref errors);

        Node.ThrowCollected("Pen cancellation failed.", errors);
    }
}
