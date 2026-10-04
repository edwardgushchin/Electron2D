# AnimationMethodKey<TOwner>

Last updated: 2026-10-04

**Inherited By:** [AnimationMethodKey with arguments](AnimationMethodKey.Generic2.md)

- **Source:** [`src/Scene/Resources/Animation.SpecialTracks.cs`](../../src/Scene/Resources/Animation.SpecialTracks.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class AnimationMethodKey<TOwner> where TOwner : Node`

## Description

Abstract receiver-typed identity for keys with heterogeneous exact payload signatures. Library consumers construct concrete keys; internal dispatch/copy hooks prevent external executable subclasses.

The complete timing, copy/borrowing, validation, callback error/reentry and verification contract is on [Scene animation](../components/scene-animation.md), including [special tracks](../components/scene-animation.md#bézier-and-method-tracks). Author/edit on the scene owner thread; target nodes and authored resources remain borrowed. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the current implementation.

## Examples

Partial authoring snippet; existing scene/library variables are indicated where required.

```csharp
AnimationMethodKey<Entity> key = new AnimationMethodKey<Entity, bool>(
    "visibility", static (target, visible) => target.Visible = visible, false);
string name = key.Name;
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected AnimationMethodKey(System.String name)` | Validates a nonblank diagnostic name. |

## Constructor Descriptions

<a id="member-894e11a45195"></a>
### .ctor

`protected AnimationMethodKey(System.String name)`

Validates a nonblank diagnostic name.

name: The exact diagnostic name.

System.ArgumentException: The name is null or blank.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.String Name { get;  }` | Gets the exact diagnostic method name. |

## Property Descriptions

<a id="member-4a306a71ffec"></a>
### Name

`public System.String Name { get;  }`

Gets the exact diagnostic method name.

Value: The validated nonblank name; the delegate supplies executable identity.


## Lifecycle, verification and limits

Resource keys do not own targets or callbacks. Animation containers copy independently; directly held arrays clone and direct Resource payloads use the graph deep-copy session, while custom nested mutable references remain borrowed. Player/mixer mutation obeys attached SceneTree owner affinity; failures/reentry abandon stale passes. Deferred accepted calls retain their typed payload until a safe point and skip disposed/deleting or moved targets. Prepared callback pool exhaustion throws; use PrepareMethodCallbacks between frames for the required burst.

[AnimationSpecialTrackTests](../../tests/Electron2D.Tests/AnimationSpecialTrackTests.cs) and the existing scene animation/blend/graph/action suites exercise the connected runtime. Special-track tests cover cubic geometry, mixed signatures/copies, filters/weights, loop/seek/section order, callback mutation/failure/disposal and capacity reuse, with zero managed bytes across 256 warmed scalar and 256 prepared deferred passes. Two Linux Wayland GPU and two compatibility hosts check five curve/color pixel poses and borrowed-resource cleanup. Cold preparation and callbacks may allocate; native/driver allocations, other platforms and human acceptance are unmeasured. Audio/nested schedulers, state machines and disk/editor persistence retain their coverage triggers.
