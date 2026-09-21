namespace Electron2D;

/// <summary>Represents motion on one game-controller axis.</summary>
public sealed class InputEventJoypadMotion : InputEvent
{
    private static readonly IReadOnlyList<PropertyDescriptor> MotionProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventJoypadMotion, JoyAxis>(nameof(Axis), @event => @event.Axis, (@event, value) => @event.Axis = value, _ => JoyAxis.LeftX, stored: true),
            new PropertyDescriptor<InputEventJoypadMotion, float>(nameof(AxisValue), @event => @event.AxisValue, (@event, value) => @event.AxisValue = value, _ => 0f, stored: true),
        ]);

    private JoyAxis _axis;
    private float _axisValue;

    /// <summary>Gets or sets the controller axis.</summary>
    /// <value>An axis index from zero through nine.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric value is outside the supported raw-axis range.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public JoyAxis Axis
    {
        get { ThrowIfDisposed(); return _axis; }
        set
        {
            ThrowIfDisposed();
            if ((int)value < 0 || (int)value >= (int)JoyAxis.Max)
                throw new ArgumentOutOfRangeException(nameof(value), value, "A controller axis must be between 0 and 9.");
            _axis = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets or sets the current signed axis position.</summary>
    /// <value>A finite value from minus one through one; zero is the resting position.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside minus one through one, NaN, or infinite.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public float AxisValue
    {
        get { ThrowIfDisposed(); return _axisValue; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value < -1f || value > 1f)
                throw new ArgumentOutOfRangeException(nameof(value), value, "An axis value must be finite and between -1 and 1.");
            _axisValue = value;
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

    /// <inheritdoc />
    public override string AsText()
    {
        ThrowIfDisposed();
        return $"Controller axis {(int)_axis} at {_axisValue:0.##}";
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

        var raw = MathF.Abs(motion._axisValue);
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
    /// <value>A standardized or raw button index from zero through 127.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric value is outside the supported raw-button range.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public JoyButton ButtonIndex
    {
        get { ThrowIfDisposed(); return _buttonIndex; }
        set
        {
            ThrowIfDisposed();
            if ((int)value < 0 || (int)value >= (int)JoyButton.Max)
                throw new ArgumentOutOfRangeException(nameof(value), value, "A controller button must be between 0 and 127.");
            _buttonIndex = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets or sets whether the controller button is pressed.</summary>
    /// <value><see langword="false"/> for a release; <see langword="true"/> for a press.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool Pressed
    {
        get => IsPressed();
        set { ThrowIfDisposed(); PressedState = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets analog pressure reported for the button.</summary>
    /// <value>A finite value from zero through one. Most hosts report zero and use <see cref="Pressed"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside zero through one, NaN, or infinite.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public float Pressure
    {
        get { ThrowIfDisposed(); return _pressure; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value < 0f || value > 1f)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Button pressure must be finite and between 0 and 1.");
            _pressure = value;
            EmitInputChanged();
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

    /// <inheritdoc />
    public override string AsText()
    {
        ThrowIfDisposed();
        return Enum.GetName(_buttonIndex) ?? $"Controller button {(int)_buttonIndex}";
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
