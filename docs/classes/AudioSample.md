# AudioSample

Last updated: 2026-10-04

**Declaration:** `public sealed class Electron2D.AudioSample` · **Source:** [AudioSample.cs](../../src/Scene/Resources/AudioSample.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

Owns immutable copied finite mono/stereo interleaved float PCM. `Stream` is borrowed. Disposing the stream does not dispose this snapshot; native playback still requires a live source. Data inspection copies outside the audio callback. The server registry owns its separate generated snapshot, and active voices retain the snapshot with which they started. Explicit re-registration captures resource edits for later voices. Native preparation supports 1000–200000 Hz; metadata itself accepts any positive rate.

Loop bounds are frame indices, begin inclusive and end exclusive. Disabled ignores bounds; enabled loops require `0 <= begin < end <= frameCount`. PingPong preparation visits each endpoint once per turn; Backward reverses the loop after the forward prefix. This native sample endpoint contract differs from the streamed WAV inclusive batch boundary. Preparation copies reversed segments once per snapshot; native playback pins shared prepared PCM rather than decoding each quantum.

## Example

```csharp
using var stream = new AudioStreamWAV { Data = new byte[44100] };
using var sample = stream.GenerateSample();
float[] pcm = sample.Data; // Independent inspection copy.
AudioServer.RegisterStreamAsSample(stream);
```

## API summary

| Full signature | Contract |
| --- | --- |
| `public AudioSample(AudioStream stream, ReadOnlySpan<float> data, int numChannels, int sampleRate, AudioLoopMode loopMode = AudioLoopMode.Disabled, int loopBegin = 0, int loopEnd = 0)` | Validates and copies complete mono/stereo finite PCM. |
| `public AudioStream Stream { get; }` | Borrowed original resource. |
| `public float[] Data { get; }` | Copied interleaved PCM. |
| `public int NumChannels { get; }` | One or two channels. |
| `public int SampleRate { get; }` | Positive source Hz. |
| `public AudioLoopMode LoopMode { get; }` | Copied traversal policy. |
| `public int LoopBegin { get; }` | Inclusive first loop frame. |
| `public int LoopEnd { get; }` | Exclusive loop end. |
| `protected override void Dispose(bool disposing)` | Releases owned snapshot storage. |

## Constructor Descriptions

### AudioSample

`stream` must be live and nonnull; it is never disposed by this object. `data` must contain complete frames for `numChannels` (one or two), with every sample finite. Empty PCM is permitted when loops are disabled. Invalid dimensions/data throw ArgumentException, invalid rate/enum/bounds throw ArgumentOutOfRangeException, and disposed source throws ObjectDisposedException. The copied PCM has independent lifetime.

## Property Descriptions

### Stream

Returns the borrowed identity supplied to construction. Later source edits do not alter this snapshot.

### Data

Returns a new caller-owned interleaved copy; writes cannot modify prepared playback.

### NumChannels

Returns source channel count. The driver projects mono/stereo to the selected output matrix.

### SampleRate

Returns source Hz, retained during native resampling.

### LoopMode

Returns the shared [AudioLoopMode](AudioLoopMode.md). No musical beat truncation is applied to native samples.

### LoopBegin

Returns the inclusive loop start in source frames.

### LoopEnd

Returns the exclusive loop end in source frames. Enabled zero-length/out-of-range intervals reject.

## Method Descriptions

### Dispose

Releases owned arrays/cache and follows ElectronObject disposal. Properties reject after disposal. Native voices own pins to their registered snapshot until native teardown.

## Verification and limits

[AudioSampleTests](../../tests/Electron2D.Tests/AudioSampleTests.cs) checks immutable snapshots, exact loop preparation, compressed PCM, boundaries and actual native output. [Native sample playback](../components/audio-playback.md#native-sample-playback) records host, allocation and platform limits. [ADR 0047](../decisions/audio.md#adr-0047) owns this transport.
