# AnimationMixer.AnimationCallbackModeMethod

Last updated: 2026-10-04

- **Source:** [`src/Scene/Animation/AnimationMixer.Methods.cs`](../../src/Scene/Animation/AnimationMixer.Methods.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Electron2D.AnimationMixer.AnimationCallbackModeMethod`

## Description

Method-track dispatch policy: Deferred (0) uses a captured SceneTree safe-point batch; Immediate (1) invokes inside evaluation. Deferred is the default; detached execution invokes synchronously.

The complete timing, copy/borrowing, validation, callback error/reentry and verification contract is on [Scene animation](../components/scene-animation.md), including [special tracks](../components/scene-animation.md#bézier-and-method-tracks). Author/edit on the scene owner thread; target nodes and authored resources remain borrowed. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the current implementation.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.AnimationMixer.AnimationCallbackModeMethod Deferred = 0` | Queue on the target SceneTree; detached targets invoke immediately. |
| `public const Electron2D.AnimationMixer.AnimationCallbackModeMethod Immediate = 1` | Invoke while the controller evaluates the key. |

## Enumeration Descriptions

<a id="member-5513aeb6ef33"></a>
### Deferred

`public const Electron2D.AnimationMixer.AnimationCallbackModeMethod Deferred = 0`

Queue on the target SceneTree; detached targets invoke immediately.

<a id="member-66b1b723101d"></a>
### Immediate

`public const Electron2D.AnimationMixer.AnimationCallbackModeMethod Immediate = 1`

Invoke while the controller evaluates the key.


## Lifecycle, verification and limits

Resource keys do not own targets or callbacks. Animation containers copy independently; directly held arrays clone and direct Resource payloads use the graph deep-copy session, while custom nested mutable references remain borrowed. Player/mixer mutation obeys attached SceneTree owner affinity; failures/reentry abandon stale passes. Deferred accepted calls retain their typed payload until a safe point and skip disposed/deleting or moved targets. Prepared callback pool exhaustion throws; use PrepareMethodCallbacks between frames for the required burst.

[AnimationSpecialTrackTests](../../tests/Electron2D.Tests/AnimationSpecialTrackTests.cs) and the existing scene animation/blend/graph/action suites exercise the connected runtime. Special-track tests cover cubic geometry, mixed signatures/copies, filters/weights, loop/seek/section order, callback mutation/failure/disposal and capacity reuse, with zero managed bytes across 256 warmed scalar and 256 prepared deferred passes. Two Linux Wayland GPU and two compatibility hosts check five curve/color pixel poses and borrowed-resource cleanup. Cold preparation and callbacks may allocate; native/driver allocations, other platforms and human acceptance are unmeasured. Audio/nested schedulers, state machines and disk/editor persistence retain their coverage triggers.
