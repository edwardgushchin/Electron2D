namespace Electron2D;

public partial class SpinBox
{
    private void Arrow(bool up)
    {
        var step = _arrowStep != 0 ? _arrowStep : Step;
        var value = Value;
        if (_arrowRound)
        {
            step = Mathf.Snapped(step, Step); var next = CalculateWithStep(value, step);
            if (up && next <= value || !up && next >= value) next = CalculateWithStep(value + (up ? step : -step), step);
            Value = next;
        }
        else Value = value + (up ? step : -step);
    }
    private void EndInteraction()
    {
        _repeat = _dragAllowed = _upPressed = _downPressed = false;
        List<Exception>? errors = null;
        if (_nativeCapture)
        {
            _nativeCapture = false;
            try { if (DisplayServer.IsAvailable && DisplayServer.HasFeature(DisplayServer.Feature.MouseWarp)) Input.MouseMode = MouseMode.Hidden; } catch (Exception error) { CollectException(ref errors, error); }
            try { if (DisplayServer.IsAvailable && DisplayServer.HasFeature(DisplayServer.Feature.MouseWarp)) { var point = GetViewport()!.GetScreenTransform() * GetGlobalTransformWithCanvas() * _capturePosition; Input.WarpMouse(point); } } catch (Exception error) { CollectException(ref errors, error); }
            try { if (DisplayServer.IsAvailable) Input.MouseMode = _priorMouseMode; } catch (Exception error) { CollectException(ref errors, error); }
        }
        _dragging = false; if (!IsDisposed) { SetInternalProcessing(false, false); QueueRedraw(); }
        ThrowCollected("SpinBox pointer restoration failed.", errors);
    }
    private void BeginDrag()
    {
        if (DisplayServer.IsAvailable) { _priorMouseMode = Input.MouseMode; Input.MouseMode = MouseMode.Captured; _nativeCapture = true; }
        _dragging = true; _dragValue = Value; _dragDifference = 0; _repeat = false; SetInternalProcessing(false, false); QueueRedraw();
    }
    /// <inheritdoc />
    protected override void OnGUIInput(InputEvent input)
    {
        base.OnGUIInput(input); if (!Editable) return; ComputeSizes();
        if (input is InputEventMouse mouse) { _mousePosition = mouse.Position; var up = _upRect.HasPoint(mouse.Position); var down = _downRect.HasPoint(mouse.Position); if (up != _upHover || down != _downHover) { _upHover = up; _downHover = down; QueueRedraw(); } }
        if (input is InputEventMouseButton button)
        {
            if (button.Pressed && button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            { if (_line.IsEditing()) { Value += Step * button.Factor * (button.ButtonIndex == MouseButton.WheelUp ? 1 : -1); AcceptEvent(); } return; }
            if (button.ButtonIndex == MouseButton.Left)
            {
                if (!button.Pressed) { EndInteraction(); return; }
                _accepted = true; var up = _upRect.HasPoint(button.Position); var down = _downRect.HasPoint(button.Position);
                if (!up && !down) return;
                _line.GrabFocus(true); _upPressed = up; _downPressed = down; _dragAllowed = true; _capturePosition = button.Position; _repeat = true; _repeatRemaining = .6; SetInternalProcessing(true, false);
                List<Exception>? errors = null; try { Arrow(up); } catch (Exception error) { CollectException(ref errors, error); }
                try { if (!IsDisposed) { QueueRedraw(); AcceptEvent(); } } catch (Exception error) { CollectException(ref errors, error); }
                ThrowCollected("SpinBox arrow callbacks failed.", errors);
            }
            else if (button.Pressed && button.ButtonIndex == MouseButton.Right)
            { var up = _upRect.HasPoint(button.Position); var down = _downRect.HasPoint(button.Position); if (up || down) { _line.GrabFocus(true); Value = up ? MaxValue : MinValue; AcceptEvent(); } }
        }
        else if (input is InputEventMouseMotion motion && (motion.ButtonMask & MouseButtonMask.Left) != 0)
        {
            if (_dragging) { _dragDifference += motion.Relative.Y; var difference = -.01 * Math.Pow(Math.Abs(_dragDifference), 1.8) * Math.Sign(_dragDifference); Value = Math.Clamp(_dragValue + Step * difference, MinValue, MaxValue); AcceptEvent(); }
            else if (_dragAllowed && _capturePosition.DistanceTo(motion.Position) > 2) { BeginDrag(); AcceptEvent(); }
        }
    }
    private void Repeat()
    {
        if (!_repeat || _dragging || !Editable || !IsVisibleInTree || !_line.HasFocus() || !CanProcess()) { EndInteraction(); return; }
        _repeatRemaining -= ProcessDeltaTime; if (_repeatRemaining > 0) return;
        _repeatRemaining += .075;
        if (_upRect.HasPoint(_mousePosition) || _downRect.HasPoint(_mousePosition)) Arrow(_upRect.HasPoint(_mousePosition));
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { base.OnNotification(what); RefreshText(true); ComputeSizes(); DrawArrows(); return; }
        List<Exception>? errors = null; try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed && _line != null) try
            {
                switch (what)
                {
                    case NotificationEnterTree: ComputeSizes(); RefreshText(); break;
                    case NotificationExitTree: EndInteraction(); break;
                    case NotificationVisibilityChanged: if (!IsVisibleInTree) EndInteraction(); break;
                    case NotificationMouseExit: _upHover = _downHover = false; QueueRedraw(); break;
                    case NotificationInternalProcess: Repeat(); break;
                    case NotificationResized: ComputeSizes(); break;
                    case NotificationThemeChanged: ClearTextCache(); ComputeSizes(); _line.UpdateMinimumSize(); UpdateMinimumSize(); QueueRedraw(); break;
                    case NotificationLayoutDirectionChanged: ComputeSizes(); QueueRedraw(); break;
                    case NotificationTranslationChanged: ClearTextCache(); RefreshText(); break;
                }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("SpinBox notification callbacks failed.", errors);
    }
    private void ComputeSizes()
    {
        if (_line == null) return;
        var separation = GetThemeConstant("field_and_buttons_separation"); var width = GetThemeConstant("buttons_width");
        if (width < 0) { width = 0; foreach (var name in ArrowIcons) width = Math.Max(width, (int)(GetThemeIcon(name)?.GetWidth() ?? 0)); }
        _buttonBlock = checked(width + separation); var height = Math.Max(0, (int)Size.Y); var gap = Math.Clamp(GetThemeConstant("buttons_vertical_separation"), 0, height); var first = (height - gap) / 2; var second = height - first - gap; var left = IsLayoutRTL() ? 0 : Size.X - width;
        _upRect = new(left, 0, width, first); _downRect = new(left, height - second, width, second); _buttonSeparator = new(left, first, width, gap); _fieldSeparator = new(IsLayoutRTL() ? width : Size.X - _buttonBlock, 0, separation, height);
        var position = new Vector2(IsLayoutRTL() ? _buttonBlock : 0, 0); var size = new Vector2(Math.Max(0, Size.X - _buttonBlock), Size.Y); if (_line.Position != position) _line.Position = position; if (_line.Size != size) _line.Size = size;
    }
    private static readonly string[] ArrowIcons = ["up", "up_hover", "up_pressed", "up_disabled", "down", "down_hover", "down_pressed", "down_disabled"];
    private static readonly string[] ArrowStyles = ["up_background", "up_background_hovered", "up_background_pressed", "up_background_disabled", "down_background", "down_background_hovered", "down_background_pressed", "down_background_disabled"];
    private static readonly string[] ArrowColors = ["up_icon_modulate", "up_hover_icon_modulate", "up_pressed_icon_modulate", "up_disabled_icon_modulate", "down_icon_modulate", "down_hover_icon_modulate", "down_pressed_icon_modulate", "down_disabled_icon_modulate"];
    private void DrawArrows()
    {
        if (GetThemeStyleBox("field_and_buttons_separator") is { } field) DrawStyleBox(field, _fieldSeparator);
        if (GetThemeStyleBox("up_down_buttons_separator") is { } divider) DrawStyleBox(divider, _buttonSeparator);
        for (var i = 0; i < 2; i++)
        {
            var up = i == 0; var disabled = !Editable || up && Value == MaxValue && !AllowGreater || !up && Value == MinValue && !AllowLesser;
            var state = i * 4 + (disabled ? 3 : !_dragging && (up ? _upPressed : _downPressed) ? 2 : !_dragging && (up ? _upHover : _downHover) ? 1 : 0); var rect = up ? _upRect : _downRect;
            var style = GetThemeStyleBox(ArrowStyles[state]); if (style != null) DrawStyleBox(style, rect);
            var icon = GetThemeIcon(ArrowIcons[state]); if (icon is { IsDisposed: false }) DrawTexture(icon, rect.Position + (rect.Size - icon.GetSize()) / 2, GetThemeColor(ArrowColors[state]));
        }
    }
}
