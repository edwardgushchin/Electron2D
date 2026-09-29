namespace Electron2D;

public partial class ScrollContainer
{
    private Vector2 _dragSpeed, _dragAccum, _dragFrom, _lastDragAccum;
    private double _timeSinceMotion;
    private bool _dragTouching, _dragDecelerating, _beyondDeadzone;

    private void CancelDrag()
    {
        var ended = _beyondDeadzone;
        _dragTouching = _dragDecelerating = _beyondDeadzone = false;
        _dragSpeed = _dragAccum = _dragFrom = _lastDragAccum = Vector2.Zero;
        _timeSinceMotion = 0;
        if (!IsDisposed) SetInternalProcessing(false, false);
        if (!ended || IsDisposed) return;
        List<Exception>? errors = null;
        try { ScrollEnded?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        try { PropagateNotification(NotificationScrollEnd); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Scroll end callbacks failed.", errors);
    }

    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(inputEvent);
        ObjectDisposedException.ThrowIf(inputEvent.IsDisposed, inputEvent);
        var beforeH = _hBar.Value;
        var beforeV = _vBar.Value;
        if (inputEvent is InputEventMouseButton button)
        {
            if (button.Pressed && button.ButtonIndex is >= MouseButton.WheelUp and <= MouseButton.WheelRight)
            {
                var swap = _horizontalByDefault != button.ShiftPressed;
                var verticalWheel = button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown;
                var direction = button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelLeft ? -1 : 1;
                var preferH = verticalWheel ? swap || !VVisible && _verticalMode != ScrollMode.ShowNever :
                    !swap && (HVisible || _horizontalMode == ScrollMode.ShowNever);
                ScrollBar? bar = preferH ? HEnabled ? _hBar : VEnabled ? _vBar : null : VEnabled ? _vBar : HEnabled ? _hBar : null;
                if (bar is not null)
                    bar.Scroll(direction * bar.Page / ScrollBar.PageDivisor * button.Factor);
            }

            if (button.ButtonIndex == MouseButton.Left &&
                (!button.Pressed && _dragTouching || DisplayServer.Instance?.IsTouchscreenAvailable() == true || Input.Instance.EmulateTouchFromMouse))
            {
                if (button.Pressed)
                {
                    if (_dragTouching) CancelDrag();
                    _dragSpeed = _dragAccum = _lastDragAccum = Vector2.Zero;
                    _dragFrom = new((float)beforeH, (float)beforeV);
                    _dragTouching = true;
                    _dragDecelerating = _beyondDeadzone = false;
                    _timeSinceMotion = 0;
                    SetInternalProcessing(true, false);
                }
                else if (_dragTouching)
                {
                    if (_dragSpeed == Vector2.Zero) CancelDrag();
                    else _dragDecelerating = true;
                }
            }
        }
        else if (inputEvent is InputEventMouseMotion motion && _dragTouching && !_dragDecelerating)
        {
            _dragAccum -= motion.Relative;
            if (_beyondDeadzone || HEnabled && Math.Abs(_dragAccum.X) > _deadzone || VEnabled && Math.Abs(_dragAccum.Y) > _deadzone)
            {
                if (!_beyondDeadzone)
                {
                    _beyondDeadzone = true;
                    _dragAccum = -motion.Relative;
                    List<Exception>? errors = null;
                    try { PropagateNotification(NotificationScrollBegin); } catch (Exception error) { CollectException(ref errors, error); }
                    try { ScrollStarted?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
                    ThrowCollected("Scroll begin callbacks failed.", errors);
                }
                if (HEnabled) _hBar.ScrollTo(_dragFrom.X + _dragAccum.X);
                else _dragAccum.X = 0;
                if (VEnabled) _vBar.ScrollTo(_dragFrom.Y + _dragAccum.Y);
                else _dragAccum.Y = 0;
                _timeSinceMotion = 0;
            }
        }
        else if (inputEvent is InputEventPanGesture pan)
        {
            if (HEnabled) _hBar.Scroll(_hBar.Page * pan.Delta.X / ScrollBar.PageDivisor);
            if (VEnabled) _vBar.Scroll(_vBar.Page * pan.Delta.Y / ScrollBar.PageDivisor);
        }
        if (_hBar.Value != beforeH || _vBar.Value != beforeV) AcceptEvent();
    }

    private void ProcessTouchDrag(double delta)
    {
        if (!_dragTouching || !double.IsFinite(delta) || delta < 0) return;
        if (_dragDecelerating)
        {
            var x = HEnabled ? Math.Clamp(_hBar.Value + _dragSpeed.X * delta, _hBar.MinValue, _hBar.MaxValue - _hBar.Page) : _hBar.Value;
            var y = VEnabled ? Math.Clamp(_vBar.Value + _dragSpeed.Y * delta, _vBar.MinValue, _vBar.MaxValue - _vBar.Page) : _vBar.Value;
            if (HEnabled) _hBar.ScrollTo(x);
            if (VEnabled) _vBar.ScrollTo(y);
            var friction = 1000 * delta;
            var nextX = Math.Max(0, Math.Abs(_dragSpeed.X) - friction);
            var nextY = Math.Max(0, Math.Abs(_dragSpeed.Y) - friction);
            _dragSpeed = new((float)(x <= _hBar.MinValue || x >= _hBar.MaxValue - _hBar.Page ? 0 : Math.CopySign(nextX, _dragSpeed.X)),
                (float)(y <= _vBar.MinValue || y >= _vBar.MaxValue - _vBar.Page ? 0 : Math.CopySign(nextY, _dragSpeed.Y)));
            if (_dragSpeed == Vector2.Zero) CancelDrag();
        }
        else
        {
            // Sub-microsecond ticks cannot produce a useful float velocity sample.
            if (delta >= 1e-6 && (_timeSinceMotion == 0 || _timeSinceMotion > .1))
            {
                var change = _dragAccum - _lastDragAccum;
                _dragSpeed = new((float)Math.Clamp(change.X / delta, -float.MaxValue, float.MaxValue),
                    (float)Math.Clamp(change.Y / delta, -float.MaxValue, float.MaxValue));
                _lastDragAccum = _dragAccum;
            }
            _timeSinceMotion += delta;
        }
    }
}
