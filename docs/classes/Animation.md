# Animation

Last updated: 2026-10-04

**Inherits:** [Resource](Resource.md)

- **Source:** [`src/Scene/Resources/Animation.cs`](../../src/Scene/Resources/Animation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class Electron2D.Animation`

## Description

Reusable typed value/Bézier/method/nested-player timelines, key/container authoring and named markers.

The complete timing, copy/borrowing, validation, callback error/reentry and verification contract is on [Scene animation](../components/scene-animation.md), including [special tracks](../components/scene-animation.md#bézier-and-method-tracks). Author/edit on the scene owner thread; target nodes and authored resources remain borrowed. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the current implementation.

## Examples

Partial snippet; ChildPlayer already exists under the controller's root:

```csharp
var sequence = new Animation { Length = 2 };
var childTrack = sequence.AddAnimationTrack();
sequence.TrackSetPath(childTrack, "ChildPlayer");
sequence.AnimationTrackInsertKey(childTrack, 0, "walk");
sequence.AnimationTrackInsertKey(childTrack, 1, "[stop]");
```


Partial authoring snippet; existing scene/library variables are indicated where required.

```csharp
var clip = new Animation { Length = 1 };
var property = new PropertyDescriptor<Entity, float>("X", n => n.Position.X,
    (n, value) => n.Position = new(value, n.Position.Y));
var track = clip.AddBezierTrack(property);
clip.TrackSetPath(track, "Sprite:X");
clip.BezierTrackInsertKey(track, 0, 16, outHandle: new(1f / 3, 0));
clip.BezierTrackInsertKey(track, 1, 80, inHandle: new(-1f / 3, 0));
```

## Enumeration types

| Type | Reference |
| --- | --- |
| `public enum Electron2D.Animation.FindMode` | [FindMode](Animation.FindMode.md) |
| `public enum Electron2D.Animation.InterpolationType` | [InterpolationType](Animation.InterpolationType.md) |
| `public enum Electron2D.Animation.LoopedFlag` | [LoopedFlag](Animation.LoopedFlag.md) |
| `public enum Electron2D.Animation.TrackType` | [TrackType](Animation.TrackType.md) |
| `public enum Electron2D.Animation.UpdateMode` | [UpdateMode](Animation.UpdateMode.md) |

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public Animation()` | Creates an empty one-second nonlooping typed timeline. |

## Constructor Descriptions

<a id="member-b222f0cbd4aa"></a>
### .ctor

`public Animation()`

Creates an empty one-second nonlooping typed timeline.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean CaptureIncluded { get;  }` | Gets whether any property track admits capture. |
| `public System.Double Length { get; set; }` | Gets or sets duration in seconds; finite values below 0.001 are clamped. |
| `public Electron2D.SpriteFrames.LoopMode LoopMode { get; set; }` | Gets or sets endpoint behavior, sharing the existing animation endpoint contract. |
| `public System.Double Step { get; set; }` | Gets or sets the authoring time-step hint, initially approximately one thirtieth second. |

## Property Descriptions

<a id="member-705a51321477"></a>
### CaptureIncluded

`public System.Boolean CaptureIncluded { get;  }`

Gets whether any property track admits capture.

<a id="member-e5633ec7c522"></a>
### Length

`public System.Double Length { get; set; }`

Gets or sets duration in seconds; finite values below 0.001 are clamped.

<a id="member-4e07b432b8e0"></a>
### LoopMode

`public Electron2D.SpriteFrames.LoopMode LoopMode { get; set; }`

Gets or sets endpoint behavior, sharing the existing animation endpoint contract.

<a id="member-9e72855a3b34"></a>
### Step

`public System.Double Step { get; set; }`

Gets or sets the authoring time-step hint, initially approximately one thirtieth second.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Int32 AddAnimationTrack(System.Int32 atPosition = -1)` | Adds a timeline controlling a relative AnimationPlayer by named clip keys. |
| `public System.Int32 AddAudioTrack(System.Int32 atPosition = -1)` | Adds a node-only audio cue track targeting a player or spatial emitter. |
| `public System.Int32 AddBezierTrack<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> property, System.Int32 atPosition = -1)` | Adds a scalar Bézier property track with an immutable float/double descriptor. |
| `public System.Void AddMarker(System.String name, System.Double time)` | Adds or moves a named marker, replacing any marker at approximately the same time and resetting its color. |
| `public System.Int32 AddMethodTrack<TOwner>(System.Int32 atPosition = -1)` | Adds an event track whose keys use exact typed callback and argument payloads. |
| `public System.Int32 AddTrack<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> property, System.Int32 atPosition = -1, Func<TValue, TValue, System.Double, TValue> interpolate = null)` | Adds a property track with an immutable typed descriptor and optional interpolation override. |
| `public System.String AnimationTrackGetKeyAnimation(System.Int32 track, System.Int32 key)` | Returns the exact child-animation name at a key. |
| `public System.Int32 AnimationTrackInsertKey(System.Int32 track, System.Double time, System.String animation)` | Inserts or replaces a named child-animation key; [stop] stops controlled playback. |
| `public System.Void AnimationTrackSetKeyAnimation(System.Int32 track, System.Int32 key, System.String animation)` | Replaces the exact child-animation name at a key. |
| `public System.Double AudioTrackGetKeyEndOffset(System.Int32 track, System.Int32 key)` | Returns end trim seconds. |
| `public System.Double AudioTrackGetKeyStartOffset(System.Int32 track, System.Int32 key)` | Returns start trim seconds. |
| `public Electron2D.AudioStream AudioTrackGetKeyStream(System.Int32 track, System.Int32 key)` | Returns a key's borrowed source. |
| `public System.Int32 AudioTrackInsertKey(System.Int32 track, System.Double time, Electron2D.AudioStream stream, System.Double startOffset = 0, System.Double endOffset = 0)` | Inserts or replaces a typed audio key, clamping negative trims to zero. |
| `public System.Boolean AudioTrackIsUseBlend(System.Int32 track)` | Returns whether clip weight affects cue volume. |
| `public System.Void AudioTrackSetKeyEndOffset(System.Int32 track, System.Int32 key, System.Double offset)` | Replaces finite end trim, clamping negative values. |
| `public System.Void AudioTrackSetKeyStartOffset(System.Int32 track, System.Int32 key, System.Double offset)` | Replaces finite start trim, clamping negative values. |
| `public System.Void AudioTrackSetKeyStream(System.Int32 track, System.Int32 key, Electron2D.AudioStream stream)` | Replaces a key's borrowed source. |
| `public System.Void AudioTrackSetUseBlend(System.Int32 track, System.Boolean enable)` | Sets whether clip weight affects cue volume. |
| `public Electron2D.Vector2 BezierTrackGetKeyInHandle(System.Int32 track, System.Int32 key)` | Returns the incoming key control offset. |
| `public Electron2D.Vector2 BezierTrackGetKeyOutHandle(System.Int32 track, System.Int32 key)` | Returns the outgoing key control offset. |
| `public System.Double BezierTrackGetKeyValue(System.Int32 track, System.Int32 key)` | Returns a scalar Bézier key value. |
| `public System.Int32 BezierTrackInsertKey(System.Int32 track, System.Double time, System.Double value, Electron2D.Vector2 inHandle = default, Electron2D.Vector2 outHandle = default)` | Inserts/replaces a scalar Bézier key, clamping incoming/outgoing handle time signs. |
| `public System.Double BezierTrackInterpolate(System.Int32 track, System.Double time)` | Samples scalar Bézier time/value geometry; no seam interpolation is performed. |
| `public System.Void BezierTrackSetKeyInHandle(System.Int32 track, System.Int32 key, Electron2D.Vector2 inHandle, System.Single balancedValueTimeRatio = 1f)` | Sets an incoming handle, clamping positive time offset to zero. |
| `public System.Void BezierTrackSetKeyOutHandle(System.Int32 track, System.Int32 key, Electron2D.Vector2 outHandle, System.Single balancedValueTimeRatio = 1f)` | Sets an outgoing handle, clamping negative time offset to zero. |
| `public System.Void BezierTrackSetKeyValue(System.Int32 track, System.Int32 key, System.Double value)` | Replaces a finite scalar Bézier value. |
| `public System.Void Clear()` | Removes tracks and restores length and loop defaults, retaining markers and the step hint. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)` | Copies derived stored state into a duplicate or copy target. |
| `public System.Void CopyTrack(System.Int32 track, Electron2D.Animation toAnimation)` | Copies one track and all keys into another animation without sharing its key container. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Creates a fresh default instance used as the target of duplication. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Int32 FindTrack(System.String path, Electron2D.Animation.TrackType type = Value)` | Returns the first track with this exact relative path and kind, or minus one. |
| `public System.String GetMarkerAtTime(System.Double time)` | Returns the first marker approximately at a time, or empty. |
| `public Electron2D.Color GetMarkerColor(System.String name)` | Returns the marker color. |
| `public System.String[] GetMarkerNames()` | Returns marker names sorted by time and then ordinal name. |
| `public System.Double GetMarkerTime(System.String name)` | Returns a marker time, or minus one when absent. |
| `public System.String GetNextMarker(System.Double time)` | Returns the nearest marker strictly after the time, or empty. |
| `public System.String GetPrevMarker(System.Double time)` | Returns the nearest marker at or before the time, or empty. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public System.Int32 GetTrackCount()` | Returns the number of authored timeline tracks. |
| `public System.Boolean HasMarker(System.String name)` | Tests marker membership. |
| `public System.String MethodTrackGetName(System.Int32 track, System.Int32 key)` | Returns a method key's diagnostic name. |
| `public TArguments MethodTrackGetParams<TArguments>(System.Int32 track, System.Int32 key)` | Returns a method key's exact typed argument payload. |
| `public System.Void RemoveMarker(System.String name)` | Removes a marker if it exists. |
| `public System.Void RemoveTrack(System.Int32 track)` | Removes one track and emits Changed. |
| `public System.Void SetMarkerColor(System.String name, Electron2D.Color color)` | Changes a marker's finite color. |
| `public System.Int32 TrackFindKey(System.Int32 track, System.Double time, Electron2D.Animation.FindMode findMode = Approx, System.Boolean limit = false, System.Boolean backward = false)` | Finds a preceding or following key; limit restricts candidates to the animation duration. |
| `public System.Boolean TrackGetInterpolationLoopWrap(System.Int32 track)` | Returns whether looping interpolation connects the last and first key. |
| `public Electron2D.Animation.InterpolationType TrackGetInterpolationType(System.Int32 track)` | Returns the interpolation mode. |
| `public System.Int32 TrackGetKeyCount(System.Int32 track)` | Returns the number of keys. |
| `public System.Double TrackGetKeyTime(System.Int32 track, System.Int32 key)` | Returns a key time in seconds. |
| `public System.Double TrackGetKeyTransition(System.Int32 track, System.Int32 key)` | Returns a key's easing transition, initially one. |
| `public TValue TrackGetKeyValue<TValue>(System.Int32 track, System.Int32 key)` | Reads an exact typed key; method keys also admit their receiver-key base type. |
| `public System.String TrackGetPath(System.Int32 track)` | Returns a track's relative node path, including an optional property suffix. |
| `public Electron2D.Animation.TrackType TrackGetType(System.Int32 track)` | Returns the track kind. |
| `public System.Int32 TrackInsertKey<TValue>(System.Int32 track, System.Double time, TValue value, System.Double transition = 1)` | Inserts a typed key, replacing an existing exactly equal time. |
| `public System.Int32 TrackInsertKey<TOwner, TArguments>(System.Int32 track, System.Double time, AnimationMethodKey<TOwner, TArguments> value, System.Double transition = 1)` | Inserts a callback key without erasing its exact payload signature. |
| `public System.Boolean TrackIsCompressed(System.Int32 track)` | Returns whether this track uses compressed storage; value-property tracks never do. |
| `public System.Boolean TrackIsEnabled(System.Int32 track)` | Returns whether a track participates in property or callback evaluation. |
| `public System.Boolean TrackIsImported(System.Int32 track)` | Returns imported authoring metadata. |
| `public System.Void TrackMoveDown(System.Int32 track)` | Moves a track toward the beginning by one position. |
| `public System.Void TrackMoveTo(System.Int32 track, System.Int32 toIndex)` | Moves a track before the original insertion boundary; the count appends. |
| `public System.Void TrackMoveUp(System.Int32 track)` | Moves a track toward the end by one position. |
| `public System.Void TrackRemoveKey(System.Int32 track, System.Int32 key)` | Removes a key by index. |
| `public System.Void TrackRemoveKeyAtTime(System.Int32 track, System.Double time)` | Removes a key at an approximately matching time, if present. |
| `public System.Void TrackSetEnabled(System.Int32 track, System.Boolean enabled)` | Enables or disables a track. |
| `public System.Void TrackSetImported(System.Int32 track, System.Boolean imported)` | Sets imported metadata without emitting Changed. |
| `public System.Void TrackSetInterpolationLoopWrap(System.Int32 track, System.Boolean interpolation)` | Sets interpolation across the linear-loop seam. |
| `public System.Void TrackSetInterpolationType(System.Int32 track, Electron2D.Animation.InterpolationType interpolation)` | Sets interpolation, rejecting unsupported value/mode combinations before mutation. |
| `public System.Void TrackSetKeyTime(System.Int32 track, System.Int32 key, System.Double time)` | Moves a key in sorted time order; an exactly equal destination time is replaced. |
| `public System.Void TrackSetKeyTransition(System.Int32 track, System.Int32 key, System.Double transition)` | Sets finite easing; zero holds the source key until the destination time. |
| `public System.Void TrackSetKeyValue<TValue>(System.Int32 track, System.Int32 key, TValue value)` | Replaces a typed key value. |
| `public System.Void TrackSetKeyValue<TOwner, TArguments>(System.Int32 track, System.Int32 key, AnimationMethodKey<TOwner, TArguments> value)` | Replaces a callback key while retaining exact receiver/payload typing. |
| `public System.Void TrackSetPath(System.Int32 track, System.String path)` | Sets a relative node path; property tracks allow their exact descriptor suffix, and method/child-animation tracks require a node-only path. |
| `public System.Void TrackSwap(System.Int32 track, System.Int32 withTrack)` | Swaps two track positions. |
| `public Electron2D.Animation.UpdateMode ValueTrackGetUpdateMode(System.Int32 track)` | Returns the value update mode. |
| `public TValue ValueTrackInterpolate<TValue>(System.Int32 track, System.Double time, System.Boolean backward = false)` | Samples the typed value at a finite time; throws if no keys exist. |
| `public System.Void ValueTrackSetUpdateMode(System.Int32 track, Electron2D.Animation.UpdateMode mode)` | Sets continuous interpolation or discrete key holding. |

## Method Descriptions

<a id="member-b068fc233902"></a>
### AddAnimationTrack

`public System.Int32 AddAnimationTrack(System.Int32 atPosition = -1)`

Adds a timeline controlling a relative AnimationPlayer by named clip keys.

atPosition: Insertion index, or minus one to append.

Returns: The inserted track index.

System.ObjectDisposedException: The animation is disposed.

System.ArgumentOutOfRangeException: The insertion index is invalid.

<a id="member-72ad856c5006"></a>
### AddAudioTrack

`public System.Int32 AddAudioTrack(System.Int32 atPosition = -1)`

Adds a node-only audio cue track targeting a player or spatial emitter.

atPosition: Insertion index or minus one to append.

Returns: The inserted index.

System.ObjectDisposedException: The animation or a borrowed source is disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

<a id="member-a3735a66a664"></a>
### AddBezierTrack

`public System.Int32 AddBezierTrack<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> property, System.Int32 atPosition = -1)`

Adds a scalar Bézier property track with an immutable float/double descriptor.

TOwner: The target Node type.

TValue: Exactly float or double.

property: A writable scalar descriptor.

atPosition: Insertion index or minus one to append.

Returns: The inserted index.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.ArgumentException: The descriptor is read-only.

System.ArgumentNullException: The descriptor is null.

System.NotSupportedException: The descriptor value type is neither float nor double.

<a id="member-8fdeffe75cdf"></a>
### AddMarker

`public System.Void AddMarker(System.String name, System.Double time)`

Adds or moves a named marker, replacing any marker at approximately the same time and resetting its color.

name: The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.

time: A finite key or marker time in seconds.

<a id="member-a06e4fbfd21f"></a>
### AddMethodTrack

`public System.Int32 AddMethodTrack<TOwner>(System.Int32 atPosition = -1)`

Adds an event track whose keys use exact typed callback and argument payloads.

TOwner: The target Node type.

atPosition: Insertion index or minus one to append.

Returns: The inserted index.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

<a id="member-7fad26673b4e"></a>
### AddTrack

`public System.Int32 AddTrack<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> property, System.Int32 atPosition = -1, Func<TValue, TValue, System.Double, TValue> interpolate = null)`

Adds a property track with an immutable typed descriptor and optional interpolation override.

TOwner: The node type accepted by the descriptor.

TValue: The exact key value type.

property: A writable descriptor; shared safely by resource copies.

atPosition: Insertion index, or minus one to append.

interpolate: Optional typed linear interpolator; otherwise the engine math profile is used.

Returns: The inserted track index. Unsupported value types initially use nearest interpolation.

<a id="member-787ce33dc1c7"></a>
### AnimationTrackGetKeyAnimation

`public System.String AnimationTrackGetKeyAnimation(System.Int32 track, System.Int32 key)`

Returns the exact child-animation name at a key.

track: An existing animation track.

key: An existing key index.

Returns: The clip name or stop sentinel.

System.ObjectDisposedException: The animation is disposed.

System.ArgumentOutOfRangeException: An index is invalid.

System.InvalidOperationException: The track kind differs.

<a id="member-aa3eb087b748"></a>
### AnimationTrackInsertKey

`public System.Int32 AnimationTrackInsertKey(System.Int32 track, System.Double time, System.String animation)`

Inserts or replaces a named child-animation key; [stop] stops controlled playback.

track: An existing animation track.

time: Finite seconds.

animation: The exact nonnull clip name or stop sentinel.

Returns: The sorted key index.

System.ObjectDisposedException: The animation is disposed.

System.ArgumentOutOfRangeException: An index or time is invalid.

System.ArgumentNullException: The name is null.

System.InvalidOperationException: The track kind differs.

<a id="member-86fc7c5fd831"></a>
### AnimationTrackSetKeyAnimation

`public System.Void AnimationTrackSetKeyAnimation(System.Int32 track, System.Int32 key, System.String animation)`

Replaces the exact child-animation name at a key.

track: An existing animation track.

key: An existing key index.

animation: The nonnull clip name or stop sentinel.

System.ObjectDisposedException: The animation is disposed.

System.ArgumentOutOfRangeException: An index is invalid.

System.ArgumentNullException: The name is null.

System.InvalidOperationException: The track kind differs.

<a id="member-5523f6c2395a"></a>
### AudioTrackGetKeyEndOffset

`public System.Double AudioTrackGetKeyEndOffset(System.Int32 track, System.Int32 key)`

Returns end trim seconds.

track: Track index.

key: Key index.

Returns: Nonnegative seconds.

System.ObjectDisposedException: The animation or a borrowed source is disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs.

<a id="member-d06bd347ace7"></a>
### AudioTrackGetKeyStartOffset

`public System.Double AudioTrackGetKeyStartOffset(System.Int32 track, System.Int32 key)`

Returns start trim seconds.

track: Track index.

key: Key index.

Returns: Nonnegative seconds.

System.ObjectDisposedException: The animation or a borrowed source is disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs.

<a id="member-d4390c2d3240"></a>
### AudioTrackGetKeyStream

`public Electron2D.AudioStream AudioTrackGetKeyStream(System.Int32 track, System.Int32 key)`

Returns a key's borrowed source.

track: Track index.

key: Key index.

Returns: Source or null.

System.ObjectDisposedException: The animation or a borrowed source is disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs.

<a id="member-4be4949b481f"></a>
### AudioTrackInsertKey

`public System.Int32 AudioTrackInsertKey(System.Int32 track, System.Double time, Electron2D.AudioStream stream, System.Double startOffset = 0, System.Double endOffset = 0)`

Inserts or replaces a typed audio key, clamping negative trims to zero.

track: Existing audio track.

time: Finite seconds.

stream: Borrowed source or null.

startOffset: Finite start trim.

endOffset: Finite end trim.

Returns: Sorted key index.

System.ObjectDisposedException: The animation or a borrowed source is disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs.

<a id="member-1dabfd59b69d"></a>
### AudioTrackIsUseBlend

`public System.Boolean AudioTrackIsUseBlend(System.Int32 track)`

Returns whether clip weight affects cue volume.

track: Audio track index.

Returns: True initially.

System.ObjectDisposedException: The animation or a borrowed source is disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs.

<a id="member-136ca1e6adb4"></a>
### AudioTrackSetKeyEndOffset

`public System.Void AudioTrackSetKeyEndOffset(System.Int32 track, System.Int32 key, System.Double offset)`

Replaces finite end trim, clamping negative values.

track: Track index.

key: Key index.

offset: Finite seconds.

System.ObjectDisposedException: The animation or a borrowed source is disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs.

<a id="member-b34fb9d063a9"></a>
### AudioTrackSetKeyStartOffset

`public System.Void AudioTrackSetKeyStartOffset(System.Int32 track, System.Int32 key, System.Double offset)`

Replaces finite start trim, clamping negative values.

track: Track index.

key: Key index.

offset: Finite seconds.

System.ObjectDisposedException: The animation or a borrowed source is disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs.

<a id="member-dc3857b84a99"></a>
### AudioTrackSetKeyStream

`public System.Void AudioTrackSetKeyStream(System.Int32 track, System.Int32 key, Electron2D.AudioStream stream)`

Replaces a key's borrowed source.

track: Track index.

key: Key index.

stream: Source or null.

System.ObjectDisposedException: The animation or a borrowed source is disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs.

<a id="member-847a845128ed"></a>
### AudioTrackSetUseBlend

`public System.Void AudioTrackSetUseBlend(System.Int32 track, System.Boolean enable)`

Sets whether clip weight affects cue volume.

track: Audio track index.

enable: Whether to scale gain.

System.ObjectDisposedException: The animation or a borrowed source is disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs.

<a id="member-7f9ddfbe1935"></a>
### BezierTrackGetKeyInHandle

`public Electron2D.Vector2 BezierTrackGetKeyInHandle(System.Int32 track, System.Int32 key)`

Returns the incoming key control offset.

track: The track index.

key: The key index.

Returns: The incoming offset.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs from the requested operation.

<a id="member-912e3a111215"></a>
### BezierTrackGetKeyOutHandle

`public Electron2D.Vector2 BezierTrackGetKeyOutHandle(System.Int32 track, System.Int32 key)`

Returns the outgoing key control offset.

track: The track index.

key: The key index.

Returns: The outgoing offset.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs from the requested operation.

<a id="member-6b683ef9abfb"></a>
### BezierTrackGetKeyValue

`public System.Double BezierTrackGetKeyValue(System.Int32 track, System.Int32 key)`

Returns a scalar Bézier key value.

track: The track index.

key: The key index.

Returns: The scalar value.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs from the requested operation.

<a id="member-ac81c6f5c7ca"></a>
### BezierTrackInsertKey

`public System.Int32 BezierTrackInsertKey(System.Int32 track, System.Double time, System.Double value, Electron2D.Vector2 inHandle = default, Electron2D.Vector2 outHandle = default)`

Inserts/replaces a scalar Bézier key, clamping incoming/outgoing handle time signs.

track: An existing Bézier track.

time: Finite seconds.

value: The finite scalar value.

inHandle: Incoming offset.

outHandle: Outgoing offset.

Returns: The sorted key index.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs from the requested operation.

<a id="member-3aff36a4f8a3"></a>
### BezierTrackInterpolate

`public System.Double BezierTrackInterpolate(System.Int32 track, System.Double time)`

Samples scalar Bézier time/value geometry; no seam interpolation is performed.

track: The track index.

time: Finite seconds.

Returns: The scalar value, or zero when no key lies within the clip length.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs from the requested operation.

<a id="member-2401dda06a66"></a>
### BezierTrackSetKeyInHandle

`public System.Void BezierTrackSetKeyInHandle(System.Int32 track, System.Int32 key, Electron2D.Vector2 inHandle, System.Single balancedValueTimeRatio = 1f)`

Sets an incoming handle, clamping positive time offset to zero.

track: The track index.

key: The key index.

inHandle: The incoming offset.

balancedValueTimeRatio: Finite authoring balance metadata; runtime handles are free and are not editor-balanced.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs from the requested operation.

<a id="member-ad116ec4e232"></a>
### BezierTrackSetKeyOutHandle

`public System.Void BezierTrackSetKeyOutHandle(System.Int32 track, System.Int32 key, Electron2D.Vector2 outHandle, System.Single balancedValueTimeRatio = 1f)`

Sets an outgoing handle, clamping negative time offset to zero.

track: The track index.

key: The key index.

outHandle: The outgoing offset.

balancedValueTimeRatio: Finite authoring balance metadata; runtime handles are free and are not editor-balanced.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs from the requested operation.

<a id="member-119611a6831e"></a>
### BezierTrackSetKeyValue

`public System.Void BezierTrackSetKeyValue(System.Int32 track, System.Int32 key, System.Double value)`

Replaces a finite scalar Bézier value.

track: The track index.

key: The key index.

value: The scalar value.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs from the requested operation.

<a id="member-e061a163bb4e"></a>
### Clear

`public System.Void Clear()`

Removes tracks and restores length and loop defaults, retaining markers and the step hint.

<a id="member-1bdc7701d8df"></a>
### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`

