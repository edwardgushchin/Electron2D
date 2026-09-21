# NodeProcessMode

Last updated: 2026-09-21

## Declaration

- Source: [`NodeProcessMode.cs`](../../src/Scene/Main/NodeProcessMode.cs)
- Namespace: `Electron2D`
- Declaration: `public enum NodeProcessMode`
- Domain: [Scene](../domains/scene.md)
- Component: [Unified 2D node](../components/unified-node.md)

## Responsibility and ownership

`NodeProcessMode` is the typed pause policy stored by each [`Node`](Node.md). It owns no resources and has no lifecycle. [`SceneTree`](SceneTree.md) resolves it before each process or physics-process callback.

## Public API

| Value | Numeric value | Behavior |
| --- | ---: | --- |
| `Inherit` | `0` | Uses the nearest ancestor's resolved mode; a root resolves to `Pausable` |
| `Pausable` | `1` | Runs only while `SceneTree.Paused` is `false` |
| `WhenPaused` | `2` | Runs only while `SceneTree.Paused` is `true` |
| `Always` | `3` | Runs in either pause state |
| `Disabled` | `4` | Never runs and makes inheriting descendants disabled |

There is no protected API.

## Invariants and errors

`Node.ProcessMode` rejects undefined enum values with `ArgumentOutOfRangeException`. Effective transitions into or out of `Disabled` synchronously deliver `NotificationDisabled` or `NotificationEnabled` to the affected node and inheriting descendants.

## Threading and interactions

Changing the mode of an attached node is an owner-thread mutation. Detached nodes have no tree pause state, but `CanProcess()` still returns `false` until attachment. The enum depends on no other production type; `Node` and `SceneTree` interpret it.

## Verification and limitations

The executable check verifies `Pausable`, `WhenPaused`, `Always`, inherited `Disabled`, and the corresponding transition notifications. The mode controls only Electron2D's two explicit callback lanes; it does not imply rendering, input, or physics-engine activity.
