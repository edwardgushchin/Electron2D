using System.Globalization;
using System.Text;

namespace Electron2D;

/// <summary>Provides the abstract base contract for all engine input events.</summary>
/// <remarks>
/// Events are mutable resources so action bindings can be configured in memory. A platform host creates concrete
/// events and passes them to <see cref="Input.ParseInputEvent"/>; this class has no dependency on a native backend.
/// </remarks>
public abstract class InputEvent : Resource
{
    private static readonly IReadOnlyList<PropertyDescriptor> InputEventProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEvent, int>(
                nameof(Device),
                @event => @event.Device,
                (@event, value) => @event.Device = value,
                @event => @event switch
                {
                    InputEventMouse => DeviceIdMouse,
                    InputEventGesture => 0,
                    InputEventWithModifiers => DeviceIdKeyboard,
                    _ => 0,
                },
                stored: true),
        ]);

    /// <summary>Identifies input synthesized from another pointing-device family.</summary>
    public const int DeviceIdEmulation = -1;

    internal const int DeviceIdInternal = -2;

    /// <summary>Identifies the primary keyboard.</summary>
    public const int DeviceIdKeyboard = 16;

    /// <summary>Identifies the primary mouse.</summary>
    public const int DeviceIdMouse = 32;

    private int _device;

    internal event Action<InputEvent>? BindingChanged;
    internal event Action<InputEvent>? BindingDisposed;

    /// <summary>Gets or sets the source device identifier.</summary>
    /// <value>A host-defined identifier. Negative values are reserved for synthesized or internal events.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public int Device
    {
        get
        {
            ThrowIfDisposed();
            return _device;
        }
        set
        {
            ThrowIfDisposed();
            _device = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets whether this event matches a registered action.</summary>
    /// <param name="action">The nonblank, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether modifiers and analog direction must match exactly.</param>
    /// <returns><see langword="true"/> when the process-wide input map contains a matching binding.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public virtual bool IsAction(string action, bool exactMatch = false)
    {
        ThrowIfDisposed();
        return InputMap.Instance.EventIsAction(this, action, exactMatch);
    }

    /// <summary>Gets whether this event presses a registered action.</summary>
    /// <param name="action">The nonblank, case-sensitive action name.</param>
    /// <param name="allowEcho">Whether a repeated keyboard event may count as a press.</param>
    /// <param name="exactMatch">Whether modifiers and analog direction must match exactly.</param>
    /// <returns><see langword="true"/> when the event matches, is pressed, and satisfies the echo policy.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public bool IsActionPressed(string action, bool allowEcho = false, bool exactMatch = false)
    {
        ThrowIfDisposed();
        return InputMap.Instance.TryGetActionStatus(this, action, exactMatch, out var status) &&
            status.Pressed && (allowEcho || !IsEcho());
    }

    /// <summary>Gets whether this event releases a registered action.</summary>
    /// <param name="action">The nonblank, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether modifiers and analog direction must match exactly.</param>
    /// <returns><see langword="true"/> when the event matches and its effective pressed state is false, including cancellation.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public bool IsActionReleased(string action, bool exactMatch = false)
    {
        ThrowIfDisposed();
        return InputMap.Instance.TryGetActionStatus(this, action, exactMatch, out var status) &&
            !status.Pressed;
    }

    /// <summary>Gets the deadzone-adjusted strength contributed by this event to an action.</summary>
    /// <param name="action">The nonblank, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether modifiers and analog direction must match exactly.</param>
    /// <returns>A value from zero through one, or zero when the event does not match.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public float GetActionStrength(string action, bool exactMatch = false)
    {
        ThrowIfDisposed();
        return InputMap.Instance.TryGetActionStatus(this, action, exactMatch, out var status) ? status.Strength : 0f;
    }

    /// <summary>Gets the strength contributed by this event before action deadzone remapping.</summary>
    /// <param name="action">The nonblank, case-sensitive action name.</param>
    /// <param name="exactMatch">Whether modifiers and analog direction must match exactly.</param>
    /// <returns>A value from zero through one, or zero when the event does not match.</returns>
    /// <exception cref="ArgumentException"><paramref name="action"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The action is not registered.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    internal float GetActionRawStrength(string action, bool exactMatch = false)
    {
        ThrowIfDisposed();
        return InputMap.Instance.TryGetActionStatus(this, action, exactMatch, out var status) ? status.RawStrength : 0f;
    }

    /// <summary>Gets whether the event was canceled by its source.</summary>
    /// <returns><see langword="true"/> for a canceled event; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public bool IsCanceled()
    {
        ThrowIfDisposed();
        return CanceledState;
    }

    /// <summary>Gets whether the event represents a non-canceled press.</summary>
    /// <returns><see langword="true"/> only when the event is pressed and not canceled.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public bool IsPressed()
    {
        ThrowIfDisposed();
        return PressedState && !CanceledState;
    }

    /// <summary>Gets whether the event represents a non-canceled release.</summary>
    /// <returns><see langword="true"/> only when the event is not pressed and not canceled.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public bool IsReleased()
    {
        ThrowIfDisposed();
        return !PressedState && !CanceledState;
    }

    /// <summary>Gets whether this is an operating-system key-repeat event.</summary>
    /// <returns><see langword="false"/> except for a repeating <see cref="InputEventKey"/>.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public virtual bool IsEcho()
    {
        ThrowIfDisposed();
        return false;
    }

    /// <summary>Gets whether this event type may be bound to an input action.</summary>
    /// <returns><see langword="true"/> for key, mouse-button, gamepad-button, gamepad-motion, and action events.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public bool IsActionType()
    {
        ThrowIfDisposed();
        return this is InputEventAction or InputEventKey or InputEventMouseButton or
            InputEventJoypadButton or InputEventJoypadMotion;
    }

    /// <summary>Tests whether this event matches another event under its event-type comparison rules.</summary>
    /// <param name="event">The event to compare.</param>
    /// <param name="exactMatch">Whether modifiers and analog direction must match exactly.</param>
    /// <returns><see langword="true"/> when the event-type comparison succeeds.</returns>
    /// <remarks>An action event can match a physical event through its named action. Exact action-map binding lookup uses action-binding matching instead of this comparison.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Either event is disposing or disposed.</exception>
    public virtual bool IsMatch(InputEvent @event, bool exactMatch = true)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(@event);
        @event.ThrowIfDisposed();
        return false;
    }

    /// <summary>Attempts to merge a newer compatible motion event into this event.</summary>
    /// <param name="withEvent">The newer event.</param>
    /// <returns><see langword="true"/> when this event was updated; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="withEvent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Either event is disposing or disposed.</exception>
    public virtual bool Accumulate(InputEvent withEvent)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(withEvent);
        withEvent.ThrowIfDisposed();
        return false;
    }

    /// <summary>Returns this event transformed into another local coordinate space.</summary>
    /// <param name="transform">The affine transform applied to local positions and local motion vectors.</param>
    /// <param name="localOffset">An offset added before local positions are transformed.</param>
    /// <returns>A transformed copy for positional events; this same event for non-positional events.</returns>
    /// <remarks>Global and screen-space coordinates are not transformed.</remarks>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public virtual InputEvent XformedBy(Transform transform, Vector2 localOffset = default)
    {
        ThrowIfDisposed();
        return this;
    }

    /// <summary>Returns a concise, human-readable representation of the event.</summary>
    /// <returns>A non-null description suitable for bindings and diagnostics.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public abstract string AsText();

    /// <summary>Gets or sets the raw press state used by concrete button-like events.</summary>
    protected bool PressedState { get; set; }

    /// <summary>Gets or sets the raw cancellation state used by concrete cancelable events.</summary>
    protected bool CanceledState { get; set; }

    /// <summary>Creates a default instance of the exact concrete event type.</summary>
    /// <returns>A new event instance.</returns>
    protected abstract InputEvent CreateEventInstance();

    /// <summary>Copies this event's concrete state to another exact-type event.</summary>
    /// <param name="target">The destination event.</param>
    protected virtual void CopyEventStateTo(InputEvent target)
    {
        target._device = _device;
        target.PressedState = PressedState;
        target.CanceledState = CanceledState;
    }

    internal virtual bool TryMatchAction(InputEvent actual, bool exactMatch, float deadzone, out InputActionMatch match)
    {
        match = default;
        return false;
    }

    internal static string FormatTextVector2(Vector2 value) =>
        string.Concat("(", FormatTextReal(value.X, 6), ", ", FormatTextReal(value.Y, 6), ")");

    internal static string FormatTextFloatAsDouble(float value) => FormatTextReal(value, 14);

    internal static string FormatTextTemplate(string translated, string source, params string[] values) =>
        TryFormatTextTemplate(translated, values) ?? TryFormatTextTemplate(source, values) ??
        throw new FormatException("The input text source template has invalid placeholders.");

    private static string? TryFormatTextTemplate(string template, string[] values)
    {
        var result = new StringBuilder(template.Length);
        var index = 0;
        for (var position = 0; position < template.Length; position++)
        {
            var remaining = template.AsSpan(position);
            var width = remaining.StartsWith("%.2f", StringComparison.Ordinal) ? 4 :
                remaining.StartsWith("%s", StringComparison.Ordinal) ||
                remaining.StartsWith("%d", StringComparison.Ordinal) ? 2 : 0;
            if (width == 0)
            {
                result.Append(template[position]);
                continue;
            }
            if (index == values.Length) return null;
            result.Append(values[index++]);
            position += width - 1;
        }
        return index == values.Length ? result.ToString() : null;
    }

    private static string FormatTextReal(double value, int precision)
    {
        if (double.IsNaN(value)) return "nan";
        if (double.IsPositiveInfinity(value)) return "inf";
        if (double.IsNegativeInfinity(value)) return "-inf";
        if (value == 0d) return "0.0";
        if (Math.Abs(value) < long.MaxValue && value == Math.Truncate(value))
            return string.Concat(((long)value).ToString(CultureInfo.InvariantCulture), ".0");

        var decimals = precision;
        var absolute = Math.Abs(value);
        // Vector components use float precision; promoted factors use double precision.
        if (absolute > 10d)
            decimals -= precision == 6 ? (int)MathF.Floor(MathF.Log10((float)absolute)) :
                (int)Math.Floor(Math.Log10(absolute));
        if (decimals < 0) decimals = 6;
        var text = value.ToString($"F{decimals}", CultureInfo.InvariantCulture);
        if (!text.Contains('.')) return text;
        text = text.TrimEnd('0');
        return text.EndsWith('.') ? string.Concat(text, "0") : text;
    }

    internal void EnsureUsable() => ThrowIfDisposed();

    internal static void ValidateActionName(string action, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(action, parameterName);
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("An input action name cannot be empty or whitespace.", parameterName);
    }

    /// <summary>Rejects non-finite two-dimensional event data.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The public argument or property-setter value name.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> contains NaN or infinity.</exception>
    protected static void ValidateFinite(Vector2 value, string parameterName)
    {
        if (!value.IsFinite())
            throw new ArgumentOutOfRangeException(parameterName, value, "Input coordinates must be finite.");
    }

    /// <summary>Rejects a non-finite event-coordinate transform.</summary>
    /// <param name="value">The transform to validate.</param>
    /// <param name="parameterName">The public parameter name.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> contains NaN or infinity.</exception>
    protected static void ValidateFinite(Transform value, string parameterName)
    {
        if (!value.IsFinite())
            throw new ArgumentOutOfRangeException(parameterName, value, "Input coordinate transforms must be finite.");
    }

    /// <summary>Invalidates registered action state, optionally reports property-list changes, and emits one content-change event.</summary>
    /// <param name="propertyListChanged">Whether the available property metadata also changed.</param>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A public property-list or content-change handler throws after internal action state is invalidated.</exception>
    protected void EmitInputChanged(bool propertyListChanged = false)
    {
        ThrowIfDisposed();
        BindingChanged?.Invoke(this);
        if (propertyListChanged)
            NotifyPropertyListChanged();
        EmitChanged();
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(InputEventProperties);

    /// <inheritdoc />
    protected sealed override Resource CreateDuplicateInstance() => CreateEventInstance();

    /// <inheritdoc />
    protected sealed override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var inputEvent = (InputEvent)target;
        CopyEventStateTo(inputEvent);
        inputEvent.BindingChanged?.Invoke(inputEvent);
    }

    /// <inheritdoc />
    /// <remarks>Removes this event from every action map entry before inherited resource cleanup and public disposal notification.</remarks>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                BindingDisposed?.Invoke(this);
            }
            finally
            {
                BindingChanged = null;
                BindingDisposed = null;
                base.Dispose(disposing);
            }

            return;
        }

        base.Dispose(disposing);
    }

    /// <summary>Returns <see cref="AsText"/>.</summary>
    /// <returns>The current human-readable event description.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public override string ToString() => AsText();
}

