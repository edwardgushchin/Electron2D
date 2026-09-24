using System.Globalization;

namespace Electron2D;

/// <summary>Represents motion on one game-controller axis.</summary>
public sealed class InputEventJoypadMotion : InputEvent
{
    private static readonly string[] AxisDescriptions =
    [
        "Left Stick X-Axis, Joystick 0 X-Axis",
        "Left Stick Y-Axis, Joystick 0 Y-Axis",
        "Right Stick X-Axis, Joystick 1 X-Axis",
        "Right Stick Y-Axis, Joystick 1 Y-Axis",
        "Joystick 2 X-Axis, Left Trigger, Sony L2, Xbox LT",
        "Joystick 2 Y-Axis, Right Trigger, Sony R2, Xbox RT",
        "Joystick 3 X-Axis",
        "Joystick 3 Y-Axis",
        "Joystick 4 X-Axis",
        "Joystick 4 Y-Axis",
    ];

    private static readonly IReadOnlyList<PropertyDescriptor> MotionProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventJoypadMotion, JoyAxis>(nameof(Axis), @event => @event.Axis, (@event, value) => @event.Axis = value, _ => JoyAxis.LeftX, stored: true),
            new PropertyDescriptor<InputEventJoypadMotion, float>(nameof(AxisValue), @event => @event.AxisValue, (@event, value) => @event.AxisValue = value, _ => 0f, stored: true),
        ]);

    private JoyAxis _axis;
    private float _axisValue;

    /// <summary>Gets or sets the controller axis.</summary>
    /// <value>An axis index from <see cref="JoyAxis.Invalid"/> through <see cref="JoyAxis.Max"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric value is below <see cref="JoyAxis.Invalid"/> or above <see cref="JoyAxis.Max"/>.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public JoyAxis Axis
    {
        get { ThrowIfDisposed(); return _axis; }
        set
        {
            ThrowIfDisposed();
            if ((int)value < (int)JoyAxis.Invalid || (int)value > (int)JoyAxis.Max)
                throw new ArgumentOutOfRangeException(nameof(value), value, "A controller axis must be between -1 and 10.");
            _axis = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets or sets the current signed axis position.</summary>
    /// <value>The source value without range or finiteness validation; zero is the resting position.</value>
    /// <remarks>Magnitude at least 0.5 marks the event pressed, independently of an action's configured deadzone.</remarks>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public float AxisValue
    {
        get { ThrowIfDisposed(); return _axisValue; }
        set
        {
            ThrowIfDisposed();
            _axisValue = value;
            PressedState = MathF.Abs(value) >= 0.5f;
            EmitInputChanged();
        }
    }

    /// <inheritdoc />
    public override bool IsMatch(InputEvent @event, bool exactMatch = true)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        return @event is InputEventJoypadMotion motion && _axis == motion._axis &&
            (!exactMatch || (_axisValue < 0f) == (motion._axisValue < 0f));
    }

    /// <summary>Gets the localized axis description and signed value.</summary>
    /// <returns>The axis number, a known or unknown control description, and a value with two decimals or non-finite source text.</returns>
    /// <remarks>The description uses this event's translation domain. A translated template with invalid placeholders falls back to the source sentence.</remarks>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public override string AsText()
    {
        ThrowIfDisposed();
        const string source = "Joypad Motion on Axis %d (%s) with Value %.2f";
        return FormatTextTemplate(Tr(source), source,
            ((int)_axis).ToString(CultureInfo.InvariantCulture),
            Tr((int)_axis >= 0 && (int)_axis < AxisDescriptions.Length ?
                AxisDescriptions[(int)_axis] : "Unknown Joypad Axis"),
            FormatAxisValue(_axisValue));
    }

    private static string FormatAxisValue(float value)
    {
        if (float.IsNaN(value)) return BitConverter.SingleToInt32Bits(value) < 0 ? "-nan" : "nan";
        if (float.IsPositiveInfinity(value)) return "inf";
        if (float.IsNegativeInfinity(value)) return "-inf";
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventJoypadMotion();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventJoypadMotion)target;
        typed._axis = _axis;
        typed._axisValue = _axisValue;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(MotionProperties);

    internal override bool TryMatchAction(InputEvent actual, bool exactMatch, float deadzone, out InputActionMatch match)
    {
        match = default;
        if (actual is not InputEventJoypadMotion motion || _axis != motion._axis ||
            (exactMatch && (_axisValue < 0f) != (motion._axisValue < 0f)))
            return false;

        var raw = Mathf.Abs(motion._axisValue);
        var sameDirection = (_axisValue < 0f) == (motion._axisValue < 0f) || motion._axisValue == 0f;
        var pressed = sameDirection && raw >= deadzone;
        var strength = !pressed ? 0f : deadzone >= 1f ? 1f : Mathf.Clamp(Mathf.InverseLerp(deadzone, 1f, raw), 0f, 1f);
        match = new InputActionMatch(pressed, strength, sameDirection ? raw : 0f);
        return true;
    }
}

