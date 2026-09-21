namespace Electron2D;

/// <summary>Represents a named input action being pressed or released.</summary>
/// <remarks>
/// This event is useful for deterministic simulation and remapping. Parsing it updates action state directly and does
/// not require a hardware binding, but the action itself must already exist in <see cref="InputMap"/>.
/// </remarks>
public sealed class InputEventAction : InputEvent
{
    private static readonly IReadOnlyList<PropertyDescriptor> ActionProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventAction, string>(nameof(Action), @event => @event.Action, (@event, value) => @event.Action = value, _ => string.Empty, stored: true),
            new PropertyDescriptor<InputEventAction, int>(nameof(EventIndex), @event => @event.EventIndex, (@event, value) => @event.EventIndex = value, _ => -1, stored: true),
            new PropertyDescriptor<InputEventAction, bool>(nameof(Pressed), @event => @event.Pressed, (@event, value) => @event.Pressed = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventAction, float>(nameof(Strength), @event => @event.Strength, (@event, value) => @event.Strength = value, _ => 1f, stored: true),
        ]);

    private string _action = string.Empty;
    private int _eventIndex = -1;
    private float _strength = 1f;

    /// <summary>Gets or sets the action name.</summary>
    /// <value>A case-sensitive name; the default is empty until configured.</value>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public string Action
    {
        get { ThrowIfDisposed(); return _action; }
        set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _action = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the corresponding binding index.</summary>
    /// <value>Minus one for an independent synthetic source, or zero through 31 for a mapped binding.</value>
    /// <remarks>An unindexed event has no available source slot when its action already contains 32 bindings and is then rejected by <see cref="Input.ParseInputEvent"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is below minus one or above 31.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public int EventIndex
    {
        get { ThrowIfDisposed(); return _eventIndex; }
        set
        {
            ThrowIfDisposed();
            if (value < -1 || value >= Input.MaxEventsPerAction)
                throw new ArgumentOutOfRangeException(nameof(value), value, "An event index must be -1 or between 0 and 31.");
            _eventIndex = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets or sets whether the action is pressed.</summary>
    /// <value><see langword="false"/> for a release; <see langword="true"/> for a press.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool Pressed
    {
        get => IsPressed();
        set { ThrowIfDisposed(); PressedState = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the analog action strength.</summary>
    /// <value>A value clamped to zero through one. A released event contributes zero regardless of this property.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is NaN or infinite.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the clamped value is assigned.</exception>
    public float Strength
    {
        get { ThrowIfDisposed(); return _strength; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Action strength must be finite.");
            _strength = Mathf.Clamp(value, 0f, 1f);
            EmitInputChanged();
        }
    }

    /// <summary>Gets whether this event names an action.</summary>
    /// <param name="action">The nonblank, case-sensitive action name to compare.</param>
    /// <param name="exactMatch">Ignored because a direct action has no modifier or direction ambiguity.</param>
    /// <returns><see langword="true"/> when <paramref name="action"/> equals <see cref="Action"/>.</returns>
    /// <remarks>The name does not need to be registered in <see cref="InputMap"/> for this comparison.</remarks>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public override bool IsAction(string action, bool exactMatch = false)
    {
        ThrowIfDisposed();
        ValidateActionName(action, nameof(action));
        return string.Equals(_action, action, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool IsMatch(InputEvent @event, bool exactMatch = true)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();
        return _action.Length != 0 && @event.IsAction(_action, exactMatch);
    }

    /// <inheritdoc />
    public override string AsText()
    {
        ThrowIfDisposed();
        if (_action.Length == 0)
            return string.Empty;

        return InputMap.Instance.TryGetFirstEventText(_action, out var text) ? text : _action;
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventAction();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventAction)target;
        typed._action = _action;
        typed._eventIndex = _eventIndex;
        typed._strength = _strength;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(ActionProperties);

    internal override bool TryMatchAction(InputEvent actual, bool exactMatch, float deadzone, out InputActionMatch match)
    {
        match = default;
        if (actual is not InputEventAction action || !string.Equals(_action, action._action, StringComparison.Ordinal))
            return false;

        var pressed = action.IsPressed();
        var strength = pressed ? action._strength : 0f;
        match = new InputActionMatch(pressed, strength, strength);
        return true;
    }
}
