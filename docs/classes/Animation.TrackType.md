# Animation.TrackType

Last updated: 2026-10-04

- **Source:** [`src/Scene/Resources/Animation.cs`](../../src/Scene/Resources/Animation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Electron2D.Animation.TrackType`

## Description

Declared executable timeline kinds: Value (0), Method (5), Bezier (6), Audio (7) and Animation (8). All applicable runtime kinds execute; value optimization/persistence retain their coverage states; 3D track roles are excluded.

The complete timing, copy/borrowing, validation, callback error/reentry and verification contract is on [Scene animation](../components/scene-animation.md), including [special tracks](../components/scene-animation.md#bézier-and-method-tracks). Author/edit on the scene owner thread; target nodes and authored resources remain borrowed. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the current implementation.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.Animation.TrackType Animation = 8` | Keys control named clips on relative AnimationPlayer targets. |
| `public const Electron2D.Animation.TrackType Audio = 7` | Keys start borrowed audio sources on relative players or emitters. |
| `public const Electron2D.Animation.TrackType Bezier = 6` | Keys describe scalar time/value cubic control handles. |
| `public const Electron2D.Animation.TrackType Method = 5` | Keys invoke typed callbacks on relative node targets. |
| `public const Electron2D.Animation.TrackType Value = 0` | Keys target one typed property. |

## Enumeration Descriptions

<a id="member-1f5b5da27e49"></a>
### Animation

`public const Electron2D.Animation.TrackType Animation = 8`

Keys control named clips on relative AnimationPlayer targets.

<a id="member-0448d5c4ce3d"></a>
### Audio

`public const Electron2D.Animation.TrackType Audio = 7`

Keys start borrowed audio sources on relative players or emitters.

<a id="member-49f255456f34"></a>
### Bezier

`public const Electron2D.Animation.TrackType Bezier = 6`

Keys describe scalar time/value cubic control handles.

<a id="member-0748fa1ec340"></a>
### Method

`public const Electron2D.Animation.TrackType Method = 5`

Keys invoke typed callbacks on relative node targets.

<a id="member-69fe08bc71b6"></a>
### Value

`public const Electron2D.Animation.TrackType Value = 0`

Keys target one typed property.


## Lifecycle, verification and limits

Resource keys do not own targets or callbacks. Animation containers copy independently; directly held arrays clone and direct Resource payloads use the graph deep-copy session, while custom nested mutable references remain borrowed. Player/mixer mutation obeys attached SceneTree owner affinity; failures/reentry abandon stale passes. Deferred accepted calls retain their typed payload until a safe point and skip disposed/deleting or moved targets. Prepared callback pool exhaustion throws; use PrepareMethodCallbacks between frames for the required burst.

[AnimationSpecialTrackTests](../../tests/Electron2D.Tests/AnimationSpecialTrackTests.cs) and the existing scene animation/blend/graph/action suites exercise the connected runtime. Special-track tests cover cubic geometry, mixed signatures/copies, filters/weights, loop/seek/section order, callback mutation/failure/disposal and capacity reuse, with zero managed bytes across 256 warmed scalar and 256 prepared deferred passes. Two Linux Wayland GPU and two compatibility hosts check five curve/color pixel poses and borrowed-resource cleanup. Cold preparation and callbacks may allocate; native/driver allocations, other platforms and human acceptance are unmeasured. State machines and disk/editor persistence retain their coverage triggers.

Nested animation tracks execute clip-name keys against borrowed child players, with latest-crossed-key ordering, child-length seek/loop rules, normal independent child clocks, update-only sampling and control-revision cleanup. [The complete contract and snippet](../components/scene-animation.md#nested-animation-tracks) explains callback/reentry/cycle rules, prepared direct/weighted caches and current native/headless evidence. AnimationNestedTrackTests covers 256 warmed recurring start/stop plus child property passes with zero managed bytes; two GPU and two compatibility Wayland hosts verify six actual poses and cleanup.

[Audio tracks](../components/scene-animation.md#audio-tracks) execute borrowed cue sources on prepared player/emitter/polyphonic transports, with offsets, weighted gain, sample/stream selection, pause/cleanup and per-trigger random choices. Native PCM and two GPU/two compatibility public emitter hosts establish current Linux execution; physical listening, driver internals and other platforms remain unverified.
