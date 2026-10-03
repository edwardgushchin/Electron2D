# AudioStreamRandomizer

Last updated: 2026-10-03

**Namespace:** `Electron2D` · **Declaration:** `public sealed class AudioStreamRandomizer : AudioStream` · **Source:** [AudioStreamRandomizer.cs](../../src/Scene/Resources/AudioStreamRandomizer.cs).

**Inherits:** [AudioStream](AudioStream.md). No production subclasses.

## Description

A borrowed audio pool selects one independent child playback at InstantiatePlayback. History is shared by resource identity across all callers and players. Calling Start again keeps the captured child and samples current pitch/volume variation again. Pool edits do not retarget existing captures. Random modes ignore null streams and nonpositive weights; no-repeat excludes every slot holding the preceding identity when another positive identity exists. Sequential mode skips nulls and duplicate identities, ignores weights, wraps in pool order and resets to the first eligible identity when the preceding choice is absent. Empty captures mix the requested silence while their own IsPlaying stays false. Native player slots may still remain active until explicitly stopped because this silence supplies full quanta.

GetLength is zero before the first choice, then queries the last selected resource, even after removing that resource from the pool. Empty later choices retain this history. IsMonophonic checks all non-null members, including zero weights; IsMetaStream is true and the stream-name hook returns Randomizer. Other optional inherited metadata and parameter lists retain base defaults. This resource does not forward child looping parameters or usage tags; applicable inherited sample/tag/parameter dependencies stay on their declaring coverage pages.

The pool owns its immutable entry array; children are borrowed. A factory-created wrapper owns its selected child playback and borrows the randomizer. Pool disposal clears references and invalidates consuming wrappers without disposing child resources. Wrapper disposal attempts child cleanup and base cleanup even after a throwing child callback. Stop remains available after parent disposal. Each cursor requires caller coordination; native mixing/control is synchronized by the existing audio gate.

A shared authoring gate makes pool edits and cycle checks atomic. Snapshots are immutable and callbacks execute outside that gate. Direct/transitive mixed composite graph cycles reject before mutation; per-thread operation guards also reject recursive custom callbacks, historical duration cycles and nesting beyond 256 operations. Authoring, playback construction, copying and property discovery allocate; warmed successful mix and metadata queries reuse prepared state. Random draws use the standard shared random source; no new seed API is introduced.

## Example

Partial scene snippet: attach the player to a Node/Window and retain its borrowed resources until the scene is disposed.

```csharp
using var stepA = AudioStreamWAV.LoadFromFile("res://audio/step-a.wav");
using var stepB = AudioStreamWAV.LoadFromFile("res://audio/step-b.wav");
using var steps = new AudioStreamRandomizer { RandomPitchSemitones = 2, RandomVolumeOffsetDB = 1 };
steps.AddStream(-1, stepA, 2);
steps.AddStream(-1, stepB);
var player = new AudioStreamPlayer { Stream = steps };
// window.AddChild(player); player.Play(); selects one sound per actual Play.
```

## Enumeration