Copies derived stored state into a duplicate or copy target.

target: A live resource with the exact same runtime type.

deep: Whether typed collection containers should be cloned recursively.

subresourceMode: The nested-resource policy for this copy.

duplicateSubresource: A graph-preserving function that returns the correct shared or duplicated instance for a nested resource. Pass every nested resource through this function when deep is true.

forceDuplicateSubresource: A graph-preserving function that duplicates a nested resource even when the current policy would share it. Use it for typed properties whose contract requires duplication; assign the original reference directly for properties whose contract forbids duplication.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Derived implementations must copy all stored custom state and call the base implementation only when they intentionally want its validation. Assigning the original nested-resource reference directly expresses a never-duplicate property.

System.NotSupportedException: A derived resource has not explicitly implemented custom-state copying.

<a id="member-a5e6ba4a9ffe"></a>
### CopyTrack

`public System.Void CopyTrack(System.Int32 track, Electron2D.Animation toAnimation)`

Copies one track and all keys into another animation without sharing its key container.

toAnimation: The live destination animation, whose track list receives an independent copy.

track: The zero-based existing track index.

<a id="member-fce4d2925d6e"></a>
### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Creates a fresh default instance used as the target of duplication.

Returns: A live resource of the exact same runtime type with empty path and scene ID.

