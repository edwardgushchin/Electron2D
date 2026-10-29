namespace Electron2D;

/// <summary>Represents a channel or system message from a MIDI input source.</summary>
/// <remarks>Device identifies a source within one open input session. These events do not create action bindings.
/// Native data uses MIDI ranges; manually authored properties retain their assigned values. System-exclusive payloads
/// are outside this event's contract. Event press/release and action matching retain the base event behavior.</remarks>
public sealed class InputEventMIDI : InputEvent
{
    private static readonly IReadOnlyList<PropertyDescriptor> MIDIProperties =
        Array.AsReadOnly<PropertyDescriptor>(
        [
            new PropertyDescriptor<InputEventMIDI, int>(nameof(Channel), e => e.Channel, (e, v) => e.Channel = v, _ => 0, stored: true),
            new PropertyDescriptor<InputEventMIDI, int>(nameof(ControllerNumber), e => e.ControllerNumber, (e, v) => e.ControllerNumber = v, _ => 0, stored: true),
            new PropertyDescriptor<InputEventMIDI, int>(nameof(ControllerValue), e => e.ControllerValue, (e, v) => e.ControllerValue = v, _ => 0, stored: true),
            new PropertyDescriptor<InputEventMIDI, int>(nameof(Instrument), e => e.Instrument, (e, v) => e.Instrument = v, _ => 0, stored: true),
            new PropertyDescriptor<InputEventMIDI, MIDIMessage>(nameof(Message), e => e.Message, (e, v) => e.Message = v, _ => MIDIMessage.None, stored: true),
            new PropertyDescriptor<InputEventMIDI, int>(nameof(Pitch), e => e.Pitch, (e, v) => e.Pitch = v, _ => 0, stored: true),
            new PropertyDescriptor<InputEventMIDI, int>(nameof(Pressure), e => e.Pressure, (e, v) => e.Pressure = v, _ => 0, stored: true),
            new PropertyDescriptor<InputEventMIDI, int>(nameof(Velocity), e => e.Velocity, (e, v) => e.Velocity = v, _ => 0, stored: true),
        ]);
    private int _channel;
    private int _controllerNumber;
    private int _controllerValue;
    private int _instrument;
    private MIDIMessage _message;
    private int _pitch;
    private int _pressure;
    private int _velocity;

    /// <summary>Gets or sets the zero-based channel; native channel messages use zero through fifteen.</summary>
    /// <value>The assigned value; the default is zero.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A resource change handler throws after assignment.</exception>
    public int Channel
    {
        get { ThrowIfDisposed(); return _channel; }
        set { ThrowIfDisposed(); _channel = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the controller number carried by a control-change message.</summary>
    /// <value>The assigned value; the default is zero.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A resource change handler throws after assignment.</exception>
    public int ControllerNumber
    {
        get { ThrowIfDisposed(); return _controllerNumber; }
        set { ThrowIfDisposed(); _controllerNumber = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the controller value carried by a control-change message.</summary>
    /// <value>The assigned value; the default is zero.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A resource change handler throws after assignment.</exception>
    public int ControllerValue
    {
        get { ThrowIfDisposed(); return _controllerValue; }
        set { ThrowIfDisposed(); _controllerValue = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the zero-based program number.</summary>
    /// <value>The assigned value; the default is zero.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A resource change handler throws after assignment.</exception>
    public int Instrument
    {
        get { ThrowIfDisposed(); return _instrument; }
        set { ThrowIfDisposed(); _instrument = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the channel-message category or full system status.</summary>
    /// <value>The assigned value; the default is zero.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A resource change handler throws after assignment.</exception>
    public MIDIMessage Message
    {
        get { ThrowIfDisposed(); return _message; }
        set { ThrowIfDisposed(); _message = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the note number, or unsigned fourteen-bit pitch-bend value with center 8192.</summary>
    /// <value>The assigned value; the default is zero.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A resource change handler throws after assignment.</exception>
    public int Pitch
    {
        get { ThrowIfDisposed(); return _pitch; }
        set { ThrowIfDisposed(); _pitch = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the polyphonic or channel pressure.</summary>
    /// <value>The assigned value; the default is zero.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A resource change handler throws after assignment.</exception>
    public int Pressure
    {
        get { ThrowIfDisposed(); return _pressure; }
        set { ThrowIfDisposed(); _pressure = value; EmitInputChanged(); }
    }

    /// <summary>Gets or sets the note velocity. A note-on with zero velocity retains its original message category.</summary>
    /// <value>The assigned value; the default is zero.</value>
    /// <exception cref="ObjectDisposedException">The event is disposing or disposed.</exception>
    /// <exception cref="Exception">A resource change handler throws after assignment.</exception>
    public int Velocity
    {
        get { ThrowIfDisposed(); return _velocity; }
        set { ThrowIfDisposed(); _velocity = value; EmitInputChanged(); }
    }

    /// <inheritdoc />
    public override string AsText()
    {
        ThrowIfDisposed();
        const string source = "MIDI Input on Channel=%s Message=%s";
        return FormatTextTemplate(Tr(source), source, _channel.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ((int)_message).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    protected override InputEvent CreateEventInstance() => new InputEventMIDI();

    /// <inheritdoc />
    protected override void CopyEventStateTo(InputEvent target)
    {
        base.CopyEventStateTo(target);
        var typed = (InputEventMIDI)target;
        typed._channel = _channel;
        typed._controllerNumber = _controllerNumber;
        typed._controllerValue = _controllerValue;
        typed._instrument = _instrument;
        typed._message = _message;
        typed._pitch = _pitch;
        typed._pressure = _pressure;
        typed._velocity = _velocity;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(MIDIProperties);
}
