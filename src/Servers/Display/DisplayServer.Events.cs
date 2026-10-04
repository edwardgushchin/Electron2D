using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private static readonly Dictionary<SDL.Keycode, Key> LogicalKeys = BuildLogicalKeys();
    private static readonly Dictionary<SDL.Scancode, Key> PhysicalKeys = BuildPhysicalKeys();

    private readonly Dictionary<(ulong Touch, ulong Finger), TouchContact> _touchContacts = new();
    private readonly Stack<int> _freeTouchIndexes = new();
    private MouseButtonMask _heldMouseButtons;
    private ulong _lastMouseMotionTimestamp;
    private int _nextTouchIndex;
    private bool _processingEvents;
    private bool _mouseHovering;
    private SDL.MouseMotionEvent _pendingMouseMotion;
    private bool _hasPendingMouseMotion;
    private List<string>? _pendingDroppedFiles;
    private SDL.Keymod _eventKeyModifiers;
    private bool _hasEventKeyModifiers;

    internal event Action? QuitRequestedCore;

    internal event Action? SystemThemeChangedCore;

    internal event Action? CloseRequestedCore;

    internal event Action? WindowMouseEnteredCore;

    internal event Action? WindowMouseExitedCore;

    internal event Action? WindowDpiChangedCore;

    internal event Action<Rect2i>? WindowRectChangedCore;

    internal event Action<bool>? WindowFocusChangedCore;

    internal event Action<string>? TextInputCore;

    internal event Action<string, Vector2i>? TextEditingCore;

    internal event Action<IReadOnlyList<string>>? FilesDroppedCore;

    internal void ProcessEventsCore() => ProcessEventsCore(dropInput: false);

    internal void ForceProcessAndDropEventsCore() => ProcessEventsCore(dropInput: true);

    private void ProcessEventsCore(bool dropInput)
    {
        EnsureOwner();
        if (_processingEvents)
            throw new InvalidOperationException("Display event processing cannot be re-entered.");
        if (!dropInput)
            Engine.MainLoop?.ValidateInputEventDispatch();

        _processingEvents = true;
        _hasEventKeyModifiers = false;
        List<Exception>? failures = null;
        try
        {
            DispatchPendingGamepadConnections(ref failures);
            if (dropInput)
            {
                _hasPendingMouseMotion = false;
                _touchContacts.Clear();
                _freeTouchIndexes.Clear();
                _nextTouchIndex = 0;
                _heldMouseButtons = MouseButtonMask.None;
                Input.ReleasePressedEvents();
            }
            while (SDL.PollEvent(out var nativeEvent))
            {
                if (dropInput && IsInputEvent((SDL.EventType)nativeEvent.Type))
                    continue;
                if ((SDL.EventType)nativeEvent.Type == SDL.EventType.MouseMotion &&
                    nativeEvent.Motion.WindowID == _sdlWindowId &&
                    nativeEvent.Motion.Which != SDL.TouchMouseID && Input.UseAccumulatedInput)
                {
                    if (_hasPendingMouseMotion && _pendingMouseMotion.Which != nativeEvent.Motion.Which)
                    {
                        try
                        {
                            FlushBufferedInput();
                        }
                        catch (Exception exception)
                        {
                            (failures ??= new List<Exception>()).Add(exception);
                        }
                    }
                    AccumulateMouseMotion(nativeEvent.Motion);
                    continue;
                }
                try
                {
                    FlushBufferedInput();
                }
                catch (Exception exception)
                {
                    (failures ??= new List<Exception>()).Add(exception);
                }
                try
                {
                    DispatchEvent(nativeEvent);
                }
                catch (Exception exception)
                {
                    (failures ??= new List<Exception>()).Add(exception);
                }
            }
            try
            {
                FlushBufferedInput();
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }
            try
            {
                DrainDialogCallbacks();
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }
        }
        finally
        {
            _processingEvents = false;
        }

        if (failures is not null)
            throw new AggregateException("Display event callbacks failed.", failures);
    }

    private static bool IsInputEvent(SDL.EventType type) => type is
        SDL.EventType.KeyDown or SDL.EventType.KeyUp or SDL.EventType.TextInput or SDL.EventType.TextEditing or
        SDL.EventType.MouseMotion or SDL.EventType.MouseButtonDown or SDL.EventType.MouseButtonUp or
        SDL.EventType.MouseWheel or SDL.EventType.FingerDown or SDL.EventType.FingerUp or
        SDL.EventType.FingerCanceled or SDL.EventType.FingerMotion or
        SDL.EventType.GamepadButtonDown or SDL.EventType.GamepadButtonUp or SDL.EventType.GamepadAxisMotion or
        SDL.EventType.JoystickButtonDown or SDL.EventType.JoystickButtonUp or SDL.EventType.JoystickAxisMotion;

    internal void FlushBufferedInput()
    {
        EnsureOwner();
        if (!_hasPendingMouseMotion)
            return;
        var motion = _pendingMouseMotion;
        _hasPendingMouseMotion = false;
        DispatchMouseMotion(motion);
    }

    private void AccumulateMouseMotion(SDL.MouseMotionEvent motion)
    {
        if (!_hasPendingMouseMotion)
        {
            _pendingMouseMotion = motion;
            _hasPendingMouseMotion = true;
            return;
        }
        _pendingMouseMotion.XRel += motion.XRel;
        _pendingMouseMotion.YRel += motion.YRel;
        _pendingMouseMotion.X = motion.X;
        _pendingMouseMotion.Y = motion.Y;
        _pendingMouseMotion.Timestamp = motion.Timestamp;
        _pendingMouseMotion.State = motion.State;
    }

    private void DispatchEvent(SDL.Event nativeEvent)
    {
        var type = (SDL.EventType)nativeEvent.Type;
        if (type is SDL.EventType.GamepadAdded or SDL.EventType.GamepadRemoved or SDL.EventType.GamepadRemapped or
            SDL.EventType.GamepadButtonDown or SDL.EventType.GamepadButtonUp or SDL.EventType.GamepadAxisMotion or
            SDL.EventType.JoystickAdded or SDL.EventType.JoystickRemoved or
            SDL.EventType.JoystickButtonDown or SDL.EventType.JoystickButtonUp or SDL.EventType.JoystickAxisMotion)
        {
            DispatchGamepadEvent(nativeEvent, type);
            return;
        }
        if (type == SDL.EventType.Quit)
        {
            QuitRequestedCore?.Invoke();
            return;
        }

        if (type == SDL.EventType.SystemThemeChanged)
        {
            SystemThemeChangedCore?.Invoke();
            return;
        }

        if (type is SDL.EventType.FingerDown or SDL.EventType.FingerUp or SDL.EventType.FingerCanceled or
            SDL.EventType.FingerMotion)
        {
            if (nativeEvent.TFinger.WindowID == _sdlWindowId && nativeEvent.TFinger.TouchID != SDL.MouseTouchID)
                DispatchTouch(nativeEvent.TFinger);
            return;
        }

        if (type is SDL.EventType.DropBegin or SDL.EventType.DropFile or SDL.EventType.DropComplete)
        {
            if (nativeEvent.Drop.WindowID == _sdlWindowId)
                DispatchDrop(type, type == SDL.EventType.DropFile
                    ? Marshal.PtrToStringUTF8(nativeEvent.Drop.Data) ?? string.Empty : null);
            return;
        }

        if (nativeEvent.Window.WindowID != _sdlWindowId)
            return;

        switch (type)
        {
            case SDL.EventType.WindowMouseEnter:
                if (!_mouseHovering)
                {
                    _mouseHovering = true;
                    WindowMouseEnteredCore?.Invoke();
                }
                break;
            case SDL.EventType.WindowMouseLeave:
                if (_mouseHovering)
                {
                    _mouseHovering = false;
                    WindowMouseExitedCore?.Invoke();
                }
                break;
            case SDL.EventType.WindowDisplayScaleChanged:
                if (_waylandWindowPosition)
                {
                    ReapplyWaylandWindowLimits();
                    RefreshBlankWindowSurface();
                }
                WindowDpiChangedCore?.Invoke();
                break;
            case SDL.EventType.WindowCloseRequested:
                _pendingDroppedFiles = null;
                CloseRequestedCore?.Invoke();
                break;
            case SDL.EventType.WindowResized when !_waylandWindowPosition:
            case SDL.EventType.WindowPixelSizeChanged when _waylandWindowPosition:
                if (_waylandWindowPosition)
                    RefreshBlankWindowSurface();
                var resizedRect = new Rect2i(_windowRect.Position,
                    new Vector2i(nativeEvent.Window.Data1, nativeEvent.Window.Data2));
                if (resizedRect != _windowRect)
                {
                    _windowRect = resizedRect;
                    WindowRectChangedCore?.Invoke(resizedRect);
                }
                break;
            case SDL.EventType.WindowMoved:
                if (_waylandWindowPosition)
                    break;
                var movedRect = new Rect2i(new Vector2i(nativeEvent.Window.Data1, nativeEvent.Window.Data2),
                    _windowRect.Size);
                if (movedRect != _windowRect)
                {
                    _windowRect = movedRect;
                    WindowRectChangedCore?.Invoke(movedRect);
                }
                break;
            case SDL.EventType.WindowFocusGained:
                DispatchFocusChanged(focused: true);
                break;
            case SDL.EventType.WindowFocusLost:
                _imeText = string.Empty;
                _imeSelection = Vector2i.Zero;
                _eventKeyModifiers = SDL.Keymod.None;
                _hasEventKeyModifiers = true;
                _touchContacts.Clear();
                _freeTouchIndexes.Clear();
                _nextTouchIndex = 0;
                _heldMouseButtons = MouseButtonMask.None;
                try { DispatchFocusChanged(focused: false); }
                finally { ApplyGamepadFocusPolicy(); }
                break;
            case SDL.EventType.KeyDown:
            case SDL.EventType.KeyUp:
                DispatchKey(nativeEvent.Key);
                break;
            case SDL.EventType.TextInput:
                _imeText = string.Empty;
                _imeSelection = Vector2i.Zero;
                TextInputCore?.Invoke(Marshal.PtrToStringUTF8(nativeEvent.Text.Text) ?? string.Empty);
                break;
            case SDL.EventType.TextEditing:
                _imeText = Marshal.PtrToStringUTF8(nativeEvent.Edit.Text) ?? string.Empty;
                _imeSelection = new Vector2i(Math.Max(0, nativeEvent.Edit.Start),
                    Math.Max(0, nativeEvent.Edit.Length));
                TextEditingCore?.Invoke(_imeText, _imeSelection);
                break;
            case SDL.EventType.MouseMotion when nativeEvent.Motion.Which != SDL.TouchMouseID:
                DispatchMouseMotion(nativeEvent.Motion);
                break;
            case SDL.EventType.MouseButtonDown or SDL.EventType.MouseButtonUp when nativeEvent.Button.Which != SDL.TouchMouseID:
                DispatchMouseButton(nativeEvent.Button);
                break;
            case SDL.EventType.MouseWheel:
                DispatchMouseWheel(nativeEvent.Wheel);
                break;
        }
    }

    private void DispatchFocusChanged(bool focused)
    {
        Exception? callbackFailure = null;
        Exception? notificationFailure = null;
        try
        {
            try
            {
                WindowFocusChangedCore?.Invoke(focused);
            }
            catch (Exception exception)
            {
                callbackFailure = exception;
            }

            try
            {
                Engine.MainLoop?.Notify(focused
                    ? MainLoop.NotificationApplicationFocusIn
                    : MainLoop.NotificationApplicationFocusOut);
            }
            catch (Exception exception)
            {
                notificationFailure = exception;
            }
        }
        finally
        {
            if (!focused)
                Input.ReleasePressedEvents();
        }

        if (callbackFailure is not null && notificationFailure is not null)
            throw new AggregateException("Both focus callbacks failed.", callbackFailure, notificationFailure);
        if (callbackFailure is not null)
            ExceptionDispatchInfo.Capture(callbackFailure).Throw();
        if (notificationFailure is not null)
            ExceptionDispatchInfo.Capture(notificationFailure).Throw();
    }

    private void DispatchDrop(SDL.EventType type, string? path)
    {
        switch (type)
        {
            case SDL.EventType.DropBegin:
                _pendingDroppedFiles = new List<string>();
                break;
            case SDL.EventType.DropFile:
                if (_pendingDroppedFiles is { } pending)
                    pending.Add(path ?? string.Empty);
                else
                    FilesDroppedCore?.Invoke(new[] { path ?? string.Empty });
                break;
            case SDL.EventType.DropComplete:
                {
                    var completed = _pendingDroppedFiles;
                    _pendingDroppedFiles = null;
                    if (completed is { Count: > 0 })
                        FilesDroppedCore?.Invoke(completed.ToArray());
                    break;
                }
        }
    }

    private void DispatchKey(SDL.KeyboardEvent source)
    {
        _eventKeyModifiers = source.Mod;
        _hasEventKeyModifiers = true;
        var ownModifier = source.Scancode switch
        {
            SDL.Scancode.LShift => SDL.Keymod.LShift,
            SDL.Scancode.RShift => SDL.Keymod.RShift,
            SDL.Scancode.LCtrl => SDL.Keymod.LCtrl,
            SDL.Scancode.RCtrl => SDL.Keymod.RCtrl,
            SDL.Scancode.LAlt => SDL.Keymod.LAlt,
            SDL.Scancode.RAlt => SDL.Keymod.RAlt,
            SDL.Scancode.LGUI => SDL.Keymod.LGUI,
            SDL.Scancode.RGUI => SDL.Keymod.RGUI,
            _ => SDL.Keymod.None,
        };
        var modifiers = source.Mod & ~ownModifier;
        using var @event = new InputEventKey
        {
            WindowID = MainWindowId,
            Pressed = source.Down,
            Echo = source.Repeat,
            Keycode = MapKeycode(source.Key),
            PhysicalKeycode = PhysicalKeys.GetValueOrDefault(source.Scancode),
            KeyLabel = MapKeycode(SDL.GetKeyFromScancode(source.Scancode, source.Mod, false)),
            Location = source.Scancode switch
            {
                SDL.Scancode.LCtrl or SDL.Scancode.LShift or SDL.Scancode.LAlt or SDL.Scancode.LGUI => KeyLocation.Left,
                SDL.Scancode.RCtrl or SDL.Scancode.RShift or SDL.Scancode.RAlt or SDL.Scancode.RGUI => KeyLocation.Right,
                _ => KeyLocation.Unspecified,
            },
            ShiftPressed = (modifiers & SDL.Keymod.Shift) != 0,
            ControlPressed = (modifiers & SDL.Keymod.Ctrl) != 0,
            AltPressed = (modifiers & SDL.Keymod.Alt) != 0,
            MetaPressed = (modifiers & SDL.Keymod.GUI) != 0,
        };
        Input.ParseInputEvent(@event);
    }

    private void DispatchMouseMotion(SDL.MouseMotionEvent source)
    {
        var scale = GetMousePixelScale();
        var position = new Vector2(source.X * scale, source.Y * scale);
        var relative = new Vector2(source.XRel * scale, source.YRel * scale);
        var dt = source.Timestamp > _lastMouseMotionTimestamp
            ? (source.Timestamp - _lastMouseMotionTimestamp) / 1_000_000_000f : 0f;
        var velocity = _mouseMode != MouseMode.Captured && dt > 0f ? relative / dt : Vector2.Zero;
        if (!position.IsFinite() || !relative.IsFinite() || !velocity.IsFinite())
            throw new ArgumentOutOfRangeException(nameof(source), "Native mouse motion must have finite coordinates and velocity.");
        _lastMouseMotionTimestamp = source.Timestamp;
        using var @event = new InputEventMouseMotion
        {
            WindowID = MainWindowId,
            ButtonMask = _heldMouseButtons,
            Position = position,
            GlobalPosition = position,
            Relative = relative,
            ScreenRelative = relative,
            Velocity = velocity,
            ScreenVelocity = velocity,
        };
        SetPointerModifiers(@event);
        Input.ParseInputEvent(@event);
    }

    private void DispatchMouseButton(SDL.MouseButtonEvent source)
    {
        var button = source.Button switch
        {
            1 => MouseButton.Left,
            2 => MouseButton.Middle,
            3 => MouseButton.Right,
            4 => MouseButton.XButton1,
            5 => MouseButton.XButton2,
            _ => MouseButton.None,
        };
        if (button == MouseButton.None)
            return;
        var scale = GetMousePixelScale();
        var position = new Vector2(source.X * scale, source.Y * scale);
        if (!position.IsFinite())
            throw new ArgumentOutOfRangeException(nameof(source), "Native mouse-button coordinates must be finite.");
        var mask = ButtonMask(button);
        _heldMouseButtons = source.Down ? _heldMouseButtons | mask : _heldMouseButtons & ~mask;
        using var @event = new InputEventMouseButton
        {
            WindowID = MainWindowId,
            ButtonIndex = button,
            ButtonMask = _heldMouseButtons,
            Position = position,
            GlobalPosition = position,
            Pressed = source.Down,
            DoubleClick = source.Clicks >= 2,
        };
        SetPointerModifiers(@event);
        Input.ParseInputEvent(@event);
    }

    private void DispatchMouseWheel(SDL.MouseWheelEvent source)
    {
        var direction = source.Direction == SDL.MouseWheelDirection.Flipped ? -1f : 1f;
        Exception? verticalFailure = null;
        if (source.Y != 0f)
        {
            try
            {
                DispatchWheelButton(source.Y * direction > 0f ? MouseButton.WheelUp : MouseButton.WheelDown,
                    Mathf.Abs(source.Y), source.MouseX, source.MouseY);
            }
            catch (Exception exception)
            {
                verticalFailure = exception;
            }
        }
        Exception? horizontalFailure = null;
        if (source.X != 0f)
        {
            try
            {
                DispatchWheelButton(source.X * direction > 0f ? MouseButton.WheelRight : MouseButton.WheelLeft,
                    Mathf.Abs(source.X), source.MouseX, source.MouseY);
            }
            catch (Exception exception)
            {
                horizontalFailure = exception;
            }
        }
        if (verticalFailure is not null && horizontalFailure is not null)
            throw new AggregateException("Both wheel axes failed.", verticalFailure, horizontalFailure);
        if (verticalFailure is not null)
            ExceptionDispatchInfo.Capture(verticalFailure).Throw();
        if (horizontalFailure is not null)
            ExceptionDispatchInfo.Capture(horizontalFailure).Throw();
    }

    private void DispatchWheelButton(MouseButton button, float factor, float x, float y)
    {
        var scale = GetMousePixelScale();
        var position = new Vector2(x * scale, y * scale);
        if (!position.IsFinite() || !float.IsFinite(factor) || factor < 0f)
            throw new ArgumentOutOfRangeException(nameof(factor), "Native mouse-wheel coordinates and amount must be finite.");
        using var @event = new InputEventMouseButton
        {
            WindowID = MainWindowId,
            ButtonIndex = button,
            ButtonMask = _heldMouseButtons,
            Position = position,
            GlobalPosition = position,
            Factor = factor,
            Pressed = true,
        };
        SetPointerModifiers(@event);
        Exception? pressFailure = null;
        try
        {
            Input.ParseInputEvent(@event);
        }
        catch (Exception exception)
        {
            pressFailure = exception;
        }
        @event.Pressed = false;
        try
        {
            Input.ParseInputEvent(@event);
        }
        catch (Exception releaseFailure) when (pressFailure is not null)
        {
            throw new AggregateException("Both wheel-button callbacks failed.", pressFailure, releaseFailure);
        }
        if (pressFailure is not null)
            ExceptionDispatchInfo.Capture(pressFailure).Throw();
    }

    private void SetPointerModifiers(InputEventWithModifiers @event)
    {
        var modifiers = _hasEventKeyModifiers ? _eventKeyModifiers : SDL.GetModState();
        @event.ShiftPressed = (modifiers & SDL.Keymod.Shift) != 0;
        @event.ControlPressed = (modifiers & SDL.Keymod.Ctrl) != 0;
        @event.AltPressed = (modifiers & SDL.Keymod.Alt) != 0;
        @event.MetaPressed = (modifiers & SDL.Keymod.GUI) != 0;
    }

    private void DispatchTouch(SDL.TouchFingerEvent source)
    {
        var id = (source.TouchID, source.FingerID);
        var down = source.Type == SDL.EventType.FingerDown;
        var active = _touchContacts.TryGetValue(id, out var contact);
        if (down && active)
            throw new InvalidOperationException("A touch contact cannot begin twice.");
        if (!down && !active)
            return;

        var size = WindowGetSizeCore();
        var position = new Vector2(source.X * size.X, source.Y * size.Y);
        if (!position.IsFinite())
            throw new ArgumentOutOfRangeException(nameof(source), "Native touch coordinates must be finite.");
        if (down)
        {
            var index = _freeTouchIndexes.Count > 0 ? _freeTouchIndexes.Pop() : checked(_nextTouchIndex++);
            contact = new TouchContact(index, source.Timestamp);
            _touchContacts.Add(id, contact);
        }
        if (source.Type == SDL.EventType.FingerMotion)
        {
            var relative = new Vector2(source.DX * size.X, source.DY * size.Y);
            var dt = source.Timestamp > contact.Timestamp
                ? (source.Timestamp - contact.Timestamp) / 1_000_000_000f : 0f;
            var velocity = dt > 0f ? relative / dt : Vector2.Zero;
            if (!relative.IsFinite() || !velocity.IsFinite() || !float.IsFinite(source.Pressure))
                throw new ArgumentOutOfRangeException(nameof(source), "Native touch movement and pressure must be finite.");
            _touchContacts[id] = contact with { Timestamp = source.Timestamp };
            using var @event = new InputEventScreenDrag
            {
                WindowID = MainWindowId,
                Device = 0,
                Index = contact.Index,
                Position = position,
                Relative = relative,
                ScreenRelative = relative,
                Velocity = velocity,
                ScreenVelocity = velocity,
                Pressure = Math.Clamp(source.Pressure, 0f, 1f),
            };
            Input.ParseInputEvent(@event);
            return;
        }

        try
        {
            using var @event = new InputEventScreenTouch
            {
                WindowID = MainWindowId,
                Device = 0,
                Index = contact.Index,
                Position = position,
                Pressed = source.Type == SDL.EventType.FingerDown,
                Canceled = source.Type == SDL.EventType.FingerCanceled,
            };
            Input.ParseInputEvent(@event);
        }
        finally
        {
            if (source.Type != SDL.EventType.FingerDown)
            {
                _touchContacts.Remove(id);
                _freeTouchIndexes.Push(contact.Index);
            }
        }
    }

    private static MouseButtonMask ButtonMask(MouseButton button) => button switch
    {
        MouseButton.Left => MouseButtonMask.Left,
        MouseButton.Right => MouseButtonMask.Right,
        MouseButton.Middle => MouseButtonMask.Middle,
        MouseButton.XButton1 => MouseButtonMask.XButton1,
        MouseButton.XButton2 => MouseButtonMask.XButton2,
        _ => MouseButtonMask.None,
    };

    private static Key MapKeycode(SDL.Keycode source)
    {
        if (source == SDL.Keycode.Unknown)
            return Key.None;
        var value = (uint)source;
        if (value is < 0x20 or 0x7f)
            return LogicalKeys.GetValueOrDefault(source);
        if (value is >= 0x61 and <= 0x7a)
            return (Key)(value - 0x20);
        if (value is >= 0x20 and <= 0x7e)
            return (Key)value;
        if (value <= 0x10ffff && System.Text.Rune.IsValid((int)value))
            return (Key)System.Text.Rune.ToUpperInvariant(new System.Text.Rune((int)value)).Value;
        return LogicalKeys.GetValueOrDefault(source);
    }

    internal Key KeyboardGetKeycodeFromPhysicalCore(Key physical)
    {
        EnsureOwner();
        return MapPhysicalKey(physical, true);
    }

    internal Key KeyboardGetLabelFromPhysicalCore(Key physical)
    {
        EnsureOwner();
        return MapPhysicalKey(physical, false);
    }

    private static Key MapPhysicalKey(Key physical, bool logicalKeycode)
    {
        var modifiers = (int)physical & (int)KeyModifierMask.ModifierMask;
        var baseKey = (Key)((int)physical & (int)KeyModifierMask.CodeMask);
        foreach (var (scancode, key) in PhysicalKeys)
        {
            if (key != baseKey)
                continue;

            var nativeKey = SDL.GetKeyFromScancode(scancode, SDL.GetModState(), false);
            var nativeValue = (uint)nativeKey;
            var mapped = logicalKeycode && nativeValue is >= 0x80 and <= 0x10ffff and not 0xa5 and not 0xa7
                ? Key.None : MapKeycode(nativeKey);
            if (mapped == Key.None)
                return physical;
            if (logicalKeycode && mapped == Key.Backtab)
                mapped = Key.Tab;
            return (Key)((int)mapped | modifiers);
        }
        return physical;
    }

    private static Dictionary<SDL.Keycode, Key> BuildLogicalKeys()
    {
        var keys = new Dictionary<SDL.Keycode, Key>();
        foreach (var key in Enum.GetValues<Key>())
            if (Enum.TryParse<SDL.Keycode>(key.ToString(), out var source))
                keys[source] = key;
        keys[SDL.Keycode.Return] = Key.Enter;
        keys[SDL.Keycode.KpEnter] = Key.KeypadEnter;
        keys[SDL.Keycode.Capslock] = Key.CapsLock;
        keys[SDL.Keycode.ScrollLock] = Key.ScrollLock;
        keys[SDL.Keycode.NumLockClear] = Key.NumLock;
        keys[SDL.Keycode.Pageup] = Key.PageUp;
        keys[SDL.Keycode.Pagedown] = Key.PageDown;
        keys[SDL.Keycode.PrintScreen] = Key.Print;
        keys[SDL.Keycode.LShift] = keys[SDL.Keycode.RShift] = Key.Shift;
        keys[SDL.Keycode.LCtrl] = keys[SDL.Keycode.RCtrl] = Key.Control;
        keys[SDL.Keycode.LAlt] = keys[SDL.Keycode.RAlt] = Key.Alt;
        keys[SDL.Keycode.LGUI] = keys[SDL.Keycode.RGUI] = Key.Meta;
        keys[SDL.Keycode.Kp0] = Key.Keypad0;
        keys[SDL.Keycode.Kp1] = Key.Keypad1;
        keys[SDL.Keycode.Kp2] = Key.Keypad2;
        keys[SDL.Keycode.Kp3] = Key.Keypad3;
        keys[SDL.Keycode.Kp4] = Key.Keypad4;
        keys[SDL.Keycode.Kp5] = Key.Keypad5;
        keys[SDL.Keycode.Kp6] = Key.Keypad6;
        keys[SDL.Keycode.Kp7] = Key.Keypad7;
        keys[SDL.Keycode.Kp8] = Key.Keypad8;
        keys[SDL.Keycode.Kp9] = Key.Keypad9;
        keys[SDL.Keycode.KpPlus] = Key.KeypadAdd;
        keys[SDL.Keycode.KpMinus] = Key.KeypadSubtract;
        keys[SDL.Keycode.KpMultiply] = Key.KeypadMultiply;
        keys[SDL.Keycode.KpDivide] = Key.KeypadDivide;
        keys[SDL.Keycode.KpPeriod] = Key.KeypadPeriod;
        return keys;
    }

    private static Dictionary<SDL.Scancode, Key> BuildPhysicalKeys()
    {
        var keys = new Dictionary<SDL.Scancode, Key>();
        foreach (var key in Enum.GetValues<Key>())
            if (Enum.TryParse<SDL.Scancode>(key.ToString(), out var source))
                keys[source] = key;
        keys[SDL.Scancode.Return] = Key.Enter;
        keys[SDL.Scancode.Alpha0] = Key.Key0;
        keys[SDL.Scancode.Alpha1] = Key.Key1;
        keys[SDL.Scancode.Alpha2] = Key.Key2;
        keys[SDL.Scancode.Alpha3] = Key.Key3;
        keys[SDL.Scancode.Alpha4] = Key.Key4;
        keys[SDL.Scancode.Alpha5] = Key.Key5;
        keys[SDL.Scancode.Alpha6] = Key.Key6;
        keys[SDL.Scancode.Alpha7] = Key.Key7;
        keys[SDL.Scancode.Alpha8] = Key.Key8;
        keys[SDL.Scancode.Alpha9] = Key.Key9;
        keys[SDL.Scancode.Leftbracket] = Key.BraceLeft;
        keys[SDL.Scancode.Rightbracket] = Key.BraceRight;
        keys[SDL.Scancode.Grave] = Key.Section;
        keys[SDL.Scancode.NonUsBackSlash] = Key.QuoteLeft;
        keys[SDL.Scancode.Capslock] = Key.CapsLock;
        keys[SDL.Scancode.Scrolllock] = Key.ScrollLock;
        keys[SDL.Scancode.NumLockClear] = Key.NumLock;
        keys[SDL.Scancode.Pageup] = Key.PageUp;
        keys[SDL.Scancode.Pagedown] = Key.PageDown;
        keys[SDL.Scancode.Printscreen] = Key.Print;
        keys[SDL.Scancode.LShift] = keys[SDL.Scancode.RShift] = Key.Shift;
        keys[SDL.Scancode.LCtrl] = keys[SDL.Scancode.RCtrl] = Key.Control;
        keys[SDL.Scancode.LAlt] = keys[SDL.Scancode.RAlt] = Key.Alt;
        keys[SDL.Scancode.LGUI] = keys[SDL.Scancode.RGUI] = Key.Meta;
        keys[SDL.Scancode.Kp0] = Key.Keypad0;
        keys[SDL.Scancode.Kp1] = Key.Keypad1;
        keys[SDL.Scancode.Kp2] = Key.Keypad2;
        keys[SDL.Scancode.Kp3] = Key.Keypad3;
        keys[SDL.Scancode.Kp4] = Key.Keypad4;
        keys[SDL.Scancode.Kp5] = Key.Keypad5;
        keys[SDL.Scancode.Kp6] = Key.Keypad6;
        keys[SDL.Scancode.Kp7] = Key.Keypad7;
        keys[SDL.Scancode.Kp8] = Key.Keypad8;
        keys[SDL.Scancode.Kp9] = Key.Keypad9;
        keys[SDL.Scancode.KpPlus] = Key.KeypadAdd;
        keys[SDL.Scancode.KpMinus] = Key.KeypadSubtract;
        keys[SDL.Scancode.KpMultiply] = Key.KeypadMultiply;
        keys[SDL.Scancode.KpDivide] = Key.KeypadDivide;
        keys[SDL.Scancode.KpPeriod] = Key.KeypadPeriod;
        keys[SDL.Scancode.KpEnter] = Key.KeypadEnter;
        keys[SDL.Scancode.KpComma] = Key.Comma;
        keys[SDL.Scancode.KpEquals] = Key.Equal;
        return keys;
    }

    private readonly record struct TouchContact(int Index, ulong Timestamp);
}
