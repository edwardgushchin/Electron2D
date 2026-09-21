using System.Text;

namespace Electron2D;

/// <summary>Represents a keyboard key press, release, or operating-system repeat.</summary>
/// <remarks>
/// A host event normally supplies logical, physical, label, and Unicode data. An action binding should generally set
/// only one of <see cref="Keycode"/>, <see cref="PhysicalKeycode"/>, or <see cref="KeyLabel"/>.
/// </remarks>
public sealed class InputEventKey : InputEventWithModifiers
{
    private static readonly IReadOnlyList<PropertyDescriptor> KeyProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventKey, bool>(nameof(Pressed), @event => @event.Pressed, (@event, value) => @event.Pressed = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventKey, bool>(nameof(Echo), @event => @event.Echo, (@event, value) => @event.Echo = value, _ => false, stored: true),
            new PropertyDescriptor<InputEventKey, Key>(nameof(Keycode), @event => @event.Keycode, (@event, value) => @event.Keycode = value, _ => Key.None, stored: true),
            new PropertyDescriptor<InputEventKey, Key>(nameof(PhysicalKeycode), @event => @event.PhysicalKeycode, (@event, value) => @event.PhysicalKeycode = value, _ => Key.None, stored: true),
            new PropertyDescriptor<InputEventKey, Key>(nameof(KeyLabel), @event => @event.KeyLabel, (@event, value) => @event.KeyLabel = value, _ => Key.None, stored: true),
            new PropertyDescriptor<InputEventKey, int>(nameof(Unicode), @event => @event.Unicode, (@event, value) => @event.Unicode = value, _ => 0, stored: true),
            new PropertyDescriptor<InputEventKey, KeyLocation>(nameof(Location), @event => @event.Location, (@event, value) => @event.Location = value, _ => KeyLocation.Unspecified, stored: true),
        ]);

    private bool _echo;
    private Key _keyLabel;
    private Key _keycode;
    private KeyLocation _location;
    private Key _physicalKeycode;
    private int _unicode;

    /// <summary>Gets or sets whether the key is pressed.</summary>
    /// <value><see langword="false"/> for a release; <see langword="true"/> for a press.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool Pressed
    {
        get => IsPressed();
        set { ThrowIfDisposed(); PressedState = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets whether this is a repeated press for a key that was already held.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public bool Echo
    {
        get { ThrowIfDisposed(); return _echo; }
        set { ThrowIfDisposed(); _echo = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the layout-aware logical key code.</summary>
    /// <value>A special-key identifier or Unicode-compatible printable key; <see cref="Key.None"/> when absent.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Key Keycode
    {
        get { ThrowIfDisposed(); return _keycode; }
        set { ThrowIfDisposed(); _keycode = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the physical key position expressed against a standard US keyboard layout.</summary>
    /// <value>A location-oriented key code; <see cref="Key.None"/> when absent.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Key PhysicalKeycode
    {
        get { ThrowIfDisposed(); return _physicalKeycode; }
        set { ThrowIfDisposed(); _physicalKeycode = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the localized label printed on the key.</summary>
    /// <value>A key identifier or Unicode-compatible printable character; <see cref="Key.None"/> when absent.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public Key KeyLabel
    {
        get { ThrowIfDisposed(); return _keyLabel; }
        set { ThrowIfDisposed(); _keyLabel = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the Unicode scalar produced by the press.</summary>
    /// <value>Zero when no text scalar is associated with the event.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not zero or a valid Unicode scalar.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public int Unicode
    {
        get { ThrowIfDisposed(); return _unicode; }
        set
        {
            ThrowIfDisposed();
            if (value != 0 && !Rune.IsValid(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be zero or a valid Unicode scalar.");
            _unicode = value;
            EmitInputChanged();
        }
    }

    /// <summary>Gets or sets the side of a key that has left and right variants.</summary>
    /// <value><see cref="KeyLocation.Unspecified"/> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is not defined.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A <see cref="Resource.Changed"/> handler throws after the value is assigned.</exception>
    public KeyLocation Location
    {
        get { ThrowIfDisposed(); return _location; }
        set
        {
            ThrowIfDisposed();
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown key location.");
            _location = value;
            EmitInputChanged();
        }
    }

    /// <inheritdoc />
    public override bool IsEcho()
    {
        ThrowIfDisposed();
        return _echo;
    }

    /// <summary>Gets the logical key code combined with active modifier bits.</summary>
    /// <returns>The numeric union of <see cref="Keycode"/> and <see cref="InputEventWithModifiers.GetModifiersMask"/>.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public Key GetKeycodeWithModifiers() => CombineWithModifiers(Keycode);

    /// <summary>Gets the physical key code combined with active modifier bits.</summary>
    /// <returns>The numeric union of <see cref="PhysicalKeycode"/> and <see cref="InputEventWithModifiers.GetModifiersMask"/>.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public Key GetPhysicalKeycodeWithModifiers() => CombineWithModifiers(PhysicalKeycode);

    /// <summary>Gets the localized key label combined with active modifier bits.</summary>
    /// <returns>The numeric union of <see cref="KeyLabel"/> and <see cref="InputEventWithModifiers.GetModifiersMask"/>.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public Key GetKeyLabelWithModifiers() => CombineWithModifiers(KeyLabel);

    /// <summary>Returns the logical key and modifier description.</summary>
    /// <returns>A portable diagnostic representation.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public string AsTextKeycode() => FormatKey(Keycode);

    /// <summary>Returns the physical key and modifier description.</summary>
    /// <returns>A portable diagnostic representation.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public string AsTextPhysicalKeycode() => FormatKey(PhysicalKeycode);

    /// <summary>Returns the localized key label and modifier description.</summary>
    /// <returns>A portable diagnostic representation.</returns>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public string AsTextKeyLabel() => FormatKey(KeyLabel);

    /// <summary>Returns the key-location description.</summary>
    /// <returns><c>Left</c>, <c>Right</c>, or an empty string for an unspecified location.</returns>
    /// <exception cref="InvalidOperationException">The event contains an invalid key-location value.</exception>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    public string AsTextLocation()
    {
        ThrowIfDisposed();
        return _location switch
        {
            KeyLocation.Unspecified => string.Empty,
            KeyLocation.Left => "Left",
            KeyLocation.Right => "Right",
            _ => throw new InvalidOperationException("The event contains an invalid key location."),
        };
    }

    /// <inheritdoc />
    public override bool IsMatch(InputEvent @event, bool exactMatch = true)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(@event);
        @event.EnsureUsable();

        if (@event is not InputEventKey key || !HasSameKeyIdentity(key))
            return false;

        if (_physicalKeycode != Key.None && _location != KeyLocation.Unspecified && _location != key._location)
            return false;

        return !exactMatch || GetModifiersMask() == key.GetModifiersMask();
    }

    /// <inheritdoc />
    public override string AsText()
    {
        ThrowIfDisposed();
        if (_keycode != Key.None) return AsTextKeycode();
        if (_physicalKeycode != Key.None) return AsTextPhysicalKeycode();
        if (_keyLabel != Key.None) return AsTextKeyLabel();
        return base.AsText();
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventKey();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventKey)target;
        typed._echo = _echo;
        typed._keyLabel = _keyLabel;
        typed._keycode = _keycode;
        typed._location = _location;
        typed._physicalKeycode = _physicalKeycode;
        typed._unicode = _unicode;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(KeyProperties);

    internal override bool TryMatchAction(InputEvent actual, bool exactMatch, float deadzone, out InputActionMatch match)
    {
        match = default;
        if (actual is not InputEventKey key || !HasSameKeyIdentity(key))
            return false;

        if (_physicalKeycode != Key.None && _location != KeyLocation.Unspecified && _location != key._location)
            return false;

        if (!HasCompatibleModifiers(key, exactMatch))
            return false;

        var pressed = key.IsPressed();
        var strength = pressed ? 1f : 0f;
        match = new InputActionMatch(pressed, strength, strength);
        return true;
    }

    private bool HasSameKeyIdentity(InputEventKey key)
    {
        if (_keycode == Key.None && _physicalKeycode == Key.None && _keyLabel != Key.None)
            return _keyLabel == key._keyLabel;
        if (_keycode != Key.None)
            return _keycode == key._keycode;
        return _physicalKeycode != Key.None && _physicalKeycode == key._physicalKeycode;
    }

    private Key CombineWithModifiers(Key key)
    {
        ThrowIfDisposed();
        return (Key)((int)key | (int)GetModifiersMask());
    }

    private string FormatKey(Key key)
    {
        ThrowIfDisposed();
        var keyText = FormatKeyCode(key);
        var modifiers = base.AsText();
        return modifiers.Length == 0 ? keyText : string.Concat(modifiers, "+", keyText);
    }

    private static string FormatKeyCode(Key key)
    {
        var raw = (int)key;
        if (raw == 0)
            return string.Empty;

        if ((raw & (int)Key.Special) == 0 && Rune.IsValid(raw))
            return new Rune(raw).ToString();

        return Enum.GetName(key) ?? $"Key({raw})";
    }
}
