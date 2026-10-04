namespace Electron2D;

public sealed partial class Input
{
    /// <summary>Occurs after a native controller connects or disconnects and its state is committed.</summary>
    /// <remarks>Raised synchronously on the display owner thread during event delivery.</remarks>
    public static event Action<int, bool>? JoyConnectionChanged
    {
        add => Service.JoyConnectionChangedCore += value;
        remove => Service.JoyConnectionChangedCore -= value;
    }

    /// <summary>Gets a sorted snapshot of connected native controller IDs.</summary>
    /// <returns>Caller-owned IDs, or an empty array when no native controller is connected.</returns>
    public static int[] GetConnectedJoypads() => Service.GetConnectedJoypadsCore();

    /// <summary>Gets the mapped name of a native controller.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>The name, or an empty string for an absent device.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public static string GetJoyName(int device) => Service.GetJoyNameCore(device);

    /// <summary>Gets the SDL-compatible identifier of a native controller.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>The hexadecimal GUID, or an empty string for an absent device.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public static string GetJoyGUID(int device) => Service.GetJoyGUIDCore(device);

    /// <summary>Gets extra typed native information for a connected controller.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>Information for the device, or null when absent.</returns>
    /// <remarks>Raw name, USB IDs and optional serial are available. Platform-specific Steam Input and XInput indices are not exposed by this runtime projection.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public static JoypadInfo? GetJoyInfo(int device) => Service.GetJoyInfoCore(device);

    /// <summary>Returns whether the controller has a standardized gamepad mapping.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>True for a mapped native controller.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public static bool IsJoyKnown(int device) => Service.IsJoyKnownCore(device);

    /// <summary>Returns whether the connected controller supports vibration.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>True when the native backend reports rumble support.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public static bool HasJoyVibration(int device) => Service.HasJoyVibrationCore(device);

    /// <summary>Returns whether the connected controller has a controllable LED.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>True when the native backend reports LED support.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public static bool HasJoyLight(int device) => Service.HasJoyLightCore(device);

    /// <summary>Reports whether an environment-configured vendor/product pair is ignored by native controller discovery.</summary>
    /// <param name="vendorID">USB vendor identifier.</param>
    /// <param name="productID">USB product identifier.</param>
    /// <returns>True for a pair in SDL_GAMECONTROLLER_IGNORE_DEVICES at input initialization.</returns>
    public static bool ShouldIgnoreDevice(int vendorID, int productID) => Service.ShouldIgnoreDeviceCore(vendorID, productID);

    /// <summary>Adds an SDL-style controller mapping for future connections.</summary>
    /// <param name="mapping">A GUID, name and binding list separated by commas.</param>
    /// <param name="updateExisting">Whether connected devices with this GUID should adopt the mapping now.</param>
    /// <remarks>The process-wide overlay applies through the native host; a false update flag leaves current devices unchanged until reconnection. The binding grammar beyond the required GUID/name fields is validated by SDL when applied to a device.</remarks>
    /// <exception cref="ArgumentNullException">Mapping is null.</exception>
    /// <exception cref="ArgumentException">The GUID, name or binding section is missing.</exception>
    /// <exception cref="InvalidOperationException">A live display is accessed off its owner thread.</exception>
    public static void AddJoyMapping(string mapping, bool updateExisting = false) => Service.AddJoyMappingCore(mapping, updateExisting);

    /// <summary>Removes a controller mapping and restores raw input for connected matching devices.</summary>
    /// <param name="guid">The SDL-compatible GUID to remove.</param>
    /// <remarks>Removal also suppresses the built-in mapping for this GUID in the current process; raw joystick events remain available.</remarks>
    /// <exception cref="ArgumentException">The GUID is blank.</exception>
    /// <exception cref="InvalidOperationException">A live display is accessed off its owner thread.</exception>
    public static void RemoveJoyMapping(string guid) => Service.RemoveJoyMappingCore(guid);

    /// <summary>Starts a controller rumble effect with independent weak and strong motors.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <param name="weakMagnitude">Weak motor strength from zero through one.</param>
    /// <param name="strongMagnitude">Strong motor strength from zero through one.</param>
    /// <param name="duration">Duration in seconds; zero requests the native maximum of about 65 seconds.</param>
    /// <exception cref="ArgumentOutOfRangeException">The device ID, motor strength or duration is invalid.</exception>
    /// <exception cref="InvalidOperationException">A live display is accessed off its owner thread.</exception>
    public static void StartJoyVibration(int device, float weakMagnitude, float strongMagnitude, float duration = 0) => Service.StartJoyVibrationCore(device, weakMagnitude, strongMagnitude, duration);

    /// <summary>Stops the controller's current rumble effect.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    /// <exception cref="InvalidOperationException">A live display is accessed off its owner thread.</exception>
    public static void StopJoyVibration(int device) => Service.StopJoyVibrationCore(device);