internal readonly record struct InputActionMatch(bool Pressed, float Strength, float RawStrength);

/// <summary>Provides the abstract base for input events received from a window.</summary>
/// <remarks>The active native host assigns <see cref="DisplayServer.MainWindowId"/> to events from its primary window.</remarks>
public abstract class InputEventFromWindow : InputEvent
{
    private static readonly IReadOnlyList<PropertyDescriptor> WindowProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventFromWindow, long>(nameof(WindowID), @event => @event.WindowID, (@event, value) => @event.WindowID = value, _ => 0L, stored: true),
        ]);

    private long _windowId;

    /// <summary>Gets or sets the receiving window identifier.</summary>
    /// <value>A signed 64-bit identifier; zero denotes the primary or unspecified window.</value>
    /// <remarks>Assignments are stored without narrowing or window lookup. Copies retain the same identifier.</remarks>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public long WindowID
    {
        get
        {
            ThrowIfDisposed();
            return _windowId;
        }
        set
        {
            ThrowIfDisposed();
            _windowId = value;
            EmitInputChanged();
        }
    }

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        ((InputEventFromWindow)target)._windowId = _windowId;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(WindowProperties);
}

/// <summary>Provides modifier-key state for keyboard, mouse, and gesture events.</summary>
/// <remarks>Native key events omit the key's own modifier bit while retaining other held modifiers. The inherited device descriptor defaults to <see cref="InputEvent.DeviceIdKeyboard"/> except where a concrete event family overrides that value.</remarks>
public abstract class InputEventWithModifiers : InputEventFromWindow
{
    private static readonly IReadOnlyList<PropertyDescriptor> ModifierProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventWithModifiers, bool>(nameof(AltPressed), @event => @event.AltPressed, (@event, value) => @event.AltPressed = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventWithModifiers, bool>(nameof(ShiftPressed), @event => @event.ShiftPressed, (@event, value) => @event.ShiftPressed = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventWithModifiers, bool>(nameof(ControlPressed), @event => @event.ControlPressed, (@event, value) => @event.ControlPressed = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventWithModifiers, bool>(nameof(MetaPressed), @event => @event.MetaPressed, (@event, value) => @event.MetaPressed = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventWithModifiers, bool>(nameof(CommandOrControlAutoremap), @event => @event.CommandOrControlAutoremap, (@event, value) => @event.CommandOrControlAutoremap = value, _ => false, stored: true),
        ]);

    private bool _altPressed;
    private bool _commandOrControlAutoremap;
    private bool _controlPressed;
    private bool _metaPressed;
    private bool _shiftPressed;

    /// <summary>Initializes a modifier-bearing event with the primary-keyboard device identifier.</summary>
    protected InputEventWithModifiers()
    {
        Device = DeviceIdKeyboard;
    }

    /// <summary>Gets or sets whether Alt or Option is pressed.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool AltPressed
    {
        get { ThrowIfDisposed(); return _altPressed; }
        set { ThrowIfDisposed(); _altPressed = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets whether Shift is pressed.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool ShiftPressed
    {
        get { ThrowIfDisposed(); return _shiftPressed; }
        set { ThrowIfDisposed(); _shiftPressed = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets whether Control is pressed.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="InvalidOperationException"><see cref="CommandOrControlAutoremap"/> is enabled.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool ControlPressed
    {
        get { ThrowIfDisposed(); return _controlPressed; }
        set
        {
            ThrowIfDisposed();
            if (_commandOrControlAutoremap)
                throw new InvalidOperationException("Control cannot be assigned while command-or-control remapping is enabled.");
            _controlPressed = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets or sets whether Meta, Command, Windows, or Super is pressed.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="InvalidOperationException"><see cref="CommandOrControlAutoremap"/> is enabled.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool MetaPressed
    {
        get { ThrowIfDisposed(); return _metaPressed; }
        set
        {
            ThrowIfDisposed();
            if (_commandOrControlAutoremap)
                throw new InvalidOperationException("Meta cannot be assigned while command-or-control remapping is enabled.");
            _metaPressed = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets or sets whether the portable command-or-control modifier is enabled.</summary>
    /// <value>
    /// When enabled, Meta is selected on macOS and Control on every other target. Disabling clears both concrete bits.
    /// </value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list or <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool CommandOrControlAutoremap
    {
        get { ThrowIfDisposed(); return _commandOrControlAutoremap; }
        set
        {
            ThrowIfDisposed();
            if (_commandOrControlAutoremap == value)
                return;

            _commandOrControlAutoremap = value;
            _controlPressed = value && !OperatingSystem.IsMacOS();
            _metaPressed = value && OperatingSystem.IsMacOS();
            EmitInputChanged(propertyListChanged: true);
        }
    }

    /// <summary>Gets the active modifier bits.</summary>
    /// <returns>The combination of Control, Shift, Alt, and Meta currently stored by the event.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public KeyModifierMask GetModifiersMask()
    {
        ThrowIfDisposed();
        var result = (KeyModifierMask)0;
        if (_controlPressed) result |= KeyModifierMask.Control;
        if (_shiftPressed) result |= KeyModifierMask.Shift;
        if (_altPressed) result |= KeyModifierMask.Alt;
        if (_metaPressed) result |= KeyModifierMask.Meta;
        return result;
    }

    /// <summary>Gets the platform-specific state of the command-or-control modifier.</summary>
    /// <returns>Meta on macOS; Control on every other target.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public bool IsCommandOrControlPressed()
    {
        ThrowIfDisposed();
        return OperatingSystem.IsMacOS() ? _metaPressed : _controlPressed;
    }

    /// <summary>Copies modifier state from another event.</summary>
    /// <param name="source">The source modifier event.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Either event is disposing or disposed.</exception>
    /// <exception cref="Exception">A property-list or <see cref="Resource.Changed"/> handler throws after all modifier values are assigned.</exception>
    public void SetModifiersFromEvent(InputEventWithModifiers source)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(source);
        source.ThrowIfDisposed();
        var propertyListChanged = _commandOrControlAutoremap != source._commandOrControlAutoremap;
        _altPressed = source._altPressed;
        _shiftPressed = source._shiftPressed;
        _controlPressed = source._controlPressed;
        _metaPressed = source._metaPressed;
        _commandOrControlAutoremap = source._commandOrControlAutoremap;
        EmitInputChanged(propertyListChanged);
    }

    /// <summary>Returns the active modifier names in keyboard display order.</summary>
    /// <returns>The joined platform-specific modifier names, or an empty string when none are active.</returns>
    /// <remarks>Alt is Option and Meta is Command on macOS; Meta is Windows on Windows.</remarks>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public override string AsText()
    {
        ThrowIfDisposed();
        var text = string.Empty;
        AppendModifier(ref text, _controlPressed, "Ctrl");
        AppendModifier(ref text, _altPressed, OperatingSystem.IsMacOS() ? "Option" : "Alt");
        AppendModifier(ref text, _shiftPressed, "Shift");
        AppendModifier(ref text, _metaPressed,
            OperatingSystem.IsMacOS() ? "Command" : OperatingSystem.IsWindows() ? "Windows" : "Meta");
        return text;
    }

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventWithModifiers)target;
        typed._altPressed = _altPressed;
        typed._shiftPressed = _shiftPressed;
        typed._controlPressed = _controlPressed;
        typed._metaPressed = _metaPressed;
        typed._commandOrControlAutoremap = _commandOrControlAutoremap;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(ModifierProperties);

    internal bool HasCompatibleModifiers(InputEventWithModifiers actual, bool exactMatch)
    {
        var binding = GetModifiersMask();
        var candidate = actual.GetModifiersMask();
        return (!actual.IsPressed() || (binding & candidate) == binding) && (!exactMatch || binding == candidate);
    }

    private static void AppendModifier(ref string text, bool enabled, string name)
    {
        if (!enabled)
            return;

        text = text.Length == 0 ? name : string.Concat(text, "+", name);
    }
}
