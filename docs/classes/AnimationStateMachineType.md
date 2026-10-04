# AnimationStateMachineType

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AnimationStateMachineType` · **Source:** [AnimationNodeStateMachine.cs](../../src/Scene/Animation/AnimationNodeStateMachine.cs).

## Description

Selects state-machine startup, completion and ancestry semantics.

One semantic enumeration defines Root=0, Nested=1 and Grouped=2 across all state-machine consumers. Undefined values reject in StateMachineType. Root internal seek resets the machine; Nested retains its current state on reset and reports terminal clip time; Grouped is controlled through a machine parent.

See [state machines](../components/scene-animation.md#state-machines) for runtime order, integration and verification boundaries. Scene controllers execute on their SceneTree owner thread; resource authoring retains the Resource thread/lifetime contract. No external backend dependency or second scene clock is introduced.

## Example

This snippet requires the indicated live tree, borrowed clip definitions and library/target setup. AnimationStateMachineTests compiles and exercises this public workflow.

```csharp
machine.StateMachineType = AnimationStateMachineType.Nested;
```

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.AnimationStateMachineType Grouped = 2` | A parent machine controls this group through its Start and End boundary edges. |
| `public const Electron2D.AnimationStateMachineType Nested = 1` | Internal seek resets the current state; a terminal state also reports completion. |
| `public const Electron2D.AnimationStateMachineType Root = 0` | Internal seek to zero restarts at Start; End exits the machine. |

## Enumeration Descriptions

<a id="member-71db7b804155"></a>
### Grouped

`public const Electron2D.AnimationStateMachineType Grouped = 2`

A parent machine controls this group through its Start and End boundary edges.

<a id="member-8debffd3cdba"></a>
### Nested

`public const Electron2D.AnimationStateMachineType Nested = 1`

Internal seek resets the current state; a terminal state also reports completion.

<a id="member-f4fbc9851b2e"></a>
### Root

`public const Electron2D.AnimationStateMachineType Root = 0`

Internal seek to zero restarts at Start; End exits the machine.


## Verification and dependencies

[AnimationStateMachineTests](../../tests/Electron2D.Tests/AnimationStateMachineTests.cs) covers public authoring, defaults, edge guards, copies/aliases, route costs/fallback, timing/fades/boundaries, typed conditions, multiple groups, state events, independent sharing, rename/removal, preparation and observer failures, reentry, tested output and method/nested effects. Warm checks use 64 prepared command/route cycles, 64 cached grouped starts and 64 idle frames with zero owner managed bytes. Separate SDL-dummy/FAudio PCM checks verify state audio start/restart/stop and departed nonblended cues. Linux Wayland GPU/compatibility hosts each run twice and check six actual pixel poses and borrowed-resource cleanup. Native/driver allocator totals, physical listening, other platforms, file/editor and human acceptance remain unverified. [ADR 0093](../decisions/scene-animation.md#adr-0093) defines the typed graph/condition/ownership contract.