    /// <summary>Gets the last requested weak and strong rumble strengths.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>The retained request, or zero when none was made.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public static Vector2 GetJoyVibrationStrength(int device) => Service.GetJoyVibrationStrengthCore(device);

    /// <summary>Gets the last requested rumble duration in seconds.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>The retained duration, or zero when none was made.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public static float GetJoyVibrationDuration(int device) => Service.GetJoyVibrationDurationCore(device);

    /// <summary>Estimates the remaining native rumble duration in seconds.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>Zero when the controller is absent, unsupported, stopped or expired.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public static float GetJoyVibrationRemainingDuration(int device) => Service.GetJoyVibrationRemainingDurationCore(device);

    /// <summary>Returns whether a requested controller rumble effect still has time remaining.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <returns>True while a supported device's effect is active.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    public static bool IsJoyVibrating(int device) => Service.IsJoyVibratingCore(device);

    /// <summary>Sets a connected controller's LED when its hardware supports it.</summary>
    /// <param name="device">Nonnegative controller ID.</param>
    /// <param name="color">Finite color; RGB channels clamp to zero through one.</param>
    /// <exception cref="ArgumentOutOfRangeException">The device ID is negative.</exception>
    /// <exception cref="ArgumentException">The color is not finite.</exception>
    /// <exception cref="InvalidOperationException">A live display is accessed off its owner thread.</exception>
    public static void SetJoyLight(int device, Color color) => Service.SetJoyLightCore(device, color);

    /// <summary>Gets or sets whether native controller input and effects are ignored while the application lacks focus.</summary>
    /// <value>False by default.</value>
    /// <remarks>Engine.Run samples <see cref="ProjectSettings.IgnoreJoypadOnUnfocusedApplication"/> before opening the native host. Enabling this while unfocused releases pressed input and stops native rumble; subsequent controller input and effect requests are suppressed until focus returns.</remarks>
    /// <exception cref="InvalidOperationException">The active display policy is changed off its owner thread.</exception>
    public static bool IgnoreJoypadOnUnfocusedApplication
    {
        get => Service.IgnoreJoypadOnUnfocusedApplicationCore;
        set => Service.IgnoreJoypadOnUnfocusedApplicationCore = value;
    }

    /// <summary>Gets or sets the active native pointer mode.</summary>
    /// <remarks>The setter applies immediately to the active display and retains the previous mode if SDL rejects it.</remarks>
    /// <exception cref="InvalidOperationException">There is no active display or the native change fails.</exception>
    public static MouseMode MouseMode
    {
        get => Service.MouseModeCore;
        set => Service.MouseModeCore = value;
    }

    /// <summary>Gets the shape most recently installed on the active display.</summary>
    /// <returns>The currently selected shape, including one selected directly through DisplayServer.</returns>
    /// <exception cref="InvalidOperationException">There is no active display.</exception>
    public static CursorShape GetCurrentCursorShape() => Service.GetCurrentCursorShapeCore();

    /// <summary>Sets the viewport's default native cursor shape and refreshes its current selection.</summary>
    /// <param name="shape">A standard shape; Arrow by default.</param>
    /// <remarks>A hovered Control can override this default. The active scene's cursor is refreshed without synthesizing a mouse-motion input event.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The shape is invalid.</exception>
    /// <exception cref="InvalidOperationException">There is no active display or the native change fails.</exception>
    public static void SetDefaultCursorShape(CursorShape shape = CursorShape.Arrow) => Service.SetDefaultCursorShapeCore(shape);

    /// <summary>Installs or clears a copied image for one native cursor shape.</summary>
    /// <param name="image">A caller-owned Image or readable Texture, or null to restore the system shape.</param>
    /// <param name="shape">The shape slot to customize.</param>
    /// <param name="hotspot">The active pixel position in the source image.</param>
    /// <remarks>The display copies the pixels before return. A later edit of the source requires another call.</remarks>
    public static void SetCustomMouseCursor(Resource? image, CursorShape shape = CursorShape.Arrow, Vector2 hotspot = default) => Service.SetCustomMouseCursorCore(image, shape, hotspot);

    /// <summary>Requests pointer movement to a client-area position on backends that support warping.</summary>
    /// <param name="position">Finite client coordinates; fractional values are truncated to native integer units.</param>
    /// <exception cref="ArgumentException">The position is nonfinite or outside native integer coordinates.</exception>
    /// <exception cref="InvalidOperationException">There is no active display.</exception>
    /// <exception cref="NotSupportedException">The display backend does not support pointer warping.</exception>
    public static void WarpMouse(Vector2 position) => Service.WarpMouseCore(position);

