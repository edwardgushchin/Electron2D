# AnimationSyncMode

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AnimationSyncMode` · **Source:** [AnimationBlendSpace.cs](../../src/Scene/Animation/AnimationBlendSpace.cs).

## Description

Selects inactive-clock and cycle-length synchronization in either animation blend space.

This exact value contract is shared by [AnimationNodeBlendSpace1D](AnimationNodeBlendSpace1D.md) and [AnimationNodeBlendSpace2D](AnimationNodeBlendSpace2D.md). The processing rules and boundaries are documented on both classes and [the component](../components/scene-animation.md#blend-spaces).

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.AnimationSyncMode CyclicConstant = 3` | Scale each clip delta to a configured common cycle length. |
| `public const Electron2D.AnimationSyncMode CyclicMutable = 2` | Scale each clip delta to the weighted active timeline length. |
| `public const Electron2D.AnimationSyncMode Independent = 1` | Advance all points at their independent rate. |
| `public const Electron2D.AnimationSyncMode None = 0` | Freeze inactive points. |

## Enumeration Descriptions

<a id="member-e7919b81b3c2"></a>
### CyclicConstant

`public const Electron2D.AnimationSyncMode CyclicConstant = 3`

Scale each clip delta to a configured common cycle length.

<a id="member-d293999a7d6b"></a>
### CyclicMutable

`public const Electron2D.AnimationSyncMode CyclicMutable = 2`

Scale each clip delta to the weighted active timeline length.

<a id="member-e8d9419a87f2"></a>
### Independent

`public const Electron2D.AnimationSyncMode Independent = 1`

Advance all points at their independent rate.

<a id="member-44cefcbf8270"></a>
### None

`public const Electron2D.AnimationSyncMode None = 0`

Freeze inactive points.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) and [blend spaces](../components/scene-animation.md#blend-spaces) own this executed profile. AnimationBlendSpaceTests verifies names/indices/capacity/cycles, endpoints/duplicate/degenerate/extreme geometry, automatic/manual topology, discrete/carry/PingPong, independent/cyclic clocks, shared libraries/live edits, callback failure/reentry/copies/borrowing and zero warmed managed bytes on 256 cyclic planar and 256 alternating carry passes. Two Wayland GPU and two compatibility Engine.Run cycles verify seven real pixel poses each and clean borrowed-resource shutdown. Cold preparation/triangulation, native/driver allocation, other-platform/human acceptance and scene/disk/editor round trips remain distinct. State-machine/transition/OneShot, expressions and non-property track schedulers remain separate coverage triggers.
