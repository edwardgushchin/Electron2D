# Tween.TweenProcessMode

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Scene/Animation/Tween.cs`](../../src/Scene/Animation/Tween.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Tween.TweenProcessMode`

> Selects the frame lane that advances a tween.

## Description

Selects the frame lane that advances a tween.

This nested enum selects the SceneTree frame lane for a [`Tween`](Tween.md). `Physics = 0` advances after physics-node callbacks and matching tree timers; `Idle = 1` advances after ordinary process-node callbacks and matching timers and is the default. Undefined values are rejected without state change. Lane mutation is owner-thread-only; changing it during an earlier phase can affect which later frame first captures the tween. Tests verify numeric identities and lane isolation. There is no physics simulation implication: the enum selects only callback timing.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = Tween.TweenProcessMode.Physics;
```

## Constants

| Member | Description |
| --- | --- |
| [`Physics = 0`](#f-electron2d-tween-tweenprocessmode-physics) | Advances after physics-frame node callbacks and timers. |
| [`Idle = 1`](#f-electron2d-tween-tweenprocessmode-idle) | Advances after process-frame node callbacks and timers. |

## Constant Descriptions

<a id="f-electron2d-tween-tweenprocessmode-physics"></a>
### `Physics = 0`

Advances after physics-frame node callbacks and timers.

<a id="f-electron2d-tween-tweenprocessmode-idle"></a>
### `Idle = 1`

Advances after process-frame node callbacks and timers.
