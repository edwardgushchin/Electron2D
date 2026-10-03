# AudioStreamSynchronized

Last updated: 2026-10-03

**Namespace:** `Electron2D` · **Declaration:** `public sealed class Electron2D.AudioStreamSynchronized` · **Source:** [AudioStreamSynchronized.cs](../../src/Scene/Resources/AudioStreamSynchronized.cs).

**Inherits:** [AudioStream](AudioStream.md).

## Description

Meta-stream resource borrowing up to 32 child resources. Configure any slot 0–31 independently of StreamCount; only its prefix participates in factory, duration/tempo and playback. Count growth/shrink retains hidden assignments on this resource. Zero dB is the initial gain for every slot. NaN, positive infinity or unrepresentable gain rejects before mutation; negative infinity is exact mute; very negative finite dB may underflow to silence. A zero gain still mixes/advances the child. Gain edits commit without Changed and allocate no storage.

SetSyncStream always rebuilds the entire live playback cohort, including equal or hidden-slot assignments, and leaves it stopped. Changed count also rebuilds; equal count only notifies PropertyListChanged. Slot/gain edits do not emit Changed; count notification follows commit. All required new children are prepared before slot/count publication. Factory failure preserves prior configuration/playback ownership, while old Stop/Dispose errors are collected after the new stopped states commit. Custom factories must return fresh independent caller-owned playback and release their own untransferred construction state. Their unrelated side effects cannot be rolled back.

Factories and structural edits serialize with native mixing through the audio gate, invoke callbacks outside the composite graph gate, and reject recursive structural/control edits. Ordinary gain writes and resource reads serialize on the graph gate; mixing reads gain atomically before each 128-frame child block. Microphone-containing factory/control/cleanup paths require the audio configuration owner, including through randomizer containers. Pure prepared stereo mixing may run on a caller/audio thread. Callers coordinate compound authoring sequences.

Metadata projects maximum child length and beat count, first nonzero BPM/bar beats and any child loop from the active prefix. Stream name is Synchronized, IsMetaStream is true, inherited monophonic policy is true and parameter list is empty. Child tags/looping parameters are not forwarded; their broader inherited coverage remains separate. Mixed randomizer/synchronized graph cycles, disposed assigned resources and recursive metadata/factory callbacks reject.

Shallow/deep/scene-local copies follow Resource graph policy, preserve aliases and store only active-prefix child/volume configuration. Hidden assignments and independent playback states are outside copied stored state. Disposal invalidates borrowing playbacks and clears resource references without disposing child resources or caller-owned playback. The factory weak registry never retains an otherwise unreferenced playback.

## Example

The snippet requires the named live stream resources.

```csharp
using var synchronized = new AudioStreamSynchronized { StreamCount = 2 };
synchronized.SetSyncStream(0, music); // Existing live AudioStream resources.
synchronized.SetSyncStream(1, percussion);
synchronized.SetSyncStreamVolume(1, -6);
var player = new AudioStreamPlayer { Stream = synchronized, Autoplay = true };
// Add player to a Window and run Engine.Instance.Run(window).
```

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public AudioStreamSynchronized()` | Creates an empty synchronized resource with zero dB in every slot. |

## Constructors descriptions

<a id="member-13ecf4a35aa7"></a>
### .ctor

`public AudioStreamSynchronized()`

Creates an empty synchronized resource with zero dB in every slot.

## Constants

| Complete signature | Contract |
| --- | --- |
| `public const System.Int32 MaxStreams = 32` | The maximum number of simultaneously configured child slots. |

## Constants descriptions

<a id="member-b57807c7fea0"></a>
### MaxStreams

`public const System.Int32 MaxStreams = 32`

The maximum number of simultaneously configured child slots.

## Properties

| Complete signature | Contract |
| --- | --- |
| `public System.Int32 StreamCount { get; set; }` | Gets or sets the prefix of configured slots used for playback and duration/tempo queries. |

## Properties descriptions

<a id="member-29ae08c28210"></a>
### StreamCount

`public System.Int32 StreamCount { get; set; }`

Gets or sets the prefix of configured slots used for playback and duration/tempo queries.

Value: Zero initially; zero through MaxStreams. Hidden slot assignments persist on this resource.

Remarks: A changed count rebuilds existing playback ownership and leaves it stopped. Equal count still notifies PropertyListChanged without rebuilding. Factory failure preserves old configuration/playback.

System.ArgumentOutOfRangeException: The count is outside zero through MaxStreams.

System.InvalidOperationException: A factory/mix/control callback reenters structural editing.

System.ObjectDisposedException: The resource is disposed.

## Methods and protected hooks

| Complete signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `public Electron2D.AudioStream GetSyncStream(System.Int32 streamIndex)` | Gets the borrowed resource configured at a slot, including hidden slots. |
| `public System.Single GetSyncStreamVolume(System.Int32 streamIndex)` | Gets a slot's stored volume in decibels. |
| `public override System.Boolean IsMetaStream()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Double OnGetBPM()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Int32 OnGetBarBeats()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Int32 OnGetBeatCount()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Double OnGetLength()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.String OnGetStreamName()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override System.Boolean OnHasLoop()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `protected override Electron2D.AudioStreamPlayback OnInstantiatePlayback()` | Concrete inherited hook; see Description for ownership, timing and failures. |
| `public System.Void SetSyncStream(System.Int32 streamIndex, Electron2D.AudioStream audioStream)` | Replaces a child slot and rebuilds existing stopped playback state. |
| `public System.Void SetSyncStreamVolume(System.Int32 streamIndex, System.Single volumeDB)` | Sets the live volume of one child without restarting playback. |