Remarks: The base implementation supports only an exact Electron2D.Resource instance. Every derived class must override this method, even when it adds no state, so duplication support is explicit.

System.NotSupportedException: The runtime type derives from Electron2D.Resource and has not overridden this method.

<a id="member-39cb9ddc07ef"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-d5edce40a566"></a>
### FindTrack

`public System.Int32 FindTrack(System.String path, Electron2D.Animation.TrackType type = Value)`

Returns the first track with this exact relative path and kind, or minus one.

path: The relative target path.

type: The executable track kind.

<a id="member-195c97d4fb6f"></a>
### GetMarkerAtTime

`public System.String GetMarkerAtTime(System.Double time)`

Returns the first marker approximately at a time, or empty.

time: A finite key or marker time in seconds.

<a id="member-fec6b752a312"></a>
### GetMarkerColor

`public Electron2D.Color GetMarkerColor(System.String name)`

Returns the marker color.

name: The exact ordinal name.

<a id="member-0c07c23881c5"></a>
### GetMarkerNames

`public System.String[] GetMarkerNames()`

Returns marker names sorted by time and then ordinal name.

<a id="member-a6ba16e6919d"></a>
### GetMarkerTime

`public System.Double GetMarkerTime(System.String name)`

Returns a marker time, or minus one when absent.

