# AnimationMixMode

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AnimationMixMode` · **Source:** [AnimationNodeOneShot.cs](../../src/Scene/Animation/AnimationNodeOneShot.cs).

## Description

Selects replacement or additive animation contributions.

This domain belongs to [AnimationNodeOneShot](AnimationNodeOneShot.md). Its default, request lifetime, blend/filter semantics and failure behavior are documented on that class and [the component](../components/scene-animation.md#action-controllers).

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.AnimationMixMode Add = 1` | Add the action to the complete base contribution. |
| `public const Electron2D.AnimationMixMode Blend = 0` | Blend the base and action contributions. |

## Enumeration Descriptions

<a id="member-969abf680a1c"></a>
### Add

`public const Electron2D.AnimationMixMode Add = 1`

Add the action to the complete base contribution.

<a id="member-f41951c7b4b7"></a>
### Blend

`public const Electron2D.AnimationMixMode Blend = 0`

Blend the base and action contributions.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) and [action controllers](../components/scene-animation.md#action-controllers) define this executed contract. AnimationActionTests covers default/edit/copy/curve semantics, requests/timers/filtering/loops, named/interrupted/self/automatic transitions, shared instances, external/internal seeks, test-only behavior, callback failure/reentry and 256 warmed restarted/interrupted frame passes with deferred events and zero managed bytes. Two Wayland GPU and two compatibility public Engine.Run cycles each verify thirteen composed poses and borrowed-resource cleanup. Cold schema/binding/input/notice-capacity preparation may allocate; manual advances without a flush can grow deferred capacity. Native/driver allocations, other platforms and human visual acceptance remain unmeasured. State-machine/grouped/expression control, event-track schedulers and disk/editor round trips remain separate coverage dependencies.
