# ProcessMode

Last updated: 2026-09-26

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Scene/Main/ProcessMode.cs`](../../src/Scene/Main/ProcessMode.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum ProcessMode`

> Controls when a node receives process and physics-process callbacks.

## Description

Controls when a node receives process and physics-process callbacks.

`ProcessMode` is the typed pause policy stored by each [`Node`](Node.md). It owns no resources and has no lifecycle. [`SceneTree`](SceneTree.md) resolves it before each process, physics-process, or scene-input callback.

The values affect Electron2D's explicitly enabled host-driven process, physics-process, and scene-input callback
lanes. Effective Disabled processing also applies each scene [CollisionObject.DisableMode](CollisionObject.md#disablemode); pausing alone does not. Rendering and audio remain independent.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = ProcessMode.Inherit;
```

## Constants

| Member | Description |
| --- | --- |
| [`Inherit = 0`](#f-electron2d-processmode-inherit) | Uses the nearest ancestor's resolved mode; hierarchy roots resolve to [`ProcessMode.Pausable`](ProcessMode.md#f-electron2d-processmode-pausable). |
| [`Pausable = 1`](#f-electron2d-processmode-pausable) | Runs only while the scene tree is not paused. |
| [`WhenPaused = 2`](#f-electron2d-processmode-whenpaused) | Runs only while the scene tree is paused. |
| [`Always = 3`](#f-electron2d-processmode-always) | Runs regardless of the scene tree pause state. |
| [`Disabled = 4`](#f-electron2d-processmode-disabled) | Never runs and disables descendants that inherit this mode. |

## Constant Descriptions

<a id="f-electron2d-processmode-inherit"></a>
### `Inherit = 0`

Uses the nearest ancestor's resolved mode; hierarchy roots resolve to [`ProcessMode.Pausable`](ProcessMode.md#f-electron2d-processmode-pausable).

<a id="f-electron2d-processmode-pausable"></a>
### `Pausable = 1`

Runs only while the scene tree is not paused.

<a id="f-electron2d-processmode-whenpaused"></a>
### `WhenPaused = 2`

Runs only while the scene tree is paused.

<a id="f-electron2d-processmode-always"></a>
### `Always = 3`

Runs regardless of the scene tree pause state.

<a id="f-electron2d-processmode-disabled"></a>
### `Disabled = 4`

Never runs and disables descendants that inherit this mode.

## Invariants and errors

`Node.ProcessMode` rejects undefined enum values with `ArgumentOutOfRangeException`. Effective transitions into or out of `Disabled` synchronously deliver `NotificationDisabled` or `NotificationEnabled` to the affected node and inheriting descendants.

## Threading and interactions

Changing the mode of an attached node is an owner-thread mutation. Detached nodes have no tree pause state, but `CanProcess()` still returns `false` until attachment. The enum depends on no other production type; `Node` and `SceneTree` interpret it.

## Verification and limitations

The executable check verifies `Pausable`, `WhenPaused`, `Always`, inherited `Disabled`, the corresponding transition notifications, and paused input suppression. The mode controls the three explicit process, physics-process, and scene-input callback families; it does not imply rendering, audio, or physics-engine activity.
