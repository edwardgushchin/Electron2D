# AudioStreamPlaybackInteractive

Last updated: 2026-10-03

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AudioStreamPlaybackInteractive` · **Source:** [AudioStreamPlaybackInteractive.cs](../../src/Scene/Resources/AudioStreamPlaybackInteractive.cs).

**Inherits:** [AudioStreamPlayback](AudioStreamPlayback.md).

## Description

Caller-owned playback created by AudioStreamInteractive.InstantiatePlayback. Player/emitter queries expose borrowed handles owned by their scene voice. Start prepares child factories and paused recording control capacity on the owner, starts InitialClip at zero, resets current/held state, and preserves a request restored before Start. Finite start/seek positions are validated and ignored; playback position/loop count remain zero. Stop attempts all cached children, cancels pending requests and retains the last current index. Source disposal invalidates Start/query/mix but Stop/Dispose remain available for cleanup.

SwitchToClip or literal-name selection stores one pending request consumed by the next active nonempty mix. Minus one/empty string cancels; null names, absent names or indices outside the active prefix reject. Null destinations are ignored. Requests while inactive do not start orphan children. GetCurrentClipIndex returns -1 before successful start, then the last scheduled nonempty clip selection. The descriptor's getter reads requested-or-current name; scene-player GetParameter separately reads its authored value. Direct handle requests never rewrite that authored player override.

BPM aligns source waits to next beat/bar or declared musical/sample End. Missing bar/tempo falls back to immediate, and unknown End is immediate. SamePosition uses source time plus wait only for a finite destination and resets beyond its end; PreviousPosition uses remembered mixed time. Regular zero fades are instant; Disabled/In give the source a one-millisecond fade except at a known nonlooping End. Automatic means Out for Start, otherwise Cross. A valid distinct filler starts full-gain and delays the destination by its declared duration; filler does not use its own auto policy. Destination auto advances at its declared end (SamePosition becomes Start), or consumes the remembered held source. Invalid/self/null targets do not queue.

Mixing uses fixed child rate one and the output clock, including when the requested rate is zero. The aggregate stays active over finite-child silence until explicit Stop or version invalidation. Scratch/event arrays are prepared for 1024-frame maximum blocks, split at waits/fade boundaries; ready clips plan before mixing. Automatic events run after all children in a boundary block, eliminating source-index artifacts and short-clip silent gaps. Current selection changes only on a nonempty scheduled span. Short child reads zero-pad, finite PCM sums are not unit-clipped, and nonfinite/unrepresentable timing/PCM fails with all children stopped.

Public controls/refresh and mutation serialize on the audio gate; callback mutation/query/mix/disposal reentry rejects before ownership is consumed. Unchanged children retain cached state at owner refresh; newly created children roll back on preparation failure, while cleanup failure after commit leaves the new stopped cache owned. Prepared scheduled internal controls support nested interactive/synchronized/randomized microphone children; public microphone controls/disposal still require the audio owner. Input preparation alone never records. Playback cleanup attempts every owned child, retains borrowed resources and releases recording reservations even after failures.

Factories, parameter/property discovery, copies, owner refresh and MixAudio result arrays are cold. Warmed direct switches/span mixing reuse storage. Child resampling prefetch remains part of concrete timing; no sample-driver transport, foreign-platform or physical recording/listening acceptance is claimed. See [interactive streams](../components/audio-playback.md#interactive-streams).

## Example

This snippet requires the named live AudioStream resources and the usual engine/scene host.

```csharp
using var playback = (AudioStreamPlaybackInteractive)interactive.InstantiatePlayback();
playback.Start();
playback.SwitchToClipByName("combat");
Vector2[] frames = playback.MixAudio(1, 256); // Cold copied output.
playback.Stop();
```

## Properties

| Complete signature | Contract |
| --- | --- |
| `public static Electron2D.PropertyDescriptor<Electron2D.AudioStreamPlaybackInteractive, System.String> SwitchToClipParameter { get;  }` | Gets the typed clip-name parameter used by AudioStreamPlayer and AudioStreamEmitter. |

## Properties descriptions

<a id="member-143efd827068"></a>
### SwitchToClipParameter

`public static Electron2D.PropertyDescriptor<Electron2D.AudioStreamPlaybackInteractive, System.String> SwitchToClipParameter { get;  }`

Gets the typed clip-name parameter used by AudioStreamPlayer and AudioStreamEmitter.

Value: An empty string cancels a pending request; names resolve to the first active matching slot.

## Methods and protected hooks

| Complete signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Concrete inherited hook; see Description and the base-class contract. |
| `public System.Int32 GetCurrentClipIndex()` | Gets the last clip whose scheduled first frame became current. |
| `protected override System.Double OnGetPlaybackPosition()` | Concrete inherited hook; see Description and the base-class contract. |
| `protected override System.Boolean OnIsPlaying()` | Concrete inherited hook; see Description and the base-class contract. |
| `protected override System.Int32 OnMix(System.Span<Electron2D.Vector2> buffer, System.Single rateScale)` | Concrete inherited hook; see Description and the base-class contract. |
| `protected override System.Void OnSeek(System.Double time)` | Concrete inherited hook; see Description and the base-class contract. |
| `protected override System.Void OnStart(System.Double fromPosition)` | Concrete inherited hook; see Description and the base-class contract. |
| `protected override System.Void OnStop()` | Concrete inherited hook; see Description and the base-class contract. |
| `public System.Void SwitchToClip(System.Int32 clipIndex)` | Queues a clip switch for the next nonempty active mix. |
| `public System.Void SwitchToClipByName(System.String clipName)` | Queues the first active clip with a matching literal name. |
| `protected override System.Void ValidateDisposal()` | Concrete inherited hook; see Description and the base-class contract. |

## Methods and protected hooks descriptions

<a id="member-2f167191fc29"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-5224f1334f82"></a>
### GetCurrentClipIndex

`public System.Int32 GetCurrentClipIndex()`

Gets the last clip whose scheduled first frame became current.

Returns: Minus one before a successful start, otherwise the last current index, also after Stop.

<a id="member-948dcc1b02b7"></a>
### OnGetPlaybackPosition

`protected override System.Double OnGetPlaybackPosition()`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-5f78beb4c5fc"></a>
### OnIsPlaying

`protected override System.Boolean OnIsPlaying()`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-684ea18dada9"></a>
### OnMix

`protected override System.Int32 OnMix(System.Span<Electron2D.Vector2> buffer, System.Single rateScale)`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-27623d8c87b4"></a>
### OnSeek

`protected override System.Void OnSeek(System.Double time)`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-bcdbffa83bcd"></a>
### OnStart

`protected override System.Void OnStart(System.Double fromPosition)`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-6e8e0f51e1de"></a>
### OnStop

`protected override System.Void OnStop()`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

<a id="member-9d883545530d"></a>
### SwitchToClip

`public System.Void SwitchToClip(System.Int32 clipIndex)`

Queues a clip switch for the next nonempty active mix.

Remarks: Requests made before Start are retained. A null clip is ignored without starting another child.

`clipIndex`: An active clip index, or minus one to cancel a pending request.

System.ArgumentOutOfRangeException: The index is outside the active prefix.

System.InvalidOperationException: A child callback reenters controls.

<a id="member-e7e5c01bc6e3"></a>
### SwitchToClipByName

`public System.Void SwitchToClipByName(System.String clipName)`

Queues the first active clip with a matching literal name.

`clipName`: Non-null name; an empty string cancels a pending request.

System.ArgumentNullException: The name is null.

System.ArgumentException: No active clip has that name.

<a id="member-1a37fa7889b9"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Concrete inherited hook; the Description and base-class contract specify behavior and verification.

## Related enums and lifecycle

The Description specifies state changes, errors, ownership and threading. [AudioInteractiveTests](../../tests/Electron2D.Tests/AudioInteractiveTests.cs) exercises deterministic PCM, lifetime/copies/typed parameters and warmed CPU/native controls. [ADR 0047](../decisions/audio.md#adr-0047) and [the component](../components/audio-playback.md#interactive-streams) record execution and physical/platform limits.

A caller-associated native sample also contributes the inherited audio-owner requirement; nested control preparation preserves that affinity. See [AudioStreamPlayback](AudioStreamPlayback.md#setsampleplayback).

[Dynamic polyphonic voices](../components/audio-playback.md#dynamic-polyphony) now integrate with this audio ownership/control path; native children inherit enclosing scene mix/pause/spatial controls and retain their own requested bus. Retired sample/input ownership stays owner-affine until cleanup.
