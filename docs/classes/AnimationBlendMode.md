# AnimationBlendMode

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AnimationBlendMode` · **Source:** [AnimationBlendSpace.cs](../../src/Scene/Animation/AnimationBlendSpace.cs).

## Description

Selects interpolation or nearest-point playback in either animation blend space.

This exact value contract is shared by [AnimationNodeBlendSpace1D](AnimationNodeBlendSpace1D.md) and [AnimationNodeBlendSpace2D](AnimationNodeBlendSpace2D.md). The processing rules and boundaries are documented on both classes and [the component](../components/scene-animation.md#blend-spaces).

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.AnimationBlendMode Discrete = 1` | Play only the nearest point, retaining its independent clock. |
| `public const Electron2D.AnimationBlendMode DiscreteCarry = 2` | Play the nearest point and carry the previous selected timeline on changes. |
| `public const Electron2D.AnimationBlendMode Interpolated = 0` | Interpolate adjacent points. |

## Enumeration Descriptions

<a id="member-7ba513fd47a3"></a>
### Discrete

`public const Electron2D.AnimationBlendMode Discrete = 1`

Play only the nearest point, retaining its independent clock.

<a id="member-b9a7539321a9"></a>
### DiscreteCarry

`public const Electron2D.AnimationBlendMode DiscreteCarry = 2`

Play the nearest point and carry the previous selected timeline on changes.

<a id="member-52442dd61e94"></a>
### Interpolated

`public const Electron2D.AnimationBlendMode Interpolated = 0`

Interpolate adjacent points.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) and [blend spaces](../components/scene-animation.md#blend-spaces) own this executed profile. AnimationBlendSpaceTests verifies names/indices/capacity/cycles, endpoints/duplicate/degenerate/extreme geometry, automatic/manual topology, discrete/carry/PingPong, independent/cyclic clocks, shared libraries/live edits, callback failure/reentry/copies/borrowing and zero warmed managed bytes on 256 cyclic planar and 256 alternating carry passes. Two Wayland GPU and two compatibility Engine.Run cycles verify seven real pixel poses each and clean borrowed-resource shutdown. Cold preparation/triangulation, native/driver allocation, other-platform/human acceptance and scene/disk/editor round trips remain distinct. State-machine/transition/OneShot, expressions and non-property track schedulers remain separate coverage triggers.