name: The exact ordinal name.

<a id="member-ddb3abb36761"></a>
### GetNextMarker

`public System.String GetNextMarker(System.Double time)`

Returns the nearest marker strictly after the time, or empty.

time: The finite time in seconds.

<a id="member-3874fe65cf1f"></a>
### GetPrevMarker

`public System.String GetPrevMarker(System.Double time)`

Returns the nearest marker at or before the time, or empty.

time: The finite time in seconds.

<a id="member-31a7585f1770"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-a19d90c45488"></a>
### GetTrackCount

`public System.Int32 GetTrackCount()`

Returns the number of authored timeline tracks.

<a id="member-56e2bb9a170a"></a>
### HasMarker

`public System.Boolean HasMarker(System.String name)`

Tests marker membership.

name: The exact ordinal name.

<a id="member-cc27a049c592"></a>
### MethodTrackGetName

`public System.String MethodTrackGetName(System.Int32 track, System.Int32 key)`

Returns a method key's diagnostic name.

track: The method track index.

key: The key index.

Returns: The exact name.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs from the requested operation.

<a id="member-08ce616cebc5"></a>
### MethodTrackGetParams

`public TArguments MethodTrackGetParams<TArguments>(System.Int32 track, System.Int32 key)`

Returns a method key's exact typed argument payload.

