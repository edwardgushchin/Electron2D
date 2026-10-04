# AnimationPlayer

Last updated: 2026-10-04

**Inherits:** [AnimationMixer](AnimationMixer.md)

- **Source:** [`src/Scene/Animation/AnimationPlayer.cs`](../../src/Scene/Animation/AnimationPlayer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Electron2D.AnimationPlayer`

## Description

Named clip playback, reverse, sections, queues, weighted transitions/capture and typed track effects.

The complete timing, copy/borrowing, validation, callback error/reentry and verification contract is on [Scene animation](../components/scene-animation.md), including [special tracks](../components/scene-animation.md#bézier-and-method-tracks). Author/edit on the scene owner thread; target nodes and authored resources remain borrowed. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the current implementation.

## Examples

Partial authoring snippet; existing scene/library variables are indicated where required.

```csharp
var player = new AnimationPlayer();
player.AddAnimationLibrary("", library); // Existing authored library.
root.AddChild(player); // Existing scene root and targets.
player.Play("clip");
player.Advance(.25);
player.Seek(.5, update: true, updateOnly: true); // Properties/curves, no method callbacks.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationPlayer()` | Creates an idle player with inherited automatic scheduling and deferred method dispatch. |

## Constructor Descriptions

<a id="member-5b1db5c26526"></a>
### .ctor

`public AnimationPlayer()`

Creates an idle player with inherited automatic scheduling and deferred method dispatch.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.String AssignedAnimation { get; set; }` | Gets or sets the selected animation; stopped assignment rewinds without playing, while an active assignment switches clips. |
| `public System.String Autoplay { get; set; }` | Gets or sets the name started on the ready notification; empty disables autoplay. |
| `public System.String CurrentAnimation { get; set; }` | Gets the playing animation name, or assigns a name to start it; empty or the stop sentinel defers stopping while attached. |
| `public System.Double CurrentAnimationLength { get;  }` | Gets the complete selected resource duration, or zero without a selection. |
| `public System.Double CurrentAnimationPosition { get;  }` | Gets the current timeline position in seconds. |
| `public System.Boolean PlaybackAutoCapture { get; set; }` | Gets or sets automatic capture before Play, initially true. |
| `public System.Double PlaybackAutoCaptureDuration { get; set; }` | Gets or sets capture duration; negative values derive it from the first or last capture keys. |
| `public Electron2D.Tween.EaseType PlaybackAutoCaptureEaseType { get; set; }` | Gets or sets automatic capture's easing, initially In. |
| `public Electron2D.Tween.TransitionType PlaybackAutoCaptureTransitionType { get; set; }` | Gets or sets automatic capture's curve, initially Linear. |
| `public System.Double PlaybackDefaultBlendTime { get; set; }` | Gets or sets the finite nonnegative default crossfade duration in seconds. |
| `public System.Double SpeedScale { get; set; }` | Gets or sets the finite signed playback multiplier; zero keeps playback enabled. |

## Property Descriptions

<a id="member-2f2940161d12"></a>
### AssignedAnimation

`public System.String AssignedAnimation { get; set; }`

Gets or sets the selected animation; stopped assignment rewinds without playing, while an active assignment switches clips.

<a id="member-d2370aa4add3"></a>
### Autoplay

`public System.String Autoplay { get; set; }`

Gets or sets the name started on the ready notification; empty disables autoplay.

<a id="member-2b2c29fcdd66"></a>
### CurrentAnimation

`public System.String CurrentAnimation { get; set; }`

Gets the playing animation name, or assigns a name to start it; empty or the stop sentinel defers stopping while attached.

<a id="member-039d244cb686"></a>
### CurrentAnimationLength

`public System.Double CurrentAnimationLength { get;  }`

Gets the complete selected resource duration, or zero without a selection.

<a id="member-47b7619cb884"></a>
### CurrentAnimationPosition

`public System.Double CurrentAnimationPosition { get;  }`

Gets the current timeline position in seconds.

<a id="member-502827406182"></a>
### PlaybackAutoCapture

`public System.Boolean PlaybackAutoCapture { get; set; }`

Gets or sets automatic capture before Play, initially true.

<a id="member-57a06fb7826b"></a>
### PlaybackAutoCaptureDuration

`public System.Double PlaybackAutoCaptureDuration { get; set; }`

Gets or sets capture duration; negative values derive it from the first or last capture keys.

<a id="member-99c64aad80ba"></a>
### PlaybackAutoCaptureEaseType

`public Electron2D.Tween.EaseType PlaybackAutoCaptureEaseType { get; set; }`

Gets or sets automatic capture's easing, initially In.

<a id="member-66aebcf7539e"></a>
### PlaybackAutoCaptureTransitionType

`public Electron2D.Tween.TransitionType PlaybackAutoCaptureTransitionType { get; set; }`

Gets or sets automatic capture's curve, initially Linear.

<a id="member-e810a59a6580"></a>
### PlaybackDefaultBlendTime

`public System.Double PlaybackDefaultBlendTime { get; set; }`

Gets or sets the finite nonnegative default crossfade duration in seconds.

<a id="member-3d1c0b6075f3"></a>
### SpeedScale

`public System.Double SpeedScale { get; set; }`

Gets or sets the finite signed playback multiplier; zero keeps playback enabled.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.String AnimationGetNext(System.String animationFrom)` | Returns the configured next name, or empty. |
| `public System.Void AnimationSetNext(System.String animationFrom, System.String animationTo)` | Sets the animation played after a finite animation; empty clears the transition. |
| `public System.Void ClearQueue()` | Clears queued animation names. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.Double GetBlendTime(System.String animationFrom, System.String animationTo)` | Returns an explicitly configured transition duration, or zero. |
| `public System.Double GetPlayingSpeed()` | Returns zero while paused, otherwise the signed effective speed. |
| `public Electron2D.AnimationMixer.AnimationCallbackModeProcess GetProcessCallback()` | Returns the automatic animation update phase. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public System.String[] GetQueue()` | Returns an independent array of queued names in playback order. |
| `public System.String GetRoot()` | Returns the animation root path. |
| `public System.Double GetSectionEndTime()` | Returns the effective section end. |
| `public System.Double GetSectionStartTime()` | Returns the effective section start. |
| `public System.Boolean HasSection()` | Returns whether either section boundary is explicit. |
| `public System.Boolean IsAnimationActive()` | Returns whether animation evaluation is active. |
| `public System.Boolean IsPlaying()` | Returns whether playback is enabled, including with zero speed. |
| `protected override System.Void OnNotification(System.Int32 what)` | Handles an engine notification delivered to this object. |
| `public System.Void Pause()` | Pauses while retaining selection/position; clears queued/captured work and stops still-controlled child players while keeping their values. |
| `public System.Void Play(System.String name = "", System.Double customBlend = -1, System.Double customSpeed = 1, System.Boolean fromEnd = false)` | Starts or resumes a selected timeline. Negative customBlend selects configured/default crossfade timing; zero switches immediately. |
| `public System.Void PlayBackwards(System.String name = "", System.Double customBlend = -1)` | Starts or resumes reverse playback from the end. |
| `public System.Void PlaySection(System.String name = "", System.Double startTime = -1, System.Double endTime = -1, System.Double customBlend = -1, System.Double customSpeed = 1, System.Boolean fromEnd = false)` | Plays a bounded timeline section; negative boundaries select the resource endpoints. |
| `public System.Void PlaySectionBackwards(System.String name = "", System.Double startTime = -1, System.Double endTime = -1, System.Double customBlend = -1)` | Plays a bounded section backwards. |
| `public System.Void PlaySectionWithMarkers(System.String name = "", System.String startMarker = "", System.String endMarker = "", System.Double customBlend = -1, System.Double customSpeed = 1, System.Boolean fromEnd = false)` | Plays a section using named marker times; empty or absent markers select endpoints. |
| `public System.Void PlaySectionWithMarkersBackwards(System.String name = "", System.String startMarker = "", System.String endMarker = "", System.Double customBlend = -1)` | Plays a marker-defined section backwards. |
| `public System.Void PlayWithCapture(System.String name = "", System.Double duration = -1, System.Double customBlend = -1, System.Double customSpeed = 1, System.Boolean fromEnd = false, Electron2D.Tween.TransitionType transitionType = Linear, Electron2D.Tween.EaseType easeType = In)` | Captures current target values and starts or resumes the selected clip. |
| `public System.Void Queue(System.String name)` | Queues an existing animation; starts immediately when no animation is playing. |
| `public System.Void ResetSection()` | Restores complete-resource playback boundaries. |
| `public System.Void Seek(System.Double seconds, System.Boolean update = false, System.Boolean updateOnly = false)` | Seeks within the current section; update applies values immediately without completion events. |
| `public System.Void SetBlendTime(System.String animationFrom, System.String animationTo, System.Double seconds)` | Sets a nonnegative named transition duration; zero removes the entry. |
| `public System.Void SetProcessCallback(Electron2D.AnimationMixer.AnimationCallbackModeProcess mode)` | Sets the automatic animation update phase. |
| `public System.Void SetRoot(System.String path)` | Sets the animation root path. |
| `public System.Void SetSection(System.Double startTime = -1, System.Double endTime = -1)` | Sets the current playback section, clamping position into it. |
| `public System.Void SetSectionWithMarkers(System.String startMarker = "", System.String endMarker = "")` | Sets the current section using markers. |
| `public System.Void Stop(System.Boolean keepState = false)` | Stops, resets position/speed and queued work, and stops still-controlled child players; keepState preserves target values. |

## Method Descriptions

<a id="member-f14d65d06687"></a>
### AnimationGetNext

`public System.String AnimationGetNext(System.String animationFrom)`

Returns the configured next name, or empty.

animationFrom: The typed argument for this operation, using the defaults described above.

<a id="member-87be7daf7e97"></a>
### AnimationSetNext

`public System.Void AnimationSetNext(System.String animationFrom, System.String animationTo)`

Sets the animation played after a finite animation; empty clears the transition.

animationFrom: The existing source animation.

animationTo: The existing next animation, or empty to remove its transition.

<a id="member-af66744efd4c"></a>
### ClearQueue

`public System.Void ClearQueue()`

Clears queued animation names.

<a id="member-ce659c076b95"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-267f128bfabc"></a>
### GetBlendTime

`public System.Double GetBlendTime(System.String animationFrom, System.String animationTo)`

Returns an explicitly configured transition duration, or zero.

animationFrom: The exact source name.

animationTo: The exact destination name.

Returns: The stored duration, excluding wildcard/default fallback.

<a id="member-2a7db01bdcd0"></a>
### GetPlayingSpeed

`public System.Double GetPlayingSpeed()`

Returns zero while paused, otherwise the signed effective speed.

<a id="member-e954ec4ca1b7"></a>
### GetProcessCallback

`public Electron2D.AnimationMixer.AnimationCallbackModeProcess GetProcessCallback()`

Returns the automatic animation update phase.

<a id="member-6231664a0888"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-abd4ffdc7d87"></a>
### GetQueue

`public System.String[] GetQueue()`

Returns an independent array of queued names in playback order.

<a id="member-28ee67a3149e"></a>
### GetRoot

`public System.String GetRoot()`

Returns the animation root path.

<a id="member-608db72a88d6"></a>
### GetSectionEndTime

`public System.Double GetSectionEndTime()`

Returns the effective section end.

<a id="member-48b2faa4feda"></a>
### GetSectionStartTime

`public System.Double GetSectionStartTime()`

Returns the effective section start.

<a id="member-60615085d62f"></a>
### HasSection

`public System.Boolean HasSection()`

Returns whether either section boundary is explicit.

<a id="member-ef1394ad3d1d"></a>
### IsAnimationActive

`public System.Boolean IsAnimationActive()`

Returns whether animation evaluation is active.

<a id="member-ad441bb6dc92"></a>
### IsPlaying

`public System.Boolean IsPlaying()`

Returns whether playback is enabled, including with zero speed.

<a id="member-046387b1eca0"></a>
### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`

Handles an engine notification delivered to this object.

what: The notification identifier.

Remarks: Derived overrides should call the base implementation unless they intentionally suppress inherited handling.

<a id="member-db6b1e05aa69"></a>
### Pause

`public System.Void Pause()`

Pauses while retaining selection/position; clears queued/captured work and stops still-controlled child players while keeping their values.

<a id="member-d88ad0972c25"></a>
### Play

`public System.Void Play(System.String name = "", System.Double customBlend = -1, System.Double customSpeed = 1, System.Boolean fromEnd = false)`

Starts or resumes a selected timeline. Negative customBlend selects configured/default crossfade timing; zero switches immediately.

name: The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.

customBlend: Finite crossfade duration; negative uses configured/default timing.

customSpeed: A finite signed playback multiplier.

fromEnd: Whether a new or completed selection starts at the section end.

<a id="member-e2d799833710"></a>
### PlayBackwards

`public System.Void PlayBackwards(System.String name = "", System.Double customBlend = -1)`

Starts or resumes reverse playback from the end.

name: The exact ordinal name.

customBlend: The typed argument for this operation, using the defaults described above.

<a id="member-0259b838bfaf"></a>
### PlaySection

`public System.Void PlaySection(System.String name = "", System.Double startTime = -1, System.Double endTime = -1, System.Double customBlend = -1, System.Double customSpeed = 1, System.Boolean fromEnd = false)`

Plays a bounded timeline section; negative boundaries select the resource endpoints.

startTime: The section start in seconds, or a negative value for the resource start.

endTime: The section end in seconds, or a negative value for the resource end.

name: The exact ordinal name.

customBlend: The typed argument for this operation, using the defaults described above.

customSpeed: The typed argument for this operation, using the defaults described above.

fromEnd: The typed argument for this operation, using the defaults described above.

<a id="member-d3ad8290c91a"></a>
### PlaySectionBackwards

`public System.Void PlaySectionBackwards(System.String name = "", System.Double startTime = -1, System.Double endTime = -1, System.Double customBlend = -1)`

Plays a bounded section backwards.

name: The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.

startTime: The section start in seconds, or a negative value for the resource start.

endTime: The section end in seconds, or a negative value for the resource end.

customBlend: Finite crossfade duration; negative uses configured/default timing.

<a id="member-8691f1bad171"></a>
### PlaySectionWithMarkers

`public System.Void PlaySectionWithMarkers(System.String name = "", System.String startMarker = "", System.String endMarker = "", System.Double customBlend = -1, System.Double customSpeed = 1, System.Boolean fromEnd = false)`

Plays a section using named marker times; empty or absent markers select endpoints.

startMarker: The start marker, or empty/missing to use the start endpoint.

endMarker: The end marker, or empty/missing to use the end endpoint.

customSpeed: A finite signed playback multiplier.

fromEnd: Whether a new or completed selection starts at the section end.

name: The exact ordinal name.

customBlend: The typed argument for this operation, using the defaults described above.

<a id="member-6c37dcb49119"></a>
### PlaySectionWithMarkersBackwards

`public System.Void PlaySectionWithMarkersBackwards(System.String name = "", System.String startMarker = "", System.String endMarker = "", System.Double customBlend = -1)`

Plays a marker-defined section backwards.

name: The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.

startMarker: The typed argument for this operation, using the defaults described above.

endMarker: The typed argument for this operation, using the defaults described above.

customBlend: The typed argument for this operation, using the defaults described above.

<a id="member-9d2cb06e9723"></a>
### PlayWithCapture

`public System.Void PlayWithCapture(System.String name = "", System.Double duration = -1, System.Double customBlend = -1, System.Double customSpeed = 1, System.Boolean fromEnd = false, Electron2D.Tween.TransitionType transitionType = Linear, Electron2D.Tween.EaseType easeType = In)`

Captures current target values and starts or resumes the selected clip.

name: The qualified name, or empty for AssignedAnimation.

duration: Finite capture duration; negative derives the key interval, zero skips capture.

customBlend: Finite transition duration, or negative for configured/default fallback.

customSpeed: Finite signed clip speed, independent of the capture clock.

fromEnd: Whether to start or capture backwards from the section end.

transitionType: The capture fade curve.

easeType: The capture fade easing.

<a id="member-58b0fad56742"></a>
### Queue

`public System.Void Queue(System.String name)`

Queues an existing animation; starts immediately when no animation is playing.

name: The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.

<a id="member-b5ebefb2b1d9"></a>
### ResetSection

`public System.Void ResetSection()`

Restores complete-resource playback boundaries.

<a id="member-46178d393a61"></a>
### Seek

`public System.Void Seek(System.Double seconds, System.Boolean update = false, System.Boolean updateOnly = false)`

Seeks within the current section; update applies values immediately without completion events.

seconds: The finite requested seek time, clamped to the current section.

update: Whether to apply values synchronously.

updateOnly: Whether to suppress method callbacks while updating properties/curves and sampling nested players without starting stopped targets.

<a id="member-7b77f96534e9"></a>
### SetBlendTime

`public System.Void SetBlendTime(System.String animationFrom, System.String animationTo, System.Double seconds)`

Sets a nonnegative named transition duration; zero removes the entry.

animationFrom: The existing source animation.

animationTo: The existing destination animation.

seconds: The finite nonnegative duration.

<a id="member-02ec3c7fd9c4"></a>
### SetProcessCallback

`public System.Void SetProcessCallback(Electron2D.AnimationMixer.AnimationCallbackModeProcess mode)`

Sets the automatic animation update phase.

mode: The defined update or process mode.

<a id="member-ea937cae2fcd"></a>
### SetRoot

`public System.Void SetRoot(System.String path)`

Sets the animation root path.

path: The relative target path.

<a id="member-3da7d1a79e22"></a>
### SetSection

`public System.Void SetSection(System.Double startTime = -1, System.Double endTime = -1)`

Sets the current playback section, clamping position into it.

startTime: The section start in seconds, or a negative value for the resource start.

endTime: The section end in seconds, or a negative value for the resource end.

<a id="member-8ec04475990e"></a>
### SetSectionWithMarkers

`public System.Void SetSectionWithMarkers(System.String startMarker = "", System.String endMarker = "")`

Sets the current section using markers.

startMarker: The start marker, or empty/missing to use the start endpoint.

endMarker: The end marker, or empty/missing to use the end endpoint.

<a id="member-00554edb133b"></a>
### Stop

`public System.Void Stop(System.Boolean keepState = false)`

Stops, resets position/speed and queued work, and stops still-controlled child players; keepState preserves target values.

keepState: Whether to leave target property values unchanged while resetting playback.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<System.String, System.String> AnimationChanged` | Occurs when queued or configured-next playback changes the selected animation. |
| `public event System.Action<System.String> CurrentAnimationChanged` | Occurs when the current animation selection changes. |

## Event Descriptions

<a id="member-1580459e8053"></a>
### AnimationChanged

`public event System.Action<System.String, System.String> AnimationChanged`

Occurs when queued or configured-next playback changes the selected animation.

<a id="member-2145eef62d6b"></a>
### CurrentAnimationChanged

`public event System.Action<System.String> CurrentAnimationChanged`

Occurs when the current animation selection changes.


## Lifecycle, verification and limits

Resource keys do not own targets or callbacks. Animation containers copy independently; directly held arrays clone and direct Resource payloads use the graph deep-copy session, while custom nested mutable references remain borrowed. Player/mixer mutation obeys attached SceneTree owner affinity; failures/reentry abandon stale passes. Deferred accepted calls retain their typed payload until a safe point and skip disposed/deleting or moved targets. Prepared callback pool exhaustion throws; use PrepareMethodCallbacks between frames for the required burst.

[AnimationSpecialTrackTests](../../tests/Electron2D.Tests/AnimationSpecialTrackTests.cs) and the existing scene animation/blend/graph/action suites exercise the connected runtime. Special-track tests cover cubic geometry, mixed signatures/copies, filters/weights, loop/seek/section order, callback mutation/failure/disposal and capacity reuse, with zero managed bytes across 256 warmed scalar and 256 prepared deferred passes. Two Linux Wayland GPU and two compatibility hosts check five curve/color pixel poses and borrowed-resource cleanup. Cold preparation and callbacks may allocate; native/driver allocations, other platforms and human acceptance are unmeasured. Audio schedulers, state machines and disk/editor persistence retain their coverage triggers.

Nested animation tracks execute clip-name keys against borrowed child players, with latest-crossed-key ordering, child-length seek/loop rules, normal independent child clocks, update-only sampling and control-revision cleanup. [The complete contract and snippet](../components/scene-animation.md#nested-animation-tracks) explains callback/reentry/cycle rules, prepared direct/weighted caches and current native/headless evidence. AnimationNestedTrackTests covers 256 warmed recurring start/stop plus child property passes with zero managed bytes; two GPU and two compatibility Wayland hosts verify six actual poses and cleanup.
