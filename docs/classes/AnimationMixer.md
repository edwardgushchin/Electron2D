# AnimationMixer

Last updated: 2026-10-04

**Inherits:** [Node](Node.md)

**Inherited By:** [AnimationPlayer](AnimationPlayer.md), [AnimationTree](AnimationTree.md)

- **Source:** [`src/Scene/Animation/AnimationMixer.cs`](../../src/Scene/Animation/AnimationMixer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Electron2D.AnimationMixer`

## Description

Borrowed animation libraries, typed property/method bindings, weighted evaluation and scheduling.

The complete timing, copy/borrowing, validation, callback error/reentry and verification contract is on [Scene animation](../components/scene-animation.md), including [special tracks](../components/scene-animation.md#bézier-and-method-tracks). Author/edit on the scene owner thread; target nodes and authored resources remain borrowed. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the current implementation.

## Examples

Partial authoring snippet; existing scene/library variables are indicated where required.

```csharp
var mixer = new AnimationPlayer { CallbackModeProcess =
    AnimationMixer.AnimationCallbackModeProcess.Manual };
mixer.AddAnimationLibrary("", library); // Existing authored library.
root.AddChild(mixer); // Existing scene root and targets.
mixer.PrepareMethodCallbacks(64); // Cold preparation before a larger deferred burst.
mixer.Play("clip");
mixer.Advance(.25);
```

## Enumeration types

| Type | Reference |
| --- | --- |
| `public enum Electron2D.AnimationMixer.AnimationCallbackModeDiscrete` | [AnimationCallbackModeDiscrete](AnimationMixer.AnimationCallbackModeDiscrete.md) |
| `public enum Electron2D.AnimationMixer.AnimationCallbackModeMethod` | [AnimationCallbackModeMethod](AnimationMixer.AnimationCallbackModeMethod.md) |
| `public enum Electron2D.AnimationMixer.AnimationCallbackModeProcess` | [AnimationCallbackModeProcess](AnimationMixer.AnimationCallbackModeProcess.md) |

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationMixer()` | Initializes internal idle animation scheduling. |

## Constructor Descriptions

<a id="member-218027746a95"></a>
### .ctor

`public AnimationMixer()`

Initializes internal idle animation scheduling.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean Active { get; set; }` | Gets or sets whether evaluation is active; defaults to true. |
| `public Electron2D.AnimationMixer.AnimationCallbackModeDiscrete CallbackModeDiscrete { get; set; }` | Gets or sets discrete/continuous precedence, initially Recessive. |
| `public Electron2D.AnimationMixer.AnimationCallbackModeMethod CallbackModeMethod { get; set; }` | Gets or sets method-key dispatch policy; defaults to Deferred. |
| `public Electron2D.AnimationMixer.AnimationCallbackModeProcess CallbackModeProcess { get; set; }` | Gets or sets the internal update phase, initially idle. |
| `public System.Boolean Deterministic { get; set; }` | Gets or sets unnormalized accumulation relative to zero or the RESET clip; defaults to false. |
| `public System.String RootNode { get; set; }` | Gets or sets the relative root used for all track paths; defaults to the parent. |

## Property Descriptions

<a id="member-283eddfa79c6"></a>
### Active

`public System.Boolean Active { get; set; }`

Gets or sets whether evaluation is active; defaults to true.

<a id="member-b5c0af0070e1"></a>
### CallbackModeDiscrete

`public Electron2D.AnimationMixer.AnimationCallbackModeDiscrete CallbackModeDiscrete { get; set; }`

Gets or sets discrete/continuous precedence, initially Recessive.

<a id="member-77f6af7f837f"></a>
### CallbackModeMethod

`public Electron2D.AnimationMixer.AnimationCallbackModeMethod CallbackModeMethod { get; set; }`

Gets or sets method-key dispatch policy; defaults to Deferred.

Value: The defined dispatch mode.

Remarks: Mutation requires the attached SceneTree owner thread. Deferred keys retain their payload until its safe point; disposed/deleting or moved targets are skipped.

System.ObjectDisposedException: This mixer has been disposed.

System.InvalidOperationException: Mutation occurs outside the owner thread.

System.ArgumentOutOfRangeException: The mode is invalid.

<a id="member-66e3637a4877"></a>
### CallbackModeProcess

`public Electron2D.AnimationMixer.AnimationCallbackModeProcess CallbackModeProcess { get; set; }`

Gets or sets the internal update phase, initially idle.

<a id="member-085c605df311"></a>
### Deterministic

`public System.Boolean Deterministic { get; set; }`

Gets or sets unnormalized accumulation relative to zero or the RESET clip; defaults to false.

<a id="member-d8d622ed2a8a"></a>
### RootNode

`public System.String RootNode { get; set; }`

Gets or sets the relative root used for all track paths; defaults to the parent.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddAnimationLibrary(System.String name, Electron2D.AnimationLibrary library)` | Adds a borrowed library; empty names form the default namespace. |
| `public System.Void Advance(System.Double delta)` | Advances the controller by finite signed seconds when active. |
| `public System.Void Capture(System.String name, System.Double duration, Electron2D.Tween.TransitionType transitionType = Linear, Electron2D.Tween.EaseType easeType = In)` | Captures current property values from enabled Capture tracks, replacing the prior capture. |
| `public System.Void ClearCaches()` | Clears all cached typed target bindings. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public System.String FindAnimation(Electron2D.Animation animation)` | Finds the first qualified name of a borrowed animation, or empty. |
| `public System.String FindAnimationLibrary(Electron2D.Animation animation)` | Finds an animation's first library namespace, or empty for the default or absent library. |
| `public Electron2D.Animation GetAnimation(System.String name)` | Returns a borrowed animation by its qualified name. |
| `public Electron2D.AnimationLibrary GetAnimationLibrary(System.String name)` | Returns a borrowed library by exact namespace. |
| `public System.String[] GetAnimationLibraryList()` | Returns an independent ordinal-sorted library namespace array. |
| `public System.String[] GetAnimationList()` | Returns all qualified animation names in ordinal order. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public System.Boolean HasAnimation(System.String name)` | Tests a qualified library/name or an unqualified default-library animation. |
| `public System.Boolean HasAnimationLibrary(System.String name)` | Tests exact library namespace membership. |
| `protected override System.Void OnNotification(System.Int32 what)` | Handles an engine notification delivered to this object. |
| `protected virtual TValue OnPostProcessKeyValue<TValue>(Electron2D.Animation animation, System.Int32 track, TValue value, System.UInt64 objectID, System.Int32 objectSubIndex = -1)` | Processes one typed key after sampling, before discrete writes or weighted accumulation. |
| `public System.Void PrepareMethodCallbacks(System.Int32 pendingCallsPerTrack)` | Prepares a minimum pending deferred-call capacity for each method-track binding. |
| `public System.Void RemoveAnimationLibrary(System.String name)` | Removes a library without disposing it. |
| `public System.Void RenameAnimationLibrary(System.String name, System.String newName)` | Renames a library namespace without changing its resources. |

## Method Descriptions

<a id="member-b4069147492a"></a>
### AddAnimationLibrary

`public System.Void AddAnimationLibrary(System.String name, Electron2D.AnimationLibrary library)`

Adds a borrowed library; empty names form the default namespace.

name: The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.

library: The live borrowed animation library.

<a id="member-755045247191"></a>
### Advance

`public System.Void Advance(System.Double delta)`

Advances the controller by finite signed seconds when active.

delta: Finite signed elapsed seconds; automatic updates use the inherited scaled delta.

<a id="member-e43de072ce6a"></a>
### Capture

`public System.Void Capture(System.String name, System.Double duration, Electron2D.Tween.TransitionType transitionType = Linear, Electron2D.Tween.EaseType easeType = In)`

Captures current property values from enabled Capture tracks, replacing the prior capture.

name: The exact qualified animation name.

duration: Positive finite capture duration in seconds.

transitionType: The fade curve, initially Linear.

easeType: The fade easing, initially In.

<a id="member-d201fe41dd82"></a>
### ClearCaches

`public System.Void ClearCaches()`

Clears all cached typed target bindings.

<a id="member-620db35b5c1d"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-3e5ea79750ff"></a>
### FindAnimation

`public System.String FindAnimation(Electron2D.Animation animation)`

Finds the first qualified name of a borrowed animation, or empty.

animation: The live borrowed animation resource.

<a id="member-6e3929dff7f0"></a>
### FindAnimationLibrary

`public System.String FindAnimationLibrary(Electron2D.Animation animation)`

Finds an animation's first library namespace, or empty for the default or absent library.

animation: The borrowed animation resource.

<a id="member-0171909769cb"></a>
### GetAnimation

`public Electron2D.Animation GetAnimation(System.String name)`

Returns a borrowed animation by its qualified name.

name: The exact ordinal name.

<a id="member-a121e7894f94"></a>
### GetAnimationLibrary

`public Electron2D.AnimationLibrary GetAnimationLibrary(System.String name)`

Returns a borrowed library by exact namespace.

name: The exact ordinal name.

<a id="member-2646e2053870"></a>
### GetAnimationLibraryList

`public System.String[] GetAnimationLibraryList()`

Returns an independent ordinal-sorted library namespace array.

<a id="member-682d9bdaf8aa"></a>
### GetAnimationList

`public System.String[] GetAnimationList()`

Returns all qualified animation names in ordinal order.

<a id="member-3f2bb021fe4f"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-97a3bb3a4e85"></a>
### HasAnimation

`public System.Boolean HasAnimation(System.String name)`

Tests a qualified library/name or an unqualified default-library animation.

name: The exact ordinal name.

<a id="member-b92580c541d7"></a>
### HasAnimationLibrary

`public System.Boolean HasAnimationLibrary(System.String name)`

Tests exact library namespace membership.

name: The exact ordinal name.

<a id="member-108169d6aa98"></a>
### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`

Handles an engine notification delivered to this object.

what: The notification identifier.

Remarks: Derived overrides should call the base implementation unless they intentionally suppress inherited handling.

<a id="member-131c6fc497fd"></a>
### OnPostProcessKeyValue

`protected virtual TValue OnPostProcessKeyValue<TValue>(Electron2D.Animation animation, System.Int32 track, TValue value, System.UInt64 objectID, System.Int32 objectSubIndex = -1)`

Processes one typed key after sampling, before discrete writes or weighted accumulation.

TValue: The exact track value type.

animation: The source animation, including an owned snapshot for captured values.

track: The originating track index.

value: The sampled typed value.

objectID: The target node's stable instance identifier.

objectSubIndex: The indexed target subobject; property tracks use minus one.

Returns: The typed value to accumulate or write.

<a id="member-06f3ce698950"></a>
### PrepareMethodCallbacks

`public System.Void PrepareMethodCallbacks(System.Int32 pendingCallsPerTrack)`

Prepares a minimum pending deferred-call capacity for each method-track binding.

pendingCallsPerTrack: A nonnegative capacity for the largest burst before a SceneTree flush.

Remarks: Bindings initially prepare max(16, key count) records. This explicit cold operation prepares bindings and reusable typed call records; capacity cannot shrink. Exceeding prepared pending capacity during dispatch throws InvalidOperationException while retaining already queued calls. The shared SceneTree queue warms separately. Successful hot dispatch never grows their capacity. User callback allocations remain outside engine control.

System.ObjectDisposedException: This mixer or a required animation resource has been disposed.

System.InvalidOperationException: Mutation occurs outside the owner thread or binding descriptors conflict.

System.ArgumentOutOfRangeException: The capacity is negative.

<a id="member-70f1a3ecbcd6"></a>
### RemoveAnimationLibrary

`public System.Void RemoveAnimationLibrary(System.String name)`

Removes a library without disposing it.

name: The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.

<a id="member-505767b889df"></a>
### RenameAnimationLibrary

`public System.Void RenameAnimationLibrary(System.String name, System.String newName)`

Renames a library namespace without changing its resources.

newName: An unoccupied valid replacement name.

name: The exact ordinal name.

## Event summary

| Complete C# signature | Contract |
| --- | --- |
| `public event System.Action<System.String> AnimationFinished` | Occurs after non-looping playback reaches its endpoint. |
| `public event System.Action AnimationLibrariesUpdated` | Occurs after library membership changes. |
| `public event System.Action AnimationListChanged` | Occurs after a library's named animation collection changes. |
| `public event System.Action<System.String> AnimationStarted` | Occurs when playback starts. |
| `public event System.Action CachesCleared` | Occurs after explicit cache clearing. |

## Event Descriptions

<a id="member-25d74529534b"></a>
### AnimationFinished

`public event System.Action<System.String> AnimationFinished`

Occurs after non-looping playback reaches its endpoint.

<a id="member-33402acac21c"></a>
### AnimationLibrariesUpdated

`public event System.Action AnimationLibrariesUpdated`

Occurs after library membership changes.

<a id="member-75a0d4c3fb2a"></a>
### AnimationListChanged

`public event System.Action AnimationListChanged`

Occurs after a library's named animation collection changes.

<a id="member-2df35395db86"></a>
### AnimationStarted

`public event System.Action<System.String> AnimationStarted`

Occurs when playback starts.

<a id="member-c417df378e86"></a>
### CachesCleared

`public event System.Action CachesCleared`

Occurs after explicit cache clearing.


## Lifecycle, verification and limits

Resource keys do not own targets or callbacks. Animation containers copy independently; directly held arrays clone and direct Resource payloads use the graph deep-copy session, while custom nested mutable references remain borrowed. Player/mixer mutation obeys attached SceneTree owner affinity; failures/reentry abandon stale passes. Deferred accepted calls retain their typed payload until a safe point and skip disposed/deleting or moved targets. Prepared callback pool exhaustion throws; use PrepareMethodCallbacks between frames for the required burst.

[AnimationSpecialTrackTests](../../tests/Electron2D.Tests/AnimationSpecialTrackTests.cs) and the existing scene animation/blend/graph/action suites exercise the connected runtime. Special-track tests cover cubic geometry, mixed signatures/copies, filters/weights, loop/seek/section order, callback mutation/failure/disposal and capacity reuse, with zero managed bytes across 256 warmed scalar and 256 prepared deferred passes. Two Linux Wayland GPU and two compatibility hosts check five curve/color pixel poses and borrowed-resource cleanup. Cold preparation and callbacks may allocate; native/driver allocations, other platforms and human acceptance are unmeasured. Audio/nested schedulers, state machines and disk/editor persistence retain their coverage triggers.