    /// <summary>Gets or sets whether the native host combines consecutive pointer-motion events.</summary>
    /// <value><see langword="true"/> by default; the setting has no effect without a native host.</value>
    /// <remarks>Changes take effect on the next native event. Keyboard, button, and touch events retain queue order.</remarks>
    public static bool UseAccumulatedInput
    {
        get => Service.UseAccumulatedInputCore;
        set => Service.UseAccumulatedInputCore = value;
    }

    /// <summary>Gets or sets whether the first active touch contact also sends left-button mouse events.</summary>
    /// <value><see langword="true"/> by default.</value>
    /// <remarks>Generated mouse events use <see cref="InputEvent.DeviceIdEmulation"/> and update mouse and action state before scene delivery. Other contacts remain touch-only. Disabling this during a contact suppresses further motion but its release still releases the emulated button.</remarks>
    public static bool EmulateMouseFromTouch
    {
        get => Service.EmulateMouseFromTouchCore;
        set => Service.EmulateMouseFromTouchCore = value;
    }

    /// <summary>Gets or sets whether left-button mouse clicks and drags also send touch events.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>Generated touch events use index zero and <see cref="InputEvent.DeviceIdEmulation"/>. One mouse device owns an active emulated contact. Events from another device cannot move or end it. Generated touch events are delivered to the scene without changing raw or mapped input state. Disabling this during a press suppresses further drags but its release still ends the emulated contact.</remarks>
    public static bool EmulateTouchFromMouse
    {
        get => Service.EmulateTouchFromMouseCore;
        set => Service.EmulateTouchFromMouseCore = value;
    }

    /// <summary>Delivers any native pointer motion currently held by the display adapter.</summary>
    /// <remarks>This is a no-op when no native host is active or its event batch has no pending motion.</remarks>
    public static void FlushBufferedEvents() => Service.FlushBufferedEventsCore();

    /// <summary>Gets the non-wheel mouse buttons currently held.</summary>
    /// <value>A thread-safe snapshot of the current button mask.</value>
    public static MouseButtonMask MouseButtonMask
    {
        get => Service.MouseButtonMaskCore;
    }

    /// <summary>Gets the most recently submitted local mouse velocity.</summary>
    /// <value>A thread-safe snapshot in content-scaled pixels per second.</value>
    public static Vector2 LastMouseVelocity
    {
        get => Service.LastMouseVelocityCore;
    }

    /// <summary>Gets the most recently submitted screen-space mouse velocity.</summary>
    /// <value>A thread-safe snapshot in unscaled screen pixels per second.</value>
    public static Vector2 LastMouseScreenVelocity
    {
        get => Service.LastMouseScreenVelocityCore;
    }

    /// <summary>Gets whether a logical key is currently held.</summary>
    /// <param name="keycode">A non-modifier logical key code.</param>
    /// <returns><see langword="true"/> when held.</returns>
    public static bool IsKeyPressed(Key keycode) => Service.IsKeyPressedCore(keycode);

    /// <summary>Gets whether a physical key position is currently held.</summary>
    /// <param name="keycode">A non-modifier physical key code.</param>
    /// <returns><see langword="true"/> when held.</returns>
    public static bool IsPhysicalKeyPressed(Key keycode) => Service.IsPhysicalKeyPressedCore(keycode);

    /// <summary>Gets whether a localized key label is currently held.</summary>
    /// <param name="keycode">A non-modifier key label.</param>
    /// <returns><see langword="true"/> when held.</returns>
    public static bool IsKeyLabelPressed(Key keycode) => Service.IsKeyLabelPressedCore(keycode);

    /// <summary>Gets whether a non-wheel mouse button is currently held.</summary>
    /// <param name="button">The button to query.</param>
    /// <returns><see langword="true"/> when the button is held.</returns>
    /// <remarks>Wheel directions are transient events and always return <see langword="false"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="button"/> is not defined.</exception>
    public static bool IsMouseButtonPressed(MouseButton button) => Service.IsMouseButtonPressedCore(button);

    /// <summary>Gets whether a controller button is currently held.</summary>
    /// <param name="button">Any signed standardized or raw button index.</param>
    /// <param name="device">The non-negative controller identifier.</param>
    /// <returns><see langword="true"/> when held.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="device"/> is negative.</exception>
    public static bool IsJoyButtonPressed(JoyButton button, int device = 0) => Service.IsJoyButtonPressedCore(button, device);

    /// <summary>Gets the latest controller-axis value.</summary>
    /// <param name="axis">Any signed standardized or raw axis index.</param>
    /// <param name="device">The non-negative controller identifier.</param>
    /// <returns>The last stored source value, or zero before the first event.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="device"/> is negative.</exception>
    public static float GetJoyAxis(JoyAxis axis, int device = 0) => Service.GetJoyAxisCore(axis, device);

