# TimerProcessCallback

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Scene/Main/TimerProcessCallback.cs`](../../src/Scene/Main/TimerProcessCallback.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum TimerProcessCallback`

> Specifies which scene-tree frame lane advances a [`Timer`](Timer.md).

## Description

Specifies which scene-tree frame lane advances a [`Timer`](Timer.md).

`TimerProcessCallback` selects the `SceneTree` frame lane that advances a [`Timer`](Timer.md). It is an immutable value and owns no resources.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = TimerProcessCallback.Physics;
```

## Constants

| Member | Description |
| --- | --- |
| [`Physics = 0`](#f-electron2d-timerprocesscallback-physics) | Advances the timer during fixed-step physics-process frames. |
| [`Idle = 1`](#f-electron2d-timerprocesscallback-idle) | Advances the timer during variable-step process frames. |

## Constant Descriptions

<a id="f-electron2d-timerprocesscallback-physics"></a>
### `Physics = 0`

Advances the timer during fixed-step physics-process frames.

<a id="f-electron2d-timerprocesscallback-idle"></a>
### `Idle = 1`

Advances the timer during variable-step process frames.

## Lifecycle, errors, and threading

The enum has no lifecycle or threading behavior. Validation and owner-thread mutation guarantees belong to `Timer`. The numeric identities are stable for typed property and future persistence contracts.

## Dependencies and interactions

The enum depends on no other type. `Timer` consumes it; `PackedScene` stores it as a reference-free value through a typed descriptor.

## Verification and known limitations

Executable checks cover both numeric identities, default value, invalid-value rejection, running lane migration, and packed-scene restoration. There is no arbitrary custom lane and no background or wall-clock mode.

## Relevant decision

- [0036: Reusable SceneNode timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
