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

    /// <summary>Occurs when the operating system requests that the application quit.</summary>
    /// <remarks>Delivery is synchronous during <see cref="ProcessEvents"/>. The server does not close automatically.</remarks>
    public event Action? QuitRequested;

    /// <summary>Occurs when the native system theme changes.</summary>
    /// <remarks>Delivery is synchronous in native event order during <see cref="ProcessEvents"/>. Query <see cref="IsDarkMode"/> after delivery to read the current theme.</remarks>
    public event Action? SystemThemeChanged;

    /// <summary>Occurs when the main window receives a close request.</summary>
    /// <remarks>Delivery is synchronous in native queue order during <see cref="ProcessEvents"/> or
    /// <see cref="ForceProcessAndDropEvents"/> for the main window only. The server does not close automatically.
    /// A failing handler does not prevent later queued events from being delivered; the pump reports its failure afterward.</remarks>
    public event Action? CloseRequested;

    /// <summary>Occurs when the pointer enters the main window.</summary>
    /// <remarks>Only effective pointer transitions for the main window notify, in native queue order during <see cref="ProcessEvents"/> or <see cref="ForceProcessAndDropEvents"/>. A failing handler does not prevent later queued events from being delivered.</remarks>
    public event Action? WindowMouseEntered;

    /// <summary>Occurs when the pointer leaves the main window.</summary>
    /// <remarks>Only effective pointer transitions for the main window notify, in native queue order during <see cref="ProcessEvents"/> or <see cref="ForceProcessAndDropEvents"/>. A failing handler does not prevent later queued events from being delivered.</remarks>
    public event Action? WindowMouseExited;

    /// <summary>Occurs when the main window's native display content scale changes.</summary>
    /// <remarks>Delivery follows native event order during <see cref="ProcessEvents"/>. The notification does not include a DPI value: native content scale may change when a window moves between displays, and it is not a measurement of physical dots per inch. A failing handler does not prevent later queued events from being delivered.</remarks>
    public event Action? WindowDpiChanged;

    /// <summary>Occurs when the main window's observed client rectangle changes.</summary>
    /// <remarks>Receives the full rectangle in pixel coordinates on Wayland and platform-native window coordinates elsewhere, after the changed position or size has been committed and in native event order during <see cref="ProcessEvents"/>. On Wayland, the position is conventionally zero because the compositor does not disclose a reliable global position. Unchanged rectangles and events from other windows do not notify. Delivery is confined to the opening thread; a failing handler does not prevent later queued events from being delivered.</remarks>
    public event Action<RectI>? WindowRectChanged;

    /// <summary>Occurs when the main window gains or loses keyboard focus.</summary>
    /// <remarks>Delivery is synchronous for the main window during <see cref="ProcessEvents"/> or <see cref="ForceProcessAndDropEvents"/>. The window callback precedes the application notification. Losing focus releases tracked pressed input afterward, even when either callback fails; failures are reported after later queued events are delivered.</remarks>
    public event Action<bool>? WindowFocusChanged;

    /// <summary>Occurs when the platform commits text input, including text composed through an IME.</summary>
    /// <remarks>Delivery is synchronous during <see cref="ProcessEvents"/>; the input event retains no string state.</remarks>
    public event Action<string>? TextInput;

    /// <summary>Occurs after the native input method updates its uncommitted composition.</summary>
    /// <remarks>The text and Unicode-codepoint selection have already committed to <see cref="IMEGetText"/> and <see cref="IMEGetSelection"/>. Unknown negative native selection offsets become zero.</remarks>
    public event Action<string, Vector2I>? TextEditing;

    /// <summary>Occurs when the operating system finishes dropping one or more files onto the main window.</summary>
    /// <remarks>Paths are copied from native event memory in arrival order. One completed drop produces one callback, even when it contains multiple files. The returned array belongs to the caller and remains valid after delivery.</remarks>
    public event Action<IReadOnlyList<string>>? FilesDropped;

    /// <summary>Drains native events and commits typed keyboard, mouse, wheel, and touch state before game callbacks.</summary>
    /// <remarks>
    /// The host calls this on the opening thread before advancing each Engine frame. Each input event is owned and
    /// disposed by this server after synchronous delivery; handlers must duplicate an event they need to retain.
    /// Malformed native pointer values are rejected before mouse button or timestamp state changes.
    /// Callback failures are collected while later queued events continue, then thrown together after the queue drains.
    /// Re-entry is rejected. No rendering or game frame is advanced here.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The server has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Called off the opening thread, re-entered, or called while the active main loop cannot accept input. Rejected calls leave the native queue untouched.</exception>
    /// <exception cref="AggregateException">One or more native pointer values or game callbacks failed; later queued events still run.</exception>
    public void ProcessEvents() => ProcessEventsCore(dropInput: false);

    /// <summary>Processes native window events while discarding pending keyboard, pointer, touch, and text input.</summary>
    /// <remarks>Tracked pressed state is released before draining the queue; window and quit callbacks still run.</remarks>
    /// <exception cref="InvalidOperationException">Called off the owner thread or re-entered.</exception>
    /// <exception cref="AggregateException">One or more delivered callbacks failed.</exception>
    public void ForceProcessAndDropEvents() => ProcessEventsCore(dropInput: true);

    private void ProcessEventsCore(bool dropInput)
    {
        EnsureOwner();
        if (_processingEvents)
            throw new InvalidOperationException("Display event processing cannot be re-entered.");
        if (!dropInput)
            Engine.Instance.MainLoop?.ValidateInputEventDispatch();

        _processingEvents = true;
        _hasEventKeyModifiers = false;
        List<Exception>? failures = null;
        try
        {
            if (dropInput)
            {
                _hasPendingMouseMotion = false;
                _touchContacts.Clear();
                _freeTouchIndexes.Clear();
                _nextTouchIndex = 0;
                _heldMouseButtons = MouseButtonMask.None;
                Input.Instance.ReleasePressedEvents();
            }
            while (SDL.PollEvent(out var nativeEvent))
            {
                if (dropInput && IsInputEvent((SDL.EventType)nativeEvent.Type))
                    continue;
                if ((SDL.EventType)nativeEvent.Type == SDL.EventType.MouseMotion &&
                    nativeEvent.Motion.WindowID == _sdlWindowId &&
                    nativeEvent.Motion.Which != SDL.TouchMouseID && Input.Instance.UseAccumulatedInput)
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
        SDL.EventType.FingerCanceled or SDL.EventType.FingerMotion;

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
        if (type == SDL.EventType.Quit)
        {
            QuitRequested?.Invoke();
            return;
        }

        if (type == SDL.EventType.SystemThemeChanged)
        {
            SystemThemeChanged?.Invoke();
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
                    WindowMouseEntered?.Invoke();
                }
                break;
            case SDL.EventType.WindowMouseLeave:
                if (_mouseHovering)
                {
                    _mouseHovering = false;
                    WindowMouseExited?.Invoke();
                }
                break;
            case SDL.EventType.WindowDisplayScaleChanged:
                if (_waylandWindowPosition)
                {
                    ReapplyWaylandWindowLimits();
                    RefreshBlankWindowSurface();
                }
                WindowDpiChanged?.Invoke();
                break;
            case SDL.EventType.WindowCloseRequested:
                _pendingDroppedFiles = null;
                CloseRequested?.Invoke();
                break;
            case SDL.EventType.WindowResized when !_waylandWindowPosition:
            case SDL.EventType.WindowPixelSizeChanged when _waylandWindowPosition:
                if (_waylandWindowPosition)
                    RefreshBlankWindowSurface();
                var resizedRect = new RectI(_windowRect.Position,
                    new Vector2I(nativeEvent.Window.Data1, nativeEvent.Window.Data2));
                if (resizedRect != _windowRect)
                {
                    _windowRect = resizedRect;
                    WindowRectChanged?.Invoke(resizedRect);
                }
                break;
            case SDL.EventType.WindowMoved:
                if (_waylandWindowPosition)
                    break;
                var movedRect = new RectI(new Vector2I(nativeEvent.Window.Data1, nativeEvent.Window.Data2),
                    _windowRect.Size);
                if (movedRect != _windowRect)
                {
                    _windowRect = movedRect;
                    WindowRectChanged?.Invoke(movedRect);
                }
                break;
            case SDL.EventType.WindowFocusGained:
                DispatchFocusChanged(focused: true);
                break;
            case SDL.EventType.WindowFocusLost:
                _imeText = string.Empty;
                _imeSelection = Vector2I.Zero;
                _eventKeyModifiers = SDL.Keymod.None;
                _hasEventKeyModifiers = true;
                _touchContacts.Clear();
                _freeTouchIndexes.Clear();
                _nextTouchIndex = 0;
                _heldMouseButtons = MouseButtonMask.None;
                DispatchFocusChanged(focused: false);
                break;
            case SDL.EventType.KeyDown:
            case SDL.EventType.KeyUp:
                DispatchKey(nativeEvent.Key);
                break;
            case SDL.EventType.TextInput:
                _imeText = string.Empty;
                _imeSelection = Vector2I.Zero;
                TextInput?.Invoke(Marshal.PtrToStringUTF8(nativeEvent.Text.Text) ?? string.Empty);
                break;
            case SDL.EventType.TextEditing:
                _imeText = Marshal.PtrToStringUTF8(nativeEvent.Edit.Text) ?? string.Empty;
                _imeSelection = new Vector2I(Math.Max(0, nativeEvent.Edit.Start),
                    Math.Max(0, nativeEvent.Edit.Length));
                TextEditing?.Invoke(_imeText, _imeSelection);
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
                WindowFocusChanged?.Invoke(focused);
            }
            catch (Exception exception)
            {
                callbackFailure = exception;
            }

            try
            {
                Engine.Instance.MainLoop?.Notify(focused
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
                Input.Instance.ReleasePressedEvents();
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
                    FilesDropped?.Invoke(new[] { path ?? string.Empty });
                break;
            case SDL.EventType.DropComplete:
                {
                    var completed = _pendingDroppedFiles;
                    _pendingDroppedFiles = null;
                    if (completed is { Count: > 0 })
                        FilesDropped?.Invoke(completed.ToArray());
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
        Input.Instance.ParseInputEvent(@event);
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
        Input.Instance.ParseInputEvent(@event);
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
        Input.Instance.ParseInputEvent(@event);
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
            Input.Instance.ParseInputEvent(@event);
        }
        catch (Exception exception)
        {
            pressFailure = exception;
        }
        @event.Pressed = false;
        try
        {
            Input.Instance.ParseInputEvent(@event);
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
        if (source.Type == SDL.EventType.FingerDown)
        {
            var index = _freeTouchIndexes.Count > 0 ? _freeTouchIndexes.Pop() : checked(_nextTouchIndex++);
            _touchContacts.Add(id, new TouchContact(index, source.Timestamp));
        }
        if (!_touchContacts.TryGetValue(id, out var contact))
            return;

        var size = WindowGetSize();
        var position = new Vector2(source.X * size.X, source.Y * size.Y);
        var pressure = float.IsFinite(source.Pressure) ? Math.Clamp(source.Pressure, 0f, 1f) : 0f;
        if (source.Type == SDL.EventType.FingerMotion)
        {
            var relative = new Vector2(source.DX * size.X, source.DY * size.Y);
            var dt = source.Timestamp > contact.Timestamp
                ? (source.Timestamp - contact.Timestamp) / 1_000_000_000f : 0f;
            _touchContacts[id] = contact with { Timestamp = source.Timestamp };
            using var @event = new InputEventScreenDrag
            {
                WindowID = MainWindowId,
                Device = 0,
                Index = contact.Index,
                Position = position,
                Relative = relative,
                ScreenRelative = relative,
                Velocity = dt > 0f ? relative / dt : Vector2.Zero,
                ScreenVelocity = dt > 0f ? relative / dt : Vector2.Zero,
                Pressure = pressure,
            };
            Input.Instance.ParseInputEvent(@event);
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
            Input.Instance.ParseInputEvent(@event);
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

    /// <summary>Maps a physical key to the logical key reported for the current keyboard layout.</summary>
    /// <param name="physical">A physical key identity.</param>
    /// <returns>The current logical key, or the supplied physical key when no layout mapping is available.</returns>
    /// <remarks>The result reflects the active keyboard layout and modifier state at the time of the call. Modifier bits in <paramref name="physical"/> are preserved. Reverse Tab is normalized to Tab, as in key events. On Wayland, SDL's keypad lookup does not distinguish Num Lock or shifted navigation symbols, so those keys can retain their physical identity instead of the current logical symbol.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposed.</exception>
    /// <exception cref="InvalidOperationException">The call is not on the opening thread.</exception>
    public Key KeyboardGetKeycodeFromPhysical(Key physical)
    {
        EnsureOwner();
        return MapPhysicalKey(physical, true);
    }

    /// <summary>Maps a physical key to its localized label in the current keyboard layout.</summary>
    /// <param name="physical">A physical key identity.</param>
    /// <returns>The current label key, including a non-Latin Unicode scalar where applicable, or the supplied physical key when no layout mapping is available.</returns>
    /// <remarks>The label follows the current layout and modifier state without event-keycode remapping. Modifier bits in <paramref name="physical"/> are preserved; key events use the same label mapping with their event modifier state. On Wayland, SDL's keypad lookup does not expose Num Lock's printable label and can retain the physical keypad identity.</remarks>
    /// <exception cref="ObjectDisposedException">The server is disposed.</exception>
    /// <exception cref="InvalidOperationException">The call is not on the opening thread.</exception>
    public Key KeyboardGetLabelFromPhysical(Key physical)
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