    /// <summary>Gets whether any key, mouse button, controller button, or action is currently pressed.</summary>
    /// <returns><see langword="true"/> when at least one tracked input is pressed.</returns>
    public static bool IsAnythingPressed() => Service.IsAnythingPressedCore();

    /// <summary>Gets whether an action is currently pressed.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether only contributions with exact modifiers or analog direction are considered.</param>
    /// <returns><see langword="true"/> when at least one matching source is pressed.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static bool IsActionPressed(string action, bool exactMatch = false) => Service.IsActionPressedCore(action, exactMatch);

    /// <summary>Gets whether an action transitioned from released to pressed since the current callback lane last completed.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether the transition must have been caused by an exact match.</param>
    /// <returns><see langword="true"/> during the current process and physics transition windows.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static bool IsActionJustPressed(string action, bool exactMatch = false) => Service.IsActionJustPressedCore(action, exactMatch);

    /// <summary>Gets whether an action transitioned from pressed to released since the current callback lane last completed.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether only exact-match state is considered.</param>
    /// <returns><see langword="true"/> during the current process and physics transition windows.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static bool IsActionJustReleased(string action, bool exactMatch = false) => Service.IsActionJustReleasedCore(action, exactMatch);

    /// <summary>Gets whether a specific event caused the action's current just-pressed transition.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="event">The event reference previously submitted to <see cref="ParseInputEvent"/>.</param>
    /// <param name="exactMatch">Whether the event must have matched exactly.</param>
    /// <returns><see langword="true"/> when the current transition was caused by <paramref name="event"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="event"/> is disposing or disposed.</exception>
    public static bool IsActionJustPressedByEvent(string action, InputEvent @event, bool exactMatch = false) => Service.IsActionJustPressedByEventCore(action, @event, exactMatch);

    /// <summary>Gets whether a specific event caused the action's current just-released transition.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="event">The event reference previously submitted to <see cref="ParseInputEvent"/>.</param>
    /// <param name="exactMatch">Whether the event must have matched exactly.</param>
    /// <returns><see langword="true"/> when the current transition was caused by <paramref name="event"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException"><paramref name="event"/> is disposing or disposed.</exception>
    public static bool IsActionJustReleasedByEvent(string action, InputEvent @event, bool exactMatch = false) => Service.IsActionJustReleasedByEventCore(action, @event, exactMatch);

    /// <summary>Gets an action's deadzone-adjusted strength.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether only exact contributions are considered.</param>
    /// <returns>The greatest matching strength from zero through one.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static float GetActionStrength(string action, bool exactMatch = false) => Service.GetActionStrengthCore(action, exactMatch);

    /// <summary>Gets an action's strength before deadzone remapping.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether only exact contributions are considered.</param>
    /// <returns>The greatest matching raw strength from zero through one.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static float GetActionRawStrength(string action, bool exactMatch = false) => Service.GetActionRawStrengthCore(action, exactMatch);

    /// <summary>Combines a negative and positive action into one signed axis.</summary>
    /// <param name="negativeAction">The action contributing toward minus one.</param>
    /// <param name="positiveAction">The action contributing toward plus one.</param>
    /// <returns>Positive strength minus negative strength, from minus one through one.</returns>
    /// <exception cref="ArgumentException">An action name is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">An action name is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">An action is not registered.</exception>
    public static float GetAxis(string negativeAction, string positiveAction) => Service.GetAxisCore(negativeAction, positiveAction);

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
    public static Vector2 GetVector(
        string negativeX,
        string positiveX,
        string negativeY,
        string positiveY,
        float deadzone = -1f) => Service.GetVectorCore(negativeX, positiveX, negativeY, positiveY, deadzone);

    /// <summary>Presses a registered action without producing an input event.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <param name="strength">A finite strength clamped to zero through one.</param>
    /// <remarks>A zero-strength source is still pressed but contributes zero analog strength. Call <see cref="ActionRelease"/> to remove it.</remarks>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="strength"/> is NaN or infinite.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static void ActionPress(string action, float strength = 1f) => Service.ActionPressCore(action, strength);

    /// <summary>Releases the synthetic source of a registered action without producing an input event.</summary>
    /// <param name="action">The registered, case-sensitive action name.</param>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    public static void ActionRelease(string action) => Service.ActionReleaseCore(action);

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
    public static void ParseInputEvent(InputEvent @event) => Service.ParseInputEventCore(@event);

    /// <summary>Releases every tracked key, mouse button, controller button, axis, and action source.</summary>
    /// <remarks>
    /// Actions that were pressed receive a just-released transition in both callback lanes. This method does not emit
    /// events or route callbacks and does not alter <see cref="InputMap"/>.
    /// </remarks>
    public static void ReleasePressedEvents() => Service.ReleasePressedEventsCore();

}
