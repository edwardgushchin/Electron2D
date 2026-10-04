# AnimationPlayMode

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AnimationPlayMode` · **Source:** [AnimationNodeAnimation.cs](../../src/Scene/Animation/AnimationNodeAnimation.cs).

## Description

Selects forward or reversed sampling of a graph animation clip.

Forward and Backward are the same two-value clip-direction contract for every graph clip leaf.

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.AnimationPlayMode Backward = 1` | Sample from the clip end toward its start. |
| `public const Electron2D.AnimationPlayMode Forward = 0` | Sample from the clip start toward its end. |

## Enumeration Descriptions

<a id="member-e9732ddbc89a"></a>
### Backward

`public const Electron2D.AnimationPlayMode Backward = 1`

Sample from the clip end toward its start.

<a id="member-7c8128d24d51"></a>
### Forward

`public const Electron2D.AnimationPlayMode Forward = 0`

Sample from the clip start toward its end.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns this typed contract. [Scene animation](../components/scene-animation.md#animation-graphs) records formulas, topology, lifetime, tests and limitations. [AnimationGraphTests](../../tests/Electron2D.Tests/AnimationGraphTests.cs) verifies public managed execution and two real Wayland GPU/two compatibility host cycles with five rendered positions. Warmed scalar graph/property passes allocate zero managed bytes on the owner thread; cold preparation/result arrays, native/driver allocations and other platforms are separate. State machines/grouped controllers, Expression evaluation, non-property track schedulers and graph/disk/editor round trips remain applicable separate slices in coverage.