## Methods and protected hooks descriptions

<a id="member-30e3c34fe7f2"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-36ffe2050867"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-f909bf142de7"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-c20df195b21a"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-81f44fd18468"></a>
### GetSyncStream

`public Electron2D.AudioStream GetSyncStream(System.Int32 streamIndex)`

Gets the borrowed resource configured at a slot, including hidden slots.

streamIndex: Zero through MaxStreams minus one.

Returns: The borrowed stream, null initially.

System.ArgumentOutOfRangeException: The index is outside fixed capacity.

System.ObjectDisposedException: This resource is disposed.

<a id="member-e6ec1c698bf7"></a>
### GetSyncStreamVolume

`public System.Single GetSyncStreamVolume(System.Int32 streamIndex)`

Gets a slot's stored volume in decibels.

streamIndex: Zero through MaxStreams minus one.

Returns: Zero dB initially, independently of StreamCount.

System.ArgumentOutOfRangeException: The index is outside capacity.

System.ObjectDisposedException: This resource is disposed.

<a id="member-5fb35157dd10"></a>
### IsMetaStream

`public override System.Boolean IsMetaStream()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-ccfcf0f06d5e"></a>
### OnGetBPM

`protected override System.Double OnGetBPM()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-276b68bc08a4"></a>
### OnGetBarBeats

`protected override System.Int32 OnGetBarBeats()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-58e7ec022ca4"></a>
### OnGetBeatCount

`protected override System.Int32 OnGetBeatCount()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-8c36d72fd056"></a>
### OnGetLength

`protected override System.Double OnGetLength()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-e8e401ac3246"></a>
### OnGetStreamName

`protected override System.String OnGetStreamName()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-b68b934d6aa6"></a>
### OnHasLoop

`protected override System.Boolean OnHasLoop()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-8082622ea561"></a>
### OnInstantiatePlayback

`protected override Electron2D.AudioStreamPlayback OnInstantiatePlayback()`

Concrete inherited hook; see Description for ownership, timing and failures.

<a id="member-d0782f989de6"></a>
### SetSyncStream

`public System.Void SetSyncStream(System.Int32 streamIndex, Electron2D.AudioStream audioStream)`

Replaces a child slot and rebuilds existing stopped playback state.

streamIndex: Zero through MaxStreams minus one, independent of StreamCount.

audioStream: Borrowed resource or null.

Remarks: Equal assignments also rebuild. No Changed notification is raised. New child factories finish before configuration commits; old child cleanup failures report after the new stopped state commits.

System.ArgumentOutOfRangeException: The index is outside capacity.

System.InvalidOperationException: The assignment creates a composite cycle or reenters editing.

System.ObjectDisposedException: This resource or an assigned child is disposed.

<a id="member-6be3951d3934"></a>
### SetSyncStreamVolume

`public System.Void SetSyncStreamVolume(System.Int32 streamIndex, System.Single volumeDB)`

Sets the live volume of one child without restarting playback.

streamIndex: Zero through MaxStreams minus one.

volumeDB: Decibels producing a finite float multiplier; negative infinity mutes and very negative finite values may underflow to silence.

Remarks: Does not raise Changed. Mixing reads the coefficient before each child block; a zero coefficient still advances the child in sync. Successful edits do not allocate.

System.ArgumentOutOfRangeException: The index or decibel/multiplier value is invalid.

System.ObjectDisposedException: This resource is disposed.

## Verification and dependencies

[ADR 0047](../decisions/audio.md#adr-0047) and [synchronized streams](../components/audio-playback.md#synchronized-streams) define the executable contract and adaptations. AudioSynchronizedTests covers resource/metadata/copy/scene state, PCM/cursors/live gain/finish, transaction/callback/cycle failures, nested microphone owner handling and warmed CPU/native allocations. AudioSynchronizedHostTests runs real finite WAVs through public Window/Engine lifecycle, pause/seek/live gain and last-child Finished. Linux x64 dummy/native output is verified; physical listening/input, other platforms and driver-internal allocations remain unverified. Sample-driver, usage tags, further music/parameter resources and scene-file persistence retain their own coverage dependencies.

WAV sample looping keeps child PCM active indefinitely while its inherited music-loop hook retains the base false value, matching its distinct sample/tempo metadata contract. Compressed and custom streams report their own loop metadata.

Interactive parents now prepare child controls on the audio owner, including paused microphone input and request capacity, then schedule selected child Start/Stop under the shared audio gate. Public microphone controls/disposal retain owner checks; preparation alone does not record. Mixed interactive/randomizer/synchronized graphs share cycle/owner validation. See [interactive streams](../components/audio-playback.md#interactive-streams) for timing, lifecycle, native evidence and limits.
