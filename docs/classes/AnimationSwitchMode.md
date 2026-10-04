# AnimationSwitchMode

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AnimationSwitchMode` · **Source:** [AnimationNodeStateMachineTransition.cs](../../src/Scene/Animation/AnimationNodeStateMachineTransition.cs).

## Description

Selects when a state-machine edge activates its destination timeline.

One semantic enumeration defines Immediate=0, Sync=1 and AtEnd=2. Immediate selects when requested/automatic; Sync seeks the destination to the source position; AtEnd waits for the finite fade threshold and optionally a looping cycle endpoint. Undefined values reject.

See [state machines](../components/scene-animation.md#state-machines) for runtime order, integration and verification boundaries. Scene controllers execute on their SceneTree owner thread; resource authoring retains the Resource thread/lifetime contract. No external backend dependency or second scene clock is introduced.

## Example

This snippet requires the indicated live tree, borrowed clip definitions and library/target setup. AnimationStateMachineTests compiles and exercises this public workflow.

```csharp
transition.SwitchMode = AnimationSwitchMode.Sync;
```

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.AnimationSwitchMode AtEnd = 2` | Wait until the source has at most the crossfade duration remaining. |
| `public const Electron2D.AnimationSwitchMode Immediate = 0` | Switch as soon as the edge is selected. |
| `public const Electron2D.AnimationSwitchMode Sync = 1` | Seek the destination to the source position before continuing. |

## Enumeration Descriptions

<a id="member-20313855e686"></a>
### AtEnd

`public const Electron2D.AnimationSwitchMode AtEnd = 2`

Wait until the source has at most the crossfade duration remaining.

<a id="member-1fecd8b4b92e"></a>
### Immediate

`public const Electron2D.AnimationSwitchMode Immediate = 0`

Switch as soon as the edge is selected.

<a id="member-8bbd3a5377df"></a>
### Sync

`public const Electron2D.AnimationSwitchMode Sync = 1`

Seek the destination to the source position before continuing.


## Verification and dependencies

[AnimationStateMachineTests](../../tests/Electron2D.Tests/AnimationStateMachineTests.cs) covers public authoring, defaults, edge guards, copies/aliases, route costs/fallback, timing/fades/boundaries, typed conditions, multiple groups, state events, independent sharing, rename/removal, preparation and observer failures, reentry, tested output and method/nested effects. Warm checks use 64 prepared command/route cycles, 64 cached grouped starts and 64 idle frames with zero owner managed bytes. Separate SDL-dummy/FAudio PCM checks verify state audio start/restart/stop and departed nonblended cues. Linux Wayland GPU/compatibility hosts each run twice and check six actual pixel poses and borrowed-resource cleanup. Native/driver allocator totals, physical listening, other platforms, file/editor and human acceptance remain unverified. [ADR 0093](../decisions/scene-animation.md#adr-0093) defines the typed graph/condition/ownership contract.