TArguments: The exact declared payload type.

track: The method track index.

key: The key index.

Returns: The typed payload; custom references remain borrowed.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidOperationException: The track kind differs from the requested operation.

System.InvalidCastException: The track receiver or key payload type differs.

<a id="member-7dd9ed08f24d"></a>
### RemoveMarker

`public System.Void RemoveMarker(System.String name)`

Removes a marker if it exists.

name: The exact ordinal name.

<a id="member-585d4195f0bc"></a>
### RemoveTrack

`public System.Void RemoveTrack(System.Int32 track)`

Removes one track and emits Changed.

track: The zero-based existing track index.

<a id="member-0567d086dff7"></a>
### SetMarkerColor

`public System.Void SetMarkerColor(System.String name, Electron2D.Color color)`

Changes a marker's finite color.

color: A color with finite components.

name: The exact ordinal name.

<a id="member-0ae3da6dc53e"></a>
### TrackFindKey

`public System.Int32 TrackFindKey(System.Int32 track, System.Double time, Electron2D.Animation.FindMode findMode = Approx, System.Boolean limit = false, System.Boolean backward = false)`

Finds a preceding or following key; limit restricts candidates to the animation duration.

findMode: The key matching rule.

limit: Whether to exclude keys outside the resource duration.

backward: Whether to sample or search in reverse direction.

