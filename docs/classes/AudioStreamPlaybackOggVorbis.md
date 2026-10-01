# AudioStreamPlaybackOggVorbis

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.AudioStreamPlaybackOggVorbis` · **Namespace:** `Electron2D` · **Source:** [AudioStreamPlaybackOggVorbis.cs](../../src/Scene/Resources/AudioStreamPlaybackOggVorbis.cs).

**Inherits:** [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md). No production subclass.

## Description

Factory-created caller-owned playback borrowing its AudioStreamOggVorbis and captured sequence. It supplies concrete resampling, active/loop/position/seek hooks and consumes inherited nullable LoopingOverride. Packet/granule writes invalidate the capture, including equal writes. Sampling-rate-only changes remain live; stored metadata can be literal but actual mixing requires a positive finite rate. Stop remains available after source disposal; all consuming operations reject disposed source/sequence. Use separate playback instances for concurrent cursors and coordinate access to each cursor. Player-provided instances remain player-owned.

## Example

Requires the indicated resource file when loading; this snippet mixes through the public CPU API.

```csharp
using var stream = AudioStreamOggVorbis.LoadFromFile("res://audio/theme.ogg");
using var playback = (AudioStreamPlaybackOggVorbis)stream.InstantiatePlayback();
playback.Start();
Vector2[] frames = playback.MixAudio(1, 128);
```

## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `protected override System.Int32 OnGetLoopCount()` | Implements the inherited AudioStreamPlaybackResampled contract; see the description. |
| `protected override System.Double OnGetPlaybackPosition()` | Implements the inherited AudioStreamPlaybackResampled contract; see the description. |
| `protected override System.Single OnGetStreamSamplingRate()` | Implements the inherited AudioStreamPlaybackResampled contract; see the description. |
| `protected override System.Boolean OnIsPlaying()` | Implements the inherited AudioStreamPlaybackResampled contract; see the description. |
| `protected override System.Int32 OnMixResampled(System.Span<Electron2D.Vector2> buffer)` | Implements the inherited AudioStreamPlaybackResampled contract; see the description. |
| `protected override System.Void OnSeek(System.Double time)` | Implements the inherited AudioStreamPlaybackResampled contract; see the description. |
| `protected override System.Void OnStart(System.Double fromPosition)` | Implements the inherited AudioStreamPlaybackResampled contract; see the description. |
| `protected override System.Void OnStop()` | Implements the inherited AudioStreamPlaybackResampled contract; see the description. |

## Methods and protected extension points descriptions

### OnGetLoopCount

`protected override System.Int32 OnGetLoopCount()`

Concrete implementation of [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md); lifecycle, effects and failures follow the description above.


### OnGetPlaybackPosition

`protected override System.Double OnGetPlaybackPosition()`

Concrete implementation of [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md); lifecycle, effects and failures follow the description above.


### OnGetStreamSamplingRate

`protected override System.Single OnGetStreamSamplingRate()`

Concrete implementation of [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md); lifecycle, effects and failures follow the description above.


### OnIsPlaying

`protected override System.Boolean OnIsPlaying()`

Concrete implementation of [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md); lifecycle, effects and failures follow the description above.


### OnMixResampled

`protected override System.Int32 OnMixResampled(System.Span<Electron2D.Vector2> buffer)`

Concrete implementation of [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md); lifecycle, effects and failures follow the description above.


### OnSeek

`protected override System.Void OnSeek(System.Double time)`

Concrete implementation of [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md); lifecycle, effects and failures follow the description above.


### OnStart

`protected override System.Void OnStart(System.Double fromPosition)`

Concrete implementation of [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md); lifecycle, effects and failures follow the description above.


### OnStop

`protected override System.Void OnStop()`

Concrete implementation of [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md); lifecycle, effects and failures follow the description above.

## Dependencies and verification

[ADR 0047](../decisions/audio.md#adr-0047), [Resource lifetime](../decisions/resources.md#adr-0014) and [audio component](../components/audio-playback.md) own the boundaries. [AudioCompressedTests](../../tests/Electron2D.Tests/AudioCompressedTests.cs) verifies copied state, independent PCM, seek/loops, lifetime, mutation, corruption, typed parameters, cache replacement and warmed active/idle CPU mixing. Actual FAudio PCM is a separate native test.

[Coverage](../coverage/classes/AudioStreamPlaybackOggVorbis.md) records inherited dependencies and multichannel/platform limits; physical listening and native allocation totals are unverified.
