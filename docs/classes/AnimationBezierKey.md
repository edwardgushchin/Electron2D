# AnimationBezierKey

Last updated: 2026-10-04

- **Source:** [`src/Scene/Resources/Animation.SpecialTracks.cs`](../../src/Scene/Resources/Animation.SpecialTracks.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public readonly struct AnimationBezierKey`

## Description

Scalar Bézier key data with relative time/value handles. Values are validated and handle time signs normalized when accepted by an Animation track.

The complete timing, copy/borrowing, validation, callback error/reentry and verification contract is on [Scene animation](../components/scene-animation.md), including [special tracks](../components/scene-animation.md#bézier-and-method-tracks). Author/edit on the scene owner thread; target nodes and authored resources remain borrowed. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the current implementation.

## Examples

Partial authoring snippet; existing scene/library variables are indicated where required.

```csharp
var key = new AnimationBezierKey(16, outHandle: new(1f / 3, 0));
var replacement = key with { Value = 32 };
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationBezierKey(System.Double value, Electron2D.Vector2 inHandle = default, Electron2D.Vector2 outHandle = default)` | Creates scalar key data; the receiving track validates and normalizes handles. |

## Constructor Descriptions

<a id="member-219cece5059c"></a>
### .ctor

`public AnimationBezierKey(System.Double value, Electron2D.Vector2 inHandle = default, Electron2D.Vector2 outHandle = default)`

Creates scalar key data; the receiving track validates and normalizes handles.

value: The finite scalar value.

inHandle: Incoming relative time/value offset.

outHandle: Outgoing relative time/value offset.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.Vector2 InHandle { get; set; }` | Gets the incoming relative time/value offset. |
| `public Electron2D.Vector2 OutHandle { get; set; }` | Gets the outgoing relative time/value offset. |
| `public System.Double Value { get; set; }` | Gets the scalar value. |

## Property Descriptions

<a id="member-f92cfa8f7957"></a>
### InHandle

`public Electron2D.Vector2 InHandle { get; set; }`

Gets the incoming relative time/value offset.

Value: The track clamps positive x to zero.

<a id="member-0d9d893cc314"></a>
### OutHandle

`public Electron2D.Vector2 OutHandle { get; set; }`

Gets the outgoing relative time/value offset.

Value: The track clamps negative x to zero.

<a id="member-4b07008791d7"></a>
### Value

`public System.Double Value { get; set; }`

Gets the scalar value.

Value: Finite scalar key data when accepted by a track.


## Lifecycle, verification and limits

Resource keys do not own targets or callbacks. Animation containers copy independently; directly held arrays clone and direct Resource payloads use the graph deep-copy session, while custom nested mutable references remain borrowed. Player/mixer mutation obeys attached SceneTree owner affinity; failures/reentry abandon stale passes. Deferred accepted calls retain their typed payload until a safe point and skip disposed/deleting or moved targets. Prepared callback pool exhaustion throws; use PrepareMethodCallbacks between frames for the required burst.

[AnimationSpecialTrackTests](../../tests/Electron2D.Tests/AnimationSpecialTrackTests.cs) and the existing scene animation/blend/graph/action suites exercise the connected runtime. Special-track tests cover cubic geometry, mixed signatures/copies, filters/weights, loop/seek/section order, callback mutation/failure/disposal and capacity reuse, with zero managed bytes across 256 warmed scalar and 256 prepared deferred passes. Two Linux Wayland GPU and two compatibility hosts check five curve/color pixel poses and borrowed-resource cleanup. Cold preparation and callbacks may allocate; native/driver allocations, other platforms and human acceptance are unmeasured. Audio/nested schedulers, state machines and disk/editor persistence retain their coverage triggers.