track: The zero-based existing track index.

time: The finite time in seconds.

<a id="member-fe339faebab8"></a>
### TrackGetInterpolationLoopWrap

`public System.Boolean TrackGetInterpolationLoopWrap(System.Int32 track)`

Returns whether looping interpolation connects the last and first key.

track: The zero-based existing track index.

<a id="member-ce10f1740033"></a>
### TrackGetInterpolationType

`public Electron2D.Animation.InterpolationType TrackGetInterpolationType(System.Int32 track)`

Returns the interpolation mode.

track: The zero-based existing track index.

<a id="member-d39e9acbec99"></a>
### TrackGetKeyCount

`public System.Int32 TrackGetKeyCount(System.Int32 track)`

Returns the number of keys.

track: The zero-based existing track index.

<a id="member-52890f4b004c"></a>
### TrackGetKeyTime

`public System.Double TrackGetKeyTime(System.Int32 track, System.Int32 key)`

Returns a key time in seconds.

key: The zero-based existing key index.

track: The zero-based existing track index.

<a id="member-f166761a1f64"></a>
### TrackGetKeyTransition

`public System.Double TrackGetKeyTransition(System.Int32 track, System.Int32 key)`

Returns a key's easing transition, initially one.

track: The zero-based existing track index.

key: The zero-based existing key index.

