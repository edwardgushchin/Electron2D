# AudioStream

Last updated: 2026-10-02

**Declaration:** `public abstract class Electron2D.AudioStream` · **Source:** [AudioStream.cs](../../src/Scene/Resources/AudioStream.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [Resource](Resource.md).

## Description

Abstract Resource extension contract with independent caller-owned playback instances. The player borrows the stream. Base duration is zero, monophonic policy true and meta policy false. Optional name, BPM, beat/bar, loop and textual tag hooks retain their source contracts for custom/composite streams. Typed parameter descriptors replace the untyped list, but player-to-playback typed descriptor propagation executes; usage tagging and complete composite parameter surfaces retain their own prerequisites. Callbacks used in actual playback run on the native audio thread; mutable custom data must be synchronized and warmed mixing must not allocate. Sample generation/registration is not implemented.

## API summary

| Full signature | Contract |
| --- | --- |
| `protected AudioStream()` | Initializes the independent stream resource. |
| `public event System.Action? ParameterListChanged` | Occurs when the available parameter descriptors change. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `public System.Double GetLength()` | Gets the duration in seconds; zero represents an unknown or empty duration. Duration in seconds. |
| `public Electron2D.PropertyDescriptor[] GetParameterList()` | Returns typed parameter descriptors for a custom stream. A caller-owned array of descriptors; empty for parameterless streams. |
| `public Electron2D.AudioStreamPlayback InstantiatePlayback()` | Creates independent playback state for this stream. A new caller-owned playback that borrows this resource. |
| System.ObjectDisposedException | The stream is disposed. |
| `public virtual System.Boolean IsMetaStream()` | Gets whether the resource selects or combines other streams rather than holding ordinary samples. False for ordinary sample resources. |
| `public System.Boolean IsMonophonic()` | Gets whether the stream permits only one active voice. True by default; ordinary WAV streams are polyphonic. |
| `protected System.Void NotifyParameterListChanged()` | Reports an actual custom parameter-list change. |
| System.ObjectDisposedException | The stream is disposed. |
| `protected virtual System.Double OnGetBPM()` | Supplies optional musical tempo metadata. Beats per minute, zero when absent. |
| `protected virtual System.Int32 OnGetBarBeats()` | Supplies the number of beats per bar. Zero when no musical metadata is supplied. |
| `protected virtual System.Int32 OnGetBeatCount()` | Supplies optional total beat metadata. Zero by default. |
| `protected virtual System.Double OnGetLength()` | Supplies the stream duration. Duration in seconds, zero by default. |
| `protected virtual Electron2D.PropertyDescriptor[] OnGetParameterList()` | Supplies custom typed parameter descriptors. A caller-owned array, empty by default. |
| `protected virtual System.String OnGetStreamName()` | Supplies a descriptive stream name for diagnostics and derived stream containers. An empty name by default. |
| `protected virtual System.Collections.Generic.Dictionary<System.String, System.String> OnGetTags()` | Supplies copied textual stream metadata. A caller-owned map, empty by default. |
| `protected virtual System.Boolean OnHasLoop()` | Supplies optional looping metadata for composite streams. False by default. |
| `protected abstract Electron2D.AudioStreamPlayback OnInstantiatePlayback()` | Creates independent playback state. A new caller-owned playback. |
| `protected virtual System.Boolean OnIsMonophonic()` | Supplies the monophonic policy. True by default. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.

[Own reference coverage](../coverage/classes/AudioStream.md) retains missing and Partial members separately.

[AudioStreamRandomizer](AudioStreamRandomizer.md) is an executable meta-stream consumer. It captures a selected child at InstantiatePlayback and queries all child monophonic policies; standard source factories are called once per actual player Play. Optional metadata and parameters not overridden by a concrete stream retain base behavior.

The procedural [AudioStreamGenerator](AudioStreamGenerator.md)/[AudioStreamGeneratorPlayback](AudioStreamGeneratorPlayback.md) supplies bounded producer queues and continuous underrun silence through this inherited contract; its class pages record exact rate/control/lifetime boundaries.
