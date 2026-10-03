# AudioSamplePlayback

Last updated: 2026-10-03

**Declaration:** `public sealed class Electron2D.AudioSamplePlayback` · **Source:** [AudioSamplePlayback.cs](../../src/Scene/Resources/AudioSamplePlayback.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [ElectronObject](ElectronObject.md).

## Description

A typed native sample request borrowing one live sample-capable AudioStream. Construction does not start output. `AudioStreamPlayback.SetSamplePlayback` transfers ownership; its getter and scene-player queries return borrowed state. An associated request cannot be disposed directly. Replacing a standalone association stops/releases native state and disposes the old request; a scene-attached native association cannot be replaced directly. Association/start/prepared controls/disposal require the audio owner and reject audio callback reentry. Gain vectors copy in and out; scalar changes reuse prepared matrix storage.

## Example

```csharp
using var stream = new AudioStreamWAV { Data = new byte[44100] };
using var playback = stream.InstantiatePlayback();
playback.SetSamplePlayback(new AudioSamplePlayback(stream) { Bus = "Master" });
playback.Start(); // Native output, independent of managed MixAudio.
playback.Stop();
```

## API summary

| Full signature | Contract |
| --- | --- |
| `public AudioSamplePlayback(AudioStream stream)` | Borrows a live sample-capable stream. |
| `public AudioStream Stream { get; }` | Borrowed source identity. |
| `public double Offset { get; set; }` | Finite requested seconds, zero initially. Prepared assignment seeks/restarts. |
| `public float PitchScale { get; set; }` | Finite positive ratio, one initially. |
| `public Vector2[] VolumeVector { get; set; }` | Four copied finite nonnegative stereo-pair gains, all unity initially. |
| `public string Bus { get; set; }` | Requested name, Master initially; missing names route to Master. |
| `protected override void ValidateDisposal()` | Rejects direct borrowed disposal and foreign native teardown. |

## Constructor Descriptions

### AudioSamplePlayback

`stream` must be nonnull and live. `CanBeSampled=false` throws NotSupportedException. PCM preparation occurs during registration/start, so invalid encoded data or metadata fails there.

## Property Descriptions

### Stream

Returns the borrowed source. Native processing stops a disposed source, with failure delivered on owner processing/query.

### Offset

Stores requested seconds. Negative requests clamp to zero at playback; finite requests beyond a nonlooped sample finish without output. Looped requests wrap into the configured source interval. Assignment to prepared state restarts that native cursor. AudioStreamPlayback.Start/Seek also update this requested offset.

### PitchScale

Applies independently to the native source ratio; a scene player's pitch/global speed also contribute. Prepared effective ratios outside 1/1024–1024 throw NotSupportedException before publishing the new setting. Invalid nonfinite/nonpositive input throws ArgumentOutOfRangeException. Native sample SRC differs from the streamed cubic resampler.

### VolumeVector

Four pairs represent front, center/low-frequency, rear and side channels. Array length must be four; values must be finite and nonnegative. Wrong length throws ArgumentException, invalid gains throw ArgumentOutOfRangeException. Native matrix overflow throws ArithmeticException and preserves the previous configuration. This explicit array control allocates cold copies.

### Bus

Stores an exact requested name; null rejects. Changing it reroutes the prepared native send. Scene spatial/bus policy can subsequently update that send through its player. Graph reconstruction and bus rename also reroute standalone native samples.

## Method Descriptions

### ValidateDisposal

An associated request is borrowed, so direct Dispose throws InvalidOperationException before logical disposal. Its owning AudioStreamPlayback releases it. Foreign native teardown rejects.

## Verification and limits

[AudioSampleTests](../../tests/Electron2D.Tests/AudioSampleTests.cs) checks ownership, live transport, cursor/pause, routing, bounds, rollback and cleanup; see [native sample playback](../components/audio-playback.md#native-sample-playback) for verification boundaries. No independent unmanaged handle or backend type is public.
