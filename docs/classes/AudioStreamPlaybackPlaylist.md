# AudioStreamPlaybackPlaylist

Last updated: 2026-10-03

**Declaration:** `public sealed class Electron2D.AudioStreamPlaybackPlaylist` · **Source:** [AudioStreamPlaybackPlaylist.cs](../../src/Scene/Resources/AudioStreamPlaybackPlaylist.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioStreamPlayback](AudioStreamPlayback.md). Created by AudioStreamPlaylist; no public constructor or additional public own methods.

## Description

Owns one playback per active resource slot, a prepared 64-slot order, current cycle cursor/loop count and one outgoing fade. It borrows the source/resources. Native/recording affinity follows all owned children, including stopped states. Control, mix, structural replacement and disposal serialize on the audio gate; callback/query/mix reentry rejects. Child factories, queue preparation, resource editing and cleanup are cold. Scene and composite users borrow this handle and cannot dispose it directly.

Start clamps negative seconds to zero and resets cursor/loops. At or beyond total duration, nonlooping Start stays stopped; looping Start modulo-wraps finite seconds without narrowing to float. Empty/all-zero lists remain stopped. The selected order determines offset-to-track resolution; Start forwards the local track offset. Seek restarts under the same policy, including stopped playback. Zero-frame demand advances no time and creates no transition.

Scheduling uses the actual output mix clock, child rate one and declared musical/sample duration, independent of caller rateScale. A destination starts at full gain; outgoing PCM fades from unity over FadeTime seconds. One outgoing state is retained: another transition stops the previous fade. Returning to the same looping child continues it rather than restarting; a nonlooping child restarts. Null/zero-duration slots are skipped with bounded search. Short child reads are zero-padded and do not move declared boundaries.

Active mixing returns the requested size, including terminal silence in the completing call. Final completion stops all child transports, including input requests. Cursor is cycle-relative double seconds; it resets at loop wrap and preserves its completed value after Stop. Loop count resets on Start/Seek and saturates at Int32.MaxValue. Count/slot edits replace the full cohort and leave the mixer stopped; fade/order/loop controls remain live.

## Example

```csharp
using var playlist = new AudioStreamPlaylist { StreamCount = 1, Loop = false };
playlist.SetListStream(0, track);
using var playback = playlist.InstantiatePlayback();
playback.Start(.1);
Vector2[] pcm = playback.MixAudio(1, 256);
playback.Seek(0);
playback.Stop();
```

`track` is a borrowed live timed resource. Engine playback uses prepared spans rather than allocating MixAudio results.

## API summary

| Full signature | Contract |
| --- | --- |
| `protected override void OnStart(double fromPosition)` | Reset/resolve absolute sequence seconds. |
| `protected override void OnStop()` | Stop every child and collect failures. |
| `protected override void OnSeek(double time)` | Restart at sequence position. |
| `protected override bool OnIsPlaying()` | Active sequence state. |
| `protected override int OnGetLoopCount()` | Completed playlist cycles. |
| `protected override double OnGetPlaybackPosition()` | Cycle-relative output-clock seconds. |
| `protected override int OnMix(Span<Vector2> buffer, float rateScale)` | Prepared finite timeline/transition/tail PCM. |
| `protected override void ValidateDisposal()` | Owner, phase and borrowed-state guards. |
| `protected override void Dispose(bool disposing)` | Release full child ownership/weak registration. |

## Method Descriptions

### OnStart

Stops old state, resets loop/cursor, validates live captured resource metadata and selects a positive-duration track in a fresh sequential/shuffled order. Seconds remain double precision. Empty/zero duration avoids modulo zero; nonlooping end stays stopped. Invalid metadata or child Start failures stop all states and report combined cleanup. Public inherited Start rejects nonfinite seconds.

### OnStop

Deactivates timeline/fade, stops every owned child and continues after callback errors. It does not dispose borrowed resources or reset the observed cursor/loop count.

### OnSeek

Uses the complete Start policy, resetting cursor and loop count and selecting from the newly generated order. Invalid inherited nonfinite time rejects.

### OnIsPlaying

False before a successful timed Start, after Stop, terminal sequence boundary, structural replacement or failed mix.

### OnGetLoopCount

Counts playlist cycle wraps, not child sample loops. Saturation avoids wrap to negative values.

### OnGetPlaybackPosition

Reports cycle-relative requested output time, including Start/Seek offset. Structural cohort replacement leaves stopped state; a later Start resets it.

### OnMix

Splits prepared blocks at track boundaries, preventing stale old-frame duplication and batch-size-dependent gaps. Child PCM is checked finite and short reads clear their remainder. Outgoing fade uses a separate prepared scratch buffer and double gain; invalid sums never escape. Zero fade contributes no old frame. Null/zero-duration slots are skipped, sub-frame positive tracks receive at least one output frame and long remaining durations need no per-frame allocation. Exceptions clear aggregate output and issue queued Stop on every state before native containment reports on owner processing.

### ValidateDisposal

Rejects callback/query reentry, foreign input/native owner cleanup and direct borrowed scene/composite disposal before logical disposal. Resource editing also blocks unsafe playback disposal.

### Dispose

Unregisters the weak consumer, releases every owned child even after Stop/Dispose failures, clears arrays and follows base sample-association cleanup. It never disposes the source or its child resources.

## Invariants and verification

Finite nonnegative FadeTime, finite tempo/nonnegative durations and finite PCM are trust boundaries. Source disposal invalidates consumption; Stop/Dispose still release ownership. Factories cannot reuse attached or already-owned playback. Input activation occurs only on eligible declared track scheduling; unknown-length microphone alone has no timed segment, while a wrapper with declared duration executes.

[AudioPlaylistTests](../../tests/Electron2D.Tests/AudioPlaylistTests.cs) covers exact PCM boundaries/fades/order, negative/end/modulo offsets, cycle counters, shuffle permutations, factory/cleanup/callback errors, typed resource copies and queued input. [Playlist playback](../components/audio-playback.md#playlist-playback) records native/public host/allocation scope. Inherited usage tagging/general parameters retain their own coverage dependencies.
