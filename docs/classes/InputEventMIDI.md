# InputEventMIDI

Last updated: 2026-10-29

Source: [InputEventMIDI.cs](../../src/Core/Input/InputEventMIDI.cs). Declaration: `public sealed class InputEventMIDI : InputEvent`. Component: [MIDI input](../components/midi-input.md).

Represents one borrowed native MIDI 1 message or a manually authored resource value. Device is a stable source index within one open OS MIDI session. Duplicate an event to retain it beyond its synchronous callback.

| Property | Contract |
| --- | --- |
| `int Channel` | Native voice channel 0–15; system channel zero. |
| `MIDIMessage Message` | Voice category or full system status. |
| `int Pitch` | Note number, or unsigned pitch bend 0–16383 centered on 8192. |
| `int Velocity` | Note attack/release velocity; zero-velocity NoteOn keeps its status. |
| `int ControllerNumber`, `int ControllerValue` | Control-change number/value. |
| `int Instrument` | Zero-based program selection. |
| `int Pressure` | Key/channel pressure. |

The constructor sets all concrete values to zero. Setters retain authored integer/enum values, emit input/resource change and reject disposed access. All properties have stored descriptors and default-revert support. `AsText()` formats the localized `MIDI Input on Channel=%s Message=%s` template with invariant decimal values. The protected exact-instance factory, state-copy override and descriptor override preserve resource duplication/storage. Inherited Device is copied too.

IsPressed is false and IsReleased is true under unchanged base event semantics, including for a native note-on. IsActionType and IsMatch are false; InputMap rejects this event as a binding. MIDI applications interpret Message/Velocity themselves. Native callback, source lifetime, failure behavior and verification limits are owned by the component page.

```csharp
protected override void OnInput(InputEvent value)
{
    if (value is InputEventMIDI midi && midi.Message == MIDIMessage.NoteOn)
        Console.WriteLine($"{midi.Device}:{midi.Channel} note {midi.Pitch}, velocity {midi.Velocity}");
}
```
