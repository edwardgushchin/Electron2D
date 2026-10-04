namespace Electron2D;

/// <summary>Owns process-wide input state and translates typed events into named actions.</summary>
/// <remarks>Public static operations delegate to the retained service object; state, identity and ownership remain object-scoped.
///
/// A platform host submits events through <see cref="ParseInputEvent"/>. State is committed before scene delivery,
/// so callbacks observe the new state. Queries and synthetic action changes are lock-serialized; event delivery is
/// synchronous on the caller thread and an attached <see cref="SceneTree"/> requires its owner thread.
/// </remarks>
public sealed partial class Input : ElectronObject
{
    /// <summary>Defines the maximum number of binding sources supported by one action.</summary>
    internal const int MaxEventsPerAction = 32;

    private static readonly Input SharedInstance = new();
    [ThreadStatic]
    private static bool _queryingPhysicsLane;
    private static readonly IReadOnlyList<PropertyDescriptor> InputProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<Input, MouseButtonMask>(nameof(MouseButtonMask), input => input.MouseButtonMaskCore),
            new PropertyDescriptor<Input, Vector2>(nameof(LastMouseVelocity), input => input.LastMouseVelocityCore),
            new PropertyDescriptor<Input, Vector2>(nameof(LastMouseScreenVelocity), input => input.LastMouseScreenVelocityCore),
        ]);

    private readonly object _gate = new();
    private readonly object _parseGate = new();
    private readonly HashSet<Key> _keysPressed = [];
    private readonly HashSet<Key> _physicalKeysPressed = [];
    private readonly HashSet<Key> _keyLabelsPressed = [];
    private readonly HashSet<JoyButtonState> _joyButtonsPressed = [];
    private readonly Dictionary<JoyAxisState, float> _joyAxes = [];
    private readonly Dictionary<int, MouseButtonMask> _mouseButtonMasks = [];
    private readonly Dictionary<string, ActionState> _actions = new(StringComparer.Ordinal);
    private readonly List<MappedInputAction> _matches = [];
    private MouseButtonMask _mouseButtonMask;
    private Vector2 _lastMouseVelocity;
    private Vector2 _lastMouseScreenVelocity;
    private bool _isParsing;
    private bool _useAccumulatedInput = true;
    private bool _emulateMouseFromTouch = true;
    private bool _emulateTouchFromMouse;
    private int? _touchFromMouseDevice;
    private (int Device, int Index)? _mouseFromTouch;
    private Action? _flushBufferedEvents;

    private Input()
    {
    }

    internal static Input Service => SharedInstance;

    internal bool UseAccumulatedInputCore
    {
        get { lock (_gate) return _useAccumulatedInput; }
        set { lock (_gate) _useAccumulatedInput = value; }
    }

    internal bool EmulateMouseFromTouchCore
    {
        get { lock (_gate) return _emulateMouseFromTouch; }
        set { lock (_gate) _emulateMouseFromTouch = value; }
    }

    internal bool EmulateTouchFromMouseCore
    {
        get { lock (_gate) return _emulateTouchFromMouse; }
        set { lock (_gate) _emulateTouchFromMouse = value; }
    }

    internal void FlushBufferedEventsCore()
    {
        Action? flush;
        lock (_gate)
            flush = _flushBufferedEvents;
        flush?.Invoke();
    }

    internal void SetNativeFlush(Action? flush)
    {
        lock (_gate)
            _flushBufferedEvents = flush;
    }

    internal MouseButtonMask MouseButtonMaskCore
    {
        get
        {
            lock (_gate)
                return _mouseButtonMask;
        }
    }

    internal Vector2 LastMouseVelocityCore
    {
        get
        {
            lock (_gate)
                return _lastMouseVelocity;
        }
    }

    internal Vector2 LastMouseScreenVelocityCore
    {
        get
        {
            lock (_gate)
                return _lastMouseScreenVelocity;
        }
    }

    internal bool IsKeyPressedCore(Key keycode)
    {
        lock (_gate)
            return _keysPressed.Contains(NormalizeKey(keycode));
    }

    internal bool IsPhysicalKeyPressedCore(Key keycode)
    {
        lock (_gate)
            return _physicalKeysPressed.Contains(NormalizeKey(keycode));
    }

    internal bool IsKeyLabelPressedCore(Key keycode)
    {
        lock (_gate)
            return _keyLabelsPressed.Contains(NormalizeKey(keycode));
    }

    internal bool IsMouseButtonPressedCore(MouseButton button)
    {
        if (!Enum.IsDefined(button))
            throw new ArgumentOutOfRangeException(nameof(button), button, "Unknown mouse button.");

        var mask = ToMask(button);
        lock (_gate)
            return mask != MouseButtonMask.None && (_mouseButtonMask & mask) != 0;
    }

    internal bool IsJoyButtonPressedCore(JoyButton button, int device = 0)
    {
        ValidateDevice(device);
        lock (_gate)
            return _joyButtonsPressed.Contains(new JoyButtonState(device, button));
    }

    internal float GetJoyAxisCore(JoyAxis axis, int device = 0)
    {
        ValidateDevice(device);
        lock (_gate)
            return _joyAxes.GetValueOrDefault(new JoyAxisState(device, axis));
    }

    internal bool IsAnythingPressedCore()
    {
        lock (_gate)
        {
            if (_keysPressed.Count != 0 || _mouseButtonMask != MouseButtonMask.None || _joyButtonsPressed.Count != 0)
                return true;

            foreach (var state in _actions.Values)
            {
                if (state.Pressed)
                    return true;
            }

            return false;
        }
    }

    internal bool IsActionPressedCore(string action, bool exactMatch = false) =>
    GetActionSnapshot(action, exactMatch).Pressed;

    internal bool IsActionJustPressedCore(string action, bool exactMatch = false)
    {
        var snapshot = GetActionSnapshot(action, exactMatch);
        return snapshot.JustPressed;
    }

    internal bool IsActionJustReleasedCore(string action, bool exactMatch = false)
    {
        var snapshot = GetActionSnapshot(action, exactMatch);
        return snapshot.JustReleased;
    }

    internal bool IsActionJustPressedByEventCore(string action, InputEvent @event, bool exactMatch = false) =>
    IsActionTransitionByEvent(action, @event, exactMatch, pressed: true);

    internal bool IsActionJustReleasedByEventCore(string action, InputEvent @event, bool exactMatch = false) =>
    IsActionTransitionByEvent(action, @event, exactMatch, pressed: false);

    internal float GetActionStrengthCore(string action, bool exactMatch = false) =>
    GetActionSnapshot(action, exactMatch).Strength;

    internal float GetActionRawStrengthCore(string action, bool exactMatch = false) =>
    GetActionSnapshot(action, exactMatch).RawStrength;

    internal float GetAxisCore(string negativeAction, string positiveAction) =>
    GetActionStrengthCore(positiveAction) - GetActionStrengthCore(negativeAction);

    internal Vector2 GetVectorCore(
    string negativeX,
    string positiveX,
    string negativeY,
    string positiveY,
    float deadzone = -1f)
    {
        if (!float.IsFinite(deadzone) || deadzone < -1f || deadzone > 1f)
            throw new ArgumentOutOfRangeException(nameof(deadzone), deadzone, "The deadzone must be -1 or between 0 and 1.");

        if (deadzone < 0f)
        {
            deadzone = (InputMap.ActionGetDeadzone(negativeX) +
                InputMap.ActionGetDeadzone(positiveX) +
                InputMap.ActionGetDeadzone(negativeY) +
                InputMap.ActionGetDeadzone(positiveY)) / 4f;
        }

        var vector = new Vector2(
            GetActionRawStrengthCore(positiveX) - GetActionRawStrengthCore(negativeX),
            GetActionRawStrengthCore(positiveY) - GetActionRawStrengthCore(negativeY));
        var length = vector.Length();
        if (length <= deadzone)
            return Vector2.Zero;

        if (length > 1f)
            vector /= length;
        else if (deadzone > 0f)
            vector *= Mathf.InverseLerp(deadzone, 1f, length) / length;

        return vector;
    }

    internal void ActionPressCore(string action, float strength = 1f)
    {
        EnsureRegisteredAction(action);
        if (!float.IsFinite(strength))
            throw new ArgumentOutOfRangeException(nameof(strength), strength, "Action strength must be finite.");

        lock (_gate)
            UpdateContribution(action, SyntheticSource, new InputActionMatch(true, Mathf.Clamp(strength, 0f, 1f), Mathf.Clamp(strength, 0f, 1f)), exact: true, eventId: 0);
    }

    internal void ActionReleaseCore(string action)
    {
        EnsureRegisteredAction(action);
        lock (_gate)
            UpdateContribution(action, SyntheticSource, default, exact: true, eventId: 0);
    }

    internal void ParseInputEventCore(InputEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        var mainLoop = Engine.MainLoop;
        mainLoop?.ValidateInputEventDispatch();

        lock (_parseGate)
        {
            if (_isParsing)
                throw new InvalidOperationException("Input parsing cannot be re-entered.");

            _isParsing = true;
            try
            {
                CommitEvent(@event);
                using var emulated = CreateEmulatedEvent(@event, mainLoop is not null);
                Exception? emulationFailure = null;
                if (emulated is not null)
                {
                    try
                    {
                        if (emulated is InputEventMouse)
                            CommitEvent(emulated);
                        mainLoop?.DispatchInputEvent(emulated);
                    }
                    catch (Exception error)
                    {
                        emulationFailure = error;
                    }
                }

                try
                {
                    mainLoop?.DispatchInputEvent(@event);
                }
                catch (Exception error) when (emulationFailure is not null)
                {
                    throw new AggregateException("Emulated and source input callbacks failed.", emulationFailure, error);
                }

                if (emulationFailure is not null)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(emulationFailure).Throw();
            }
            finally
            {
                _matches.Clear();
                _isParsing = false;
            }
        }
    }

    private void CommitEvent(InputEvent @event)
    {
        try
        {
            InputMap.Service.CollectMatches(@event, _matches);
            lock (_gate)
            {
                UpdateRawState(@event);
                if (!@event.IsCanceled() || @event is InputEventMouseButton { Pressed: false })
                {
                    foreach (var match in _matches)
                    {
                        UpdateContribution(match.Action, new ActionSource(@event.Device, match.SourceIndex),
                            match.Status, match.Exact, @event.InstanceID);
                    }
                }
            }
        }
        finally
        {
            _matches.Clear();
        }
    }

    private InputEvent? CreateEmulatedEvent(InputEvent source, bool hasScene)
    {
        if (source.Device == InputEvent.DeviceIdEmulation)
            return null;

        lock (_gate)
        {
            if (source is InputEventMouseButton button && button.ButtonIndex == MouseButton.Left)
            {
                if (button.Pressed && hasScene && _emulateTouchFromMouse && _touchFromMouseDevice is null)
                    _touchFromMouseDevice = button.Device;
                var deliver = hasScene && _touchFromMouseDevice == button.Device;
                if (!button.Pressed && _touchFromMouseDevice == button.Device)
                    _touchFromMouseDevice = null;
                return deliver ? new InputEventScreenTouch
                {
                    Device = InputEvent.DeviceIdEmulation,
                    WindowID = button.WindowID,
                    Index = 0,
                    Position = button.Position,
                    Pressed = button.Pressed,
                    Canceled = button.Canceled,
                    DoubleTap = button.DoubleClick,
                } : null;
            }

            if (source is InputEventMouseMotion motion && hasScene && _emulateTouchFromMouse &&
                _touchFromMouseDevice == motion.Device &&
                (motion.ButtonMask & MouseButtonMask.Left) != 0)
                return new InputEventScreenDrag
                {
                    Device = InputEvent.DeviceIdEmulation,
                    WindowID = motion.WindowID,
                    Index = 0,
                    Position = motion.Position,
                    Relative = motion.Relative,
                    ScreenRelative = motion.ScreenRelative,
                    Velocity = motion.Velocity,
                    ScreenVelocity = motion.ScreenVelocity,
                    Tilt = motion.Tilt,
                    PenInverted = motion.PenInverted,
                    Pressure = motion.Pressure,
                };

            if (source is InputEventScreenTouch touch)
            {
                var current = _mouseFromTouch == (touch.Device, touch.Index);
                if (touch.Pressed && _emulateMouseFromTouch && _mouseFromTouch is null)
                {
                    _mouseFromTouch = (touch.Device, touch.Index);
                    current = true;
                }
                else if (!touch.Pressed && current)
                    _mouseFromTouch = null;

                if (!current)
                    return null;

                return new InputEventMouseButton
                {
                    Device = InputEvent.DeviceIdEmulation,
                    WindowID = touch.WindowID,
                    ButtonIndex = MouseButton.Left,
                    ButtonMask = touch.Pressed ? _mouseButtonMask | MouseButtonMask.Left : _mouseButtonMask & ~MouseButtonMask.Left,
                    Position = touch.Position,
                    GlobalPosition = touch.Position,
                    Pressed = touch.Pressed,
                    Canceled = touch.Canceled,
                    DoubleClick = touch.DoubleTap,
                };
            }

            if (source is InputEventScreenDrag drag && _emulateMouseFromTouch &&
                _mouseFromTouch == (drag.Device, drag.Index))
                return new InputEventMouseMotion
                {
                    Device = InputEvent.DeviceIdEmulation,
                    WindowID = drag.WindowID,
                    ButtonMask = _mouseButtonMask,
                    Position = drag.Position,
                    GlobalPosition = drag.Position,
                    Relative = drag.Relative,
                    ScreenRelative = drag.ScreenRelative,
                    Velocity = drag.Velocity,
                    ScreenVelocity = drag.ScreenVelocity,
                    Tilt = drag.Tilt,
                    PenInverted = drag.PenInverted,
                    Pressure = drag.Pressure,
                };
        }

        return null;
    }

    internal void ReleasePressedEventsCore()
    {
        lock (_gate)
        {
            _keysPressed.Clear();
            _physicalKeysPressed.Clear();
            _keyLabelsPressed.Clear();
            _joyButtonsPressed.Clear();
            _joyAxes.Clear();
            _mouseButtonMasks.Clear();
            _mouseButtonMask = MouseButtonMask.None;
            _mouseFromTouch = null;
            _touchFromMouseDevice = null;

            foreach (var state in _actions.Values)
            {
                if (state.Pressed)
                {
                    state.ProcessJustReleased = true;
                    state.PhysicsJustReleased = true;
                    state.LastReleasedEventId = 0;
                }

                if (state.ExactPressed)
                {
                    state.ExactProcessJustReleased = true;
                    state.ExactPhysicsJustReleased = true;
                    state.ExactLastReleasedEventId = 0;
                }

                state.Contributions.Clear();
                state.Pressed = false;
                state.ExactPressed = false;
                state.Strength = 0f;
                state.RawStrength = 0f;
                state.ExactStrength = 0f;
                state.ExactRawStrength = 0f;
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>Appends read-only current-state descriptors.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(InputProperties);

    /// <inheritdoc />
    /// <remarks>The process-wide input service cannot be disposed.</remarks>
    /// <exception cref="InvalidOperationException">Always thrown because the singleton has process lifetime.</exception>
    protected override void ValidateDisposal() =>
        throw new InvalidOperationException("The process-wide Input instance cannot be disposed.");

    internal void OnActionMapChanged(string action, bool removed)
    {
        lock (_gate)
            _actions.Remove(action);
    }

    internal void CompleteFrame(bool physics)
    {
        lock (_gate)
        {
            foreach (var state in _actions.Values)
            {
                if (physics)
                {
                    state.PhysicsJustPressed = false;
                    state.PhysicsJustReleased = false;
                    state.ExactPhysicsJustPressed = false;
                    state.ExactPhysicsJustReleased = false;
                }
                else
                {
                    state.ProcessJustPressed = false;
                    state.ProcessJustReleased = false;
                    state.ExactProcessJustPressed = false;
                    state.ExactProcessJustReleased = false;
                }
            }
        }

        _queryingPhysicsLane = false;
    }

    internal void BeginFrame(bool physics) => _queryingPhysicsLane = physics;

    private ActionSnapshot GetActionSnapshot(string action, bool exactMatch)
    {
        EnsureRegisteredAction(action);
        lock (_gate)
        {
            if (!_actions.TryGetValue(action, out var state))
                return default;

            return new ActionSnapshot(
                exactMatch ? state.ExactPressed : state.Pressed,
                exactMatch ? state.ExactStrength : state.Strength,
                exactMatch ? state.ExactRawStrength : state.RawStrength,
                exactMatch
                    ? (_queryingPhysicsLane ? state.ExactPhysicsJustPressed : state.ExactProcessJustPressed)
                    : (_queryingPhysicsLane ? state.PhysicsJustPressed : state.ProcessJustPressed),
                exactMatch
                    ? (_queryingPhysicsLane ? state.ExactPhysicsJustReleased : state.ExactProcessJustReleased)
                    : (_queryingPhysicsLane ? state.PhysicsJustReleased : state.ProcessJustReleased));
        }
    }

    private bool IsActionTransitionByEvent(string action, InputEvent @event, bool exactMatch, bool pressed)
    {
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        var snapshot = GetActionSnapshot(action, exactMatch);
        if (pressed ? !snapshot.JustPressed : !snapshot.JustReleased)
            return false;

        lock (_gate)
        {
            if (!_actions.TryGetValue(action, out var state))
                return false;

            var eventId = exactMatch
                ? (pressed ? state.ExactLastPressedEventId : state.ExactLastReleasedEventId)
                : (pressed ? state.LastPressedEventId : state.LastReleasedEventId);
            return eventId == @event.InstanceID;
        }
    }

    private void UpdateRawState(InputEvent @event)
    {
        if (@event.IsCanceled() && @event is not InputEventMouseButton)
            return;

        switch (@event)
        {
            case InputEventKey key:
                UpdateSet(_keysPressed, NormalizeKey(key.Keycode), key.IsPressed() && key.Keycode != Key.None);
                UpdateSet(_physicalKeysPressed, NormalizeKey(key.PhysicalKeycode), key.IsPressed() && key.PhysicalKeycode != Key.None);
                UpdateSet(_keyLabelsPressed, NormalizeKey(key.KeyLabel), key.IsPressed() && key.KeyLabel != Key.None);
                break;

            case InputEventMouseButton mouseButton:
                var mask = ToMask(mouseButton.ButtonIndex);
                if (mask != MouseButtonMask.None)
                {
                    var deviceMask = _mouseButtonMasks.GetValueOrDefault(mouseButton.Device);
                    deviceMask = mouseButton.IsPressed() && !mouseButton.IsCanceled()
                        ? deviceMask | mask : deviceMask & ~mask;
                    SetMouseDeviceMask(mouseButton.Device, deviceMask);
                }
                break;

            case InputEventMouseMotion mouseMotion:
                SetMouseDeviceMask(mouseMotion.Device,
                    mouseMotion.Device == InputEvent.DeviceIdEmulation
                        ? mouseMotion.ButtonMask & MouseButtonMask.Left : mouseMotion.ButtonMask);
                _lastMouseVelocity = mouseMotion.Velocity;
                _lastMouseScreenVelocity = mouseMotion.ScreenVelocity;
                break;

            case InputEventJoypadButton joyButton:
                var joyButtonState = new JoyButtonState(joyButton.Device, joyButton.ButtonIndex);
                if (joyButton.IsPressed())
                    _joyButtonsPressed.Add(joyButtonState);
                else
                    _joyButtonsPressed.Remove(joyButtonState);
                break;

            case InputEventJoypadMotion joyMotion:
                _joyAxes[new JoyAxisState(joyMotion.Device, joyMotion.Axis)] = joyMotion.AxisValue;
                break;
        }
    }

    private void SetMouseDeviceMask(int device, MouseButtonMask mask)
    {
        if (mask == MouseButtonMask.None)
            _mouseButtonMasks.Remove(device);
        else
            _mouseButtonMasks[device] = mask;

        _mouseButtonMask = MouseButtonMask.None;
        foreach (var deviceMask in _mouseButtonMasks.Values)
            _mouseButtonMask |= deviceMask;
    }

    private void UpdateContribution(
        string action,
        ActionSource source,
        InputActionMatch match,
        bool exact,
        ulong eventId)
    {
        if (!_actions.TryGetValue(action, out var state))
        {
            state = new ActionState();
            _actions.Add(action, state);
        }

        var wasPressed = state.Pressed;
        var wasExactPressed = state.ExactPressed;

        if (match.Pressed)
            state.Contributions[source] = new ActionContribution(match.Strength, match.RawStrength, exact);
        else
            state.Contributions.Remove(source);

        Recalculate(state);

        if (!wasPressed && state.Pressed)
        {
            state.ProcessJustPressed = true;
            state.PhysicsJustPressed = true;
            state.LastPressedEventId = eventId;
        }
        else if (wasPressed && !state.Pressed)
        {
            state.ProcessJustReleased = true;
            state.PhysicsJustReleased = true;
            state.LastReleasedEventId = eventId;
        }

        if (!wasExactPressed && state.ExactPressed)
        {
            state.ExactProcessJustPressed = true;
            state.ExactPhysicsJustPressed = true;
            state.ExactLastPressedEventId = eventId;
        }
        else if (wasExactPressed && !state.ExactPressed)
        {
            state.ExactProcessJustReleased = true;
            state.ExactPhysicsJustReleased = true;
            state.ExactLastReleasedEventId = eventId;
        }
    }

    private static void Recalculate(ActionState state)
    {
        var strength = 0f;
        var rawStrength = 0f;
        var exactStrength = 0f;
        var exactRawStrength = 0f;
        var exactPressed = false;

        foreach (var contribution in state.Contributions.Values)
        {
            strength = Mathf.Max(strength, contribution.Strength);
            rawStrength = Mathf.Max(rawStrength, contribution.RawStrength);
            if (contribution.Exact)
            {
                exactPressed = true;
                exactStrength = Mathf.Max(exactStrength, contribution.Strength);
                exactRawStrength = Mathf.Max(exactRawStrength, contribution.RawStrength);
            }
        }

        state.Strength = strength;
        state.RawStrength = rawStrength;
        state.ExactStrength = exactStrength;
        state.ExactRawStrength = exactRawStrength;
        state.Pressed = state.Contributions.Count != 0;
        state.ExactPressed = exactPressed;
    }

    private static void UpdateSet(HashSet<Key> set, Key key, bool pressed)
    {
        if (key == Key.None)
            return;

        if (pressed)
            set.Add(key);
        else
            set.Remove(key);
    }

    private static Key NormalizeKey(Key keycode) =>
        (Key)((int)keycode & (int)KeyModifierMask.CodeMask);

    private static MouseButtonMask ToMask(MouseButton button) => button switch
    {
        MouseButton.Left => MouseButtonMask.Left,
        MouseButton.Right => MouseButtonMask.Right,
        MouseButton.Middle => MouseButtonMask.Middle,
        MouseButton.XButton1 => MouseButtonMask.XButton1,
        MouseButton.XButton2 => MouseButtonMask.XButton2,
        _ => MouseButtonMask.None,
    };

    private static void EnsureRegisteredAction(string action)
    {
        if (!InputMap.HasAction(action))
            throw new KeyNotFoundException($"Input action '{action}' is not registered.");
    }

    private static void ValidateDevice(int device)
    {
        if (device < 0)
            throw new ArgumentOutOfRangeException(nameof(device), device, "A controller device identifier cannot be negative.");
    }

    private static readonly ActionSource SyntheticSource = new(InputEvent.DeviceIdInternal, MaxEventsPerAction);

    private readonly record struct JoyButtonState(int Device, JoyButton Button);
    private readonly record struct JoyAxisState(int Device, JoyAxis Axis);
    private readonly record struct ActionSource(int Device, int EventIndex);
    private readonly record struct ActionContribution(float Strength, float RawStrength, bool Exact);
    private readonly record struct ActionSnapshot(bool Pressed, float Strength, float RawStrength, bool JustPressed, bool JustReleased);

    private sealed class ActionState
    {
        public readonly Dictionary<ActionSource, ActionContribution> Contributions = [];
        public bool Pressed;
        public bool ExactPressed;
        public float Strength;
        public float RawStrength;
        public float ExactStrength;
        public float ExactRawStrength;
        public bool ProcessJustPressed;
        public bool ProcessJustReleased;
        public bool PhysicsJustPressed;
        public bool PhysicsJustReleased;
        public bool ExactProcessJustPressed;
        public bool ExactProcessJustReleased;
        public bool ExactPhysicsJustPressed;
        public bool ExactPhysicsJustReleased;
        public ulong LastPressedEventId;
        public ulong LastReleasedEventId;
        public ulong ExactLastPressedEventId;
        public ulong ExactLastReleasedEventId;
    }
}
