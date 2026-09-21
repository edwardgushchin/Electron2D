namespace Electron2D;

/// <summary>Represents one touch contact beginning, ending, or being canceled.</summary>
public sealed class InputEventScreenTouch : InputEventFromWindow
{
    private static readonly IReadOnlyList<PropertyDescriptor> TouchProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventScreenTouch, int>(nameof(Index), @event => @event.Index, (@event, value) => @event.Index = value, _ => 0, stored: true),
            new PropertyDescriptor<InputEventScreenTouch, Vector2>(nameof(Position), @event => @event.Position, (@event, value) => @event.Position = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventScreenTouch, bool>(nameof(Pressed), @event => @event.Pressed, (@event, value) => @event.Pressed = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventScreenTouch, bool>(nameof(Canceled), @event => @event.Canceled, (@event, value) => @event.Canceled = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventScreenTouch, bool>(nameof(DoubleTap), @event => @event.DoubleTap, (@event, value) => @event.DoubleTap = value, _ => false, stored: true),
        ]);

    private bool _doubleTap;
    private int _index;
    private Vector2 _position;

    /// <summary>Gets or sets the touch-contact index.</summary>
    /// <value>A non-negative identifier that remains stable for the life of one contact.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public int Index
    {
        get { ThrowIfDisposed(); return _index; }
        set
        {
            ThrowIfDisposed();
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "A touch index cannot be negative.");
            _index = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets or sets the touch position in the current local coordinate space.</summary>
    /// <value>A finite position in pixels.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value contains NaN or infinity.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Position
    {
        get { ThrowIfDisposed(); return _position; }
        set { ThrowIfDisposed(); ValidateFinite(value, nameof(value)); _position = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets whether the contact is pressed.</summary>
    /// <value><see langword="false"/> for an end; <see langword="true"/> for a beginning.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool Pressed
    {
        get => IsPressed();
        set { ThrowIfDisposed(); PressedState = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets whether the platform canceled the contact.</summary>
    /// <value>A canceled contact is neither pressed nor released.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool Canceled
    {
        get => IsCanceled();
        set { ThrowIfDisposed(); CanceledState = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets whether the contact begins a double tap.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool DoubleTap
    {
        get { ThrowIfDisposed(); return _doubleTap; }
        set { ThrowIfDisposed(); _doubleTap = value; EmitInputChanged(); }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="transform"/> or <paramref name="localOffset"/> contains NaN or infinity.</exception>
    public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)
    {
        ThrowIfDisposed();
        ValidateFinite(transform, nameof(transform));
        ValidateFinite(localOffset, nameof(localOffset));
        var result = (InputEventScreenTouch)Duplicate();
        result.Position = transform * (Position + localOffset);
        return result;
    }

    /// <inheritdoc />
    public override string AsText()
    {
        ThrowIfDisposed();
        var state = IsCanceled() ? "canceled" : IsPressed() ? "pressed" : "released";
        return $"Touch {_index} {state} at {_position}";
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventScreenTouch();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventScreenTouch)target;
        typed._doubleTap = _doubleTap;
        typed._index = _index;
        typed._position = _position;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(TouchProperties);
}

/// <summary>Represents movement of one active touch or stylus contact.</summary>
public sealed class InputEventScreenDrag : InputEventFromWindow
{
    private static readonly IReadOnlyList<PropertyDescriptor> DragProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventScreenDrag, int>(nameof(Index), @event => @event.Index, (@event, value) => @event.Index = value, _ => 0, stored: true),
            new PropertyDescriptor<InputEventScreenDrag, bool>(nameof(PenInverted), @event => @event.PenInverted, (@event, value) => @event.PenInverted = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventScreenDrag, Vector2>(nameof(Position), @event => @event.Position, (@event, value) => @event.Position = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventScreenDrag, float>(nameof(Pressure), @event => @event.Pressure, (@event, value) => @event.Pressure = value, _ => 0f, stored: true),
            new PropertyDescriptor<InputEventScreenDrag, Vector2>(nameof(Relative), @event => @event.Relative, (@event, value) => @event.Relative = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventScreenDrag, Vector2>(nameof(ScreenRelative), @event => @event.ScreenRelative, (@event, value) => @event.ScreenRelative = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventScreenDrag, Vector2>(nameof(Velocity), @event => @event.Velocity, (@event, value) => @event.Velocity = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventScreenDrag, Vector2>(nameof(ScreenVelocity), @event => @event.ScreenVelocity, (@event, value) => @event.ScreenVelocity = value, _ => Vector2.Zero, stored: true),
            new PropertyDescriptor<InputEventScreenDrag, Vector2>(nameof(Tilt), @event => @event.Tilt, (@event, value) => @event.Tilt = value, _ => Vector2.Zero, stored: true),
        ]);

    private int _index;
    private bool _penInverted;
    private Vector2 _position;
    private float _pressure;
    private Vector2 _relative;
    private Vector2 _screenRelative;
    private Vector2 _screenVelocity;
    private Vector2 _tilt;
    private Vector2 _velocity;

    /// <summary>Gets or sets the touch-contact index.</summary>
    /// <value>A non-negative identifier matching the corresponding touch event.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public int Index
    {
        get { ThrowIfDisposed(); return _index; }
        set
        {
            ThrowIfDisposed();
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "A touch index cannot be negative.");
            _index = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets or sets whether the eraser end of a stylus generated the event.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool PenInverted
    {
        get { ThrowIfDisposed(); return _penInverted; }
        set { ThrowIfDisposed(); _penInverted = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the drag position in the current local coordinate space.</summary>
    /// <value>A finite position in pixels.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value contains NaN or infinity.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Position
    {
        get { ThrowIfDisposed(); return _position; }
        set { ThrowIfDisposed(); ValidateFinite(value, nameof(value)); _position = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets stylus pressure.</summary>
    /// <value>A finite value from zero through one.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside zero through one, NaN, or infinite.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public float Pressure
    {
        get { ThrowIfDisposed(); return _pressure; }
        set { ThrowIfDisposed(); ValidateUnit(value, nameof(value)); _pressure = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets local drag movement since the previous event.</summary>
    /// <value>A finite, content-scaled delta in pixels.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value contains NaN or infinity.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Relative
    {
        get { ThrowIfDisposed(); return _relative; }
        set { ThrowIfDisposed(); ValidateFinite(value, nameof(value)); _relative = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets unscaled screen-space movement since the previous event.</summary>
    /// <value>A finite delta in screen pixels that is not transformed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value contains NaN or infinity.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 ScreenRelative
    {
        get { ThrowIfDisposed(); return _screenRelative; }
        set { ThrowIfDisposed(); ValidateFinite(value, nameof(value)); _screenRelative = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets local drag velocity.</summary>
    /// <value>A finite content-scaled velocity in pixels per second.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value contains NaN or infinity.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Velocity
    {
        get { ThrowIfDisposed(); return _velocity; }
        set { ThrowIfDisposed(); ValidateFinite(value, nameof(value)); _velocity = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets unscaled screen-space drag velocity.</summary>
    /// <value>A finite velocity in screen pixels per second that is not transformed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value contains NaN or infinity.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 ScreenVelocity
    {
        get { ThrowIfDisposed(); return _screenVelocity; }
        set { ThrowIfDisposed(); ValidateFinite(value, nameof(value)); _screenVelocity = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets stylus tilt.</summary>
    /// <value>Finite X and Y components, each from minus one through one.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is outside the documented range, NaN, or infinite.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Tilt
    {
        get { ThrowIfDisposed(); return _tilt; }
        set
        {
            ThrowIfDisposed();
            ValidateFinite(value, nameof(value));
            if (MathF.Abs(value.X) > 1f || MathF.Abs(value.Y) > 1f)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Stylus tilt components must be between -1 and 1.");
            _tilt = value;
            EmitInputChanged();
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">A summed relative vector contains NaN or infinity.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the complete accumulated state is assigned.</exception>
    public override bool Accumulate(InputEvent withEvent)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(withEvent);
        withEvent.EnsureUsable();
        if (withEvent is not InputEventScreenDrag drag || _index != drag._index)
            return false;

        var relative = _relative + drag._relative;
        var screenRelative = _screenRelative + drag._screenRelative;
        ValidateFinite(relative, nameof(Relative));
        ValidateFinite(screenRelative, nameof(ScreenRelative));
        _position = drag._position;
        _velocity = drag._velocity;
        _screenVelocity = drag._screenVelocity;
        _relative = relative;
        _screenRelative = screenRelative;
        EmitInputChanged();
        return true;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="transform"/> or <paramref name="localOffset"/> contains NaN or infinity.</exception>
    public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)
    {
        ThrowIfDisposed();
        ValidateFinite(transform, nameof(transform));
        ValidateFinite(localOffset, nameof(localOffset));
        var result = (InputEventScreenDrag)Duplicate();
        result.Position = transform * (Position + localOffset);
        result.Relative = transform.BasisXform(Relative);
        result.Velocity = transform.BasisXform(Velocity);
        return result;
    }

    /// <inheritdoc />
    public override string AsText()
    {
        ThrowIfDisposed();
        return $"Touch {_index} dragged at {_position} with velocity {_velocity}";
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventScreenDrag();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventScreenDrag)target;
        typed._index = _index;
        typed._penInverted = _penInverted;
        typed._position = _position;
        typed._pressure = _pressure;
        typed._relative = _relative;
        typed._screenRelative = _screenRelative;
        typed._screenVelocity = _screenVelocity;
        typed._tilt = _tilt;
        typed._velocity = _velocity;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(DragProperties);

    private static void ValidateUnit(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite and between 0 and 1.");
    }
}

/// <summary>Provides local position and modifier state shared by multi-touch gesture events.</summary>
public abstract class InputEventGesture : InputEventWithModifiers
{
    private static readonly IReadOnlyList<PropertyDescriptor> GestureProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventGesture, Vector2>(nameof(Position), @event => @event.Position, (@event, value) => @event.Position = value, _ => Vector2.Zero, stored: true),
        ]);

    private Vector2 _position;

    /// <summary>Initializes a gesture event with the primary touch-device identifier.</summary>
    protected InputEventGesture()
    {
        Device = 0;
    }

    /// <summary>Gets or sets the gesture position in the current local coordinate space.</summary>
    /// <value>A finite position in pixels.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value contains NaN or infinity.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Position
    {
        get { ThrowIfDisposed(); return _position; }
        set { ThrowIfDisposed(); ValidateFinite(value, nameof(value)); _position = value; EmitInputChanged(); }
    }

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        ((InputEventGesture)target)._position = _position;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(GestureProperties);
}

/// <summary>Represents a two-contact magnification gesture.</summary>
public sealed class InputEventMagnifyGesture : InputEventGesture
{
    private static readonly IReadOnlyList<PropertyDescriptor> MagnifyProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventMagnifyGesture, float>(nameof(Factor), @event => @event.Factor, (@event, value) => @event.Factor = value, _ => 1f, stored: true),
        ]);

    private float _factor = 1f;

    /// <summary>Gets or sets the magnification delta.</summary>
    /// <value>A finite positive factor; values above one magnify and values below one reduce.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite or is less than or equal to zero.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public float Factor
    {
        get { ThrowIfDisposed(); return _factor; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(nameof(value), value, "A magnification factor must be finite and positive.");
            _factor = value;
            EmitInputChanged();
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="transform"/> or <paramref name="localOffset"/> contains NaN or infinity.</exception>
    public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)
    {
        ThrowIfDisposed();
        ValidateFinite(transform, nameof(transform));
        ValidateFinite(localOffset, nameof(localOffset));
        var result = (InputEventMagnifyGesture)Duplicate();
        result.Position = transform * (Position + localOffset);
        return result;
    }

    /// <inheritdoc />
    public override string AsText()
    {
        ThrowIfDisposed();
        return $"Magnify gesture at {Position} with factor {_factor:0.##}";
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventMagnifyGesture();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        ((InputEventMagnifyGesture)target)._factor = _factor;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(MagnifyProperties);
}

/// <summary>Represents a two-contact panning gesture.</summary>
public sealed class InputEventPanGesture : InputEventGesture
{
    private static readonly IReadOnlyList<PropertyDescriptor> PanProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventPanGesture, Vector2>(nameof(Delta), @event => @event.Delta, (@event, value) => @event.Delta = value, _ => Vector2.Zero, stored: true),
        ]);

    private Vector2 _delta;

    /// <summary>Gets or sets the local panning amount since the previous gesture event.</summary>
    /// <value>A finite local-space delta.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value contains NaN or infinity.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Vector2 Delta
    {
        get { ThrowIfDisposed(); return _delta; }
        set { ThrowIfDisposed(); ValidateFinite(value, nameof(value)); _delta = value; EmitInputChanged(); }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="transform"/> or <paramref name="localOffset"/> contains NaN or infinity.</exception>
    public override InputEvent XformedBy(Transform transform, Vector2 localOffset = default)
    {
        ThrowIfDisposed();
        ValidateFinite(transform, nameof(transform));
        ValidateFinite(localOffset, nameof(localOffset));
        var result = (InputEventPanGesture)Duplicate();
        result.Position = transform * (Position + localOffset);
        return result;
    }

    /// <inheritdoc />
    public override string AsText()
    {
        ThrowIfDisposed();
        return $"Pan gesture at {Position} with delta {_delta}";
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventPanGesture();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        ((InputEventPanGesture)target)._delta = _delta;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(PanProperties);
}