<a id="member-2b43064dd1fc"></a>
### TrackGetKeyValue

`public TValue TrackGetKeyValue<TValue>(System.Int32 track, System.Int32 key)`

Reads an exact typed key; method keys also admit their receiver-key base type.

TValue: The declared value type, actual concrete method-key type or receiver-key base.

key: The zero-based existing key index.

track: The zero-based existing track index.

<a id="member-c444bc55adb2"></a>
### TrackGetPath

`public System.String TrackGetPath(System.Int32 track)`

Returns a track's relative node path, including an optional property suffix.

track: The zero-based existing track index.

<a id="member-5a80bca9b90e"></a>
### TrackGetType

`public Electron2D.Animation.TrackType TrackGetType(System.Int32 track)`

Returns the track kind.

track: The zero-based existing track index.

<a id="member-88102a440dd5"></a>
### TrackInsertKey

`public System.Int32 TrackInsertKey<TValue>(System.Int32 track, System.Double time, TValue value, System.Double transition = 1)`

Inserts a typed key, replacing an existing exactly equal time.

TValue: The exact declared track value type.

time: A finite key or marker time in seconds.

value: The exact declared typed key value.

track: The zero-based existing track index.

transition: The typed argument for this operation, using the defaults described above.

<a id="member-1202b44e1d6a"></a>
### TrackInsertKey

`public System.Int32 TrackInsertKey<TOwner, TArguments>(System.Int32 track, System.Double time, AnimationMethodKey<TOwner, TArguments> value, System.Double transition = 1)`

Inserts a callback key without erasing its exact payload signature.

TOwner: The track receiver type.

TArguments: The key payload type.

track: An existing method track.

time: Finite key seconds.

value: The typed callback key.

transition: Finite stored easing metadata.

Returns: The sorted key index.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidCastException: The track receiver or key payload type differs.

<a id="member-38b6079b2140"></a>
### TrackIsCompressed

`public System.Boolean TrackIsCompressed(System.Int32 track)`

Returns whether this track uses compressed storage; value-property tracks never do.

track: The zero-based existing track index.

<a id="member-e3caae8eea28"></a>
### TrackIsEnabled

`public System.Boolean TrackIsEnabled(System.Int32 track)`

Returns whether a track participates in property or callback evaluation.

track: The zero-based existing track index.

<a id="member-4167b0be4b3d"></a>
### TrackIsImported

`public System.Boolean TrackIsImported(System.Int32 track)`

Returns imported authoring metadata.

track: The zero-based existing track index.

<a id="member-dcf36fbad5e6"></a>
### TrackMoveDown

`public System.Void TrackMoveDown(System.Int32 track)`

Moves a track toward the beginning by one position.

track: The zero-based existing track index.

<a id="member-be25e893a7cd"></a>
### TrackMoveTo

`public System.Void TrackMoveTo(System.Int32 track, System.Int32 toIndex)`

Moves a track before the original insertion boundary; the count appends.

toIndex: The insertion boundary in the original track list, including its count.

track: The zero-based existing track index.

<a id="member-bd4873e47f6b"></a>
### TrackMoveUp

`public System.Void TrackMoveUp(System.Int32 track)`

Moves a track toward the end by one position.

track: The zero-based existing track index.

<a id="member-63a0e6721192"></a>
### TrackRemoveKey

`public System.Void TrackRemoveKey(System.Int32 track, System.Int32 key)`

Removes a key by index.

track: The zero-based existing track index.

key: The zero-based existing key index.

<a id="member-9b18b87d5210"></a>
### TrackRemoveKeyAtTime

`public System.Void TrackRemoveKeyAtTime(System.Int32 track, System.Double time)`

Removes a key at an approximately matching time, if present.

track: The zero-based existing track index.

time: The finite time in seconds.

<a id="member-59596b22f4d4"></a>
### TrackSetEnabled

`public System.Void TrackSetEnabled(System.Int32 track, System.Boolean enabled)`

Enables or disables a track.

enabled: Whether this track contributes values.

track: The zero-based existing track index.

<a id="member-1e8a8ccd039d"></a>
### TrackSetImported

`public System.Void TrackSetImported(System.Int32 track, System.Boolean imported)`

Sets imported metadata without emitting Changed.

imported: The authoring-only imported flag.

track: The zero-based existing track index.

<a id="member-cf9d1112ef38"></a>
### TrackSetInterpolationLoopWrap

`public System.Void TrackSetInterpolationLoopWrap(System.Int32 track, System.Boolean interpolation)`

Sets interpolation across the linear-loop seam.

track: The zero-based existing track index.

interpolation: The typed argument for this operation, using the defaults described above.

<a id="member-4d153603e4f6"></a>
### TrackSetInterpolationType

`public System.Void TrackSetInterpolationType(System.Int32 track, Electron2D.Animation.InterpolationType interpolation)`

Sets interpolation, rejecting unsupported value/mode combinations before mutation.

interpolation: The interpolation mode or loop-wrap flag.

track: The zero-based existing track index.

<a id="member-9d4f3a297020"></a>
### TrackSetKeyTime

`public System.Void TrackSetKeyTime(System.Int32 track, System.Int32 key, System.Double time)`

Moves a key in sorted time order; an exactly equal destination time is replaced.

time: A finite key or marker time in seconds.

