# GroupCallFlags

Last updated: 2026-09-21

## Declaration

- Source: [`GroupCallFlags.cs`](../../src/Scene/Main/GroupCallFlags.cs)
- Namespace: `Electron2D`
- Declaration: `[Flags] public enum GroupCallFlags`
- Domain: [Scene](../domains/scene.md)
- Component: [Scene tree](../components/scene-tree.md)

## Responsibility

`GroupCallFlags` controls scheduling, hierarchy order, and coalescing for `SceneTree.CallGroup`, `SetGroup`, and `NotifyGroup`.

## Complete public API

| Value | Numeric value | Behavior |
| --- | ---: | --- |
| `Default` | `0` | Immediate hierarchy pre-order |
| `Reverse` | `1` | Reverses the selected pre-order so descendants precede ancestors |
| `Deferred` | `2` | Queues the operation for a future deferred flush |
| `Unique` | `4` | Coalesces an equal deferred operation until its queued callback begins; requires `Deferred` |

Unknown bits are rejected. For unique setters, differing captured values do not distinguish operations, so the first accepted value is retained.

## Lifecycle, invariants, and threading

The enum owns no state. Immediate operations require the tree owner thread. Deferred combinations may be submitted concurrently because the owning tree serializes queue acceptance and uniqueness with disposal.

## Dependencies and verification

The type depends only on `System.FlagsAttribute` and is consumed by `SceneTree`. Executable checks cover numeric behavior through default, reverse, deferred-unique, and invalid `Unique`-without-`Deferred` operations. Unknown-bit rejection is specified by `SceneTree` and covered by its argument-validation contract.

## Known limitations

Uniqueness is scoped to one tree and lasts only until the queued wrapper starts. It is not a persistent connection or cross-frame cache.