/// <summary>Represents a game-controller button press or release.</summary>
public sealed class InputEventJoypadButton : InputEvent
{
    private static readonly string[] ButtonDescriptions =
    [
        "Bottom Action, Sony Cross, Xbox A, Nintendo B",
        "Right Action, Sony Circle, Xbox B, Nintendo A",
        "Left Action, Sony Square, Xbox X, Nintendo Y",
        "Top Action, Sony Triangle, Xbox Y, Nintendo X",
        "Back, Sony Select, Xbox Back, Nintendo -",
        "Guide, Sony PS, Xbox Home",
        "Start, Xbox Menu, Nintendo +",
        "Left Stick, Sony L3, Xbox L/LS",
        "Right Stick, Sony R3, Xbox R/RS",
        "Left Shoulder, Sony L1, Xbox LB",
        "Right Shoulder, Sony R1, Xbox RB",
        "D-pad Up",
        "D-pad Down",
        "D-pad Left",
        "D-pad Right",
        "Xbox Share, PS5 Microphone, Nintendo Capture",
        "Xbox Paddle 1",
        "Xbox Paddle 2",
        "Xbox Paddle 3",
        "Xbox Paddle 4",
        "PS4/5 Touchpad",
    ];

    private static readonly IReadOnlyList<PropertyDescriptor> ButtonProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventJoypadButton, JoyButton>(nameof(ButtonIndex), @event => @event.ButtonIndex, (@event, value) => @event.ButtonIndex = value, _ => JoyButton.A, stored: true),
            new PropertyDescriptor<InputEventJoypadButton, bool>(nameof(Pressed), @event => @event.Pressed, (@event, value) => @event.Pressed = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventJoypadButton, float>(nameof(Pressure), @event => @event.Pressure, (@event, value) => @event.Pressure = value, _ => 0f, stored: true),
        ]);

    private JoyButton _buttonIndex;
    private float _pressure;

    /// <summary>Gets or sets the controller button.</summary>
    /// <value>A standardized or arbitrary signed raw button index.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public JoyButton ButtonIndex
    {
        get { ThrowIfDisposed(); return _buttonIndex; }
        set
        {
            ThrowIfDisposed();
            _buttonIndex = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets or sets whether the controller button is pressed.</summary>
    /// <value><see langword="false"/> for a release; <see langword="true"/> for a press.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public bool Pressed
    {
        get => IsPressed();
        set { ThrowIfDisposed(); PressedState = value; }
    }

    /// <summary>Gets or sets analog pressure reported for the button.</summary>
    /// <value>The source value without range or finiteness validation. Most hosts report zero and use <see cref="Pressed"/>.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public float Pressure
    {
        get { ThrowIfDisposed(); return _pressure; }
        set
        {
            ThrowIfDisposed();
            _pressure = value;
        }
    }

    /// <inheritdoc />
    public override bool IsMatch(InputEvent @event, bool exactMatch = true)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        return @event is InputEventJoypadButton button && _buttonIndex == button._buttonIndex;
    }

    /// <summary>Gets the localized button number, known description, and nonzero pressure.</summary>
    /// <returns>A numbered button with an optional known control description and pressure suffix.</returns>
    /// <remarks>Built-in descriptions cover IDs zero through 20; all other signed IDs use the numeric fallback. Pressure uses the full real-value text of the stored float.</remarks>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public override string AsText()
    {
        ThrowIfDisposed();
        const string source = "Joypad Button %d";
        var index = (int)_buttonIndex;
        var text = FormatTextTemplate(Tr(source), source, index.ToString(CultureInfo.InvariantCulture));
        if (index >= 0 && index < ButtonDescriptions.Length)
            text = string.Concat(text, " (", Tr(ButtonDescriptions[index]), ")");
        if (_pressure != 0f)
            text = string.Concat(text, ", ", Tr("Pressure:"), " ", FormatTextFloatAsDouble(_pressure));
        return text;
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventJoypadButton();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventJoypadButton)target;
        typed._buttonIndex = _buttonIndex;
        typed._pressure = _pressure;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(ButtonProperties);

    internal override bool TryMatchAction(InputEvent actual, bool exactMatch, float deadzone, out InputActionMatch match)
    {
        match = default;
        if (actual is not InputEventJoypadButton button || _buttonIndex != button._buttonIndex)
            return false;

        var pressed = button.IsPressed();
        var strength = pressed ? 1f : 0f;
        match = new InputActionMatch(pressed, strength, strength);
        return true;
    }
}
