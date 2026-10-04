# AnimationAdvanceMode

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AnimationAdvanceMode` · **Source:** [AnimationNodeStateMachineTransition.cs](../../src/Scene/Animation/AnimationNodeStateMachineTransition.cs).

## Description

Selects whether an edge participates in travel and automatic advancement.

One semantic enumeration defines Disabled=0, Enabled=1 and Auto=2. Disabled edges cannot be travelled or automatically selected; Enabled admits explicit routing; Auto additionally evaluates the owning typed condition/predicate. Undefined values reject.

See [state machines](../components/scene-animation.md#state-machines) for runtime order, integration and verification boundaries. Scene controllers execute on their SceneTree owner thread; resource authoring retains the Resource thread/lifetime contract. No external backend dependency or second scene clock is introduced.

## Example

This snippet requires the indicated live tree, borrowed clip definitions and library/target setup. AnimationStateMachineTests compiles and exercises this public workflow.

```csharp
transition.AdvanceMode = AnimationAdvanceMode.Auto;
```

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.AnimationAdvanceMode Auto = 2` | Also allow automatic advancement when all typed conditions pass. |
| `public const Electron2D.AnimationAdvanceMode Disabled = 0` | Exclude the edge from travel and automatic advancement. |
| `public const Electron2D.AnimationAdvanceMode Enabled = 1` | Allow explicit travel along the edge. |

## Enumeration Descriptions

<a id="member-99c3a6fefb17"></a>
### Auto

`public const Electron2D.AnimationAdvanceMode Auto = 2`

Also allow automatic advancement when all typed conditions pass.

<a id="member-045943ad7e06"></a>
### Disabled

`public const Electron2D.AnimationAdvanceMode Disabled = 0`

Exclude the edge from travel and automatic advancement.

<a id="member-fafa571f0de1"></a>
### Enabled

`public const Electron2D.AnimationAdvanceMode Enabled = 1`

Allow explicit travel along the edge.


## Verification and dependencies

[AnimationStateMachineTests](../../tests/Electron2D.Tests/AnimationStateMachineTests.cs) covers public authoring, defaults, edge guards, copies/aliases, route costs/fallback, timing/fades/boundaries, typed conditions, multiple groups, state events, independent sharing, rename/removal, preparation and observer failures, reentry, tested output and method/nested effects. Warm checks use 64 prepared command/route cycles, 64 cached grouped starts and 64 idle frames with zero owner managed bytes. Separate SDL-dummy/FAudio PCM checks verify state audio start/restart/stop and departed nonblended cues. Linux Wayland GPU/compatibility hosts each run twice and check six actual pixel poses and borrowed-resource cleanup. Native/driver allocator totals, physical listening, other platforms, file/editor and human acceptance remain unverified. [ADR 0093](../decisions/scene-animation.md#adr-0093) defines the typed graph/condition/ownership contract.
