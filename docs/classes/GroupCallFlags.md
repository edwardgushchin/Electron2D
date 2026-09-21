# GroupCallFlags

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Scene/Main/GroupCallFlags.cs`](../../src/Scene/Main/GroupCallFlags.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum GroupCallFlags`

> Controls ordering and scheduling for typed scene-group operations.

## Description

Controls ordering and scheduling for typed scene-group operations.

`GroupCallFlags` controls scheduling, hierarchy order, and coalescing for `SceneTree.CallGroup`, `SetGroup`, and `NotifyGroup`.

Public group methods reject unknown bits and require [`GroupCallFlags.Unique`](GroupCallFlags.md#f-electron2d-groupcallflags-unique) to be combined with
[`GroupCallFlags.Deferred`](GroupCallFlags.md#f-electron2d-groupcallflags-deferred). Unique identity is the operation kind, group, and delegate or notification identifier;
later setter values and other supported flags do not replace the first accepted operation.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = GroupCallFlags.Default;
```

## Constants

| Member | Description |
| --- | --- |
| [`Default = 0`](#f-electron2d-groupcallflags-default) | Executes immediately in hierarchy order. |
| [`Reverse = 1`](#f-electron2d-groupcallflags-reverse) | Visits descendants before their ancestors by reversing hierarchy order. |
| [`Deferred = 2`](#f-electron2d-groupcallflags-deferred) | Queues the operation for a future deferred flush instead of executing it immediately. |
| [`Unique = 4`](#f-electron2d-groupcallflags-unique) | Coalesces equal deferred operations until their queued callback starts. |

## Constant Descriptions

<a id="f-electron2d-groupcallflags-default"></a>
### `Default = 0`

Executes immediately in hierarchy order.

<a id="f-electron2d-groupcallflags-reverse"></a>
### `Reverse = 1`

Visits descendants before their ancestors by reversing hierarchy order.

<a id="f-electron2d-groupcallflags-deferred"></a>
### `Deferred = 2`

Queues the operation for a future deferred flush instead of executing it immediately.

<a id="f-electron2d-groupcallflags-unique"></a>
### `Unique = 4`

Coalesces equal deferred operations until their queued callback starts. This value requires
[`GroupCallFlags.Deferred`](GroupCallFlags.md#f-electron2d-groupcallflags-deferred); the first operation's captured arguments are retained.

## Lifecycle, invariants, and threading

The enum owns no state. Immediate operations require the tree owner thread. Deferred combinations may be submitted concurrently because the owning tree serializes queue acceptance and uniqueness with disposal.

## Dependencies and verification

The type depends only on `System.FlagsAttribute` and is consumed by `SceneTree`. Executable checks cover numeric behavior through default, reverse, deferred-unique, and invalid `Unique`-without-`Deferred` operations. Unknown-bit rejection is specified by `SceneTree` and covered by its argument-validation contract.

## Known limitations

Uniqueness is scoped to one tree and lasts only until the queued wrapper starts. It is not a persistent connection or cross-frame cache.
