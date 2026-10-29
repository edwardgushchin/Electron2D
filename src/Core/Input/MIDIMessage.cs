namespace Electron2D;

/// <summary>Identifies MIDI channel-message categories and system status bytes.</summary>
public enum MIDIMessage
{
    /// <summary>No message.</summary>
    None = 0,
    /// <summary>Note release.</summary>
    NoteOff = 8,
    /// <summary>Note attack.</summary>
    NoteOn = 9,
    /// <summary>Polyphonic key pressure.</summary>
    Aftertouch = 10,
    /// <summary>Controller change.</summary>
    ControlChange = 11,
    /// <summary>Program selection.</summary>
    ProgramChange = 12,
    /// <summary>Channel pressure.</summary>
    ChannelPressure = 13,
    /// <summary>Fourteen-bit pitch bend.</summary>
    PitchBend = 14,
    /// <summary>System-exclusive status; payload acquisition is outside the input-event contract.</summary>
    SystemExclusive = 240,
    /// <summary>Time-code quarter frame.</summary>
    QuarterFrame = 241,
    /// <summary>Song position.</summary>
    SongPositionPointer = 242,
    /// <summary>Song selection.</summary>
    SongSelect = 243,
    /// <summary>Tuning request.</summary>
    TuneRequest = 246,
    /// <summary>Timing clock.</summary>
    TimingClock = 248,
    /// <summary>Transport start.</summary>
    Start = 250,
    /// <summary>Transport continue.</summary>
    Continue = 251,
    /// <summary>Transport stop.</summary>
    Stop = 252,
    /// <summary>Active sensing.</summary>
    ActiveSensing = 254,
    /// <summary>System reset.</summary>
    SystemReset = 255,
}