track: The zero-based existing track index.

key: The zero-based existing key index.

<a id="member-0b4fa652bd24"></a>
### TrackSetKeyTransition

`public System.Void TrackSetKeyTransition(System.Int32 track, System.Int32 key, System.Double transition)`

Sets finite easing; zero holds the source key until the destination time.

transition: The finite easing exponent; one is linear.

track: The zero-based existing track index.

key: The zero-based existing key index.

<a id="member-f4444e754b49"></a>
### TrackSetKeyValue

`public System.Void TrackSetKeyValue<TValue>(System.Int32 track, System.Int32 key, TValue value)`

Replaces a typed key value.

TValue: The exact declared track value type.

track: The zero-based existing track index.

key: The zero-based existing key index.

value: The typed argument for this operation, using the defaults described above.

<a id="member-84098b9318e0"></a>
### TrackSetKeyValue

`public System.Void TrackSetKeyValue<TOwner, TArguments>(System.Int32 track, System.Int32 key, AnimationMethodKey<TOwner, TArguments> value)`

Replaces a callback key while retaining exact receiver/payload typing.

TOwner: The track receiver type.

TArguments: The key payload type.

track: An existing method track.

key: An existing key index.

value: The typed callback key.

System.ObjectDisposedException: The animation has been disposed.

System.ArgumentOutOfRangeException: An index is invalid or a numeric argument is nonfinite.

System.InvalidCastException: The track receiver or key payload type differs.

<a id="member-00307ff407bb"></a>
### TrackSetPath

`public System.Void TrackSetPath(System.Int32 track, System.String path)`

Sets a relative node path; property tracks allow their exact descriptor suffix, and method/child-animation tracks require a node-only path.

track: The zero-based existing track index.

path: The relative node/property path.

<a id="member-52c42ffa58fe"></a>
### TrackSwap

`public System.Void TrackSwap(System.Int32 track, System.Int32 withTrack)`

Swaps two track positions.

withTrack: The second zero-based track index.

track: The zero-based existing track index.

<a id="member-bde1481dce8e"></a>
### ValueTrackGetUpdateMode

`public Electron2D.Animation.UpdateMode ValueTrackGetUpdateMode(System.Int32 track)`

Returns the value update mode.

track: The zero-based existing track index.

<a id="member-fcf4b563dab9"></a>
### ValueTrackInterpolate

`public TValue ValueTrackInterpolate<TValue>(System.Int32 track, System.Double time, System.Boolean backward = false)`

Samples the typed value at a finite time; throws if no keys exist.

TValue: The exact declared track value type.

track: The zero-based existing track index.

time: A finite key or marker time in seconds.

backward: Whether to sample or search in reverse direction.

<a id="member-515a3e8804d3"></a>
### ValueTrackSetUpdateMode

`public System.Void ValueTrackSetUpdateMode(System.Int32 track, Electron2D.Animation.UpdateMode mode)`

Sets continuous interpolation or discrete key holding.

mode: The defined update or process mode.

track: The zero-based existing track index.


## Lifecycle, verification and limits

Resource keys do not own targets or callbacks. Animation containers copy independently; directly held arrays clone and direct Resource payloads use the graph deep-copy session, while custom nested mutable references remain borrowed. Player/mixer mutation obeys attached SceneTree owner affinity; failures/reentry abandon stale passes. Deferred accepted calls retain their typed payload until a safe point and skip disposed/deleting or moved targets. Prepared callback pool exhaustion throws; use PrepareMethodCallbacks between frames for the required burst.

[AnimationSpecialTrackTests](../../tests/Electron2D.Tests/AnimationSpecialTrackTests.cs) and the existing scene animation/blend/graph/action suites exercise the connected runtime. Special-track tests cover cubic geometry, mixed signatures/copies, filters/weights, loop/seek/section order, callback mutation/failure/disposal and capacity reuse, with zero managed bytes across 256 warmed scalar and 256 prepared deferred passes. Two Linux Wayland GPU and two compatibility hosts check five curve/color pixel poses and borrowed-resource cleanup. Cold preparation and callbacks may allocate; native/driver allocations, other platforms and human acceptance are unmeasured. State machines and disk/editor persistence retain their coverage triggers.

Nested animation tracks execute clip-name keys against borrowed child players, with latest-crossed-key ordering, child-length seek/loop rules, normal independent child clocks, update-only sampling and control-revision cleanup. [The complete contract and snippet](../components/scene-animation.md#nested-animation-tracks) explains callback/reentry/cycle rules, prepared direct/weighted caches and current native/headless evidence. AnimationNestedTrackTests covers 256 warmed recurring start/stop plus child property passes with zero managed bytes; two GPU and two compatibility Wayland hosts verify six actual poses and cleanup.

[Audio tracks](../components/scene-animation.md#audio-tracks) execute borrowed cue sources on prepared player/emitter/polyphonic transports, with offsets, weighted gain, sample/stream selection, pause/cleanup and per-trigger random choices. Native PCM and two GPU/two compatibility public emitter hosts establish current Linux execution; physical listening, driver internals and other platforms remain unverified.

Weighted graph evaluation now retires controlled nested players and audio cues when their binding no longer participates, including state stop/departed clips and nonblended audio. Playback ownership revisions protect user takeovers; already accepted deferred method calls keep their existing safe-point lifetime. [AnimationStateMachineTests](../../tests/Electron2D.Tests/AnimationStateMachineTests.cs) exercises this integration.