[PlaybackMode](AudioStreamRandomizer.PlaybackMode.md) retains RandomNoRepeats=0, Random=1 and Sequential=2. `Mode` is the typed property; its distinct name avoids the C# property/nested-type name collision.

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public AudioStreamRandomizer()` | Creates an empty pool, no-repeat selection and disabled pitch/volume variation. |

## Constructors descriptions

### .ctor

`public AudioStreamRandomizer()`

Summary: Creates an empty pool, no-repeat selection and disabled pitch/volume variation.


## Properties

| Complete signature | Contract |
| --- | --- |
| `public Electron2D.AudioStreamRandomizer.PlaybackMode Mode { get; set; }` | Gets or sets the stream selection policy. |
| `public System.Single RandomPitch { get; set; }` | Gets or sets the largest random frequency multiplier. |
| `public System.Single RandomPitchSemitones { get; set; }` | Gets or sets the equivalent semitone variation. |
| `public System.Single RandomVolumeOffsetDB { get; set; }` | Gets or sets the symmetric random volume range in decibels. |
| `public System.Int32 StreamsCount { get; set; }` | Gets or sets the pool length, retaining existing entries before the new end. |

## Properties descriptions

### Mode

`public Electron2D.AudioStreamRandomizer.PlaybackMode Mode { get; set; }`

Summary: Gets or sets the stream selection policy.

Value: RandomNoRepeats initially; history persists when the mode changes.

System.ArgumentOutOfRangeException: The mode is undefined.

System.ObjectDisposedException: The resource is disposed.


### RandomPitch

`public System.Single RandomPitch { get; set; }`

Summary: Gets or sets the largest random frequency multiplier.

Value: One initially; assignments below one clamp to one. Pitch is uniform in logarithmic space between its reciprocal and itself.

System.ArgumentOutOfRangeException: The value is nonfinite.

System.ObjectDisposedException: The resource is disposed.


### RandomPitchSemitones

`public System.Single RandomPitchSemitones { get; set; }`

Summary: Gets or sets the equivalent semitone variation.

Value: Zero initially. Writes set the multiplier to 2^(value/12); reads use 12*log2(max(1,multiplier)).

Remarks: Negative semitone writes retain a reciprocal multiplier while this getter reports zero.

System.ArgumentOutOfRangeException: The exponent does not produce a positive finite float multiplier.

System.ObjectDisposedException: The resource is disposed.


### RandomVolumeOffsetDB

`public System.Single RandomVolumeOffsetDB { get; set; }`

Summary: Gets or sets the symmetric random volume range in decibels.

Value: Zero initially; negative writes clamp to zero. Start samples a uniform offset in [-value,+value].

System.ArgumentOutOfRangeException: The value is nonfinite.

System.ObjectDisposedException: The resource is disposed.


### StreamsCount

`public System.Int32 StreamsCount { get; set; }`

Summary: Gets or sets the pool length, retaining existing entries before the new end.

Value: Zero initially. New slots contain null streams and weight one. Resizing is silent.

System.ArgumentOutOfRangeException: The count is negative.

System.ObjectDisposedException: The resource is disposed.


## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `public System.Void AddStream(System.Int32 index, Electron2D.AudioStream? stream, System.Single weight = 1f)` | Inserts a borrowed stream and its literal probability weight. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Concrete inherited contract; see the description. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Concrete inherited contract; see the description. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Concrete inherited contract; see the description. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Concrete inherited contract; see the description. |
| `public Electron2D.AudioStream? GetStream(System.Int32 index)` | Gets the borrowed stream at an existing index. |
| `public System.Single GetStreamProbabilityWeight(System.Int32 index)` | Gets an entry's literal probability weight. |
| `public override System.Boolean IsMetaStream()` | Concrete inherited contract; see the description. |
| `public System.Void MoveStream(System.Int32 indexFrom, System.Int32 indexTo)` | Moves an entry to an insertion boundary in the pool before removal. |
| `protected override System.Double OnGetLength()` | Concrete inherited contract; see the description. |
| `protected override System.String OnGetStreamName()` | Concrete inherited contract; see the description. |
| `protected override Electron2D.AudioStreamPlayback OnInstantiatePlayback()` | Concrete inherited contract; see the description. |
| `protected override System.Boolean OnIsMonophonic()` | Concrete inherited contract; see the description. |
| `public System.Void RemoveStream(System.Int32 index)` | Removes an entry without disposing its borrowed stream. |
| `public System.Void SetStream(System.Int32 index, Electron2D.AudioStream? stream)` | Replaces the borrowed stream at an existing index. |
| `public System.Void SetStreamProbabilityWeight(System.Int32 index, System.Single weight)` | Replaces an entry's literal probability weight. |

## Methods and protected extension points descriptions

### AddStream

`public System.Void AddStream(System.Int32 index, Electron2D.AudioStream? stream, System.Single weight = 1f)`

Summary: Inserts a borrowed stream and its literal probability weight.

index: Insertion position through StreamsCount; any negative value appends.

stream: Borrowed stream, or null for an empty slot.

weight: Finite weight; one initially. Nonpositive weights are ignored by random modes.

Remarks: Commits before Changed and PropertyListChanged notifications. Nested randomizer cycles reject before mutation.

System.ArgumentOutOfRangeException: The insertion position is beyond the end or the weight is nonfinite.

System.InvalidOperationException: The assignment creates a randomizer cycle.

System.ObjectDisposedException: This resource or the assigned stream is disposed.


### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Projects the inherited typed resource/audio contract with the selection, ownership and error rules in Description.


### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Projects the inherited typed resource/audio contract with the selection, ownership and error rules in Description.


### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Projects the inherited typed resource/audio contract with the selection, ownership and error rules in Description.


### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Projects the inherited typed resource/audio contract with the selection, ownership and error rules in Description.


### GetStream

`public Electron2D.AudioStream? GetStream(System.Int32 index)`

Summary: Gets the borrowed stream at an existing index.

index: Existing entry index.

Returns: The borrowed resource or null.

System.ArgumentOutOfRangeException: The index is outside the pool.

System.ObjectDisposedException: This resource is disposed.


### GetStreamProbabilityWeight

`public System.Single GetStreamProbabilityWeight(System.Int32 index)`

Summary: Gets an entry's literal probability weight.

index: Existing entry index.

Returns: The stored signed weight.

System.ArgumentOutOfRangeException: The index is outside the pool.

System.ObjectDisposedException: The resource is disposed.


### IsMetaStream

`public override System.Boolean IsMetaStream()`

Projects the inherited typed resource/audio contract with the selection, ownership and error rules in Description.


### MoveStream

`public System.Void MoveStream(System.Int32 indexFrom, System.Int32 indexTo)`

Summary: Moves an entry to an insertion boundary in the pool before removal.

indexFrom: Existing entry index.

indexTo: Boundary from zero through StreamsCount in the original pool.

Remarks: Moving index 1 to boundary 3 in [0,1,2,3] produces [0,2,1,3]. Emits both change notifications even for an unchanged order.

System.ArgumentOutOfRangeException: An index is outside its stated range.

System.ObjectDisposedException: The resource is disposed.


### OnGetLength

`protected override System.Double OnGetLength()`

Projects the inherited typed resource/audio contract with the selection, ownership and error rules in Description.


### OnGetStreamName

`protected override System.String OnGetStreamName()`

Projects the inherited typed resource/audio contract with the selection, ownership and error rules in Description.


### OnInstantiatePlayback

`protected override Electron2D.AudioStreamPlayback OnInstantiatePlayback()`

Projects the inherited typed resource/audio contract with the selection, ownership and error rules in Description.


### OnIsMonophonic

`protected override System.Boolean OnIsMonophonic()`

Projects the inherited typed resource/audio contract with the selection, ownership and error rules in Description.


### RemoveStream

`public System.Void RemoveStream(System.Int32 index)`

Summary: Removes an entry without disposing its borrowed stream.

index: Existing entry index.

Remarks: Emits Changed and PropertyListChanged; the previous selection remains available for GetLength.

System.ArgumentOutOfRangeException: The index is outside the pool.

System.ObjectDisposedException: The resource is disposed.


### SetStream

`public System.Void SetStream(System.Int32 index, Electron2D.AudioStream? stream)`

Summary: Replaces the borrowed stream at an existing index.

index: Existing entry index.

stream: Borrowed stream or null.

Remarks: Retains the weight and emits Changed after commit, including equal assignments.

System.ArgumentOutOfRangeException: The index is outside the pool.

System.InvalidOperationException: The assignment creates a randomizer cycle.

System.ObjectDisposedException: This resource or the assigned stream is disposed.


### SetStreamProbabilityWeight

`public System.Void SetStreamProbabilityWeight(System.Int32 index, System.Single weight)`

Summary: Replaces an entry's literal probability weight.

index: Existing entry index.

weight: Finite signed weight; random modes consume only positive values.

Remarks: Emits Changed after commit, including equal assignments.

System.ArgumentOutOfRangeException: The index is outside the pool or the weight is nonfinite.

System.ObjectDisposedException: The resource is disposed.

## State, notifications and errors

New slots are null with weight one. Add/Move/Remove report Changed then PropertyListChanged after committing. SetStream/SetStreamProbabilityWeight report Changed even for equal writes. Metadata setters and StreamsCount resizing are silent. Structural notification delivery attempts both events after a throwing Changed callback unless disposal has completed; two callback failures aggregate. Property discovery supplies typed Stream_{index}/Stream and Stream_{index}/Weight descriptors rather than untyped string dispatch; a descriptor whose index was removed rejects on use.

RandomPitch clamps writes below one. Semitone writes instead store 2^(semitones/12), including reciprocal multipliers for negative semitones; the semitone getter clamps its display calculation to a multiplier of one. RandomVolumeOffsetDB clamps negative writes to zero. Nonfinite data, undefined modes, invalid indices/counts and an exponent outside positive finite float storage reject. Start validates sampled variation before committing pitch/gain; child errors propagate. Weights remain literal finite signed floats, summed in double precision when consumed.

Duplicate copies pool containers/configuration and starts independent empty history, borrowing children shallowly. DuplicateDeep follows the standard Resource subresource policy and preserves repeated child identity. PackedScene local resource duplication uses those hooks; no file serializer, import registration or general composite graph authoring format is added.

## Verification and dependencies

[AudioRandomizerTests](../../tests/Electron2D.Tests/AudioRandomizerTests.cs) checks defaults, typed schema/notifications, indices/moves, duplicate/null/weight modes, shared history, 4000 weighted draws, 1000 logarithmic/DB variation samples, warm active/idle spans, copy/local scenes, concurrent cycles, recursion/depth, historical loops, borrowed lifetime and callback failures. Native mode checks actual FAudio PCM, one selection per Play, lazy polyphony, replacement/cleanup/reentrancy recovery and 64 active/64 paused passes after 20 warmup passes with zero managed bytes and zero custom FAudio allocator calls.

[AudioRandomizerHostTests](../../tests/Electron2D.Tests/AudioRandomizerHostTests.cs) uses public Engine.Run/Window/Autoplay/Stop/Play and verifies two sequential clean host runs for each Linux Wayland GPU/compatibility renderer. Native callback measurement does not cover all driver/OS allocations. Physical listening, other platforms and broad workload performance remain unverified.

[ADR 0047](../decisions/audio.md#adr-0047), [Resource lifetime](../decisions/resources.md#adr-0014), [audio component](../components/audio-playback.md) and [own coverage](../coverage/classes/AudioStreamRandomizer.md) define the current boundaries.

Mixed synchronized/randomizer graphs now use the same authoring/callback validation. Microphone selection preflights the audio owner needed by child cleanup before invoking that factory; the wrapper propagates that affinity and validates disposal before consuming its own state. This prevents a wrapper from losing an active owner-bound child after an off-owner Dispose refusal.

Interactive parents now prepare child controls on the audio owner, including paused microphone input and request capacity, then schedule selected child Start/Stop under the shared audio gate. Public microphone controls/disposal retain owner checks; preparation alone does not record. Mixed interactive/randomizer/synchronized graphs share cycle/owner validation. See [interactive streams](../components/audio-playback.md#interactive-streams) for timing, lifecycle, native evidence and limits.

An associated native sample on a randomizer playback contributes the inherited audio-owner requirement alongside its selected child.
