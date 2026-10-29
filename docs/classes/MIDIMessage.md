# MIDIMessage

Last updated: 2026-10-29

Source: [MIDIMessage.cs](../../src/Core/Input/MIDIMessage.cs). Declaration: `public enum MIDIMessage`. Component: [MIDI input](../components/midi-input.md).

Typed MIDI 1 message identity. Voice messages use the high status nibble; system messages retain the full status byte.

| Members | Values |
| --- | --- |
| None | 0 |
| NoteOff, NoteOn, Aftertouch, ControlChange, ProgramChange, ChannelPressure, PitchBend | 8, 9, 10, 11, 12, 13, 14 |
| SystemExclusive, QuarterFrame, SongPositionPointer, SongSelect | 240, 241, 242, 243 |
| TuneRequest, TimingClock | 246, 248 |
| Start, Continue, Stop | 250, 251, 252 |
| ActiveSensing, SystemReset | 254, 255 |

SystemExclusive supplies the status identity for authored values; the current native event does not expose variable payload data. Event data projection, storage and native verification limits are described in the owning component.
