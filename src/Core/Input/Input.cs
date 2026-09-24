namespace Electron2D;

/// <summary>Owns process-wide input state and translates typed events into named actions.</summary>
/// <remarks>
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
            new PropertyDescriptor<Input, MouseButtonMask>(nameof(MouseButtonMask), input => input.MouseButtonMask),
            new PropertyDescriptor<Input, Vector2>(nameof(LastMouseVelocity), input => input.LastMouseVelocity),
            new PropertyDescriptor<Input, Vector2>(nameof(LastMouseScreenVelocity), input => input.LastMouseScreenVelocity),
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

    /// <summary>Gets the process-wide input service.</summary>
    /// <value>The same non-disposable instance for the lifetime of the process.</value>
    public static Input Instance => SharedInstance;

    /// <summary>Gets or sets whether the native host combines consecutive pointer-motion events.</summary>
    /// <value><see langword="true"/> by default; the setting has no effect without a native host.</value>
    /// <remarks>Changes take effect on the next native event. Keyboard, button, and touch events retain queue order.</remarks>
    public bool UseAccumulatedInput
    {
        get { lock (_gate) return _useAccumulatedInput; }
        set { lock (_gate) _useAccumulatedInput = value; }
    }

    /// <summary>Gets or sets whether the first active touch contact also sends left-button mouse events.</summary>
    /// <value><see langword="true"/> by default.</value>
    /// <remarks>Generated mouse events use <see cref="InputEvent.DeviceIdEmulation"/> and update mouse and action state before scene delivery. Other contacts remain touch-only. Disabling this during a contact suppresses further motion but its release still releases the emulated button.</remarks>
    public bool EmulateMouseFromTouch
    {
        get { lock (_gate) return _emulateMouseFromTouch; }
        set { lock (_gate) _emulateMouseFromTouch = value; }
    }

    /// <summary>Gets or sets whether left-button mouse clicks and drags also send touch events.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>Generated touch events use index zero and <see cref="InputEvent.DeviceIdEmulation"/>. One mouse device owns an active emulated contact. Events from another device cannot move or end it. Generated touch events are delivered to the scene without changing raw or mapped input state. Disabling this during a press suppresses further drags but its release still ends the emulated contact.</remarks>
    public bool EmulateTouchFromMouse
    {
        get { lock (_gate) return _emulateTouchFromMouse; }
        set { lock (_gate) _emulateTouchFromMouse = value; }
    }

    /// <summary>Delivers any native pointer motion currently held by the display adapter.</summary>
    /// <remarks>This is a no-op when no native host is active or its event batch has no pending motion.</remarks>
    public void FlushBufferedEvents()
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

    /// <summary>Gets the non-wheel mouse buttons currently held.</summary>
    /// <value>A thread-safe snapshot of the current button mask.</value>
    public MouseButtonMask MouseButtonMask
    {
        get
        {
            lock (_gate)
                return _mouseButtonMask;
        }
    }

    /// <summary>Gets the most recently submitted local mouse velocity.</summary>
    /// <value>A thread-safe snapshot in content-scaled pixels per second.</value>
    public Vector2 LastMouseVelocity
    {
        get
        {
            lock (_gate)
                return _lastMouseVelocity;
        }
    }

    /// <summary>Gets the most recently submitted screen-space mouse velocity.</summary>
    /// <value>A thread-safe snapshot in unscaled screen pixels per second.</value>
    public Vector2 LastMouseScreenVelocity
    {
        get
        {
            lock (_gate)
                return _lastMouseScreenVelocity;
        }
    }

    /// <summary>Gets whether a logical key is currently held.</summary>
    /// <param name="keycode">A non-modifier logical key code.</param>
    /// <returns><see langword="true"/> when held.</returns>
    public bool IsKeyPressed(Key keycode)
    {
        lock (_gate)
            return _keysPressed.Contains(NormalizeKey(keycode));
    }

    /// <summary>Gets whether a physical key position is currently held.</summary>
    /// <param name="keycode">A non-modifier physical key code.</param>
    /// <returns><see langword="true"/> when held.</returns>
    public bool IsPhysicalKeyPressed(Key keycode)
    {
        lock (_gate)
            return _physicalKeysPressed.Contains(NormalizeKey(keycode));
    }

    /// <summary>Gets whether a localized key label is currently held.</summary>
    /// <param name="keycode">A non-modifier key label.</param>
    /// <returns><see langword="true"/> when held.</returns>
    public bool IsKeyLabelPressed(Key keycode)
    {
        lock (_gate)
            return _keyLabelsPressed.Contains(NormalizeKey(keycode));
    }

    /// <summary>Gets whether a non-wheel mouse button is currently held.</summary>
    /// <param name="button">The button to query.</param>
    /// <returns><see langword="true"/> when the button is held.</returns>
    /// <remarks>Wheel directions are transient events and always return <see langword="false"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="button"/> is not defined.</exception>
    public bool IsMouseButtonPressed(MouseButton button)
    {
        if (!Enum.IsDefined(button))
            throw new ArgumentOutOfRangeException(nameof(button), button, "Unknown mouse button.");

        var mask = ToMask(button);
        lock (_gate)
            return mask != MouseButtonMask.None && (_mouseButtonMask & mask) != 0;
    }

    /// <summary>Gets whether a controller button is currently held.</summary>
    /// <param name="button">Any signed standardized or raw button index.</param>
    /// <param name="device">The non-negative controller identifier.</param>
    /// <returns><see langword="true"/> when held.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="device"/> is negative.</exception>
    public bool IsJoyButtonPressed(JoyButton button, int device = 0)
    {
        ValidateDevice(device);
        lock (_gate)
            return _joyButtonsPressed.Contains(new JoyButtonState(device, button));
    }

    /// <summary>Gets the latest controller-axis value.</summary>
    /// <param name="axis">Any signed standardized or raw axis index.</param>
    /// <param name="device">The non-negative controller identifier.</param>
    /// <returns>The last stored source value, or zero before the first event.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="device"/> is negative.</exception>
    public float GetJoyAxis(JoyAxis axis, int device = 0)
    {
        ValidateDevice(device);
        lock (_gate)
            return _joyAxes.GetValueOrDefault(new JoyAxisState(device, axis));
    }

    /// <summary>Gets whether any key, mouse button, controller button, or action is currently pressed.</summary>
    /// <returns><see langword="true"/> when at least one tracked input is pressed.</returns>
    public bool IsAnythingPressed()
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

    /// <summary>Gets whether an action is currently pressed.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether only contributions with exact modifiers or analog direction are considered.</param>
    /// <returns><see langword="true"/> when at least one matching source is pressed.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public bool IsActionPressed(string action, bool exactMatch = false) =>
        GetActionSnapshot(action, exactMatch).Pressed;

    /// <summary>Gets whether an action transitioned from released to pressed since the current callback lane last completed.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether the transition must have been caused by an exact match.</param>
    /// <returns><see langword="true"/> during the current process and physics transition windows.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public bool IsActionJustPressed(string action, bool exactMatch = false)
    {
        var snapshot = GetActionSnapshot(action, exactMatch);
        return snapshot.JustPressed;
    }

    /// <summary>Gets whether an action transitioned from pressed to released since the current callback lane last completed.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether only exact-match state is considered.</param>
    /// <returns><see langword="true"/> during the current process and physics transition windows.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public bool IsActionJustReleased(string action, bool exactMatch = false)
    {
        var snapshot = GetActionSnapshot(action, exactMatch);
        return snapshot.JustReleased;
    }

    /// <summary>Gets whether a specific event caused the action's current just-pressed transition.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="event">The event reference previously submitted to <see cref="ParseInputEvent"/>.</param>
    /// <param name="exactMatch">Whether the event must have matched exactly.</param>
    /// <returns><see langword="true"/> when the current transition was caused by <paramref name="event"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="event"/> is disposing or disposed.</exception>
    public bool IsActionJustPressedByEvent(string action, InputEvent @event, bool exactMatch = false) =>
        IsActionTransitionByEvent(action, @event, exactMatch, pressed: true);

    /// <summary>Gets whether a specific event caused the action's current just-released transition.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="event">The event reference previously submitted to <see cref="ParseInputEvent"/>.</param>
    /// <param name="exactMatch">Whether the event must have matched exactly.</param>
    /// <returns><see langword="true"/> when the current transition was caused by <paramref name="event"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="event"/> is disposing or disposed.</exception>
    public bool IsActionJustReleasedByEvent(string action, InputEvent @event, bool exactMatch = false) =>
        IsActionTransitionByEvent(action, @event, exactMatch, pressed: false);

    /// <summary>Gets an action's deadzone-adjusted strength.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether only exact contributions are considered.</param>
    /// <returns>The greatest matching strength from zero through one.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public float GetActionStrength(string action, bool exactMatch = false) =>
        GetActionSnapshot(action, exactMatch).Strength;

    /// <summary>Gets an action's strength before deadzone remapping.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether only exact contributions are considered.</param>
    /// <returns>The greatest matching raw strength from zero through one.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public float GetActionRawStrength(string action, bool exactMatch = false) =>
        GetActionSnapshot(action, exactMatch).RawStrength;

    /// <summary>Combines a negative and positive action into one signed axis.</summary>
    /// <param name="negativeAction">The action contributing toward minus one.</param>
    /// <param name="positiveAction">The action contributing toward plus one.</param>
    /// <returns>Positive strength minus negative strength, from minus one through one.</returns>
    /// <exception cref="ArgumentException">An action name is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">An action name is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">An action is not registered.</exception>
    public float GetAxis(string negativeAction, string positiveAction) =>
        GetActionStrength(positiveAction) - GetActionStrength(negativeAction);

    /// <summary>Combines four actions into a circularly deadzoned two-dimensional input vector.</summary>
    /// <param name="negativeX">The action contributing toward negative X.</param>
    /// <param name="positiveX">The action contributing toward positive X.</param>
    /// <param name="negativeY">The action contributing toward negative Y.</param>
    /// <param name="positiveY">The action contributing toward positive Y.</param>
    /// <param name="deadzone">A finite override from zero through one, or minus one to average the four action deadzones.</param>
    /// <returns>A vector whose length does not exceed one.</returns>
    /// <exception cref="ArgumentException">An action name is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">An action name is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="deadzone"/> is not minus one or a finite value from zero through one.</exception>
    /// <exception cref="KeyNotFoundException">An action is not registered.</exception>
    public Vector2 GetVector(
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
            deadzone = (InputMap.Instance.ActionGetDeadzone(negativeX) +
                InputMap.Instance.ActionGetDeadzone(positiveX) +
                InputMap.Instance.ActionGetDeadzone(negativeY) +
                InputMap.Instance.ActionGetDeadzone(positiveY)) / 4f;
        }

        var vector = new Vector2(
            GetActionRawStrength(positiveX) - GetActionRawStrength(negativeX),
            GetActionRawStrength(positiveY) - GetActionRawStrength(negativeY));
        var length = vector.Length();
        if (length <= deadzone)
            return Vector2.Zero;

        if (length > 1f)
            vector /= length;
        else if (deadzone > 0f)
            vector *= Mathf.InverseLerp(deadzone, 1f, length) / length;

        return vector;
    }

    /// <summary>Presses a registered action without producing an input event.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="strength">A finite strength clamped to zero through one.</param>
    /// <remarks>A zero-strength source is still pressed but contributes zero analog strength. Call <see cref="ActionRelease"/> to remove it.</remarks>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="strength"/> is NaN or infinite.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public void ActionPress(string action, float strength = 1f)
    {
        EnsureRegisteredAction(action);
        if (!float.IsFinite(strength))
            throw new ArgumentOutOfRangeException(nameof(strength), strength, "Action strength must be finite.");

        lock (_gate)
            UpdateContribution(action, SyntheticSource, new InputActionMatch(true, Mathf.Clamp(strength, 0f, 1f), Mathf.Clamp(strength, 0f, 1f)), exact: true, eventId: 0);
    }

    /// <summary>Releases the synthetic source of a registered action without producing an input event.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public void ActionRelease(string action)
    {
        EnsureRegisteredAction(action);
        lock (_gate)
            UpdateContribution(action, SyntheticSource, default, exact: true, eventId: 0);
    }

    /// <summary>Submits one typed input event, updates state, and synchronously routes it to the active main loop.</summary>
    /// <param name="event">A live event. Ownership remains with the caller.</param>
    /// <remarks>
    /// Mapping and state changes are committed before callbacks. Optional pointer emulation delivers its synthetic
    /// event before the source event, with device ID <see cref="InputEvent.DeviceIdEmulation"/>. Re-entry is rejected.
    /// Both events remain delivered if either callback fails; committed state is not rolled back. An active loop
    /// validates owner-thread and lifecycle eligibility before any state changes.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Parsing is re-entered, an active loop is called off its owner thread or during another callback, or a direct action event cannot obtain a valid source index.</exception>
    /// <exception cref="KeyNotFoundException">An <see cref="InputEventAction"/> names an unregistered action.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="event"/> or a matched binding is disposing or disposed.</exception>
    /// <exception cref="AggregateException">One or more scene input callbacks throw after state is committed.</exception>
    public void ParseInputEvent(InputEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        var mainLoop = Engine.Instance.MainLoop;
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
            InputMap.Instance.CollectMatches(@event, _matches);
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

    /// <summary>Releases every tracked key, mouse button, controller button, axis, and action source.</summary>
    /// <remarks>
    /// Actions that were pressed receive a just-released transition in both callback lanes. This method does not emit
    /// events or route callbacks and does not alter <see cref="InputMap"/>.
    /// </remarks>
    public void ReleasePressedEvents()
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
        if (!InputMap.Instance.HasAction(action))
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
