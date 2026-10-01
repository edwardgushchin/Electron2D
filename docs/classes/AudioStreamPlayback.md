# AudioStreamPlayback

Last updated: 2026-10-01

**Declaration:** `public abstract class Electron2D.AudioStreamPlayback` · **Source:** [AudioStream.cs](../../src/Scene/Resources/AudioStream.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Caller-owned independent playback. Start/Seek accept finite seconds, including negative requests for the concrete stream to interpret; MixAudio accepts finite nonnegative rate and nonnegative frame count, and allocates a caller-owned result trimmed to the reported mixed count. The engine uses the same OnMix contract with a prepared span. Invalid callback counts throw; native callback failures are captured, silence output and are reported on the owner scene frame, with failed voices stopped. A player-exposed playback is borrowed and must not be disposed or mixed concurrently with its player. Concrete cursors can include decode prefetch. Typed parameter propagation, usage tagging and native sample-playback handles are absent.

## API summary

| Full signature | Contract |
| --- | --- |
| `protected AudioStreamPlayback()` | Initializes the independent playback extension state. |
| `public System.Int32 GetLoopCount()` | Gets the concrete loop counter. The number reported by this playback implementation. |
| `public System.Double GetPlaybackPosition()` | Gets the current stream position in seconds. The concrete playback cursor, which can include resampling prefetch. |
| `public System.Boolean IsPlaying()` | Gets whether playback is currently active. The concrete active state. |
| `public Electron2D.Vector2[] MixAudio(System.Single rateScale, System.Int32 frames)` | Mixes up to the requested number of stereo frames. A caller-owned array containing only the frames reported mixed. |
| rateScale | Finite nonnegative playback-rate multiplier. |
| frames | Nonnegative requested frame count. |
| System.ArgumentOutOfRangeException | The rate/count is invalid or the callback reports an invalid count. |
| System.ObjectDisposedException | The playback is disposed. |
| `protected virtual System.Int32 OnGetLoopCount()` | Supplies the loop counter. Zero by default. |
| `protected abstract System.Double OnGetPlaybackPosition()` | Supplies the playback cursor. Position in seconds. |
| `protected abstract System.Boolean OnIsPlaying()` | Supplies the active state. The concrete active state. |
| `protected abstract System.Int32 OnMix(System.Span<Electron2D.Vector2> buffer, System.Single rateScale)` | Fills the prepared stereo frame span and reports frames before the first silence. Mixed frame count, between zero and buffer.Length. |
| buffer | Prepared caller-owned output storage. |
| rateScale | Finite nonnegative playback-rate multiplier. |
| `protected abstract System.Void OnSeek(System.Double time)` | Moves the concrete cursor. |
| time | Finite time in seconds. |
| `protected abstract System.Void OnStart(System.Double fromPosition)` | Starts the concrete playback. |
| fromPosition | Finite requested time in seconds. |
| `protected abstract System.Void OnStop()` | Stops the concrete playback. |
| `public System.Void Seek(System.Double time = 0)` | Seeks the concrete playback. |
| time | Finite requested time, zero by default. |
| System.ArgumentOutOfRangeException | The time is not finite. |
| System.ObjectDisposedException | The playback or its stream is disposed. |
| `public System.Void Start(System.Double fromPosition = 0)` | Starts playback from a position in seconds. |
| fromPosition | Requested time, zero by default. |
| System.ArgumentOutOfRangeException | The time is not finite. |
| System.ObjectDisposedException | The playback or its stream is disposed. |
| `public System.Void Stop()` | Stops playback. |
| System.ObjectDisposedException | The playback is disposed. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.

[Own reference coverage](../coverage/classes/AudioStreamPlayback.md) retains missing and Partial members separately.
