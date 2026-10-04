# AnimationOneShotRequest

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AnimationOneShotRequest` · **Source:** [AnimationNodeOneShot.cs](../../src/Scene/Animation/AnimationNodeOneShot.cs).

## Description

Requests an action from a one-shot animation controller.

This domain belongs to [AnimationNodeOneShot](AnimationNodeOneShot.md). Its default, request lifetime, blend/filter semantics and failure behavior are documented on that class and [the component](../components/scene-animation.md#action-controllers).

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.AnimationOneShotRequest Abort = 2` | Stop immediately and cancel automatic restart. |
| `public const Electron2D.AnimationOneShotRequest FadeOut = 3` | Fade out and cancel the pending automatic restart. |
| `public const Electron2D.AnimationOneShotRequest Fire = 1` | Start or restart the action. |
| `public const Electron2D.AnimationOneShotRequest None = 0` | Make no request. |

## Enumeration Descriptions

<a id="member-42f9b1105ae6"></a>
### Abort

`public const Electron2D.AnimationOneShotRequest Abort = 2`

Stop immediately and cancel automatic restart.

<a id="member-5f962042dc15"></a>
### FadeOut

`public const Electron2D.AnimationOneShotRequest FadeOut = 3`

Fade out and cancel the pending automatic restart.

<a id="member-9eb83d754420"></a>
### Fire

`public const Electron2D.AnimationOneShotRequest Fire = 1`

Start or restart the action.

<a id="member-88359603f0c9"></a>
### None

`public const Electron2D.AnimationOneShotRequest None = 0`

Make no request.

## Lifecycle, verification and dependencies

[ADR 0093](../decisions/scene-animation.md#adr-0093) and [action controllers](../components/scene-animation.md#action-controllers) define this executed contract. AnimationActionTests covers default/edit/copy/curve semantics, requests/timers/filtering/loops, named/interrupted/self/automatic transitions, shared instances, external/internal seeks, test-only behavior, callback failure/reentry and 256 warmed restarted/interrupted frame passes with deferred events and zero managed bytes. Two Wayland GPU and two compatibility public Engine.Run cycles each verify thirteen composed poses and borrowed-resource cleanup. Cold schema/binding/input/notice-capacity preparation may allocate; manual advances without a flush can grow deferred capacity. Native/driver allocations, other platforms and human visual acceptance remain unmeasured. State-machine/grouped/expression control, event-track schedulers and disk/editor round trips remain separate coverage dependencies.
