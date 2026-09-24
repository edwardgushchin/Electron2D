using System.Globalization;

namespace Electron2D;

/// <summary>Provides position, button-mask, and modifier state shared by mouse events.</summary>
public abstract class InputEventMouse : InputEventWithModifiers
{
    private static readonly IReadOnlyList<PropertyDescriptor> MouseProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventMouse, MouseButtonMask>(nameof(ButtonMask), @event => @event.ButtonMask, (@event, value) => @event.ButtonMask = value, _ => MouseButtonMask.None, stored: true),
            new PropertyDescriptor<InputEventMouse, Vector2>(nameof(Position), @event => @event.Position, (@event, value) => @event.Position = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventMouse, Vector2>(nameof(GlobalPosition), @event => @event.GlobalPosition, (@event, value) => @event.GlobalPosition = value, _ => Vector2.Zero, stored: true),
        ]);

    private MouseButtonMask _buttonMask;
    private Vector2 _globalPosition;
    private Vector2 _position;

    /// <summary>Initializes a mouse event with the primary-mouse device identifier.</summary>
    protected InputEventMouse()
    {
        Device = DeviceIdMouse;
    }

    /// <summary>Gets or sets the buttons held while this event occurred.</summary>
    /// <value>The source button bitfield, normally a combination of the five named non-wheel mouse-button bits.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public MouseButtonMask ButtonMask
    {
        get { ThrowIfDisposed(); return _buttonMask; }
        set { ThrowIfDisposed(); _buttonMask = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the pointer position in the current local coordinate space.</summary>
    /// <value>The source local position in pixels, retained without normalization.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Position
    {
        get { ThrowIfDisposed(); return _position; }
        set { ThrowIfDisposed(); _position = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the pointer position in the containing window or viewport coordinate space.</summary>
    /// <value>The source viewport or canvas position in pixels, preserved by <see cref="InputEvent.XformedBy"/>.</value>
    /// <remarks>Raw host events use client coordinates. Viewport localization sets this to the localized Position;
    /// CanvasItem.MakeInputLocal preserves that viewport position. Control GUI delivery sets its temporary copy to
    /// the receiving canvas layer's coordinates.</remarks>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 GlobalPosition
    {
        get { ThrowIfDisposed(); return _globalPosition; }
        set { ThrowIfDisposed(); _globalPosition = value; EmitInputChanged(); }
    }

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventMouse)target;
        typed._buttonMask = _buttonMask;
        typed._globalPosition = _globalPosition;
        typed._position = _position;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(MouseProperties);

    private protected void CopyAccumulatedPositionsFrom(InputEventMouse source)
    {
        _position = source._position;
        _globalPosition = source._globalPosition;
    }
}

/// <summary>Represents a mouse button or wheel press and release.</summary>
public sealed class InputEventMouseButton : InputEventMouse
{
    private static readonly IReadOnlyList<PropertyDescriptor> ButtonProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventMouseButton, MouseButton>(nameof(ButtonIndex), @event => @event.ButtonIndex, (@event, value) => @event.ButtonIndex = value, _ => MouseButton.None, stored: true),
            new PropertyDescriptor<InputEventMouseButton, bool>(nameof(Pressed), @event => @event.Pressed, (@event, value) => @event.Pressed = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventMouseButton, bool>(nameof(Canceled), @event => @event.Canceled, (@event, value) => @event.Canceled = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventMouseButton, bool>(nameof(DoubleClick), @event => @event.DoubleClick, (@event, value) => @event.DoubleClick = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventMouseButton, float>(nameof(Factor), @event => @event.Factor, (@event, value) => @event.Factor = value, _ => 1f, stored: true),
        ]);

    private MouseButton _buttonIndex;
    private bool _doubleClick;
    private float _factor = 1f;

    /// <summary>Gets or sets the button or wheel direction.</summary>
    /// <value><see cref="MouseButton.None"/> by default; caller-supplied numeric values are retained.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public MouseButton ButtonIndex
    {
        get { ThrowIfDisposed(); return _buttonIndex; }
        set { ThrowIfDisposed(); _buttonIndex = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets whether the button is pressed.</summary>
    /// <value><see langword="false"/> for a release; <see langword="true"/> for a press.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool Pressed
    {
        get => IsPressed();
        set { ThrowIfDisposed(); PressedState = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets whether the event was canceled.</summary>
    /// <value>A canceled event is neither pressed nor released.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool Canceled
    {
        get => IsCanceled();
        set { ThrowIfDisposed(); CanceledState = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets whether this press completed a double click.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool DoubleClick
    {
        get { ThrowIfDisposed(); return _doubleClick; }
        set { ThrowIfDisposed(); _doubleClick = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the platform-provided event amount.</summary>
    /// <value>The source amount; high-precision wheel events use it as their scroll amount. Zero and arbitrary values are retained.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public float Factor
    {
        get { ThrowIfDisposed(); return _factor; }
        set { ThrowIfDisposed(); _factor = value; EmitInputChanged(); }
    }

    /// <inheritdoc />
    public override bool IsMatch(InputEvent @event, bool exactMatch = true)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        return @event is InputEventMouseButton button && _buttonIndex == button._buttonIndex &&
            (!exactMatch || GetModifiersMask() == button.GetModifiersMask());
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="transform"/> or <paramref name="localOffset"/> is nonfinite, or transformed coordinates overflow; rejected before copying.</exception>
    public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)
    {
        ThrowIfDisposed();
        ValidateFinite(transform, nameof(transform));
        ValidateFinite(localOffset, nameof(localOffset));
        var position = transform * (Position + localOffset);
        ValidateFinite(position, nameof(transform));
        var result = (InputEventMouseButton)Duplicate();
        result.Position = position;
        return result;
    }

    /// <summary>Gets the localized button or wheel name with modifiers and double-click state.</summary>
    /// <returns>A known button description, or <c>Button #n</c> for an unknown numeric identifier.</returns>
    /// <remarks>Names and the optional double-click label use this event's translation domain.</remarks>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public override string AsText()
    {
        ThrowIfDisposed();
        var modifiers = base.AsText();
        var button = _buttonIndex switch
        {
            MouseButton.Left => Tr("Left Mouse Button"),
            MouseButton.Right => Tr("Right Mouse Button"),
            MouseButton.Middle => Tr("Middle Mouse Button"),
            MouseButton.WheelUp => Tr("Mouse Wheel Up"),
            MouseButton.WheelDown => Tr("Mouse Wheel Down"),
            MouseButton.WheelLeft => Tr("Mouse Wheel Left"),
            MouseButton.WheelRight => Tr("Mouse Wheel Right"),
            MouseButton.XButton1 => Tr("Mouse Thumb Button 1"),
            MouseButton.XButton2 => Tr("Mouse Thumb Button 2"),
            _ => string.Concat(Tr("Button"), " #", ((int)_buttonIndex).ToString(CultureInfo.InvariantCulture)),
        };
        var text = modifiers.Length == 0 ? button : string.Concat(modifiers, "+", button);
        return _doubleClick ? string.Concat(text, " (", Tr("Double Click"), ")") : text;
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventMouseButton();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventMouseButton)target;
        typed._buttonIndex = _buttonIndex;
        typed._doubleClick = _doubleClick;
        typed._factor = _factor;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(ButtonProperties);

    internal override bool TryMatchAction(InputEvent actual, bool exactMatch, float deadzone, out InputActionMatch match)
    {
        match = default;
        if (actual is not InputEventMouseButton button || _buttonIndex != button._buttonIndex ||
            !HasCompatibleModifiers(button, exactMatch))
            return false;

        var pressed = button.IsPressed();
        var strength = pressed ? 1f : 0f;
        match = new InputActionMatch(pressed, strength, strength);
        return true;
    }
}

/// <summary>Represents mouse or stylus motion.</summary>
public sealed class InputEventMouseMotion : InputEventMouse
{
    private static readonly IReadOnlyList<PropertyDescriptor> MotionProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventMouseMotion, bool>(nameof(PenInverted), @event => @event.PenInverted, (@event, value) => @event.PenInverted = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventMouseMotion, float>(nameof(Pressure), @event => @event.Pressure, (@event, value) => @event.Pressure = value, _ => 0f, stored: true),
            new PropertyDescriptor<InputEventMouseMotion, Vector2>(nameof(Relative), @event => @event.Relative, (@event, value) => @event.Relative = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventMouseMotion, Vector2>(nameof(ScreenRelative), @event => @event.ScreenRelative, (@event, value) => @event.ScreenRelative = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventMouseMotion, Vector2>(nameof(Velocity), @event => @event.Velocity, (@event, value) => @event.Velocity = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventMouseMotion, Vector2>(nameof(ScreenVelocity), @event => @event.ScreenVelocity, (@event, value) => @event.ScreenVelocity = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventMouseMotion, Vector2>(nameof(Tilt), @event => @event.Tilt, (@event, value) => @event.Tilt = value, _ => Vector2.Zero, stored: true),
        ]);

    private bool _penInverted;
    private float _pressure;
    private Vector2 _relative;
    private Vector2 _screenRelative;
    private Vector2 _screenVelocity;
    private Vector2 _tilt;
    private Vector2 _velocity;

    /// <summary>Gets or sets whether the eraser end of a stylus generated the event.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool PenInverted
    {
        get { ThrowIfDisposed(); return _penInverted; }
        set { ThrowIfDisposed(); _penInverted = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets stylus pressure.</summary>
    /// <value>The source pressure, normally from zero through one; arbitrary values are retained.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public float Pressure
    {
        get { ThrowIfDisposed(); return _pressure; }
        set { ThrowIfDisposed(); _pressure = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets local movement since the previous event.</summary>
    /// <value>The source content-scaled delta in pixels.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Relative
    {
        get { ThrowIfDisposed(); return _relative; }
        set { ThrowIfDisposed(); _relative = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets unscaled screen-space movement since the previous event.</summary>
    /// <value>The source delta in screen pixels that is not changed by <see cref="InputEvent.XformedBy"/>.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 ScreenRelative
    {
        get { ThrowIfDisposed(); return _screenRelative; }
        set { ThrowIfDisposed(); _screenRelative = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets local pointer velocity.</summary>
    /// <value>The source content-scaled velocity in pixels per second.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Velocity
    {
        get { ThrowIfDisposed(); return _velocity; }
        set { ThrowIfDisposed(); _velocity = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets unscaled screen-space pointer velocity.</summary>
    /// <value>The source velocity in screen pixels per second that is not transformed.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 ScreenVelocity
    {
        get { ThrowIfDisposed(); return _screenVelocity; }
        set { ThrowIfDisposed(); _screenVelocity = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets stylus tilt.</summary>
    /// <value>The source tilt components, normally from minus one through one; arbitrary values are retained.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Tilt
    {
        get { ThrowIfDisposed(); return _tilt; }
        set { ThrowIfDisposed(); _tilt = value; EmitInputChanged(); }
    }

    /// <inheritdoc />
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the complete accumulated state is assigned.</exception>
    public override bool Accumulate(InputEvent withEvent)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(withEvent);
        withEvent.EnsureUsable();
        if (withEvent is not InputEventMouseMotion motion || WindowID != motion.WindowID ||
            CanceledState != motion.CanceledState || PressedState != motion.PressedState ||
            ButtonMask != motion.ButtonMask || ShiftPressed != motion.ShiftPressed ||
            ControlPressed != motion.ControlPressed || AltPressed != motion.AltPressed || MetaPressed != motion.MetaPressed)
            return false;

        var relative = _relative + motion._relative;
        var screenRelative = _screenRelative + motion._screenRelative;
        CopyAccumulatedPositionsFrom(motion);
        _velocity = motion._velocity;
        _screenVelocity = motion._screenVelocity;
        _relative = relative;
        _screenRelative = screenRelative;
        EmitInputChanged();
        return true;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="transform"/> or <paramref name="localOffset"/> is nonfinite, or transformed coordinates overflow; rejected before copying.</exception>
    public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)
    {
        ThrowIfDisposed();
        ValidateFinite(transform, nameof(transform));
        ValidateFinite(localOffset, nameof(localOffset));
        var position = transform * (Position + localOffset);
        ValidateFinite(position, nameof(transform));
        var relative = transform.BasisXform(Relative); var velocity = transform.BasisXform(Velocity);
        ValidateFinite(relative, nameof(transform)); ValidateFinite(velocity, nameof(transform));
        var result = (InputEventMouseMotion)Duplicate();
        result.Position = position;
        result.Relative = relative;
        result.Velocity = velocity;
        return result;
    }

    /// <summary>Gets a localized description of position and velocity.</summary>
    /// <returns>The source-format motion sentence with invariant position and velocity values.</returns>
    /// <remarks>Integral components retain <c>.0</c> and exact decimal digits, fractional components use a six-decimal float real policy, and non-finite components use <c>nan</c>/<c>inf</c>. The source template has two <c>%s</c> placeholders; translated templates require two supported placeholders or fall back to the source sentence.</remarks>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public override string AsText()
    {
        ThrowIfDisposed();
        const string source = "Mouse motion at position (%s) with velocity (%s)";
        return FormatTextTemplate(Tr(source), source, FormatTextVector2(Position), FormatTextVector2(Velocity));
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventMouseMotion();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventMouseMotion)target;
        typed._penInverted = _penInverted;
        typed._pressure = _pressure;
        typed._relative = _relative;
        typed._screenRelative = _screenRelative;
        typed._screenVelocity = _screenVelocity;
        typed._tilt = _tilt;
        typed._velocity = _velocity;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(MotionProperties);

}
