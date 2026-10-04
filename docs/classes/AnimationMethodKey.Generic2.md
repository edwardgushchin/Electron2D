# AnimationMethodKey<TOwner, TArguments>

Last updated: 2026-10-04

**Inherits:** [AnimationMethodKey<TOwner>](AnimationMethodKey.Generic.md)

- **Source:** [`src/Scene/Resources/Animation.SpecialTracks.cs`](../../src/Scene/Resources/Animation.SpecialTracks.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class AnimationMethodKey<TOwner, TArguments> : AnimationMethodKey<TOwner> where TOwner : Node`

## Description

Immutable typed delegate/payload key. The diagnostic name does not perform method lookup. Tuples/immutable records represent multiple arguments; delegates and custom references remain borrowed.

The complete timing, copy/borrowing, validation, callback error/reentry and verification contract is on [Scene animation](../components/scene-animation.md), including [special tracks](../components/scene-animation.md#bézier-and-method-tracks). Author/edit on the scene owner thread; target nodes and authored resources remain borrowed. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the current implementation.

## Examples

Partial authoring snippet; existing scene/library variables are indicated where required.

```csharp
var key = new AnimationMethodKey<Entity, (float X, float Y)>("move",
    static (target, point) => target.Position = new(point.X, point.Y), (16, 32));
// clip is an existing Animation with a receiver-typed Entity method track.
clip.TrackInsertKey(track, .5, key);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationMethodKey(System.String name, Action<TOwner, TArguments> callback, TArguments arguments)` | Creates a typed callback key. |

## Constructor Descriptions

<a id="member-60e6ee2f4bda"></a>
### .ctor

`public AnimationMethodKey(System.String name, Action<TOwner, TArguments> callback, TArguments arguments)`

Creates a typed callback key.

name: The nonblank diagnostic method name.

callback: The callback accepting a resolved target and typed arguments.

arguments: The typed payload; custom mutable references remain borrowed.

System.ArgumentException: The name is null or blank.

System.ArgumentNullException: The callback is null.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public TArguments Arguments { get;  }` | Gets the exact typed payload. |
| `public Action<TOwner, TArguments> Callback { get;  }` | Gets the borrowed typed callback. |

## Property Descriptions

<a id="member-abfff373b171"></a>
### Arguments

`public TArguments Arguments { get;  }`

Gets the exact typed payload.

Value: The payload supplied at construction; custom references remain borrowed.

<a id="member-a73d6174d0d1"></a>
### Callback

`public Action<TOwner, TArguments> Callback { get;  }`

Gets the borrowed typed callback.

Value: The validated nonnull delegate.


## Lifecycle, verification and limits

Resource keys do not own targets or callbacks. Animation containers copy independently; directly held arrays clone and direct Resource payloads use the graph deep-copy session, while custom nested mutable references remain borrowed. Player/mixer mutation obeys attached SceneTree owner affinity; failures/reentry abandon stale passes. Deferred accepted calls retain their typed payload until a safe point and skip disposed/deleting or moved targets. Prepared callback pool exhaustion throws; use PrepareMethodCallbacks between frames for the required burst.

[AnimationSpecialTrackTests](../../tests/Electron2D.Tests/AnimationSpecialTrackTests.cs) and the existing scene animation/blend/graph/action suites exercise the connected runtime. Special-track tests cover cubic geometry, mixed signatures/copies, filters/weights, loop/seek/section order, callback mutation/failure/disposal and capacity reuse, with zero managed bytes across 256 warmed scalar and 256 prepared deferred passes. Two Linux Wayland GPU and two compatibility hosts check five curve/color pixel poses and borrowed-resource cleanup. Cold preparation and callbacks may allocate; native/driver allocations, other platforms and human acceptance are unmeasured. Audio/nested schedulers, state machines and disk/editor persistence retain their coverage triggers.
