# TimerProcessCallback

Last updated: 2026-09-21

## Declaration

- Source: [`TimerProcessCallback.cs`](../../src/Scene/Main/TimerProcessCallback.cs)
- Namespace: `Electron2D`
- Declaration: `public enum TimerProcessCallback`
- Domain: [Scene](../domains/scene.md)
- Component: [Scene tree](../components/scene-tree.md)

## Responsibility and ownership

`TimerProcessCallback` selects the `SceneTree` frame lane that advances a [`Timer`](Timer.md). It is an immutable value and owns no resources.

## Complete public API

| Value | Numeric identity | Current behavior |
| --- | ---: | --- |
| `Physics` | `0` | Advances during eligible fixed-step physics frames |
| `Idle` | `1` | Advances during eligible variable-step process frames; the default |

Undefined values are rejected by `Timer.ProcessCallback` and by its typed property descriptor. Changing a running timer's value atomically moves its internal scheduling lane without resetting remaining time.

## Lifecycle, errors, and threading

The enum has no lifecycle or threading behavior. Validation and owner-thread mutation guarantees belong to `Timer`. The numeric identities are stable for typed property and future persistence contracts.

## Dependencies and interactions

The enum depends on no other type. `Timer` consumes it; `PackedScene` stores it as a reference-free value through a typed descriptor.

## Verification and known limitations

Executable checks cover both numeric identities, default value, invalid-value rejection, running lane migration, and packed-scene restoration. There is no arbitrary custom lane and no background or wall-clock mode.

## Relevant decision

- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
