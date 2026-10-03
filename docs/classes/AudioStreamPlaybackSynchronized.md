# AudioStreamPlaybackSynchronized

Last updated: 2026-10-03

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AudioStreamPlaybackSynchronized` · **Source:** [AudioStreamPlaybackSynchronized.cs](../../src/Scene/Resources/AudioStreamPlaybackSynchronized.cs).

**Inherits:** [AudioStreamPlayback](AudioStreamPlayback.md).

## Description

Concrete caller-owned playback created by AudioStreamSynchronized.InstantiatePlayback; player queries expose borrowed player-owned handles. It owns one independent playback per non-null active child slot and one prepared 128-frame stereo scratch block. Start uses the same finite requested seconds for all children; starting an active aggregate stops prior states first. Stop reaches all owned children; Seek forwards to selected states even when stopped. Child start/stop/seek failures attempt remaining children; failed Start stops all states before reporting. Microphone ownership propagates through nested synchronized/randomizer playback and validates controls/disposal before consuming ownership.

Mixing sums child stereo samples without unit-range clipping, applies live dB coefficients and forwards rate scale. Short child reads contribute only reported valid frames; the rest is explicit silence. Nonfinite child PCM or unrepresentable finite sums throw into the existing native containment/owner-reporting path. Earlier child cursors may already advance on failure. Requests split into bounded 128-frame blocks; zero requests preserve state. An active call reports the complete request, including zero-padded completed children. A complete call with no active child clears the aggregate latch and still reports its silent request; subsequent inactive calls return zero. A looping/indefinite child keeps the aggregate running.

GetLoopCount returns the minimum among currently playing children; GetPlaybackPosition returns their maximum cursor (double precision), or zero if none. Control/mix/cursor callbacks reject mutation or recursive mixing/querying. Resource replacement/count changes atomically adopt freshly prepared child ownership and leave this handle stopped. The old children are stopped/disposed after commit; cleanup errors do not remove new ownership. Source disposal invalidates Start/mix/query, while Stop/Dispose remain available to close children. Disposal attempts every child even after cleanup failures and unregisters its weak factory entry. Construction, structural edits and MixAudio result copies allocate; warmed span mixing and live gain updates reuse storage.

## Example

The snippet requires the named live stream resources.

```csharp
using var playback = (AudioStreamPlaybackSynchronized)synchronized.InstantiatePlayback();
playback.Start();
Vector2[] copiedFrames = playback.MixAudio(1, 256); // Explicit cold result allocation.
playback.Stop();
```

## Methods and protected hooks

| Complete signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Int32 OnGetLoopCount()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Double OnGetPlaybackPosition()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Boolean OnIsPlaying()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Int32 OnMix(System.Span<Electron2D.Vector2> buffer, System.Single rateScale)` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Void OnSeek(System.Double time)` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Void OnStart(System.Double fromPosition)` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Void OnStop()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Void ValidateDisposal()` | Concrete inherited hook; see Description for ownership, timing and failures. |

## Methods and protected hooks descriptions

<a id="member-03b765e0cf18"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-57779635893a"></a>
### OnGetLoopCount

`protected override System.Int32 OnGetLoopCount()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-b16ec760fed6"></a>
### OnGetPlaybackPosition

`protected override System.Double OnGetPlaybackPosition()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-9434e3a925ea"></a>
### OnIsPlaying

`protected override System.Boolean OnIsPlaying()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-cd040a038287"></a>
### OnMix

`protected override System.Int32 OnMix(System.Span<Electron2D.Vector2> buffer, System.Single rateScale)`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-7d6150d9a49e"></a>
### OnSeek

`protected override System.Void OnSeek(System.Double time)`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-9685746566ec"></a>
### OnStart

`protected override System.Void OnStart(System.Double fromPosition)`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-fc481d8fba52"></a>
### OnStop

`protected override System.Void OnStop()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-96bb8291d0bf"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Concrete inherited hook; see Description for ownership, timing and failures.

## Verification and dependencies

[ADR 0047](../decisions/audio.md#adr-0047) and [synchronized streams](../components/audio-playback.md#synchronized-streams) define the executable contract and adaptations. AudioSynchronizedTests covers resource/metadata/copy/scene state, PCM/cursors/live gain/finish, transaction/callback/cycle failures, nested microphone owner handling and warmed CPU/native allocations. AudioSynchronizedHostTests runs real finite WAVs through public Window/Engine lifecycle, pause/seek/live gain and last-child Finished. Linux x64 dummy/native output is verified; physical listening/input, other platforms and driver-internal allocations remain unverified. Sample-driver, usage tags, further music/parameter resources and scene-file persistence retain their own coverage dependencies.

Interactive parents now prepare child controls on the audio owner, including paused microphone input and request capacity, then schedule selected child Start/Stop under the shared audio gate. Public microphone controls/disposal retain owner checks; preparation alone does not record. Mixed interactive/randomizer/synchronized graphs share cycle/owner validation. See [interactive streams](../components/audio-playback.md#interactive-streams) for timing, lifecycle, native evidence and limits.

A caller-associated native sample also contributes the inherited audio-owner requirement; nested control preparation preserves that affinity. See [AudioStreamPlayback](AudioStreamPlayback.md#setsampleplayback).
